using UnityEngine;
using UnityEngine.UI;

namespace HumPer
{
    /// <summary>
    /// Debug test bench panel (shown between trials): choose the condition, choose when the audio sphere is visible, run trials,
    /// go back to the menu. Pure UI: ExperimentFlow reads the button clicks and drives the trials.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        public Button quietButton, reverbButton, noisyButton;
        public Button sphereToggleButton;
        public Text sphereToggleLabel;
        public Button runTrialButton, practiceTrialButton, backButton;
        public Text statusText;

        public Color selectedColor = new Color(0.2f, 0.45f, 0.8f, 1f);
        public Color idleColor = new Color(0.22f, 0.25f, 0.32f, 1f);

        public void SetCondition(Condition condition)
        {
            Tint(quietButton, condition == Condition.Quiet);
            Tint(reverbButton, condition == Condition.Reverb);
            Tint(noisyButton, condition == Condition.Noisy);
        }

        public void SetSphereAlways(bool always) =>
            sphereToggleLabel.text = always ? "Audio sphere: ALWAYS visible" : "Audio sphere: only after confirm";

        public void SetStatus(string text) => statusText.text = text;

        void Tint(Button b, bool selected)
        {
            var colors = b.colors;
            colors.normalColor = selected ? selectedColor : idleColor;
            colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.35f);
            colors.pressedColor = Color.Lerp(colors.normalColor, Color.white, 0.6f);
            colors.selectedColor = colors.normalColor;
            b.colors = colors;
        }
    }
}
