# Study Brief: HW2 (filled example for Instructions.md)

Current values after the iterations of this project. The original, longer instruction text is in git history (commit `dacb1b0`, `Docs/References/Instructions.md`).

| Item | Value |
|---|---|
| Study / course, package id | NYCU Human Perception course, HW2; `com.nycu.ventriloquist_hw2`; Unity 6000.6.5f1 |
| Research question, hypothesis | RQ1: How do acoustic conditions affect the audio-visual disparity threshold of the ventriloquist effect in VR? H1: the threshold is larger in the reverb condition than in the quiet condition; the speech-background condition falls at or above quiet but below reverb. |
| Task | Seated participant rotates the audio stimulus on a fixed-radius circle with the controller triggers (left trigger = left, right trigger = right, 10 deg/s) until it seems to come from the cardboard; A confirms. |
| Visual stimulus | Square cardboard 0.25 m wide x 0.2 m high (image to be replaced), center at eye level, facing the participant; experiment: 2 m, at -37.5, -22.5, -7.5, 7.5, 22.5 or 37.5 deg (never 0); practice: 0 deg, 1.2 m. (First version: male cardboard figure on the floor at 1 m; felt wrong next to eye-level audio.) |
| Audio stimulus | White noise, normalized -23 LUFS mono, head height, experiment 2 m, starts at +60 / -60 deg alternating; practice +30 deg, 1.2 m; radius locked. |
| Conditions and order | Quiet (RT60 0.2 s, no background), Noisy (RT60 0.7 s + three spatial backgrounds), Reverb (RT60 2.2 s, background continues); fixed order Quiet, Noisy, Reverb. RT60 values calculated, not measured. (First protocol: Quiet dry, Reverb with the Concrete preset, Noisy dry; order Quiet, Reverb, Noisy.) |
| Trials | 6 per condition: audio start +60, -60, +60, -60, +60, -60; visual side left, left, right, right, left, right; each side's three positions once per condition (seeded shuffle, seed logged). |
| Practice | Unlimited, GUI hints shown, fixed geometry (visual 0 deg / 1.2 m, audio +30 deg / 1.2 m), "Practice again" / "I'm ready" buttons. |
| Room | 8.72 m wide x 15 m deep x 3.5 m high (dialog default, same as previous lab studies; earlier 8 x 10 and 6 x 6), white walls and ceiling, light-gray floor, no clutter, baked soft light, shoebox same size, RT60 per condition (see conditions). 500 cd/m2 dropped (hardware limit). |
| Background audio | Three podcast / soundscape recordings (`SFX-C-02..04`, -23 LUFS), spatialized, eye level, 2 m from the head at left (-90), right (+90) and back (180), looping together, unity gain. (First version: one non-spatial speech background at 70 dB with +15 dB SNR; replaced.) |
| GUI policy | Menu (ID, Start, Debug) for the experimenter; language choice, practice instructions, result buttons for the participant; no GUI and no feedback in real trials (only a fixation dot before each trial); rest screens between conditions; thank-you screen. English and Chinese (Taiwan), Noto Sans TC. |
| Participant ID, data, log fields | ID via on-screen number pad (system keyboard did not open); CSV in `persistentDataPath`, pulled with MQDH; file `yyyyMMdd_HHmmss[_ID][_DEBUG].csv`; per trial: visual and audio start angle, final audio angle, signed error, duration, stimulus level, background level; stage/condition begin and end timestamps, seed. |
| Debug / experimenter tools | Debug test bench: condition switch, sphere always / after confirm, unlimited trials with signed difference, back to menu; Validate Setup; Hot Fix step. |
| Platforms and tools | Windows (Quest Link, builds) and macOS (builds); standalone Quest 3; MQDH. |
| Assets the PO provides | Normalized audio (`Assets/Audio`, preparation notes in its README), Noto Sans TC font, later the cardboard image. |
| Measurements the PO will do | Sound pressure with a level meter; RT60 decision (calculated candidates in `Docs/AudioCalibration.md`). |
