# Product Backlog

Product Owner: researcher. Status: Done / Open / Closed (won't do or accepted).

| ID | Ticket | Acceptance | Status |
|---|---|---|---|
| PB-01 | Project hygiene | `.gitignore` excludes Unity temp, OS files, builds; Git LFS for binaries | Done |
| PB-02 | Room by editor step | Dialog sets room size (default 8.72 x 15 x 3.5 m since 2026-10-18; earlier 8 x 10 x 3.5); builds walls, floor, ceiling, light and the Meta shoebox with the same size | Done |
| PB-03 | Baked GI lighting | No directional light, soft shadowless light | Done |
| PB-04 | Stimuli | Cardboard 0.25 x 0.2 m at eye level; audio at head height; fixed radius (2 m, practice 1.2 m) | Done (image to be replaced) |
| PB-05 | Trial task | Triggers rotate audio at 10 deg/s, A confirms, forward-facing gate before each trial | Done |
| PB-06 | Practice stage | Language, instructions, unlimited practice, "ready" | Done |
| PB-07 | Experiment stage | Quiet, Noisy, Reverb x 6 trials, counterbalanced visual positions, rest and thank-you screens, no GUI/feedback in trials | Done |
| PB-08 | GUI via OVROverlayCanvas | Visible once in both eyes, hidden layer, own ray pointer, system keyboard for the ID | Done |
| PB-09 | Logging | CSV with timestamp + ID file name, per-row flush, console mirror, all configured fields | Done |
| PB-10 | Debug test bench | Condition switch, sphere always / after confirm, unlimited trials, back to menu | Done |
| PB-11 | Acoustic conditions | Quiet dry; Reverb via shoebox; Noisy with three spatial backgrounds | Done |
| PB-12 | Stimulus volume | Level acceptable | Closed (PO happy; PO to measure with a level meter later) |
| PB-13 | Luminance 500 cd/m2 | Reach the target brightness | Closed (hardware limit of Quest 3) |
| PB-14 | Reverb material | Choose a shoebox material for RT60 0.7 s (and 0.2 s); verify by measurement | Closed: superseded by RT60 set directly per condition (0.2 / 0.7 / 2.2 s, calculated), see PB-29; measuring the real RT60 (PB-15) is still recommended |
| PB-15 | RT60 measurement tool | Debug bench button and a measurement at the start of every condition block; measured T20 logged with every trial (`rt60_measured_s`); coefficient table calibrated by measurement | Closed |
| PB-16 | Standalone headset test | Build and run the APK on Quest 3 (only Quest Link tested so far); verify keyboard, CSV in MQDH | **Open** |
| PB-17 | Cardboard image | Replace the placeholder with the real image | **Open** (PO to provide) |
| PB-18 | "Temp" step | Keep it as the "Hot Fix" step (renamed) | Closed (kept as Hot Fix) |
| PB-19 | Docs and README | README, design, setup, data format, decisions, prompt log, backlog | Done |
| PB-20 | Analysis export | Aggregate CSVs per participant | **Closed** (`Tools/prepare_for_spss.sh`, guide `Docs/SpssAnalysis.md`) |
| PB-21 | Resume after interruption | The participant can take a break at any time; the headset may sleep or the app may be closed by accident. On restart the experiment resumes from the saved progress (participant, condition order, seed, completed trials). Pressing **Y** shows a dialog in the upper region asking whether to cancel this participant; cancel is **logged** (new CSV event), the CSV is never deleted. | Closed (done, see ADR-0004) |
| PB-22 | System keyboard missing in standalone build | Participant ID can be entered on the headset | Closed: replaced by an on-screen number pad (cause of the system keyboard failure not found) |
| PB-23 | CSV retrieval on macOS | MQDH cannot list the files; Windows Explorer can. Document a Mac path (adb pull) and test it | **Closed** (PO retrieved the CSV on a Mac) |
| PB-24 | Setup instructions before the practice | Participant instructions (EN / zh-TW): (1) recenter with a long press of the Meta button, (2) set the headset IPD dial to the value the experimenter gives, (3) look at the black dot; then the procedure continues | Closed |
| PB-25 | Button labels on the controllers | Practice hints as labels next to the trigger and A buttons; Meta's Controller Buttons Mapper block cannot do this (input mapper only) | Closed: tried, concept did not work, reverted (HUD hint stays) |
| PB-26 | Background sounds fixed in the room | The three backgrounds stay where they were placed when the Noisy block starts (do not follow the head, not moved by a recenter); positions re-applied every frame; CSV note shows listener-relative positions and spatializer state | Closed |
| PB-27 | Stimulus pulse train | The 2 s noise plays for 2 s, pauses 1 s, repeats until confirmed (`StimulusController.gapSec`) | Closed |
| PB-28 | Debug: reverb time and playback tests | Buttons for reverb 0.3 / 0.7 / 2.2 s (= Quiet / Noisy / Reverb), preset (Concrete), play stimulus, play / pause the background noise in any condition | Closed |
| PB-29 | Protocol change: acoustics per condition | Quiet RT60 0.3 s (PO decision after the measurement); Noisy RT60 0.7 s + background; Reverb RT60 2.2 s + background continues; order Quiet, Noisy, Reverb; practice = Quiet (ADR-0005) | Closed |
| PB-30 | Default room size | 8.72 x 3.5 x 15 m (width x height x depth, as in previous studies of the lab); rebuild the room with Step 2 | Closed |

## PB-14 findings and recommendation (2026-10-18, calculated, not measured)
- **Finding:** Meta's Concrete preset (mean reflection coefficient 0.98) would give an RT60 of about 4.7 s (reflection coefficient read as amplitude) or about 9.4 s (read as energy) in the 8 x 10 x 3.5 m room. This was calculated (Sabine and Eyring formulas on the preset coefficients), not measured in software; Meta does not say which reading applies, and its own late-reverb model may differ. The 6 x 6 room figure of "about 3 s" quoted earlier was a rough estimate and should be ignored.
- **Recommendation for about 0.7 s:** GypsumBoard (0.58-0.66 s on the amplitude reading) or Mud (0.72-0.80 s on the energy reading).
- **Recommendation for about 0.2 s:** AcousticTile (0.09-0.27 s, closest under both readings); alternatives Curtain or CarpetHeavyPadded.
- **Update:** the measurement tool (PB-15) was built and showed that these formulas do not match the plugin; see ExperimentDesign.md, "Reverberation time: measurement and calibration".
