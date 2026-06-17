# Design: Code Optimization and Refactoring

## 1. Class Splitting: `EventComponent.cs`
`EventComponent` has 1097 lines and handles multiple concerns. We will split it into five files using C# `partial class` support:
1. `EventComponent.cs`: Contains fields, constructor, properties, parameter registration, and icon/guid properties.
2. `EventComponent.Solve.cs`: Contains the core `SolveInstance` method, visiblity/lock update logic (`UpdateGroupVisibilityAndLock`), and custom layout/drawing hooks if any.
3. `EventComponent.Events.cs`: Contains the Grasshopper document event subscriptions and callbacks (`AddedToDocument`, `RemovedFromDocument`, `DocumentContextChanged`, `OnSliderValueChanged`, etc.).
4. `EventComponent.Serialization.cs`: Contains state persistence (`Write` and `Read` methods) and document load handlers.
5. `EventComponent.Menu.cs`: Handles contextual menu additions (`AppendAdditionalMenuItems`) and related click/action events.

All split files will use the same namespace:
```csharp
namespace Motion.Animation
{
    public partial class EventComponent : GH_Component
    {
        // ...
    }
}
```

## 2. Async Safety: `ExportSliderAnimation.cs`
The `SolveInstance` override is currently marked `async void`:
```csharp
protected override async void SolveInstance(IGH_DataAccess DA)
```
Since it runs synchronously on the main UI thread via `RhinoApp.InvokeOnUiThread` nested inside a `Task.Run`, we will remove `async void` and make `SolveInstance` a standard `void` method. We will execute the task safely by discarding the returned `Task` via `_ = ExecuteRenderingWithParams(parameters, DA);`, which prevents any unhandled background crashes while keeping execution asynchronous.
We will also change the lambda in `CreateAttributes()` from `async (sender, e, isExport) => { await ExecuteRenderingAsync(); }` to a normal non-async lambda `(sender, e, isExport) => { _ = ExecuteRenderingAsync(); }` to avoid an anonymous `async void` method.

## 3. Resource Cleanup: `ClickFinderButton.cs`
We will fix the unmanaged memory leak of `Mesh` objects in `ClickFinderButton.cs`:
1. Wrap local temporary `boxMesh` in a `using` block:
   ```csharp
   using (Mesh boxMesh = Mesh.CreateFromBox(box, 1, 1, 1))
   {
       if (boxMesh != null && boxMesh.IsValid)
       {
           boxMesh.Compact();
           previewMesh.Append(boxMesh);
       }
   }
   ```
2. Correctly dispose the member `previewMesh` before assigning `null` or re-creating it:
   ```csharp
   if (previewMesh != null)
   {
       previewMesh.Dispose();
       previewMesh = null;
   }
   ```
