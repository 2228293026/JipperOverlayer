# Changelog

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
