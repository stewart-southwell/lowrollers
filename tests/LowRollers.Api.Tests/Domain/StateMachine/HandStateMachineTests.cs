using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.StateMachine;
using LowRollers.Api.Domain.StateMachine.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LowRollers.Api.Tests.Domain.StateMachine;

public class HandStateMachineTests
{
    private readonly HandStateMachine _stateMachine;
    private readonly ILogger<HandStateMachine> _logger;

    public HandStateMachineTests()
    {
        _logger = NullLogger<HandStateMachine>.Instance;
        var handlers = CreateDefaultHandlers();
        _stateMachine = new HandStateMachine(handlers, _logger);
    }

    private static IEnumerable<IHandPhaseHandler> CreateDefaultHandlers()
    {
        yield return new WaitingPhaseHandler(NullLogger<WaitingPhaseHandler>.Instance);
        yield return new PreflopPhaseHandler(NullLogger<PreflopPhaseHandler>.Instance);
        yield return new FlopPhaseHandler(NullLogger<FlopPhaseHandler>.Instance);
        yield return new TurnPhaseHandler(NullLogger<TurnPhaseHandler>.Instance);
        yield return new RiverPhaseHandler(NullLogger<RiverPhaseHandler>.Instance);
        yield return new ShowdownPhaseHandler(NullLogger<ShowdownPhaseHandler>.Instance);
        yield return new CompletePhaseHandler(NullLogger<CompletePhaseHandler>.Instance);
    }

    private static Hand CreateTestHand(int playerCount = 4)
    {
        var playerIds = Enumerable.Range(1, playerCount)
            .Select(_ => Guid.NewGuid())
            .ToList();

        return Hand.Create(
            tableId: Guid.NewGuid(),
            handNumber: 1,
            buttonPosition: 1,
            smallBlindPosition: 2,
            bigBlindPosition: 3,
            smallBlindAmount: 0.5m,
            bigBlindAmount: 1m,
            playerIds: playerIds);
    }

    #region Static Transition Validation Tests

    [Theory]
    [InlineData(HandPhase.Waiting, HandPhase.Preflop, true)]
    [InlineData(HandPhase.Waiting, HandPhase.Flop, false)]
    [InlineData(HandPhase.Preflop, HandPhase.Flop, true)]
    [InlineData(HandPhase.Preflop, HandPhase.Showdown, true)]
    [InlineData(HandPhase.Preflop, HandPhase.Complete, true)]
    [InlineData(HandPhase.Flop, HandPhase.Turn, true)]
    [InlineData(HandPhase.Flop, HandPhase.Showdown, true)]
    [InlineData(HandPhase.Flop, HandPhase.Complete, true)]
    [InlineData(HandPhase.Flop, HandPhase.Preflop, false)]
    [InlineData(HandPhase.Turn, HandPhase.River, true)]
    [InlineData(HandPhase.Turn, HandPhase.Flop, false)]
    [InlineData(HandPhase.River, HandPhase.Showdown, true)]
    [InlineData(HandPhase.River, HandPhase.Complete, true)]
    [InlineData(HandPhase.River, HandPhase.Turn, false)]
    [InlineData(HandPhase.Showdown, HandPhase.Complete, true)]
    [InlineData(HandPhase.Showdown, HandPhase.River, false)]
    [InlineData(HandPhase.Complete, HandPhase.Waiting, false)]
    [InlineData(HandPhase.Complete, HandPhase.Preflop, false)]
    public void IsTransitionValid_ReturnsExpectedResult(HandPhase from, HandPhase to, bool expected)
    {
        // Act
        var result = HandStateMachine.IsTransitionValid(from, to);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetValidTransitions_FromWaiting_ReturnsOnlyPreflop()
    {
        // Act
        var transitions = HandStateMachine.GetValidTransitions(HandPhase.Waiting);

        // Assert
        Assert.Single(transitions);
        Assert.Contains(HandPhase.Preflop, transitions);
    }

    [Fact]
    public void GetValidTransitions_FromComplete_ReturnsEmpty()
    {
        // Act
        var transitions = HandStateMachine.GetValidTransitions(HandPhase.Complete);

        // Assert
        Assert.Empty(transitions);
    }

    [Fact]
    public void GetValidTransitions_FromPreflop_ReturnsMultipleOptions()
    {
        // Act
        var transitions = HandStateMachine.GetValidTransitions(HandPhase.Preflop);

        // Assert
        Assert.Contains(HandPhase.Flop, transitions);
        Assert.Contains(HandPhase.Showdown, transitions);
        Assert.Contains(HandPhase.Complete, transitions);
        Assert.Equal(3, transitions.Count);
    }

    #endregion

    #region Transition Execution Tests

    [Fact]
    public async Task TransitionAsync_ValidTransition_Succeeds()
    {
        // Arrange
        var hand = CreateTestHand();

        // Act
        var result = await _stateMachine.TransitionAsync(
            hand, HandPhase.Preflop, TransitionTrigger.StartHand);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.Preflop, hand.Phase);
        Assert.NotNull(result.Transition);
        Assert.Equal(HandPhase.Waiting, result.Transition.Value.FromPhase);
        Assert.Equal(HandPhase.Preflop, result.Transition.Value.ToPhase);
    }

