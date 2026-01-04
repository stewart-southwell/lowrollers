using LowRollers.Api.Domain.Betting;
using LowRollers.Api.Domain.StateMachine;

namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Event raised when a player takes an action during a betting round.
/// </summary>
public sealed record PlayerActedEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required int SequenceNumber { get; init; }
    public string EventType => nameof(PlayerActedEvent);

    /// <summary>
    /// The player who took the action.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// The type of action taken.
    /// </summary>
    public required PlayerActionType ActionType { get; init; }

    /// <summary>
    /// The amount associated with the action.
    /// Zero for Fold and Check.
    /// For Call: the amount added to match the current bet.
    /// For Raise: the total bet amount (not just the raise portion).
    /// For AllIn: the player's entire remaining stack.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// The current betting phase when this action was taken.
    /// </summary>
    public required HandPhase Phase { get; init; }

    /// <summary>
    /// Player's remaining chip stack after this action.
    /// </summary>
    public required decimal RemainingStack { get; init; }

    /// <summary>
    /// Total pot after this action.
    /// </summary>
    public required decimal PotTotal { get; init; }

    /// <summary>
    /// The current bet level in the betting round after this action.
    /// </summary>
    public required decimal CurrentBetLevel { get; init; }

    /// <summary>
    /// Whether this action was a timeout (auto-fold).
    /// </summary>
    public bool IsTimeout { get; init; }
}
