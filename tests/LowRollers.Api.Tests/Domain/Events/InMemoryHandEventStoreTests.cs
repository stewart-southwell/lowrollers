using LowRollers.Api.Domain.Events;
using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.StateMachine;

namespace LowRollers.Api.Tests.Domain.Events;

public class InMemoryHandEventStoreTests
{
    private readonly InMemoryHandEventStore _store = new();

    private static readonly Guid TestHandId = Guid.NewGuid();
    private static readonly Guid TestTableId = Guid.NewGuid();
    private static readonly Guid TestPlayerId1 = Guid.NewGuid();
    private static readonly Guid TestPlayerId2 = Guid.NewGuid();

    private static HandStartedEvent CreateHandStartedEvent(Guid? handId = null, Guid? tableId = null, int handNumber = 1)
    {
        return new HandStartedEvent
        {
            HandId = handId ?? TestHandId,
            TableId = tableId ?? TestTableId,
            HandNumber = handNumber,
            ButtonPosition = 1,
            SmallBlindPosition = 2,
            BigBlindPosition = 3,
            SmallBlindAmount = 0.5m,
            BigBlindAmount = 1m,
            PlayerIds = [TestPlayerId1, TestPlayerId2]
        };
    }

    #region AppendAsync Tests

    [Fact]
    public async Task AppendAsync_SingleEvent_StoresSuccessfully()
    {
        // Arrange
        var @event = CreateHandStartedEvent();

        // Act
        await _store.AppendAsync(@event);

        // Assert
        var eventCount = _store.GetEventCount(@event.HandId);
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task AppendAsync_MultipleEventsSequentially_StoresAll()
    {
        // Arrange
        var started = CreateHandStartedEvent();
        var blinds = new BlindsPostedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 2,
            SmallBlindPlayerId = TestPlayerId1,
            SmallBlindAmount = 0.5m,
            BigBlindPlayerId = TestPlayerId2,
            BigBlindAmount = 1m,
            PotTotal = 1.5m
        };

        // Act
        await _store.AppendAsync(started);
        await _store.AppendAsync(blinds);

        // Assert
        var eventCount = _store.GetEventCount(TestHandId);
        Assert.Equal(2, eventCount);
    }

    [Fact]
    public async Task AppendAsync_DuplicateSequenceNumber_ThrowsException()
    {
        // Arrange
        var event1 = CreateHandStartedEvent();
        var event2 = new BlindsPostedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 1, // Same as HandStartedEvent
            SmallBlindPlayerId = TestPlayerId1,
            SmallBlindAmount = 0.5m,
            BigBlindPlayerId = TestPlayerId2,
            BigBlindAmount = 1m,
            PotTotal = 1.5m
        };

