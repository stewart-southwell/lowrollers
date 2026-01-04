using LowRollers.Api.Domain.Betting;
using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Features.GameEngine;

/// <summary>
/// Result of a game orchestration operation.
/// </summary>
public readonly record struct GameResult
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// The updated hand state.
    /// </summary>
    public Hand? Hand { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GameResult Success(Hand hand) => new() { IsSuccess = true, Hand = hand };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static GameResult Failure(string error) => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Result of starting a new hand.
/// </summary>
public sealed record HandStartResult
{
    /// <summary>
    /// Whether the hand was started successfully.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if the hand could not be started.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// The new hand if successful.
    /// </summary>
    public Hand? Hand { get; init; }

    /// <summary>
    /// Hole cards dealt to each player (player ID -> cards).
    /// Only populated on success.
    /// </summary>
    public IReadOnlyDictionary<Guid, Card[]>? HoleCards { get; init; }

    public static HandStartResult Success(Hand hand, Dictionary<Guid, Card[]> holeCards)
        => new() { IsSuccess = true, Hand = hand, HoleCards = holeCards };

    public static HandStartResult Failure(string error)
        => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Result of a player action.
/// </summary>
public sealed record ActionResult
{
    /// <summary>
    /// Whether the action was executed successfully.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if the action failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// The updated hand state.
    /// </summary>
    public Hand? Hand { get; init; }

    /// <summary>
    /// Whether the betting round is complete after this action.
    /// </summary>
    public bool BettingRoundComplete { get; init; }

    /// <summary>
    /// Whether the hand is complete (all folded or showdown finished).
    /// </summary>
    public bool HandComplete { get; init; }

    /// <summary>
    /// Community cards dealt after this action (if phase advanced).
    /// </summary>
    public Card[]? NewCommunityCards { get; init; }

    /// <summary>
    /// Pot winnings if hand completed (player ID -> amount won).
    /// </summary>
    public IReadOnlyDictionary<Guid, decimal>? Winnings { get; init; }

    /// <summary>
    /// The next player to act (null if hand complete or showdown).
    /// </summary>
    public Guid? NextPlayerId { get; init; }

    public static ActionResult Success(Hand hand, Guid? nextPlayerId)
        => new() { IsSuccess = true, Hand = hand, NextPlayerId = nextPlayerId };

    public static ActionResult Failure(string error)
        => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Orchestrates the entire flow of a poker hand from start to finish.
/// Coordinates the state machine, betting, dealing, pot management, and events.
/// </summary>
public interface IGameOrchestrator
{
    /// <summary>
    /// Starts a new hand at the given table.
    /// Rotates button, posts blinds, shuffles, and deals hole cards.
    /// </summary>
    /// <param name="table">The table to start the hand on.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the new hand and dealt cards.</returns>
    Task<HandStartResult> StartNewHandAsync(Table table, CancellationToken ct = default);

    /// <summary>
    /// Starts a bomb pot hand where all players post an ante and skip to flop.
    /// </summary>
    /// <param name="table">The table to start the hand on.</param>
    /// <param name="anteAmount">The ante amount each player posts.</param>
    /// <param name="isDoubleBoard">Whether to deal two boards.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the new hand, dealt cards, and flop.</returns>
    Task<HandStartResult> StartBombPotAsync(
        Table table,
        decimal anteAmount,
        bool isDoubleBoard = false,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a player action (fold, check, call, raise, all-in).
    /// Validates the action, updates state, and advances the hand if needed.
    /// </summary>
    /// <param name="table">The table where the action is taken.</param>
    /// <param name="playerId">The player taking the action.</param>
    /// <param name="actionType">The type of action.</param>
    /// <param name="amount">The amount for raise actions (ignored for fold/check).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the updated state and any new cards dealt.</returns>
    Task<ActionResult> ExecutePlayerActionAsync(
        Table table,
        Guid playerId,
        PlayerActionType actionType,
        decimal amount = 0,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the available actions for the current player.
    /// </summary>
    /// <param name="table">The table.</param>
    /// <returns>Available actions or null if no player to act.</returns>
    AvailableActions? GetAvailableActions(Table table);

    /// <summary>
    /// Forces the current player to fold (used by timer expiry).
    /// </summary>
    /// <param name="table">The table.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the forced fold action.</returns>
    Task<ActionResult> ForceTimeoutFoldAsync(Table table, CancellationToken ct = default);

    /// <summary>
    /// Gets the current betting round state for a hand.
    /// </summary>
    /// <param name="handId">The hand ID.</param>
    /// <returns>The betting round or null if not found.</returns>
    BettingRound? GetBettingRound(Guid handId);
}
