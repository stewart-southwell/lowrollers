using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine.Handlers;

/// <summary>
/// Handles the Showdown phase - reveal hands and determine winner(s).
/// </summary>
public sealed class ShowdownPhaseHandler : BasePhaseHandler
{
    public ShowdownPhaseHandler(ILogger<ShowdownPhaseHandler> logger) : base(logger)
    {
    }

    public override HandPhase Phase => HandPhase.Showdown;

    public override Task OnEnterAsync(Hand hand, PhaseTransitionContext context)
    {
        Logger.LogInformation(
            "Hand {HandId} entering showdown. Total pot: {TotalPot}",
            hand.Id, hand.TotalPot);

        return base.OnEnterAsync(hand, context);
    }

    public override Task OnExitAsync(Hand hand, PhaseTransitionContext context)
    {
        Logger.LogInformation(
            "Hand {HandId} showdown complete",
            hand.Id);

        return base.OnExitAsync(hand, context);
    }
}
