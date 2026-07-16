# Changelog

## v1.1.3 — 2026.07.16

### Features

- **Per-section fonts & font sizes**: separate font and font-size configuration for each overlay section (Main, BPM, Judgement, ComboTitle, ComboVal, Timing, Attempt); "Use Global Font" toggle per slot (ac9a396)
- **UI patch toggles**: independent boolean toggles in Settings — Patch Beta Watermark, Patch Level Name, Reposition Auto Text — each can be enabled/disabled and dynamically reset (46b9dcc)
- **Text effects panel**: global shadow (TMP Underlay) and outline (TMP Outline keyword) with toggles, RGBA color pickers, Width/Softness sliders; changes apply live via `ApplyFontToAll()` + `ShadowManager.ClearCache()`
- **Trilingual localization**: all new UI sections fully translated (EN/KO/CN) via `Tr.Get()` — Text Effects (session) + Section-level font options + UI toggle labels (46b9dcc)
- **Dual-loader architecture**: support for both MelonLoader and UMM via `IModLoader` interface; separate entry assemblies (`JipperOverlayer.Loader.Melon` / `.UMM`); platform-specific build artifacts (e985450)

### Performance

- **Music time throttle**: update interval tightened from 1s → 0.1s (`_lastMusicTimeTick`) for smoother display at minimal CPU cost; added clip‑null/zero‑length fallback using last floor `entryTime` as total duration (71d73ec)

### Bug Fixes

- **Toggle UI**: checkbox and label merged into a single clickable area (c2e80a4)
- **Level name position drifts after death**: `Hide()` now calls `ResetLevelName()` to restore original position/scale/size before nulling cache fields, preventing cumulative offset on scene restart
- **Level name size not restored on mod disable**: `Destroy()` now calls `ResetLevelName()` so scale/sizeDelta revert when mod unloads

### Refactors

- **PatchManager**: removed `Main` dependency; `HarmonyId` stored internally instead of requiring assembly‑scoped id (9f989fc)
- **Awake_Rewind patch removed**: level‑name positioning moved from Harmony patch into `Overlay.UpdateSize()` / `Show()`, giving finer control over when and whether the patch applies (46b9dcc)
- **Extracted `ApplyLevelNamePatch()`**: unified level-name positioning logic from `Show()` and `UpdateSize()` into a single method; always applies patch on `Show()` regardless of cache state (separated save-once from apply-always)
- `ResetLevelName()`: always nulls cache fields at exit (removed early-return skip), ensuring clean state

## v1.1.2 — 2026.07.08

### Features

- **Alpha slider in color editor**: RGBA four-slider layout replaces the previous RGB-only UI; hex input already supported 8-char alpha values

### Bug Fixes

- **Jongyeol `song.time` log spam**: added `_lastMusicTimeSec` guard to `UpdateTime()`, preventing per-frame `AudioSource.time` access when `song.clip` is null (r142+ levels without standard audio clips); log no longer flooded with Unity warnings
- **v136 XPerfect per-player fallback**: when XPerfect doesn't provide `GetPlayerXPerfectCount` methods (v136), per-player getters now redirect to main static values instead of always returning 0
- **DetectApiVersion static property binding**: fixed `CreatePropertyGetter<T, F>` → `CreateStaticPropertyGetter<TField>` for `ADOBase.playerManager` (was throwing on r14x, causing version detection to fall back to v136 path and breaking judgement/XAccuracy display)

### Refactors

- **PatchManager thread safety**: added `lock (_lock)` to all public methods; `List<Type>` → `HashSet<Type>` for O(1) `Contains` lookups
- **Cached reflection utilities**: added 9 helper methods (`GetMethodInfo`, `GetFieldInfo`, `CreateFieldRef`, `CreatePropertyGetter/Setter`, `CreateStaticFieldGetter/Setter`, `CreateStaticPropertyGetter/Setter`) with dictionary caching; used by `VersionSafe` v136 path and `ShadowManager`
- **VersionSafe v136 bindings**: replaced bare `GetField`/`GetValue` reflection with PatchManager cached variants; missing fields now log a warning before falling back to defaults
- **Settings JSON migration**: replaced UMM default XML serialization with `Newtonsoft.Json`; existing `Settings.xml` auto-migrates to `Settings.json` on first load, then deletes the old XML file

