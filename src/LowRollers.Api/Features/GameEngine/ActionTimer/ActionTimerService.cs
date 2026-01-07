using System.Collections.Concurrent;
using System.Timers;
using LowRollers.Api.Domain.Models;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace LowRollers.Api.Features.GameEngine.ActionTimer;

/// <summary>
/// Background service that manages action timers for poker tables.
/// Uses System.Timers to tick every second and broadcast timer updates to clients.
/// </summary>
public sealed partial class ActionTimerService : IActionTimerService, IDisposable
{
    private const int WarningThresholdSeconds = 10;
    private const int TickIntervalMs = 1000;

    private readonly IActionTimerBroadcaster _broadcaster;
    private readonly IGameOrchestrator _gameOrchestrator;
    private readonly Func<Guid, Table?> _tableProvider;
    private readonly ILogger<ActionTimerService> _logger;

    private readonly ConcurrentDictionary<Guid, TimerContext> _timers = new();

    public ActionTimerService(
        IActionTimerBroadcaster broadcaster,
        IGameOrchestrator gameOrchestrator,
        Func<Guid, Table?> tableProvider,
        ILogger<ActionTimerService> logger)
    {
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
        _gameOrchestrator = gameOrchestrator ?? throw new ArgumentNullException(nameof(gameOrchestrator));
        _tableProvider = tableProvider ?? throw new ArgumentNullException(nameof(tableProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> StartTimerAsync(
        Guid tableId,
        Guid handId,
        Guid playerId,
        int actionSeconds,
        bool timeBankEnabled,
        int timeBankSeconds,
        CancellationToken ct = default)
    {
        // If action timer is disabled (0), don't start a timer
        if (actionSeconds <= 0)
        {
            Log.ActionTimerDisabled(_logger, tableId);
            return false;
        }

        // Stop any existing timer for this table
        await StopAllTimersAsync(tableId);

        var state = ActionTimerState.Create(
            tableId,
            handId,
            playerId,
            actionSeconds,
            timeBankEnabled,
            timeBankSeconds);

        var timer = new Timer(TickIntervalMs)
        {
            AutoReset = true,
            Enabled = false
        };

        var context = new TimerContext(state, timer, new CancellationTokenSource());

        timer.Elapsed += async (_, _) => await OnTimerTickAsync(tableId);

        if (!_timers.TryAdd(tableId, context))
        {
            // Failed to add, cleanup
            timer.Dispose();
            context.CancellationTokenSource.Dispose();
            Log.FailedToStartTimerAlreadyExists(_logger, tableId);
            return false;
        }

        timer.Start();

        Log.StartedActionTimer(_logger, playerId, tableId, actionSeconds, timeBankEnabled, timeBankSeconds);

        // Broadcast timer started
        await _broadcaster.BroadcastTimerStartedAsync(
            tableId, playerId, actionSeconds, timeBankSeconds, ct);

        return true;
    }

    /// <inheritdoc/>
    public async Task<int> CancelTimerAsync(Guid tableId, Guid playerId, CancellationToken ct = default)
    {
        if (!_timers.TryRemove(tableId, out var context))
        {
            return 0;
        }

        ActionTimerState state;
        lock (context.StateLock)
        {
            state = context.State;
        }

        // Validate it's the right player
        if (state.ActivePlayerId != playerId)
        {
            Log.TimerCancelMismatch(_logger, state.ActivePlayerId, playerId);
        }

        // Calculate time bank used
        var timeBankUsed = state.TimeBankSecondsUsed;

        // Clean up timer
        context.Timer.Stop();
        context.Timer.Dispose();
        context.CancellationTokenSource.Cancel();
        context.CancellationTokenSource.Dispose();

        Log.CancelledTimer(_logger, playerId, tableId, timeBankUsed);

        // Broadcast timer cancelled
        await _broadcaster.BroadcastTimerCancelledAsync(tableId, playerId, ct);

        return timeBankUsed;
    }

    /// <inheritdoc/>
    public ActionTimerState? GetTimerState(Guid tableId)
    {
        if (!_timers.TryGetValue(tableId, out var context))
        {
            return null;
        }

        lock (context.StateLock)
        {
            return context.State;
        }
    }

    /// <inheritdoc/>
    public bool IsTimerActive(Guid tableId)
    {
        return _timers.TryGetValue(tableId, out var context) && !context.IsPaused;
    }

    /// <inheritdoc/>
    public Task PauseTimerAsync(Guid tableId)
    {
        if (_timers.TryGetValue(tableId, out var context))
        {
            context.Timer.Stop();
            context.IsPaused = true;
            Log.PausedTimer(_logger, tableId);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResumeTimerAsync(Guid tableId)
    {
        if (_timers.TryGetValue(tableId, out var context) && context.IsPaused)
        {
            context.IsPaused = false;
            context.Timer.Start();
            Log.ResumedTimer(_logger, tableId);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAllTimersAsync(Guid tableId)
    {
        if (_timers.TryRemove(tableId, out var context))
        {
            context.Timer.Stop();
            context.Timer.Dispose();
            context.CancellationTokenSource.Cancel();
            context.CancellationTokenSource.Dispose();
            Log.StoppedAllTimers(_logger, tableId);
        }
        return Task.CompletedTask;
    }

    private async Task OnTimerTickAsync(Guid tableId)
    {
        if (!_timers.TryGetValue(tableId, out var context) || context.IsPaused)
        {
            return;
        }

        var ct = context.CancellationTokenSource.Token;
        if (ct.IsCancellationRequested)
        {
            return;
        }

        try
        {
            ActionTimerState newState;
            bool needsTimeBankBroadcast;
            bool needsWarningBroadcast;
            bool isExpired;

            // Update state atomically under lock
            lock (context.StateLock)
            {
                var oldState = context.State;
                newState = oldState.Tick();

                // Check if time bank just became active
                needsTimeBankBroadcast = newState.NeedsTimeBankActivationBroadcast;
                if (needsTimeBankBroadcast)
                {
                    newState = newState.WithTimeBankActivationBroadcast();
                }

                // Check for warning threshold
                needsWarningBroadcast = !newState.WarningSent &&
                    newState.EffectiveRemainingSeconds <= WarningThresholdSeconds;
                if (needsWarningBroadcast)
                {
                    newState = newState.WithWarningSent();
                }

                isExpired = newState.IsExpired;

                // Update state
                context.State = newState;
            }

            // Perform broadcasts outside the lock to avoid holding it during I/O
            if (needsTimeBankBroadcast)
            {
                await _broadcaster.BroadcastTimeBankActivatedAsync(
                    tableId,
                    newState.ActivePlayerId,
                    newState.OriginalTimeBankSeconds,
                    newState.TimeBankRemainingSeconds,
                    ct);
            }

            if (needsWarningBroadcast)
            {
                await _broadcaster.BroadcastTimerWarningAsync(
                    tableId,
                    newState.ActivePlayerId,
                    newState.EffectiveRemainingSeconds,
                    ct);
            }

            if (isExpired)
            {
                await HandleTimerExpiredAsync(tableId, newState, ct);
                return;
            }

            // Broadcast tick
            await _broadcaster.BroadcastTimerTickAsync(
                tableId,
                newState.ActivePlayerId,
                newState.RemainingSeconds,
                newState.IsTimeBankActive,
                newState.TimeBankRemainingSeconds,
                ct);
        }
        catch (OperationCanceledException)
        {
            // Timer was cancelled, ignore
        }
        catch (Exception ex)
        {
            Log.ErrorProcessingTimerTick(_logger, ex, tableId);
        }
    }

    private async Task HandleTimerExpiredAsync(Guid tableId, ActionTimerState state, CancellationToken ct)
    {
        Log.TimerExpiredAutoFolding(_logger, state.ActivePlayerId, tableId);

        // Remove and cleanup timer first to prevent further ticks
        if (_timers.TryRemove(tableId, out var removed))
        {
            removed.Timer.Stop();
            removed.Timer.Dispose();
            removed.CancellationTokenSource.Dispose();
        }

        // Broadcast expiry
        await _broadcaster.BroadcastTimerExpiredAsync(tableId, state.ActivePlayerId, ct);

        // Get table and execute auto-fold
        var table = _tableProvider(tableId);
        if (table != null)
        {
            try
            {
                // Calculate time bank consumed (all of it was used on expiry)
                var timeBankConsumed = state.IsTimeBankActive ? state.OriginalTimeBankSeconds : 0;

                await _gameOrchestrator.ForceTimeoutFoldAsync(table, timeBankConsumed, ct);
            }
            catch (Exception ex)
            {
                Log.FailedToExecuteAutoFold(_logger, ex, state.ActivePlayerId, tableId);
            }
        }
        else
        {
            Log.TableNotFoundForAutoFold(_logger, tableId);
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _timers)
        {
            kvp.Value.Timer.Stop();
            kvp.Value.Timer.Dispose();
            kvp.Value.CancellationTokenSource.Cancel();
            kvp.Value.CancellationTokenSource.Dispose();
        }
        _timers.Clear();
    }

    /// <summary>
    /// Internal context for tracking timer state and resources per table.
    /// </summary>
    private sealed class TimerContext
    {
        private volatile bool _isPaused;

        public TimerContext(ActionTimerState state, Timer timer, CancellationTokenSource cts)
        {
            State = state;
            Timer = timer;
            CancellationTokenSource = cts;
        }

        public ActionTimerState State { get; set; }
        public Timer Timer { get; }
        public CancellationTokenSource CancellationTokenSource { get; }

        /// <summary>
        /// Whether the timer is paused. Uses volatile for thread-safe reads/writes
        /// without lock overhead. Worst case is a one-tick delay on pause/resume.
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            set => _isPaused = value;
        }

        /// <summary>
        /// Lock object for synchronizing state updates.
        /// Timer.Elapsed runs on thread pool threads, so we need to protect state access.
        /// </summary>
        public object StateLock { get; } = new();
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Debug, Message = "Action timer disabled for table {TableId}")]
        public static partial void ActionTimerDisabled(ILogger logger, Guid tableId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to start timer for table {TableId} - timer already exists")]
        public static partial void FailedToStartTimerAlreadyExists(ILogger logger, Guid tableId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Started action timer for player {PlayerId} at table {TableId}. Time: {Seconds}s, TimeBank: {TimeBankEnabled} ({TimeBankSeconds}s)")]
        public static partial void StartedActionTimer(ILogger logger, Guid playerId, Guid tableId, int seconds, bool timeBankEnabled, int timeBankSeconds);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Timer cancel mismatch: expected player {Expected}, got {Actual}")]
        public static partial void TimerCancelMismatch(ILogger logger, Guid expected, Guid actual);

        [LoggerMessage(Level = LogLevel.Information, Message = "Cancelled timer for player {PlayerId} at table {TableId}. TimeBankUsed: {TimeBankUsed}s")]
        public static partial void CancelledTimer(ILogger logger, Guid playerId, Guid tableId, int timeBankUsed);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Paused timer for table {TableId}")]
        public static partial void PausedTimer(ILogger logger, Guid tableId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Resumed timer for table {TableId}")]
        public static partial void ResumedTimer(ILogger logger, Guid tableId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Stopped all timers for table {TableId}")]
        public static partial void StoppedAllTimers(ILogger logger, Guid tableId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error processing timer tick for table {TableId}")]
        public static partial void ErrorProcessingTimerTick(ILogger logger, Exception ex, Guid tableId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Timer expired for player {PlayerId} at table {TableId}. Auto-folding.")]
        public static partial void TimerExpiredAutoFolding(ILogger logger, Guid playerId, Guid tableId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to execute auto-fold for player {PlayerId} at table {TableId}")]
        public static partial void FailedToExecuteAutoFold(ILogger logger, Exception ex, Guid playerId, Guid tableId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Table {TableId} not found for auto-fold")]
        public static partial void TableNotFoundForAutoFold(ILogger logger, Guid tableId);
    }
}
