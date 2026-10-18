# Agent Instructions: VR Perception Study on Meta Quest 3 (Unity)

Reusable prompt for an AI coding agent. Copy this file into a new project (`Docs/References/Instructions.md`), fill in the **Study Brief** (end of this file, or a separate `Study-Brief.md`), then tell the agent: "Follow Instructions.md and the Study Brief to set up this project." A filled example is in [Study-Brief-HW2.md](Study-Brief-HW2.md).

Written from the lessons of the first project (HW2, ventriloquist effect). Its first build was written in one go from a long prompt and came out buggy; the rules below prevent that.

---

## 1. Roles and process
- Follow AI-DLC and Scrum, adapted for a solo project. The user is the **Product Owner (PO)**; you switch between **Scrum Master** (tickets, board, decisions, retros) and **engineer**.
- Keep in `Docs/`: product backlog and sprint board (Markdown tables are enough), decision records (`Docs/Decisions/ADR-000x-*.md`), a prompt log, the experiment design, scene setup instructions, the data format, and a `README.md`. Update them as work progresses; the PO can close tickets.
- **Ask before coding** when something is unclear (use the discovery checklist in section 6). Do not guess an unspecified experimental parameter; propose a default and ask.
- **Small steps.** Do what the PO asks in the current request, verify it, hand over, wait. Do not build the whole application in one pass. Plan non-trivial work and get approval first.
- Report honestly: what you ran and verified, what you could only calculate, and what only a headset can show. Never present an estimate as a measurement.
- Commit only when asked; push only when asked. Never force-push without explicit approval (then use `--force-with-lease`). Never delete or overwrite the PO's work without looking at it first.

## 2. Engineering rules
1. **Verify before handoff.** The PO usually has Unity open on the project, and batch mode cannot open an open project. So copy `Assets`, `Packages`, `ProjectSettings` to a temp folder (first import is slow; reuse it) and run Unity there with `-batchmode -nographics`: compile, `-executeMethod` the setup steps, run EditMode tests. Fix every compile error and failing test before reporting. Check the saved scene afterwards (objects exist, references assigned, layers, positions).
2. **Pure logic goes in a Unity-free assembly** (asmdef with no engine references): trial planning, math, localization table, CSV log (inject the clock and writer). Unit-test it (EditMode). Keep the Unity layer thin.
3. **Scene setup by Editor scripts, never at runtime.** Provide a menu (default name **Magic**, e.g. **Magic > Step 1 ...**): first one **reset/build** command; once stable, split into **steps** (lighting, room, GUI, wiring) that are idempotent, edit only the open scene, and never touch Meta Building Blocks the PO added by hand. Keep a **Hot Fix** step for corrections after scene changes (regroup hierarchy, re-place objects). Never edit scene YAML while the editor is open.
4. **Helper tools to keep building:** a **Validate Setup** menu (lists unassigned references), a headless entry point that runs all steps, on-screen diagnostics for things that fail silently (see keyboard), and console logging with a common prefix.
5. **Check SDK source before using an API.** Meta packages are in `Library/PackageCache`; read the real class and field names, do not guess. When SDK limits matter (presets only, legacy Input), say so.
6. Keep a clean hierarchy with root groups (XR, Audio, Stimuli, Experiment, Environment, UI), identity transforms.
7. Git hygiene from day one: `.gitignore` for Unity temp folders, IDE files, OS files (`.DS_Store`, `Thumbs.db`), build outputs (`*.apk`, `*.aab`), keystores; commit `.meta` files; **Git LFS** for audio, images, lightmaps (`exr`), models, fonts. Set the author from the existing git config.
8. Tooling notes: prefer the file-writing tool to shell heredocs for code (quotes and `\n` get mangled); write C# strings with `\n` using an editor tool, not `sed`.

