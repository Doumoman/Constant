#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Core.Save;
using StarNight.Core.State;

namespace StarNight.Core.Tests
{
    public sealed class RunManagerTests
    {
        [Test]
        public void StartNewRun_UsesDocumentedInitialValues()
        {
            var manager = new RunManager(() => 12345);

            RunState run = manager.StartNewRun();

            Assert.That(run.seed, Is.EqualTo(12345));
            Assert.That(run.phase, Is.EqualTo(RunPhase.Running));
            Assert.That(run.currentStageId, Is.EqualTo("0-1"));
            Assert.That(run.health, Is.EqualTo(4));
            Assert.That(run.ropes, Is.EqualTo(4));
            Assert.That(run.bombs, Is.EqualTo(4));
            Assert.That(run.moneyWon, Is.Zero);
            Assert.That(run.handToolId, Is.Empty);
            Assert.That(run.lanternAvailable, Is.True);
        }

        [Test]
        public void TwentyNewRunsNeverKeepPreviousFlagsAndDoNotTouchSettings()
        {
            int seed = 0;
            var manager = new RunManager(() => ++seed);
            SettingsData settings = SettingsData.CreateDefault();
            settings.audio.masterVolume = 3;

            for (int index = 0; index < 20; index++)
            {
                RunState run = manager.StartNewRun();
                Assert.That(run.flags, Is.Empty);
                Assert.That(run.items, Is.Empty);
                Assert.That(run.visitedStages, Is.Empty);
                Assert.That(run.actionRecords, Is.Empty);

                run.flags.Add("must-not-survive");
                run.items.Add("must-not-survive");
            }

            Assert.That(settings.audio.masterVolume, Is.EqualTo(3));
            Assert.That(manager.Current.seed, Is.EqualTo(20));
        }

        [Test]
        public void AbandonRun_RemovesContinueState()
        {
            var manager = new RunManager(() => 7);
            manager.StartNewRun();

            manager.AbandonRun();

            Assert.That(manager.HasActiveRun, Is.False);
            Assert.That(manager.Current, Is.Null);
        }
    }
}

#endif
