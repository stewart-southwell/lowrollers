using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine;

/// <summary>
/// Manages state transitions for a poker hand with validation, guards, and logging.
/// </summary>
public sealed partial class HandStateMachine
{
    private readonly ILogger<HandStateMachine> _logger;
    private readonly Dictionary<HandPhase, IHandPhaseHandler> _handlers;
    private readonly List<HandStateTransition> _transitionHistory = [];

    // Valid state transitions: current phase -> allowed next phases
    private static readonly Dictionary<HandPhase, HashSet<HandPhase>> ValidTransitions =
        new()
        {
            [HandPhase.Waiting] = [HandPhase.Preflop],
            [HandPhase.Preflop] = [HandPhase.Flop, HandPhase.Showdown, HandPhase.Complete],
            [HandPhase.Flop] = [HandPhase.Turn, HandPhase.Showdown, HandPhase.Complete],
            [HandPhase.Turn] = [HandPhase.River, HandPhase.Showdown, HandPhase.Complete],
            [HandPhase.River] = [HandPhase.Showdown, HandPhase.Complete],
            [HandPhase.Showdown] = [HandPhase.Complete],
            [HandPhase.Complete] = [] // Terminal state
        };

    /// <summary>
    /// Creates a new hand state machine with the given handlers.
    /// </summary>
    public HandStateMachine(
        IEnumerable<IHandPhaseHandler> handlers,
        ILogger<HandStateMachine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _handlers = handlers?.ToDictionary(h => h.Phase)
            ?? throw new ArgumentNullException(nameof(handlers));
    }

    /// <summary>
    /// Gets the transition history for the current session.
    /// </summary>
    public IReadOnlyList<HandStateTransition> TransitionHistory => _transitionHistory.AsReadOnly();

    /// <summary>
    /// Checks if a transition from the current phase to the target phase is structurally valid.
    /// </summary>
    /// <param name="currentPhase">The current phase.</param>
    /// <param name="targetPhase">The target phase.</param>
    /// <returns>True if the transition is allowed by the state machine.</returns>
    public static bool IsTransitionValid(HandPhase currentPhase, HandPhase targetPhase)
    {
        return ValidTransitions.TryGetValue(currentPhase, out var allowed)
               && allowed.Contains(targetPhase);
    }

    /// <summary>
    /// Gets all valid next phases from the given phase.
    /// </summary>
    public static IReadOnlySet<HandPhase> GetValidTransitions(HandPhase currentPhase)
    {
        return ValidTransitions.TryGetValue(currentPhase, out var allowed)
            ? allowed
            : new HashSet<HandPhase>();
    }

    /// <summary>
    /// Attempts to transition the hand to a new phase.
    /// </summary>
    /// <param name="hand">The hand to transition.</param>
    /// <param name="targetPhase">The phase to transition to.</param>
    /// <param name="trigger">The reason for the transition.</param>
    /// <param name="context">Optional context for the transition.</param>
    /// <returns>Result of the transition attempt.</returns>
    public async Task<TransitionResult> TransitionAsync(
        Hand hand,
        HandPhase targetPhase,
        TransitionTrigger trigger,
        PhaseTransitionContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(hand);

        var currentPhase = hand.Phase;
        context ??= new PhaseTransitionContext { Trigger = trigger };

        Log.AttemptingTransition(_logger, hand.Id, currentPhase, targetPhase, trigger);

        // Guard: Check if transition is structurally valid
        if (!IsTransitionValid(currentPhase, targetPhase))
        {
            var error = $"Invalid transition from {currentPhase} to {targetPhase}";
            Log.TransitionDenied(_logger, hand.Id, error);

            return TransitionResult.Failure(error);
        }

        // Guard: Run phase-specific validation
        if (_handlers.TryGetValue(currentPhase, out var currentHandler))
        {
            var validation = currentHandler.ValidateTransition(hand, targetPhase);
            if (!validation.IsValid)
            {
                var errors = string.Join(", ", validation.Errors ?? []);
                Log.TransitionValidationFailed(_logger, hand.Id, errors);

                return TransitionResult.Failure(errors);
            }
        }

        // Execute exit handler for current phase
        if (_handlers.TryGetValue(currentPhase, out currentHandler))
        {
            try
            {
                await currentHandler.OnExitAsync(hand, context);
                Log.ExitedPhase(_logger, currentPhase, hand.Id);
            }
            catch (Exception ex)
            {
                Log.ErrorDuringPhaseExit(_logger, ex, currentPhase, hand.Id);
                return TransitionResult.Failure($"Error exiting {currentPhase}: {ex.Message}");
            }
        }

        // Perform the transition
        var previousPhase = hand.Phase;
        hand.Phase = targetPhase;

        // Reset betting state when entering a new betting round
        if (IsBettingRound(targetPhase))
        {
            hand.CurrentBet = 0;
            hand.RaisesThisRound = 0;
        }

        // Mark completion time
        if (targetPhase == HandPhase.Complete)
        {
            hand.CompletedAt = DateTimeOffset.UtcNow;
        }

        // Record the transition
        var transition = HandStateTransition.Create(previousPhase, targetPhase, trigger);
        _transitionHistory.Add(transition);

        Log.HandTransitioned(_logger, hand.Id, previousPhase, targetPhase, trigger);

        // Execute enter handler for new phase
        if (_handlers.TryGetValue(targetPhase, out var newHandler))
        {
            try
            {
                await newHandler.OnEnterAsync(hand, context);
                Log.EnteredPhase(_logger, targetPhase, hand.Id);
            }
            catch (Exception ex)
            {
                Log.ErrorDuringPhaseEntry(_logger, ex, targetPhase, hand.Id);
                // Note: We don't roll back the transition - the phase change has happened
                // The error should be handled by retry logic at a higher level
            }
        }

        return TransitionResult.Success(transition);
    }

