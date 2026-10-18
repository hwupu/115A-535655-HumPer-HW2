using UnityEngine;

namespace HumPer
{
    /// <summary>
    /// Switches the acoustic condition. Every condition is rendered in the Meta XR Audio shoebox room (the stimulus and the backgrounds
    /// have Enable Acoustics on; the room is rendered by the mixer effect "Meta XR Audio Reflection"). The conditions differ in the
    /// reverb time and the background: Quiet RT60 0.2 s, Noisy 0.7 s plus the three background recordings, Reverb 2.2 s with the
    /// background still playing (see <see cref="ConditionAcoustics"/>). The backgrounds are spatialized at eye level, 2 m from the head
    /// (left, right, back), loop together at unity gain and stay fixed in the room once placed. The only software gain is the
    /// stimulus' Meta gain boost.
    /// </summary>
    public class AcousticConditions : MonoBehaviour
    {
        /// <summary>How the reverb time is chosen: by condition (experiment), a Debug custom value, or Meta's material preset.</summary>
        public enum RtMode { ConditionDefault, Custom, Preset }

        public MetaXRAudioSource stimulusMeta;
        public AudioSource[] backgroundSources = new AudioSource[0];
        public MetaXRAudioSource[] backgroundMeta = new MetaXRAudioSource[0];
        public AudioClip[] backgroundClips = new AudioClip[0];
        public float[] backgroundAzimuthsDeg = { -90f, 90f, 180f };
        public float backgroundRadiusM = 2f;
        public ReverbTuner reverb;

        public Condition Current { get; private set; } = Condition.Quiet;

        /// <summary>Debug bench: play the backgrounds whatever the condition is.</summary>
        public bool ForceBackground { get; private set; }

        /// <summary>Debug bench: override the condition's reverb time (custom value or Meta's material preset).</summary>
        public RtMode Mode { get; private set; } = RtMode.ConditionDefault;
        public float OverrideRt60 { get; private set; }

        /// <summary>Reverb time (s) in effect for the current condition; 0 = Meta material preset.</summary>
        public float CurrentRt60 { get; private set; }

        /// <summary>True while the background recordings are playing.</summary>
        public bool BackgroundPlaying => _backgroundOn;

        float _backgroundDbfs = -200f;
        bool _backgroundOn;
        Vector3[] _fixedPositions;

        /// <summary>Combined background level in the software chain (dBFS, power sum of the clips); -200 when not playing.</summary>
        public float BackgroundLevelDbfs => _backgroundOn ? _backgroundDbfs : -200f;

