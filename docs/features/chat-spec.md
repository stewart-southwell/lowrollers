# Feature Specification: Text Chat

## Overview

Text-based chat system for player communication during games, with moderation controls for the host.

**Phase:** 3
**Priority:** Medium
**BRD References:** REQ-CHAT-001 to REQ-CHAT-013

---

## User Stories

### US-024: Chat with Players
> As a player, I want to chat with other players during the game so I can maintain social interaction.

**Acceptance Criteria:**
- Chat panel is easily accessible and doesn't obstruct gameplay
- Messages display with sender name and timestamp
- User can type and send messages using enter key
- Chat history is visible and scrollable
- URLs in chat are clickable

### US-025: Moderate Chat
> As a host, I want to moderate chat so I can maintain a friendly environment.

**Acceptance Criteria:**
- Host can mute individual players from chat
- Host can clear chat history if needed
- Host can disable chat entirely if preferred
- Moderation actions are visible to affected players

---

## Functional Requirements

### Chat Interface (REQ-CHAT-001 to REQ-CHAT-006)
| ID | Requirement |
|----|-------------|
| REQ-CHAT-001 | Text chat interface visible to all players at table |
| REQ-CHAT-002 | Chat interface in dedicated panel (right side or overlay) |
| REQ-CHAT-003 | Display sender username and timestamp |
| REQ-CHAT-004 | Send messages via text input with enter key |
| REQ-CHAT-005 | Chat history persists for duration of session |
| REQ-CHAT-006 | Scrollable chat history |

### Chat Features (REQ-CHAT-007 to REQ-CHAT-011)
| ID | Requirement |
|----|-------------|
| REQ-CHAT-007 | Auto-detect URLs and make clickable (new tab) |
| REQ-CHAT-008 | Display system messages for game events |
| REQ-CHAT-009 | Toggle to show/hide hand events in chat |
| REQ-CHAT-010 | Host can clear chat history |
| REQ-CHAT-011 | Character limit per message (host configurable) |

### Moderation (REQ-CHAT-012 to REQ-CHAT-013)
| ID | Requirement |
|----|-------------|
| REQ-CHAT-012 | Host can mute individual players from chat |
| REQ-CHAT-013 | Host can enable/disable chat entirely |

---

## Technical Design

### Chat Message Model

```csharp
public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid TableId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; }
    public string Content { get; set; }
    public ChatMessageType Type { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum ChatMessageType
{
    Player,      // Regular player message
    System,      // System notifications
    GameEvent    // Hand events (configurable visibility)
}
```

### System Messages

| Event | Message |
|-------|---------|
| Player joins | "Alice joined the table" |
| Player leaves | "Bob left the table" |
| Hand winner | "Charlie wins $50 with Full House" |
| Bomb pot trigger | "BOMB POT triggered!" |
| Button money won | "Dave wins the button money ($25)!" |

### SignalR Methods

**Client → Server:**
- `SendChatMessage(string content)`

**Server → Client:**
- `ChatMessageReceived(ChatMessage message)`
- `PlayerMuted(Guid playerId)`
- `ChatCleared()`
- `ChatDisabled(bool disabled)`

---

## UI Components

| Component | Description |
|-----------|-------------|
| `chat-panel` | Collapsible right-side panel |
| `message-list` | Scrollable message history |
| `message-input` | Text input with send button |
| `message-item` | Individual message display |
| `chat-settings` | Toggle for game events |

---

*See [chat-tasks.md](./chat-tasks.md) for implementation tasks*
