using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;
using StarNight.MapAuthoring.WorldGeneration.Import;
using StarNight.MapAuthoring.WorldGeneration.TerrainClusters;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.TerrainClusters
{
    [TestFixture]
    [Category("MAP11_09")]
    public sealed class Map11ClusterPhaseExitTests
    {
        private const string ApprovedCatalogDigest =
            "9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7";
        private const string ApprovedSignatureSetDigest =
            "2884a639d9cef923e8b86a7fba2c0430cdfad2de11a63fd138d51dacdce13d8a";
        private const string ApprovedAuthoringManifest =
            "ff4761537986a4c9433775359d9b62ad806914ef30462a320c97b355126a5b6c";

        private static readonly string[] RepresentativeClusters =
        {
            "TC_CRATER_QUIET_RIM",
            "TC_ROOT_HOLLOW_POCKET",
            "TC_MILL_BROKEN_PILLAR",
            "TC_DOUGH_STICKY_RISE_RECOVERY",
        };

        private static readonly string[][] RepresentativePatterns =
        {
            new[] { "MP_CRATER_BOWL", "MP_CRATER_ROCK_SHELF" },
            new[] { "MP_ROOT_ARCH", "MP_ROOT_HOLLOW_POCKET" },
            new[] { "MP_MILL_BROKEN_PILLAR", "MP_MILL_ORTHOGONAL_CARVE" },
            new[] { "MP_DOUGH_BOUNCE_CUP", "MP_DOUGH_STICKY_SHELF" },
        };

        private static readonly Dictionary<string, ClusterChunkCoord[]> RecoveryFootprints =
            new Dictionary<string, ClusterChunkCoord[]>(StringComparer.Ordinal)
            {
                {
                    "TC_CRATER_ROCK_SHELF_RECOVERY",
                    new[]
                    {
                        new ClusterChunkCoord(0, 0), new ClusterChunkCoord(1, 0),
                        new ClusterChunkCoord(2, 0), new ClusterChunkCoord(2, 1),
                        new ClusterChunkCoord(3, 1),
                    }
                },
                {
                    "TC_ROOT_FORKED_CANOPY_RECOVERY",
                    new[]
                    {
                        new ClusterChunkCoord(0, 1), new ClusterChunkCoord(1, 0),
                        new ClusterChunkCoord(1, 1), new ClusterChunkCoord(1, 2),
                        new ClusterChunkCoord(2, 1),
                    }
                },
                {
                    "TC_MILL_ORTHOGONAL_SHAFT_RECOVERY",
                    new[]
                    {
                        new ClusterChunkCoord(0, 2), new ClusterChunkCoord(1, 0),
                        new ClusterChunkCoord(1, 1), new ClusterChunkCoord(1, 2),
                        new ClusterChunkCoord(2, 0),
                    }
                },
                {
                    "TC_DOUGH_STICKY_RISE_RECOVERY",
                    new[]
                    {
                        new ClusterChunkCoord(0, 0), new ClusterChunkCoord(0, 1),
                        new ClusterChunkCoord(1, 1), new ClusterChunkCoord(1, 2),
                        new ClusterChunkCoord(2, 2),
                    }
                },
            };

        private static readonly Lazy<ExitEvidence> Evidence =
            new Lazy<ExitEvidence>(BuildEvidence);

        [Test]
        public void PhysicalAuthorityImportsExactApprovedInventoryAndDigestsAtomically()
        {
            var evidence = Evidence.Value;
            var tables = V2AuthoringSchemaRegistry.DescribeDefaultTables().ToArray();
            var terrainTables = tables.Where(value => value.Owner == V2AuthoringOwner.TerrainCluster)
                .OrderBy(value => value.RelativeAuthoringPath, StringComparer.Ordinal).ToArray();
            var expectedPaths = terrainTables.Select(value =>
                    TerrainClusterCsvImporterV2.AuthoringRootProjectRelativePath +
                    value.RelativeAuthoringPath)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();

            Assert.That(tables, Has.Length.EqualTo(24));
            Assert.That(tables.Sum(value => value.Columns.Count), Is.EqualTo(143));
            Assert.That(tables.Sum(value => value.Columns.Count(column => column.ForeignKey != null)),
                Is.EqualTo(44));
            Assert.That(terrainTables, Has.Length.EqualTo(13));
            Assert.That(terrainTables.Sum(value => value.Columns.Count), Is.EqualTo(89));
            Assert.That(TerrainClusterCsvImporterV2.ProjectRelativePaths,
                Is.EqualTo(expectedPaths));

            foreach (var path in expectedPaths)
            {
                Assert.That(File.Exists(FullPath(path)), Is.True, path);
                Assert.That(File.Exists(FullPath(path + ".meta")), Is.True, path + ".meta");
            }

            var authoringRoot = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var terrainRoot = FullPath(
                "Assets/_Game/Map/Data/WorldGeneration/Authoring/TerrainCluster");
            var generatedRoot = FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated");
            var authoringCsv = Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories);
            Assert.That(authoringCsv, Has.Length.EqualTo(65));
            Assert.That(Directory.GetFiles(authoringRoot, "*.csv.meta", SearchOption.AllDirectories),
                Has.Length.EqualTo(65));
            Assert.That(Directory.GetFiles(terrainRoot, "*.csv"), Has.Length.EqualTo(13));
            Assert.That(Directory.GetFiles(terrainRoot, "*.csv.meta"), Has.Length.EqualTo(13));
            Assert.That(Directory.GetFiles(generatedRoot, "*.csv", SearchOption.AllDirectories), Is.Empty);

            Assert.That(evidence.Import.Success, Is.True, ImportErrors(evidence.Import));
            Assert.That(evidence.Import.Published, Is.True);
            Assert.That(evidence.Import.Errors, Is.Empty);
            Assert.That(evidence.Import.Catalog.Entries.Count, Is.EqualTo(16));
            Assert.That(evidence.Import.StableDigest, Is.EqualTo(ApprovedCatalogDigest));
            Assert.That(SignatureSetDigest(evidence.Import.Catalog.Entries),
                Is.EqualTo(ApprovedSignatureSetDigest));
            Assert.That(ComputeManifest(authoringRoot, authoringCsv),
                Is.EqualTo(ApprovedAuthoringManifest));
            Assert.That(evidence.Import.Catalog.Entries.Select(value => value.StructuralSignature)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(16));
        }

        [Test]
        public void AllSixteenByTwoVariantsAreRepeatReverseAndCultureDeterministicWithoutRngOrRetry()
        {
            var evidence = Evidence.Value;
            Assert.That(evidence.Compiled.Count, Is.EqualTo(16));
            Assert.That(evidence.PatternFree.Count, Is.EqualTo(32));
            Assert.That(evidence.Import.Catalog.Entries.Sum(value => value.Contract.Traversal.Variants.Count),
                Is.EqualTo(32));
            Assert.That(evidence.Import.Catalog.Entries.Sum(value =>
                value.Contract.Traversal.Variants.Count(variant => variant.IsBaseline)), Is.EqualTo(16));

            foreach (var compiled in evidence.Compiled)
            {
                var repeated = Compile(compiled.Entry, compiled.Entry.Contract,
                    compiled.Entry.RouteIntent, false);
                var reversedContract = ReverseEnumerated(compiled.Entry.Contract);
                var reversedIntent = ReverseEnumerated(compiled.Entry.RouteIntent);
                var reversed = Compile(compiled.Entry, reversedContract, reversedIntent, true);
                Assert.That(repeated.DigestChain, Is.EqualTo(compiled.DigestChain), compiled.Entry.Id.Value);
                Assert.That(reversed.DigestChain, Is.EqualTo(compiled.DigestChain), compiled.Entry.Id.Value);
            }

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var model = new TerrainClusterPreviewModel();
                foreach (var canonical in evidence.PatternFree)
                {
                    var result = model.Build(new TerrainClusterPreviewRequest(
                        canonical.ClusterId, canonical.VariantId, TerrainClusterPreviewMode.PatternFree));
                    Assert.That(result.Success, Is.True, PreviewErrors(result));
                    Assert.That(result.Snapshot.StableDigest, Is.EqualTo(canonical.StableDigest),
                        canonical.ClusterId + "/" + canonical.VariantId);
                    Assert.That(result.Snapshot.Density.SolidCount,
                        Is.EqualTo(canonical.Density.SolidCount));
                    Assert.That(result.Snapshot.Density.AirCount,
                        Is.EqualTo(canonical.Density.AirCount));
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            foreach (var requestType in CompilerRequestTypes())
            {
                var surface = requestType.GetConstructors().SelectMany(value => value.GetParameters())
                    .Select(value => (value.Name ?? string.Empty) + "|" + value.ParameterType.FullName)
                    .ToArray();
                Assert.That(surface.Any(value => ContainsAny(value, "rng", "random", "seed", "retry")),
                    Is.False, requestType.FullName);
            }
        }

        [Test]
        public void FootprintsCanvasesPortsAnchorsAndRecoveryShapesFitTheFixedSectorFrame()
        {
            var evidence = Evidence.Value;
            var distribution = evidence.Compiled.GroupBy(value =>
                    value.Entry.Contract.Footprint.ActiveChunks.Count)
                .ToDictionary(value => value.Key, value => value.Count());
            Assert.That(distribution,
                Is.EquivalentTo(new Dictionary<int, int> { { 2, 4 }, { 3, 4 }, { 4, 4 }, { 5, 4 } }));

            foreach (var compiled in evidence.Compiled)
            {
                var entry = compiled.Entry;
                var chunks = entry.Contract.Footprint.ActiveChunks;
                var validation = TerrainClusterContractValidator.Validate(entry.Contract);
                Assert.That(validation.IsValid, Is.True, ContractErrors(validation));
                Assert.That(chunks.Distinct().Count(), Is.EqualTo(chunks.Count), entry.Id.Value);
                Assert.That(compiled.Canvas.ChunkCells.Where(value =>
                        value.State == ClusterChunkMaskState.Active).Select(value => value.SourceCoordinate),
                    Is.EquivalentTo(chunks), entry.Id.Value);

                var width = chunks.Max(value => value.X) - chunks.Min(value => value.X) + 1;
                var height = chunks.Max(value => value.Y) - chunks.Min(value => value.Y) + 1;
                Assert.That(width, Is.LessThanOrEqualTo(4), entry.Id.Value);
                Assert.That(height, Is.LessThanOrEqualTo(4), entry.Id.Value);
                Assert.That(width * 12, Is.LessThanOrEqualTo(48), entry.Id.Value);
                Assert.That(height * 8, Is.LessThanOrEqualTo(32), entry.Id.Value);

                var activeTiles = compiled.Canvas.TileCells.Where(value =>
                    value.State == ClusterChunkMaskState.Active).ToArray();
                Assert.That(activeTiles, Has.Length.EqualTo(chunks.Count * 96), entry.Id.Value);
                Assert.That(activeTiles.Select(value => value.Coordinate).Distinct().Count(),
                    Is.EqualTo(activeTiles.Length), entry.Id.Value);
                foreach (var cell in activeTiles)
                {
                    Assert.That(compiled.Canvas.TryGetSourceTile(cell.Coordinate, out var source), Is.True);
                    Assert.That(compiled.Canvas.TryGetCompiledTile(source, out var roundTrip), Is.True);
                    Assert.That(roundTrip, Is.EqualTo(cell.Coordinate));
                }

                foreach (var coordinate in entry.Contract.RoleAnchors.Select(value => value.Tile)
                             .Concat(entry.Contract.Ports.Select(value => value.Tile)))
                {
                    Assert.That(compiled.Canvas.TryGetCompiledTile(coordinate, out var projected), Is.True,
                        entry.Id.Value + " " + coordinate);
                    Assert.That(compiled.Canvas.TryGetTileCell(projected, out var cell), Is.True);
                    Assert.That(cell.State, Is.EqualTo(ClusterChunkMaskState.Active));
                }

                foreach (var snapshot in evidence.PatternFree.Where(value => value.ClusterId == entry.Id.Value))
                {
                    Assert.That(snapshot.SectorFrame.GridColumnCount, Is.EqualTo(4));
                    Assert.That(snapshot.SectorFrame.GridRowCount, Is.EqualTo(4));
                    Assert.That(snapshot.Cells.Where(value => value.Active).All(value =>
                        snapshot.SectorFrame.Contains(value.FrameCoordinate)), Is.True);
                    Assert.That(snapshot.Cells.All(value => value.FrameCoordinate ==
                        snapshot.SectorFrame.Translate(value.LocalCoordinate)), Is.True);
                    Assert.That(snapshot.SectorFrame.ActiveCoordinates.Count,
                        Is.EqualTo(activeTiles.Length));
                }
            }

            foreach (var expected in RecoveryFootprints)
            {
                var actual = evidence.Import.Catalog.Entries.Single(value =>
                    value.Id.Value == expected.Key).Contract.Footprint.ActiveChunks;
                Assert.That(actual, Is.EqualTo(expected.Value.OrderBy(value => value)), expected.Key);
            }
        }

        [Test]
        public void SourceBackedBaselineHighAndRecoveryWitnessesRemainInsideTraversalEnvelope()
        {
            var evidence = Evidence.Value;
            var baselineEdges = 0;
            var highEdges = 0;
            var recoveryEdges = 0;
            var recoveryDurations = new List<int>();
            foreach (var compiled in evidence.Compiled)
            {
                Assert.That(compiled.Traversal.Variants.Count, Is.EqualTo(2), compiled.Entry.Id.Value);
                Assert.That(compiled.Traversal.Nodes.All(value =>
                    value.SourceGraphKind == TraversalGraphKind.Traversal), Is.True);
                Assert.That(compiled.Traversal.Edges.All(value =>
                    value.SourceGraphKind == TraversalGraphKind.Traversal), Is.True);

                var baseline = compiled.Witness.BaselineRoute;
                Assert.That(compiled.Traversal.TryGetVariant(baseline.VariantId, out var baselineVariant), Is.True);
                Assert.That(baselineVariant.IsBaseline, Is.True);
                Assert.That(baseline.EntryNodeId, Is.EqualTo(baseline.OrderedNodeIds.First()));
                Assert.That(baseline.ExitNodeId, Is.EqualTo(baseline.OrderedNodeIds.Last()));
                Assert.That(baseline.EntryPortId, Is.Not.Empty);
                Assert.That(baseline.ExitPortId, Is.Not.Empty);
                Assert.That(baseline.PreservedMandatoryRoles, Does.Contain(ClusterRoleKind.BuildUp));
                Assert.That(baseline.PreservedMandatoryRoles, Does.Contain(ClusterRoleKind.Core));
                Assert.That(baseline.PreservedMandatoryRoles, Does.Contain(ClusterRoleKind.Recovery));
                foreach (var witnessEdge in baseline.OrderedEdges)
                {
                    AssertSourceEdge(baselineVariant, witnessEdge);
                    baselineEdges++;
                }

                Assert.That(compiled.Witness.HighRoutes.Count, Is.EqualTo(1));
                foreach (var high in compiled.Witness.HighRoutes)
                {
                    Assert.That(compiled.Traversal.TryGetVariant(high.VariantId, out var highVariant), Is.True);
                    Assert.That(high.OrderedNodeIds, Does.Contain(high.BaseDivergenceNodeId));
                    Assert.That(high.OrderedNodeIds, Does.Contain(high.BaseRejoinNodeId));
                    Assert.That(high.OrderedNodeIds, Does.Contain(high.HighPointNodeId));
                    Assert.That(high.BenefitIds.Count, Is.GreaterThanOrEqualTo(2));
                    Assert.That(high.FailureNodeIds, Is.Not.Empty);
                    foreach (var witnessEdge in high.OrderedEdges)
                    {
                        AssertSourceEdge(highVariant, witnessEdge);
                        highEdges++;
                    }
                }

                Assert.That(compiled.Witness.RecoveryRoutes.Count, Is.EqualTo(1));
                foreach (var recovery in compiled.Witness.RecoveryRoutes)
                {
                    Assert.That(compiled.Traversal.TryGetVariant(
                        compiled.Witness.HighRoutes.Single(value =>
                            value.HighRouteId == recovery.HighRouteId).VariantId,
                        out var recoveryVariant), Is.True);
                    Assert.That(recovery.TotalEstimatedDurationMilliseconds, Is.InRange(2000, 5000));
                    Assert.That(baseline.OrderedNodeIds, Does.Contain(recovery.RejoinedBaselineNodeId));
                    Assert.That(recovery.CompiledCoordinates.All(value =>
                        compiled.Canvas.TryGetTileCell(value, out var cell) &&
                        cell.State == ClusterChunkMaskState.Active), Is.True);
                    foreach (var witnessEdge in recovery.OrderedEdges)
                    {
                        AssertSourceEdge(recoveryVariant, witnessEdge);
                        recoveryEdges++;
                    }
                    recoveryDurations.Add(recovery.TotalEstimatedDurationMilliseconds);
                }

                var protectedCoordinates = compiled.Traversal.ProtectedTiles
                    .Select(value => value.CompiledCoordinate).ToHashSet();
                Assert.That(baseline.CompiledCoordinates.All(protectedCoordinates.Contains), Is.True);
                Assert.That(compiled.Witness.HighRoutes.SelectMany(value => value.CoveredProtectedTiles)
                    .All(protectedCoordinates.Contains), Is.True);
                Assert.That(compiled.Witness.RecoveryRoutes.SelectMany(value => value.CoveredProtectedTiles)
                    .All(protectedCoordinates.Contains), Is.True);
            }
            TestContext.WriteLine("ROUTE_TOTALS baseline/high/recovery edges=" +
                                  baselineEdges + "/" + highEdges + "/" + recoveryEdges +
                                  " recovery_ms=" + recoveryDurations.Min() + ".." + recoveryDurations.Max());
        }

        [Test]
        public void StaticShellHasAbsentActivityAndEventInputsAndPatternsPreserveProtectedAuthority()
        {
            var evidence = Evidence.Value;
            foreach (var requestType in CompilerRequestTypes())
            {
                var surface = requestType.GetConstructors().SelectMany(value => value.GetParameters())
                    .Select(value => (value.Name ?? string.Empty) + "|" + value.ParameterType.FullName)
                    .ToArray();
                Assert.That(surface.Any(value => ContainsAny(value, "activity", "eventoverlay", "event_overlay")),
                    Is.False, requestType.FullName);
            }

            Assert.That(evidence.Compiled.All(value => value.Witness.StaticShell != null), Is.True);
            Assert.That(evidence.Compiled.All(value => value.Witness.PatternOperationCount == 0 &&
                value.Witness.BaselineRoute.PatternOperationCount == 0), Is.True);
            Assert.That(evidence.PatternFree.Count, Is.EqualTo(32));
            Assert.That(evidence.PatternFree.All(value => value.Pattern.IsPatternFree &&
                value.Pattern.TargetCount == 0 && value.Pattern.ChangedCount == 0 &&
                value.Pattern.ProtectedWriteCount == 0 &&
                value.Pattern.ProtectedValueChangeCount == 0), Is.True);
            Assert.That(evidence.PatternFree.All(value => value.RouteEvidence.Any(item =>
                item.StartsWith("BASE|", StringComparison.Ordinal)) && value.RouteEvidence.Any(item =>
                item.StartsWith("RECOVERY|", StringComparison.Ordinal))), Is.True);

            for (var index = 0; index < RepresentativeClusters.Length; index++)
            {
                var pair = evidence.PatternPairs[RepresentativeClusters[index]];
                Assert.That(pair[0].Pattern.PatternId, Is.EqualTo(RepresentativePatterns[index][0]));
                Assert.That(pair[1].Pattern.PatternId, Is.EqualTo(RepresentativePatterns[index][1]));
                var free = evidence.PatternFree.First(value =>
                    value.ClusterId == RepresentativeClusters[index] && value.IsBaselineVariant);
                foreach (var snapshot in pair)
                {
                    Assert.That(snapshot.Pattern.ChangedCount, Is.GreaterThan(0));
                    Assert.That(snapshot.Pattern.ProtectedWriteCount, Is.Zero);
                    Assert.That(snapshot.Pattern.ProtectedValueChangeCount, Is.Zero);
                    Assert.That(snapshot.Pattern.ChangedCoordinates.All(value =>
                        !snapshot.AbsoluteProtectedCoordinates.Contains(value)), Is.True);
                    Assert.That(snapshot.CanvasDigest, Is.EqualTo(free.CanvasDigest));
                    Assert.That(snapshot.RoleSocketDigest, Is.EqualTo(free.RoleSocketDigest));
                    Assert.That(snapshot.TraversalDigest, Is.EqualTo(free.TraversalDigest));
                    Assert.That(snapshot.RouteWitnessDigest, Is.EqualTo(free.RouteWitnessDigest));
                    Assert.That(snapshot.BaselineCoordinates, Is.EqualTo(free.BaselineCoordinates));
                    Assert.That(snapshot.HighRouteCoordinates, Is.EqualTo(free.HighRouteCoordinates));
                    Assert.That(snapshot.RecoveryCoordinates, Is.EqualTo(free.RecoveryCoordinates));
                    Assert.That(snapshot.AbsoluteProtectedCoordinates,
                        Is.EqualTo(free.AbsoluteProtectedCoordinates));
                }
            }
        }

        [Test]
        public void RawDensityQuietPoolAndPreviewStayUncalibratedDeterministicAndReadOnly()
        {
            var evidence = Evidence.Value;
            var authoringRoot = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var generatedRoot = FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated");
            var authoringCsv = Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories);
            var manifestBefore = ComputeManifest(authoringRoot, authoringCsv);
            var generatedBefore = Directory.GetFiles(generatedRoot, "*.csv", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var sceneBefore = EditorSceneManager.GetActiveScene();
            var dirtyBefore = sceneBefore.isDirty;
            var rootsBefore = sceneBefore.GetRootGameObjects().Length;

            var snapshots = evidence.PatternFree.Concat(
                evidence.PatternPairs.Values.SelectMany(value => value)).ToArray();
            foreach (var snapshot in snapshots)
            {
                var density = snapshot.Density;
                Assert.That(new[]
                {
                    density.ActiveCount, density.SolidCount, density.AirCount,
                    density.AbsoluteProtectedCount, density.PatternTargetCount,
                    density.PatternChangedCount,
                }, Has.All.GreaterThanOrEqualTo(0));
                Assert.That(density.SolidCount + density.AirCount, Is.EqualTo(density.ActiveCount));
                Assert.That(density.Chunks.Sum(value => value.ActiveCount),
                    Is.EqualTo(density.ActiveCount));
                Assert.That(density.Chunks.Sum(value => value.SolidCount),
                    Is.EqualTo(density.SolidCount));
                Assert.That(snapshot.Pattern.ChangedCoordinates.All(value =>
                    snapshot.Cells.Any(cell => cell.Active && cell.LocalCoordinate == value)), Is.True);
                Assert.That(snapshot.Pattern.ProtectedValueChangeCount, Is.Zero);
            }
            Assert.That(evidence.PatternPairs.Values.SelectMany(value => value)
                .All(value => value.Density.PatternChangedCount > 0), Is.True);

            var quietProfiles = evidence.Compiled.Where(value => value.Entry.PacingRole == PacingRole.Quiet)
                .Select(QuietProfile).ToArray();
            var pool = TerrainClusterQuietBufferPoolCompiler.Compile(
                new TerrainClusterQuietBufferPoolCompileRequest(quietProfiles));
            Assert.That(pool.IsSuccess, Is.True, QuietErrors(pool));
            Assert.That(pool.Candidates.Count, Is.EqualTo(4));
            Assert.That(pool.Candidates.Select(value => value.Biome).Distinct().Count(), Is.EqualTo(4));
            Assert.That(pool.Candidates.All(value => value.RewardRoleCount == 0 &&
                value.MarkerCount == 0 && value.HazardCount == 0 &&
                value.ProtectedWriteCount == 0 && value.ProtectedValueChangeCount == 0), Is.True);
            foreach (var candidate in pool.Candidates)
            foreach (var use in new[]
                     {
                         TerrainClusterQuietBufferUse.BeforeLandmark,
                         TerrainClusterQuietBufferUse.AfterLandmark,
                         TerrainClusterQuietBufferUse.UnplacedSpace,
                     })
            {
                var query = TerrainClusterQuietBufferPoolCompiler.Query(pool.Pool,
                    new TerrainClusterQuietBufferQuery(candidate.Biome,
                        use,
                        candidate.EntrySide, candidate.ExitSide,
                        candidate.CompatibleRouteTypes.First(), PacingRole.Quiet,
                        AccessClass.MandatoryNoTool, candidate.ActiveChunkCount,
                        pool.Pool.CanonicalDigest));
                Assert.That(query.IsSuccess, Is.True, QuietErrors(query));
                Assert.That(query.QueryResult.MatchCount, Is.EqualTo(1));
                Assert.That(query.QueryResult.Matches.Single().ClusterId, Is.EqualTo(candidate.ClusterId));
                Assert.That(query.QueryResult.RngDrawCount, Is.Zero);
                Assert.That(query.QueryResult.SelectionCount, Is.Zero);
            }

            foreach (var snapshot in evidence.PatternFree)
            {
                var compiled = evidence.Compiled.Single(value =>
                    value.Entry.Id.Value == snapshot.ClusterId);
                Assert.That(snapshot.CatalogDigest, Is.EqualTo(ApprovedCatalogDigest));
                Assert.That(snapshot.StructuralSignature, Is.EqualTo(compiled.Entry.StructuralSignature));
                Assert.That(snapshot.ContractDigest, Is.EqualTo(compiled.ValidationDigest));
                Assert.That(snapshot.CanvasDigest, Is.EqualTo(compiled.Canvas.CanonicalDigest));
                Assert.That(snapshot.RoleSocketDigest, Is.EqualTo(compiled.RoleSocket.CanonicalDigest));
                Assert.That(snapshot.TraversalDigest, Is.EqualTo(compiled.Traversal.CanonicalDigest));
                Assert.That(snapshot.RouteWitnessDigest, Is.EqualTo(compiled.Witness.CanonicalDigest));
            }

            var rebuilt = new TerrainClusterPreviewModel().Build(new TerrainClusterPreviewRequest(
                evidence.PatternFree[0].ClusterId, evidence.PatternFree[0].VariantId,
                TerrainClusterPreviewMode.PatternFree));
            Assert.That(rebuilt.Success, Is.True, PreviewErrors(rebuilt));
            Assert.That(rebuilt.Snapshot.StableDigest, Is.EqualTo(evidence.PatternFree[0].StableDigest));
            Assert.That(ComputeManifest(authoringRoot,
                    Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories)),
                Is.EqualTo(manifestBefore));
            Assert.That(Directory.GetFiles(generatedRoot, "*.csv", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal), Is.EqualTo(generatedBefore));
            Assert.That(EditorSceneManager.GetActiveScene().isDirty, Is.EqualTo(dirtyBefore));
            Assert.That(EditorSceneManager.GetActiveScene().GetRootGameObjects(),
                Has.Length.EqualTo(rootsBefore));

            TestContext.WriteLine("DENSITY_UNCALIBRATED active=" +
                                  snapshots.Min(value => value.Density.ActiveCount) + ".." +
                                  snapshots.Max(value => value.Density.ActiveCount) +
                                  " solid=" + snapshots.Min(value => value.Density.SolidCount) + ".." +
                                  snapshots.Max(value => value.Density.SolidCount) +
                                  " air=" + snapshots.Min(value => value.Density.AirCount) + ".." +
                                  snapshots.Max(value => value.Density.AirCount) +
                                  " changed=" + snapshots.Min(value => value.Density.PatternChangedCount) + ".." +
                                  snapshots.Max(value => value.Density.PatternChangedCount));
        }

        [Test]
        public void InMemoryDuplicateOversizeMissingEdgeAndProtectedWriteFixturesFailAtomically()
        {
            var catalogPath = PathFor("terrain_cluster_catalog_v2.csv");
            var cellsPath = PathFor("terrain_cluster_cells_v2.csv");

            var duplicateId = ReadAllPhysicalBytes();
            duplicateId[catalogPath] = DuplicateFirstDataRow(duplicateId[catalogPath]);
            AssertAtomicImportFailure(new TerrainClusterCsvImporterV2().ParseBytes(duplicateId));

            var duplicateCoordinate = ReadAllPhysicalBytes();
            duplicateCoordinate[cellsPath] = DuplicateFirstDataRow(duplicateCoordinate[cellsPath]);
            AssertAtomicImportFailure(new TerrainClusterCsvImporterV2().ParseBytes(duplicateCoordinate));

            foreach (var invalid in new[]
                     {
                         new { Bounds = "5x1", Chunks = Enumerable.Range(0, 5)
                             .Select(value => new ClusterChunkCoord(value, 0)).ToArray() },
                         new { Bounds = "1x5", Chunks = Enumerable.Range(0, 5)
                             .Select(value => new ClusterChunkCoord(0, value)).ToArray() },
                     })
            {
                var physical = ReadAllPhysicalBytes();
                physical[cellsPath] = ReplaceClusterCells(physical[cellsPath],
                    "TC_CRATER_ROCK_SHELF_RECOVERY", invalid.Chunks);
                var result = new TerrainClusterCsvImporterV2().ParseBytes(physical);
                AssertAtomicImportFailure(result);
                Assert.That(result.Errors.Any(value =>
                    value.Code == TerrainClusterCsvImportErrorCode.AuthoringValidation &&
                    value.Detail.Contains("footprint bounds observed " + invalid.Bounds + " chunks; allowed 4x4.")),
                    Is.True, ImportErrors(result));
            }

            var compiled = Evidence.Value.Compiled[0];
            var high = compiled.Entry.RouteIntent.HighRoutes.Single();
            var invalidEdges = high.OrderedEdgeIds.ToArray();
            invalidEdges[0] = "EDGE_MISSING_MAP11_09";
            var invalidHigh = new TerrainClusterHighRouteDefinition(
                high.HighRouteId, high.VariantId, high.BaseDivergenceNodeId, invalidEdges,
                high.BaseRejoinNodeId, high.HighPointNodeId, high.BenefitIds, high.FailureNodeIds);
            var invalidIntent = new TerrainClusterRouteWitnessIntent(
                compiled.Entry.RouteIntent.BaselineVariantId, new[] { invalidHigh },
                compiled.Entry.RouteIntent.EdgeDurationEvidence);
            var missingEdge = TerrainClusterRouteWitnessCompiler.Compile(
                new TerrainClusterRouteWitnessCompileRequest(
                    compiled.Canvas, compiled.Canvas.CanonicalDigest,
                    compiled.RoleSocket, compiled.RoleSocket.CanonicalDigest,
                    compiled.Traversal, compiled.Traversal.CanonicalDigest, invalidIntent));
            Assert.That(missingEdge.IsSuccess, Is.False);
            Assert.That(missingEdge.Report, Is.Null);
            Assert.That(missingEdge.CanonicalDigest, Is.Empty);
            Assert.That(missingEdge.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterRouteWitnessCompileErrorCode.InvalidHighRoutePath));

            var forceNoChange = RenderProtectedForceNoChange(
                Evidence.Value.Compiled.First(value => value.Entry.Biome == MoonpalaceBiomeId.MoonCrater));
            Assert.That(forceNoChange.Success, Is.True, PatternErrors(forceNoChange));
            Assert.That(forceNoChange.ApplicationPlans.Single().ProtectedHits, Is.Not.Empty);
            Assert.That(forceNoChange.Report.ProtectedWriteCount, Is.Zero);
            Assert.That(forceNoChange.Report.ProtectedValueChangeCount, Is.Zero);
            Assert.That(forceNoChange.RenderDelta.Writes, Is.Empty);
        }

        private static ExitEvidence BuildEvidence()
        {
            var import = new TerrainClusterCsvImporterV2().Import();
            Assert.That(import.Success, Is.True, ImportErrors(import));
            var micro = new MicroPatternCsvImporterV2().Import();
            Assert.That(micro.Success, Is.True,
                string.Join("\n", micro.Errors.Select(value => value.ToString())));
            var compiled = import.Catalog.Entries.Select(entry =>
                Compile(entry, entry.Contract, entry.RouteIntent, false)).ToArray();

            var model = new TerrainClusterPreviewModel();
            var free = new List<TerrainClusterPreviewSnapshot>();
            foreach (var entry in import.Catalog.Entries)
            foreach (var variant in entry.Contract.Traversal.Variants)
            {
                var result = model.Build(new TerrainClusterPreviewRequest(
                    entry.Id.Value, variant.Id.Value, TerrainClusterPreviewMode.PatternFree));
                Assert.That(result.Success, Is.True, PreviewErrors(result));
                free.Add(result.Snapshot);
            }

            var pairs = new Dictionary<string, TerrainClusterPreviewSnapshot[]>(StringComparer.Ordinal);
            foreach (var clusterId in RepresentativeClusters)
            {
                var a = model.Build(new TerrainClusterPreviewRequest(
                    clusterId, string.Empty, TerrainClusterPreviewMode.PatternA));
                var b = model.Build(new TerrainClusterPreviewRequest(
                    clusterId, string.Empty, TerrainClusterPreviewMode.PatternB));
                Assert.That(a.Success, Is.True, PreviewErrors(a));
                Assert.That(b.Success, Is.True, PreviewErrors(b));
                pairs.Add(clusterId, new[] { a.Snapshot, b.Snapshot });
            }
            return new ExitEvidence(import, compiled, free, pairs);
        }

        private static CompiledEntry Compile(
            TerrainClusterAuthoringEntry entry,
            TerrainClusterContract contract,
            TerrainClusterRouteWitnessIntent intent,
            bool reverseSocketEvidence)
        {
            var validation = TerrainClusterContractValidator.Validate(contract);
            Assert.That(validation.IsValid, Is.True, ContractErrors(validation));
            var footprint = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(contract, ClusterFootprintTransform.R0));
            Assert.That(footprint.IsSuccess, Is.True, FootprintErrors(footprint));
            var canvas = footprint.LocalCanvas;
            var sourceEntry = contract.Ports.Single(value =>
                value.IsPrimary && value.Kind == ClusterPortKind.Entry);
            var sourceExit = contract.Ports.Single(value =>
                value.IsPrimary && value.Kind == ClusterPortKind.Exit);
            var sockets = new[]
            {
                new ClusterSectorSocketEvidence("SR_ENTRY_" + contract.Id.Value,
                    "SOCKET_ENTRY_" + contract.Id.Value, sourceEntry.OutwardSide,
                    2, true, ClusterPortKind.Entry),
                new ClusterSectorSocketEvidence("SR_EXIT_" + contract.Id.Value,
                    "SOCKET_EXIT_" + contract.Id.Value, sourceExit.OutwardSide,
                    3, true, ClusterPortKind.Exit),
            };
            var role = TerrainClusterRoleSocketCompiler.Compile(
                new TerrainClusterRoleSocketCompileRequest(contract, validation.CanonicalDigest,
                    canvas, canvas.CanonicalDigest,
                    reverseSocketEvidence ? sockets.Reverse() : sockets));
            Assert.That(role.IsSuccess, Is.True, RoleErrors(role));
            var traversal = TerrainClusterTraversalCompiler.Compile(
                new TerrainClusterTraversalCompileRequest(contract, validation.CanonicalDigest,
                    canvas, canvas.CanonicalDigest, role.Contract, role.CanonicalDigest));
            Assert.That(traversal.IsSuccess, Is.True, TraversalErrors(traversal));
            var witness = TerrainClusterRouteWitnessCompiler.Compile(
                new TerrainClusterRouteWitnessCompileRequest(canvas, canvas.CanonicalDigest,
                    role.Contract, role.CanonicalDigest, traversal.Compilation,
                    traversal.CanonicalDigest, intent));
            Assert.That(witness.IsSuccess, Is.True, WitnessErrors(witness));
            var micro = new MicroPatternCsvImporterV2().Import();
            Assert.That(micro.Success, Is.True,
                string.Join("\n", micro.Errors.Select(value => value.ToString())));
            var render = TerrainClusterPatternRenderer.Render(
                new TerrainClusterPatternRenderRequest(canvas, canvas.CanonicalDigest,
                    traversal.Compilation, traversal.CanonicalDigest,
                    witness.Report, witness.CanonicalDigest,
                    micro.Catalog, micro.StableDigest,
                    Array.Empty<TerrainClusterPatternZoneCell>(),
                    Array.Empty<TerrainClusterPatternPlacementIntent>()));
            Assert.That(render.Success, Is.True, PatternErrors(render));
            return new CompiledEntry(entry, validation.CanonicalDigest, canvas,
                role.Contract, traversal.Compilation, witness.Report, render.Report);
        }

        private static TerrainClusterContract ReverseEnumerated(TerrainClusterContract source)
        {
            var variants = source.Traversal.Variants.Reverse().Select(variant =>
                new SpineVariant(variant.Id, variant.IsBaseline, variant.GraphKind,
                    variant.Nodes.Reverse(), variant.Edges.Reverse()));
            return new TerrainClusterContract(source.Id,
                new ClusterFootprint(source.Footprint.ActiveChunks.Reverse()),
                source.RoleAnchors.Reverse(), source.Ports.Reverse(),
                new TerrainClusterTraversalContract(variants), source.DisplayText);
        }

        private static TerrainClusterRouteWitnessIntent ReverseEnumerated(
            TerrainClusterRouteWitnessIntent source)
        {
            return new TerrainClusterRouteWitnessIntent(source.BaselineVariantId,
                source.HighRoutes.Reverse(), source.EdgeDurationEvidence.Reverse());
        }

        private static void AssertSourceEdge(
            CompiledClusterSpineVariant variant,
            TerrainClusterRouteWitnessEdge witness)
        {
            Assert.That(variant.TryGetEdge(witness.EdgeId, out var source), Is.True,
                witness.VariantId + "/" + witness.EdgeId);
            Assert.That(source.FromNodeId, Is.EqualTo(witness.FromNodeId));
            Assert.That(source.ToNodeId, Is.EqualTo(witness.ToNodeId));
            Assert.That(source.MovementKind, Is.EqualTo(witness.MovementKind));
            Assert.That(source.CompiledStartCoordinate, Is.EqualTo(witness.CompiledStartCoordinate));
            Assert.That(source.CompiledEndCoordinate, Is.EqualTo(witness.CompiledEndCoordinate));
            Assert.That(source.MinimumClearanceWidth, Is.GreaterThan(0));
            Assert.That(source.MinimumClearanceHeight, Is.GreaterThan(0));
            Assert.That(source.Envelope.Clearance, Is.Not.Empty);
            Assert.That(source.Envelope.Landing, Is.Not.Empty);
            Assert.That(source.Envelope.AllTiles, Is.Not.Empty);
            Assert.That(witness.EstimatedDurationMilliseconds, Is.GreaterThan(0));
        }

        private static IEnumerable<Type> CompilerRequestTypes()
        {
            return new[]
            {
                typeof(TerrainClusterFootprintCompileRequest),
                typeof(TerrainClusterRoleSocketCompileRequest),
                typeof(TerrainClusterTraversalCompileRequest),
                typeof(TerrainClusterRouteWitnessCompileRequest),
                typeof(TerrainClusterPatternRenderRequest),
                typeof(TerrainClusterPreviewRequest),
            };
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            return tokens.Any(token => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static TerrainClusterQuietBufferProfile QuietProfile(CompiledEntry source)
        {
            return new TerrainClusterQuietBufferProfile(
                "QBUF_" + source.Entry.Id.Value.Substring(3), source.Entry.Biome,
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
                source.Canvas, source.Canvas.CanonicalDigest,
                source.RoleSocket, source.RoleSocket.CanonicalDigest,
                source.Traversal, source.Traversal.CanonicalDigest,
                source.Witness, source.Witness.CanonicalDigest,
                source.Render, source.Render.CanonicalDigest);
        }

        private static TerrainClusterPatternRenderResult RenderProtectedForceNoChange(
            CompiledEntry source)
        {
            var active = source.Canvas.TileCells.Where(value => value.State == ClusterChunkMaskState.Active)
                .Select(value => value.Coordinate).ToHashSet();
            var protectedCoordinates = source.Traversal.ProtectedTiles
                .Select(value => value.CompiledCoordinate).ToHashSet();
            LocalTileCoord origin = default(LocalTileCoord);
            LocalTileCoord protectedCoordinate = default(LocalTileCoord);
            var found = false;
            for (var y = 0; y <= source.Canvas.TileHeight - 4 && !found; y++)
            for (var x = 0; x <= source.Canvas.TileWidth - 4 && !found; x++)
            {
                var candidate = new LocalTileCoord(x, y);
                var footprint = PatternFootprint(candidate);
                if (footprint.All(active.Contains) && footprint.Any(protectedCoordinates.Contains))
                {
                    origin = candidate;
                    protectedCoordinate = footprint.First(protectedCoordinates.Contains);
                    found = true;
                }
            }
            Assert.That(found, Is.True);
            var local = new LocalTileCoord(
                protectedCoordinate.X - origin.X, protectedCoordinate.Y - origin.Y);
            var catalogRow = new MicroPatternCatalogRowV2(
                "MP_MAP11_09_FORCE_NO_CHANGE", "1", source.Entry.Biome.CanonicalId,
                "R0", "FORCE_NO_CHANGE", "catalog.csv", 2);
            var cells = new List<MicroPatternCellRowV2>();
            var row = 2;
            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
            {
                var operation = x == local.X && y == local.Y ? "ADD_SOLID" : "NO_CHANGE";
                cells.Add(new MicroPatternCellRowV2(catalogRow.PatternId, Number(x), Number(y),
                    operation, "GEOMETRY", string.Empty, "cells.csv", row++));
            }
            var built = new MicroPatternCellSchemaBuilder().Build(new[] { catalogRow }, cells);
            Assert.That(built.Success, Is.True,
                string.Join("\n", built.Errors.Select(value => value.ToString())));
            Assert.That(built.Catalog.TryGetDefinition(
                new MicroPatternId(catalogRow.PatternId), out var definition), Is.True);
            var intent = new TerrainClusterPatternPlacementIntent(
                "TCP_MAP11_09_FORCE_NO_CHANGE", definition.Id, MicroPatternTransform.R0,
                origin, definition.ComputeStableDigest());
            return TerrainClusterPatternRenderer.Render(
                new TerrainClusterPatternRenderRequest(source.Canvas, source.Canvas.CanonicalDigest,
                    source.Traversal, source.Traversal.CanonicalDigest,
                    source.Witness, source.Witness.CanonicalDigest,
                    built.Catalog, built.StableDigest,
                    Array.Empty<TerrainClusterPatternZoneCell>(), new[] { intent }));
        }

        private static LocalTileCoord[] PatternFootprint(LocalTileCoord origin)
        {
            return Enumerable.Range(0, 4).SelectMany(y => Enumerable.Range(0, 4)
                .Select(x => new LocalTileCoord(origin.X + x, origin.Y + y))).ToArray();
        }

        private static Dictionary<string, byte[]> ReadAllPhysicalBytes()
        {
            return TerrainClusterCsvImporterV2.ProjectRelativePaths.ToDictionary(
                value => value, value => File.ReadAllBytes(FullPath(value)), StringComparer.Ordinal);
        }

        private static string PathFor(string fileName)
        {
            return TerrainClusterCsvImporterV2.TerrainClusterRootProjectRelativePath + fileName;
        }

        private static byte[] DuplicateFirstDataRow(byte[] source)
        {
            var text = Encoding.UTF8.GetString(source).TrimStart('\uFEFF');
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            lines.Insert(2, lines[1]);
            return new UTF8Encoding(true).GetPreamble()
                .Concat(new UTF8Encoding(false).GetBytes(string.Join("\n", lines) + "\n")).ToArray();
        }

        private static byte[] ReplaceClusterCells(
            byte[] source,
            string clusterId,
            IReadOnlyList<ClusterChunkCoord> chunks)
        {
            var text = Encoding.UTF8.GetString(source).TrimStart('\uFEFF');
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            var indexes = lines.Select((line, index) => new { Line = line, Index = index })
                .Where(value => value.Line.StartsWith(clusterId + ",", StringComparison.Ordinal))
                .Select(value => value.Index).ToArray();
            Assert.That(indexes, Has.Length.EqualTo(chunks.Count));
            for (var index = 0; index < indexes.Length; index++)
                lines[indexes[index]] = clusterId + "," + chunks[index].X + "," + chunks[index].Y + ",,,,,";
            return new UTF8Encoding(true).GetPreamble()
                .Concat(new UTF8Encoding(false).GetBytes(string.Join("\n", lines))).ToArray();
        }

        private static void AssertAtomicImportFailure(TerrainClusterCsvImportResult result)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Published, Is.False);
            Assert.That(result.Catalog, Is.Null);
            Assert.That(result.StableDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterCsvImportErrorCode.AtomicPublishRejected));
        }

        private static string SignatureSetDigest(
            IEnumerable<TerrainClusterAuthoringEntry> entries)
        {
            var material = string.Join("\n", entries.OrderBy(value => value.Id)
                .Select(value => value.Id.Value + "\t" + value.StructuralSignature));
            return Sha256(new UTF8Encoding(false).GetBytes(material));
        }

        private static string ComputeManifest(string root, IEnumerable<string> paths)
        {
            var noBom = new UTF8Encoding(false);
            var withBom = new UTF8Encoding(true);
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
                    return value.Relative + "\t" + Sha256(
                        withBom.GetPreamble().Concat(noBom.GetBytes(normalized)).ToArray());
                });
            return Sha256(noBom.GetBytes(string.Join("\n", records)));
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(bytes)
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string ImportErrors(TerrainClusterCsvImportResult value) =>
            string.Join("\n", value.Errors.Select(error => error.ToString()));
        private static string PreviewErrors(TerrainClusterPreviewBuildResult value) =>
            string.Join("\n", value.Errors.Select(error => error.ToString()));
        private static string ContractErrors(TerrainClusterValidationResult value) =>
            string.Join("\n", value.Errors.Select(error => error.ToString()));
        private static string FootprintErrors(TerrainClusterFootprintCompileResult value) =>
            string.Join("\n", value.Errors.Select(error => error.ToString()));
        private static string RoleErrors(TerrainClusterRoleSocketCompileResult value) =>
            string.Join("\n", value.Errors.Select(error => error.ToString()));
        private static string TraversalErrors(TerrainClusterTraversalCompileResult value) =>
            string.Join("\n", value.Errors.Select(error => error.ToString()));
        private static string WitnessErrors(TerrainClusterRouteWitnessCompileResult value) =>
            string.Join("\n", value.Errors.Select(error => error.ToString()));
        private static string PatternErrors(TerrainClusterPatternRenderResult value) =>
            string.Join("\n", value.Errors.Select(error => error.ToString()));
        private static string QuietErrors(TerrainClusterQuietBufferResult value) =>
            string.Join("\n", value.Errors.Select(error => error.ToString()));

        private sealed class ExitEvidence
        {
            public ExitEvidence(
                TerrainClusterCsvImportResult import,
                IReadOnlyList<CompiledEntry> compiled,
                IReadOnlyList<TerrainClusterPreviewSnapshot> patternFree,
                IReadOnlyDictionary<string, TerrainClusterPreviewSnapshot[]> patternPairs)
            {
                Import = import;
                Compiled = compiled;
                PatternFree = patternFree;
                PatternPairs = patternPairs;
            }

            public TerrainClusterCsvImportResult Import { get; }
            public IReadOnlyList<CompiledEntry> Compiled { get; }
            public IReadOnlyList<TerrainClusterPreviewSnapshot> PatternFree { get; }
            public IReadOnlyDictionary<string, TerrainClusterPreviewSnapshot[]> PatternPairs { get; }
        }

        private sealed class CompiledEntry
        {
            public CompiledEntry(
                TerrainClusterAuthoringEntry entry,
                string validationDigest,
                TerrainClusterLocalCanvas canvas,
                TerrainClusterRoleSocketContract roleSocket,
                TerrainClusterTraversalCompilation traversal,
                TerrainClusterRouteWitnessReport witness,
                TerrainClusterPatternRenderReport render)
            {
                Entry = entry;
                ValidationDigest = validationDigest;
                Canvas = canvas;
                RoleSocket = roleSocket;
                Traversal = traversal;
                Witness = witness;
                Render = render;
            }

            public TerrainClusterAuthoringEntry Entry { get; }
            public string ValidationDigest { get; }
            public TerrainClusterLocalCanvas Canvas { get; }
            public TerrainClusterRoleSocketContract RoleSocket { get; }
            public TerrainClusterTraversalCompilation Traversal { get; }
            public TerrainClusterRouteWitnessReport Witness { get; }
            public TerrainClusterPatternRenderReport Render { get; }
            public string DigestChain => string.Join("|", ValidationDigest, Canvas.CanonicalDigest,
                RoleSocket.CanonicalDigest, Traversal.CanonicalDigest,
                Witness.CanonicalDigest, Render.CanonicalDigest);
        }
    }
}
