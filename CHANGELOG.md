# Changelog

## v1.0.1 — 2026-05-29

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
