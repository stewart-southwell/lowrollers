using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Domain.Evaluation;

/// <summary>
/// Interface for poker hand evaluation services.
/// </summary>
public interface IHandEvaluationService
{
    /// <summary>
    /// Evaluates a poker hand from 5-7 cards.
    /// </summary>
    /// <param name="cards">The cards to evaluate (5-7 cards for Texas Hold'em).</param>
    /// <returns>The evaluated hand with ranking, category, and description.</returns>
    EvaluatedHand Evaluate(IReadOnlyList<Card> cards);

    /// <summary>
    /// Evaluates a Texas Hold'em hand from hole cards and community cards.
    /// </summary>
    /// <param name="holeCards">The player's two hole cards.</param>
    /// <param name="communityCards">The community cards (3-5 cards).</param>
    /// <returns>The evaluated hand with ranking, category, and description.</returns>
    EvaluatedHand Evaluate(IReadOnlyList<Card> holeCards, IReadOnlyList<Card> communityCards);

    /// <summary>
    /// Compares multiple hands and returns them ordered by strength (strongest first).
    /// </summary>
    /// <param name="hands">The hands to compare.</param>
    /// <returns>Hands ordered by strength, strongest first.</returns>
    IReadOnlyList<EvaluatedHand> RankHands(IEnumerable<EvaluatedHand> hands);

    /// <summary>
    /// Determines the winner(s) from a set of hands.
    /// Returns multiple hands in case of a tie (split pot).
    /// </summary>
    /// <param name="hands">The hands to compare.</param>
    /// <returns>The winning hand(s). Multiple hands indicate a split pot.</returns>
    IReadOnlyList<EvaluatedHand> DetermineWinners(IEnumerable<EvaluatedHand> hands);

    /// <summary>
    /// Gets just the ranking integer for quick comparison.
    /// </summary>
    /// <param name="cards">The cards to evaluate.</param>
    /// <returns>The ranking integer (lower = better).</returns>
    int GetRanking(IReadOnlyList<Card> cards);
}
