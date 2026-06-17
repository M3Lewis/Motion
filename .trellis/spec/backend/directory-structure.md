# Directory Structure (C# / .NET)

> How the Motion plugin source code is organized.

---

## Directory Layout

```text
Motion/
├── Button/            # Custom toolbar buttons (WPF/WinForms buttons)
├── Components/        # Grasshopper components, grouped by category
│   ├── 01_Animation/  # Timeline slider, senders, events, remote params
│   ├── 02_Export/     # Rendering, viewports, cycles export animators
│   ├── 03_Utils/      # Layout arrange, dynamic outputs, color, viewport FOV
│   └── UI/            # Reusable button controls and custom attributes templates
├── General/           # Shared utility classes and extension methods
├── Icons/             # Component and button image resources
├── Properties/        # Assembly metadata and resources configuration (Resources.resx)
└── Toolbar/           # Toolbar launch priority and menus integration
```

---

## Namespaces Mapping

- `Motion.Animation`: Core animation timeline, sender, receiver, event logic.
- `Motion.Export`: Rendering execution, Cycle passes, image export, Directory Opus integration.
- `Motion.UI`: Custom Canvas UI controls, drawing attributes templates.
- `Motion.Utils`: Math utilities, color conversion, layout arranging, viewport manipulation.
- `ExtraButtons` / `Toolbar`: Assembly priority load (`GH_AssemblyPriority`), toolbar initialization.

---

## Code Reuse Rule

- Always search `General/` or other utility classes before writing a helper function.
- Do NOT repeat rendering or canvas-related utility functions across components; group them in `General/` or use extension methods.
