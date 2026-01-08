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

- [x] **Implement cryptographic shuffle**
  Task ID: `core-gameplay-03`
  > **Implementation**: Create `src/LowRollers.Api/Domain/Services/ShuffleService.cs`
  > **Details**:
  > - Use `System.Security.Cryptography.RandomNumberGenerator`
  > - Implement Fisher-Yates shuffle algorithm exactly per spec
  > - Create shuffle verification method
  > - Unit tests proving uniform distribution (chi-square test)

- [x] **Create finite state machine for hand phases**
  Task ID: `core-gameplay-04`
  > **Implementation**: Create `src/LowRollers.Api/Domain/StateMachine/`
  > **Details**:
  > - `HandPhase.cs` - Enum: Waiting, Preflop, Flop, Turn, River, Showdown, Complete
  > - `HandStateMachine.cs` - State transitions, validation
  > - `IHandPhaseHandler` interface for phase-specific logic
  > - Guards for valid transitions only
  > - Logging for state changes

- [x] **Implement betting logic**
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

- [x] **Implement pot management and side pots**
  Task ID: `core-gameplay-06`
  > **Implementation**: Create `src/LowRollers.Api/Domain/Pots/`
  > **Details**:
  > - `PotManager.cs` - Main pot and side pot calculations
  > - Handle all-in scenarios correctly:
  >   - Player all-in for less creates side pot
  >   - Multiple all-ins create multiple side pots
  >   - Track eligible players per pot
  > - Unit tests for complex scenarios (3+ all-ins at different amounts)

- [x] **Create event sourcing for hand history**
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

- [x] **Implement game flow orchestration**
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

- [x] **Implement showdown logic**
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

- [x] **Implement action timer system**
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

- [x] **Create player action SignalR methods**
  Task ID: `core-gameplay-11`
  > **Implementation**: Extend `src/LowRollers.Api/Hubs/GameHub.cs`
  > **Details**:
  > - `Fold()` - Validate turn, execute, broadcast
  > - `Check()` - Validate no bet facing, execute, broadcast
  > - `Call()` - Validate bet facing, match bet, broadcast
  > - `Raise(decimal amount)` - Validate min raise, execute, broadcast
  > - `AllIn()` - Execute all-in, broadcast
  > - All methods: validate it's player's turn, log action

- [x] **Implement game state broadcasting**
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

> **DESIGN REFERENCE**: Use `docs/designs/poker-table-mockup-v2.html` as the approved design reference for all UI components below. This mockup contains the complete styling, layout, and SVG assets (card backs, casino chips) to be used during implementation.

- [x] **Create poker table component**
  Task ID: `core-gameplay-13`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/poker-table/`
  > **Design Reference**: `docs/designs/poker-table-mockup-v2.html` - `.poker-table`, `.table-rail`, `.table-felt`, `.table-area`
  > **Details**:
  > - `poker-table.component.ts` - Main table layout
  > - Dark navy rail with green felt interior (racetrack/oval shape with ~42% border-radius)
  > - Position player seats around table edge (see seat positioning classes)
  > - Center area for community cards and pot display
  > - Diagonal pattern background (`.diagonal-pattern`)
  > - Responsive layout with scaling for different screen sizes
  > - Include dealer button component
  > - Use PrimeNG components where appropriate

- [x] **Create player seat component**
  Task ID: `core-gameplay-14`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/player-seat/`
  > **Design Reference**: `docs/designs/poker-table-mockup-v2.html` - `.player-seat`, `.player-container`, `.avatar-wrapper`, `.player-info`
  > **Details**:
  > - `player-seat.component.ts`
  > - Horizontal layout: avatar with info card beside it (direction varies by seat position)
  > - Avatar with emoji placeholder, status badges (mic/video icons)
  > - Hole cards below avatar using SVG card back pattern (`#cardBack`)
  > - Player info card: name, chip count (green text)
  > - Active player: orange border glow, pulsating indicator, action timer bar
  > - Folded state: greyed out avatar/cards, "Folded" label
  > - Bet display: chip stack graphics + amount badge, positioned toward table center
  > - Empty seat: dashed border, "Open Seat" text, hover state

