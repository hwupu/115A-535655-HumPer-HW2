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
            ["practice_hud"] = (
                "Left trigger: sound left     Right trigger: sound right     A: confirm",
                "左扳機：聲音向左　右扳機：聲音向右　A：確認"),
            ["practice_result"] = ("Practice again, or begin the experiment?", "要再練習一次，還是開始實驗？"),
            ["practice_again"] = ("Practice again", "再練習一次"),
            ["ready"] = ("I'm ready to begin the experiment", "我準備好了，開始實驗"),
            ["gate_hint"] = ("Please look straight ahead", "請直視前方"),
            ["block_done"] = ("Part {0} of 3 completed.\nPress Continue when you are ready to go on.", "第 {0} 部分（共 3 部分）已完成。\n準備好後請按「繼續」。"),
            ["continue"] = ("Continue", "繼續"),
            ["thanks"] = ("Thank you for participating!", "感謝您的參與！"),
            // onboarding slides (buttons; the slide pictures carry their own text)
            ["onb_next"] = ("Next", "下一步"),
            ["onb_prev"] = ("Previous", "上一頁"),
            ["onb_language"] = ("Language", "語言"),
            ["onb_ipd"] = ("Set IPD", "設定瞳距"),
            ["onb_procedure"] = ("Procedure", "實驗說明"),
            ["onb_controllers"] = ("Controllers", "操作說明"),
            ["onb_trials"] = ("Trials", "練習回合"),
            ["onb_begin"] = ("Begin Practice", "開始練習"),
            ["resume_intro"] =("Press Continue when you are ready to go on.", "準備好後請按「繼續」。"),
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
