# Technology Stack

## Overview

| Layer | Technology | Version |
|-------|------------|---------|
| Frontend | Angular | 21 |
| UI Components | PrimeNG | Latest |
| Backend | .NET / ASP.NET Core | 10.0 |
| Real-time | SignalR | (included in ASP.NET Core) |
| Video Chat | LiveKit | Self-hosted |
| Primary Database | PostgreSQL | 16+ |
| Cache/Session | Redis | 7+ |
| Orchestration | .NET Aspire | Latest |
| Hosting | Azure Container Apps | - |

---

## Frontend

### Angular 21
**Why Angular:**
- Strong typing with TypeScript
- Signals for reactive state (Angular 17+)
- Standalone components (no NgModules needed)
- Built-in dependency injection
- Excellent tooling (CLI, DevTools)

**State Management:**
- Angular services + signals
- No NgRx—SignalR pushes authoritative state from server
- Client is mostly a view layer

**Key Packages:**
```json
{
  "@angular/core": "^21.0.0",
  "@microsoft/signalr": "^8.0.0",
  "primeng": "^19.0.0",
  "primeicons": "^7.0.0",
  "livekit-client": "^2.0.0"
}
```

### PrimeNG
**Why PrimeNG:**
- Native Angular components (not wrapped React/Vue)
- Comprehensive component library (80+ components)
- Built-in themes (Aura, Lara) with dark mode
- Excellent form components for configuration UI
- Data tables for hand history
- Dialogs, overlays, tooltips for game UI
- Active development and Angular 21 support

**Key Components We'll Use:**
| Component | Use Case |
|-----------|----------|
| Button | Action buttons (Fold, Call, Raise) |
| Dialog | Confirmations, buy-in dialogs |
| InputNumber | Raise amount, buy-in amount |
| Slider | Raise slider |
| DataTable | Hand history |
| Toast | Notifications |
| OverlayPanel | Player info popups |
| Menu | Settings, options |
| Chip | Player status indicators |
| ProgressBar | Action timer |

---

## Backend

### .NET 10.0 / ASP.NET Core
**Why .NET:**
- High performance (consistently top-tier in TechEmpower benchmarks)
- Excellent SignalR implementation
- Strong typing with C#
- Great tooling (Visual Studio, Rider, VS Code)
- Azure-native integration

**Key Packages:**
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
<PackageReference Include="StackExchange.Redis" Version="2.7.0" />
<PackageReference Include="Livekit.Server.Sdk" Version="1.0.0" />
```

### SignalR
**Why SignalR over raw WebSockets:**
- Automatic connection management
- Automatic fallback (WebSocket → Server-Sent Events → Long Polling)
- Built-in reconnection logic
- Group management for tables
- Strong .NET + JavaScript client support
- Azure SignalR Service for scaling

**Hub Design:**
```csharp
// Single hub for all game actions
public class GameHub : Hub
{
    // Authentication via JWT in connection
    // Group per table (SignalR groups)
    // All game actions as hub methods
}
```

---

## Video Chat

### LiveKit (Self-Hosted SFU)
**Why LiveKit over alternatives:**

| Option | Pros | Cons |
|--------|------|------|
| **LiveKit** | Open source, self-hosted, no per-minute costs | Requires hosting |
| Twilio | Easy to use | ~$0.004/min/participant = $115/month for 4hr game |
| Daily.co | Good API | Similar per-minute costs |
| Peer-to-peer | No server costs | Struggles at 10 participants |

**Why self-hosted:**
- Monthly 4-hour games × 10 people = 40 participant-hours
- Twilio/Daily would cost $50-150/month just for video
- Self-hosted LiveKit on Container Apps ≈ $5-10/month
- Full control over quality settings

**Configuration:**
- Adaptive bitrate streaming
- Simulcast for variable quality per viewer
- Audio priority over video when bandwidth constrained
- TURN server for NAT traversal

---

## Database

### PostgreSQL (Primary)
**Why PostgreSQL:**
- Robust, battle-tested
- Excellent JSON support for event storage
- Good Azure integration (Flexible Server)
- Lower cost than SQL Server

**Schema Design:**
```sql
-- Core tables
Tables (Id, InviteCode, HostSessionId, Settings, CreatedAt, Status)
Players (Id, TableId, SessionId, DisplayName, SeatPosition, ChipStack, Status)
Hands (Id, TableId, ButtonPosition, StartedAt, CompletedAt, Phase)
HandEvents (Id, HandId, EventType, EventData, Timestamp)
```

### Redis (Cache/Session)
**Why Redis:**
- Sub-millisecond latency
- Perfect for session tokens
- Real-time game state caching
- Pub/sub for SignalR backplane (if scaling beyond one server)

**What we store in Redis:**
- Guest session tokens (5-minute reconnection window)
- Active game state (for fast reads)
- Player connection status
- Action timers

---

## Hosting & Infrastructure

### Azure Container Apps
**Why Container Apps:**
- Serverless containers (pay for usage)
- Built-in auto-scaling
- Easy deployment from containers
- Supports .NET Aspire orchestration
- Lower cost than AKS for our scale

### .NET Aspire
**Why Aspire:**
- Local development orchestration
- Automatic service discovery
- Health checks and telemetry built-in
- Easy Azure deployment
- Manages Redis, PostgreSQL, SignalR dependencies

**AppHost Configuration:**
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("lowrollers");

var redis = builder.AddRedis("redis");

var api = builder.AddProject<Projects.LowRollers_Api>("api")
    .WithReference(postgres)
    .WithReference(redis);

builder.AddNpmApp("web", "../LowRollers.Web")
    .WithReference(api);
```

### Azure SignalR Service
**Why managed SignalR:**
- Handles WebSocket scaling automatically
- No sticky sessions needed
- Built-in connection management
- Pay per unit (1 unit = 1,000 connections = plenty for us)

---

## Performance Targets

| Metric | Target | How We Achieve It |
|--------|--------|-------------------|
| Action latency | <100ms | SignalR WebSocket, Redis caching |
| Video latency | <500ms | LiveKit SFU, regional deployment |
| Sound latency | <50ms | Preloaded Web Audio API |
| Frame rate | 60fps | CSS transforms, GPU acceleration |
| Animation duration | <500ms | Optimized animations |
| Page load | <3s | Code splitting, lazy loading |

---

## Development Tools

| Tool | Purpose |
|------|---------|
| Visual Studio / Rider | Backend development |
| VS Code | Frontend development |
| Angular CLI | Frontend scaffolding |
| .NET CLI | Backend scaffolding |
| Docker | Local containerization |
| Azure CLI | Deployment |
| xUnit | Backend testing |
| Jest/Karma | Frontend testing |

---

## Cost Estimate (Monthly Game Usage)

Assuming monthly 4-hour games with 8 players:

| Service | Estimated Cost |
|---------|----------------|
| Azure Container Apps (scale-to-zero) | $5-15 |
| Azure PostgreSQL Flexible (Burstable) | $15-25 |
| Azure Cache for Redis (Basic) | $15 |
| Azure SignalR Service (Free tier) | $0 |
| LiveKit (self-hosted in Container Apps) | Included above |
| **Total** | **$35-55/month** |

*Cost optimization: Scale to zero between games using scheduled start/stop*

---

*See also: [architecture.md](./architecture.md) for system design*
