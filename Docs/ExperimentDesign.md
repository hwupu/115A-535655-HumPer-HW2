# Experiment Design

**RQ1** How do acoustic conditions affect the audio-visual disparity threshold of the ventriloquist effect in VR?
**H1** Threshold(reverb) > Threshold(quiet); quiet <= Threshold(noisy room) < reverb.

## Stimuli
| | Visual | Audio |
|---|---|---|
| Type | Square cardboard, 0.25 m wide x 0.2 m high (placeholder; image to be replaced), facing the participant | White noise (`SFX-C-01`, -23 LUFS, looped) |
| Height | Center at eye (head) height | Head height |
| Distance | 2 m (practice 1.2 m) | 2 m (practice 1.2 m); radius locked while it rotates |
| Azimuth | Experiment: one of -37.5, -22.5, -7.5, 7.5, 22.5, 37.5 deg (never 0). Practice: 0 deg | Starts at +60 / -60 deg (practice +30 deg); the participant rotates it |

Azimuth: 0 = straight ahead, negative = left, positive = right. The origin is the head position captured at the start of each trial; "forward" is the rig forward after a recenter at the start of the session.

## Task (method of adjustment)
Left trigger held: the audio moves left; right trigger: right (10 deg/s each; both = no motion); A (right controller) confirms and ends the trial. Azimuth is clamped to +/-90 deg. Before every trial the participant must face forward (yaw +/-5 deg, pitch +/-10 deg, held 1 s) while a small fixation dot is shown.

## Conditions (fixed order per participant: Quiet, Reverb, Noisy)
1. **Quiet:** dry (no room reflections), no background.
2. **Reverb:** Meta XR Audio shoebox room (same size as the visual room, no clutter, one material on all six surfaces); the stimulus is sent into the room (`EnableAcoustics`). Material choice for the target RT60: see [AudioCalibration.md](AudioCalibration.md).
3. **Noisy:** dry, plus three background recordings (`SFX-C-02..04`) playing together, looping, spatialized at eye level 2 m from the head: left (-90), right (+90), back (180). Unity gain.

Practice uses the Quiet acoustics.

## Trials
6 per condition. Audio start: +60, -60, +60, -60, +60, -60. Visual side: left, left, right, right, left, right. Each side's three positions appear exactly once per condition (seeded shuffle; seed logged in `experiment_begin`).

## Protocol
Menu (experimenter: ID, Start / Debug) -> language (Chinese (Taiwan) / English) -> practice instructions -> practice trials (as many as the participant likes; GUI hints shown; "Practice again" / "I'm ready" buttons) -> Quiet block -> rest screen -> Reverb block -> rest screen -> Noisy block -> thank-you screen.
- No GUI during real trials (only the fixation dot before each trial), no feedback or measurement of any kind. Progress is shown only on the rest screens.
- The room is 8 m wide x 10 m deep x 3.5 m high (default; set in the room dialog), white walls and ceiling, light-gray floor, soft baked lighting without hard shadows.

## Debug test bench
**Debug** opens a panel instead of the experiment: choose the condition (Quiet / Reverb / Noisy, applied immediately), choose whether the orange audio sphere is always visible or only after confirming, run experiment-style or practice-style trials as often as you like, and go back to the menu. After each trial the sphere stays and the HUD shows the signed difference (audio - visual) until A is pressed. Trials are logged to a `_DEBUG` file.

## Levels
Clips are loudness-normalized (-23 LUFS, mono); software volume is 1 everywhere and the stimulus Meta gain boost is 0 dB; loudness is set with the headset volume. Logged levels are software levels (clip RMS dBFS + gain boost; background = power sum of the three clips).

## Interruption, resume, cancel (PB-21)
Real sessions save their progress after every finished trial (`progress_<ID>.txt`). After a break that closed the app, enter the same ID and press Start: the menu offers **Resume** (same CSV, same trial plan, continues with the next trial). **Y** on the left controller opens a cancel dialog (upper region); cancelling keeps the CSV and marks the session cancelled. Details: [ADR-0004](Decisions/ADR-0004-resume-and-cancel.md).

## Setup instructions before the practice (PB-24)
After the language choice and before the practice, three screens (each with Continue): recenter with a long press of the Meta button (the headset may have been set up standing and handed over), set the IPD dial to the value the experimenter gives, then the task instructions, then "a black dot will appear, look at it". The dot is the head gate: the trial starts once the head has faced forward (yaw +/-5 deg, pitch +/-10 deg) for 1 s.
