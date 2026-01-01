# Business Requirements Document: Online Poker Platform

**Document Version:** 1.1  
**Date:** October 11, 2025  
**Project:** Real-time Online Poker Platform with Video Chat  
**Prepared For:** Private Group Poker Game (with Public Expansion Capability)

---

## 1. Executive Summary

### 1.1 Project Overview
Development of a web-based poker platform enabling a private friend group to play Texas Hold'em poker online with integrated video chat. The platform will serve 4-10 players for monthly games, replicating the in-person home game experience with customizable rules and side games. Future scalability to public deployment is a consideration but not an immediate priority.

### 1.2 Business Objectives
- **Primary:** Enable monthly poker games for private friend group with same experience quality as in-person games
- **Secondary:** Provide flexible customization so host can configure side games (bomb pots, button money) to match group preferences
- **Tertiary:** Build clean architecture that could expand to public poker platform if desired in future
- **Key Differentiator:** Fully customizable game variants and side games that match the group's unique home game structure

### 1.3 Success Criteria
- ✅ Support 4-10 simultaneous players at single table with <100ms action latency
- ✅ Players can join game within 30 seconds of receiving invite link (no registration)
- ✅ Stable video chat with all players visible throughout entire game session
- ✅ Zero critical bugs causing game state corruption or unfair outcomes
- ✅ 95%+ player satisfaction rating after initial testing period
- ✅ Technical architecture capable of scaling to 10+ concurrent tables if needed

---

## 2. Scope

### 2.1 In Scope
**Phase 1 - MVP:**
- Complete Texas Hold'em poker gameplay for single private table (2-10 players)
- **Guest access system** (no registration required - display name only)
- **Invite link/code system** for table access control
- Real-time game state synchronization
- Player chip management and buy-ins (per-session basis)
- Hand history recording (session-based)
- Simple table creation by host with shareable invite link
- **Core animations** (card dealing, chip movements, pot collection)
- **Essential sound effects** (cards, chips, actions, notifications)
- Basic visual feedback for player actions and game state

**Phase 2 - Video Integration:**
- Multi-party video chat (up to 10 participants)
- Audio communication
- Video quality controls (mute, camera toggle)
- **Camera and microphone device selection**
- Network resilience (game continues if video fails)
- **Enhanced animations** for video-related events (player join/leave)

**Phase 3 - Customization & Side Games:**
- **Host configuration system** for table settings and side game rules
- Double board bomb pot variant with configurable triggers
- Button money game mechanic with configurable amounts
- Keyboard hotkey support for actions (fixed scheme)
- Player voting system for bomb pots
- Table setting persistence (host configurations saved and reusable)
- **Text chat interface** for player communication during games
- Chat moderation tools for host
- **Special event animations** (bomb pot, button money, celebrations)
- **Comprehensive sound effect library** with user controls

**Phase 4 - Polish & Optional Multi-Table:**
- Enhanced UI/UX based on friend group feedback
- Performance optimization
- Multiple private tables support (if friend group expands or wants parallel games)
- Spectator mode for eliminated players or friends watching
- Advanced statistics and hand analysis tools
- **Optional account system** for players who want persistent stats/history (guest access remains primary)
- **Additional user preferences** based on actual user feedback

### 2.2 Out of Scope
- Real money transactions (play money/chips only)
- Tournament structures (cash games only initially)
- Mobile native apps (web-responsive only)
- Other poker variants beyond Texas Hold'em (Omaha, Stud, etc.)
- Artificial intelligence/bot players
- Integration with third-party poker networks
- Regulatory compliance for real-money gambling
- Public player matchmaking and open table listings (private tables only)
- Marketing and player acquisition systems
- Extensive multi-table tournament features
- Traditional account system with registration/login (guest access only for MVP)
- Persistent chip balances across sessions (reset each game for MVP)
- User profiles and long-term statistics tracking (future consideration)
- Extensive UI customization options (themes, colors, card designs, etc.)
- Advanced sound and animation customization
- Customizable hotkeys (fixed scheme for MVP)

### 2.3 Future Considerations
- Optional user accounts for persistent statistics and chip tracking
- Tournament support with scheduled events
- Real money integration with payment processing
- Mobile native applications (iOS, Android)
- Additional poker variants
- Advanced statistics and analytics dashboards
- Social features (friends lists, achievements, leaderboards)
- Cross-session hand history for individual players
- UI theming and visual customization options
- Advanced hotkey customization
- Granular audio/video preference controls

---

## 3. User Personas

### 3.1 Primary Persona: "Host/Developer"
**Name:** Stewart, 42-year-old software engineer  
**Goals:**
- Build and host monthly poker games for close friend group
- Configure game exactly how the group has played for years (bomb pots, button money)
- Create platform that "just works" without constant troubleshooting
- See and talk to friends during games (social aspect critical)
- Trust that game is fair with provably random card shuffling
- Quick setup using saved configurations from previous games

**Pain Points with Existing Solutions:**
- Generic platforms lack the specific variants the group plays
- Can't configure custom rules like their button money structure
- Video chat integration is poor or non-existent
- Too complicated to set up recurring games with same rules
- Unknown players and trust concerns on public platforms
- Existing platforms over-engineer features nobody uses

**Technical Proficiency:** High - comfortable building the platform, hosting, troubleshooting

**Relationship to Platform:** Primary developer and likely default host for monthly games

### 3.2 Secondary Persona: "Social Player with Tech Challenges"
**Name:** Blair, lawyer  
**Goals:**
- Stay connected with friend group through monthly games
- Play poker without worrying about technical setup
- See everyone's faces and maintain social atmosphere
- Fair gameplay with clear rules
- Reliable experience that doesn't require tech troubleshooting

**Pain Points:**
- Frequently experiences audio issues (microphone problems, connection quality)
- Frustrated when technical problems interrupt the game flow
- Wants to focus on poker and socializing, not fixing technical issues
- Needs platform that gracefully handles audio/video problems
- Prefers simple, intuitive interface over advanced features

**Technical Proficiency:** Medium - comfortable using web applications but not troubleshooting network/audio issues

**Relationship to Platform:** Regular monthly player who represents the non-technical members of the group

---

## 4. Customization and Configuration

### 4.1 User Preferences

User preferences are minimal for the MVP, focusing only on essential device configuration. The platform will use sensible defaults for all other settings. Additional preference options may be added in Phase 4+ based on actual user feedback.

