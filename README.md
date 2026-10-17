# HumPer HW2 — acoustic conditions and the ventriloquist-effect disparity threshold in VR

Unity project for a VR perception experiment on **Meta Quest 3** (standalone Android build; Quest Link for development).

- **RQ1:** How do acoustic conditions affect the audio-visual disparity threshold of the ventriloquist effect in VR?
- **H1:** The disparity threshold will be larger in the reverb condition than in the quiet condition, and the speech-background condition will fall at or above quiet but below reverb.

A seated participant rotates a white-noise sound source around them with the triggers until it seems to come from a small square cardboard (eye level, 2 m). The final placement is logged per trial. Design details: [Docs/ExperimentDesign.md](Docs/ExperimentDesign.md).

## Requirements
- Unity **6000.6.5f1** (URP, OpenXR) with Android Build Support
- Meta XR Core SDK 207.0.1 and Meta XR Audio SDK 85.0.0 (in `Packages/manifest.json`); Input System
- Quest 3, Meta Quest Developer Hub (MQDH); Windows (Quest Link + builds) or macOS (builds)
- Git LFS (audio, images, fonts, models are stored in LFS): run `git lfs install` once

## Quick start
1. Clone, `git lfs pull`, open the project in Unity.
2. Open `Assets/Scenes/ExperimentScene.unity`. It already contains the Meta Building Blocks (Camera Rig, Controller Tracking, 4 x Spatial Audio) and the generated setup. To rebuild the setup, see [Docs/SceneSetup.md](Docs/SceneSetup.md) (**HumPer** menu, Steps 1-4).
3. Play with Quest Link, or build for Android (File > Build Profiles > Meta Quest) and install on the headset.
4. In the headset: enter the participant ID, then **Start Experiment** (hand over to the participant) or **Debug** (test bench: all functions, sphere and feedback).

## Data
One CSV per session, flushed row by row. File name: `yyyyMMdd_HHmmss[_ParticipantID][_DEBUG].csv`.
- Quest: `/sdcard/Android/data/com.nycu.humperhw2/files/` (pull with MQDH).
- Editor / Quest Link (Windows): `%USERPROFILE%\AppData\LocalLow\NYCU\115A-535655-HumPer-HW2\`.

Schema: [Docs/DataFormat.md](Docs/DataFormat.md). Every row is also printed to the Unity console (`[HumPer] ...`).

## Process (AI-DLC + Scrum, solo)
Product Owner: the researcher. The AI agent alternates Scrum Master / engineer. Work is done in small, verified increments (setup steps, headless checks, then headset tests).
- [Docs/ProductBacklog.md](Docs/ProductBacklog.md) and [Docs/SprintBoard.md](Docs/SprintBoard.md): tickets and status
- [Docs/Decisions/](Docs/Decisions): decision records
- [Docs/PromptLog.md](Docs/PromptLog.md): prompt log
- [Docs/AudioCalibration.md](Docs/AudioCalibration.md): levels and the reverb material question

## Layout
```
Assets/Scripts/Core      pure C# (trial planner, math, localization, CSV log), unit-tested, no Unity dependency
Assets/Scripts/Runtime   experiment flow, UI pointer, stimuli, acoustic conditions
Assets/Editor            HumPer setup steps (menu) and room dialog
Assets/Tests/EditMode    unit tests (11)
Assets/Audio             normalized clips (-23 LUFS): SFX-C-01 stimulus (white noise), SFX-C-02..04 backgrounds
Docs/                    process docs, design, setup, references
```

## Platform notes
`.gitignore` excludes Unity temp folders, IDE and OS files, build outputs (`*.apk`, `*.aab`) and keystores; `.meta` files are committed. Large binary types are tracked with Git LFS (`.gitattributes`).
