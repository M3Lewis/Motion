# Directory Structure (UI & Interaction)

> Where UI controls, custom attributes, and WPF views are located.

---

## UI Components Layout

- **`Motion/Components/UI/`**
  - Contains core reusable custom attributes templates and custom button parameters (e.g., `MotionButton`, `MotionButtonAttributes`).
- **`Motion/Components/01_Animation/`**
  - Contains custom drawing attributes for specific animation parameters, such as `RemoteParamAttributes` for the `Motion Sender` component.
- **`Motion/Button/`**
  - Custom WPF and WinForms button controls and dialogs that extend Grasshopper canvas (e.g. `ModifySliderWindow`, `ScribbleWPFWindow`).
- **`Motion/Toolbar/`**
  - Main menu registrations, toolbar icons loader, and layout configurations.
