using UnityEngine;
using UnityEngine.UI;

namespace HumPer
{
    /// <summary>
    /// Participant ID entry with an on-screen number pad (digits 0-9, backspace, clear), operated with the controller ray.
    /// It replaces the Quest system keyboard, which did not open reliably from this app (see ADR-0002).
    /// The buttons are created by Magic > Step 3 and assigned here; this component only listens to them.
    /// </summary>
    public class ParticipantIdField : MonoBehaviour
    {
        public Text label;
        [Tooltip("Index = digit (0-9).")]
        public Button[] digitButtons = new Button[10];
        public Button backspaceButton;
        public Button clearButton;
        public string placeholder = "(use the number pad)";
        public int maxLength = 8;

        public string Value { get; private set; } = "";

        void Awake()
        {
            for (int d = 0; d < digitButtons.Length; d++)
            {
                if (digitButtons[d] == null) continue;
                char digit = (char)('0' + d);
                digitButtons[d].onClick.AddListener(() => Append(digit));
            }
            if (backspaceButton != null) backspaceButton.onClick.AddListener(Backspace);
            if (clearButton != null) clearButton.onClick.AddListener(Clear);
            Refresh();
        }

        public void Append(char digit)
        {
            if (Value.Length < maxLength) Value += digit;
            Refresh();
        }

        public void Backspace()
        {
            if (Value.Length > 0) Value = Value.Substring(0, Value.Length - 1);
            Refresh();
        }

        public void Clear()
        {
            Value = "";
            Refresh();
        }

        void Refresh()
        {
            if (label != null) label.text = Value.Length > 0 ? Value : placeholder;
        }
    }
}
