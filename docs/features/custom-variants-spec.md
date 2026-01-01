# Feature Specification: Custom Variants

## Overview

Custom poker variants unique to the friend group: Double Board Bomb Pots and Button Money. These are the key differentiators from other poker platforms.

**Phase:** 3
**Priority:** High
**BRD References:** REQ-CV-001 to REQ-CV-017

---

## Bomb Pots

### User Stories

#### US-012: Play Bomb Pot
> As a player, I want to participate in bomb pot hands so I can experience the high-action variant we play in person.

**Acceptance Criteria:**
- System collects ante from all players automatically
- Two separate boards are dealt and displayed clearly
- Each board evaluated independently at showdown
- Winner(s) announced for each board with 50% pot allocation

#### US-013: Configure Bomb Pot
> As a table host, I want to configure bomb pot frequency so games match our group's preferences.

**Acceptance Criteria:**
- Setting to trigger bomb pot every N hands (configurable 5-25)
- Option for player voting with configurable threshold
- Option to trigger bomb pot when button money is won
- Current bomb pot status visible to all players
- Bomb pot rules explained in tooltip/help

### Functional Requirements (REQ-CV-001 to REQ-CV-010)

| ID | Requirement |
|----|-------------|
| REQ-CV-001 | When triggered, collect ante from all players (no folding) |
| REQ-CV-002 | Skip preflop betting round in bomb pot hands |
| REQ-CV-003 | Deal two separate flops simultaneously |
| REQ-CV-004 | Proceed through normal betting for both boards |
| REQ-CV-005 | Evaluate each board independently at showdown |
| REQ-CV-006 | Award 50% of pot to best hand on each board |
| REQ-CV-007 | Same player may win both boards (scoop) |
| REQ-CV-008 | Trigger via configured hand interval |
| REQ-CV-009 | Trigger via player voting |
| REQ-CV-010 | Trigger via button money win |

### Trigger Methods

| Method | Configuration |
|--------|---------------|
| Fixed Interval | Every N hands (5, 10, 15, 20, 25) |
| Random Percentage | X% chance per hand (5%, 10%, 15%, 20%) |
| Player Voting | Threshold required (50%, 67%, 75%, 100%) |
| Manual | Host triggers specific hands |
| Button Money Win | Auto-trigger when button wins kitty |

---

## Button Money

### User Stories

#### US-014: Win Button Kitty
> As a button position player, I want to have a chance to win the accumulated button kitty so I can enjoy the extra pot incentive.

**Acceptance Criteria:**
- Button contribution collected automatically each hand
- Current kitty amount displays near button indicator
- Kitty awarded to button player when winning pot
- Kitty rolls over when not won
- If button chops pot, kitty splashes into next pot

### Functional Requirements (REQ-CV-011 to REQ-CV-017)

| ID | Requirement |
|----|-------------|
| REQ-CV-011 | Maintain accumulating "button kitty" separate from main pot |
| REQ-CV-012 | Button position contributes to kitty each hand |
| REQ-CV-013 | Only button position player eligible to win kitty |
| REQ-CV-014 | Button wins kitty when winning any pot |
| REQ-CV-015 | Kitty rolls over to next hand if not won |
| REQ-CV-016 | Display current kitty amount to all players |
| REQ-CV-017 | If button chops, kitty splashes into next pot |

---

## Technical Design

### Bomb Pot Domain

```csharp
public class BombPotSettings
{
    public bool Enabled { get; set; }
    public BombPotVariant Variant { get; set; } // Single or Double
    public decimal AnteAmount { get; set; }
    public BombPotTrigger Trigger { get; set; }
    public int? IntervalHands { get; set; }
    public int? RandomPercentage { get; set; }
    public int? VotingThreshold { get; set; }
}

public enum BombPotVariant { SingleBoard, DoubleBoard }
public enum BombPotTrigger { Interval, Random, Voting, Manual, ButtonMoneyWin }
```

### Button Money Domain

```csharp
public class ButtonMoneySettings
{
    public bool Enabled { get; set; }
    public decimal ContributionAmount { get; set; }
    public bool TriggersBombPot { get; set; }
}

public class ButtonMoneyState
{
    public decimal KittyAmount { get; set; }
    public int HandsAccumulated { get; set; }
}
```

### Double Board Showdown Logic

```
1. Evaluate each board independently
2. For each board:
   - Find best hand among remaining players
   - Handle ties (split that board's half)
3. Award 50% to Board A winner(s)
4. Award 50% to Board B winner(s)
5. If same player wins both = scoop (100%)
```

---

## UI Components

| Component | Description |
|-----------|-------------|
| `bomb-pot-announcement` | Overlay when bomb pot triggers |
| `double-board-display` | Two board layouts |
| `pot-split-display` | Shows 50/50 allocation |
| `kitty-display` | Current button money amount |
| `bomb-pot-voting` | Player voting interface |

---

*See [custom-variants-tasks.md](./custom-variants-tasks.md) for implementation tasks*
