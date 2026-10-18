using System;

namespace HumPer
{
    public struct Rt60Result
    {
        public bool Valid;
        /// <summary>Reverberation time from the -5..-25 dB part of the decay, extrapolated to 60 dB (seconds).</summary>
        public float Rt60T20;
        /// <summary>Same from -5..-35 dB (NaN if the recording did not decay that far).</summary>
        public float Rt60T30;
        /// <summary>Depth of the decay curve in the recording (dB below the start).</summary>
        public float DynamicRangeDb;
        public string Reason;

        public override string ToString() =>
            Valid ? "T20 " + Rt60T20.ToString("0.00") + " s, T30 " + (float.IsNaN(Rt60T30) ? "n/a" : Rt60T30.ToString("0.00") + " s") + ", range " + DynamicRangeDb.ToString("0") + " dB"
                  : "invalid (" + Reason + ")";
    }

    /// <summary>
    /// Reverberation time from a recorded impulse response (ISO 3382 style): Schroeder backward integration of the squared signal,
    /// then a straight-line fit to the decay between -5 dB and -25 dB (T20) or -35 dB (T30), extrapolated to -60 dB.
    /// </summary>
    public static class Rt60Estimator
    {
        /// <param name="energy">Squared samples (summed over the channels).</param>
        /// <param name="start">Index of the impulse (the direct sound).</param>
        /// <param name="sampleRate">Samples per second of <paramref name="energy"/>.</param>
        public static Rt60Result Estimate(float[] energy, int start, int count, float sampleRate)
        {
            var result = new Rt60Result { Rt60T20 = float.NaN, Rt60T30 = float.NaN, Reason = "" };
            if (energy == null || start < 0 || count < 100 || start + count > energy.Length) { result.Reason = "no data"; return result; }

            // remove the noise floor (mean energy of the last tenth) so that the integral does not flatten the curve
            int tail = Math.Max(10, count / 10);
            double floor = 0;
            for (int i = count - tail; i < count; i++) floor += energy[start + i];
            floor /= tail;

            // the recording must have decayed well before its end, otherwise the floor estimate would eat the real decay
            int headCount = Math.Max(10, (int)(0.02f * sampleRate));
            double head = 0;
            for (int i = 0; i < headCount && i < count; i++) head += energy[start + i];
            head /= headCount;
            if (head <= 0 || floor > head * 1e-3) { result.Reason = "recording ends before the decay is complete"; return result; }

            var edc = new double[count];
            double sum = 0;
            for (int i = count - 1; i >= 0; i--)
            {
                sum += Math.Max(0.0, energy[start + i] - floor);
                edc[i] = sum;
            }
            if (edc[0] <= 0) { result.Reason = "silent"; return result; }

            var db = new double[count];
            for (int i = 0; i < count; i++) db[i] = edc[i] <= 0 ? double.NegativeInfinity : 10.0 * Math.Log10(edc[i] / edc[0]);

            int i5 = IndexBelow(db, -5.0), i25 = IndexBelow(db, -25.0), i35 = IndexBelow(db, -35.0);
            double minDb = 0;
            for (int i = 0; i < count; i++) if (!double.IsNegativeInfinity(db[i]) && db[i] < minDb) minDb = db[i];
            result.DynamicRangeDb = (float)-minDb;

            if (i5 < 0 || i25 < 0 || i25 <= i5 + 2) { result.Reason = "decay does not reach -25 dB (range " + (-minDb).ToString("0") + " dB)"; return result; }
            result.Rt60T20 = Fit(db, i5, i25, sampleRate);
            if (i35 > i5 + 2) result.Rt60T30 = Fit(db, i5, i35, sampleRate);
            result.Valid = !float.IsNaN(result.Rt60T20) && result.Rt60T20 > 0f;
            if (!result.Valid) result.Reason = "fit failed";
            return result;
        }

        static int IndexBelow(double[] db, double level)
        {
            for (int i = 0; i < db.Length; i++) if (db[i] <= level) return i;
            return -1;
        }

        /// <summary>Least-squares line through the decay curve between two indices; returns the time for 60 dB of decay.</summary>
        static float Fit(double[] db, int from, int to, float sampleRate)
        {
            double n = to - from + 1, sx = 0, sy = 0, sxx = 0, sxy = 0;
            for (int i = from; i <= to; i++)
            {
                double t = i / (double)sampleRate;
                sx += t; sy += db[i]; sxx += t * t; sxy += t * db[i];
            }
            double denom = n * sxx - sx * sx;
            if (Math.Abs(denom) < 1e-12) return float.NaN;
            double slope = (n * sxy - sx * sy) / denom; // dB per second (negative)
            return slope < 0 ? (float)(-60.0 / slope) : float.NaN;
        }
    }
}
