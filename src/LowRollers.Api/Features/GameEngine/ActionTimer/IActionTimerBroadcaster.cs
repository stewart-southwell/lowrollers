namespace LowRollers.Api.Features.GameEngine.ActionTimer;

/// <summary>
/// Broadcasts action timer events to connected clients.
/// Implemented by SignalR hub adapter.
/// </summary>
public interface IActionTimerBroadcaster
{
    /// <summary>
    /// Broadcasts a timer tick to all clients at the table.
    /// Called every second while a player's timer is running.
    /// </summary>
    /// <param name="tableId">The table to broadcast to.</param>
    /// <param name="playerId">The player whose turn it is.</param>
    /// <param name="remainingSeconds">Seconds remaining on the action timer.</param>
    /// <param name="isTimeBankActive">Whether the time bank is currently being used.</param>
    /// <param name="timeBankRemaining">Seconds remaining in the time bank.</param>
    /// <param name="ct">Cancellation token.</param>
    Task BroadcastTimerTickAsync(
        Guid tableId,
        Guid playerId,
        int remainingSeconds,
        bool isTimeBankActive,
        int timeBankRemaining,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcasts a warning that the player is running low on time.
    /// Called when the timer reaches the warning threshold (10 seconds).
    /// </summary>
    /// <param name="tableId">The table to broadcast to.</param>
    /// <param name="playerId">The player running low on time.</param>
    /// <param name="remainingSeconds">Seconds remaining.</param>
    /// <param name="ct">Cancellation token.</param>
    Task BroadcastTimerWarningAsync(
        Guid tableId,
        Guid playerId,
        int remainingSeconds,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcasts that the timer has started for a player.
    /// </summary>
    /// <param name="tableId">The table to broadcast to.</param>
    /// <param name="playerId">The player whose timer started.</param>
    /// <param name="totalSeconds">Total seconds allowed for the action.</param>
    /// <param name="timeBankAvailable">Seconds available in time bank.</param>
    /// <param name="ct">Cancellation token.</param>
    Task BroadcastTimerStartedAsync(
        Guid tableId,
        Guid playerId,
        int totalSeconds,
        int timeBankAvailable,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcasts that the timer was cancelled (player acted).
    /// </summary>
    /// <param name="tableId">The table to broadcast to.</param>
    /// <param name="playerId">The player whose timer was cancelled.</param>
    /// <param name="ct">Cancellation token.</param>
    Task BroadcastTimerCancelledAsync(
        Guid tableId,
        Guid playerId,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcasts that the player's time bank has been activated.
    /// Called when the main timer expires but time bank is available.
    /// </summary>
    /// <param name="tableId">The table to broadcast to.</param>
    /// <param name="playerId">The player whose time bank was activated.</param>
    /// <param name="timeBankSecondsAdded">Seconds added from time bank.</param>
    /// <param name="timeBankRemaining">Total seconds remaining in time bank after activation.</param>
    /// <param name="ct">Cancellation token.</param>
    Task BroadcastTimeBankActivatedAsync(
        Guid tableId,
        Guid playerId,
        int timeBankSecondsAdded,
        int timeBankRemaining,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcasts that the timer expired and the player will be auto-folded.
    /// </summary>
    /// <param name="tableId">The table to broadcast to.</param>
    /// <param name="playerId">The player who timed out.</param>
    /// <param name="ct">Cancellation token.</param>
    Task BroadcastTimerExpiredAsync(
        Guid tableId,
        Guid playerId,
        CancellationToken ct = default);
}