## v1.1.1 — 2026.06.04

### Features

- **ComboLineReversed**: new toggle to swap the vertical order of the combo number and combo title label; animation anchor adapts to both orientations (3 Tr keys: EN/KO/CN)
- **ShowAutoInXPerfect**: when XPerfect mode is active, optionally show the auto-tile perfect count in orange (`#FF8000`) after the `−Perfect` value

### Refactors

- **Jongyeol State/Death/PurePerfect via `IOverlayTextManager`**: moved all three Jongyeol-mode helper methods (`UpdateDeath`, `UpdateState`, `CheckPurePerfect`, `GetTooJudgement`) out of `JongyeolModule` and into `IOverlayTextManager`; both `OverlayTextManagerNormal` and `OverlayTextManagerCoop` now implement them, enabling full coop-aware Jongyeol display
- **Coop Jongyeol Death/State**: `OverlayTextManagerCoop` now renders per-player death counts and state labels in each player's color, with `IsPurePerfect` checked independently per player
- **`_mono` field cache**: `OverlayMono` component reference stored as `_mono` field at construction; replaces repeated `GetComponent<OverlayMono>()` calls in `UpdateCombo`, `Show()`, and `Hide()`
- **Static `StringBuilder` pools**: `_judgementSb`, `_attemptSb`, `_bpmSb`, `_comboSb`, `_timingSb` declared as static fields; eliminates per-frame heap allocations across `BuildJudgementString`, `UpdateAttempts`, `BuildBpmText`, `UpdateCombo`, and `UpdateTimingScale`
- **`UpdateTimingScale` early-exit**: value cached in `_lastTimingScale`; text rebuild skipped when scale changes less than 0.001%
- **`OutExpoChange` lookup table**: combo animation easing replaced with a 31-entry pre-built float array (`_expTable`), removing `Math.Pow` calls per animation frame
- **`OverlayMono.Update` guard**: added `!Overlay.GameObject.activeSelf` check to skip update loop when overlay is hidden

## v1.1.0 — 2026.06.02

### Features
- Full Jongyeol color customization: 3 gradients (JCombo/JDeath/JTiming) + 11 static colors (8 states + FPS/Author/Start)
- Display order: reorder main stack elements with ▲▼ (General 7 items, Jongyeol 13 items)
- BPM line order: reorder TBPM/CBPM/KPS lines independently
- BPM line visibility: per-line ✓ toggle to show/hide each BPM line
- Attempt line order: swap Attempt/Full Attempt display order
- 14 new Tr keys for state colors + 23 keys for display order (EN/KO/CN)

### Refactors
- Settings folder namespace: JipperOverlayer.Overlayer.Settings → JipperOverlayer.Overlayer (fixes Settings type collision)
- DrawReorderList helper: unified ▲▼/✓ UI for all order lists, 3 callers share 1 implementation (-45 lines)
- BPM text building: extracted shared BuildBpmText() static method, used by both General and Jongyeol mode

### Bug Fixes
- ColorPerDictionary.GetColor cache: added noCache parameter for static color updates
- EnsureDefaults() migration: detect stale colors.json and reset transparent (a==0) colors
- FPS/Author/Start text: label white via <color=white>, value takes configured color
- BPM/Combo colors: added missing reset buttons in General section
- State colors: all 8 conditions fully translated with "Color"/"색상"/"颜色" suffix
- Empty reference guard: null-check in SetupLocationMain to prevent NRE from stale config
- Config migration: auto-filter invalid IDs from display order arrays on load
- BPM cache: DirtyBpmCache() forces text rebuild when visibility/order changes

