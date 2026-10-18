# ADR-0005: Acoustics per condition, room size (2026-10-18)

Status: accepted; headset test pending. Supersedes the condition definitions in ADR-0003 (the material question PB-14 is closed).

- **Protocol:** order Quiet, Noisy, Reverb. Quiet: room with RT60 0.2 s, no background. Noisy: RT60 0.7 s plus the three spatial backgrounds. Reverb: RT60 2.2 s, the background continues playing (also over the rest screen). Practice uses the Quiet acoustics.
- **Everything is in the room:** stimulus and backgrounds both have Enable Acoustics on (previously the Noisy condition was dry). The room is rendered by the mixer effect "Meta XR Audio Reflection" (`Assets/Audio/HumPerMixer.mixer`, created by Step 4; without it the sources stay dry).
- **RT60 is set directly:** Meta's shoebox only offers material presets, so `ReverbTuner` sends one uniform reflection coefficient on all six surfaces to the plugin, calculated from the target RT60 and the room size with Eyring's formula (`RoomAcousticsMath`). **The values are calculated, not measured**, and assume Meta's coefficient is a pressure coefficient (alpha = 1 - R^2); if it is an energy coefficient the real times are about twice as long. Measuring the rendered RT60 (PB-15) is still recommended before the data is interpreted.
- **Debug bench:** condition buttons apply the condition's own RT60 and background; the reverb buttons (0.2 / 0.7 / 2.2 s, Meta preset = Concrete about 4.7 s, +/-10%) override it until the next condition button.
- **Room size default:** 8.72 m wide x 3.5 m high x 15 m deep (as in previous studies of the lab). The existing scene keeps its room until Step 2 is run again.
- **Consequences:** progress files of sessions started under the old order (Quiet, Reverb, Noisy) must not be resumed; the H1 text in `ExperimentDesign.md` has not been revisited for the new conditions.
