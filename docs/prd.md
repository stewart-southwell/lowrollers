# Product Requirements Document: Low Rollers

## Problem & Goal

Existing poker platforms don't support our friend group's custom game variants (double board bomb pots, button money) and have poor video integration. Build a private online poker platform where 4-10 friends can play Texas Hold'em with integrated video chat, custom side games, and instant guest access—no registration required.

**Target Users:** A specific friend group (4-10 players) who play monthly poker sessions lasting 4+ hours.

**Success Criteria:**
- Support 4-10 players at single table with <100ms action latency
- Players join within 30 seconds of clicking invite link
- Stable video chat throughout entire session (4+ hours)
- Zero critical bugs causing game state corruption or unfair outcomes
- 95%+ satisfaction from the friend group after testing
- Friends don't ask "can we just go back to Zoom + PokerStars?"

---

## Detailed Specification Reference

For detailed requirements, acceptance criteria, and user stories, see:

**`specs/poker_brd_v1.1.md`** - Business Requirements Document (1096 lines)

| BRD Section | What It Contains |
|-------------|------------------|
| Section 4 | Host configuration system (30+ settings) |
| Section 5 | Functional requirements (100+ REQ-* items) |
| Section 6 | Non-functional requirements (40 REQ-NF-* items) |
| Section 7 | User stories with acceptance criteria (27 stories) |
| Section 11 | Acceptance criteria per phase |

---

## Phased Delivery

### Phase 1: MVP - Core Gameplay
**Goal:** Playable poker with guest access

| Feature | Details |
|---------|---------|
| Texas Hold'em gameplay | Full game flow, betting, showdown, pot management |
| Guest access | Invite link, display name (2-20 chars), no registration |
| Real-time sync | <100ms state updates, reconnection within 5 minutes |
| Hand history | Session-based recording of all hands |
| Core animations | Card dealing, chip movements, pot collection |
| Sound effects | Cards, chips, action confirmations |

**Key BRD References:** REQ-GP-001 to GP-034, REQ-UM-001 to UM-022

### Phase 2: Video Integration
**Goal:** See and hear friends during play

| Feature | Details |
|---------|---------|
| Multi-party video | Up to 10 simultaneous streams via LiveKit SFU |
| Audio/video controls | Mute mic, disable camera, device selection |
| Network resilience | Game continues if video fails |
| Bandwidth adaptation | Auto-adjust quality, prioritize audio over video |

**Key BRD References:** REQ-VC-001 to VC-011

### Phase 3: Customization & Side Games
**Goal:** Match the home game experience

| Feature | Details |
|---------|---------|
| Host configuration | Blinds, buy-ins, timers, game flow settings |
| Bomb pots | Single/double board, 5 trigger methods |
| Button money | Configurable contribution, kitty tracking, chop rules |
| Table templates | Save/load configurations for recurring games |
| Text chat | Messages, timestamps, URL detection, system events |
| Hotkeys | Fixed scheme: F (fold), C (call/check), R (raise), A (all-in) |
| Away status | Skip dealing to away players, missed blind tracking |

**Key BRD References:** HOST-* requirements, REQ-CV-001 to CV-017, REQ-CHAT-001 to CHAT-013

### Phase 4: Polish & Scale
**Goal:** Production-ready for expanded use

| Feature | Details |
|---------|---------|
| Multi-table support | Multiple concurrent private tables |
| Spectator mode | Watch games without playing |
| Optional accounts | Persistent stats/history (guest access remains default) |
| Performance optimization | Based on real-world feedback |
| Advanced statistics | Hand analysis tools |

**Key BRD References:** Section 2.1 Phase 4, Section 11.4

---

## Requirements Summary

### Must-Have Features

#### 1. Invite-Link Table Access
- Host creates table, gets shareable link
- Guest clicks link, enters display name (2-20 characters), joins immediately
- No registration, no accounts, no passwords
- Session-based access only
- Host can kick/ban players, transfer host role, regenerate invite link

#### 2. Core Texas Hold'em Gameplay
- Deal hole cards to each player
- Full betting rounds: fold, check, call, raise, all-in
- Community cards: flop (3), turn (1), river (1)
- Pot management with side pot calculations
- Hand evaluation and showdown logic (last aggressor shows first, muck option)
- Server-authoritative game state (client sends intents, server validates)
- Cryptographically secure RNG with Fisher-Yates shuffle

#### 3. Real-Time State Synchronization
- All players see identical game state within 100ms
- Graceful disconnection handling:
  - Auto-fold on timeout
  - Rejoin within 5 minutes with state preserved
- Optimistic UI with server reconciliation

#### 4. Integrated Video Chat
- Support up to 10 simultaneous video streams
- Individual camera/mic controls per player
- Camera and microphone device selection (persisted in localStorage)
- Video latency <500ms
- Game continues uninterrupted if video fails
- Key differentiator: feels like home game, not PokerStars + Discord

#### 5. Custom Variants: Bomb Pots + Button Money
- **Bomb Pots:** Single or double board, configurable ante
  - Triggers: fixed interval, random %, player voting, manual, button money win
  - Skip preflop betting, all players must participate
  - Double board splits pot 50/50 between boards
- **Button Money:** Configurable contribution per hand
  - Only button position eligible to win kitty
  - Kitty awarded when button wins pot
  - If button chops, kitty splashes into next pot
  - Resets each session

#### 6. Host Configuration System
- **Table settings:** Blinds, buy-in limits, action timers, time bank
- **Game flow:** Pause between hands, showdown duration, auto-muck
- **Templates:** Save/load table configurations
- **Controls:** Start/pause/stop game, mid-game changes require 67% player approval

---

## Out of Scope

| Exclusion | Rationale |
|-----------|-----------|
| Real money / payment processing | Play money only—avoids gambling regulations |
| Public matchmaking | Private invite-link only—this is for friends |
| Mobile native apps | Responsive web only |
| Tournament structures | Cash game format only |
| Other poker variants | Texas Hold'em only (no Omaha, Stud, etc.) |
| AI/bot players | Human players only |
| Account system / registration | Guest access only (optional accounts in Phase 4) |
| Persistent chip balances | Resets each game session |
| User profiles / long-term statistics | No persistent identity (Phase 4 consideration) |
| UI theming / customization | No card backs, table colors, etc. |
| Customizable hotkeys | Fixed scheme: F/C/R/A |
| Hand replayer UI | History stored but no visual replay |
| Advanced analytics dashboards | No charts or metrics UI |

---

## Definition of Done

Friends can play a full evening session (4+ hours) with bomb pots and button money, video chat stays stable, Blair doesn't have audio issues, and nobody asks "can we just go back to Zoom + PokerStars?"

---

*Generated with Clavix Planning Mode*
*Last Updated: 2026-01-01*
