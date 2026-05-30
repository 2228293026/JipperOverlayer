# Changelog

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
