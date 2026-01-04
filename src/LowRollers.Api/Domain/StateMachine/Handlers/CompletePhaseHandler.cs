using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine.Handlers;

/// <summary>
/// Handles the Complete phase - hand finished, pot awarded.
/// </summary>
public sealed class CompletePhaseHandler : BasePhaseHandler
{
    public CompletePhaseHandler(ILogger<CompletePhaseHandler> logger) : base(logger)
    {
    }

    public override HandPhase Phase => HandPhase.Complete;

    public override Task OnEnterAsync(Hand hand, PhaseTransitionContext context)
    {
        hand.CompletedAt = DateTimeOffset.UtcNow;

        Logger.LogInformation(
            "Hand {HandId} complete. Duration: {Duration:g}",
            hand.Id, hand.CompletedAt - hand.StartedAt);

        return base.OnEnterAsync(hand, context);
    }

    public override PhaseTransitionValidation ValidateTransition(Hand hand, HandPhase targetPhase)
    {
        // Complete is a terminal state - no transitions allowed
        return PhaseTransitionValidation.Invalid("Cannot transition from Complete phase");
    }
}
