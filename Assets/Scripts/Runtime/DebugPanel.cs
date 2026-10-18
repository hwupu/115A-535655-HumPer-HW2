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

        [Header("Reverb and playback tests")]
        public Button rt02Button, rt07Button, rt22Button, presetButton, rtDownButton, rtUpButton;
        public Text presetLabel;
        public Text reverbReadout;
        public Button stimulusToggleButton, backgroundToggleButton;
        public Text stimulusToggleLabel, backgroundToggleLabel;


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

        public void SetStimulusPlaying(bool on)
        {
            stimulusToggleLabel.text = on ? "Stimulus: PLAYING (tap to stop)" : "Play stimulus (2 s on, 1 s off)";
            Tint(stimulusToggleButton, on);
        }

        public void SetBackgroundPlaying(bool on)
        {
            backgroundToggleLabel.text = on ? "Background: PLAYING (tap to stop)" : "Play background noise";
            Tint(backgroundToggleButton, on);
        }

        /// <summary>Highlight the active reverb button (1 = 0.2 s, 2 = 0.7 s, 3 = 2.2 s, 4 = Meta preset, else none) and show the readout.</summary>
        public void SetReverb(string readout, int active)
        {
            reverbReadout.text = readout;
            Tint(rt02Button, active == 1);
            Tint(rt07Button, active == 2);
            Tint(rt22Button, active == 3);
            Tint(presetButton, active == 4);
        }

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