        await _store.AppendAsync(event1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _store.AppendAsync(event2));
        Assert.Contains("sequence number", ex.Message);
    }

    #endregion

    #region AppendRangeAsync Tests

    [Fact]
    public async Task AppendRangeAsync_MultipleEvents_StoresAll()
    {
        // Arrange
        var events = new IHandEvent[]
        {
            CreateHandStartedEvent(),
            new BlindsPostedEvent
            {
                HandId = TestHandId,
                SequenceNumber = 2,
                SmallBlindPlayerId = TestPlayerId1,
                SmallBlindAmount = 0.5m,
                BigBlindPlayerId = TestPlayerId2,
                BigBlindAmount = 1m,
                PotTotal = 1.5m
            },
            new HoleCardsDealtEvent
            {
                HandId = TestHandId,
                SequenceNumber = 3,
                PlayerCards = new Dictionary<Guid, Card[]>()
            }
        };

        // Act
        await _store.AppendRangeAsync(events);

        // Assert
        var eventCount = _store.GetEventCount(TestHandId);
        Assert.Equal(3, eventCount);
    }

    [Fact]
    public async Task AppendRangeAsync_DuplicateSequenceInBatch_ThrowsException()
    {
        // Arrange
        var events = new IHandEvent[]
        {
            CreateHandStartedEvent(),
            new BlindsPostedEvent
            {
                HandId = TestHandId,
                SequenceNumber = 1, // Duplicate
                SmallBlindPlayerId = TestPlayerId1,
                SmallBlindAmount = 0.5m,
                BigBlindPlayerId = TestPlayerId2,
                BigBlindAmount = 1m,
                PotTotal = 1.5m
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _store.AppendRangeAsync(events));
    }

    #endregion

    #region GetEventsAsync Tests

    [Fact]
    public async Task GetEventsAsync_ReturnsEventsInSequenceOrder()
    {
        // Arrange
        var blinds = new BlindsPostedEvent
        {
            HandId = TestHandId,
            SequenceNumber = 2,
            SmallBlindPlayerId = TestPlayerId1,
            SmallBlindAmount = 0.5m,
            BigBlindPlayerId = TestPlayerId2,
            BigBlindAmount = 1m,
            PotTotal = 1.5m
        };
        var started = CreateHandStartedEvent(); // Sequence 1

        // Add out of order
        await _store.AppendAsync(blinds);
        await _store.AppendAsync(started);

        // Act
        var events = await _store.GetEventsAsync(TestHandId).ToListAsync();

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal(1, events[0].SequenceNumber);
        Assert.Equal(2, events[1].SequenceNumber);
        Assert.IsType<HandStartedEvent>(events[0]);
        Assert.IsType<BlindsPostedEvent>(events[1]);
    }

    [Fact]
    public async Task GetEventsAsync_NonExistentHand_ReturnsEmpty()
    {
        // Act
        var events = await _store.GetEventsAsync(Guid.NewGuid()).ToListAsync();

        // Assert
        Assert.Empty(events);
    }

    #endregion

    #region GetEventsFromAsync Tests

    [Fact]
    public async Task GetEventsFromAsync_ReturnsFromSequenceOnwards()
    {
        // Arrange
        await _store.AppendRangeAsync(new IHandEvent[]
        {
            CreateHandStartedEvent(),
            new BlindsPostedEvent
            {
                HandId = TestHandId, SequenceNumber = 2,
                SmallBlindPlayerId = TestPlayerId1, SmallBlindAmount = 0.5m,
                BigBlindPlayerId = TestPlayerId2, BigBlindAmount = 1m, PotTotal = 1.5m
            },
            new HoleCardsDealtEvent
            {
                HandId = TestHandId, SequenceNumber = 3,
                PlayerCards = new Dictionary<Guid, Card[]>()
            }
        });

        // Act
        var events = await _store.GetEventsFromAsync(TestHandId, fromSequence: 2).ToListAsync();

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal(2, events[0].SequenceNumber);
        Assert.Equal(3, events[1].SequenceNumber);
    }

    #endregion

    #region GetLastSequenceNumberAsync Tests

    [Fact]
    public async Task GetLastSequenceNumberAsync_ReturnsHighestSequence()
    {
        // Arrange
        await _store.AppendRangeAsync(new IHandEvent[]
        {
            CreateHandStartedEvent(),
            new BlindsPostedEvent
            {
                HandId = TestHandId, SequenceNumber = 2,
                SmallBlindPlayerId = TestPlayerId1, SmallBlindAmount = 0.5m,
                BigBlindPlayerId = TestPlayerId2, BigBlindAmount = 1m, PotTotal = 1.5m
            },
            new HoleCardsDealtEvent
            {
                HandId = TestHandId, SequenceNumber = 5, // Gap in sequence
                PlayerCards = new Dictionary<Guid, Card[]>()
            }
        });

        // Act
        var lastSequence = await _store.GetLastSequenceNumberAsync(TestHandId);

        // Assert
        Assert.Equal(5, lastSequence);
    }

    [Fact]
    public async Task GetLastSequenceNumberAsync_NoEvents_ReturnsZero()
    {
        // Act
        var lastSequence = await _store.GetLastSequenceNumberAsync(Guid.NewGuid());

        // Assert
        Assert.Equal(0, lastSequence);
    }

    #endregion

    #region HandSummary Tests

    [Fact]
    public async Task GetHandSummaryAsync_HandNotComplete_ReturnsNull()
    {
        // Arrange
        await _store.AppendAsync(CreateHandStartedEvent());

        // Act
        var summary = await _store.GetHandSummaryAsync(TestHandId);

        // Assert
        Assert.Null(summary);
    }

    [Fact]
    public async Task GetHandSummaryAsync_HandComplete_ReturnsSummary()
    {
        // Arrange
        var winnerId = Guid.NewGuid();
        await _store.AppendRangeAsync(new IHandEvent[]
        {
            CreateHandStartedEvent(handNumber: 42),
            new HandCompletedEvent
            {
                HandId = TestHandId,
                SequenceNumber = 15,
                TotalPotAmount = 100m,
                DurationMs = 30000,
                PlayerCount = 4,
                WentToShowdown = true,
                FinalPhase = HandPhase.Showdown,
                PlayerResults = new Dictionary<Guid, decimal>(),
                WinnerIds = [winnerId]
            }
        });

        // Act
        var summary = await _store.GetHandSummaryAsync(TestHandId);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(TestHandId, summary.HandId);
        Assert.Equal(TestTableId, summary.TableId);
        Assert.Equal(42, summary.HandNumber);
        Assert.Equal(100m, summary.TotalPot);
        Assert.Equal(30000, summary.DurationMs);
        Assert.Equal(4, summary.PlayerCount);
        Assert.True(summary.WentToShowdown);
        Assert.Contains(winnerId, summary.WinnerIds);
    }

    #endregion

    #region GetTableHistoryAsync Tests

    [Fact]
    public async Task GetTableHistoryAsync_ReturnsCompletedHandsForTable()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var hand1Id = Guid.NewGuid();
        var hand2Id = Guid.NewGuid();

        await _store.AppendRangeAsync(new IHandEvent[]
        {
            CreateHandStartedEvent(hand1Id, tableId, 1),
            new HandCompletedEvent
            {
                HandId = hand1Id, SequenceNumber = 10,
                TotalPotAmount = 50m, DurationMs = 20000,
                PlayerCount = 3, WentToShowdown = false,
                FinalPhase = HandPhase.Turn,
                PlayerResults = new Dictionary<Guid, decimal>(),
                WinnerIds = [Guid.NewGuid()]
            }
        });

        await _store.AppendRangeAsync(new IHandEvent[]
        {
            CreateHandStartedEvent(hand2Id, tableId, 2),
            new HandCompletedEvent
            {
                HandId = hand2Id, SequenceNumber = 15,
                TotalPotAmount = 100m, DurationMs = 45000,
                PlayerCount = 5, WentToShowdown = true,
                FinalPhase = HandPhase.Showdown,
                PlayerResults = new Dictionary<Guid, decimal>(),
                WinnerIds = [Guid.NewGuid()]
            }
        });

        // Act
        var history = await _store.GetTableHistoryAsync(tableId, limit: 10).ToListAsync();

        // Assert
        Assert.Equal(2, history.Count);
        // Should be ordered by most recent first
        Assert.Equal(hand2Id, history[0].HandId);
        Assert.Equal(hand1Id, history[1].HandId);
    }

    [Fact]
    public async Task GetTableHistoryAsync_RespectsLimit()
    {
        // Arrange
        var tableId = Guid.NewGuid();

        for (int i = 1; i <= 5; i++)
        {
            var handId = Guid.NewGuid();
            await _store.AppendRangeAsync(new IHandEvent[]
            {
                CreateHandStartedEvent(handId, tableId, i),
                new HandCompletedEvent
                {
                    HandId = handId, SequenceNumber = 10,
                    TotalPotAmount = i * 10m, DurationMs = 10000,
                    PlayerCount = 3, WentToShowdown = false,
                    FinalPhase = HandPhase.Flop,
                    PlayerResults = new Dictionary<Guid, decimal>(),
                    WinnerIds = [Guid.NewGuid()]
                }
            });
        }

        // Act
        var history = await _store.GetTableHistoryAsync(tableId, limit: 3).ToListAsync();

        // Assert
        Assert.Equal(3, history.Count);
    }

    [Fact]
    public async Task GetTableHistoryAsync_DifferentTables_OnlyReturnsRequestedTable()
    {
        // Arrange
        var table1Id = Guid.NewGuid();
        var table2Id = Guid.NewGuid();
        var hand1Id = Guid.NewGuid();
        var hand2Id = Guid.NewGuid();

        await _store.AppendRangeAsync(new IHandEvent[]
        {
            CreateHandStartedEvent(hand1Id, table1Id, 1),
            new HandCompletedEvent
            {
                HandId = hand1Id, SequenceNumber = 10,
                TotalPotAmount = 50m, DurationMs = 20000,
                PlayerCount = 3, WentToShowdown = false,
                FinalPhase = HandPhase.Turn,
                PlayerResults = new Dictionary<Guid, decimal>(),
                WinnerIds = [Guid.NewGuid()]
            }
        });

        await _store.AppendRangeAsync(new IHandEvent[]
        {
            CreateHandStartedEvent(hand2Id, table2Id, 1),
            new HandCompletedEvent
            {
                HandId = hand2Id, SequenceNumber = 10,
                TotalPotAmount = 100m, DurationMs = 30000,
                PlayerCount = 4, WentToShowdown = true,
                FinalPhase = HandPhase.Showdown,
                PlayerResults = new Dictionary<Guid, decimal>(),
                WinnerIds = [Guid.NewGuid()]
            }
        });

        // Act
        var table1History = await _store.GetTableHistoryAsync(table1Id).ToListAsync();
        var table2History = await _store.GetTableHistoryAsync(table2Id).ToListAsync();

        // Assert
        Assert.Single(table1History);
        Assert.Equal(hand1Id, table1History[0].HandId);

        Assert.Single(table2History);
        Assert.Equal(hand2Id, table2History[0].HandId);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public async Task Clear_RemovesAllData()
    {
        // Arrange
        await _store.AppendAsync(CreateHandStartedEvent());

        // Act
        _store.Clear();

        // Assert
        var events = await _store.GetEventsAsync(TestHandId).ToListAsync();
        Assert.Empty(events);
    }

    #endregion
}
