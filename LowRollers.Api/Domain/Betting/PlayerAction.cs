namespace LowRollers.Api.Domain.Betting;

/// <summary>
/// Represents the types of actions a player can take during a betting round.
/// </summary>
public enum PlayerActionType
{
    /// <summary>
    /// Surrender the hand and forfeit any chips already bet.
    /// Always valid when it's the player's turn.
    /// </summary>
    Fold = 0,

    /// <summary>
    /// Pass the action without betting when no bet is facing.
    /// Only valid when CurrentBet == 0 or player has already matched the current bet.
    /// </summary>
    Check = 1,

    /// <summary>
    /// Match the current bet to stay in the hand.
    /// Only valid when there is a bet to call (CurrentBet > player's current bet).
    /// </summary>
    Call = 2,

    /// <summary>
    /// Increase the current bet.
    /// Amount must be >= CurrentBet + LastRaise (minimum raise rule).
    /// </summary>
    Raise = 3,

    /// <summary>
    /// Bet all remaining chips.
    /// Always valid when it's the player's turn (may be less than a full raise).
    /// </summary>
    AllIn = 4
}

/// <summary>
/// Represents a player's action with its associated amount.
/// </summary>
public sealed record PlayerAction
{
    /// <summary>
    /// The type of action taken.
    /// </summary>
    public required PlayerActionType Type { get; init; }

    /// <summary>
    /// The amount associated with the action (for Call, Raise, AllIn).
    /// Zero for Fold and Check.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// The player who took the action.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// Timestamp when the action was taken.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a fold action.
    /// </summary>
    public static PlayerAction Fold(Guid playerId) => new()
    {
        Type = PlayerActionType.Fold,
        PlayerId = playerId,
        Amount = 0
    };

    /// <summary>
    /// Creates a check action.
    /// </summary>
    public static PlayerAction Check(Guid playerId) => new()
    {
        Type = PlayerActionType.Check,
        PlayerId = playerId,
        Amount = 0
    };

    /// <summary>
    /// Creates a call action.
    /// </summary>
    public static PlayerAction Call(Guid playerId, decimal amount) => new()
    {
        Type = PlayerActionType.Call,
        PlayerId = playerId,
        Amount = amount
    };

    /// <summary>
    /// Creates a raise action.
    /// </summary>
    public static PlayerAction Raise(Guid playerId, decimal totalAmount) => new()
    {
        Type = PlayerActionType.Raise,
        PlayerId = playerId,
        Amount = totalAmount
    };

    /// <summary>
    /// Creates an all-in action.
    /// </summary>
    public static PlayerAction AllIn(Guid playerId, decimal amount) => new()
    {
        Type = PlayerActionType.AllIn,
        PlayerId = playerId,
        Amount = amount
    };
}
