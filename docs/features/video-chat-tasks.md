# Implementation Tasks: Video Chat

**Feature:** Video Chat
**Spec:** [video-chat-spec.md](./video-chat-spec.md)
**Phase:** 2

---

## Infrastructure

- [ ] **Set up LiveKit server infrastructure**
  Task ID: `video-01`
  > **Implementation**: Add LiveKit to .NET Aspire orchestration
  > **Details**:
  > - Add LiveKit container to `src/LowRollers.AppHost/Program.cs`
  > - Configure TURN server for NAT traversal
  > - Generate API key and secret
  > - Environment variables for connection
  > - Health check endpoint

- [ ] **Create LiveKit token service**
  Task ID: `video-02`
  > **Implementation**: Create `src/LowRollers.Api/Features/VideoChat/LiveKitTokenService.cs`
  > **Details**:
  > - Install `Livekit.Server.Sdk` package
  > - `GenerateToken(tableId, sessionId, displayName)`
  > - Token grants: canPublish, canSubscribe
  > - Token expiry: 24 hours
  > - Room name = tableId

- [ ] **Create video room API endpoint**
  Task ID: `video-03`
  > **Implementation**: Create `src/LowRollers.Api/Features/VideoChat/JoinVideoRoomCommand.cs`
  > **Details**:
  > - Validate session is at table
  > - Generate LiveKit token
  > - Return token and room info
  > - Endpoint: `POST /api/tables/{id}/video/join`

---

## Angular Integration

> **DESIGN APPROVAL REQUIRED**: Before implementing any UI component below (video-05 through video-10), either:
> 1. Agent presents a generated mockup for user approval, OR
> 2. User provides a screenshot/design reference to emulate
>
> Approved designs are stored in `docs/designs/` for implementation reference.

- [ ] **Create video service**
  Task ID: `video-04`
  > **Implementation**: Create `src/LowRollers.Web/src/app/core/services/video.service.ts`
  > **Details**:
  > - Install `livekit-client` package
  > - `connect(token, serverUrl)` - Join room
  > - `disconnect()` - Leave room
  > - `setMicEnabled(enabled)` - Toggle mic
  > - `setCameraEnabled(enabled)` - Toggle camera
  > - Signal for connection state
  > - Handle reconnection automatically

- [ ] **Create video grid component**
  Task ID: `video-05`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/video/video-grid/`
  > **Details**:
  > - `video-grid.component.ts`
  > - Layout participants in grid
  > - Adaptive layout based on count (2-4, 5-7, 8-10)
  > - Highlight speaking participant
  > - Responsive for different screen sizes

- [ ] **Create video tile component**
  Task ID: `video-06`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/video/video-tile/`
  > **Details**:
  > - `video-tile.component.ts`
  > - Attach video track to element
  > - Display name overlay
  > - Muted indicator icon
  > - Camera off placeholder (avatar)
  > - Network quality indicator

- [ ] **Create video controls component**
  Task ID: `video-07`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/video/video-controls/`
  > **Details**:
  > - `video-controls.component.ts`
  > - Mic toggle button with icon state
  > - Camera toggle button with icon state
  > - Settings button (opens device selector)
  > - Use PrimeNG Button, ToggleButton

- [ ] **Create device selector component**
  Task ID: `video-08`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/video/device-selector/`
  > **Details**:
  > - `device-selector.component.ts`
  > - Enumerate `navigator.mediaDevices.enumerateDevices()`
  > - Dropdown for cameras
  > - Dropdown for microphones
  > - Preview current selection
  > - Save to localStorage
  > - Use PrimeNG Select, Dialog

---

## Quality Adaptation

- [ ] **Implement bandwidth adaptation**
  Task ID: `video-09`
  > **Implementation**: Extend `src/LowRollers.Web/src/app/core/services/video.service.ts`
  > **Details**:
  > - Configure simulcast layers in publish options
  > - Subscribe to appropriate layer based on layout
  > - Monitor connection quality events
  > - Degrade gracefully (video before audio)

- [ ] **Create network indicator component**
  Task ID: `video-10`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/video/network-indicator/`
  > **Details**:
  > - Display connection quality (bars or dots)
  > - Color coding: green/yellow/red
  > - Tooltip with details
  > - Update from LiveKit connection quality events

---

## Game Independence

- [ ] **Implement video/game separation**
  Task ID: `video-11`
  > **Implementation**: Ensure in architecture
  > **Details**:
  > - Video uses WebRTC (LiveKit)
  > - Game uses SignalR (separate connection)
  > - Video failure doesn't affect game actions
  > - Player can fold/call/raise without video
  > - UI shows placeholder for video-less players

- [ ] **Implement video reconnection**
  Task ID: `video-12`
  > **Implementation**: Extend `video.service.ts`
  > **Details**:
  > - Detect disconnection
  > - Auto-retry every 5 seconds
  > - Show reconnecting status
  > - After 30 seconds, show "unavailable"
  > - Manual reconnect button option

---

## Testing

- [ ] **Create video integration tests**
  Task ID: `video-13`
  > **Implementation**: Manual and automated testing plan
  > **Details**:
  > - 2-player video working
  > - 10-player video working
  > - Mute/unmute working
  > - Camera toggle working
  > - Device selection working
  > - Game continues during video drop
  > - 3+ hour stability test

---

## Completion Checklist

- [ ] LiveKit server running in Aspire
- [ ] Token generation working
- [ ] 10 simultaneous video streams working
- [ ] Mute/camera controls working
- [ ] Device selection persists
- [ ] Video drops don't affect game
- [ ] Quality adapts to bandwidth
- [ ] 3-hour stability achieved

---

*Generated by Clavix /clavix:plan*