- [x] **Create community cards component**
  Task ID: `core-gameplay-15`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/community-cards/`
  > **Design Reference**: `docs/designs/poker-table-mockup-v2.html` - `.community-cards`, `.card`
  > **Details**:
  > - Display 5 card positions in horizontal row
  > - Card design: white background, rank/suit in corners, large center suit
  > - Red color for hearts/diamonds, black for spades/clubs
  > - Cards appear as dealt (3 flop, 1 turn, 1 river)
  > - Card flip animation on deal
  > - Highlight winning cards at showdown

- [x] **Create pot display component**
  Task ID: `core-gameplay-16`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/pot-display/`
  > **Design Reference**: `docs/designs/poker-table-mockup-v2.html` - `.pot-display`, `.pot-chips`, `.pot-info`, `.chip-stack`
  > **Details**:
  > - Chip stack graphics using SVG symbols (`#chipRed`, `#chipBlue`, `#chipGreen`, `#chipBlack`)
  > - Stacked chip columns with overlapping effect
  > - Pot info card: "Main Pot" label + yellow amount
  > - Side pots listed separately with eligible player count
  > - Animate chip additions on pot changes

- [ ] **Create action panel component**
  Task ID: `core-gameplay-17`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/action-panel/`
  > **Design Reference**: `docs/designs/poker-table-mockup-v2.html` - `.action-panel`, `.action-btn`, `.btn-fold`, `.btn-call`, `.btn-raise`, `.btn-allin`
  > **Details**:
  > - `action-panel.component.ts`
  > - Fixed bottom bar with dark background
  > - Left section: player avatar, chip count, turn timer
  > - Center section: raise slider control
  > - Right section: action buttons (Fold/Call/Raise/All-In)
  > - Contextual buttons:
  >   - Fold (always when active)
  >   - Check (when no bet facing)
  >   - Call $X (when bet facing)
  >   - Raise (opens amount input)
  >   - All-In (with confirmation)
  > - Button colors: red=fold, green=call, blue=raise, orange=all-in
  > - Hotkey hints shown on buttons `[F]`, `[C]`, `[R]`, `[A]`
  > - Quick bet buttons row above: 2BB, 3BB, 1/2 Pot, POT, MAX, settings
  > - Use PrimeNG Button, ConfirmDialog
  > - Disable when not player's turn

- [ ] **Create raise slider component**
  Task ID: `core-gameplay-18`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/action-panel/raise-slider/`
  > **Design Reference**: `docs/designs/poker-table-mockup-v2.html` - `.raise-control`, `.raise-slider`, `.raise-input`
  > **Details**:
  > - Dark container with "Raise to:" label
  > - Slider from min raise to max (all-in)
  > - Blue slider thumb with shadow
  > - Number input for exact amount
  > - Quick bet buttons: 2BB, 3BB, 1/2 Pot, POT, MAX
  > - Use PrimeNG Slider, InputNumber

- [ ] **Create action timer component**
  Task ID: `core-gameplay-19`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/action-timer/`
  > **Design Reference**: `docs/designs/poker-table-mockup-v2.html` - `.action-timer-bar`, `.timer-bar-progress`, `.timer-text`
  > **Details**:
  > - Timer bar on active player's avatar (not separate component)
  > - Progress bar depletes over 30 seconds
  > - Color transitions: green → yellow → orange → red
  > - Digital time display below bar (e.g., "0:18")
  > - Time bank indicator if available
  > - Also show timer in action panel left section
  > - Use PrimeNG ProgressBar

- [ ] **Implement keyboard hotkeys**
  Task ID: `core-gameplay-20`
  > **Implementation**: Add to `src/LowRollers.Web/src/app/features/game/action-panel/`
  > **Design Reference**: `docs/designs/poker-table-mockup-v2.html` - `.hotkey` class on buttons
  > **Details**:
  > - F = Fold
  > - C = Call/Check (contextual)
  > - R = Raise (focus raise input)
  > - A = All-In (with confirmation)
  > - Only active when player's turn
  > - Hotkey hints displayed in brackets on each button

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
