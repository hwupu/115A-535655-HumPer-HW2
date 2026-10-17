# ADR-0002: GUI, pointer, keyboard (2026-10-18)

Status: accepted.

- All GUI uses `OVROverlayCanvas` (single-pass stereo). The source canvases sit on a hidden layer (30, `HiddenOverlayCanvas`) that the eye cameras do not render (otherwise the canvas is also drawn in the scene, in one eye only); camera culling masks drop layers 30 and 31.
- `OVRInputModule` uses the legacy Input API, which is disabled in this project (Input System only). `UiPointer` therefore does its own ray / rectangle hit test against active `Selectable`s; the ray starts at the active controller model's `laser_begin`. The laser line uses a URP Unlit material (`Sprites/Default` renders in one eye only under single-pass stereo).
- Participant ID: Quest system keyboard through Unity's `TouchScreenKeyboard` ("Requires System Keyboard" in the Oculus project config; the manifest feature is added at build). Over Quest Link / Editor there is no system keyboard: PC-keyboard fallback with an on-screen hint.
- GUI placement: in front of the rig at head height, once head tracking is valid (the first frames report 0,0,0), re-placed after the recenter at session start. During real trials no GUI is shown except the fixation dot of the head gate.
- Text uses uGUI `Text`; Chinese needs a CJK font (Noto Sans TC in `Assets/Fonts`).
