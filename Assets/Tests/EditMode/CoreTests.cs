using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace HumPer.Tests
{
    public class CoreTests
    {
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
    }
}
