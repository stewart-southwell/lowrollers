# Low Rollers - Quick PRD

> **Detailed Specification:** See `specs/poker_brd_v1.1.md` for 100+ functional requirements, 27 user stories, and acceptance criteria.

**Goal:** Build a private online poker platform for a friend group (4-10 players) to play Texas Hold'em with integrated video chat and custom game variants. Developed in 4 phases: (1) MVP with core gameplay, guest access, animations, and sounds; (2) Video integration via self-hosted LiveKit SFU; (3) Custom variants (bomb pots, button money), host configuration, text chat, and table templates; (4) Polish, multi-table, and optional accounts. Key differentiator: seamless video + custom variants no other platform offers.

**Technical Stack:** Angular 21 frontend with signals-based state management, .NET 10.0 / ASP.NET Core backend with lightweight CQRS, SignalR for real-time game actions, self-hosted LiveKit WebRTC SFU for video, Azure PostgreSQL + Redis for persistence, hosted on Azure Container Apps with .NET Aspire orchestration. Architecture uses vertical slices with FSM + event sourcing for the game engine. Performance targets: <100ms action latency, <500ms video latency, <50ms sound latency, 60fps gameplay, 500ms animation completion. Budget target <$50/month using scheduled start/stop.

**Scope Boundaries:** No real money (play money only), no public matchmaking (private invite-link only), no mobile native apps (responsive web), no tournaments (cash game only), no other poker variants, no AI bots, no mandatory accounts (guest access primary, optional accounts in Phase 4), no persistent chips or user profiles, no UI customization, fixed hotkeys (F/C/R/A), no hand replayer UI. Success criteria: 4-10 players with <100ms latency, 30-second join time, stable 4+ hour video sessions, zero game-corrupting bugs, 95% friend group satisfaction.

---

*Generated with Clavix Planning Mode*
*Updated: 2026-01-01*
