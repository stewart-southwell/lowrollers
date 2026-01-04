using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Event raised when hole cards are dealt to players.
/// For security, cards can be stored as hashes until revealed at showdown.
/// </summary>
public sealed record HoleCardsDealtEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required int SequenceNumber { get; init; }
    public string EventType => nameof(HoleCardsDealtEvent);

    /// <summary>
    /// Map of PlayerId to their hole cards.
    /// Cards are stored directly since hand history is only visible after completion.
    /// </summary>
    public required IReadOnlyDictionary<Guid, Card[]> PlayerCards { get; init; }
}

/// <summary>
/// Represents a player's hole cards in a serializable format.
/// </summary>
public sealed record PlayerHoleCards
{
    /// <summary>
    /// The player's ID.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// The two hole cards dealt to this player.
    /// </summary>
    public required Card[] Cards { get; init; }

    /// <summary>
    /// SHA256 hash of the cards for verification.
    /// Format: Hash of sorted card codes (e.g., "AhKs" -> hash)
    /// </summary>
    public string? CardHash { get; init; }
}
