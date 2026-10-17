using System;
using System.Collections.Generic;

namespace HumPer
{
    /// <summary>
    /// Trial list for one condition (6 trials). Audio starts alternate +60 / -60 degrees. The visual stimulus side follows
    /// left, left, right, right, left, right; each side's three positions are used exactly once per condition (shuffled with a seeded
    /// RNG), so 0 degrees is never used and the positions are counterbalanced.
    /// </summary>
    public static class TrialPlanner
    {
        /// <summary>Every participant goes through the conditions in this order.</summary>
        public static readonly Condition[] ConditionOrder = { Condition.Quiet, Condition.Reverb, Condition.Noisy };

        /// <summary>true = the visual stimulus is on the right (positive azimuth).</summary>
        public static readonly bool[] VisualRightSequence = { false, false, true, true, false, true };

        public static List<TrialSpec> PlanCondition(Condition condition, int seed, float[] leftPoolDeg, float[] rightPoolDeg,
                                                    float audioStartDeg, float radiusM)
        {
            var rng = new Random(seed);
            var left = new List<float>(leftPoolDeg);
            var right = new List<float>(rightPoolDeg);
            Shuffle(left, rng);
            Shuffle(right, rng);

            var trials = new List<TrialSpec>();
            int l = 0, r = 0;
            for (int i = 0; i < VisualRightSequence.Length; i++)
            {
                float visual = VisualRightSequence[i] ? right[r++ % right.Count] : left[l++ % left.Count];
                float audioStart = i % 2 == 0 ? audioStartDeg : -audioStartDeg;
                trials.Add(new TrialSpec(condition, i + 1, audioStart, visual, radiusM));
            }
            return trials;
        }

        static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
