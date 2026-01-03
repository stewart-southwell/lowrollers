using LowRollers.Api.Domain.Betting;

namespace LowRollers.Api.Tests.Domain.Betting;

public class BettingRoundTests
{
    #region Creation Tests

    [Fact]
    public void Create_SetsMinimumRaise()
    {
        // Act
        var round = BettingRound.Create(minimumRaise: 2m);

        // Assert
        Assert.Equal(2m, round.MinimumRaise);
        Assert.Equal(2m, round.LastRaiseAmount);
        Assert.Equal(0m, round.CurrentBet);
        Assert.Equal(0, round.RaiseCount);
    }

    [Fact]
    public void CreatePreflop_SetsBlinds()
    {
        // Arrange
        var sbPlayer = Guid.NewGuid();
        var bbPlayer = Guid.NewGuid();

        // Act
        var round = BettingRound.CreatePreflop(0.5m, 1m, sbPlayer, bbPlayer);

        // Assert
        Assert.Equal(1m, round.CurrentBet);
        Assert.Equal(1m, round.MinimumRaise);
        Assert.Equal(0.5m, round.GetPlayerBet(sbPlayer));
        Assert.Equal(1m, round.GetPlayerBet(bbPlayer));
    }

    #endregion

    #region Action Recording Tests

    [Fact]
    public void RecordFold_AddsToActions()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        var playerId = Guid.NewGuid();

        // Act
        round.RecordFold(playerId);

