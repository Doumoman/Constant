#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Debugging;
using StarNight.Folklore.P9;
using StarNight.Generation.P6;
using StarNight.Rooms;
using UnityEditor;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    public sealed class P9FolkloreRecordTests
    {
        private const string CatalogPath =
            "Assets/StarNight/Data/P9/P9_RecordGuestCatalog.asset";

        [Test]
        public void FolkloreChain_MatchingGiftsResolveAndGrantBranchRelics()
        {
            GameObject root = new GameObject("P9ChainTest");
            try
            {
                P9FolkloreChainState2D state =
                    root.AddComponent<P9FolkloreChainState2D>();
                state.Configure(true, true);

                Assert.That(
                    state.TryResolveWithGift(
                        P9CorrespondenceEventKind.HungryMagpie,
                        P9FolkloreItemKind.JadeRabbitMedicine),
                    Is.False);
                Assert.That(
                    state.TryResolveWithGift(
                        P9CorrespondenceEventKind.HungryMagpie,
                        P9FolkloreItemKind.MoonCake),
                    Is.True);
                Assert.That(state.HasMoonCake, Is.False);
                Assert.That(state.HasJadeRabbitMedicine, Is.True);
                Assert.That(
                    state.TryGrantBranchRelic(P9BranchKind.MagpieBridge),
                    Is.True);
                Assert.That(state.HasRedWeaverThread, Is.True);
                Assert.That(state.CanOpenPostOfficeLetterDrawer, Is.True);
                Assert.That(
                    state.CanEnterOppositeBranchAfter(
                        P9BranchKind.MagpieBridge),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FolkloreChain_AlternativeEventsAndIgnoringThemNeverBlockExit()
        {
            GameObject root = new GameObject("P9OptionalTest");
            try
            {
                P9FolkloreChainState2D state =
                    root.AddComponent<P9FolkloreChainState2D>();
                state.Configure(false, false);

                Assert.That(
                    state.TryResolveWithAlternative(
                        P9CorrespondenceEventKind.HungryMagpie),
                    Is.True);
                Assert.That(
                    state.ResolutionFor(
                        P9CorrespondenceEventKind.HungryMagpie),
                    Is.EqualTo(
                        P9CorrespondenceResolution.AlternativeRescue));
                Assert.That(state.MainProgressAlwaysAvailable, Is.True);
                Assert.That(
                    state.OptionalEventsIgnoredWithoutPenalty,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RecordGuestCatalog_HasExactSixRegionalHelpContracts()
        {
            P9RecordGuestCatalog catalog =
                AssetDatabase.LoadAssetAtPath<P9RecordGuestCatalog>(
                    CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Definitions.Count, Is.EqualTo(6));
            Assert.That(
                catalog.Definitions
                    .Select(item => item.GuestId)
                    .Distinct()
                    .Count(),
                Is.EqualTo(6));
            Assert.That(
                catalog.Definitions
                    .Select(item => item.Region)
                    .Distinct()
                    .Count(),
                Is.EqualTo(6));
            Assert.That(
                catalog.Definitions.All(
                    item => !string.IsNullOrWhiteSpace(item.HelpSentence)
                        && item.RequiresCulturalReview),
                Is.True);
            Assert.That(
                catalog.FindForRegion(RoomRegion.MoonPalace).GuestId,
                Is.EqualTo("record_seo_bok"));
            Assert.That(
                catalog.FindForRegion(RoomRegion.MagpieBridge)
                    .ImmediateSupport,
                Is.EqualTo(
                    P9RecordGuestImmediateSupport
                        .SafeMainAndOptionalRoute));
        }

        [Test]
        public void RecordGuestDirector_X2GuaranteesAndOtherStagesUseFifteenPercent()
        {
            Assert.That(
                P9RecordGuestDirector2D.ShouldPlaceArchive(
                    P6StageSlot.X2,
                    0.99f),
                Is.True);
            Assert.That(
                P9RecordGuestDirector2D.ShouldPlaceArchive(
                    P6StageSlot.X1,
                    0.149f),
                Is.True);
            Assert.That(
                P9RecordGuestDirector2D.ShouldPlaceArchive(
                    P6StageSlot.X3,
                    0.151f),
                Is.False);
            Assert.That(
                P9RecordGuestDirector2D.OptionalStageArchiveChance,
                Is.EqualTo(0.15f));
        }

        [Test]
        public void StarArchive_HasMultipleOpeningsAndCanBeIgnored()
        {
            GameObject root = new GameObject("P9ArchiveTest");
            GameObject closed = new GameObject("Closed");
            GameObject opened = new GameObject("Opened");
            GameObject cue = new GameObject("Cue");
            try
            {
                closed.transform.SetParent(root.transform);
                opened.transform.SetParent(root.transform);
                cue.transform.SetParent(root.transform);
                P9StarArchive2D archive =
                    root.AddComponent<P9StarArchive2D>();
                archive.Configure(
                    P9ArchiveUnlockMethods.SealLever
                    | P9ArchiveUnlockMethods.CrackedOuterWall
                    | P9ArchiveUnlockMethods.HookLatch,
                    cue.transform,
                    closed,
                    opened);

                Assert.That(archive.UnlockMethodCount, Is.EqualTo(3));
                Assert.That(archive.BombIsNotTheOnlySolution, Is.True);
                Assert.That(archive.IgnoreAndContinue(), Is.True);
                Assert.That(
                    archive.TryOpen(P9ArchiveUnlockMethods.HookLatch),
                    Is.True);
                Assert.That(archive.IsOpen, Is.True);
                Assert.That(closed.activeSelf, Is.False);
                Assert.That(opened.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RecordGuestFollower_UsesHelpOnceAndNeverFightsOrTakesDamage()
        {
            P9RecordGuestCatalog catalog =
                AssetDatabase.LoadAssetAtPath<P9RecordGuestCatalog>(
                    CatalogPath);
            GameObject root = new GameObject("P9FollowerTest");
            GameObject target = new GameObject("Target");
            GameObject visual = new GameObject("Visual");
            try
            {
                visual.transform.SetParent(root.transform);
                P9RecordGuestFollower2D follower =
                    root.AddComponent<P9RecordGuestFollower2D>();
                follower.Configure(
                    catalog.FindForRegion(RoomRegion.MoonPalace),
                    target.transform,
                    visual.transform,
                    new Vector3(3f, 4f, 0f));

                Assert.That(follower.Rescue(), Is.True);
                Assert.That(follower.TryUseSupport(), Is.True);
                Assert.That(follower.TryUseSupport(), Is.False);
                Assert.That(follower.HasCombatAi, Is.False);
                Assert.That(follower.CanTakeDamage, Is.False);
                Assert.That(follower.ReceivesTerrainDamage, Is.False);
                follower.ReturnToArchive();
                Assert.That(follower.IsRescued, Is.False);
                Assert.That(root.transform.position, Is.EqualTo(
                    new Vector3(3f, 4f, 0f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ComprehensionTelemetry_UsesExactEightyAndEightyFiveGates()
        {
            GameObject root = new GameObject("P9TelemetryTest");
            try
            {
                P9ComprehensionTelemetry2D telemetry =
                    root.AddComponent<P9ComprehensionTelemetry2D>();
                for (int index = 0; index < 5; index++)
                {
                    telemetry.RecordGiftInference(index < 4);
                }

                for (int index = 0; index < 20; index++)
                {
                    telemetry.RecordGuestHelpUnderstanding(index < 17);
                }

                Assert.That(telemetry.GiftInferenceRate, Is.EqualTo(0.8f));
                Assert.That(
                    telemetry.GuestHelpUnderstandingRate,
                    Is.EqualTo(0.85f));
                Assert.That(telemetry.GiftGatePassed, Is.True);
                Assert.That(telemetry.GuestHelpGatePassed, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CorridorReview_RemainsAnExplicitP9Followup()
        {
            Assert.That(
                P9FolkloreRecordLabContract.CorridorReviewText,
                Does.Contain("corridor"));
            Assert.That(
                P9FolkloreRecordLabContract.CorridorReviewText,
                Does.Contain("follow-up"));
        }
    }
}

#endif
