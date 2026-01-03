# Implementation Tasks: Core Gameplay

**Feature:** Core Gameplay
**Spec:** [core-gameplay-spec.md](./core-gameplay-spec.md)
**Phase:** 1 (MVP)

---

## Domain Models & Game Engine

- [x] **Create core domain models**
  Task ID: `core-gameplay-01`
  > **Implementation**: Create `src/LowRollers.Api/Domain/Models/`
  > **Details**:
  > - `Card.cs` - Suit enum, Rank enum, Card record
  > - `Deck.cs` - 52 cards, Fisher-Yates shuffle with crypto RNG
  > - `Player.cs` - Id, DisplayName, ChipStack, SeatPosition, Status
  > - `Hand.cs` - Id, Phase, Pot, SidePots, CommunityCards, ButtonPosition
  > - `Pot.cs` - Amount, EligiblePlayers, Type (Main/Side)

- [x] **Integrate HoldemPoker.Evaluator library**
  Task ID: `core-gameplay-02`
  > **Implementation**: Create `src/LowRollers.Api/Domain/Evaluation/`
  > **Details**:
  > - Install NuGet packages:
  >   - `HoldemPoker.Evaluator` (v1.0.1+)
  >   - `HoldemPoker.Cards` (>= 0.0.1)
  > - Create `HandEvaluationService.cs` - Wrapper service around HoldemPoker.Evaluator
  > - Map domain `Card` model to `HoldemPoker.Cards` types
  > - Use library API:
  >   - `HoldemHandEvaluator.GetHandRanking()` - Integer rank (lower = better)
  >   - `HoldemHandEvaluator.GetHandCategory()` - Hand type (Flush, Pair, etc.)
  >   - `HoldemHandEvaluator.GetHandDescription()` - Display text ("Pair of Kings")
  > - Create `EvaluatedHand.cs` - Wrapper record with Ranking, Category, Description
  > - Library uses hashtable-cached lookups for few-cycle performance
  > - Unit tests verify correct integration with library

- [ ] **Implement cryptographic shuffle**
  Task ID: `core-gameplay-03`
  > **Implementation**: Create `src/LowRollers.Api/Domain/Services/ShuffleService.cs`
  > **Details**:
  > - Use `System.Security.Cryptography.RandomNumberGenerator`
  > - Implement Fisher-Yates shuffle algorithm exactly per spec
  > - Create shuffle verification method
  > - Unit tests proving uniform distribution (chi-square test)

- [ ] **Create finite state machine for hand phases**
  Task ID: `core-gameplay-04`
  > **Implementation**: Create `src/LowRollers.Api/Domain/StateMachine/`
  > **Details**:
  > - `HandPhase.cs` - Enum: Waiting, Preflop, Flop, Turn, River, Showdown, Complete
  > - `HandStateMachine.cs` - State transitions, validation
  > - `IHandPhaseHandler` interface for phase-specific logic
  > - Guards for valid transitions only
  > - Logging for state changes

- [ ] **Implement betting logic**
  Task ID: `core-gameplay-05`
  > **Implementation**: Create `src/LowRollers.Api/Domain/Betting/`
  > **Details**:
  > - `BettingRound.cs` - CurrentBet, LastRaise, Raises count
  > - `PlayerAction.cs` - Enum: Fold, Check, Call, Raise, AllIn
  > - `ActionValidator.cs` - Validate actions:
  >   - Fold: always valid when it's player's turn
  >   - Check: only when CurrentBet == 0 or player already matched
  >   - Call: only when CurrentBet > player's current bet
  >   - Raise: amount >= CurrentBet + LastRaise
  >   - AllIn: always valid, calculates effective amount

- [ ] **Implement pot management and side pots**
  Task ID: `core-gameplay-06`
  > **Implementation**: Create `src/LowRollers.Api/Domain/Pots/`
  > **Details**:
  > - `PotManager.cs` - Main pot and side pot calculations
  > - Handle all-in scenarios correctly:
  >   - Player all-in for less creates side pot
  >   - Multiple all-ins create multiple side pots
  >   - Track eligible players per pot
  > - Unit tests for complex scenarios (3+ all-ins at different amounts)

