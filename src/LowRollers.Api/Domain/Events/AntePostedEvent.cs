namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Event raised when a player posts an ante (typically for bomb pots).
/// One event per player who posts an ante.
/// </summary>
public sealed record AntePostedEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required int SequenceNumber { get; init; }
    public string EventType => nameof(AntePostedEvent);

    /// <summary>
    /// Player who posted the ante.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// Amount posted as ante.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Player's remaining chip stack after posting.
    /// </summary>
    public required decimal RemainingStack { get; init; }

    /// <summary>
    /// Total pot after this ante is posted.
    /// </summary>
    public required decimal PotTotal { get; init; }
}
