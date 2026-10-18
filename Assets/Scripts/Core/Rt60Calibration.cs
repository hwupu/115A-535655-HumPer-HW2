using System;

namespace HumPer
{
    /// <summary>
    /// Maps a target reverberation time to the uniform reflection coefficient to send to Meta XR Audio, from a MEASURED relation.
    /// Eyring's formula (<see cref="RoomAcousticsMath"/>) did not predict what the plugin renders (target 0.7 s measured 1.05 s,
    /// 2.2 s measured 2.7 s, 0.2 s measured 0.5 s), so the relation was measured: for each coefficient an impulse was played through the
    /// whole chain (Meta spatializer + reflection mixer effect) in a 8.72 x 3.5 x 15 m room, the source 2 m ahead of the listener, and
    /// T30 was evaluated with <see cref="Rt60Estimator"/> (Windows plugin, 2026-10-18). For other room sizes the target is scaled by
    /// V/S (the reverberation time of a shoebox at a fixed coefficient is proportional to V/S). Re-check on the headset with the Debug
    /// bench "Measure RT60"; the Android plugin is built from the same code but was not the one measured here.
    /// Below about 0.3 s the decay is no longer a clean exponential (the direct sound and the first reflections dominate), so 0.2 s
    /// cannot be reached: the lowest table entry (0.30 s measured) is used.
    /// </summary>
    public static class Rt60Calibration
    {
        // coefficient (ascending) -> measured T30 in seconds (ascending)
        static readonly float[] Reflection = { 0.50f, 0.60f, 0.65f, 0.70f, 0.75f, 0.80f, 0.85f, 0.88f, 0.90f, 0.92f, 0.94f, 0.95f, 0.96f, 0.97f, 0.975f, 0.98f, 0.985f, 0.99f };
        static readonly float[] MeasuredRt60 = { 0.30f, 0.33f, 0.37f, 0.42f, 0.51f, 0.65f, 0.89f, 1.10f, 1.30f, 1.58f, 1.89f, 2.11f, 2.49f, 3.05f, 3.52f, 3.94f, 4.81f, 5.74f };

        /// <summary>V/S of the room used for the measurement (8.72 x 3.5 x 15 m).</summary>
        public const float CalibrationVolumeOverSurface = 457.8f / 427.64f;

        public static float LowestRt60 => MeasuredRt60[0];

        public static float ReflectionForRt60(float rt60Sec, float volumeM3, float surfaceM2)
        {
            float scaled = rt60Sec * CalibrationVolumeOverSurface / (volumeM3 / surfaceM2); // the equivalent time in the calibration room
            if (scaled <= MeasuredRt60[0]) return Reflection[0];
            int last = MeasuredRt60.Length - 1;
            if (scaled >= MeasuredRt60[last]) return Reflection[last];
            for (int i = 1; i <= last; i++)
            {
                if (scaled > MeasuredRt60[i]) continue;
                float t = (float)((Math.Log(scaled) - Math.Log(MeasuredRt60[i - 1])) / (Math.Log(MeasuredRt60[i]) - Math.Log(MeasuredRt60[i - 1])));
                return Reflection[i - 1] + t * (Reflection[i] - Reflection[i - 1]);
            }
            return Reflection[last];
        }

        /// <summary>Reverberation time (measured scale) expected for a coefficient in a room of the given size.</summary>
        public static float Rt60ForReflection(float reflection, float volumeM3, float surfaceM2)
        {
            int last = Reflection.Length - 1;
            float inCalibrationRoom;
            if (reflection <= Reflection[0]) inCalibrationRoom = MeasuredRt60[0];
            else if (reflection >= Reflection[last]) inCalibrationRoom = MeasuredRt60[last];
            else
            {
                int i = 1;
                while (reflection > Reflection[i]) i++;
                float t = (reflection - Reflection[i - 1]) / (Reflection[i] - Reflection[i - 1]);
                inCalibrationRoom = (float)Math.Exp(Math.Log(MeasuredRt60[i - 1]) + t * (Math.Log(MeasuredRt60[i]) - Math.Log(MeasuredRt60[i - 1])));
            }
            return inCalibrationRoom * (volumeM3 / surfaceM2) / CalibrationVolumeOverSurface;
        }
    }
}
