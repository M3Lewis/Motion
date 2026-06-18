# Fix UnionSlider Window Resizing

## Goal

Resolve the layout bug in the UnionSlider control window (`SliderControl.xaml`), where allowing vertical resizing causes buttons to be squeezed and disappear when the window height becomes too small. The window should only be resizable in the horizontal (width/length) direction.

## Requirements

1. **Lock Vertical Resize**: Disable height resizing of the `SliderControlWPF` window while keeping width resizing enabled.
2. **Standard WPF constraints**: Implement this by enforcing equal `MinHeight` and `MaxHeight` properties in `SliderControl.xaml`.
3. **Minimum Width**: Enforce a reasonable `MinWidth` (e.g. `200` or `250`) to ensure horizontal resizing does not squash the controls horizontally either.

## Acceptance Criteria

- [ ] The `SliderControlWPF` window can be resized horizontally (length direction) by dragging the left/right window borders.
- [ ] The window height is fixed (locked) and cannot be resized vertically by dragging top/bottom borders.
- [ ] No controls/buttons are squeezed or hidden when adjusting window size.