    [Fact]
    public async Task TransitionAsync_InvalidTransition_Fails()
    {
        // Arrange
        var hand = CreateTestHand();

        // Act
        var result = await _stateMachine.TransitionAsync(
            hand, HandPhase.River, TransitionTrigger.BettingComplete);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(HandPhase.Waiting, hand.Phase); // Unchanged
        Assert.Contains("Invalid transition", result.Error);
    }

    [Fact]
    public async Task TransitionAsync_NotEnoughPlayers_Fails()
    {
        // Arrange
        var hand = CreateTestHand(playerCount: 1);

        // Act
        var result = await _stateMachine.TransitionAsync(
            hand, HandPhase.Preflop, TransitionTrigger.StartHand);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(HandPhase.Waiting, hand.Phase);
        Assert.Contains("2 players", result.Error);
    }

    [Fact]
    public async Task TransitionAsync_RecordsTransitionHistory()
    {
        // Arrange
        var hand = CreateTestHand();

        // Act
        await _stateMachine.TransitionAsync(hand, HandPhase.Preflop, TransitionTrigger.StartHand);

        // Assert
        Assert.Single(_stateMachine.TransitionHistory);
        var transition = _stateMachine.TransitionHistory[0];
        Assert.Equal(HandPhase.Waiting, transition.FromPhase);
        Assert.Equal(HandPhase.Preflop, transition.ToPhase);
        Assert.Equal(TransitionTrigger.StartHand, transition.Trigger);
    }

