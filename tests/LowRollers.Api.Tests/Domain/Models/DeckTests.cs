using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.Services;

namespace LowRollers.Api.Tests.Domain.Models;

public class DeckTests
{
    [Fact]
    public void Constructor_Creates52Cards()
    {
        // Arrange & Act
        var deck = new Deck();

        // Assert
        Assert.Equal(52, deck.CardsRemaining);
    }

    [Fact]
    public void Constructor_WithShuffleService_Creates52Cards()
    {
        // Arrange
        var shuffleService = new ShuffleService();

        // Act
        var deck = new Deck(shuffleService);

        // Assert
        Assert.Equal(52, deck.CardsRemaining);
    }

    [Fact]
    public void Constructor_ThrowsOnNullShuffleService()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Deck(null!));
    }

    [Fact]
    public void Cards_ContainsAll52UniqueCards()
    {
        // Arrange
        var deck = new Deck();

        // Act
        var cards = deck.Cards;

        // Assert
        Assert.Equal(52, cards.Count);
        Assert.Equal(52, cards.Distinct().Count());

        // Verify each suit has 13 cards
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            Assert.Equal(13, cards.Count(c => c.Suit == suit));
        }

        // Verify each rank appears 4 times
        foreach (Rank rank in Enum.GetValues<Rank>())
        {
            Assert.Equal(4, cards.Count(c => c.Rank == rank));
        }
    }

    [Fact]
    public void Shuffle_ChangesCardOrder()
    {
        // Arrange
        var deck = new Deck();
        var originalOrder = deck.Cards.ToList();

        // Act
        deck.Shuffle();

        // Assert - Very unlikely to remain in same order
        Assert.NotEqual(originalOrder, deck.Cards);
    }

    [Fact]
    public void Shuffle_PreservesAllCards()
    {
        // Arrange
        var deck = new Deck();
        var originalCards = deck.Cards.ToHashSet();

        // Act
        deck.Shuffle();

        // Assert
        Assert.Equal(52, deck.Cards.Count);
        Assert.True(deck.Cards.All(originalCards.Contains));
    }

    [Fact]
    public void Shuffle_ResetsDealPosition()
    {
        // Arrange
        var deck = new Deck();
        deck.Shuffle();
        deck.Deal(10); // Deal some cards
        Assert.Equal(42, deck.CardsRemaining);

        // Act
        deck.Shuffle();

        // Assert
        Assert.Equal(52, deck.CardsRemaining);
    }

    [Fact]
    public void Deal_ReturnsSingleCard()
    {
        // Arrange
        var deck = new Deck();
        deck.Shuffle();
        var initialCount = deck.CardsRemaining;

        // Act
        var card = deck.Deal();

        // Assert
        Assert.Equal(initialCount - 1, deck.CardsRemaining);
    }

    [Fact]
    public void Deal_ReturnsCardsInOrder()
    {
        // Arrange
        var deck = new Deck();
        deck.Shuffle();
        var expectedCards = deck.Cards.Take(5).ToList();

        // Act
        var dealtCards = new List<Card>();
        for (int i = 0; i < 5; i++)
        {
            dealtCards.Add(deck.Deal());
        }

        // Assert
        Assert.Equal(expectedCards, dealtCards);
    }

    [Fact]
    public void Deal_ThrowsWhenNoCardsRemaining()
    {
        // Arrange
        var deck = new Deck();
        deck.Shuffle();
        deck.Deal(52); // Deal all cards

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => deck.Deal());
        Assert.Contains("No cards remaining", exception.Message);
    }

    [Fact]
    public void DealMultiple_ReturnsRequestedNumberOfCards()
    {
        // Arrange
        var deck = new Deck();
        deck.Shuffle();

        // Act
        var cards = deck.Deal(5);

        // Assert
        Assert.Equal(5, cards.Length);
        Assert.Equal(47, deck.CardsRemaining);
    }

    [Fact]
    public void DealMultiple_ThrowsWhenNotEnoughCards()
    {
        // Arrange
        var deck = new Deck();
        deck.Shuffle();
        deck.Deal(50); // Leave only 2 cards

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => deck.Deal(5));
        Assert.Contains("Cannot deal 5 cards", exception.Message);
        Assert.Contains("2 remaining", exception.Message);
    }

    [Fact]
    public void Burn_RemovesOneCard()
    {
        // Arrange
        var deck = new Deck();
        deck.Shuffle();
        var initialCount = deck.CardsRemaining;

        // Act
        deck.Burn();

        // Assert
        Assert.Equal(initialCount - 1, deck.CardsRemaining);
    }

    [Fact]
    public void Reset_RestoresToOriginalState()
    {
        // Arrange
        var deck = new Deck();
        var originalCards = deck.Cards.ToList();
        deck.Shuffle();
        deck.Deal(20); // Deal some cards

        // Act
        deck.Reset();

        // Assert
        Assert.Equal(52, deck.CardsRemaining);
        Assert.Equal(originalCards, deck.Cards);
    }

    [Fact]
    public void DealFullHand_WorksForTexasHoldem()
    {
        // Arrange
        var deck = new Deck();
        deck.Shuffle();
        const int numPlayers = 9;

        // Act - Simulate dealing: 2 hole cards per player + 5 community + 3 burns
        var holeCards = new Card[numPlayers][];
        for (int i = 0; i < numPlayers; i++)
        {
            holeCards[i] = deck.Deal(2);
        }

        deck.Burn(); // Pre-flop burn
        var flop = deck.Deal(3);

        deck.Burn(); // Pre-turn burn
        var turn = deck.Deal(1);

        deck.Burn(); // Pre-river burn
        var river = deck.Deal(1);

        // Assert
        // 9 players * 2 hole cards = 18
        // + 5 community cards (3 flop + 1 turn + 1 river)
        // + 3 burns
        // = 26 cards dealt, 26 remaining
        Assert.Equal(52 - (numPlayers * 2) - 5 - 3, deck.CardsRemaining); // 26 remaining
        Assert.All(holeCards, h => Assert.Equal(2, h.Length));
        Assert.Equal(3, flop.Length);
        Assert.Single(turn);
        Assert.Single(river);

        // All visible dealt cards should be unique (excluding burns)
        var allDealtCards = holeCards.SelectMany(h => h)
            .Concat(flop)
            .Concat(turn)
            .Concat(river);
        // 18 hole cards + 5 community = 23 visible cards
        Assert.Equal(23, allDealtCards.Distinct().Count());
    }
}