#### 4.1.1 Audio and Video Device Selection
**PREF-AV-001:** Users shall be able to select preferred camera device from available cameras  
**PREF-AV-002:** Users shall be able to select preferred microphone device from available microphones  
**PREF-AV-003:** Device selections shall persist across sessions in browser local storage  

**Note:** All other audio/video controls (mute, camera on/off, volume) are session-based settings that do not persist.

### 4.2 Host/Table Configuration

The table host (creator) has administrative privileges to configure all gameplay rules and side game structures for their table. These settings affect all players at the table and define the game experience.

#### 4.2.1 Host Role and Privileges
**HOST-ROLE-001:** User who creates table is automatically designated as host  
**HOST-ROLE-002:** Host shall be able to transfer host privileges to another seated player  
**HOST-ROLE-003:** If host leaves table, system shall automatically promote longest-seated player to host  
**HOST-ROLE-004:** Host shall be able to modify table settings before game starts  
**HOST-ROLE-005:** Host shall be able to modify certain settings (side games) during game with player agreement  
**HOST-ROLE-006:** Host shall be able to pause game for breaks with all player consent  
**HOST-ROLE-007:** Host shall be able to remove disruptive players (kick)  
**HOST-ROLE-008:** Host shall be able to save table configuration as template for future games  

#### 4.2.2 Basic Table Settings
**HOST-TABLE-001:** Host shall configure small blind amount ($0.25, $0.50, $1, $2, $5, $10, custom)  
**HOST-TABLE-002:** Host shall configure big blind amount (must be 2x small blind)  
**HOST-TABLE-003:** Host shall set minimum buy-in (minimum 20x big blind, maximum 100x big blind)  
**HOST-TABLE-004:** Host shall set maximum buy-in (minimum 100x big blind, no maximum)  
**HOST-TABLE-005:** Host shall set table capacity (2-10 players)  
**HOST-TABLE-006:** Host shall configure action timer duration (15s, 30s, 45s, 60s, or unlimited)  
**HOST-TABLE-007:** Host shall enable/disable time bank and configure time bank duration (30s, 60s, 90s)  
**HOST-TABLE-008:** Host shall set table privacy (private invite-only or public)  
**HOST-TABLE-009:** Host shall configure table name for identification  

#### 4.2.3 Bomb Pot Configuration
**HOST-BOMB-001:** Host shall enable/disable bomb pot feature  
**HOST-BOMB-002:** Host shall select bomb pot variant (single board or double board)  
**HOST-BOMB-003:** Host shall configure bomb pot ante amount (fixed dollar amount or multiple of big blind)  
**HOST-BOMB-004:** Host shall configure bomb pot trigger method:
- Fixed interval (every N hands: 5, 10, 15, 20, 25 hands)
- Random percentage (5%, 10%, 15%, 20% chance per hand)
- Player voting (configurable threshold: 50%, 67%, 75%, 100% agreement)
- Manual trigger (host activates specific hands)
- Button money win trigger (bomb pot triggered when button player wins the kitty)

**Implementation Notes:**
- Bomb pots always skip preflop betting (traditional rules)
- All players must participate (no sitting out)
- Ante collected automatically when triggered
- Double board variant always splits pot 50/50 between boards

#### 4.2.4 Button Money Configuration
**HOST-BTN-001:** Host shall enable/disable button money feature  
**HOST-BTN-002:** Host shall configure button contribution amount per hand (fixed dollar or multiple of big blind)

**Implementation Notes:**
- Button position player only is eligible to win kitty (traditional rules)
- Kitty automatically awarded when button wins any pot
- No minimum pot size requirement
- No maximum kitty accumulation limit
- Kitty does not carry over between game sessions (resets each session)
- Full kitty awarded on win (no partial awards)

#### 4.2.5 Game Flow Configuration
**HOST-FLOW-002:** Host shall configure pause between hands (0s, 3s, 5s, 10s)  
**HOST-FLOW-003:** Host shall configure showdown display duration (3s, 5s, 10s, manual advance)  
**HOST-FLOW-004:** Host shall enable/disable auto-muck losing hands (or always show at showdown)  
**HOST-FLOW-005:** Host shall configure disconnection handling:
- Auto-fold immediately
- Time bank auto-applies
- Sit-out for N hands then fold

**Implementation Notes:**
- Rebuys/add-ons allowed at any time (no restrictions)
- The host shall have Start/Pause/Stop controls, enabling the host to control start and end of the game

#### 4.2.6 Table Template System
**HOST-TMPL-001:** Host shall be able to save complete table configuration as named template  
**HOST-TMPL-002:** Host shall be able to load saved templates when creating new table  
**HOST-TMPL-003:** Host shall be able to edit and update saved templates  
**HOST-TMPL-004:** Host shall be able to delete saved templates

### 4.3 Configuration Precedence and Overrides

**PREF-PREC-001:** Table/host configuration shall always override user preferences for gameplay rules  
**PREF-PREC-002:** Mid-game configuration changes by host shall require majority player approval (67% agreement)  
**PREF-PREC-003:** Side game triggers (bomb pots, button money) cannot be modified mid-hand  

---

## 5. Functional Requirements

### 5.1 User Management

#### 5.1.1 Guest Access (No Registration Required)
**REQ-UM-001:** Users shall be able to join tables as guests without creating an account  
**REQ-UM-002:** Guest users shall provide display name when joining table  
**REQ-UM-003:** System shall validate display name uniqueness within each table  
**REQ-UM-004:** Display names shall be between 2-20 characters  
**REQ-UM-005:** System shall assign temporary session ID to guest users  
**REQ-UM-006:** Guest sessions shall persist until browser closes or user leaves table  
**REQ-UM-007:** System shall allow same user to rejoin with same display name if disconnected (within 5 minutes)  

#### 5.1.2 Optional Account System (Future Enhancement)
**REQ-UM-008:** System may offer optional account creation for stat tracking (Phase 4+)  
**REQ-UM-009:** Optional accounts would retain hand history and statistics  
**REQ-UM-010:** Optional accounts would allow cross-session chip balance persistence  
**REQ-UM-011:** Guest access shall remain available even if account system is added  

#### 5.1.3 Table Access Control
**REQ-UM-012:** Tables shall be protected by unique invite code or URL  
**REQ-UM-013:** Host shall be able to set optional table password for additional security  
**REQ-UM-014:** Users must have invite link/code to access private table  
**REQ-UM-015:** Host shall be able to kick unwanted guests from table  
**REQ-UM-016:** Host shall be able to ban specific display names from rejoining  

