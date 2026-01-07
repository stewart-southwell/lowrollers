using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Domain.Pots;

/// <summary>
/// Manages pot calculations including main pot and side pots.
/// Handles all-in scenarios where players contribute different amounts.
/// </summary>
public sealed class PotManager : IPotManager
{
    /// <inheritdoc/>
    public List<Pot> CalculatePots(
        IReadOnlyDictionary<Guid, decimal> contributions,
        IReadOnlySet<Guid> allInPlayerIds,
        IReadOnlySet<Guid> foldedPlayerIds)
    {
        if (contributions.Count == 0)
        {
            return [Pot.CreateMainPot()];
        }

        // All contributions (including folded) for calculating pot amounts
        var allContributors = contributions
            .Where(c => c.Value > 0)
            .ToDictionary(c => c.Key, c => c.Value);

        // Active contributors (not folded) for determining eligibility
        var activeContributors = contributions
            .Where(c => c.Value > 0 && !foldedPlayerIds.Contains(c.Key))
            .ToDictionary(c => c.Key, c => c.Value);

        if (allContributors.Count == 0)
        {
            return [Pot.CreateMainPot()];
        }

        // If all contributors folded, create main pot with their contributions
        // but no eligible winners (will be handled by game logic)
        if (activeContributors.Count == 0)
        {
            var mainPot = Pot.CreateMainPot();
            mainPot.Amount = allContributors.Values.Sum();
            return [mainPot];
        }

        // Find all unique contribution levels from all-in players (active only)
        // These define the "caps" at which side pots are created
        var allInContributions = activeContributors
            .Where(c => allInPlayerIds.Contains(c.Key))
            .Select(c => c.Value)
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        // Add the maximum active contribution as the final level
        var maxActiveContribution = activeContributors.Values.Max();
        if (!allInContributions.Contains(maxActiveContribution))
        {
            allInContributions.Add(maxActiveContribution);
        }

        var pots = new List<Pot>();
        decimal previousLevel = 0;
        int potOrder = 0;

        foreach (var level in allInContributions)
        {
            // Calculate the contribution increment for this level
            var levelIncrement = level - previousLevel;
            if (levelIncrement <= 0) continue;

            // Find eligible players for this pot level (active players only)
            // A player is eligible if they contributed at least up to this level
            var eligiblePlayers = activeContributors
                .Where(c => c.Value >= level)
                .Select(c => c.Key)
                .ToList();

            // Calculate pot amount from ALL contributors (including folded)
            // Players contribute min(their contribution - previous level, level increment)
            var potAmount = 0m;
            foreach (var (playerId, contribution) in allContributors)
            {
                var playerContributionAtThisLevel = Math.Min(
                    Math.Max(0, contribution - previousLevel),
                    levelIncrement);
                potAmount += playerContributionAtThisLevel;
            }

            if (potAmount > 0)
            {
                var pot = potOrder == 0
                    ? Pot.CreateMainPot()
                    : Pot.CreateSidePot(eligiblePlayers, potOrder);

                pot.Amount = potAmount;

                // For main pot, add all active players as eligible
                if (potOrder == 0)
                {
                    foreach (var playerId in activeContributors.Keys)
                    {
                        pot.AddEligiblePlayer(playerId);
                    }
                }

                pots.Add(pot);
                potOrder++;
            }

            previousLevel = level;
        }

        // Ensure we always have at least a main pot
        if (pots.Count == 0)
        {
            pots.Add(Pot.CreateMainPot());
        }

        return pots;
    }

    /// <inheritdoc/>
    public List<Pot> CollectBets(
        List<Pot> existingPots,
        IReadOnlyDictionary<Guid, decimal> playerContributions,
        IReadOnlySet<Guid> allInPlayerIds,
        IReadOnlySet<Guid> foldedPlayerIds)
    {
        if (playerContributions.Count == 0 || playerContributions.Values.All(v => v == 0))
        {
            // No bets to collect, return existing pots
            return existingPots;
        }

        // Check if any side pots need to be created
        var hasAllInWithDifferentAmounts = allInPlayerIds.Count > 0 &&
            playerContributions
                .Where(c => allInPlayerIds.Contains(c.Key) && c.Value > 0)
                .Select(c => c.Value)
                .Distinct()
                .Count() > 1;

        var somePlayersContributedMore = allInPlayerIds.Count > 0 &&
            playerContributions.Values.Max() >
            playerContributions
                .Where(c => allInPlayerIds.Contains(c.Key) && c.Value > 0)
                .Select(c => c.Value)
                .DefaultIfEmpty(0)
                .Min();

        var needsSidePots = hasAllInWithDifferentAmounts || somePlayersContributedMore;

        if (!needsSidePots)
        {
            // Simple case: just add all contributions to the main pot
            var mainPot = existingPots.FirstOrDefault(p => p.Type == PotType.Main)
                ?? Pot.CreateMainPot();

            var totalContributions = playerContributions.Values.Sum();
            mainPot.AddChips(totalContributions);

            // Add eligible players
            foreach (var playerId in playerContributions.Keys.Where(id => !foldedPlayerIds.Contains(id)))
            {
                mainPot.AddEligiblePlayer(playerId);
            }

            if (!existingPots.Contains(mainPot))
            {
                return [mainPot];
            }
            return existingPots;
        }

        // Complex case: calculate side pots
        return CalculatePotsWithExisting(existingPots, playerContributions, allInPlayerIds, foldedPlayerIds);
    }

