namespace LowRollers.Api.Domain.Models;

/// <summary>
/// Represents the four suits in a standard deck of cards.
/// </summary>
public enum Suit
{
    Clubs = 0,
    Diamonds = 1,
    Hearts = 2,
    Spades = 3
}

/// <summary>
/// Represents card ranks from Two (lowest) to Ace (highest).
/// Values are 2-14 for easy comparison.
/// </summary>
public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}

/// <summary>
/// Represents a playing card with a suit and rank.
/// Immutable record type for value equality.
/// </summary>
/// <param name="Suit">The suit of the card (Clubs, Diamonds, Hearts, Spades).</param>
/// <param name="Rank">The rank of the card (Two through Ace).</param>
public readonly record struct Card(Suit Suit, Rank Rank)
{
    /// <summary>
    /// Returns a short string representation of the card (e.g., "As" for Ace of Spades).
    /// </summary>
    public override string ToString()
    {
        var rankChar = Rank switch
        {
            Rank.Ten => "T",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            Rank.Ace => "A",
            _ => ((int)Rank).ToString()
        };

        var suitChar = Suit switch
        {
            Suit.Clubs => "c",
            Suit.Diamonds => "d",
            Suit.Hearts => "h",
            Suit.Spades => "s",
            _ => "?"
        };

        return $"{rankChar}{suitChar}";
    }

    /// <summary>
    /// Returns a display-friendly name (e.g., "Ace of Spades").
    /// </summary>
    public string ToDisplayString() => $"{Rank} of {Suit}";
}
