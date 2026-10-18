using System.Collections;
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
        [Tooltip("Silence between two plays of the clip.")]
        public float gapSec = 1f;
        /// <summary>Meta gain boost currently set on the stimulus' MetaXRAudioSource (inspector value; this script does not change it).</summary>
        public float GainBoostDb => audioMeta != null ? audioMeta.GainBoostDb : 0f;

        Vector3 _origin;
        Quaternion _yaw = Quaternion.identity;
        float _radius = 1f;
        float _visualAz, _audioAz;
        Coroutine _pulse;

        /// <summary>Level of the audio stimulus in the software chain: clip RMS (dBFS) + the Meta gain boost value. Volume stays at unity.</summary>
        public float LevelDbfs { get; private set; } = -200f;

        void Awake()
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false; // pulsed by PulseLoop: clip, then a pause, then the clip again
            audioSource.volume = 1f; // unity gain: loudness comes from the headset volume
            if (clip != null) audioSource.clip = clip;
            // The radius is locked, so no distance attenuation is wanted (practice 1.2 m vs trials 1.0 m must sound equally loud).
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 500f, 1f));
            LevelDbfs = ClipLevels.RmsDbfs(audioSource.clip) + GainBoostDb;
            HideAll();
        }

        /// <summary>Capture the trial origin (head position, yaw reference) and place both stimuli at head height on the circle.</summary>
        public void Begin(Vector3 headPosition, Quaternion yawReference, float radiusM, float visualAzDeg, float audioAzDeg)
        {
            _origin = headPosition;
            _yaw = yawReference;
            _radius = radiusM;
            _visualAz = visualAzDeg;

            var (vx, vz) = PolarMath.ToXZ(visualAzDeg, radiusM);
            Vector3 vp = _origin + _yaw * new Vector3(vx, 0f, vz);
            vp.y = _origin.y; // the cardboard is centered at eye (head) height, like the audio
            visual.position = vp;
            Vector3 toOrigin = new Vector3(_origin.x - vp.x, 0f, _origin.z - vp.z);
            if (toOrigin.sqrMagnitude > 1e-6f) visual.rotation = Quaternion.LookRotation(toOrigin, Vector3.up); // face the participant

            SetAudioAzimuth(audioAzDeg);
        }

        /// <summary>After a headset recenter: keep the same azimuths but rebuild the circle around the new head position / yaw.</summary>
        public void Replace(Vector3 headPosition, Quaternion yawReference)
        {
            Begin(headPosition, yawReference, _radius, _visualAz, _audioAz);
            if (debugSphere.gameObject.activeSelf) SyncDebugSphere();
        }

        public void SetAudioAzimuth(float azDeg)
        {
            _audioAz = azDeg;
            var (x, z) = PolarMath.ToXZ(azDeg, _radius);
            audioSource.transform.position = _origin + _yaw * new Vector3(x, 0f, z);
        }

        public void ShowVisual(bool on) => visual.gameObject.SetActive(on);
        /// <summary>Start the pulse train: the clip (2 s) plays, then <see cref="gapSec"/> of silence, repeatedly until <see cref="StopAudio"/>.</summary>
        public void PlayAudio()
        {
            StopAudio();
            _pulse = StartCoroutine(PulseLoop());
        }

        IEnumerator PulseLoop()
        {
            var cycle = new WaitForSeconds(audioSource.clip.length + gapSec); // scaled time: pauses with the cancel dialog
            while (true)
            {
                audioSource.Play();
                yield return cycle;
            }
        }
        public void StopAudio()
        {
            if (_pulse != null) { StopCoroutine(_pulse); _pulse = null; }
            audioSource.Stop();
        }

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
