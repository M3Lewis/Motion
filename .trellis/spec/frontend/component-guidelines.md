# Canvas UI & Custom Attributes

> Guidelines for writing custom `GH_ComponentAttributes` and drawing custom UI controls directly on the Grasshopper Canvas.

---

## 1. Canvas Layout & Bounds Inflation

When adding custom UI elements (like buttons, sliders, or status texts) to a component:
- Override `Layout()`.
- Call `base.Layout()` first to calculate the standard component box size.
- Expand `Bounds` (inflate width or height) to accommodate your custom drawing elements.

```csharp
protected override void Layout()
{
    base.Layout();
    // Increase height by 40 units to fit two custom buttons at the bottom
    Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + 40);
}
```

---

## 2. Rendering with Capsules

- Always check that `channel == GH_CanvasChannel.Objects` before drawing custom visual elements to prevent double-draw or rendering artifacts.
- Use `GH_Capsule.CreateCapsule` to draw buttons/containers matching Grasshopper's native style.
- Dispose of drawing brushes, capsules, and fonts (or use the built-in `GH_FontServer` fonts) to prevent graphics memory leaks.

```csharp
protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
{
    base.Render(canvas, graphics, channel);
    if (channel == GH_CanvasChannel.Objects)
    {
        RectangleF buttonRect = new RectangleF(Bounds.X, Bounds.Bottom - 20, Bounds.Width, 20.0f);
        buttonRect.Inflate(-2.0f, -2.0f);

        using (GH_Capsule capsule = GH_Capsule.CreateCapsule(buttonRect, PressedOpen ? GH_Palette.Grey : GH_Palette.Black))
        {
            capsule.Render(graphics, Selected, Owner.Locked, Owner.Hidden);
        }
    }
}
```

---

## 3. Mouse Event Responses

- Override `RespondToMouseDown` and `RespondToMouseUp` to capture clicks on custom button rects.
- Always check if the click location (`e.CanvasLocation`) is within your custom buttons bounding box.
- Trigger actions, change button state properties (e.g. `PressedExport = true`), call `sender.Refresh()`, and return `GH_ObjectResponse.Handled` to consume the click.
- In `RespondToMouseUp`, reset pressed states and call `sender.Refresh()`.
