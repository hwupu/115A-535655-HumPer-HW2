using System;
using System.Collections;
using UnityEngine;

namespace HumPer
{
    /// <summary>
    /// Measures the reverberation time of the rendered audio. It sits on the AudioListener's GameObject, where OnAudioFilterRead sees the
    /// final mix (after the Meta spatializer and the mixer with the reflection effect). Procedure: the backgrounds are paused and the
    /// stimulus stopped, the stimulus source plays a single-sample impulse, the output is recorded for a few seconds and the decay is
    /// evaluated with <see cref="Rt60Estimator"/> (Schroeder backward integration, T20 / T30). The measurement is of the whole chain as
    /// the participant would hear it (both ears summed), so it also includes the direct sound and the early reflections.
    /// </summary>
    public class Rt60Meter : MonoBehaviour
    {
        [Range(0.05f, 1f)] public float impulseAmplitude = 0.5f;
        public float recordSeconds = 3.5f;
        [Tooltip("Silence before the impulse, so the tail of the previous sound has died away (at least this and 1.2 x the reverb time).")]
        public float settleSeconds = 1f;

        float[] _energy = new float[0];
        int _write;
        volatile bool _recording;
        int _sampleRate = 48000;
        AudioClip _impulse;

        public bool Measuring { get; private set; }

        void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            _energy = new float[(int)(_sampleRate * (recordSeconds + 1.5f))];
        }

        // Audio thread: only copies the squared output into a preallocated buffer.
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_recording) return;
            int frames = data.Length / channels;
            int w = _write;
            for (int f = 0; f < frames && w < _energy.Length; f++, w++)
            {
                float e = 0f;
                for (int c = 0; c < channels; c++) { float s = data[f * channels + c]; e += s * s; }
                _energy[w] = e;
            }
            _write = w;
        }

        /// <summary>Run one measurement at the current acoustic settings. <paramref name="done"/> receives the result.</summary>
        public IEnumerator Measure(StimulusController stimuli, AcousticConditions acoustics, float expectedRt60, Action<Rt60Result> done)
        {
            Measuring = true;
            var needed = (int)(_sampleRate * (recordSeconds + 1.5f));
            if (_energy.Length != needed) _energy = new float[needed];
            if (_impulse == null) _impulse = MakeImpulse(_sampleRate, impulseAmplitude);

            acoustics.PauseBackground(true);
            stimuli.StopAudio();
            yield return new WaitForSeconds(Mathf.Max(settleSeconds, 1.2f * expectedRt60));

            _write = 0;
            _recording = true;
            yield return null;
            stimuli.PlayProbe(_impulse);
            yield return new WaitForSeconds(recordSeconds);
            _recording = false;
            stimuli.EndProbe();
            acoustics.PauseBackground(false);

            yield return null;
            int recorded = _write;
            // the impulse is the loudest event in the first part of the recording
            int peak = 0;
            float max = 0f;
            int search = Mathf.Min(recorded, (int)(_sampleRate * 1.5f));
            for (int i = 0; i < search; i++) if (_energy[i] > max) { max = _energy[i]; peak = i; }
            var result = max <= 0f ? new Rt60Result { Reason = "no signal recorded", Rt60T20 = float.NaN, Rt60T30 = float.NaN }
                                   : Rt60Estimator.Estimate(_energy, peak, recorded - peak, _sampleRate);
            Debug.Log("[HumPer] RT60 measurement: " + result + " (target " + expectedRt60.ToString("0.0#") + " s)");
            Measuring = false;
            done?.Invoke(result);
        }

        static AudioClip MakeImpulse(int sampleRate, float amplitude)
        {
            // 200 ms with a single non-zero sample after 100 ms: the spatializer fades a new voice in over its first buffers, which would swallow an impulse at the very start
            var samples = new float[sampleRate / 5];
            samples[sampleRate / 10] = amplitude;
            var clip = AudioClip.Create("rt60_impulse", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
