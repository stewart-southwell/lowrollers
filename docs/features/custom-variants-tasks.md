# Implementation Tasks: Custom Variants

**Feature:** Custom Variants (Bomb Pots + Button Money)
**Spec:** [custom-variants-spec.md](./custom-variants-spec.md)
**Phase:** 3

---

## Bomb Pots - Backend

- [ ] **Implement bomb pot trigger service**
  Task ID: `variants-01`
  > **Implementation**: Create `src/LowRollers.Api/Features/CustomVariants/BombPot/BombPotTriggerService.cs`
  > **Details**:
  > - `ShouldTriggerBombPot(TableState)` - Check all trigger conditions
  > - Interval: track hands since last bomb pot
  > - Random: use crypto RNG for percentage check
  > - Voting: check if threshold met
  > - Manual: check host trigger flag
  > - Button Money Win: check if button won last hand

- [ ] **Implement bomb pot game flow**
  Task ID: `variants-02`
  > **Implementation**: Create `src/LowRollers.Api/Features/CustomVariants/BombPot/BombPotHandler.cs`
  > **Details**:
  > - Collect ante from all players (no fold option)
  > - Skip preflop betting (move directly to flop)
  > - Deal two boards for double variant
  > - Track both boards through turn and river
  > - Single betting round per street (applies to both boards)

- [ ] **Implement double board evaluation**
  Task ID: `variants-03`
  > **Implementation**: Create `src/LowRollers.Api/Features/CustomVariants/BombPot/DoubleBoardEvaluator.cs`
  > **Details**:
  > - `EvaluateBoardA(players, boardA, holeCards)` - Best hand on board A
  > - `EvaluateBoardB(players, boardB, holeCards)` - Best hand on board B
  > - Split pot 50/50 between boards
  > - Handle ties on individual boards
  > - Handle scoop (same player wins both)
  > - Side pots apply to each board independently

- [ ] **Implement bomb pot voting**
  Task ID: `variants-04`
  > **Implementation**: Create `src/LowRollers.Api/Features/CustomVariants/BombPot/BombPotVoting/`
  > **Details**:
  > - `StartVoteCommand.cs` - Initiate vote (any player or auto)
  > - `CastVoteCommand.cs` - Player votes yes/no
  > - Track votes, check against threshold
  > - Timeout for voting (30 seconds)
  > - If threshold met, trigger bomb pot next hand

---

## Bomb Pots - Frontend

- [ ] **Create bomb pot announcement component**
  Task ID: `variants-05`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/bomb-pot/bomb-pot-announcement/`
  > **Details**:
  > - Full-screen overlay announcement
  > - Animation effect (explosion/dramatic)
  > - Sound effect trigger
  > - Display ante amount
  > - Fade after 2 seconds

- [ ] **Create double board display component**
  Task ID: `variants-06`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/bomb-pot/double-board/`
  > **Details**:
  > - Two community card displays side by side
  > - Labels: "Board A" / "Board B"
  > - Highlight winning board at showdown
  > - Show 50% pot allocation per board

- [ ] **Create pot split display**
  Task ID: `variants-07`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/bomb-pot/pot-split-display/`
  > **Details**:
  > - Visual split of pot
  > - Arrow to each board's winner
  > - Amount per winner
  > - Scoop indicator if same player

- [ ] **Create bomb pot voting component**
  Task ID: `variants-08`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/bomb-pot/voting/`
  > **Details**:
  > - Popup when vote initiated
  > - Yes/No buttons
  > - Current vote tally
  > - Threshold indicator
  > - Countdown timer
  > - Use PrimeNG Dialog, Button, ProgressBar

---

## Button Money - Backend

- [ ] **Implement button money service**
  Task ID: `variants-09`
  > **Implementation**: Create `src/LowRollers.Api/Features/CustomVariants/ButtonMoney/ButtonMoneyService.cs`
  > **Details**:
  > - `CollectContribution(tableId)` - Add to kitty
  > - `CheckButtonWin(tableId, winnerId)` - Did button win?
  > - `AwardKitty(tableId, playerId)` - Transfer kitty to player
  > - `SplashKitty(tableId)` - Add kitty to next pot (on chop)
  > - `GetKittyAmount(tableId)` - Current kitty
  > - `ResetKitty(tableId)` - New session

- [ ] **Integrate button money with game flow**
  Task ID: `variants-10`
  > **Implementation**: Extend `src/LowRollers.Api/Features/GameEngine/GameOrchestrator.cs`
  > **Details**:
  > - At hand start: collect button contribution
  > - At showdown: check if button position won
  > - If button wins outright: award kitty
  > - If button chops: splash into next pot
  > - If button loses: kitty rolls over
  > - Trigger bomb pot if configured

---

## Button Money - Frontend

- [ ] **Create kitty display component**
  Task ID: `variants-11`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/button-money/kitty-display/`
  > **Details**:
  > - Display near dealer button
  > - Current kitty amount
  > - Chip stack visual
  > - Pulsing animation when large
  > - Tooltip: "Button wins this when they win the pot"

- [ ] **Create kitty award animation**
  Task ID: `variants-12`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/button-money/kitty-award/`
  > **Details**:
  > - Animation: kitty flies to winner
  > - Celebration effect
  > - Sound effect
  > - Amount display

- [ ] **Create kitty splash animation**
  Task ID: `variants-13`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/button-money/kitty-splash/`
  > **Details**:
  > - Animation: kitty splits into pot
  > - "Kitty splashed into next pot" message
  > - Occurs on button chop scenario

---

## Configuration UI

- [ ] **Create bomb pot settings component**
  Task ID: `variants-14`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/table/configuration/bomb-pot-config/`
  > **Details**:
  > - Enable/disable toggle
  > - Variant selection (single/double)
  > - Ante amount input
  > - Trigger method selection
  > - Interval hands slider
  > - Random percentage slider
  > - Voting threshold dropdown
  > - Use PrimeNG ToggleSwitch, Select, Slider

- [ ] **Create button money settings component**
  Task ID: `variants-15`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/table/configuration/button-money-config/`
  > **Details**:
  > - Enable/disable toggle
  > - Contribution amount input
  > - Triggers bomb pot toggle
  > - Use PrimeNG ToggleSwitch, InputNumber

---

## Testing

- [ ] **Create bomb pot unit tests**
  Task ID: `variants-16`
  > **Implementation**: Create `tests/LowRollers.Api.Tests/Features/CustomVariants/BombPot/`
  > **Details**:
  > - Ante collection from all players
  > - Preflop skipped
  > - Double board evaluation correct
  > - 50/50 split correct
  > - Scoop detection correct
  > - All trigger methods work
  > - Voting threshold logic

- [ ] **Create button money unit tests**
  Task ID: `variants-17`
  > **Implementation**: Create `tests/LowRollers.Api.Tests/Features/CustomVariants/ButtonMoney/`
  > **Details**:
  > - Contribution collected each hand
  > - Kitty awarded on button win
  > - Kitty rolls over on button loss
  > - Kitty splashes on button chop
  > - Bomb pot triggered on win (if configured)

---

## Completion Checklist

- [ ] Bomb pot triggers correctly (all 5 methods)
- [ ] Double board dealt and displayed correctly
- [ ] Each board evaluated independently
- [ ] 50/50 split correct
- [ ] Scoop (same winner) works
- [ ] Button money collects each hand
- [ ] Button wins kitty when winning pot
- [ ] Kitty rolls over when button loses
- [ ] Kitty splashes on chop
- [ ] Configuration UI working
- [ ] All animations smooth

---

*Generated by Clavix /clavix:plan*
