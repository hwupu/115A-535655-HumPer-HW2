using UnityEngine;

namespace HumPer
{
    /// <summary>
    /// The visual stimulus (box on the floor), the audio stimulus (Meta Spatial Audio building block, at head height) and the
    /// debug sphere that marks where the audio ended up. Both stimuli sit on a circle around the head position captured at trial
    /// start; azimuth 0 = rig forward, negative = left, positive = right. The audio radius stays fixed while it rotates.
    /// </summary>
    public class StimulusController : MonoBehaviour
    {
        public Transform visual;                 // box / cardboard figure, pivot at the feet
        public AudioSource audioSource;          // white noise (building block "Spatial Audio Stimuli")
        public MetaXRAudioSource audioMeta;      // for the gain boost
        public Transform debugSphere;            // shown after a trial in Debug mode only
        public AudioClip clip;
        [Range(0f, 20f)] public float gainBoostDb = 20f;

        Vector3 _origin;
        Quaternion _yaw = Quaternion.identity;
        float _radius = 1f;

        /// <summary>Level of the audio stimulus in the software chain: clip RMS (dBFS) + gain boost. Volume stays at unity.</summary>
        public float LevelDbfs { get; private set; } = -200f;

        void Awake()
        {
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.volume = 1f; // unity gain: loudness comes from the headset volume
            if (clip != null) audioSource.clip = clip;
            // The radius is locked, so no distance attenuation is wanted (practice 1.2 m vs trials 1.0 m must sound equally loud).
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 500f, 1f));
            if (audioMeta != null) audioMeta.GainBoostDb = gainBoostDb;
            LevelDbfs = ClipLevels.RmsDbfs(audioSource.clip) + gainBoostDb;
            HideAll();
        }

        /// <summary>Capture the trial origin (head position, yaw reference) and place both stimuli.</summary>
        public void Begin(Vector3 headPosition, Quaternion yawReference, float radiusM, float visualAzDeg, float audioAzDeg, float floorY)
        {
            _origin = headPosition;
            _yaw = yawReference;
            _radius = radiusM;

            var (vx, vz) = PolarMath.ToXZ(visualAzDeg, radiusM);
            Vector3 vp = _origin + _yaw * new Vector3(vx, 0f, vz);
            vp.y = floorY;
            visual.position = vp;
            Vector3 toOrigin = new Vector3(_origin.x - vp.x, 0f, _origin.z - vp.z);
            if (toOrigin.sqrMagnitude > 1e-6f) visual.rotation = Quaternion.LookRotation(toOrigin, Vector3.up); // face the participant

            SetAudioAzimuth(audioAzDeg);
        }

        public void SetAudioAzimuth(float azDeg)
        {
            var (x, z) = PolarMath.ToXZ(azDeg, _radius);
            audioSource.transform.position = _origin + _yaw * new Vector3(x, 0f, z);
        }

        public void ShowVisual(bool on) => visual.gameObject.SetActive(on);
        public void PlayAudio() => audioSource.Play();
        public void StopAudio() => audioSource.Stop();

        public void ShowDebugSphere(float audioAzDeg)
        {
            SetAudioAzimuth(audioAzDeg);
            debugSphere.position = audioSource.transform.position;
            debugSphere.gameObject.SetActive(true);
        }

        /// <summary>Keep a visible sphere on the audio stimulus while it rotates.</summary>
        public void SyncDebugSphere() => debugSphere.position = audioSource.transform.position;

        public void HideDebugSphere() => debugSphere.gameObject.SetActive(false);

        public void HideAll()
        {
            ShowVisual(false);
            HideDebugSphere();
            StopAudio();
        }
    }
}
