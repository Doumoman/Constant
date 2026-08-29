using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
using StarNight.MapAuthoring.WorldGeneration.Import;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.Import
{
    [TestFixture]
    [Category("MAP12_05")]
    public sealed class ActivityEventStarterContentTests
    {
        private static readonly string[] ActivityIds =
        {
            "ACT_CRATER_BOULDER_CHAIN", "ACT_CRATER_RICOCHET_MINE", "ACT_DOUGH_TIME_TRIAL",
            "ACT_MARU_REWIND_ANOMALY", "ACT_MILL_ESCORT_CART", "ACT_MILL_GEAR_GRID",
            "ACT_MILL_PESTLE_WORKSHOP",
        };

        private static readonly IReadOnlyDictionary<string, ActivitySpec> Activities =
            new Dictionary<string, ActivitySpec>(StringComparer.Ordinal)
            {
                { "ACT_CRATER_BOULDER_CHAIN", new ActivitySpec("TC_CRATER_ROCK_SHELF_RECOVERY", 1400, ActivityStrengthClass.Strong, 7) },
                { "ACT_CRATER_RICOCHET_MINE", new ActivitySpec("TC_CRATER_BROKEN_SLOPE", 1400, ActivityStrengthClass.Strong, 8) },
                { "ACT_DOUGH_TIME_TRIAL", new ActivitySpec("TC_DOUGH_BOUNCE_CUP", 1800, ActivityStrengthClass.Ordinary, 7) },
                { "ACT_MARU_REWIND_ANOMALY", new ActivitySpec("TC_DOUGH_STICKY_RISE_RECOVERY", 600, ActivityStrengthClass.Strong, 7) },
                { "ACT_MILL_ESCORT_CART", new ActivitySpec("TC_MILL_BEAM_OVERHANG", 1200, ActivityStrengthClass.Ordinary, 8) },
                { "ACT_MILL_GEAR_GRID", new ActivitySpec("TC_MILL_BROKEN_PILLAR", 1200, ActivityStrengthClass.Ordinary, 7) },
                { "ACT_MILL_PESTLE_WORKSHOP", new ActivitySpec("TC_MILL_ORTHOGONAL_SHAFT_RECOVERY", 1000, ActivityStrengthClass.Strong, 8) },
            };

        private static readonly IReadOnlyDictionary<string, EventSpec> Events =
            new Dictionary<string, EventSpec>(StringComparer.Ordinal)
            {
                { "EVT_EMPTY", new EventSpec(EventOverlayKind.Empty, 0, 0, 0) },
                { "EVT_MARU_INTERVENTION", new EventSpec(EventOverlayKind.State, 1000, 10, 1) },
                { "EVT_METEOR_FALL", new EventSpec(EventOverlayKind.State, 3000, 4, 1) },
                { "EVT_RARE_CREATURE", new EventSpec(EventOverlayKind.Npc, 1500, 8, 1) },
                { "EVT_WANDERING_MERCHANT", new EventSpec(EventOverlayKind.Npc, 2500, 6, 1) },
            };

        private static readonly Lazy<Fixture> Content = new Lazy<Fixture>(BuildFixture);

        [Test]
        public void TenPhysicalTablesMatchRepairedRegistryAndExactInventory()
        {
            var descriptors = V2AuthoringSchemaRegistry.DescribeDefaultTables();
            Assert.That(descriptors, Has.Count.EqualTo(29));
            Assert.That(descriptors.Sum(value => value.Columns.Count), Is.EqualTo(189));
            Assert.That(descriptors.Sum(value => value.Columns.Count(column => column.ForeignKey != null)), Is.EqualTo(59));
            Assert.That(descriptors.Count(value => value.Owner == V2AuthoringOwner.Activity), Is.EqualTo(7));
            Assert.That(descriptors.Where(value => value.Owner == V2AuthoringOwner.Activity).Sum(value => value.Columns.Count), Is.EqualTo(51));
            Assert.That(descriptors.Count(value => value.Owner == V2AuthoringOwner.EventOverlay), Is.EqualTo(3));
            Assert.That(descriptors.Where(value => value.Owner == V2AuthoringOwner.EventOverlay).Sum(value => value.Columns.Count), Is.EqualTo(20));
            Assert.That(ActivityEventCsvImporterV2.ProjectRelativePaths, Has.Count.EqualTo(10));

            foreach (var descriptor in descriptors.Where(value => value.Owner == V2AuthoringOwner.Activity ||
                                                                   value.Owner == V2AuthoringOwner.EventOverlay))
            {
                var path = ActivityEventCsvImporterV2.AuthoringRootProjectRelativePath + descriptor.RelativeAuthoringPath;
                Assert.That(ActivityEventCsvImporterV2.ProjectRelativePaths, Does.Contain(path));
                var bytes = File.ReadAllBytes(FullPath(path));
                Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }), path);
                Assert.That(bytes, Has.None.EqualTo((byte)'\r'), path);
                Assert.That(bytes.Last(), Is.EqualTo((byte)'\n'), path);
                Assert.That(bytes[bytes.Length - 2], Is.Not.EqualTo((byte)'\n'), path);
                var header = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF').Split('\n')[0];
                Assert.That(header, Is.EqualTo(string.Join(",", descriptor.Columns.OrderBy(value => value.ColumnOrder)
                    .Select(value => value.ColumnName))), path);
                Assert.That(File.Exists(FullPath(path + ".meta")), Is.True, path);
            }

            var authoring = FullPath(ActivityEventCsvImporterV2.AuthoringRootProjectRelativePath);
            Assert.That(Directory.GetFiles(authoring, "*.csv", SearchOption.AllDirectories), Has.Length.EqualTo(75));
            Assert.That(Directory.GetFiles(authoring, "*.csv.meta", SearchOption.AllDirectories), Has.Length.EqualTo(75));
            Assert.That(Directory.GetFiles(authoring, "*.csv", SearchOption.AllDirectories)
                .Count(path => !path.Replace('\\', '/').Contains("/Activity/") && !path.Replace('\\', '/').Contains("/EventOverlay/")), Is.EqualTo(65));
            Assert.That(Directory.GetFiles(FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated"),
                "*.csv", SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void SevenActivitiesRoundTripExactProfilesContractsGraphsAndSafety()
        {
            var fixture = Content.Value;
            var micro = new MicroPatternCsvImporterV2().Import();
            Assert.That(micro.Success, Is.True, string.Join("\n", micro.Errors));
            Assert.That(fixture.Result.ActivityCatalog.Entries.Select(value => value.Id.Value), Is.EqualTo(ActivityIds));
            Assert.That(fixture.Result.ActivityCatalog.Entries.Count(value => value.PlacementProfile.Strength == ActivityStrengthClass.Strong), Is.EqualTo(4));
            Assert.That(fixture.Result.ActivityCatalog.Entries.Count(value => value.PlacementProfile.Strength == ActivityStrengthClass.Ordinary), Is.EqualTo(3));
            foreach (var entry in fixture.Result.ActivityCatalog.Entries)
            {
                var spec = Activities[entry.Id.Value];
                Assert.That(entry.Contract.TerrainClusterId.Value, Is.EqualTo(spec.ClusterId), entry.Id.Value);
                Assert.That(entry.PlacementProfile.Weight, Is.EqualTo(spec.Weight), entry.Id.Value);
                Assert.That(entry.PlacementProfile.Strength, Is.EqualTo(spec.Strength), entry.Id.Value);
                Assert.That(entry.Contract.Slots, Has.Count.EqualTo(spec.SlotCount), entry.Id.Value);
                Assert.That(entry.Contract.Slots.Select(value => value.Kind), Does.Contain(ActivitySlotKind.Cue));
                Assert.That(entry.Contract.Slots.Select(value => value.Kind), Does.Contain(ActivitySlotKind.Trigger));
                Assert.That(entry.Contract.Slots.Select(value => value.Kind), Does.Contain(ActivitySlotKind.Device));
                Assert.That(entry.Contract.Slots.Select(value => value.Kind), Does.Contain(ActivitySlotKind.Hazard));
                Assert.That(entry.Contract.Slots.Select(value => value.Kind), Does.Contain(ActivitySlotKind.Reward));
                Assert.That(entry.Contract.Slots.Select(value => value.Kind), Does.Contain(ActivitySlotKind.Recovery));
                Assert.That(entry.Contract.Slots.Select(value => value.Kind), Does.Contain(ActivitySlotKind.Reset));
                Assert.That(entry.Contract.Cues, Has.Count.EqualTo(1));
                Assert.That(entry.Contract.Cues.Single().DetectableBeforeActivation, Is.True);
                Assert.That(entry.Contract.RemovalSafety.SafePocketTiles, Is.Not.Empty);
                Assert.That(entry.Contract.RemovalSafety.RecoveryTiles, Is.Not.Empty);
                Assert.That(entry.Contract.RemovalSafety.PreserveStaticTraversal, Is.True);
                Assert.That(entry.Contract.RemovalSafety.PreserveAccessClass, Is.True);
                Assert.That(entry.Contract.RemovalSafety.PermanentSolidMutationAllowed, Is.False);
                Assert.That(entry.Contract.RemovalSafety.MandatoryExitDestructionAllowed, Is.False);
                Assert.That(entry.Contract.CompatibleAccessClasses, Does.Contain(AccessClass.OptionalNoTool));
                Assert.That(entry.PlacementProfile.RequiredOpenClearanceWidth, Is.GreaterThanOrEqualTo(3));
                Assert.That(entry.PlacementProfile.RequiredOpenClearanceHeight, Is.GreaterThanOrEqualTo(3));
                Assert.That(entry.Contract.ProgressionGraph.Nodes.Single(value => value.NodeId == entry.Contract.ProgressionGraph.StartNodeId).Phase,
                    Is.EqualTo(ProgressionPhaseKind.Cue));
                Assert.That(entry.Contract.ProgressionGraph.Nodes.Single(value => value.NodeId == entry.Contract.ProgressionGraph.TerminalNodeId).Phase,
                    Is.EqualTo(ProgressionPhaseKind.Exit));
                Assert.That(entry.Contract.ProgressionGraph.Edges.Count(value => value.Kind == ProgressionEdgeKind.Failure), Is.EqualTo(1));
                Assert.That(entry.Contract.ProgressionGraph.Edges.Count(value => value.Kind == ProgressionEdgeKind.Reset), Is.EqualTo(1));
                Assert.That(fixture.Terrain.TryGet(entry.Contract.TerrainClusterId, out var terrain), Is.True);
                var validation = ActivityContractValidator.Validate(entry.Contract, terrain.Contract);
                Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
                Assert.That(validation.CanonicalDigest, Is.EqualTo(entry.PlacementProfile.ActivityDigest));
                var removal = CompilePublicActivityChain(entry, terrain, micro.Catalog);
                Assert.That(removal.IsSuccess, Is.True,
                    entry.Id.Value + "\n" + string.Join("\n", removal.Errors.Select(value => value.ToString())));
                Assert.That(removal.CueProofs, Has.Count.EqualTo(entry.Contract.Cues.Count), entry.Id.Value);
                Assert.That(removal.SafePocketProofs, Has.Count.EqualTo(entry.Contract.RemovalSafety.SafePocketTiles.Count), entry.Id.Value);
                Assert.That(removal.RecoveryProofs, Has.Count.EqualTo(entry.Contract.RemovalSafety.RecoveryTiles.Count), entry.Id.Value);
                Assert.That(removal.CriticalTargetProofs.Select(value => value.Kind), Is.EquivalentTo(new[]
                {
                    ActivityCriticalTargetKind.MandatoryExit,
                    ActivityCriticalTargetKind.Reward,
                }), entry.Id.Value);
                Assert.That(removal.Proof.ResidualOverlayCount, Is.Zero, entry.Id.Value);
                Assert.That(removal.Proof.UnderlyingTileDeltaCount, Is.Zero, entry.Id.Value);
                Assert.That(removal.Proof.RngDrawCount, Is.Zero, entry.Id.Value);
            }
            Assert.That(fixture.Result.ActivityCatalog.ById is IDictionary<ActivityStructureId, ActivityAuthoringEntry>, Is.True);
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<ActivityStructureId, ActivityAuthoringEntry>)fixture.Result.ActivityCatalog.ById)
                .Add(default, fixture.Result.ActivityCatalog.Entries[0]));
        }

        [Test]
        public void FiveEventsRoundTripExactProfilesMarkerProvenanceAndRemovalEvidence()
        {
            var fixture = Content.Value;
            Assert.That(fixture.Result.EventCatalog.Entries.Select(value => value.Id.Value), Is.EqualTo(Events.Keys));
            Assert.That(fixture.Result.EventCatalog.Entries.Count(value => value.Contract.Kind == EventOverlayKind.Empty), Is.EqualTo(1));
            foreach (var entry in fixture.Result.EventCatalog.Entries)
            {
                var spec = Events[entry.Id.Value];
                Assert.That(entry.Contract.Kind, Is.EqualTo(spec.Kind), entry.Id.Value);
                Assert.That(entry.Profile.Weight, Is.EqualTo(spec.Weight), entry.Id.Value);
                Assert.That(entry.Profile.MinimumProgressionGap, Is.EqualTo(spec.Gap), entry.Id.Value);
                Assert.That(entry.Contract.Assignments, Has.Count.EqualTo(spec.MarkerCount), entry.Id.Value);
                Assert.That(entry.MarkerTargets, Has.Count.EqualTo(spec.MarkerCount), entry.Id.Value);
                Assert.That(entry.Profile.CompatibleBiomes, Is.Not.Empty);
                Assert.That(entry.Profile.CompatiblePacingRoles, Is.Not.Empty);
                Assert.That(entry.Profile.CompatibleAccessClasses, Is.Not.Empty);
                Assert.That(fixture.Terrain.TryGet(entry.Contract.TerrainClusterId, out var terrain), Is.True);
                ActivityStructureContract activity = null;
                if (entry.Contract.ActivityStructureId.HasValue)
                    activity = fixture.Result.ActivityCatalog.ById[entry.Contract.ActivityStructureId.Value].Contract;
                var validation = EventOverlayValidator.Validate(entry.Contract, terrain.Contract, activity,
                    entry.MarkerTargets.Select(value => value.MarkerId), entry.RemovalEvidence);
                Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
                Assert.That(validation.CanonicalDigest, Is.EqualTo(entry.Profile.ContractDigest));
                Assert.That(entry.MarkerTargets.All(value => value.Operation == entry.Contract.Assignments.Single().Operation),
                    Is.True, entry.Id.Value);
            }
            Assert.That(fixture.Result.EventCatalog.ById is IDictionary<EventOverlayId, EventOverlayAuthoringEntry>, Is.True);
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<EventOverlayId, EventOverlayAuthoringEntry>)fixture.Result.EventCatalog.ById)
                .Add(default, fixture.Result.EventCatalog.Entries[0]));
        }

        [Test]
        public void AllProfilesPassMap12CandidateIndexPublicApisWithoutRng()
        {
            var fixture = Content.Value;
            var first = fixture.Result.ActivityCatalog.Entries[0];
            var patchId = new BiomePatchId("PATCH_MAP12_05");
            var indices = Enumerable.Range(0, WorldGenConstants.SectorCount).ToArray();
            var patch = new BiomePatch(patchId, "BIO_MOON_CRATER", "PATCH_RULE_MAP12_05", BiomePatchRole.Satellite,
                new[] { new BiomePatchSeed(0, WorldGridIndex.ToCoordinate(0), BiomePatchRole.Satellite, null) }, indices);
            var ownership = indices.Select(index => new BiomeSectorOwnership(index, WorldGridIndex.ToCoordinate(index),
                "BIO_MOON_CRATER", string.Empty, patchId)).ToArray();
            var snapshot = new BiomePatchSnapshot(12, new[] { patch }, ownership, Array.Empty<BiomePatchSiteBinding>());
            var coordinates = (from y in Enumerable.Range(0, 3) from x in Enumerable.Range(0, 3)
                select new LocalTileCoord(x, y)).ToArray();
            var shaA = new string('a', 64);
            var shaB = new string('b', 64);
            var shaC = new string('c', 64);
            var opportunity = new ActivityPlacementOpportunity("ACTIVITY_OPP_MAP12_05", WorldGridIndex.ToCoordinate(0), patchId,
                MoonpalaceBiomeId.MoonCrater, first.Contract.TerrainClusterId, first.Contract.CompatibleSpineVariantId,
                PacingRole.Activity, AccessClass.OptionalNoTool, 5,
                new ActivityPlacementClearanceEvidence(new LocalTileCoord(0, 0), 3, 3, coordinates, coordinates,
                    Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>()), shaA, shaB, shaC,
                first.PlacementProfile.ShellDigest, first.PlacementProfile.RemovalSafetyDigest);
            var activityIndex = ActivityCandidateIndexCompiler.Compile(new ActivityCandidateIndexCompileRequest(
                fixture.Result.ActivityCatalog.Entries.Select(value => value.PlacementProfile), new[] { opportunity },
                snapshot, shaA, shaB, shaC));
            Assert.That(activityIndex.Success, Is.True, string.Join("\n", activityIndex.Errors.Select(value => value.Code + "|" + value.Path + "|" + value.Detail)));
            Assert.That(activityIndex.Index.CandidateCount, Is.EqualTo(1));
            Assert.That(activityIndex.RngStreamCreationCount, Is.Zero);
            Assert.That(activityIndex.RngDrawCount, Is.Zero);

            var meteor = fixture.Result.EventCatalog.ById[new EventOverlayId("EVT_METEOR_FALL")].MarkerTargets.Single();
            var marker = new EventMarkerTargetEvidence(meteor.MarkerId, EventMarkerTargetSourceKind.TerrainCluster,
                meteor.SourceOwnerId, meteor.Coordinate, meteor.Coordinate, meteor.SourceSlotKind, "AIR", "AIR",
                shaA, shaA, shaB, shaB, default(SpecialPersistenceKey), string.Empty, string.Empty);
            var eventOpportunity = new EventOverlayOpportunity("EVENT_OPP_MAP12_05", WorldGridIndex.ToCoordinate(0), patchId,
                0, MoonpalaceBiomeId.MoonCrater, PacingRole.Risk, AccessClass.OptionalNoTool,
                new TerrainClusterId("TC_CRATER_BROKEN_SLOPE"), null, shaC, new[] { marker });
            var eventIndex = EventOverlayCandidateIndexCompiler.Compile(new EventOverlayCandidateIndexRequest(
                fixture.Result.EventCatalog.Entries.Select(value => value.Profile), new[] { eventOpportunity }, shaC));
            Assert.That(eventIndex.Success, Is.True, string.Join("\n", eventIndex.Errors.Select(value => value.Code + "|" + value.Path + "|" + value.Detail)));
            Assert.That(eventIndex.Index.Candidates.Count(value => value.IsEmpty), Is.EqualTo(1));
            Assert.That(eventIndex.Index.Candidates.Count(value => !value.IsEmpty), Is.EqualTo(1));
            Assert.That(eventIndex.RngStreamCreationCount, Is.Zero);
            Assert.That(eventIndex.RngDrawCount, Is.Zero);
        }

        [Test]
        public void ReversedInputRowsRepeatAndTurkishCultureKeepCanonicalDigests()
        {
            var fixture = Content.Value;
            var bytes = ReadAllBytes().Reverse().ToDictionary(value => value.Key, value => value.Value, StringComparer.Ordinal);
            var reversedInput = new ActivityEventCsvImporterV2().ParseBytes(bytes, fixture.Terrain);
            AssertSuccess(reversedInput);
            Assert.That(reversedInput.AggregateStableDigest, Is.EqualTo(fixture.Result.AggregateStableDigest));
            var reversedActivityRows = fixture.Result.ActivityCatalog.Entries.SelectMany(value => value.SourceRows).Reverse().ToArray();
            var activity = ActivityAuthoringCatalogBuilder.Build(reversedActivityRows, fixture.Terrain);
            Assert.That(activity.Success, Is.True, string.Join("\n", activity.Errors));
            Assert.That(activity.Catalog.StableDigest, Is.EqualTo(fixture.Result.ActivityCatalog.StableDigest));
            var reversedEventRows = fixture.Result.EventCatalog.Entries.SelectMany(value => value.SourceRows).Reverse().ToArray();
            var events = EventOverlayAuthoringCatalogBuilder.Build(reversedEventRows, fixture.Terrain, activity.Catalog);
            Assert.That(events.Success, Is.True, string.Join("\n", events.Errors));
            Assert.That(events.Catalog.StableDigest, Is.EqualTo(fixture.Result.EventCatalog.StableDigest));

            var original = CultureInfo.CurrentCulture;
            var originalUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var turkish = new ActivityEventCsvImporterV2().ParseBytes(ReadAllBytes(), fixture.Terrain);
                AssertSuccess(turkish);
                Assert.That(turkish.AggregateStableDigest, Is.EqualTo(fixture.Result.AggregateStableDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
                CultureInfo.CurrentUICulture = originalUi;
            }
        }

        [Test]
        public void InvalidFkDuplicateIdMissingEmptyAndBadGraphRejectBothCatalogsAtomically()
        {
            var fixture = Content.Value;
            AssertRejected(fixture.Terrain, Mutate("Activity/activity_catalog_v2.csv",
                text => text.Replace("TC_CRATER_ROCK_SHELF_RECOVERY", "TC_UNKNOWN_CLUSTER")));
            AssertRejected(fixture.Terrain, Mutate("Activity/activity_catalog_v2.csv", text =>
            {
                var lines = text.TrimEnd('\n').Split('\n').ToList();
                lines.Insert(2, lines[1]);
                return string.Join("\n", lines) + "\n";
            }));
            AssertRejected(fixture.Terrain, Mutate("EventOverlay/event_overlay_catalog_v2.csv",
                text => text.Replace("EVT_EMPTY,0,EMPTY,true", "EVT_EMPTY,1,STATE,false")));
            AssertRejected(fixture.Terrain, Mutate("Activity/activity_graph_edges_v2.csv",
                text => text.Replace("MECH_CRATER_BOULDER_CHAIN_CUE,1", "MECH_UNKNOWN_NODE,1")));
        }

        private static Fixture BuildFixture()
        {
            var terrain = new TerrainClusterCsvImporterV2().Import();
            Assert.That(terrain.Success, Is.True, string.Join("\n", terrain.Errors));
            var result = new ActivityEventCsvImporterV2().Import(terrain.Catalog);
            AssertSuccess(result);
            Assert.That(result.AggregateStableDigest, Does.Match("^[0-9a-f]{64}$"));
            return new Fixture(terrain.Catalog, result);
        }

        private static void AssertSuccess(ActivityEventCsvImportResult result)
        {
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Published, Is.True);
            Assert.That(result.ActivityCatalog, Is.Not.Null);
            Assert.That(result.EventCatalog, Is.Not.Null);
        }

        private static void AssertRejected(TerrainClusterAuthoringCatalog terrain,
            IReadOnlyDictionary<string, byte[]> bytes)
        {
            var result = new ActivityEventCsvImporterV2().ParseBytes(bytes, terrain);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Published, Is.False);
            Assert.That(result.ActivityCatalog, Is.Null);
            Assert.That(result.EventCatalog, Is.Null);
            Assert.That(result.AggregateStableDigest, Is.Empty);
            Assert.That(result.Errors.Any(value => value.Code == ActivityEventCsvImportErrorCode.AtomicPublishRejected), Is.True);
        }

        private static ActivityRemovalSafetyCompileResult CompilePublicActivityChain(
            ActivityAuthoringEntry authored,
            TerrainClusterAuthoringEntry terrain,
            MicroPatternAuthoringCatalog micro)
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
                    new ClusterSectorSocketEvidence("SR_MAP12_05_ENTRY", "SOCKET_MAP12_05_ENTRY",
                        sourceEntry.OutwardSide, 2, true, ClusterPortKind.Entry),
                    new ClusterSectorSocketEvidence("SR_MAP12_05_EXIT", "SOCKET_MAP12_05_EXIT",
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
            ProjectedClusterPort projectedExit;
            Assert.That(role.Contract.TryGetPrimaryPort(ClusterPortKind.Exit, out projectedExit), Is.True,
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
            var removal = ActivityRemovalSafetyCompiler.Compile(new ActivityRemovalSafetyCompileRequest(
                terrain.Contract, authored.Contract, shell.Canvas,
                footprint.LocalCanvas, role.Contract, traversal.Compilation,
                witness.Report, pattern.Report, shell.CanonicalDigest,
                cueEvidence, new ActivityOverlayRemovalIntent(OverlayIdentities(shell.Canvas)), critical));
            return removal;
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
            LocalTileCoord cueCompiled;
            Assert.That(localCanvas.TryGetCompiledTile(slot.Tile, out cueCompiled), Is.True, authored.Id.Value);
            CompiledClusterSpineVariant baseline;
            Assert.That(traversal.TryGetVariant(authored.Contract.CompatibleSpineVariantId, out baseline), Is.True,
                authored.Id.Value);
            var ordered = witness.BaselineRoute.OrderedEdges;
            for (var index = 0; index < ordered.Count - 1; index++)
            {
                CompiledTraversalEdge edge;
                Assert.That(baseline.TryGetEdge(ordered[index].EdgeId, out edge), Is.True, authored.Id.Value);
                foreach (var tile in edge.Envelope.Centerline.Concat(edge.Envelope.Clearance)
                             .Concat(edge.Envelope.Landing)
                             .OrderBy(value => value.SourceCoordinate.Y)
                             .ThenBy(value => value.SourceCoordinate.X))
                {
                    TerrainClusterPatternWorkingCell cell;
                    if (!working.TryGetCell(tile.CompiledCoordinate, out cell) || cell.Solid ||
                        !GridSupercover(tile.CompiledCoordinate, cueCompiled).All(coordinate =>
                        {
                            TerrainClusterPatternWorkingCell lineCell;
                            return working.TryGetCell(coordinate, out lineCell) && !lineCell.Solid;
                        }))
                    {
                        continue;
                    }
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

        private static IReadOnlyDictionary<string, byte[]> Mutate(string relativePath, Func<string, string> mutation)
        {
            var result = ReadAllBytes().ToDictionary(value => value.Key, value => value.Value.ToArray(), StringComparer.Ordinal);
            var path = ActivityEventCsvImporterV2.AuthoringRootProjectRelativePath + relativePath;
            var text = Encoding.UTF8.GetString(result[path]).TrimStart('\uFEFF');
            result[path] = Encode(mutation(text));
            return result;
        }

        private static Dictionary<string, byte[]> ReadAllBytes() => ActivityEventCsvImporterV2.ProjectRelativePaths
            .ToDictionary(path => path, path => File.ReadAllBytes(FullPath(path)), StringComparer.Ordinal);

        private static byte[] Encode(string text)
        {
            var payload = new UTF8Encoding(false).GetBytes(text.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n') + "\n");
            return new byte[] { 0xef, 0xbb, 0xbf }.Concat(payload).ToArray();
        }

        private static string FullPath(string projectRelativePath)
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(root, projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private sealed class Fixture
        {
            public Fixture(TerrainClusterAuthoringCatalog terrain, ActivityEventCsvImportResult result)
            {
                Terrain = terrain;
                Result = result;
            }
            public TerrainClusterAuthoringCatalog Terrain { get; }
            public ActivityEventCsvImportResult Result { get; }
        }

        private sealed class ActivitySpec
        {
            public ActivitySpec(string clusterId, int weight, ActivityStrengthClass strength, int slotCount)
            { ClusterId = clusterId; Weight = weight; Strength = strength; SlotCount = slotCount; }
            public string ClusterId { get; }
            public int Weight { get; }
            public ActivityStrengthClass Strength { get; }
            public int SlotCount { get; }
        }

        private sealed class EventSpec
        {
            public EventSpec(EventOverlayKind kind, int weight, int gap, int markerCount)
            { Kind = kind; Weight = weight; Gap = gap; MarkerCount = markerCount; }
            public EventOverlayKind Kind { get; }
            public int Weight { get; }
            public int Gap { get; }
            public int MarkerCount { get; }
        }
    }
}
