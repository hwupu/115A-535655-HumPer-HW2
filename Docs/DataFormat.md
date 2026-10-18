# Log Data Format

File: `yyyyMMdd_HHmmss[_ParticipantID][_DEBUG].csv` in `Application.persistentDataPath` (see README for the Quest and Editor locations). UTF-8, one header row, flushed after every row.

Columns: `event, timestamp_local, elapsed_s, participant, mode, condition, trial, visual_az_deg, audio_start_az_deg, audio_final_az_deg, signed_error_deg, duration_s, stim_level_dbfs, bg_level_dbfs, note`

| event | meaning |
|---|---|
| `session_start` | note: app version, device, stimulus gain boost (dB) |
| `language` | selected language |
| `practice_begin` / `practice_end` | practice stage (note: number of practice trials) |
| `experiment_begin` | note: random seed used for the visual positions |
| `condition_begin` / `condition_end` | `quiet`, `reverb`, `noisy` |
| `trial` | one finished trial (practice trials: `condition=practice`) |
| `experiment_end` | end of the experiment |
| `debug_begin` / `condition` / `debug_end` | Debug test bench only |
| `app_paused` | headset removed / app paused |
| `recenter` | the Meta button was long-pressed; GUI and stimuli were re-placed around the new forward direction |

- `mode`: `EXPERIMENT` or `DEBUG`. Timestamps are local time; `elapsed_s` counts from the start of the log.
- `signed_error_deg` = audio_final - visual (positive: the audio ended to the right of the cardboard).
- `duration_s`: from stimulus onset to the A press.
- `stim_level_dbfs`: software RMS level of the stimulus clip plus its Meta gain boost. `bg_level_dbfs`: combined level (power sum) of the three background clips, blank outside Noisy. These are software levels; the sound pressure at the ear depends on the headset volume and must be measured.
