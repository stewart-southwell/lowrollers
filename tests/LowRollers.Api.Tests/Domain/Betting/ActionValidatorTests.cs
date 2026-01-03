using LowRollers.Api.Domain.Betting;
using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Tests.Domain.Betting;

public class ActionValidatorTests
{
    private readonly ActionValidator _validator = new();

    private static Player CreateActivePlayer(decimal chipStack = 100m, decimal currentBet = 0m)
    {
        var player = Player.Create(
            id: Guid.NewGuid(),
            displayName: "TestPlayer",
            seatPosition: 1,
            buyInAmount: chipStack);

        player.Status = PlayerStatus.Active;
        player.CurrentBet = currentBet;
        return player;
    }

    #region Fold Tests

    [Fact]
    public void ValidateFold_PlayersTurn_ReturnsValid()
    {
        // Arrange
        var player = CreateActivePlayer();
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var result = _validator.ValidateFold(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(PlayerActionType.Fold, result.Action.Type);
        Assert.Equal(0m, result.Action.Amount);
    }

    [Fact]
    public void ValidateFold_NotPlayersTurn_ReturnsInvalid()
    {
        // Arrange
        var player = CreateActivePlayer();
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var result = _validator.ValidateFold(player, round, isPlayersTurn: false);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("not your turn", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFold_PlayerNotActive_ReturnsInvalid()
    {
        // Arrange
        var player = CreateActivePlayer();
        player.Status = PlayerStatus.Folded;
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var result = _validator.ValidateFold(player, round, isPlayersTurn: true);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("cannot act", result.ErrorMessage);
    }

    #endregion

    #region Check Tests

    [Fact]
    public void ValidateCheck_NoBetFacing_ReturnsValid()
    {
        // Arrange
        var player = CreateActivePlayer();
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var result = _validator.ValidateCheck(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(PlayerActionType.Check, result.Action.Type);
    }

    [Fact]
    public void ValidateCheck_BetFacing_ReturnsInvalid()
    {
        // Arrange
        var player = CreateActivePlayer();
        var smallBlindPlayer = Guid.NewGuid();
        var bigBlindPlayer = Guid.NewGuid();
        var round = BettingRound.CreatePreflop(0.5m, 1m, smallBlindPlayer, bigBlindPlayer);

        // Act
        var result = _validator.ValidateCheck(player, round, isPlayersTurn: true);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("cannot check", result.ErrorMessage);
    }

    [Fact]
    public void ValidateCheck_AlreadyMatchedBet_ReturnsValid()
    {
        // Arrange
        var player = CreateActivePlayer();
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m);
        round.RecordCall(player.Id, 10m); // Player already matched

        // Now another player acts and it comes back to this player
        // Current bet is still 10, player has bet 10

        // Act
        var result = _validator.ValidateCheck(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCheck_NotPlayersTurn_ReturnsInvalid()
    {
        // Arrange
        var player = CreateActivePlayer();
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var result = _validator.ValidateCheck(player, round, isPlayersTurn: false);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("not your turn", result.ErrorMessage);
    }

    #endregion

    #region Call Tests

    [Fact]
    public void ValidateCall_BetFacing_ReturnsValid()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 100m);
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m);

        // Act
        var result = _validator.ValidateCall(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(PlayerActionType.Call, result.Action.Type);
        Assert.Equal(10m, result.Action.Amount);
        Assert.Equal(90m, result.Action.RemainingStack);
    }

    [Fact]
    public void ValidateCall_NoBetFacing_ReturnsInvalid()
    {
        // Arrange
        var player = CreateActivePlayer();
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var result = _validator.ValidateCall(player, round, isPlayersTurn: true);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("nothing to call", result.ErrorMessage);
    }

    [Fact]
    public void ValidateCall_NotEnoughChips_ReturnsAllIn()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 5m);
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m);

        // Act
        var result = _validator.ValidateCall(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(PlayerActionType.AllIn, result.Action.Type);
        Assert.Equal(5m, result.Action.Amount);
        Assert.Equal(0m, result.Action.RemainingStack);
    }

    [Fact]
    public void ValidateCall_PartialBetAlreadyIn_ReturnsCorrectAmount()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 100m);
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(player.Id, 5m); // Player previously raised to 5
        round.RecordRaise(Guid.NewGuid(), 15m); // Someone else raised to 15

