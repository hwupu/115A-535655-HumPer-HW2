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
                "練習\n\n請使用左、右扳機鍵移動聲音，直到聲音聽起來來自立牌的位置。\n完成後按 A 鍵確認。\n您可以練習任意次數。"),
            ["practice_start"] = ("Start practice", "開始練習"),
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
            ["setup_recenter"] = (
                "Before we start\n\nSit comfortably and look straight ahead.\nIf the picture looks off-center, or too high or too low, press and hold the Meta button on your right controller to recenter.\nTell the experimenter if it still does not look right.",
                "開始之前\n\n請坐好並直視前方。\n如果畫面偏離中央，或位置過高、過低，請按住右手控制器上的 Meta 按鈕以重新置中。\n若仍覺得不對，請告知實驗人員。"),
            ["setup_ipd"] = (
                "Adjust the headset\n\nTurn the IPD dial on the bottom of the headset to the value the experimenter gives you.\nThe picture should look sharp and comfortable.",
                "調整頭戴裝置\n\n請依實驗人員告知的瞳距（IPD）數值，轉動頭戴裝置下方的瞳距旋鈕。\n畫面應該清晰且舒適。"),
            ["setup_dot"] = (
                "When you press Continue, a black dot will appear.\nLook at the dot and keep your head still.\nThe practice continues automatically once you look straight ahead.\nThe dot also appears before every trial.",
                "按下「繼續」後會出現一個黑點。\n請注視黑點並保持頭部不動。\n當您直視前方時，練習會自動繼續。\n每次試驗開始前也都會出現這個黑點。"),
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
