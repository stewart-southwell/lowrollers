using LowRollers.Api.Domain.Betting;
using LowRollers.Api.Domain.Evaluation;
using LowRollers.Api.Domain.Events;
using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.StateMachine;

namespace LowRollers.Api.Tests.Domain.Events;

public class HandEventTests
{
    private static readonly Guid TestHandId = Guid.NewGuid();
    private static readonly Guid TestTableId = Guid.NewGuid();
    private static readonly Guid TestPlayerId1 = Guid.NewGuid();
    private static readonly Guid TestPlayerId2 = Guid.NewGuid();

    #region IHandEvent Interface Tests

    [Fact]
    public void HandStartedEvent_ImplementsIHandEvent()
    {
        // Arrange & Act
        var @event = new HandStartedEvent
        {
            HandId = TestHandId,
            TableId = TestTableId,
            HandNumber = 1,
            ButtonPosition = 1,
            SmallBlindPosition = 2,
            BigBlindPosition = 3,
            SmallBlindAmount = 0.5m,
            BigBlindAmount = 1m,
            PlayerIds = [TestPlayerId1, TestPlayerId2]
        };

        // Assert
        Assert.IsAssignableFrom<IHandEvent>(@event);
        Assert.Equal(TestHandId, @event.HandId);
        Assert.Equal(1, @event.SequenceNumber);
        Assert.Equal(nameof(HandStartedEvent), @event.EventType);
        Assert.True(@event.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void AllEvents_HaveCorrectEventType()
    {
        // Arrange
        var events = new IHandEvent[]
        {
            new HandStartedEvent
            {
                HandId = TestHandId, TableId = TestTableId, HandNumber = 1,
                ButtonPosition = 1, SmallBlindPosition = 2, BigBlindPosition = 3,
                SmallBlindAmount = 0.5m, BigBlindAmount = 1m, PlayerIds = []
            },
            new BlindsPostedEvent
            {
                HandId = TestHandId, SequenceNumber = 2,
                SmallBlindPlayerId = TestPlayerId1, SmallBlindAmount = 0.5m,
                BigBlindPlayerId = TestPlayerId2, BigBlindAmount = 1m, PotTotal = 1.5m
            },
            new AntePostedEvent
            {
                HandId = TestHandId, SequenceNumber = 3,
                PlayerId = TestPlayerId1, Amount = 0.25m, RemainingStack = 99.75m, PotTotal = 0.25m
            },
            new HoleCardsDealtEvent
            {
                HandId = TestHandId, SequenceNumber = 4,
                PlayerCards = new Dictionary<Guid, Card[]>()
            },
            new PlayerActedEvent
            {
                HandId = TestHandId, SequenceNumber = 5,
                PlayerId = TestPlayerId1, ActionType = PlayerActionType.Fold,
                Amount = 0m, Phase = HandPhase.Preflop, RemainingStack = 100m,
                PotTotal = 1.5m, CurrentBetLevel = 1m
            },
            new BettingRoundCompletedEvent
            {
                HandId = TestHandId, SequenceNumber = 6,
                CompletedPhase = HandPhase.Preflop, PotTotal = 10m,
                ActivePlayerCount = 3, PlayersInHand = 4
            },
            new CommunityCardsDealtEvent
            {
                HandId = TestHandId, SequenceNumber = 7,
                Phase = HandPhase.Flop,
                Cards = [new Card(Suit.Hearts, Rank.Ace)],
                BoardState = [new Card(Suit.Hearts, Rank.Ace)]
            },
            new PlayerShowedCardsEvent
            {
                HandId = TestHandId, SequenceNumber = 8,
                PlayerId = TestPlayerId1,
                HoleCards = [new Card(Suit.Hearts, Rank.Ace), new Card(Suit.Spades, Rank.King)],
                HandCategory = HandCategory.Pair, HandDescription = "Pair of Aces",
                HandRanking = 1000, BestFiveCards = [], ShowOrder = 1
            },
            new PlayerMuckedCardsEvent
            {
                HandId = TestHandId, SequenceNumber = 9,
                PlayerId = TestPlayerId2, ShowdownOrder = 2
            },
            new PotAwardedEvent
            {
                HandId = TestHandId, SequenceNumber = 10,
                PotId = Guid.NewGuid(), PotType = PotType.Main, Amount = 100m,
                WinnerIds = [TestPlayerId1], WinnerAmounts = new Dictionary<Guid, decimal> { { TestPlayerId1, 100m } }
            },
            new HandCompletedEvent
            {
                HandId = TestHandId, SequenceNumber = 11,
                TotalPotAmount = 100m, DurationMs = 5000, PlayerCount = 4,
                WentToShowdown = true, FinalPhase = HandPhase.Showdown,
                PlayerResults = new Dictionary<Guid, decimal>(), WinnerIds = [TestPlayerId1]
            }
        };

        // Assert
        foreach (var @event in events)
        {
            Assert.Equal(@event.GetType().Name, @event.EventType);
        }
    }

    #endregion

    #region HandStartedEvent Tests

    [Fact]
    public void HandStartedEvent_BombPotProperties()
    {
        // Arrange & Act
        var @event = new HandStartedEvent
        {
            HandId = TestHandId,
            TableId = TestTableId,
            HandNumber = 5,
            ButtonPosition = 3,
            SmallBlindPosition = 4,
            BigBlindPosition = 5,
            SmallBlindAmount = 1m,
            BigBlindAmount = 2m,
            PlayerIds = [TestPlayerId1, TestPlayerId2],
            IsBombPot = true,
            IsDoubleBoard = true,
            AnteAmount = 5m
        };

        // Assert
        Assert.True(@event.IsBombPot);
        Assert.True(@event.IsDoubleBoard);
        Assert.Equal(5m, @event.AnteAmount);
    }

    #endregion

    #region PlayerActedEvent Tests

    [Theory]
    [InlineData(PlayerActionType.Fold, 0)]
    [InlineData(PlayerActionType.Check, 0)]
    [InlineData(PlayerActionType.Call, 10)]
    [InlineData(PlayerActionType.Raise, 25)]
    [InlineData(PlayerActionType.AllIn, 100)]
    public void PlayerActedEvent_DifferentActionTypes(PlayerActionType actionType, decimal amount)
    {
        // Arrange & Act
        var @event = new PlayerActedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 5,
            PlayerId = TestPlayerId1,
            ActionType = actionType,
            Amount = amount,
            Phase = HandPhase.Preflop,
            RemainingStack = 100m - amount,
            PotTotal = 50m + amount,
            CurrentBetLevel = amount > 0 ? amount : 0
        };

        // Assert
        Assert.Equal(actionType, @event.ActionType);
        Assert.Equal(amount, @event.Amount);
    }

    [Fact]
    public void PlayerActedEvent_TimeoutFlag()
    {
        // Arrange & Act
        var @event = new PlayerActedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 5,
            PlayerId = TestPlayerId1,
            ActionType = PlayerActionType.Fold,
            Amount = 0m,
            Phase = HandPhase.Flop,
            RemainingStack = 80m,
            PotTotal = 40m,
            CurrentBetLevel = 10m,
            IsTimeout = true
        };

        // Assert
        Assert.True(@event.IsTimeout);
    }

