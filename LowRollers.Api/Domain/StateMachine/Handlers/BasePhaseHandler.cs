using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine.Handlers;

/// <summary>
/// Base implementation of a hand phase handler with common functionality.
/// </summary>
public abstract class BasePhaseHandler : IHandPhaseHandler
{
    protected readonly ILogger Logger;

    protected BasePhaseHandler(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public abstract HandPhase Phase { get; }

    /// <inheritdoc />
    public virtual Task OnEnterAsync(Hand hand, PhaseTransitionContext context)
    {
        Logger.LogDebug("Entering {Phase} for hand {HandId}", Phase, hand.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task OnExitAsync(Hand hand, PhaseTransitionContext context)
    {
        Logger.LogDebug("Exiting {Phase} for hand {HandId}", Phase, hand.Id);
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
}
