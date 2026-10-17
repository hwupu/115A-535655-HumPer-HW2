using UnityEngine;

namespace HumPer
{
    public static class ClipLevels
    {
        /// <summary>RMS level of a clip in dBFS (needs Decompress On Load); -200 if unavailable.</summary>
        public static float RmsDbfs(AudioClip clip)
        {
            if (clip == null) return -200f;
            if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();
            var data = new float[clip.samples * clip.channels];
            return clip.GetData(data, 0) ? AudioLevelMath.RmsDbfs(data) : -200f;
        }
    }
}
