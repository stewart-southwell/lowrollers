using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine.Handlers;

/// <summary>
/// Handles the Showdown phase - reveal hands and determine winner(s).
/// </summary>
public sealed partial class ShowdownPhaseHandler : BasePhaseHandler
{
    public ShowdownPhaseHandler(ILogger<ShowdownPhaseHandler> logger) : base(logger)
    {
    }

    public override HandPhase Phase => HandPhase.Showdown;

    public override Task OnEnterAsync(Hand hand, PhaseTransitionContext context)
    {
        Log.EnteringShowdown(Logger, hand.Id, hand.TotalPot);

        return base.OnEnterAsync(hand, context);
    }

    public override Task OnExitAsync(Hand hand, PhaseTransitionContext context)
    {
        Log.ShowdownComplete(Logger, hand.Id);

        return base.OnExitAsync(hand, context);
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Hand {HandId} entering showdown. Total pot: {TotalPot}")]
        public static partial void EnteringShowdown(ILogger logger, Guid handId, decimal totalPot);

        [LoggerMessage(Level = LogLevel.Information, Message = "Hand {HandId} showdown complete")]
        public static partial void ShowdownComplete(ILogger logger, Guid handId);
    }
}