    [Fact]
    public async Task TransitionAsync_ToComplete_SetsCompletedAt()
    {
        // Arrange
        var hand = CreateTestHand();
        await _stateMachine.TransitionAsync(hand, HandPhase.Preflop, TransitionTrigger.StartHand);

        // Act
        var result = await _stateMachine.TransitionAsync(
            hand, HandPhase.Complete, TransitionTrigger.AllFolded);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.Complete, hand.Phase);
        Assert.NotNull(hand.CompletedAt);
    }

    [Fact]
    public async Task TransitionAsync_ResetsBettingState_ForBettingRounds()
    {
        // Arrange
        var hand = CreateTestHand();
        await _stateMachine.TransitionAsync(hand, HandPhase.Preflop, TransitionTrigger.StartHand);
        hand.CurrentBet = 10m;
        hand.RaisesThisRound = 2;

        // Add community cards for flop
        hand.CommunityCards.AddRange([
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.King),
            new Card(Suit.Diamonds, Rank.Queen)
        ]);

        // Act
        var result = await _stateMachine.TransitionAsync(
            hand, HandPhase.Flop, TransitionTrigger.BettingComplete);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, hand.CurrentBet);
        Assert.Equal(0, hand.RaisesThisRound);
    }

    #endregion

    #region Advance Tests

    [Fact]
    public async Task AdvanceAsync_DeterminesCorrectNextPhase()
    {
        // Arrange
        var hand = CreateTestHand();

        // Act
        var result = await _stateMachine.AdvanceAsync(hand, TransitionTrigger.StartHand);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.Preflop, hand.Phase);
    }

    [Fact]
    public async Task AdvanceAsync_AllFolded_GoesToComplete()
    {
        // Arrange
        var hand = CreateTestHand();
        await _stateMachine.TransitionAsync(hand, HandPhase.Preflop, TransitionTrigger.StartHand);

        // Act
        var result = await _stateMachine.AdvanceAsync(hand, TransitionTrigger.AllFolded);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.Complete, hand.Phase);
    }

    [Fact]
    public async Task AdvanceAsync_InvalidTrigger_Fails()
    {
        // Arrange
        var hand = CreateTestHand();

        // Act - ShowdownComplete doesn't make sense from Waiting
        var result = await _stateMachine.AdvanceAsync(hand, TransitionTrigger.ShowdownComplete);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(HandPhase.Waiting, hand.Phase);
    }

    #endregion

    #region DetermineNextPhase Tests

    [Theory]
    [InlineData(HandPhase.Waiting, TransitionTrigger.StartHand, HandPhase.Preflop)]
    [InlineData(HandPhase.Preflop, TransitionTrigger.BettingComplete, HandPhase.Flop)]
    [InlineData(HandPhase.Flop, TransitionTrigger.BettingComplete, HandPhase.Turn)]
    [InlineData(HandPhase.Turn, TransitionTrigger.BettingComplete, HandPhase.River)]
    [InlineData(HandPhase.River, TransitionTrigger.BettingComplete, HandPhase.Showdown)]
    [InlineData(HandPhase.Showdown, TransitionTrigger.ShowdownComplete, HandPhase.Complete)]
    public void DetermineNextPhase_ReturnsExpectedPhase(
        HandPhase currentPhase,
        TransitionTrigger trigger,
        HandPhase expectedNext)
    {
        // Arrange
        var hand = CreateTestHand();
        hand.Phase = currentPhase;

        // Act
        var nextPhase = HandStateMachine.DetermineNextPhase(hand, trigger);

        // Assert
        Assert.NotNull(nextPhase);
        Assert.Equal(expectedNext, nextPhase.Value);
    }

    [Theory]
    [InlineData(HandPhase.Preflop, TransitionTrigger.AllFolded, HandPhase.Complete)]
    [InlineData(HandPhase.Flop, TransitionTrigger.AllFolded, HandPhase.Complete)]
    [InlineData(HandPhase.Turn, TransitionTrigger.AllFolded, HandPhase.Complete)]
    [InlineData(HandPhase.River, TransitionTrigger.AllFolded, HandPhase.Complete)]
    public void DetermineNextPhase_AllFolded_ReturnsComplete(
        HandPhase currentPhase,
        TransitionTrigger trigger,
        HandPhase expectedNext)
    {
        // Arrange
        var hand = CreateTestHand();
        hand.Phase = currentPhase;

        // Act
        var nextPhase = HandStateMachine.DetermineNextPhase(hand, trigger);

        // Assert
        Assert.NotNull(nextPhase);
        Assert.Equal(expectedNext, nextPhase.Value);
    }

    [Fact]
    public void DetermineNextPhase_Complete_ReturnsNull()
    {
        // Arrange
        var hand = CreateTestHand();
        hand.Phase = HandPhase.Complete;

        // Act
        var nextPhase = HandStateMachine.DetermineNextPhase(hand, TransitionTrigger.AllFolded);

        // Assert - Can't transition from Complete
        Assert.Null(nextPhase);
    }

    #endregion

    #region Full Game Flow Test

    [Fact]
    public async Task FullHandFlow_CompletesSuccessfully()
    {
        // Arrange
        var hand = CreateTestHand();

        // Act & Assert - Walk through all phases
        var result = await _stateMachine.AdvanceAsync(hand, TransitionTrigger.StartHand);
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.Preflop, hand.Phase);

        // Add flop cards
        hand.CommunityCards.AddRange([
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.King),
            new Card(Suit.Diamonds, Rank.Queen)
        ]);

        result = await _stateMachine.AdvanceAsync(hand, TransitionTrigger.BettingComplete);
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.Flop, hand.Phase);

        // Add turn card
        hand.CommunityCards.Add(new Card(Suit.Clubs, Rank.Jack));

        result = await _stateMachine.AdvanceAsync(hand, TransitionTrigger.BettingComplete);
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.Turn, hand.Phase);

        // Add river card
        hand.CommunityCards.Add(new Card(Suit.Hearts, Rank.Ten));

        result = await _stateMachine.AdvanceAsync(hand, TransitionTrigger.BettingComplete);
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.River, hand.Phase);

        result = await _stateMachine.AdvanceAsync(hand, TransitionTrigger.BettingComplete);
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.Showdown, hand.Phase);

        result = await _stateMachine.AdvanceAsync(hand, TransitionTrigger.ShowdownComplete);
        Assert.True(result.IsSuccess);
        Assert.Equal(HandPhase.Complete, hand.Phase);

        // Verify all transitions recorded
        Assert.Equal(6, _stateMachine.TransitionHistory.Count);
        Assert.NotNull(hand.CompletedAt);
    }

    #endregion
}
