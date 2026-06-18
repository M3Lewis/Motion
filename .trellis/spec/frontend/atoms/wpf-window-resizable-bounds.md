---
id: wpf.window-resizable-bounds
type: pitfall
priority: must
applies_when:
  - building or modifying WPF/WinForms dialogs or control windows
  - configuring window resizing properties (ResizeMode, Height, Width)
code_anchors:
  - Motion/Button/SliderControl.xaml
verify:
  - Window cannot be resized to a size that causes controls to overlap, compress, or disappear.
  - Functional single-axis floating windows (e.g. sliders) are locked in the orthogonal axis.
source:
  kind: bug_history
  ref: task-2026-06-18-fix-unionslider-window-resizing
last_checked: 2026-06-18
---

# Rule

When designing floating control/tool windows in WPF:
1. **Axis Resizing Lock**: For windows where elements primarily stretch along a single layout axis (such as slider controllers), lock the orthogonal axis. For horizontal layout windows, set the window's `MinHeight` and `MaxHeight` to the same fixed value (e.g., `230`) to disable vertical resizing.
2. **Minimum Bound Limits**: Always specify a safe `MinWidth` (and `MinHeight` if vertically resizable) to prevent the user from shrinking the window to a size that squashes buttons or hides crucial UI controls.

# Why

Without height locks and minimum bounds, users can drag the window border to an extremely small height, which causes WPF's layout engine to collapse controls, resulting in buttons disappearing or overlapping, breaking usability.
