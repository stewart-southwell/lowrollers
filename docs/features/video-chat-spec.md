# Feature Specification: Video Chat

## Overview

Multi-party video chat integration using self-hosted LiveKit SFU, enabling friends to see and hear each other during poker games.

**Phase:** 2
**Priority:** High
**BRD References:** REQ-VC-001 to REQ-VC-011

---

## User Stories

### US-015: See and Hear Players
> As a seated player, I want to see and hear all other players so I can have a social poker experience.

**Acceptance Criteria:**
- Video feeds for all players display in designated areas
- Audio from all players is audible (adjustable volume)
- Video quality adapts to network conditions automatically
- Names display on video feeds for identification

### US-016: Audio/Video Controls
> As a player, I want to mute my microphone and disable my camera so I can control my audio/video privacy.

**Acceptance Criteria:**
- Mute button toggles microphone on/off with clear visual indicator
- Camera button toggles video on/off with placeholder image when disabled
- Other players see muted/camera-off status
- Settings persist across disconnections

### US-017: Network Resilience
> As a player experiencing connection issues, I want the game to continue even if my video drops so I don't miss hands.

**Acceptance Criteria:**
- Game state updates continue with video failures
- Player can still make actions without video
- Video reconnects automatically when network improves
- Other players notified of video connection issues

### US-023: Device Selection
> As a player, I want to select my preferred camera and microphone so the platform uses the right devices.

**Acceptance Criteria:**
- User can select from available camera devices
- User can select from available microphone devices
- Device selections persist across sessions in browser
- Device selector accessible before joining video and during game

---

## Functional Requirements

### Core Video (REQ-VC-001 to REQ-VC-007)
| ID | Requirement |
|----|-------------|
| REQ-VC-001 | Enable video chat for all seated players at table |
| REQ-VC-002 | Display video feeds for up to 10 players simultaneously |
| REQ-VC-003 | Users can mute their microphone |
| REQ-VC-004 | Users can disable their camera |
| REQ-VC-005 | Game continues if video connection fails |
| REQ-VC-006 | Display player name and position when video unavailable |
| REQ-VC-007 | Select camera and microphone devices before joining |

### Video Quality (REQ-VC-008 to REQ-VC-011)
| ID | Requirement |
|----|-------------|
| REQ-VC-008 | Optimize video quality based on available bandwidth |
| REQ-VC-009 | Prioritize audio quality over video during bandwidth constraints |
| REQ-VC-010 | Display network quality indicator for each player |
| REQ-VC-011 | Minimum video resolution of 360p at 15fps per player |

---

## Technical Design

### LiveKit Integration

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Angular Client │────►│  .NET API        │────►│  LiveKit Server │
│  (livekit-client)│     │  (Token Service) │     │  (SFU)          │
└─────────────────┘     └──────────────────┘     └─────────────────┘
        │                                                │
        └────────────────── WebRTC ──────────────────────┘
```

### Room Mapping
- One LiveKit room per poker table
- Room name = Table ID
- Participant identity = Session ID + Display Name

### Token Generation
```csharp
public class LiveKitTokenService
{
    public string GenerateToken(Guid tableId, Guid sessionId, string displayName)
    {
        // Create token with:
        // - Room: tableId.ToString()
        // - Identity: sessionId.ToString()
        // - Name: displayName
        // - Grants: canPublish, canSubscribe
        // - Expiry: 24 hours
    }
}
```

### Quality Settings

| Participants | Video Resolution | Framerate | Bitrate |
|--------------|------------------|-----------|---------|
| 2-4 | 720p | 30fps | 1.5 Mbps |
| 5-7 | 480p | 24fps | 800 Kbps |
| 8-10 | 360p | 15fps | 400 Kbps |

### Simulcast Layers
- High: 720p (when bandwidth allows)
- Medium: 480p (default)
- Low: 180p (poor connections)

---

## UI Components

| Component | Description |
|-----------|-------------|
| `video-grid` | Layout for all video tiles |
| `video-tile` | Individual participant video |
| `video-controls` | Mute/camera/settings buttons |
| `device-selector` | Camera/mic dropdown |
| `network-indicator` | Connection quality display |

---

## Failure Handling

### Video Disconnect
1. Game continues normally (SignalR separate from WebRTC)
2. Show placeholder for player (avatar + name)
3. Attempt auto-reconnect every 5 seconds
4. After 30 seconds, show "Video unavailable" status

### Audio Priority
- When bandwidth drops, reduce video quality first
- Audio maintains higher bitrate (48kbps minimum)
- If audio fails, show speaking indicator from volume levels

---

*See [video-chat-tasks.md](./video-chat-tasks.md) for implementation tasks*
