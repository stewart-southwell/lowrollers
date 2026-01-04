using LowRollers.Api.Domain.Betting;
using LowRollers.Api.Domain.Models;
using LowRollers.Api.Features.GameEngine.ActionTimer;
using LowRollers.Api.Features.GameEngine.Broadcasting;
using LowRollers.Api.Features.GameEngine.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Features.GameEngine;

/// <summary>
/// SignalR hub for real-time poker game communication.
/// Handles player actions, game state broadcasting, and connection management.
/// </summary>
public sealed class GameHub : Hub
{
    private readonly IGameOrchestrator _gameOrchestrator;
    private readonly IActionTimerService _actionTimerService;
    private readonly IGameStateBroadcaster _stateBroadcaster;
    private readonly IConnectionManager _connectionManager;
    private readonly Func<Guid, Table?> _tableProvider;
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        IGameOrchestrator gameOrchestrator,
        IActionTimerService actionTimerService,
        IGameStateBroadcaster stateBroadcaster,
        IConnectionManager connectionManager,
        Func<Guid, Table?> tableProvider,
        ILogger<GameHub> logger)
    {
        _gameOrchestrator = gameOrchestrator ?? throw new ArgumentNullException(nameof(gameOrchestrator));
        _actionTimerService = actionTimerService ?? throw new ArgumentNullException(nameof(actionTimerService));
        _stateBroadcaster = stateBroadcaster ?? throw new ArgumentNullException(nameof(stateBroadcaster));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _tableProvider = tableProvider ?? throw new ArgumentNullException(nameof(tableProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Connection Management

    /// <summary>
    /// Joins a player to a table's SignalR group.
    /// Must be called when a player sits down at a table.
    /// </summary>
    public async Task JoinTableAsync(Guid tableId, Guid playerId)
    {
        var groupName = GameHubConstants.GetTableGroupName(tableId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _connectionManager.AddPlayerConnection(Context.ConnectionId, tableId, playerId);

        _logger.LogInformation(
            "Player {PlayerId} joined table {TableId} (Connection: {ConnectionId})",
            playerId, tableId, Context.ConnectionId);

        await Clients.Group(groupName).SendAsync("PlayerJoined", playerId);

        // Send current game state to the joining player
        var table = _tableProvider(tableId);
        if (table != null)
        {
            await _stateBroadcaster.SendGameStateToPlayerAsync(table, playerId);
        }
    }

    /// <summary>
    /// Joins a spectator to a table's SignalR group.
    /// Spectators can watch the game but not participate.
    /// </summary>
    public async Task JoinAsSpectatorAsync(Guid tableId)
    {
        var groupName = GameHubConstants.GetTableGroupName(tableId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _connectionManager.AddSpectatorConnection(Context.ConnectionId, tableId);

        _logger.LogInformation(
            "Spectator joined table {TableId} (Connection: {ConnectionId})",
            tableId, Context.ConnectionId);

        await Clients.Group(groupName).SendAsync("SpectatorJoined");

        // Send current game state to this spectator only (sanitized - no hole cards)
        var table = _tableProvider(tableId);
        if (table != null)
        {
            await _stateBroadcaster.SendGameStateToSpectatorAsync(table, Context.ConnectionId);
        }
    }

    /// <summary>
    /// Removes a player or spectator from a table's SignalR group.
    /// Called when leaving a table.
    /// </summary>
    public async Task LeaveTableAsync()
    {
        var connection = _connectionManager.GetConnection(Context.ConnectionId);
        if (connection == null)
        {
            return;
        }

        var groupName = GameHubConstants.GetTableGroupName(connection.TableId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _connectionManager.RemoveConnection(Context.ConnectionId);

        if (connection.PlayerId.HasValue)
        {
            _logger.LogInformation(
                "Player {PlayerId} left table {TableId} (Connection: {ConnectionId})",
                connection.PlayerId.Value, connection.TableId, Context.ConnectionId);

            await Clients.Group(groupName).SendAsync("PlayerLeft", connection.PlayerId.Value);
        }
        else
        {
            _logger.LogInformation(
                "Spectator left table {TableId} (Connection: {ConnectionId})",
                connection.TableId, Context.ConnectionId);

            await Clients.Group(groupName).SendAsync("SpectatorLeft");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connection = _connectionManager.RemoveConnection(Context.ConnectionId);

        if (connection != null)
        {
            var groupName = GameHubConstants.GetTableGroupName(connection.TableId);

            if (connection.PlayerId.HasValue)
            {
                await Clients.Group(groupName).SendAsync("PlayerDisconnected", connection.PlayerId.Value);

                _logger.LogInformation(
                    "Player {PlayerId} disconnected from table {TableId} (Connection: {ConnectionId})",
                    connection.PlayerId.Value, connection.TableId, Context.ConnectionId);
            }
            else
            {
                _logger.LogDebug(
                    "Spectator disconnected from table {TableId} (Connection: {ConnectionId})",
                    connection.TableId, Context.ConnectionId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region Player Actions

    /// <summary>
    /// Folds the current hand. Always valid when it's the player's turn.
    /// </summary>
    public Task<ActionResult> FoldAsync()
    {
        return ExecuteActionAsync(PlayerActionType.Fold, 0);
    }

    /// <summary>
    /// Checks (passes without betting). Only valid when no bet is facing.
    /// </summary>
    public Task<ActionResult> CheckAsync()
    {
        return ExecuteActionAsync(PlayerActionType.Check, 0);
    }

    /// <summary>
    /// Calls the current bet. Only valid when there is a bet to call.
    /// </summary>
    public Task<ActionResult> CallAsync()
    {
        return ExecuteActionAsync(PlayerActionType.Call, 0);
    }

    /// <summary>
    /// Raises the current bet. Amount must meet minimum raise requirements.
    /// </summary>
    /// <param name="amount">The total bet amount (not the raise increment).</param>
    public Task<ActionResult> RaiseAsync(decimal amount)
    {
        return ExecuteActionAsync(PlayerActionType.Raise, amount);
    }

    /// <summary>
    /// Goes all-in with all remaining chips.
    /// </summary>
    public Task<ActionResult> AllInAsync()
    {
        return ExecuteActionAsync(PlayerActionType.AllIn, 0);
    }

    /// <summary>
    /// Gets the available actions for the current player.
    /// </summary>
    public Task<AvailableActions?> GetAvailableActionsAsync()
    {
        var connection = _connectionManager.GetConnection(Context.ConnectionId);
        if (connection?.PlayerId == null)
        {
            return Task.FromResult<AvailableActions?>(null);
        }

        var table = _tableProvider(connection.TableId);
        if (table == null)
        {
            return Task.FromResult<AvailableActions?>(null);
        }

        var actions = _gameOrchestrator.GetAvailableActions(table);
        return Task.FromResult(actions);
    }

    /// <summary>
    /// Gets the current timer state for the player's table.
    /// </summary>
    public Task<ActionTimerState?> GetTimerStateAsync()
    {
        var connection = _connectionManager.GetConnection(Context.ConnectionId);
        if (connection == null)
        {
            return Task.FromResult<ActionTimerState?>(null);
        }

        var state = _actionTimerService.GetTimerState(connection.TableId);
        return Task.FromResult(state);
    }

    #endregion

    #region Private Methods

    private async Task<ActionResult> ExecuteActionAsync(
        PlayerActionType actionType,
        decimal amount)
    {
        // Validate connection and get player info
        var connection = _connectionManager.GetConnection(Context.ConnectionId);
        if (connection?.PlayerId == null)
        {
            _logger.LogWarning(
                "Action {ActionType} rejected: Connection {ConnectionId} not associated with a player",
                actionType, Context.ConnectionId);
            return ActionResult.Failure("Not connected to a table as a player");
        }

        var tableId = connection.TableId;
        var playerId = connection.PlayerId.Value;

        // Get table from server (authoritative source)
        var table = _tableProvider(tableId);
        if (table == null)
        {
            _logger.LogWarning(
                "Action {ActionType} rejected: Table {TableId} not found",
                actionType, tableId);
            return ActionResult.Failure("Table not found");
        }

        // Validate it's the player's turn
        if (table.CurrentHand?.CurrentPlayerId != playerId)
        {
            _logger.LogWarning(
                "Action {ActionType} rejected: Not player {PlayerId}'s turn",
                actionType, playerId);
            return ActionResult.Failure("Not your turn");
        }

        _logger.LogInformation(
            "Player {PlayerId} executing {ActionType} at table {TableId} (amount: {Amount})",
            playerId, actionType, tableId, amount);

        // Execute the action
        var result = await _gameOrchestrator.ExecutePlayerActionAsync(
            table, playerId, actionType, amount);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Action {ActionType} failed for player {PlayerId}: {Error}",
                actionType, playerId, result.Error);
            return result;
        }

        // Cancel the timer since player acted
        var timeBankUsed = await _actionTimerService.CancelTimerAsync(tableId, playerId);

        // Update player's time bank if any was used
        if (timeBankUsed > 0 && table.Players.TryGetValue(playerId, out var player))
        {
            player.ConsumeTimeBank(timeBankUsed);
        }

        // Broadcast immediate action feedback to all viewers
        var groupName = GameHubConstants.GetTableGroupName(tableId);
        await Clients.Group(groupName).SendAsync("ActionExecuted", new ActionBroadcast
        {
            PlayerId = playerId,
            ActionType = actionType,
            Amount = amount,
            NextPlayerId = result.NextPlayerId,
            BettingRoundComplete = result.BettingRoundComplete,
            HandComplete = result.HandComplete
        });

        // Broadcast full game state to all players (with sanitized hole cards)
        await _stateBroadcaster.BroadcastGameStateAsync(table);

        // Start timer for next player if hand continues
        if (result.NextPlayerId.HasValue && !result.HandComplete)
        {
            await StartTimerForNextPlayer(table, result.NextPlayerId.Value);
        }

        _logger.LogInformation(
            "Action {ActionType} completed for player {PlayerId}. Next: {NextPlayerId}, RoundComplete: {RoundComplete}",
            actionType, playerId, result.NextPlayerId, result.BettingRoundComplete);

        return result;
    }

    private async Task StartTimerForNextPlayer(Table table, Guid nextPlayerId)
    {
        if (table.ActionTimerSeconds <= 0)
        {
            return; // Timer disabled
        }

        if (!table.Players.TryGetValue(nextPlayerId, out var player))
        {
            return;
        }

        if (table.CurrentHand == null)
        {
            return;
        }

        await _actionTimerService.StartTimerAsync(
            table.Id,
            table.CurrentHand.Id,
            nextPlayerId,
            table.ActionTimerSeconds,
            table.TimeBankEnabled,
            player.TimeBankSeconds);
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Lightweight broadcast payload for immediate action feedback.
    /// Full game state is sent separately via IGameStateBroadcaster.
    /// </summary>
    public sealed class ActionBroadcast
    {
        public required Guid PlayerId { get; init; }
        public required PlayerActionType ActionType { get; init; }
        public decimal Amount { get; init; }
        public Guid? NextPlayerId { get; init; }
        public bool BettingRoundComplete { get; init; }
        public bool HandComplete { get; init; }
    }

    #endregion
}
