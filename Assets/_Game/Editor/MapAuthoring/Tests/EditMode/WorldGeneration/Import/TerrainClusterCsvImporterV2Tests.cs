using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;
using StarNight.MapAuthoring.WorldGeneration.Import;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.Import
{
    [TestFixture]
    [Category("MAP11_07")]
    public sealed class TerrainClusterCsvImporterV2Tests
    {
        private static readonly IReadOnlyDictionary<string, Spec> Expected =
            new[]
            {
                new Spec("MoonCrater", "TC_CRATER_QUIET_RIM", PacingRole.Quiet, 2, ClusterPortSide.L, ClusterPortSide.R),
                new Spec("MoonCrater", "TC_CRATER_BOWL_ASCENT", PacingRole.Traversal, 3, ClusterPortSide.L, ClusterPortSide.U),
                new Spec("MoonCrater", "TC_CRATER_BROKEN_SLOPE", PacingRole.Discovery, 4, ClusterPortSide.U, ClusterPortSide.D),
                new Spec("MoonCrater", "TC_CRATER_ROCK_SHELF_RECOVERY", PacingRole.Recovery, 5, ClusterPortSide.L, ClusterPortSide.D),
                new Spec("CassiaRoot", "TC_ROOT_QUIET_ARCH", PacingRole.Quiet, 2, ClusterPortSide.L, ClusterPortSide.R),
                new Spec("CassiaRoot", "TC_ROOT_HOLLOW_POCKET", PacingRole.Traversal, 3, ClusterPortSide.L, ClusterPortSide.U),
                new Spec("CassiaRoot", "TC_ROOT_VERTICAL_TUNNEL", PacingRole.Discovery, 4, ClusterPortSide.U, ClusterPortSide.D),
                new Spec("CassiaRoot", "TC_ROOT_FORKED_CANOPY_RECOVERY", PacingRole.Recovery, 5, ClusterPortSide.L, ClusterPortSide.D),
                new Spec("AbandonedMill", "TC_MILL_QUIET_BEAM", PacingRole.Quiet, 2, ClusterPortSide.L, ClusterPortSide.R),
                new Spec("AbandonedMill", "TC_MILL_BEAM_OVERHANG", PacingRole.Traversal, 3, ClusterPortSide.L, ClusterPortSide.U),
                new Spec("AbandonedMill", "TC_MILL_BROKEN_PILLAR", PacingRole.Discovery, 4, ClusterPortSide.U, ClusterPortSide.D),
                new Spec("AbandonedMill", "TC_MILL_ORTHOGONAL_SHAFT_RECOVERY", PacingRole.Recovery, 5, ClusterPortSide.L, ClusterPortSide.D),
                new Spec("MoonDough", "TC_DOUGH_QUIET_SHELF", PacingRole.Quiet, 2, ClusterPortSide.L, ClusterPortSide.R),
                new Spec("MoonDough", "TC_DOUGH_BOUNCE_CUP", PacingRole.Traversal, 3, ClusterPortSide.L, ClusterPortSide.U),
                new Spec("MoonDough", "TC_DOUGH_SOFT_POCKET", PacingRole.Discovery, 4, ClusterPortSide.U, ClusterPortSide.D),
                new Spec("MoonDough", "TC_DOUGH_STICKY_RISE_RECOVERY", PacingRole.Recovery, 5, ClusterPortSide.L, ClusterPortSide.D),
            }.ToDictionary(value => value.ClusterId, StringComparer.Ordinal);

        private static readonly IReadOnlyDictionary<string, int> ExpectedRows =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "terrain_cluster_catalog_v2.csv", 16 },
                { "terrain_cluster_cells_v2.csv", 56 },
                { "terrain_cluster_spine_edges_v2.csv", 200 },
                { "terrain_cluster_envelope_cells_v2.csv", 1200 },
                { "terrain_cluster_variants_v2.csv", 32 },
                { "terrain_cluster_role_anchors_v2.csv", 92 },
                { "terrain_cluster_role_variant_links_v2.csv", 184 },
                { "terrain_cluster_ports_v2.csv", 32 },
                { "terrain_cluster_nodes_v2.csv", 224 },
                { "terrain_cluster_high_routes_v2.csv", 16 },
                { "terrain_cluster_high_route_edges_v2.csv", 32 },
                { "terrain_cluster_high_route_benefits_v2.csv", 32 },
                { "terrain_cluster_high_route_failures_v2.csv", 16 },
            };

        private static readonly Lazy<ContentFixture> Fixture =
            new Lazy<ContentFixture>(BuildFixture);

        [Test]
        public void ThirteenPhysicalTablesMatchRegistryHeadersRowsMetasAndInventory()
        {
            var descriptors = V2AuthoringSchemaRegistry.DescribeDefaultTables()
                .Where(value => value.Owner == V2AuthoringOwner.TerrainCluster)
                .OrderBy(value => value.RelativeAuthoringPath, StringComparer.Ordinal).ToArray();
            Assert.That(descriptors, Has.Length.EqualTo(13));
            Assert.That(TerrainClusterCsvImporterV2.ProjectRelativePaths, Has.Count.EqualTo(13));
            foreach (var descriptor in descriptors)
            {
                var projectPath = TerrainClusterCsvImporterV2.AuthoringRootProjectRelativePath +
                                  descriptor.RelativeAuthoringPath;
                Assert.That(TerrainClusterCsvImporterV2.ProjectRelativePaths, Does.Contain(projectPath));
                var bytes = File.ReadAllBytes(FullPath(projectPath));
                Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }), projectPath);
                Assert.That(bytes, Has.None.EqualTo((byte)'\r'), projectPath);
                Assert.That(bytes.Last(), Is.EqualTo((byte)'\n'), projectPath);
                Assert.That(bytes[bytes.Length - 2], Is.Not.EqualTo((byte)'\n'), projectPath);
                var text = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
                var lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
                Assert.That(lines[0], Is.EqualTo(string.Join(",", descriptor.Columns
                    .OrderBy(value => value.ColumnOrder).Select(value => value.ColumnName))), projectPath);
                Assert.That(lines.Length - 2, Is.EqualTo(ExpectedRows[descriptor.FileName]), projectPath);
                Assert.That(File.Exists(FullPath(projectPath + ".meta")), Is.True, projectPath);
            }

            var authoringRoot = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var terrainRoot = FullPath(
                "Assets/_Game/Map/Data/WorldGeneration/Authoring/TerrainCluster");
            Assert.That(Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories),
                Has.Length.EqualTo(65));
            Assert.That(Directory.GetFiles(authoringRoot, "*.csv.meta", SearchOption.AllDirectories),
                Has.Length.EqualTo(65));
            Assert.That(Directory.GetFiles(terrainRoot, "*.csv"), Has.Length.EqualTo(13));
            Assert.That(Directory.GetFiles(terrainRoot, "*.csv.meta"), Has.Length.EqualTo(13));
            Assert.That(Directory.GetFiles(FullPath(
                "Assets/_Game/Map/Data/WorldGeneration/Generated"), "*.csv",
                SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void CatalogPublishesExactBiomePacingFootprintRolePortAndVariantMatrix()
        {
            var result = Import();
            Assert.That(result.Catalog.Entries, Has.Count.EqualTo(16));
            Assert.That(result.Catalog.Entries.Select(value => value.Id.Value),
                Is.EqualTo(Expected.Keys.OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(result.Catalog.Entries.GroupBy(value => value.Biome).All(value => value.Count() == 4),
                Is.True);
            Assert.That(result.Catalog.Entries.GroupBy(value => value.PacingRole).All(value => value.Count() == 4),
                Is.True);
            Assert.That(result.Catalog.Entries.Select(value => value.StructuralSignature).Distinct().Count(),
                Is.EqualTo(16));

            foreach (var entry in result.Catalog.Entries)
            {
                var spec = Expected[entry.Id.Value];
                Assert.That(entry.Biome.CanonicalId, Is.EqualTo(spec.Biome), entry.Id.Value);
                Assert.That(entry.PacingRole, Is.EqualTo(spec.Pacing), entry.Id.Value);
                Assert.That(entry.Contract.Footprint.ActiveChunks, Has.Count.EqualTo(spec.Chunks), entry.Id.Value);
                Assert.That(entry.Contract.Footprint.ActiveChunks.Min(value => value.X), Is.Zero, entry.Id.Value);
                Assert.That(entry.Contract.Footprint.ActiveChunks.Min(value => value.Y), Is.Zero, entry.Id.Value);
                Assert.That(entry.Contract.Footprint.ActiveChunks.Distinct().Count(), Is.EqualTo(spec.Chunks));
                Assert.That(entry.Contract.Traversal.Variants, Has.Count.EqualTo(2), entry.Id.Value);
                Assert.That(entry.Contract.Traversal.Variants.Count(value => value.IsBaseline), Is.EqualTo(1));
                Assert.That(entry.Contract.Traversal.Variants.Single(value => value.IsBaseline).Id,
                    Is.EqualTo(entry.BaselineVariantId));
                var portEntry = entry.Contract.Ports.Single(value =>
                    value.IsPrimary && value.Kind == ClusterPortKind.Entry);
                var portExit = entry.Contract.Ports.Single(value =>
                    value.IsPrimary && value.Kind == ClusterPortKind.Exit);
                Assert.That(portEntry.OutwardSide, Is.EqualTo(spec.EntrySide), entry.Id.Value);
                Assert.That(portExit.OutwardSide, Is.EqualTo(spec.ExitSide), entry.Id.Value);
                Assert.That(portEntry.CompatibleRouteTypes, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
                Assert.That(portExit.CompatibleRouteTypes, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
                Assert.That(entry.TryGetPortAccess(portEntry.PortId, out var entryAccess), Is.True);
                Assert.That(entry.TryGetPortAccess(portExit.PortId, out var exitAccess), Is.True);
                Assert.That(entryAccess, Is.EqualTo(AccessClass.MandatoryNoTool));
                Assert.That(exitAccess, Is.EqualTo(AccessClass.MandatoryNoTool));
                foreach (var required in new[]
                         {
                             ClusterRoleKind.Entry, ClusterRoleKind.BuildUp, ClusterRoleKind.Core,
                             ClusterRoleKind.Recovery, ClusterRoleKind.Exit,
                         })
                    Assert.That(entry.Contract.RoleAnchors.Any(value => value.Role == required), Is.True);
                Assert.That(entry.Contract.RoleAnchors.Count(value => value.Role == ClusterRoleKind.Reward),
                    Is.EqualTo(entry.PacingRole == PacingRole.Quiet ? 0 : 1), entry.Id.Value);
                Assert.That(entry.RouteIntent.HighRoutes, Has.Count.EqualTo(1), entry.Id.Value);
                Assert.That(entry.RouteIntent.HighRoutes[0].BenefitIds, Has.Count.EqualTo(2), entry.Id.Value);
                Assert.That(entry.RouteIntent.HighRoutes[0].FailureNodeIds, Has.Count.EqualTo(1), entry.Id.Value);
            }

            Assert.That(result.Catalog.Entries.GroupBy(value => value.Contract.Footprint.ActiveChunks.Count)
                .ToDictionary(value => value.Key, value => value.Count()),
                Is.EquivalentTo(new Dictionary<int, int> { { 2, 4 }, { 3, 4 }, { 4, 4 }, { 5, 4 } }));
        }

        [Test]
        public void ImportIsCultureStableImmutableAndSemanticDigestSensitive()
        {
            var canonical = Import();
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var cultureResult = Import();
                Assert.That(cultureResult.StableDigest, Is.EqualTo(canonical.StableDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }

            Assert.Throws<NotSupportedException>(() =>
                ((IList<TerrainClusterAuthoringEntry>)canonical.Catalog.Entries).Clear());
            var bytes = ReadAllBytes();
            var benefitPath = PathFor("terrain_cluster_high_route_benefits_v2.csv");
            bytes[benefitPath] = ReplaceText(bytes[benefitPath],
                "BENEFIT_CRATER_BOWL_ASCENT_FLOW",
                "BENEFIT_CRATER_BOWL_ASCENT_PACE");
            var changed = new TerrainClusterCsvImporterV2().ParseBytes(bytes);
            Assert.That(changed.Success, Is.True, Errors(changed));
            Assert.That(changed.StableDigest, Is.Not.EqualTo(canonical.StableDigest));
        }

        [Test]
        public void BomHeaderOrphanAndDuplicateFailuresPublishNothingAtomically()
        {
            var physical = ReadAllBytes();
            var catalogPath = PathFor("terrain_cluster_catalog_v2.csv");
            var cellsPath = PathFor("terrain_cluster_cells_v2.csv");
            physical[catalogPath] = physical[catalogPath].Skip(3).ToArray();
            physical[cellsPath] = ReplaceText(physical[cellsPath], "cluster_id,chunk_x", "cluster,chunk_x");
            var physicalFailure = new TerrainClusterCsvImporterV2().ParseBytes(physical);
            AssertAtomicFailure(physicalFailure);
            Assert.That(physicalFailure.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterCsvImportErrorCode.InvalidBom));
            Assert.That(physicalFailure.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterCsvImportErrorCode.HeaderMismatch));

            var orphan = ReadAllBytes();
            var failuresPath = PathFor("terrain_cluster_high_route_failures_v2.csv");
            orphan[failuresPath] = ReplaceText(orphan[failuresPath],
                "NODE_CRATER_BOWL_ASCENT_ALT_HIGH", "NODE_UNKNOWN_ORPHAN");
            var orphanFailure = new TerrainClusterCsvImporterV2().ParseBytes(orphan);
            AssertAtomicFailure(orphanFailure);
            Assert.That(orphanFailure.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterCsvImportErrorCode.AuthoringValidation));

            var duplicate = ReadAllBytes();
            var benefitsPath = PathFor("terrain_cluster_high_route_benefits_v2.csv");
            duplicate[benefitsPath] = DuplicateFirstDataRow(duplicate[benefitsPath]);
            var duplicateFailure = new TerrainClusterCsvImporterV2().ParseBytes(duplicate);
            AssertAtomicFailure(duplicateFailure);
        }

        [Test]
        public void AllSixteenCompileThroughFootprintRolesTraversalWitnessAndPatternFreeCanvas()
        {
            var fixture = Fixture.Value;
            Assert.That(fixture.Compiled, Has.Count.EqualTo(16));
            foreach (var compiled in fixture.Compiled)
            {
                var spec = Expected[compiled.Entry.Id.Value];
                Assert.That(compiled.Canvas.ChunkCells.Count(value =>
                    value.State == ClusterChunkMaskState.Active), Is.EqualTo(spec.Chunks));
                Assert.That(compiled.Canvas.TileCells.Count(value =>
                    value.State == ClusterChunkMaskState.Active), Is.EqualTo(spec.Chunks * 96));
                var coveredChunks = compiled.Witness.BaselineRoute.CompiledCoordinates.Select(coordinate =>
                {
                    Assert.That(compiled.Canvas.TryGetTileCell(coordinate, out var cell), Is.True);
                    return cell.OwningChunk;
                }).Distinct().ToArray();
                Assert.That(coveredChunks, Has.Length.EqualTo(spec.Chunks), compiled.Entry.Id.Value);
                Assert.That(compiled.Witness.HighRoutes, Has.Count.EqualTo(1), compiled.Entry.Id.Value);
                Assert.That(compiled.Witness.HighRoutes[0].BenefitIds, Has.Count.EqualTo(2));
                Assert.That(compiled.Witness.RecoveryRoutes, Has.Count.EqualTo(1));
                Assert.That(compiled.Witness.RecoveryRoutes[0].TotalEstimatedDurationMilliseconds,
                    Is.InRange(2000, 5000));
                Assert.That(compiled.Render.Placements, Is.Empty);
                Assert.That(compiled.Render.ApplicationPlans, Is.Empty);
                Assert.That(compiled.Render.RendererDeltaCoordinateCount, Is.Zero);
                Assert.That(compiled.Render.ProtectedWriteCount, Is.Zero);
                Assert.That(compiled.Render.ProtectedValueChangeCount, Is.Zero);
                Assert.That(compiled.Render.FullWorkingCanvasCoordinateCount, Is.EqualTo(spec.Chunks * 96));
                Assert.That(compiled.Render.InitialWorkingCanvas.CanonicalDigest,
                    Is.EqualTo(compiled.Render.FinalWorkingCanvas.CanonicalDigest));
            }
        }

        [Test]
        public void FourQuietClustersCompileOnePerBiomeAndEveryUseQueryHasOneZeroRngMatch()
        {
            var fixture = Fixture.Value;
            var profiles = fixture.Compiled.Where(value => value.Entry.PacingRole == PacingRole.Quiet)
                .Select(value => new TerrainClusterQuietBufferProfile(
                    "QBUF_" + value.Entry.Id.Value.Substring(3),
                    value.Entry.Biome,
                    new[]
                    {
                        TerrainClusterQuietBufferUse.BeforeLandmark,
                        TerrainClusterQuietBufferUse.AfterLandmark,
                        TerrainClusterQuietBufferUse.UnplacedSpace,
                    },
                    new[]
                    {
                        PacingRole.Quiet, PacingRole.Traversal, PacingRole.Recovery,
                        PacingRole.Safe, PacingRole.Flow,
                    },
                    new[] { AccessClass.MandatoryNoTool },
                    value.Canvas, value.Canvas.CanonicalDigest,
                    value.RoleSocket, value.RoleSocket.CanonicalDigest,
                    value.Traversal, value.Traversal.CanonicalDigest,
                    value.Witness, value.Witness.CanonicalDigest,
                    value.Render, value.Render.CanonicalDigest)).ToArray();
            var poolResult = TerrainClusterQuietBufferPoolCompiler.Compile(
                new TerrainClusterQuietBufferPoolCompileRequest(profiles));
            Assert.That(poolResult.IsSuccess, Is.True, QuietErrors(poolResult));
            Assert.That(poolResult.Candidates, Has.Count.EqualTo(4));
            Assert.That(poolResult.Candidates.Select(value => value.Biome).Distinct().Count(), Is.EqualTo(4));
            Assert.That(poolResult.Candidates.All(value =>
                value.ActiveChunkCount == 2 && value.RewardRoleCount == 0 &&
                value.MarkerCount == 0 && value.HazardCount == 0 &&
                value.ProtectedWriteCount == 0 && value.ProtectedValueChangeCount == 0), Is.True);
            Assert.That(poolResult.Candidates.All(value =>
                value.ChunkEvidence.All(chunk => chunk.SolidCount >= 1 && chunk.AirCount >= 1)), Is.True);

            foreach (var candidate in poolResult.Candidates)
            {
                foreach (var use in new[]
                         {
                             TerrainClusterQuietBufferUse.BeforeLandmark,
                             TerrainClusterQuietBufferUse.AfterLandmark,
                             TerrainClusterQuietBufferUse.UnplacedSpace,
                         })
                {
                    var query = TerrainClusterQuietBufferPoolCompiler.Query(
                        poolResult.Pool,
                        new TerrainClusterQuietBufferQuery(
                            candidate.Biome, use, ClusterPortSide.L, ClusterPortSide.R,
                            2, PacingRole.Quiet, AccessClass.MandatoryNoTool, 2,
                            poolResult.Pool.CanonicalDigest));
                    Assert.That(query.IsSuccess, Is.True, QuietErrors(query));
                    Assert.That(query.QueryResult.MatchCount, Is.EqualTo(1));
                    Assert.That(query.QueryResult.Matches[0].ClusterId, Is.EqualTo(candidate.ClusterId));
                    Assert.That(query.QueryResult.RngDrawCount, Is.Zero);
                    Assert.That(query.QueryResult.SelectionCount, Is.Zero);
                }
            }
        }

        [Test]
        public void ImporterAndRuntimeSourcesStayInsideApprovedNoRngNoGeneratedBoundary()
        {
            var sources = new[]
            {
                FullPath("Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import/TerrainClusterCsvImporterV2.cs"),
                FullPath("Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Authoring/TerrainClusterAuthoringRows.cs"),
                FullPath("Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Authoring/TerrainClusterAuthoringCatalog.cs"),
                FullPath("Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Authoring/TerrainClusterAuthoringValidation.cs"),
            };
            var runtime = string.Join("\n", sources.Skip(1).Select(File.ReadAllText));
            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "System.IO", "StageMapGenerator", "GridWorld",
                         "RoomTemplate", "RoomGridTransform", "TileMutationService",
                         "SectorRecipeResolver", "System.Random", "UnityEngine.Random",
                         "DeterministicRngStreamFactory", "Time.deltaTime", "Tilemap",
                     })
                Assert.That(runtime, Does.Not.Contain(forbidden), forbidden);
            var importer = File.ReadAllText(sources[0]);
            Assert.That(importer, Does.Not.Contain("SearchOption"));
            Assert.That(importer, Does.Not.Contain("GetFiles("));
            Assert.That(importer, Does.Not.Contain("Generated"));
            Assert.That(importer, Does.Not.Contain("File.Write"));
        }

        private static ContentFixture BuildFixture()
        {
            var import = Import();
            var micro = new MicroPatternCsvImporterV2().Import();
            Assert.That(micro.Success, Is.True, string.Join("\n", micro.Errors));
            var compiled = new List<CompiledEntry>();
            foreach (var entry in import.Catalog.Entries)
            {
                var validation = TerrainClusterContractValidator.Validate(entry.Contract);
                Assert.That(validation.IsValid, Is.True,
                    string.Join("\n", validation.Errors.Select(value => value.ToString())));
                var footprint = TerrainClusterFootprintCompiler.Compile(
                    new TerrainClusterFootprintCompileRequest(
                        entry.Contract, ClusterFootprintTransform.R0));
                Assert.That(footprint.IsSuccess, Is.True,
                    string.Join("\n", footprint.Errors.Select(value => value.ToString())));
                var canvas = footprint.LocalCanvas;
                var sourceEntry = entry.Contract.Ports.Single(value =>
                    value.IsPrimary && value.Kind == ClusterPortKind.Entry);
                var sourceExit = entry.Contract.Ports.Single(value =>
                    value.IsPrimary && value.Kind == ClusterPortKind.Exit);
                var socketEvidence = new[]
                {
                    new ClusterSectorSocketEvidence(
                        "SR_ENTRY_" + entry.Id.Value, "SOCKET_ENTRY_" + entry.Id.Value,
                        sourceEntry.OutwardSide, 2, true, ClusterPortKind.Entry),
                    new ClusterSectorSocketEvidence(
                        "SR_EXIT_" + entry.Id.Value, "SOCKET_EXIT_" + entry.Id.Value,
                        sourceExit.OutwardSide, 3, true, ClusterPortKind.Exit),
                };
                var role = TerrainClusterRoleSocketCompiler.Compile(
                    new TerrainClusterRoleSocketCompileRequest(
                        entry.Contract, validation.CanonicalDigest,
                        canvas, canvas.CanonicalDigest, socketEvidence));
                Assert.That(role.IsSuccess, Is.True,
                    string.Join("\n", role.Errors.Select(value => value.ToString())));
                var traversal = TerrainClusterTraversalCompiler.Compile(
                    new TerrainClusterTraversalCompileRequest(
                        entry.Contract, validation.CanonicalDigest,
                        canvas, canvas.CanonicalDigest,
                        role.Contract, role.CanonicalDigest));
                Assert.That(traversal.IsSuccess, Is.True,
                    string.Join("\n", traversal.Errors.Select(value => value.ToString())));
                var witness = TerrainClusterRouteWitnessCompiler.Compile(
                    new TerrainClusterRouteWitnessCompileRequest(
                        canvas, canvas.CanonicalDigest,
                        role.Contract, role.CanonicalDigest,
                        traversal.Compilation, traversal.CanonicalDigest,
                        entry.RouteIntent));
                Assert.That(witness.IsSuccess, Is.True,
                    string.Join("\n", witness.Errors.Select(value => value.ToString())));
                var render = TerrainClusterPatternRenderer.Render(
                    new TerrainClusterPatternRenderRequest(
                        canvas, canvas.CanonicalDigest,
                        traversal.Compilation, traversal.CanonicalDigest,
                        witness.Report, witness.CanonicalDigest,
                        micro.Catalog, micro.StableDigest,
                        Array.Empty<TerrainClusterPatternZoneCell>(),
                        Array.Empty<TerrainClusterPatternPlacementIntent>()));
                Assert.That(render.Success, Is.True,
                    string.Join("\n", render.Errors.Select(value => value.ToString())));
                compiled.Add(new CompiledEntry(
                    entry, canvas, role.Contract, traversal.Compilation,
                    witness.Report, render.Report));
            }
            return new ContentFixture(import, compiled);
        }

        private static TerrainClusterCsvImportResult Import()
        {
            var result = new TerrainClusterCsvImporterV2().Import();
            Assert.That(result.Success, Is.True, Errors(result));
            Assert.That(result.Published, Is.True);
            Assert.That(result.StableDigest, Has.Length.EqualTo(64));
            return result;
        }

        private static Dictionary<string, byte[]> ReadAllBytes()
        {
            return TerrainClusterCsvImporterV2.ProjectRelativePaths.ToDictionary(
                value => value,
                value => File.ReadAllBytes(FullPath(value)),
                StringComparer.Ordinal);
        }

        private static string PathFor(string fileName)
        {
            return TerrainClusterCsvImporterV2.TerrainClusterRootProjectRelativePath + fileName;
        }

        private static byte[] ReplaceText(byte[] source, string before, string after)
        {
            var text = Encoding.UTF8.GetString(source).TrimStart('\uFEFF');
            Assert.That(text, Does.Contain(before));
            return new UTF8Encoding(true).GetPreamble()
                .Concat(new UTF8Encoding(false).GetBytes(text.Replace(before, after))).ToArray();
        }

        private static byte[] DuplicateFirstDataRow(byte[] source)
        {
            var text = Encoding.UTF8.GetString(source).TrimStart('\uFEFF');
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            lines.Insert(2, lines[1]);
            return new UTF8Encoding(true).GetPreamble()
                .Concat(new UTF8Encoding(false).GetBytes(string.Join("\n", lines) + "\n")).ToArray();
        }

        private static void AssertAtomicFailure(TerrainClusterCsvImportResult result)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Published, Is.False);
            Assert.That(result.Catalog, Is.Null);
            Assert.That(result.StableDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterCsvImportErrorCode.AtomicPublishRejected));
            Assert.That(result.Errors, Is.Ordered);
        }

        private static string Errors(TerrainClusterCsvImportResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string QuietErrors(TerrainClusterQuietBufferResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private sealed class Spec
        {
            public Spec(
                string biome,
                string clusterId,
                PacingRole pacing,
                int chunks,
                ClusterPortSide entrySide,
                ClusterPortSide exitSide)
            {
                Biome = biome;
                ClusterId = clusterId;
                Pacing = pacing;
                Chunks = chunks;
                EntrySide = entrySide;
                ExitSide = exitSide;
            }

            public string Biome { get; }
            public string ClusterId { get; }
            public PacingRole Pacing { get; }
            public int Chunks { get; }
            public ClusterPortSide EntrySide { get; }
            public ClusterPortSide ExitSide { get; }
        }

        private sealed class ContentFixture
        {
            public ContentFixture(
                TerrainClusterCsvImportResult import,
                IReadOnlyList<CompiledEntry> compiled)
            {
                Import = import;
                Compiled = compiled;
            }

            public TerrainClusterCsvImportResult Import { get; }
            public IReadOnlyList<CompiledEntry> Compiled { get; }
        }

        private sealed class CompiledEntry
        {
            public CompiledEntry(
                TerrainClusterAuthoringEntry entry,
                TerrainClusterLocalCanvas canvas,
                TerrainClusterRoleSocketContract roleSocket,
                TerrainClusterTraversalCompilation traversal,
                TerrainClusterRouteWitnessReport witness,
                TerrainClusterPatternRenderReport render)
            {
                Entry = entry;
                Canvas = canvas;
                RoleSocket = roleSocket;
                Traversal = traversal;
                Witness = witness;
                Render = render;
            }

            public TerrainClusterAuthoringEntry Entry { get; }
            public TerrainClusterLocalCanvas Canvas { get; }
            public TerrainClusterRoleSocketContract RoleSocket { get; }
            public TerrainClusterTraversalCompilation Traversal { get; }
            public TerrainClusterRouteWitnessReport Witness { get; }
            public TerrainClusterPatternRenderReport Render { get; }
        }
    }
}
