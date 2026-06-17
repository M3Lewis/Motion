# Solver & Data Flow Guidelines

> Best practices for parameter registration, input extraction, and solver calculations in Grasshopper Components.

---

## 1. Parameter Registration

- Define human-readable Name, Nickname, and Description for all input/output parameters.
- Mark optional parameters explicitly as `.Optional = true` to avoid Solver compilation warnings.
- Prefer system-default standard parameters (e.g., `Param_FilePath`, `GH_Interval`) over plain text strings when managing typed data.

```csharp
protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
{
    pManager.AddTextParameter("View Name", "V", "视图名称", GH_ParamAccess.item, "Perspective");
    pManager.AddIntervalParameter("Range Domain", "D", "导出范围（可选）", GH_ParamAccess.item);
    pManager[1].Optional = true; // explicitly optional
}
```

---

## 2. Parameter Retrieval

- Always perform structured validation in `SolveInstance` or local parameter extraction helpers.
- Use `DA.GetData` or `VolatileData.AllData` safely.
- Avoid raw index access without first checking bounds, especially on dynamic outputs.

---

## 3. Data Tree Operations

- When processing lists or data trees, use `GH_Structure` or `List<T>` as appropriate.
- Ensure that if inputs are changed, you properly invalidate downstream data using `ExpireSolution(true)`.

---

## 4. Performance & Solvers

- Avoid heavy computation inside `SolveInstance` on the main UI thread.
- If performing rendering or export (e.g., exporting frames, processing audio files), use asynchronous code (`async`/`Task.Run`) and handle UI updates on `RhinoApp.InvokeOnUiThread`.
