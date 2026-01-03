namespace LowRollers.Api.Domain.Models;

/// <summary>
/// Represents the current phase of a poker hand.
/// </summary>
public enum HandPhase
{
    /// <summary>Waiting to start a new hand.</summary>
    Waiting = 0,

    /// <summary>Preflop betting round (before community cards).</summary>
    Preflop = 1,

    /// <summary>Flop betting round (3 community cards dealt).</summary>
    Flop = 2,

    /// <summary>Turn betting round (4th community card dealt).</summary>
    Turn = 3,

    /// <summary>River betting round (5th community card dealt).</summary>
    River = 4,

    /// <summary>Showdown - determining winner(s).</summary>
    Showdown = 5,

    /// <summary>Hand is complete.</summary>
    Complete = 6
}

/// <summary>
/// Represents a single poker hand from deal to showdown.
/// </summary>
public sealed class Hand
{
    /// <summary>
    /// Unique identifier for this hand.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The table this hand is being played on.
    /// </summary>
    public Guid TableId { get; init; }

    /// <summary>
    /// Sequential hand number for this table session.
    /// </summary>
    public int HandNumber { get; init; }

    /// <summary>
    /// Current phase of the hand.
    /// </summary>
    public HandPhase Phase { get; set; } = HandPhase.Waiting;

    /// <summary>
    /// Seat position of the dealer button (1-10).
    /// </summary>
    public int ButtonPosition { get; init; }

    /// <summary>
    /// Seat position of the small blind.
    /// </summary>
    public int SmallBlindPosition { get; init; }

    /// <summary>
    /// Seat position of the big blind.
    /// </summary>
    public int BigBlindPosition { get; init; }

    /// <summary>
    /// Small blind amount for this hand.
    /// </summary>
    public decimal SmallBlindAmount { get; init; }

    /// <summary>
    /// Big blind amount for this hand.
    /// </summary>
    public decimal BigBlindAmount { get; init; }

    /// <summary>
    /// The main pot and any side pots.
    /// </summary>
    public List<Pot> Pots { get; init; } = [Pot.CreateMainPot()];

    /// <summary>
    /// Community cards on the board (0-5 cards).
    /// </summary>
    public List<Card> CommunityCards { get; init; } = [];

    /// <summary>
    /// Second board for double-board bomb pots (null for normal hands).
    /// </summary>
    public List<Card>? SecondBoard { get; init; }

    /// <summary>
    /// Player IDs in this hand, in seat order.
    /// </summary>
    public List<Guid> PlayerIds { get; init; } = [];

    /// <summary>
    /// ID of the player whose turn it is to act.
    /// </summary>
    public Guid? CurrentPlayerId { get; set; }

    /// <summary>
    /// Current bet amount that players must match to stay in the hand.
    /// </summary>
    public decimal CurrentBet { get; set; }

    /// <summary>
    /// Minimum raise amount (equal to the last raise).
    /// </summary>
    public decimal MinRaise { get; set; }

    /// <summary>
    /// Number of raises in the current betting round.
    /// </summary>
    public int RaisesThisRound { get; set; }

    /// <summary>
    /// Whether this is a bomb pot (no preflop betting).
    /// </summary>
    public bool IsBombPot { get; set; }

    /// <summary>
    /// Whether this is a double-board bomb pot.
    /// </summary>
    public bool IsDoubleBoard { get; set; }

    /// <summary>
    /// Timestamp when the hand started.
    /// </summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when the hand completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// ID of the player who was last aggressor (for showdown order).
    /// </summary>
    public Guid? LastAggressorId { get; set; }

    /// <summary>
    /// Gets the main pot.
    /// </summary>
    public Pot MainPot => Pots.First(p => p.Type == PotType.Main);

    /// <summary>
    /// Gets all side pots in creation order.
    /// </summary>
    public IEnumerable<Pot> SidePots => Pots
        .Where(p => p.Type == PotType.Side)
        .OrderBy(p => p.CreationOrder);

    /// <summary>
    /// Gets the total amount across all pots.
    /// </summary>
    public decimal TotalPot => Pots.Sum(p => p.Amount);

    /// <summary>
    /// Creates a new hand for the given table.
    /// </summary>
    public static Hand Create(
        Guid tableId,
        int handNumber,
        int buttonPosition,
        int smallBlindPosition,
        int bigBlindPosition,
        decimal smallBlindAmount,
        decimal bigBlindAmount,
        IEnumerable<Guid> playerIds)
    {
        return new Hand
        {
            TableId = tableId,
            HandNumber = handNumber,
            ButtonPosition = buttonPosition,
            SmallBlindPosition = smallBlindPosition,
            BigBlindPosition = bigBlindPosition,
            SmallBlindAmount = smallBlindAmount,
            BigBlindAmount = bigBlindAmount,
            MinRaise = bigBlindAmount,
            PlayerIds = playerIds.ToList(),
            Phase = HandPhase.Waiting
        };
    }

    /// <summary>
    /// Creates a bomb pot hand.
    /// </summary>
    public static Hand CreateBombPot(
        Guid tableId,
        int handNumber,
        int buttonPosition,
        int smallBlindPosition,
        int bigBlindPosition,
        decimal smallBlindAmount,
        decimal bigBlindAmount,
        decimal anteAmount,
        IEnumerable<Guid> playerIds,
        bool isDoubleBoard = false)
    {
        var hand = Create(
            tableId, handNumber, buttonPosition, smallBlindPosition,
            bigBlindPosition, smallBlindAmount, bigBlindAmount, playerIds);

        hand.IsBombPot = true;
        hand.IsDoubleBoard = isDoubleBoard;

        if (isDoubleBoard)
        {
            // Initialize second board for double-board bomb pot
            // Note: We need to use a new instance, can't modify init-only property
        }

        return hand;
    }

    /// <summary>
    /// Advances to the next phase of the hand.
    /// </summary>
    public void AdvancePhase()
    {
        Phase = Phase switch
        {
            HandPhase.Waiting => HandPhase.Preflop,
            HandPhase.Preflop => HandPhase.Flop,
            HandPhase.Flop => HandPhase.Turn,
            HandPhase.Turn => HandPhase.River,
            HandPhase.River => HandPhase.Showdown,
            HandPhase.Showdown => HandPhase.Complete,
            _ => throw new InvalidOperationException($"Cannot advance from phase {Phase}")
        };

        // Reset betting round state
        CurrentBet = 0;
        RaisesThisRound = 0;
    }

    /// <summary>
    /// Marks the hand as complete.
    /// </summary>
    public void Complete()
    {
        Phase = HandPhase.Complete;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
