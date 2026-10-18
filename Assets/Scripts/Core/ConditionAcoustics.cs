namespace HumPer
{
    /// <summary>
    /// Acoustics of each condition (protocol of 2026-10-18): Quiet = room with RT60 0.2 s, no background; Noisy = RT60 0.7 s with the
    /// three background recordings; Reverb = RT60 2.2 s, the background keeps playing. Practice uses the Quiet acoustics.
    /// The RT60 values are calculated targets (see <see cref="RoomAcousticsMath"/>), not measurements.
    /// </summary>
    public static class ConditionAcoustics
    {
        public const float QuietRt60 = 0.2f;
        public const float NoisyRt60 = 0.7f;
        public const float ReverbRt60 = 2.2f;

        public static float Rt60(Condition condition)
        {
            switch (condition)
            {
                case Condition.Noisy: return NoisyRt60;
                case Condition.Reverb: return ReverbRt60;
                default: return QuietRt60; // Quiet and Practice
            }
        }

        public static bool HasBackground(Condition condition) => condition == Condition.Noisy || condition == Condition.Reverb;
    }
}
