# Technical Architecture

## Overview

Low Rollers uses a **vertical slice architecture** with a server-authoritative game engine. The frontend is a thin client that sends player intents; all game logic executes server-side.

```
┌─────────────────────────────────────────────────────────────────┐
│                        Azure Container Apps                      │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   Angular   │  │   .NET 10   │  │   LiveKit SFU           │  │
│  │   Frontend  │◄─┤   API       │  │   (Video)               │  │
│  │   (SPA)     │  │   + SignalR │  │                         │  │
│  └─────────────┘  └──────┬──────┘  └─────────────────────────┘  │
│                          │                                       │
│         ┌────────────────┼────────────────┐                     │
│         ▼                ▼                ▼                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  PostgreSQL │  │    Redis    │  │   Azure     │              │
│  │  (Primary)  │  │   (Cache)   │  │   SignalR   │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
└─────────────────────────────────────────────────────────────────┘
```

---

## Architecture Principles

### 1. Server-Authoritative Game State
- Client sends **intents** (e.g., "I want to raise $50")
- Server **validates** and **executes** the action
- Server **broadcasts** the resulting state to all clients
- Clients **never** determine game outcomes

### 2. Vertical Slices over Clean Architecture
Each feature owns its complete stack:
```
src/LowRollers.Api/Features/
├── TableManagement/
│   ├── CreateTableCommand.cs
│   ├── JoinTableCommand.cs
│   ├── TableRepository.cs
│   └── TableDto.cs
├── GameEngine/
│   ├── PlayerActionCommand.cs
│   ├── GameOrchestrator.cs
│   └── ...
└── VideoChat/
    └── ...
```

**Rationale:** Less abstraction, easier navigation. Clean Architecture adds layers unnecessary at our scale (100 tables max).

### 3. Lightweight CQRS
- **Commands:** Mutate state (PlayerAction, CreateTable, JoinTable)
- **Queries:** Read state (GetTableState, GetHandHistory)
- No MediatR—just organized folders with handler classes

---

## Game Engine Design

### Finite State Machine (Hand Phases)

```
┌─────────┐    ┌──────────┐    ┌───────┐    ┌───────┐    ┌───────┐    ┌──────────┐    ┌──────────┐
│ Waiting │───►│ Preflop  │───►│ Flop  │───►│ Turn  │───►│ River │───►│ Showdown │───►│ Complete │
└─────────┘    └──────────┘    └───────┘    └───────┘    └───────┘    └──────────┘    └──────────┘
                    │              │            │            │              │
                    └──────────────┴────────────┴────────────┴──────────────┘
                                    (all fold = skip to Complete)
```

### Event Sourcing for Hand History

Each hand is a sequence of events:
```csharp
interface IHandEvent {
    Guid HandId { get; }
    DateTime Timestamp { get; }
}

// Events
record BlindsPosted(Guid HandId, DateTime Timestamp, decimal SmallBlind, decimal BigBlind);
record HoleCardsDealt(Guid HandId, DateTime Timestamp, Dictionary<Guid, Card[]> PlayerCards);
record PlayerActed(Guid HandId, DateTime Timestamp, Guid PlayerId, PlayerAction Action, decimal? Amount);
record CommunityCardsDealt(Guid HandId, DateTime Timestamp, Card[] Cards, HandPhase Phase);
record PotAwarded(Guid HandId, DateTime Timestamp, Guid WinnerId, decimal Amount, string HandDescription);
```

**Benefits:**
- Natural fit—hands ARE sequences of events
- Complete audit trail
- Replay any hand by replaying events
- Hand history is just event retrieval

---

## Communication Patterns

### REST API (Table CRUD, Configuration)
```
POST   /api/tables              Create table, returns invite code
GET    /api/tables/{id}         Get table state (if authorized)
POST   /api/tables/{id}/join    Join table with display name
DELETE /api/tables/{id}/leave   Leave table
GET    /api/tables/{id}/history Get hand history
```

### SignalR Hub (Real-time Game Actions)

```csharp
public class GameHub : Hub
{
    // Client → Server (intents)
    Task Fold();
    Task Check();
    Task Call();
    Task Raise(decimal amount);
    Task AllIn();

    // Server → Client (broadcasts)
    // GameStateUpdated(TableGameState state)
    // PlayerJoined(PlayerInfo player)
    // PlayerLeft(Guid playerId)
    // ActionRequired(Guid playerId, int timeoutSeconds)
}
```

### State Sanitization

Each client receives **sanitized** state:
- Own hole cards: visible
- Other players' hole cards: hidden (until showdown)
- Community cards: visible to all
- Pot amounts: visible to all
- Other players' actions: visible to all

---

## Data Flow Example: Player Raises

```
1. Client clicks "Raise $50" button
   └─► SignalR: Raise(50)

2. Server GameHub receives intent
   └─► Validate: Is it this player's turn? Can they afford $50? Is $50 >= min raise?

3. Server GameOrchestrator executes action
   └─► Update game state
   └─► Create PlayerActed event
   └─► Check if betting round complete

4. Server broadcasts sanitized state
   └─► SignalR: GameStateUpdated(sanitizedState)

5. All clients receive update
   └─► Update UI within 100ms of step 1
```

---

## Scalability Design

### Horizontal Scaling
- **Stateless API servers** behind load balancer
- **Redis** for session state and real-time caching
- **Azure SignalR Service** for WebSocket fan-out
- **PostgreSQL** for persistent data

### Scale Targets
| Metric | Target |
|--------|--------|
| Concurrent tables | 100 |
| Concurrent players | 1,000 |
| Action latency | <100ms |
| Auto-scale response | <5 minutes |

---

## Security Architecture

### Authentication Flow (Guest Sessions)
```
1. User clicks invite link → /join/{inviteCode}
2. User enters display name
3. Server validates:
   - Invite code exists and is active
   - Display name unique at table
   - Not banned from table
4. Server creates JWT session token:
   - Contains: SessionId, DisplayName, TableId
   - Expires: 24 hours (or browser close)
   - Stored in Redis with 5-min reconnection window
5. Client stores token, uses for all requests
```

### Game Integrity
- **Fisher-Yates shuffle** with `System.Security.Cryptography.RandomNumberGenerator`
- **Server-side validation** of all actions
- **Rate limiting** to prevent abuse
- **TLS 1.3** for all communications
- **Audit logging** of all game events

---

## Failure Handling

### Player Disconnection
```
1. SignalR detects disconnect
2. Mark player as "Disconnected" in Redis
3. Start 5-minute reconnection timer
4. If player's turn:
   - Action timer continues
   - Auto-fold if timer expires
5. If player reconnects within 5 minutes:
   - Restore to same seat and chip stack
   - Rejoin SignalR group
6. If timer expires:
   - Mark as "Away"
   - Skip in future hands until return
```

### Video Failure Independence
- Game state uses SignalR (separate from video)
- Video drop does not affect game actions
- Players can continue playing without video
- Video reconnects automatically when possible

---

*See also: [tech-stack.md](./tech-stack.md) for technology decisions*
