using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Domain.Evaluation;

/// <summary>
/// Represents the result of evaluating a poker hand.
/// </summary>
/// <param name="Ranking">
/// Integer ranking where lower values indicate stronger hands.
/// Used for comparing hands - the hand with the lower ranking wins.
/// </param>
/// <param name="Category">The category of the hand (e.g., Flush, Pair, etc.).</param>
/// <param name="Description">Human-readable description (e.g., "Pair of Kings", "Flush, Ace high").</param>
/// <param name="Cards">The cards that were evaluated.</param>
public readonly record struct EvaluatedHand(
    int Ranking,
    HandCategory Category,
    string Description,
    IReadOnlyList<Card> Cards)
{
    /// <summary>
    /// Compares this hand against another.
    /// Returns a negative value if this hand is stronger (lower ranking),
    /// zero if equal, positive if this hand is weaker.
    /// </summary>
    /// <param name="other">The hand to compare against.</param>
    /// <returns>Comparison result where negative means this hand wins.</returns>
    public int CompareTo(EvaluatedHand other) => Ranking.CompareTo(other.Ranking);

    /// <summary>
    /// Returns true if this hand beats the other hand.
    /// </summary>
    public bool Beats(EvaluatedHand other) => Ranking < other.Ranking;

    /// <summary>
    /// Returns true if this hand ties with the other hand.
    /// </summary>
    public bool TiesWith(EvaluatedHand other) => Ranking == other.Ranking;
}
