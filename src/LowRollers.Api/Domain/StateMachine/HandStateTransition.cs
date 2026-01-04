namespace LowRollers.Api.Domain.StateMachine;

/// <summary>
/// Represents the reason/trigger for a hand state transition.
/// </summary>
public enum TransitionTrigger
{
    /// <summary>Minimum players reached, starting hand.</summary>
    StartHand,

    /// <summary>Betting round complete, advancing to next phase.</summary>
    BettingComplete,

    /// <summary>All but one player folded, awarding pot.</summary>
    AllFolded,

    /// <summary>Showdown complete, awarding pots.</summary>
    ShowdownComplete,

    /// <summary>Hand forcibly ended (timeout, disconnect, etc.).</summary>
    ForceEnd
}

/// <summary>
/// Represents a state transition in the hand state machine.
/// </summary>
/// <param name="FromPhase">The phase before the transition.</param>
/// <param name="ToPhase">The phase after the transition.</param>
/// <param name="Trigger">What caused the transition.</param>
/// <param name="Timestamp">When the transition occurred.</param>
public readonly record struct HandStateTransition(
    HandPhase FromPhase,
    HandPhase ToPhase,
    TransitionTrigger Trigger,
    DateTimeOffset Timestamp)
{
    /// <summary>
    /// Creates a new transition with the current timestamp.
    /// </summary>
    public static HandStateTransition Create(HandPhase from, HandPhase to, TransitionTrigger trigger)
        => new(from, to, trigger, DateTimeOffset.UtcNow);
}
