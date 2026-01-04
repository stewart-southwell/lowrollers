using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.StateMachine;

namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Event raised when community cards are dealt (flop, turn, or river).
/// </summary>
public sealed record CommunityCardsDealtEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required int SequenceNumber { get; init; }
    public string EventType => nameof(CommunityCardsDealtEvent);

    /// <summary>
    /// The phase these cards are being dealt for.
    /// Flop = 3 cards, Turn = 1 card, River = 1 card.
    /// </summary>
    public required HandPhase Phase { get; init; }

    /// <summary>
    /// The cards dealt in this phase.
    /// Flop: 3 cards, Turn: 1 card, River: 1 card.
    /// </summary>
    public required IReadOnlyList<Card> Cards { get; init; }

    /// <summary>
    /// All community cards on the board after this deal.
    /// </summary>
    public required IReadOnlyList<Card> BoardState { get; init; }

    /// <summary>
    /// Board index for double-board bomb pots (0 = first board, 1 = second board).
    /// Always 0 for regular hands.
    /// </summary>
    public int BoardIndex { get; init; }
}
