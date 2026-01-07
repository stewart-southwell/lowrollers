using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace LowRollers.Api.Domain.Events;

/// <summary>
/// In-memory implementation of <see cref="IHandEventStore"/> for testing.
/// Not suitable for production use as events are lost on restart.
/// </summary>
public sealed class InMemoryHandEventStore : IHandEventStore
{
    private readonly ConcurrentDictionary<Guid, List<IHandEvent>> _eventsByHand = new();
    private readonly ConcurrentDictionary<Guid, Guid> _handToTable = new();
    private readonly ConcurrentDictionary<Guid, HandSummary> _summaries = new();
    private readonly object _lock = new();

    public Task AppendAsync(IHandEvent handEvent, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var events = _eventsByHand.GetOrAdd(handEvent.HandId, _ => []);

            // Check for duplicate sequence number
            if (events.Any(e => e.SequenceNumber == handEvent.SequenceNumber))
            {
                throw new InvalidOperationException(
                    $"Event with sequence number {handEvent.SequenceNumber} already exists for hand {handEvent.HandId}");
            }

            events.Add(handEvent);

            // Track table association from HandStartedEvent
            if (handEvent is HandStartedEvent started)
            {
                _handToTable[handEvent.HandId] = started.TableId;
            }

            // Create summary on completion
            if (handEvent is HandCompletedEvent completed)
            {
                CreateSummary(handEvent.HandId, completed);
            }
        }

        return Task.CompletedTask;
    }

    public Task AppendRangeAsync(IEnumerable<IHandEvent> events, CancellationToken ct = default)
    {
        lock (_lock)
        {
            foreach (var @event in events)
            {
                var handEvents = _eventsByHand.GetOrAdd(@event.HandId, _ => []);

                if (handEvents.Any(e => e.SequenceNumber == @event.SequenceNumber))
                {
                    throw new InvalidOperationException(
                        $"Event with sequence number {@event.SequenceNumber} already exists for hand {@event.HandId}");
                }

                handEvents.Add(@event);

                if (@event is HandStartedEvent started)
                {
                    _handToTable[@event.HandId] = started.TableId;
                }

                if (@event is HandCompletedEvent completed)
                {
                    CreateSummary(@event.HandId, completed);
                }
            }
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<IHandEvent> GetEventsAsync(
        Guid handId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!_eventsByHand.TryGetValue(handId, out var events))
        {
            yield break;
        }

        List<IHandEvent> snapshot;
        lock (_lock)
        {
            snapshot = events.OrderBy(e => e.SequenceNumber).ToList();
        }

        foreach (var @event in snapshot)
        {
            ct.ThrowIfCancellationRequested();
            yield return @event;
            await Task.Yield();
        }
    }

    public async IAsyncEnumerable<IHandEvent> GetEventsFromAsync(
        Guid handId,
        int fromSequence,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!_eventsByHand.TryGetValue(handId, out var events))
        {
            yield break;
        }

        List<IHandEvent> snapshot;
        lock (_lock)
        {
            snapshot = events
                .Where(e => e.SequenceNumber >= fromSequence)
                .OrderBy(e => e.SequenceNumber)
                .ToList();
        }

        foreach (var @event in snapshot)
        {
            ct.ThrowIfCancellationRequested();
            yield return @event;
            await Task.Yield();
        }
    }

    public async IAsyncEnumerable<HandSummary> GetTableHistoryAsync(
        Guid tableId,
        int limit = 100,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        List<HandSummary> summaries;
        lock (_lock)
        {
            summaries = _summaries.Values
                .Where(s => s.TableId == tableId)
                .OrderByDescending(s => s.CompletedAt)
                .Take(limit)
                .ToList();
        }

        foreach (var summary in summaries)
        {
            ct.ThrowIfCancellationRequested();
            yield return summary;
            await Task.Yield();
        }
    }

    public Task<HandSummary?> GetHandSummaryAsync(Guid handId, CancellationToken ct = default)
    {
        _summaries.TryGetValue(handId, out var summary);
        return Task.FromResult(summary);
    }

    public Task<int> GetLastSequenceNumberAsync(Guid handId, CancellationToken ct = default)
    {
        if (!_eventsByHand.TryGetValue(handId, out var events))
        {
            return Task.FromResult(0);
        }

        lock (_lock)
        {
            var lastSequence = events.Count > 0 ? events.Max(e => e.SequenceNumber) : 0;
            return Task.FromResult(lastSequence);
        }
    }

    private void CreateSummary(Guid handId, HandCompletedEvent completed)
    {
        if (!_handToTable.TryGetValue(handId, out var tableId))
        {
            return;
        }

        // Find HandStartedEvent to get hand number
        if (!_eventsByHand.TryGetValue(handId, out var events))
        {
            return;
        }

        var started = events.OfType<HandStartedEvent>().FirstOrDefault();
        if (started == null)
        {
            return;
        }

        var summary = new HandSummary
        {
            HandId = handId,
            TableId = tableId,
            HandNumber = started.HandNumber,
            WinnerIds = completed.WinnerIds,
            TotalPot = completed.TotalPotAmount,
            DurationMs = completed.DurationMs,
            PlayerCount = completed.PlayerCount,
            WentToShowdown = completed.WentToShowdown,
            CompletedAt = completed.Timestamp
        };

        _summaries[handId] = summary;
    }

    /// <summary>
    /// Clears all stored events. For testing only.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _eventsByHand.Clear();
            _handToTable.Clear();
            _summaries.Clear();
        }
    }

    /// <summary>
    /// Gets the count of events for a hand. For testing only.
    /// </summary>
    public int GetEventCount(Guid handId)
    {
        if (!_eventsByHand.TryGetValue(handId, out var events))
        {
            return 0;
        }

        lock (_lock)
        {
            return events.Count;
        }
    }
}
