# Type Safety & Parameter Casting

> Guidelines for safe casting and parameter checks in Grasshopper components.

---

## 1. Parameter Conversion

Grasshopper inputs can be fed with varied or incorrect data types from users. Always validate parameter conversion safely:

- **Check Types**: Do not perform direct type casts (`(GH_Interval)data`). Instead, use the `is` keyword or `as` keyword checks to verify data types before extraction.
- **Handling Defaults**: Provide sensible fallbacks or default values when parameters are optional and empty.

```csharp
var rangeGoo = this.Params.Input[7].VolatileData.AllData(true).FirstOrDefault();
if (rangeGoo != null && rangeGoo is GH_Interval ghInterval)
{
    parameters.Range = ghInterval.Value;
    parameters.IsCustomRange = true;
}
```

---

## 2. Canvas Object Verification

When searching for connected components or sliders on the document canvas:
- Iterate objects using type filters, e.g. `doc.Objects.OfType<GH_NumberSlider>()`.
- Verify the object is not null and matches expected ID or Nickname formats.
- When inspecting target source inputs, check types:

```csharp
var source = this.Params.Input[9].Sources[0];
if (source is MotionSlider motionSlider)
{
    // safe to use motionSlider
}
```
