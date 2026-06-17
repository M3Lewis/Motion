# Optimize and Refactor Code

## Goal

Optimize the Motion Grasshopper plugin codebase by refactoring large files, reducing cognitive complexity, resolving async/threading errors, and addressing resource cleanup warnings (disposable misuse) to improve maintainability and stability.

## Requirements

### 1. File & Class Splitting (Cognitive Load Reduction)
- Split bloated classes (files exceeding 500 lines) into logical, cohesive `partial` classes or separate files following the `file-splitter` skill.
- Target: `Motion.Animation.EventComponent` (1097 lines). Divide it into:
  - Main class/ctor/metadata (`EventComponent.cs`)
  - Solver & UI State Update (`EventComponent.Solve.cs`)
  - Document Event & Connection Handlers (`EventComponent.Events.cs`)
  - XML/GH Serialization (`EventComponent.Serialization.cs`)
  - Context Menu Handlers (`EventComponent.Menu.cs`)

### 2. Async/Threading Violations
- Resolve the critical compiler/async violation:
  - `ExportSliderAnimation.cs` line 103: `async void SolveInstance` must be refactored to avoid unhandled exceptions crash and proper async/Task execution flow in Grasshopper.

### 3. Resource & GDI Cleanup (Disposable Misuse)
- Resolve GDI Pen/Brush and geometry (Mesh) leaks identified by static analysis to prevent memory leaks in Rhino.
- Target key disposable warnings in `CameraPointsButton.cs`, `ClickFinderButton.cs`, and other toolbar buttons where canvas documents and GDI objects are used.

### 4. Compilation & Verification
- Ensure the solution builds successfully under all target SDK configurations without breaking existing canvas features.

## Acceptance Criteria

- [ ] `EventComponent.cs` is split into smaller, organized files with no single file exceeding 500 lines.
- [ ] `ExportSliderAnimation.SolveInstance` does not use `async void` in a way that risks unhandled background thread crashes.
- [ ] Key disposable leaks in toolbar buttons (`CameraPointsButton.cs`, `ClickFinderButton.cs`) are fixed using `using` blocks.
- [ ] The solution builds successfully with `dotnet build` or `msbuild`.
- [ ] No regression of existing Grasshopper slider animation or custom button canvas interactions.

## Notes
- Keep all refactorings completely backwards-compatible; do not change public component GUIDs or user-facing parameter inputs.
- Use `partial class` to split `EventComponent` to ensure full binary/assembly compatibility without changing namespace or inheritance hierarchy.
