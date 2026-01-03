using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine.Handlers;

/// <summary>
/// Handles the River phase - deal 5th community card, final betting round.
/// </summary>
public sealed class RiverPhaseHandler : BasePhaseHandler
{
    public RiverPhaseHandler(ILogger<RiverPhaseHandler> logger) : base(logger)
    {
    }

    public override HandPhase Phase => HandPhase.River;

    public override Task OnEnterAsync(Hand hand, PhaseTransitionContext context)
    {
        Logger.LogInformation(
            "Hand {HandId} entering river. Community cards count: {CardCount}",
            hand.Id, hand.CommunityCards.Count);

        // Reset betting for new round
        hand.CurrentBet = 0;
        hand.MinRaise = hand.BigBlindAmount;
        hand.RaisesThisRound = 0;

        return base.OnEnterAsync(hand, context);
    }

    public override PhaseTransitionValidation ValidateTransition(Hand hand, HandPhase targetPhase)
    {
        var baseValidation = base.ValidateTransition(hand, targetPhase);
        if (!baseValidation.IsValid)
        {
            return baseValidation;
        }

        // When leaving river, we should have 5 community cards (all dealt)
        if (targetPhase == HandPhase.Showdown && hand.CommunityCards.Count < 5)
        {
            return PhaseTransitionValidation.Invalid(
                $"Expected 5 community cards for showdown, but have {hand.CommunityCards.Count}");
        }

        return PhaseTransitionValidation.Valid();
    }
}
