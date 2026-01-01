# Implementation Tasks: Host Configuration

**Feature:** Host Configuration
**Spec:** [host-config-spec.md](./host-config-spec.md)
**Phase:** 3

---

## Settings Models

- [ ] **Create table settings domain models**
  Task ID: `config-01`
  > **Implementation**: Create `src/LowRollers.Api/Domain/Models/TableSettings.cs`
  > **Details**:
  > - `TableSettings` class with all configurable options
  > - `BombPotSettings` nested class
  > - `ButtonMoneySettings` nested class
  > - Validation attributes (ranges, required)
  > - Default values for quick start

- [ ] **Create template entity and repository**
  Task ID: `config-02`
  > **Implementation**: Create `src/LowRollers.Api/Features/TableConfiguration/Templates/`
  > **Details**:
  > - `TableTemplate.cs` - Entity
  > - `TableTemplateRepository.cs` - CRUD operations
  > - Store in PostgreSQL
  > - Scoped to host session (stored with session ID reference)

---

## Settings API

- [ ] **Create settings update endpoint**
  Task ID: `config-03`
  > **Implementation**: Create `src/LowRollers.Api/Features/TableConfiguration/UpdateTableSettingsCommand.cs`
  > **Details**:
  > - Validate all settings values in range
  > - Check if game in progress:
  >   - If not: apply immediately
  >   - If yes: require 67% vote
  > - Cannot change during active hand
  > - Broadcast changes to all players

- [ ] **Create settings change voting**
  Task ID: `config-04`
  > **Implementation**: Create `src/LowRollers.Api/Features/TableConfiguration/Voting/`
  > **Details**:
  > - `ProposeSettingsChangeCommand.cs` - Host proposes change
  > - `VoteOnSettingsChangeCommand.cs` - Player votes
  > - Track votes in Redis
  > - 60 second timeout
  > - If 67% approve, apply changes
  > - If rejected or timeout, cancel

---

## Template System

- [ ] **Create template CRUD endpoints**
  Task ID: `config-05`
  > **Implementation**: Create `src/LowRollers.Api/Features/TableConfiguration/Templates/`
  > **Details**:
  > - `SaveTemplateCommand.cs` - Save current settings as template
  > - `GetTemplatesQuery.cs` - List templates for host
  > - `LoadTemplateCommand.cs` - Apply template to table
  > - `UpdateTemplateCommand.cs` - Update existing template
  > - `DeleteTemplateCommand.cs` - Remove template
  > - Endpoints:
  >   - `POST /api/templates` - Save
  >   - `GET /api/templates` - List
  >   - `PUT /api/templates/{id}` - Update
  >   - `DELETE /api/templates/{id}` - Delete

---

## Angular Configuration UI

- [ ] **Create table settings form**
  Task ID: `config-06`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/table/configuration/table-settings/`
  > **Details**:
  > - Reactive form with all settings
  > - Sections: Basic, Timers, Game Flow, Side Games
  > - Validation feedback
  > - Save button (applies to table)
  > - Use PrimeNG InputNumber, Select, ToggleSwitch, Slider

- [ ] **Create blind selector component**
  Task ID: `config-07`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/table/configuration/blind-selector/`
  > **Details**:
  > - Preset buttons: $0.25/$0.50, $0.50/$1, $1/$2, $2/$5, $5/$10
  > - Custom input option
  > - Auto-calculate big blind (2x)
  > - Display format: "$1/$2"
  > - Use PrimeNG SelectButton, InputNumber

- [ ] **Create buy-in limits component**
  Task ID: `config-08`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/table/configuration/buy-in-limits/`
  > **Details**:
  > - Min buy-in slider (20x-100x BB)
  > - Max buy-in input (100x+ BB, optional no max)
  > - Display in BB multiples and dollar amounts
  > - Validation: min <= max
  > - Use PrimeNG Slider, InputNumber

- [ ] **Create timer configuration component**
  Task ID: `config-09`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/table/configuration/timer-config/`
  > **Details**:
  > - Action timer: 15s, 30s, 45s, 60s, Unlimited
  > - Time bank toggle
  > - Time bank duration: 30s, 60s, 90s
  > - Use PrimeNG SelectButton, ToggleSwitch

- [ ] **Create game flow configuration**
  Task ID: `config-10`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/table/configuration/game-flow-config/`
  > **Details**:
  > - Pause between hands: 0s, 3s, 5s, 10s
  > - Showdown duration: 3s, 5s, 10s, Manual
  > - Auto-muck losers toggle
  > - Disconnect handling: Auto-fold, Time bank, Sit-out
  > - Use PrimeNG SelectButton, ToggleSwitch

---

## Template Manager UI

- [ ] **Create template manager component**
  Task ID: `config-11`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/table/configuration/template-manager/`
  > **Details**:
  > - List saved templates
  > - Load button per template
  > - Edit button → opens settings pre-filled
  > - Delete button with confirmation
  > - Save current as new template button
  > - Template name input
  > - Use PrimeNG DataView, Button, Dialog, ConfirmDialog

---

## Voting UI

- [ ] **Create settings change vote dialog**
  Task ID: `config-12`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/table/configuration/change-vote-dialog/`
  > **Details**:
  > - Show proposed changes (diff view)
  > - Approve / Reject buttons
  > - Current vote tally
  > - Required votes indicator
  > - Countdown timer (60s)
  > - Auto-close on result
  > - Use PrimeNG Dialog, Button, ProgressBar

---

## Testing

- [ ] **Create configuration tests**
  Task ID: `config-13`
  > **Implementation**: Create `tests/LowRollers.Api.Tests/Features/TableConfiguration/`
  > **Details**:
  > - Settings validation (all range checks)
  > - Template CRUD operations
  > - Voting threshold calculation
  > - Vote timeout handling
  > - Cannot change mid-hand

---

## Completion Checklist

- [ ] All settings configurable
- [ ] Validation working
- [ ] Templates save/load working
- [ ] Mid-game changes require vote
- [ ] 67% threshold enforced
- [ ] Cannot change during hand
- [ ] All players see changes
- [ ] Presets for quick setup

---

*Generated by Clavix /clavix:plan*
