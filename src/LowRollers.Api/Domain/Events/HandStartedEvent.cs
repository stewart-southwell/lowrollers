namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Event raised when a new hand begins.
/// Contains all initial configuration for the hand.
/// </summary>
public sealed record HandStartedEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public int SequenceNumber { get; init; } = 1;
    public string EventType => nameof(HandStartedEvent);

    /// <summary>
    /// The table where this hand is being played.
    /// </summary>
    public required Guid TableId { get; init; }

    /// <summary>
    /// Sequential hand number for this table session.
    /// </summary>
    public required int HandNumber { get; init; }

    /// <summary>
    /// Seat position of the dealer button (1-10).
    /// </summary>
    public required int ButtonPosition { get; init; }

    /// <summary>
    /// Seat position of the small blind.
    /// </summary>
    public required int SmallBlindPosition { get; init; }

    /// <summary>
    /// Seat position of the big blind.
    /// </summary>
    public required int BigBlindPosition { get; init; }

    /// <summary>
    /// Small blind amount.
    /// </summary>
    public required decimal SmallBlindAmount { get; init; }

    /// <summary>
    /// Big blind amount.
    /// </summary>
    public required decimal BigBlindAmount { get; init; }

    /// <summary>
    /// Player IDs participating in this hand, in seat order.
    /// </summary>
    public required IReadOnlyList<Guid> PlayerIds { get; init; }

    /// <summary>
    /// Whether this is a bomb pot (all players post ante, no preflop betting).
    /// </summary>
    public bool IsBombPot { get; init; }

    /// <summary>
    /// Whether this is a double-board bomb pot.
    /// </summary>
    public bool IsDoubleBoard { get; init; }

    /// <summary>
    /// Ante amount for bomb pots (zero for regular hands).
    /// </summary>
    public decimal AnteAmount { get; init; }
}
