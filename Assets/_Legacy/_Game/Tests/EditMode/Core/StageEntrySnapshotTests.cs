#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Core.State;

namespace StarNight.Core.Tests
{
    public sealed class StageEntrySnapshotTests
    {
        [Test]
        public void Restore_RewindsResourcesAndCollectionsWithoutSharingReferences()
        {
            RunState run = RunState.CreateNew(17);
            run.currentStageId = "1-1";
            run.health = 3;
            run.moneyWon = 200;
            run.items.Add("moon-cake");
            run.flags.Add("met-rabbit");
            StageEntrySnapshot snapshot = StageEntrySnapshot.Capture(run);

            run.health = 1;
            run.moneyWon = 900;
            run.items.Add("must-disappear");
            run.flags.Clear();
            snapshot.RestoreInto(run);

            Assert.That(run.health, Is.EqualTo(3));
            Assert.That(run.moneyWon, Is.EqualTo(200));
            Assert.That(run.items, Is.EquivalentTo(new[] { "moon-cake" }));
            Assert.That(run.flags, Is.EquivalentTo(new[] { "met-rabbit" }));
            run.items.Add("after-restore");
            Assert.That(snapshot.items, Has.None.EqualTo("after-restore"));
        }
    }
}

#endif
