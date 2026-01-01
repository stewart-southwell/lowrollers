# Feature Specification: Host Configuration

## Overview

Comprehensive host configuration system for table settings, game flow, and reusable templates.

**Phase:** 3
**Priority:** High
**BRD References:** HOST-ROLE-001 to HOST-ROLE-008, HOST-TABLE-001 to HOST-TABLE-009, HOST-TMPL-001 to HOST-TMPL-004

---

## User Stories

### US-020: Configure Game
> As a table host, I want to configure all game settings before starting so the game matches our group's preferences.

**Acceptance Criteria:**
- Host can set blinds, buy-in limits, and max players
- Host can enable/disable bomb pots and configure trigger method
- Host can enable/disable button money and set contribution amount
- Settings display clearly to all players before game starts
- Host can modify settings before dealing first hand

### US-021: Save Templates
> As a table host, I want to save my table configuration as a template so I can quickly recreate our favorite game setup.

**Acceptance Criteria:**
- Host can save current configuration with custom name
- Host can load saved template when creating new table
- Template includes all game settings (blinds, side games, timers)
- Host can edit and delete saved templates

### US-022: Mid-Game Changes
> As a table host, I want to control side game triggers during the session so we can adapt to the group's mood.

**Acceptance Criteria:**
- Host can manually trigger bomb pot for next hand
- Host can enable player voting for bomb pots with configurable threshold
- Changes require player approval if game is in progress (67% agreement)
- Players receive notification of configuration changes
- Changes cannot occur during active hand

---

## Functional Requirements

### Host Role (HOST-ROLE-001 to HOST-ROLE-008)
| ID | Requirement |
|----|-------------|
| HOST-ROLE-001 | Table creator is automatically designated as host |
| HOST-ROLE-002 | Host can transfer privileges to another seated player |
| HOST-ROLE-003 | If host leaves, promote longest-seated player |
| HOST-ROLE-004 | Host can modify settings before game starts |
| HOST-ROLE-005 | Host can modify certain settings during game with player agreement |
| HOST-ROLE-006 | Host can pause game for breaks with all player consent |
| HOST-ROLE-007 | Host can remove disruptive players (kick) |
| HOST-ROLE-008 | Host can save table configuration as template |

### Table Settings (HOST-TABLE-001 to HOST-TABLE-009)
| ID | Requirement |
|----|-------------|
| HOST-TABLE-001 | Configure small blind ($0.25, $0.50, $1, $2, $5, $10, custom) |
| HOST-TABLE-002 | Big blind = 2x small blind (enforced) |
| HOST-TABLE-003 | Minimum buy-in (20x-100x big blind) |
| HOST-TABLE-004 | Maximum buy-in (100x+ big blind) |
| HOST-TABLE-005 | Table capacity (2-10 players) |
| HOST-TABLE-006 | Action timer (15s, 30s, 45s, 60s, unlimited) |
| HOST-TABLE-007 | Time bank enable/duration (30s, 60s, 90s) |
| HOST-TABLE-008 | Table privacy (private invite-only or public) |
| HOST-TABLE-009 | Table name for identification |

### Game Flow (HOST-FLOW-002 to HOST-FLOW-005)
| ID | Requirement |
|----|-------------|
| HOST-FLOW-002 | Pause between hands (0s, 3s, 5s, 10s) |
| HOST-FLOW-003 | Showdown display duration (3s, 5s, 10s, manual) |
| HOST-FLOW-004 | Auto-muck losing hands toggle |
| HOST-FLOW-005 | Disconnection handling (auto-fold, time bank, sit-out) |

### Templates (HOST-TMPL-001 to HOST-TMPL-004)
| ID | Requirement |
|----|-------------|
| HOST-TMPL-001 | Save complete configuration as named template |
| HOST-TMPL-002 | Load saved templates when creating new table |
| HOST-TMPL-003 | Edit and update saved templates |
| HOST-TMPL-004 | Delete saved templates |

### Mid-Game Changes (PREF-PREC-001 to PREF-PREC-003)
| ID | Requirement |
|----|-------------|
| PREF-PREC-001 | Table configuration overrides user preferences |
| PREF-PREC-002 | Mid-game changes require 67% player approval |
| PREF-PREC-003 | Side game triggers cannot be modified mid-hand |

---

## Technical Design

### TableSettings Model

```csharp
public class TableSettings
{
    // Basic
    public string TableName { get; set; }
    public decimal SmallBlind { get; set; }
    public decimal BigBlind => SmallBlind * 2;
    public decimal MinBuyIn { get; set; }
    public decimal MaxBuyIn { get; set; }
    public int MaxPlayers { get; set; } // 2-10

    // Timers
    public int ActionTimerSeconds { get; set; } // 0 = unlimited
    public bool TimeBankEnabled { get; set; }
    public int TimeBankSeconds { get; set; }

    // Game Flow
    public int PauseBetweenHandsSeconds { get; set; }
    public int ShowdownDurationSeconds { get; set; } // 0 = manual
    public bool AutoMuckLosers { get; set; }
    public DisconnectHandling DisconnectHandling { get; set; }

    // Side Games
    public BombPotSettings BombPot { get; set; }
    public ButtonMoneySettings ButtonMoney { get; set; }
}

public enum DisconnectHandling { AutoFold, UseTimeBank, SitOut }
```

### Template Model

```csharp
public class TableTemplate
{
    public Guid Id { get; set; }
    public Guid HostSessionId { get; set; }
    public string Name { get; set; }
    public TableSettings Settings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Mid-Game Change Voting

```csharp
public class SettingsChangeRequest
{
    public Guid Id { get; set; }
    public Guid TableId { get; set; }
    public TableSettings ProposedSettings { get; set; }
    public Dictionary<Guid, bool> Votes { get; set; } // PlayerId -> Vote
    public int RequiredApprovals => (int)Math.Ceiling(TotalPlayers * 0.67);
    public DateTime ExpiresAt { get; set; } // 60 second timeout
}
```

---

## UI Components

| Component | Description |
|-----------|-------------|
| `table-settings` | Full settings form |
| `blind-selector` | Blind amount picker |
| `buy-in-limits` | Min/max buy-in config |
| `timer-config` | Action timer and time bank |
| `game-flow-config` | Pause, showdown, auto-muck |
| `template-manager` | Save/load/delete templates |
| `change-vote-dialog` | Vote on mid-game changes |

---

*See [host-config-tasks.md](./host-config-tasks.md) for implementation tasks*
