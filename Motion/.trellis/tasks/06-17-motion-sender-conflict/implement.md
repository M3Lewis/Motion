# Implementation Plan: Motion Sender & PersistentDataEditor Compatibility

## Ordered Checklist

- [x] Edit `Toolbar\ToolbarLaunchPriority.cs` to add reflection patch method `TryPatchPersistentDataEditor()` and hook into `AppDomain.CurrentDomain.AssemblyLoad`.
- [x] Run build validation command to verify project compiles successfully.
- [x] Fix compilation mismatch of `CreateMultipleEventsButton.ToolbarOrder` access modifier.

## Validation Steps
- Verified compilation succeeds with `dotnet build` (Exit code: 0).
- Double-checked namespaces and imports using git diff (we reverted all `Telepathy` namespace-faking changes).
- Re-analyzed assembly decompilation of `PersistentDataEditor` to confirm that mutating the array element in `_paramException` bypasses `FieldAccessException` and is correctly read by PDE's `ChangeDocumentObject` logic.

## Review Gates
- Verified that `ToolbarLaunchPriority.cs` dynamically mutates the first element of `_paramException` array to `"Motion.Animation.RemoteParamAttributes"` in PDE at startup or on assembly load.
