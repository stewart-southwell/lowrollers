using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine.Handlers;

/// <summary>
/// Handles the Flop phase - deal 3 community cards, second betting round.
/// </summary>
public sealed class FlopPhaseHandler : BasePhaseHandler
{
    public FlopPhaseHandler(ILogger<FlopPhaseHandler> logger) : base(logger)
    {
    }

    public override HandPhase Phase => HandPhase.Flop;

    public override Task OnEnterAsync(Hand hand, PhaseTransitionContext context)
    {
        Logger.LogInformation(
            "Hand {HandId} entering flop. Community cards count: {CardCount}",
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

        // When leaving flop, we should have at least 3 community cards (flop was dealt)
        // Turn card may or may not be dealt yet depending on when validation is called
        if (targetPhase == HandPhase.Turn && hand.CommunityCards.Count < 3)
        {
            return PhaseTransitionValidation.Invalid(
                $"Expected at least 3 community cards for flop, but have {hand.CommunityCards.Count}");
        }

        return PhaseTransitionValidation.Valid();
    }
}
