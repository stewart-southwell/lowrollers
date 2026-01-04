using LowRollers.Api.Domain.StateMachine;

namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Event raised when a betting round completes.
/// </summary>
public sealed record BettingRoundCompletedEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required int SequenceNumber { get; init; }
    public string EventType => nameof(BettingRoundCompletedEvent);

    /// <summary>
    /// The phase that just completed.
    /// </summary>
    public required HandPhase CompletedPhase { get; init; }

    /// <summary>
    /// Total pot at the end of this betting round.
    /// </summary>
    public required decimal PotTotal { get; init; }

    /// <summary>
    /// Number of players still active (not folded or all-in).
    /// </summary>
    public required int ActivePlayerCount { get; init; }

    /// <summary>
    /// Number of players still in the hand (including all-in).
    /// </summary>
    public required int PlayersInHand { get; init; }

    /// <summary>
    /// Whether the hand ended early due to all but one player folding.
    /// </summary>
    public bool AllFoldedToOne { get; init; }
}
