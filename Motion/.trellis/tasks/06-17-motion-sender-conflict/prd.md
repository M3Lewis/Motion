# Resolve Motion Sender and PersistentDataEditor conflict

## Goal
Resolve the conflict between the `Motion Sender` component and the `PersistentDataEditor` (PDE) plugin. The conflict currently prevents the `Motion Sender` UI from rendering correctly and disables double-clicking on `Motion Sender` components to create events.

## Requirements
- Fix the rendering of `Motion Sender` components when `PersistentDataEditor` is installed.
- Restore double-click functionality for `Motion Sender` to correctly trigger its event/graph creation pipeline.
- The fix must not break any existing functionality of `Motion Sender` (such as settings, creating events, named view switches, etc.).
- Ensure compilation passes and all existing code remains fully functional.

## Acceptance Criteria
- [ ] `Motion Sender` renders correctly with its custom UI (capsules, labels, state tags, and action buttons).
- [ ] Double-clicking `Motion Sender` properly triggers the creation of associated Event and Graph Mapper components.
- [ ] No regression in other components/buttons.
