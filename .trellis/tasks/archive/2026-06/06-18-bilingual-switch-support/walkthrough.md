# Walkthrough: WPF Dialogs Language Switch Fix

This walkthrough describes the diagnosis, design, implementation, and verification of the bilingual switch support for the WPF windows in the Motion Grasshopper plugin.

---

## 1. Problem Diagnosis
In the initial implementation of the bilingual switch feature, language changes did not take effect on the WPF window elements due to several key factors:
1. **Mismatched Translation Keys**: The translations in `LanguageManager.cs` were written with keys that did not match the actual XAML header/content strings in the current UI controls.
2. **Modeless Window Dynamic Updating**: While most WPF windows are modal (`ShowDialog()`), `SliderControlWPF` is modeless (`Show()`). When the user clicked the language switch button on the toolbar, there was no mechanism to trigger localization on open/visible windows.
3. **Loss of Original Text**: When translating controls, writing the translated text directly back to the control's properties causes the original text to be lost. Switching languages back and forth resulted in lookup failures.
4. **Visual Tree vs. Logical Tree**: Controls defined in XAML templates or tooltips (e.g. `<Run>` inside tooltip `TextBlock`) were not traversed by simple visual tree helper routines before they were fully rendered.

---

## 2. Technical Architecture & Solution

To resolve these issues cleanly and robustly, we introduced a three-fold enhancement in `LanguageManager.cs`:

### A. Attached Dependency Property for Original Text Tracking
We registered a private attached dependency property `OriginalTextProperty` on `LanguageManager`.
- When localizing a WPF element, we capture its current header/content/text/tooltip and save it to `OriginalTextProperty` as the "ground-truth" source key.
- On subsequent translation passes (e.g. toggling languages repeatedly), the system retrieves the preserved source text from `OriginalTextProperty`, ensuring perfect translation lookups in both directions.

### B. Weak-Reference Active Windows Registry
We maintained a static registry `ActiveWindows` using `WeakReference<Window>` to track currently active or open windows without introducing memory leaks.
- Every time a window is initialized and calls `LocalizeWindow(this)`, it registers itself in `ActiveWindows`.
- When `LanguageManager.UpdateAllUI()` is executed, it loops through all registered active windows and forces a dynamic re-localization.

### C. Tree Traversal & Control Support
`LocalizeWindow` now uses:
- **Logical Tree Traversal** (`LogicalTreeHelper`) immediately in the constructor to capture elements declared in XAML.
- **Visual Tree Traversal** (`VisualTreeHelper`) registered on the window's `Loaded` event to capture dynamically generated template components.
- Support for `HeaderedContentControl` (headers), `ContentControl` (buttons, checkboxes, labels), `TextBlock` (text), `Run` (inline text in tooltips), and `ToolTip` objects.

---

## 3. Translation Key Updates
We added extensive context-specific and generic translation keys for all 6 dialog windows:
1. `ModifySliderWindow` (Modify Sender)
2. `MotionSenderSettingsWindow` (Motion Settings)
3. `RangeSelectorDialog` (Select Range)
4. `ScribbleControlWPF` (Scribble Manager)
5. `SliderControlWPF` (Slider Controller)
6. `JumpToComponentDialog` (Jump to Component)

For buttons like `确定`/`取消` or `新建`/`替换`, we use generic keys like `UI.确定` to avoid duplicating translations across different dialogs.

---

## 4. Verification & Status
- **Build Status**: `dotnet build` executes successfully with **0 errors and 0 warnings**.
- **WPF Localization**: Modeless windows (like the slider control) now update text/tooltips instantly on toolbar toggles. Modal dialogs load in the correct language and support back-and-forth language switching.
- **Trellis Task**: Phase 1 is fully complete and functional.

---

## 5. Additional Bug Fixes and Refinements
During verification, the following issues were reported and resolved:
1. **SliderControl Modeless Window Traversal Crash**: When switching to English and opening the `SliderControl` window, a `Collection was modified; enumeration operation may not execute` exception was thrown. This occurred because modifying `TextBlock` content during `LogicalTreeHelper.GetChildren` iteration modified the underlying child/inlines collection. Resolved by copying the logical child collection to a temporary list before traversing recursively.
2. **Event Component Canvas Button Localization**: Added dynamic localization to the `"Hide"`, `"Lock"`, and `"Empty Mode"` canvas labels on the `EventComponent` via `LanguageManager.GetString` inside `EventComponentAttributes.cs`.
3. **Translation Value Correction**: Corrected a typo in the Chinese translation of `"生成不重叠的区间"` (was mistakenly written as `"生成不重叠 of the intervals"`) to `"生成不重叠区间"`.
4. **Event & Event Operation Component and Parameter Localization Refinement**: Restored the original English component names `"Event"` and `"Event Operation"` for both Chinese and English localizations. Added proper bilingual translation keys for all of their input and output parameters (names and descriptions) to keep component names untouched while localizing parameter sockets correctly.
5. **Code Splitting of LanguageManager**: Refactored and split `LanguageManager.cs` using a `partial class` strategy. Moved the massive translation catalog containing all bilingual strings to a separate data-only partial file `LanguageManager.Translations.cs`. This cleanly separates localization logic from data and keeps both files under 500 lines.



