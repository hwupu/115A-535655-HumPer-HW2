using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HumPer
{
    /// <summary>
    /// Start experiment: menu -> language -> practice instructions -> repeatable practice trials -> "ready" (the three conditions come next).
    /// Debug: a test bench (see <see cref="DebugPanel"/>) where every function can be tried: pick the condition, show the audio sphere
    /// always or only after confirming, run as many trials as you like, go back to the menu.
    ///
    /// Trial: face forward (gate), then rotate the audio stimulus with the triggers (left -5 deg/s, right +5 deg/s) and confirm with A.
    /// A real experiment run gives no feedback of any kind; the sphere and the signed difference exist only in the Debug bench.
    /// </summary>
    public class ExperimentFlow : MonoBehaviour
    {
        public GuiRefs gui;
        public StimulusController stimuli;
        public AcousticConditions acoustics;
        public OVRCameraRig rig;
        [Tooltip("Debug bench: custom reverberation times (Meta only offers material presets).")]
        public ReverbTuner reverb;
        [Tooltip("The background noise keeps playing over the rest screen between Noisy and Reverb (otherwise it restarts with the Reverb block).")]
        public bool continueBackgroundOverRest = true;
        [Tooltip("Measure the reverberation time (impulse response of the rendered audio) at the start of every condition block and log it with every trial, for reference.")]
        public bool measureRt60AtBlockStart = true;
        public Rt60Meter rt60Meter;

        [Header("Practice trial geometry (fixed, different from the experiment trials)")]
        public float practiceRadiusM = 1.2f;
        public float practiceVisualAzDeg = 0f;
        public float practiceAudioStartAzDeg = 30f;

        [Header("Experiment trial geometry")]
        public float trialRadiusM = 2f;
        public float[] leftVisualPoolDeg = { -37.5f, -22.5f, -7.5f };
        public float[] rightVisualPoolDeg = { 7.5f, 22.5f, 37.5f };
        public float audioStartDeg = 60f; // +/-60, alternating

        [Header("Task")]
        public float audioRateDegPerSec = 10f;
        public float azimuthLimitDeg = 90f;
        public float confirmIgnoreSec = 0.3f;
        public float interTrialSec = 0.6f;

        [Header("Head alignment gate (before every trial)")]
        public float gateYawToleranceDeg = 5f;
        public float gatePitchToleranceDeg = 10f;
        public float gateHoldSec = 1f;
        public float gateHintAfterSec = 4f;

        ExperimentLog _log;
        bool _running;
        float _lastFinalAz;

        Transform Head => rig.centerEyeAnchor;
        Quaternion Yaw => Quaternion.Euler(0f, rig.transform.eulerAngles.y, 0f);

        void Start()
        {
            if (rig == null) rig = FindFirstObjectByType<OVRCameraRig>();
            gui.menu.Chosen += OnChosen;
            _store = new ProgressStore(Application.persistentDataPath);
            gui.Show(gui.menu.gameObject);
            RefreshMenuHint();
            StartCoroutine(SubscribeRecenter());
            if (rt60Meter == null)
            {
                var listener = FindFirstObjectByType<AudioListener>();
                if (listener != null) rt60Meter = listener.GetComponent<Rt60Meter>() ?? listener.gameObject.AddComponent<Rt60Meter>(); // OnAudioFilterRead must sit next to the listener
            }
        }

        void OnDestroy()
        {
            if (gui != null && gui.menu != null) gui.menu.Chosen -= OnChosen;
            if (_recenterSource != null) _recenterSource.RecenteredPose -= OnRecentered;
            _log?.Dispose();
        }

        // ---- Recenter (long press of the Meta button): the GUI and the stimuli follow the new "forward" ----

        OVRDisplay _recenterSource;
        bool _recenterPending;

        IEnumerator SubscribeRecenter()
        {
            while (OVRManager.display == null) yield return null; // created by OVRManager after its Awake
            _recenterSource = OVRManager.display;
            _recenterSource.RecenteredPose += OnRecentered;
        }

        void OnRecentered()
        {
            if (_recenterPending) return;
            _recenterPending = true;
            StartCoroutine(ApplyRecenter());
        }

        IEnumerator ApplyRecenter()
        {
            yield return null; // let tracking deliver the pose after the recenter
            yield return null;
            gui.PlaceAll();
            stimuli.Replace(Head.position, Yaw);
            _log?.Event("recenter");
            _recenterPending = false;
        }

        void OnApplicationPause(bool paused)
        {
            _log?.Event(paused ? "app_paused" : "app_resumed");
        }

        // ---- Experimenter controls: Y = cancel this participant; unfinished sessions can be resumed ----

        ProgressStore _store;
        SessionProgress _progress;
        Coroutine _runCo;
        bool _realRun;      // a real (non-debug) session is running: Y opens the cancel dialog
        bool _dialogOpen;
        float _blockRt60 = float.NaN; // measured reverberation time of the current condition block (s), logged with every trial

        void Update()
        {
            if (_realRun && !_dialogOpen && OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch)) // Y
                StartCoroutine(CancelDialog());
        }

        /// <summary>Shows the unfinished sessions under the menu so the experimenter knows which ID to enter.</summary>
        void RefreshMenuHint()
        {
            var open = _store.ListUnfinished();
            var sb = new StringBuilder();
            foreach (var p in open)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append("ID ").Append(p.ParticipantId.Length > 0 ? p.ParticipantId : "(none)").Append(" (").Append(p.Describe()).Append(")");
            }
            gui.menu.SetStatus(open.Count > 0 ? "Unfinished: " + sb + ". Enter the ID and press Start to resume." : "");
        }

        IEnumerator CancelDialog()
        {
            _dialogOpen = true;
            Time.timeScale = 0f;        // freezes the trial, timers and gate; the pointer still works (it polls OVRInput)
            AudioListener.pause = true;
            string id = _progress != null && _progress.ParticipantId.Length > 0 ? _progress.ParticipantId : "(no ID)";
            gui.cancelText.text = "Cancel participant " + id + "?\nThe CSV is kept and marked 'cancelled'; the session cannot be resumed.";
            gui.cancelPanel.SetActive(true);
            UiPointer.Modal = gui.cancelPanel.transform;

            Button clicked = null;
            yield return WaitForAny(b => clicked = b, gui.cancelConfirmButton, gui.cancelKeepButton);

            gui.cancelPanel.SetActive(false);
            UiPointer.Modal = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            _dialogOpen = false;
            if (clicked == gui.cancelConfirmButton) AbortRealRun("cancelled_by_experimenter");
        }

        /// <summary>Stop the running session, keep its CSV (with a cancel row) and mark the progress file cancelled. Back to the menu.</summary>
        void AbortRealRun(string reason)
        {
            if (_runCo != null) StopCoroutine(_runCo);
            _runCo = null;
            stimuli.HideAll();
            acoustics.StopAll();
            gui.ShowHud(null);
            if (_log != null)
            {
                _log.Event("participant_cancelled", reason);
                _log.Dispose();
                _log = null;
            }
            if (_progress != null)
            {
                _progress.Status = SessionStatus.Cancelled;
                _store.Save(_progress);
            }
            _realRun = false;
            _running = false;
            gui.Show(gui.menu.gameObject);
            RefreshMenuHint();
        }

        void OnChosen(string participantId, bool debug)
        {
            if (_running) return;
            _running = true;
            _runCo = StartCoroutine(debug ? RunDebug(participantId) : StartReal(participantId));
        }

        /// <summary>Real session: if an unfinished one exists for this ID, ask whether to resume it or start a new one.</summary>
        IEnumerator StartReal(string participantId)
        {
            var existing = _store.LoadUnfinished(participantId);
            SessionProgress resume = null;
            if (existing != null)
            {
                gui.resumeText.text = "Unfinished session for ID " + (participantId.Length > 0 ? participantId : "(none)") + ": " + existing.Describe() +
                                      ".\nResume it, or start a new session (the old one is marked cancelled)?";
                gui.Show(gui.resumePanel);
                Button clicked = null;
                yield return WaitForAny(b => clicked = b, gui.resumeButton, gui.startNewButton, gui.resumeBackButton);
                if (clicked == gui.resumeBackButton)
                {
                    gui.Show(gui.menu.gameObject);
                    _running = false;
                    yield break;
                }
                if (clicked == gui.resumeButton) resume = existing;
                else CancelOldSession(existing, "replaced_by_new_session");
            }
            yield return Run(participantId, resume);
        }

        void CancelOldSession(SessionProgress old, string reason)
        {
            string path = Path.Combine(Application.persistentDataPath, old.CsvFile);
            if (File.Exists(path))
            {
                using (var writer = new StreamWriter(path, true, new UTF8Encoding(false)))
                    new ExperimentLog(writer, () => DateTime.Now, old.ParticipantId, false, null, writeHeader: false).Event("participant_cancelled", reason);
            }
            old.Status = SessionStatus.Cancelled;
            _store.Save(old);
        }

        // ==== Start experiment ====================================================================================

        IEnumerator Run(string participantId, SessionProgress resume)
        {
            bool resumed = resume != null;
            _progress = resumed ? resume : new SessionProgress { ParticipantId = participantId };
            OpenLog(participantId, false, resumed ? _progress.CsvFile : null);
            _realRun = true;
            _store.Save(_progress);
            if (resumed) _log.Event("resume", _progress.Describe());
            yield return RecenterAndPlaceGui();

            // ---- language ----
            if (!_progress.LanguageChosen)
            {
                gui.Show(gui.languagePanel);
                Button picked = null;
                yield return WaitForAny(b => picked = b, gui.chineseButton, gui.englishButton);
                _progress.Language = picked == gui.chineseButton ? Language.ChineseTaiwan : Language.English;
                _progress.LanguageChosen = true;
                _store.Save(_progress);
                Localization.Set(_progress.Language);
                _log.Event("language", _progress.Language.ToString());
            }
            else Localization.Set(_progress.Language);

            // ---- practice, as many trials as the participant likes ----
            bool practiceWasDone = _progress.PracticeDone;
            if (!practiceWasDone)
            {
                // the headset was probably set up standing and handed over: recenter, then the IPD dial
                yield return ShowSetupStep("setup_recenter");
                yield return ShowSetupStep("setup_ipd");

                gui.Show(gui.introPanel);
                yield return WaitForAny(_ => { }, gui.startPracticeButton);
                yield return ShowSetupStep("setup_dot"); // then the first trial starts with the fixation dot (head gate)

                _log.ConditionEvent("practice_begin", "practice");
                int number = 0;
                bool ready = false;
                while (!ready)
                {
                    var spec = new TrialSpec(Condition.Practice, ++number, practiceAudioStartAzDeg, practiceVisualAzDeg, practiceRadiusM);
                    yield return RunTrial(spec, showPracticeHint: true, sphereAlways: false, feedback: false, hudInfo: null);
                    gui.Show(gui.resultPanel);
                    yield return WaitForAny(b => ready = b == gui.readyButton, gui.againButton, gui.readyButton);
                }
                _log.ConditionEvent("practice_end", "practice", "trials=" + number);

                _progress.Seed = (int)(DateTime.Now.Ticks & 0x7FFFFFFF);
                _progress.PracticeDone = true;
                _store.Save(_progress);
                _log.Event("experiment_begin", "seed=" + _progress.Seed);
            }
            else if (resumed)
            {
                // Welcome back screen (reuses the rest screen) before the next trial.
                gui.blockDoneText.key = "resume_intro";
                gui.blockDoneText.SetArgs();
                gui.Show(gui.blockDonePanel);
                yield return WaitForAny(_ => { }, gui.continueButton);
                gui.HideAll();
                gui.blockDoneText.key = "block_done";
            }

            // ---- the experiment: Quiet, Reverb, Noisy; 6 trials each; a rest screen between conditions ----
            var order = TrialPlanner.ConditionOrder;
            int startBlock = _progress.BlockIndex;
            for (int c = startBlock; c < order.Length; c++)
            {
                if (c > startBlock)
                {
                    gui.blockDoneText.key = "block_done";
                    gui.blockDoneText.SetArgs(c);
                    gui.Show(gui.blockDonePanel);
                    yield return WaitForAny(_ => { }, gui.continueButton);
                    gui.HideAll();
                }

                var condition = order[c];
                var plan = TrialPlanner.PlanCondition(condition, _progress.Seed + c, leftVisualPoolDeg, rightVisualPoolDeg, audioStartDeg, trialRadiusM);
                int first = c == startBlock ? _progress.TrialsDone : 0;
                acoustics.SetCondition(condition, Head.position, Yaw); // reverb time of the condition; Noisy / Reverb: the background plays
                _blockRt60 = float.NaN;
                string measuredNote = "";
                if (measureRt60AtBlockStart && rt60Meter != null)
                {
                    Rt60Result measured = default;
                    yield return MeasureRt60(r => measured = r);
                    _blockRt60 = measured.Valid ? measured.Rt60T20 : float.NaN;
                    measuredNote = ";rt60_measured=" + (measured.Valid ? measured.Rt60T20.ToString("0.00") : "n/a");
                    _log.Event("rt60_measured", "condition=" + condition.ToString().ToLowerInvariant() + ";target=" + acoustics.CurrentRt60.ToString("0.0#") + ";" + measured);
                }
                string note = (first > 0 ? "resumed;trials_done=" + first + ";" : "") + "rt60=" + acoustics.CurrentRt60.ToString("0.0#") + measuredNote;
                if (acoustics.BackgroundPlaying) note += ";" + acoustics.DescribeBackground(Head); // listener-relative positions, S = spatializer on
                _log.ConditionEvent("condition_begin", condition.ToString().ToLowerInvariant(), note);
                for (int i = first; i < plan.Count; i++)
                {
                    yield return RunTrial(plan[i], showPracticeHint: false, sphereAlways: false, feedback: false, hudInfo: null);
                    _progress.TrialFinished();
                    _store.Save(_progress);
                }
                _log.ConditionEvent("condition_end", condition.ToString().ToLowerInvariant());
                // the background continues from Noisy into Reverb (also over the rest screen)
                bool nextHasBackground = c + 1 < order.Length && ConditionAcoustics.HasBackground(order[c + 1]);
                if (!(nextHasBackground && continueBackgroundOverRest)) acoustics.StopAll();
                stimuli.HideAll();
            }

            _log.Event("experiment_end");
            _log.Dispose();
            _log = null;
            _progress.Status = SessionStatus.Completed;
            _store.Save(_progress);
            _realRun = false;
            gui.Show(gui.thanksPanel);
        }

        /// <summary>One instruction screen before the practice (key from the localization table) with a Continue button.</summary>
        IEnumerator ShowSetupStep(string key)
        {
            gui.setupText.key = key;
            gui.setupText.Refresh();
            gui.Show(gui.setupPanel);
            yield return WaitForAny(_ => { }, gui.setupContinueButton);
            _log.Event("setup_step", key);
        }

        // ==== Debug test bench ====================================================================================

        IEnumerator RunDebug(string participantId)
        {
            OpenLog(participantId, true);
            yield return RecenterAndPlaceGui();

            var panel = gui.debugPanel;
            var condition = Condition.Quiet;
            bool sphereAlways = false;
            int number = 0;
            var rng = new System.Random();
            bool rightSide = true;
            bool stimulusPlaying = false;

            _log.Event("debug_begin");
            panel.SetSphereAlways(sphereAlways);
            panel.SetStimulusPlaying(false);
            panel.SetBackgroundState(false, false);
            panel.SetStatus("Pick a reverb time (= condition: 0.3 Quiet, 0.7 Noisy, 2.2 Reverb), then run a trial. " + (reverb != null ? "Room: " + reverb.RoomModelingStatus : ""));
            acoustics.SetCondition(condition, Head.position, Yaw);
            UpdateReverbReadout(panel, condition);

            while (true)
            {
                gui.Show(panel.gameObject);
                Button clicked = null;
                yield return WaitForAny(b => clicked = b,
                                        panel.rtQuietButton, panel.rtNoisyButton, panel.rtReverbButton, panel.presetButton,
                                        panel.stimulusToggleButton, panel.backgroundToggleButton, panel.measureButton,
                                        panel.sphereToggleButton, panel.runTrialButton, panel.practiceTrialButton, panel.backButton);

                if (clicked == panel.rtQuietButton || clicked == panel.rtNoisyButton || clicked == panel.rtReverbButton)
                {
                    // reverb time and condition are one-to-one: 0.3 s = Quiet, 0.7 s = Noisy (+ background), 2.2 s = Reverb (+ background)
                    condition = clicked == panel.rtQuietButton ? Condition.Quiet : clicked == panel.rtNoisyButton ? Condition.Noisy : Condition.Reverb;
                    acoustics.SetConditionDefault(condition, Head.position, Yaw);
                    panel.SetBackgroundState(acoustics.BackgroundPlaying, acoustics.BackgroundPaused);
                    UpdateReverbReadout(panel, condition);
                    _log.ConditionEvent("condition", condition.ToString().ToLowerInvariant(), "rt60=" + acoustics.CurrentRt60.ToString("0.0#"));
                }
                else if (clicked == panel.presetButton)
                {
                    if (reverb == null || reverb.room == null) panel.SetStatus("No reverb tuner / shoebox in the scene: run Step 2 and Step 4.");
                    else
                    {
                        acoustics.SetReverbOverride(AcousticConditions.RtMode.Preset, 0f, Head.position, Yaw);
                        UpdateReverbReadout(panel, condition);
                        _log.Event("reverb", reverb.Describe() + "; " + reverb.RoomModelingStatus);
                    }
                }
                else if (clicked == panel.stimulusToggleButton)
                {
                    stimulusPlaying = !stimulusPlaying;
                    if (stimulusPlaying)
                    {
                        // straight ahead at the experiment radius, with the cardboard, in the current condition
                        stimuli.Begin(Head.position, Yaw, trialRadiusM, 0f, 0f);
                        stimuli.ShowVisual(true);
                        stimuli.PlayAudio();
                    }
                    else { stimuli.StopAudio(); stimuli.ShowVisual(false); }
                    panel.SetStimulusPlaying(stimulusPlaying);
                }
                else if (clicked == panel.backgroundToggleButton)
                {
                    // pause / resume the background (Noisy and Reverb start with it playing); in Quiet this starts it
                    acoustics.SetBackgroundPlaying(!acoustics.BackgroundPlaying, Head.position, Yaw);
                    panel.SetBackgroundState(acoustics.BackgroundPlaying, acoustics.BackgroundPaused);
                }
                else if (clicked == panel.measureButton)
                {
                    if (rt60Meter == null) panel.SetStatus("No Rt60Meter (needs an AudioListener).");
                    else
                    {
                        stimulusPlaying = false;
                        panel.SetStimulusPlaying(false);
                        panel.SetStatus("Measuring the reverberation time (about 5 s): stay still, the background pauses briefly.");
                        yield return null;
                        float target = acoustics.CurrentRt60 > 0f ? acoustics.CurrentRt60 : (reverb != null ? reverb.PresetRt60() : 0f);
                        Rt60Result measured = default;
                        yield return MeasureRt60(r => measured = r);
                        gui.Show(panel.gameObject);
                        panel.SetStatus("RT60 measured " + (measured.Valid ? measured.Rt60T20.ToString("0.00") + " s" : "n/a") + " (" + measured +
                                        "); target " + target.ToString("0.0#") + " s (calibrated).");
                        _log.Event("rt60_measured", "condition=" + condition.ToString().ToLowerInvariant() + ";target=" + target.ToString("0.0#") + ";" + measured);
                    }
                }
                else if (clicked == panel.sphereToggleButton)
                {
                    sphereAlways = !sphereAlways;
                    panel.SetSphereAlways(sphereAlways);
                }
                else if (clicked == panel.runTrialButton || clicked == panel.practiceTrialButton)
                {
                    stimulusPlaying = false;
                    panel.SetStimulusPlaying(false);
                    TrialSpec spec;
                    if (clicked == panel.practiceTrialButton)
                        spec = new TrialSpec(condition, ++number, practiceAudioStartAzDeg, practiceVisualAzDeg, practiceRadiusM);
                    else
                    {
                        // experiment-style geometry: random visual position (never 0), audio starts at +60 / -60 alternating
                        float visual = rng.Next(2) == 0 ? leftVisualPoolDeg[rng.Next(leftVisualPoolDeg.Length)] : rightVisualPoolDeg[rng.Next(rightVisualPoolDeg.Length)];
                        spec = new TrialSpec(condition, ++number, rightSide ? audioStartDeg : -audioStartDeg, visual, trialRadiusM);
                        rightSide = !rightSide;
                    }
                    string info = string.Format("Debug | {0} | trial {1} | figure {2:0.#} deg | audio start {3:0.#} deg",
                                                condition, number, spec.VisualAzDeg, spec.AudioStartAzDeg);
                    yield return RunTrial(spec, showPracticeHint: false, sphereAlways: sphereAlways, feedback: true, hudInfo: info);
                    float diff = _lastFinalAz - spec.VisualAzDeg;
                    panel.SetStatus(string.Format("Last: trial {0} [{1}] figure {2:0.#}, audio {3:0.#} -> {4:0.#}   difference {5}{6:0.#} deg",
                                                  spec.Index, condition, spec.VisualAzDeg, spec.AudioStartAzDeg, _lastFinalAz,
                                                  diff >= 0f ? "+" : "-", Mathf.Abs(diff)));
                }
                else break; // back
            }

            acoustics.StopAll();
            stimuli.HideAll();
            gui.ShowHud(null);
            _log.Event("debug_end", "trials=" + number);
            _log.Dispose();
            _log = null;
            gui.Show(gui.menu.gameObject);
            RefreshMenuHint();
            _running = false;
        }

        void UpdateReverbReadout(DebugPanel panel, Condition condition)
        {
            if (reverb == null) { panel.SetReverb("(no reverb tuner in the scene)", 0); return; }
            int active = 0; // 1 = Quiet, 2 = Noisy, 3 = Reverb (condition defaults), 4 = Meta preset, 0 = custom value
            if (acoustics.Mode == AcousticConditions.RtMode.Preset) active = 4;
            else if (acoustics.Mode == AcousticConditions.RtMode.ConditionDefault) active = condition == Condition.Quiet ? 1 : condition == Condition.Noisy ? 2 : 3;
            if (panel.presetLabel != null) panel.presetLabel.text = reverb.PresetRt60().ToString("0.#") + " s (preset)";
            string mode = acoustics.Mode == AcousticConditions.RtMode.ConditionDefault ? "  [condition default]" : "  [override]";
            panel.SetReverb(reverb.Describe() + mode, active);
        }

        /// <summary>Reverberation time of the audio as rendered right now (see <see cref="Rt60Meter"/>): about 5 s of silence, a click, and a recording.</summary>
        IEnumerator MeasureRt60(System.Action<Rt60Result> done)
        {
            gui.HideAll();
            stimuli.Begin(Head.position, Yaw, trialRadiusM, 0f, 0f); // the probe plays straight ahead at the experiment radius
            float expected = acoustics.CurrentRt60 > 0f ? acoustics.CurrentRt60 : (reverb != null ? reverb.PresetRt60() : 1f);
            Rt60Result result = default;
            yield return rt60Meter.Measure(stimuli, acoustics, expected, r => result = r);
            done?.Invoke(result);
        }

        // ==== One trial ===========================================================================================

        IEnumerator RunTrial(TrialSpec spec, bool showPracticeHint, bool sphereAlways, bool feedback, string hudInfo)
        {
            gui.HideAll();
            gui.ShowHud(null);
            stimuli.HideAll();
            acoustics.SetCondition(spec.Condition, Head.position, Yaw);
            yield return new WaitForSeconds(interTrialSec);
            yield return WaitForHeadAlignment();

            // Origin = head position now; yaw reference = rig forward; the figure stands on the floor (rig origin height).
            stimuli.Begin(Head.position, Yaw, spec.RadiusM, spec.VisualAzDeg, spec.AudioStartAzDeg);
            stimuli.ShowVisual(true);
            if (sphereAlways) stimuli.ShowDebugSphere(spec.AudioStartAzDeg);
            stimuli.PlayAudio();
            if (showPracticeHint) gui.ShowHud("practice_hud");
            else if (hudInfo != null) gui.ShowHud(null, hudInfo);

            float az = spec.AudioStartAzDeg;
            float t0 = Time.time;
            while (true)
            {
                bool left = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch) > 0.5f;
                bool right = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) > 0.5f;
                az = PolarMath.StepAzimuth(az, left, right, audioRateDegPerSec, Time.deltaTime, azimuthLimitDeg);
                stimuli.SetAudioAzimuth(az);
                if (sphereAlways) stimuli.SyncDebugSphere();
                if (Time.time - t0 > confirmIgnoreSec && ConfirmPressed()) break;
                yield return null;
            }

            float duration = Time.time - t0;
            _lastFinalAz = az;
            stimuli.StopAudio();
            gui.ShowHud(null);
            _log.Trial(spec.ConditionName, spec.Index, spec.VisualAzDeg, spec.AudioStartAzDeg, az, duration,
                       stimuli.LevelDbfs, acoustics.BackgroundLevelDbfs, _realRun && spec.Condition != Condition.Practice ? _blockRt60 : float.NaN);

            if (feedback)
            {
                // Debug only: where the audio ended up and the signed difference (audio - visual); hidden again when the next trial begins.
                stimuli.ShowDebugSphere(az);
                float diff = az - spec.VisualAzDeg;
                gui.ShowHud(null, string.Format("Audio {0}{1:0.#} deg from the figure   (A: continue)", diff >= 0f ? "+" : "-", Mathf.Abs(diff)));
                yield return null; // let the confirming press pass
                while (!ConfirmPressed()) yield return null;
                gui.ShowHud(null);
            }
            stimuli.HideDebugSphere();
            stimuli.ShowVisual(false);
        }

        static bool ConfirmPressed() => OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);

        /// <summary>Wait until the head faces the rig's forward direction (within tolerance) for the hold time; shows the fixation dot.</summary>
        IEnumerator WaitForHeadAlignment()
        {
            gui.Show(gui.gatePanel);
            gui.gateHint.SetActive(false);
            float held = 0f, waited = 0f;
            while (held < gateHoldSec)
            {
                Vector3 fwd = Head.forward;
                Vector3 refFwd = Vector3.ProjectOnPlane(rig.transform.forward, Vector3.up).normalized;
                float yawErr = Vector3.SignedAngle(refFwd, Vector3.ProjectOnPlane(fwd, Vector3.up), Vector3.up);
                float pitch = Mathf.Asin(Mathf.Clamp(fwd.y, -1f, 1f)) * Mathf.Rad2Deg;
                bool aligned = Mathf.Abs(yawErr) <= gateYawToleranceDeg && Mathf.Abs(pitch) <= gatePitchToleranceDeg;
                held = aligned ? held + Time.deltaTime : 0f;
                waited += Time.deltaTime;
                if (!aligned && waited > gateHintAfterSec) gui.gateHint.SetActive(true);
                yield return null;
            }
            gui.HideAll();
        }

        /// <summary>Recenter so "forward" is where the participant faces now, then bring the GUI in front of them.</summary>
        IEnumerator RecenterAndPlaceGui()
        {
            gui.HideAll();
            OVRManager.display.RecenterPose();
            yield return new WaitForSeconds(0.5f);
            gui.PlaceAll();
        }

        static IEnumerator WaitForAny(Action<Button> result, params Button[] buttons)
        {
            Button clicked = null;
            var handlers = new UnityAction[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                handlers[i] = () => clicked = b;
                b.onClick.AddListener(handlers[i]);
            }
            while (clicked == null) yield return null;
            for (int i = 0; i < buttons.Length; i++) buttons[i].onClick.RemoveListener(handlers[i]);
            result(clicked);
        }

        /// <param name="existingCsv">File name of the CSV of a resumed session: rows are appended (no new header). If it is gone, a new file is started.</param>
        void OpenLog(string participantId, bool debug, string existingCsv = null)
        {
            string file = existingCsv;
            bool append = !string.IsNullOrEmpty(file) && File.Exists(Path.Combine(Application.persistentDataPath, file));
            if (!append) file = ExperimentLog.FileName(DateTime.Now, participantId, debug);
            string path = Path.Combine(Application.persistentDataPath, file);
            var writer = new StreamWriter(path, append, new UTF8Encoding(false)) { AutoFlush = true };
            _log = new ExperimentLog(writer, () => DateTime.Now, participantId, debug, line => Debug.Log("[HumPer] " + line), writeHeader: !append);
            Debug.Log("[HumPer] Logging to " + path + (append ? " (appending, resumed session)" : ""));
            if (!debug && _progress != null) _progress.CsvFile = file;
            _log.Event(append ? "session_resume" : "session_start", "app=" + Application.version + ";device=" + SystemInfo.deviceModel +
                                        ";stimulus_gain_boost_db=" + stimuli.GainBoostDb + ";stimulus_on_s=" + stimuli.audioSource.clip.length.ToString("0.##") + ";stimulus_gap_s=" + stimuli.gapSec);
        }
    }
}
