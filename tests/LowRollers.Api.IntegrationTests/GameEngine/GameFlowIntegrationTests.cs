using System.Diagnostics;
using LowRollers.Api.Domain.Betting;
using LowRollers.Api.Domain.Evaluation;
using LowRollers.Api.Domain.Events;
using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.Pots;
using LowRollers.Api.Domain.Services;
using LowRollers.Api.Domain.StateMachine;
using LowRollers.Api.Domain.StateMachine.Handlers;
using LowRollers.Api.Features.GameEngine;
using LowRollers.Api.Features.GameEngine.Showdown;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace LowRollers.Api.IntegrationTests.GameEngine;

/// <summary>
/// Shared fixture for game engine tests. Instantiated once per test class.
/// </summary>
public class GameEngineFixture
{
    public GameOrchestrator Orchestrator { get; }
    public InMemoryHandEventStore EventStore { get; }
    public IPotManager PotManager { get; }

    public GameEngineFixture()
    {
        var shuffleService = new ShuffleService();
        PotManager = new PotManager();
        EventStore = new InMemoryHandEventStore();

        var handlers = new IHandPhaseHandler[]
        {
            new WaitingPhaseHandler(NullLogger<WaitingPhaseHandler>.Instance),
            new PreflopPhaseHandler(NullLogger<PreflopPhaseHandler>.Instance),
            new FlopPhaseHandler(NullLogger<FlopPhaseHandler>.Instance),
            new TurnPhaseHandler(NullLogger<TurnPhaseHandler>.Instance),
            new RiverPhaseHandler(NullLogger<RiverPhaseHandler>.Instance),
            new ShowdownPhaseHandler(NullLogger<ShowdownPhaseHandler>.Instance),
            new CompletePhaseHandler(NullLogger<CompletePhaseHandler>.Instance)
        };

        var stateMachine = new HandStateMachine(handlers, NullLogger<HandStateMachine>.Instance);

        var showdownHandler = new ShowdownHandler(
            new HandEvaluationService(),
            PotManager,
            EventStore,
            NullLogger<ShowdownHandler>.Instance);

        Orchestrator = new GameOrchestrator(
            shuffleService,
            PotManager,
            EventStore,
            stateMachine,
            showdownHandler,
            NullLogger<GameOrchestrator>.Instance);
    }
}

/// <summary>
/// Integration tests for complete poker game flow scenarios.
/// These tests verify end-to-end behavior from hand start through completion.
/// </summary>
public class GameFlowIntegrationTests : IClassFixture<GameEngineFixture>
{
    private readonly GameOrchestrator _orchestrator;
    private readonly InMemoryHandEventStore _eventStore;
    private readonly IPotManager _potManager;
    private readonly ITestOutputHelper _output;

    public GameFlowIntegrationTests(GameEngineFixture fixture, ITestOutputHelper output)
    {
        _orchestrator = fixture.Orchestrator;
        _eventStore = fixture.EventStore;
        _potManager = fixture.PotManager;
        _output = output;

        // Clear state from previous tests
        _eventStore.Clear();
    }

    #region Test Helpers

    private static Table CreateTestTable(int playerCount, decimal smallBlind = 1m, decimal bigBlind = 2m)
    {
        var table = new Table
        {
            Id = Guid.NewGuid(),
            Name = "Test Table",
            SmallBlind = smallBlind,
            BigBlind = bigBlind,
            ButtonPosition = 1,
            ActionTimerSeconds = 30,
            TimeBankEnabled = true,
            InitialTimeBankSeconds = 60
        };

        for (int i = 0; i < playerCount; i++)
        {
            var player = Player.Create(
                Guid.NewGuid(),
                $"Player{i + 1}",
                seatPosition: i + 1,
                buyInAmount: 100m);
            player.TimeBankSeconds = 60;
            table.Players[player.Id] = player;
        }

        return table;
    }

    private static Table CreateTableWithVariableStacks(params (string name, int seat, decimal stack)[] playerConfigs)
    {
        var table = new Table
        {
            Id = Guid.NewGuid(),
            Name = "Test Table",
            SmallBlind = 1m,
            BigBlind = 2m,
            ButtonPosition = 1,
            ActionTimerSeconds = 30,
            TimeBankEnabled = true,
            InitialTimeBankSeconds = 60
        };

        foreach (var (name, seat, stack) in playerConfigs)
        {
            var player = Player.Create(
                Guid.NewGuid(),
                name,
                seatPosition: seat,
                buyInAmount: stack);
            player.TimeBankSeconds = 60;
            table.Players[player.Id] = player;
        }

        return table;
    }

    /// <summary>
    /// Plays a hand to showdown by checking and calling when necessary.
    /// </summary>
    private async Task PlayToShowdownAsync(Table table)
    {
        var hand = table.CurrentHand!;

        // Play through all betting rounds with checks and calls
        while (hand.Phase != HandPhase.Showdown && hand.CurrentPlayerId.HasValue)
        {
            var playerId = hand.CurrentPlayerId.Value;
            var actions = _orchestrator.GetAvailableActions(table);

            if (actions == null) break;

            // Choose check if available, otherwise call
            PlayerActionType action;
            if (actions.CanCheck)
            {
                action = PlayerActionType.Check;
            }
            else if (actions.CanCall)
            {
                action = PlayerActionType.Call;
            }
            else
            {
                break;
            }

            var result = await _orchestrator.ExecutePlayerActionAsync(table, playerId, action);
            if (!result.IsSuccess || result.HandComplete) break;
        }
    }

    /// <summary>
    /// Plays to a specific phase by checking and calling.
    /// </summary>
    private async Task PlayToPhaseAsync(Table table, HandPhase targetPhase)
    {
        var hand = table.CurrentHand!;

        while (hand.Phase != targetPhase && hand.CurrentPlayerId.HasValue)
        {
            var playerId = hand.CurrentPlayerId.Value;
            var actions = _orchestrator.GetAvailableActions(table);

            if (actions == null) break;

            PlayerActionType action;
            if (actions.CanCheck)
            {
                action = PlayerActionType.Check;
            }
            else if (actions.CanCall)
            {
                action = PlayerActionType.Call;
            }
            else
            {
                break;
            }

            var result = await _orchestrator.ExecutePlayerActionAsync(table, playerId, action);
            if (!result.IsSuccess || result.HandComplete) break;
        }
    }

