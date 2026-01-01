# Feature Specification: Core Gameplay

## Overview

The core poker gameplay engine implementing Texas Hold'em rules with server-authoritative state management.

**Phase:** 1 (MVP)
**Priority:** Critical
**BRD References:** REQ-GP-001 to REQ-GP-034

---

## User Stories

### US-007: Private Hole Cards
> As a seated player, I want to see my two hole cards privately so only I know my hand.

**Acceptance Criteria:**
- Hole cards display face-up on player's screen only
- Other players see card backs for opponent hands
- Cards are clearly visible and identifiable

### US-008: Player Actions
> As an active player on my turn, I want to fold, check, call, or raise so I can make strategic decisions.

**Acceptance Criteria:**
- Action buttons display contextually (check vs call based on bet state)
- Raise button opens input for raise amount with min/max limits
- Actions are confirmed immediately in UI
- All players see action notification within 100ms

### US-009: Game Information
> As a player, I want to see the pot size and all players' chip stacks so I can make informed decisions.

**Acceptance Criteria:**
- Total pot displays prominently in center of table
- Each player's chip stack displays near their position
- Values update in real-time as bets are placed
- Side pots are clearly labeled and differentiated

### US-010: Showdown Results
> As a player, I want to see who won the hand and with what cards so I can learn from the outcome.

**Acceptance Criteria:**
- All active players' cards are revealed at showdown
- Winning hand is highlighted with description (e.g., "Full House, Kings over Jacks")
- Chips animate moving to winner's stack
- Hand result visible for configured duration before next hand

### US-011: Action Timer
> As a player in a hand, I want a reasonable time to make my decision so I don't feel rushed.

**Acceptance Criteria:**
- Action timer displays during player's turn (host-configured duration)
- Visual and audio warnings at 10 seconds remaining
- Player auto-folds if timer expires
- Optional time bank for critical decisions (if enabled by host)

---

## Functional Requirements

### Game Flow (REQ-GP-001 to REQ-GP-009)
| ID | Requirement |
|----|-------------|
| REQ-GP-001 | Deal two hole cards face-down to each player at hand start |
| REQ-GP-002 | Collect small blind and big blind before each hand |
| REQ-GP-003 | Deal flop (3 community cards) after preflop betting round |
| REQ-GP-004 | Deal turn (1 community card) after flop betting round |
| REQ-GP-005 | Deal river (1 community card) after turn betting round |
| REQ-GP-006 | Determine winner(s) after river betting or when all but one folds |
| REQ-GP-007 | Award pot to winner(s) based on hand strength |
| REQ-GP-008 | Rotate dealer button clockwise after each hand |
| REQ-GP-009 | Skip empty seats when rotating button and blinds |

### Player Actions (REQ-GP-010 to REQ-GP-018)
| ID | Requirement |
|----|-------------|
| REQ-GP-010 | Players can fold, forfeiting interest in pot |
| REQ-GP-011 | Players can check when no bet is facing them |
| REQ-GP-012 | Players can call, matching current bet amount |
| REQ-GP-013 | Players can raise, increasing current bet |
| REQ-GP-014 | Minimum raise = current bet + last raise amount |
| REQ-GP-015 | Players can go all-in with remaining chips |
| REQ-GP-016 | Configurable action timer (15s, 30s, 45s, 60s, unlimited) |
| REQ-GP-017 | Auto-fold players who exceed action timer |
| REQ-GP-018 | Optional time bank for critical decisions |

### Pot Management (REQ-GP-019 to REQ-GP-023)
| ID | Requirement |
|----|-------------|
| REQ-GP-019 | Track total pot amount visible to all players |
| REQ-GP-020 | Create side pots when player(s) go all-in for less than current bet |
| REQ-GP-021 | Distribute side pots only to eligible players |
| REQ-GP-022 | Handle split pots when multiple players have equal hands |
| REQ-GP-023 | Display pot amounts clearly differentiated (main pot, side pots) |

### Hand Evaluation & Showdown (REQ-GP-024 to REQ-GP-034)
| ID | Requirement |
|----|-------------|
| REQ-GP-024 | Evaluate hands using standard poker hand rankings |
| REQ-GP-025 | At showdown, only players who choose to show reveal cards |
| REQ-GP-026 | Last aggressor reveals cards first at showdown |
| REQ-GP-027 | Other players have option to show or muck |
| REQ-GP-027a | If all check river, first to act shows first, eval clockwise |
| REQ-GP-028 | Winner by all folding not required to show cards |
| REQ-GP-029 | Players can voluntarily show cards when not required |
| REQ-GP-030 | Display winning hand description only when cards shown |
| REQ-GP-031 | Handle ties with split pot logic |
| REQ-GP-032 | Mucked cards remain hidden from all players |
| REQ-GP-033 | Award pot without showing if winner mucks |
| REQ-GP-034 | Record whether cards were shown or mucked in history |

---

## Technical Design

### Domain Models

```
Card (Suit, Rank)
Deck (Cards[], Shuffle(), Deal())
Player (Id, DisplayName, ChipStack, SeatPosition, Status, HoleCards)
Hand (Id, Phase, Pot, SidePots, CommunityCards, ButtonPosition, Events)
Pot (Amount, EligiblePlayers, Type)
PlayerAction (Fold, Check, Call, Raise, AllIn)
HandPhase (Waiting, Preflop, Flop, Turn, River, Showdown, Complete)
```

### State Machine

```
Waiting → Preflop → Flop → Turn → River → Showdown → Complete
              ↓        ↓       ↓        ↓         ↓
              └────────┴───────┴────────┴─────────┘
                    (all fold = skip to Complete)
```

### Key Services

| Service | Responsibility |
|---------|----------------|
| `GameOrchestrator` | Coordinates game flow, state transitions |
| `HandEvaluator` | Evaluates 7 cards → best 5-card hand |
| `PotManager` | Calculates main pot and side pots |
| `ShuffleService` | Fisher-Yates with crypto RNG |
| `ActionValidator` | Validates player actions against game state |
| `ActionTimerService` | Manages turn timers, auto-fold |

### SignalR Methods

**Client → Server:**
- `Fold()`
- `Check()`
- `Call()`
- `Raise(decimal amount)`
- `AllIn()`

**Server → Client:**
- `GameStateUpdated(TableGameState state)`
- `ActionRequired(Guid playerId, int timeoutSeconds)`
- `HandCompleted(HandResult result)`

---

## UI Components

| Component | Description |
|-----------|-------------|
| `poker-table` | Main table layout with player positions |
| `player-seat` | Individual player (chips, cards, status) |
| `community-cards` | Center board display |
| `pot-display` | Main pot and side pots |
| `action-panel` | Fold/Check/Call/Raise buttons |
| `raise-slider` | Slider for raise amount |
| `action-timer` | Countdown display |

---

## Testing Requirements

### Unit Tests
- Hand evaluation for all hand types (Royal Flush through High Card)
- Pot calculations including multi-way side pots
- FSM state transitions
- Action validation (valid/invalid scenarios)
- Shuffle distribution verification

### Integration Tests
- Complete hand from deal to showdown
- All-in scenarios with side pots
- Timer expiration and auto-fold
- Multiple players folding
- Split pot scenarios

---

*See [core-gameplay-tasks.md](./core-gameplay-tasks.md) for implementation tasks*
