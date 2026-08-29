using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Activities.Authoring;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.EventOverlays.Authoring;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;
using StarNight.MapAuthoring.WorldGeneration.Activities;
using StarNight.MapAuthoring.WorldGeneration.Import;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.Activities
{
    [TestFixture]
    [Category("MAP12_07")]
    public sealed class Map12ActivityPhaseExitTests
    {
        private const string ApprovedAggregateDigest =
            "46330eb01dd302bf80dab6eacf88dea59f107cbecc9225b2243a395c1d0dbc8b";
        private const string ApprovedActivityDigest =
            "3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a";
        private const string ApprovedEventDigest =
            "2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0";
        private const string PriorLifecycleResultDigest =
            "a2c9dfb7e78c94b57b4362b5026c271de9c606a4ff6cb8998516fd4bc641d569";
        private const string PriorLifecycleResultPath =
            "MapDesign/MCP/REPORTS/MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES_RESULT.md";

        private static readonly Lazy<PhysicalFixture> Physical =
            new Lazy<PhysicalFixture>(PhysicalFixture.Load);

        private static readonly IReadOnlyDictionary<string, string> EventActivities =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "EVT_METEOR_FALL", "ACT_CRATER_RICOCHET_MINE" },
                { "EVT_WANDERING_MERCHANT", "ACT_MILL_ESCORT_CART" },
                { "EVT_RARE_CREATURE", "ACT_MILL_ESCORT_CART" },
                { "EVT_MARU_INTERVENTION", "ACT_MARU_REWIND_ANOMALY" },
                { "EVT_EMPTY", "ACT_DOUGH_TIME_TRIAL" },
            };

        [Test]
        public void PhysicalAuthorityAtomicImportAndCultureDeterminismExitGate()
        {
            var fixture = Physical.Value;
            var descriptors = V2AuthoringSchemaRegistry.DescribeDefaultTables();
            var activityEvent = descriptors.Where(value => value.Owner == V2AuthoringOwner.Activity ||
                                                            value.Owner == V2AuthoringOwner.EventOverlay).ToArray();
            Assert.That(descriptors, Has.Count.EqualTo(29));
            Assert.That(descriptors.Sum(value => value.Columns.Count), Is.EqualTo(189));
            Assert.That(descriptors.Sum(value => value.Columns.Count(column => column.ForeignKey != null)), Is.EqualTo(59));
            Assert.That(activityEvent, Has.Length.EqualTo(10));
            Assert.That(activityEvent.Sum(value => value.Columns.Count), Is.EqualTo(71));
            Assert.That(ActivityEventCsvImporterV2.ProjectRelativePaths, Has.Count.EqualTo(10));

            foreach (var descriptor in activityEvent)
            {
                var path = ActivityEventCsvImporterV2.AuthoringRootProjectRelativePath + descriptor.RelativeAuthoringPath;
                Assert.That(ActivityEventCsvImporterV2.ProjectRelativePaths, Does.Contain(path));
                var bytes = File.ReadAllBytes(FullPath(path));
                Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }), path);
                Assert.That(bytes, Has.None.EqualTo((byte)'\r'), path);
                Assert.That(bytes.Last(), Is.EqualTo((byte)'\n'), path);
                var header = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF').Split('\n')[0];
                Assert.That(header, Is.EqualTo(string.Join(",", descriptor.Columns
                    .OrderBy(value => value.ColumnOrder).Select(value => value.ColumnName))), path);
                Assert.That(File.Exists(FullPath(path + ".meta")), Is.True, path);
            }

            var authoringRoot = FullPath(ActivityEventCsvImporterV2.AuthoringRootProjectRelativePath);
            Assert.That(Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories), Has.Length.EqualTo(75));
            Assert.That(Directory.GetFiles(authoringRoot, "*.csv.meta", SearchOption.AllDirectories), Has.Length.EqualTo(75));
            Assert.That(Directory.GetFiles(FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated"),
                "*.csv", SearchOption.AllDirectories), Is.Empty);
            Assert.That(fixture.Content.ActivityCatalog.Entries, Has.Count.EqualTo(7));
            Assert.That(fixture.Content.EventCatalog.Entries, Has.Count.EqualTo(5));
            Assert.That(fixture.Content.ActivityCatalog.Entries.Count(value =>
                value.PlacementProfile.Strength == ActivityStrengthClass.Strong), Is.EqualTo(4));
            Assert.That(fixture.Content.ActivityCatalog.Entries.Count(value =>
                value.PlacementProfile.Strength == ActivityStrengthClass.Ordinary), Is.EqualTo(3));
            Assert.That(fixture.Content.ActivityCatalog.Entries.Sum(value => value.Contract.Slots.Count), Is.EqualTo(52));
            Assert.That(fixture.Content.EventCatalog.Entries.Count(value => value.Contract.Kind != EventOverlayKind.Empty), Is.EqualTo(4));
            Assert.That(fixture.Content.EventCatalog.Entries.Count(value => value.Contract.Kind == EventOverlayKind.Empty), Is.EqualTo(1));
            Assert.That(fixture.Content.AggregateStableDigest, Is.EqualTo(ApprovedAggregateDigest));
            Assert.That(fixture.Content.ActivityCatalog.StableDigest, Is.EqualTo(ApprovedActivityDigest));
            Assert.That(fixture.Content.EventCatalog.StableDigest, Is.EqualTo(ApprovedEventDigest));

            var reverseBytes = ReadActivityEventBytes().Reverse()
                .ToDictionary(value => value.Key, value => value.Value, StringComparer.Ordinal);
            var reversed = new ActivityEventCsvImporterV2().ParseBytes(reverseBytes, fixture.Terrain);
            AssertImportSuccess(reversed);
            Assert.That(reversed.AggregateStableDigest, Is.EqualTo(ApprovedAggregateDigest));
            var repeated = new ActivityEventCsvImporterV2().Import(fixture.Terrain);
            AssertImportSuccess(repeated);
            Assert.That(repeated.AggregateStableDigest, Is.EqualTo(ApprovedAggregateDigest));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var turkish = new ActivityEventCsvImporterV2().ParseBytes(ReadActivityEventBytes(), fixture.Terrain);
                AssertImportSuccess(turkish);
                Assert.That(turkish.AggregateStableDigest, Is.EqualTo(ApprovedAggregateDigest));
                Assert.That(turkish.ActivityCatalog.StableDigest, Is.EqualTo(ApprovedActivityDigest));
                Assert.That(turkish.EventCatalog.StableDigest, Is.EqualTo(ApprovedEventDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            AssertImportRejected(MutateActivityEvent("Activity/activity_catalog_v2.csv",
                text => text.Replace("TC_CRATER_ROCK_SHELF_RECOVERY", "TC_UNKNOWN_CLUSTER")));
            AssertImportRejected(MutateActivityEvent("Activity/activity_catalog_v2.csv", text =>
            {
                var lines = text.TrimEnd('\n').Split('\n').ToList();
                lines.Insert(2, lines[1]);
                return string.Join("\n", lines) + "\n";
            }));
            AssertImportRejected(MutateActivityEvent("EventOverlay/event_overlay_catalog_v2.csv",
                text => text.Replace("EVT_EMPTY,0,EMPTY,true", "EVT_EMPTY,1,STATE,false")));
            AssertImportRejected(MutateActivityEvent("EventOverlay/event_overlay_markers_v2.csv",
                text => text.Replace("TERRAIN_CLUSTER,TC_CRATER_BROKEN_SLOPE,CORE",
                    "TERRAIN_CLUSTER,TC_UNKNOWN_CLUSTER,CORE")));

            TestContext.WriteLine("AUTHORITY schema=29/189/59 target=10/71 authoring=75/75 generated=0 " +
                                  "entries=7/5 strength=4/3 slots=52 events=4/1 digests=" +
                                  ApprovedAggregateDigest + "/" + ApprovedActivityDigest + "/" + ApprovedEventDigest);
        }

        [Test]
        public void AllSevenShellRemovalAndStaticSoftlockExitGate()
        {
            var fixture = Physical.Value;
            var totals = new SoftlockTotals();
            foreach (var authored in fixture.Content.ActivityCatalog.Entries)
            {
                Assert.That(fixture.Terrain.TryGet(authored.Contract.TerrainClusterId, out var terrain), Is.True,
                    authored.Id.Value);
                var validation = ActivityContractValidator.Validate(authored.Contract, terrain.Contract);
                Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
                Assert.That(validation.CanonicalDigest, Is.EqualTo(authored.PlacementProfile.ActivityDigest));
                var removal = CompilePublicActivityChain(authored, terrain, fixture.Patterns, false);
                Assert.That(removal.IsSuccess, Is.True,
                    authored.Id.Value + "\n" + string.Join("\n", removal.Errors.Select(value => value.ToString())));
                Assert.That(removal.CueProofs, Has.Count.EqualTo(authored.Contract.Cues.Count));
                Assert.That(removal.SafePocketProofs, Has.Count.EqualTo(authored.Contract.RemovalSafety.SafePocketTiles.Count));
                Assert.That(removal.RecoveryProofs, Has.Count.EqualTo(authored.Contract.RemovalSafety.RecoveryTiles.Count));
                Assert.That(removal.CriticalTargetProofs.Select(value => value.Kind), Is.EquivalentTo(new[]
                {
                    ActivityCriticalTargetKind.MandatoryExit,
                    ActivityCriticalTargetKind.Reward,
                }));
                Assert.That(removal.CueProofs.All(value => value.ObservationEdgeOrdinal < value.ActivationBoundaryEdgeOrdinal), Is.True);
                Assert.That(removal.RecoveryProofs.All(value => value.UsesSourceEdgesOnly &&
                    value.SyntheticEdgeCount == 0 && value.TeleportEdgeCount == 0), Is.True);
                Assert.That(removal.CriticalTargetProofs.All(value => value.IsPreserved), Is.True);
                Assert.That(removal.ActiveSnapshot.StaticShellDigest, Is.EqualTo(removal.RemovedSnapshot.StaticShellDigest));
                Assert.That(removal.ActiveSnapshot.WorkingCanvasDigest, Is.EqualTo(removal.RemovedSnapshot.WorkingCanvasDigest));
                Assert.That(removal.ActiveSnapshot.TraversalDigest, Is.EqualTo(removal.RemovedSnapshot.TraversalDigest));
                Assert.That(removal.ActiveSnapshot.RouteWitnessDigest, Is.EqualTo(removal.RemovedSnapshot.RouteWitnessDigest));
                Assert.That(removal.ActiveSnapshot.RouteType, Is.EqualTo(removal.RemovedSnapshot.RouteType));
                Assert.That(removal.ActiveSnapshot.AccessClass, Is.EqualTo(removal.RemovedSnapshot.AccessClass));
                Assert.That(removal.Proof.ResidualOverlayCount, Is.Zero);
                Assert.That(removal.Proof.UnderlyingTileDeltaCount, Is.Zero);
                Assert.That(removal.Proof.RendererInvocationCount, Is.Zero);
                Assert.That(removal.Proof.GeometryWriteCount, Is.Zero);
                Assert.That(removal.Proof.GeometryCarveCount, Is.Zero);
                Assert.That(removal.Proof.RngDrawCount, Is.Zero);

                totals.MissingEntryExit += removal.CriticalTargetProofs.Count(value =>
                    value.Kind == ActivityCriticalTargetKind.MandatoryExit && !value.IsPreserved);
                totals.IdentityMismatch += removal.ActiveSnapshot.StaticShellDigest == removal.RemovedSnapshot.StaticShellDigest &&
                                           removal.ActiveSnapshot.TraversalDigest == removal.RemovedSnapshot.TraversalDigest &&
                                           removal.ActiveSnapshot.AccessClass == removal.RemovedSnapshot.AccessClass ? 0 : 1;
                totals.MissingPocketRecovery += removal.SafePocketProofs.Count == 0 || removal.RecoveryProofs.Count == 0 ? 1 : 0;
                totals.DestroyedCritical += removal.CriticalTargetProofs.Count(value => !value.IsPreserved);
                totals.ResidualMarkers += removal.Proof.ResidualOverlayCount;
                totals.MissingLifecycleWitness += removal.CueProofs.Count == 0 ? 1 : 0;
                totals.SyntheticFallback += removal.RecoveryProofs.Sum(value =>
                    value.SyntheticEdgeCount + value.TeleportEdgeCount);

                TestContext.WriteLine("SHELL activity=" + authored.Id.Value + " shell=" +
                                      removal.Proof.ActivityShellDigest + " removal=" + removal.Proof.CanonicalDigest +
                                      " cue/safe/recovery/critical=" + removal.CueProofs.Count + "/" +
                                      removal.SafePocketProofs.Count + "/" + removal.RecoveryProofs.Count + "/" +
                                      removal.CriticalTargetProofs.Count + " residual=0 rng=0");
            }

            Assert.That(totals.Sum, Is.Zero);
            TestContext.WriteLine("STATIC_SOFTLOCK missing_entry_exit=0 identity_mismatch=0 " +
                                  "missing_pocket_recovery=0 destroyed_critical=0 residual_markers=0 " +
                                  "missing_lifecycle_witness=0 synthetic_fallback=0");
        }

        [Test]
        public void ActivityFrequencyCompatibilityStrongCapAndRateExitGate()
        {
            var profiles = Physical.Value.Content.ActivityCatalog.Entries
                .Select(value => value.PlacementProfile).ToArray();
            var opportunities = ActivityOpportunities(profiles, 100);
            var compiled = CompileActivityIndex(profiles, opportunities);
            Assert.That(compiled.Success, Is.True, ActivityErrors(compiled.Errors));
            Assert.That(compiled.Index.Candidates.Select(value => value.ActivityId.Value).Distinct(),
                Is.EquivalentTo(profiles.Select(value => value.ActivityId.Value)));
            Assert.That(compiled.Index.Candidates.Select(value => value.CandidateKey).Distinct().Count(),
                Is.EqualTo(compiled.Index.CandidateCount));

            var first = profiles[0];
            var valid = ActivityOpportunity(first, 0);
            var invalidClearance = InvalidClearance(first);
            var otherBiome = AllBiomes().First(value => !first.AllowedBiomes.Contains(value));
            var otherPacing = AllPacings().First(value => value != PacingRole.None &&
                                                           !first.AllowedPacingRoles.Contains(value));
            var otherAccess = AllAccessClasses().First(value => value != AccessClass.Unspecified &&
                                                               !first.AllowedAccessClasses.Contains(value));
            var mismatchOpportunities = new[]
            {
                valid,
                ActivityOpportunity(first, 1, biome: otherBiome),
                ActivityOpportunity(first, 2, pacing: otherPacing),
                ActivityOpportunity(first, 3, access: otherAccess),
                ActivityOpportunity(first, 4, activeChunks: first.MaximumActiveChunkCount + 1),
                ActivityOpportunity(first, 5, clusterId: new TerrainClusterId("TC_EXIT_MISMATCH")),
                ActivityOpportunity(first, 6, variantId: new SpineVariantId("SPINE_EXIT_MISMATCH")),
                ActivityOpportunity(first, 7, shellDigest: Digest('c')),
                ActivityOpportunity(first, 8, safetyDigest: Digest('d')),
                ActivityOpportunity(first, 9, clearance: invalidClearance),
            };
            var mismatches = CompileActivityIndex(new[] { first }, mismatchOpportunities);
            Assert.That(mismatches.Success, Is.True, ActivityErrors(mismatches.Errors));
            var rejectionCodes = new HashSet<ActivityCompatibilityRejectionCode>(
                mismatches.Index.Rejections.Select(value => value.Code));
            foreach (var code in new[]
                     {
                         ActivityCompatibilityRejectionCode.BiomeMismatch,
                         ActivityCompatibilityRejectionCode.PacingRoleMismatch,
                         ActivityCompatibilityRejectionCode.AccessClassMismatch,
                         ActivityCompatibilityRejectionCode.ActiveChunkCountMismatch,
                         ActivityCompatibilityRejectionCode.TerrainClusterMismatch,
                         ActivityCompatibilityRejectionCode.SpineVariantMismatch,
                         ActivityCompatibilityRejectionCode.ActivityShellDigestMismatch,
                         ActivityCompatibilityRejectionCode.RemovalSafetyDigestMismatch,
                         ActivityCompatibilityRejectionCode.ClearanceNotRectangular,
                         ActivityCompatibilityRejectionCode.ClearanceReserved,
                         ActivityCompatibilityRejectionCode.ClearanceAbsoluteProtected,
                     })
                Assert.That(rejectionCodes, Does.Contain(code), code.ToString());

            foreach (var rate in new[] { 60, 80, 120 })
            {
                var plan = PlanActivity(compiled.Index, rate, 100, 100, 1);
                Assert.That(plan.Success, Is.True, ActivityErrors(plan.Errors));
                Assert.That(plan.Plan.WorldBudget.SelectedCount, Is.EqualTo(rate / 10));
                Assert.That(plan.Plan.PatchBudgets.Sum(value => value.TargetCount),
                    Is.EqualTo(plan.Plan.WorldBudget.TargetCount));
                Assert.That(plan.Plan.SectorBudgets.Sum(value => value.TargetCount),
                    Is.EqualTo(plan.Plan.WorldBudget.TargetCount));
                Assert.That(plan.Plan.WorldBudget.StrongCount, Is.LessThanOrEqualTo(100));
                Assert.That(plan.Plan.PatchBudgets.All(value => value.StrongCount <= 100), Is.True);
                Assert.That(plan.Plan.SectorBudgets.All(value => value.StrongCount <= 1), Is.True);
                TestContext.WriteLine("ACTIVITY_RATE permille=" + rate + " selected=" +
                                      plan.Plan.WorldBudget.SelectedCount + " strong=" + plan.Plan.WorldBudget.StrongCount +
                                      " patch_target=" + plan.Plan.PatchBudgets.Sum(value => value.TargetCount) +
                                      " sector_target=" + plan.Plan.SectorBudgets.Sum(value => value.TargetCount));
            }

            foreach (var invalidRate in new[] { 59, 121 })
            {
                var invalid = PlanActivity(compiled.Index, invalidRate, 100, 100, 1);
                Assert.That(invalid.Success, Is.False);
                Assert.That(invalid.Plan, Is.Null);
                AssertActivityCode(invalid.Errors, ActivityCompatibilityErrorCode.InvalidFrequencyPolicy);
                Assert.That(invalid.RngStreamCreationCount, Is.Zero);
                Assert.That(invalid.RngDrawCount, Is.Zero);
            }

            var ordinaryProfiles = profiles.Where(value => value.Strength == ActivityStrengthClass.Ordinary).ToArray();
            var ordinaryIndex = CompileActivityIndex(ordinaryProfiles, ActivityOpportunities(ordinaryProfiles, 100));
            Assert.That(ordinaryIndex.Success, Is.True, ActivityErrors(ordinaryIndex.Errors));
            var fallback = PlanActivity(ordinaryIndex.Index, 80, 0, 0, 0);
            Assert.That(fallback.Success, Is.True, ActivityErrors(fallback.Errors));
            Assert.That(fallback.Plan.WorldBudget.SelectedCount, Is.EqualTo(8));
            Assert.That(fallback.Plan.WorldBudget.StrongCount, Is.Zero);
            Assert.That(fallback.Plan.Decisions.All(value => value.Strength == ActivityStrengthClass.Ordinary), Is.True);

            var strongProfiles = profiles.Where(value => value.Strength == ActivityStrengthClass.Strong).ToArray();
            var strongIndex = CompileActivityIndex(strongProfiles, ActivityOpportunities(strongProfiles, 100));
            Assert.That(strongIndex.Success, Is.True, ActivityErrors(strongIndex.Errors));
            var unsatisfied = PlanActivity(strongIndex.Index, 80, 0, 0, 0);
            Assert.That(unsatisfied.Success, Is.False);
            Assert.That(unsatisfied.Plan, Is.Null);
            AssertActivityCode(unsatisfied.Errors, ActivityCompatibilityErrorCode.StrongCapUnsatisfiable);
            TestContext.WriteLine("ACTIVITY_CAP fallback=8 ordinary/0 strong strong_only=StrongCapUnsatisfiable");
        }

        [Test]
        public void EventMarkerOnlyCooldownEmptyAndRateExitGate()
        {
            var fixture = Physical.Value;
            var entries = fixture.Content.EventCatalog.Entries;
            Assert.That(entries.Count(value => value.Contract.Kind == EventOverlayKind.Empty), Is.EqualTo(1));
            foreach (var entry in entries)
            {
                Assert.That(fixture.Terrain.TryGet(entry.Contract.TerrainClusterId, out var terrain), Is.True);
                ActivityStructureContract activity = null;
                if (entry.Contract.ActivityStructureId.HasValue)
                    activity = fixture.Content.ActivityCatalog.ById[entry.Contract.ActivityStructureId.Value].Contract;
                var validation = EventOverlayValidator.Validate(entry.Contract, terrain.Contract, activity,
                    entry.MarkerTargets.Select(value => value.MarkerId), entry.RemovalEvidence);
                Assert.That(validation.IsValid, Is.True, entry.Id.Value + "\n" + string.Join("\n", validation.Errors));
                Assert.That(validation.CanonicalDigest, Is.EqualTo(entry.Profile.ContractDigest));
                Assert.That(entry.Contract.Kind == EventOverlayKind.Empty ? entry.Contract.Assignments.Count : 1,
                    Is.EqualTo(entry.MarkerTargets.Count));
            }

            var profiles = entries.Select(value => value.Profile).ToArray();
            var opportunities = EventOpportunities(fixture, 100);
            var compiled = CompileEventIndex(profiles, opportunities);
            Assert.That(compiled.Success, Is.True, EventErrors(compiled.Errors));
            Assert.That(compiled.Index.Candidates.Count(value => value.IsEmpty), Is.EqualTo(100));
            Assert.That(compiled.Index.Candidates.Count(value => !value.IsEmpty), Is.EqualTo(100));
            Assert.That(compiled.Index.RngStreamCreationCount, Is.Zero);
            Assert.That(compiled.Index.RngDrawCount, Is.Zero);

            var cooldownEvidence = 0;
            foreach (var rate in new[] { 30, 50, 80 })
            {
                var plan = PlanEvent(compiled.Index, rate);
                Assert.That(plan.Success, Is.True, EventErrors(plan.Errors));
                Assert.That(plan.Plan.WorldBudget.AssignedCount, Is.EqualTo(rate / 10));
                Assert.That(plan.Plan.WorldBudget.EmptyCount, Is.EqualTo(100 - (rate / 10)));
                Assert.That(plan.Plan.Decisions, Has.Count.EqualTo(100));
                Assert.That(plan.Plan.Decisions.Where(value =>
                        value.DecisionKind == EventOverlayAssignmentDecisionKind.Assigned)
                    .All(value => value.PreviousProgressionOrdinal < 0 ||
                                  value.ActualProgressionGap >= value.RequiredProgressionGap), Is.True);
                Assert.That(plan.Plan.Decisions.Where(value =>
                        value.DecisionKind == EventOverlayAssignmentDecisionKind.Empty)
                    .All(value => value.EventKind == EventOverlayKind.Empty), Is.True);
                cooldownEvidence += plan.Plan.Decisions.Sum(value => value.CooldownExclusionEvidence.Count);
                Assert.That(plan.Plan.GeometryWriteCount + plan.Plan.CollisionWriteCount + plan.Plan.RouteWriteCount +
                            plan.Plan.AccessWriteCount + plan.Plan.PacingWriteCount + plan.Plan.EnvelopeWriteCount,
                    Is.Zero);
                TestContext.WriteLine("EVENT_RATE permille=" + rate + " assigned/empty=" +
                                      plan.Plan.WorldBudget.AssignedCount + "/" + plan.Plan.WorldBudget.EmptyCount +
                                      " cooldown_exclusions=" + plan.Plan.Decisions.Sum(value => value.CooldownExclusionEvidence.Count));
            }
            foreach (var invalidRate in new[] { 29, 81 })
            {
                var invalid = PlanEvent(compiled.Index, invalidRate);
                Assert.That(invalid.Success, Is.False);
                Assert.That(invalid.Plan, Is.Null);
                AssertEventCode(invalid.Errors, EventOverlayAssignmentErrorCode.InvalidFrequencyPolicy);
                Assert.That(invalid.RngStreamCreationCount, Is.Zero);
                Assert.That(invalid.RngDrawCount, Is.Zero);
            }

            var empty = profiles.Single(value => value.Contract.Kind == EventOverlayKind.Empty);
            var meteor = profiles.Single(value => value.Contract.Id.Value == "EVT_METEOR_FALL");
            var impossibleMeteor = new EventOverlayAssignmentProfile(meteor.Contract, meteor.ContractDigest,
                meteor.Weight, 200, meteor.CompatibleBiomes, meteor.CompatiblePacingRoles,
                meteor.CompatibleAccessClasses, meteor.ReferencedActivityId);
            var fillerContract = new EventOverlayContract(new EventOverlayId("EVT_EXIT_COOLDOWN_FILLER"),
                EventOverlayKind.State, meteor.Contract.TerrainClusterId, null,
                new[]
                {
                    new EventMarkerAssignment(meteor.Contract.Assignments.Single().TargetMarkerId,
                        EventMarkerOperation.SetState, "STATE_EXIT_COOLDOWN_FILLER"),
                });
            var filler = new EventOverlayAssignmentProfile(fillerContract, 1, 0,
                meteor.CompatibleBiomes, meteor.CompatiblePacingRoles, meteor.CompatibleAccessClasses);
            var cooldownProbeIndex = CompileEventIndex(new[] { empty, impossibleMeteor, filler }, opportunities);
            Assert.That(cooldownProbeIndex.Success, Is.True, EventErrors(cooldownProbeIndex.Errors));
            var cooldownProbe = PlanEvent(cooldownProbeIndex.Index, 80, 913UL, 0);
            Assert.That(cooldownProbe.Success, Is.True, EventErrors(cooldownProbe.Errors));
            Assert.That(cooldownProbe.Plan.Decisions.Where(value =>
                    value.DecisionKind == EventOverlayAssignmentDecisionKind.Assigned)
                .All(value => value.PreviousProgressionOrdinal < 0 ||
                              value.ActualProgressionGap >= value.RequiredProgressionGap), Is.True);
            cooldownEvidence += cooldownProbe.Plan.Decisions.Sum(value => value.CooldownExclusionEvidence.Count);
            Assert.That(cooldownEvidence, Is.GreaterThan(0));
            var impossibleIndex = CompileEventIndex(new[] { empty, impossibleMeteor }, opportunities);
            Assert.That(impossibleIndex.Success, Is.True, EventErrors(impossibleIndex.Errors));
            var impossible = PlanEvent(impossibleIndex.Index, 80);
            Assert.That(impossible.Success, Is.False);
            Assert.That(impossible.Plan, Is.Null);
            AssertEventCode(impossible.Errors, EventOverlayAssignmentErrorCode.CooldownMakesTargetUnsatisfiable);
            TestContext.WriteLine("EVENT_COOLDOWN physical_gap=4 evidence=" + cooldownEvidence +
                                  " impossible_gap=200 result=CooldownMakesTargetUnsatisfiable");
        }

        [Test]
        public void CrossPlannerDeterminismRngIsolationAndImmutabilityExitGate()
        {
            var fixture = Physical.Value;
            var activityProfiles = fixture.Content.ActivityCatalog.Entries.Select(value => value.PlacementProfile).ToArray();
            var activityOpportunities = ActivityOpportunities(activityProfiles, 100);
            var activityIndex = CompileActivityIndex(activityProfiles, activityOpportunities);
            var reversedActivityIndex = CompileActivityIndex(activityProfiles.Reverse(), activityOpportunities.Reverse());
            Assert.That(activityIndex.Success && reversedActivityIndex.Success, Is.True,
                ActivityErrors(activityIndex.Errors.Concat(reversedActivityIndex.Errors)));
            Assert.That(reversedActivityIndex.Index.CanonicalDigest, Is.EqualTo(activityIndex.Index.CanonicalDigest));

            var originalCulture = CultureInfo.CurrentCulture;
            ActivityFrequencyPlanResult activityFirst;
            ActivityFrequencyPlanResult activityRepeat;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                activityFirst = PlanActivity(activityIndex.Index, 80, 100, 100, 1, 0x12070001UL, 7);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ko-KR");
                activityRepeat = PlanActivity(reversedActivityIndex.Index, 80, 100, 100, 1, 0x12070001UL, 7);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
            Assert.That(activityFirst.Success && activityRepeat.Success, Is.True);
            Assert.That(activityRepeat.Plan.CanonicalDigest, Is.EqualTo(activityFirst.Plan.CanonicalDigest));
            CollectionAssert.AreEqual(activityFirst.Plan.Decisions.Select(ActivityDecisionEvidence),
                activityRepeat.Plan.Decisions.Select(ActivityDecisionEvidence));
            Assert.That(PlanActivity(activityIndex.Index, 80, 100, 100, 1, 0x12070002UL, 7)
                .Plan.CanonicalDigest, Is.Not.EqualTo(activityFirst.Plan.CanonicalDigest));
            Assert.That(PlanActivity(activityIndex.Index, 80, 100, 100, 1, 0x12070001UL, 8)
                .Plan.CanonicalDigest, Is.Not.EqualTo(activityFirst.Plan.CanonicalDigest));

            var eventProfiles = fixture.Content.EventCatalog.Entries.Select(value => value.Profile).ToArray();
            var eventOpportunities = EventOpportunities(fixture, 100);
            var eventIndex = CompileEventIndex(eventProfiles, eventOpportunities);
            var reversedEventIndex = CompileEventIndex(eventProfiles.Reverse(), eventOpportunities.Reverse());
            Assert.That(eventIndex.Success && reversedEventIndex.Success, Is.True,
                EventErrors(eventIndex.Errors.Concat(reversedEventIndex.Errors)));
            Assert.That(reversedEventIndex.Index.CanonicalDigest, Is.EqualTo(eventIndex.Index.CanonicalDigest));
            EventOverlayAssignmentPlanResult eventFirst;
            EventOverlayAssignmentPlanResult eventRepeat;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                eventFirst = PlanEvent(eventIndex.Index, 80, 0x12070003UL, 7);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ko-KR");
                eventRepeat = PlanEvent(reversedEventIndex.Index, 80, 0x12070003UL, 7);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
            Assert.That(eventFirst.Success && eventRepeat.Success, Is.True);
            Assert.That(eventRepeat.Plan.CanonicalDigest, Is.EqualTo(eventFirst.Plan.CanonicalDigest));
            Assert.That(PlanEvent(eventIndex.Index, 80, 0x12070004UL, 7).Plan.CanonicalDigest,
                Is.Not.EqualTo(eventFirst.Plan.CanonicalDigest));
            Assert.That(PlanEvent(eventIndex.Index, 80, 0x12070003UL, 8).Plan.CanonicalDigest,
                Is.Not.EqualTo(eventFirst.Plan.CanonicalDigest));

            var activityFactory = RngFactory();
            var populationBefore = activityFactory.Create(WorldGenerationRngStreams.PopulationStreamId,
                77UL, RngStreamScope.Spawn("MAP12_EXIT_OTHER", 2)).NextUInt64();
            var isolatedActivity = ActivityFrequencyPlanner.Plan(new ActivityFrequencyPlanRequest(activityIndex.Index,
                new ActivityFrequencyPolicy(80, 100, 100, 1), 77UL, 2, activityFactory));
            var populationAfter = activityFactory.Create(WorldGenerationRngStreams.PopulationStreamId,
                77UL, RngStreamScope.Spawn("MAP12_EXIT_OTHER", 2)).NextUInt64();
            Assert.That(isolatedActivity.Success, Is.True, ActivityErrors(isolatedActivity.Errors));
            Assert.That(isolatedActivity.Plan.RngStreamId, Is.EqualTo(WorldGenerationRngStreams.SectorRecipeStreamId));
            Assert.That(populationAfter, Is.EqualTo(populationBefore));

            var eventFactory = RngFactory();
            var sectorBefore = eventFactory.Create(WorldGenerationRngStreams.SectorRecipeStreamId,
                78UL, RngStreamScope.Sector(new SectorCoord(4, 4), 2)).NextUInt64();
            var isolatedEvent = EventOverlayAssignmentPlanner.Plan(new EventOverlayAssignmentPlanRequest(eventIndex.Index,
                new EventOverlayAssignmentPolicy(80), 78UL, 2, eventFactory));
            var sectorAfter = eventFactory.Create(WorldGenerationRngStreams.SectorRecipeStreamId,
                78UL, RngStreamScope.Sector(new SectorCoord(4, 4), 2)).NextUInt64();
            Assert.That(isolatedEvent.Success, Is.True, EventErrors(isolatedEvent.Errors));
            Assert.That(isolatedEvent.Plan.RngStreamId, Is.EqualTo(WorldGenerationRngStreams.PopulationStreamId));
            Assert.That(sectorAfter, Is.EqualTo(sectorBefore));

            var invalidActivity = ActivityFrequencyPlanner.Plan(new ActivityFrequencyPlanRequest(null,
                new ActivityFrequencyPolicy(80, 1, 1, 1), 1UL, 0, RngFactory()));
            var invalidEvent = EventOverlayAssignmentPlanner.Plan(new EventOverlayAssignmentPlanRequest(eventIndex.Index,
                new EventOverlayAssignmentPolicy(81), 1UL, 0, RngFactory()));
            Assert.That(invalidActivity.Plan, Is.Null);
            Assert.That(invalidActivity.RngStreamCreationCount + (long)invalidActivity.RngDrawCount, Is.Zero);
            Assert.That(invalidEvent.Plan, Is.Null);
            Assert.That(invalidEvent.RngStreamCreationCount + (long)invalidEvent.RngDrawCount, Is.Zero);
            Assert.Throws<NotSupportedException>(() => ((IList)activityFirst.Plan.Decisions).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList)eventFirst.Plan.Decisions).Clear());
            Assert.That(activityIndex.Index.Candidates.Select(value => value.CandidateKey).Distinct().Count(),
                Is.EqualTo(activityIndex.Index.CandidateCount));
            Assert.That(eventIndex.Index.Candidates.Select(value => value.CandidateKey).Distinct().Count(),
                Is.EqualTo(eventIndex.Index.CandidateCount));
            TestContext.WriteLine("DETERMINISM activity=" + activityFirst.Plan.CanonicalDigest +
                                  " event=" + eventFirst.Plan.CanonicalDigest +
                                  " streams=RNG_SECTOR_RECIPE/RNG_POPULATION isolation=PASS invalid_rng=0/0");
        }

        [Test]
        public void PreviewReadOnlyAndPriorLifecycleEvidenceExitGate()
        {
            var fixture = Physical.Value;
            Assert.That(fixture.Model.ActivityIds, Has.Count.EqualTo(7));
            Assert.That(fixture.Model.EventIds, Has.Count.EqualTo(5));
            foreach (var activityId in fixture.Model.ActivityIds)
            {
                var result = fixture.BuildPreview(activityId);
                AssertPreviewSuccess(result, activityId);
                Assert.That(result.StaticSnapshot.UnderlyingDigest, Is.EqualTo(result.ActiveSnapshot.UnderlyingDigest));
                Assert.That(result.StaticSnapshot.UnderlyingDigest, Is.EqualTo(result.RemovedSnapshot.UnderlyingDigest));
                Assert.That(result.StaticSnapshot.RouteDigest, Is.EqualTo(result.RemovedSnapshot.RouteDigest));
                Assert.That(result.StaticSnapshot.AccessDigest, Is.EqualTo(result.RemovedSnapshot.AccessDigest));
                Assert.That(result.StaticSnapshot.ProtectionDigest, Is.EqualTo(result.RemovedSnapshot.ProtectionDigest));
                Assert.That(result.RemovedSnapshot.MarkerCount, Is.Zero);
                Assert.That(result.Comparison.MarkerOnly, Is.True);
            }
            foreach (var pair in EventActivities)
            {
                var result = fixture.BuildPreview(pair.Value, pair.Key);
                AssertPreviewSuccess(result, pair.Key);
                Assert.That(result.EventSnapshot.MarkerCount, Is.EqualTo(pair.Key == "EVT_EMPTY" ? 0 : 1));
                Assert.That(result.ActiveSnapshot.EventMarkerCount, Is.EqualTo(result.EventSnapshot.MarkerCount));
                Assert.That(result.RemovedSnapshot.EventMarkerCount, Is.Zero);
                Assert.That(result.Comparison.MarkerOnly, Is.True);
            }

            var priorBytes = File.ReadAllBytes(FullPath(PriorLifecycleResultPath));
            Assert.That(Sha256(priorBytes), Is.EqualTo(PriorLifecycleResultDigest));
            var priorText = Encoding.UTF8.GetString(priorBytes);
            foreach (var evidence in new[]
                     {
                         "ACT_CRATER_RICOCHET_MINE + EVT_METEOR_FALL",
                         "ACT_MILL_ESCORT_CART + EVT_WANDERING_MERCHANT",
                         "ACT_MILL_ESCORT_CART + EVT_RARE_CREATURE",
                         "ACT_MARU_REWIND_ANOMALY + EVT_MARU_INTERVENTION",
                         "ACT_DOUGH_TIME_TRIAL + EVT_EMPTY",
                         "discovered / executed / passed: 2 / 2 / 2",
                     })
                Assert.That(priorText, Does.Contain(evidence), evidence);

            var before = TreeDigest(ActivityEventCsvImporterV2.AuthoringRootProjectRelativePath,
                "Assets/_Game/Map/Data/WorldGeneration/Generated");
            Assert.That(ActivityEventPreviewWindow.MenuPath, Is.EqualTo("Tools/MapDesign/Activity & Event Preview"));
            Assert.That(ActivityEventPreviewWindow.WindowTitle, Is.EqualTo("Activity & Event Preview"));
            var window = ActivityEventPreviewWindow.Open();
            try
            {
                Assert.That(window.Reload(), Is.True, window.LastError);
                Assert.That(window.ActivityIds, Has.Count.EqualTo(7));
                Assert.That(window.EventIds.Skip(1), Has.Count.EqualTo(5));
                Assert.That(window.TrySelectViewMode(ActivityEventPreviewViewMode.Compare), Is.True);
                Assert.That(window.StatePanelCount, Is.EqualTo(3));
            }
            finally
            {
                window.Close();
            }
            var after = TreeDigest(ActivityEventCsvImporterV2.AuthoringRootProjectRelativePath,
                "Assets/_Game/Map/Data/WorldGeneration/Generated");
            Assert.That(after, Is.EqualTo(before));
            TestContext.WriteLine("PREVIEW selectors=7/5 snapshots=7 event_pairs=5 marker_counts=1/1/1/1/0 " +
                                  "read_only=PASS prior_lifecycle_sha=" + PriorLifecycleResultDigest +
                                  " PlayMode_selection=0");
        }

        [Test]
        public void NegativeAtomicFixturesExitGate()
        {
            var fixture = Physical.Value;
            var profile = fixture.Content.ActivityCatalog.Entries[0].PlacementProfile;
            var duplicateOpportunity = ActivityOpportunity(profile, 0);
            var duplicateActivity = CompileActivityIndex(new[] { profile },
                new[] { duplicateOpportunity, duplicateOpportunity });
            Assert.That(duplicateActivity.Success, Is.False);
            Assert.That(duplicateActivity.Index, Is.Null);
            AssertActivityCode(duplicateActivity.Errors, ActivityCompatibilityErrorCode.DuplicateCandidate);
            Assert.That(duplicateActivity.RngStreamCreationCount + duplicateActivity.RngDrawCount, Is.Zero);

            var eventProfiles = fixture.Content.EventCatalog.Entries.Select(value => value.Profile).ToArray();
            var eventOpportunities = EventOpportunities(fixture, 100);
            var missingEmpty = CompileEventIndex(eventProfiles.Where(value => value.Contract.Kind != EventOverlayKind.Empty),
                eventOpportunities);
            Assert.That(missingEmpty.Success, Is.False);
            Assert.That(missingEmpty.Index, Is.Null);
            AssertEventCode(missingEmpty.Errors, EventOverlayAssignmentErrorCode.MissingEmptyVariant);
            var empty = eventProfiles.Single(value => value.Contract.Kind == EventOverlayKind.Empty);
            var duplicateEmptyContract = new EventOverlayContract(new EventOverlayId("EVT_EMPTY_EXIT_DUPLICATE"),
                EventOverlayKind.Empty, empty.Contract.TerrainClusterId, empty.Contract.ActivityStructureId,
                Array.Empty<EventMarkerAssignment>());
            var duplicateEmptyProfile = new EventOverlayAssignmentProfile(duplicateEmptyContract, 0, 0,
                empty.CompatibleBiomes, empty.CompatiblePacingRoles, empty.CompatibleAccessClasses,
                empty.ReferencedActivityId);
            var duplicateEmpty = CompileEventIndex(eventProfiles.Concat(new[] { duplicateEmptyProfile }),
                eventOpportunities);
            Assert.That(duplicateEmpty.Success, Is.False);
            Assert.That(duplicateEmpty.Index, Is.Null);
            AssertEventCode(duplicateEmpty.Errors, EventOverlayAssignmentErrorCode.DuplicateEmptyVariant);

            var clearanceIndex = CompileActivityIndex(new[] { profile }, new[]
            {
                ActivityOpportunity(profile, 0),
                ActivityOpportunity(profile, 1, clearance: InvalidClearance(profile)),
            });
            Assert.That(clearanceIndex.Success, Is.True, ActivityErrors(clearanceIndex.Errors));
            Assert.That(clearanceIndex.Index.Rejections.Select(value => value.Code),
                Does.Contain(ActivityCompatibilityRejectionCode.ClearanceAbsoluteProtected));

            Assert.That(fixture.Terrain.TryGet(fixture.Content.ActivityCatalog.Entries[0].Contract.TerrainClusterId,
                out var terrain), Is.True);
            var removalMismatch = CompilePublicActivityChain(fixture.Content.ActivityCatalog.Entries[0], terrain,
                fixture.Patterns, true);
            Assert.That(removalMismatch.IsSuccess, Is.False);
            Assert.That(removalMismatch.Proof, Is.Null);
            Assert.That(removalMismatch.Errors.Select(value => value.Code),
                Does.Contain(ActivityRemovalSafetyCompileErrorCode.InvalidActiveSnapshot));

            var meteorEntry = fixture.Content.EventCatalog.ById[new EventOverlayId("EVT_METEOR_FALL")];
            var markerId = meteorEntry.Contract.Assignments.Single().TargetMarkerId;
            var invalidOperationContract = new EventOverlayContract(new EventOverlayId("EVT_EXIT_BAD_OPERATION"),
                EventOverlayKind.Npc, meteorEntry.Contract.TerrainClusterId, null,
                new[] { new EventMarkerAssignment(markerId, EventMarkerOperation.SetState, "PAYLOAD_EXIT_BAD") });
            var invalidOperationProfile = new EventOverlayAssignmentProfile(invalidOperationContract, 1, 0,
                meteorEntry.Profile.CompatibleBiomes, meteorEntry.Profile.CompatiblePacingRoles,
                meteorEntry.Profile.CompatibleAccessClasses);
            var invalidOperation = CompileEventIndex(new[] { empty, invalidOperationProfile },
                new[] { eventOpportunities[0] });
            Assert.That(invalidOperation.Success, Is.False);
            Assert.That(invalidOperation.Index, Is.Null);
            AssertEventCode(invalidOperation.Errors, EventOverlayAssignmentErrorCode.InvalidMarkerOperation);

            AssertImportRejected(MutateActivityEvent("EventOverlay/event_overlay_markers_v2.csv",
                text => text.Replace("TERRAIN_CLUSTER,TC_CRATER_BROKEN_SLOPE,CORE",
                    "TERRAIN_CLUSTER,TC_EXIT_BAD_OWNER,CORE")));

            var strongProfiles = fixture.Content.ActivityCatalog.Entries.Select(value => value.PlacementProfile)
                .Where(value => value.Strength == ActivityStrengthClass.Strong).ToArray();
            var strongIndex = CompileActivityIndex(strongProfiles, ActivityOpportunities(strongProfiles, 100));
            var strongFailure = PlanActivity(strongIndex.Index, 80, 0, 0, 0);
            Assert.That(strongFailure.Plan, Is.Null);
            AssertActivityCode(strongFailure.Errors, ActivityCompatibilityErrorCode.StrongCapUnsatisfiable);

            var meteor = eventProfiles.Single(value => value.Contract.Id.Value == "EVT_METEOR_FALL");
            var impossibleMeteor = new EventOverlayAssignmentProfile(meteor.Contract, meteor.ContractDigest,
                meteor.Weight, 200, meteor.CompatibleBiomes, meteor.CompatiblePacingRoles,
                meteor.CompatibleAccessClasses, meteor.ReferencedActivityId);
            var cooldownIndex = CompileEventIndex(new[] { empty, impossibleMeteor }, eventOpportunities);
            var cooldownFailure = PlanEvent(cooldownIndex.Index, 80);
            Assert.That(cooldownFailure.Plan, Is.Null);
            AssertEventCode(cooldownFailure.Errors,
                EventOverlayAssignmentErrorCode.CooldownMakesTargetUnsatisfiable);

            TestContext.WriteLine("NEGATIVE duplicate_activity=atomic missing_empty=atomic duplicate_empty=atomic " +
                                  "clearance/protected=rejected removal_identity=proof0 event_operation/source_owner=rejected " +
                                  "strong_cap/cooldown=plan0");
        }

        private static ActivityCandidateIndexCompileResult CompileActivityIndex(
            IEnumerable<ActivityPlacementProfile> profiles,
            IEnumerable<ActivityPlacementOpportunity> opportunities)
        {
            var opportunityArray = opportunities.ToArray();
            return ActivityCandidateIndexCompiler.Compile(new ActivityCandidateIndexCompileRequest(
                profiles, opportunityArray, ActivityOwnership(opportunityArray),
                ApprovedActivityDigest, ApprovedAggregateDigest, ApprovedEventDigest));
        }

        private static ActivityPlacementOpportunity[] ActivityOpportunities(
            IReadOnlyList<ActivityPlacementProfile> profiles,
            int count)
        {
            return Enumerable.Range(0, count)
                .Select(index => ActivityOpportunity(profiles[index % profiles.Count], index)).ToArray();
        }

        private static ActivityPlacementOpportunity ActivityOpportunity(
            ActivityPlacementProfile profile,
            int ordinal,
            ActivityPlacementClearanceEvidence clearance = null,
            MoonpalaceBiomeId? biome = null,
            PacingRole? pacing = null,
            AccessClass? access = null,
            int? activeChunks = null,
            TerrainClusterId? clusterId = null,
            SpineVariantId? variantId = null,
            string shellDigest = null,
            string safetyDigest = null)
        {
            var selectedBiome = biome ?? profile.AllowedBiomes[0];
            return new ActivityPlacementOpportunity(
                "MAP12_EXIT_ACTIVITY_" + ordinal.ToString("D3", CultureInfo.InvariantCulture),
                WorldGridIndex.ToCoordinate(ordinal), PatchId(selectedBiome), selectedBiome,
                clusterId ?? profile.TerrainClusterId, variantId ?? profile.SpineVariantId,
                pacing ?? profile.AllowedPacingRoles[0], access ?? profile.AllowedAccessClasses[0],
                activeChunks ?? profile.MinimumActiveChunkCount, clearance ?? ValidClearance(profile),
                ApprovedActivityDigest, ApprovedAggregateDigest, ApprovedEventDigest,
                shellDigest ?? profile.ShellDigest, safetyDigest ?? profile.RemovalSafetyDigest);
        }

        private static ActivityPlacementClearanceEvidence ValidClearance(ActivityPlacementProfile profile)
        {
            var coordinates = (from y in Enumerable.Range(0, profile.RequiredOpenClearanceHeight)
                               from x in Enumerable.Range(0, profile.RequiredOpenClearanceWidth)
                               select new LocalTileCoord(x, y)).ToArray();
            return new ActivityPlacementClearanceEvidence(new LocalTileCoord(0, 0),
                profile.RequiredOpenClearanceWidth, profile.RequiredOpenClearanceHeight,
                coordinates, coordinates, Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>());
        }

        private static ActivityPlacementClearanceEvidence InvalidClearance(ActivityPlacementProfile profile)
        {
            var coordinates = (from y in Enumerable.Range(0, profile.RequiredOpenClearanceHeight)
                               from x in Enumerable.Range(0, profile.RequiredOpenClearanceWidth)
                               select new LocalTileCoord(x, y)).ToList();
            var protectedCoordinate = coordinates[0];
            coordinates.Add(protectedCoordinate);
            return new ActivityPlacementClearanceEvidence(new LocalTileCoord(0, 0),
                profile.RequiredOpenClearanceWidth, profile.RequiredOpenClearanceHeight,
                coordinates, coordinates.Take(coordinates.Count - 2), new[] { coordinates[1] },
                new[] { protectedCoordinate });
        }

        private static BiomePatchSnapshot ActivityOwnership(
            IReadOnlyList<ActivityPlacementOpportunity> opportunities)
        {
            var byIndex = opportunities.GroupBy(value => WorldGridIndex.ToIndex(value.Sector))
                .ToDictionary(group => group.Key, group => group.First().PrimaryBiome);
            var assignments = Enumerable.Range(0, WorldGenConstants.SectorCount)
                .Select(index => new
                {
                    Index = index,
                    Biome = byIndex.TryGetValue(index, out var biome) ? biome : MoonpalaceBiomeId.MoonCrater,
                }).ToArray();
            var patches = assignments.GroupBy(value => value.Biome)
                .OrderBy(group => group.Key.Order)
                .Select(group => new BiomePatch(PatchId(group.Key), BiomeAuthorityId(group.Key),
                    "RULE_MAP12_EXIT_" + group.Key.Order.ToString(CultureInfo.InvariantCulture),
                    BiomePatchRole.Satellite,
                    new[]
                    {
                        new BiomePatchSeed(group.First().Index, WorldGridIndex.ToCoordinate(group.First().Index),
                            BiomePatchRole.Satellite, null),
                    }, group.Select(value => value.Index))).ToArray();
            var ownership = assignments.Select(value => new BiomeSectorOwnership(value.Index,
                WorldGridIndex.ToCoordinate(value.Index), BiomeAuthorityId(value.Biome), string.Empty,
                PatchId(value.Biome))).ToArray();
            return new BiomePatchSnapshot(1207, patches, ownership, Array.Empty<BiomePatchSiteBinding>());
        }

        private static BiomePatchId PatchId(MoonpalaceBiomeId biome)
            => new BiomePatchId("PATCH_MAP12_EXIT_" + biome.Order.ToString(CultureInfo.InvariantCulture));

        private static string BiomeAuthorityId(MoonpalaceBiomeId biome)
        {
            if (biome == MoonpalaceBiomeId.MoonCrater) return "BIO_MOON_CRATER";
            if (biome == MoonpalaceBiomeId.CassiaRoot) return "BIO_CASSIA_ROOT";
            if (biome == MoonpalaceBiomeId.AbandonedMill) return "BIO_ABANDONED_MILL";
            if (biome == MoonpalaceBiomeId.MoonDough) return "BIO_MOON_DOUGH";
            throw new ArgumentOutOfRangeException(nameof(biome));
        }

        private static ActivityFrequencyPlanResult PlanActivity(
            ActivityCandidateIndex index,
            int targetPermille,
            int worldStrong,
            int patchStrong,
            int sectorStrong,
            ulong seed = 0x12070001UL,
            int attempt = 7)
        {
            return ActivityFrequencyPlanner.Plan(new ActivityFrequencyPlanRequest(index,
                new ActivityFrequencyPolicy(targetPermille, worldStrong, patchStrong, sectorStrong),
                seed, attempt, RngFactory()));
        }

        private static EventOverlayCandidateIndexResult CompileEventIndex(
            IEnumerable<EventOverlayAssignmentProfile> profiles,
            IEnumerable<EventOverlayOpportunity> opportunities)
        {
            return EventOverlayCandidateIndexCompiler.Compile(new EventOverlayCandidateIndexRequest(
                profiles, opportunities, ApprovedActivityDigest));
        }

        private static EventOverlayOpportunity[] EventOpportunities(PhysicalFixture fixture, int count)
        {
            var meteor = fixture.Content.EventCatalog.ById[new EventOverlayId("EVT_METEOR_FALL")];
            var target = meteor.MarkerTargets.Single();
            return Enumerable.Range(0, count).Select(index =>
            {
                var marker = new EventMarkerTargetEvidence(target.MarkerId,
                    EventMarkerTargetSourceKind.TerrainCluster, target.SourceOwnerId,
                    target.Coordinate, target.Coordinate, target.SourceSlotKind,
                    "AIR", "AIR", ApprovedAggregateDigest, ApprovedAggregateDigest,
                    ApprovedEventDigest, ApprovedEventDigest, default(SpecialPersistenceKey), string.Empty, string.Empty);
                return new EventOverlayOpportunity(
                    "EVENT_OPP_MAP12_EXIT_" + index.ToString("D3", CultureInfo.InvariantCulture),
                    new SectorCoord(index % 10, index / 10),
                    new BiomePatchId(index < 50 ? "PATCH_MAP12_EXIT_EVENT_A" : "PATCH_MAP12_EXIT_EVENT_B"),
                    index, MoonpalaceBiomeId.MoonCrater, PacingRole.Risk, AccessClass.OptionalNoTool,
                    meteor.Contract.TerrainClusterId, null, ApprovedActivityDigest, new[] { marker });
            }).ToArray();
        }

        private static EventOverlayAssignmentPlanResult PlanEvent(
            EventOverlayCandidateIndex index,
            int targetPermille,
            ulong seed = 0x12070003UL,
            int attempt = 7)
        {
            return EventOverlayAssignmentPlanner.Plan(new EventOverlayAssignmentPlanRequest(index,
                new EventOverlayAssignmentPolicy(targetPermille), seed, attempt, RngFactory()));
        }

        private static DeterministicRngStreamFactory RngFactory()
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                {
                    WorldGenerationRngStreams.SectorRecipeStreamId,
                    RngDefinition(WorldGenerationRngStreams.SectorRecipeStreamId, "E9931A70C2D520F4", "SECTOR")
                },
                {
                    WorldGenerationRngStreams.PopulationStreamId,
                    RngDefinition(WorldGenerationRngStreams.PopulationStreamId, "A63D4078F9E21C55", "SPAWN")
                },
            };
            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return new DeterministicRngStreamFactory(set);
        }

        private static RngStreamDefinition RngDefinition(string id, string salt, string scope)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", Hex(salt));
            SetAutoProperty(definition, "ResetScope", scope);
            SetAutoProperty(definition, "DescriptionKo", "MAP12_07 phase exit fixture");
            SetAutoProperty(definition, "Active", true);
            return definition;
        }

        private static CsvHexValue Hex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2)
                .Select(index => byte.Parse(value.Substring(index * 2, 2), NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture)).ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string property, object value)
        {
            var field = target.GetType().GetField("<" + property + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, property);
            field.SetValue(target, value);
        }

        private static ActivityRemovalSafetyCompileResult CompilePublicActivityChain(
            ActivityAuthoringEntry authored,
            TerrainClusterAuthoringEntry terrain,
            MicroPatternAuthoringCatalog micro,
            bool injectRemovalIdentityMismatch)
        {
            var sourceValidation = TerrainClusterContractValidator.Validate(terrain.Contract);
            Assert.That(sourceValidation.IsValid, Is.True,
                authored.Id.Value + "\n" + string.Join("\n", sourceValidation.Errors));
            var footprint = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(terrain.Contract, ClusterFootprintTransform.R0));
            Assert.That(footprint.IsSuccess, Is.True,
                authored.Id.Value + "\n" + string.Join("\n", footprint.Errors));

            var sourceEntry = terrain.Contract.Ports.Single(value =>
                value.IsPrimary && value.Kind == ClusterPortKind.Entry);
            var sourceExit = terrain.Contract.Ports.Single(value =>
                value.IsPrimary && value.Kind == ClusterPortKind.Exit);
            var role = TerrainClusterRoleSocketCompiler.Compile(new TerrainClusterRoleSocketCompileRequest(
                terrain.Contract, sourceValidation.CanonicalDigest,
                footprint.LocalCanvas, footprint.CanonicalDigest,
                new[]
                {
                    new ClusterSectorSocketEvidence("SR_MAP12_EXIT_ENTRY", "SOCKET_MAP12_EXIT_ENTRY",
                        sourceEntry.OutwardSide, 2, true, ClusterPortKind.Entry),
                    new ClusterSectorSocketEvidence("SR_MAP12_EXIT_EXIT", "SOCKET_MAP12_EXIT_EXIT",
                        sourceExit.OutwardSide, 3, true, ClusterPortKind.Exit),
                }));
            Assert.That(role.IsSuccess, Is.True,
                authored.Id.Value + "\n" + string.Join("\n", role.Errors));

            var traversal = TerrainClusterTraversalCompiler.Compile(new TerrainClusterTraversalCompileRequest(
                terrain.Contract, sourceValidation.CanonicalDigest,
                footprint.LocalCanvas, footprint.CanonicalDigest,
                role.Contract, role.CanonicalDigest));
            Assert.That(traversal.IsSuccess, Is.True,
                authored.Id.Value + "\n" + string.Join("\n", traversal.Errors));
            var witness = TerrainClusterRouteWitnessCompiler.Compile(new TerrainClusterRouteWitnessCompileRequest(
                footprint.LocalCanvas, footprint.CanonicalDigest,
                role.Contract, role.CanonicalDigest,
                traversal.Compilation, traversal.CanonicalDigest, terrain.RouteIntent));
            Assert.That(witness.IsSuccess, Is.True,
                authored.Id.Value + "\n" + string.Join("\n", witness.Errors));
            var pattern = TerrainClusterPatternRenderer.Render(new TerrainClusterPatternRenderRequest(
                footprint.LocalCanvas, footprint.CanonicalDigest,
                traversal.Compilation, traversal.CanonicalDigest,
                witness.Report, witness.CanonicalDigest,
                micro, micro.StableDigest,
                Array.Empty<TerrainClusterPatternZoneCell>(),
                Array.Empty<TerrainClusterPatternPlacementIntent>()));
            Assert.That(pattern.Success, Is.True,
                authored.Id.Value + "\n" + string.Join("\n", pattern.Errors));
            Assert.That(pattern.Report.IsPatternFree, Is.True, authored.Id.Value);

            var slots = authored.Contract.Slots;
            var shell = ActivityShellCompiler.Compile(new ActivityShellCompileRequest(
                terrain.Contract, sourceValidation.CanonicalDigest,
                authored.Contract, authored.PlacementProfile.ActivityDigest,
                footprint.LocalCanvas, footprint.CanonicalDigest,
                role.Contract, role.CanonicalDigest,
                traversal.Compilation, traversal.CanonicalDigest,
                witness.Report, witness.CanonicalDigest,
                pattern.Report, pattern.CanonicalDigest, pattern.Report.FinalWorkingCanvas.CanonicalDigest,
                ActivityZones(slots),
                slots.Select(value => new ActivitySlotProjectionIntent(value.Id, SlotSemantic(value.Kind)))));
            Assert.That(shell.IsSuccess, Is.True,
                authored.Id.Value + "\n" + string.Join("\n", shell.Errors.Select(value => value.ToString())));

            var cueEvidence = authored.Contract.Cues.Select(cue => CueEvidence(
                authored, cue, footprint.LocalCanvas, traversal.Compilation,
                witness.Report, pattern.Report.FinalWorkingCanvas)).ToArray();
            Assert.That(role.Contract.TryGetPrimaryPort(ClusterPortKind.Exit, out var projectedExit), Is.True,
                authored.Id.Value);
            var reward = shell.Canvas.Slots.Single(value =>
                value.Semantic == ActivitySlotSemanticKind.RewardAnchor);
            var rewardBinding = shell.Canvas.ProgressionBindings.Single(value =>
                value.Phase == ProgressionPhaseKind.Reward);
            var critical = new[]
            {
                new ActivityCriticalTargetEvidence(
                    ActivityCriticalTargetKind.MandatoryExit,
                    projectedExit.PortId, projectedExit.SourceCoordinate,
                    projectedExit.RoleAnchorId, witness.Report.BaselineRoute.ExitNodeId),
                new ActivityCriticalTargetEvidence(
                    ActivityCriticalTargetKind.Reward,
                    reward.SlotId.Value, reward.SourceCoordinate,
                    rewardBinding.ProgressionNodeId, string.Empty),
            };
            var identities = OverlayIdentities(shell.Canvas).ToList();
            if (injectRemovalIdentityMismatch) identities.Add("EXTRA|MAP12_EXIT_IDENTITY_MISMATCH");
            return ActivityRemovalSafetyCompiler.Compile(new ActivityRemovalSafetyCompileRequest(
                terrain.Contract, authored.Contract, shell.Canvas,
                footprint.LocalCanvas, role.Contract, traversal.Compilation,
                witness.Report, pattern.Report, shell.CanonicalDigest,
                cueEvidence, new ActivityOverlayRemovalIntent(identities), critical));
        }

        private static ActivityCueObservationEvidence CueEvidence(
            ActivityAuthoringEntry authored,
            ActivityCue cue,
            TerrainClusterLocalCanvas localCanvas,
            TerrainClusterTraversalCompilation traversal,
            TerrainClusterRouteWitnessReport witness,
            TerrainClusterPatternWorkingCanvas working)
        {
            var slot = authored.Contract.Slots.Single(value => value.Id == cue.SlotId);
            Assert.That(localCanvas.TryGetCompiledTile(slot.Tile, out var cueCompiled), Is.True, authored.Id.Value);
            Assert.That(traversal.TryGetVariant(authored.Contract.CompatibleSpineVariantId, out var baseline), Is.True,
                authored.Id.Value);
            var ordered = witness.BaselineRoute.OrderedEdges;
            for (var index = 0; index < ordered.Count - 1; index++)
            {
                Assert.That(baseline.TryGetEdge(ordered[index].EdgeId, out var edge), Is.True, authored.Id.Value);
                foreach (var tile in edge.Envelope.Centerline.Concat(edge.Envelope.Clearance)
                             .Concat(edge.Envelope.Landing)
                             .OrderBy(value => value.SourceCoordinate.Y)
                             .ThenBy(value => value.SourceCoordinate.X))
                {
                    if (!working.TryGetCell(tile.CompiledCoordinate, out var cell) || cell.Solid ||
                        !GridSupercover(tile.CompiledCoordinate, cueCompiled).All(coordinate =>
                        {
                            return working.TryGetCell(coordinate, out var lineCell) && !lineCell.Solid;
                        }))
                        continue;
                    var distance = Math.Abs(tile.CompiledCoordinate.X - cueCompiled.X) +
                                   Math.Abs(tile.CompiledCoordinate.Y - cueCompiled.Y);
                    return new ActivityCueObservationEvidence(
                        "CUE_PROOF_" + authored.Id.Value,
                        cue.Kind, cue.SlotId, ordered[index].EdgeId, ordered[index + 1].EdgeId,
                        tile.SourceCoordinate, Math.Max(1, distance));
                }
            }
            Assert.Fail("No clear pre-activation cue observation witness for " + authored.Id.Value);
            return null;
        }

        private static ActivityShellZoneDefinition[] ActivityZones(IEnumerable<ActivitySlot> source)
        {
            var slots = source.ToArray();
            return new[]
            {
                new ActivityShellZoneDefinition(ActivityShellZoneKind.Cue,
                    slots.Where(value => value.Kind == ActivitySlotKind.Cue).Select(value => value.Tile)),
                new ActivityShellZoneDefinition(ActivityShellZoneKind.Core,
                    slots.Where(value => value.Kind == ActivitySlotKind.Cue ||
                                         value.Kind == ActivitySlotKind.Trigger ||
                                         value.Kind == ActivitySlotKind.Device ||
                                         value.Kind == ActivitySlotKind.Hazard ||
                                         value.Kind == ActivitySlotKind.Projectile ||
                                         value.Kind == ActivitySlotKind.Npc).Select(value => value.Tile)),
                new ActivityShellZoneDefinition(ActivityShellZoneKind.Reward,
                    slots.Where(value => value.Kind == ActivitySlotKind.Reward).Select(value => value.Tile)),
                new ActivityShellZoneDefinition(ActivityShellZoneKind.Recovery,
                    slots.Where(value => value.Kind == ActivitySlotKind.Recovery ||
                                         value.Kind == ActivitySlotKind.Reset).Select(value => value.Tile)),
            };
        }

        private static ActivitySlotSemanticKind SlotSemantic(ActivitySlotKind kind)
        {
            switch (kind)
            {
                case ActivitySlotKind.Cue: return ActivitySlotSemanticKind.CueMarker;
                case ActivitySlotKind.Trigger: return ActivitySlotSemanticKind.PressurePlateTrigger;
                case ActivitySlotKind.Device: return ActivitySlotSemanticKind.DeviceAnchor;
                case ActivitySlotKind.Hazard: return ActivitySlotSemanticKind.ChaseOrHazardSpawn;
                case ActivitySlotKind.Projectile: return ActivitySlotSemanticKind.ProjectileEmitter;
                case ActivitySlotKind.Reward: return ActivitySlotSemanticKind.RewardAnchor;
                case ActivitySlotKind.Recovery: return ActivitySlotSemanticKind.RecoveryAnchor;
                case ActivitySlotKind.Reset: return ActivitySlotSemanticKind.ResetAnchor;
                case ActivitySlotKind.Npc: return ActivitySlotSemanticKind.NpcAnchor;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static IReadOnlyList<string> OverlayIdentities(ActivityShellCanvas shell)
        {
            return shell.Zones.Select(value => "ZONE|" + ((int)value.Kind).ToString(CultureInfo.InvariantCulture))
                .Concat(shell.Slots.Select(value => "SLOT|" + value.SlotId.Value))
                .Concat(shell.CueBindings.Select(value => "CUE|" +
                    ((int)value.CueKind).ToString(CultureInfo.InvariantCulture) + "|" + value.SlotId.Value))
                .Concat(shell.MechanismBindings.Select(value =>
                    "MECHANISM|" + value.MechanismNodeId + "|" + value.SlotId.Value))
                .Concat(shell.ProgressionBindings.Select(value => "PROGRESSION|" + value.ProgressionNodeId))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static IEnumerable<LocalTileCoord> GridSupercover(LocalTileCoord start, LocalTileCoord end)
        {
            var x = start.X;
            var y = start.Y;
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var nx = Math.Abs(dx);
            var ny = Math.Abs(dy);
            var signX = Math.Sign(dx);
            var signY = Math.Sign(dy);
            var ix = 0;
            var iy = 0;
            yield return new LocalTileCoord(x, y);
            while (ix < nx || iy < ny)
            {
                var xDecision = (1 + (2 * ix)) * ny;
                var yDecision = (1 + (2 * iy)) * nx;
                if (xDecision == yDecision)
                {
                    x += signX;
                    y += signY;
                    ix++;
                    iy++;
                }
                else if (xDecision < yDecision)
                {
                    x += signX;
                    ix++;
                }
                else
                {
                    y += signY;
                    iy++;
                }
                yield return new LocalTileCoord(x, y);
            }
        }

        private static void AssertImportSuccess(ActivityEventCsvImportResult result)
        {
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Published, Is.True);
            Assert.That(result.ActivityCatalog, Is.Not.Null);
            Assert.That(result.EventCatalog, Is.Not.Null);
        }

        private static void AssertImportRejected(IReadOnlyDictionary<string, byte[]> bytes)
        {
            var result = new ActivityEventCsvImporterV2().ParseBytes(bytes, Physical.Value.Terrain);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Published, Is.False);
            Assert.That(result.ActivityCatalog, Is.Null);
            Assert.That(result.EventCatalog, Is.Null);
            Assert.That(result.AggregateStableDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(ActivityEventCsvImportErrorCode.AtomicPublishRejected));
        }

        private static Dictionary<string, byte[]> ReadActivityEventBytes()
        {
            return ActivityEventCsvImporterV2.ProjectRelativePaths.ToDictionary(path => path,
                path => File.ReadAllBytes(FullPath(path)), StringComparer.Ordinal);
        }

        private static IReadOnlyDictionary<string, byte[]> MutateActivityEvent(
            string relativePath,
            Func<string, string> mutation)
        {
            var result = ReadActivityEventBytes().ToDictionary(value => value.Key,
                value => value.Value.ToArray(), StringComparer.Ordinal);
            var path = ActivityEventCsvImporterV2.AuthoringRootProjectRelativePath + relativePath;
            var text = Encoding.UTF8.GetString(result[path]).TrimStart('\uFEFF');
            result[path] = EncodeCsv(mutation(text));
            return result;
        }

        private static byte[] EncodeCsv(string text)
        {
            var payload = new UTF8Encoding(false).GetBytes(text.Replace("\r\n", "\n")
                .Replace('\r', '\n').TrimEnd('\n') + "\n");
            return new byte[] { 0xef, 0xbb, 0xbf }.Concat(payload).ToArray();
        }

        private static void AssertPreviewSuccess(ActivityEventPreviewBuildResult result, string context)
        {
            Assert.That(result.Success, Is.True,
                context + "\n" + string.Join("\n", result.Errors.Select(value => value.ToString())));
            Assert.That(result.StableDigest, Does.Match("^[0-9a-f]{64}$"), context);
            Assert.That(result.StaticSnapshot, Is.Not.Null, context);
            Assert.That(result.ActiveSnapshot, Is.Not.Null, context);
            Assert.That(result.RemovedSnapshot, Is.Not.Null, context);
            Assert.That(result.EventSnapshot, Is.Not.Null, context);
            Assert.That(result.Comparison, Is.Not.Null, context);
        }

        private static MoonpalaceBiomeId[] AllBiomes()
            => new[]
            {
                MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.CassiaRoot,
                MoonpalaceBiomeId.AbandonedMill, MoonpalaceBiomeId.MoonDough,
            };

        private static PacingRole[] AllPacings()
            => (PacingRole[])Enum.GetValues(typeof(PacingRole));

        private static AccessClass[] AllAccessClasses()
            => (AccessClass[])Enum.GetValues(typeof(AccessClass));

        private static string ActivityDecisionEvidence(ActivityPlacementDecision value)
        {
            return value.OpportunityId + "|" + value.ActivityId.Value + "|" + value.CandidateKey + "|" +
                   value.Priority.ToString(CultureInfo.InvariantCulture) + "|" +
                   value.WeightedTicket.ToString(CultureInfo.InvariantCulture);
        }

        private static string ActivityErrors(IEnumerable<ActivityCompatibilityError> errors)
            => string.Join(";", (errors ?? Array.Empty<ActivityCompatibilityError>())
                .Select(value => value.Code + ":" + value.Path + ":" + value.Detail));

        private static string EventErrors(IEnumerable<EventOverlayAssignmentError> errors)
            => string.Join(";", (errors ?? Array.Empty<EventOverlayAssignmentError>())
                .Select(value => value.ToString()));

        private static void AssertActivityCode(
            IEnumerable<ActivityCompatibilityError> errors,
            ActivityCompatibilityErrorCode code)
            => Assert.That(errors.Select(value => value.Code), Does.Contain(code), ActivityErrors(errors));

        private static void AssertEventCode(
            IEnumerable<EventOverlayAssignmentError> errors,
            EventOverlayAssignmentErrorCode code)
            => Assert.That(errors.Select(value => value.Code), Does.Contain(code), EventErrors(errors));

        private static string TreeDigest(params string[] projectRelativeRoots)
        {
            var material = new StringBuilder();
            foreach (var root in projectRelativeRoots.OrderBy(value => value, StringComparer.Ordinal))
            {
                var fullRoot = FullPath(root);
                foreach (var file in Directory.GetFiles(fullRoot, "*", SearchOption.AllDirectories)
                             .OrderBy(value => value, StringComparer.Ordinal))
                {
                    material.Append(file.Substring(fullRoot.Length).Replace('\\', '/')).Append('|')
                        .Append(Sha256(File.ReadAllBytes(file))).Append('\n');
                }
            }
            return Sha256(Encoding.UTF8.GetBytes(material.ToString()));
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(bytes)
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static string Digest(char value) => new string(value, 64);

        private static string FullPath(string projectRelativePath)
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(root,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private sealed class PhysicalFixture
        {
            private PhysicalFixture(
                TerrainClusterAuthoringCatalog terrain,
                string terrainDigest,
                MicroPatternAuthoringCatalog patterns,
                string patternDigest,
                ActivityEventCsvImportResult content,
                ActivityEventPreviewModel model)
            {
                Terrain = terrain;
                TerrainDigest = terrainDigest;
                Patterns = patterns;
                PatternDigest = patternDigest;
                Content = content;
                Model = model;
            }

            public TerrainClusterAuthoringCatalog Terrain { get; }
            public string TerrainDigest { get; }
            public MicroPatternAuthoringCatalog Patterns { get; }
            public string PatternDigest { get; }
            public ActivityEventCsvImportResult Content { get; }
            public ActivityEventPreviewModel Model { get; }

            public static PhysicalFixture Load()
            {
                var terrain = new TerrainClusterCsvImporterV2().Import();
                Assert.That(terrain.Success, Is.True, string.Join("\n", terrain.Errors));
                var patterns = new MicroPatternCsvImporterV2().Import();
                Assert.That(patterns.Success && patterns.Published, Is.True, string.Join("\n", patterns.Errors));
                var content = new ActivityEventCsvImporterV2().Import(terrain.Catalog);
                AssertImportSuccess(content);
                Assert.That(content.AggregateStableDigest, Is.EqualTo(ApprovedAggregateDigest));
                Assert.That(content.ActivityCatalog.StableDigest, Is.EqualTo(ApprovedActivityDigest));
                Assert.That(content.EventCatalog.StableDigest, Is.EqualTo(ApprovedEventDigest));
                return new PhysicalFixture(terrain.Catalog, terrain.StableDigest,
                    patterns.Catalog, patterns.StableDigest, content, new ActivityEventPreviewModel());
            }

            public ActivityEventPreviewBuildResult BuildPreview(string activityId, string eventId = "")
                => Model.Build(new ActivityEventPreviewRequest(activityId, eventId, ApprovedAggregateDigest),
                    Terrain, TerrainDigest, Patterns, PatternDigest, Content);
        }

        private sealed class SoftlockTotals
        {
            public int MissingEntryExit;
            public int IdentityMismatch;
            public int MissingPocketRecovery;
            public int DestroyedCritical;
            public int ResidualMarkers;
            public int MissingLifecycleWitness;
            public int SyntheticFallback;
            public int Sum => MissingEntryExit + IdentityMismatch + MissingPocketRecovery + DestroyedCritical +
                              ResidualMarkers + MissingLifecycleWitness + SyntheticFallback;
        }
    }
}
