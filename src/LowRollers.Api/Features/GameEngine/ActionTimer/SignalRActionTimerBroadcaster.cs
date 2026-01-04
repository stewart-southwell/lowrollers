using Microsoft.AspNetCore.SignalR;

namespace LowRollers.Api.Features.GameEngine.ActionTimer;

/// <summary>
/// Implements IActionTimerBroadcaster using SignalR to broadcast timer events to clients.
/// Uses the GameHub's client groups to send messages to all players at a table.
/// </summary>
public sealed class SignalRActionTimerBroadcaster : IActionTimerBroadcaster
{
    private readonly IHubContext<GameHub> _hubContext;

    public SignalRActionTimerBroadcaster(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    /// <inheritdoc/>
    public async Task BroadcastTimerStartedAsync(
        Guid tableId,
        Guid playerId,
        int totalSeconds,
        int timeBankAvailable,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group(GameHubConstants.GetTableGroupName(tableId))
            .SendAsync("TimerStarted", new TimerStartedMessage
            {
                PlayerId = playerId,
                TotalSeconds = totalSeconds,
                TimeBankAvailable = timeBankAvailable
            }, ct);
    }

    /// <inheritdoc/>
    public async Task BroadcastTimerTickAsync(
        Guid tableId,
        Guid playerId,
        int remainingSeconds,
        bool isTimeBankActive,
        int timeBankRemaining,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group(GameHubConstants.GetTableGroupName(tableId))
            .SendAsync("TimerTick", new TimerTickMessage
            {
                PlayerId = playerId,
                RemainingSeconds = remainingSeconds,
                IsTimeBankActive = isTimeBankActive,
                TimeBankRemaining = timeBankRemaining
            }, ct);
    }

    /// <inheritdoc/>
    public async Task BroadcastTimerWarningAsync(
        Guid tableId,
        Guid playerId,
        int remainingSeconds,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group(GameHubConstants.GetTableGroupName(tableId))
            .SendAsync("TimerWarning", new TimerWarningMessage
            {
                PlayerId = playerId,
                RemainingSeconds = remainingSeconds
            }, ct);
    }

    /// <inheritdoc/>
    public async Task BroadcastTimerCancelledAsync(
        Guid tableId,
        Guid playerId,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group(GameHubConstants.GetTableGroupName(tableId))
            .SendAsync("TimerCancelled", new TimerCancelledMessage
            {
                PlayerId = playerId
            }, ct);
    }

    /// <inheritdoc/>
    public async Task BroadcastTimeBankActivatedAsync(
        Guid tableId,
        Guid playerId,
        int timeBankSecondsAdded,
        int timeBankRemaining,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group(GameHubConstants.GetTableGroupName(tableId))
            .SendAsync("TimeBankActivated", new TimeBankActivatedMessage
            {
                PlayerId = playerId,
                TimeBankSecondsAdded = timeBankSecondsAdded,
                TimeBankRemaining = timeBankRemaining
            }, ct);
    }

    /// <inheritdoc/>
    public async Task BroadcastTimerExpiredAsync(
        Guid tableId,
        Guid playerId,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group(GameHubConstants.GetTableGroupName(tableId))
            .SendAsync("TimerExpired", new TimerExpiredMessage
            {
                PlayerId = playerId
            }, ct);
    }

    #region Message Types

    /// <summary>
    /// Message sent when a player's action timer starts.
    /// </summary>
    public sealed class TimerStartedMessage
    {
        public required Guid PlayerId { get; init; }
        public required int TotalSeconds { get; init; }
        public required int TimeBankAvailable { get; init; }
    }

    /// <summary>
    /// Message sent every second while the timer is running.
    /// </summary>
    public sealed class TimerTickMessage
    {
        public required Guid PlayerId { get; init; }
        public required int RemainingSeconds { get; init; }
        public required bool IsTimeBankActive { get; init; }
        public required int TimeBankRemaining { get; init; }
    }

    /// <summary>
    /// Message sent when the timer reaches the warning threshold.
    /// </summary>
    public sealed class TimerWarningMessage
    {
        public required Guid PlayerId { get; init; }
        public required int RemainingSeconds { get; init; }
    }

    /// <summary>
    /// Message sent when the timer is cancelled (player acted).
    /// </summary>
    public sealed class TimerCancelledMessage
    {
        public required Guid PlayerId { get; init; }
    }

    /// <summary>
    /// Message sent when the time bank is activated.
    /// </summary>
    public sealed class TimeBankActivatedMessage
    {
        public required Guid PlayerId { get; init; }
        public required int TimeBankSecondsAdded { get; init; }
        public required int TimeBankRemaining { get; init; }
    }

    /// <summary>
    /// Message sent when the timer expires and player will be auto-folded.
    /// </summary>
    public sealed class TimerExpiredMessage
    {
        public required Guid PlayerId { get; init; }
    }

    #endregion
}
