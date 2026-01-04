using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Event raised when a pot is awarded to winner(s).
/// One event per pot (main pot, then each side pot).
/// </summary>
public sealed record PotAwardedEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required int SequenceNumber { get; init; }
    public string EventType => nameof(PotAwardedEvent);

    /// <summary>
    /// The pot that was awarded.
    /// </summary>
    public required Guid PotId { get; init; }

    /// <summary>
    /// Type of pot (Main or Side).
    /// </summary>
    public required PotType PotType { get; init; }

    /// <summary>
    /// Total amount in this pot.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Player IDs who won this pot.
    /// Multiple winners in case of split pot.
    /// </summary>
    public required IReadOnlyList<Guid> WinnerIds { get; init; }

    /// <summary>
    /// Breakdown of amount awarded to each winner.
    /// Handles odd chip scenarios where amounts may differ.
    /// In split pots with odd chips, the player in earliest position
    /// from the button receives the extra chip.
    /// </summary>
    public required IReadOnlyDictionary<Guid, decimal> WinnerAmounts { get; init; }

    /// <summary>
    /// Description of the winning hand (e.g., "Full House, Aces over Kings").
    /// Null if won by all others folding.
    /// </summary>
    public string? WinningHandDescription { get; init; }

    /// <summary>
    /// Whether this pot was won by all other players folding (no showdown).
    /// </summary>
    public bool WonByFold { get; init; }
}
