using UnityEngine;
using UnityEngine.UI;

namespace HumPer
{
    /// <summary>
    /// A uGUI Text that shows the localized string for <see cref="key"/> and picks a font that has the glyphs:
    /// the assigned CJK font (Assets/Fonts), else an installed OS CJK font, for Chinese; the default font for English.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string key;
        [Tooltip("Always use a Chinese-capable font (e.g. the language buttons).")]
        public bool alwaysCjk;
        public object[] args = System.Array.Empty<object>();

        /// <summary>Set once by GuiRefs: font with Traditional Chinese glyphs (may be null: then an OS font is used).</summary>
        public static Font CjkFont;

        static Font _osCjkFont;
        static Font _defaultFont;

        Text _text;

        void Awake() => _text = GetComponent<Text>();

        void OnEnable()
        {
            Localization.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => Localization.Changed -= Refresh;

        public void SetArgs(params object[] newArgs)
        {
            args = newArgs;
            Refresh();
        }

        public void Refresh()
        {
            if (_text == null) _text = GetComponent<Text>();
            if (!string.IsNullOrEmpty(key)) _text.text = Localization.Get(key, args);
            _text.font = alwaysCjk ? CjkFontOrFallback() : FontFor(Localization.Current);
        }

        public static Font FontFor(Language language)
        {
            if (_defaultFont == null) _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (language != Language.ChineseTaiwan) return _defaultFont;
            return CjkFontOrFallback();
        }

        /// <summary>Font for text that must always show Chinese glyphs (e.g. the language buttons).</summary>
        public static Font CjkFontOrFallback()
        {
            if (CjkFont != null) return CjkFont;
            if (_osCjkFont == null)
                _osCjkFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Noto Sans CJK TC", "Noto Sans TC", "Noto Sans CJK JP", "Microsoft JhengHei", "PingFang TC", "Droid Sans Fallback", "Arial Unicode MS" }, 36);
            return _osCjkFont;
        }
    }
}
