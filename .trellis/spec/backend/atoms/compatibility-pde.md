---
id: compatibility.pde.attributes-patch
type: pitfall
priority: must
applies_when:
  - modifying custom floating parameter attributes
  - changing assembly priority startup logic
  - working on RemoteParamAttributes or ToolbarLaunchPriority
code_anchors:
  - Motion/Components/01_Animation/RemoteParamAttributes.cs
  - Motion/Toolbar/ToolbarLaunchPriority.cs
verify:
  - TryPatchPersistentDataEditor() mutates the array element in-place to avoid FieldAccessException in .NET 7+
source:
  kind: bug_history
  ref: task-06-17-motion-sender-conflict
last_checked: 2026-06-17
---

# Rule

To prevent the PersistentDataEditor (PDE) plugin from dynamically replacing our custom floating parameter attributes (`RemoteParamAttributes`) with its own, we must mutate PDE's internal array of exceptions at startup. 

Do NOT fake or change the namespace of `RemoteParamAttributes` to `Telepathy`. Instead, dynamically overwrite the first element of PDE's `_paramException` array at runtime.

# Why

PDE intercepts floating parameters and replaces their attributes. It has a hardcoded array of exceptions: `_paramException = ["Telepathy.RemoteParamAttributes"]`. Faking the `Telepathy` namespace pollutes the codebase and is hard to maintain.

Because modern Grasshopper (Rhino 8) runs on .NET Core / .NET 7.0+, modifying `static readonly` fields directly via reflection throws a `FieldAccessException`. However, mutating the elements of the existing array in-place (`original[0] = "Motion.Animation.RemoteParamAttributes"`) bypasses this restriction and is safe on all .NET runtimes.
