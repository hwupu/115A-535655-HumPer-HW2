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
        public Button sphereToggleButton;
        public Text sphereToggleLabel;
        public Button runTrialButton, practiceTrialButton, backButton;
        public Text statusText;

        [Header("Reverb and playback tests")]
        public Button rtQuietButton, rtNoisyButton, rtReverbButton, presetButton;
        public Text presetLabel;
        public Text reverbReadout;
        public Button stimulusToggleButton, backgroundToggleButton, measureButton;
        public Text stimulusToggleLabel, backgroundToggleLabel;


        public Color selectedColor = new Color(0.2f, 0.45f, 0.8f, 1f);
        public Color idleColor = new Color(0.22f, 0.25f, 0.32f, 1f);

        public void SetSphereAlways(bool always) =>
            sphereToggleLabel.text = always ? "Audio sphere: ALWAYS visible" : "Audio sphere: only after confirm";

        public void SetStimulusPlaying(bool on)
        {
            stimulusToggleLabel.text = on ? "Stimulus: PLAYING (tap to stop)" : "Play stimulus (2 s on, 1 s off)";
            Tint(stimulusToggleButton, on);
        }

        public void SetBackgroundState(bool playing, bool paused)
        {
            backgroundToggleLabel.text = playing ? "Background: PLAYING (tap to pause)" : paused ? "Background: PAUSED (tap to resume)" : "Play background noise";
            Tint(backgroundToggleButton, playing);
        }

        /// <summary>Highlight the active reverb button (1 = Quiet 0.3 s, 2 = Noisy 0.7 s, 3 = Reverb 2.2 s, 4 = Meta preset, else none) and show the readout.</summary>
        public void SetReverb(string readout, int active)
        {
            reverbReadout.text = readout;
            Tint(rtQuietButton, active == 1);
            Tint(rtNoisyButton, active == 2);
            Tint(rtReverbButton, active == 3);
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
