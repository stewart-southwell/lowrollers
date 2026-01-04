using LowRollers.Api.Domain.Evaluation;
using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Features.GameEngine.Showdown;

/// <summary>
/// Result of a showdown, containing all player outcomes and pot distributions.
/// </summary>
public sealed record ShowdownResult
{
    /// <summary>
    /// Whether the showdown executed successfully.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if the showdown failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// The hand ID this showdown was for.
    /// </summary>
    public Guid HandId { get; init; }

    /// <summary>
    /// Results for each player at showdown, in show order.
    /// </summary>
    public IReadOnlyList<PlayerShowdownResult> PlayerResults { get; init; } = [];

    /// <summary>
    /// Pot awards showing which players won which pots.
    /// </summary>
    public IReadOnlyList<PotAward> PotAwards { get; init; } = [];

    /// <summary>
    /// Total winnings per player (sum across all pots).
    /// </summary>
    public IReadOnlyDictionary<Guid, decimal> TotalWinnings { get; init; } =
        new Dictionary<Guid, decimal>();

    /// <summary>
    /// Creates a successful showdown result.
    /// </summary>
    public static ShowdownResult Success(
        Guid handId,
        IReadOnlyList<PlayerShowdownResult> playerResults,
        IReadOnlyList<PotAward> potAwards,
        IReadOnlyDictionary<Guid, decimal> totalWinnings) =>
        new()
        {
            IsSuccess = true,
            HandId = handId,
            PlayerResults = playerResults,
            PotAwards = potAwards,
            TotalWinnings = totalWinnings
        };

    /// <summary>
    /// Creates a failed showdown result.
    /// </summary>
    public static ShowdownResult Failure(string error) =>
        new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Result for an individual player at showdown.
/// </summary>
public sealed record PlayerShowdownResult
{
    /// <summary>
    /// The player's ID.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// The player's display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The player's hole cards.
    /// </summary>
    public required Card[] HoleCards { get; init; }

    /// <summary>
    /// Whether the player showed their cards (vs mucked).
    /// </summary>
    public required bool Showed { get; init; }

    /// <summary>
    /// Whether the muck was automatic (inferior hand) or player choice.
    /// Only applicable if Showed is false.
    /// </summary>
    public bool AutoMucked { get; init; }

    /// <summary>
    /// The evaluated hand (null if player mucked without showing).
    /// </summary>
    public EvaluatedHand? EvaluatedHand { get; init; }

    /// <summary>
    /// Order in which this player was evaluated (1 = first to show).
    /// </summary>
    public required int ShowOrder { get; init; }

    /// <summary>
    /// Pot IDs this player won (if any).
    /// </summary>
    public IReadOnlyList<Guid> WonPotIds { get; init; } = [];

    /// <summary>
    /// Total amount won across all pots.
    /// </summary>
    public decimal AmountWon { get; init; }
}

/// <summary>
/// Represents a pot being awarded to winner(s).
/// </summary>
public sealed record PotAward
{
    /// <summary>
    /// The pot ID.
    /// </summary>
    public required Guid PotId { get; init; }

    /// <summary>
    /// The pot type (Main or Side).
    /// </summary>
    public required PotType PotType { get; init; }

    /// <summary>
    /// Total amount in the pot.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Players who won this pot.
    /// Multiple in case of split pot.
    /// </summary>
    public required IReadOnlyList<Guid> WinnerIds { get; init; }

    /// <summary>
    /// Amount awarded to each winner.
    /// </summary>
    public required IReadOnlyDictionary<Guid, decimal> WinnerAmounts { get; init; }

    /// <summary>
    /// Description of the winning hand.
    /// </summary>
    public required string WinningHandDescription { get; init; }

    /// <summary>
    /// Hand category of the winning hand.
    /// </summary>
    public required HandCategory WinningHandCategory { get; init; }
}
