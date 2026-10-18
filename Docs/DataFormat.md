# Log Data Format

File: `yyyyMMdd_HHmmss[_ParticipantID][_DEBUG].csv` in `Application.persistentDataPath` (see README for the Quest and Editor locations). UTF-8, one header row, flushed after every row.

Columns: `event, timestamp_local, elapsed_s, participant, mode, condition, trial, visual_az_deg, audio_start_az_deg, audio_final_az_deg, signed_error_deg, duration_s, stim_level_dbfs, bg_level_dbfs, rt60_measured_s, note`

| event | meaning |
|---|---|
| `session_start` | note: app version, device, stimulus gain boost (dB), stimulus on / gap seconds |
| `language` | selected language |
| `practice_begin` / `practice_end` | practice stage (note: number of practice trials) |
| `experiment_begin` | note: random seed used for the visual positions |
| `condition_begin` / `condition_end` | `quiet`, `noisy`, `reverb` (in that order). Note: `rt60=` target in s, `rt60_measured=` T20 in s (measured at the block start), and when the background plays `bg:x..y..z..S|...` = listener-relative position (m) of the three backgrounds, `S` = spatializer on |
| `trial` | one finished trial (practice trials: `condition=practice`) |
| `experiment_end` | end of the experiment |
| `debug_begin` / `condition` / `debug_end` | Debug test bench only |
| `app_paused` | headset removed / app paused |
| `recenter` | the Meta button was long-pressed; GUI and stimuli were re-placed around the new forward direction |
| `rt60_measured` | reverberation time measured from an impulse response of the rendered audio (note: condition, target, T20, T30, depth of the decay in dB); at the start of every condition block and on the Debug "Measure RT60" button |
| `reverb` | Debug only: reverb setting changed (note: RT60 target and coefficient, or preset) |
| `setup_step` | a pre-practice instruction screen was confirmed (note: `setup_recenter`, `setup_ipd`, `setup_dot`) |
| `session_resume` / `resume` | an interrupted session was continued (appended to the same CSV; note: position, e.g. `Reverb, 3 of 6 trials done`) |
| `app_resumed` | app back from pause |
| `participant_cancelled` | the experimenter cancelled the participant (Y button, or "Start new" over an unfinished session); the session cannot be resumed |

- `mode`: `EXPERIMENT` or `DEBUG`. Timestamps are local time; `elapsed_s` counts from the start of the log.
- `signed_error_deg` = audio_final - visual (positive: the audio ended to the right of the cardboard).
- `duration_s`: from stimulus onset to the A press.
- `rt60_measured_s`: measured reverberation time (T20, seconds) of the condition block, repeated on every trial row of the block (reference only; empty for practice and Debug trials).
- `stim_level_dbfs`: software RMS level of the stimulus clip plus its Meta gain boost. `bg_level_dbfs`: combined level (power sum) of the three background clips, blank outside Noisy. These are software levels; the sound pressure at the ear depends on the headset volume and must be measured.
