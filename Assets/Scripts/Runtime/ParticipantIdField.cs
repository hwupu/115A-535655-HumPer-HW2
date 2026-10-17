using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HumPer
{
    /// <summary>
    /// Participant ID entry. A button that shows the ID; clicking it opens the Quest system keyboard (Unity's
    /// TouchScreenKeyboard; needs "Requires System Keyboard" in the Oculus project config, set by the GUI setup step, and a
    /// build made after that setting). The system keyboard does not exist in the Editor or over Quest Link
    /// (TouchScreenKeyboard.isSupported is false there), so type on the PC keyboard with the Game view focused and press Enter.
    /// A small hint line under the field says which path is used, so problems are visible without a log.
    /// Clicking the field again while editing cancels and restarts.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ParticipantIdField : MonoBehaviour
    {
        public Text label;
        public Text hint;
        public string placeholder = "(tap to enter)";
        public int maxLength = 16;

        public string Value { get; private set; } = "";

        Coroutine _editing;
        TouchScreenKeyboard _keyboard;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClicked);
            Refresh(null);
            SetHint("");
        }

        void OnClicked()
        {
            if (_editing != null)
            {
                StopCoroutine(_editing);
                if (_keyboard != null) _keyboard.active = false;
                _editing = null;
            }
            _editing = StartCoroutine(Edit());
        }

        void Refresh(string typing)
        {
            if (label == null) return;
            if (typing != null) label.text = typing + "_";
            else label.text = Value.Length > 0 ? Value : placeholder;
        }

        void SetHint(string message)
        {
            if (hint != null) hint.text = message;
            if (!string.IsNullOrEmpty(message)) Debug.Log("[HumPer] ID field: " + message);
        }

        IEnumerator Edit()
        {
            if (TouchScreenKeyboard.isSupported)
            {
                _keyboard = TouchScreenKeyboard.Open(Value, TouchScreenKeyboardType.Default, false, false, false, false, "Participant ID");
                if (_keyboard == null)
                {
                    SetHint("System keyboard unavailable (TouchScreenKeyboard.Open returned null)");
                }
                else
                {
                    SetHint("Keyboard opened. Press Done to confirm; tap the field again to retry.");
                    while (_keyboard != null && _keyboard.status == TouchScreenKeyboard.Status.Visible)
                    {
                        Refresh(Clean(_keyboard.text));
                        yield return null;
                    }
                    if (_keyboard != null)
                    {
                        if (_keyboard.status == TouchScreenKeyboard.Status.Done) { Value = Clean(_keyboard.text); SetHint(""); }
                        else SetHint("Keyboard closed (" + _keyboard.status + ")");
                    }
                    _keyboard = null;
                }
            }
            else
            {
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard == null) SetHint("No system keyboard here (Editor/Link) and no PC keyboard found");
                else
                {
                    SetHint("No system keyboard in Editor/Link: type on the PC keyboard (Game view focused), Enter to finish");
                    string text = Value;
                    bool finished = false;
                    System.Action<char> onChar = c =>
                    {
                        if (c == '\n' || c == '\r') finished = true;
                        else if (c == '\b') { if (text.Length > 0) text = text.Substring(0, text.Length - 1); }
                        else text = Clean(text + c);
                    };
                    keyboard.onTextInput += onChar;
                    while (!finished) { Refresh(text); yield return null; }
                    keyboard.onTextInput -= onChar;
                    Value = text;
                    SetHint("");
                }
            }
            Refresh(null);
            _editing = null;
        }

        string Clean(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s ?? "")
                if ((char.IsLetterOrDigit(c) || c == '-' || c == '_') && sb.Length < maxLength) sb.Append(c);
            return sb.ToString();
        }
    }
}
