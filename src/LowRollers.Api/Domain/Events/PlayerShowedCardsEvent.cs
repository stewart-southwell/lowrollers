using LowRollers.Api.Domain.Evaluation;
using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Event raised when a player shows their cards at showdown.
/// </summary>
public sealed record PlayerShowedCardsEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required int SequenceNumber { get; init; }
    public string EventType => nameof(PlayerShowedCardsEvent);

    /// <summary>
    /// The player who showed their cards.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// The player's hole cards.
    /// </summary>
    public required Card[] HoleCards { get; init; }

    /// <summary>
    /// The hand category (e.g., Flush, Pair, etc.).
    /// </summary>
    public required HandCategory HandCategory { get; init; }

    /// <summary>
    /// Human-readable description of the hand (e.g., "Pair of Kings").
    /// </summary>
    public required string HandDescription { get; init; }

    /// <summary>
    /// Integer ranking for comparison (lower = better).
    /// </summary>
    public required int HandRanking { get; init; }

    /// <summary>
    /// The five cards that make up the best hand.
    /// </summary>
    public required IReadOnlyList<Card> BestFiveCards { get; init; }

    /// <summary>
    /// Order in which this player showed (1 = first).
    /// </summary>
    public required int ShowOrder { get; init; }
}
