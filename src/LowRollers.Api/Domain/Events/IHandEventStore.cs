namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Summary projection of a completed hand for quick lookups.
/// </summary>
public sealed record HandSummary
{
    public required Guid HandId { get; init; }
    public required Guid TableId { get; init; }
    public required int HandNumber { get; init; }
    public required IReadOnlyList<Guid> WinnerIds { get; init; }
    public required decimal TotalPot { get; init; }
    public required long DurationMs { get; init; }
    public required int PlayerCount { get; init; }
    public required bool WentToShowdown { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}

/// <summary>
/// Interface for storing and retrieving hand events.
/// Implementations may use in-memory storage (testing), PostgreSQL (production),
/// or other backing stores.
/// </summary>
public interface IHandEventStore
{
    /// <summary>
    /// Appends a single event to the store.
    /// </summary>
    /// <param name="event">The event to append.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the sequence number already exists for this hand.
    /// </exception>
    Task AppendAsync(IHandEvent @event, CancellationToken ct = default);

    /// <summary>
    /// Appends multiple events to the store atomically.
    /// </summary>
    /// <param name="events">The events to append.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AppendRangeAsync(IEnumerable<IHandEvent> events, CancellationToken ct = default);

    /// <summary>
    /// Gets all events for a specific hand in sequence order.
    /// </summary>
    /// <param name="handId">The hand ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Events ordered by sequence number.</returns>
    IAsyncEnumerable<IHandEvent> GetEventsAsync(Guid handId, CancellationToken ct = default);

    /// <summary>
    /// Gets events for a specific hand starting from a sequence number.
    /// Useful for partial replays.
    /// </summary>
    /// <param name="handId">The hand ID.</param>
    /// <param name="fromSequence">Starting sequence number (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    IAsyncEnumerable<IHandEvent> GetEventsFromAsync(Guid handId, int fromSequence, CancellationToken ct = default);

    /// <summary>
    /// Gets recent hands for a table.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <param name="limit">Maximum number of hands to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Hand summaries ordered by most recent first.</returns>
    IAsyncEnumerable<HandSummary> GetTableHistoryAsync(Guid tableId, int limit = 100, CancellationToken ct = default);

    /// <summary>
    /// Gets a summary of a specific hand.
    /// </summary>
    /// <param name="handId">The hand ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The hand summary, or null if the hand doesn't exist or isn't complete.</returns>
    Task<HandSummary?> GetHandSummaryAsync(Guid handId, CancellationToken ct = default);

    /// <summary>
    /// Gets the last sequence number for a hand.
    /// Returns 0 if no events exist for the hand.
    /// </summary>
    /// <param name="handId">The hand ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<int> GetLastSequenceNumberAsync(Guid handId, CancellationToken ct = default);
}
