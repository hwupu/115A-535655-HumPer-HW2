using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace HumPer
{
    /// <summary>
    /// CSV experiment log. Writes every row immediately (the caller supplies an auto-flushing writer) so data survives a crash
    /// or headset power-off. Pure C#: the clock and a mirror callback (e.g. Debug.Log) are injected.
    /// </summary>
    public class ExperimentLog : IDisposable
    {
        public const string Header =
            "event,timestamp_local,elapsed_s,participant,mode,condition,trial,visual_az_deg,audio_start_az_deg,audio_final_az_deg,signed_error_deg,duration_s,stim_level_dbfs,bg_level_dbfs,note";

        readonly TextWriter _writer;
        readonly Func<DateTime> _now;
        readonly Action<string> _mirror;
        readonly DateTime _start;
        readonly string _participant;
        readonly string _mode;

        public ExperimentLog(TextWriter writer, Func<DateTime> now, string participantId, bool debug, Action<string> mirror = null)
        {
            _writer = writer;
            _now = now;
            _mirror = mirror;
            _start = now();
            _participant = SanitizeId(participantId);
            _mode = debug ? "DEBUG" : "EXPERIMENT";
            WriteLine(Header);
        }

        /// <summary>yyyyMMdd_HHmmss[_ParticipantID][_DEBUG].csv</summary>
        public static string FileName(DateTime start, string participantId, bool debug)
        {
            string name = start.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string id = SanitizeId(participantId);
            if (id.Length > 0) name += "_" + id;
            if (debug) name += "_DEBUG";
            return name + ".csv";
        }

        public void Event(string evt, string note = "") => Row(evt, "", null, "", "", "", "", "", "", "", note);

        public void ConditionEvent(string evt, string condition, string note = "") => Row(evt, condition, null, "", "", "", "", "", "", "", note);

        /// <summary>One finished trial. bgLevelDbfs &lt;= -199 means no background (blank).</summary>
        public void Trial(string condition, int trial, float visualAz, float audioStartAz, float audioFinalAz,
                          float durationSec, float stimLevelDbfs, float bgLevelDbfs, string note = "")
        {
            Row("trial", condition, trial, F(visualAz), F(audioStartAz), F(audioFinalAz), F(audioFinalAz - visualAz),
                F(durationSec), F(stimLevelDbfs), bgLevelDbfs <= -199f ? "" : F(bgLevelDbfs), note);
        }

        public void Dispose() => _writer.Dispose();

        void Row(string evt, string condition, int? trial, string vis, string audStart, string audFinal, string err,
                 string dur, string stim, string bg, string note)
        {
            var now = _now();
            WriteLine(string.Join(",",
                evt,
                now.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture),
                F((float)(now - _start).TotalSeconds),
                Csv(_participant), _mode, condition,
                trial.HasValue ? trial.Value.ToString(CultureInfo.InvariantCulture) : "",
                vis, audStart, audFinal, err, dur, stim, bg, Csv(note)));
        }

        void WriteLine(string line)
        {
            _writer.WriteLine(line);
            _mirror?.Invoke(line);
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }

        public static string SanitizeId(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            foreach (char c in s) if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
            return sb.ToString();
        }
    }
}
