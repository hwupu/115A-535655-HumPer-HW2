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

## Conditions (fixed order per participant: Quiet, Noisy, Reverb; protocol of 2026-10-18)
All conditions are rendered in the Meta XR Audio shoebox room (same size as the visual room, no clutter, one uniform reflection coefficient on all six surfaces calculated for the target RT60; the room is rendered by the mixer effect "Meta XR Audio Reflection"). **The RT60 values are calculated targets (Eyring, assuming a pressure coefficient), not measurements.**
1. **Quiet:** RT60 0.2 s, no background.
2. **Noisy:** RT60 0.7 s, plus three background recordings (`SFX-C-02..04`) playing together, looping, spatialized at eye level 2 m from the head: left (-90), right (+90), back (180). Unity gain. The backgrounds go through the room like the stimulus.
3. **Reverb:** RT60 2.2 s, the background keeps playing (it continues from the Noisy block, also over the rest screen: `ExperimentFlow.continueBackgroundOverRest`).

Practice uses the Quiet acoustics (RT60 0.2 s). The earlier protocol (Quiet dry, Reverb with the Concrete preset, Noisy dry; order Quiet, Reverb, Noisy) is replaced; the hypothesis text above has not been revisited.

## Trials
6 per condition. Audio start: +60, -60, +60, -60, +60, -60. Visual side: left, left, right, right, left, right. Each side's three positions appear exactly once per condition (seeded shuffle; seed logged in `experiment_begin`).

## Protocol
Menu (experimenter: ID, Start / Debug) -> language (Chinese (Taiwan) / English) -> practice instructions -> practice trials (as many as the participant likes; GUI hints shown; "Practice again" / "I'm ready" buttons) -> Quiet block -> rest screen -> Noisy block -> rest screen -> Reverb block -> thank-you screen.
- No GUI during real trials (only the fixation dot before each trial), no feedback or measurement of any kind. Progress is shown only on the rest screens.
- The room is 8.72 m wide x 15 m deep x 3.5 m high (default, same as previous studies of the lab; set in the room dialog), white walls and ceiling, light-gray floor, soft baked lighting without hard shadows.

## Debug test bench
**Debug** opens a panel instead of the experiment: choose the condition (Quiet / Noisy / Reverb, applied immediately with its reverb time and background), choose whether the orange audio sphere is always visible or only after confirming, run experiment-style or practice-style trials as often as you like, and go back to the menu. After each trial the sphere stays and the HUD shows the signed difference (audio - visual) until A is pressed. Trials are logged to a `_DEBUG` file.

## Levels
Clips are loudness-normalized (-23 LUFS, mono); software volume is 1 everywhere and the stimulus Meta gain boost is 0 dB; loudness is set with the headset volume. Logged levels are software levels (clip RMS dBFS + gain boost; background = power sum of the three clips).

## Interruption, resume, cancel (PB-21)
Real sessions save their progress after every finished trial (`progress_<ID>.txt`). After a break that closed the app, enter the same ID and press Start: the menu offers **Resume** (same CSV, same trial plan, continues with the next trial). **Y** on the left controller opens a cancel dialog (upper region); cancelling keeps the CSV and marks the session cancelled. Details: [ADR-0004](Decisions/ADR-0004-resume-and-cancel.md).

## Setup instructions before the practice (PB-24)
After the language choice and before the practice, three screens (each with Continue): recenter with a long press of the Meta button (the headset may have been set up standing and handed over), set the IPD dial to the value the experimenter gives, then the task instructions, then "a black dot will appear, look at it". The dot is the head gate: the trial starts once the head has faced forward (yaw +/-5 deg, pitch +/-10 deg) for 1 s.

## Stimulus pulse and fixed backgrounds (PB-26, PB-27)
- The audio stimulus (2 s white noise) is a pulse train: 2 s on, 1 s off, repeated until the participant confirms with A.
- The three backgrounds are placed once when the background starts (Noisy block) (left, right, behind; 2 m; eye level) and stay fixed in the room; they are not moved by a recenter. Turning the head must turn the sounds the other way.

## Debug bench: reverb time and playback tests (PB-28)
The Debug bench has buttons for reverb time 0.2 s and 0.7 s, Preset = Meta's material preset from Step 2 (Concrete: about 4.7 s calculated), and -10% / +10% for fine tuning, **Play stimulus** (the pulse train straight ahead at 2 m with the cardboard, in the current condition) and **Play background noise** (the three backgrounds in any condition, so the stimulus can be tried against them in the reverb room). The reverb time applies to the stimulus in the Reverb condition. Meta's shoebox only offers material presets, so a custom value is sent to the plugin by `ReverbTuner` as one uniform reflection coefficient on all six surfaces, calculated from the target RT60 and the room size (Eyring). **These RT60 values are calculated, not measured**, and assume Meta's coefficient is a pressure coefficient (alpha = 1 - R^2); if it is an energy coefficient, the real times are about twice as long. Leaving Debug returns to the preset.

**No reverb was audible at first (cause found).** Meta XR Audio renders the shoebox room (early reflections and late reverb) in the "Meta XR Audio Reflection" **audio mixer effect**; room modeling and late reverb are off by default and sources with Enable Acoustics stay dry without it. The project had no mixer. Step 4 now creates `Assets/Audio/HumPerMixer.mixer` (Master group with that effect, created through the editor's internal mixer API by reflection) and routes the stimulus and the three backgrounds to it; Validate Setup warns if a source is not routed. `ReverbTuner` also switches room modeling and late reverb on from code. Not yet confirmed by ear on the headset. In the Reverb condition the Debug-forced backgrounds get the room too; in Noisy they stay dry.
