# Feature Specification: Table Management

## Overview

Table creation, guest access, invite links, and session management. This is the entry point for all users.

**Phase:** 1 (MVP)
**Priority:** Critical
**BRD References:** REQ-UM-001 to REQ-UM-022, REQ-LT-001 to REQ-LT-010

---

## User Stories

### US-001: Quick Join
> As a new player, I want to join a table by clicking an invite link so I can start playing immediately without registration.

**Acceptance Criteria:**
- User clicks invite link shared by host
- User prompted only for display name (2-20 characters)
- Display name validated for uniqueness at table
- User immediately joins table after name entry
- No email, password, or account creation required

### US-002: Returning Player
> As a returning player, I want to use the same display name so friends recognize me.

**Acceptance Criteria:**
- User can enter previously used display name
- System allows same name if not currently in use at table
- If disconnected within 5 minutes, user can rejoin with same name and restore chip stack
- Display name persists in browser for convenience

### US-003: Table Access Control
> As a host, I want to control table access so only invited friends can join.

**Acceptance Criteria:**
- System generates unique invite link/code when table created
- Only users with invite link can access table
- Host can optionally set table password for additional security
- Host can kick unwanted guests
- Host can ban specific display names from rejoining

### US-004: Starting Chips
> As a guest player, I want to start with chips immediately so I can begin playing without setup delays.

**Acceptance Criteria:**
- System automatically assigns starting chip balance based on table buy-in
- User can adjust buy-in amount within table limits before sitting
- Chips are virtual and reset each game session
- User can rebuy chips during game according to table rules
- No cross-session chip tracking (fresh start each game)

### US-005: Create Table
> As a host, I want to create a private table quickly so I can get the game started.

**Acceptance Criteria:**
- User enters display name (first time only)
- User can configure table settings efficiently
- System generates shareable invite link immediately
- Host can copy invite link with one click
- Table remains open until host closes it or all players leave

### US-006: Join via Link
> As a player, I want to join via invite link so I can get into the game quickly.

**Acceptance Criteria:**
- User clicks invite link from any device
- User prompted only for display name (if first time joining)
- User sees table interface immediately after name entry
- Available seats clearly visible
- User can select seat and set buy-in amount within table limits

---

## Functional Requirements

### Guest Access (REQ-UM-001 to REQ-UM-007)
| ID | Requirement |
|----|-------------|
| REQ-UM-001 | Join tables as guests without creating an account |
| REQ-UM-002 | Provide display name when joining table |
| REQ-UM-003 | Validate display name uniqueness within each table |
| REQ-UM-004 | Display names between 2-20 characters |
| REQ-UM-005 | Assign temporary session ID to guest users |
| REQ-UM-006 | Session persists until browser closes or user leaves |
| REQ-UM-007 | Allow rejoin with same name if disconnected within 5 minutes |

### Table Access Control (REQ-UM-012 to REQ-UM-016)
| ID | Requirement |
|----|-------------|
| REQ-UM-012 | Tables protected by unique invite code or URL |
| REQ-UM-013 | Optional table password for additional security |
| REQ-UM-014 | Require invite link/code to access private table |
| REQ-UM-015 | Host can kick unwanted guests |
| REQ-UM-016 | Host can ban specific display names from rejoining |

### Session Chip Management (REQ-UM-017 to REQ-UM-022)
| ID | Requirement |
|----|-------------|
| REQ-UM-017 | Start with virtual chip balance defined by table buy-in |
| REQ-UM-018 | Chip balances persist only for duration of game session |
| REQ-UM-019 | Rebuy chips during session according to table rules |
| REQ-UM-020 | Chip balances reset when table closes or user leaves permanently |
| REQ-UM-021 | No chip balance tracking across different sessions |
| REQ-UM-022 | Host configures starting chip amount within buy-in limits |

### Table Creation (REQ-LT-001 to REQ-LT-005)
| ID | Requirement |
|----|-------------|
| REQ-LT-001 | Create new table without registration |
| REQ-LT-002 | Provide display name if first time accessing |
| REQ-LT-003 | Generate unique invite link/code for each table |
| REQ-LT-004 | Invite links valid until table closed by host |
| REQ-LT-005 | Create table with configurable settings |

### Table Access (REQ-LT-006 to REQ-LT-010)
| ID | Requirement |
|----|-------------|
| REQ-LT-006 | Access tables exclusively via invite link (no public lobby) |
| REQ-LT-007 | Users without invite cannot find or access private tables |
| REQ-LT-008 | Validate invite link before allowing table access |
| REQ-LT-009 | Clear error message for expired/invalid invite links |
| REQ-LT-010 | Host can regenerate invite link (invalidates old link) |

---

## Technical Design

### Domain Models

```
Table
  - Id: Guid
  - InviteCode: string (8-12 chars, cryptographically secure)
  - InviteCodeHash: string (stored, not plain text)
  - Password: string? (optional, hashed)
  - HostSessionId: Guid
  - Name: string
  - Settings: TableSettings
  - Status: TableStatus (Waiting, Playing, Paused, Closed)
  - CreatedAt: DateTime
  - Players: List<Player>
  - BannedNames: List<string>

GuestSession
  - Id: Guid
  - DisplayName: string
  - TableId: Guid
  - ChipStack: decimal
  - SeatPosition: int?
  - ConnectedAt: DateTime
  - LastSeenAt: DateTime
  - Status: SessionStatus (Connected, Disconnected, Expired)
```

### API Endpoints

```
POST   /api/tables                    Create table, returns invite code
GET    /api/tables/validate/{code}    Validate invite code exists
POST   /api/tables/{id}/join          Join table with display name
POST   /api/tables/{id}/leave         Leave table
POST   /api/tables/{id}/sit           Take seat at position
POST   /api/tables/{id}/stand         Stand up from seat
POST   /api/tables/{id}/buy-in        Set initial or additional chips
DELETE /api/tables/{id}/kick/{playerId}   Host kicks player
POST   /api/tables/{id}/ban           Host bans display name
POST   /api/tables/{id}/regenerate-invite  Host regenerates invite
POST   /api/tables/{id}/transfer-host      Host transfers to another player
```

### Session Flow

```
1. User clicks invite link → /join/{inviteCode}
2. Validate invite code exists and table is active
3. Prompt for display name (or use cached from localStorage)
4. Validate display name:
   - 2-20 characters
   - Unique at this table
   - Not banned
5. Create GuestSession in Redis:
   - JWT token with SessionId, DisplayName, TableId
   - 5-minute reconnection window on disconnect
6. Return JWT, redirect to table view
7. User selects seat, sets buy-in
8. User joins SignalR group for table
```

---

## UI Components

| Component | Description |
|-----------|-------------|
| `create-table` | Form for new table with settings |
| `join-table` | Invite code validation and name entry |
| `table-lobby` | Pre-game waiting room |
| `seat-selector` | Available seats display |
| `buy-in-dialog` | Chip amount selection |
| `player-list` | Current players with status |

---

## Security Considerations

- Invite codes cryptographically secure (not guessable)
- Store invite code hash, not plain text
- Optional password adds second factor
- Session tokens are JWTs with short expiry
- Rate limit join attempts
- Log all access attempts

---

*See [table-management-tasks.md](./table-management-tasks.md) for implementation tasks*
