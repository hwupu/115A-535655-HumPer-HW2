using System;
using System.Collections.Generic;

namespace HumPer
{
    public enum Language { English, ChineseTaiwan }

    /// <summary>String table for participant-facing text (English and Chinese, Taiwan). The experimenter menu stays English.</summary>
    public static class Localization
    {
        public static Language Current { get; private set; } = Language.English;
        public static event Action Changed;

        static readonly Dictionary<string, (string en, string zh)> Table = new Dictionary<string, (string, string)>
        {
            ["lang_title"] = ("Select language\n選擇語言", "選擇語言\nSelect language"),
            ["practice_intro"] = (
                "Practice\n\nUse the left and right triggers to move the sound until it seems to come from the figure.\nPress A to confirm.\nYou can practice as many times as you like.",
                "練習\n\n請使用左、右扳機鍵移動聲音，直到聲音聽起來來自人形立牌的位置。\n完成後按 A 鍵確認。\n您可以練習任意次數。"),
            ["practice_start"] = ("Start practice", "開始練習"),
            ["practice_hud"] = (
                "Left trigger: sound left     Right trigger: sound right     A: confirm",
                "左扳機：聲音向左　右扳機：聲音向右　A：確認"),
            ["practice_result"] = ("Practice again, or begin the experiment?", "要再練習一次，還是開始實驗？"),
            ["practice_again"] = ("Practice again", "再練習一次"),
            ["ready"] = ("I'm ready to begin the experiment", "我準備好了，開始實驗"),
            ["gate_hint"] = ("Please look straight ahead", "請直視前方"),
            ["practice_done"] = ("Practice finished.\n(The next stage is not built yet.)", "練習結束。\n（下一階段尚未建置。）"),
        };

        public static void Set(Language language)
        {
            Current = language;
            Changed?.Invoke();
        }

        public static string Get(string key, params object[] args)
        {
            if (!Table.TryGetValue(key, out var entry)) return "[" + key + "]";
            string s = Current == Language.English ? entry.en : entry.zh;
            return args.Length > 0 ? string.Format(s, args) : s;
        }
    }
}
