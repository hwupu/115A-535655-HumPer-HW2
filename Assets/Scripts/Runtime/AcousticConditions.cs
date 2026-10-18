using UnityEngine;

namespace HumPer
{
    /// <summary>
    /// Switches the acoustic condition. Practice and Quiet are dry (no room reflections, no background); Reverb sends the stimulus
    /// into the Meta XR Audio shoebox room (EnableAcoustics); Noisy is dry plus three spatialized background recordings (left, right,
    /// back; eye level; 2 m from the head) looping together at unity gain. The only software gain is the stimulus' Meta gain boost.
    /// </summary>
    public class AcousticConditions : MonoBehaviour
    {
        public MetaXRAudioSource stimulusMeta;
        public AudioSource[] backgroundSources = new AudioSource[0];
        public MetaXRAudioSource[] backgroundMeta = new MetaXRAudioSource[0];
        public AudioClip[] backgroundClips = new AudioClip[0];
        public float[] backgroundAzimuthsDeg = { -90f, 90f, 180f };
        public float backgroundRadiusM = 2f;

        public Condition Current { get; private set; } = Condition.Quiet;

        float _backgroundDbfs = -200f;
        bool _backgroundOn;

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
                if (i < backgroundMeta.Length && backgroundMeta[i] != null) backgroundMeta[i].EnableAcoustics = false; // background is always dry
            }
            _backgroundDbfs = AudioLevelMath.PowerSumDb(levels);
        }

        /// <summary>Apply a condition. For Noisy, the backgrounds are placed around the given head position/yaw and start playing.</summary>
        public void SetCondition(Condition condition, Vector3 headPosition, Quaternion yaw)
        {
            Current = condition;
            if (stimulusMeta != null) stimulusMeta.EnableAcoustics = condition == Condition.Reverb;

            bool noisy = condition == Condition.Noisy;
            if (noisy && !_backgroundOn) PlaceBackground(headPosition, yaw);
            foreach (var s in backgroundSources)
            {
                if (noisy) { if (s.clip != null && !s.isPlaying) s.Play(); }
                else s.Stop();
            }
            _backgroundOn = noisy;
            Debug.Log("[HumPer] Acoustic condition: " + condition + (condition == Condition.Reverb ? " (room reverb on)" : " (dry)") +
                      (noisy ? ", background " + _backgroundDbfs.ToString("0.0") + " dBFS" : ""));
        }

        /// <summary>After a headset recenter: move the running backgrounds around the new head position / yaw.</summary>
        public void Replace(Vector3 headPosition, Quaternion yaw)
        {
            if (_backgroundOn) PlaceBackground(headPosition, yaw);
        }

        public void StopAll()
        {
            _backgroundOn = false;
            foreach (var s in backgroundSources) s.Stop();
        }

        void PlaceBackground(Vector3 headPosition, Quaternion yaw)
        {
            for (int i = 0; i < backgroundSources.Length; i++)
            {
                float az = i < backgroundAzimuthsDeg.Length ? backgroundAzimuthsDeg[i] : 0f;
                var (x, z) = PolarMath.ToXZ(az, backgroundRadiusM);
                backgroundSources[i].transform.position = headPosition + yaw * new Vector3(x, 0f, z);
            }
        }
    }
}