    /// <summary>
    /// Determines the next phase based on the current state and trigger.
    /// </summary>
    public static HandPhase? DetermineNextPhase(Hand hand, TransitionTrigger trigger)
    {
        return (hand.Phase, trigger) switch
        {
            (HandPhase.Waiting, TransitionTrigger.StartHand) => HandPhase.Preflop,

            (HandPhase.Preflop, TransitionTrigger.BettingComplete) => HandPhase.Flop,
            (HandPhase.Flop, TransitionTrigger.BettingComplete) => HandPhase.Turn,
            (HandPhase.Turn, TransitionTrigger.BettingComplete) => HandPhase.River,
            (HandPhase.River, TransitionTrigger.BettingComplete) => HandPhase.Showdown,

            (_, TransitionTrigger.AllFolded) when hand.Phase != HandPhase.Complete => HandPhase.Complete,

            (HandPhase.Showdown, TransitionTrigger.ShowdownComplete) => HandPhase.Complete,

            (_, TransitionTrigger.ForceEnd) when hand.Phase != HandPhase.Complete => HandPhase.Complete,

            _ => null
        };
    }

    /// <summary>
    /// Attempts to advance the hand based on the given trigger.
    /// </summary>
    public async Task<TransitionResult> AdvanceAsync(
        Hand hand,
        TransitionTrigger trigger,
        PhaseTransitionContext? context = null)
    {
        var nextPhase = DetermineNextPhase(hand, trigger);
        if (nextPhase is null)
        {
            return TransitionResult.Failure($"No valid transition for trigger {trigger} from phase {hand.Phase}");
        }

        return await TransitionAsync(hand, nextPhase.Value, trigger, context);
    }

    private static bool IsBettingRound(HandPhase phase)
        => phase is HandPhase.Preflop or HandPhase.Flop or HandPhase.Turn or HandPhase.River;

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Debug, Message = "Attempting transition: {HandId} from {CurrentPhase} to {TargetPhase} via {Trigger}")]
        public static partial void AttemptingTransition(ILogger logger, Guid handId, HandPhase currentPhase, HandPhase targetPhase, TransitionTrigger trigger);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Transition denied for {HandId}: {Error}")]
        public static partial void TransitionDenied(ILogger logger, Guid handId, string error);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Transition validation failed for {HandId}: {Errors}")]
        public static partial void TransitionValidationFailed(ILogger logger, Guid handId, string errors);

        [LoggerMessage(Level = LogLevel.Trace, Message = "Exited phase {Phase} for hand {HandId}")]
        public static partial void ExitedPhase(ILogger logger, HandPhase phase, Guid handId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error during exit from phase {Phase} for hand {HandId}")]
        public static partial void ErrorDuringPhaseExit(ILogger logger, Exception ex, HandPhase phase, Guid handId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Hand {HandId} transitioned from {FromPhase} to {ToPhase} via {Trigger}")]
        public static partial void HandTransitioned(ILogger logger, Guid handId, HandPhase fromPhase, HandPhase toPhase, TransitionTrigger trigger);

        [LoggerMessage(Level = LogLevel.Trace, Message = "Entered phase {Phase} for hand {HandId}")]
        public static partial void EnteredPhase(ILogger logger, HandPhase phase, Guid handId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error during entry to phase {Phase} for hand {HandId}")]
        public static partial void ErrorDuringPhaseEntry(ILogger logger, Exception ex, HandPhase phase, Guid handId);
    }
}

/// <summary>
/// Result of a state transition attempt.
/// </summary>
public readonly record struct TransitionResult
{
    /// <summary>
    /// Whether the transition was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if the transition failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// The transition that occurred (if successful).
    /// </summary>
    public HandStateTransition? Transition { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static TransitionResult Success(HandStateTransition transition)
        => new() { IsSuccess = true, Transition = transition };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static TransitionResult Failure(string error)
        => new() { IsSuccess = false, Error = error };
}
