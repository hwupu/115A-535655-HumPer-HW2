using System;

namespace HumPer
{
    public static class PolarMath
    {
        /// <summary>Position in the head-yaw frame (x = right, z = forward) for an azimuth (0 = ahead, negative = left) and radius.</summary>
        public static (float x, float z) ToXZ(float azDeg, float radius)
        {
            float a = azDeg * (float)Math.PI / 180f;
            return (radius * (float)Math.Sin(a), radius * (float)Math.Cos(a));
        }

        /// <summary>Advance azimuth: left held = -rate, right held = +rate (both = no motion). Clamped to +/-limit.</summary>
        public static float StepAzimuth(float azDeg, bool leftHeld, bool rightHeld, float rateDegPerSec, float dt, float limitDeg = 90f)
        {
            float dir = (rightHeld ? 1f : 0f) - (leftHeld ? 1f : 0f);
            float next = azDeg + dir * rateDegPerSec * dt;
            return Math.Max(-limitDeg, Math.Min(limitDeg, next));
        }
    }

    public static class AudioLevelMath
    {
        /// <summary>RMS level in dBFS (0 dBFS = RMS of 1.0). Returns -200 for silence.</summary>
        public static float RmsDbfs(float[] samples)
        {
            if (samples == null || samples.Length == 0) return -200f;
            double sum = 0;
            for (int i = 0; i < samples.Length; i++) sum += (double)samples[i] * samples[i];
            double rms = Math.Sqrt(sum / samples.Length);
            return rms <= 1e-10 ? -200f : (float)(20.0 * Math.Log10(rms));
        }

        /// <summary>Combined level of uncorrelated sources: 10*log10(sum of 10^(L/10)). Silent (-200) sources are ignored.</summary>
        public static float PowerSumDb(params float[] levelsDb)
        {
            double sum = 0;
            foreach (float l in levelsDb) if (l > -199f) sum += Math.Pow(10.0, l / 10.0);
            return sum <= 0 ? -200f : (float)(10.0 * Math.Log10(sum));
        }
    }
}