## 3. Quest 3 / Unity 6 / Meta XR pitfalls (all hit in practice)
- **Packages:** Meta XR Core SDK, Meta XR Audio SDK, OpenXR, Input System. Use **Building Blocks** (Camera Rig, Controller Tracking, Spatial Audio) for the rig, controller models and spatial sources.
- **GUI under single-pass stereo:** use `OVROverlayCanvas`. Its source canvas must sit on a **hidden layer** (e.g. 30) that the eye cameras do not render (also remove the overlay render layer, 31, from their culling masks), otherwise a second copy is drawn in the scene, in one eye only. Set the layer recursively after the hierarchy is built. Re-place the canvas after recenter.
- **Any in-scene visual** (laser line, markers) must use a shader that supports single-pass stereo: use URP Unlit/Lit from a **material asset** referenced by the component (so it is included in the build). `Sprites/Default` renders in the left eye only.
- **Pointer / UI input:** with "Input System only", Meta's `OVRInputModule` fails (it calls the legacy `Input`). Use your own ray-versus-rectangle hit test on `Selectable`s, `OVRInput` for triggers, and start the ray at the controller model's `laser_begin` (from the Controller Tracking block). Show the laser only while interactive UI exists. Hover via `Selectable.OnPointerEnter/Exit`, click via `Button.onClick`.
- **Text input (participant ID):** do **not** depend on the Quest system keyboard. Unity's `TouchScreenKeyboard` with "Requires System Keyboard" (manifest feature `oculus.software.overlay_keyboard`) worked in one build and later returned a "visible" keyboard that the headset never showed (no input-focus change), with manifest, focus awareness and target SDK all correct. Use an **on-screen number pad / keypad** on the GUI, driven by the same ray pointer; IDs are short. It also works over Quest Link and in the Editor.
- **Recenter:** the participant or experimenter may long-press the Meta button at any time. Subscribe to `OVRManager.display.RecenteredPose` (wait for `OVRManager.display` to exist), wait two frames, then re-place the GUI, the stimuli (same azimuths) and any placed audio sources around the new head position and yaw. Log it.
- **Tracking:** floor-level origin. Head tracking reports (0,0,0) for the first frames: wait for a valid head height before placing UI. Recenter at session start and place UI after. Before every trial require the head to face forward (yaw and pitch tolerance, hold time) with a small fixation cue; capture the trial origin then.
- **Conventions:** azimuth 0 = forward, negative = left, positive = right; position = origin + yaw * (r sin az, 0, r cos az). Put visual and audio stimuli at the **same height** (eye level) unless the brief says otherwise; mismatched heights feel wrong.
- **Audio:**
  - Provide normalized clips (state the loudness target, e.g. -23 LUFS, mono); no software gain (volume 1, Meta gain boost 0); loudness comes from the headset volume; flat rolloff when the radius is fixed. Log software levels (clip RMS dBFS + gain; background as power sum), and tell the PO that sound pressure must be measured at the ear.
  - Reverb with the Meta shoebox: one room component (size equal to the visual room, clutter 0), per-source `EnableAcoustics` to switch it by condition; backgrounds dry. The shoebox offers **material presets only**, so an exact RT60 cannot be typed in: calculate candidates (Sabine/Eyring on the preset coefficients, state the assumption), flag that they are calculated, and propose measuring from the rendered output.
  - A requested SNR with a requested absolute level may conflict; state the arithmetic and ask how it will be measured.
