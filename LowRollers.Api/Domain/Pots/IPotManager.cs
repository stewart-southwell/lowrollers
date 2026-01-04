using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Domain.Pots;

/// <summary>
/// Manages pot calculations including main pot and side pots.
/// </summary>
public interface IPotManager
{
    /// <summary>
    /// Calculates and returns all pots (main and side) based on player contributions.
    /// </summary>
    /// <param name="contributions">Dictionary of player ID to total contribution amount.</param>
    /// <param name="allInPlayerIds">Set of player IDs who are all-in.</param>
    /// <param name="foldedPlayerIds">Set of player IDs who have folded.</param>
    /// <returns>List of pots with eligible players and amounts.</returns>
    List<Pot> CalculatePots(
        IReadOnlyDictionary<Guid, decimal> contributions,
        IReadOnlySet<Guid> allInPlayerIds,
        IReadOnlySet<Guid> foldedPlayerIds);

    /// <summary>
    /// Collects bets from a betting round and updates pots accordingly.
    /// Call this at the end of each betting round.
    /// </summary>
    /// <param name="existingPots">Current list of pots.</param>
    /// <param name="playerContributions">Contributions from this betting round.</param>
    /// <param name="allInPlayerIds">Players who went all-in.</param>
    /// <param name="foldedPlayerIds">Players who have folded.</param>
    /// <returns>Updated list of pots.</returns>
    List<Pot> CollectBets(
        List<Pot> existingPots,
        IReadOnlyDictionary<Guid, decimal> playerContributions,
        IReadOnlySet<Guid> allInPlayerIds,
        IReadOnlySet<Guid> foldedPlayerIds);

    /// <summary>
    /// Removes a player from eligibility in all pots (when they fold).
    /// </summary>
    /// <param name="pots">Current list of pots.</param>
    /// <param name="playerId">Player ID to remove.</param>
    void RemovePlayerFromPots(List<Pot> pots, Guid playerId);

    /// <summary>
    /// Awards pots to winners.
    /// </summary>
    /// <param name="pots">List of pots to award.</param>
    /// <param name="winnersByPot">Dictionary of pot ID to list of winner IDs (multiple for split pots).</param>
    /// <returns>Dictionary of player ID to amount won.</returns>
    Dictionary<Guid, decimal> AwardPots(
        List<Pot> pots,
        IReadOnlyDictionary<Guid, List<Guid>> winnersByPot);
}
