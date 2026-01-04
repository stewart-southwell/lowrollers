namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Event raised when a player mucks their cards at showdown.
/// </summary>
public sealed record PlayerMuckedCardsEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required int SequenceNumber { get; init; }
    public string EventType => nameof(PlayerMuckedCardsEvent);

    /// <summary>
    /// The player who mucked their cards.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// Whether the muck was automatic (inferior hand) or player's choice.
    /// </summary>
    public bool IsAutoMuck { get; init; }

    /// <summary>
    /// Order in the showdown sequence.
    /// </summary>
    public required int ShowdownOrder { get; init; }
}
