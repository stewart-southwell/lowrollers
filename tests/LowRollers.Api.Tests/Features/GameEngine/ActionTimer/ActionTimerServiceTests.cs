using LowRollers.Api.Domain.Models;
using LowRollers.Api.Features.GameEngine;
using LowRollers.Api.Features.GameEngine.ActionTimer;
using Microsoft.Extensions.Logging.Abstractions;

namespace LowRollers.Api.Tests.Features.GameEngine.ActionTimer;

public class ActionTimerServiceTests : IDisposable
{
    private readonly MockActionTimerBroadcaster _broadcaster;
    private readonly MockGameOrchestrator _orchestrator;
    private readonly Dictionary<Guid, Table> _tables;
    private readonly ActionTimerService _service;

    public ActionTimerServiceTests()
    {
        _broadcaster = new MockActionTimerBroadcaster();
        _orchestrator = new MockGameOrchestrator();
        _tables = new Dictionary<Guid, Table>();

        _service = new ActionTimerService(
            _broadcaster,
            _orchestrator,
            tableId => _tables.TryGetValue(tableId, out var table) ? table : null,
            NullLogger<ActionTimerService>.Instance);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    private Table CreateTestTable(Guid? tableId = null)
    {
        var id = tableId ?? Guid.NewGuid();
        var table = new Table
        {
            Id = id,
            Name = "Test Table",
            SmallBlind = 1m,
            BigBlind = 2m,
            ActionTimerSeconds = 30,
            TimeBankEnabled = true,
            InitialTimeBankSeconds = 60
        };

        var player = Player.Create(
            Guid.NewGuid(),
            "Player1",
            seatPosition: 1,
            buyInAmount: 100m);
        player.TimeBankSeconds = 60;
        table.Players[player.Id] = player;

        _tables[id] = table;
        return table;
    }

    #region StartTimerAsync Tests

    [Fact]
    public async Task StartTimerAsync_WithValidSettings_ReturnsTrue()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var handId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        // Act
        var result = await _service.StartTimerAsync(
            tableId, handId, playerId,
            actionSeconds: 30,
            timeBankEnabled: true,
            timeBankSeconds: 60);

        // Assert
        Assert.True(result);
        Assert.True(_service.IsTimerActive(tableId));
    }

    [Fact]
    public async Task StartTimerAsync_WithZeroSeconds_ReturnsFalse()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var handId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        // Act
        var result = await _service.StartTimerAsync(
            tableId, handId, playerId,
            actionSeconds: 0,
            timeBankEnabled: true,
            timeBankSeconds: 60);