#### 5.1.4 Session-Based Chip Management
**REQ-UM-017:** Each guest user starts with virtual chip balance defined by table buy-in  
**REQ-UM-018:** Chip balances persist only for duration of game session  
**REQ-UM-019:** Users can rebuy chips during session according to table rebuy rules  
**REQ-UM-020:** Chip balances reset when table closes or user leaves permanently  
**REQ-UM-021:** System shall not track chip balances across different game sessions  
**REQ-UM-022:** Host can configure starting chip amount for all players (within buy-in limits)  

### 5.2 Table Creation and Access

#### 5.2.1 Table Creation
**REQ-LT-001:** Users shall be able to create new table without registration  
**REQ-LT-002:** Table creator shall provide display name if first time accessing platform  
**REQ-LT-003:** System shall generate unique invite link/code for each table  
**REQ-LT-004:** Invite links shall remain valid until table is closed by host  
**REQ-LT-005:** Users shall be able to create table with configurable settings  

#### 5.2.2 Table Access
**REQ-LT-006:** Users shall access tables exclusively via invite link (no public lobby)  
**REQ-LT-007:** Users without invite link shall not be able to find or access private tables  
**REQ-LT-008:** System shall validate invite link before allowing table access  
**REQ-LT-009:** Expired or invalid invite links shall display clear error message  
**REQ-LT-010:** Host shall be able to regenerate invite link (invalidating old link)  

### 5.3 Core Poker Gameplay

#### 5.3.1 Game Flow
**REQ-GP-001:** System shall deal two hole cards face-down to each player at hand start  
**REQ-GP-002:** System shall collect small blind and big blind before each hand  
**REQ-GP-003:** System shall deal flop (3 community cards) after preflop betting round  
**REQ-GP-004:** System shall deal turn (1 community card) after flop betting round  
**REQ-GP-005:** System shall deal river (1 community card) after turn betting round  
**REQ-GP-006:** System shall determine winner(s) after river betting round or when all but one player folds  
**REQ-GP-007:** System shall award pot to winner(s) based on hand strength  
**REQ-GP-008:** System shall rotate dealer button clockwise after each hand  
**REQ-GP-009:** System shall skip empty seats when rotating button and blinds  

#### 5.3.2 Player Actions
**REQ-GP-010:** Players shall be able to fold, forfeiting interest in pot  
**REQ-GP-011:** Players shall be able to check when no bet is facing them  
**REQ-GP-012:** Players shall be able to call, matching current bet amount  
**REQ-GP-013:** Players shall be able to raise, increasing current bet  
**REQ-GP-014:** System shall enforce minimum raise as current bet plus last raise amount  
**REQ-GP-015:** Players shall be able to go all-in with remaining chips  
**REQ-GP-016:** System shall enforce 30-second action timer per player (configurable by host)  
**REQ-GP-017:** System shall auto-fold players who exceed action timer  
**REQ-GP-018:** System shall provide time bank for critical decisions (optional, configured by host)  

#### 5.3.3 Pot Management
**REQ-GP-019:** System shall track total pot amount visible to all players  
**REQ-GP-020:** System shall create side pots when player(s) go all-in for less than current bet  
**REQ-GP-021:** System shall distribute side pots only to eligible players  
**REQ-GP-022:** System shall handle split pots when multiple players have equal hands  
**REQ-GP-023:** System shall display pot amounts clearly differentiated (main pot, side pot 1, etc.)  

#### 5.3.4 Hand Evaluation and Showdown
**REQ-GP-024:** System shall evaluate hands using standard poker hand rankings  
**REQ-GP-025:** At showdown, only players who choose to show cards shall reveal their hole cards  
**REQ-GP-026:** Last aggressor (last to bet/raise) shall reveal cards first at showdown  
**REQ-GP-027:** Other players shall have option to show or muck their cards  
**REQ-GP-027a:** If all players check the river, the cards of the first player to act shall be revealed first. Evaluation proceeds clockwise. Inferior hands are auto-mucked. A superior hand is revealed and sets the new benchmark.  
**REQ-GP-028:** Player who wins by all others folding shall not be required to show cards  
**REQ-GP-029:** Players shall have option to voluntarily show cards even when not required  
**REQ-GP-030:** System shall display winning hand description only when cards are shown  
**REQ-GP-031:** System shall handle ties with split pot logic  
**REQ-GP-032:** Mucked cards shall remain hidden from all players  
**REQ-GP-033:** System shall award pot to winner without showing cards if winner chooses to muck  
**REQ-GP-034:** Hand history shall record whether cards were shown or mucked  

### 5.4 Custom Game Variants

#### 5.4.1 Double Board Bomb Pot
**REQ-CV-001:** When triggered, system shall collect ante from all players (no folding option)  
**REQ-CV-002:** System shall skip preflop betting round in bomb pot hands  
**REQ-CV-003:** System shall deal two separate flops simultaneously  
**REQ-CV-004:** System shall proceed through normal betting rounds for both boards  
**REQ-CV-005:** System shall evaluate each board independently at showdown  
**REQ-CV-006:** System shall award 50% of pot to best hand on each board  
**REQ-CV-007:** Same player may win both boards (scoop entire pot)  
**REQ-CV-008:** System shall allow bomb pot trigger via configured percentage (e.g., every 10th hand)  
**REQ-CV-009:** System shall allow bomb pot trigger via player voting (configurable threshold)  
**REQ-CV-010:** System shall allow bomb pot trigger via winning the button money game  

#### 5.4.2 Button Money Game
**REQ-CV-011:** System shall maintain accumulating "button kitty" separate from main pot  
**REQ-CV-012:** Player in button position shall contribute to kitty each hand (configurable amount)  
**REQ-CV-013:** Only button position player shall be eligible to win kitty  
**REQ-CV-014:** Button player wins kitty when winning pot above minimum threshold (configurable)  
**REQ-CV-015:** Kitty shall roll over to next hand if not won  
**REQ-CV-016:** System shall display current kitty amount to all players  
**REQ-CV-017:** If the button position chops the pot with at least one other player, the button money is splashed into the next pot  

### 5.5 Video Chat Integration

#### 5.5.1 Core Video Functionality
**REQ-VC-001:** System shall enable video chat for all seated players at table  
**REQ-VC-002:** System shall display video feeds for up to 10 players simultaneously  
**REQ-VC-003:** Users shall be able to mute their microphone  
**REQ-VC-004:** Users shall be able to disable their camera  
**REQ-VC-005:** System shall continue game functionality if video connection fails  
**REQ-VC-006:** System shall display player name and position when video unavailable  
**REQ-VC-007:** Users shall be able to select camera and microphone devices before joining video

