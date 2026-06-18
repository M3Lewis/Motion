---
id: frontend.wpf.logical-tree-traversal-modification
type: pitfall
priority: must
applies_when:
  - traversing WPF logical tree elements
  - performing batch modifications on UI elements in WPF windows or modeless dialogs
code_anchors:
  - Motion/General/LanguageManager.cs
verify:
  - Verify that child lists are cloned to a temporary collection before iteration when modifying child properties.
source:
  kind: human_confirmed
  ref: task-06-18-bilingual-switch-support
last_checked: 2026-06-18
---

# Rule

When traversing WPF logical tree elements using `LogicalTreeHelper.GetChildren()` and modifying their properties (such as `Text` or text-related inlines) during iteration, always clone the children collection to a temporary list first. Do not enumerate the live logical tree children collection directly.

```csharp
// Correct: Clone to a temporary list before iteration
var children = new List<object>();
var logicalChildren = System.Windows.LogicalTreeHelper.GetChildren(element);
if (logicalChildren != null)
{
    foreach (object child in logicalChildren)
    {
        if (child != null) children.Add(child);
    }
}

foreach (object child in children)
{
    if (child is System.Windows.DependencyObject dobj)
    {
        LocalizeLogicalElement(dobj, windowName);
    }
}
```

# Why

Modifying text properties or inlines on elements (such as `TextBlock` or custom controls) mutates the underlying logical tree child collection. Iterating directly on the live collection will throw a `System.InvalidOperationException: "Collection was modified; enumeration operation may not execute"` exception, causing runtime crashes (particularly when modeless windows are open during dynamic localization/language switching).
