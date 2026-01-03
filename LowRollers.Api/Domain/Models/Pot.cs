namespace LowRollers.Api.Domain.Models;

/// <summary>
/// Type of pot in a poker hand.
/// </summary>
public enum PotType
{
    /// <summary>The main pot that all active players are eligible to win.</summary>
    Main = 0,

    /// <summary>A side pot created when a player goes all-in for less than the current bet.</summary>
    Side = 1
}

/// <summary>
/// Represents a pot (main or side) in a poker hand.
/// </summary>
public sealed class Pot
{
    /// <summary>
    /// Unique identifier for this pot.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Type of pot (Main or Side).
    /// </summary>
    public PotType Type { get; init; }

    /// <summary>
    /// Current amount in the pot.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Player IDs eligible to win this pot.
    /// </summary>
    public HashSet<Guid> EligiblePlayerIds { get; init; } = [];

    /// <summary>
    /// Order in which this pot was created (for side pots).
    /// Main pot is always 0.
    /// </summary>
    public int CreationOrder { get; init; }

    /// <summary>
    /// Creates the main pot for a hand.
    /// </summary>
    public static Pot CreateMainPot()
    {
        return new Pot
        {
            Type = PotType.Main,
            Amount = 0,
            CreationOrder = 0
        };
    }

    /// <summary>
    /// Creates a side pot when a player goes all-in.
    /// </summary>
    /// <param name="eligiblePlayerIds">Players who can win this side pot.</param>
    /// <param name="order">Order in which this side pot was created.</param>
    public static Pot CreateSidePot(IEnumerable<Guid> eligiblePlayerIds, int order)
    {
        return new Pot
        {
            Type = PotType.Side,
            Amount = 0,
            EligiblePlayerIds = new HashSet<Guid>(eligiblePlayerIds),
            CreationOrder = order
        };
    }

    /// <summary>
    /// Adds chips to this pot.
    /// </summary>
    public void AddChips(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount must be non-negative.", nameof(amount));
        }
        Amount += amount;
    }

    /// <summary>
    /// Adds a player to the eligible players for this pot.
    /// </summary>
    public void AddEligiblePlayer(Guid playerId)
    {
        EligiblePlayerIds.Add(playerId);
    }

    /// <summary>
    /// Removes a player from eligibility (e.g., when they fold).
    /// </summary>
    public void RemoveEligiblePlayer(Guid playerId)
    {
        EligiblePlayerIds.Remove(playerId);
    }

    /// <summary>
    /// Checks if a player is eligible to win this pot.
    /// </summary>
    public bool IsPlayerEligible(Guid playerId) => EligiblePlayerIds.Contains(playerId);
}
