# Scene Setup

The scene is built by editor menu steps (**HumPer** menu), not at runtime. Every step is idempotent, edits only the open scene, and never touches the Meta Building Blocks. Save the scene after each step.

## Prerequisites (done by hand once, in `Assets/Scenes/ExperimentScene.unity`)
Add these Meta Building Blocks (Meta > Tools > Building Blocks): **Camera Rig**, **Controller Tracking** (left and right), and **Spatial Audio** x 4 named `Stimuli`, `Background Left`, `Background Right`, `Background Back`. Put a Noto Sans TC font (`.ttf` / `.otf`) in `Assets/Fonts` for Chinese text (already included).

## Steps
| Menu | What it does |
|---|---|
| **Step 1 - Lighting: Global Illumination** | Removes directional lights; flat ambient light and baked GI settings (`Assets/Settings/ExperimentLighting.lighting`). |
| **Step 2 - Room Setup...** | Dialog: width / depth / height (default 8 x 10 x 3.5 m), shoebox material for all six surfaces, bake option. Builds `Environment/Room` (floor, ceiling, four walls, soft baked ceiling light, light probes) and `Environment/Shoebox Acoustics` (Meta XR Audio room, same size, clutter 0). The size is stored on a `RoomInfo` component. |
| **Hot Fix - Organize Hierarchy, Place Audio, Add Visual Cardboard** | Groups objects into `XR`, `Audio`, `Stimuli`, `Experiment`, `Environment`, `UI`; places the four Spatial Audio blocks at eye level, 2 m from the origin (stimulus +60, backgrounds -90 / +90 / 180); turns off Play On Awake; creates the hidden placeholder cardboard (0.25 x 0.2 m, eye level). Re-run it after changing the scene by hand. |
| **Step 3 - GUI** | Builds the `GUI` overlay canvas (menu, language, practice instructions, fixation gate, practice result, rest screen, thank-you, Debug test bench), the `HUD` strip and the `UI Pointer`. Sets "Requires System Keyboard". Re-wires Step 4 automatically if it exists. |
| **Step 4 - Practice: stimulus controller + experiment flow** | Creates the `Debug Audio Sphere`, `Stimulus Controller` (clip `SFX-C-01`), `Acoustic Conditions` (backgrounds `SFX-C-02..04`) and `Experiment Flow`, and assigns all references. |
| **Validate Setup** | Lists unassigned references on HumPer components. |

Order for a fresh scene: Step 1, Step 2, Hot Fix, Step 3, Step 4, Validate. After changing code that adds fields, re-run Step 4 (it recreates the flow with the code defaults).

## Run
- **Quest Link:** open the scene, Play. The ID field falls back to the PC keyboard (type in the Game view, Enter to finish) because the system keyboard does not exist over Link.
- **Standalone:** File > Build Profiles > Meta Quest > Build and Run (Windows or macOS). The ID field opens the Quest system keyboard (needs a build made after Step 3). **Not yet tested on the headset** (only via Quest Link so far).
- Retrieve CSV files with MQDH (Device Manager > Files > `/sdcard/Android/data/com.nycu.humperhw2/files/`).

## Smoke test (Debug mode, then a real run)
Debug: run a trial in each condition, toggle the sphere, go back to the menu. Real run: language, practice, three blocks of six trials; check the CSV has `session_start`, `language`, `practice_begin`, `practice_end`, `experiment_begin`, three `condition_begin` / `condition_end`, 18 `trial` rows (conditions not `practice`) and `experiment_end`.
