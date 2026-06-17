# Interaction & Event Hooks

> Guidelines for extending user interactions, double-click behaviors, and context menu options.

---

## 1. Double-click Interactions

In Motion, double-clicking component bodies triggers navigation or parameter setting commands.
- Override `RespondToMouseDoubleClick` or add a double-click handler inside custom attributes.
- Use double-clicks to jump to corresponding views or event controllers (e.g., jumping from `Event` component to its `EventOperation`).

---

## 2. Custom Canvas Context Menus

When adding context menus to components:
- Override `AppendAdditionalMenuItems(ToolStripDropDown menu)`.
- Use `GH_DocumentObject.Menu_AppendItem` to add custom actions, toggle parameters, or open WPF dialog windows.
- Always check state bounds and verify the component context before modifying parameter properties.

```csharp
public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
{
    base.AppendAdditionalMenuItems(menu);
    Menu_AppendItem(menu, "Name Current Group as Event Name", (sender, e) => {
        SyncGroupNames();
    });
}
```

---

## 3. Global Assembly Priority Hooks

- Startup patches, custom menu overrides, and assembly resolutions should run inside a class extending `GH_AssemblyPriority` (e.g., `ToolbarLaunchPriority`).
- Keep priority load logic lightweight to avoid slowing down Grasshopper loading time.