#### 5.5.2 Video Quality and Performance
**REQ-VC-008:** System shall optimize video quality based on available bandwidth  
**REQ-VC-009:** System shall prioritize audio quality over video quality during bandwidth constraints  
**REQ-VC-010:** System shall display network quality indicator for each player  
**REQ-VC-011:** System shall support minimum video resolution of 360p at 15fps per player  

### 5.6 Chat and Communication

#### 5.6.1 Text Chat Interface
**REQ-CHAT-001:** System shall provide text chat interface visible to all players at table  
**REQ-CHAT-002:** Chat interface shall display in dedicated panel (right side or overlay)  
**REQ-CHAT-003:** Chat messages shall display sender username and timestamp  
**REQ-CHAT-004:** Users shall be able to send messages via text input with enter key submission  
**REQ-CHAT-005:** Chat history shall persist for duration of game session  
**REQ-CHAT-006:** Users shall be able to scroll through chat history  

#### 5.6.2 Chat Features
**REQ-CHAT-007:** System shall auto-detect URLs in chat messages and make them clickable (open in new tab/window)  
**REQ-CHAT-008:** System shall display system messages for game events (player joins/leaves, hand winners)  
**REQ-CHAT-009:** System shall provide toggle (on by default) to display hand events in chat window, including dealer actions, player actions, player hand evaluation, and final disposition  
**REQ-CHAT-010:** Host shall be able to clear chat history  
**REQ-CHAT-011:** Chat messages shall have character limit configured by host  

#### 5.6.3 Chat Moderation
**REQ-CHAT-012:** Host shall be able to mute individual players from chat  
**REQ-CHAT-013:** Host shall be able to enable/disable chat entirely  

### 5.7 User Interface and Controls

#### 5.7.1 Table Interface
**REQ-UI-001:** System shall display all seated players with their positions, chip counts, and status  
**REQ-UI-002:** System shall highlight current player's turn with clear visual indicator  
**REQ-UI-003:** System shall display community cards in center of table  
**REQ-UI-004:** System shall display total pot amount prominently  
**REQ-UI-005:** System shall display each player's current bet in front of their position  
**REQ-UI-006:** System shall display action history for current hand  
**REQ-UI-007:** System shall display dealer button position clearly  
**REQ-UI-008:** Chat interface shall be positioned on right side of screen or as collapsible panel  
**REQ-UI-009:** Chat panel shall not obstruct critical game elements (cards, actions, pot)  
**REQ-UI-010:** Players shall have the ability to set their status to Away. Cards will not be dealt to away players, nor shall an away player be dealer.
**REQ-UI-011:** If an Away player would have otherwise been obligated to post any big blinds during their away time, the player will be assessed one big blind when they return.

#### 5.7.2 Hotkey Support
**REQ-UI-012:** Users shall be able to perform actions using keyboard shortcuts  
**REQ-UI-013:** Fixed hotkeys: F (fold), C (call/check), R (raise), A (all-in)  
**REQ-UI-014:** System shall display available hotkeys contextually during player's turn  
**REQ-UI-015:** System shall require confirmation for all-in to prevent accidents  

**Note:** Hotkey customization is deferred to Phase 4+ based on user feedback.

#### 5.7.3 Responsive Design
**REQ-UI-016:** Interface shall be responsive and functional on desktop screens (1920x1080 minimum)  
**REQ-UI-017:** Interface shall be usable on laptop screens (1366x768 minimum)  
**REQ-UI-018:** Interface shall adapt video layout based on screen size  

### 5.8 Game State and Persistence

#### 5.8.1 Connection Management
**REQ-GS-001:** System shall handle player disconnections gracefully  
**REQ-GS-002:** Disconnected players shall auto-fold after action timer expires  
**REQ-GS-003:** Reconnecting players shall see current accurate game state  
**REQ-GS-004:** System shall display disconnected player status to all players  
**REQ-GS-005:** A disconnected player shall be treated as Away until either they return, the game is ended by the host, or they are removed from the game  

#### 5.8.2 Hand History
**REQ-GS-006:** System shall record complete hand history for every completed hand  
**REQ-GS-007:** Hand history shall include all player actions, bet amounts, and cards shown  
**REQ-GS-008:** Users shall be able to review hand history for tables they participated in  
**REQ-GS-009:** System shall retain hand history for minimum 90 days  

---

## 6. Non-Functional Requirements

### 6.1 Performance
**REQ-NF-001:** Player actions shall reflect in UI within 100ms under normal network conditions  
**REQ-NF-002:** System shall support 10 concurrent players at single table with <5% packet loss  
**REQ-NF-003:** System shall scale to support 100 concurrent tables (1000 players)  
**REQ-NF-004:** Page load time shall not exceed 3 seconds on broadband connection  
**REQ-NF-005:** Video latency shall not exceed 500ms end-to-end  
**REQ-NF-006:** Animations shall complete within 500ms to maintain game pace  
**REQ-NF-007:** Sound effects shall play with <50ms latency from trigger event  
**REQ-NF-008:** Multiple simultaneous animations shall not cause frame rate drops below 30fps  
**REQ-NF-009:** System shall maintain 60fps during normal gameplay with animations enabled  

### 6.2 Reliability and Availability
**REQ-NF-010:** System shall maintain 99.5% uptime during scheduled game hours  
**REQ-NF-011:** System shall recover from crashes without data loss (persistent game state)  
**REQ-NF-012:** System shall provide automatic failover for critical services  
**REQ-NF-013:** System shall perform database backups every 6 hours  

### 6.3 Security
**REQ-NF-014:** All communications shall be encrypted using TLS 1.3  
**REQ-NF-015:** Table access shall be controlled via unique invite links/codes  
**REQ-NF-016:** System shall use cryptographically secure random number generator for card shuffling  
**REQ-NF-017:** System shall validate all player actions server-side (never trust client)  
**REQ-NF-018:** System shall log all game actions with timestamps for audit trail  
**REQ-NF-019:** System shall detect and prevent common cheating patterns  
**REQ-NF-020:** System shall implement rate limiting to prevent abuse  
**REQ-NF-021:** Session tokens shall be cryptographically secure and expire after inactivity  
**REQ-NF-022:** Host shall have ability to kick/ban disruptive players  