    #endregion

    #region CommunityCardsDealtEvent Tests

    [Theory]
    [InlineData(HandPhase.Flop, 3)]
    [InlineData(HandPhase.Turn, 1)]
    [InlineData(HandPhase.River, 1)]
    public void CommunityCardsDealtEvent_CorrectCardCounts(HandPhase phase, int expectedCards)
    {
        // Arrange
        var cards = Enumerable.Range(0, expectedCards)
            .Select(i => new Card(Suit.Hearts, (Rank)(i + 2)))
            .ToList();

        var boardState = phase == HandPhase.Flop
            ? cards
            : cards.Concat([new Card(Suit.Clubs, Rank.Ace)]).ToList();

        // Act
        var @event = new CommunityCardsDealtEvent
        {
            HandId = TestHandId,
            SequenceNumber = 7,
            Phase = phase,
            Cards = cards,
            BoardState = boardState
        };

        // Assert
        Assert.Equal(expectedCards, @event.Cards.Count);
        Assert.Equal(phase, @event.Phase);
    }

    [Fact]
    public void CommunityCardsDealtEvent_DoubleBoardIndex()
    {
        // Arrange & Act
        var @event = new CommunityCardsDealtEvent
        {
            HandId = TestHandId,
            SequenceNumber = 7,
            Phase = HandPhase.Flop,
            Cards = [new Card(Suit.Hearts, Rank.Ace)],
            BoardState = [new Card(Suit.Hearts, Rank.Ace)],
            BoardIndex = 1
        };

        // Assert
        Assert.Equal(1, @event.BoardIndex);
    }

    #endregion

    #region PotAwardedEvent Tests

    [Fact]
    public void PotAwardedEvent_SingleWinner()
    {
        // Arrange & Act
        var winnerId = Guid.NewGuid();
        var @event = new PotAwardedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 10,
            PotId = Guid.NewGuid(),
            PotType = PotType.Main,
            Amount = 100m,
            WinnerIds = [winnerId],
            WinnerAmounts = new Dictionary<Guid, decimal> { { winnerId, 100m } },
            WinningHandDescription = "Full House, Aces over Kings"
        };

