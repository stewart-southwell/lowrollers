using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Domain.StateMachine.Handlers;

/// <summary>
/// Handles the Waiting phase - validating that enough players are ready to start.
/// </summary>
public sealed class WaitingPhaseHandler : BasePhaseHandler
{
    private const int MinPlayersToStart = 2;

    public WaitingPhaseHandler(ILogger<WaitingPhaseHandler> logger) : base(logger)
    {
    }

    public override HandPhase Phase => HandPhase.Waiting;

    public override PhaseTransitionValidation ValidateTransition(Hand hand, HandPhase targetPhase)
    {
        var baseValidation = base.ValidateTransition(hand, targetPhase);
        if (!baseValidation.IsValid)
        {
            return baseValidation;
        }

        // Must have minimum players to start
        if (targetPhase == HandPhase.Preflop && hand.PlayerIds.Count < MinPlayersToStart)
        {
            return PhaseTransitionValidation.Invalid(
                $"Need at least {MinPlayersToStart} players to start a hand");
        }

        return PhaseTransitionValidation.Valid();
    }
}
