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
All conditions are rendered in the Meta XR Audio shoebox room (same size as the visual room, no clutter, one uniform reflection coefficient on all six surfaces; the room is rendered by the mixer effect "Meta XR Audio Reflection"). The coefficient for each target RT60 comes from a **measured table** (`Rt60Calibration`), and the RT60 that is actually rendered is **measured at the start of every condition block and logged with every trial** (see below). Targets below about 0.3 s cannot be reached: Quiet is therefore set to 0.3 s (decided by the PO), the lowest table entry.
1. **Quiet:** RT60 0.3 s (the first target was 0.2 s; 0.3 s is the lowest value that can be rendered and measured), no background.
2. **Noisy:** RT60 0.7 s, plus three background recordings (`SFX-C-02..04`) playing together, looping, spatialized at eye level 2 m from the head: left (-90), right (+90), back (180). Unity gain. The backgrounds go through the room like the stimulus.
3. **Reverb:** RT60 2.2 s, the background keeps playing (it continues from the Noisy block, also over the rest screen: `ExperimentFlow.continueBackgroundOverRest`).

Practice uses the Quiet acoustics (RT60 0.3 s). The earlier protocol (Quiet dry, Reverb with the Concrete preset, Noisy dry; order Quiet, Reverb, Noisy) is replaced; the hypothesis text above has not been revisited.

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

## Onboarding before the practice (PB-24, PB-31)
After the language choice and before the practice, six slides (pictures, English and Chinese sets in `Assets/Images`, 2000 x 1400 with an empty 200 px strip at the bottom for the buttons, 50 px padding): 1 Before we start (sit comfortably, look forward, press and hold the Meta button to recenter), 2 Set IPD, 3 Procedure, 4 Controllers, 5 Practice rounds, 6 Ready? (the black dot, then Continue). The buttons name the slide they lead to: slide 1 "Next", slide 2 "Previous" / "Procedure", slide 3 "Set IPD" / "Controllers", slide 4 "Procedure" / "Trials", slide 5 "Controllers" / "Next", slide 6 "Trials" / "Begin Practice" (Begin Practice starts the practice; the first trial starts with the fixation dot, the head gate). The left button of slide 1 is "Language" (語言): it reopens the language choice and returns to slide 1 in the chosen language (the choice is logged again). The buttons are 260 x 80 canvas units with 50 units of padding from the bottom and the sides. The participant can go back and forth freely; every slide shown is logged as `setup_step` (`onboarding_slide_N`). The text of the slides is part of the pictures (typos included: change them in the image files).

## Stimulus pulse and fixed backgrounds (PB-26, PB-27)
- The audio stimulus (2 s white noise) is a pulse train: 2 s on, 1 s off, repeated until the participant confirms with A.
- The three backgrounds are placed once when the background starts (Noisy block) (left, right, behind; 2 m; eye level) and stay fixed in the room; they are not moved by a recenter. Turning the head must turn the sounds the other way.

## Debug bench: reverb time and playback tests (PB-28)
The Debug bench has no condition buttons any more: reverb time and condition are one-to-one, so the buttons are **0.3 s Quiet**, **0.7 s Noisy** (starts the backgrounds) and **2.2 s Reverb** (backgrounds keep playing). **Preset** is Meta's material preset from Step 2 (Concrete: about 4.4 s, calibrated estimate). Also: **Play stimulus** (the pulse train straight ahead at 2 m with the cardboard, in the current condition), **Play background noise** (starts the backgrounds in any condition; in Noisy and Reverb, where they start playing, it pauses and resumes them where they stopped; a condition button resets it) and **Measure RT60**. Leaving Debug returns to the condition defaults.

**No reverb was audible at first (cause found).** Meta XR Audio renders the shoebox room (early reflections and late reverb) in the "Meta XR Audio Reflection" **audio mixer effect**; room modeling and late reverb are off by default and sources with Enable Acoustics stay dry without it. The project had no mixer. Step 4 now creates `Assets/Audio/HumPerMixer.mixer` (Master group with that effect, created through the editor's internal mixer API by reflection) and routes the stimulus and the three backgrounds to it; Validate Setup warns if a source is not routed. `ReverbTuner` also switches room modeling and late reverb on from code. Not yet confirmed by ear on the headset. In the Reverb condition the Debug-forced backgrounds get the room too; in Noisy they stay dry.

## Reverberation time: measurement and calibration (PB-15)
- **Method:** the stimulus source plays a single-sample impulse (after silence, with the backgrounds paused); the final audio output is recorded at the AudioListener (`Rt60Meter`, `OnAudioFilterRead`, so it includes the Meta spatializer and the mixer's reflection effect, both ears summed); the decay is evaluated by Schroeder backward integration (`Rt60Estimator`): T20 (-5 to -25 dB) and T30 (-5 to -35 dB), extrapolated to 60 dB. It takes about 5 s.
- **Per trial (reference only, off by default since 2026-10-18; the PO measures manually in the Debug bench):** when `measureRt60AtBlockStart` is ticked, at the start of each condition block the measurement runs once (`ExperimentFlow.measureRt60AtBlockStart`) and the T20 value is written to the column `rt60_measured_s` of every trial row of that block and to the `rt60_measured` event (target, T20, T30, depth of the decay). It is not measured during a trial: the stimulus and the backgrounds would mask the decay, and a click every trial would disturb the participant. Practice trials and Debug trials leave the column empty.
- **Calibration (Windows plugin, 8.72 x 3.5 x 15 m, source 2 m ahead):** Eyring's formula did not match the plugin (target 0.7 s measured 1.05 s, 2.2 s measured 2.7 s, 0.2 s measured 0.5 s). A sweep of the coefficient gave the table in `Rt60Calibration`; with it the measured T30 is 0.70 s for target 0.7, 2.18 s for 2.2 and 0.30 s for the lowest entry. Below about 0.3 s the decay is not a clean exponential (T20 and T30 disagree), so 0.2 s is not reachable or meaningful with this method.
- **To check on the headset:** Debug bench, **Measure RT60** in each condition. The Android plugin may differ slightly from the Windows one that was calibrated.
