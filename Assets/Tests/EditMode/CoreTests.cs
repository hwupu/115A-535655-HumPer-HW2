using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace HumPer.Tests
{
    public class CoreTests
    {
        static readonly float[] Left = { -37.5f, -22.5f, -7.5f };
        static readonly float[] Right = { 7.5f, 22.5f, 37.5f };

        [Test]
        public void Planner_UsesEachVisualPositionOnce_AlternatesAudio_FollowsSideSequence()
        {
            var t = TrialPlanner.PlanCondition(Condition.Quiet, 123, Left, Right, 60f, 1f);
            Assert.AreEqual(6, t.Count);
            CollectionAssert.AreEquivalent(Left.Concat(Right).ToArray(), t.Select(x => x.VisualAzDeg).ToArray());
            CollectionAssert.AreEqual(new[] { 60f, -60f, 60f, -60f, 60f, -60f }, t.Select(x => x.AudioStartAzDeg).ToArray());
            CollectionAssert.AreEqual(new[] { false, false, true, true, false, true }, t.Select(x => x.VisualAzDeg > 0).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, t.Select(x => x.Index).ToArray());
            Assert.IsTrue(t.All(x => x.Condition == Condition.Quiet && x.RadiusM == 1f));
        }

        [Test]
        public void Planner_IsDeterministicPerSeed_AndVariesAcrossSeeds()
        {
            var a = TrialPlanner.PlanCondition(Condition.Reverb, 7, Left, Right, 60f, 1f).Select(x => x.VisualAzDeg).ToArray();
            var b = TrialPlanner.PlanCondition(Condition.Reverb, 7, Left, Right, 60f, 1f).Select(x => x.VisualAzDeg).ToArray();
            CollectionAssert.AreEqual(a, b);
            bool anyDifferent = Enumerable.Range(0, 20).Any(s =>
                !TrialPlanner.PlanCondition(Condition.Reverb, s, Left, Right, 60f, 1f).Select(x => x.VisualAzDeg).SequenceEqual(a));
            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void ConditionOrder_IsQuietReverbNoisy()
        {
            CollectionAssert.AreEqual(new[] { Condition.Quiet, Condition.Reverb, Condition.Noisy }, TrialPlanner.ConditionOrder);
        }

        [Test]
        public void StepAzimuth_RotatesAtRate_AndClamps()
        {
            Assert.AreEqual(35f, PolarMath.StepAzimuth(30f, false, true, 5f, 1f), 1e-4f);
            Assert.AreEqual(25f, PolarMath.StepAzimuth(30f, true, false, 5f, 1f), 1e-4f);
            Assert.AreEqual(30f, PolarMath.StepAzimuth(30f, true, true, 5f, 1f), 1e-4f);
            Assert.AreEqual(90f, PolarMath.StepAzimuth(89f, false, true, 5f, 1f), 1e-4f);
            Assert.AreEqual(-90f, PolarMath.StepAzimuth(-89f, true, false, 5f, 1f), 1e-4f);
        }

        [Test]
        public void ToXZ_KeepsRadius_AndSigns()
        {
            var (x, z) = PolarMath.ToXZ(30f, 1.2f);
            Assert.AreEqual(1.2f * 1.2f, x * x + z * z, 1e-4f);
            Assert.Greater(x, 0f);
            Assert.Greater(z, 0f);
            Assert.Less(PolarMath.ToXZ(-30f, 1f).x, 0f);
            Assert.Less(PolarMath.ToXZ(180f, 1f).z, 0f);
        }

        [Test]
        public void PowerSum_ThreeEqualSources_Adds4p77dB()
        {
            Assert.AreEqual(-23f + 4.771f, AudioLevelMath.PowerSumDb(-23f, -23f, -23f), 1e-2f);
            Assert.AreEqual(-23f, AudioLevelMath.PowerSumDb(-23f, -200f), 1e-3f);
        }

        [Test]
        public void Rms_OfConstantSignal()
        {
            Assert.AreEqual(-6.02f, AudioLevelMath.RmsDbfs(Enumerable.Repeat(0.5f, 100).ToArray()), 1e-2f);
            Assert.AreEqual(-200f, AudioLevelMath.RmsDbfs(new float[10]));
        }

        [Test]
        public void FileName_TimestampThenIdThenDebug()
        {
            var t = new DateTime(2026, 10, 18, 9, 5, 7);
            Assert.AreEqual("20261018_090507_P01.csv", ExperimentLog.FileName(t, "P01", false));
            Assert.AreEqual("20261018_090507_P01_DEBUG.csv", ExperimentLog.FileName(t, "P01", true));
            Assert.AreEqual("20261018_090507.csv", ExperimentLog.FileName(t, "", false));
            Assert.AreEqual("20261018_090507_ab.csv", ExperimentLog.FileName(t, "a/b", false));
        }

        [Test]
        public void Log_WritesHeaderEventsAndTrialRows()
        {
            var sw = new StringWriter();
            var clock = new DateTime(2026, 10, 18, 9, 0, 0);
            var log = new ExperimentLog(sw, () => clock, "P01", debug: false);
            log.Event("session_start", "seed=1");
            clock = clock.AddSeconds(5);
            log.ConditionEvent("practice_begin", "practice");
            clock = clock.AddSeconds(10);
            log.Trial("practice", 1, 0f, 30f, 12.5f, 8.25f, -17.5f, -200f);

            var lines = sw.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(4, lines.Length);
            Assert.AreEqual(ExperimentLog.Header, lines[0]);
            StringAssert.StartsWith("session_start,2026-10-18T09:00:00.000,0,P01,EXPERIMENT,,", lines[1]);
            StringAssert.StartsWith("practice_begin,2026-10-18T09:00:05.000,5,P01,EXPERIMENT,practice,", lines[2]);
            Assert.AreEqual("trial,2026-10-18T09:00:15.000,15,P01,EXPERIMENT,practice,1,0,30,12.5,12.5,8.25,-17.5,,", lines[3]);
        }

        [Test]
        public void Log_DebugModeIsFlagged_AndNotesAreEscaped()
        {
            var sw = new StringWriter();
            var log = new ExperimentLog(sw, () => DateTime.UnixEpoch, "x", debug: true);
            log.Event("note", "a,\"b\"");
            StringAssert.Contains(",DEBUG,", sw.ToString());
            StringAssert.Contains("\"a,\"\"b\"\"\"", sw.ToString());
        }

        [Test]
        public void Localization_SwitchesLanguage_AndRaisesEvent()
        {
            int changes = 0;
            Action handler = () => changes++;
            Localization.Changed += handler;
            Localization.Set(Language.ChineseTaiwan);
            Assert.AreEqual("開始練習", Localization.Get("practice_start"));
            Localization.Set(Language.English);
            Assert.AreEqual("Start practice", Localization.Get("practice_start"));
            Localization.Changed -= handler;
            Assert.AreEqual(2, changes);
            Assert.AreEqual("[nope]", Localization.Get("nope"));
        }

        [Test]
        public void Progress_RoundTrips_AndMovesToNextBlockAfterSixTrials()
        {
            var p = new SessionProgress { ParticipantId = "12", CsvFile = "a.csv", Language = Language.ChineseTaiwan, LanguageChosen = true, PracticeDone = true, Seed = 99 };
            for (int i = 0; i < 6; i++) p.TrialFinished();
            Assert.AreEqual(1, p.BlockIndex);
            Assert.AreEqual(0, p.TrialsDone);
            p.TrialFinished();
            var q = SessionProgress.Parse(p.Serialize());
            Assert.AreEqual("12", q.ParticipantId);
            Assert.AreEqual("a.csv", q.CsvFile);
            Assert.AreEqual(Language.ChineseTaiwan, q.Language);
            Assert.IsTrue(q.PracticeDone && q.LanguageChosen);
            Assert.AreEqual(99, q.Seed);
            Assert.AreEqual(1, q.BlockIndex);
            Assert.AreEqual(1, q.TrialsDone);
            Assert.AreEqual(SessionStatus.InProgress, q.Status);
            StringAssert.Contains("Reverb", q.Describe());
        }

        [Test]
        public void ProgressStore_OnlyReturnsUnfinishedSessions()
        {
            string dir = Path.Combine(Path.GetTempPath(), "humper_progress_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var store = new ProgressStore(dir);
                Assert.IsNull(store.LoadUnfinished("7"));
                store.Save(new SessionProgress { ParticipantId = "7", CsvFile = "x.csv" });
                store.Save(new SessionProgress { ParticipantId = "8", Status = SessionStatus.Cancelled });
                store.Save(new SessionProgress { ParticipantId = "9", Status = SessionStatus.Completed });
                Assert.IsNotNull(store.LoadUnfinished("7"));
                Assert.IsNull(store.LoadUnfinished("8"));
                Assert.IsNull(store.LoadUnfinished("9"));
                Assert.AreEqual(1, store.ListUnfinished().Count);
                var p = store.Load("7");
                p.TrialFinished();
                store.Save(p); // overwrite
                Assert.AreEqual(1, store.Load("7").TrialsDone);
                Assert.AreEqual(Path.Combine(dir, "progress_noid.txt"), store.PathFor(""));
            }
            finally { Directory.Delete(dir, true); }
        }

        [Test]
        public void Log_AppendMode_WritesNoSecondHeader()
        {
            var sw = new StringWriter();
            var log = new ExperimentLog(sw, () => DateTime.UnixEpoch, "5", debug: false, writeHeader: false);
            log.Event("resume");
            StringAssert.DoesNotContain("event,timestamp_local", sw.ToString());
            StringAssert.StartsWith("resume,", sw.ToString());
        }

        [Test]
        public void Localization_HasSetupInstructions_InBothLanguages()
        {
            foreach (var language in new[] { Language.English, Language.ChineseTaiwan })
            {
                Localization.Set(language);
                foreach (var key in new[] { "setup_recenter", "setup_ipd", "setup_dot" })
                    Assert.IsFalse(Localization.Get(key).StartsWith("["), key + " missing in " + language);
            }
            Localization.Set(Language.English);
        }
    }
}