        // Assert
        Assert.False(result);
        Assert.False(_service.IsTimerActive(tableId));
    }

    [Fact]
    public async Task StartTimerAsync_BroadcastsTimerStarted()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var handId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        // Act
        await _service.StartTimerAsync(
            tableId, handId, playerId,
            actionSeconds: 30,
            timeBankEnabled: true,
            timeBankSeconds: 60);

        // Assert
        Assert.Single(_broadcaster.TimerStartedEvents);
        var evt = _broadcaster.TimerStartedEvents[0];
        Assert.Equal(tableId, evt.TableId);
        Assert.Equal(playerId, evt.PlayerId);
        Assert.Equal(30, evt.TotalSeconds);
        Assert.Equal(60, evt.TimeBankAvailable);
    }

    [Fact]
    public async Task StartTimerAsync_StopsExistingTimer()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var handId = Guid.NewGuid();
        var playerId1 = Guid.NewGuid();
        var playerId2 = Guid.NewGuid();

        // Start first timer
        await _service.StartTimerAsync(
            tableId, handId, playerId1,
            actionSeconds: 30,
            timeBankEnabled: true,
            timeBankSeconds: 60);

        // Act - Start second timer for same table
        await _service.StartTimerAsync(
            tableId, handId, playerId2,
            actionSeconds: 30,
            timeBankEnabled: true,
            timeBankSeconds: 60);

        // Assert
        var state = _service.GetTimerState(tableId);
        Assert.NotNull(state);
        Assert.Equal(playerId2, state.ActivePlayerId);
    }

    #endregion

    #region CancelTimerAsync Tests

    [Fact]
    public async Task CancelTimerAsync_WithActiveTimer_StopsTimer()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var handId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        await _service.StartTimerAsync(
            tableId, handId, playerId,
            actionSeconds: 30,
            timeBankEnabled: true,
            timeBankSeconds: 60);

        // Act
        var timeBankUsed = await _service.CancelTimerAsync(tableId, playerId);

        // Assert
        Assert.False(_service.IsTimerActive(tableId));
        Assert.Null(_service.GetTimerState(tableId));
        Assert.Equal(0, timeBankUsed); // No time bank used yet
    }

    [Fact]
    public async Task CancelTimerAsync_BroadcastsTimerCancelled()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var handId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        await _service.StartTimerAsync(
            tableId, handId, playerId,
            actionSeconds: 30,
            timeBankEnabled: true,
            timeBankSeconds: 60);

        // Act
        await _service.CancelTimerAsync(tableId, playerId);

        // Assert
        Assert.Single(_broadcaster.TimerCancelledEvents);
        Assert.Equal(tableId, _broadcaster.TimerCancelledEvents[0].TableId);
        Assert.Equal(playerId, _broadcaster.TimerCancelledEvents[0].PlayerId);
    }

    [Fact]
    public async Task CancelTimerAsync_WithNoTimer_ReturnsZero()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        // Act
        var result = await _service.CancelTimerAsync(tableId, playerId);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region GetTimerState Tests

    [Fact]
    public async Task GetTimerState_WithActiveTimer_ReturnsState()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var handId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        await _service.StartTimerAsync(
            tableId, handId, playerId,
            actionSeconds: 30,
            timeBankEnabled: true,
            timeBankSeconds: 60);

        // Act
        var state = _service.GetTimerState(tableId);

        // Assert
        Assert.NotNull(state);
        Assert.Equal(tableId, state.TableId);
        Assert.Equal(handId, state.HandId);
        Assert.Equal(playerId, state.ActivePlayerId);
        Assert.Equal(30, state.RemainingSeconds);
        Assert.True(state.HasTimeBank);
        Assert.Equal(60, state.TimeBankRemainingSeconds);
    }

    [Fact]
    public void GetTimerState_WithNoTimer_ReturnsNull()
    {
        // Arrange
        var tableId = Guid.NewGuid();

        // Act
        var state = _service.GetTimerState(tableId);

        // Assert
        Assert.Null(state);
    }

    #endregion

    #region PauseTimerAsync / ResumeTimerAsync Tests

    [Fact]
    public async Task PauseTimerAsync_WithActiveTimer_SetsIsPausedFalse()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var handId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        await _service.StartTimerAsync(
            tableId, handId, playerId,
            actionSeconds: 30,
            timeBankEnabled: true,
            timeBankSeconds: 60);

        // Act
        await _service.PauseTimerAsync(tableId);

        // Assert
        Assert.False(_service.IsTimerActive(tableId));
    }

    [Fact]
    public async Task ResumeTimerAsync_AfterPause_ResumesTimer()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var handId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        await _service.StartTimerAsync(
            tableId, handId, playerId,
            actionSeconds: 30,
            timeBankEnabled: true,
            timeBankSeconds: 60);

        await _service.PauseTimerAsync(tableId);

        // Act
        await _service.ResumeTimerAsync(tableId);

        // Assert
        Assert.True(_service.IsTimerActive(tableId));
    }

    #endregion

    #region StopAllTimersAsync Tests

    [Fact]
    public async Task StopAllTimersAsync_WithActiveTimer_StopsTimer()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var handId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        await _service.StartTimerAsync(
            tableId, handId, playerId,
            actionSeconds: 30,
            timeBankEnabled: true,
            timeBankSeconds: 60);

        // Act
        await _service.StopAllTimersAsync(tableId);

        // Assert
        Assert.False(_service.IsTimerActive(tableId));
        Assert.Null(_service.GetTimerState(tableId));
    }

    #endregion

    #region ActionTimerState Tests

    [Fact]
    public void ActionTimerState_Create_InitializesCorrectly()
    {
        // Act
        var state = ActionTimerState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actionSeconds: 30,
            hasTimeBank: true,
            timeBankSeconds: 60);

        // Assert
        Assert.Equal(30, state.RemainingSeconds);
        Assert.Equal(30, state.TotalActionSeconds);
        Assert.True(state.HasTimeBank);
        Assert.False(state.IsTimeBankActive);
        Assert.Equal(60, state.TimeBankRemainingSeconds);
        Assert.Equal(60, state.OriginalTimeBankSeconds);
        Assert.False(state.WarningSent);
        Assert.False(state.TimeBankActivationBroadcast);
    }

    [Fact]
    public void ActionTimerState_Tick_DecrementsTime()
    {
        // Arrange
        var state = ActionTimerState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actionSeconds: 30,
            hasTimeBank: false,
            timeBankSeconds: 0);

        // Act
        var newState = state.Tick();

        // Assert
        Assert.Equal(29, newState.RemainingSeconds);
    }

    [Fact]
    public void ActionTimerState_Tick_WhenMainTimerExpires_ActivatesTimeBank()
    {
        // Arrange
        var state = ActionTimerState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actionSeconds: 1,
            hasTimeBank: true,
            timeBankSeconds: 30);

        // Act - tick when 1 second remaining
        var newState = state.Tick();

        // Assert
        Assert.Equal(0, newState.RemainingSeconds);
        Assert.True(newState.IsTimeBankActive);
        Assert.True(newState.NeedsTimeBankActivationBroadcast);
    }

    [Fact]
    public void ActionTimerState_Tick_WhenTimeBankActive_DecrementsTimeBank()
    {
        // Arrange
        var state = ActionTimerState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actionSeconds: 1,
            hasTimeBank: true,
            timeBankSeconds: 30);

        var stateWithTimeBank = state.Tick(); // Activates time bank
        stateWithTimeBank = stateWithTimeBank.WithTimeBankActivationBroadcast();

        // Act
        var newState = stateWithTimeBank.Tick();

        // Assert
        Assert.Equal(29, newState.TimeBankRemainingSeconds);
    }

    [Fact]
    public void ActionTimerState_Tick_WhenTimeBankAtZero_DoesNotGoNegative()
    {
        // Arrange
        var state = ActionTimerState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actionSeconds: 1,
            hasTimeBank: true,
            timeBankSeconds: 1);

        var stateWithTimeBank = state.Tick(); // Activates time bank
        var stateTimeBankExpiring = stateWithTimeBank.Tick(); // Time bank = 0

        // Act
        var newState = stateTimeBankExpiring.Tick();

        // Assert
        Assert.Equal(0, newState.TimeBankRemainingSeconds);
    }

    [Fact]
    public void ActionTimerState_IsExpired_WhenNoTimeBank_TrueWhenZero()
    {
        // Arrange
        var state = ActionTimerState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actionSeconds: 1,
            hasTimeBank: false,
            timeBankSeconds: 0);

        // Act
        var newState = state.Tick();

        // Assert
        Assert.True(newState.IsExpired);
    }

    [Fact]
    public void ActionTimerState_IsExpired_WhenTimeBankActive_FalseWhileTimeBankRemains()
    {
        // Arrange
        var state = ActionTimerState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actionSeconds: 1,
            hasTimeBank: true,
            timeBankSeconds: 30);

        var newState = state.Tick(); // Activates time bank

        // Assert
        Assert.False(newState.IsExpired);
    }

    [Fact]
    public void ActionTimerState_TimeBankSecondsUsed_WhenNotActive_ReturnsZero()
    {
        // Arrange
        var state = ActionTimerState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actionSeconds: 30,
            hasTimeBank: true,
            timeBankSeconds: 60);

        // Assert
        Assert.Equal(0, state.TimeBankSecondsUsed);
    }

    [Fact]
    public void ActionTimerState_TimeBankSecondsUsed_WhenActive_ReturnsUsedAmount()
    {
        // Arrange
        var state = ActionTimerState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actionSeconds: 1,
            hasTimeBank: true,
            timeBankSeconds: 30);

        var stateWithTimeBank = state.Tick()
            .WithTimeBankActivationBroadcast()
            .Tick() // 29
            .Tick() // 28
            .Tick(); // 27

        // Assert - Used 3 seconds
        Assert.Equal(3, stateWithTimeBank.TimeBankSecondsUsed);
    }

    [Fact]
    public void ActionTimerState_EffectiveRemainingSeconds_ReturnsCorrectValue()
    {
        // Main timer active
        var state1 = ActionTimerState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actionSeconds: 30,
            hasTimeBank: true,
            timeBankSeconds: 60);

        Assert.Equal(30, state1.EffectiveRemainingSeconds);

        // Time bank active
        var state2 = ActionTimerState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actionSeconds: 1,
            hasTimeBank: true,
            timeBankSeconds: 60);

        var stateWithTimeBank = state2.Tick();
        Assert.Equal(60, stateWithTimeBank.EffectiveRemainingSeconds);
    }

    #endregion

    #region Mock Implementations

    private class MockActionTimerBroadcaster : IActionTimerBroadcaster
    {
        public List<(Guid TableId, Guid PlayerId, int RemainingSeconds, bool IsTimeBankActive, int TimeBankRemaining)> TickEvents { get; } = new();
        public List<(Guid TableId, Guid PlayerId, int RemainingSeconds)> WarningEvents { get; } = new();
        public List<(Guid TableId, Guid PlayerId, int TotalSeconds, int TimeBankAvailable)> TimerStartedEvents { get; } = new();
        public List<(Guid TableId, Guid PlayerId)> TimerCancelledEvents { get; } = new();
        public List<(Guid TableId, Guid PlayerId, int TimeBankSecondsAdded, int TimeBankRemaining)> TimeBankActivatedEvents { get; } = new();
        public List<(Guid TableId, Guid PlayerId)> TimerExpiredEvents { get; } = new();

        public Task BroadcastTimerTickAsync(Guid tableId, Guid playerId, int remainingSeconds, bool isTimeBankActive, int timeBankRemaining, CancellationToken ct = default)
        {
            TickEvents.Add((tableId, playerId, remainingSeconds, isTimeBankActive, timeBankRemaining));
            return Task.CompletedTask;
        }

        public Task BroadcastTimerWarningAsync(Guid tableId, Guid playerId, int remainingSeconds, CancellationToken ct = default)
        {
            WarningEvents.Add((tableId, playerId, remainingSeconds));
            return Task.CompletedTask;
        }

        public Task BroadcastTimerStartedAsync(Guid tableId, Guid playerId, int totalSeconds, int timeBankAvailable, CancellationToken ct = default)
        {
            TimerStartedEvents.Add((tableId, playerId, totalSeconds, timeBankAvailable));
            return Task.CompletedTask;
        }

        public Task BroadcastTimerCancelledAsync(Guid tableId, Guid playerId, CancellationToken ct = default)
        {
            TimerCancelledEvents.Add((tableId, playerId));
            return Task.CompletedTask;
        }

        public Task BroadcastTimeBankActivatedAsync(Guid tableId, Guid playerId, int timeBankSecondsAdded, int timeBankRemaining, CancellationToken ct = default)
        {
            TimeBankActivatedEvents.Add((tableId, playerId, timeBankSecondsAdded, timeBankRemaining));
            return Task.CompletedTask;
        }

        public Task BroadcastTimerExpiredAsync(Guid tableId, Guid playerId, CancellationToken ct = default)
        {
            TimerExpiredEvents.Add((tableId, playerId));
            return Task.CompletedTask;
        }
    }

    private class MockGameOrchestrator : IGameOrchestrator
    {
        public List<(Table Table, int TimeBankConsumed)> ForceTimeoutFoldCalls { get; } = new();

        public Task<HandStartResult> StartNewHandAsync(Table table, CancellationToken ct = default)
            => Task.FromResult(HandStartResult.Failure("Not implemented"));

        public Task<HandStartResult> StartBombPotAsync(Table table, decimal anteAmount, bool isDoubleBoard = false, CancellationToken ct = default)
            => Task.FromResult(HandStartResult.Failure("Not implemented"));

        public Task<ActionResult> ExecutePlayerActionAsync(Table table, Guid playerId, LowRollers.Api.Domain.Betting.PlayerActionType actionType, decimal amount = 0, CancellationToken ct = default)
            => Task.FromResult(ActionResult.Failure("Not implemented"));

        public LowRollers.Api.Domain.Betting.AvailableActions? GetAvailableActions(Table table) => null;

        public Task<ActionResult> ForceTimeoutFoldAsync(Table table, int timeBankConsumed = 0, CancellationToken ct = default)
        {
            ForceTimeoutFoldCalls.Add((table, timeBankConsumed));
            return Task.FromResult(ActionResult.Success(table.CurrentHand!, null));
        }

        public LowRollers.Api.Domain.Betting.BettingRound? GetBettingRound(Guid handId) => null;

        public Task<LowRollers.Api.Features.GameEngine.Showdown.ShowdownResult> ExecuteShowdownAsync(Table table, CancellationToken ct = default)
            => Task.FromResult(LowRollers.Api.Features.GameEngine.Showdown.ShowdownResult.Failure("Not implemented"));

        public Task<bool> RequestShowdownMuckAsync(Table table, Guid playerId, CancellationToken ct = default)
            => Task.FromResult(false);
    }

    #endregion
}
