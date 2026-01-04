namespace LowRollers.Api.Domain.Models;

/// <summary>
/// Represents a player's current status in a hand.
/// </summary>
public enum PlayerStatus
{
    /// <summary>Player is waiting for the next hand.</summary>
    Waiting = 0,

    /// <summary>Player is active in the current hand.</summary>
    Active = 1,

    /// <summary>Player has folded this hand.</summary>
    Folded = 2,

    /// <summary>Player is all-in.</summary>
    AllIn = 3,

    /// <summary>Player is away/sitting out.</summary>
    Away = 4
}

/// <summary>
/// Represents a player at the poker table.
/// </summary>
public sealed class Player
{
    /// <summary>
    /// Unique identifier for the player's session.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Display name shown at the table (2-20 characters).
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Current chip stack amount.
    /// </summary>
    public decimal ChipStack { get; set; }

    /// <summary>
    /// Seat position at the table (1-10).
    /// </summary>
    public int SeatPosition { get; set; }

    /// <summary>
    /// Current status in the hand.
    /// </summary>
    public PlayerStatus Status { get; set; } = PlayerStatus.Waiting;

    /// <summary>
    /// Player's hole cards for the current hand.
    /// Null when not in a hand or cards not yet dealt.
    /// </summary>
    public Card[]? HoleCards { get; set; }

    /// <summary>
    /// Amount bet in the current betting round.
    /// </summary>
    public decimal CurrentBet { get; set; }

    /// <summary>
    /// Total amount invested in the current hand (all betting rounds).
    /// </summary>
    public decimal TotalBetThisHand { get; set; }

    /// <summary>
    /// Whether this player is the current host of the table.
    /// </summary>
    public bool IsHost { get; set; }

    /// <summary>
    /// Time bank remaining in seconds.
    /// </summary>
    public int TimeBankSeconds { get; set; }

    /// <summary>
    /// Timestamp when player joined the table.
    /// </summary>
    public DateTimeOffset JoinedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp of last activity (action, chat, etc.).
    /// </summary>
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Number of hands sat out (for missed blind tracking).
    /// </summary>
    public int HandsSatOut { get; set; }

    /// <summary>
    /// Whether the player owes missed blinds.
    /// </summary>
    public bool OwesMissedBlinds { get; set; }

    /// <summary>
    /// Creates a new player with the specified details.
    /// </summary>
    public static Player Create(Guid id, string displayName, int seatPosition, decimal buyInAmount, bool isHost = false)
    {
        if (displayName.Length < 2 || displayName.Length > 20)
        {
            throw new ArgumentException("Display name must be 2-20 characters.", nameof(displayName));
        }

        return new Player
        {
            Id = id,
            DisplayName = displayName,
            SeatPosition = seatPosition,
            ChipStack = buyInAmount,
            IsHost = isHost,
            Status = PlayerStatus.Waiting
        };
    }

    /// <summary>
    /// Resets the player's state for a new hand.
    /// </summary>
    public void ResetForNewHand()
    {
        HoleCards = null;
        CurrentBet = 0;
        TotalBetThisHand = 0;

        if (Status != PlayerStatus.Away)
        {
            Status = PlayerStatus.Waiting;
        }
    }

    /// <summary>
    /// Determines if the player can act in the current hand.
    /// </summary>
    public bool CanAct => Status == PlayerStatus.Active;

    /// <summary>
    /// Determines if the player is still in the hand (not folded).
    /// </summary>
    public bool IsInHand => Status is PlayerStatus.Active or PlayerStatus.AllIn;

    /// <summary>
    /// Consumes time from the player's time bank.
    /// </summary>
    /// <param name="seconds">Seconds to consume from the time bank.</param>
    public void ConsumeTimeBank(int seconds)
    {
        TimeBankSeconds = Math.Max(0, TimeBankSeconds - seconds);
    }
}
