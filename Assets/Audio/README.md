# Audio stimulus preparation (ffmpeg → Unity / Meta XR Audio)

Pipeline: **clip → mono WAV → loudness-normalize → verify → spectrogram**, plus a generated white-noise stimulus.
Commands were tested with ffmpeg 6.1 (output format is the same in 9.x). Replace file names with yours.

---

## 0. Inspect the source

```bash
ffprobe -hide_banner input.mp3
```

Check: channel count, sample rate, and whether the file is true stereo or dual mono.
Dual-mono test (difference of the two channels; near silence, roughly ≤ -60 dB peak, means dual mono):

```bash
ffmpeg -hide_banner -i input.mp3 -af "pan=mono|c0=c0-c1,volumedetect" -f null - 2>&1 | grep -E "mean_volume|max_volume"
```

If the channels differ clearly, the source is true stereo (panning, widening, or baked-in stereo effects such as reverb).
For a localization study, prefer a **dry mono** source (re-export from the DAW if possible). A reverb baked into the clip contaminates a reverb manipulation.

## 1. Clip, convert to mono WAV, 48 kHz, 24-bit

Why: the Meta XR Audio source needs **mono** clips; WAV (uncompressed PCM) imports into Unity without altering levels; 48 kHz matches typical headset output.

Clip 05:00 → 10:00 and convert in one step:

```bash
ffmpeg -i input.mp3 -ss 00:05:00 -to 00:10:00 -map_metadata -1 \
  -af "pan=mono|c0=0.5*c0+0.5*c1" -ar 48000 -c:a pcm_s24le clip.wav
```

- `-map_metadata -1` strips tags (e.g. iTunes Sound Check data, which Unity ignores anyway).
- `pan=mono|c0=0.5*c0+0.5*c1` averages L and R. For true-stereo sources compare the mix with each channel first (`volumedetect` on `c0`, `c1` and the mix). If the mix is more than about 3 dB below either channel (rule of thumb), phase cancellation is thinning the sound; use one channel (`pan=mono|c0=c0`) or re-export.

**MP3 sources only: remove encoder delay/padding.** If the file has no gapless header, ffmpeg keeps the extra samples. The `iTunSMPB` tag gives delay, padding and true length. Example: delay 528 and length 352,800 samples (= 8.000 s at 44.1 kHz) →

```bash
ffmpeg -i input.mp3 -map_metadata -1 \
  -af "atrim=start_sample=528:end_sample=353328,asetpts=PTS-STARTPTS,pan=mono|c0=0.5*c0+0.5*c1" \
  -ar 48000 -c:a pcm_s24le clip.wav
```

(`end_sample` = delay + length.) Verify the output duration afterwards.

## 2. Loudness-normalize (two-pass `loudnorm`, target -23 LUFS)

**Cut to the final stimuli first, then normalize each stimulus.** Normalizing a long clip and cutting later leaves per-trial loudness different.

Pass 1 (measure):

```bash
ffmpeg -hide_banner -i clip.wav -af loudnorm=I=-23:TP=-1.5:LRA=7:print_format=json -f null -
```

Pass 2 (apply; paste the measured values; keep `-ar 48000`, because `loudnorm` resamples internally):

```bash
ffmpeg -i clip.wav -af "loudnorm=I=-23:TP=-1.5:LRA=7:measured_I=<input_i>:measured_TP=<input_tp>:measured_LRA=<input_lra>:measured_thresh=<input_thresh>:offset=<target_offset>:linear=true" \
  -ar 48000 -ac 1 -c:a pcm_s24le clip_final.wav
```

Notes: if the clip's loudness range exceeds the `LRA` target, `linear=true` falls back to dynamic mode (check the log for a warning). LUFS is less reliable on very short clips.

### Optional: one-command helper (needs `jq`; works in zsh and bash)

```zsh
norm23() {   # usage: norm23 in.wav out.wav
  local j i tp lra th off
  j=$(ffmpeg -hide_banner -nostats -i "$1" -af loudnorm=I=-23:TP=-1.5:LRA=7:print_format=json -f null - 2>&1 | sed -n '/^{/,/^}/p')
  i=$(echo "$j" | jq -r .input_i);  tp=$(echo "$j" | jq -r .input_tp)
  lra=$(echo "$j" | jq -r .input_lra); th=$(echo "$j" | jq -r .input_thresh); off=$(echo "$j" | jq -r .target_offset)
  ffmpeg -hide_banner -loglevel error -y -i "$1" \
    -af "loudnorm=I=-23:TP=-1.5:LRA=7:measured_I=$i:measured_TP=$tp:measured_LRA=$lra:measured_thresh=$th:offset=$off:linear=true" \
    -ar 48000 -ac 1 -c:a pcm_s24le "$2"
}
```

### Verify

```bash
ffmpeg -hide_banner -i clip_final.wav -af ebur128=peak=true -f null - 2>&1 | tail -n 14
```

Expect integrated loudness near -23 LUFS (a few tenths of a LU off is normal; my 2 s noise test gave -23.2) and true peak ≤ -1.5 dBTP. Also confirm sample rate, channels and duration:

```bash
ffprobe -v error -show_entries stream=sample_rate,channels,duration -of default=nw=1 clip_final.wav
```

## 3. Spectrogram for every WAV in the folder (zsh)

```zsh
for f in *.wav(N); do
  ffmpeg -hide_banner -loglevel error -y -i "$f" \
    -lavfi "showspectrumpic=s=1200x600:legend=1" "${f:r}_spectrum.png"
done
```

Use it to spot baked-in reverb tails, band limits (MP3 low-pass), hum, or clicks.

## 4. White noise (2 s, with ramps, normalized)

