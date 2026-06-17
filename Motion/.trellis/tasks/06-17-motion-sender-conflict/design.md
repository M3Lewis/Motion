# Technical Design: Motion Sender & PersistentDataEditor Bypass

## Background & Problem
- **PersistentDataEditor (PDE)** dynamically patches Grasshopper canvas rendering and layout to inject its custom parameter attributes (`GH_AdvancedFloatingParamAttr` and `GH_AdvancedLinkParamAttr`).
- When a floating parameter is added, PDE checks if its attributes are a subclass of `GH_FloatingParamAttributes` but not already `GH_AdvancedFloatingParamAttr`.
- Since `MotionSender` inherits from `RemoteParam` and its attributes class `RemoteParamAttributes` inherits from `GH_FloatingParamAttributes`, PDE replaces the attributes object with `GH_AdvancedFloatingParamAttr`.
- This wipes out our custom `RemoteParamAttributes` drawing/interaction logic, breaking `Motion Sender` UI and double-clicking.

## Bypass Analysis
- In PDE's source code, `ChangeDocumentObject` checks a list of exceptions (`_paramException`) before performing the replacement.
- This list is defined in `PersistentDataEditor.SimpleAssemblyPriority` as:
  ```csharp
  static readonly string[] _paramException = ["Telepathy.RemoteParamAttributes"]
  ```
- Rather than faking the `Telepathy` namespace in our own assemblies (which would pollute the code and be confusing), we can dynamically modify PDE's `_paramException` array at runtime using reflection.

## Proposed Design
We will dynamically modify the `_paramException` list in `PersistentDataEditor.SimpleAssemblyPriority` when the `Motion` assembly loads.
- Since Rhino 8 / Grasshopper runs on modern .NET (.NET 7.0+), a `static readonly` field cannot be overwritten via reflection without throwing `FieldAccessException`.
- However, the array *instance* itself is mutable. We can retrieve the array reference via reflection and overwrite its first element `original[0] = "Motion.Animation.RemoteParamAttributes"`.
- This avoids setting the field itself and works reliably on modern .NET.
- This will be done in `ToolbarLaunchPriority.cs` (which inherits from `GH_AssemblyPriority` and runs during startup).
- To make this robust against assembly load ordering, we also subscribe to the `AppDomain.CurrentDomain.AssemblyLoad` event. If PDE loads after `Motion`, we catch it and apply the patch.

## Code Adjustments
1. **`Toolbar\ToolbarLaunchPriority.cs`**:
   - Add a private field `_pdePatched` to avoid patching multiple times.
   - Run `TryPatchPersistentDataEditor()` inside `PriorityLoad()`.
   - Subscribe to `AppDomain.CurrentDomain.AssemblyLoad` and run `TryPatchPersistentDataEditor()` when `PersistentDataEditor` assembly is loaded.
   - The method searches for the `PersistentDataEditor` assembly, retrieves the `PersistentDataEditor.SimpleAssemblyPriority` type, finds the static non-public field `_paramException`, reads the existing string array, and overwrites the first element (index 0) with `"Motion.Animation.RemoteParamAttributes"`.
