using LowRollers.Api.Domain.Models;
using LowRollers.Api.Features.GameEngine.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Features.GameEngine.Broadcasting;

/// <summary>
/// Implements game state broadcasting using SignalR.
/// Sends personalized state views to each connected client based on their role.
/// </summary>
public sealed partial class SignalRGameStateBroadcaster : IGameStateBroadcaster
{
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IConnectionManager _connectionManager;
    private readonly IGameStateSanitizer _sanitizer;
    private readonly ILogger<SignalRGameStateBroadcaster> _logger;

    public SignalRGameStateBroadcaster(
        IHubContext<GameHub> hubContext,
        IConnectionManager connectionManager,
        IGameStateSanitizer sanitizer,
        ILogger<SignalRGameStateBroadcaster> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task BroadcastGameStateAsync(
        Table table,
        IReadOnlyDictionary<Guid, Card[]>? shownCards = null,
        CancellationToken ct = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        // Get all player connections for personalized broadcasts
        var playerConnections = _connectionManager.GetPlayerConnections(table.Id);
        var spectatorConnections = _connectionManager.GetSpectatorConnections(table.Id);

        var tasks = new List<Task>();

        // Send personalized state to each player
        foreach (var (connectionId, playerId) in playerConnections)
        {
            var state = _sanitizer.Sanitize(table, playerId, shownCards);
            tasks.Add(_hubContext.Clients.Client(connectionId)
                .SendAsync("GameStateUpdated", state, ct));
        }

        // Send spectator view (no hole cards visible)
        if (spectatorConnections.Count > 0)
        {
            var spectatorState = _sanitizer.Sanitize(table, viewerPlayerId: null, shownCards);
            foreach (var connectionId in spectatorConnections)
            {
                tasks.Add(_hubContext.Clients.Client(connectionId)
                    .SendAsync("GameStateUpdated", spectatorState, ct));
            }
        }

        await Task.WhenAll(tasks);

        var elapsed = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
        Log.BroadcastGameStateCompleted(_logger, table.Id, playerConnections.Count, spectatorConnections.Count, elapsed);

        if (elapsed > 100)
        {
            Log.BroadcastExceededTarget(_logger, elapsed, table.Id);
        }
    }

    /// <inheritdoc/>
    public async Task SendGameStateToPlayerAsync(
        Table table,
        Guid playerId,
        IReadOnlyDictionary<Guid, Card[]>? shownCards = null,
        CancellationToken ct = default)
    {
        var connectionId = _connectionManager.GetPlayerConnectionId(table.Id, playerId);
        if (connectionId == null)
        {
            Log.PlayerNotConnected(_logger, playerId, table.Id);
            return;
        }

        var state = _sanitizer.Sanitize(table, playerId, shownCards);
        await _hubContext.Clients.Client(connectionId)
            .SendAsync("GameStateUpdated", state, ct);

        Log.SentGameStateToPlayer(_logger, playerId, table.Id);
    }

    /// <inheritdoc/>
    public async Task SendGameStateToSpectatorAsync(
        Table table,
        string connectionId,
        IReadOnlyDictionary<Guid, Card[]>? shownCards = null,
        CancellationToken ct = default)
    {
        var state = _sanitizer.Sanitize(table, viewerPlayerId: null, shownCards);
        await _hubContext.Clients.Client(connectionId)
            .SendAsync("GameStateUpdated", state, ct);

        Log.SentGameStateToSpectator(_logger, connectionId, table.Id);
    }

    /// <inheritdoc/>
    public async Task BroadcastHandStartedAsync(
        Table table,
        IReadOnlyDictionary<Guid, Card[]> holeCards,
        CancellationToken ct = default)
    {
        var playerConnections = _connectionManager.GetPlayerConnections(table.Id);
        var spectatorConnections = _connectionManager.GetSpectatorConnections(table.Id);

        var tasks = new List<Task>();

        // Send each player their own hole cards with the game state
        foreach (var (connectionId, playerId) in playerConnections)
        {
            var state = _sanitizer.Sanitize(table, playerId, shownCards: null);
            var myCards = holeCards.TryGetValue(playerId, out var cards)
                ? cards.Select(CardDto.FromCard).ToArray()
                : null;

            tasks.Add(_hubContext.Clients.Client(connectionId)
                .SendAsync("HandStarted", new HandStartedMessage
                {
                    GameState = state,
                    YourHoleCards = myCards
                }, ct));
        }

        // Notify spectators (no hole cards)
        if (spectatorConnections.Count > 0)
        {
            var spectatorState = _sanitizer.Sanitize(table, viewerPlayerId: null, shownCards: null);
            foreach (var connectionId in spectatorConnections)
            {
                tasks.Add(_hubContext.Clients.Client(connectionId)
                    .SendAsync("HandStarted", new HandStartedMessage
                    {
                        GameState = spectatorState,
                        YourHoleCards = null
                    }, ct));
            }
        }

        await Task.WhenAll(tasks);

        Log.BroadcastHandStarted(_logger, table.Id, table.CurrentHand?.HandNumber ?? 0, playerConnections.Count + spectatorConnections.Count);
    }

    /// <inheritdoc/>
    public async Task BroadcastHandCompletedAsync(
        Table table,
        IReadOnlyDictionary<Guid, decimal> winnings,
        IReadOnlyDictionary<Guid, Card[]>? shownCards = null,
        IReadOnlyDictionary<Guid, string>? handDescriptions = null,
        CancellationToken ct = default)
    {
        var groupName = GameHubConstants.GetTableGroupName(table.Id);

        // Build winner info
        var winners = winnings.Select(w => new WinnerInfo
        {
            PlayerId = w.Key,
            Amount = w.Value,
            ShownCards = shownCards?.TryGetValue(w.Key, out var cards) == true
                ? cards.Select(CardDto.FromCard).ToArray()
                : null,
            HandDescription = handDescriptions?.TryGetValue(w.Key, out var desc) == true
                ? desc
                : null
        }).ToList();

        // Hand complete message is the same for all viewers
        var message = new HandCompletedMessage
        {
            TableId = table.Id,
            HandNumber = table.CurrentHand?.HandNumber ?? 0,
            Winners = winners,
            FinalPot = table.CurrentHand?.TotalPot ?? 0
        };

        await _hubContext.Clients.Group(groupName)
            .SendAsync("HandCompleted", message, ct);

        Log.BroadcastHandCompleted(_logger, table.Id, message.HandNumber, winners.Count);
    }

    /// <inheritdoc/>
    public async Task BroadcastActionRequiredAsync(
        Guid tableId,
        Guid playerId,
        int timeoutSeconds,
        CancellationToken ct = default)
    {
        var groupName = GameHubConstants.GetTableGroupName(tableId);

        await _hubContext.Clients.Group(groupName)
            .SendAsync("ActionRequired", new ActionRequiredMessage
            {
                PlayerId = playerId,
                TimeoutSeconds = timeoutSeconds
            }, ct);

        Log.BroadcastActionRequired(_logger, playerId, tableId, timeoutSeconds);
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Debug, Message = "Broadcast game state for table {TableId} to {PlayerCount} players and {SpectatorCount} spectators in {ElapsedMs:F1}ms")]
        public static partial void BroadcastGameStateCompleted(ILogger logger, Guid tableId, int playerCount, int spectatorCount, double elapsedMs);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Game state broadcast exceeded 100ms target: {ElapsedMs:F1}ms for table {TableId}")]
        public static partial void BroadcastExceededTarget(ILogger logger, double elapsedMs, Guid tableId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot send state to player {PlayerId}: not connected to table {TableId}")]
        public static partial void PlayerNotConnected(ILogger logger, Guid playerId, Guid tableId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Sent game state to player {PlayerId} on table {TableId}")]
        public static partial void SentGameStateToPlayer(ILogger logger, Guid playerId, Guid tableId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Sent game state to spectator (Connection: {ConnectionId}) on table {TableId}")]
        public static partial void SentGameStateToSpectator(ILogger logger, string connectionId, Guid tableId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Broadcast hand started for table {TableId}, hand #{HandNumber} to {ClientCount} clients")]
        public static partial void BroadcastHandStarted(ILogger logger, Guid tableId, int handNumber, int clientCount);

        [LoggerMessage(Level = LogLevel.Information, Message = "Broadcast hand completed for table {TableId}, hand #{HandNumber}. Winners: {WinnerCount}")]
        public static partial void BroadcastHandCompleted(ILogger logger, Guid tableId, int handNumber, int winnerCount);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Broadcast action required for player {PlayerId} at table {TableId}. Timeout: {Timeout}s")]
        public static partial void BroadcastActionRequired(ILogger logger, Guid playerId, Guid tableId, int timeout);
    }

    #region Message Types

    /// <summary>
    /// Message sent when a new hand starts.
    /// </summary>
    public sealed class HandStartedMessage
    {
        public required TableGameState GameState { get; init; }
        public CardDto[]? YourHoleCards { get; init; }
    }

    /// <summary>
    /// Message sent when a hand completes.
    /// </summary>
    public sealed class HandCompletedMessage
    {
        public required Guid TableId { get; init; }
        public required int HandNumber { get; init; }
        public required IReadOnlyList<WinnerInfo> Winners { get; init; }
        public required decimal FinalPot { get; init; }
    }

    /// <summary>
    /// Information about a winner.
    /// </summary>
    public sealed class WinnerInfo
    {
        public required Guid PlayerId { get; init; }
        public required decimal Amount { get; init; }
        public CardDto[]? ShownCards { get; init; }
        public string? HandDescription { get; init; }
    }

    /// <summary>
    /// Message indicating a player must act.
    /// </summary>
    public sealed class ActionRequiredMessage
    {
        public required Guid PlayerId { get; init; }
        public required int TimeoutSeconds { get; init; }
    }

    #endregion
}
