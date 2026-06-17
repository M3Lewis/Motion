# Logging Guidelines

> Guidelines for emitting diagnostic messages and user notifications in the Rhino/Grasshopper context.

---

## 1. Grasshopper Runtime Messages

Use the Grasshopper built-in component message mechanism to notify users of status, warnings, and errors directly on the canvas.

- `GH_RuntimeMessageLevel.Error`: Used when the solve operation cannot complete (e.g. missing inputs, invalid file types, exceptions).
- `GH_RuntimeMessageLevel.Warning`: Used when the solver succeeded but with unexpected parameters (e.g. customized range exceeds slider bounds).
- `GH_RuntimeMessageLevel.Remark`: Information messages, non-intrusive notes.

---

## 2. Rhino Command Line Output

For longer operations (e.g., animation exporting sequence), write progress to the Rhino command line window to allow tracking.

- Use `RhinoApp.WriteLine(string)` or `RhinoApp.Write(string)`.
- Include timestamps for start/end actions.

```csharp
RhinoApp.WriteLine($"Render {(wasAborted ? "cancelled" : "finished")} at {DateTime.Now:HH:mm:ss}");
```

---

## 3. Debug Output

- For internal developer tracing, use `System.Diagnostics.Debug.WriteLine` or `Console.WriteLine`.
- Ensure heavy debug logging is compiled out in Release builds or wrapped in `#if DEBUG` directives.
