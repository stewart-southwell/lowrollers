using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Domain.StateMachine;

/// <summary>
/// Defines phase-specific logic for handling poker hand phases.
/// Each phase has entry, exit, and validation logic.
/// </summary>
public interface IHandPhaseHandler
{
    /// <summary>
    /// The phase this handler manages.
    /// </summary>
    HandPhase Phase { get; }

    /// <summary>
    /// Called when entering this phase.
    /// </summary>
    /// <param name="hand">The current hand state.</param>
    /// <param name="context">Contextual information for the transition.</param>
    /// <returns>A task representing the async operation.</returns>
    Task OnEnterAsync(Hand hand, PhaseTransitionContext context);

    /// <summary>
    /// Called when exiting this phase.
    /// </summary>
    /// <param name="hand">The current hand state.</param>
    /// <param name="context">Contextual information for the transition.</param>
    /// <returns>A task representing the async operation.</returns>
    Task OnExitAsync(Hand hand, PhaseTransitionContext context);

    /// <summary>
    /// Validates whether the hand can transition from this phase to the target phase.
    /// </summary>
    /// <param name="hand">The current hand state.</param>
    /// <param name="targetPhase">The phase to transition to.</param>
    /// <returns>Validation result with any error messages.</returns>
    PhaseTransitionValidation ValidateTransition(Hand hand, HandPhase targetPhase);
}

/// <summary>
/// Context information passed during phase transitions.
/// </summary>
public record PhaseTransitionContext
{
    /// <summary>
    /// The trigger that caused this transition.
    /// </summary>
    public TransitionTrigger Trigger { get; init; }

    /// <summary>
    /// Optional player ID associated with the transition (e.g., last to act).
    /// </summary>
    public Guid? PlayerId { get; init; }

    /// <summary>
    /// Additional metadata for the transition.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Result of validating a phase transition.
/// </summary>
public readonly record struct PhaseTransitionValidation
{
    /// <summary>
    /// Whether the transition is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Error messages if the transition is invalid.
    /// </summary>
    public IReadOnlyList<string>? Errors { get; init; }

    /// <summary>
    /// Creates a valid result.
    /// </summary>
    public static PhaseTransitionValidation Valid() => new() { IsValid = true };

    /// <summary>
    /// Creates an invalid result with error messages.
    /// </summary>
    public static PhaseTransitionValidation Invalid(params string[] errors)
        => new() { IsValid = false, Errors = errors };
}
