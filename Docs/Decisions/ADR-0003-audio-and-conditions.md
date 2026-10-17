# ADR-0003: Audio, conditions, levels (2026-10-18)

Status: accepted; reverb material open (PB-14).

- Clips: loudness-normalized to -23 LUFS, mono (`Assets/Audio/README.md`). Stimulus `SFX-C-01` (white noise); backgrounds `SFX-C-02..04`.
- No software level changes: AudioSource volume 1, Meta gain boost 0 dB, flat rolloff (fixed radius, so no distance attenuation); the headset volume is the only level control. (A 20 dB boost was tried and removed: too loud.)
- Quiet and Practice: dry. Reverb: the stimulus is sent into the Meta shoebox (`MetaXRAudioSource.EnableAcoustics`); Noisy: dry, three spatialized backgrounds (left, right, back) at eye level, same distance as the stimulus (2 m), looping together for the whole block.
- Trial geometry: experiment 2 m, practice 1.2 m; visual cardboard 0.25 x 0.2 m at eye level; audio starts +/-60 (alternating), visual positions +/-7.5, 22.5, 37.5 deg per side, each once per condition (seeded shuffle, seed logged).
- Task speed 10 deg/s per trigger; the participant confirms with A; forward-facing gate before every trial (+/-5 deg yaw, +/-10 deg pitch, 1 s).
- Debug mode is a test bench, not a run of the experiment (condition switch, sphere always / after confirm, unlimited trials, back to menu). Real runs give no feedback.
- Luminance 500 cd/m2 is not reachable on Quest 3: closed as a hardware limit.
