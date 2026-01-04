using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Features.GameEngine.Broadcasting;

/// <summary>
/// Broadcasts game state updates to connected clients.
/// Handles per-viewer state sanitization:
/// - Players see only their own hole cards (until showdown)
/// - Spectators see no hole cards (only community cards and bets)
/// - At showdown, shown cards are visible to all
/// </summary>
public interface IGameStateBroadcaster
{
    /// <summary>
    /// Broadcasts the current game state to all connected clients at a table.
    /// Each viewer receives a sanitized view based on their role:
    /// - Players see their own hole cards
    /// - Spectators see no hole cards
    /// - Everyone sees community cards, pots, and bet amounts
    /// - At showdown, shown cards are visible to all
    /// </summary>
    /// <param name="table">The table to broadcast state for.</param>
    /// <param name="shownCards">
    /// Optional dictionary of player IDs to their shown cards at showdown.
    /// Cards in this dictionary are visible to all viewers.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the broadcast operation.</returns>
    Task BroadcastGameStateAsync(
        Table table,
        IReadOnlyDictionary<Guid, Card[]>? shownCards = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends the current game state to a specific player.
    /// Used for reconnection scenarios or initial state sync.
    /// The player receives their personalized view with their own hole cards visible.
    /// </summary>
    /// <param name="table">The table to send state for.</param>
    /// <param name="playerId">The player to send state to.</param>
    /// <param name="shownCards">
    /// Optional dictionary of shown cards (for showdown scenarios).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the send operation.</returns>
    Task SendGameStateToPlayerAsync(
        Table table,
        Guid playerId,
        IReadOnlyDictionary<Guid, Card[]>? shownCards = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends the current game state to a specific spectator connection.
    /// Used for initial state sync when a spectator joins.
    /// Spectators see no hole cards (only community cards and public information).
    /// </summary>
    /// <param name="table">The table to send state for.</param>
    /// <param name="connectionId">The SignalR connection ID of the spectator.</param>
    /// <param name="shownCards">
    /// Optional dictionary of shown cards (for showdown scenarios).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the send operation.</returns>
    Task SendGameStateToSpectatorAsync(
        Table table,
        string connectionId,
        IReadOnlyDictionary<Guid, Card[]>? shownCards = null,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcasts a hand started event with dealt hole cards to all players.
    /// Each player receives their own hole cards privately.
    /// Spectators receive notification that a hand started but no hole cards.
    /// </summary>
    /// <param name="table">The table where the hand started.</param>
    /// <param name="holeCards">Dictionary of player IDs to their dealt hole cards.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the broadcast operation.</returns>
    Task BroadcastHandStartedAsync(
        Table table,
        IReadOnlyDictionary<Guid, Card[]> holeCards,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcasts a hand completed event with results.
    /// All viewers see the same result including winnings and shown cards.
    /// </summary>
    /// <param name="table">The table where the hand completed.</param>
    /// <param name="winnings">Dictionary of player IDs to amounts won.</param>
    /// <param name="shownCards">Optional cards that were shown at showdown.</param>
    /// <param name="handDescriptions">
    /// Optional dictionary of player IDs to their winning hand descriptions.
    /// Supports split pots where different players show different hands.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the broadcast operation.</returns>
    Task BroadcastHandCompletedAsync(
        Table table,
        IReadOnlyDictionary<Guid, decimal> winnings,
        IReadOnlyDictionary<Guid, Card[]>? shownCards = null,
        IReadOnlyDictionary<Guid, string>? handDescriptions = null,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcasts an action required notification to all viewers.
    /// Indicates which player must act and the timeout duration.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <param name="playerId">The player who must act.</param>
    /// <param name="timeoutSeconds">Seconds until timeout.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the broadcast operation.</returns>
    Task BroadcastActionRequiredAsync(
        Guid tableId,
        Guid playerId,
        int timeoutSeconds,
        CancellationToken ct = default);
}
