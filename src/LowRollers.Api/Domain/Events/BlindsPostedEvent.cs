namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Event raised when blinds are posted at the start of a hand.
/// </summary>
public sealed record BlindsPostedEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required int SequenceNumber { get; init; }
    public string EventType => nameof(BlindsPostedEvent);

    /// <summary>
    /// Player ID of the small blind.
    /// </summary>
    public required Guid SmallBlindPlayerId { get; init; }

    /// <summary>
    /// Amount posted as small blind.
    /// </summary>
    public required decimal SmallBlindAmount { get; init; }

    /// <summary>
    /// Player ID of the big blind.
    /// </summary>
    public required Guid BigBlindPlayerId { get; init; }

    /// <summary>
    /// Amount posted as big blind.
    /// </summary>
    public required decimal BigBlindAmount { get; init; }

    /// <summary>
    /// Total pot after blinds are posted.
    /// </summary>
    public required decimal PotTotal { get; init; }
}
