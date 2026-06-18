# Implementation Plan: Bilingual Switch Support (v2)

## Phase 1: Core Infrastructure + Visible UI (This Task)

### Step 1 — LanguageManager
- [x] Create `Motion/General/LanguageManager.cs`
  - `Language` enum (ZH, EN)
  - Static `CurrentLanguage` property with setter that fires `LanguageChanged`
  - `GetString(key, fallback)` lookup
  - `Translations` dictionary with hierarchical keys
  - `Load()` / `Save()` persistence to `%appdata%\Grasshopper\Motion\LanguageSettings.xml`
  - `DetectSystemLanguage()` using `CultureInfo.CurrentUICulture`
  - `UpdateAllUI()` orchestrator

### Step 2 — Toolbar Base Class
- [x] Update `MotionToolbarButton.cs`
  - Add `protected ToolStripItem MyButton { get; set; }`
  - Set `MyButton` in `AddButtonToToolbars()` before delegation
  - Add `public virtual void UpdateLanguage() { }`

### Step 3 — Language Switch Button
- [x] Create `Motion/Button/LanguageSwitchButton.cs`
  - Inherit `MotionToolbarButton`
  - Dynamic GDI+ icon rendering ("中" / "EN")
  - Click handler toggles language + calls `UpdateAllUI()`
  - Subscribe to `LanguageChanged` for self-update

### Step 4 — Toolbar Button Tooltips
- [x] Override `UpdateLanguage()` in each toolbar button class:
  - `ToolbarPositionManagerButton` (tooltip + context menu items)
  - `ModifySenderButton`
  - `MotionSenderSettingsButton`
  - `ClickFinderButton`
  - `CreateMultipleEventsButton`
  - `CreateUnionSliderButton`
  - `JumpToAffectedComponentButton`
  - `NamedViewSwitch`
  - `RangeSelectorButton`
  - `ScribbleControlButton`
  - `SliderControlWPFButton`
  - `ConnectToEventOperationButton`
  - `CameraPointsButton`

### Step 5 — Component Name/Description
- [x] Override `Name` and `Description` getters in all 23 GH_Component subclasses:
  - Animation: EventComponent, EventOperation, IntervalLock, IntervalSwitcher, MultiTransform, TimeInterval
  - Params: MotionSender (RemoteParam), MotionSlider (GH_NumberSlider)
  - Export: ExportSliderAnimation
  - Utils: ZDepth, PointOnView, MotionText, MotionMaterial, MotionImageSelector, MotionImagePreview, MotionCamera, MetroTileComponent, ImageTransformSettings, GetViewportFOV, FilletEdgeIndex, DynamicOutput, ComponentArrange, ColorAlpha, AdjustSearchCount, Win8TileFlip
- [x] Add all translation entries to `LanguageManager.Translations`

### Step 6 — Canvas-Rendered Custom Text
- [x] `EventComponentAttributes.cs`: Replace hardcoded "Hide", "Lock", "Empty Mode" with `LanguageManager.GetString()`
- [x] `MotionButtonTemplate.cs`: Replace hardcoded button labels in `Render()` with `LanguageManager.GetString()`
- [x] `EventComponent.Menu.cs`: Translate right-click menu tooltip text

### Step 7 — WPF Dialog Localization
- [x] Add `ApplyLanguage()` method to each dialog, called from constructor:
  - `ModifySenderButtonWindow.xaml.cs` (window title, ~8 GroupBox headers, ~12 button contents, ~6 labels, ~4 checkboxes, tooltips)
  - `MotionSenderSettingsButtonWindow.xaml.cs` (window title, labels, button)
  - `RangeSelectorDialog.xaml.cs` (window title, label, button)
  - `ScribbleControlWPF.xaml.cs` (window title, ~6 labels, buttons)
  - `SliderControl.xaml.cs` (window title, button tooltips) + subscribe to `LanguageChanged`
  - `JumpToComponentDialog.xaml.cs` (window title, buttons)

### Step 8 — Ribbon Update Logic
- [x] Implement ribbon proxy update in `LanguageManager.UpdateAllUI()`
  - Iterate `Instances.ComponentServer.ObjectProxies` matching our library GUID
  - Update `proxy.Desc.Name` and `proxy.Desc.Description`
  - Call `Instances.DocumentEditor?.RebuildRibbon()`

### Step 9 — MessageBox, Toast & Port Translation
- [x] Translate all ~30 MessageBox.Show strings in code
- [x] Translate all ~15 ShowTemporaryMessage toast notifications in code
- [x] Implement parameter localization helper and localize all component/parameter input/output ports

### Step 10 — Build Validation
- [x] Compile using MSBuild, verify exit code 0
- [x] Verify no regressions in existing toolbar/component behavior

## Validation Steps
- Compile using `dotnet build` or MSBuild → exit code 0
- Verify: language auto-detected on first launch matches Windows system culture
- Verify: clicking language button toggles EN↔ZH
- Verify: canvas components update Name/Description immediately
- Verify: ribbon panel updates names/tooltips after toggle
- Verify: toolbar button tooltips change language
- Verify: canvas-rendered "Hide"/"Lock"/"Empty Mode" text switches
- Verify: WPF dialogs open in correct language
- Verify: SliderControlWPF updates dynamically if open during toggle
- Verify: language setting persists across Grasshopper restarts
