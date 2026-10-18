# Audio Levels and Reverb Material

## Levels
- All clips are loudness-normalized (-23 LUFS, mono). The software changes no level: AudioSource volume 1, Meta gain boost 0 dB (inspector value on the Spatial Audio blocks), no distance attenuation (flat rolloff, so the 1.2 m practice and 2 m trials are equally loud). The headset volume is the only level control.
- The Noisy background is three clips playing together, so its combined level is about 4.8 dB above one clip (power sum of three equal clips). The logged `stim_level_dbfs` / `bg_level_dbfs` are software levels, not sound pressure.
- Status: the PO is happy with the current volume (ticket closed). Measuring sound pressure at the ear with a level meter is planned by the PO; record the headset volume step and the meter reading in the experiment notes.

## Reverb: which material gives RT60 0.7 s (or 0.2 s)?
Meta's shoebox model offers material presets (four frequency-band reflection coefficients per preset); a custom RT60 cannot be typed in. Concrete (mean reflection coefficient 0.98) is very reflective.

**These numbers are calculated, not measured.** Method: uniform material on all six surfaces of the 8 x 10 x 3.5 m room (V = 280 m3, S = 286 m2); mean of the four band coefficients; absorption alpha = 1 - R^2 (if R is an amplitude coefficient, interpretation A) or 1 - R (if R is an energy coefficient, interpretation B); RT60 by Sabine, 0.161 V / (S alpha), and Eyring. Meta does not document which interpretation applies, and its late-reverb model may differ, so use the table to pick candidates and then measure.

| Preset | A (amplitude): Sabine / Eyring | B (energy): Sabine / Eyring |
|---|---|---|
| Concrete | 4.8 / 4.7 s | 9.5 / 9.4 s |
| GypsumBoard | 0.66 / 0.58 s | 1.24 / 1.16 s |
| Steel | 0.73 / 0.64 s | 1.37 / 1.29 s |
| WoodThick, MetaDefault | 0.83 / 0.75 s | 1.6 / 1.5 s |
| Mud | 0.45 / 0.36 s | 0.80 / 0.72 s |
| Glass, WoodThin | 0.5 / 0.4 s | 0.9 / 0.8 s |
| Carpet | 0.31 / 0.23 s | 0.54 / 0.45 s |
| AcousticTile | 0.19 / 0.09 s | 0.27 / 0.18 s |
| Curtain, CarpetHeavyPadded | 0.21 / 0.11 s | 0.31 / 0.22 s |

Candidates: **about 0.7 s**: GypsumBoard (A) or Mud (B); **about 0.2 s**: AcousticTile (close under both interpretations), alternatively Curtain or CarpetHeavyPadded. Surfaces can also differ (e.g. different floor and ceiling) to fine-tune. Pick by measuring (proposed ticket: an RT60 measurement in the Debug bench using the rendered audio output).

## Luminance
The 500 cd/m2 goal is above the Quest 3 hardware limit (roughly 100 nits). Ticket closed as a hardware limit; the room uses baked soft lighting tuned by eye (`HumPerSetup`: ambient 0.7, ceiling area light intensity 2).

## Update 2026-10-18
The material question above is superseded: the RT60 per condition (0.2 / 0.7 / 2.2 s) is now set directly through a calculated reflection coefficient (ADR-0005). The table remains useful as the calculated RT60 of Meta's presets in the old 8 x 10 x 3.5 m room; for the new room (8.72 x 3.5 x 15 m, V = 458 m3, S = 428 m2) the Debug bench shows the calculated RT60 of the current preset.

## Measured relation (2026-10-18, PB-15)
Impulse response through the real chain (Meta spatializer + reflection mixer effect, Windows plugin, room 8.72 x 3.5 x 15 m, source 2 m ahead, uniform coefficient on all six surfaces), T30 in seconds: R 0.5 -> 0.30, 0.6 -> 0.33, 0.7 -> 0.42, 0.8 -> 0.65, 0.85 -> 0.89, 0.9 -> 1.30, 0.94 -> 1.89, 0.95 -> 2.11, 0.96 -> 2.49, 0.97 -> 3.05, 0.98 -> 3.94, 0.99 -> 5.74. This replaces the Eyring-based table above for the room used in the experiment (the Eyring numbers were too short by 20-170 %). The Concrete preset (mean R about 0.98) therefore gives about 4 s here, not 4.7 s.
