namespace LowRollers.Api.Domain.Events;

/// <summary>
/// Base interface for all hand events in the event sourcing system.
/// Events are immutable records of state changes that occurred during a poker hand.
/// </summary>
public interface IHandEvent
{
    /// <summary>
    /// The unique identifier of the hand this event belongs to.
    /// </summary>
    Guid HandId { get; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Sequential ordering within the hand (1-based).
    /// Used for replay ordering and gap detection.
    /// </summary>
    int SequenceNumber { get; }

    /// <summary>
    /// The type name of the event for serialization/deserialization.
    /// </summary>
    string EventType { get; }
}
