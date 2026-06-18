# Bilingual Switch Support

## Goal
Add bilingual switching functionality to the Motion plugin, allowing users to toggle between Chinese (ZH) and English (EN). This includes updating component/parameter names and descriptions (both on the canvas and in the Grasshopper ribbon), Grasshopper toolbar button tooltips, and all WPF UI dialog windows dynamically.

## Requirements
- **Language Selection**: Add a new dedicated language switch toolbar button (`LanguageSwitchButton`) that toggles the active language between English and Chinese and dynamically updates its icon ("EN" / "中").
- **Configuration Persistence**: Save the selected language setting locally (e.g. `LanguageSettings.xml` in ApplicationData) and load it upon startup. On first launch, the default language is auto-detected from the Windows system culture settings (defaulting to Chinese if it is Chinese, and English otherwise).
- **Component & Parameter Descriptions**: Update names and descriptions of all custom Grasshopper components and parameters dynamically when the language switches.
- **Grasshopper Toolbar Buttons**: Translate tooltips/descriptions for all custom toolbar buttons.
- **WPF UI Dialogs**: Localize all WPF windows (titles, labels, buttons, tooltips, checkboxes) and ensure they open with the active language.
- **Immediate Update**: Changing the language must immediately update the canvas components, ribbon icons/tooltips, and toolbar buttons without requiring a Rhino/Grasshopper restart.

## Acceptance Criteria
- [ ] Language settings are persisted to `LanguageSettings.xml` and loaded on startup; first launch auto-detects Windows system culture.
- [ ] A dedicated `LanguageSwitchButton` on the toolbar toggles between English and 中文, with a dynamic icon ("EN" / "中").
- [ ] Upon language switch, canvas components dynamically update their displayed Name and Description.
- [ ] Ribbon panel component entries update their names and tooltips.
- [ ] All 13 toolbar buttons update their tooltips.
- [ ] Canvas-rendered custom text ("Hide", "Lock", "Empty Mode", "Export", "Open") switches language.
- [ ] All WPF dialog windows and popup MessageBoxes are fully translated into English and Chinese.
- [ ] All toast notifications (ShowTemporaryMessage) are fully translated.
- [ ] Component input/output parameter names and descriptions are translated.
- [ ] Compilation succeeds without error or regression.
