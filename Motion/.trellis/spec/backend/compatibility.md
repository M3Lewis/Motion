# Third-Party Plugin Compatibility Guidelines

This document specifies how `Motion` resolves conflicts and ensures compatibility with third-party Grasshopper plugins at runtime.

---

## Scenario: PersistentDataEditor (PDE) Attributes Conflict

### 1. Scope / Trigger
- **Trigger**: When third-party plugins (like `PersistentDataEditor`) dynamically intercept and replace floating parameter attributes (`GH_FloatingParamAttributes`) with their own custom attributes, rendering custom UI elements or event handlers in our own floating parameters (e.g. `RemoteParamAttributes`) inactive.

### 2. Signatures
- Target class to patch: `PersistentDataEditor.SimpleAssemblyPriority`
- Target field to patch: `private static readonly string[] _paramException`
- Signature of patching method in `Motion`:
  ```csharp
  private static void TryPatchPersistentDataEditor()
  ```

### 3. Contracts
- **Assembly Detection**: Check for loaded `PersistentDataEditor` assembly in the current `AppDomain`.
- **Target Field Retrieval**: Query `_paramException` via reflection (`BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public`).
- **Array Mutation**: Since setting `static readonly` fields using reflection throws `FieldAccessException` in modern .NET (.NET Core / .NET 7.0+), the array contents are mutated in-place by overwriting the first element (`original[0] = "Motion.Animation.RemoteParamAttributes"`).
- **Race Condition Guard**: Must execute both at `PriorityLoad` and on the `AppDomain.CurrentDomain.AssemblyLoad` event handler to support arbitrary load order.

### 4. Validation & Error Matrix

| Condition | Action | Result / Recovery |
|-----------|--------|-------------------|
| PDE not loaded yet | Subscribes to `AssemblyLoad` | Patches once PDE loads later |
| PDE already loaded | Mutates array element | Succeeds immediately |
| Reflection fails (e.g. field renamed) | Catch and log/debug write | Quietly ignore, prevents crash |

### 5. Good/Base/Bad Cases

#### Base Case
PDE is loaded before or after `Motion`. The string `"Motion.Animation.RemoteParamAttributes"` is dynamically written into `_paramException[0]`. PDE bypasses replacement of our custom parameter attributes.

#### Bad Case (Forbidden Namespace Faking)
Modifying the namespace of `RemoteParamAttributes` to `Telepathy` to match PDE's hardcoded exclusions. This pollutes codebases, creates import mismatches, and confuses code maintainers.

### 6. Tests Required
- **Static Analysis**: Verify compile-time checks that `RemoteParamAttributes` is in `Motion.Animation` namespace.
- **Runtime Check**: Debug output verification that `TryPatchPersistentDataEditor()` does not throw an exception when PDE is not present, and successfully completes patch when PDE is present.

### 7. Wrong vs Correct

#### Wrong (Namespace Faking Hack)
```csharp
namespace Telepathy
{
    // Pollution: defining custom attributes in another plugin's namespace
    public class RemoteParamAttributes : GH_FloatingParamAttributes
    {
        ...
    }
}
```

#### Correct (Dynamic Runtime Reflection Patch via Array Mutation)
```csharp
namespace ExtraButtons
{
    public class ToolbarLaunchPriority : GH_AssemblyPriority
    {
        private static bool _pdePatched = false;

        public override GH_LoadingInstruction PriorityLoad()
        {
            TryPatchPersistentDataEditor();
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
            return GH_LoadingInstruction.Proceed;
        }

        private static void TryPatchPersistentDataEditor()
        {
            if (_pdePatched) return;
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var pdeAssembly = assemblies.FirstOrDefault(a => a.GetName().Name.Equals("PersistentDataEditor", StringComparison.OrdinalIgnoreCase));
                if (pdeAssembly != null)
                {
                    var type = pdeAssembly.GetType("PersistentDataEditor.SimpleAssemblyPriority");
                    if (type != null)
                    {
                        var field = type.GetField("_paramException", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                        if (field != null)
                        {
                            var original = (string[])field.GetValue(null);
                            if (original != null && original.Length > 0)
                            {
                                original[0] = "Motion.Animation.RemoteParamAttributes";
                                _pdePatched = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Motion] PDE patch failed: " + ex.Message);
            }
        }
    }
}
```
