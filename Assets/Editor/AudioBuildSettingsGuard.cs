using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HumPer.EditorTools
{
    /// <summary>
    /// Pins the audio settings of every build to the values in the repository. Meta's Project Setup Tool task "The Spatial Audio Building
    /// Block requires the audio settings to be updated" rewrites the live AudioManager when the Editor opens (DSP buffer 256 and the sample
    /// rate of the Editor machine's current output device, e.g. 96000 Hz), and the build picks that up without the ProjectSettings file
    /// showing a change. An APK built that way had no audible reverberation, while the same commit built on another machine did.
    /// Running last, this step puts the committed values back before the player data is written.
    /// </summary>
    class AudioBuildSettingsGuard : IPreprocessBuildWithReport
    {
        const string AudioManagerAssetPath = "ProjectSettings/AudioManager.asset";

        // Same as ProjectSettings/AudioManager.asset in git: system default sample rate (Quest: 48 kHz), default DSP buffer.
        const int SampleRate = 0;
        const int DspBufferSize = 1024;
        const int RequestedDspBufferSize = 0;

        public int callbackOrder => int.MaxValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(AudioManagerAssetPath);
            if (objects == null || objects.Length == 0) { Debug.LogWarning("[HumPer] AudioManager not found, audio settings not pinned"); return; }

            var audioManager = new SerializedObject(objects[0]);
            var sampleRate = audioManager.FindProperty("m_SampleRate");
            var dspBuffer = audioManager.FindProperty("m_DSPBufferSize");
            var requestedBuffer = audioManager.FindProperty("m_RequestedDSPBufferSize");
            string before = "sample rate " + sampleRate.intValue + ", DSP buffer " + dspBuffer.intValue + ", requested " + requestedBuffer.intValue;

            sampleRate.intValue = SampleRate;
            dspBuffer.intValue = DspBufferSize;
            requestedBuffer.intValue = RequestedDspBufferSize;
            audioManager.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[HumPer] Audio settings pinned for the build: sample rate " + SampleRate + ", DSP buffer " + DspBufferSize + ", requested " + RequestedDspBufferSize +
                      " (Editor had: " + before + ")");
        }
    }
}
