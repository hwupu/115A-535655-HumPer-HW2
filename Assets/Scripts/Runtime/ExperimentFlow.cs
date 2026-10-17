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

        [Header("Practice trial geometry (fixed, different from the experiment trials)")]
        public float practiceRadiusM = 1.2f;
        public float practiceVisualAzDeg = 0f;
        public float practiceAudioStartAzDeg = 30f;

        [Header("Experiment trial geometry")]
        public float trialRadiusM = 1f;
        public float[] visualPositionsDeg = { -37.5f, -22.5f, -7.5f, 7.5f, 22.5f, 37.5f };
        public float audioStartDeg = 60f; // +/-60, alternating

        [Header("Task")]
        public float audioRateDegPerSec = 5f;
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
            gui.Show(gui.menu.gameObject);
        }

        void OnDestroy()
        {
            if (gui != null && gui.menu != null) gui.menu.Chosen -= OnChosen;
            _log?.Dispose();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) _log?.Event("app_paused");
        }

        void OnChosen(string participantId, bool debug)
        {
            if (_running) return;
            _running = true;
            StartCoroutine(debug ? RunDebug(participantId) : Run(participantId));
        }

        // ==== Start experiment ====================================================================================

        IEnumerator Run(string participantId)
        {
            OpenLog(participantId, false);
            yield return RecenterAndPlaceGui();

            // ---- language ----
            gui.Show(gui.languagePanel);
            Button picked = null;
            yield return WaitForAny(b => picked = b, gui.chineseButton, gui.englishButton);
            Localization.Set(picked == gui.chineseButton ? Language.ChineseTaiwan : Language.English);
            _log.Event("language", Localization.Current.ToString());

            // ---- practice instructions ----
            gui.Show(gui.introPanel);
            yield return WaitForAny(_ => { }, gui.startPracticeButton);

            // ---- practice trials, as many as the participant likes ----
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

            gui.Show(gui.donePanel); // temporary: the three experiment conditions come next
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

            _log.Event("debug_begin");
            panel.SetCondition(condition);
            panel.SetSphereAlways(sphereAlways);
            panel.SetStatus("Pick a condition, then run a trial.");
            acoustics.SetCondition(condition, Head.position, Yaw);

            while (true)
            {
                gui.Show(panel.gameObject);
                Button clicked = null;
                yield return WaitForAny(b => clicked = b, panel.quietButton, panel.reverbButton, panel.noisyButton,
                                        panel.sphereToggleButton, panel.runTrialButton, panel.practiceTrialButton, panel.backButton);

                if (clicked == panel.quietButton || clicked == panel.reverbButton || clicked == panel.noisyButton)
                {
                    condition = clicked == panel.quietButton ? Condition.Quiet : clicked == panel.reverbButton ? Condition.Reverb : Condition.Noisy;
                    panel.SetCondition(condition);
                    acoustics.SetCondition(condition, Head.position, Yaw); // audible right away (e.g. the background in Noisy)
                    _log.ConditionEvent("condition", condition.ToString().ToLowerInvariant());
                }
                else if (clicked == panel.sphereToggleButton)
                {
                    sphereAlways = !sphereAlways;
                    panel.SetSphereAlways(sphereAlways);
                }
                else if (clicked == panel.runTrialButton || clicked == panel.practiceTrialButton)
                {
                    TrialSpec spec;
                    if (clicked == panel.practiceTrialButton)
                        spec = new TrialSpec(condition, ++number, practiceAudioStartAzDeg, practiceVisualAzDeg, practiceRadiusM);
                    else
                    {
                        // experiment-style geometry: random visual position (never 0), audio starts at +60 / -60 alternating
                        float visual = visualPositionsDeg[rng.Next(visualPositionsDeg.Length)];
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
            _running = false;
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
            stimuli.Begin(Head.position, Yaw, spec.RadiusM, spec.VisualAzDeg, spec.AudioStartAzDeg, rig.transform.position.y);
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
                       stimuli.LevelDbfs, acoustics.BackgroundLevelDbfs);

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

        void OpenLog(string participantId, bool debug)
        {
            var now = DateTime.Now;
            string path = Path.Combine(Application.persistentDataPath, ExperimentLog.FileName(now, participantId, debug));
            var writer = new StreamWriter(path, false, new UTF8Encoding(false)) { AutoFlush = true };
            _log = new ExperimentLog(writer, () => DateTime.Now, participantId, debug, line => Debug.Log("[HumPer] " + line));
            Debug.Log("[HumPer] Logging to " + path);
            _log.Event("session_start", "app=" + Application.version + ";device=" + SystemInfo.deviceModel +
                                        ";stimulus_gain_boost_db=" + stimuli.gainBoostDb);
        }
    }
}
