using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine.Handlers;

/// <summary>
/// Handles the Preflop phase - deal hole cards, collect blinds, first betting round.
/// </summary>
public sealed class PreflopPhaseHandler : BasePhaseHandler
{
    public PreflopPhaseHandler(ILogger<PreflopPhaseHandler> logger) : base(logger)
    {
    }

    public override HandPhase Phase => HandPhase.Preflop;

    public override Task OnEnterAsync(Hand hand, PhaseTransitionContext context)
    {
        Logger.LogInformation(
            "Hand {HandId} starting preflop. Button: seat {Button}, Blinds: {SB}/{BB}",
            hand.Id, hand.ButtonPosition, hand.SmallBlindAmount, hand.BigBlindAmount);

        // Set initial betting state
        hand.CurrentBet = hand.BigBlindAmount;
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

        // Additional validation could go here
        // e.g., ensure all players have acted, betting is complete, etc.

        return PhaseTransitionValidation.Valid();
    }
}