### 6.4 Fairness and Integrity
**REQ-NF-023:** Card shuffling algorithm shall be provably random and unbiased  
**REQ-NF-023a:** Card shuffling shall use Fisher–Yates shuffle algorithm. See: https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle  
**REQ-NF-024:** All game logic shall execute server-side with authoritative state  
**REQ-NF-025:** System shall ensure identical odds to physical deck (1/52! shuffles)  
**REQ-NF-026:** System shall prevent viewing of other players' hole cards under any circumstance  
**REQ-NF-026a:** Each client shall only receive information about the community cards and the hole cards of that client/player  
**REQ-NF-027:** System shall provide shuffle verification mechanism (provably fair)  

### 6.5 Usability
**REQ-NF-028:** New users shall join their first game within 30 seconds of clicking invite link  
**REQ-NF-029:** Display name entry shall be simple and require only 2-20 character name  
**REQ-NF-030:** Error messages shall be clear and provide actionable guidance  
**REQ-NF-031:** Interface shall follow accessibility guidelines (WCAG 2.1 Level AA)  
**REQ-NF-032:** Animations shall enhance understanding of game state without being distracting  
**REQ-NF-033:** Sound effects shall provide intuitive feedback without being jarring  

### 6.6 Scalability
**REQ-NF-034:** Architecture shall support horizontal scaling by adding server instances  
**REQ-NF-035:** System shall handle 10x traffic increase within 15 minutes (auto-scaling)  
**REQ-NF-036:** Database shall support partitioning/sharding for future growth  

### 6.7 Maintainability
**REQ-NF-037:** Code shall maintain minimum 80% unit test coverage  
**REQ-NF-038:** System shall implement comprehensive logging for debugging  
**REQ-NF-039:** System shall expose health check endpoints for monitoring  
**REQ-NF-040:** Deployment shall support zero-downtime updates  

---

## 7. User Stories

### 7.1 Epic 1: Quick Join Access

**US-001:** As a new player, I want to join a table by clicking an invite link so I can start playing immediately without registration.  
**Acceptance Criteria:**
- User clicks invite link shared by host
- User prompted only for display name (2-20 characters)
- Display name validated for uniqueness at table
- User immediately joins table after name entry
- No email, password, or account creation required

**US-002:** As a returning player, I want to use the same display name so friends recognize me.  
**Acceptance Criteria:**
- User can enter previously used display name
- System allows same name if not currently in use at table
- If disconnected within 5 minutes, user can rejoin with same name and restore chip stack
- Display name persists in browser for convenience

**US-003:** As a host, I want to control table access so only invited friends can join.  
**Acceptance Criteria:**
- System generates unique invite link/code when table created
- Only users with invite link can access table
- Host can optionally set table password for additional security
- Host can kick unwanted guests
- Host can ban specific display names from rejoining

**US-004:** As a guest player, I want to start with chips immediately so I can begin playing without setup delays.  
**Acceptance Criteria:**
- System automatically assigns starting chip balance based on table buy-in
- User can adjust buy-in amount within table limits before sitting
- Chips are virtual and reset each game session
- User can rebuy chips during game according to table rules
- No cross-session chip tracking (fresh start each game)

### 7.2 Epic 2: Table Access and Setup

**US-005:** As a host, I want to create a private table quickly so I can get the game started.  
**Acceptance Criteria:**
- User enters display name (first time only)
- User can configure table settings efficiently
- System generates shareable invite link immediately
- Host can copy invite link with one click
- Table remains open until host closes it or all players leave

**US-006:** As a player, I want to join via invite link so I can get into the game quickly.  
**Acceptance Criteria:**
- User clicks invite link from any device
- User prompted only for display name (if first time joining)
- User sees table interface immediately after name entry
- Available seats clearly visible
- User can select seat and set buy-in amount within table limits

### 7.3 Epic 3: Core Gameplay

**US-007:** As a seated player, I want to see my two hole cards privately so only I know my hand.  
**Acceptance Criteria:**
- Hole cards display face-up on player's screen only
- Other players see card backs for opponent hands
- Cards are clearly visible and identifiable

**US-008:** As an active player on my turn, I want to fold, check, call, or raise so I can make strategic decisions.  
**Acceptance Criteria:**
- Action buttons display contextually (check vs call based on bet state)
- Raise button opens input for raise amount with min/max limits
- Actions are confirmed immediately in UI
- All players see action notification within 100ms

**US-009:** As a player, I want to see the pot size and all players' chip stacks so I can make informed decisions.  
**Acceptance Criteria:**
- Total pot displays prominently in center of table
- Each player's chip stack displays near their position
- Values update in real-time as bets are placed
- Side pots are clearly labeled and differentiated

**US-010:** As a player, I want to see who won the hand and with what cards so I can learn from the outcome.  
**Acceptance Criteria:**
- All active players' cards are revealed at showdown
- Winning hand is highlighted with description (e.g., "Full House, Kings over Jacks")
- Chips animate moving to winner's stack
- Hand result visible for 5 seconds before next hand begins

**US-011:** As a player in a hand, I want a reasonable time to make my decision so I don't feel rushed.  
**Acceptance Criteria:**
- Action timer displays during player's turn (host-configured duration)
- Visual and audio warnings at 10 seconds remaining
- Player auto-folds if timer expires
- Optional time bank for critical decisions (if enabled by host)

### 7.4 Epic 4: Custom Game Features

**US-012:** As a player, I want to participate in bomb pot hands so I can experience the high-action variant we play in person.  
**Acceptance Criteria:**
- System collects ante from all players automatically
- Two separate boards are dealt and displayed clearly
- Each board evaluated independently at showdown
- Winner(s) announced for each board with 50% pot allocation

**US-013:** As a table host, I want to configure bomb pot frequency so games match our group's preferences.  
**Acceptance Criteria:**
- Setting to trigger bomb pot every N hands (configurable 5-25)
- Option for player voting with configurable threshold
- Option to trigger bomb pot when button money is won
- Current bomb pot status visible to all players
- Bomb pot rules explained in tooltip/help

**US-014:** As a button position player, I want to have a chance to win the accumulated button kitty so I can enjoy the extra pot incentive.  
**Acceptance Criteria:**
- Button contribution collected automatically each hand
- Current kitty amount displays near button indicator
- Kitty awarded to button player when winning pot
- Kitty rolls over when not won
- If button chops pot, kitty splashes into next pot

### 7.5 Epic 5: Video Chat

**US-015:** As a seated player, I want to see and hear all other players so I can have a social poker experience.  
**Acceptance Criteria:**
- Video feeds for all players display in designated areas
- Audio from all players is audible (adjustable volume)
- Video quality adapts to network conditions automatically
- Names display on video feeds for identification

