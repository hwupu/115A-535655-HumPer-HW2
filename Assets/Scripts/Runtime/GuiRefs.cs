using UnityEngine;
using UnityEngine.UI;

namespace HumPer
{
    /// <summary>Scene references to the GUI built by HumPer > Step 3. Panels are shown one at a time with <see cref="Show"/>.</summary>
    public class GuiRefs : MonoBehaviour
    {
        [Header("Main canvas")]
        public GuiPanelPlacer mainPlacer;
        public MenuStage menu;                  // stage 1
        public GameObject languagePanel;        // stage 2
        public Button chineseButton, englishButton;
        public GameObject introPanel;           // practice instructions
        public Button startPracticeButton;
        public GameObject setupPanel;           // recenter / IPD / look-at-the-dot instructions before the practice
        public LocalizedText setupText;
        public Button setupContinueButton;
        public GameObject gatePanel;            // "look straight ahead" fixation dot (no background)
        public GameObject gateHint;
        public GameObject resultPanel;          // practice again / ready
        public Button againButton, readyButton;
        public GameObject blockDonePanel;       // rest screen between the three conditions
        public LocalizedText blockDoneText;
        public Button continueButton;
        public GameObject thanksPanel;          // end of the experiment
        public DebugPanel debugPanel;           // Debug test bench (between trials)

        [Header("Experimenter screens (English)")]
        public GameObject resumePanel;          // an unfinished session exists for the entered ID
        public Text resumeText;
        public Button resumeButton, startNewButton, resumeBackButton;
        public GameObject cancelPanel;          // Y button: cancel this participant? (upper region, shown on top of any screen)
        public Text cancelText;
        public Button cancelConfirmButton, cancelKeepButton;

        [Header("HUD canvas (small strip above the stimuli)")]
        public GameObject hudRoot;
        public GuiPanelPlacer hudPlacer;
        public Text hudText;
        public LocalizedText hudLocalized;

        [Header("Font")]
        public Font cjkFont;

        GameObject[] _panels;

        void Awake()
        {
            LocalizedText.CjkFont = cjkFont;
            _panels = new[] { menu.gameObject, languagePanel, introPanel, setupPanel, gatePanel, resultPanel, blockDonePanel, thanksPanel, debugPanel.gameObject, resumePanel };
            HideAll();
            if (cancelPanel != null) cancelPanel.SetActive(false);
            ShowHud(null);
        }

        public void HideAll()
        {
            foreach (var p in _panels) p.SetActive(false);
        }

        public void Show(GameObject panel)
        {
            HideAll();
            panel.SetActive(true);
        }

        /// <summary>Show the HUD strip with a localized key, or literal text if key is null; hide it when both are null.</summary>
        public void ShowHud(string localizedKey, string literal = null)
        {
            if (localizedKey == null && literal == null) { hudRoot.SetActive(false); return; }
            hudLocalized.enabled = localizedKey != null;
            if (localizedKey != null) { hudLocalized.key = localizedKey; hudLocalized.Refresh(); }
            else hudText.text = literal;
            hudRoot.SetActive(true);
        }

        /// <summary>Re-center both canvases in front of the participant (after a recenter).</summary>
        public void PlaceAll()
        {
            if (mainPlacer != null) mainPlacer.Place();
            if (hudPlacer != null) hudPlacer.Place();
        }
    }
}
