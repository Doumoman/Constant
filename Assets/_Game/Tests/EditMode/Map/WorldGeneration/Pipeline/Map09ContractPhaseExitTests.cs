using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.Tests.EditMode.WorldGeneration.Activities;
using StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Pipeline
{
    [TestFixture]
    [Category("MAP09_08")]
    public sealed class Map09ContractPhaseExitTests
    {
        private const string LayerDigest =
            "d0888c865cbdcc0884dc8abab9fac92900addd662a12a1ec30dc930f9cf4c94e";
        private const string MicroPatternDigest =
            "42c88cdb30154f098593d0e3be65063111613612fe5e9e1b9b11f2d9f1297a3d";
        private const string TerrainClusterDigest =
            "e8c3228e6f9df360637023d68e9c243cb70df4122342a3251740054bbcc8f9f1";
        private const string ActivityDigest =
            "7a5357320d8e2634ab9416ae7c90fb80a83c1c7f799a8df7689ba37b8a0903bc";
        private const string EventOverlayDigest =
            "722a490f054e5bfc5a75ac81e03eee4978cd7f51d34e01fa1e01818c9d4ce904";
        private const string SpecialRegionDigest =
            "73fd2085ecf65057f25eec8b2ff4fceb1a4d1a1a0eadfd60b7595071936a7066";
        private const string SectorCanvasDigest =
            "7c26d2d12d418a6f203e793bffd49216c003a6c0fc6f6f2bea06d210d3bded0c";
        private const string ValidationStampDigest =
            "cb909e6a1fc2a14bbd4e8b5a6ab103b5926e0428f535163f428f8dafda38a9f6";
        private const string GeneratedSliceDigest =
            "2066f58b09e3ac8ef0118c54e243008f54bcefe1e3bb032fa67dbe5d25156368";
        private const string SchemaDigest =
            "29ab147fe92487499a0cc5a1ca6dab0ba84d4c742320bc3ca2180c9ecbf2813c";

        private static Map09ContractPhaseExitFixture Fixture => Map09ContractPhaseExitFixture.Live;

        [Test]
        public void ExactLiveBaselineCountsAndDigestsRemainApproved()
        {
            Assert.That(V2PassCatalog.Entries, Has.Count.EqualTo(10));
            Assert.That(V2PassCatalog.StableDigest, Is.EqualTo(Map09ApprovedBaseline.CatalogDigest));
            Assert.That(GenerationLayerCatalog.Entries, Has.Count.EqualTo(7));
            Assert.That(GenerationLayerCatalog.StableDigest, Is.EqualTo(LayerDigest));
            Assert.That(Fixture.MicroPattern.IsValid, Is.True, Join(Fixture.MicroPattern.Errors));
            Assert.That(Fixture.MicroPattern.StableDigest, Is.EqualTo(MicroPatternDigest));
            Assert.That(Fixture.TerrainCluster.IsValid, Is.True, Join(Fixture.TerrainCluster.Errors));
            Assert.That(Fixture.TerrainCluster.CanonicalDigest, Is.EqualTo(TerrainClusterDigest));
            Assert.That(Fixture.Activity.IsValid, Is.True, Join(Fixture.Activity.Errors));
            Assert.That(Fixture.Activity.CanonicalDigest, Is.EqualTo(ActivityDigest));
            Assert.That(Fixture.EventOverlay.IsValid, Is.True, Join(Fixture.EventOverlay.Errors));
            Assert.That(Fixture.EventOverlay.CanonicalDigest, Is.EqualTo(EventOverlayDigest));
            Assert.That(Fixture.SpecialRegion.IsValid, Is.True, Join(Fixture.SpecialRegion.Errors));
            Assert.That(Fixture.SpecialRegion.CanonicalDigest, Is.EqualTo(SpecialRegionDigest));
            Assert.That(Fixture.Canvas.IsValid, Is.True, Join(Fixture.Canvas.Errors));
            Assert.That(Fixture.Canvas.CanonicalDigest, Is.EqualTo(SectorCanvasDigest));
            Assert.That(Fixture.CanvasContract.ValidationStamp.StableDigest, Is.EqualTo(ValidationStampDigest));
            Assert.That(Fixture.GeneratedSlices.IsValid, Is.True, Join(Fixture.GeneratedSlices.Errors));
            Assert.That(Fixture.GeneratedSlices.CanonicalDigest, Is.EqualTo(GeneratedSliceDigest));
            Assert.That(Fixture.Registry.Tables, Has.Count.EqualTo(29));
            Assert.That(Fixture.Registry.Tables.Sum(value => value.Columns.Count), Is.EqualTo(189));
            Assert.That(Fixture.Registry.Tables.Count(value =>
                value.Owner == V2AuthoringOwner.TerrainCluster), Is.EqualTo(13));
            Assert.That(Fixture.Registry.Tables.SelectMany(value => value.Columns)
                .Count(value => value.ForeignKey != null), Is.EqualTo(59));
            Assert.That(Fixture.Registry.CanonicalDigest, Is.EqualTo(SchemaDigest));
        }

        [Test]
        public void PassDependencyOrderIdentityOutputsAndFailureOwnershipAreAcyclicAndUnique()
        {
            var expected = new[]
            {
                V2WorldGenerationPassId.Pacing,
                V2WorldGenerationPassId.SpecialRegionReservation,
                V2WorldGenerationPassId.TerrainClusterReservation,
                V2WorldGenerationPassId.RouteSpine,
                V2WorldGenerationPassId.TraversalEnvelope,
                V2WorldGenerationPassId.MicroPattern,
                V2WorldGenerationPassId.TerrainCleanup,
                V2WorldGenerationPassId.ActivityEventOverlay,
                V2WorldGenerationPassId.TileValidation,
                V2WorldGenerationPassId.MicroChunkSlice,
            };
            var entries = V2PassCatalog.Entries;
            var validation = V2PassCatalogValidator.Validate(entries);
            Assert.That(validation.IsValid, Is.True, Join(validation.Issues));
            Assert.That(entries.Select(value => value.PassId), Is.EqualTo(expected));
            Assert.That(entries.Select(value => value.Order).Distinct().Count(), Is.EqualTo(entries.Count));
            Assert.That(entries.Select(value => value.FailureOwner).Distinct().Count(), Is.EqualTo(entries.Count));
            Assert.That(entries.SelectMany(value => value.OutputArtifactIds).Distinct().Count(),
                Is.EqualTo(entries.Count));

            var available = new HashSet<V2WorldGenerationArtifactId>
            {
                V2WorldGenerationArtifactId.ApprovedMapBaseline,
            };
            foreach (var entry in entries)
            {
                Assert.That(entry.InputArtifactIds.All(available.Contains), Is.True, entry.PassId.ToString());
                foreach (var output in entry.OutputArtifactIds) available.Add(output);
            }

            var validationPass = entries.Single(value => value.PassId == V2WorldGenerationPassId.TileValidation);
            Assert.That(validationPass.RetryEscalation,
                Is.EqualTo(new[] { V2RetryScope.Pattern, V2RetryScope.Cluster, V2RetryScope.Footprint }));
            Assert.That(entries.All(value => !value.AllowsSilentFallback), Is.True);
        }

        [Test]
        public void SevenLayerResponsibilitiesAndTraversalMechanismProgressionGraphsStaySeparated()
        {
            var validation = GenerationLayerCatalogValidator.Validate(GenerationLayerCatalog.Entries);
            Assert.That(validation.IsValid, Is.True, Join(validation.Errors));
            var ownership = GenerationLayerCatalog.Entries
                .SelectMany(layer => layer.OwnedResponsibilities.Select(value => new { layer.LayerId, Value = value }))
                .ToArray();
            Assert.That(ownership, Has.Length.EqualTo(9));
            Assert.That(ownership.GroupBy(value => value.Value).All(value => value.Count() == 1), Is.True);
            Assert.That(ownership.Single(value => value.Value == LayerResponsibilityId.StaticTerrainTraversal).LayerId,
                Is.EqualTo(GenerationLayerId.TerrainCluster));
            Assert.That(ownership.Single(value => value.Value == LayerResponsibilityId.StrongGameplayIncident).LayerId,
                Is.EqualTo(GenerationLayerId.ActivityStructure));
            Assert.That(ownership.Single(value => value.Value == LayerResponsibilityId.MarkerOnlyRunVariation).LayerId,
                Is.EqualTo(GenerationLayerId.EventOverlay));
            Assert.That(Fixture.ClusterContract.Traversal.Variants.All(value =>
                value.GraphKind == TraversalGraphKind.Traversal), Is.True);
            Assert.That(Fixture.ActivityContract.MechanismGraph.GraphKind, Is.EqualTo(TraversalGraphKind.Mechanism));
            Assert.That(Fixture.ActivityContract.ProgressionGraph.GraphKind, Is.EqualTo(TraversalGraphKind.Progression));
            Assert.That(typeof(EventOverlayContract).GetProperties().Select(value => value.Name),
                Has.None.Contains("Graph"));
        }

        [Test]
        public void SpecialReservationPrecedesClusterAndPatternWhileProtectedTraversalSourcesRemainFixed()
        {
            var passes = V2PassCatalog.Entries.ToDictionary(value => value.PassId, value => value.Order);
            Assert.That(passes[V2WorldGenerationPassId.SpecialRegionReservation],
                Is.LessThan(passes[V2WorldGenerationPassId.TerrainClusterReservation]));
            Assert.That(passes[V2WorldGenerationPassId.TraversalEnvelope],
                Is.LessThan(passes[V2WorldGenerationPassId.MicroPattern]));
            Assert.That(Fixture.SpecialContract.FixedShell, Is.Not.Empty);
            Assert.That(Fixture.SpecialContract.Slots.Single(value => value.Kind == SpecialRegionSlotKind.Entry).Required,
                Is.True);
            Assert.That(Fixture.SpecialContract.Ports.All(value => value.AccessClass == AccessClass.MandatoryNoTool),
                Is.True);
            Assert.That(Fixture.MicroPattern.Definition.ProtectedPolicy,
                Is.EqualTo(MicroPatternProtectedPolicy.ForceNoChange));
            var mandatoryEdges = Fixture.ClusterContract.Traversal.Variants
                .SelectMany(value => value.Edges).Where(value => value.IsMandatory).ToArray();
            Assert.That(mandatoryEdges, Is.Not.Empty);
            Assert.That(mandatoryEdges.All(value => value.Envelope.ProtectedTiles.Contains(value.StartTile) &&
                                                    value.Envelope.ProtectedTiles.Contains(value.EndTile)), Is.True);
        }

        [Test]
        public void FourByFourPatternAndTwelveByEightGeneratedSlicesRemainDistinctContracts()
        {
            Assert.That(Fixture.MicroPattern.Definition.Width, Is.EqualTo(4));
            Assert.That(Fixture.MicroPattern.Definition.Height, Is.EqualTo(4));
            Assert.That(Fixture.MicroPattern.Definition.Cells, Has.Count.EqualTo(16));
            Assert.That(Fixture.CanvasContract.Width, Is.EqualTo(48));
            Assert.That(Fixture.CanvasContract.Height, Is.EqualTo(32));
            Assert.That(Fixture.SliceSet.Slices, Has.Count.EqualTo(16));
            Assert.That(Fixture.SliceSet.Slices.All(value => value.Cells.Count == 96), Is.True);
            Assert.That(Fixture.SliceSet.Slices.All(value => value.Cells.Max(cell => cell.LocalCoordinate.X) == 11 &&
                                                             value.Cells.Max(cell => cell.LocalCoordinate.Y) == 7),
                Is.True);
        }

        [Test]
        public void ActivityAndEventRemovalPreserveStaticTraversalAndMarkerOnlyOwnership()
        {
            var safety = Fixture.ActivityContract.RemovalSafety;
            Assert.That(safety.PreserveStaticTraversal, Is.True);
            Assert.That(safety.PreserveAccessClass, Is.True);
            Assert.That(safety.PermanentSolidMutationAllowed, Is.False);
            Assert.That(safety.MandatoryExitDestructionAllowed, Is.False);
            Assert.That(safety.TraversalDigestAfterRemoval, Is.EqualTo(safety.TraversalDigestBeforeRemoval));
            Assert.That(safety.AccessClassAfterRemoval, Is.EqualTo(safety.AccessClassBeforeRemoval));
            var evidence = Fixture.EventEvidence;
            Assert.That(evidence.StaticShellDigestAfterRemoval, Is.EqualTo(evidence.StaticShellDigestBeforeRemoval));
            Assert.That(evidence.MandatoryPathDigestAfterRemoval,
                Is.EqualTo(evidence.MandatoryPathDigestBeforeRemoval));
            Assert.That(evidence.AccessClassAfterRemoval, Is.EqualTo(evidence.AccessClassBeforeRemoval));
            Assert.That(evidence.DeclaresNonMarkerMutation, Is.False);
            Assert.That(Fixture.EventContract.Assignments, Is.Not.Empty);
            Assert.That(typeof(EventOverlayContract).GetProperties().Select(value => value.Name),
                Has.None.Contains("Collision").And.None.Contains("Route").And.None.Contains("Access"));
        }

        [Test]
        public void CanvasStampAndSlicesCoverEveryCellExactlyOnceWithUnchangedProvenance()
        {
            var canvas = Fixture.CanvasContract;
            Assert.That(canvas.Cells, Has.Count.EqualTo(1536));
            Assert.That(canvas.Cells.Select(value => value.CanonicalIndex).Distinct().Count(), Is.EqualTo(1536));
            Assert.That(canvas.ValidationStamp.State, Is.EqualTo(SectorCanvasValidationState.Validated));
            Assert.That(canvas.ValidationStamp.PassCatalogDigest, Is.EqualTo(V2PassCatalog.StableDigest));
            Assert.That(canvas.ValidationStamp.LayerCatalogDigest, Is.EqualTo(GenerationLayerCatalog.StableDigest));

            var projected = Fixture.SliceSet.Slices.SelectMany(slice => slice.Cells.Select(cell => new
            {
                GlobalX = slice.Coordinate.X * WorldGenConstants.MicroChunkWidthTiles + cell.LocalCoordinate.X,
                GlobalY = slice.Coordinate.Y * WorldGenConstants.MicroChunkHeightTiles + cell.LocalCoordinate.Y,
                Cell = cell,
                Slice = slice,
            })).ToArray();
            Assert.That(projected, Has.Length.EqualTo(1536));
            Assert.That(projected.Select(value => value.GlobalY * 48 + value.GlobalX).Distinct().Count(),
                Is.EqualTo(1536));
            foreach (var item in projected)
            {
                var source = canvas.Cells[item.GlobalY * 48 + item.GlobalX];
                Assert.That(item.Cell.Layers, Is.SameAs(source.Layers));
                Assert.That(item.Cell.Provenance, Is.SameAs(source.Provenance));
                Assert.That(item.Slice.Provenance.SourceCanvasId, Is.EqualTo(canvas.Id));
                Assert.That(item.Slice.Provenance.Transform, Is.EqualTo(GeneratedSliceTransform.None));
            }
        }

        [Test]
        public void SchemaRegistryPreservesPrimaryForeignKeyIndexesAndGeneratedSeparation()
        {
            var registry = Fixture.Registry;
            Assert.That(registry.Tables, Has.Count.EqualTo(29));
            Assert.That(registry.Tables.Sum(value => value.Columns.Count), Is.EqualTo(189));
            Assert.That(registry.Tables.Count(value =>
                value.Owner == V2AuthoringOwner.TerrainCluster), Is.EqualTo(13));
            Assert.That(registry.Tables.Where(value =>
                    value.Owner == V2AuthoringOwner.Activity || value.Owner == V2AuthoringOwner.EventOverlay)
                .Select(value => value.RelativeAuthoringPath), Is.EqualTo(new[]
            {
                "Activity/activity_catalog_v2.csv",
                "Activity/activity_compatibility_v2.csv",
                "Activity/activity_cues_v2.csv",
                "Activity/activity_graph_edges_v2.csv",
                "Activity/activity_graph_nodes_v2.csv",
                "Activity/activity_safety_cells_v2.csv",
                "Activity/activity_slots_v2.csv",
                "EventOverlay/event_overlay_catalog_v2.csv",
                "EventOverlay/event_overlay_compatibility_v2.csv",
                "EventOverlay/event_overlay_markers_v2.csv",
            }));
            Assert.That(registry.Tables.All(table =>
                registry.ForeignKeyIndex.GetPrimaryKeyColumns(table.FileName).Count > 0), Is.True);
            Assert.That(registry.Tables.All(table =>
                registry.ForeignKeyIndex.GetPrimaryKeyColumns(table.FileName)
                    .Select(value => value.PrimaryKeyOrder.Value)
                    .SequenceEqual(Enumerable.Range(1,
                        registry.ForeignKeyIndex.GetPrimaryKeyColumns(table.FileName).Count))), Is.True);
            var foreignKeys = registry.Tables.SelectMany(value => value.Columns)
                .Where(value => value.ForeignKey != null).Select(value => value.ForeignKey).ToArray();
            Assert.That(foreignKeys, Has.Length.EqualTo(59));
            Assert.That(foreignKeys.Count(value =>
                value.TargetDomain == V2AuthoringSchemaDomain.LegacyAuthoring), Is.EqualTo(2));
            Assert.That(foreignKeys.Count(value =>
                value.TargetDomain == V2AuthoringSchemaDomain.Generated), Is.Zero);
            Assert.That(registry.Tables.Count(value => value.RelativeAuthoringPath.IndexOf(
                "Generated", StringComparison.OrdinalIgnoreCase) >= 0), Is.Zero);
        }

        [Test]
        public void Map07AndMap08CompatibilityRemainReadOnlyWithExactBoundaryEvidence()
        {
            var foreignKeys = Fixture.Registry.Tables.SelectMany(table => table.Columns
                    .Where(column => column.ForeignKey != null &&
                                     column.ForeignKey.TargetDomain == V2AuthoringSchemaDomain.LegacyAuthoring)
                    .Select(column => column.ForeignKey.TargetFileName + "." +
                                      column.ForeignKey.TargetColumnName))
                .ToArray();
            Assert.That(foreignKeys, Is.EqualTo(new[]
            {
                "microchunk_catalog.csv.microchunk_id",
                "boundary_chunk_catalog.csv.boundary_chunk_id",
            }));

            var evidence = BoundaryCoverageAuthoringHarness.GetOrCreate();
            Assert.That(evidence.Report.Accepted, Is.True, Join(evidence.Report.Issues));
            Assert.That(evidence.Report.PairReportCount, Is.EqualTo(6));
            Assert.That(evidence.Report.CandidateCountTotal, Is.EqualTo(31));
            Assert.That(evidence.Candidates.Count * 2, Is.EqualTo(62));
            Assert.That(evidence.Report.StableDigest, Is.EqualTo(Map09ApprovedBaseline.BoundaryDigest));
        }

        [Test]
        public void PublishedCollectionsAreImmutableDigestsDeterministicAndInvalidSchemasPublishNothing()
        {
            Assert.Throws<NotSupportedException>(() => ((IList<V2PassContract>)V2PassCatalog.Entries).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<GenerationLayerContract>)GenerationLayerCatalog.Entries).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<V2AuthoringTableDescriptor>)Fixture.Registry.Tables).Clear());
            Assert.That(V2PassCatalog.ComputeStableDigest(V2PassCatalog.Entries.Reverse()),
                Is.EqualTo(V2PassCatalog.StableDigest));
            Assert.That(GenerationLayerCatalog.ComputeStableDigest(GenerationLayerCatalog.Entries.Reverse()),
                Is.EqualTo(GenerationLayerCatalog.StableDigest));
            Assert.That(V2AuthoringSchemaCanonicalDigest.Compute(Fixture.Registry.Tables.Reverse()),
                Is.EqualTo(Fixture.Registry.CanonicalDigest));

            var invalid = V2AuthoringSchemaValidator.Validate(
                Fixture.Registry.Tables.Concat(new[] { Fixture.Registry.Tables[0] }), Fixture.LegacyCatalog);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Registry, Is.Null);
            Assert.That(invalid.ForeignKeyIndex, Is.Null);
            Assert.That(invalid.CanonicalDigest, Is.Null);
        }

        [Test]
        public void Map09RuntimeProductionHasNoForbiddenLegacyUnityOrPdfDependencies()
        {
            var roots = new[]
            {
                "Pipeline", "MicroPatterns", "TerrainClusters", "Activities",
                "EventOverlays", "SpecialRegions", "Baking",
            };
            var paths = roots.SelectMany(root => Directory.GetFiles(FullPath(
                    "Assets/_Game/Map/Runtime/WorldGeneration/" + root), "*.cs", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(FullPath("Assets/_Game/Map/Runtime/WorldGeneration/Data"),
                    "V2AuthoringSchema*.cs", SearchOption.TopDirectoryOnly))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var source = string.Join("\n", paths.Select(File.ReadAllText));
            foreach (var forbidden in new[]
                     {
                         "StageMapGenerator", "GridWorld", "RoomTemplate", "RoomGridTransform",
                         "TileMutationService", "SectorRecipeResolver", "UnityEditor", ".pdf",
                     })
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
        }

        [Test]
        public void LegacyAuthoringManifestAndGeneratedInventoryRemainAtApprovedBoundary()
        {
            var authoringRoot = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var csvFiles = Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories);
            var metaFiles = Directory.GetFiles(authoringRoot, "*.csv.meta", SearchOption.AllDirectories);
            var registered = Fixture.Registry.Tables.ToDictionary(
                table => table.RelativeAuthoringPath.Replace('\\', '/'),
                table => table,
                StringComparer.Ordinal);
            var physical = csvFiles.Select(path => new
                {
                    Path = path,
                    Relative = path.Substring(authoringRoot.Length + 1).Replace('\\', '/'),
                })
                .ToArray();
            Assert.That(physical.GroupBy(value => value.Relative, StringComparer.Ordinal)
                .All(group => group.Count() == 1), Is.True);

            var registeredPhysical = physical.Where(value => registered.ContainsKey(value.Relative)).ToArray();
            var legacyPhysical = physical.Where(value => !registered.ContainsKey(value.Relative)).ToArray();
            var legacyMeta = metaFiles.Count(path =>
            {
                var relative = path.Substring(authoringRoot.Length + 1).Replace('\\', '/');
                return !registered.ContainsKey(relative.Substring(0, relative.Length - ".meta".Length));
            });
            Assert.That(legacyPhysical, Has.Length.EqualTo(50));
            Assert.That(legacyMeta, Is.EqualTo(50));
            Assert.That(ComputeAuthoringManifest(authoringRoot, legacyPhysical.Select(value => value.Path)),
                Is.EqualTo(Map09ApprovedBaseline.AuthoringManifest));

            foreach (var file in registeredPhysical)
            {
                var descriptor = registered[file.Relative];
                var matchingMeta = metaFiles.Count(path => string.Equals(
                    path.Substring(authoringRoot.Length + 1).Replace('\\', '/'),
                    file.Relative + ".meta", StringComparison.Ordinal));
                Assert.That(matchingMeta, Is.EqualTo(1), file.Relative);
                var bytes = File.ReadAllBytes(file.Path);
                Assert.That(bytes.Length, Is.GreaterThanOrEqualTo(4), file.Relative);
                Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }), file.Relative);
                var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
                Assert.That(text, Does.Not.Contain("\r"), file.Relative);
                Assert.That(text, Does.EndWith("\n"), file.Relative);
                var header = text.Substring(0, text.IndexOf('\n'));
                Assert.That(header, Is.EqualTo(string.Join(",", descriptor.Columns
                    .OrderBy(column => column.ColumnOrder).Select(column => column.ColumnName))), file.Relative);
                Assert.That(file.Relative.IndexOf("Generated", StringComparison.OrdinalIgnoreCase),
                    Is.LessThan(0), file.Relative);
            }

            Assert.That(Directory.GetFiles(FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated"),
                "*.csv", SearchOption.AllDirectories), Is.Empty);
        }

        private static string ComputeAuthoringManifest(string root, IEnumerable<string> paths)
        {
            var utf8WithoutBom = new UTF8Encoding(false);
            var utf8WithBom = new UTF8Encoding(true);
            var records = paths.Select(path => new
                {
                    Path = path,
                    Relative = path.Substring(root.Length + 1).Replace('\\', '/'),
                })
                .OrderBy(value => value.Relative, StringComparer.Ordinal)
                .Select(value =>
                {
                    var normalized = File.ReadAllText(value.Path, Encoding.UTF8)
                        .Replace("\r\n", "\n").Replace("\r", "\n");
                    var body = utf8WithoutBom.GetBytes(normalized);
                    return value.Relative + "\t" + Sha256(utf8WithBom.GetPreamble().Concat(body).ToArray());
                });
            return Sha256(utf8WithoutBom.GetBytes(string.Join("\n", records)));
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Join(IEnumerable<object> values)
        {
            return string.Join("\n", values.Select(value => value == null ? "null" : value.ToString()));
        }
    }

    internal sealed class Map09ContractPhaseExitFixture
    {
        private static readonly Lazy<Map09ContractPhaseExitFixture> LazyLive =
            new Lazy<Map09ContractPhaseExitFixture>(() => new Map09ContractPhaseExitFixture());

        private Map09ContractPhaseExitFixture()
        {
            MicroPattern = MicroPatternValidator.Validate(CreateMicroPattern());
            ClusterContract = CreateTerrainCluster();
            TerrainCluster = TerrainClusterContractValidator.Validate(ClusterContract);

            ActivityShell = ActivityEventFixture.CreateShell();
            ActivityContract = ActivityEventFixture.CreateActivity(ActivityShell);
            Activity = ActivityContractValidator.Validate(ActivityContract, ActivityShell);
            EventEvidence = ActivityEventFixture.CreateEventEvidence(ActivityShell, ActivityContract);
            EventContract = new EventOverlayContract(new EventOverlayId("EVT_LIVE_BASELINE"),
                EventOverlayKind.Npc, ActivityShell.Id, ActivityContract.Id,
                new[]
                {
                    new EventMarkerAssignment(new EventMarkerId("MARKER_ACTIVITY_CUE"),
                        EventMarkerOperation.SpawnNpc, "NPC_MOON_GUIDE"),
                }, "Fixture event");
            EventOverlay = EventOverlayValidator.Validate(EventContract, ActivityShell, ActivityContract,
                ActivityContract.Slots.Select(value => new EventMarkerId(value.MarkerId)), EventEvidence);

            var special = CreateSpecialRegion();
            SpecialContract = special.Contract;
            SpecialReservation = special.Reservation;
            SpecialRegion = SpecialRegionValidator.Validate(SpecialContract, SpecialReservation);

            CanvasContract = CreateCanvas();
            Canvas = SectorCanvasContractValidator.Validate(CanvasContract);
            SliceSet = CreateSlices(CanvasContract);
            GeneratedSlices = GeneratedSliceContractValidator.Validate(SliceSet, CanvasContract);

            LegacyCatalog = BuildLegacyCatalog();
            var registryResult = V2AuthoringSchemaRegistry.CreateDefault(LegacyCatalog);
            if (!registryResult.Success)
                throw new InvalidOperationException(string.Join("\n",
                    registryResult.Errors.Select(value => value.ToString())));
            Registry = registryResult.Registry;
        }

        public static Map09ContractPhaseExitFixture Live => LazyLive.Value;
        public MicroPatternValidationResult MicroPattern { get; }
        public TerrainClusterContract ClusterContract { get; }
        public TerrainClusterValidationResult TerrainCluster { get; }
        public TerrainClusterContract ActivityShell { get; }
        public ActivityStructureContract ActivityContract { get; }
        public ActivityValidationResult Activity { get; }
        public EventOverlayContract EventContract { get; }
        public EventOverlayRemovalEvidence EventEvidence { get; }
        public EventOverlayValidationResult EventOverlay { get; }
        public SpecialRegionContract SpecialContract { get; }
        public SiteReservation SpecialReservation { get; }
        public SpecialRegionValidationResult SpecialRegion { get; }
        public SectorCanvasContract CanvasContract { get; }
        public SectorCanvasValidationResult Canvas { get; }
        public GeneratedSliceSet SliceSet { get; }
        public GeneratedSliceValidationResult GeneratedSlices { get; }
        public CsvSchemaCatalog LegacyCatalog { get; }
        public V2AuthoringSchemaRegistry Registry { get; }

        private static MicroPatternDefinition CreateMicroPattern()
        {
            var cells = new List<MicroPatternCell>();
            for (var y = 0; y < MicroPatternDefinition.RequiredHeight; y++)
            for (var x = 0; x < MicroPatternDefinition.RequiredWidth; x++)
                cells.Add(new MicroPatternCell(new LocalTileCoord(x, y),
                    Array.Empty<MicroPatternInstruction>()));
            return new MicroPatternDefinition(new MicroPatternId("MP_LIVE_BASELINE"),
                MicroPatternDefinition.RequiredWidth, MicroPatternDefinition.RequiredHeight, cells, 1,
                MoonpalaceBiomePairCatalog.Canonical.Biomes, new[] { MicroPatternTransform.R0 },
                MicroPatternProtectedPolicy.ForceNoChange, "DISPLAY");
        }

        private static TerrainClusterContract CreateTerrainCluster()
        {
            const int chunkCount = 2;
            var id = new TerrainClusterId("TC_LIVE_BASELINE");
            var chunks = Enumerable.Range(0, chunkCount).Select(value => new ClusterChunkCoord(value, 0)).ToArray();
            var maxX = chunkCount * WorldGenConstants.MicroChunkWidthTiles - 1;
            var roles = new[]
            {
                Role("ANCHOR_ENTRY", ClusterRoleKind.Entry, 0, "NODE_ENTRY"),
                Role("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp, 4, "NODE_BUILD_UP"),
                Role("ANCHOR_CORE", ClusterRoleKind.Core, 9, "NODE_CORE"),
                Role("ANCHOR_RECOVERY", ClusterRoleKind.Recovery, maxX - 8, "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_REWARD", ClusterRoleKind.Reward,
                    new LocalTileCoord(maxX - 5, 2), "NODE_REWARD"),
                Role("ANCHOR_EXIT", ClusterRoleKind.Exit, maxX, "NODE_EXIT"),
            };
            var nodes = roles.Select(value => new TraversalNode(value.TraversalNodeId, value.Tile,
                value.Role != ClusterRoleKind.Reward, value.AnchorId)).ToArray();
            var byId = nodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var edges = new[]
            {
                Edge("EDGE_ENTRY_BUILD", byId["NODE_ENTRY"], byId["NODE_BUILD_UP"],
                    TraversalMovementKind.Walk, byId["NODE_BUILD_UP"].Tile),
                Edge("EDGE_BUILD_CORE", byId["NODE_BUILD_UP"], byId["NODE_CORE"],
                    TraversalMovementKind.Jump, byId["NODE_BUILD_UP"].Tile),
                Edge("EDGE_CORE_RECOVERY", byId["NODE_CORE"], byId["NODE_RECOVERY"],
                    TraversalMovementKind.Drop, byId["NODE_CORE"].Tile),
                Edge("EDGE_RECOVERY_EXIT", byId["NODE_RECOVERY"], byId["NODE_EXIT"],
                    TraversalMovementKind.Slide, byId["NODE_RECOVERY"].Tile),
            };
            var variant = new SpineVariant(new SpineVariantId("SPINE_BASELINE"), true,
                TraversalGraphKind.Traversal, nodes, edges);
            var ports = new[]
            {
                new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true, "ANCHOR_ENTRY",
                    roles[0].Tile, ClusterPortSide.L, new[] { 0, 1, 2, 3, 4 }),
                new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true, "ANCHOR_EXIT",
                    roles[5].Tile, ClusterPortSide.R, new[] { 1, 2, 3, 4 }),
            };
            return new TerrainClusterContract(id, new ClusterFootprint(chunks), roles, ports,
                new TerrainClusterTraversalContract(new[] { variant }), "Fixture display text");
        }

        private static ClusterRoleAnchor Role(string id, ClusterRoleKind kind, int x, string nodeId)
        {
            return new ClusterRoleAnchor(id, kind, new LocalTileCoord(x, 1), nodeId);
        }

        private static TraversalEdge Edge(string id, TraversalNode from, TraversalNode to,
            TraversalMovementKind movement, LocalTileCoord recovery)
        {
            var floor = movement == TraversalMovementKind.Walk || movement == TraversalMovementKind.Slide
                ? new[] { new LocalTileCoord(from.Tile.X, 0) }
                : Array.Empty<LocalTileCoord>();
            var jump = movement == TraversalMovementKind.Jump || movement == TraversalMovementKind.Bounce
                ? new[] { new LocalTileCoord((from.Tile.X + to.Tile.X) / 2,
                    Math.Min(7, Math.Max(from.Tile.Y, to.Tile.Y) + 2)) }
                : Array.Empty<LocalTileCoord>();
            var drop = movement == TraversalMovementKind.Drop
                ? new[] { new LocalTileCoord((from.Tile.X + to.Tile.X) / 2,
                    Math.Min(7, Math.Max(from.Tile.Y, to.Tile.Y) + 1)) }
                : Array.Empty<LocalTileCoord>();
            var envelope = new TraversalEnvelope(new[] { from.Tile, to.Tile }, floor,
                new[] { from.Tile, to.Tile }, jump, drop, new[] { to.Tile }, new[] { recovery });
            return new TraversalEdge(id, from.NodeId, to.NodeId, movement, from.Tile, to.Tile,
                1, 2, to.Tile, recovery, true, envelope);
        }

        private static SpecialFixture CreateSpecialRegion()
        {
            var regionId = new SpecialRegionId("SR_LIVE_BASELINE");
            var reservationId = new SiteReservationId("RES_SPECIAL_VILLAGE");
            var footprint = new SiteFootprint(2, 1, SiteFootprintTransform.R0, new[]
            {
                new SiteFootprintCell(0, 0, "ENTRY", "", "", new[] { SiteEntrySide.L }),
                new SiteFootprintCell(1, 0, "CORE", "", "", Array.Empty<SiteEntrySide>()),
            });
            var anchor = new SiteEntryAnchor(reservationId, "ENTRY_MAIN", new SectorCoord(2, 2),
                SiteEntrySide.L, new[] { 1, 2, 3 }, true, true);
            var reservation = new SiteReservation(reservationId, SiteReservationKind.Village,
                "VILLAGE_MOON", new SectorCoord(2, 2), footprint, "BIOME_ROOT", 1, new[] { anchor });
            var rewardId = new SpecialRegionSlotId("SR_SLOT_REWARD");
            var rewardKey = SpecialPersistenceKey.ForSlot(regionId, SpecialPersistenceScope.Reward, rewardId);
            var slots = new[]
            {
                new SpecialRegionSlot(new SpecialRegionSlotId("SR_SLOT_ENTRY"), SpecialRegionSlotKind.Entry,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 5), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
                new SpecialRegionSlot(rewardId, SpecialRegionSlotKind.Reward,
                    new SpecialRegionSectorOffset(1, 0), new LocalTileCoord(4, 4), true,
                    SpecialPersistenceScope.Reward, rewardKey),
                new SpecialRegionSlot(new SpecialRegionSlotId("SR_SLOT_RETURN"), SpecialRegionSlotKind.Return,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 6), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
            };
            var ports = new[]
            {
                new SpecialRegionPort("SR_PORT_ENTRY", slots[0].Id, SpecialRegionSlotKind.Entry,
                    slots[0].SectorOffset, slots[0].Tile, SiteEntrySide.L, AccessClass.MandatoryNoTool),
                new SpecialRegionPort("SR_PORT_RETURN", slots[2].Id, SpecialRegionSlotKind.Return,
                    slots[2].SectorOffset, slots[2].Tile, SiteEntrySide.L, AccessClass.MandatoryNoTool),
            };
            var persistence = new[]
            {
                new SpecialPersistenceBinding(SpecialPersistenceKey.ForRegion(regionId),
                    SpecialPersistenceScope.Region, default(SpecialRegionSlotId), "INITIAL_UNCLAIMED"),
                new SpecialPersistenceBinding(rewardKey, SpecialPersistenceScope.Reward,
                    rewardId, "INITIAL_AVAILABLE"),
            };
            var contract = new SpecialRegionContract(regionId, SpecialRegionKind.Village, reservationId,
                new SpecialRegionFootprint(new[]
                {
                    new SpecialRegionSectorOffset(0, 0), new SpecialRegionSectorOffset(1, 0),
                }),
                new[]
                {
                    new SpecialRegionFixedShellCell(new SpecialRegionSectorOffset(0, 0),
                        new LocalTileCoord(1, 1), "SHELL_WALL"),
                }, slots, ports, persistence, "Fixture");
            return new SpecialFixture(contract, reservation);
        }

        private static SectorCanvasContract CreateCanvas()
        {
            var cells = CreateCanvasCells().ToArray();
            var stamp = new SectorCanvasValidationStamp(SectorCanvasValidationState.Validated,
                V2PassCatalog.StableDigest, GenerationLayerCatalog.StableDigest,
                BakingCanonicalDigest.ComputeSourceArtifactSet(cells),
                BakingCanonicalDigest.ComputeResolvedCells(cells), new string('e', 64));
            return new SectorCanvasContract(new SectorCanvasId("CANVAS_LIVE_BASELINE"),
                WorldGenConstants.SectorWidthTiles, WorldGenConstants.SectorHeightTiles, cells, stamp);
        }

        private static IEnumerable<SectorCanvasCell> CreateCanvasCells()
        {
            for (var y = 0; y < WorldGenConstants.SectorHeightTiles; y++)
            for (var x = 0; x < WorldGenConstants.SectorWidthTiles; x++)
            {
                var first = x == 0 && y == 0;
                var sources = new List<CanvasSourceRef> { OwnerSource() };
                var keys = new List<SpecialPersistenceKey>();
                if (first)
                {
                    sources.Add(new CanvasSourceRef(CanvasSourceKind.Boundary, "BOUNDARY_CRATER_ROOT", 20,
                        true, new[] { SectorCanvasLayerKind.Background }));
                    sources.Add(new CanvasSourceRef(CanvasSourceKind.SpecialRegion, "SR_VILLAGE", 25,
                        false, new[] { SectorCanvasLayerKind.Marker }));
                    keys.Add(new SpecialPersistenceKey("SR_STATE_VILLAGE_REWARD_TREASURE"));
                }
                yield return new SectorCanvasCell(new LocalTileCoord(x, y),
                    new SectorCanvasLayerSnapshot(
                        ResolvedLayerValue.FromId("SOLID_STONE"),
                        first ? ResolvedLayerValue.FromId("BG_BOUNDARY") : ResolvedLayerValue.Empty,
                        ResolvedLayerValue.Empty, ResolvedLayerValue.Empty,
                        ResolvedLayerValue.FromId("MAT_STONE"), ResolvedLayerValue.Empty,
                        first ? ResolvedLayerValue.FromId("MARKER_SPECIAL") : ResolvedLayerValue.Empty,
                        ResolvedLayerValue.FromId("TC_CANVAS_OWNER")),
                    new SectorCanvasProvenance(sources, keys));
            }
        }

        private static CanvasSourceRef OwnerSource()
        {
            return new CanvasSourceRef(CanvasSourceKind.TerrainCluster, "TC_CANVAS_OWNER", 30, true,
                new[] { SectorCanvasLayerKind.Solid, SectorCanvasLayerKind.Owner });
        }

        private static GeneratedSliceSet CreateSlices(SectorCanvasContract canvas)
        {
            var canvasResult = SectorCanvasContractValidator.Validate(canvas);
            var slices = new List<GeneratedMicroChunkSlice>();
            for (var sliceY = 0; sliceY < WorldGenConstants.MicroChunkRowsPerSector; sliceY++)
            for (var sliceX = 0; sliceX < WorldGenConstants.MicroChunkColumnsPerSector; sliceX++)
            {
                var cells = new List<GeneratedSliceCell>();
                for (var localY = 0; localY < WorldGenConstants.MicroChunkHeightTiles; localY++)
                for (var localX = 0; localX < WorldGenConstants.MicroChunkWidthTiles; localX++)
                {
                    var canvasX = sliceX * WorldGenConstants.MicroChunkWidthTiles + localX;
                    var canvasY = sliceY * WorldGenConstants.MicroChunkHeightTiles + localY;
                    var source = canvas.Cells[canvasY * WorldGenConstants.SectorWidthTiles + canvasX];
                    cells.Add(new GeneratedSliceCell(new LocalTileCoord(localX, localY),
                        source.Layers, source.Provenance));
                }
                slices.Add(new GeneratedMicroChunkSlice(new GeneratedSliceCoord(sliceX, sliceY), cells,
                    new GeneratedSliceProvenance(canvas.Id, canvasResult.CanonicalDigest,
                        canvas.ValidationStamp.StableDigest, GeneratedSliceTransform.None)));
            }
            return new GeneratedSliceSet(canvas.Id, slices, GeneratedSliceBoundaryRole.GeneratedOutput);
        }

        private static CsvSchemaCatalog BuildLegacyCatalog()
        {
            var rows = ReadCsv(AuthoringPath("CSV_DATA_DICTIONARY.csv"))
                .Select((value, index) => new CsvSchemaDictionaryRow(
                    value["file_name"], value["column_order"], value["column_name"],
                    value["data_type"], value["required"], value["primary_key_order"],
                    value["default_value"], value["allowed_values"], value["foreign_key"],
                    value["description"], index + 2));
            var result = new CsvSchemaCatalogBuilder().Build(rows);
            if (!result.Success)
                throw new InvalidOperationException(string.Join("\n", result.Errors.Select(value => value.ToString())));
            return result.Catalog;
        }

        private static List<Dictionary<string, string>> ReadCsv(string path)
        {
            var lines = File.ReadAllLines(path);
            var headers = ParseCsvLine(lines[0].TrimStart('\uFEFF')).ToArray();
            var rows = new List<Dictionary<string, string>>();
            for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex])) continue;
                var fields = ParseCsvLine(lines[lineIndex]).ToArray();
                if (fields.Length != headers.Length)
                    throw new InvalidDataException(path + " row width mismatch at " + (lineIndex + 1));
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var index = 0; index < headers.Length; index++) row.Add(headers[index], fields[index]);
                rows.Add(row);
            }
            return rows;
        }

        private static IEnumerable<string> ParseCsvLine(string line)
        {
            var field = new StringBuilder();
            var quoted = false;
            for (var index = 0; index < line.Length; index++)
            {
                var character = line[index];
                if (character == '"')
                {
                    if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else quoted = !quoted;
                }
                else if (character == ',' && !quoted)
                {
                    yield return field.ToString();
                    field.Length = 0;
                }
                else field.Append(character);
            }
            if (quoted) throw new InvalidDataException("Unclosed quoted CSV field.");
            yield return field.ToString();
        }

        private static string AuthoringPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Assets", "_Game", "Map", "Data",
                "WorldGeneration", "Authoring", relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private sealed class SpecialFixture
        {
            public SpecialFixture(SpecialRegionContract contract, SiteReservation reservation)
            {
                Contract = contract;
                Reservation = reservation;
            }

            public SpecialRegionContract Contract { get; }
            public SiteReservation Reservation { get; }
        }
    }
}
