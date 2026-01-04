using LowRollers.Api.Domain.Betting;
using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.StateMachine;

namespace LowRollers.Api.Features.GameEngine.Broadcasting;

/// <summary>
/// Represents the complete game state for a table, sanitized per-player.
/// This is the primary DTO sent to clients after each action.
/// </summary>
public sealed record TableGameState
{
    /// <summary>
    /// Table identifier.
    /// </summary>
    public required Guid TableId { get; init; }

    /// <summary>
    /// Table display name.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Current table status.
    /// </summary>
    public required TableStatus Status { get; init; }

    /// <summary>
    /// All players at the table with their visible state.
    /// </summary>
    public required IReadOnlyList<PlayerState> Players { get; init; }

    /// <summary>
    /// Current hand state (null if between hands).
    /// </summary>
    public HandState? CurrentHand { get; init; }

    /// <summary>
    /// Current dealer button position (1-10).
    /// </summary>
    public required int ButtonPosition { get; init; }

    /// <summary>
    /// Small blind amount.
    /// </summary>
    public required decimal SmallBlind { get; init; }

    /// <summary>
    /// Big blind amount.
    /// </summary>
    public required decimal BigBlind { get; init; }

    /// <summary>
    /// Number of hands played this session.
    /// </summary>
    public required int HandCount { get; init; }

    /// <summary>
    /// Action timer setting in seconds (0 = unlimited).
    /// </summary>
    public required int ActionTimerSeconds { get; init; }

    /// <summary>
    /// Whether time bank is enabled.
    /// </summary>
    public required bool TimeBankEnabled { get; init; }

    /// <summary>
    /// Server timestamp when this state was generated.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Represents a player's visible state at the table.
/// Hole cards are sanitized based on the viewing player.
/// </summary>
public sealed record PlayerState
{
    /// <summary>
    /// Player's unique identifier.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// Player's display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Seat position (1-10).
    /// </summary>
    public required int SeatPosition { get; init; }

    /// <summary>
    /// Current chip stack.
    /// </summary>
    public required decimal ChipStack { get; init; }

    /// <summary>
    /// Player status (Active, Folded, AllIn, Away, Waiting).
    /// </summary>
    public required PlayerStatus Status { get; init; }

    /// <summary>
    /// Amount bet in the current betting round.
    /// </summary>
    public required decimal CurrentBet { get; init; }

    /// <summary>
    /// Total amount invested in current hand.
    /// </summary>
    public required decimal TotalBetThisHand { get; init; }

    /// <summary>
    /// Visible hole cards. Null if cards should be hidden.
    /// Only populated for:
    /// - The viewing player's own cards
    /// - All players at showdown (if shown, not mucked)
    /// </summary>
    public CardDto[]? HoleCards { get; init; }

    /// <summary>
    /// Whether hole cards exist but are hidden (shows card backs in UI).
    /// </summary>
    public required bool HasHiddenCards { get; init; }

    /// <summary>
    /// Whether this player is the current host.
    /// </summary>
    public required bool IsHost { get; init; }

    /// <summary>
    /// Remaining time bank in seconds.
    /// </summary>
    public required int TimeBankSeconds { get; init; }

    /// <summary>
    /// Whether this is the viewing player.
    /// </summary>
    public required bool IsViewer { get; init; }
}

/// <summary>
/// Represents the current hand state.
/// </summary>
public sealed record HandState
{
    /// <summary>
    /// Hand identifier.
    /// </summary>
    public required Guid HandId { get; init; }

    /// <summary>
    /// Sequential hand number for this session.
    /// </summary>
    public required int HandNumber { get; init; }

    /// <summary>
    /// Current phase of the hand.
    /// </summary>
    public required HandPhase Phase { get; init; }

    /// <summary>
    /// Community cards on the board.
    /// </summary>
    public required IReadOnlyList<CardDto> CommunityCards { get; init; }

    /// <summary>
    /// Second board for double-board bomb pots (null for normal hands).
    /// </summary>
    public IReadOnlyList<CardDto>? SecondBoard { get; init; }

    /// <summary>
    /// All pots (main pot first, then side pots).
    /// </summary>
    public required IReadOnlyList<PotState> Pots { get; init; }

    /// <summary>
    /// Total pot amount across all pots.
    /// </summary>
    public required decimal TotalPot { get; init; }

    /// <summary>
    /// Current bet to call.
    /// </summary>
    public required decimal CurrentBet { get; init; }

    /// <summary>
    /// Minimum raise amount.
    /// </summary>
    public required decimal MinRaise { get; init; }

    /// <summary>
    /// ID of the player whose turn it is to act (null if not in betting).
    /// </summary>
    public Guid? CurrentPlayerId { get; init; }

    /// <summary>
    /// Small blind seat position.
    /// </summary>
    public required int SmallBlindPosition { get; init; }

    /// <summary>
    /// Big blind seat position.
    /// </summary>
    public required int BigBlindPosition { get; init; }

    /// <summary>
    /// Whether this is a bomb pot.
    /// </summary>
    public required bool IsBombPot { get; init; }

    /// <summary>
    /// Whether this is a double-board bomb pot.
    /// </summary>
    public required bool IsDoubleBoard { get; init; }

    /// <summary>
    /// When the hand started.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }
}

/// <summary>
/// Represents a pot (main or side).
/// </summary>
public sealed record PotState
{
    /// <summary>
    /// Amount in this pot.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Type of pot (Main or Side).
    /// </summary>
    public required PotType Type { get; init; }

    /// <summary>
    /// Player IDs eligible to win this pot.
    /// </summary>
    public required IReadOnlyList<Guid> EligiblePlayerIds { get; init; }

    /// <summary>
    /// Display name for side pots (e.g., "Side Pot 1").
    /// </summary>
    public string? Name { get; init; }
}

/// <summary>
/// Represents a card for transmission to clients.
/// </summary>
public sealed record CardDto
{
    /// <summary>
    /// Card suit.
    /// </summary>
    public required Suit Suit { get; init; }

    /// <summary>
    /// Card rank.
    /// </summary>
    public required Rank Rank { get; init; }

    /// <summary>
    /// Short display string (e.g., "As" for Ace of Spades).
    /// </summary>
    public required string Display { get; init; }

    /// <summary>
    /// Creates a CardDto from a domain Card.
    /// </summary>
    public static CardDto FromCard(Card card) => new()
    {
        Suit = card.Suit,
        Rank = card.Rank,
        Display = card.ToString()
    };
}
