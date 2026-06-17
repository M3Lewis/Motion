# Error & Resource Management Guidelines

> Guidelines for crash prevention, async safety, and resource cleanups in Grasshopper.

---

## 1. Exception Boundaries

Rhino is a single-process application. An unhandled exception inside a Grasshopper solver cycle or mouse handler can freeze or crash Rhino entirely.

- **Catch All**: Wrap heavy operations and external process calls in `try-catch` blocks.
- **Feedback**: Display error diagnostics via `AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message)` rather than failing silently.

---

## 2. Asynchronous Safety & cancellation

When running async tasks (e.g., rendering views, saving images, scanning folders):
- Always instantiate a `CancellationTokenSource`.
- Ensure the cancellation token is propagated down to the animator or exporter.
- Listen to keyboard hooks (like pressing `ESC`) to trigger token cancellation.

```csharp
Action<int, int> updateProgress = (frame, total) =>
{
    if (Control.ModifierKeys == Keys.Escape && _cancellationTokenSource != null)
    {
        _cancellationTokenSource.Cancel();
    }
};
```

---

## 3. Resource Disposal Lifecycle

- Components that manage `CancellationTokenSource`, file streams, or GDI bitmaps MUST implement `Dispose` or override the component's clean-up methods to release unmanaged objects.
- Always clear references to event handlers when components are removed or garbage-collected to prevent memory leaks.

```csharp
protected override void Dispose()
{
    if (_cancellationTokenSource != null)
    {
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }
    base.Dispose();
}
```
