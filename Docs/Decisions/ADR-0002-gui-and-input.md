# ADR-0002: GUI, pointer, keyboard (2026-10-18)

Status: accepted.

- All GUI uses `OVROverlayCanvas` (single-pass stereo). The source canvases sit on a hidden layer (30, `HiddenOverlayCanvas`) that the eye cameras do not render (otherwise the canvas is also drawn in the scene, in one eye only); camera culling masks drop layers 30 and 31.
- `OVRInputModule` uses the legacy Input API, which is disabled in this project (Input System only). `UiPointer` therefore does its own ray / rectangle hit test against active `Selectable`s; the ray starts at the active controller model's `laser_begin`. The laser line uses a URP Unlit material (`Sprites/Default` renders in one eye only under single-pass stereo).
- Participant ID: an on-screen number pad (0-9, backspace, clear) operated with the same ray. The Quest system keyboard (Unity `TouchScreenKeyboard`, "Requires System Keyboard") was tried first and worked once, then stopped opening in standalone builds: `TouchScreenKeyboard.Open` returned a keyboard in state Visible, but the headset never took input focus (`OVRManager.InputFocusLost` 0x). The cause was not found (manifest, focus awareness and target SDK were correct), so the dependency was dropped (PB-22).
- Recenter (long press of the Meta button): `OVRDisplay.RecenteredPose` re-places the GUI canvases, the stimuli (same azimuths) and the background sources around the new head position and yaw; logged as event `recenter`.
- GUI placement: in front of the rig at head height, once head tracking is valid (the first frames report 0,0,0), re-placed after the recenter at session start. During real trials no GUI is shown except the fixation dot of the head gate.
- Text uses uGUI `Text`; Chinese needs a CJK font (Noto Sans TC in `Assets/Fonts`).