    /// <summary>
    /// Plays a complete betting round where all players check.
    /// </summary>
    private async Task PlayCheckRoundAsync(Table table, int expectedPlayers)
    {
        var hand = table.CurrentHand!;
        for (int i = 0; i < expectedPlayers; i++)
        {
            if (hand.CurrentPlayerId == null) break;
            var actions = _orchestrator.GetAvailableActions(table);
            if (actions?.CanCheck != true) break;
            await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId.Value, PlayerActionType.Check);
        }
    }

    /// <summary>
    /// Completes a hand quickly by having all but one player fold.
    /// </summary>
    private async Task CompleteHandQuicklyAsync(Table table)
    {
        var hand = table.CurrentHand!;
        var playersInHand = table.Players.Values.Count(p => p.IsInHand);

        // Fold everyone except the last player
        for (int i = 0; i < playersInHand - 1; i++)
        {
            if (hand.CurrentPlayerId == null || table.CurrentHand == null) break;
            var result = await _orchestrator.ExecutePlayerActionAsync(
                table, hand.CurrentPlayerId.Value, PlayerActionType.Fold);
            if (result.HandComplete) break;
        }
    }

    /// <summary>
    /// Resets all players with chips to Waiting status for a new hand.
    /// </summary>
    private static void ResetPlayersForNewHand(Table table)
    {
        foreach (var player in table.Players.Values)
        {
            if (player.ChipStack > 0)
                player.Status = PlayerStatus.Waiting;
        }
    }

    #endregion

    #region Complete Hand Flow Tests (Deal → Betting → Showdown)

    [Fact]
    public async Task CompleteHand_DealThroughShowdown_EvaluatesHandsAndDistributesPot()
    {
        var sw = Stopwatch.StartNew();

        // Arrange
        var table = CreateTestTable(playerCount: 3, smallBlind: 1m, bigBlind: 2m);
        var startResult = await _orchestrator.StartNewHandAsync(table);

        Assert.True(startResult.IsSuccess);
        var hand = table.CurrentHand!;

        // Act - Play through all betting rounds with calls/checks
        await PlayToShowdownAsync(table);

        // Execute showdown
        Assert.Equal(HandPhase.Showdown, hand.Phase);
        Assert.Equal(5, hand.CommunityCards.Count);

        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);

        // Assert
        Assert.True(showdownResult.IsSuccess);
        Assert.NotNull(showdownResult.PlayerResults);
        Assert.NotEmpty(showdownResult.PlayerResults);
        Assert.NotNull(showdownResult.PotAwards);
        Assert.NotEmpty(showdownResult.PotAwards);
        Assert.NotNull(showdownResult.TotalWinnings);
        Assert.NotEmpty(showdownResult.TotalWinnings);

        // Verify pot was distributed correctly
        // With 3 players, SB=1, BB=2, everyone calls/checks to showdown:
        // Preflop: SB(1) + BB(2) + BTN calls(2) + SB completes(1) = 6
        // Flop/Turn/River: all check (no additional bets)
        // Total pot = 6m
        var totalWon = showdownResult.TotalWinnings.Values.Sum();
        Assert.Equal(6m, totalWon);

        // Verify hand is complete
        Assert.Null(table.CurrentHand);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task CompleteHand_WithRaises_CorrectlyBuildsPotAndAdvancesPhases()
    {
        var sw = Stopwatch.StartNew();

        // Arrange - Use 4 players to have UTG, Button, SB, and BB
        var table = CreateTestTable(playerCount: 4, smallBlind: 1m, bigBlind: 2m);
        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);
        var hand = table.CurrentHand!;

        // With 4 players, button rotates to seat 2, SB at seat 3, BB at seat 4, UTG at seat 1
        // Preflop action order: UTG (1) → Button (2) → SB (3) → BB (4)

        // Act - Preflop: UTG raises, others call
        var utg = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, utg, PlayerActionType.Raise, 6m);

        var btn = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, btn, PlayerActionType.Call);

        var sb = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, sb, PlayerActionType.Call);

        var bb = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, bb, PlayerActionType.Call);

        Assert.Equal(HandPhase.Flop, hand.Phase);
        Assert.Equal(3, hand.CommunityCards.Count);

        // Flop: check around (4 players)
        await PlayCheckRoundAsync(table, 4);
        Assert.Equal(HandPhase.Turn, hand.Phase);
        Assert.Equal(4, hand.CommunityCards.Count);

        // Turn: check around (4 players)
        await PlayCheckRoundAsync(table, 4);
        Assert.Equal(HandPhase.River, hand.Phase);
        Assert.Equal(5, hand.CommunityCards.Count);

        // River: check around (4 players)
        await PlayCheckRoundAsync(table, 4);

        // Assert - Should be at showdown
        Assert.Equal(HandPhase.Showdown, hand.Phase);

        // Execute showdown
        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);
        Assert.True(showdownResult.IsSuccess);

        // Pot should be 6 * 4 = 24 (all 4 players put in 6)
        var totalWon = showdownResult.TotalWinnings.Values.Sum();
        Assert.Equal(24m, totalWon);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task CompleteHand_HandEventsAreRecorded()
    {
        var sw = Stopwatch.StartNew();

        // Arrange
        var table = CreateTestTable(playerCount: 2);
        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);
        var handId = startResult.Hand!.Id;

        // Act - Complete the hand
        await PlayToShowdownAsync(table);

        // Verify we reached showdown
        Assert.Equal(HandPhase.Showdown, table.CurrentHand!.Phase);

        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);
        Assert.True(showdownResult.IsSuccess);

        // Assert - Verify events were recorded
        var events = await _eventStore.GetEventsAsync(handId).ToListAsync();

        Assert.NotEmpty(events);
        Assert.Contains(events, e => e is HandStartedEvent);
        Assert.Contains(events, e => e is BlindsPostedEvent);
        Assert.Contains(events, e => e is HoleCardsDealtEvent);
        Assert.Contains(events, e => e is CommunityCardsDealtEvent);
        Assert.Contains(events, e => e is PlayerShowedCardsEvent || e is PlayerMuckedCardsEvent);
        Assert.Contains(events, e => e is PotAwardedEvent);
        Assert.Contains(events, e => e is HandCompletedEvent);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms. Events recorded: {events.Count}");
    }

    [Fact]
    public async Task CompleteHand_ThreePlayersCheckDown_CorrectPotSize()
    {
        var sw = Stopwatch.StartNew();

        // Arrange - 3 players
        var table = CreateTestTable(playerCount: 3, smallBlind: 1m, bigBlind: 2m);
        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);
        var hand = table.CurrentHand!;

        // With 3 players: Button at seat 2, SB at seat 3, BB at seat 1
        // Preflop order: Button (2) → SB (3) → BB (1)

        // Act - Preflop: Button calls, SB completes, BB checks
        var r1 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Call);
        Assert.True(r1.IsSuccess);

        var r2 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Call);
        Assert.True(r2.IsSuccess);

        var r3 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);
        Assert.True(r3.IsSuccess);

        Assert.Equal(HandPhase.Flop, hand.Phase);

        // Flop: all check (3 players)
        await PlayCheckRoundAsync(table, 3);
        Assert.Equal(HandPhase.Turn, hand.Phase);

        // Turn: all check (3 players)
        await PlayCheckRoundAsync(table, 3);
        Assert.Equal(HandPhase.River, hand.Phase);

        // River: all check (3 players)
        await PlayCheckRoundAsync(table, 3);
        Assert.Equal(HandPhase.Showdown, hand.Phase);

        // Execute showdown
        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);
        Assert.True(showdownResult.IsSuccess);

        // Pot should be 2 * 3 = 6 (all 3 players put in 2 BB)
        var totalWon = showdownResult.TotalWinnings.Values.Sum();
        Assert.Equal(6m, totalWon);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
    }

    #endregion

    #region All Players Fold Tests

    [Fact]
    public async Task AllPlayersFold_LastPlayerRemainingWinsPot()
    {
        var sw = Stopwatch.StartNew();

        // Arrange
        var table = CreateTestTable(playerCount: 4, smallBlind: 1m, bigBlind: 2m);
        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);
        var hand = table.CurrentHand!;
        var initialPot = 3m; // SB + BB

        // Capture the BB player - they should be the winner when everyone else folds
        var bbPlayer = table.Players.Values.First(p => p.SeatPosition == hand.BigBlindPosition);

        // Act - First three players fold (UTG, BTN, SB)
        for (int i = 0; i < 3; i++)
        {
            var playerId = hand.CurrentPlayerId!.Value;
            var result = await _orchestrator.ExecutePlayerActionAsync(table, playerId, PlayerActionType.Fold);

            if (i < 2)
            {
                Assert.True(result.IsSuccess);
                Assert.False(result.HandComplete);
            }
            else
            {
                // Last fold completes the hand
                Assert.True(result.IsSuccess);
                Assert.True(result.HandComplete);
                Assert.NotNull(result.Winnings);
                Assert.Single(result.Winnings);

                // Winner should be the BB
                var (winnerId, amount) = result.Winnings.First();
                Assert.Equal(bbPlayer.Id, winnerId);
                Assert.Equal(initialPot, amount);

                // BB's stack should be starting stack minus BB posted plus pot won
                // 100 - 2 + 3 = 101
                Assert.Equal(101m, bbPlayer.ChipStack);
            }
        }

        // Assert - Hand should be complete
        Assert.Null(table.CurrentHand);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task AllPlayersFold_AfterRaise_PotIncludesRaise()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3, smallBlind: 1m, bigBlind: 2m);
        var startResult = await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Act - First player raises to 6, second folds, third folds
        var player1 = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, player1, PlayerActionType.Raise, 6m);

        var player2 = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, player2, PlayerActionType.Fold);

        var player3 = hand.CurrentPlayerId!.Value;
        var result = await _orchestrator.ExecutePlayerActionAsync(table, player3, PlayerActionType.Fold);

        // Assert - Raiser wins pot including blinds
        Assert.True(result.IsSuccess);
        Assert.True(result.HandComplete);
        Assert.NotNull(result.Winnings);
        Assert.Single(result.Winnings);

        // Winner is the raiser (player1)
        Assert.Equal(player1, result.Winnings.Keys.First());

        // Pot contains: SB (1) + BB (2) + raiser's bet (6) = 9
        Assert.Equal(9m, result.Winnings[player1]);
    }

    [Fact]
    public async Task AllPlayersFold_ExceptBigBlind_BBWinsUncontested()
    {
        // Arrange - 3 players
        var table = CreateTestTable(playerCount: 3, smallBlind: 1m, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // With 3 players after button rotation:
        // Button at seat 2, SB at seat 3, BB at seat 1
        var bbPlayer = table.Players.Values.First(p => p.SeatPosition == hand.BigBlindPosition);

        // Act - First player (Button) folds, SB folds
        var button = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, button, PlayerActionType.Fold);

        var sb = hand.CurrentPlayerId!.Value;
        var result = await _orchestrator.ExecutePlayerActionAsync(table, sb, PlayerActionType.Fold);

        // Assert - BB wins
        Assert.True(result.IsSuccess);
        Assert.True(result.HandComplete);
        Assert.NotNull(result.Winnings);
        Assert.Single(result.Winnings);
        Assert.True(result.Winnings.ContainsKey(bbPlayer.Id));

        // BB wins SB contribution + their own BB = 1 + 2 = 3
        Assert.Equal(3m, result.Winnings[bbPlayer.Id]);
    }

    #endregion

    #region Heads-Up Blind Structure Tests

    [Fact]
    public async Task HeadsUp_ButtonPostsSmallBlind()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);
        table.ButtonPosition = 1;

        // Act
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Assert - In heads-up, button (seat 2 after rotation) is SB, other player is BB
        // Button rotates from 1 to 2
        Assert.Equal(2, table.ButtonPosition);
        Assert.Equal(2, hand.SmallBlindPosition); // Button is SB
        Assert.Equal(1, hand.BigBlindPosition);   // Non-button is BB

        // Button/SB acts first pre-flop in heads-up
        var firstToAct = table.Players[hand.CurrentPlayerId!.Value];
        Assert.Equal(2, firstToAct.SeatPosition); // Button/SB
    }

    [Fact]
    public async Task HeadsUp_PostFlop_BigBlindActsFirst()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Complete preflop - SB calls, BB checks
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Call);
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);

        Assert.Equal(HandPhase.Flop, hand.Phase);

        // Assert - Post-flop, BB (first to left of button) acts first
        var firstToActFlop = table.Players[hand.CurrentPlayerId!.Value];
        Assert.Equal(hand.BigBlindPosition, firstToActFlop.SeatPosition);
    }

    [Fact]
    public async Task HeadsUp_BlindAmountsCorrect()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2, smallBlind: 5m, bigBlind: 10m);

        // Act
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Assert - Check blind amounts were deducted correctly
        var sbPlayer = table.Players.Values.First(p => p.SeatPosition == hand.SmallBlindPosition);
        var bbPlayer = table.Players.Values.First(p => p.SeatPosition == hand.BigBlindPosition);

        Assert.Equal(95m, sbPlayer.ChipStack); // 100 - 5
        Assert.Equal(90m, bbPlayer.ChipStack); // 100 - 10
        Assert.Equal(15m, hand.TotalPot);       // 5 + 10
    }

    #endregion

    #region All-In Tests (Various Streets)

    [Fact]
    public async Task HeadsUpAllInPreflop_RunsOutBoardAndShowsdown()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2, smallBlind: 1m, bigBlind: 2m);
        var startResult = await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Act - First player (SB/Button in heads-up) goes all-in
        var sbButton = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, sbButton, PlayerActionType.AllIn);

        // BB calls all-in
        var bb = hand.CurrentPlayerId!.Value;
        var callResult = await _orchestrator.ExecutePlayerActionAsync(table, bb, PlayerActionType.AllIn);

        // Assert - Board is run out
        Assert.True(callResult.BettingRoundComplete);
        Assert.Equal(5, hand.CommunityCards.Count);
        Assert.Equal(HandPhase.Showdown, hand.Phase);

        // Execute showdown
        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);

        // Assert - Showdown completed successfully
        Assert.True(showdownResult.IsSuccess);
        Assert.NotEmpty(showdownResult.TotalWinnings);

        // Total pot should be both stacks (200 chips total)
        var totalWon = showdownResult.TotalWinnings.Values.Sum();
        Assert.Equal(200m, totalWon);

        // Verify hand completed
        Assert.Null(table.CurrentHand);
    }

    [Fact]
    public async Task AllInOnFlop_RunsOutTurnAndRiver()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Play to flop
        await PlayToPhaseAsync(table, HandPhase.Flop);
        Assert.Equal(HandPhase.Flop, hand.Phase);
        Assert.Equal(3, hand.CommunityCards.Count);

        // Act - Both go all-in on the flop
        var player1 = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, player1, PlayerActionType.AllIn);

        var player2 = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, player2, PlayerActionType.AllIn);

        // Assert - Turn and river are dealt
        Assert.Equal(5, hand.CommunityCards.Count);
        Assert.Equal(HandPhase.Showdown, hand.Phase);

        // Execute showdown
        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);
        Assert.True(showdownResult.IsSuccess);
        Assert.Equal(200m, showdownResult.TotalWinnings.Values.Sum());
    }

    [Fact]
    public async Task AllInOnTurn_RunsOutRiver_GoesToShowdown()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Play to turn
        await PlayToPhaseAsync(table, HandPhase.Turn);
        Assert.Equal(HandPhase.Turn, hand.Phase);
        Assert.Equal(4, hand.CommunityCards.Count);

        // Act - Both go all-in on the turn
        var player1 = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, player1, PlayerActionType.AllIn);

        var player2 = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, player2, PlayerActionType.AllIn);

        // Assert - River is dealt (5 cards total)
        Assert.Equal(5, hand.CommunityCards.Count);
        Assert.Equal(HandPhase.Showdown, hand.Phase);

        // Execute showdown
        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);
        Assert.True(showdownResult.IsSuccess);
        Assert.Equal(200m, showdownResult.TotalWinnings.Values.Sum());
    }

    [Fact]
    public async Task AllInOnRiver_NoMoreCardsDealt_GoesToShowdown()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Play to river
        await PlayToPhaseAsync(table, HandPhase.River);
        Assert.Equal(HandPhase.River, hand.Phase);
        Assert.Equal(5, hand.CommunityCards.Count);

        // Act - Both go all-in on the river
        var player1 = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, player1, PlayerActionType.AllIn);

        var player2 = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, player2, PlayerActionType.AllIn);

        // Assert - Still 5 cards (river was already dealt), goes to showdown
        Assert.Equal(5, hand.CommunityCards.Count);
        Assert.Equal(HandPhase.Showdown, hand.Phase);

        // Execute showdown
        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);
        Assert.True(showdownResult.IsSuccess);
    }

    [Fact]
    public async Task HeadsUpAllInPreflop_ShorterStackAllIn_CorrectPotSize()
    {
        // Arrange - One player has less chips
        var table = CreateTableWithVariableStacks(
            ("Short", 1, 50m),   // Short stack at seat 1
            ("Deep", 2, 150m)    // Deep stack at seat 2
        );
        table.ButtonPosition = 1;
        table.SmallBlind = 1m;
        table.BigBlind = 2m;

        var startResult = await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // In heads-up, button is SB. Button rotates to seat 2 (Deep), so:
        // Deep is SB/Button, Short is BB
        // Deep (SB) acts first preflop

        // Act - Both go all-in
        var firstToAct = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, firstToAct, PlayerActionType.AllIn);

        var second = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, second, PlayerActionType.AllIn);

        // Assert - Board runs out
        Assert.Equal(5, hand.CommunityCards.Count);
        Assert.Equal(HandPhase.Showdown, hand.Phase);

        // Execute showdown
        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);

        Assert.True(showdownResult.IsSuccess);

        // Total pot is based on effective stacks (50 + 50 = 100)
        var totalWon = showdownResult.TotalWinnings.Values.Sum();
        Assert.Equal(100m, totalWon);
    }

    [Fact]
    public async Task HeadsUpAllInPreflop_AllCardsUnique()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Act - Both go all-in
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.AllIn);
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.AllIn);

        // Assert - All 5 community cards dealt and unique
        Assert.Equal(5, hand.CommunityCards.Count);
        var uniqueCards = hand.CommunityCards.Distinct().ToList();
        Assert.Equal(5, uniqueCards.Count);

        // Cards should not include any player's hole cards
        foreach (var player in table.Players.Values)
        {
            if (player.HoleCards != null)
            {
                foreach (var holeCard in player.HoleCards)
                {
                    Assert.DoesNotContain(holeCard, hand.CommunityCards);
                }
            }
        }
    }

    #endregion

    #region Multi-Way Pot with Side Pots Tests

    [Fact]
    public async Task MultiWaySidePots_ThreeAllInsAtDifferentAmounts_DistributesCorrectly()
    {
        var sw = Stopwatch.StartNew();

        // Arrange - Three players with different stacks
        var table = CreateTableWithVariableStacks(
            ("Short", 1, 30m),   // Shortest stack
            ("Medium", 2, 60m),  // Medium stack
            ("Deep", 3, 100m)    // Deepest stack
        );
        table.ButtonPosition = 3; // Deep is button
        table.SmallBlind = 1m;
        table.BigBlind = 2m;

        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);
        var hand = table.CurrentHand!;

        // Button at 1 (Short) after rotation, SB at 2 (Medium), BB at 3 (Deep)
        // Preflop action order: Short (Button/UTG) → Medium (SB) → Deep (BB)

        // Act - All three go all-in
        var player1 = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, player1, PlayerActionType.AllIn);

        var player2 = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, player2, PlayerActionType.AllIn);

        var player3 = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, player3, PlayerActionType.AllIn);

        // Assert - Board runs out
        Assert.Equal(5, hand.CommunityCards.Count);
        Assert.Equal(HandPhase.Showdown, hand.Phase);

        // Verify pot structure BEFORE showdown
        // Should have exactly 2 pots (no side pot for uncallable chips):
        // - Main pot: 30 * 3 = 90 (all 3 eligible)
        // - Side pot 1: 30 * 2 = 60 (Medium and Deep eligible)
        Assert.Equal(2, hand.Pots.Count);

        var mainPot = hand.Pots.First(p => p.Type == PotType.Main);
        Assert.Equal(90m, mainPot.Amount);
        Assert.Equal(3, mainPot.EligiblePlayerIds.Count);

        var sidePot = hand.Pots.First(p => p.Type == PotType.Side);
        Assert.Equal(60m, sidePot.Amount);
        Assert.Equal(2, sidePot.EligiblePlayerIds.Count);

        // Total pot should be 150 (no side pot 2 for uncallable 40)
        var totalPot = hand.Pots.Sum(p => p.Amount);
        Assert.Equal(150m, totalPot);

        // Execute showdown
        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);
        Assert.True(showdownResult.IsSuccess);

        // Verify pot awards exist
        Assert.NotEmpty(showdownResult.PotAwards);

        // Total winnings should equal total pot (150)
        // Deep's uncallable 40 is returned before showdown, not counted as "winnings"
        var totalWon = showdownResult.TotalWinnings.Values.Sum();
        Assert.Equal(150m, totalWon);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task MultiWaySidePots_ShortStackWins_OnlyGetsMainPot()
    {
        // Arrange - Setup with clear stack differences
        var table = CreateTableWithVariableStacks(
            ("Short", 1, 20m),   // Will be all-in for main pot only
            ("Medium", 2, 100m),
            ("Deep", 3, 100m)
        );
        table.ButtonPosition = 3;
        table.SmallBlind = 1m;
        table.BigBlind = 2m;

        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);
        var hand = table.CurrentHand!;

        var shortStackPlayer = table.Players.Values.First(p => p.DisplayName == "Short");

        // Act - Short goes all-in, others RAISE to create side pot
        var r1 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.AllIn); // Short: 20m
        Assert.True(r1.IsSuccess);

        var r2 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Raise, 50m); // Medium raises to 50m
        Assert.True(r2.IsSuccess);

        var r3 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Call); // Deep calls 50m
        Assert.True(r3.IsSuccess);

        // Now we have:
        // - Main pot: 60m (20 × 3) - all eligible
        // - Side pot: 60m (30 × 2) - only Medium and Deep eligible
        // - Total: 120m

        // Play through remaining streets (check down)
        while (hand.Phase != HandPhase.Showdown && hand.CurrentPlayerId.HasValue)
        {
            var actions = _orchestrator.GetAvailableActions(table);
            if (actions?.CanCheck == true)
            {
                await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId.Value, PlayerActionType.Check);
            }
            else
            {
                break;
            }
        }

        Assert.Equal(HandPhase.Showdown, hand.Phase);

        // Verify pot structure before showdown
        Assert.Equal(2, hand.Pots.Count);

        var mainPot = hand.Pots.First(p => p.Type == PotType.Main);
        Assert.Equal(60m, mainPot.Amount);
        Assert.Equal(3, mainPot.EligiblePlayerIds.Count);

        var sidePot = hand.Pots.First(p => p.Type == PotType.Side);
        Assert.Equal(60m, sidePot.Amount);
        Assert.Equal(2, sidePot.EligiblePlayerIds.Count);
        Assert.DoesNotContain(shortStackPlayer.Id, sidePot.EligiblePlayerIds);

        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);
        Assert.True(showdownResult.IsSuccess);

        // If Short won, they can ONLY win the main pot (60m max)
        if (showdownResult.TotalWinnings.ContainsKey(shortStackPlayer.Id))
        {
            Assert.Equal(60m, showdownResult.TotalWinnings[shortStackPlayer.Id]); // Exactly main pot
        }

        // Total distributed should equal total pot (120m)
        var totalWon = showdownResult.TotalWinnings.Values.Sum();
        Assert.Equal(120m, totalWon);
    }

    [Fact]
    public async Task MultiWaySidePots_TwoPlayersAllIn_ThirdActive_CorrectTotalPot()
    {
        // Arrange
        var table = CreateTableWithVariableStacks(
            ("AllIn1", 1, 40m),
            ("AllIn2", 2, 80m),
            ("Active", 3, 200m)
        );
        table.ButtonPosition = 3;
        table.SmallBlind = 1m;
        table.BigBlind = 2m;

        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);
        var hand = table.CurrentHand!;

        // Track player IDs
        var allIn1 = table.Players.Values.First(p => p.DisplayName == "AllIn1");

        // Act - First goes all-in (40 chips)
        var r1 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.AllIn);
        Assert.True(r1.IsSuccess);

        // Second goes all-in (80 chips)
        var r2 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.AllIn);
        Assert.True(r2.IsSuccess);

        // Third calls (matches 80)
        var r3 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Call);
        Assert.True(r3.IsSuccess);

        // Board is run out since two are all-in and third can't act alone
        Assert.Equal(5, hand.CommunityCards.Count);
        Assert.Equal(HandPhase.Showdown, hand.Phase);

        // Verify pot structure before showdown
        Assert.Equal(2, hand.Pots.Count);

        var mainPot = hand.Pots.First(p => p.Type == PotType.Main);
        Assert.Equal(120m, mainPot.Amount); // 40 × 3
        Assert.Equal(3, mainPot.EligiblePlayerIds.Count);

        var sidePot = hand.Pots.First(p => p.Type == PotType.Side);
        Assert.Equal(80m, sidePot.Amount); // 40 × 2
        Assert.Equal(2, sidePot.EligiblePlayerIds.Count);
        Assert.DoesNotContain(allIn1.Id, sidePot.EligiblePlayerIds);

        // Execute showdown
        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);

        Assert.True(showdownResult.IsSuccess);
        Assert.NotEmpty(showdownResult.PotAwards);

        // Total distributed should equal total contributed: 40 + 80 + 80 = 200
        var totalDistributed = showdownResult.TotalWinnings.Values.Sum();
        Assert.Equal(200m, totalDistributed);

        // Verify AllIn1 can only win exactly main pot (40 * 3 = 120)
        if (showdownResult.TotalWinnings.ContainsKey(allIn1.Id))
        {
            Assert.Equal(120m, showdownResult.TotalWinnings[allIn1.Id]);
        }
    }

    [Fact]
    public async Task MultiWaySidePots_VerifyPotStructure()
    {
        // Arrange - Setup for predictable pot structure
        var table = CreateTableWithVariableStacks(
            ("P1", 1, 25m),
            ("P2", 2, 50m),
            ("P3", 3, 100m)
        );
        table.ButtonPosition = 3;
        table.SmallBlind = 1m;
        table.BigBlind = 2m;

        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);
        var hand = table.CurrentHand!;

        var p1 = table.Players.Values.First(p => p.DisplayName == "P1");

        // Act - All go all-in
        var r1 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.AllIn);
        Assert.True(r1.IsSuccess);

        var r2 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.AllIn);
        Assert.True(r2.IsSuccess);

        var r3 = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.AllIn);
        Assert.True(r3.IsSuccess);

        // Assert - Verify pot structure before showdown
        Assert.Equal(HandPhase.Showdown, hand.Phase);
        Assert.Equal(2, hand.Pots.Count); // Exactly 2 pots

        var mainPot = hand.Pots.First(p => p.Type == PotType.Main);
        Assert.Equal(75m, mainPot.Amount); // 25 × 3
        Assert.Equal(3, mainPot.EligiblePlayerIds.Count);

        var sidePot = hand.Pots.First(p => p.Type == PotType.Side);
        Assert.Equal(50m, sidePot.Amount); // 25 × 2
        Assert.Equal(2, sidePot.EligiblePlayerIds.Count);
        Assert.DoesNotContain(p1.Id, sidePot.EligiblePlayerIds);

        // Total pot = 125m
        Assert.Equal(125m, hand.Pots.Sum(p => p.Amount));

        // Execute showdown
        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);
        Assert.True(showdownResult.IsSuccess);

        var totalWon = showdownResult.TotalWinnings.Values.Sum();
        Assert.Equal(125m, totalWon);
    }

    #endregion

    #region Timer Expiration Auto-Fold Tests

    [Fact]
    public async Task TimerExpiration_AutoFoldsPlayer()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);
        var hand = table.CurrentHand!;
        var currentPlayerId = hand.CurrentPlayerId!.Value;
        var currentPlayer = table.Players[currentPlayerId];

        // Act - Force timeout fold
        var result = await _orchestrator.ForceTimeoutFoldAsync(table);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.HandComplete); // Hand should continue
        Assert.Equal(PlayerStatus.Folded, currentPlayer.Status);
        Assert.NotEqual(currentPlayerId, hand.CurrentPlayerId);
        Assert.NotNull(hand.CurrentPlayerId); // Action moved to next player
    }

    [Fact]
    public async Task TimerExpiration_DeductsTimeBank()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;
        var currentPlayerId = hand.CurrentPlayerId!.Value;
        var currentPlayer = table.Players[currentPlayerId];
        var initialTimeBank = currentPlayer.TimeBankSeconds;

        // Act - Force timeout fold with time bank consumed
        var result = await _orchestrator.ForceTimeoutFoldAsync(table, timeBankConsumed: 30);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(initialTimeBank - 30, currentPlayer.TimeBankSeconds);
    }

    [Fact]
    public async Task TimerExpiration_AllButOneFold_AwardsPot()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;
        var currentPlayerId = hand.CurrentPlayerId!.Value;
        var otherPlayerId = table.Players.Values.First(p => p.Id != currentPlayerId).Id;

        // Act - Force timeout fold (folds the current player, other wins)
        var result = await _orchestrator.ForceTimeoutFoldAsync(table);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.HandComplete);
        Assert.NotNull(result.Winnings);
        Assert.Single(result.Winnings);
        Assert.True(result.Winnings.ContainsKey(otherPlayerId));
    }

    [Fact]
    public async Task TimerExpiration_ConsecutiveTimeouts_EventuallyCompletesHand()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 4);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Act - Multiple consecutive timeouts until hand completes
        ActionResult? lastResult = null;
        for (int i = 0; i < 4; i++)
        {
            if (table.CurrentHand == null) break;

            lastResult = await _orchestrator.ForceTimeoutFoldAsync(table);
            Assert.True(lastResult.IsSuccess);

            if (lastResult.HandComplete)
            {
                break;
            }
        }

        // Assert - Hand should eventually complete with one winner
        Assert.True(lastResult!.HandComplete);
        Assert.NotNull(lastResult.Winnings);
        Assert.Single(lastResult.Winnings);
    }

    [Fact]
    public async Task TimerExpiration_NoActiveHand_ReturnsFailure()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);
        // No hand started

        // Act
        var result = await _orchestrator.ForceTimeoutFoldAsync(table);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Error)); // Just verify error exists
    }

    [Fact]
    public async Task TimerExpiration_TimeBankDoesNotGoNegative()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);
        var hand = table.CurrentHand!;
        var currentPlayerId = hand.CurrentPlayerId!.Value;
        var currentPlayer = table.Players[currentPlayerId];
        currentPlayer.TimeBankSeconds = 10; // Set low time bank

        // Act - Try to consume more time bank than available
        var result = await _orchestrator.ForceTimeoutFoldAsync(table, timeBankConsumed: 50);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, currentPlayer.TimeBankSeconds); // Should not go negative
        Assert.Equal(PlayerStatus.Folded, currentPlayer.Status); // Fold still happened
    }

    #endregion

    #region Button Advancement Tests

    [Fact]
    public async Task ButtonAdvances_AfterEachHand_WrapsAroundTable()
    {
        var sw = Stopwatch.StartNew();

        // Arrange
        var table = CreateTestTable(playerCount: 3);
        Assert.Equal(1, table.ButtonPosition); // Initial position

        // Act & Assert - Hand 1: Button moves to seat 2
        await _orchestrator.StartNewHandAsync(table);
        await CompleteHandQuicklyAsync(table);
        Assert.Equal(2, table.ButtonPosition);

        // Hand 2: Button moves to seat 3
        ResetPlayersForNewHand(table);
        await _orchestrator.StartNewHandAsync(table);
        await CompleteHandQuicklyAsync(table);
        Assert.Equal(3, table.ButtonPosition);

        // Hand 3: Button wraps to seat 1
        ResetPlayersForNewHand(table);
        await _orchestrator.StartNewHandAsync(table);
        await CompleteHandQuicklyAsync(table);
        Assert.Equal(1, table.ButtonPosition);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ButtonAdvances_SkipsEmptySeats()
    {
        var sw = Stopwatch.StartNew();

        // Arrange - Players at seats 1, 3, 5 (gaps at 2, 4)
        var table = new Table
        {
            Id = Guid.NewGuid(),
            Name = "Test Table",
            SmallBlind = 1m,
            BigBlind = 2m,
            ButtonPosition = 1,
            ActionTimerSeconds = 30,
            TimeBankEnabled = true,
            InitialTimeBankSeconds = 60
        };

        var p1 = Player.Create(Guid.NewGuid(), "P1", seatPosition: 1, buyInAmount: 100m);
        p1.TimeBankSeconds = 60;
        var p3 = Player.Create(Guid.NewGuid(), "P3", seatPosition: 3, buyInAmount: 100m);
        p3.TimeBankSeconds = 60;
        var p5 = Player.Create(Guid.NewGuid(), "P5", seatPosition: 5, buyInAmount: 100m);
        p5.TimeBankSeconds = 60;

        table.Players[p1.Id] = p1;
        table.Players[p3.Id] = p3;
        table.Players[p5.Id] = p5;

        // Act - Hand 1: Button moves from 1 to 3 (skips empty seat 2)
        await _orchestrator.StartNewHandAsync(table);
        await CompleteHandQuicklyAsync(table);
        Assert.Equal(3, table.ButtonPosition);

        // Hand 2: Button moves from 3 to 5 (skips empty seat 4)
        ResetPlayersForNewHand(table);
        await _orchestrator.StartNewHandAsync(table);
        await CompleteHandQuicklyAsync(table);
        Assert.Equal(5, table.ButtonPosition);

        // Hand 3: Button wraps from 5 to 1 (skips empty seats 6-10, 2)
        ResetPlayersForNewHand(table);
        await _orchestrator.StartNewHandAsync(table);
        await CompleteHandQuicklyAsync(table);
        Assert.Equal(1, table.ButtonPosition);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Full Integration Scenario Tests

    [Fact]
    public async Task FullScenario_MultipleHandsInSequence_HandCountIncreases()
    {
        var sw = Stopwatch.StartNew();

        // Arrange
        var table = CreateTestTable(playerCount: 3, smallBlind: 1m, bigBlind: 2m);
        Assert.Equal(0, table.HandCount);

        // Act - Play 3 hands
        for (int i = 0; i < 3; i++)
        {
            if (i > 0) ResetPlayersForNewHand(table); // Only reset after first hand
            await _orchestrator.StartNewHandAsync(table);
            await CompleteHandQuicklyAsync(table);

            Assert.Equal(i + 1, table.HandCount); // Verify count after each hand
        }

        // Assert
        Assert.Equal(3, table.HandCount);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task FullScenario_ChipsAreConserved()
    {
        var sw = Stopwatch.StartNew();

        // Arrange
        var table = CreateTestTable(playerCount: 2, smallBlind: 1m, bigBlind: 2m);
        var initialTotalChips = table.Players.Values.Sum(p => p.ChipStack);

        // Act - Start hand and one player folds
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        var foldingPlayerId = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, foldingPlayerId, PlayerActionType.Fold);

        // Assert - Total chips unchanged (conservation of chips)
        var finalTotalChips = table.Players.Values.Sum(p => p.ChipStack);
        Assert.Equal(initialTotalChips, finalTotalChips);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task FullScenario_PlayerBustsOut_ExcludedFromNextHand()
    {
        var sw = Stopwatch.StartNew();

        // Arrange - One player will bust
        var table = CreateTableWithVariableStacks(
            ("WillBust", 1, 3m),
            ("Player2", 2, 100m),
            ("Player3", 3, 50m)
        );
        table.SmallBlind = 1m;
        table.BigBlind = 2m;

        var bustPlayer = table.Players.Values.First(p => p.DisplayName == "WillBust");

        // Play hand where bust player goes all-in
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        const int maxIterations = 50;
        int iterations = 0;

        while (hand.CurrentPlayerId.HasValue && hand.Phase != HandPhase.Showdown)
        {
            if (++iterations > maxIterations)
            {
                Assert.Fail($"Loop exceeded {maxIterations} iterations");
            }

            var current = hand.CurrentPlayerId.Value;
            var player = table.Players[current];

            if (player.Id == bustPlayer.Id)
            {
                await _orchestrator.ExecutePlayerActionAsync(table, current, PlayerActionType.AllIn);
            }
            else
            {
                var actions = _orchestrator.GetAvailableActions(table);
                if (actions?.CanCheck == true)
                    await _orchestrator.ExecutePlayerActionAsync(table, current, PlayerActionType.Check);
                else if (actions?.CanCall == true)
                    await _orchestrator.ExecutePlayerActionAsync(table, current, PlayerActionType.Call);
                else
                    await _orchestrator.ExecutePlayerActionAsync(table, current, PlayerActionType.Fold);
            }

            if (table.CurrentHand == null) break;
        }

        if (table.CurrentHand?.Phase == HandPhase.Showdown)
        {
            await _orchestrator.ExecuteShowdownAsync(table);
        }

        // FORCE the bust scenario for testing purposes
        // (In reality, cards are random - bust player might have won)
        bustPlayer.ChipStack = 0;
        bustPlayer.Status = PlayerStatus.Away;

        // Reset other players for next hand
        foreach (var p in table.Players.Values.Where(p => p.ChipStack > 0))
        {
            p.Status = PlayerStatus.Waiting;
        }

        // Act - Start next hand
        var nextHand = await _orchestrator.StartNewHandAsync(table);

        // Assert - Bust player should be excluded
        Assert.True(nextHand.IsSuccess);
        Assert.DoesNotContain(bustPlayer.Id, nextHand.Hand!.PlayerIds);
        Assert.Equal(2, nextHand.Hand.PlayerIds.Count);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task FullScenario_ShowdownCompletes_ChipsConserved()
    {
        var sw = Stopwatch.StartNew();

        // Arrange
        var table = CreateTestTable(playerCount: 2);
        var initialTotal = table.Players.Values.Sum(p => p.ChipStack);

        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);

        // Act - Play to showdown
        await PlayToShowdownAsync(table);
        Assert.Equal(HandPhase.Showdown, table.CurrentHand!.Phase);

        var showdownResult = await _orchestrator.ExecuteShowdownAsync(table);

        // Assert - Showdown succeeds and chips are conserved
        Assert.True(showdownResult.IsSuccess);
        var finalTotal = table.Players.Values.Sum(p => p.ChipStack);
        Assert.Equal(initialTotal, finalTotal);

        _output.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Invalid Action Rejection Tests

    [Fact]
    public async Task InvalidAction_RaiseWhenCantAfford_ReturnsError()
    {
        // Arrange
        var table = CreateTableWithVariableStacks(("Poor", 1, 10m), ("Rich", 2, 100m));
        table.SmallBlind = 1m;
        table.BigBlind = 2m;

        var startResult = await _orchestrator.StartNewHandAsync(table);
        Assert.True(startResult.IsSuccess);
        var hand = table.CurrentHand!;

        var poorPlayer = table.Players.Values.First(p => p.DisplayName == "Poor");

        // Wait until it's Poor player's turn
        while (hand.CurrentPlayerId.HasValue && hand.CurrentPlayerId.Value != poorPlayer.Id)
        {
            var current = hand.CurrentPlayerId.Value;
            var actions = _orchestrator.GetAvailableActions(table);
            if (actions?.CanCall == true)
            {
                await _orchestrator.ExecutePlayerActionAsync(table, current, PlayerActionType.Call);
            }
            else if (actions?.CanCheck == true)
            {
                await _orchestrator.ExecutePlayerActionAsync(table, current, PlayerActionType.Check);
            }
            else
            {
                break;
            }
        }

        // Verify we reached Poor's turn
        Assert.Equal(poorPlayer.Id, hand.CurrentPlayerId);

        // Act - Try to raise more than stack allows
        var result = await _orchestrator.ExecutePlayerActionAsync(
            table, poorPlayer.Id, PlayerActionType.Raise, 500m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Error));
    }

    [Fact]
    public async Task InvalidAction_CheckWhenFacingBet_ReturnsError()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // First player raises
        var raiser = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, raiser, PlayerActionType.Raise, 10m);

        // Act - Next player tries to check when facing a bet
        var nextPlayer = hand.CurrentPlayerId!.Value;
        var result = await _orchestrator.ExecutePlayerActionAsync(table, nextPlayer, PlayerActionType.Check);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task InvalidAction_ActingOutOfTurn_ReturnsError()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        var currentPlayerId = hand.CurrentPlayerId!.Value;
        var otherPlayer = table.Players.Values.First(p => p.Id != currentPlayerId);

        // Act - Other player tries to act out of turn
        var result = await _orchestrator.ExecutePlayerActionAsync(table, otherPlayer.Id, PlayerActionType.Call);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task InvalidAction_RaiseBelowMinimum_ReturnsError()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3, smallBlind: 1m, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Minimum raise is BB (2m), so raise to at least 4m
        var player = hand.CurrentPlayerId!.Value;

        // Act - Try to raise to only 3m (below minimum of 4m)
        var result = await _orchestrator.ExecutePlayerActionAsync(table, player, PlayerActionType.Raise, 3m);

        // Assert
        Assert.False(result.IsSuccess);
    }

    #endregion
}
