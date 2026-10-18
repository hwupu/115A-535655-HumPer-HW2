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

namespace HumPer
{
    /// <summary>
    /// Reverberation time of a shoebox room with one uniform reflection coefficient on all six surfaces (Eyring).
    /// Meta does not say whether its coefficient is a pressure (amplitude) or an energy coefficient: amplitude means absorption
    /// alpha = 1 - R^2, energy means alpha = 1 - R. The amplitude reading is the default; the two differ by a factor 2 in RT60.
    /// These are calculated values, not measurements.
    /// </summary>
    public static class RoomAcousticsMath
    {
        public static void BoxVolumeSurface(float width, float depth, float height, out float volume, out float surface)
        {
            volume = width * depth * height;
            surface = 2f * (width * depth + width * height + depth * height);
        }

        /// <summary>Reflection coefficient that gives the requested RT60 (seconds), limited to [0, 0.999].</summary>
        public static float ReflectionForRt60(float rt60Sec, float volumeM3, float surfaceM2, bool amplitude = true)
        {
            double k = amplitude ? 2.0 : 1.0;
            double r = Math.Exp(-0.161 * volumeM3 / (k * surfaceM2 * Math.Max(rt60Sec, 0.01f)));
            return (float)Math.Min(0.999, Math.Max(0.0, r));
        }

        public static float Rt60ForReflection(float reflection, float volumeM3, float surfaceM2, bool amplitude = true)
        {
            double k = amplitude ? 2.0 : 1.0;
            double r = Math.Min(0.999, Math.Max(1e-6, reflection));
            return (float)(0.161 * volumeM3 / (-k * surfaceM2 * Math.Log(r)));
        }
    }
}
