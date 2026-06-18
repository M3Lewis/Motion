# UI Quality & Layout Guidelines

> Rules for designing and rendering Grasshopper canvas components, WPF windows, and WinForms dialogs.

---

## 1. WPF & WinForms Styling

When building toolbar buttons, dialog boxes, or settings windows:
- Use clean, modern layouts (e.g. multi-column WPF grids, structured panels).
- Align typography using standard fonts like *Segoe UI*, *Inter*, or *Outfit* (rather than default plain system fonts).
- Ensure dialog inputs support quick keyboard shortcuts (like `Enter` to submit, `Esc` to close).
- **Pitfall**: When traversing the WPF logical tree to apply styling or localization, ensure you clone children collections to temporary lists before iterating. Direct iteration while mutating properties causes `"Collection was modified"` crashes (see [WPF Logical Tree Traversal Safety](./atoms/wpf-logical-tree-modification.md)).
- **Pitfall**: When repositioning or sorting custom WinForms toolbar/ToolStrip items, ensure sorting keys are accessed directly (or with correct reflection BindingFlags), and keep layout dimensions consistent across all dock positions to prevent icon distortion (see [Toolbar Layout and Order Safety](./atoms/toolbar-layout-and-order.md)).

---

## 2. Canvas UI Layout Rules

- Buttons drawn directly on the canvas must have a height of at least `20.0f` to allow comfortable clicking.
- Space elements consistently (e.g. using `buttonRect.Inflate(-2.0f, -2.0f)` for clear margins).
- Use `GH_FontServer.ConsoleSmall` or similar system-scaled fonts for text rendering.

---

## 3. High DPI Compatibility

- All canvas drawing dimensions should scale based on the Grasshopper Zoom factor (`GH_Canvas.Zoom`).
- Always check that colors and capsules render properly in both Grasshopper's light palette and dark theme environments.
- Use `Brushes.Azure` or high-contrast standard drawing brushes on dark buttons.
