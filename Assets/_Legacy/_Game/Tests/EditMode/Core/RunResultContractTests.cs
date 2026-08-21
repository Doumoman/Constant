#if LEGACY_DISABLED
using System.IO;
using NUnit.Framework;
using StarNight.Core.Save;
using StarNight.Core.State;

namespace StarNight.Core.Tests
{
    public sealed class RunResultContractTests
    {
        [Test]
        public void CompleteRunKeepsExactlyOneEndingCategory()
        {
            var manager = new RunManager(() => 10);
            RunState run = manager.StartNewRun();
            run.flags.Add(RunManager.NormalEndingFlag);
            run.flags.Add(RunManager.MemoryEndingFlag);
            run.flags.Add(RunManager.ChallengeEndingFlag);

            Assert.That(manager.CompleteRun(RunPhase.ClearedMemory, "memory_bell"), Is.True);

            Assert.That(run.phase, Is.EqualTo(RunPhase.ClearedMemory));
            Assert.That(run.endingId, Is.EqualTo("memory_bell"));
            Assert.That(run.flags, Does.Contain(RunManager.MemoryEndingFlag));
            Assert.That(run.flags, Does.Not.Contain(RunManager.NormalEndingFlag));
            Assert.That(run.flags, Does.Not.Contain(RunManager.ChallengeEndingFlag));
        }

        [Test]
        public void SnapshotCapturesFailureStageTimePeakAndDiscoveryCounts()
        {
            var manager = new RunManager(() => 20);
            RunState run = manager.StartNewRun();
            run.currentStageId = "3-2";
            run.runTime = 92.75f;
            run.moneyWon = 700;
            run.peakMoney = 1250;
            run.flags.Add(RunResultSnapshot.HelpedEventFlagPrefix + "SUN_NEST");
            run.flags.Add(RunResultSnapshot.MemoryTravelerFlagPrefix + "DABOK");
            Assert.That(manager.FailRun("maru_bite"), Is.True);

            RunResultSnapshot result = RunResultSnapshot.Capture(run);

            Assert.That(result.phase, Is.EqualTo(RunPhase.Failed));
            Assert.That(result.failureReason, Is.EqualTo("maru_bite"));
            Assert.That(result.reachedStageId, Is.EqualTo("3-2"));
            Assert.That(result.runTime, Is.EqualTo(92.75f));
            Assert.That(result.peakMoney, Is.EqualTo(1250));
            Assert.That(result.helpedEventCount, Is.EqualTo(1));
            Assert.That(result.memoryTravelerCount, Is.EqualTo(1));
        }

        [Test]
        public void RecordRepositoryPersistsEndingsDiscoveriesAndBestRun()
        {
            string directory = Path.Combine(Path.GetTempPath(), "StarNight.RunRecordTests", TestContext.CurrentContext.Test.ID);
            string path = Path.Combine(directory, RunRecordRepository.FileName);
            Directory.CreateDirectory(directory);
            try
            {
                var manager = new RunManager(() => 30);
                RunState run = manager.StartNewRun();
                run.currentStageId = "5-3";
                run.runTime = 180f;
                run.flags.Add(RunResultSnapshot.MemoryTravelerFlagPrefix + "RANI");
                run.flags.Add(RunRecordRepository.FolkloreFlagPrefix + "MOON_RABBIT");
                Assert.That(manager.CompleteRun(RunPhase.ClearedChallenge, "starless_sea"), Is.True);

                var repository = new RunRecordRepository(path);
                repository.Load();
                repository.Record(RunResultSnapshot.Capture(run), run);

                RunRecordData loaded = new RunRecordRepository(path).Load();
                Assert.That(loaded.viewedEndingIds, Is.EquivalentTo(new[] { "starless_sea" }));
                Assert.That(loaded.metMemoryTravelerIds, Is.EquivalentTo(new[] { "RANI" }));
                Assert.That(loaded.discoveredFolkloreIds, Is.EquivalentTo(new[] { "MOON_RABBIT" }));
                Assert.That(loaded.highestReachedStage, Is.EqualTo("5-3"));
                Assert.That(loaded.bestClearedRunTime, Is.EqualTo(180f));
                Assert.That(loaded.completedRunCount, Is.EqualTo(1));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}

#endif
