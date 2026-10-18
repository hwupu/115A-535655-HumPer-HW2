using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HumPer
{
    public enum SessionStatus { InProgress, Completed, Cancelled }

    /// <summary>
    /// What is needed to continue an interrupted real session: who, which CSV, the seed (so the trial plan is identical), the language,
    /// and how far the participant got. Saved after every finished trial. A trial that was running when the app stopped is not counted
    /// and is simply run again.
    /// </summary>
    public class SessionProgress
    {
        public string ParticipantId = "";
        public string CsvFile = "";
        public Language Language = Language.English;
        public bool LanguageChosen;
        public bool PracticeDone;
        public int Seed;
        public int BlockIndex;      // 0-based index into TrialPlanner.ConditionOrder
        public int TrialsDone;      // finished trials in the current block
        public SessionStatus Status = SessionStatus.InProgress;

        public const int TrialsPerBlock = 6;

        /// <summary>Count one finished trial; moves to the next block after the last trial of a block.</summary>
        public void TrialFinished()
        {
            TrialsDone++;
            if (TrialsDone >= TrialsPerBlock) { BlockIndex++; TrialsDone = 0; }
        }

        /// <summary>Human-readable position for the experimenter, e.g. "Reverb, 3 of 6 trials done".</summary>
        public string Describe()
        {
            if (!PracticeDone) return "practice";
            if (BlockIndex >= TrialPlanner.ConditionOrder.Length) return "finished";
            return TrialPlanner.ConditionOrder[BlockIndex] + ", " + TrialsDone + " of " + TrialsPerBlock + " trials done";
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            void Put(string k, string v) => sb.Append(k).Append('=').Append(v).Append('\n');
            Put("participant", ParticipantId);
            Put("csv", CsvFile);
            Put("language", Language.ToString());
            Put("language_chosen", LanguageChosen ? "1" : "0");
            Put("practice_done", PracticeDone ? "1" : "0");
            Put("seed", Seed.ToString(CultureInfo.InvariantCulture));
            Put("block", BlockIndex.ToString(CultureInfo.InvariantCulture));
            Put("trials_done", TrialsDone.ToString(CultureInfo.InvariantCulture));
            Put("status", Status.ToString());
            return sb.ToString();
        }

        public static SessionProgress Parse(string text)
        {
            var map = new Dictionary<string, string>();
            foreach (var line in (text ?? "").Split('\n'))
            {
                int eq = line.IndexOf('=');
                if (eq > 0) map[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
            string Get(string k) => map.TryGetValue(k, out var v) ? v : "";
            int Int(string k) => int.TryParse(Get(k), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;

            var p = new SessionProgress
            {
                ParticipantId = Get("participant"),
                CsvFile = Get("csv"),
                LanguageChosen = Get("language_chosen") == "1",
                PracticeDone = Get("practice_done") == "1",
                Seed = Int("seed"),
                BlockIndex = Int("block"),
                TrialsDone = Int("trials_done"),
            };
            if (Enum.TryParse(Get("language"), out Language lang)) p.Language = lang;
            if (Enum.TryParse(Get("status"), out SessionStatus status)) p.Status = status;
            return p;
        }
    }

    /// <summary>One progress file per participant ID in a folder (<c>progress_ID.txt</c>), rewritten after every finished trial.</summary>
    public class ProgressStore
    {
        const string Prefix = "progress_";
        readonly string _dir;

        public ProgressStore(string directory) => _dir = directory;

        public string PathFor(string participantId)
        {
            string id = ExperimentLog.SanitizeId(participantId);
            return Path.Combine(_dir, Prefix + (id.Length > 0 ? id : "noid") + ".txt");
        }

        public void Save(SessionProgress progress)
        {
            string path = PathFor(progress.ParticipantId);
            string temp = path + ".tmp"; // write then replace, so a power-off never leaves a half-written file
            File.WriteAllText(temp, progress.Serialize());
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }

        public SessionProgress Load(string participantId)
        {
            string path = PathFor(participantId);
            return File.Exists(path) ? SessionProgress.Parse(File.ReadAllText(path)) : null;
        }

        /// <summary>The unfinished (not completed, not cancelled) session for this ID, else null.</summary>
        public SessionProgress LoadUnfinished(string participantId)
        {
            var p = Load(participantId);
            return p != null && p.Status == SessionStatus.InProgress ? p : null;
        }

        public List<SessionProgress> ListUnfinished()
        {
            var list = new List<SessionProgress>();
            if (!Directory.Exists(_dir)) return list;
            foreach (var file in Directory.GetFiles(_dir, Prefix + "*.txt"))
            {
                var p = SessionProgress.Parse(File.ReadAllText(file));
                if (p.Status == SessionStatus.InProgress) list.Add(p);
            }
            return list;
        }
    }
}