**US-016:** As a player, I want to mute my microphone and disable my camera so I can control my audio/video privacy.  
**Acceptance Criteria:**
- Mute button toggles microphone on/off with clear visual indicator
- Camera button toggles video on/off with placeholder image when disabled
- Other players see muted/camera-off status
- Settings persist across disconnections

**US-017:** As a player experiencing connection issues, I want the game to continue even if my video drops so I don't miss hands.  
**Acceptance Criteria:**
- Game state updates continue with video failures
- Player can still make actions without video
- Video reconnects automatically when network improves
- Other players notified of video connection issues

### 7.6 Epic 6: Controls and Usability

**US-018:** As a player, I want to use keyboard shortcuts for actions so I can play quickly without clicking.  
**Acceptance Criteria:**
- Hotkeys F (fold), C (call/check), R (raise), A (all-in) work during player's turn
- Hotkeys displayed in tooltip or help section
- Raise hotkey prompts for amount with pre-filled common values
- All-in requires confirmation to prevent accidents

**US-019:** As a player reviewing my history, I want to see detailed hand records so I can analyze my play.  
**Acceptance Criteria:**
- Hand history accessible from table interface
- Each hand shows all actions, bet amounts, and final cards (if shown)
- Hands filterable by session
- Hand can be replayed with step-by-step action replay

### 7.7 Epic 7: Host Configuration and Customization

**US-020:** As a table host, I want to configure all game settings before starting so the game matches our group's preferences.  
**Acceptance Criteria:**
- Host can set blinds, buy-in limits, and max players
- Host can enable/disable bomb pots and configure trigger method
- Host can enable/disable button money and set contribution amount
- Settings display clearly to all players before game starts
- Host can modify settings before dealing first hand

**US-021:** As a table host, I want to save my table configuration as a template so I can quickly recreate our favorite game setup.  
**Acceptance Criteria:**
- Host can save current configuration with custom name
- Host can load saved template when creating new table
- Template includes all game settings (blinds, side games, timers)
- Host can edit and delete saved templates

**US-022:** As a table host, I want to control side game triggers during the session so we can adapt to the group's mood.  
**Acceptance Criteria:**
- Host can manually trigger bomb pot for next hand
- Host can enable player voting for bomb pots with configurable threshold
- Changes require player approval if game is in progress (67% agreement)
- Players receive notification of configuration changes
- Changes cannot occur during active hand

**US-023:** As a player, I want to select my preferred camera and microphone so the platform uses the right devices.  
**Acceptance Criteria:**
- User can select from available camera devices
- User can select from available microphone devices
- Device selections persist across sessions in browser
- Device selector accessible before joining video and during game

**US-024:** As a player, I want to chat with other players during the game so I can maintain social interaction.  
**Acceptance Criteria:**
- Chat panel is easily accessible and doesn't obstruct gameplay
- Messages display with sender name and timestamp
- User can type and send messages using enter key
- Chat history is visible and scrollable
- URLs in chat are clickable

**US-025:** As a host, I want to moderate chat so I can maintain a friendly environment.  
**Acceptance Criteria:**
- Host can mute individual players from chat
- Host can clear chat history if needed
- Host can disable chat entirely if preferred
- Moderation actions are visible to affected players

**US-026:** As a player, I want smooth animations and realistic sound effects so the game feels engaging and polished.  
**Acceptance Criteria:**
- Cards deal with smooth animation from deck to players
- Chips animate when betting and collecting into pot
- Winner's pot animates to their stack with chip counting effect
- All actions have appropriate sound effects (card dealing, chip stacking)
- Sounds are realistic but not overwhelming
- Animations complete quickly enough to maintain game pace

**US-027:** As a player, I want to set my status to Away so I can take a break without holding up the game.  
**Acceptance Criteria:**
- Away status button is easily accessible
- Cards not dealt to Away players
- Away players not assigned dealer button
- Clear visual indicator shows Away status to all players
- Missed big blinds assessed when player returns

---

## 8. Use Cases

### 8.1 Use Case: Create and Join Private Table

**Actor:** Player (Table Creator/Host), Players (Invitees)  
**Preconditions:** Users have web browser and internet connection  
**Main Flow:**
1. Host navigates to platform landing page
2. Host clicks "Create Table" (no login required)
3. Host enters display name (2-20 characters)
4. Host configures table settings (stakes $1/$2, buy-in $100-$200, max 8 players, private)
5. System generates unique invite link (e.g., poker.app/table/abc123xyz)
6. Host copies invite link and shares via external communication (text, email, Discord, etc.)
7. Invitee clicks invite link
8. Invitee prompted to enter display name
9. Invitee validates name is unique at this table
10. Invitee automatically joins table and sees available seats
11. Each player selects seat and confirms buy-in amount
12. Host clicks "Start Game" when ready (minimum 2 players)
13. System deals first hand

**Alternate Flows:**
- **A1:** If invitee display name already taken at table, prompt for different name
- **A2:** If host sets table password, invitees must enter password after clicking link
- **A3:** If invitee has insufficient chips, system prompts to set buy-in amount
- **A4:** If host leaves before starting, oldest seated player becomes new host

**Postconditions:** Game is active with all players seated and first hand in progress

### 8.2 Use Case: Play Complete Poker Hand

**Actor:** All Seated Players  
**Preconditions:** Players seated at active table, previous hand complete  
**Main Flow:**
1. System rotates dealer button clockwise
2. System collects small blind ($1) and big blind ($2)
3. System shuffles deck using cryptographic RNG (Fisher-Yates algorithm)
4. System deals two hole cards to each player
5. Preflop betting round begins with player left of big blind
6. Each player in turn folds, calls, or raises until betting round complete
7. System deals flop (3 community cards)
8. Flop betting round proceeds from small blind position
9. System deals turn card
10. Turn betting round proceeds
11. System deals river card
12. River betting round proceeds
13. System evaluates remaining players' hands
14. System awards pot to winner
15. System updates chip stacks
16. Brief pause (configurable) before next hand

**Alternate Flows:**
- **A1:** If all players except one fold, remaining player wins pot immediately (skip to step 14)
- **A2:** If player(s) go all-in, system creates side pot(s) and continues with eligible players
- **A3:** If betting timer expires, system auto-folds player
- **A4:** If player disconnects, system marks player as Away and auto-folds at their turn

**Postconditions:** Hand complete, pot distributed, chip stacks updated, ready for next hand

