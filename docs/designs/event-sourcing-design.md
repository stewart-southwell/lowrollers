# Event Sourcing Design for Hand History

**Task ID:** `core-gameplay-07`
**Created:** 2026-01-03
**Status:** Design Approved

---

## Overview

Event sourcing captures all state changes as immutable events, enabling:
- Complete hand history reconstruction
- Audit trail for disputes
- Player statistics aggregation
- Replay functionality for analysis

---

## 1. Events to Capture

| Event | Data | When |
|-------|------|------|
| `HandStarted` | HandId, TableId, HandNumber, ButtonPosition, SmallBlindPosition, BigBlindPosition, BlindAmounts, PlayerIds, IsBombPot, IsDoubleBoard | Hand begins |
| `BlindsPosted` | SmallBlindPlayerId, SmallBlindAmount, BigBlindPlayerId, BigBlindAmount | After posting blinds |
| `AntePosted` | PlayerId, Amount | For bomb pots |
| `HoleCardsDealt` | PlayerId -> Cards mapping (hashed for storage security) | After dealing hole cards |
| `PlayerActed` | PlayerId, ActionType, Amount, NewChipStack, PotTotal | Each player action |
| `BettingRoundCompleted` | Phase, PotAmount, ActivePlayerCount | End of betting round |
| `CommunityCardsDealt` | Cards, Phase (Flop/Turn/River), BoardIndex (for double board) | Community card deals |
| `PlayerShowedCards` | PlayerId, Cards, HandDescription, HandRanking | At showdown |
| `PlayerMuckedCards` | PlayerId | At showdown |
| `PotAwarded` | PotId, PotType, WinnerIds, Amount, SplitAmount, HandDescription | Per pot |
| `HandCompleted` | WinnersSummary, FinalPotAmount, Duration | Hand ends |

### Event Hierarchy

```
IHandEvent (interface)
├── HandId: Guid
├── Timestamp: DateTimeOffset
└── SequenceNumber: int

Concrete Events:
├── HandStartedEvent
├── BlindsPostedEvent
├── AntePostedEvent
├── HoleCardsDealtEvent
├── PlayerActedEvent
├── BettingRoundCompletedEvent
├── CommunityCardsDealtEvent
├── PlayerShowedCardsEvent
├── PlayerMuckedCardsEvent
├── PotAwardedEvent
└── HandCompletedEvent
```

---

## 2. Event Store Structure

### PostgreSQL Schema

```sql
CREATE TABLE hand_events (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    hand_id           UUID NOT NULL,
    table_id          UUID NOT NULL,
    sequence_number   INT NOT NULL,
    event_type        VARCHAR(50) NOT NULL,
    event_data        JSONB NOT NULL,
    timestamp         TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uk_hand_sequence UNIQUE (hand_id, sequence_number)
);

-- Indexes for common queries
CREATE INDEX ix_hand_events_hand_id ON hand_events (hand_id);
CREATE INDEX ix_hand_events_table_id ON hand_events (table_id);
CREATE INDEX ix_hand_events_timestamp ON hand_events (timestamp DESC);
```

### Design Decisions

| Decision | Rationale |
|----------|-----------|
| JSONB for event_data | Schema flexibility, PostgreSQL native JSON support, indexable |
| Sequence numbers | Guaranteed ordering for replay, gap detection |
| Hole cards hashed | Security - actual cards stored only in Redis during live hand |
| Composite unique key | Prevents duplicate events, enables idempotent writes |

---

## 3. Replay/Reconstruction Strategy

### Interface Design

```csharp
public interface IHandEventStore
{
    // Write operations
    Task AppendAsync(IHandEvent @event, CancellationToken ct = default);
    Task AppendRangeAsync(IEnumerable<IHandEvent> events, CancellationToken ct = default);

    // Read operations
    IAsyncEnumerable<IHandEvent> GetEventsAsync(Guid handId, CancellationToken ct = default);
    IAsyncEnumerable<IHandEvent> GetEventsByTableAsync(Guid tableId, int limit = 100, CancellationToken ct = default);

    // Projections
    Task<HandSummary?> GetHandSummaryAsync(Guid handId, CancellationToken ct = default);
}

public interface IHandStateReconstructor
{
    Hand Replay(IEnumerable<IHandEvent> events);
    Hand ReplayToPhase(IEnumerable<IHandEvent> events, HandPhase targetPhase);
}
```

### Reconstruction Approach

