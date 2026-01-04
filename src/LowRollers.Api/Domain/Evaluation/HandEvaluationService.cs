using LowRollers.Api.Domain.Models;
using HoldemPoker.Evaluator;
using HoldemCards = HoldemPoker.Cards;

namespace LowRollers.Api.Domain.Evaluation;

/// <summary>
/// Service for evaluating poker hands using the HoldemPoker.Evaluator library.
/// Provides a clean wrapper around the library with domain-specific types.
/// </summary>
public class HandEvaluationService : IHandEvaluationService
{
    /// <summary>
    /// Evaluates a poker hand from 5-7 cards.
    /// </summary>
    /// <param name="cards">The cards to evaluate (5-7 cards for Texas Hold'em).</param>
    /// <returns>The evaluated hand with ranking, category, and description.</returns>
    /// <exception cref="ArgumentException">Thrown when cards count is not between 5 and 7.</exception>
    public EvaluatedHand Evaluate(IReadOnlyList<Card> cards)
    {
        if (cards.Count < 5 || cards.Count > 7)
        {
            throw new ArgumentException(
                $"Hand evaluation requires 5-7 cards, but {cards.Count} were provided.",
                nameof(cards));
        }

        var evaluatorCards = CardMapper.ToEvaluatorCards(cards);

        var ranking = HoldemHandEvaluator.GetHandRanking(evaluatorCards);
        var libraryCategory = HoldemHandEvaluator.GetHandCategory(evaluatorCards);
        var description = HoldemHandEvaluator.GetHandDescription(evaluatorCards);

        var category = MapCategory(libraryCategory);

        return new EvaluatedHand(ranking, category, description, cards);
    }

    /// <summary>
    /// Evaluates a Texas Hold'em hand from hole cards and community cards.
    /// </summary>
    /// <param name="holeCards">The player's two hole cards.</param>
    /// <param name="communityCards">The community cards (3-5 cards).</param>
    /// <returns>The evaluated hand with ranking, category, and description.</returns>
    public EvaluatedHand Evaluate(IReadOnlyList<Card> holeCards, IReadOnlyList<Card> communityCards)
    {
        if (holeCards.Count != 2)
        {
            throw new ArgumentException(
                $"Texas Hold'em requires exactly 2 hole cards, but {holeCards.Count} were provided.",
                nameof(holeCards));
        }

        if (communityCards.Count < 3 || communityCards.Count > 5)
        {
            throw new ArgumentException(
                $"Community cards must be 3-5 cards, but {communityCards.Count} were provided.",
                nameof(communityCards));
        }

        var allCards = holeCards.Concat(communityCards).ToList();
        return Evaluate(allCards);
    }

    /// <summary>
    /// Compares multiple hands and returns them ordered by strength (strongest first).
    /// Hands with equal ranking are grouped together (for split pot scenarios).
    /// </summary>
    /// <param name="hands">The hands to compare.</param>
    /// <returns>Hands ordered by strength, strongest first.</returns>
    public IReadOnlyList<EvaluatedHand> RankHands(IEnumerable<EvaluatedHand> hands)
    {
        return hands.OrderBy(h => h.Ranking).ToList();
    }

    /// <summary>
    /// Determines the winner(s) from a set of hands.
    /// Returns multiple hands in case of a tie (split pot).
    /// </summary>
    /// <param name="hands">The hands to compare.</param>
    /// <returns>The winning hand(s). Multiple hands indicate a split pot.</returns>
    public IReadOnlyList<EvaluatedHand> DetermineWinners(IEnumerable<EvaluatedHand> hands)
    {
        var handList = hands.ToList();
        if (handList.Count == 0)
        {
            return [];
        }

        var bestRanking = handList.Min(h => h.Ranking);
        return handList.Where(h => h.Ranking == bestRanking).ToList();
    }

    /// <summary>
    /// Gets just the ranking integer for quick comparison without full evaluation overhead.
    /// </summary>
    /// <param name="cards">The cards to evaluate.</param>
    /// <returns>The ranking integer (lower = better).</returns>
    public int GetRanking(IReadOnlyList<Card> cards)
    {
        var evaluatorCards = CardMapper.ToEvaluatorCards(cards);
        return HoldemHandEvaluator.GetHandRanking(evaluatorCards);
    }

    private static HandCategory MapCategory(PokerHandCategory libraryCategory)
    {
        return libraryCategory switch
        {
            PokerHandCategory.HighCard => HandCategory.HighCard,
            PokerHandCategory.OnePair => HandCategory.Pair,
            PokerHandCategory.TwoPairs => HandCategory.TwoPair,
            PokerHandCategory.ThreeOfAKind => HandCategory.ThreeOfAKind,
            PokerHandCategory.Straight => HandCategory.Straight,
            PokerHandCategory.Flush => HandCategory.Flush,
            PokerHandCategory.FullHouse => HandCategory.FullHouse,
            PokerHandCategory.FourOfAKind => HandCategory.FourOfAKind,
            PokerHandCategory.StraightFlush => HandCategory.StraightFlush,
            PokerHandCategory.RoyalFlush => HandCategory.RoyalFlush,
            _ => throw new ArgumentOutOfRangeException(nameof(libraryCategory), $"Unknown category: {libraryCategory}")
        };
    }
}
