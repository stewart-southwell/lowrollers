using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine.Handlers;

/// <summary>
/// Handles the Complete phase - hand finished, pot awarded.
/// </summary>
public sealed partial class CompletePhaseHandler : BasePhaseHandler
{
    public CompletePhaseHandler(ILogger<CompletePhaseHandler> logger) : base(logger)
    {
    }

    public override HandPhase Phase => HandPhase.Complete;

    public override Task OnEnterAsync(Hand hand, PhaseTransitionContext context)
    {
        hand.CompletedAt = DateTimeOffset.UtcNow;

        Log.HandComplete(Logger, hand.Id, hand.CompletedAt.Value - hand.StartedAt);

        return base.OnEnterAsync(hand, context);
    }

    public override PhaseTransitionValidation ValidateTransition(Hand hand, HandPhase targetPhase)
    {
        // Complete is a terminal state - no transitions allowed
        return PhaseTransitionValidation.Invalid("Cannot transition from Complete phase");
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Hand {HandId} complete. Duration: {Duration}")]
        public static partial void HandComplete(ILogger logger, Guid handId, TimeSpan duration);
    }
}