- **Room and lighting:** baked global illumination (flat ambient + one baked area light + probes; no directional light, no hard shadows). Absolute luminance (cd/m2) is not controllable in Unity and Quest 3 peaks near 100 nits: say so up front. One room-size dialog feeds both the visual room and the shoebox.
- **Localization:** string table in the pure-logic assembly; Traditional Chinese needs a CJK font asset (e.g. Noto Sans TC) in the project; the experimenter menu can stay English.
- **Logging:** CSV, one row per event, **flush every row**, console mirror, file name `yyyyMMdd_HHmmss[_ParticipantID][_DEBUG].csv`, in `Application.persistentDataPath` (Quest: `/sdcard/Android/data/<package>/files/`, pulled with MQDH; Editor on Windows: `%USERPROFILE%\AppData\LocalLow\<company>\<product>\`). Log session start, language, stage and condition begin/end, per-trial configuration and result, durations, levels, seed, app pause.

## 4. Experiment-flow guidance
- One scene, a coroutine state machine; scene references in one holder component. Stages: experimenter menu (ID, **Start**, **Debug**) -> language -> practice instructions -> repeatable practice -> ready -> conditions (fixed or counterbalanced order, per the brief) with rest screens -> thank-you.
- **Participant phases:** GUI allowed in practice; **no GUI, no feedback of any kind during real trials** (only the fixation cue); progress and completion only on rest/thank-you screens.
- **Debug mode is a test bench, not a run with feedback**: switch conditions on the fly, choose when the stimulus marker (sphere) is visible (always / after confirm), run unlimited trials, show the signed error after each trial (hidden again at the next trial), go back to the menu. Log with a `_DEBUG` file name.
- **Counterbalancing:** seeded shuffle; log the seed; avoid forbidden values (e.g. 0 degrees); unit-test the planner.
- Trial task parameters (rotation speed, confirm button, limits, gate tolerances) are inspector fields with documented defaults.
- Show the GUI so it never overlaps the stimuli (menu panels hidden during trials; hint strip above the stimuli).

## 5. Recommended build order (each step verified headless, then a short headset test by the PO)
0. Repo: `.gitignore`, LFS, initial commit.
1. Rig via Building Blocks; lighting step.
2. Room step (dialog; visual room + shoebox).
3. GUI shell: overlay canvas, pointer, number pad, menu screen. **PO headset test** (visibility, height, ray, number pad, recenter).
4. Stimuli and the trial task, plus the Debug test bench. **PO headset test.**
5. Practice stage (language, instructions, gate), logging, unit tests.
6. Conditions and the experiment stage.
7. Docs (README, design, setup, data format, ADRs, prompt log, backlog), Validate Setup.
8. Standalone Android build test on the headset (CSV retrieval, performance); calibration measurements (levels, RT60).

## 6. Discovery checklist: ask the PO before coding
Research question and hypothesis; independent variables (conditions) and dependent measure; task type (adjustment, forced choice, ...) and confirm button; stimuli (type, size, height, distance, positions, forbidden values, assets to be provided); conditions (order fixed or counterbalanced across participants), trials per condition, practice design (different geometry from trials?); room (size, materials, target RT60, lighting, distractors); audio (clips, loudness target, headset-volume protocol, background design: spatial or not, SNR definition and who measures it); GUI policy per phase; interface languages and fonts; participant ID entry and how data is retrieved; log fields; experimenter tools (Debug meaning: test bench vs run with feedback); platforms (Windows/Mac, Link vs standalone, package id); what the PO can measure (level meter, luminance meter); anything that can only be verified on the headset.

## 7. Flag these early (known conflicts)
- Absolute luminance is not reachable or controllable.
- A named material (e.g. concrete) and a target RT60 conflict in most rooms.
- An absolute level plus an SNR requires measurement at the ear; software levels are only relative.
- Anything the brief says that depends on the headset volume, the room size or the SDK version.

---

## Study Brief (fill in; replace `<...>`)
| Item | Value |
|---|---|
| Study / course, package id (`com.<org>.<name>`) | <...> |
| Research question, hypothesis | <...> |
| Task (what the participant does, controls, confirm button, speed) | <...> |
| Visual stimulus (type, size, height, distance, positions, forbidden values) | <...> |
| Audio stimulus (clip, loudness target, height, distance, start positions) | <...> |
| Conditions and order | <...> |
| Trials per condition, trial sequence (audio, visual), counterbalancing | <...> |
| Practice (geometry, repetition, ready button) | <...> |
| Room (size default, materials, RT60 target, lighting, color scheme) | <...> |
| Background audio (clips, spatial or not, positions, distance, level / SNR target) | <...> |
| GUI policy (menu, practice, trials, rest, thank-you), languages, fonts | <...> |
| Participant ID, data retrieval, log fields | <...> |
| Debug / experimenter tools | <...> |
| Platforms and tools (Unity version, Windows/Mac, Link/standalone, MQDH) | <...> |
| Assets the PO will provide | <...> |
| Measurements the PO will do (level meter, luminance, RT60) | <...> |
