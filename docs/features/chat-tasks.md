# Implementation Tasks: Text Chat

**Feature:** Text Chat
**Spec:** [chat-spec.md](./chat-spec.md)
**Phase:** 3

---

## Backend

- [ ] **Create chat service**
  Task ID: `chat-01`
  > **Implementation**: Create `src/LowRollers.Api/Features/Chat/ChatService.cs`
  > **Details**:
  > - `SendMessage(tableId, senderId, content)` - Validate and broadcast
  > - `GetHistory(tableId)` - Return session messages
  > - `ClearHistory(tableId)` - Host only
  > - Store messages in Redis (session-scoped, no persistence)
  > - Character limit validation

- [ ] **Create chat SignalR methods**
  Task ID: `chat-02`
  > **Implementation**: Extend `src/LowRollers.Api/Hubs/GameHub.cs`
  > **Details**:
  > - `SendChatMessage(string content)`
  >   - Validate not muted
  >   - Validate character limit
  >   - Broadcast to table group
  > - Broadcast methods:
  >   - `ChatMessageReceived(ChatMessage)`
  >   - `PlayerMuted(Guid playerId)`
  >   - `ChatCleared()`
  >   - `ChatDisabled(bool)`

- [ ] **Implement chat moderation**
  Task ID: `chat-03`
  > **Implementation**: Create `src/LowRollers.Api/Features/Chat/Moderation/`
  > **Details**:
  > - `MutePlayerCommand.cs` - Add player to mute list
  > - `UnmutePlayerCommand.cs` - Remove from mute list
  > - `ClearChatCommand.cs` - Clear all messages
  > - `ToggleChatCommand.cs` - Enable/disable chat
  > - Store mute list in Redis (session-scoped)
  > - Broadcast moderation actions

- [ ] **Create system message service**
  Task ID: `chat-04`
  > **Implementation**: Create `src/LowRollers.Api/Features/Chat/SystemMessageService.cs`
  > **Details**:
  > - `SendPlayerJoined(tableId, playerName)`
  > - `SendPlayerLeft(tableId, playerName)`
  > - `SendHandWinner(tableId, playerName, amount, handDescription)`
  > - `SendBombPotTriggered(tableId)`
  > - `SendButtonMoneyWon(tableId, playerName, amount)`
  > - Messages marked as Type = System or GameEvent

---

## Angular Components

- [ ] **Create chat panel component**
  Task ID: `chat-05`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/chat/chat-panel/`
  > **Details**:
  > - `chat-panel.component.ts`
  > - Collapsible panel (right side of screen)
  > - Collapse/expand button
  > - Does not obstruct game elements
  > - Use PrimeNG Sidebar or custom panel

- [ ] **Create message list component**
  Task ID: `chat-06`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/chat/message-list/`
  > **Details**:
  > - `message-list.component.ts`
  > - Virtual scrolling for performance
  > - Auto-scroll to bottom on new message
  > - Manual scroll disables auto-scroll
  > - "New messages" indicator when scrolled up
  > - Use PrimeNG VirtualScroller or native scrolling

- [ ] **Create message item component**
  Task ID: `chat-07`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/chat/message-item/`
  > **Details**:
  > - `message-item.component.ts`
  > - Display: name, timestamp, content
  > - URL detection and linking (open in new tab)
  > - Different styling for:
  >   - Player messages (normal)
  >   - System messages (subtle, gray)
  >   - Game events (formatted, optional)
  > - Muted player indicator

- [ ] **Create message input component**
  Task ID: `chat-08`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/chat/message-input/`
  > **Details**:
  > - `message-input.component.ts`
  > - Text input field
  > - Enter key submits
  > - Send button
  > - Character count / limit indicator
  > - Disabled state when muted
  > - Use PrimeNG InputText, Button

- [ ] **Create chat settings component**
  Task ID: `chat-09`
  > **Implementation**: Create `src/LowRollers.Web/src/app/features/game/chat/chat-settings/`
  > **Details**:
  > - Toggle: Show game events
  > - Host-only: Mute player dropdown
  > - Host-only: Clear chat button
  > - Host-only: Disable chat toggle
  > - Use PrimeNG ToggleSwitch, Select, Button

---

## URL Detection

- [ ] **Implement URL detection and linking**
  Task ID: `chat-10`
  > **Implementation**: Create `src/LowRollers.Web/src/app/shared/pipes/linkify.pipe.ts`
  > **Details**:
  > - Detect URLs in message content
  > - Convert to clickable links
  > - Open in new tab (`target="_blank"`)
  > - Security: `rel="noopener noreferrer"`
  > - Support http, https, www prefixes

---

## Testing

- [ ] **Create chat tests**
  Task ID: `chat-11`
  > **Implementation**: Create `tests/LowRollers.Api.Tests/Features/Chat/`
  > **Details**:
  > - Message sending and receiving
  > - Character limit enforcement
  > - Muted player cannot send
  > - Clear chat removes all messages
  > - Disable chat blocks all messages
  > - System messages generated correctly

---

## Completion Checklist

- [ ] Chat panel visible and collapsible
- [ ] Messages display with name/timestamp
- [ ] Enter key sends message
- [ ] URLs clickable (new tab)
- [ ] System messages appear
- [ ] Game events toggleable
- [ ] Host can mute players
- [ ] Host can clear chat
- [ ] Host can disable chat
- [ ] Character limit enforced

---

*Generated by Clavix /clavix:plan*
