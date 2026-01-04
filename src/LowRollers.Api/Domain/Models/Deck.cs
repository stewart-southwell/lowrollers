using LowRollers.Api.Domain.Services;

namespace LowRollers.Api.Domain.Models;

/// <summary>
/// Represents a standard 52-card deck with cryptographically secure shuffling.
/// </summary>
public sealed class Deck
{
    private readonly List<Card> _cards;
    private readonly IShuffleService _shuffleService;
    private int _dealIndex;

    /// <summary>
    /// Creates a new deck with all 52 cards in order, using the provided shuffle service.
    /// </summary>
    /// <param name="shuffleService">Service for cryptographic shuffling.</param>
    public Deck(IShuffleService shuffleService)
    {
        ArgumentNullException.ThrowIfNull(shuffleService);
        _shuffleService = shuffleService;
        _cards = new List<Card>(52);
        InitializeCards();
    }

    /// <summary>
    /// Creates a new deck with all 52 cards in order, using the default shuffle service.
    /// </summary>
    public Deck() : this(new ShuffleService())
    {
    }

    /// <summary>
    /// Gets the number of cards remaining in the deck.
    /// </summary>
    public int CardsRemaining => _cards.Count - _dealIndex;

    /// <summary>
    /// Gets a read-only view of all cards in current order (for verification/testing).
    /// </summary>
    public IReadOnlyList<Card> Cards => _cards.AsReadOnly();

    /// <summary>
    /// Shuffles the deck using Fisher-Yates algorithm with cryptographically secure RNG.
    /// Resets the deal position to the top of the deck.
    /// </summary>
    public void Shuffle()
    {
        _dealIndex = 0;
        _shuffleService.Shuffle(_cards);
    }

    /// <summary>
    /// Deals the next card from the deck.
    /// </summary>
    /// <returns>The next card.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no cards remain.</exception>
    public Card Deal()
    {
        if (_dealIndex >= _cards.Count)
        {
            throw new InvalidOperationException("No cards remaining in deck.");
        }

        return _cards[_dealIndex++];
    }

    /// <summary>
    /// Deals multiple cards from the deck.
    /// </summary>
    /// <param name="count">Number of cards to deal.</param>
    /// <returns>Array of dealt cards.</returns>
    /// <exception cref="InvalidOperationException">Thrown when not enough cards remain.</exception>
    public Card[] Deal(int count)
    {
        if (count > CardsRemaining)
        {
            throw new InvalidOperationException($"Cannot deal {count} cards. Only {CardsRemaining} remaining.");
        }

        var cards = new Card[count];
        for (int i = 0; i < count; i++)
        {
            cards[i] = Deal();
        }
        return cards;
    }

    /// <summary>
    /// Burns a card (deals and discards it).
    /// Standard poker practice before dealing community cards.
    /// </summary>
    public void Burn() => Deal();

    /// <summary>
    /// Resets the deck to its initial unshuffled state.
    /// </summary>
    public void Reset()
    {
        _cards.Clear();
        InitializeCards();
        _dealIndex = 0;
    }

    private void InitializeCards()
    {
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
            {
                _cards.Add(new Card(suit, rank));
            }
        }
    }
}
