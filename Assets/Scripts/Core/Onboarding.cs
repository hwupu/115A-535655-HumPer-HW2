namespace HumPer
{
    /// <summary>
    /// The onboarding slides shown before the practice (reposition, IPD, procedure, controllers, trials, ready). The pictures carry the
    /// instructions; this class only decides which buttons each slide has and what they say: the label of a button names the slide it leads to.
    /// </summary>
    public static class Onboarding
    {
        public const int SlideCount = 6;

        // localization key of the left (back) and right (forward) button of each slide; null = no button; slide 1 has the language button
        static readonly string[] LeftKeys = { "onb_language", "onb_prev", "onb_ipd", "onb_procedure", "onb_controllers", "onb_trials" };
        static readonly string[] RightKeys = { "onb_next", "onb_procedure", "onb_controllers", "onb_trials", "onb_next", "onb_begin" };

        public static bool HasLeft(int slide) => LeftKeys[slide] != null;

        /// <summary>The last slide's right button ends the onboarding (it has no arrow).</summary>
        public static bool IsLast(int slide) => slide == SlideCount - 1;

        /// <summary>Slide 1's left button is not a back button: it reopens the language choice (no arrow).</summary>
        public static string LeftLabel(int slide) =>
            LeftKeys[slide] == null ? "" : slide == 0 ? Localization.Get(LeftKeys[slide]) : "← " + Localization.Get(LeftKeys[slide]);

        public static string RightLabel(int slide) =>
            IsLast(slide) ? Localization.Get(RightKeys[slide]) : Localization.Get(RightKeys[slide]) + " →";

        /// <summary>Slide shown after a click (-1 = back, +1 = forward); the result is SlideCount when the last slide is confirmed.</summary>
        public static int Move(int slide, int direction)
        {
            int target = slide + direction;
            return target < 0 ? 0 : target > SlideCount ? SlideCount : target;
        }
    }
}
