# Low Rollers - Claude Code Context

## Project Overview

**Low Rollers** is a private online poker platform for friend groups with integrated video chat and custom game variants. The platform enables 4-10 friends to play Texas Hold'em online with instant guest access, real-time video chat, and custom game modes like double board bomb pots and button money.

## Tech Stack

| Layer | Technology | Notes |
|-------|------------|-------|
| Frontend | Angular 21 | Using Signals for state management |
| Backend | .NET 10.0 / ASP.NET Core | C# backend services |
| Real-time | SignalR | WebSocket-based for <100ms latency |
| Video | LiveKit | Self-hosted WebRTC SFU |
| Database | Azure PostgreSQL | Primary data store |
| Cache | Redis | Session and real-time state |
| Hosting | Azure Container Apps | With .NET Aspire orchestration |

## Project Structure

```
lowrollers/
├── src/                    # Source code (scaffolding in progress)
├── tests/                  # Test files
├── specs/                  # Feature specifications
│   └── poker_brd_v1.1.md   # Business Requirements Document (1096 lines)
├── docs/                   # Project documentation
├── .clavix/                # Clavix workflow outputs
│   └── outputs/            # PRDs, tasks, and prompts
└── README.md               # Project overview
```

## Key Documentation

- **BRD**: `specs/poker_brd_v1.1.md` - Comprehensive business requirements (100+ requirements)
- **PRD**: `.clavix/outputs/lowrollers/full-prd.md` - Product requirements (if generated)
- **Quick PRD**: `.clavix/outputs/lowrollers/quick-prd.md` - AI-optimized summary

## Development Commands

*Note: Project scaffolding is in progress. Commands will be added as the codebase develops.*

```bash
# Placeholder - add actual commands when available
# npm run dev          # Start development server
# npm run build        # Production build
# npm run test         # Run tests
# npm run lint         # Lint codebase
```

## Architecture Guidelines

### Frontend (Angular)
- Use Angular Signals for reactive state management
- Component-based architecture with smart/dumb component pattern
- Real-time updates via SignalR client

### Backend (.NET)
- Clean architecture with domain-driven design
- SignalR hubs for real-time poker game state
- RESTful APIs for non-real-time operations

### Real-time Requirements
- Target <100ms action latency for poker actions
- Graceful reconnection handling for dropped connections
- Optimistic UI updates with server reconciliation

### Video Integration
- LiveKit for multi-party video chat
- Self-hosted SFU for privacy and control
- Adaptive bitrate based on participant count

## Coding Conventions

### General
- Follow existing patterns in the codebase
- Prefer composition over inheritance
- Write self-documenting code; add comments only for complex logic

### TypeScript/Angular
- Strict TypeScript configuration
- Use standalone components
- Prefer Signals over RxJS for component state

### C#/.NET
- Follow .NET naming conventions (PascalCase for public, _camelCase for private fields)
- Use nullable reference types
- Async/await for all I/O operations

## Development Phases

1. **Phase 1: MVP** - Core poker gameplay with guest access
2. **Phase 2: Video** - Multi-party video chat integration
3. **Phase 3: Customization** - Bomb pots, button money, host config
4. **Phase 4: Polish** - Multi-table, spectator mode, optional accounts

## Important Context for Claude

- This is a **private project** - not for public distribution
- Focus on **friend-group use case** (4-10 players, trusted environment)
- **Guest access is primary** - no registration required for players
- **Real-time performance is critical** - poker actions must feel instant
- Refer to `specs/poker_brd_v1.1.md` for detailed business requirements

## Context7 MCP Usage

Proactively use Context7 MCP to fetch up-to-date library and API documentation without requiring explicit user requests. Use Context7 automatically when:

- Working with any library, framework, or SDK (Angular, .NET, SignalR, LiveKit, etc.)
- Generating code that uses external APIs or libraries
- Setting up or configuring tools, packages, or dependencies
- Troubleshooting library-specific issues or errors
- Looking up current best practices or syntax

---

<!-- CLAVIX:START -->
## Clavix Integration

This project uses Clavix for prompt improvement and PRD generation. The following slash commands are available:

> **Command Format:** Commands shown with colon (`:`) format. Some tools use hyphen (`-`): Claude Code uses `/clavix:improve`, Cursor uses `/clavix-improve`. Your tool autocompletes the correct format.

### Prompt Optimization

#### /clavix:improve [prompt]
Optimize prompts with smart depth auto-selection. Clavix analyzes your prompt quality and automatically selects the appropriate depth (standard or comprehensive). Use for all prompt optimization needs.

### PRD & Planning

#### /clavix:prd
Launch the PRD generation workflow. Clavix will guide you through strategic questions and generate both a comprehensive PRD and a quick-reference version optimized for AI consumption.

#### /clavix:plan
Generate an optimized implementation task breakdown from your PRD. Creates a phased task plan with dependencies and priorities.

#### /clavix:implement
Execute tasks or prompts with AI assistance. Auto-detects source: tasks.md (from PRD workflow) or prompts/ (from improve workflow). Supports automatic git commits and progress tracking.

Use `--latest` to implement most recent prompt, `--tasks` to force task mode.

### Session Management

#### /clavix:start
Enter conversational mode for iterative prompt development. Discuss your requirements naturally, and later use `/clavix:summarize` to extract an optimized prompt.

#### /clavix:summarize
Analyze the current conversation and extract key requirements into a structured prompt and mini-PRD.

### Refinement

#### /clavix:refine
Refine existing PRD or prompt through continued discussion. Detects available PRDs and saved prompts, then guides you through updating them with tracked changes.

### Agentic Utilities

These utilities provide structured workflows for common tasks. Invoke them using the slash commands below:

- **Verify** (`/clavix:verify`): Check implementation against PRD requirements. Runs automated validation and generates pass/fail reports.
- **Archive** (`/clavix:archive`): Archive completed work. Moves finished PRDs and outputs to archive for future reference.

**When to use which mode:**
- **Improve mode** (`/clavix:improve`): Smart prompt optimization with auto-depth selection
- **PRD mode** (`/clavix:prd`): Strategic planning with architecture and business impact

**Recommended Workflow:**
1. Start with `/clavix:prd` or `/clavix:start` for complex features
2. Refine requirements with `/clavix:refine` as needed
3. Generate tasks with `/clavix:plan`
4. Implement with `/clavix:implement`
5. Verify with `/clavix:verify`
6. Archive when complete with `/clavix:archive`

**Pro tip**: Start complex features with `/clavix:prd` or `/clavix:start` to ensure clear requirements before implementation.
<!-- CLAVIX:END -->
