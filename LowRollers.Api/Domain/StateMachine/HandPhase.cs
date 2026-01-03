namespace LowRollers.Api.Domain.StateMachine;

/// <summary>
/// Represents the phases of a poker hand in Texas Hold'em.
/// </summary>
public enum HandPhase
{
    /// <summary>
    /// Waiting for enough players to start a hand.
    /// </summary>
    Waiting = 0,

    /// <summary>
    /// Blinds have been posted and hole cards dealt; first betting round.
    /// </summary>
    Preflop = 1,

    /// <summary>
    /// First three community cards dealt; second betting round.
    /// </summary>
    Flop = 2,

    /// <summary>
    /// Fourth community card dealt; third betting round.
    /// </summary>
    Turn = 3,

    /// <summary>
    /// Fifth community card dealt; final betting round.
    /// </summary>
    River = 4,

    /// <summary>
    /// Players reveal their hands to determine winner(s).
    /// </summary>
    Showdown = 5,

    /// <summary>
    /// Hand is complete; pot has been awarded.
    /// </summary>
    Complete = 6
}
