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

namespace LowRollers.Api.Tests.Features.GameEngine;

public class GameOrchestratorTests
{
    private readonly GameOrchestrator _orchestrator;
    private readonly IHandEventStore _eventStore;

    public GameOrchestratorTests()
    {
        var shuffleService = new ShuffleService();
        var potManager = new PotManager();
        _eventStore = new InMemoryHandEventStore();

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
            potManager,
            _eventStore,
            NullLogger<ShowdownHandler>.Instance);

        _orchestrator = new GameOrchestrator(
            shuffleService,
            potManager,
            _eventStore,
            stateMachine,
            showdownHandler,
            NullLogger<GameOrchestrator>.Instance);
    }

    private static Table CreateTestTable(int playerCount = 3, decimal smallBlind = 1m, decimal bigBlind = 2m)
    {
        var table = new Table
        {
            Id = Guid.NewGuid(),
            Name = "Test Table",
            SmallBlind = smallBlind,
            BigBlind = bigBlind,
            ButtonPosition = 1
        };

        for (int i = 0; i < playerCount; i++)
        {
            var player = Player.Create(
                Guid.NewGuid(),
                $"Player{i + 1}",
                seatPosition: i + 1,
                buyInAmount: 100m);
            table.Players[player.Id] = player;
        }

        return table;
    }

    #region StartNewHandAsync Tests

    [Fact]
    public async Task StartNewHandAsync_WithMinimumPlayers_ReturnsSuccess()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);

        // Act
        var result = await _orchestrator.StartNewHandAsync(table);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Hand);
        Assert.NotNull(result.HoleCards);
        Assert.Equal(2, result.HoleCards.Count);
    }

    [Fact]
    public async Task StartNewHandAsync_WithOnePlayer_ReturnsFailure()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 1);

        // Act
        var result = await _orchestrator.StartNewHandAsync(table);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("2 active players", result.Error);
    }

    [Fact]
    public async Task StartNewHandAsync_RotatesButton()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        table.ButtonPosition = 1;

        // Act
        var result = await _orchestrator.StartNewHandAsync(table);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, table.ButtonPosition); // Moved from 1 to 2
    }

    [Fact]
    public async Task StartNewHandAsync_PostsBlindsCorrectly()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3, smallBlind: 1m, bigBlind: 2m);
        table.ButtonPosition = 1;

        // Act
        var result = await _orchestrator.StartNewHandAsync(table);

        // Assert
        Assert.True(result.IsSuccess);

        // Button moves to seat 2, SB is seat 3, BB is seat 1
        var sbPlayer = table.Players.Values.First(p => p.SeatPosition == 3);
        var bbPlayer = table.Players.Values.First(p => p.SeatPosition == 1);

        Assert.Equal(99m, sbPlayer.ChipStack); // 100 - 1
        Assert.Equal(98m, bbPlayer.ChipStack); // 100 - 2
        Assert.Equal(3m, result.Hand!.TotalPot); // 1 + 2
    }

    [Fact]
    public async Task StartNewHandAsync_DealsHoleCardsToAllPlayers()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 4);

        // Act
        var result = await _orchestrator.StartNewHandAsync(table);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.HoleCards!.Count);

        foreach (var (playerId, cards) in result.HoleCards)
        {
            Assert.Equal(2, cards.Length);
            Assert.NotEqual(cards[0], cards[1]); // Different cards
        }
    }

    [Fact]
    public async Task StartNewHandAsync_SetsFirstToActAsUTG()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 4);
        table.ButtonPosition = 1;

        // Act
        var result = await _orchestrator.StartNewHandAsync(table);

        // Assert
        Assert.True(result.IsSuccess);
        // Button at 2, SB at 3, BB at 4, UTG at 1
        var utgPlayer = table.Players.Values.First(p => p.SeatPosition == 1);
        Assert.Equal(utgPlayer.Id, result.Hand!.CurrentPlayerId);
    }

    [Fact]
    public async Task StartNewHandAsync_TransitionsToPreflop()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);

        // Act
        var result = await _orchestrator.StartNewHandAsync(table);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.Preflop, result.Hand!.Phase);
    }

    [Fact]
    public async Task StartNewHandAsync_RecordsEvents()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);

        // Act
        var result = await _orchestrator.StartNewHandAsync(table);

        // Assert
        var events = await _eventStore.GetEventsAsync(result.Hand!.Id).ToListAsync();
        Assert.True(events.Count >= 3); // HandStarted, BlindsPosted, HoleCardsDealt
        Assert.IsType<HandStartedEvent>(events[0]);
        Assert.IsType<BlindsPostedEvent>(events[1]);
        Assert.IsType<HoleCardsDealtEvent>(events[2]);
    }

    #endregion

    #region Heads-Up Blind Position Tests

    [Fact]
    public async Task StartNewHandAsync_HeadsUp_ButtonIsSmallBlind()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);
        table.ButtonPosition = 1;

        // Act
        var result = await _orchestrator.StartNewHandAsync(table);

        // Assert
        Assert.True(result.IsSuccess);
        // In heads-up, button (seat 2 after rotation) is SB
        Assert.Equal(2, result.Hand!.SmallBlindPosition);
        Assert.Equal(1, result.Hand!.BigBlindPosition);
    }

    [Fact]
    public async Task StartNewHandAsync_HeadsUp_BigBlindActsFirst()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);
        table.ButtonPosition = 1;

        // Act
        var result = await _orchestrator.StartNewHandAsync(table);

        // Assert
        // In heads-up preflop, player left of BB acts first, which is the button/SB
        var sbPlayer = table.Players.Values.First(p => p.SeatPosition == result.Hand!.SmallBlindPosition);
        Assert.Equal(sbPlayer.Id, result.Hand!.CurrentPlayerId);
    }

    #endregion

    #region ExecutePlayerActionAsync Tests

    [Fact]
    public async Task ExecutePlayerActionAsync_Fold_UpdatesPlayerStatus()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;
        var currentPlayerId = hand.CurrentPlayerId!.Value;

        // Act
        var result = await _orchestrator.ExecutePlayerActionAsync(table, currentPlayerId, PlayerActionType.Fold);

        // Assert
        Assert.True(result.IsSuccess);
        var player = table.Players[currentPlayerId];
        Assert.Equal(PlayerStatus.Folded, player.Status);
    }

    [Fact]
    public async Task ExecutePlayerActionAsync_WrongPlayer_ReturnsFailure()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;
        var wrongPlayer = table.Players.Values.First(p => p.Id != hand.CurrentPlayerId);

        // Act
        var result = await _orchestrator.ExecutePlayerActionAsync(table, wrongPlayer.Id, PlayerActionType.Fold);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not your turn", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecutePlayerActionAsync_Call_DeductsChips()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;
        var currentPlayerId = hand.CurrentPlayerId!.Value;
        var player = table.Players[currentPlayerId];
        var initialStack = player.ChipStack;

        // Act
        var result = await _orchestrator.ExecutePlayerActionAsync(table, currentPlayerId, PlayerActionType.Call);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(initialStack - 2m, player.ChipStack); // Called 2 BB
    }

    [Fact]
    public async Task ExecutePlayerActionAsync_Raise_UpdatesBetLevel()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;
        var currentPlayerId = hand.CurrentPlayerId!.Value;

        // Act - raise to 6 (min raise is BB + BB = 4, so 6 is valid)
        var result = await _orchestrator.ExecutePlayerActionAsync(table, currentPlayerId, PlayerActionType.Raise, 6m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(6m, hand.CurrentBet);
    }

    [Fact]
    public async Task ExecutePlayerActionAsync_AllIn_SetsPlayerStatusAllIn()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;
        var currentPlayerId = hand.CurrentPlayerId!.Value;

        // Act
        var result = await _orchestrator.ExecutePlayerActionAsync(table, currentPlayerId, PlayerActionType.AllIn);

        // Assert
        Assert.True(result.IsSuccess);
        var player = table.Players[currentPlayerId];
        Assert.Equal(PlayerStatus.AllIn, player.Status);
        Assert.Equal(0m, player.ChipStack);
    }

    [Fact]
    public async Task ExecutePlayerActionAsync_AdvancesToNextPlayer()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;
        var firstPlayerId = hand.CurrentPlayerId!.Value;

        // Act
        var result = await _orchestrator.ExecutePlayerActionAsync(table, firstPlayerId, PlayerActionType.Call);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(firstPlayerId, result.NextPlayerId);
        Assert.NotNull(result.NextPlayerId);
    }

    #endregion

    #region Betting Round Completion Tests

    [Fact]
    public async Task ExecutePlayerActionAsync_AllPlayersCall_CompletesBettingRound()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Act - UTG calls, Button calls, SB calls, BB checks
        var utg = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, utg, PlayerActionType.Call);

        var btn = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, btn, PlayerActionType.Call);

        var sb = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, sb, PlayerActionType.Call); // SB completes to BB

        var bb = hand.CurrentPlayerId!.Value;
        var result = await _orchestrator.ExecutePlayerActionAsync(table, bb, PlayerActionType.Check);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.BettingRoundComplete);
        Assert.Equal(HandPhase.Flop, hand.Phase);
        Assert.Equal(3, hand.CommunityCards.Count); // Flop dealt
    }

    [Fact]
    public async Task ExecutePlayerActionAsync_AfterBettingComplete_DealsFlop()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Act - SB calls, BB checks (heads-up)
        var sb = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, sb, PlayerActionType.Call);

        var bb = hand.CurrentPlayerId!.Value;
        var result = await _orchestrator.ExecutePlayerActionAsync(table, bb, PlayerActionType.Check);

        // Assert
        Assert.True(result.BettingRoundComplete);
        Assert.NotNull(result.NewCommunityCards);
        Assert.Equal(3, result.NewCommunityCards.Length);
        Assert.Equal(3, hand.CommunityCards.Count);
    }

    #endregion

    #region All-Fold Tests

    [Fact]
    public async Task ExecutePlayerActionAsync_AllButOneFold_AwardsPot()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Act - UTG folds, Button folds
        var utg = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, utg, PlayerActionType.Fold);

        var btn = hand.CurrentPlayerId!.Value;
        var result = await _orchestrator.ExecutePlayerActionAsync(table, btn, PlayerActionType.Fold);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.HandComplete);
        Assert.NotNull(result.Winnings);
        Assert.Single(result.Winnings);

        var winnerId = result.Winnings.Keys.First();
        var winner = table.Players[winnerId];
        Assert.Equal(3m, result.Winnings[winnerId]); // SB + BB = 1 + 2 = 3
    }

    [Fact]
    public async Task ExecutePlayerActionAsync_AllFold_CleansUpHand()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Act - SB folds
        var sb = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, sb, PlayerActionType.Fold);

        // Assert
        Assert.Null(table.CurrentHand);
        Assert.Null(_orchestrator.GetBettingRound(hand.Id));
    }

    [Fact]
    public async Task ExecutePlayerActionAsync_AllFold_RecordsCompletionEvents()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;
        var handId = hand.Id;

        // Act
        var sb = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, sb, PlayerActionType.Fold);

        // Assert
        var events = await _eventStore.GetEventsAsync(handId).ToListAsync();
        Assert.Contains(events, e => e is PotAwardedEvent);
        Assert.Contains(events, e => e is HandCompletedEvent);
    }

    #endregion

    #region Phase Advancement Tests

    [Fact]
    public async Task ExecutePlayerActionAsync_CompletesFlop_DealsTurn()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Complete preflop
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Call);
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);

        Assert.Equal(HandPhase.Flop, hand.Phase);
        Assert.Equal(3, hand.CommunityCards.Count);

        // Complete flop
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);
        var result = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);

        // Assert
        Assert.True(result.BettingRoundComplete);
        Assert.Equal(HandPhase.Turn, hand.Phase);
        Assert.Equal(4, hand.CommunityCards.Count);
        Assert.Single(result.NewCommunityCards!);
    }

    [Fact]
    public async Task ExecutePlayerActionAsync_CompletesRiver_GoesToShowdown()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Complete all streets with checks
        // Preflop
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Call);
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);

        // Flop
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);

        // Turn
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);

        // River
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);
        var result = await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);

        // Assert
        Assert.True(result.BettingRoundComplete);
        Assert.Equal(HandPhase.Showdown, hand.Phase);
        Assert.Equal(5, hand.CommunityCards.Count);
    }

    #endregion

    #region GetAvailableActions Tests

    [Fact]
    public async Task GetAvailableActions_ForCurrentPlayer_ReturnsValidActions()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);

        // Act
        var actions = _orchestrator.GetAvailableActions(table);

        // Assert
        Assert.NotNull(actions);
        Assert.True(actions.CanFold);
        Assert.True(actions.CanCall);
        Assert.True(actions.CanRaise);
        Assert.Equal(2m, actions.CallAmount);
    }

    [Fact]
    public async Task GetAvailableActions_AfterCheck_IncludesCheck()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Complete preflop
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Call);
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Check);

        // Now on flop, first to act can check
        var actions = _orchestrator.GetAvailableActions(table);

        // Assert
        Assert.NotNull(actions);
        Assert.True(actions.CanCheck);
        Assert.False(actions.CanCall); // No bet to call
    }

    [Fact]
    public void GetAvailableActions_NoActiveHand_ReturnsNull()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);

        // Act
        var actions = _orchestrator.GetAvailableActions(table);

        // Assert
        Assert.Null(actions);
    }

    #endregion

    #region ForceTimeoutFoldAsync Tests

    [Fact]
    public async Task ForceTimeoutFoldAsync_FoldsCurrentPlayer()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;
        var currentPlayerId = hand.CurrentPlayerId!.Value;

        // Act
        var result = await _orchestrator.ForceTimeoutFoldAsync(table);

        // Assert
        Assert.True(result.IsSuccess);
        var player = table.Players[currentPlayerId];
        Assert.Equal(PlayerStatus.Folded, player.Status);
    }

    [Fact]
    public async Task ForceTimeoutFoldAsync_NoActiveHand_ReturnsFailure()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2);

        // Act
        var result = await _orchestrator.ForceTimeoutFoldAsync(table);

        // Assert
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Run-Out Tests (All-In Scenarios)

    [Fact]
    public async Task ExecutePlayerActionAsync_AllPlayersAllIn_RunsOutBoard()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 2, bigBlind: 2m);
        await _orchestrator.StartNewHandAsync(table);
        var hand = table.CurrentHand!;

        // Act - Both players all-in preflop
        var sb = hand.CurrentPlayerId!.Value;
        await _orchestrator.ExecutePlayerActionAsync(table, sb, PlayerActionType.AllIn);

        var bb = hand.CurrentPlayerId!.Value;
        var result = await _orchestrator.ExecutePlayerActionAsync(table, bb, PlayerActionType.AllIn);

        // Assert - Board should be run out
        Assert.True(result.BettingRoundComplete);
        Assert.Equal(5, hand.CommunityCards.Count);
        Assert.Equal(HandPhase.Showdown, hand.Phase);
    }

    #endregion

    #region Bomb Pot Tests

    [Fact]
    public async Task StartBombPotAsync_DoesNotRotateButton()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        table.ButtonPosition = 2;
        var initialButtonPosition = table.ButtonPosition;

        // Act
        var result = await _orchestrator.StartBombPotAsync(table, anteAmount: 5m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(initialButtonPosition, table.ButtonPosition); // Button should NOT move
    }

    [Fact]
    public async Task StartBombPotAsync_CollectsAntesFromAllPlayers()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        var initialStacks = table.Players.Values.ToDictionary(p => p.Id, p => p.ChipStack);

        // Act
        var result = await _orchestrator.StartBombPotAsync(table, anteAmount: 5m);

        // Assert
        Assert.True(result.IsSuccess);
        foreach (var player in table.Players.Values)
        {
            Assert.Equal(initialStacks[player.Id] - 5m, player.ChipStack);
        }
        Assert.Equal(15m, result.Hand!.TotalPot); // 3 players x 5 = 15
    }

    [Fact]
    public async Task StartBombPotAsync_SkipsToFlop()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);

        // Act
        var result = await _orchestrator.StartBombPotAsync(table, anteAmount: 5m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.Flop, result.Hand!.Phase);
        Assert.Equal(3, result.Hand.CommunityCards.Count);
    }

    [Fact]
    public async Task StartBombPotAsync_ThenRegularHand_RotatesButton()
    {
        // Arrange
        var table = CreateTestTable(playerCount: 3);
        table.ButtonPosition = 1;

        // Act - Start bomb pot (button stays at 1)
        await _orchestrator.StartBombPotAsync(table, anteAmount: 5m);
        Assert.Equal(1, table.ButtonPosition);

        // Complete the bomb pot hand (all fold)
        var hand = table.CurrentHand!;
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Fold);
        await _orchestrator.ExecutePlayerActionAsync(table, hand.CurrentPlayerId!.Value, PlayerActionType.Fold);

        // Start regular hand - NOW button should rotate
        var result = await _orchestrator.StartNewHandAsync(table);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, table.ButtonPosition); // Button moved from 1 to 2
    }

    #endregion
}
