# Low Rollers

Private online poker platform for friend groups with integrated video chat and custom game variants.

## Overview

Low Rollers enables 4-10 friends to play Texas Hold'em online with:
- **Instant guest access** - No registration, just click invite link and enter a display name
- **Integrated video chat** - See and hear everyone, just like a home game
- **Custom variants** - Double board bomb pots and button money
- **Real-time sync** - <100ms action latency with graceful reconnection

## Project Structure

```
lowrollers/
├── docs/              # Project documentation
├── specs/             # Feature specifications (BRD, PRD)
├── src/               # Source code
├── tests/             # Test files
├── .clavix/           # Clavix workflow outputs
└── README.md          # This file
```

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | Angular 21, Signals |
| Backend | .NET 10.0 / ASP.NET Core |
| Real-time | SignalR (WebSockets) |
| Video | LiveKit (self-hosted WebRTC SFU) |
| Database | Azure PostgreSQL, Redis |
| Hosting | Azure Container Apps, .NET Aspire |

## Development Phases

1. **Phase 1: MVP** - Core poker gameplay with guest access
2. **Phase 2: Video** - Multi-party video chat integration
3. **Phase 3: Customization** - Bomb pots, button money, host config
4. **Phase 4: Polish** - Multi-table, spectator mode, optional accounts

## Documentation

- **BRD:** `specs/poker_brd_v1.1.md` - Business Requirements (1096 lines, 100+ requirements)
- **PRD:** `.clavix/outputs/lowrollers/full-prd.md` - Product Requirements
- **Quick PRD:** `.clavix/outputs/lowrollers/quick-prd.md` - AI-optimized summary

## Getting Started

*Coming soon - project scaffolding in progress*

## License

Private project - not for public distribution.
