using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine.Handlers;

/// <summary>
/// Base implementation of a hand phase handler with common functionality.
/// </summary>
public abstract partial class BasePhaseHandler : IHandPhaseHandler
{
    protected BasePhaseHandler(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected ILogger Logger { get; }

    /// <inheritdoc />
    public abstract HandPhase Phase { get; }

    /// <inheritdoc />
    public virtual Task OnEnterAsync(Hand hand, PhaseTransitionContext context)
    {
        Log.EnteringPhase(Logger, Phase, hand.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task OnExitAsync(Hand hand, PhaseTransitionContext context)
    {
        Log.ExitingPhase(Logger, Phase, hand.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual PhaseTransitionValidation ValidateTransition(Hand hand, HandPhase targetPhase)
    {
        // Default validation: check structural validity
        if (!HandStateMachine.IsTransitionValid(Phase, targetPhase))
        {
            return PhaseTransitionValidation.Invalid(
                $"Cannot transition from {Phase} to {targetPhase}");
        }

        return PhaseTransitionValidation.Valid();
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Debug, Message = "Entering {Phase} for hand {HandId}")]
        public static partial void EnteringPhase(ILogger logger, HandPhase phase, Guid handId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Exiting {Phase} for hand {HandId}")]
        public static partial void ExitingPhase(ILogger logger, HandPhase phase, Guid handId);
    }
}
