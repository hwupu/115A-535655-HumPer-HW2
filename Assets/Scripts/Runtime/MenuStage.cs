using System;
using UnityEngine;
using UnityEngine.UI;

namespace HumPer
{
    /// <summary>
    /// GUI stage 1: experimenter menu. Participant ID, "Start Experiment" (hand over to the participant) and "Debug".
    /// For now it only reports the choice (status line + console); later stages subscribe to <see cref="Chosen"/>.
    /// </summary>
    public class MenuStage : MonoBehaviour
    {
        public ParticipantIdField idField;
        public Button startButton;
        public Button debugButton;
        public Text statusText;

        /// <summary>(participantId, isDebug)</summary>
        public event Action<string, bool> Chosen;

        void Awake()
        {
            startButton.onClick.AddListener(() => Choose(false));
            debugButton.onClick.AddListener(() => Choose(true));
            if (statusText != null) statusText.text = "";
        }

        void Choose(bool debug)
        {
            string id = idField != null ? idField.Value : "";
            string message = (debug ? "Debug" : "Start experiment") + " clicked, participant ID: " + (id.Length > 0 ? id : "(none)");
            Debug.Log("[HumPer] Menu: " + message);
            if (statusText != null) statusText.text = message;
            Chosen?.Invoke(id, debug);
        }
    }
}