- [ ] **Create event sourcing for hand history**
  Task ID: `core-gameplay-07`
  > **Implementation**: Create `src/LowRollers.Api/Domain/Events/`
  > **Details**:
  > - `IHandEvent` interface: HandId, Timestamp
  > - Events:
  >   - `BlindsPosted` - SmallBlind, BigBlind amounts
  >   - `HoleCardsDealt` - Player → Cards mapping (encrypted/hashed for storage)
  >   - `PlayerActed` - PlayerId, Action, Amount
  >   - `CommunityCardsDealt` - Cards, Phase
  >   - `PotAwarded` - WinnerId, Amount, HandDescription
  >   - `HandCompleted` - Summary
  > - `HandEventStore.cs` - Append, GetEvents(handId)
  > - Store as JSON in PostgreSQL

---

## Game Orchestration

- [ ] **Implement game flow orchestration**
  Task ID: `core-gameplay-08`
  > **Implementation**: Create `src/LowRollers.Api/Features/GameEngine/GameOrchestrator.cs`
  > **Details**:
  > - Coordinate entire hand flow
  > - Dealer button rotation (skip empty/away seats)
  > - Blind collection (small blind, big blind)
  > - Deal hole cards (2 per player)
  > - Execute betting rounds
  > - Deal community cards (3-1-1 pattern)
  > - Trigger showdown or award pot on all-fold

- [ ] **Implement showdown logic**
  Task ID: `core-gameplay-09`
  > **Implementation**: Create `src/LowRollers.Api/Features/GameEngine/Showdown/ShowdownHandler.cs`
  > **Details**:
  > - Inject `HandEvaluationService` for hand comparisons
  > - Determine show order:
  >   - Last aggressor shows first
  >   - If all checked, first-to-act shows first
  > - Evaluate clockwise using `GetHandRanking()` (lower = better)
  > - Auto-muck inferior hands based on ranking comparison
  > - Allow muck option for losers
  > - Track who showed/mucked for history
  > - Use `GetHandDescription()` for winner announcement text
  > - Calculate winners per pot
  > - Distribute pots correctly

- [ ] **Implement action timer system**
  Task ID: `core-gameplay-10`
  > **Implementation**: Create `src/LowRollers.Api/Features/GameEngine/ActionTimer/ActionTimerService.cs`
  > **Details**:
  > - Background service with System.Timers
  > - Track: ActivePlayerId, RemainingTime, HasTimeBank
  > - Broadcast timer ticks to clients (every second)
  > - Warning at 10 seconds
  > - Auto-fold on expiry
  > - Time bank: add time if enabled and available
  > - Cancel timer on player action

---

## SignalR Integration

- [ ] **Create player action SignalR methods**
  Task ID: `core-gameplay-11`
  > **Implementation**: Extend `src/LowRollers.Api/Hubs/GameHub.cs`
  > **Details**:
  > - `Fold()` - Validate turn, execute, broadcast
  > - `Check()` - Validate no bet facing, execute, broadcast
  > - `Call()` - Validate bet facing, match bet, broadcast
  > - `Raise(decimal amount)` - Validate min raise, execute, broadcast
  > - `AllIn()` - Execute all-in, broadcast
  > - All methods: validate it's player's turn, log action

- [ ] **Implement game state broadcasting**
  Task ID: `core-gameplay-12`
  > **Implementation**: Extend `src/LowRollers.Api/Hubs/GameHub.cs`
  > **Details**:
  > - `BroadcastGameState(TableGameState state)` - After each action
  > - Sanitize state per player:
  >   - Own hole cards: visible
  >   - Others' hole cards: null (until showdown)
  >   - Community cards: visible
  >   - Pot amounts: visible
  > - Target <100ms from action to all clients receiving update

---

## Angular Components

> **DESIGN APPROVAL REQUIRED**: Before implementing any UI component below, either:
> 1. Agent presents a generated mockup for user approval, OR
> 2. User provides a screenshot/design reference to emulate
>
> Approved designs are stored in `docs/designs/` for implementation reference.

- [ ] **Create poker table component**
  Task ID: `core-gameplay-13`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/poker-table/`
  > **Details**:
  > - `poker-table.component.ts` - Main table layout
  > - Position 10 player seats in oval arrangement
  > - Center area for community cards and pot
  > - Responsive layout for different screen sizes
  > - Use PrimeNG components where appropriate

- [ ] **Create player seat component**
  Task ID: `core-gameplay-14`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/player-seat/`
  > **Details**:
  > - `player-seat.component.ts`
  > - Display: name, chip count, cards (face-up/down), current bet
  > - Status indicators: active turn, folded, all-in, away
  > - Dealer button indicator
  > - Highlight when it's player's turn
  > - Use PrimeNG Chip, Badge components