```bash
ffmpeg -f lavfi -i "anoisesrc=d=2:c=white:r=48000:a=0.5:seed=1" \
  -af "afade=t=in:d=0.02:curve=hsin,afade=t=out:st=1.98:d=0.02:curve=hsin" \
  -ac 1 -c:a pcm_s24le noise_raw.wav
```

then normalize with section 2 (`norm23 noise_raw.wav noise_final.wav`).

- `c=white` = white noise; `seed=1` makes it reproducible; `a=0.5` raw amplitude (final level is set by normalization).
- 20 ms ramps avoid onset/offset clicks (Huisman et al., 2021, used a 20 ms raised-cosine ramp on a noise burst).
- **Frequency range:** flat from 0 Hz to the Nyquist frequency (24 kHz at 48 kHz sampling). Human hearing is roughly 20 Hz–20 kHz and the earphones/speakers will roll off earlier. To define a band, add e.g. `highpass=f=20,lowpass=f=16000` before the fades and report it.
- Verified: the spectrogram of the normalized noise is uniform from DC to 24 kHz.
- Huisman et al. used pink noise (equal energy per octave), which sounds less harsh than white noise at the same loudness; check comfort in a pilot.

---

## Why -23 LUFS?

- **It is a convention, not a value derived from the VR literature.** -23 LUFS is the broadcast programme-loudness reference (EBU R128); I have not cited a source for it in this project. Any consistent target would work.
- **Purpose: equal loudness across stimuli as a file property.** LUFS uses a perceptual (K-weighted) measure, so it tracks perceived loudness better than peak or raw RMS. Every clip then enters the system at the same nominal level.
- **Headroom.** With a -1.5 dBTP true-peak limit, the spatializer and reverb can add some energy without clipping. Do **not** peak-normalize to 0 dBFS.
- **Reportable.** A stated number (-23 LUFS, -1.5 dBTP) is easy to document in methods.
- If you need a quieter headset volume step for the target SPL, you could normalize louder (e.g. -16 LUFS) as long as peaks stay under the limit; very low volume steps on a consumer device can be coarse. This is general audio practice, not verified for the Quest.
- Limits: equal LUFS between very different sounds (noise vs speech vs SFX) is not guaranteed to sound equally loud. Pilot it.

## Why keep Unity gains at 0 dB (unity) and set level with the headset volume?

- **One calibrated control.** Level at the ear depends on file level × software gains × headset volume × transducer. If you leave software at unity, the digital signal is exactly the normalized file, and the only variable is the headset volume, which you measure once and report.
- **No stacked gains.** Unity AudioSource volume, mixer attenuation, AudioListener volume, Meta Gain Boost and reverb send levels all multiply or add. Changing several makes levels irreproducible between builds and participants.
- **Avoid clipping and quantization.** Boosting in software risks clipping; cutting in software wastes bit depth.

Set to unity:
- AudioSource volume = 1; AudioListener volume = 1
- Mixer group attenuation = 0 dB
- Meta XR Audio Source **Gain Boost = 0 dB** (Meta documents 0 as unity gain)
- Reverb Send Level at default (Reverb Level on the mixer effect is a deliberate experimental parameter; keep it fixed and report it)

Still measure:
- The spatializer changes level with direction (interaural level differences) and reverb adds energy, so measure at the actual positions (±60° and ahead) in each acoustic condition.
- I did not verify how Meta's spatializer handles distance attenuation at 1 m; confirm unity by measurement.
- Unity cannot set the Quest system volume: check and record the volume step at the start of every session.
- Absolute SPL needs a measurement (calibrated mic/ear simulator). Phone apps with an external calibrated microphone are within about ±1 dB in published tests (Kardous & Shaw, 2016), but a phone alone is not a certified meter.

---

## Assets

- SFX-C-02
  > Thought Lab with Guowei. (2026, September 12). 同樣用AI，為什麼矽谷人早就「活在未來」？用AI做App要到「能賣」的關鍵是？| 思想實驗室 Video Podcast ep118：ft.科技浪 哈利 [Video]. YouTube. https://www.youtube.com/watch?v=IVmQrtC_ul0
- SFX-C-03
  > 阿淇博士 Dr. Achi. (2026, January 16). 年輕人沒錢了！？消費數字看出中美經濟問題！需要全民普發現金？ [Video]. YouTube. https://www.youtube.com/watch?v=3A5dGUcrjZU
- SFX-C-04
  > 大禾音樂 (Harvest Music). (2018, March). 迪化街：買年貨 [Dihua Street: Lunar New Year shopping] [Field recording; file: mrt-soundscape-dihua-street-lunar-new-year-shopping.mp3]. In 捷運聲音地景計畫 [MRT Soundscape Project]. Taiwan.md. https://taiwan.md/soundscape

---

## References

- Huisman, T., Ahrens, A., & MacDonald, E. (2021). Ambisonics sound source localization with varying amount of visual information in virtual reality. *Frontiers in Virtual Reality, 2*. https://doi.org/10.3389/frvir.2021.722321
- Kardous, C. A., & Shaw, P. B. (2016). Evaluation of smartphone sound measurement applications (apps) using external microphones—A follow-up study. *JASA, 140*(4), EL327–EL333. https://doi.org/10.1121/1.4964639
- Meta XR Audio SDK for Unity documentation: [spatialize](https://developers.meta.com/vr/documentation/unity/meta-xr-audio-sdk-unity-spatialize/), [room acoustics](https://developers.meta.com/horizon/documentation/unity/meta-xr-audio-sdk-unity-room-acoustics), [setup](https://developers.meta.com/horizon/documentation/unity/meta-xr-audio-sdk-unity-req-setup/)