        // Player needs to call 10 more (15 - 5 = 10)

        // Act
        var result = _validator.ValidateCall(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(10m, result.Action.Amount);
        Assert.Equal(15m, result.Action.NewTotalBet);
    }

    #endregion

    #region Raise Tests

    [Fact]
    public void ValidateRaise_ValidAmount_ReturnsValid()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 100m);
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act - Raise to 2 (min raise is 1, so total must be at least 1)
        var result = _validator.ValidateRaise(player, round, raiseToAmount: 2m, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(PlayerActionType.Raise, result.Action.Type);
        Assert.Equal(2m, result.Action.Amount);
        Assert.Equal(2m, result.Action.NewTotalBet);
        Assert.True(result.Action.IsRaise);
    }

    [Fact]
    public void ValidateRaise_BelowMinimum_ReturnsInvalid()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 100m);
        var round = BettingRound.Create(minimumRaise: 2m);
        round.RecordRaise(Guid.NewGuid(), 10m); // Current bet is 10, last raise was 10

        // Min raise total should be 10 + 10 = 20

        // Act - Try to raise to 15 (below minimum of 20)
        var result = _validator.ValidateRaise(player, round, raiseToAmount: 15m, isPlayersTurn: true);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Minimum raise", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRaise_NotEnoughChips_ReturnsInvalid()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 10m);
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act - Try to raise to 20 (only have 10)
        var result = _validator.ValidateRaise(player, round, raiseToAmount: 20m, isPlayersTurn: true);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("don't have enough chips", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRaise_ExactlyAllIn_ReturnsAllIn()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 20m);
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act - Raise to exactly all chips
        var result = _validator.ValidateRaise(player, round, raiseToAmount: 20m, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(PlayerActionType.AllIn, result.Action.Type);
        Assert.Equal(0m, result.Action.RemainingStack);
    }

    [Fact]
    public void ValidateRaise_AfterPreviousRaises_CalculatesMinCorrectly()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 100m);
        var round = BettingRound.Create(minimumRaise: 1m);

        // Player A raises to 4 (raise of 4)
        round.RecordRaise(Guid.NewGuid(), 4m);
        // Player B re-raises to 12 (raise of 8)
        round.RecordRaise(Guid.NewGuid(), 12m);

        // Min raise is now 8, so minimum total is 12 + 8 = 20

        // Act - Try to raise to 20 (exactly minimum)
        var result = _validator.ValidateRaise(player, round, raiseToAmount: 20m, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateRaise_AllInBelowMinimum_ReturnsValidAllIn()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 15m);
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m);

        // Min raise total is 20, but player only has 15
        // All-in for less than min raise is allowed

        // Act
        var result = _validator.ValidateRaise(player, round, raiseToAmount: 15m, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(PlayerActionType.AllIn, result.Action.Type);
    }

    #endregion

    #region All-In Tests

    [Fact]
    public void ValidateAllIn_HasChips_ReturnsValid()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 50m);
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var result = _validator.ValidateAllIn(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(PlayerActionType.AllIn, result.Action.Type);
        Assert.Equal(50m, result.Action.Amount);
        Assert.Equal(50m, result.Action.NewTotalBet);
        Assert.Equal(0m, result.Action.RemainingStack);
    }

    [Fact]
    public void ValidateAllIn_NoChips_ReturnsInvalid()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 0m);
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var result = _validator.ValidateAllIn(player, round, isPlayersTurn: true);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("no chips", result.ErrorMessage);
    }

    [Fact]
    public void ValidateAllIn_LessThanCall_IsNotRaise()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 5m);
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m);

        // Player goes all-in for 5, but current bet is 10
        // This is not a raise, just an all-in call

        // Act
        var result = _validator.ValidateAllIn(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.False(result.Action.IsRaise);
    }

    [Fact]
    public void ValidateAllIn_MoreThanCurrentBet_IsRaise()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 50m);
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m);

        // Player goes all-in for 50, current bet is 10
        // This is a raise

        // Act
        var result = _validator.ValidateAllIn(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.True(result.Action.IsRaise);
    }

    #endregion

    #region GetAvailableActions Tests

    [Fact]
    public void GetAvailableActions_NotPlayersTurn_ReturnsNone()
    {
        // Arrange
        var player = CreateActivePlayer();
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var actions = _validator.GetAvailableActions(player, round, isPlayersTurn: false);

        // Assert
        Assert.False(actions.CanFold);
        Assert.False(actions.CanCheck);
        Assert.False(actions.CanCall);
        Assert.False(actions.CanRaise);
        Assert.False(actions.CanAllIn);
    }

    [Fact]
    public void GetAvailableActions_NoBetFacing_CanCheckAndRaise()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 100m);
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var actions = _validator.GetAvailableActions(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(actions.CanFold);
        Assert.True(actions.CanCheck);
        Assert.False(actions.CanCall);
        Assert.True(actions.CanRaise);
        Assert.True(actions.CanAllIn);
        Assert.Equal(1m, actions.MinRaiseTotal);
        Assert.Equal(100m, actions.MaxRaiseTotal);
    }

    [Fact]
    public void GetAvailableActions_BetFacing_CanCallAndRaise()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 100m);
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m);

        // Act
        var actions = _validator.GetAvailableActions(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(actions.CanFold);
        Assert.False(actions.CanCheck);
        Assert.True(actions.CanCall);
        Assert.Equal(10m, actions.CallAmount);
        Assert.True(actions.CanRaise);
        Assert.Equal(20m, actions.MinRaiseTotal); // Current 10 + min raise 10
    }

    [Fact]
    public void GetAvailableActions_LowStack_LimitedRaise()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 15m);
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m);

        // Min raise is 20, player only has 15
        // Can still all-in, but not a full raise

        // Act
        var actions = _validator.GetAvailableActions(player, round, isPlayersTurn: true);

        // Assert
        Assert.True(actions.CanCall);
        Assert.False(actions.CanRaise); // Can't make a full raise
        Assert.True(actions.CanAllIn);
        Assert.Equal(15m, actions.AllInAmount);
    }

    #endregion

    #region Validate Generic Method Tests

    [Theory]
    [InlineData(PlayerActionType.Fold)]
    [InlineData(PlayerActionType.Check)]
    [InlineData(PlayerActionType.AllIn)]
    public void Validate_DifferentActionTypes_DelegatesToCorrectMethod(PlayerActionType actionType)
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 100m);
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var result = _validator.Validate(player, round, actionType, 0m, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(actionType, result.Action.Type);
    }

    [Fact]
    public void Validate_Call_DelegatesToCallMethod()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 100m);
        var round = BettingRound.Create(minimumRaise: 1m);
        round.RecordRaise(Guid.NewGuid(), 10m);

        // Act
        var result = _validator.Validate(player, round, PlayerActionType.Call, 0m, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(PlayerActionType.Call, result.Action.Type);
    }

    [Fact]
    public void Validate_Raise_DelegatesToRaiseMethod()
    {
        // Arrange
        var player = CreateActivePlayer(chipStack: 100m);
        var round = BettingRound.Create(minimumRaise: 1m);

        // Act
        var result = _validator.Validate(player, round, PlayerActionType.Raise, 5m, isPlayersTurn: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Action);
        Assert.Equal(PlayerActionType.Raise, result.Action.Type);
    }

    #endregion
}