- [ ] **Create community cards component**
  Task ID: `core-gameplay-15`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/community-cards/`
  > **Details**:
  > - Display 5 card positions (blank until dealt)
  > - Cards appear as dealt (3, then 1, then 1)
  > - Card flip animation on deal
  > - Highlight winning cards at showdown

- [ ] **Create pot display component**
  Task ID: `core-gameplay-16`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/pot-display/`
  > **Details**:
  > - Main pot prominently displayed
  > - Side pots listed separately with eligible player count
  > - Animate pot changes
  > - Use PrimeNG styling

- [ ] **Create action panel component**
  Task ID: `core-gameplay-17`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/action-panel/`
  > **Details**:
  > - `action-panel.component.ts`
  > - Contextual buttons:
  >   - Fold (always when active)
  >   - Check (when no bet facing)
  >   - Call $X (when bet facing)
  >   - Raise (opens amount input)
  >   - All-In (with confirmation)
  > - Use PrimeNG Button, ConfirmDialog
  > - Disable when not player's turn

- [ ] **Create raise slider component**
  Task ID: `core-gameplay-18`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/action-panel/raise-slider/`
  > **Details**:
  > - Slider from min raise to max (all-in)
  > - Quick buttons: Min, 1/2 Pot, Pot, All-In
  > - Number input for exact amount
  > - Use PrimeNG Slider, InputNumber

- [ ] **Create action timer component**
  Task ID: `core-gameplay-19`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/action-timer/`
  > **Details**:
  > - Countdown display (seconds remaining)
  > - Progress bar depleting
  > - Color change at warning threshold (10s)
  > - Time bank indicator if available
  > - Use PrimeNG ProgressBar

- [ ] **Implement keyboard hotkeys**
  Task ID: `core-gameplay-20`
  > **Implementation**: Add to `src/LowRollers.Web/src/app/features/game/action-panel/`
  > **Details**:
  > - F = Fold
  > - C = Call/Check (contextual)
  > - R = Raise (focus raise input)
  > - A = All-In (with confirmation)
  > - Only active when player's turn
  > - Display hotkey hints on buttons

---

## Testing

- [ ] **Create hand evaluation integration tests**
  Task ID: `core-gameplay-21`
  > **Implementation**: Create `tests/LowRollers.Api.Tests/Domain/Evaluation/`
  > **Details**:
  > - Test `HandEvaluationService` wrapper correctly maps domain cards to library
  > - Test hand comparison logic (ranking integer comparison)
  > - Verify `GetHandCategory()` returns expected types for known hands
  > - Verify `GetHandDescription()` returns readable descriptions
  > - Test winner determination in multi-player scenarios
  > - Edge cases: ties, split pots, wheel straight (A-2-3-4-5)
  > - Note: Library handles hand ranking logic; tests focus on integration correctness

- [ ] **Create pot calculation unit tests**
  Task ID: `core-gameplay-22`
  > **Implementation**: Create `tests/LowRollers.Api.Tests/Domain/Pots/`
  > **Details**:
  > - Simple pot (no all-ins)
  > - Single all-in side pot
  > - Multiple all-ins at different amounts
  > - Split pots with side pots
  > - All-in for exact amount (no side pot)

- [ ] **Create game flow integration tests**
  Task ID: `core-gameplay-23`
  > **Implementation**: Create `tests/LowRollers.Api.IntegrationTests/GameEngine/`
  > **Details**:
  > - Complete hand: deal → betting → showdown
  > - All players fold to one
  > - Heads-up all-in preflop
  > - Multi-way pot with side pots
  > - Timer expiration auto-fold

---

## Completion Checklist

- [ ] All poker actions work (fold, check, call, raise, all-in)
- [ ] Hand evaluation correct for all hand types
- [ ] Pot calculations correct including side pots
- [ ] Action timer works with auto-fold
- [ ] Hotkeys functional (F/C/R/A)
- [ ] State broadcasts to all players <100ms
- [ ] Hand history records all events
- [ ] 80% test coverage on game engine

---

*Generated by Clavix /clavix:plan*