## v1.0.8 — 2026.06.02

feat: integrate XPerfect counter into judgement display

Add optional integration with XPerfect mod:
- Detect XPerfect availability via UnityModManager and cache delegates
- Replace perfect counts in judgement line with +Perfect / X-Perfect / -Perfect
- Ensure correct execution order with HarmonyAfter
- Handle dynamic enable/disable of XPerfect via OnToggle event
- Add settings toggle "Show XPerfect in Judgement"

## v1.0.7 — 2026-05-31

- All custom positions changed to pixel offsets (position += offset), not affected by alignment
- New PosSlide2: XY on the same line, -2000~2000 range, integer pixel values
- Tr.cs: Added Coop and 11 position tags Key, trilingual translation
- Position grouping fold: Main/BPM, Judge(P1~P4), Others
- FPS refresh rate slider indented below ShowFPS, hidden when turned off
- DecimalPrecision remove extra {} blocks, indentation alignment
- Attempt added Coop independent offset field
- ApplyFontToAll remove redundant try-catch
- Configuration migration: ConfigVersion 0→2, old PX/PY converted to offset

- Change JudgementText/_judgementObject to [4] array
- SetupLocationJudgement: P1/P3 x=-250, P2/P4 x=250  First row y=35, Second row y=5 (same as single-player default height)
- UpdateJudgement: Read per-player marginTrackers in coop mode
- Settings: P1~P4 JudgePX/PY sliders, default values aligned with two-column layout
- Move Attempt text to x=550 in multiplayer mode to avoid overlap
- In Show(), set up SetupLocationJudgement first, then UpdateJudgement

- Title text updates in real time, pausing switching Jongyeol does not lose text, DecimalPrecision injection, code cleanup

## v1.0.6 — 2026-05-30

### Bug Fixes
- Fix version detection for game API changes: detect v141+ via scrMarginTracker and ADOBase.playerManager instead of removed scrController.playerManager
- Fix percentAcc/percentXAcc delegate bindings: use ADOBase.playerManager (static) instead of scrController.instance.playerManager (removed property)

## v1.0.5 — 2026-05-30

### Features
- Customizable text labels: all overlay labels can be customized via Custom Labels settings panel
- Label presets: English / Korean / Chinese one-click presets
- FPS refresh rate slider (0.05~1.0s) for Jongyeol mode
- Settings UI reorganized with collapsible panels (General/Display/Jongyeol/Alignment/Labels)

### Refactors
- Replaced JOverlay inheritance with JongyeolModule composition (-405 lines)
- Removed all unused virtual keywords (Overlay 13, OverlayTextManagerCoop 4, OverlayTextManagerNormal 3)
- Renamed YellowCombo → AllowELCombo for accurate naming (EL = Early/Late judgment)
- Settings.OnGUI split into 5 collapsible sections with sub-folders

### Bug Fixes
- PlanetMoveToNextFloorPatch: include Jongyeol settings in registration condition
- JCombo patches: require ShowCombo guard
- Show(): call SetupLocationMain when Jongyeol is active regardless of standard settings
- RefreshVisibility: actively refresh BPM/Combo/Judgement/TimingScale/ProgressBar when toggled on
- Fix UpdateDeath division-by-zero when currentSeqID == StartTile
- Fix GUI.changed false-positive triggering unnecessary overlay updates
- Fix time label cache not refreshing when edited in Custom Labels panel

## v1.0.4.2-preview — 2026-05-30

- Fix Chinese translations
- Fix value update issues when toggling settings

## v1.0.4.1-preview — 2026-05-30

Same as v1.0.4, preview release for testing.

## v1.0.4 — 2026-05-30

### Refactors
- Replaced JOverlay inheritance with JongyeolModule composition (-405 lines)
  - Deleted JOverlay.cs, JOverlayTextManagerNormal.cs, JOverlayTextManagerCoop.cs, IJOverlayTextManager.cs
  - Created JongyeolModule.cs as composable module
  - Overlay fields changed from protected to internal for JongyeolModule access
  - PurePerfectColor changed to public static readonly
