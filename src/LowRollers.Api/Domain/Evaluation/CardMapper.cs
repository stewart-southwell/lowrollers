using LowRollers.Api.Domain.Models;
using HoldemCards = HoldemPoker.Cards;

namespace LowRollers.Api.Domain.Evaluation;

/// <summary>
/// Maps between domain Card models and HoldemPoker.Cards types.
/// </summary>
public static class CardMapper
{
    /// <summary>
    /// Converts a domain Card to HoldemPoker.Cards.Card.
    /// Uses string parsing which is the primary API of HoldemPoker.Cards.
    /// </summary>
    /// <param name="card">The domain card to convert.</param>
    /// <returns>The equivalent HoldemPoker.Cards.Card.</returns>
    public static HoldemCards.Card ToEvaluatorCard(Card card)
    {
        // Build the notation string (e.g., "Ah" for Ace of hearts)
        // Our Card.ToString() returns format like "As" for Ace of spades
        var notation = card.ToString();
        return HoldemCards.Card.Parse(notation);
    }

    /// <summary>
    /// Converts an array of domain Cards to HoldemPoker.Cards.Card array.
    /// </summary>
    /// <param name="cards">The domain cards to convert.</param>
    /// <returns>Array of equivalent HoldemPoker.Cards.Card instances.</returns>
    public static HoldemCards.Card[] ToEvaluatorCards(IEnumerable<Card> cards)
    {
        return cards.Select(ToEvaluatorCard).ToArray();
    }
}
