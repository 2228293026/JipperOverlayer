# JipperOverlayer
![C#](https://img.shields.io/badge/Lang-C%23-c9c8e4?logo=csharp)
![Visual Studio 2022](https://img.shields.io/badge/IDE-Visual%20Studio%202022-5C2D91?logo=visualstudio&logoColor=white)
[![Downloads](https://img.shields.io/github/downloads/2228293026/JipperOverlayer/total)](https://github.com/2228293026/JipperOverlayer/releases/latest)
[![Build](https://github.com/2228293026/JipperOverlayer/actions/workflows/build.yml/badge.svg)](https://github.com/2228293026/JipperOverlayer/actions/workflows/build.yml)

A Unity Mod Manager mod for **A Dance of Fire and Ice (ADOFAI)** that provides an in-game overlay with progress, accuracy, BPM, combo, judgement, and more.

## Features

- **Real-time Overlay** — Progress, Accuracy, XAccuracy, Music/Map Time, Checkpoints, Best Record
- **BPM Display** — Tile BPM, Current BPM, KPS, with pseudo-BPM detection
- **Combo Counter** — Animated combo display with color gradients
- **Judgement Display** — Hit margin breakdown (Miss, Bad, Good, Perfect, etc.)
- **Timing Scale** — Current timing scale percentage
- **Attempt Tracker** — Per-map attempt count with persistent storage
- **Progress Bar** — Visual progress indicator
- **Jongyeol Mode** — Extended overlay with FPS, State, Death count, Start position, Timing analysis, Debug text hiding
- **Co-op Support** — Per-player display for multiplayer
- **Color Editor** — Interactive gradient editor for all overlay colors

## Settings UI Languages

- English
- 한국어 (Korean)
- 中文 (Chinese)

## Installation

1. Install [Unity Mod Manager (UMM)](https://www.nexusmods.com/site/mods/21)
2. Download the latest release from [Releases](https://github.com/2228293026/JipperOverlayer/releases)
3. Install the mod via UMM, or extract the zip to `ADOFAI/Mods/JipperOverlayer/`

### Manual Installation

```
ADOFAI/Mods/JipperOverlayer/
├── Info.json
├── JipperOverlayer.dll
├── jipperoverlayerbundle
├── jipperoverlayerbundle2022
├── jipperoverlayerbundle6000
├── Linux/
└── Mac/
```

## Requirements

- A Dance of Fire and Ice (Steam version)
- Unity Mod Manager 0.22.14+
- Supports game versions v136 and v141+

## Build from Source

### Prerequisites

- Visual Studio 2022+ with .NET Framework 4.8.1 SDK
- Steam installation of ADOFAI (for reference DLLs in `Libs/`)

### Build

```bash
msbuild JipperOverlayer/JipperOverlayer.csproj -restore -p:Configuration=Release
```

The compiled DLL will be at `JipperOverlayer/bin/Release/JipperOverlayer.dll`.

## CI/CD

This project uses GitHub Actions for automated builds:

| Trigger | Action |
|---------|--------|
| Push to `master` | Build + package artifact |
| Pull request to `master` | Build verification |
| Tag `v*.*.*` | Build + package + GitHub Release |
| Tag containing `-` (e.g. `v1.0.0-pre1`) | Prerelease |

## License

- Primarily **MIT License** — see [LICENSE](./LICENSE.txt).

- Code adapted from [JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack) by Jongyeol is under **BSD 3-Clause** — see [LICENSE-BSD](./LICENSE-BSD).

