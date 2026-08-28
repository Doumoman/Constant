using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Microchunks;
using UnityEngine;
using ActiveMicrochunkDefinition = StarNight.Map.WorldGeneration.Microchunks.MicrochunkDefinition;
using ActiveMicrochunkObjectSlotDefinition = StarNight.Map.WorldGeneration.Microchunks.MicrochunkObjectSlotDefinition;
using ActiveMicrochunkSocketDefinition = StarNight.Map.WorldGeneration.Microchunks.MicrochunkSocketDefinition;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Data
{
    [Category("MAP09_07")]
    public sealed class V2AuthoringSchemaRegistryTests
    {
        private const string AuthoringManifest =
            "4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851";
        private const string BoundaryDigest =
            "f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68";

        private CsvSchemaCatalog legacyCatalog;
        private V2AuthoringSchemaRegistry registry;

        [OneTimeSetUp]
        public void BuildApprovedRegistry()
        {
            legacyCatalog = BuildLegacyCatalogFromCurrentDictionary();
            var result = V2AuthoringSchemaRegistry.CreateDefault(legacyCatalog);
            Assert.That(result.Success, Is.True,
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            registry = result.Registry;
        }

        [Test]
        public void RegistryPublishesExactTwentyFourTablesAcrossFiveApprovedOwnerRoots()
        {
            var expected = new[]
            {
                "Activity/activity_catalog_v2.csv",
                "Activity/activity_cues_v2.csv",
                "Activity/activity_graph_edges_v2.csv",
                "EventOverlay/event_overlay_catalog_v2.csv",
                "EventOverlay/event_overlay_markers_v2.csv",
                "MicroPattern/micro_pattern_catalog_v2.csv",
                "MicroPattern/micro_pattern_cells_v2.csv",
                "SpecialRegion/special_region_catalog_v2.csv",
                "SpecialRegion/special_region_cells_v2.csv",
                "SpecialRegion/special_region_persistence_v2.csv",
                "SpecialRegion/special_region_ports_v2.csv",
                "TerrainCluster/terrain_cluster_catalog_v2.csv",
                "TerrainCluster/terrain_cluster_cells_v2.csv",
                "TerrainCluster/terrain_cluster_envelope_cells_v2.csv",
                "TerrainCluster/terrain_cluster_high_route_benefits_v2.csv",
                "TerrainCluster/terrain_cluster_high_route_edges_v2.csv",
                "TerrainCluster/terrain_cluster_high_route_failures_v2.csv",
                "TerrainCluster/terrain_cluster_high_routes_v2.csv",
                "TerrainCluster/terrain_cluster_nodes_v2.csv",
                "TerrainCluster/terrain_cluster_ports_v2.csv",
                "TerrainCluster/terrain_cluster_role_anchors_v2.csv",
                "TerrainCluster/terrain_cluster_role_variant_links_v2.csv",
                "TerrainCluster/terrain_cluster_spine_edges_v2.csv",
                "TerrainCluster/terrain_cluster_variants_v2.csv",
            };
            Assert.That(registry.Tables.Select(value => value.RelativeAuthoringPath), Is.EqualTo(expected));
            Assert.That(registry.Tables, Has.Count.EqualTo(24));
            Assert.That(registry.Tables.Sum(value => value.Columns.Count), Is.EqualTo(143));
            Assert.That(registry.Tables.Count(value => value.Owner == V2AuthoringOwner.TerrainCluster),
                Is.EqualTo(13));
            Assert.That(registry.Tables.Select(value => value.Owner).Distinct().Count(), Is.EqualTo(5));
            Assert.That(registry.Tables.All(value =>
                value.RelativeAuthoringPath.StartsWith(value.Owner + "/", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void ExactSemanticFieldSetsAreDeclaredInContiguousColumnOrder()
        {
            var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "micro_pattern_catalog_v2.csv", Fields("pattern_id selection_weight biome_ids allowed_transforms protected_policy") },
                { "micro_pattern_cells_v2.csv", Fields("pattern_id local_x local_y operation layer payload_id") },
                { "terrain_cluster_catalog_v2.csv", Fields("cluster_id pacing_role biome_id footprint_variant_id spine_variant_id") },
                { "terrain_cluster_cells_v2.csv", Fields("cluster_id chunk_x chunk_y cell_role port_id access_class source_microchunk_id source_boundary_chunk_id") },
                { "terrain_cluster_variants_v2.csv", Fields("cluster_id spine_variant_id graph_kind") },
                { "terrain_cluster_role_anchors_v2.csv", Fields("cluster_id role_anchor_id role_kind local_x local_y") },
                { "terrain_cluster_role_variant_links_v2.csv", Fields("cluster_id spine_variant_id role_anchor_id node_id") },
                { "terrain_cluster_ports_v2.csv", Fields("cluster_id port_id port_kind is_primary role_anchor_id local_x local_y outward_side compatible_route_types access_class") },
                { "terrain_cluster_nodes_v2.csv", Fields("cluster_id spine_variant_id node_id local_x local_y mandatory") },
                { "terrain_cluster_spine_edges_v2.csv", Fields("cluster_id spine_variant_id edge_id from_node_id to_node_id movement start_x start_y end_x end_y mandatory graph_kind clearance_width clearance_height landing_width landing_x landing_y recovery_width recovery_x recovery_y estimated_duration_ms timing_ruleset_id") },
                { "terrain_cluster_envelope_cells_v2.csv", Fields("cluster_id spine_variant_id edge_id envelope_kind local_x local_y") },
                { "terrain_cluster_high_routes_v2.csv", Fields("cluster_id spine_variant_id high_route_id divergence_node_id rejoin_node_id high_point_node_id") },
                { "terrain_cluster_high_route_edges_v2.csv", Fields("cluster_id spine_variant_id high_route_id edge_order edge_id") },
                { "terrain_cluster_high_route_benefits_v2.csv", Fields("cluster_id spine_variant_id high_route_id benefit_id") },
                { "terrain_cluster_high_route_failures_v2.csv", Fields("cluster_id spine_variant_id high_route_id failure_node_id preferred_recovery_target_node_id") },
                { "activity_catalog_v2.csv", Fields("activity_id static_shell_id reward_policy recovery_policy removal_safe") },
                { "activity_cues_v2.csv", Fields("activity_id cue_id cue_kind marker_id") },
                { "activity_graph_edges_v2.csv", Fields("activity_id edge_id graph_kind edge_kind from_node_id to_node_id edge_order") },
                { "event_overlay_catalog_v2.csv", Fields("overlay_id selection_weight variant_kind is_empty") },
                { "event_overlay_markers_v2.csv", Fields("overlay_id marker_id marker_kind local_x local_y") },
                { "special_region_catalog_v2.csv", Fields("region_id region_kind reservation_id footprint_width footprint_height") },
                { "special_region_cells_v2.csv", Fields("region_id local_x local_y cell_kind slot_id") },
                { "special_region_ports_v2.csv", Fields("region_id port_id port_kind side access_class") },
                { "special_region_persistence_v2.csv", Fields("region_id persistence_key scope") },
            };

            foreach (var table in registry.Tables)
            {
                Assert.That(table.Columns.Select(value => value.ColumnName),
                    Is.EqualTo(expected[table.FileName]), table.FileName);
                Assert.That(table.Columns.Select(value => value.ColumnOrder),
                    Is.EqualTo(Enumerable.Range(1, table.Columns.Count)), table.FileName);
                Assert.That(table.Columns.Where(value => value.DataType == CsvSchemaDataType.Enum ||
                                                         value.DataType == CsvSchemaDataType.EnumList)
                    .All(value => value.AllowedValues.Count > 0), Is.True, table.FileName);
                Assert.That(table.Columns.SelectMany(value => value.AllowedValues)
                    .All(value => !string.IsNullOrWhiteSpace(value)), Is.True, table.FileName);
            }

            var sources = registry.Tables.Single(value => value.FileName == "terrain_cluster_cells_v2.csv");
            Assert.That(sources.Columns.Single(value => value.ColumnName == "source_microchunk_id").IsRequired,
                Is.False);
            Assert.That(sources.Columns.Single(value => value.ColumnName == "source_boundary_chunk_id").IsRequired,
                Is.False);
            Assert.That(sources.Columns.Single(value => value.ColumnName == "cell_role").IsRequired, Is.False);
            Assert.That(sources.Columns.Single(value => value.ColumnName == "port_id").IsRequired, Is.False);
            Assert.That(sources.Columns.Single(value => value.ColumnName == "access_class").IsRequired, Is.False);
            Assert.That(Column("terrain_cluster_ports_v2.csv", "compatible_route_types").DataType,
                Is.EqualTo(CsvSchemaDataType.IntList));
            Assert.That(Column("terrain_cluster_spine_edges_v2.csv", "graph_kind").AllowedValues,
                Is.EqualTo(new[] { "TRAVERSAL" }));
            Assert.That(registry.Tables.SelectMany(value => value.Columns)
                .Count(value => value.ForeignKey != null), Is.EqualTo(44));
            Assert.That(Column("micro_pattern_catalog_v2.csv", "protected_policy").AllowedValues,
                Is.EqualTo(new[] { "FORCE_NO_CHANGE", "REJECT_CANDIDATE" }));
            Assert.That(Column("micro_pattern_cells_v2.csv", "operation").AllowedValues,
                Does.Contain("NO_CHANGE").And.Contain("SET_MARKER"));
            Assert.That(Column("activity_graph_edges_v2.csv", "graph_kind").AllowedValues,
                Is.EqualTo(new[] { "MECHANISM", "PROGRESSION" }));
            Assert.That(Column("event_overlay_catalog_v2.csv", "variant_kind").AllowedValues,
                Does.Contain("EMPTY"));
        }

        [Test]
        public void EveryPrimaryKeyIsRequiredUniqueAndContiguous()
        {
            foreach (var table in registry.Tables)
            {
                var primaryKeys = registry.ForeignKeyIndex.GetPrimaryKeyColumns(table.FileName);
                Assert.That(primaryKeys, Is.Not.Empty, table.FileName);
                Assert.That(primaryKeys.All(value => value.IsRequired), Is.True, table.FileName);
                Assert.That(primaryKeys.Select(value => value.PrimaryKeyOrder.Value),
                    Is.EqualTo(Enumerable.Range(1, primaryKeys.Count)), table.FileName);
            }
        }

        [Test]
        public void OnlyTwoApprovedLegacyForeignKeysExistAndResolveToLegacyPrimaryKeys()
        {
            var legacy = registry.Tables.SelectMany(table => table.Columns
                    .Where(column => column.ForeignKey != null &&
                                     column.ForeignKey.TargetDomain == V2AuthoringSchemaDomain.LegacyAuthoring)
                    .Select(column => table.FileName + "." + column.ColumnName + "->" + column.ForeignKey.TargetFileName + "." + column.ForeignKey.TargetColumnName))
                .ToArray();
            Assert.That(legacy, Is.EqualTo(new[]
            {
                "terrain_cluster_cells_v2.csv.source_microchunk_id->microchunk_catalog.csv.microchunk_id",
                "terrain_cluster_cells_v2.csv.source_boundary_chunk_id->boundary_chunk_catalog.csv.boundary_chunk_id",
            }));
            Assert.That(legacyCatalog.GetFile("microchunk_catalog.csv").GetColumn("microchunk_id").PrimaryKeyOrder,
                Is.EqualTo(1));
            Assert.That(legacyCatalog.GetFile("boundary_chunk_catalog.csv").GetColumn("boundary_chunk_id").PrimaryKeyOrder,
                Is.EqualTo(1));
        }

        [Test]
        public void ForeignKeyIndexProvidesExactPathColumnPrimaryKeyIncomingAndOutgoingLookups()
        {
            var index = registry.ForeignKeyIndex;
            Assert.That(index.TryGetTable("Activity/activity_cues_v2.csv", out var table), Is.True);
            Assert.That(table.FileName, Is.EqualTo("activity_cues_v2.csv"));
            Assert.That(index.TryGetTable("activity/activity_cues_v2.csv", out _), Is.False);
            Assert.That(index.TryGetColumn("activity_cues_v2.csv", "cue_id", out var column), Is.True);
            Assert.That(column.PrimaryKeyOrder, Is.EqualTo(2));
            Assert.That(index.TryGetColumn("activity_cues_v2.csv", "Cue_Id", out _), Is.False);
            Assert.That(index.GetOutgoingForeignKeys("activity_graph_edges_v2.csv"), Has.Count.EqualTo(1));
            Assert.That(index.GetIncomingForeignKeys("activity_catalog_v2.csv"), Has.Count.EqualTo(2));
            Assert.That(index.GetIncomingForeignKeys("missing.csv"), Is.Empty);
        }

        [Test]
        public void RegistryDescriptorAndIndexCollectionsAreReadOnlyDefensiveSnapshots()
        {
            Assert.Throws<NotSupportedException>(() =>
                ((IList<V2AuthoringTableDescriptor>)registry.Tables).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<V2AuthoringColumnDescriptor>)registry.Tables[0].Columns).Clear());
            var enumColumn = registry.Tables.SelectMany(value => value.Columns)
                .First(value => value.AllowedValues.Count > 0);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)enumColumn.AllowedValues).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<V2AuthoringForeignKey>)registry.ForeignKeyIndex
                    .GetOutgoingForeignKeys("activity_cues_v2.csv")).Clear());
        }

        [Test]
        public void CanonicalDigestIsLowercaseStableAndIgnoresDisplayDescriptionAndEnumerationOrder()
        {
            Assert.That(registry.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(registry.CanonicalDigest,
                Is.EqualTo("78a0df2056db7b12241c127ba85c573e26859503856cd8c8ea1a12648c8f4b57"));
            Assert.That(V2AuthoringSchemaCanonicalDigest.Compute(registry.Tables.Reverse()),
                Is.EqualTo(registry.CanonicalDigest));
            var renamed = registry.Tables.Select(table => new V2AuthoringTableDescriptor(
                table.TableId,
                table.Owner,
                table.RelativeAuthoringPath,
                table.Columns.Select(column => new V2AuthoringColumnDescriptor(
                    column.ColumnOrder,
                    column.ColumnName,
                    column.DataType,
                    column.IsRequired,
                    column.PrimaryKeyOrder,
                    column.DefaultValue,
                    column.AllowedValues,
                    column.ForeignKey,
                    "changed description")),
                "changed display"));
            Assert.That(V2AuthoringSchemaCanonicalDigest.Compute(renamed), Is.EqualTo(registry.CanonicalDigest));
            Assert.That(V2AuthoringSchemaCanonicalDigest.Compute(registry.Tables.Where(
                    value => value.Owner == V2AuthoringOwner.MicroPattern)),
                Is.EqualTo("5d5423e226626de563c2dcb47b2c1aa7516ceae202f91082e1ebb70dba5b357c"));
            Assert.That(V2AuthoringSchemaCanonicalDigest.Compute(registry.Tables.Where(
                    value => value.Owner == V2AuthoringOwner.Activity)),
                Is.EqualTo("ee17b14fcf89136b2fa16d97ba78ffb2d636572715fb8af51e5d30d3e3c3d0a2"));
            Assert.That(V2AuthoringSchemaCanonicalDigest.Compute(registry.Tables.Where(
                    value => value.Owner == V2AuthoringOwner.EventOverlay)),
                Is.EqualTo("a5630b53fd943704194bf9b81ab5d1fbe3eff279af8c001caa3c7fc180610df5"));
            Assert.That(V2AuthoringSchemaCanonicalDigest.Compute(registry.Tables.Where(
                    value => value.Owner == V2AuthoringOwner.SpecialRegion)),
                Is.EqualTo("a0c5d9f97f0dc6e5281ef3d39fb69844d569656fd405af8c07c642b96eeb3b4e"));
        }

        [TestCase("missing_target", "MISSING_V2_FOREIGN_KEY_TARGET")]
        [TestCase("cycle", "V2_FOREIGN_KEY_CYCLE")]
        [TestCase("case_collision", "TABLE_PATH_CASE_COLLISION")]
        [TestCase("generated_target", "GENERATED_FOREIGN_KEY_TARGET")]
        [TestCase("duplicate", "DUPLICATE_TABLE_PATH")]
        [TestCase("unapproved_legacy", "UNAPPROVED_LEGACY_FOREIGN_KEY")]
        public void InvalidSchemasAccumulateDeterministicErrorsAndPublishNothing(
            string mutation,
            string expectedCode)
        {
            var result = V2AuthoringSchemaValidator.Validate(Mutate(mutation), legacyCatalog);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(expectedCode));
            Assert.That(result.Registry, Is.Null);
            Assert.That(result.ForeignKeyIndex, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Null);
            Assert.That(result.Errors, Is.Ordered.Using<V2AuthoringSchemaValidationError>(
                Comparer<V2AuthoringSchemaValidationError>.Create(V2AuthoringSchemaValidationErrorComparer)));
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        [Test]
        public void ActiveMap07MicrochunkProjectsLosslesslyAsReadOnlyLegacyFkEvidence()
        {
            var sourceIds = ReadCsv(AuthoringPath("MicroChunk/microchunk_catalog.csv"))
                .Select(value => value["microchunk_id"])
                .ToArray();
            var source = BuildCurrentMicrochunk("MC_GRAY_H_STRAIGHT_01");
            var first = Map09MicrochunkCompatibilityFixture.Project(source, sourceIds);
            var second = Map09MicrochunkCompatibilityFixture.Project(source, sourceIds.Reverse());

            Assert.That(first.SourceMicrochunkId, Is.EqualTo(source.Id.Value));
            Assert.That(first.WidthTiles, Is.EqualTo(12));
            Assert.That(first.HeightTiles, Is.EqualTo(8));
            Assert.That(first.Cells, Has.Count.EqualTo(96));
            Assert.That(first.Cells.Select(value => value.CoordinateKey).Distinct().Count(), Is.EqualTo(96));
            Assert.That(first.Cells.Select(value => value.Payload), Is.EqualTo(source.TileCells.Select(TilePayload)));
            Assert.That(first.Sockets.Select(value => value.Identity),
                Is.EqualTo(source.Sockets.Select(SocketIdentity)));
            Assert.That(first.CanonicalDigest, Is.EqualTo(second.CanonicalDigest));
            Assert.That(first.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<Map09MicrochunkCompatibilityFixture.CellView>)first.Cells).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<Map09MicrochunkCompatibilityFixture.SocketView>)first.Sockets).Clear());
        }

        [TestCase("missing_id", "MISSING_ID")]
        [TestCase("invalid_geometry", "INVALID_GEOMETRY")]
        [TestCase("missing_cell", "INVALID_CELL_COVERAGE")]
        [TestCase("duplicate_cell", "INVALID_CELL_COVERAGE")]
        [TestCase("unknown_fk", "UNKNOWN_LEGACY_FK")]
        public void InvalidMap07CompatibilityFixturesAreRejected(string mutation, string expectedCode)
        {
            var cells = Enumerable.Range(0, 96)
                .Select(value => new Map09MicrochunkCompatibilityFixture.RawCell(value % 12, value / 12))
                .ToList();
            var id = "MC_GRAY_H_STRAIGHT_01";
            var width = 12;
            var height = 8;
            var known = new[] { id };
            switch (mutation)
            {
                case "missing_id": id = string.Empty; break;
                case "invalid_geometry": width = 11; break;
                case "missing_cell": cells.RemoveAt(cells.Count - 1); break;
                case "duplicate_cell": cells[cells.Count - 1] = cells[0]; break;
                case "unknown_fk": known = new[] { "MC_OTHER" }; break;
            }

            Assert.That(Map09MicrochunkCompatibilityFixture.ValidateRaw(
                    id, width, height, cells, known), Does.Contain(expectedCode));
        }

        [Test]
        public void CurrentMap08BoundaryProjectionRecomputesExactApprovedCountsAndDigest()
        {
            var evidence = BoundaryCoverageAuthoringHarness.GetOrCreate();
            Assert.That(evidence.Report.Accepted, Is.True,
                string.Join("\n", evidence.Report.Issues.Select(value => value.ToString())));
            Assert.That(evidence.Report.PairReportCount, Is.EqualTo(6));
            Assert.That(evidence.Report.CandidateCountTotal, Is.EqualTo(31));
            Assert.That(evidence.Report.MicrochunkCountTotal, Is.EqualTo(31));
            Assert.That(evidence.Report.TileRowCountTotal, Is.EqualTo(2976));
            Assert.That(evidence.Report.SocketRowCountTotal, Is.EqualTo(62));
            Assert.That(evidence.Candidates.Count * 2, Is.EqualTo(62));
            Assert.That(evidence.Candidates.Count(value => value.ToolRequirement == "NONE"), Is.EqualTo(31));
            Assert.That(evidence.Report.StableDigest, Is.EqualTo(BoundaryDigest));
            Assert.That(evidence.Candidates.Select(value => value.CandidateId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(31));
            Assert.That(evidence.Candidates.Select(value => value.MicrochunkId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(31));
            Assert.That(evidence.Candidates.All(value => value.RouteType == 1 && value.Reversible &&
                                                        !string.IsNullOrEmpty(value.EntryEdgeSignatureId) &&
                                                        !string.IsNullOrEmpty(value.ExitEdgeSignatureId)), Is.True);
        }

        [Test]
        public void AuthoringAndGeneratedPhysicalInventoriesRemainAtApprovedBoundary()
        {
            var authoringRoot = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var csvFiles = Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories);
            var metaFiles = Directory.GetFiles(authoringRoot, "*.csv.meta", SearchOption.AllDirectories);
            Assert.That(csvFiles, Has.Length.EqualTo(52));
            Assert.That(metaFiles, Has.Length.EqualTo(52));
            Assert.That(ComputeAuthoringManifest(authoringRoot, csvFiles), Is.EqualTo(AuthoringManifest));
            Assert.That(registry.Tables.Count(value => value.RelativeAuthoringPath.IndexOf(
                "Generated", StringComparison.OrdinalIgnoreCase) >= 0), Is.Zero);
            Assert.That(registry.Tables.Count(value => value.FileName.StartsWith(
                "generated_", StringComparison.OrdinalIgnoreCase)), Is.Zero);
            Assert.That(registry.Tables.SelectMany(value => value.Columns).Count(value =>
                value.ForeignKey != null && value.ForeignKey.TargetDomain == V2AuthoringSchemaDomain.Generated),
                Is.Zero);
            var physicalV2 = registry.Tables.Where(table => File.Exists(Path.Combine(authoringRoot,
                    table.RelativeAuthoringPath.Replace('/', Path.DirectorySeparatorChar))))
                .Select(table => table.RelativeAuthoringPath).ToArray();
            Assert.That(physicalV2, Is.EqualTo(new[]
            {
                "MicroPattern/micro_pattern_catalog_v2.csv",
                "MicroPattern/micro_pattern_cells_v2.csv",
            }));
            Assert.That(registry.Tables.Where(value => value.Owner == V2AuthoringOwner.TerrainCluster)
                .All(table => !File.Exists(Path.Combine(authoringRoot,
                    table.RelativeAuthoringPath.Replace('/', Path.DirectorySeparatorChar)))), Is.True);
            Assert.That(Directory.GetFiles(
                FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated"),
                "*.csv", SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void NewRuntimeSchemaScopeContainsNoFileWriteRngLifecycleOrGeneratedOwnership()
        {
            var root = FullPath("Assets/_Game/Map/Runtime/WorldGeneration/Data");
            var sources = Directory.GetFiles(root, "V2Authoring*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(File.ReadAllText)
                .ToArray();
            var forbidden = new[]
            {
                "UnityEditor", "MonoBehaviour", "ScriptableObject", "System.Random", "UnityEngine.Random",
                "File.Write", "File.Create", "Directory.Create", "GeneratedSlice", "SectorCanvas",
                "Importer", "Exporter", "Writer", "Solver", "Composer", "Renderer", "Slicer",
            };
            Assert.That(forbidden.Where(value => sources.Any(source => source.Contains(value))), Is.Empty);
        }

        private IEnumerable<V2AuthoringTableDescriptor> Mutate(string mutation)
        {
            var tables = V2AuthoringSchemaRegistry.DescribeDefaultTables().ToList();
            if (mutation == "duplicate")
            {
                tables.Add(tables[0]);
                return tables;
            }
            if (mutation == "case_collision")
            {
                var source = tables.Single(value => value.FileName == "micro_pattern_catalog_v2.csv");
                tables.Add(new V2AuthoringTableDescriptor(
                    "Micro_Pattern_Catalog_V2", source.Owner,
                    "Micropattern/micro_pattern_catalog_v2.csv", source.Columns));
                return tables;
            }

            var table = mutation == "cycle"
                ? tables.Single(value => value.FileName == "micro_pattern_catalog_v2.csv")
                : mutation == "unapproved_legacy"
                    ? tables.Single(value => value.FileName == "activity_cues_v2.csv")
                    : tables.Single(value => value.FileName == "micro_pattern_cells_v2.csv");
            var sourceColumn = mutation == "cycle"
                ? table.Columns.Single(value => value.ColumnName == "pattern_id")
                : table.Columns.First(value => value.ForeignKey != null);
            V2AuthoringForeignKey replacement;
            switch (mutation)
            {
                case "missing_target":
                    replacement = new V2AuthoringForeignKey(
                        V2AuthoringSchemaDomain.AuthoringV2, "missing_v2.csv", "missing_id");
                    break;
                case "cycle":
                    replacement = new V2AuthoringForeignKey(
                        V2AuthoringSchemaDomain.AuthoringV2, "micro_pattern_cells_v2.csv", "pattern_id");
                    break;
                case "generated_target":
                    replacement = new V2AuthoringForeignKey(
                        V2AuthoringSchemaDomain.Generated, "generated_slice.csv", "slice_id");
                    break;
                case "unapproved_legacy":
                    replacement = new V2AuthoringForeignKey(
                        V2AuthoringSchemaDomain.LegacyAuthoring, "microchunk_catalog.csv", "microchunk_id");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mutation));
            }
            var columns = table.Columns.Select(value => ReferenceEquals(value, sourceColumn)
                ? Copy(value, replacement)
                : value);
            var replacementTable = new V2AuthoringTableDescriptor(
                table.TableId, table.Owner, table.RelativeAuthoringPath, columns, table.DisplayName);
            tables[tables.IndexOf(table)] = replacementTable;
            return tables;
        }

        private static V2AuthoringColumnDescriptor Copy(
            V2AuthoringColumnDescriptor value,
            V2AuthoringForeignKey foreignKey)
        {
            return new V2AuthoringColumnDescriptor(
                value.ColumnOrder, value.ColumnName, value.DataType, value.IsRequired,
                value.PrimaryKeyOrder, value.DefaultValue, value.AllowedValues, foreignKey, value.Description);
        }

        private V2AuthoringColumnDescriptor Column(string fileName, string columnName)
        {
            Assert.That(registry.ForeignKeyIndex.TryGetColumn(fileName, columnName, out var column), Is.True);
            return column;
        }

        private static int V2AuthoringSchemaValidationErrorComparer(
            V2AuthoringSchemaValidationError left,
            V2AuthoringSchemaValidationError right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.TablePath, right.TablePath);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.ColumnName, right.ColumnName);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.Code, right.Code);
            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(left.Message, right.Message);
        }

        private static CsvSchemaCatalog BuildLegacyCatalogFromCurrentDictionary()
        {
            var rows = ReadCsv(AuthoringPath("CSV_DATA_DICTIONARY.csv"))
                .Select((value, index) => new CsvSchemaDictionaryRow(
                    value["file_name"], value["column_order"], value["column_name"],
                    value["data_type"], value["required"], value["primary_key_order"],
                    value["default_value"], value["allowed_values"], value["foreign_key"],
                    value["description"], index + 2));
            var result = new CsvSchemaCatalogBuilder().Build(rows);
            Assert.That(result.Success, Is.True,
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            return result.Catalog;
        }

        private static ActiveMicrochunkDefinition BuildCurrentMicrochunk(string id)
        {
            var catalog = ReadCsv(AuthoringPath("MicroChunk/microchunk_catalog.csv"))
                .Single(value => value["microchunk_id"] == id);
            var cells = ReadCsv(AuthoringPath("MicroChunk/microchunk_tile_cells.csv"))
                .Where(value => value["microchunk_id"] == id)
                .Select(value => new MicrochunkTileCell(
                    new MicrochunkLocalCoord(ParseInt(value["local_x"]), ParseInt(value["local_y"])),
                    value["ground_code"], value["one_way_code"], value["breakable_code"],
                    value["hazard_code"], value["liquid_code"], value["decor_back_code"],
                    value["decor_front_code"], value["marker_code"]));
            var sockets = ReadCsv(AuthoringPath("MicroChunk/microchunk_sockets.csv"))
                .Where(value => value["microchunk_id"] == id)
                .Select(value => new ActiveMicrochunkSocketDefinition(
                    value["socket_id"], ParseSide(value["side"]), value["band_id"],
                    ParseTraversal(value["traversal_kind"]), value["direction"],
                    ParseBool(value["mandatory_allowed"]), ParseTool(value["tool_requirement"]),
                    value["edge_signature_id"], ParseRouteLayer(value["route_layer"]),
                    ParseInt(value["minimum_safe_tiles"]), value["notes"]));
            return new ActiveMicrochunkDefinition(
                new MicrochunkId(catalog["microchunk_id"]), catalog["display_name_ko"],
                ParseInt(catalog["width_tiles"]), ParseInt(catalog["height_tiles"]),
                ParseUsageClass(catalog["usage_class"]), Split(catalog["biome_ids"]),
                Split(catalog["route_roles"]), Split(catalog["allowed_transforms"]).Select(ParseTransform),
                ParseInt(catalog["selection_weight"]), ParseInt(catalog["threat"]),
                ParseInt(catalog["cognitive"]), ParseInt(catalog["chain"]),
                ParseBool(catalog["tile_data_complete"]), catalog["prefab_id"],
                ParseBool(catalog["active"]), catalog["notes"], cells, sockets,
                Array.Empty<ActiveMicrochunkObjectSlotDefinition>());
        }

        private static string TilePayload(MicrochunkTileCell value)
        {
            return string.Join("|", new[]
            {
                value.GroundCode, value.OneWayCode, value.BreakableCode, value.HazardCode,
                value.LiquidCode, value.DecorationBackCode, value.DecorationFrontCode, value.MarkerCode,
            });
        }

        private static string SocketIdentity(ActiveMicrochunkSocketDefinition value)
        {
            return string.Join("|", new[]
            {
                value.SocketId, value.Side.ToString(), value.BandId, value.TraversalKind.ToString(),
                value.Direction, value.EdgeSignatureId, value.RouteLayer.ToString(),
            });
        }

        private static string[] Fields(string value)
        {
            return value.Split(' ');
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

        private static string AuthoringPath(string relativePath)
        {
            return FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring/" + relativePath);
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string[] Split(string value)
        {
            return value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static int ParseInt(string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static bool ParseBool(string value)
        {
            if (value == "1") return true;
            if (value == "0") return false;
            throw new FormatException("Unknown bool token: " + value);
        }

        private static MicrochunkUsageClass ParseUsageClass(string value)
        {
            switch (value)
            {
                case "TRAVERSAL": return MicrochunkUsageClass.Traversal;
                case "BOUNDARY": return MicrochunkUsageClass.Boundary;
                case "FILLER": return MicrochunkUsageClass.Filler;
                case "SPECIAL": return MicrochunkUsageClass.Special;
                case "VILLAGE": return MicrochunkUsageClass.Village;
                case "ADAPTER": return MicrochunkUsageClass.Adapter;
                default: throw new FormatException("Unknown usage class: " + value);
            }
        }

        private static MicrochunkTransform ParseTransform(string value)
        {
            switch (value)
            {
                case "R0": return MicrochunkTransform.R0;
                case "MIRROR_X": return MicrochunkTransform.MirrorX;
                case "MIRROR_Y": return MicrochunkTransform.MirrorY;
                case "R180": return MicrochunkTransform.R180;
                default: throw new FormatException("Unknown transform: " + value);
            }
        }

        private static MicrochunkSide ParseSide(string value)
        {
            switch (value)
            {
                case "L": return MicrochunkSide.Left;
                case "R": return MicrochunkSide.Right;
                case "U": return MicrochunkSide.Up;
                case "D": return MicrochunkSide.Down;
                default: throw new FormatException("Unknown side: " + value);
            }
        }

        private static MicrochunkTraversalKind ParseTraversal(string value)
        {
            switch (value)
            {
                case "WALK": return MicrochunkTraversalKind.Walk;
                case "DROP": return MicrochunkTraversalKind.Drop;
                case "CLIMB": return MicrochunkTraversalKind.Climb;
                case "OPTIONAL_BREAK": return MicrochunkTraversalKind.OptionalBreak;
                case "HIDDEN": return MicrochunkTraversalKind.Hidden;
                case "DECORATION": return MicrochunkTraversalKind.Decoration;
                default: throw new FormatException("Unknown traversal: " + value);
            }
        }

        private static MicrochunkToolRequirement ParseTool(string value)
        {
            switch (value)
            {
                case "NONE": return MicrochunkToolRequirement.None;
                case "PICKAXE": return MicrochunkToolRequirement.Pickaxe;
                case "SHOVEL": return MicrochunkToolRequirement.Shovel;
                case "ROPE": return MicrochunkToolRequirement.Rope;
                case "EXPLOSIVE": return MicrochunkToolRequirement.Explosive;
                case "ENVIRONMENT": return MicrochunkToolRequirement.Environment;
                default: throw new FormatException("Unknown tool: " + value);
            }
        }

        private static MicrochunkRouteLayer ParseRouteLayer(string value)
        {
            switch (value)
            {
                case "MANDATORY": return MicrochunkRouteLayer.Mandatory;
                case "OPTIONAL": return MicrochunkRouteLayer.Optional;
                case "BOTH": return MicrochunkRouteLayer.Both;
                default: throw new FormatException("Unknown route layer: " + value);
            }
        }
    }

    internal static class Map09MicrochunkCompatibilityFixture
    {
        internal sealed class RawCell
        {
            public RawCell(int x, int y) { X = x; Y = y; }
            public int X { get; }
            public int Y { get; }
        }

        internal sealed class CellView
        {
            public CellView(MicrochunkTileCell source)
            {
                CoordinateKey = source.Coordinate.RowMajorIndex;
                Payload = string.Join("|", new[]
                {
                    source.GroundCode, source.OneWayCode, source.BreakableCode, source.HazardCode,
                    source.LiquidCode, source.DecorationBackCode, source.DecorationFrontCode, source.MarkerCode,
                });
            }
            public int CoordinateKey { get; }
            public string Payload { get; }
        }

        internal sealed class SocketView
        {
            public SocketView(ActiveMicrochunkSocketDefinition source)
            {
                Identity = string.Join("|", new[]
                {
                    source.SocketId, source.Side.ToString(), source.BandId, source.TraversalKind.ToString(),
                    source.Direction, source.EdgeSignatureId, source.RouteLayer.ToString(),
                });
            }
            public string Identity { get; }
        }

        internal sealed class View
        {
            public View(
                string sourceMicrochunkId,
                int widthTiles,
                int heightTiles,
                IEnumerable<CellView> cells,
                IEnumerable<SocketView> sockets,
                string canonicalDigest)
            {
                SourceMicrochunkId = sourceMicrochunkId;
                WidthTiles = widthTiles;
                HeightTiles = heightTiles;
                Cells = new ReadOnlyCollection<CellView>(cells.ToList());
                Sockets = new ReadOnlyCollection<SocketView>(sockets.ToList());
                CanonicalDigest = canonicalDigest;
            }
            public string SourceMicrochunkId { get; }
            public int WidthTiles { get; }
            public int HeightTiles { get; }
            public IReadOnlyList<CellView> Cells { get; }
            public IReadOnlyList<SocketView> Sockets { get; }
            public string CanonicalDigest { get; }
        }

        public static View Project(ActiveMicrochunkDefinition source, IEnumerable<string> knownLegacyIds)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var errors = ValidateRaw(
                source.Id.Value,
                source.WidthTiles,
                source.HeightTiles,
                source.TileCells.Select(value => new RawCell(value.Coordinate.X, value.Coordinate.Y)),
                knownLegacyIds).ToArray();
            if (!source.Active) errors = errors.Concat(new[] { "INACTIVE_SOURCE" }).ToArray();
            if (errors.Length > 0) throw new InvalidOperationException(string.Join("|", errors));

            var cells = source.TileCells.OrderBy(value => value.Coordinate.RowMajorIndex)
                .Select(value => new CellView(value)).ToArray();
            var sockets = source.Sockets.OrderBy(value => value.SocketId, StringComparer.Ordinal)
                .Select(value => new SocketView(value)).ToArray();
            var canonical = new StringBuilder();
            canonical.Append(source.Id.Value).Append('|').Append(source.WidthTiles).Append('|')
                .Append(source.HeightTiles).Append('\n');
            foreach (var cell in cells)
                canonical.Append(cell.CoordinateKey).Append('|').Append(cell.Payload).Append('\n');
            foreach (var socket in sockets)
                canonical.Append(socket.Identity).Append('\n');
            return new View(source.Id.Value, source.WidthTiles, source.HeightTiles, cells, sockets,
                Sha256(Encoding.UTF8.GetBytes(canonical.ToString())));
        }

        public static IReadOnlyList<string> ValidateRaw(
            string id,
            int width,
            int height,
            IEnumerable<RawCell> sourceCells,
            IEnumerable<string> knownLegacyIds)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(id)) errors.Add("MISSING_ID");
            if (width != 12 || height != 8) errors.Add("INVALID_GEOMETRY");
            var cells = (sourceCells ?? Array.Empty<RawCell>()).ToArray();
            if (cells.Length != 96 || cells.Any(value => value == null ||
                                                       value.X < 0 || value.X >= 12 ||
                                                       value.Y < 0 || value.Y >= 8) ||
                cells.Where(value => value != null).Select(value => value.Y * 12 + value.X).Distinct().Count() != 96)
                errors.Add("INVALID_CELL_COVERAGE");
            if (!string.IsNullOrWhiteSpace(id) && !(knownLegacyIds ?? Array.Empty<string>())
                    .Contains(id, StringComparer.Ordinal))
                errors.Add("UNKNOWN_LEGACY_FK");
            return new ReadOnlyCollection<string>(errors.Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
        }
    }
}
