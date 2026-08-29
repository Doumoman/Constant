using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.Editor.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.SpecialRegions;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace StarNight.Map.Editor.Tests.WorldGeneration.SpecialRegions
{
    [TestFixture]
    [Category("MAP13_08")]
    public sealed class SpecialRegionValidationPreviewTests
    {
        [Test]
        public void ExactTenSelectorsAndThreeThreeFourFamilyMatrixArePublished()
        {
            var model = BuildModel();
            Assert.That(model.Artifacts, Has.Count.EqualTo(10));
            Assert.That(model.Artifacts.GroupBy(value => value.Family)
                .ToDictionary(value => value.Key, value => value.Count()), Is.EquivalentTo(
                new Dictionary<SpecialRegionAuditFamily, int>
                {
                    { SpecialRegionAuditFamily.Village, 3 },
                    { SpecialRegionAuditFamily.CoreResource, 3 },
                    { SpecialRegionAuditFamily.Landmark, 4 },
                }));
            Assert.That(model.Artifacts.Select(value => value.CanonicalOrder),
                Is.EqualTo(Enumerable.Range(0, 10)));
        }

        [Test]
        public void BindingLabelsAreExactEightReferenceAndTwoDeferred()
        {
            var report = BuildModel().AuditResult.Report;
            Assert.That(report.ReferenceFixtureCount, Is.EqualTo(8));
            Assert.That(report.DeferredToMAP14Count, Is.EqualTo(2));
            Assert.That(report.Artifacts.Where(value => value.Binding == SpecialRegionAuditBinding.DeferredToMAP14)
                .Select(value => value.ArtifactId), Is.EquivalentTo(new[]
                {
                    "SR_MARU_TIME_SHRINE_5", "SR_WANDERING_MERCHANT_CAVE_3",
                }));
        }

        [Test]
        public void PlacedFootprintCoverageSeamSiteAndBufferAuditPasses()
        {
            var report = BuildModel().AuditResult.Report;
            Assert.That(report.ArtifactCount, Is.EqualTo(10));
            Assert.That(report.SectionPassCount, Is.EqualTo(80));
            Assert.That(report.SectionFailCount, Is.Zero);
            foreach (var artifact in report.Artifacts.Where(value =>
                         value.Binding == SpecialRegionAuditBinding.ReferenceFixture))
            {
                var input = artifact.Input;
                Assert.That(input.Metrics.SectorCoverageCount,
                    Is.EqualTo(input.FootprintWidth * input.FootprintHeight), input.ArtifactId);
                if (input.FootprintWidth * input.FootprintHeight > 1)
                    Assert.That(input.Metrics.SeamCrossingCount, Is.GreaterThan(0), input.ArtifactId);
                Assert.That(input.Metrics.SiteBindingMatches, Is.True, input.ArtifactId);
                Assert.That(input.Metrics.BufferMatches, Is.True, input.ArtifactId);
            }
        }

        [Test]
        public void FixedAccessFiveSlotKindsAndPersistenceAreSeparated()
        {
            var report = BuildModel().AuditResult.Report;
            var kinds = report.Artifacts.SelectMany(value => value.Input.SlotKinds).Distinct().ToArray();
            Assert.That(kinds, Is.SupersetOf(new[]
            {
                SpecialRegionSlotKind.Facility, SpecialRegionSlotKind.Npc,
                SpecialRegionSlotKind.Enemy, SpecialRegionSlotKind.Event,
                SpecialRegionSlotKind.Reward,
            }));
            Assert.That(report.Artifacts.Where(value =>
                    value.Binding == SpecialRegionAuditBinding.ReferenceFixture)
                .All(value => value.Input.FixedCollisionCount > 0 && value.Input.FixedAccessCount > 0), Is.True);
            Assert.That(report.Artifacts.All(value =>
                value.Input.Metrics.FixedReplaceableOverlapCount == 0 && value.Input.Metrics.PersistenceMatches), Is.True);
        }

        [Test]
        public void EveryFamilyPublishesOrderedEntryTriggerRewardReturnWitnesses()
        {
            var report = BuildModel().AuditResult.Report;
            Assert.That(report.RouteCount, Is.EqualTo(46));
            foreach (var artifact in report.Artifacts)
            foreach (var route in artifact.Routes)
            {
                Assert.That(route.NodeIds.Count, Is.GreaterThanOrEqualTo(2), route.RouteId);
                Assert.That(route.Ordered, Is.True, route.RouteId);
                Assert.That(route.MandatoryNoTool, Is.True, route.RouteId);
            }
            Assert.That(report.Artifacts.All(value => value.Input.Metrics.RouteOrderMatches), Is.True);
        }

        [Test]
        public void EveryFailureRecoversAndSyntheticMutationCountersRemainZero()
        {
            var report = BuildModel().AuditResult.Report;
            Assert.That(report.Artifacts.All(value => value.Input.Metrics.UnrecoverableFailureCount == 0), Is.True);
            Assert.That(report.Artifacts.SelectMany(value => value.Routes)
                .Count(value => value.Recovery), Is.EqualTo(9));
            Assert.That(report.MutationClaimCount, Is.Zero);
            Assert.That(report.SolverClaimCount, Is.Zero);
            Assert.That(report.GameplayClaimCount, Is.Zero);
        }

        [Test]
        public void VillageCoreForgeBossAndOptionalStateResetCountsAreExact()
        {
            var artifacts = BuildModel().AuditResult.Report.Artifacts.ToDictionary(value => value.ArtifactId);
            AssertCounts(artifacts["SR_MAP13_08_VILLAGE_1X1"], 6, 5, 0);
            AssertCounts(artifacts["SR_MAP13_08_VILLAGE_1X2"], 6, 5, 0);
            AssertCounts(artifacts["SR_MAP13_08_VILLAGE_2X1"], 7, 5, 0);
            AssertCounts(artifacts["SR_CASSIA_SAP_SITE_5"], 3, 7, 1);
            AssertCounts(artifacts["SR_MOON_CORE_SITE_5"], 3, 7, 1);
            AssertCounts(artifacts["SR_STAR_NURUK_SITE_5"], 3, 7, 1);
            AssertCounts(artifacts["SR_MOON_SEAL_FORGE_9"], 6, 14, 3);
            AssertCounts(artifacts["SR_MOON_BOSS_SEAL_ARENA_12"], 5, 4, 3);
            AssertCounts(artifacts["SR_WANDERING_MERCHANT_CAVE_3"], 3, 3, 1);
            AssertCounts(artifacts["SR_MARU_TIME_SHRINE_5"], 4, 4, 2);
            Assert.That(artifacts["SR_MOON_SEAL_FORGE_9"].Input.PersistenceCheckpointCount, Is.EqualTo(7));
            Assert.That(artifacts.Values.Where(value => value.Family == SpecialRegionAuditFamily.CoreResource)
                .All(value => value.Input.PersistenceCheckpointCount == 7 && value.Input.RequiredRewardCount == 1), Is.True);
        }

        [Test]
        public void DeferredOptionalArtifactsPublishZeroWorldClaims()
        {
            foreach (var input in BuildModel().Artifacts.Where(value =>
                         value.Binding == SpecialRegionAuditBinding.DeferredToMAP14))
            {
                Assert.That(input.FootprintWidth + input.FootprintHeight + input.Metrics.SectorCoverageCount, Is.Zero);
                Assert.That(input.Metrics.WorldOriginClaimCount + input.Metrics.ReservationClaimCount +
                            input.Metrics.BridgeClaimCount + input.Metrics.PlacedOwnershipClaimCount, Is.Zero);
            }
        }

        [Test]
        public void ReverseRepeatTurkishCultureAndCollectionsRemainStable()
        {
            var model = BuildModel();
            var expected = model.AuditResult.CanonicalDigest;
            var reversed = SpecialRegionValidationAuditor.Audit(
                new SpecialRegionAuditRequest(model.Artifacts.Reverse()));
            Assert.That(reversed.Success, Is.True, string.Join("\n", reversed.Errors));
            Assert.That(reversed.CanonicalDigest, Is.EqualTo(expected));
            Assert.That(BuildModel().AuditResult.CanonicalDigest, Is.EqualTo(expected));

            var previousCulture = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                Assert.That(BuildModel().AuditResult.CanonicalDigest, Is.EqualTo(expected));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUi;
            }

            Assert.Throws<NotSupportedException>(() =>
                ((IList<SpecialRegionAuditArtifactInput>)model.Artifacts).Add(model.Artifacts[0]));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SpecialRegionAuditToken>)model.Artifacts[0].Tokens).Clear());
        }

        [Test]
        public void InvalidDigestDuplicateMissingOverlapRouteStateResetAndDeferredClaimsFailAtomically()
        {
            var source = BuildModel().Artifacts.ToArray();
            foreach (var code in new[]
                     {
                         SpecialRegionValidationAuditErrorCode.DigestMismatch,
                         SpecialRegionValidationAuditErrorCode.FixedReplaceableOverlap,
                         SpecialRegionValidationAuditErrorCode.RouteOrderMismatch,
                         SpecialRegionValidationAuditErrorCode.StateVariantMismatch,
                         SpecialRegionValidationAuditErrorCode.ResetMismatch,
                         SpecialRegionValidationAuditErrorCode.ResourceLossRisk,
                         SpecialRegionValidationAuditErrorCode.NonCanonicalPublication,
                     })
            {
                var invalid = source.ToArray();
                invalid[0] = invalid[0].WithViolation(code);
                AssertAtomicFailure(SpecialRegionValidationAuditor.Audit(new SpecialRegionAuditRequest(invalid)), code);
            }

            var deferred = source.ToArray();
            var deferredIndex = Array.FindIndex(deferred, value =>
                value.Binding == SpecialRegionAuditBinding.DeferredToMAP14);
            deferred[deferredIndex] = deferred[deferredIndex].WithViolation(
                SpecialRegionValidationAuditErrorCode.DeferredWorldClaim);
            AssertAtomicFailure(SpecialRegionValidationAuditor.Audit(new SpecialRegionAuditRequest(deferred)),
                SpecialRegionValidationAuditErrorCode.DeferredWorldClaim);

            var duplicate = source.Concat(new[] { source[0] }).ToArray();
            AssertAtomicFailure(SpecialRegionValidationAuditor.Audit(new SpecialRegionAuditRequest(duplicate)),
                SpecialRegionValidationAuditErrorCode.DuplicateArtifact);
            AssertAtomicFailure(SpecialRegionValidationAuditor.Audit(new SpecialRegionAuditRequest(source.Skip(1))),
                SpecialRegionValidationAuditErrorCode.MissingArtifact);
        }

        [Test]
        public void WindowContractPublishesMenuTitleMinimumSelectorsTogglesBannerLegendAndAudit()
        {
            Assert.That(SpecialRegionPreviewWindow.MenuPath,
                Is.EqualTo("Tools/MapDesign/Special Region Validator & Preview"));
            Assert.That(SpecialRegionPreviewWindow.WindowTitle,
                Is.EqualTo("Special Region Validator & Preview"));
            Assert.That(SpecialRegionPreviewWindow.MinimumWidth, Is.EqualTo(1000f));
            Assert.That(SpecialRegionPreviewWindow.MinimumHeight, Is.EqualTo(680f));
            Assert.That(Enum.GetValues(typeof(SpecialRegionPreviewViewMode)).Length, Is.EqualTo(8));

            var model = BuildModel();
            var snapshot = model.BuildDefault().Snapshot;
            Assert.That(snapshot.BindingBanner, Is.EqualTo("REFERENCE FIXTURE"));
            Assert.That(snapshot.PhysicsWarning, Is.EqualTo("PHYSICS NOT VERIFIED"));
            Assert.That(snapshot.Legend, Has.Count.EqualTo(18));
            Assert.That(snapshot.AuditSectionPassCount, Is.EqualTo(8));
            Assert.That(snapshot.AuditSectionFailCount, Is.Zero);
            Assert.That(snapshot.ScaleToFitTokenCount, Is.GreaterThan(0));
        }

        [Test]
        public void OpenReloadSelectionToggleCloseHasNoSceneAssetOrGeneratedMutation()
        {
            var activeScene = SceneManager.GetActiveScene();
            var rootCount = activeScene.GetRootGameObjects().Length;
            var dirty = activeScene.isDirty;
            var selected = Selection.activeObject;
            var inventory = FindMapDataAssets();

            var window = SpecialRegionPreviewWindow.Open();
            try
            {
                Assert.That(window.Reload(), Is.True, window.LastError);
                Assert.That(window.SelectorCount, Is.EqualTo(3));
                Assert.That(window.OverlayToggleCount, Is.EqualTo(13));
                Assert.That(window.PanelCount, Is.EqualTo(5));
                Assert.That(window.TrySelectFamily(SpecialRegionAuditFamily.Landmark), Is.True);
                Assert.That(window.TrySelectArtifact("SR_MOON_SEAL_FORGE_9"), Is.True);
                Assert.That(window.TrySelectViewMode(SpecialRegionPreviewViewMode.Audit), Is.True);
                Assert.That(window.TrySetOverlay(SpecialRegionPreviewOverlay.HighRoute, false), Is.True);
                Assert.That(window.CurrentSnapshot.BindingBanner, Is.EqualTo("REFERENCE FIXTURE"));
            }
            finally
            {
                window.Close();
            }

            Assert.That(activeScene.GetRootGameObjects().Length, Is.EqualTo(rootCount));
            Assert.That(activeScene.isDirty, Is.EqualTo(dirty));
            Assert.That(Selection.activeObject, Is.SameAs(selected));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(activeScene.path));
            Assert.That(FindMapDataAssets(), Is.EqualTo(inventory));
        }

        private static string[] FindMapDataAssets()
        {
            var folders = new[]
            {
                "Assets/_Game/MapData/Authoring", "Assets/_Game/MapData/Generated",
            }.Where(AssetDatabase.IsValidFolder).ToArray();
            return folders.Length == 0
                ? Array.Empty<string>()
                : AssetDatabase.FindAssets(string.Empty, folders)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static SpecialRegionPreviewModel BuildModel()
        {
            var model = new SpecialRegionPreviewModel();
            Assert.That(model.AuditResult, Is.Not.Null);
            Assert.That(model.AuditResult.Success, Is.True, string.Join("\n", model.AuditResult.Errors));
            return model;
        }

        private static void AssertCounts(
            SpecialRegionAuditArtifactResult artifact,
            int routes,
            int states,
            int resets)
        {
            Assert.That(artifact.Routes, Has.Count.EqualTo(routes), artifact.ArtifactId);
            Assert.That(artifact.Input.StateCount, Is.EqualTo(states), artifact.ArtifactId);
            Assert.That(artifact.Input.ResetCount, Is.EqualTo(resets), artifact.ArtifactId);
        }

        private static void AssertAtomicFailure(
            SpecialRegionValidationAuditResult result,
            SpecialRegionValidationAuditErrorCode expected)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Report, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(expected));
            Assert.That(result.Errors, Is.Ordered);
        }
    }
}
