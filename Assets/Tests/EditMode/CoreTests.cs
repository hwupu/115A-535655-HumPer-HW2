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
        public void ConditionOrder_IsQuietNoisyReverb()
        {
            CollectionAssert.AreEqual(new[] { Condition.Quiet, Condition.Noisy, Condition.Reverb }, TrialPlanner.ConditionOrder);
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
            Assert.AreEqual("trial,2026-10-18T09:00:15.000,15,P01,EXPERIMENT,practice,1,0,30,12.5,12.5,8.25,-17.5,,,", lines[3]);
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
            Assert.AreEqual("繼續", Localization.Get("continue"));
            Localization.Set(Language.English);
            Assert.AreEqual("Continue", Localization.Get("continue"));
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
            StringAssert.Contains("Noisy", q.Describe());
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
        public void Onboarding_ButtonsNameTheSlideTheyLeadTo_InBothLanguages()
        {
            foreach (var language in new[] { Language.English, Language.ChineseTaiwan })
            {
                Localization.Set(language);
                for (int i = 0; i < Onboarding.SlideCount; i++)
                {
                    Assert.IsFalse(Onboarding.RightLabel(i).Contains("["), "right label of slide " + i + " missing in " + language);
                    if (Onboarding.HasLeft(i)) Assert.IsFalse(Onboarding.LeftLabel(i).Contains("["), "left label of slide " + i + " missing in " + language);
                }
                // the back button of a slide names the slide before it, like the forward button of the slide before that (slide 1 and 2 are special: "Next", "Previous")
                for (int i = 3; i < Onboarding.SlideCount; i++)
                    Assert.AreEqual(Onboarding.RightLabel(i - 2).Replace(" →", ""), Onboarding.LeftLabel(i).Replace("← ", ""), "slide " + i);
            }
            Localization.Set(Language.English);
            Assert.IsTrue(Onboarding.HasLeft(0));
            Assert.AreEqual("Language", Onboarding.LeftLabel(0)); // slide 1: opens the language choice, no arrow
            Assert.AreEqual("Next →", Onboarding.RightLabel(0));
            Assert.AreEqual("Next →", Onboarding.RightLabel(4));
            Assert.AreEqual("Begin Practice", Onboarding.RightLabel(Onboarding.SlideCount - 1)); // the last one ends the onboarding, no arrow
            Localization.Set(Language.ChineseTaiwan);
            Assert.AreEqual("設定瞳距", Onboarding.LeftLabel(2).Replace("← ", ""));
            Localization.Set(Language.English);
        }

        [Test]
        public void Onboarding_Move_StaysInRange_AndEndsAfterTheLastSlide()
        {
            Assert.AreEqual(0, Onboarding.Move(0, -1));
            Assert.AreEqual(3, Onboarding.Move(2, 1));
            Assert.AreEqual(1, Onboarding.Move(2, -1));
            Assert.AreEqual(Onboarding.SlideCount, Onboarding.Move(Onboarding.SlideCount - 1, 1));
        }

        [Test]
        public void RoomAcoustics_ReflectionForRt60_RoundTrips_AndGrowsWithRt60()
        {
            RoomAcousticsMath.BoxVolumeSurface(8f, 10f, 3.5f, out float v, out float s);
            Assert.AreEqual(280f, v, 1e-3f);
            Assert.AreEqual(286f, s, 1e-3f);
            float r02 = RoomAcousticsMath.ReflectionForRt60(0.2f, v, s);
            float r07 = RoomAcousticsMath.ReflectionForRt60(0.7f, v, s);
            float r3 = RoomAcousticsMath.ReflectionForRt60(3f, v, s);
            Assert.Less(r02, r07);
            Assert.Less(r07, r3);
            Assert.AreEqual(0.894f, r07, 0.005f);
            foreach (var rt in new[] { 0.2f, 0.7f, 3f })
                Assert.AreEqual(rt, RoomAcousticsMath.Rt60ForReflection(RoomAcousticsMath.ReflectionForRt60(rt, v, s), v, s), 0.01f);
            // energy reading needs a lower coefficient for the same RT60
            Assert.Less(RoomAcousticsMath.ReflectionForRt60(0.7f, v, s, amplitude: false), r07);
        }

        [Test]
        public void ConditionAcoustics_QuietNoisyReverb_HaveTheProtocolValues()
        {
            Assert.AreEqual(0.3f, ConditionAcoustics.Rt60(Condition.Quiet));
            Assert.AreEqual(0.3f, ConditionAcoustics.Rt60(Condition.Practice));
            Assert.AreEqual(0.7f, ConditionAcoustics.Rt60(Condition.Noisy));
            Assert.AreEqual(2.2f, ConditionAcoustics.Rt60(Condition.Reverb));
            Assert.IsFalse(ConditionAcoustics.HasBackground(Condition.Quiet));
            Assert.IsFalse(ConditionAcoustics.HasBackground(Condition.Practice));
            Assert.IsTrue(ConditionAcoustics.HasBackground(Condition.Noisy));
            Assert.IsTrue(ConditionAcoustics.HasBackground(Condition.Reverb));
        }

        [Test]
        public void Log_TrialRowHasMeasuredRt60Column()
        {
            var sw = new StringWriter();
            var log = new ExperimentLog(sw, () => DateTime.UnixEpoch, "P", debug: false, writeHeader: false);
            log.Trial("noisy", 2, 7.5f, 60f, 10f, 5f, -20f, -18.2f, 0.68f);
            StringAssert.Contains(",-20,-18.2,0.68,", sw.ToString());
            StringAssert.Contains("rt60_measured_s,note", ExperimentLog.Header);
        }

        // Synthetic impulse response: exponentially decaying noise whose energy falls 60 dB in rt60 seconds.
        static float[] SyntheticDecay(float rt60, float sampleRate, float seconds, int seed)
        {
            var rng = new Random(seed);
            var energy = new float[(int)(sampleRate * seconds)];
            for (int i = 1; i < energy.Length; i++)
            {
                double t = i / sampleRate;
                double gauss = Math.Sqrt(-2.0 * Math.Log(1.0 - rng.NextDouble())) * Math.Cos(2.0 * Math.PI * rng.NextDouble());
                double amp = gauss * Math.Pow(10.0, -3.0 * t / rt60);
                energy[i] = (float)(amp * amp);
            }
            energy[0] = 1f; // the direct sound
            return energy;
        }

        [TestCase(0.2f)]
        [TestCase(0.7f)]
        [TestCase(2.2f)]
        public void Rt60Estimator_RecoversTheDecayTime(float rt60)
        {
            var energy = SyntheticDecay(rt60, 48000f, rt60 * 2.5f, seed: 11);
            var r = Rt60Estimator.Estimate(energy, 0, energy.Length, 48000f);
            Assert.IsTrue(r.Valid, r.ToString());
            Assert.AreEqual(rt60, r.Rt60T20, rt60 * 0.15f);
        }

        [Test]
        public void Rt60Estimator_RejectsSilenceAndShortDecays()
        {
            Assert.IsFalse(Rt60Estimator.Estimate(new float[48000], 0, 48000, 48000f).Valid);
            var tooShort = SyntheticDecay(5f, 48000f, 0.3f, seed: 3); // 0.3 s of a 5 s decay never reaches -25 dB
            Assert.IsFalse(Rt60Estimator.Estimate(tooShort, 0, tooShort.Length, 48000f).Valid);
        }

        [Test]
        public void Rt60Calibration_MapsTargetsToMeasuredCoefficients_AndScalesWithRoom()
        {
            RoomAcousticsMath.BoxVolumeSurface(8.72f, 15f, 3.5f, out float v, out float s);
            float r02 = Rt60Calibration.ReflectionForRt60(0.2f, v, s);   // below the lowest measured value: clamped
            float r07 = Rt60Calibration.ReflectionForRt60(0.7f, v, s);
            float r22 = Rt60Calibration.ReflectionForRt60(2.2f, v, s);
            Assert.AreEqual(0.5f, r02, 1e-4f);
            Assert.AreEqual(0.81f, r07, 0.01f);
            Assert.AreEqual(0.952f, r22, 0.005f);
            Assert.Less(r02, r07);
            Assert.Less(r07, r22);
            Assert.AreEqual(0.7f, Rt60Calibration.Rt60ForReflection(r07, v, s), 0.01f);
            Assert.AreEqual(2.2f, Rt60Calibration.Rt60ForReflection(r22, v, s), 0.02f);
            // a room with twice the volume per surface has longer reverberation at the same coefficient, so it needs a lower one
            Assert.Less(Rt60Calibration.ReflectionForRt60(0.7f, v * 2f, s), r07);
        }
    }
}
