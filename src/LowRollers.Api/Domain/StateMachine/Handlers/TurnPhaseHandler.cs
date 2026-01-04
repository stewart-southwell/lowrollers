using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine.Handlers;

/// <summary>
/// Handles the Turn phase - deal 4th community card, third betting round.
/// </summary>
public sealed class TurnPhaseHandler : BasePhaseHandler
{
    public TurnPhaseHandler(ILogger<TurnPhaseHandler> logger) : base(logger)
    {
    }

    public override HandPhase Phase => HandPhase.Turn;

    public override Task OnEnterAsync(Hand hand, PhaseTransitionContext context)
    {
        Logger.LogInformation(
            "Hand {HandId} entering turn. Community cards count: {CardCount}",
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

        // When leaving turn, we should have at least 4 community cards (turn was dealt)
        // River card may or may not be dealt yet depending on when validation is called
        if (targetPhase == HandPhase.River && hand.CommunityCards.Count < 4)
        {
            return PhaseTransitionValidation.Invalid(
                $"Expected at least 4 community cards for turn, but have {hand.CommunityCards.Count}");
        }

        return PhaseTransitionValidation.Valid();
    }
}
