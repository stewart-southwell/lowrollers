using LowRollers.Api.Domain.Evaluation;
using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Tests.Domain.Evaluation;

public class HandEvaluationServiceTests
{
    private readonly HandEvaluationService _service = new();

    #region Helper Methods

    private static Card C(Rank rank, Suit suit) => new(suit, rank);

    private static List<Card> CreateRoyalFlush() =>
    [
        C(Rank.Ace, Suit.Hearts),
        C(Rank.King, Suit.Hearts),
        C(Rank.Queen, Suit.Hearts),
        C(Rank.Jack, Suit.Hearts),
        C(Rank.Ten, Suit.Hearts)
    ];

    private static List<Card> CreateStraightFlush() =>
    [
        C(Rank.Nine, Suit.Spades),
        C(Rank.Eight, Suit.Spades),
        C(Rank.Seven, Suit.Spades),
        C(Rank.Six, Suit.Spades),
        C(Rank.Five, Suit.Spades)
    ];

    private static List<Card> CreateFourOfAKind() =>
    [
        C(Rank.King, Suit.Hearts),
        C(Rank.King, Suit.Diamonds),
        C(Rank.King, Suit.Clubs),
        C(Rank.King, Suit.Spades),
        C(Rank.Two, Suit.Hearts)
    ];

    private static List<Card> CreateFullHouse() =>
    [
        C(Rank.Jack, Suit.Hearts),
        C(Rank.Jack, Suit.Diamonds),
        C(Rank.Jack, Suit.Clubs),
        C(Rank.Seven, Suit.Hearts),
        C(Rank.Seven, Suit.Spades)
    ];

    private static List<Card> CreateFlush() =>
    [
        C(Rank.Ace, Suit.Diamonds),
        C(Rank.Ten, Suit.Diamonds),
        C(Rank.Seven, Suit.Diamonds),
        C(Rank.Four, Suit.Diamonds),
        C(Rank.Two, Suit.Diamonds)
    ];

    private static List<Card> CreateStraight() =>
    [
        C(Rank.Ten, Suit.Hearts),
        C(Rank.Nine, Suit.Diamonds),
        C(Rank.Eight, Suit.Clubs),
        C(Rank.Seven, Suit.Spades),
        C(Rank.Six, Suit.Hearts)
    ];

    private static List<Card> CreateThreeOfAKind() =>
    [
        C(Rank.Queen, Suit.Hearts),
        C(Rank.Queen, Suit.Diamonds),
        C(Rank.Queen, Suit.Clubs),
        C(Rank.Eight, Suit.Spades),
        C(Rank.Two, Suit.Hearts)
    ];

    private static List<Card> CreateTwoPair() =>
    [
        C(Rank.Nine, Suit.Hearts),
        C(Rank.Nine, Suit.Diamonds),
        C(Rank.Five, Suit.Clubs),
        C(Rank.Five, Suit.Spades),
        C(Rank.King, Suit.Hearts)
    ];

    private static List<Card> CreatePair() =>
    [
        C(Rank.Ace, Suit.Hearts),
        C(Rank.Ace, Suit.Diamonds),
        C(Rank.King, Suit.Clubs),
        C(Rank.Queen, Suit.Spades),
        C(Rank.Jack, Suit.Hearts)
    ];

    private static List<Card> CreateHighCard() =>
    [
        C(Rank.Ace, Suit.Hearts),
        C(Rank.King, Suit.Diamonds),
        C(Rank.Ten, Suit.Clubs),
        C(Rank.Five, Suit.Spades),
        C(Rank.Two, Suit.Hearts)
    ];

    private static List<Card> CreateWheelStraight() =>
    [
        C(Rank.Ace, Suit.Hearts),
        C(Rank.Two, Suit.Diamonds),
        C(Rank.Three, Suit.Clubs),
        C(Rank.Four, Suit.Spades),
        C(Rank.Five, Suit.Hearts)
    ];

    #endregion

    #region Hand Category Tests

