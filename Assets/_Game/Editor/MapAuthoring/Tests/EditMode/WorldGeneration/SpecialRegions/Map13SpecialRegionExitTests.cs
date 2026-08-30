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
    [Category("MAP13_09")]
    public sealed class Map13SpecialRegionExitTests
    {
        private const string AuditDigest =
            "a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e";
        private const string VillageOneByOne = "SR_MAP13_08_VILLAGE_1X1";
        private const string VillageOneByTwo = "SR_MAP13_08_VILLAGE_1X2";
        private const string VillageTwoByOne = "SR_MAP13_08_VILLAGE_2X1";
        private const string Forge = "SR_MOON_SEAL_FORGE_9";
        private const string Boss = "SR_MOON_BOSS_SEAL_ARENA_12";
        private const string Merchant = "SR_WANDERING_MERCHANT_CAVE_3";
        private const string Maru = "SR_MARU_TIME_SHRINE_5";

        [Test]
        public void CanonicalPublicationPublishesExactPhaseExitTotals()
        {
            var model = BuildModel();
            var report = model.AuditResult.Report;

            Assert.That(report.ArtifactCount, Is.EqualTo(10));
            Assert.That(report.Artifacts.GroupBy(value => value.Family)
                .ToDictionary(value => value.Key, value => value.Count()), Is.EquivalentTo(
                new Dictionary<SpecialRegionAuditFamily, int>
                {
                    { SpecialRegionAuditFamily.Village, 3 },
                    { SpecialRegionAuditFamily.CoreResource, 3 },
                    { SpecialRegionAuditFamily.Landmark, 4 },
                }));
            Assert.That(report.ReferenceFixtureCount, Is.EqualTo(8));
            Assert.That(report.DeferredToMAP14Count, Is.EqualTo(2));
            Assert.That(report.SectionPassCount, Is.EqualTo(80));
            Assert.That(report.SectionFailCount, Is.Zero);
            Assert.That(model.AuditResult.Errors, Is.Empty);
            Assert.That(report.RouteCount, Is.EqualTo(46));
            Assert.That(report.Artifacts.SelectMany(value => value.Routes).Count(value => value.Recovery),
                Is.EqualTo(9));
            Assert.That(report.StateCount, Is.EqualTo(61));
            Assert.That(report.ResetCount, Is.EqualTo(12));
            Assert.That(report.PersistenceCheckpointCount, Is.EqualTo(28));
            Assert.That(report.Artifacts.Sum(value => value.Tokens.Count), Is.EqualTo(435));
            Assert.That(report.MutationClaimCount, Is.Zero);
            Assert.That(report.SolverClaimCount, Is.Zero);
            Assert.That(report.GameplayClaimCount, Is.Zero);
        }

        [Test]
        public void CanonicalDigestsRemainLowerHexAcrossRepeatReverseAndTurkishCulture()
        {
            var model = BuildModel();
            var report = model.AuditResult.Report;
            var digests = model.Artifacts.SelectMany(value => new[]
                {
                    value.SourceDigest, value.ComponentDigest, value.ArtifactDigest,
                })
                .Concat(report.Artifacts.Select(value => value.CanonicalDigest))
                .Concat(report.Artifacts.SelectMany(value => value.Sections)
                    .Select(value => value.CanonicalDigest))
                .Concat(new[] { report.CanonicalDigest, model.AuditResult.CanonicalDigest })
                .ToArray();

            Assert.That(digests, Has.Length.EqualTo(122));
            foreach (var digest in digests)
                Assert.That(IsLowerSha256(digest), Is.True, digest);
            Assert.That(model.AuditResult.CanonicalDigest, Is.EqualTo(AuditDigest));
            Assert.That(BuildModel().AuditResult.CanonicalDigest, Is.EqualTo(AuditDigest));

            var reversed = SpecialRegionValidationAuditor.Audit(
                new SpecialRegionAuditRequest(model.Artifacts.Reverse()));
            Assert.That(reversed.Success, Is.True, string.Join("\n", reversed.Errors));
            Assert.That(reversed.CanonicalDigest, Is.EqualTo(AuditDigest));

            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                Assert.That(BuildModel().AuditResult.CanonicalDigest, Is.EqualTo(AuditDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void MandatoryReferenceSitesClosePlacementAndOverlapWithoutVillageRewardDependency()
        {
            var report = BuildModel().AuditResult.Report;
            var placed = report.Artifacts.Where(value =>
                value.Binding == SpecialRegionAuditBinding.ReferenceFixture).ToArray();

            Assert.That(placed, Has.Length.EqualTo(8));
            Assert.That(placed.Count(value => value.Family == SpecialRegionAuditFamily.Village), Is.EqualTo(3));
            Assert.That(placed.Count(value => value.Family == SpecialRegionAuditFamily.CoreResource), Is.EqualTo(3));
            Assert.That(placed.Where(value => value.Family == SpecialRegionAuditFamily.Landmark)
                .Select(value => value.ArtifactId), Is.EquivalentTo(new[] { Forge, Boss }));
            Assert.That(report.Artifacts.Select(value => value.ArtifactId).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(10));

            foreach (var artifact in placed)
            {
                var input = artifact.Input;
                var metrics = input.Metrics;
                Assert.That(metrics.WorldOriginClaimCount, Is.EqualTo(1), input.ArtifactId);
                Assert.That(metrics.ReservationClaimCount, Is.EqualTo(1), input.ArtifactId);
                Assert.That(metrics.BridgeClaimCount, Is.EqualTo(1), input.ArtifactId);
                Assert.That(metrics.PlacedOwnershipClaimCount, Is.EqualTo(1), input.ArtifactId);
                Assert.That(metrics.SectorCoverageCount,
                    Is.EqualTo(input.FootprintWidth * input.FootprintHeight), input.ArtifactId);
                Assert.That(metrics.SiteBindingMatches, Is.True, input.ArtifactId);
                Assert.That(metrics.BufferMatches, Is.True, input.ArtifactId);
                Assert.That(metrics.CollisionOwnerMatches, Is.True, input.ArtifactId);
                Assert.That(metrics.FixedReplaceableOverlapCount, Is.Zero, input.ArtifactId);
                Assert.That(input.FixedCollisionCount, Is.GreaterThan(0), input.ArtifactId);
                Assert.That(input.FixedAccessCount, Is.GreaterThan(0), input.ArtifactId);
                Assert.That(metrics.MandatoryToolDependencyCount, Is.Zero, input.ArtifactId);
                Assert.That(metrics.UnrecoverableFailureCount, Is.Zero, input.ArtifactId);
                Assert.That(metrics.MutationClaimCount, Is.Zero, input.ArtifactId);
                Assert.That(artifact.Routes.All(value => value.Ordered && value.MandatoryNoTool),
                    Is.True, input.ArtifactId);
            }

            Assert.That(placed.Where(value => value.Family == SpecialRegionAuditFamily.Village)
                .All(value => value.Input.RequiredRewardCount == 0), Is.True);
            Assert.That(report.MutationClaimCount + report.SolverClaimCount + report.GameplayClaimCount, Is.Zero);
        }

        [Test]
        public void VillagesPublishFiveShellPreservingLocalVariantsFacilitiesAndSeams()
        {
            var report = BuildModel().AuditResult.Report;
            var expected = new Dictionary<string, Tuple<int, int, int>>
            {
                { VillageOneByOne, Tuple.Create(1, 1, 6) },
                { VillageOneByTwo, Tuple.Create(1, 2, 6) },
                { VillageTwoByOne, Tuple.Create(2, 1, 7) },
            };

            foreach (var pair in expected)
            {
                var artifact = Artifact(report, pair.Key);
                var input = artifact.Input;
                Assert.That(input.FootprintWidth, Is.EqualTo(pair.Value.Item1), pair.Key);
                Assert.That(input.FootprintHeight, Is.EqualTo(pair.Value.Item2), pair.Key);
                Assert.That(artifact.Routes, Has.Count.EqualTo(pair.Value.Item3), pair.Key);
                Assert.That(input.StateCount, Is.EqualTo(5), pair.Key);
                Assert.That(input.ResetCount, Is.Zero, pair.Key);
                Assert.That(input.PersistenceCheckpointCount, Is.Zero, pair.Key);
                Assert.That(input.RequiredRewardCount, Is.Zero, pair.Key);
                Assert.That(input.Metrics.StateVariantMatches, Is.True, pair.Key);
                Assert.That(input.Metrics.MutationClaimCount, Is.Zero, pair.Key);

                var road = artifact.Routes.Single(value => value.RouteKind == "Low");
                Assert.That(road.NodeIds, Is.EqualTo(new[] { "Entry", "CentralRoad", "Return" }), pair.Key);
                var facilities = artifact.Routes.Where(value => value.RouteKind == "FacilityAccess").ToArray();
                Assert.That(facilities, Has.Length.EqualTo(pair.Value.Item3 - 1), pair.Key);
                foreach (var route in facilities)
                    Assert.That(route.NodeIds, Is.EqualTo(new[]
                    {
                        "Entry", "CentralRoad", route.NodeIds[2], "Access", "CentralRoad", "Return",
                    }), route.RouteId);

                if (input.FootprintWidth * input.FootprintHeight > 1)
                    Assert.That(input.Metrics.SeamCrossingCount, Is.GreaterThan(0), pair.Key);
            }
        }

        [Test]
        public void CoreResourcesPublishOneRewardSevenCheckpointsAndRecoveryRejoin()
        {
            var report = BuildModel().AuditResult.Report;
            var cores = report.Artifacts.Where(value =>
                value.Family == SpecialRegionAuditFamily.CoreResource).ToArray();

            Assert.That(cores, Has.Length.EqualTo(3));
            Assert.That(CoreResourceRegionStarterCatalog.Entries.Select(value => value.Resource),
                Is.EquivalentTo(new[]
                {
                    CoreResourceKind.MoonCore, CoreResourceKind.CassiaSap, CoreResourceKind.StarNuruk,
                }));
            foreach (var artifact in cores)
            {
                var input = artifact.Input;
                var definition = CoreResourceRegionStarterCatalog.Entries.Single(value =>
                    string.Equals(value.RegionId.Value, artifact.ArtifactId, StringComparison.Ordinal));
                var low = artifact.Routes.Single(value => value.RouteKind == "Low");
                var recovery = artifact.Routes.Single(value => value.Recovery);

                Assert.That(artifact.Routes, Has.Count.EqualTo(3), artifact.ArtifactId);
                Assert.That(input.RequiredRewardCount, Is.EqualTo(1), artifact.ArtifactId);
                Assert.That(input.PersistenceCheckpointCount, Is.EqualTo(7), artifact.ArtifactId);
                Assert.That(input.ResetCount, Is.EqualTo(1), artifact.ArtifactId);
                Assert.That(definition.RequiredReward, Is.Not.Null, artifact.ArtifactId);
                Assert.That(definition.RequiredReward.Required, Is.True, artifact.ArtifactId);
                Assert.That(definition.RequiredReward.Amount, Is.EqualTo(1), artifact.ArtifactId);
                Assert.That(definition.Recoveries, Has.Count.EqualTo(1), artifact.ArtifactId);
                Assert.That(definition.Edges.Where(value => value.RouteKind == CoreResourceRouteKind.Low)
                    .All(value => value.Dependency == CoreResourceDependencyKind.None), Is.True, artifact.ArtifactId);
                Assert.That(input.SlotKinds.Contains(SpecialRegionSlotKind.Facility), Is.False, artifact.ArtifactId);
                Assert.That(definition.Nodes.Single(value => value.NodeId == recovery.NodeIds.First()).Role,
                    Is.EqualTo(CoreResourceNodeRole.Failure), artifact.ArtifactId);
                Assert.That(definition.Nodes.Single(value => value.NodeId == recovery.NodeIds.Last()).Role,
                    Is.EqualTo(CoreResourceNodeRole.RecoveryJoin), artifact.ArtifactId);
                Assert.That(low.NodeIds, Does.Contain(recovery.NodeIds.Last()), artifact.ArtifactId);
                Assert.That(input.Metrics.PersistenceMatches, Is.True, artifact.ArtifactId);
                Assert.That(input.Metrics.MandatoryToolDependencyCount, Is.Zero, artifact.ArtifactId);
                Assert.That(input.Metrics.ResourceLossRiskCount, Is.Zero, artifact.ArtifactId);
                Assert.That(input.Metrics.DuplicateBenefitRiskCount, Is.Zero, artifact.ArtifactId);
                Assert.That(input.Metrics.MutationClaimCount, Is.Zero, artifact.ArtifactId);
            }
        }

        [Test]
        public void ForgePublishesOrderedMoonSealProcessAndLosslessManualResets()
        {
            var report = BuildModel().AuditResult.Report;
            var artifact = Artifact(report, Forge);
            var definition = SpecialLandmarkRegionStarterCatalog.GetDefinition(SpecialLandmarkKind.MoonSealForge);
            var process = definition.Markers
                .Where(value => value.Kind == SpecialLandmarkMarkerKind.ForgeProcessStep)
                .OrderBy(value => value.Order).Select(value => value.NodeId).ToArray();
            var expectedProcess = new[]
            {
                "SL_NODE_FORGE_GRIND", "SL_NODE_FORGE_MIX", "SL_NODE_FORGE_PRESS", "SL_NODE_FORGE_CURE",
            };
            var low = artifact.Routes.Single(value => value.RouteKind == "Low");

            Assert.That(process, Is.EqualTo(expectedProcess));
            Assert.That(low.NodeIds, Is.EqualTo(new[]
            {
                "SL_NODE_FORGE_ENTRY", "SL_NODE_FORGE_GRIND", "SL_NODE_FORGE_MIX",
                "SL_NODE_FORGE_PRESS", "SL_NODE_FORGE_CURE", "SL_NODE_FORGE_REWARD",
                "SL_NODE_FORGE_RETURN",
            }));
            Assert.That(definition.Markers.Single(value =>
                value.Kind == SpecialLandmarkMarkerKind.MoonSealOutput).NodeId,
                Is.EqualTo("SL_NODE_FORGE_REWARD"));
            Assert.That(definition.RequiredReward, Is.Not.Null);
            Assert.That(definition.RequiredReward.RewardId, Is.EqualTo("SL_REWARD_MOON_SEAL"));
            Assert.That(definition.RequiredReward.Required, Is.True);
            Assert.That(definition.RequiredReward.Amount, Is.EqualTo(1));
            Assert.That(artifact.Input.RequiredRewardCount, Is.EqualTo(1));
            Assert.That(artifact.Input.PersistenceCheckpointCount, Is.EqualTo(7));
            Assert.That(definition.ForgeLedgers.Select(value => value.Resource), Is.EqualTo(new[]
            {
                SpecialLandmarkForgeResource.MoonCore,
                SpecialLandmarkForgeResource.CassiaSap,
                SpecialLandmarkForgeResource.StarNuruk,
            }));
            Assert.That(definition.Resets, Has.Count.EqualTo(3));
            Assert.That(definition.Resets.All(value => value.ReturnsAllForgeInputs &&
                value.Policy == SpecialLandmarkResetPolicy.ManualReset &&
                value.RecoveryNodeId == "SL_NODE_FORGE_SAFE_CORRIDOR"), Is.True);
            Assert.That(artifact.Routes.Count(value => value.Recovery), Is.EqualTo(3));
            foreach (var route in artifact.Routes.Where(value => value.Recovery))
            {
                Assert.That(route.NodeIds, Does.Contain("SL_NODE_FORGE_SAFE_CORRIDOR"), route.RouteId);
                Assert.That(route.NodeIds.Last(), Is.EqualTo("SL_NODE_FORGE_GRIND"), route.RouteId);
            }
            Assert.That(artifact.Input.Metrics.ResourceLossRiskCount, Is.Zero);
            Assert.That(artifact.Input.Metrics.DuplicateBenefitRiskCount, Is.Zero);
            Assert.That(artifact.Input.Metrics.MutationClaimCount, Is.Zero);
        }

        [Test]
        public void BossPublishesSealGateStateOrderAcceptedResetAndCentralRecovery()
        {
            var model = BuildModel();
            var artifact = Artifact(model.AuditResult.Report, Boss);
            var definition = SpecialLandmarkRegionStarterCatalog.GetDefinition(SpecialLandmarkKind.BossSealArena);
            var transitions = definition.Transitions.OrderBy(value => value.Order).ToArray();

            Assert.That(definition.States.Select(value => value.Role), Is.EquivalentTo(new[]
            {
                SpecialLandmarkStateRole.GateLocked,
                SpecialLandmarkStateRole.GateAccepted,
                SpecialLandmarkStateRole.EncounterActive,
                SpecialLandmarkStateRole.Defeated,
            }));
            Assert.That(transitions.Select(value => value.Trigger), Is.EqualTo(new[]
            {
                SpecialLandmarkTransitionTrigger.PresentMoonSeal,
                SpecialLandmarkTransitionTrigger.EnterEncounter,
                SpecialLandmarkTransitionTrigger.EncounterFailed,
                SpecialLandmarkTransitionTrigger.BossDefeated,
            }));
            Assert.That(transitions[0].FromStateId, Is.EqualTo("SL_STATE_BOSS_GATE_LOCKED"));
            Assert.That(transitions[0].ToStateId, Is.EqualTo("SL_STATE_BOSS_GATE_ACCEPTED"));
            Assert.That(transitions[1].ToStateId, Is.EqualTo("SL_STATE_BOSS_ENCOUNTER_ACTIVE"));
            Assert.That(transitions[3].ToStateId, Is.EqualTo("SL_STATE_BOSS_DEFEATED"));
            Assert.That(definition.Markers.Count(value =>
                value.Kind == SpecialLandmarkMarkerKind.MoonSealRequirement && value.Required), Is.EqualTo(1));

            var encounterReset = definition.Resets.Single(value =>
                value.Policy == SpecialLandmarkResetPolicy.EncounterReset);
            Assert.That(encounterReset.PreservesSealAcceptance, Is.True);
            Assert.That(encounterReset.FromStateId, Is.EqualTo("SL_STATE_BOSS_ENCOUNTER_ACTIVE"));
            Assert.That(encounterReset.ToStateId, Is.EqualTo("SL_STATE_BOSS_ENCOUNTER_ACTIVE"));
            var failures = definition.Nodes.Where(value => value.Role == SpecialLandmarkNodeRole.Failure).ToArray();
            Assert.That(failures, Has.Length.EqualTo(2));
            Assert.That(failures.All(failure => definition.Resets.Any(reset =>
                reset.FailureNodeId == failure.NodeId &&
                reset.RecoveryNodeId == "SL_NODE_BOSS_CENTRAL_RECOVERY")), Is.True);
            Assert.That(artifact.Routes.Count(value => value.Recovery), Is.EqualTo(2));
            Assert.That(artifact.Routes.Where(value => value.Recovery)
                .All(value => value.NodeIds.Last() == "SL_NODE_BOSS_CENTRAL_RECOVERY"), Is.True);
            Assert.That(definition.IntroducesNewMovementRule, Is.False);
            Assert.That(artifact.Input.StateCount, Is.EqualTo(4));
            Assert.That(artifact.Input.ResetCount, Is.EqualTo(3));
            Assert.That(artifact.Input.Metrics.MandatoryToolDependencyCount, Is.Zero);
            Assert.That(artifact.Input.Metrics.MutationClaimCount, Is.Zero);
            Assert.That(model.AuditResult.Report.GameplayClaimCount, Is.Zero);
        }

        [Test]
        public void OptionalMerchantAndMaruRemainDeferredLocalAndNonProgression()
        {
            var report = BuildModel().AuditResult.Report;
            var expected = new Dictionary<string, SpecialLandmarkKind>
            {
                { Merchant, SpecialLandmarkKind.WanderingMerchantCave },
                { Maru, SpecialLandmarkKind.MaruTimeShrine },
            };

            foreach (var pair in expected)
            {
                var artifact = Artifact(report, pair.Key);
                var definition = SpecialLandmarkRegionStarterCatalog.GetDefinition(pair.Value);
                var metrics = artifact.Input.Metrics;
                Assert.That(artifact.Binding, Is.EqualTo(SpecialRegionAuditBinding.DeferredToMAP14), pair.Key);
                Assert.That(definition.Binding, Is.EqualTo(SpecialLandmarkBindingKind.DeferredOptionalLocal), pair.Key);
                Assert.That(artifact.Input.FootprintWidth + artifact.Input.FootprintHeight, Is.Zero, pair.Key);
                Assert.That(artifact.Input.FixedCollisionCount + artifact.Input.FixedAccessCount, Is.Zero, pair.Key);
                Assert.That(metrics.SectorCoverageCount + metrics.WorldOriginClaimCount +
                            metrics.ReservationClaimCount + metrics.BridgeClaimCount +
                            metrics.PlacedOwnershipClaimCount, Is.Zero, pair.Key);
                Assert.That(definition.ReservedWidth + definition.ReservedHeight, Is.Zero, pair.Key);
                Assert.That(definition.MandatoryProgressionDependency, Is.False, pair.Key);
                Assert.That(definition.IntroducesNewMovementRule, Is.False, pair.Key);
                Assert.That(definition.RequiredReward, Is.Null, pair.Key);
                Assert.That(artifact.Input.RequiredRewardCount, Is.Zero, pair.Key);
                Assert.That(metrics.DuplicateBenefitRiskCount, Is.Zero, pair.Key);
                Assert.That(metrics.MutationClaimCount, Is.Zero, pair.Key);
                Assert.That(artifact.Routes.All(value => value.Ordered && value.MandatoryNoTool), Is.True, pair.Key);
            }

            Assert.That(Artifact(report, Merchant).Routes.Single(value => value.RouteKind == "Low").NodeIds,
                Is.EqualTo(new[]
                {
                    "SL_NODE_MERCHANT_ENTRY", "SL_NODE_MERCHANT_SAFE",
                    "SL_NODE_MERCHANT_SHOP", "SL_NODE_MERCHANT_RETURN",
                }));
            Assert.That(Artifact(report, Maru).Routes.Single(value => value.RouteKind == "Low").NodeIds,
                Is.EqualTo(new[]
                {
                    "SL_NODE_MARU_ENTRY", "SL_NODE_MARU_SAFE", "SL_NODE_MARU_PREVIEW",
                    "SL_NODE_MARU_CHOICE", "SL_NODE_MARU_RETURN",
                }));
            var maru = SpecialLandmarkRegionStarterCatalog.GetDefinition(SpecialLandmarkKind.MaruTimeShrine);
            Assert.That(maru.Markers.Count(value =>
                value.Kind == SpecialLandmarkMarkerKind.ChoicePreview && value.Required), Is.EqualTo(1));
            Assert.That(maru.Resets.Count(value =>
                value.Policy == SpecialLandmarkResetPolicy.PersistentChoice && value.PreventsReroll), Is.EqualTo(1));
            Assert.That(maru.States.All(value => value.Persistent), Is.True);
        }

        [Test]
        public void PreviewPublishesExactSelectorsOverlaysLegendWarningsAndDefaultSnapshot()
        {
            var model = BuildModel();
            var built = model.BuildDefault();
            Assert.That(built.Success, Is.True, string.Join("\n", built.Errors));
            var snapshot = built.Snapshot;

            Assert.That(model.Artifacts.Select(value => value.Family).Distinct().Count(), Is.EqualTo(3));
            Assert.That(model.Artifacts, Has.Count.EqualTo(10));
            Assert.That(Enum.GetValues(typeof(SpecialRegionPreviewViewMode)).Length, Is.EqualTo(8));
            Assert.That(Enum.GetValues(typeof(SpecialRegionPreviewOverlay))
                .Cast<SpecialRegionPreviewOverlay>().Count(IsSingleOverlay), Is.EqualTo(13));
            Assert.That(model.Legend, Has.Count.EqualTo(18));
            Assert.That(snapshot.Selection.Family, Is.EqualTo(SpecialRegionAuditFamily.Village));
            Assert.That(snapshot.Selection.ArtifactId, Is.EqualTo(VillageOneByOne));
            Assert.That(snapshot.Artifact.Input.KindOrTheme, Is.EqualTo("OneByOne"));
            Assert.That(snapshot.ViewMode, Is.EqualTo(SpecialRegionPreviewViewMode.Overview));
            Assert.That(snapshot.BindingBanner, Is.EqualTo("REFERENCE FIXTURE"));
            Assert.That(snapshot.PhysicsWarning, Is.EqualTo("PHYSICS NOT VERIFIED"));
            Assert.That(snapshot.Tokens, Has.Count.EqualTo(42));
            Assert.That(snapshot.AuditSectionPassCount, Is.EqualTo(8));
            Assert.That(snapshot.AuditSectionFailCount, Is.Zero);

            Assert.That(model.TrySelectArtifact(Maru, out var deferredSelection), Is.True);
            var deferred = model.Build(deferredSelection, SpecialRegionPreviewViewMode.Overview,
                SpecialRegionPreviewOverlay.All);
            Assert.That(deferred.Success, Is.True, string.Join("\n", deferred.Errors));
            Assert.That(new[] { snapshot.BindingBanner, deferred.Snapshot.BindingBanner }.Distinct(),
                Is.EquivalentTo(new[] { "REFERENCE FIXTURE", "DEFERRED TO MAP14" }));
        }

        [Test]
        public void PreviewModelBuildIsReadOnlyForSceneSelectionAndMapDataInventory()
        {
            var activeScene = SceneManager.GetActiveScene();
            var scenePath = activeScene.path;
            var rootCount = activeScene.GetRootGameObjects().Length;
            var dirty = activeScene.isDirty;
            var activeSelection = Selection.activeObject;
            var selectedObjects = Selection.objects.Select(value => value.GetInstanceID()).OrderBy(value => value).ToArray();
            var inventory = FindMapDataAssets();

            var model = new SpecialRegionPreviewModel();
            var reloaded = model.Reload();
            Assert.That(reloaded.Success, Is.True, string.Join("\n", reloaded.Errors));
            Assert.That(model.TrySelectArtifact(Forge, out var forgeSelection), Is.True);
            var built = model.Build(forgeSelection, SpecialRegionPreviewViewMode.Compare,
                SpecialRegionPreviewOverlay.All);
            Assert.That(built.Success, Is.True, string.Join("\n", built.Errors));

            var after = SceneManager.GetActiveScene();
            Assert.That(after.path, Is.EqualTo(scenePath));
            Assert.That(after.GetRootGameObjects().Length, Is.EqualTo(rootCount));
            Assert.That(after.isDirty, Is.EqualTo(dirty));
            Assert.That(Selection.activeObject, Is.SameAs(activeSelection));
            Assert.That(Selection.objects.Select(value => value.GetInstanceID()).OrderBy(value => value),
                Is.EqualTo(selectedObjects));
            Assert.That(FindMapDataAssets(), Is.EqualTo(inventory));
        }

        private static SpecialRegionPreviewModel BuildModel()
        {
            var model = new SpecialRegionPreviewModel();
            Assert.That(model.AuditResult, Is.Not.Null);
            Assert.That(model.AuditResult.Success, Is.True, string.Join("\n", model.AuditResult.Errors));
            return model;
        }

        private static SpecialRegionAuditArtifactResult Artifact(
            SpecialRegionValidationReport report,
            string artifactId)
            => report.Artifacts.Single(value =>
                string.Equals(value.ArtifactId, artifactId, StringComparison.Ordinal));

        private static bool IsLowerSha256(string value)
            => value != null && value.Length == 64 && value.All(character =>
                (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'));

        private static bool IsSingleOverlay(SpecialRegionPreviewOverlay value)
        {
            var number = (int)value;
            return number > 0 && (number & (number - 1)) == 0;
        }

        private static string[] FindMapDataAssets()
        {
            var folders = new[]
            {
                "Assets/_Game/Map/Data/WorldGeneration/Authoring",
                "Assets/_Game/Map/Data/WorldGeneration/Generated",
                "Assets/_Game/MapData/Authoring",
                "Assets/_Game/MapData/Generated",
            }.Where(AssetDatabase.IsValidFolder).ToArray();
            return folders.Length == 0
                ? Array.Empty<string>()
                : AssetDatabase.FindAssets(string.Empty, folders)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }
    }
}