        // Assert
        Assert.Single(@event.WinnerIds);
        Assert.Equal(100m, @event.WinnerAmounts[winnerId]);
        Assert.False(@event.WonByFold);
    }

    [Fact]
    public void PotAwardedEvent_SplitPotEven()
    {
        // Arrange
        var winner1 = Guid.NewGuid();
        var winner2 = Guid.NewGuid();

        // Act
        var @event = new PotAwardedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 10,
            PotId = Guid.NewGuid(),
            PotType = PotType.Main,
            Amount = 100m,
            WinnerIds = [winner1, winner2],
            WinnerAmounts = new Dictionary<Guid, decimal>
            {
                { winner1, 50m },
                { winner2, 50m }
            },
            WinningHandDescription = "Straight, Ace high"
        };

        // Assert
        Assert.Equal(2, @event.WinnerIds.Count);
        Assert.Equal(50m, @event.WinnerAmounts[winner1]);
        Assert.Equal(50m, @event.WinnerAmounts[winner2]);
    }

    [Fact]
    public void PotAwardedEvent_SplitPotOddChip()
    {
        // Arrange - Odd chip goes to player in earliest position
        var winner1 = Guid.NewGuid();
        var winner2 = Guid.NewGuid();

        // Act
        var @event = new PotAwardedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 10,
            PotId = Guid.NewGuid(),
            PotType = PotType.Main,
            Amount = 101m,
            WinnerIds = [winner1, winner2],
            WinnerAmounts = new Dictionary<Guid, decimal>
            {
                { winner1, 51m }, // Gets odd chip (earliest position)
                { winner2, 50m }
            },
            WinningHandDescription = "Two Pair, Aces and Kings"
        };

        // Assert
        Assert.Equal(101m, @event.Amount);
        Assert.Equal(51m, @event.WinnerAmounts[winner1]);
        Assert.Equal(50m, @event.WinnerAmounts[winner2]);
        Assert.Equal(101m, @event.WinnerAmounts.Values.Sum());
    }

    [Fact]
    public void PotAwardedEvent_WonByFold()
    {
        // Arrange & Act
        var winnerId = Guid.NewGuid();
        var @event = new PotAwardedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 10,
            PotId = Guid.NewGuid(),
            PotType = PotType.Main,
            Amount = 15m,
            WinnerIds = [winnerId],
            WinnerAmounts = new Dictionary<Guid, decimal> { { winnerId, 15m } },
            WonByFold = true
        };

        // Assert
        Assert.True(@event.WonByFold);
        Assert.Null(@event.WinningHandDescription);
    }

    [Fact]
    public void PotAwardedEvent_SidePot()
    {
        // Arrange & Act
        var winnerId = Guid.NewGuid();
        var @event = new PotAwardedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 11,
            PotId = Guid.NewGuid(),
            PotType = PotType.Side,
            Amount = 50m,
            WinnerIds = [winnerId],
            WinnerAmounts = new Dictionary<Guid, decimal> { { winnerId, 50m } },
            WinningHandDescription = "Flush, King high"
        };

        // Assert
        Assert.Equal(PotType.Side, @event.PotType);
    }

    #endregion

    #region HandCompletedEvent Tests

    [Fact]
    public void HandCompletedEvent_WithShowdown()
    {
        // Arrange
        var winner = Guid.NewGuid();
        var loser = Guid.NewGuid();

        // Act
        var @event = new HandCompletedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 15,
            TotalPotAmount = 200m,
            DurationMs = 45000,
            PlayerCount = 6,
            WentToShowdown = true,
            FinalPhase = HandPhase.Showdown,
            PlayerResults = new Dictionary<Guid, decimal>
            {
                { winner, 150m },  // Net gain
                { loser, -50m }   // Net loss
            },
            WinnerIds = [winner]
        };

        // Assert
        Assert.True(@event.WentToShowdown);
        Assert.Equal(HandPhase.Showdown, @event.FinalPhase);
        Assert.Equal(150m, @event.PlayerResults[winner]);
        Assert.Equal(-50m, @event.PlayerResults[loser]);
    }

    [Fact]
    public void HandCompletedEvent_WithoutShowdown()
    {
        // Arrange
        var winner = Guid.NewGuid();

        // Act
        var @event = new HandCompletedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 8,
            TotalPotAmount = 15m,
            DurationMs = 12000,
            PlayerCount = 4,
            WentToShowdown = false,
            FinalPhase = HandPhase.Flop,
            PlayerResults = new Dictionary<Guid, decimal>
            {
                { winner, 10m }
            },
            WinnerIds = [winner]
        };

        // Assert
        Assert.False(@event.WentToShowdown);
        Assert.Equal(HandPhase.Flop, @event.FinalPhase);
    }

    #endregion

    #region HoleCardsDealtEvent Tests

    [Fact]
    public void HoleCardsDealtEvent_StoresPlayerCards()
    {
        // Arrange
        var player1Cards = new Card[] { new(Suit.Hearts, Rank.Ace), new(Suit.Spades, Rank.King) };
        var player2Cards = new Card[] { new(Suit.Clubs, Rank.Queen), new(Suit.Diamonds, Rank.Jack) };

        // Act
        var @event = new HoleCardsDealtEvent
        {
            HandId = TestHandId,
            SequenceNumber = 3,
            PlayerCards = new Dictionary<Guid, Card[]>
            {
                { TestPlayerId1, player1Cards },
                { TestPlayerId2, player2Cards }
            }
        };

        // Assert
        Assert.Equal(2, @event.PlayerCards.Count);
        Assert.Equal(player1Cards, @event.PlayerCards[TestPlayerId1]);
        Assert.Equal(player2Cards, @event.PlayerCards[TestPlayerId2]);
    }

    #endregion
}
