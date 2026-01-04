namespace LowRollers.Api.Features.GameEngine.ActionTimer;

/// <summary>
/// Manages action timers for poker tables.
/// Tracks player turn timers, broadcasts ticks, handles time bank, and auto-folds on expiry.
/// </summary>
public interface IActionTimerService
{
    /// <summary>
    /// Starts an action timer for a player's turn.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <param name="handId">The current hand ID.</param>
    /// <param name="playerId">The player whose turn it is.</param>
    /// <param name="actionSeconds">Seconds allowed for the action (0 for unlimited).</param>
    /// <param name="timeBankEnabled">Whether time bank is enabled for this table.</param>
    /// <param name="timeBankSeconds">Seconds available in the player's time bank.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if timer was started; false if timer is disabled (actionSeconds = 0).</returns>
    Task<bool> StartTimerAsync(
        Guid tableId,
        Guid handId,
        Guid playerId,
        int actionSeconds,
        bool timeBankEnabled,
        int timeBankSeconds,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels the action timer for a table (player acted).
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <param name="playerId">The player who acted (for validation).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The time bank seconds used, or 0 if none used.</returns>
    Task<int> CancelTimerAsync(Guid tableId, Guid playerId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current timer state for a table.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <returns>The current timer state, or null if no timer is active.</returns>
    ActionTimerState? GetTimerState(Guid tableId);

    /// <summary>
    /// Checks if a timer is active for a table.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <returns>True if a timer is running.</returns>
    bool IsTimerActive(Guid tableId);

    /// <summary>
    /// Pauses all timers (e.g., when game is paused).
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    Task PauseTimerAsync(Guid tableId);

    /// <summary>
    /// Resumes a paused timer.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    Task ResumeTimerAsync(Guid tableId);

    /// <summary>
    /// Stops all timers for a table (e.g., when hand ends).
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    Task StopAllTimersAsync(Guid tableId);
}