1. **Full Replay**: Apply all events sequentially to rebuild complete Hand state
2. **Partial Replay**: Stop at specific phase for historical analysis
3. **Caching**: Recent hand states cached in Redis for quick access
4. **Snapshots** (future): For long-running tables, periodic snapshots reduce replay time

### Event Application Pattern

```csharp
public Hand Replay(IEnumerable<IHandEvent> events)
{
    Hand? hand = null;

    foreach (var @event in events.OrderBy(e => e.SequenceNumber))
    {
        hand = @event switch
        {
            HandStartedEvent e => ApplyHandStarted(e),
            BlindsPostedEvent e => ApplyBlindsPosted(hand!, e),
            PlayerActedEvent e => ApplyPlayerActed(hand!, e),
            CommunityCardsDealtEvent e => ApplyCommunityCardsDealt(hand!, e),
            // ... other events
            _ => hand
        };
    }

    return hand ?? throw new InvalidOperationException("No HandStarted event found");
}
```

---

## 4. Query Projections

| Projection | Purpose | Storage | Update Strategy |
|------------|---------|---------|-----------------|
| `HandSummary` | Quick stats: winners, amounts, duration | PostgreSQL | Materialized on HandCompleted |
| `PlayerHandHistory` | Player's recent hands at table | Query hand_events | On-demand query |
| `TableHandHistory` | Last N hands for a table | Query hand_events | On-demand query |
| `PlayerStats` | Win rate, avg pot, hands played | Separate table | Async background job |

### HandSummary Schema

```sql
CREATE TABLE hand_summaries (
    hand_id         UUID PRIMARY KEY REFERENCES hand_events(hand_id),
    table_id        UUID NOT NULL,
    hand_number     INT NOT NULL,
    winner_ids      UUID[] NOT NULL,
    total_pot       DECIMAL(12,2) NOT NULL,
    duration_ms     INT NOT NULL,
    player_count    INT NOT NULL,
    went_to_showdown BOOLEAN NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## 5. Security Considerations

### Hole Card Protection

During live play:
- Actual hole cards stored only in Redis (ephemeral)
- Event store receives hashed representation

After hand completion:
- Full cards can be stored (hand is public knowledge at showdown)
- Or remain hashed if players mucked

```csharp
public record HoleCardsDealtEvent : IHandEvent
{
    public required Guid HandId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required int SequenceNumber { get; init; }

    // Map of PlayerId -> CardHash (SHA256 of sorted card codes)
    public required Dictionary<Guid, string> PlayerCardHashes { get; init; }

    // Only populated after showdown or if player showed
    public Dictionary<Guid, Card[]>? RevealedCards { get; init; }
}
```

---

## 6. Implementation Phases

### Phase 1: Core Events (This Task)
- [ ] `IHandEvent` interface
- [ ] All concrete event records
- [ ] `IHandEventStore` interface
- [ ] `InMemoryHandEventStore` (for testing)
- [ ] Unit tests for event serialization

### Phase 2: Persistence (Future Task)
- [ ] PostgreSQL schema migration
- [ ] `PostgresHandEventStore` implementation
- [ ] Integration tests

### Phase 3: Reconstruction (Future Task)
- [ ] `IHandStateReconstructor` interface
- [ ] `HandStateReconstructor` implementation
- [ ] Replay unit tests

### Phase 4: Projections (Future Task)
- [ ] `HandSummary` projection
- [ ] Background job for stats aggregation
- [ ] Query APIs for hand history

---

## 7. File Structure

```
LowRollers.Api/
└── Domain/
    └── Events/
        ├── IHandEvent.cs           # Base interface
        ├── HandStartedEvent.cs
        ├── BlindsPostedEvent.cs
        ├── AntePostedEvent.cs
        ├── HoleCardsDealtEvent.cs
        ├── PlayerActedEvent.cs
        ├── BettingRoundCompletedEvent.cs
        ├── CommunityCardsDealtEvent.cs
        ├── PlayerShowedCardsEvent.cs
        ├── PlayerMuckedCardsEvent.cs
        ├── PotAwardedEvent.cs
        ├── HandCompletedEvent.cs
        ├── IHandEventStore.cs       # Store interface
        └── InMemoryHandEventStore.cs # Test implementation
```

---

## References

- [Event Sourcing Pattern - Microsoft](https://docs.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)
- [Domain Events - Martin Fowler](https://martinfowler.com/eaaDev/DomainEvent.html)
- Task spec: `docs/features/core-gameplay-tasks.md` (core-gameplay-07)