    [Fact]
    public void Evaluate_RoyalFlush_ReturnsCorrectCategory()
    {
        var cards = CreateRoyalFlush();

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.RoyalFlush, result.Category);
    }

    [Fact]
    public void Evaluate_StraightFlush_ReturnsCorrectCategory()
    {
        var cards = CreateStraightFlush();

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.StraightFlush, result.Category);
    }

    [Fact]
    public void Evaluate_FourOfAKind_ReturnsCorrectCategory()
    {
        var cards = CreateFourOfAKind();

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.FourOfAKind, result.Category);
    }

    [Fact]
    public void Evaluate_FullHouse_ReturnsCorrectCategory()
    {
        var cards = CreateFullHouse();

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.FullHouse, result.Category);
    }

    [Fact]
    public void Evaluate_Flush_ReturnsCorrectCategory()
    {
        var cards = CreateFlush();

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.Flush, result.Category);
    }

    [Fact]
    public void Evaluate_Straight_ReturnsCorrectCategory()
    {
        var cards = CreateStraight();

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.Straight, result.Category);
    }

    [Fact]
    public void Evaluate_ThreeOfAKind_ReturnsCorrectCategory()
    {
        var cards = CreateThreeOfAKind();

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.ThreeOfAKind, result.Category);
    }

    [Fact]
    public void Evaluate_TwoPair_ReturnsCorrectCategory()
    {
        var cards = CreateTwoPair();

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.TwoPair, result.Category);
    }

    [Fact]
    public void Evaluate_Pair_ReturnsCorrectCategory()
    {
        var cards = CreatePair();

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.Pair, result.Category);
    }

    [Fact]
    public void Evaluate_HighCard_ReturnsCorrectCategory()
    {
        var cards = CreateHighCard();

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.HighCard, result.Category);
    }

    [Fact]
    public void Evaluate_WheelStraight_ReturnsCorrectCategory()
    {
        var cards = CreateWheelStraight();

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.Straight, result.Category);
    }

    #endregion

    #region Hand Ranking Comparison Tests

    [Fact]
    public void Evaluate_RoyalFlush_HasBetterRankingThanStraightFlush()
    {
        var royalFlush = _service.Evaluate(CreateRoyalFlush());
        var straightFlush = _service.Evaluate(CreateStraightFlush());

        Assert.True(royalFlush.Beats(straightFlush));
    }

    [Fact]
    public void Evaluate_StraightFlush_HasBetterRankingThanFourOfAKind()
    {
        var straightFlush = _service.Evaluate(CreateStraightFlush());
        var fourOfAKind = _service.Evaluate(CreateFourOfAKind());

        Assert.True(straightFlush.Beats(fourOfAKind));
    }

    [Fact]
    public void Evaluate_FourOfAKind_HasBetterRankingThanFullHouse()
    {
        var fourOfAKind = _service.Evaluate(CreateFourOfAKind());
        var fullHouse = _service.Evaluate(CreateFullHouse());

        Assert.True(fourOfAKind.Beats(fullHouse));
    }

    [Fact]
    public void Evaluate_FullHouse_HasBetterRankingThanFlush()
    {
        var fullHouse = _service.Evaluate(CreateFullHouse());
        var flush = _service.Evaluate(CreateFlush());

        Assert.True(fullHouse.Beats(flush));
    }

    [Fact]
    public void Evaluate_Flush_HasBetterRankingThanStraight()
    {
        var flush = _service.Evaluate(CreateFlush());
        var straight = _service.Evaluate(CreateStraight());

        Assert.True(flush.Beats(straight));
    }

    [Fact]
    public void Evaluate_Straight_HasBetterRankingThanThreeOfAKind()
    {
        var straight = _service.Evaluate(CreateStraight());
        var threeOfAKind = _service.Evaluate(CreateThreeOfAKind());

        Assert.True(straight.Beats(threeOfAKind));
    }

    [Fact]
    public void Evaluate_ThreeOfAKind_HasBetterRankingThanTwoPair()
    {
        var threeOfAKind = _service.Evaluate(CreateThreeOfAKind());
        var twoPair = _service.Evaluate(CreateTwoPair());

        Assert.True(threeOfAKind.Beats(twoPair));
    }

    [Fact]
    public void Evaluate_TwoPair_HasBetterRankingThanPair()
    {
        var twoPair = _service.Evaluate(CreateTwoPair());
        var pair = _service.Evaluate(CreatePair());

        Assert.True(twoPair.Beats(pair));
    }

    [Fact]
    public void Evaluate_Pair_HasBetterRankingThanHighCard()
    {
        var pair = _service.Evaluate(CreatePair());
        var highCard = _service.Evaluate(CreateHighCard());

        Assert.True(pair.Beats(highCard));
    }

    #endregion

    #region Seven Card Evaluation Tests

    [Fact]
    public void Evaluate_SevenCards_FindsBestFiveCardHand()
    {
        // Royal flush hidden in 7 cards
        var cards = new List<Card>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Jack, Suit.Hearts),
            C(Rank.Ten, Suit.Hearts),
            C(Rank.Two, Suit.Clubs),      // Extra card
            C(Rank.Three, Suit.Diamonds)  // Extra card
        };

        var result = _service.Evaluate(cards);

        Assert.Equal(HandCategory.RoyalFlush, result.Category);
    }

    [Fact]
    public void Evaluate_WithHoleCardsAndCommunity_EvaluatesCorrectly()
    {
        var holeCards = new List<Card>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Hearts)
        };

        var communityCards = new List<Card>
        {
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Jack, Suit.Hearts),
            C(Rank.Ten, Suit.Hearts)
        };

        var result = _service.Evaluate(holeCards, communityCards);

        Assert.Equal(HandCategory.RoyalFlush, result.Category);
    }

    #endregion

    #region Winner Determination Tests

    [Fact]
    public void DetermineWinners_SingleWinner_ReturnsOneHand()
    {
        var royalFlush = _service.Evaluate(CreateRoyalFlush());
        var straightFlush = _service.Evaluate(CreateStraightFlush());
        var fullHouse = _service.Evaluate(CreateFullHouse());

        var winners = _service.DetermineWinners([royalFlush, straightFlush, fullHouse]);

        Assert.Single(winners);
        Assert.Equal(HandCategory.RoyalFlush, winners[0].Category);
    }

    [Fact]
    public void DetermineWinners_TiedHands_ReturnsMultipleWinners()
    {
        // Two identical pairs (both pair of aces with same kickers)
        var pair1 = new List<Card>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Ace, Suit.Diamonds),
            C(Rank.King, Suit.Clubs),
            C(Rank.Queen, Suit.Spades),
            C(Rank.Jack, Suit.Hearts)
        };

        var pair2 = new List<Card>
        {
            C(Rank.Ace, Suit.Clubs),
            C(Rank.Ace, Suit.Spades),
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Diamonds),
            C(Rank.Jack, Suit.Clubs)
        };

        var evaluated1 = _service.Evaluate(pair1);
        var evaluated2 = _service.Evaluate(pair2);

        var winners = _service.DetermineWinners([evaluated1, evaluated2]);

        Assert.Equal(2, winners.Count);
    }

    [Fact]
    public void DetermineWinners_EmptyList_ReturnsEmptyList()
    {
        var winners = _service.DetermineWinners([]);

        Assert.Empty(winners);
    }

    #endregion

    #region RankHands Tests

    [Fact]
    public void RankHands_OrdersByRanking_BestFirst()
    {
        var highCard = _service.Evaluate(CreateHighCard());
        var pair = _service.Evaluate(CreatePair());
        var royalFlush = _service.Evaluate(CreateRoyalFlush());

        var ranked = _service.RankHands([highCard, pair, royalFlush]);

        Assert.Equal(HandCategory.RoyalFlush, ranked[0].Category);
        Assert.Equal(HandCategory.Pair, ranked[1].Category);
        Assert.Equal(HandCategory.HighCard, ranked[2].Category);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Evaluate_TooFewCards_ThrowsArgumentException()
    {
        var cards = new List<Card>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Jack, Suit.Hearts)
        };

        Assert.Throws<ArgumentException>(() => _service.Evaluate(cards));
    }

    [Fact]
    public void Evaluate_TooManyCards_ThrowsArgumentException()
    {
        var cards = new List<Card>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Jack, Suit.Hearts),
            C(Rank.Ten, Suit.Hearts),
            C(Rank.Nine, Suit.Hearts),
            C(Rank.Eight, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts)
        };

        Assert.Throws<ArgumentException>(() => _service.Evaluate(cards));
    }

    [Fact]
    public void Evaluate_WrongHoleCardCount_ThrowsArgumentException()
    {
        var holeCards = new List<Card>
        {
            C(Rank.Ace, Suit.Hearts)  // Only 1 hole card
        };

        var communityCards = new List<Card>
        {
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Jack, Suit.Hearts),
            C(Rank.Ten, Suit.Hearts)
        };

        Assert.Throws<ArgumentException>(() => _service.Evaluate(holeCards, communityCards));
    }

    [Fact]
    public void Evaluate_WrongCommunityCardCount_ThrowsArgumentException()
    {
        var holeCards = new List<Card>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Hearts)
        };

        var communityCards = new List<Card>
        {
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Jack, Suit.Hearts)  // Only 2 community cards
        };

        Assert.Throws<ArgumentException>(() => _service.Evaluate(holeCards, communityCards));
    }

    #endregion

    #region Description Tests

    [Fact]
    public void Evaluate_ReturnsNonEmptyDescription()
    {
        var cards = CreateRoyalFlush();

        var result = _service.Evaluate(cards);

        Assert.False(string.IsNullOrEmpty(result.Description));
    }

    [Fact]
    public void Evaluate_DescriptionContainsHandInfo()
    {
        var cards = CreateFullHouse();

        var result = _service.Evaluate(cards);

        // Description should mention the hand details (e.g., "Jacks Full over Sevens")
        // The library uses descriptive names like "X Full over Y" for full houses
        Assert.True(
            result.Description.Contains("Full", StringComparison.OrdinalIgnoreCase) ||
            result.Description.Contains("Jacks", StringComparison.OrdinalIgnoreCase),
            $"Expected description to contain hand info, but got: {result.Description}");
    }

    #endregion

    #region GetRanking Tests

    [Fact]
    public void GetRanking_ReturnsConsistentValue()
    {
        var cards = CreateFlush();

        var ranking1 = _service.GetRanking(cards);
        var ranking2 = _service.GetRanking(cards);

        Assert.Equal(ranking1, ranking2);
    }

    [Fact]
    public void GetRanking_BetterHandHasLowerRanking()
    {
        var royalFlushRanking = _service.GetRanking(CreateRoyalFlush());
        var highCardRanking = _service.GetRanking(CreateHighCard());

        Assert.True(royalFlushRanking < highCardRanking);
    }

    #endregion
}
