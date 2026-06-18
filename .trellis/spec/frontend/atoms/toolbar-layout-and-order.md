---
id: toolbar.layout-and-order
type: pitfall
priority: must
applies_when:
  - modifying toolbar buttons
  - adding or updating CustomMotionToolbar layout
  - rearranging or sorting ToolStrip items
code_anchors:
  - Motion/Button/ToolbarPositionManagerButton.cs
  - Motion/Toolbar/CustomMotionToolbar.cs
verify:
  - ToolStrip items are sorted correctly in all dock positions
  - ToolStrip width/height in all dock positions is at least iconSize + 16 (40px) to prevent icon distortion
source:
  kind: bug_history
  ref: task-2026-06-18-fix-toolbar-icon-sorting
last_checked: 2026-06-18
---

# Rule

1. **Direct Access to Sorting Keys**: Avoid using C# reflection to retrieve public properties (like `ToolbarOrder`). If reflection is required, always include `BindingFlags.Public` in addition to `BindingFlags.NonPublic` to ensure the lookup does not silently fail and return default values.
2. **Synchronize Layout Dimensions**: Always keep toolbar/docking dimensions consistent between the layout manager (`CustomMotionToolbar.SetPosition`) and orientation helper methods (`ConfigureCustomToolbarAppearance`). The toolbar height (in Top/Bottom positions) and width (in Left/Right positions) must be at least `40px` (iconSize + 16) to prevent WinForms from scaling/distorting the 24x24 icons.

# Why

1. A reflection mismatch (specifying only `NonPublic` for a public property) caused `GetToolbarOrder` to return `0` for all buttons. This broke the sorting comparator and resulted in reversed layout loops when items were collected from the toolbar.
2. Overwriting the toolbar height to `30px` for Top/Bottom positions while keeping Left/Right at `40px` caused the 24x24 icons to become squashed and distorted vertically.