### 8.3 Use Case: Trigger and Play Bomb Pot

**Actor:** System (automatic trigger) or Players (voting)  
**Preconditions:** Table configured with bomb pot enabled  
**Main Flow:**
1. System determines bomb pot trigger (hand counter, button money win, or player vote threshold met)
2. System announces "BOMB POT" to all players
3. System collects ante from each player (e.g., $10 each)
4. System skips preflop betting round
5. System deals two separate flops (Board A and Board B)
6. Flop betting round proceeds normally for both boards
7. System deals turn for Board A and Board B
8. Turn betting round proceeds
9. System deals river for Board A and Board B
10. River betting round proceeds
11. System evaluates hands on Board A, determines winner(s)
12. System evaluates hands on Board B, determines winner(s)
13. System splits pot 50/50 between board winners
14. System awards pot portions to winners
15. Next hand proceeds as normal (not bomb pot unless triggered again)

**Alternate Flows:**
- **A1:** Same player has best hand on both boards, wins entire pot (scoop)
- **A2:** All players fold during betting, last player wins entire pot without showdown
- **A3:** Bomb pot triggered by button money win, combines celebration with bomb pot announcement

**Postconditions:** Bomb pot complete, pot distributed across two boards, game continues

---

## 9. Constraints and Assumptions

### 9.1 Technical Constraints
- Platform must be web-based (browser access, no native apps in MVP)
- Must support modern browsers (Chrome 90+, Firefox 88+, Safari 14+, Edge 90+)
- Video chat limited to 10 simultaneous participants due to bandwidth constraints
- Initial deployment limited to single Azure region (US East or West)

### 9.2 Business Constraints
- Initial budget limited (self-funded development)
- Development team of one primary developer (project requester)
- No real-money gambling features (play money only to avoid regulatory requirements)
- Iterative development approach with regular testing and feedback from friend group

### 9.3 Assumptions
- Players are part of known friend group with existing trust relationships (no strangers)
- Friend group size is stable and known to each other (eliminates need for registration)
- Players don't need persistent chip balances across sessions (start fresh each game)
- Session-based play model acceptable to group (not tracking long-term statistics initially)
- Players have reliable broadband internet (minimum 5 Mbps down, 1 Mbps up)
- Players have webcam and microphone for video chat
- Player base is tech-savvy and can troubleshoot minor issues
- Regular game sessions provide adequate testing cycles and feedback opportunities
- Azure infrastructure costs acceptable for small-scale deployment (single table initially)
- Group members available for user acceptance testing and feedback
- No migration from existing platforms required (starting fresh)
- Group size remains relatively stable at 4-10 players
- Host (table creator) will be consistent person or rotates among trusted members
- Invite link sharing through existing communication channels (text, email) is acceptable

### 9.4 Dependencies
- Azure cloud services availability and pricing stability
- Open-source poker hand evaluation libraries maintenance
- WebRTC browser support continuation
- Third-party STUN/TURN server availability (LiveKit or similar)
- .NET and Angular framework LTS support
- Browser local storage for temporary session persistence (display name, device preferences)
- Secure session token generation for guest authentication

---

## 10. Risks and Mitigation

### 10.1 Technical Risks

