using LowRollers.Api.Domain.StateMachine;

namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Event raised when a hand is completed.
/// Contains summary information for quick lookups.
/// </summary>
public sealed record HandCompletedEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required int SequenceNumber { get; init; }
    public string EventType => nameof(HandCompletedEvent);

    /// <summary>
    /// Total pot amount awarded.
    /// </summary>
    public required decimal TotalPotAmount { get; init; }

    /// <summary>
    /// Duration of the hand in milliseconds.
    /// </summary>
    public required long DurationMs { get; init; }

    /// <summary>
    /// Number of players who participated in the hand.
    /// </summary>
    public required int PlayerCount { get; init; }

    /// <summary>
    /// Whether the hand reached showdown.
    /// </summary>
    public required bool WentToShowdown { get; init; }

    /// <summary>
    /// The final phase reached before completion.
    /// </summary>
    public required HandPhase FinalPhase { get; init; }

    /// <summary>
    /// Summary of winnings per player.
    /// Negative amounts indicate net loss, positive indicate net gain.
    /// </summary>
    public required IReadOnlyDictionary<Guid, decimal> PlayerResults { get; init; }

    /// <summary>
    /// IDs of all players who won any pot.
    /// </summary>
    public required IReadOnlyList<Guid> WinnerIds { get; init; }
}