- Removed all unused virtual keywords (Overlay 13, OverlayTextManagerCoop 4, OverlayTextManagerNormal 3)
- Removed redundant Jbpm.BpmColorMax wrapper; callers use Main.Settings.BpmColorMax directly
- Renamed YellowCombo → AllowELCombo for accurate naming (EL = Early/Late judgment)
- Removed redundant UpdateState() call in RdcSetAutoPatch

### Bug Fixes
- PlanetMoveToNextFloorPatch: include Jongyeol settings in registration condition so State/Death/Start/Timing update when all standard settings are off
- JCombo patches: require ShowCombo to prevent combo updating when disabled
- Show(): call SetupLocationMain when Jongyeol is active regardless of standard settings
- RefreshVisibility: actively refresh BPM/Combo/Judgement/TimingScale/ProgressBar when toggled on

## v1.0.2 — 2026-05-29

### Features
- Configurable text alignment: per-element 3x3 alignment grid (TL/T/TR/L/C/R/BL/B/BR)
- Font style toggles: Bold, Italic, Underline, Strikethrough, Highlight per element

### Fixes
- Custom fonts: shadow material cache keyed by font asset (not alpha), GetFontMaterial
  reflection for cross-Unity-version compatibility, font selection persisted by name
- Font list no longer polluted by other mods' file-loaded fonts (path-name filter)
- PlayCount.Save: null-data Hash keys no longer cause NRE
- Combo "Perfect" text animation restored after ContentSizeFitter removal
- RefreshPatches empty catches now log warnings
- PatchManager.ApplyAll skips already-applied patches (no double-patch)
  Fix Combo title-value spacing: restore ContentSizeFitter with Unconstrained width

- horizontalFit = Unconstrained (keep 300px width for alignment)
- verticalFit = PreferredSize (auto-height, proper title-to-value spacing)
- OverlayMono.ComboAnim reverted to sizeDelta.y (not preferredHeight)

  Remove planet speed from PlayCount Multiplier

  Multiplier no longer includes VersionSafe.GetPlanetSpeed (which changes
  mid-level with BPM events), only song.pitch (constant per level).
  Fixes attempt count key mismatch when speed changes during gameplay.

### Refactors
- RegisterChangeStatePatch: 30-line reflection search replaced with direct [HarmonyPatch]
- Game API method targets use nameof() where compile-accessible
- Tr.cs: removed obsolete Get(string) overload and _keyMap dictionary
- All settings labels unified through Tr.Get(Key) (no hardcoded strings)
- RegisterPatchesSafe removed (dead duplicate)

### Architecture & Performance
- OverlayMono MonoBehaviour: per-frame update moved out of UMM OnUpdate
- Combo animation: Stopwatch polling replaced with coroutine (zero idle cost)
- OverlayMono disabled when overlay hidden (no per-frame overhead in menus)
- Merged 3 MoveToNextFloor Harmony patches into 1 (fewer detours per tile)
- Tr.cs: flattened to array index instead of Dictionary lookup
- StringBuilder for BPM/Judgement/FPS text building
- ColorToHex: char array lookup instead of ToString(X2)
- Shadow materials cached by alpha
- ColorPerDictionary: cached GUIStyle, one-entry color cache
- Coop string arrays cached (no re-allocation per update)
- JOverlay timing list: running sum instead of O(n) per hit
- Time labels cached, only rebuilt on change

### Bug Fixes
- PlayCount.Save: write to .tmp first, then atomically replace (was truncating file on failure)
- Added null guard in Save() preventing empty file writes
- ColorChanged no longer calls redundant RefreshVisibility on every edit
- RepositionAutoText caches component reference (was FindObjectsOfTypeAll per frame)
- PlayCount data now persists correctly across sessions
