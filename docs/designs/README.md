# UI Design Approvals

This folder contains approved UI mockups and design references for Low Rollers.

## Workflow

Before implementing any UI component:

1. **Option A: Generated Mockup**
   - Agent generates a visual mockup
   - User reviews and approves (or requests changes)
   - Approved mockup saved here

2. **Option B: Reference Design**
   - User provides a screenshot or design to emulate
   - Screenshot saved here as reference
   - Implementation matches the reference style

## Folder Structure

```
docs/designs/
├── README.md                    # This file
├── core-gameplay/               # Poker table, seats, cards, actions
├── table-management/            # Create/join table pages
├── video-chat/                  # Video grid, controls, tiles
├── chat/                        # Chat panel, messages
├── host-config/                 # Settings forms, templates
└── custom-variants/             # Bomb pot, button money displays
```

## Naming Convention

- `{feature}-{component}-mockup.png` - AI-generated mockups
- `{feature}-{component}-reference.png` - User-provided references
- `{feature}-{component}-approved.png` - Final approved design

## Status

| Feature | Component | Status |
|---------|-----------|--------|
| core-gameplay | poker-table | Pending |
| core-gameplay | player-seat | Pending |
| core-gameplay | community-cards | Pending |
| core-gameplay | pot-display | Pending |
| core-gameplay | action-panel | Pending |
| ... | ... | ... |

*Update this table as designs are approved.*
