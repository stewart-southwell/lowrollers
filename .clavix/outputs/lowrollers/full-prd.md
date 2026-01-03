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

This PRD provides a high-level overview. For detailed requirements, acceptance criteria, and user stories, see:

**📄 `specs/poker_brd_v1.1.md`** - Business Requirements Document (1096 lines)

| BRD Section | What It Contains |
|-------------|------------------|
| Section 4 | Host configuration system (30+ settings) |
| Section 5 | Functional requirements (100+ REQ-* items) |
| Section 6 | Non-functional requirements (40 REQ-NF-* items) |
| Section 7 | User stories with acceptance criteria (27 stories) |
| Section 11 | Acceptance criteria per phase |

---

## Phased Delivery

Development is structured in 4 phases. Each phase builds on the previous.

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

## Requirements

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
- Hand evaluation using **HoldemPoker.Evaluator** library (showdown logic: last aggressor shows first, muck option)
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

### Technical Requirements

#### Frontend
- **Framework:** Angular 21
- **State Management:** Angular services + signals (Angular 17+). No NgRx—SignalR pushes authoritative state, client is mostly a view.
- **Browser Support:** Modern browsers only (Chrome, Firefox, Safari, Edge - last 2 versions)
- **Responsive:** Desktop 1920x1080, laptop 1366x768 minimum

#### Backend
- **Framework:** .NET 10.0 / ASP.NET Core Web API 10.0 (C#)
- **Real-time:** SignalR (WebSockets with automatic fallback)
- **Pattern:** Lightweight CQRS—separate command handlers (PlayerAction, CreateTable) from query handlers (GetHandHistory, GetTableState). No MediatR, just organized folders.
- **Hand Evaluation:** HoldemPoker.Evaluator (NuGet package)
  - Package: `HoldemPoker.Evaluator` v1.0.1+ (Apache 2.0 license)
  - Dependency: `HoldemPoker.Cards` (>= 0.0.1)
  - API: `GetHandRanking()` for comparisons, `GetHandCategory()` for hand type, `GetHandDescription()` for display
  - Performance: Hashtable-cached lookups, evaluates hands in few CPU cycles

#### Video Chat
- **Approach:** Self-hosted WebRTC SFU using LiveKit
- **Rationale:** P2P struggles at 10 participants; Twilio/Daily.co incur per-minute costs unsuitable for monthly 4-hour sessions

#### Database
- **Primary:** Azure PostgreSQL for game state and hand history
- **Cache:** Redis for session state and real-time caching

#### Hosting
- **Platform:** Azure Container Apps
- **Orchestration:** .NET Aspire for local development and deployment
- **WebSocket Scaling:** Azure SignalR Service (managed)
- **Region:** Azure East US or Central US
- **Budget Target:** <$50/month (scheduled start/stop for monthly games)

#### Performance Requirements
- <100ms action latency (game actions)
- <500ms video latency
- <50ms sound effect latency
- 60fps during normal gameplay
- Animations complete within 500ms
- Scale to 100 concurrent tables / 1000 players
- Cryptographically secure RNG for Fisher-Yates shuffle
- TLS 1.3 everywhere

#### Operational Requirements
- 99.5% uptime during scheduled game hours
- Database backups every 6 hours
- Health check endpoints for monitoring
- Comprehensive logging for debugging
- Zero-downtime deployments

---

### Architecture & Design

#### Project Structure
- **Pattern:** Vertical slices over Clean Architecture
- **Organization:** Each feature (TableManagement, GameEngine, VideoChat) owns its handlers, models, and persistence
- **Rationale:** Less abstraction, easier to navigate. Clean Architecture adds layers unnecessary at 100 tables.

#### Game Engine
- **State Machine:** Finite state machine for hand phases
  - Waiting → Preflop → Flop → Turn → River → Showdown → Complete
- **Event Sourcing:** For hand history
  - Events: PlayerPosted, CardsDealt, PlayerActed, BoardDealt, PotAwarded
  - Natural fit since hands ARE a sequence of events
  - Replay any hand by replaying events
- **Authority:** Server-authoritative—client sends intents, server validates and broadcasts state

#### API Style
- **REST:** Table CRUD, join/leave, configuration, hand history queries
- **SignalR:** All in-game actions (fold/call/raise), state broadcasts, player presence, chat

#### Testing Strategy
- **Coverage Target:** 80%
- **Unit Tests:** Hand evaluation integration (HoldemPoker.Evaluator), pot/side-pot calculation, FSM transitions, shuffle verification
- **Integration Tests:** Full hand scenarios, reconnection flows, bomb pot triggers
- **Note:** Hand ranking logic tested via library integration; focus unit tests on pot distribution and game flow

#### UI Design Workflow
- **Design-First Approach:** All UI components require visual approval before implementation
- **Mockup Options:**
  - Agent generates a visual mockup for user approval, OR
  - User provides a screenshot/design reference to emulate
- **Approval Gate:** No UI implementation begins until design is approved
- **Design Reference:** Approved mockups/screenshots stored in `docs/designs/` for implementation reference
- **Iteration:** User may request design changes before approving

---

## Out of Scope

The following are explicitly NOT included in v1:

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

## Additional Context

### Team & Timeline
- **Team:** Solo developer using Claude Code for scaffolding, architecture, and test generation
- **Timeline:** No hard deadline. MVP target: 3-6 months of focused work
- **Existing Code:** Greenfield project—no legacy integration

### Compliance & Privacy
- **GDPR:** Not applicable (US friend group only)
- **Gambling Regulations:** Not applicable (play money only)
- **PII:** Minimal—just display names stored temporarily per session

### Definition of Done
Friends can play a full evening session (4+ hours) with bomb pots and button money, video chat stays stable, Blair doesn't have audio issues, and nobody asks "can we just go back to Zoom + PokerStars?"

---

## Refinement History

### January 3, 2026 (Update 2)

**Changes:**
- [ADDED] UI Design Workflow section in Architecture & Design
- [ADDED] Design-first approach requirement for all UI components
- [ADDED] Mockup approval gate before UI implementation
- [ADDED] `docs/designs/` as storage location for approved designs

**Why:** Ensure UI components match user expectations before implementation. Allows user to provide reference designs or approve generated mockups before coding begins.

---

### January 3, 2026

**Changes:**
- [ADDED] HoldemPoker.Evaluator library specification in Backend requirements
- [MODIFIED] Core gameplay section to reference HoldemPoker.Evaluator for hand evaluation
- [MODIFIED] Testing strategy to note library handles hand ranking logic

**Why:** Use established, high-performance NuGet library for hand evaluation instead of custom implementation. HoldemPoker.Evaluator provides hashtable-cached lookups with few-cycle evaluation performance, Apache 2.0 licensed.

---

### January 1, 2026

**Changes:**
- [ADDED] Phased delivery structure (4 phases from BRD)
- [ADDED] BRD reference section with section guide
- [ADDED] Host configuration system summary
- [ADDED] Bomb pot trigger methods (5 options)
- [ADDED] Button money chop rule
- [ADDED] Device selection for camera/mic
- [ADDED] Hotkeys (F/C/R/A)
- [ADDED] Away status feature
- [ADDED] Operational requirements (uptime, backups, logging)
- [ADDED] Animation and sound performance targets
- [MODIFIED] Out of scope clarified with Phase 4 notes

**Why:** Aligned PRD with comprehensive BRD (poker_brd_v1.1.md) while keeping PRD as high-level overview

---

*Generated with Clavix Planning Mode*
*Updated: 2026-01-01*
