# Product Backlog

Product Owner: researcher. Status: Done / Open / Closed (won't do or accepted).

| ID | Ticket | Acceptance | Status |
|---|---|---|---|
| PB-01 | Project hygiene | `.gitignore` excludes Unity temp, OS files, builds; Git LFS for binaries | Done |
| PB-02 | Room by editor step | Dialog sets room size (default 8 x 10 x 3.5 m); builds walls, floor, ceiling, light and the Meta shoebox with the same size | Done |
| PB-03 | Baked GI lighting | No directional light, soft shadowless light | Done |
| PB-04 | Stimuli | Cardboard 0.25 x 0.2 m at eye level; audio at head height; fixed radius (2 m, practice 1.2 m) | Done (image to be replaced) |
| PB-05 | Trial task | Triggers rotate audio at 10 deg/s, A confirms, forward-facing gate before each trial | Done |
| PB-06 | Practice stage | Language, instructions, unlimited practice, "ready" | Done |
| PB-07 | Experiment stage | Quiet, Reverb, Noisy x 6 trials, counterbalanced visual positions, rest and thank-you screens, no GUI/feedback in trials | Done |
| PB-08 | GUI via OVROverlayCanvas | Visible once in both eyes, hidden layer, own ray pointer, system keyboard for the ID | Done |
| PB-09 | Logging | CSV with timestamp + ID file name, per-row flush, console mirror, all configured fields | Done |
| PB-10 | Debug test bench | Condition switch, sphere always / after confirm, unlimited trials, back to menu | Done |
| PB-11 | Acoustic conditions | Quiet dry; Reverb via shoebox; Noisy with three spatial backgrounds | Done |
| PB-12 | Stimulus volume | Level acceptable | Closed (PO happy; PO to measure with a level meter later) |
| PB-13 | Luminance 500 cd/m2 | Reach the target brightness | Closed (hardware limit of Quest 3) |
| PB-14 | Reverb material | Choose a shoebox material for RT60 0.7 s (and 0.2 s); verify by measurement | **Open, postponed by PO** (findings and recommendation below) |
| PB-15 | RT60 measurement tool (proposed) | Debug bench action that records the rendered output and computes RT60 | **Open** (needs PO approval) |
| PB-16 | Standalone headset test | Build and run the APK on Quest 3 (only Quest Link tested so far); verify keyboard, CSV in MQDH | **Open** |
| PB-17 | Cardboard image | Replace the placeholder with the real image | **Open** (PO to provide) |
| PB-18 | "Temp" step | Keep it as the "Hot Fix" step (renamed) | Closed (kept as Hot Fix) |
| PB-19 | Docs and README | README, design, setup, data format, decisions, prompt log, backlog | Done |
| PB-20 | Analysis export | Aggregate CSVs per participant | Open (PO will do it later) |

## PB-14 findings and recommendation (2026-10-18, calculated, not measured)
- **Finding:** Meta's Concrete preset (mean reflection coefficient 0.98) would give an RT60 of about 4.7 s (reflection coefficient read as amplitude) or about 9.4 s (read as energy) in the 8 x 10 x 3.5 m room. This was calculated (Sabine and Eyring formulas on the preset coefficients), not measured in software; Meta does not say which reading applies, and its own late-reverb model may differ. The 6 x 6 room figure of "about 3 s" quoted earlier was a rough estimate and should be ignored.
- **Recommendation for about 0.7 s:** GypsumBoard (0.58-0.66 s on the amplitude reading) or Mud (0.72-0.80 s on the energy reading).
- **Recommendation for about 0.2 s:** AcousticTile (0.09-0.27 s, closest under both readings); alternatives Curtain or CarpetHeavyPadded.
- **Next step when picked up:** choose by measurement. PB-15 (Debug-bench RT60 measurement from the rendered audio output) would replace the guesswork. Full table and method: [AudioCalibration.md](AudioCalibration.md).