        // Assert
        Assert.Single(round.Actions);
        Assert.Equal(PlayerActionType.Fold, round.Actions[0].Type);
        Assert.Equal(playerId, round.Actions[0].PlayerId);
    }

    [Fact]
    public void RecordCheck_AddsToActions()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        var playerId = Guid.NewGuid();

        // Act
        round.RecordCheck(playerId);

        // Assert
        Assert.Single(round.Actions);
        Assert.Equal(PlayerActionType.Check, round.Actions[0].Type);
    }

    [Fact]
    public void RecordCall_UpdatesPlayerBet()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        var playerId = Guid.NewGuid();
        round.RecordRaise(Guid.NewGuid(), 10m);

        // Act
        round.RecordCall(playerId, 10m);

        // Assert
        Assert.Equal(10m, round.GetPlayerBet(playerId));
        Assert.Equal(2, round.Actions.Count);
    }

    [Fact]
    public void RecordRaise_UpdatesState()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        var playerId = Guid.NewGuid();

        // Act
        round.RecordRaise(playerId, 10m);

        // Assert
        Assert.Equal(10m, round.CurrentBet);
        Assert.Equal(10m, round.LastRaiseAmount);
        Assert.Equal(10m, round.MinimumRaise);
        Assert.Equal(1, round.RaiseCount);
        Assert.Equal(playerId, round.LastAggressorId);
        Assert.Equal(10m, round.GetPlayerBet(playerId));
    }

    [Fact]
    public void RecordRaise_MultipleRaises_TracksCorrectly()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        var player1 = Guid.NewGuid();
        var player2 = Guid.NewGuid();

        // Act
        round.RecordRaise(player1, 4m);   // Raise to 4
        round.RecordRaise(player2, 12m);  // Raise to 12 (raise of 8)

        // Assert
        Assert.Equal(12m, round.CurrentBet);
        Assert.Equal(8m, round.LastRaiseAmount);
        Assert.Equal(8m, round.MinimumRaise);
        Assert.Equal(2, round.RaiseCount);
        Assert.Equal(player2, round.LastAggressorId);
    }

    [Fact]
    public void RecordAllIn_AsRaise_UpdatesState()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m);
        var playerId = Guid.NewGuid();

        // Act - All-in for 50 (current bet is 10)
        round.RecordAllIn(playerId, 50m, isRaise: true);

        // Assert
        Assert.Equal(50m, round.CurrentBet);
        Assert.Equal(40m, round.LastRaiseAmount); // 50 - 10 = 40
        Assert.Equal(40m, round.MinimumRaise);
        Assert.Equal(2, round.RaiseCount);
        Assert.Equal(playerId, round.LastAggressorId);
    }

    [Fact]
    public void RecordAllIn_NotARaise_DoesNotUpdateCurrentBet()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 100m);
        var playerId = Guid.NewGuid();

        // Act - All-in for 50 (less than current bet of 100)
        round.RecordAllIn(playerId, 50m, isRaise: false);

        // Assert
        Assert.Equal(100m, round.CurrentBet); // Unchanged
        Assert.Equal(1, round.RaiseCount); // Not increased
        Assert.Equal(50m, round.GetPlayerBet(playerId));
    }

    [Fact]
    public void RecordAllIn_BelowMinRaise_DoesNotUpdateMinRaise()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m); // Raise of 10
        var playerId = Guid.NewGuid();

        // Act - All-in for 5 more (to 15 total), but min raise is 10
        // This is an all-in but not a full raise
        round.RecordAllIn(playerId, 15m, isRaise: true);

        // Assert
        Assert.Equal(15m, round.CurrentBet);
        // MinimumRaise stays at 10 because raise was only 5
        Assert.Equal(10m, round.MinimumRaise);
    }

    #endregion

    #region Calculation Tests

    [Fact]
    public void GetAmountToCall_NoPlayerBet_ReturnsCurrentBet()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m);
        var playerId = Guid.NewGuid();

        // Act
        var amountToCall = round.GetAmountToCall(playerId);

        // Assert
        Assert.Equal(10m, amountToCall);
    }

    [Fact]
    public void GetAmountToCall_PartialBet_ReturnsDifference()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        var playerId = Guid.NewGuid();
        round.RecordRaise(playerId, 5m);
        round.RecordRaise(Guid.NewGuid(), 15m);

        // Act
        var amountToCall = round.GetAmountToCall(playerId);

        // Assert
        Assert.Equal(10m, amountToCall); // 15 - 5 = 10
    }

    [Fact]
    public void GetAmountToCall_AlreadyMatched_ReturnsZero()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        var playerId = Guid.NewGuid();
        round.RecordRaise(playerId, 10m);

        // Act
        var amountToCall = round.GetAmountToCall(playerId);

        // Assert
        Assert.Equal(0m, amountToCall);
    }

    [Fact]
    public void GetMinimumRaiseTotal_ReturnsCorrectValue()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 2m);
        round.RecordRaise(Guid.NewGuid(), 10m); // Raise of 10

        // Act
        var minRaiseTotal = round.GetMinimumRaiseTotal();

        // Assert
        Assert.Equal(20m, minRaiseTotal); // CurrentBet 10 + MinRaise 10
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsRoundState()
    {
        // Arrange
        var round = BettingRound.Create(minimumRaise: 1m);
        var lastAggressor = Guid.NewGuid();
        round.RecordRaise(Guid.NewGuid(), 10m);
        round.RecordRaise(lastAggressor, 25m);
        round.RecordCall(Guid.NewGuid(), 15m);

        // Act
        round.Reset(minimumRaise: 2m);

        // Assert
        Assert.Equal(0m, round.CurrentBet);
        Assert.Equal(2m, round.MinimumRaise);
        Assert.Equal(2m, round.LastRaiseAmount);
        Assert.Equal(0, round.RaiseCount);
        Assert.Empty(round.Actions);
        Assert.Empty(round.PlayerBets);

        // LastAggressorId is preserved for showdown order
        Assert.Equal(lastAggressor, round.LastAggressorId);
    }

    #endregion

    #region Preflop Scenario Tests

    [Fact]
    public void PreflopScenario_StandardBlinds()
    {
        // Arrange
        var sbPlayer = Guid.NewGuid();
        var bbPlayer = Guid.NewGuid();
        var utg = Guid.NewGuid();
        var button = Guid.NewGuid();

        // Act
        var round = BettingRound.CreatePreflop(0.5m, 1m, sbPlayer, bbPlayer);

        // UTG raises to 3
        round.RecordRaise(utg, 3m);

        // Button calls 3
        round.RecordCall(button, 3m);

        // Small blind folds
        round.RecordFold(sbPlayer);

        // Big blind calls 2 more (already has 1 in)
        round.RecordCall(bbPlayer, 2m);

        // Assert
        Assert.Equal(3m, round.CurrentBet);
        Assert.Equal(0.5m, round.GetPlayerBet(sbPlayer)); // Folded, bet stays
        Assert.Equal(3m, round.GetPlayerBet(bbPlayer));
        Assert.Equal(3m, round.GetPlayerBet(utg));
        Assert.Equal(3m, round.GetPlayerBet(button));
        Assert.Equal(utg, round.LastAggressorId);
        Assert.Equal(4, round.Actions.Count); // Blinds are not recorded as actions
    }

    [Fact]
    public void PreflopScenario_ThreeWayAllIn()
    {
        // Arrange
        var sbPlayer = Guid.NewGuid();
        var bbPlayer = Guid.NewGuid();
        var shortStack = Guid.NewGuid();
        var mediumStack = Guid.NewGuid();
        var bigStack = Guid.NewGuid();

        var round = BettingRound.CreatePreflop(0.5m, 1m, sbPlayer, bbPlayer);

        // Act
        // Short stack all-in for 8
        round.RecordAllIn(shortStack, 8m, isRaise: true);

        // Medium stack all-in for 25
        round.RecordAllIn(mediumStack, 25m, isRaise: true);

        // Big stack all-in for 100
        round.RecordAllIn(bigStack, 100m, isRaise: true);

        // Assert
        Assert.Equal(100m, round.CurrentBet);
        Assert.Equal(3, round.RaiseCount);
        Assert.Equal(8m, round.GetPlayerBet(shortStack));
        Assert.Equal(25m, round.GetPlayerBet(mediumStack));
        Assert.Equal(100m, round.GetPlayerBet(bigStack));
    }

    #endregion
}
