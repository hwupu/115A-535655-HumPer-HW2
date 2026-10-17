namespace HumPer
{
    /// <summary>Acoustic condition. Practice behaves like Quiet (dry room, no background).</summary>
    public enum Condition { Practice, Quiet, Reverb, Noisy }

    /// <summary>One trial's stimulus configuration. Azimuth: 0 = straight ahead, negative = left, positive = right.</summary>
    public readonly struct TrialSpec
    {
        public readonly Condition Condition;
        public readonly int Index;          // 1-based
        public readonly float AudioStartAzDeg;
        public readonly float VisualAzDeg;
        public readonly float RadiusM;      // distance of both stimuli; the audio stays on this circle while it rotates

        public TrialSpec(Condition condition, int index, float audioStartAzDeg, float visualAzDeg, float radiusM)
        {
            Condition = condition;
            Index = index;
            AudioStartAzDeg = audioStartAzDeg;
            VisualAzDeg = visualAzDeg;
            RadiusM = radiusM;
        }

        public string ConditionName => Condition.ToString().ToLowerInvariant();
    }
}