        void Awake()
        {
            var levels = new float[backgroundSources.Length];
            for (int i = 0; i < backgroundSources.Length; i++)
            {
                var src = backgroundSources[i];
                src.playOnAwake = false;
                src.loop = true;
                src.volume = 1f; // unity gain
                src.rolloffMode = AudioRolloffMode.Custom;
                src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 500f, 1f));
                if (i < backgroundClips.Length && backgroundClips[i] != null) src.clip = backgroundClips[i];
                levels[i] = ClipLevels.RmsDbfs(src.clip);
            }
            _backgroundDbfs = AudioLevelMath.PowerSumDb(levels);
        }

        /// <summary>Apply a condition: reverb time, and the backgrounds (placed around the given head position/yaw when they start).</summary>
        public void SetCondition(Condition condition, Vector3 headPosition, Quaternion yaw)
        {
            Current = condition;
            if (stimulusMeta != null) stimulusMeta.EnableAcoustics = true;
            foreach (var meta in backgroundMeta) if (meta != null) meta.EnableAcoustics = true; // everything is in the room

            ApplyReverb(condition);

            bool background = ConditionAcoustics.HasBackground(condition) || ForceBackground;
            if (background && !_backgroundOn) PlaceBackground(headPosition, yaw);
            foreach (var s in backgroundSources)
            {
                if (background) { if (s.clip != null && !s.isPlaying) s.Play(); }
                else s.Stop();
            }
            _backgroundOn = background;
            Debug.Log("[HumPer] Acoustic condition: " + condition + ", reverb " + (CurrentRt60 > 0f ? CurrentRt60.ToString("0.0#") + " s (calculated)" : "Meta preset") +
                      (background ? ", background " + _backgroundDbfs.ToString("0.0") + " dBFS" : ", no background"));
        }

        void ApplyReverb(Condition condition)
        {
            switch (Mode)
            {
                case RtMode.Preset:
                    CurrentRt60 = 0f;
                    if (reverb != null) reverb.UsePreset();
                    break;
                case RtMode.Custom:
                    CurrentRt60 = OverrideRt60;
                    if (reverb != null) reverb.SetRt60(OverrideRt60);
                    break;
                default:
                    CurrentRt60 = ConditionAcoustics.Rt60(condition);
                    if (reverb != null) reverb.SetRt60(CurrentRt60);
                    break;
            }
        }

        /// <summary>Debug bench: reverb time override (seconds, or the Meta preset), applied to the current condition at once.</summary>
        public void SetReverbOverride(RtMode mode, float seconds, Vector3 headPosition, Quaternion yaw)
        {
            Mode = mode;
            OverrideRt60 = seconds;
            SetCondition(Current, headPosition, yaw);
        }

        /// <summary>Debug bench: switch condition and drop any reverb override (back to the condition's own reverb time).</summary>
        public void SetConditionDefault(Condition condition, Vector3 headPosition, Quaternion yaw)
        {
            Mode = RtMode.ConditionDefault;
            SetCondition(condition, headPosition, yaw);
        }

        public void SetBackgroundForced(bool on, Vector3 headPosition, Quaternion yaw)
        {
            ForceBackground = on;
            SetCondition(Current, headPosition, yaw);
        }

        /// <summary>Stop the backgrounds and clear the Debug overrides.</summary>
        public void StopAll()
        {
            _backgroundOn = false;
            ForceBackground = false;
            Mode = RtMode.ConditionDefault;
            foreach (var s in backgroundSources) s.Stop();
        }

        /// <summary>
        /// Placed once when the background starts and then fixed in the room: the backgrounds do not follow the head (turning the head
        /// turns the sounds the other way) and are not moved by a recenter. The positions are re-applied every frame so that nothing
        /// else can drag the sources along.
        /// </summary>
        void PlaceBackground(Vector3 headPosition, Quaternion yaw)
        {
            _fixedPositions = new Vector3[backgroundSources.Length];
            for (int i = 0; i < backgroundSources.Length; i++)
            {
                float az = i < backgroundAzimuthsDeg.Length ? backgroundAzimuthsDeg[i] : 0f;
                var (x, z) = PolarMath.ToXZ(az, backgroundRadiusM);
                _fixedPositions[i] = headPosition + yaw * new Vector3(x, 0f, z);
                backgroundSources[i].transform.position = _fixedPositions[i];
            }
        }

        void LateUpdate()
        {
            if (!_backgroundOn || _fixedPositions == null) return;
            for (int i = 0; i < backgroundSources.Length && i < _fixedPositions.Length; i++)
            {
                var t = backgroundSources[i].transform;
                if ((t.position - _fixedPositions[i]).sqrMagnitude > 1e-8f) t.position = _fixedPositions[i];
            }
        }

        /// <summary>Where the backgrounds are relative to the listener and whether the spatializer is on, for the CSV (condition_begin note).</summary>
        public string DescribeBackground(Transform listener)
        {
            var sb = new System.Text.StringBuilder("bg");
            for (int i = 0; i < backgroundSources.Length; i++)
            {
                Vector3 rel = listener.InverseTransformPoint(backgroundSources[i].transform.position);
                sb.Append(i == 0 ? ":" : "|").Append("x").Append(rel.x.ToString("0.0")).Append("y").Append(rel.y.ToString("0.0")).Append("z").Append(rel.z.ToString("0.0"))
                  .Append(backgroundSources[i].spatialize ? "S" : "-");
            }
            return sb.ToString();
        }
    }
}