**RISK-01: Video chat bandwidth limitations**  
**Probability:** High | **Impact:** High  
**Description:** 10-player video may exceed available bandwidth for some players (especially Blair with known audio issues)  
**Mitigation:**
- Implement adaptive bitrate with quality degradation
- Allow selective stream subscription (disable specific players' video)
- Provide audio-only fallback mode
- Test with group members' actual network conditions
- Document minimum bandwidth requirements clearly
- Prioritize audio quality over video when bandwidth constrained

**RISK-02: Game state synchronization bugs**  
**Probability:** Medium | **Impact:** Critical  
**Description:** Race conditions or network issues could corrupt game state  
**Mitigation:**
- Implement server-authoritative architecture with validation
- Comprehensive unit and integration testing of game logic
- Persistent state storage for recovery
- Extensive logging for debugging
- Alpha testing with small group before full deployment

**RISK-03: Cheating vulnerabilities**  
**Probability:** Medium | **Impact:** High  
**Description:** Players could exploit client-side code to view cards or manipulate state  
**Mitigation:**
- All game logic server-side, never trust client
- Cryptographic RNG for shuffling (Fisher-Yates algorithm)
- Encrypted communications (TLS 1.3)
- Audit logging of all actions
- Code review focused on security
- Only send each client their own hole cards (REQ-NF-026a)

**RISK-04: Scalability bottlenecks**  
**Probability:** Medium | **Impact:** Medium  
**Description:** Architecture may not scale from 10 to 1000 players smoothly  
**Mitigation:**
- Design with scalability from start (SignalR, stateless services)
- Load testing at incremental scales (50, 100, 500 concurrent players)
- Azure auto-scaling configured proactively
- Performance monitoring and alerting
- Budget for infrastructure upgrades

**RISK-05: Animation and sound performance degradation**  
**Probability:** Medium | **Impact:** Low  
**Description:** Multiple simultaneous animations or sound effects may cause performance issues on lower-end devices  
**Mitigation:**
- Use CSS transforms and GPU acceleration for animations
- Implement animation pooling and recycling
- Test on various devices including older hardware
- Lazy-load sound assets and cache appropriately
- Limit simultaneous sound effect playback

### 10.2 Business Risks

**RISK-06: Low adoption/retention**  
**Probability:** Low | **Impact:** Medium  
**Description:** Group may prefer other platforms or in-person games  
**Mitigation:**
- Gather requirements directly from potential users
- Iterative development with feedback loops
- Focus on differentiators (custom variants, great UX)
- Smooth migration path with minimal learning curve

**RISK-07: Development scope creep**  
**Probability:** Medium | **Impact:** Medium  
**Description:** Feature complexity may cause delays in delivering working platform  
**Mitigation:**
- Prioritize MVP features ruthlessly
- Modular architecture allows incremental delivery
- Regular progress reviews with stakeholders
- Accept technical debt in non-critical areas for speed
- Use phased approach with clear deliverables per phase

**RISK-08: Cost overruns on Azure infrastructure**  
**Probability:** Low | **Impact:** Medium  
**Description:** Actual costs may exceed estimates as usage scales  
**Mitigation:**
- Implement cost monitoring and alerts
- Use Azure Cost Management + Billing dashboards
- Right-size resources based on actual usage
- Reserved instances for predictable workloads
- Scale-to-zero for non-peak hours (monthly games only)

---

## 11. Acceptance Criteria

### 11.1 MVP Acceptance (Phase 1)
- ✅ 5 concurrent users can join table as guests without registration
- ✅ Players can join via invite link and provide display name only
- ✅ Complete poker hand with correct dealing and pot distribution functions properly
- ✅ All standard poker actions (fold, check, call, raise, all-in) function correctly
- ✅ Hand history records all actions and can be reviewed post-game
- ✅ Invite link/code system prevents unauthorized access to private tables
- ✅ System recovers gracefully from player disconnections with rejoin capability
- ✅ No critical bugs causing game state corruption in 10-hour testing period
- ✅ Core animations present and smooth (cards dealing, chips moving, pot collecting)
- ✅ Essential sound effects working (card sounds, chip sounds, action confirmations)
- ✅ Animations complete within acceptable timeframes (no lag perception)

### 11.2 Video Integration Acceptance (Phase 2)
- ✅ 8 concurrent players maintain stable video chat for 3-hour game session
- ✅ Video quality remains acceptable (360p minimum) under normal network conditions
- ✅ Audio remains clear and synchronized throughout session
- ✅ Game continues correctly if 1-2 players lose video connection
- ✅ Video controls (mute, camera off) work consistently
- ✅ Camera and microphone device selection works correctly
- ✅ No video issues cause gameplay interruptions

### 11.3 Custom Features Acceptance (Phase 3)
- ✅ Bomb pot triggers correctly via percentage, voting, or button money win
- ✅ Double board evaluation awards pot correctly to each board winner
- ✅ Button money accumulates and awards correctly based on rules
- ✅ Button money chop rule (splash into next pot) works correctly
- ✅ All fixed hotkeys perform correct actions when pressed during player's turn
- ✅ Table template system saves and loads configurations correctly
- ✅ Text chat functions correctly with URL detection and system messages
- ✅ Players express satisfaction with custom features matching home game experience

### 11.4 Scale Testing Acceptance (Phase 4)
- ✅ System supports 10 concurrent tables (50-100 players) without degradation
- ✅ Action latency remains <100ms under load
- ✅ Video quality maintained for all tables under load
- ✅ Auto-scaling responds within 5 minutes to load increases
- ✅ System maintains 99% uptime over 30-day period
- ✅ All monitoring and alerting systems functional

---

## 12. Success Metrics

### 12.1 Quantitative Metrics

**Technical Performance:**
- Action latency: <100ms for P95 of player actions
- Video uptime: >99% of game time with functioning video
- Game state corruption rate: 0 instances per 1000 hands
- System uptime: >99.5% during scheduled game hours
- Page load time: <3 seconds for P95 of page loads

**User Engagement:**
- Monthly active players: Target 10 in first 3 months
- Average game session length: Target 3+ hours
- Hands played per session: Target 100+ hands
- Player retention: Target 90% month-over-month for first 6 months
- Tables created per week: Target 4+ (weekly games)

**Scale Readiness:**
- Concurrent players supported: Target 100+ by month 6
- Concurrent tables supported: Target 10+ by month 6
- Infrastructure cost per table-hour: Target <$2

### 12.2 Qualitative Metrics

**Friend Group Satisfaction:**
- Players report platform is "as good or better" than in-person games (survey after initial testing period)
- Zero requests to return to previous platform or in-person only
- Players appreciate frictionless join process (no registration required)
- Positive feedback on custom features matching home game experience
- High confidence in fairness and security among all players
- Group recommends platform to other poker groups
- Players report joining new game is faster and easier than previous platforms
- Players appreciate polished feel from animations and sound effects
- Animations and sounds enhance rather than distract from gameplay

**Customization Success:**
- Host reports ease of configuring table to group preferences
- All desired side game variations supported and working correctly
- Template system saves time in recurring game setup

**Developer Experience:**
- Codebase maintainability rated "good" by external code review
- New features can be developed and deployed in reasonable sprints
- Bug resolution time averages <48 hours for critical issues
- Documentation sufficient for future enhancements or onboarding help

---

## 13. Appendices

### 13.1 Glossary

**All-in:** Betting all remaining chips  
**Animation:** Visual motion effect that provides feedback and enhances game feel  
**Ante:** Forced bet all players contribute before hand  
**Away:** Player status indicating temporary absence from active play  
**Big Blind:** Larger forced bet from player two left of dealer  
**Bomb Pot:** Hand where all players pay ante and see flop without preflop betting  
**Button:** Dealer position, rotates clockwise each hand  
**Chat:** Text messaging interface for player communication  
**Flop:** First three community cards  
**Guest:** Player who joins without registration using only display name  
**Hole Cards:** Two cards dealt face-down to each player  
**Host:** Player who creates and configures table settings  
**Invite Link:** Unique URL that grants access to private table  
**Kitty:** Accumulated pot separate from main pot (button money)  
**Pot:** Total amount of chips/money bet in current hand  
**River:** Fifth and final community card  
**SFU:** Selective Forwarding Unit (video routing server)  
**Showdown:** Revealing hands to determine winner  
**Side Pot:** Separate pot when player goes all-in for less than current bet  
**Small Blind:** Smaller forced bet from player left of dealer  
**Sound Effect:** Audio feedback that confirms actions or provides game atmosphere  
**Turn:** Fourth community card  

### 13.2 References

- Technical Implementation Guide (separate document)
- Texas Hold'em Official Rules (Bicycle Cards)
- WebRTC Standards (W3C)
- Azure Architecture Best Practices (Microsoft)
- Fisher-Yates Shuffle Algorithm: https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
- Web Animation API (MDN Web Docs)
- Web Audio API (MDN Web Docs)
- Accessibility Guidelines for Animations (WCAG 2.1)

### 13.3 Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | October 11, 2025 | Project Team | Initial BRD creation |
| 1.1 | October 11, 2025 | Project Team | Streamlined user preferences (removed 30+ options), updated personas (Stewart/Blair), simplified bomb pot and button money rules, added camera/mic device selection, fixed hotkey scheme, added Away status, added chat URL detection and hand events toggle, added button money win as bomb pot trigger, clarified showdown rules |

---

**Document Approval:**

This Business Requirements Document requires approval from the following stakeholders:

- [ ] Project Sponsor (Primary Developer)
- [ ] Key User Representative (Monthly Game Participants)
- [ ] Technical Lead (Primary Developer)

**Notes:** This document should be reviewed and updated after each major phase completion and whenever significant requirement changes are identified.