    /// <summary>
    /// Calculates pots when side pots are needed, preserving existing pot amounts.
    /// </summary>
    private static List<Pot> CalculatePotsWithExisting(
        List<Pot> existingPots,
        IReadOnlyDictionary<Guid, decimal> contributions,
        IReadOnlySet<Guid> allInPlayerIds,
        IReadOnlySet<Guid> foldedPlayerIds)
    {
        // Get existing pot amounts to preserve
        var existingMainPotAmount = existingPots
            .FirstOrDefault(p => p.Type == PotType.Main)?.Amount ?? 0;
        var existingSidePots = existingPots
            .Where(p => p.Type == PotType.Side)
            .OrderBy(p => p.CreationOrder)
            .ToList();

        // All contributions for calculating pot amounts
        var allContributors = contributions
            .Where(c => c.Value > 0)
            .ToDictionary(c => c.Key, c => c.Value);

        // Active contributors (not folded) for eligibility
        var activeContributors = contributions
            .Where(c => c.Value > 0 && !foldedPlayerIds.Contains(c.Key))
            .ToDictionary(c => c.Key, c => c.Value);

        if (activeContributors.Count == 0)
        {
            // All contributors folded, add their bets to existing pot
            var mainPot = existingPots.FirstOrDefault(p => p.Type == PotType.Main)
                ?? Pot.CreateMainPot();
            mainPot.AddChips(contributions.Values.Sum());
            return existingPots.Count > 0 ? existingPots : [mainPot];
        }

        // Find all-in cap levels from active contributors
        var allInContributions = activeContributors
            .Where(c => allInPlayerIds.Contains(c.Key))
            .Select(c => c.Value)
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        var maxActiveContribution = activeContributors.Values.Max();
        if (!allInContributions.Contains(maxActiveContribution))
        {
            allInContributions.Add(maxActiveContribution);
        }

        // Calculate new pots based on this betting round
        var newPots = new List<Pot>();
        decimal previousLevel = 0;
        int nextSidePotOrder = existingSidePots.Count + 1;

        // Handle main pot first
        var mainPotNew = Pot.CreateMainPot();
        mainPotNew.Amount = existingMainPotAmount;

        foreach (var playerId in activeContributors.Keys)
        {
            mainPotNew.AddEligiblePlayer(playerId);
        }

        // Calculate contribution to main pot (everyone contributes up to lowest all-in)
        var lowestAllIn = allInContributions.FirstOrDefault();
        if (lowestAllIn > 0)
        {
            foreach (var (playerId, contribution) in allContributors)
            {
                var contributionToMain = Math.Min(contribution, lowestAllIn);
                mainPotNew.AddChips(contributionToMain);
            }
            previousLevel = lowestAllIn;
        }
        else
        {
            // No all-ins, everything goes to main pot
            mainPotNew.AddChips(allContributors.Values.Sum());
            newPots.Add(mainPotNew);
            return newPots;
        }

        newPots.Add(mainPotNew);

        // Create side pots for each level beyond the first
        for (int i = 1; i < allInContributions.Count; i++)
        {
            var level = allInContributions[i];
            var levelIncrement = level - previousLevel;
            if (levelIncrement <= 0) continue;

            // Find eligible players for this side pot (active only)
            var eligiblePlayers = activeContributors
                .Where(c => c.Value >= level)
                .Select(c => c.Key)
                .ToList();

            // Calculate pot amount from all contributors
            var potAmount = 0m;
            foreach (var (playerId, contribution) in allContributors)
            {
                var playerContributionAtThisLevel = Math.Min(
                    Math.Max(0, contribution - previousLevel),
                    levelIncrement);
                potAmount += playerContributionAtThisLevel;
            }

            if (potAmount > 0)
            {
                var sidePot = Pot.CreateSidePot(eligiblePlayers, nextSidePotOrder++);
                sidePot.Amount = potAmount;
                newPots.Add(sidePot);
            }

            previousLevel = level;
        }

        return newPots;
    }

    /// <inheritdoc/>
    public void RemovePlayerFromPots(List<Pot> pots, Guid playerId)
    {
        foreach (var pot in pots)
        {
            pot.RemoveEligiblePlayer(playerId);
        }
    }

    /// <inheritdoc/>
    public Dictionary<Guid, decimal> AwardPots(
        List<Pot> pots,
        IReadOnlyDictionary<Guid, List<Guid>> winnersByPot)
    {
        var winnings = new Dictionary<Guid, decimal>();

        foreach (var pot in pots.OrderBy(p => p.CreationOrder))
        {
            if (!winnersByPot.TryGetValue(pot.Id, out var winners) || winners.Count == 0)
            {
                continue;
            }

            // Filter winners to only those eligible for this pot
            var eligibleWinners = winners
                .Where(w => pot.IsPlayerEligible(w))
                .ToList();

            if (eligibleWinners.Count == 0)
            {
                continue;
            }

            // Split pot evenly among winners
            var sharePerWinner = Math.Floor(pot.Amount / eligibleWinners.Count * 100) / 100;
            var remainder = pot.Amount - (sharePerWinner * eligibleWinners.Count);

            for (int i = 0; i < eligibleWinners.Count; i++)
            {
                var winnerId = eligibleWinners[i];
                var share = sharePerWinner;

                // First winner gets the remainder (odd chips)
                if (i == 0)
                {
                    share += remainder;
                }

                if (!winnings.ContainsKey(winnerId))
                {
                    winnings[winnerId] = 0;
                }
                winnings[winnerId] += share;
            }

            pot.Amount = 0; // Pot has been awarded
        }

        return winnings;
    }
}
