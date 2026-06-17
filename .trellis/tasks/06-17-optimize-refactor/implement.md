# Implementation Plan: Code Optimization and Refactoring

## Steps

### Step 1: Pre-development Verification
- Confirm the current clean workspace status.
- Compile the solution to establish a baseline:
  ```powershell
  dotnet build Motion.sln
  ```

### Step 2: Fix Resource Leaks in `ClickFinderButton.cs`
- Add a `using` statement to dispose the transient `boxMesh` in `CollectPreviewObjects()`.
- Add `.Dispose()` calls to `previewMesh` before assigning `null` in `ToggleClickFinderMode` and `CheckMouseClick`, and before instantiating a new `Mesh` in `CollectPreviewObjects()`.

### Step 3: Fix Async Violations in `ExportSliderAnimation.cs`
- Remove `async` from `SolveInstance(IGH_DataAccess DA)` and replace the `await` call with `_ = ExecuteRenderingWithParams(parameters, DA);`.
- In `CreateAttributes()`, remove `async` from the lambda and replace `await ExecuteRenderingAsync();` with `_ = ExecuteRenderingAsync();`.

### Step 4: Split `EventComponent.cs`
- Make `EventComponent` a `partial class`.
- Create the four new partial class files under `Motion/Components/01_Animation/`:
  - `EventComponent.Solve.cs`
  - `EventComponent.Events.cs`
  - `EventComponent.Serialization.cs`
  - `EventComponent.Menu.cs`
- Move respective code blocks from `EventComponent.cs` to the corresponding files.
- Verify that `EventComponent.cs` is cleanly reduced under 400 lines.

### Step 5: Verify & Build
- Re-run `dotnet build Motion.sln` to ensure compilation is fully successful.

### Step 6: Fix Compiler Warnings
- Fix unused event `ValueChanged` in `MotionSlider.cs`.
- Remove unused fields `_isDraggingSlider` and unused `ex` catch variables in `MotionSlider.cs`.
- Remove unused fields `_buttonRepeatTimer` and `_currentButton` in `SliderControl.xaml.cs`.
- Comment out unused field `button` and unused `Instantiate` method in `UpdateSenderToolbarButton.cs`.
- Remove unused field `_ImageFiles` and `ex` catch variables in `MotionMaterial.cs`.
- Remove unused fields `HideButtonBounds`, `LockButtonBounds`, `DataButtonBounds`, `CollapseButtonBounds`, `IsCollapsed`, `mouseOver` and RespondToMouseMove method in `RemoteParamAttributes.cs`.
- Remove unused fields `ButtonWidth`, `ButtonHeight`, `ButtonSpacing` in `EventComponentAttributes.cs`.
- Re-run build to verify 0 warnings and 0 errors.
