using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.TerrainClusters
{
    [TestFixture]
    [Category("MAP11_01")]
    public sealed class TerrainClusterFootprintCompilerTests
    {
        private static readonly ClusterChunkCoord[] IrregularFootprint =
        {
            new ClusterChunkCoord(0, 0),
            new ClusterChunkCoord(1, 0),
            new ClusterChunkCoord(2, 0),
            new ClusterChunkCoord(0, 1),
        };

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void StandardConnectedFootprintSizesCompile(int chunkCount)
        {
            var result = Compile(LinearChunks(chunkCount));

            AssertSuccess(result);
            Assert.That(result.LocalCanvas.ChunkCells.Count, Is.EqualTo(chunkCount));
            Assert.That(CountChunks(result, ClusterChunkMaskState.Active), Is.EqualTo(chunkCount));
            Assert.That(CountChunks(result, ClusterChunkMaskState.Inactive), Is.Zero);
            Assert.That(CountTiles(result, ClusterChunkMaskState.Active),
                Is.EqualTo(chunkCount * WorldGenConstants.TilesPerMicroChunk));
            Assert.That(CountTiles(result, ClusterChunkMaskState.Inactive), Is.Zero);
        }

        [Test]
        public void ExactSixChunkAllowlistControlsPublication()
        {
            var id = new TerrainClusterId("TC_SIX_CHUNK");
            var contract = CreateContract(LinearChunks(6), id);

            var rejected = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(
                    contract,
                    ClusterFootprintTransform.R0,
                    new[] { new TerrainClusterId("TC_OTHER") }));
            var accepted = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(
                    contract,
                    ClusterFootprintTransform.R0,
                    new[] { new TerrainClusterId("TC_OTHER"), id }));

            AssertFailure(rejected, TerrainClusterFootprintCompileErrorCode.SixChunkNotAllowlisted);
            AssertSuccess(accepted);
            Assert.That(CountChunks(accepted, ClusterChunkMaskState.Active), Is.EqualTo(6));
            Assert.That(CountTiles(accepted, ClusterChunkMaskState.Active),
                Is.EqualTo(6 * WorldGenConstants.TilesPerMicroChunk));
        }

        [Test]
        public void InvalidCountDuplicateNegativeUnnormalizedAndDisconnectedFootprintsAreAtomic()
        {
            var baseline = CreateContract(LinearChunks(2));
            var invalidContracts = new[]
            {
                CreateContract(LinearChunks(1)),
                CreateContract(LinearChunks(7)),
                Rebuild(baseline, baseline.Footprint.ActiveChunks.Concat(
                    new[] { new ClusterChunkCoord(0, 0) })),
                Rebuild(baseline, new[]
                {
                    new ClusterChunkCoord(-1, 0),
                    new ClusterChunkCoord(0, 0),
                }),
                Rebuild(baseline, new[]
                {
                    new ClusterChunkCoord(1, 0),
                    new ClusterChunkCoord(2, 0),
                }),
                Rebuild(baseline, new[]
                {
                    new ClusterChunkCoord(0, 0),
                    new ClusterChunkCoord(2, 0),
                }),
                Rebuild(baseline, new[]
                {
                    new ClusterChunkCoord(0, 0),
                    new ClusterChunkCoord(1, 1),
                }),
            };

            foreach (var contract in invalidContracts)
            {
                var result = TerrainClusterFootprintCompiler.Compile(
                    new TerrainClusterFootprintCompileRequest(
                        contract,
                        ClusterFootprintTransform.R0));
                AssertFailure(result, TerrainClusterFootprintCompileErrorCode.InvalidSourceFootprint);
            }
        }

        [Test]
        public void IrregularBoundsPublishExplicitInactiveChunksAndTiles()
        {
            var result = Compile(IrregularFootprint);

            AssertSuccess(result);
            Assert.That(result.LocalCanvas.ChunkWidth, Is.EqualTo(3));
            Assert.That(result.LocalCanvas.ChunkHeight, Is.EqualTo(2));
            Assert.That(result.LocalCanvas.TileWidth, Is.EqualTo(36));
            Assert.That(result.LocalCanvas.TileHeight, Is.EqualTo(16));
            Assert.That(result.ChunkCells.Count, Is.EqualTo(6));
            Assert.That(CountChunks(result, ClusterChunkMaskState.Active), Is.EqualTo(4));
            Assert.That(CountChunks(result, ClusterChunkMaskState.Inactive), Is.EqualTo(2));
            Assert.That(CountTiles(result, ClusterChunkMaskState.Active), Is.EqualTo(384));
            Assert.That(CountTiles(result, ClusterChunkMaskState.Inactive), Is.EqualTo(192));

            CompiledClusterChunkCell inactive;
            Assert.That(result.LocalCanvas.TryGetChunkCell(new ClusterChunkCoord(2, 1), out inactive), Is.True);
            Assert.That(inactive.State, Is.EqualTo(ClusterChunkMaskState.Inactive));
            Assert.That(result.LocalCanvas.TryGetChunkCell(new ClusterChunkCoord(3, 0), out inactive), Is.False);
        }

        [TestCase(ClusterFootprintTransform.R0, 0, 1, 2, 0)]
        [TestCase(ClusterFootprintTransform.MirrorX, 2, 1, 0, 0)]
        [TestCase(ClusterFootprintTransform.MirrorY, 0, 0, 2, 1)]
        [TestCase(ClusterFootprintTransform.R180, 2, 0, 0, 1)]
        public void ChunkTransformMappingsAreExact(
            ClusterFootprintTransform transform,
            int firstX,
            int firstY,
            int secondX,
            int secondY)
        {
            var result = Compile(IrregularFootprint, transform);
            AssertSuccess(result);

            ClusterChunkCoord compiled;
            ClusterChunkCoord source;
            Assert.That(result.LocalCanvas.TryGetCompiledChunk(
                new ClusterChunkCoord(0, 1), out compiled), Is.True);
            Assert.That(compiled, Is.EqualTo(new ClusterChunkCoord(firstX, firstY)));
            Assert.That(result.LocalCanvas.TryGetSourceChunk(compiled, out source), Is.True);
            Assert.That(source, Is.EqualTo(new ClusterChunkCoord(0, 1)));

            Assert.That(result.LocalCanvas.TryGetCompiledChunk(
                new ClusterChunkCoord(2, 0), out compiled), Is.True);
            Assert.That(compiled, Is.EqualTo(new ClusterChunkCoord(secondX, secondY)));
        }

        [TestCase(ClusterFootprintTransform.R0, 2, 9)]
        [TestCase(ClusterFootprintTransform.MirrorX, 33, 9)]
        [TestCase(ClusterFootprintTransform.MirrorY, 2, 6)]
        [TestCase(ClusterFootprintTransform.R180, 33, 6)]
        public void TileTransformMappingsUseFullLocalBounds(
            ClusterFootprintTransform transform,
            int expectedX,
            int expectedY)
        {
            var result = Compile(IrregularFootprint, transform);
            AssertSuccess(result);

            LocalTileCoord compiled;
            LocalTileCoord source;
            Assert.That(result.LocalCanvas.TryGetCompiledTile(
                new LocalTileCoord(2, 9), out compiled), Is.True);
            Assert.That(compiled, Is.EqualTo(new LocalTileCoord(expectedX, expectedY)));
            Assert.That(result.LocalCanvas.TryGetSourceTile(compiled, out source), Is.True);
            Assert.That(source, Is.EqualTo(new LocalTileCoord(2, 9)));
        }

        [Test]
        public void EveryTransformIsAnInvolutionAndPreservesConnectivity()
        {
            foreach (ClusterFootprintTransform transform in Enum.GetValues(typeof(ClusterFootprintTransform)))
            {
                var result = Compile(IrregularFootprint, transform);
                AssertSuccess(result);
                var canvas = result.LocalCanvas;

                foreach (var sourceChunk in canvas.ChunkCells.Select(value => value.SourceCoordinate))
                {
                    ClusterChunkCoord compiled;
                    ClusterChunkCoord roundTrip;
                    Assert.That(canvas.TryGetCompiledChunk(sourceChunk, out compiled), Is.True);
                    Assert.That(canvas.TryGetSourceChunk(compiled, out roundTrip), Is.True);
                    Assert.That(roundTrip, Is.EqualTo(sourceChunk));
                }

                foreach (var sourceTile in canvas.TileCells.Select(value => value.SourceCoordinate))
                {
                    LocalTileCoord compiled;
                    LocalTileCoord roundTrip;
                    Assert.That(canvas.TryGetCompiledTile(sourceTile, out compiled), Is.True);
                    Assert.That(canvas.TryGetSourceTile(compiled, out roundTrip), Is.True);
                    Assert.That(roundTrip, Is.EqualTo(sourceTile));
                }

                Assert.That(IsConnected(canvas.ChunkCells
                    .Where(value => value.State == ClusterChunkMaskState.Active)
                    .Select(value => value.Coordinate)), Is.True);
            }
        }

        [Test]
        public void EveryTilePublishesExactOwningChunkWithinChunkAndSourceRoundTrip()
        {
            var result = Compile(IrregularFootprint, ClusterFootprintTransform.R180);
            AssertSuccess(result);
            var canvas = result.LocalCanvas;

            Assert.That(canvas.TileCells.Count, Is.EqualTo(canvas.TileWidth * canvas.TileHeight));
            for (var index = 0; index < canvas.TileCells.Count; index++)
            {
                var cell = canvas.TileCells[index];
                Assert.That(cell.CanonicalIndex, Is.EqualTo(index));
                Assert.That(cell.Coordinate,
                    Is.EqualTo(new LocalTileCoord(index % canvas.TileWidth, index / canvas.TileWidth)));
                Assert.That(cell.OwningChunk, Is.EqualTo(new ClusterChunkCoord(
                    cell.Coordinate.X / WorldGenConstants.MicroChunkWidthTiles,
                    cell.Coordinate.Y / WorldGenConstants.MicroChunkHeightTiles)));
                Assert.That(cell.WithinChunkCoordinate, Is.EqualTo(new LocalTileCoord(
                    cell.Coordinate.X % WorldGenConstants.MicroChunkWidthTiles,
                    cell.Coordinate.Y % WorldGenConstants.MicroChunkHeightTiles)));
                Assert.That(cell.SourceChunkCoordinate, Is.EqualTo(new ClusterChunkCoord(
                    cell.SourceCoordinate.X / WorldGenConstants.MicroChunkWidthTiles,
                    cell.SourceCoordinate.Y / WorldGenConstants.MicroChunkHeightTiles)));

                CompiledClusterLocalTileCell lookup;
                Assert.That(canvas.TryGetTileCell(cell.Coordinate, out lookup), Is.True);
                Assert.That(lookup, Is.SameAs(cell));
            }
        }

        [Test]
        public void CanonicalOrderDigestAndMappingsIgnoreEnumerationCultureAndDisplayText()
        {
            var forward = CreateContract(IrregularFootprint, displayText: "Forward");
            var reversed = CreateContract(IrregularFootprint.Reverse(), displayText: "다른 표시 문자열");
            var oldCulture = CultureInfo.CurrentCulture;
            var oldUiCulture = CultureInfo.CurrentUICulture;
            TerrainClusterFootprintCompileResult first;
            TerrainClusterFootprintCompileResult second;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                first = TerrainClusterFootprintCompiler.Compile(
                    new TerrainClusterFootprintCompileRequest(
                        forward,
                        ClusterFootprintTransform.MirrorX));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ko-KR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ko-KR");
                second = TerrainClusterFootprintCompiler.Compile(
                    new TerrainClusterFootprintCompileRequest(
                        reversed,
                        ClusterFootprintTransform.MirrorX));
            }
            finally
            {
                CultureInfo.CurrentCulture = oldCulture;
                CultureInfo.CurrentUICulture = oldUiCulture;
            }

            AssertSuccess(first);
            AssertSuccess(second);
            Assert.That(second.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
            Assert.That(second.LocalCanvas.SourceFootprintDigest,
                Is.EqualTo(first.LocalCanvas.SourceFootprintDigest));
            Assert.That(second.ChunkCells.Select(CellSignature),
                Is.EqualTo(first.ChunkCells.Select(CellSignature)));
            Assert.That(first.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void PublicationAndRequestInputsAreDefensivelyImmutable()
        {
            var id = new TerrainClusterId("TC_SIX_CHUNK");
            var allowlist = new[] { id };
            var request = new TerrainClusterFootprintCompileRequest(
                CreateContract(LinearChunks(6), id),
                ClusterFootprintTransform.R0,
                allowlist);
            allowlist[0] = new TerrainClusterId("TC_MUTATED");
            var result = TerrainClusterFootprintCompiler.Compile(request);
            AssertSuccess(result);

            var chunks = (IList<CompiledClusterChunkCell>)result.LocalCanvas.ChunkCells;
            var tiles = (IList<CompiledClusterLocalTileCell>)result.LocalCanvas.TileCells;
            Assert.Throws<NotSupportedException>(() => chunks[0] = chunks[0]);
            Assert.Throws<NotSupportedException>(() => tiles[0] = tiles[0]);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void MissingSourceAndInvalidTransformAccumulateWithoutPartialPublication()
        {
            var result = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(
                    null,
                    (ClusterFootprintTransform)999));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.LocalCanvas, Is.Null);
            Assert.That(result.ChunkCells, Is.Empty);
            Assert.That(result.TileCells, Is.Empty);
            Assert.That(result.ChunkMappingCount, Is.Zero);
            Assert.That(result.TileMappingCount, Is.Zero);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Is.EqualTo(new[]
            {
                TerrainClusterFootprintCompileErrorCode.MissingInput,
                TerrainClusterFootprintCompileErrorCode.InvalidTransform,
            }));
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        [Test]
        public void LocalMaskCompilerHasNoRoleSpinePatternFinalCanvasOrUnitySideEffects()
        {
            var runtimeFiles = new[]
            {
                "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterFootprintTransform.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterLocalCanvas.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterFootprintCompiler.cs",
            };
            var source = string.Join("\n", runtimeFiles.Select(File.ReadAllText));
            var forbidden = new[]
            {
                "UnityEditor", "StageMapGenerator", "GridWorld", "RoomTemplate",
                "RoomGridTransform", "TileMutationService", "SectorRecipeResolver",
                "System.Random", "UnityEngine.Random", "ClusterRoleAnchor", "SpineVariant",
                "MicroPattern", "SectorCanvasContract", "GeneratedSlice", "Tilemap",
                "StarNight.Map.WorldGeneration.Baking",
            };

            foreach (var symbol in forbidden)
            {
                Assert.That(source, Does.Not.Contain(symbol), symbol);
            }
        }

        [Test]
        public void OutOfBoundsLookupsDoNotPublishImplicitCellsOrMappings()
        {
            var canvas = Compile(IrregularFootprint).LocalCanvas;
            CompiledClusterChunkCell chunkCell;
            CompiledClusterLocalTileCell tileCell;
            ClusterChunkCoord chunkCoordinate;
            LocalTileCoord tileCoordinate;

            Assert.That(canvas.TryGetChunkCell(new ClusterChunkCoord(-1, 0), out chunkCell), Is.False);
            Assert.That(canvas.TryGetTileCell(new LocalTileCoord(36, 0), out tileCell), Is.False);
            Assert.That(canvas.TryGetCompiledChunk(new ClusterChunkCoord(3, 0), out chunkCoordinate), Is.False);
            Assert.That(canvas.TryGetSourceChunk(new ClusterChunkCoord(3, 0), out chunkCoordinate), Is.False);
            Assert.That(canvas.TryGetCompiledTile(new LocalTileCoord(36, 0), out tileCoordinate), Is.False);
            Assert.That(canvas.TryGetSourceTile(new LocalTileCoord(36, 0), out tileCoordinate), Is.False);
        }

        [Test]
        public void TransformChangesArtifactDigestButNotSourceFootprintDigestOrMass()
        {
            var r0 = Compile(IrregularFootprint, ClusterFootprintTransform.R0);
            var r180 = Compile(IrregularFootprint, ClusterFootprintTransform.R180);
            AssertSuccess(r0);
            AssertSuccess(r180);

            Assert.That(r180.LocalCanvas.SourceFootprintDigest,
                Is.EqualTo(r0.LocalCanvas.SourceFootprintDigest));
            Assert.That(r180.CanonicalDigest, Is.Not.EqualTo(r0.CanonicalDigest));
            Assert.That(CountChunks(r180, ClusterChunkMaskState.Active),
                Is.EqualTo(CountChunks(r0, ClusterChunkMaskState.Active)));
            Assert.That(CountTiles(r180, ClusterChunkMaskState.Active),
                Is.EqualTo(CountTiles(r0, ClusterChunkMaskState.Active)));
        }

        private static TerrainClusterFootprintCompileResult Compile(
            IEnumerable<ClusterChunkCoord> chunks,
            ClusterFootprintTransform transform = ClusterFootprintTransform.R0)
        {
            var contract = CreateContract(chunks);
            Assert.That(TerrainClusterContractValidator.Validate(contract).IsValid, Is.True,
                "The success fixture must first pass the MAP09_04 authority.");
            return TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(contract, transform));
        }

        private static TerrainClusterContract CreateContract(
            IEnumerable<ClusterChunkCoord> chunks,
            TerrainClusterId? id = null,
            string displayText = "Fixture display text")
        {
            var chunkCopy = chunks.ToArray();
            var clusterId = id ?? new TerrainClusterId("TC_LIVE_BASELINE");
            var maxChunkX = chunkCopy.Length == 0 ? 0 : Math.Max(0, chunkCopy.Max(value => value.X));
            var maxX = Math.Max(1,
                (maxChunkX + 1) * WorldGenConstants.MicroChunkWidthTiles - 1);
            var roleData = new[]
            {
                Role("ANCHOR_ENTRY", ClusterRoleKind.Entry, 0, "NODE_ENTRY"),
                Role("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp, Math.Min(4, maxX), "NODE_BUILD_UP"),
                Role("ANCHOR_CORE", ClusterRoleKind.Core, Math.Min(9, maxX), "NODE_CORE"),
                Role("ANCHOR_RECOVERY", ClusterRoleKind.Recovery, Math.Max(1, maxX - 8), "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_REWARD", ClusterRoleKind.Reward,
                    new LocalTileCoord(Math.Max(1, maxX - 5), 2), "NODE_REWARD"),
                Role("ANCHOR_EXIT", ClusterRoleKind.Exit, maxX, "NODE_EXIT"),
            };
            var nodes = roleData.Select(value => new TraversalNode(
                value.TraversalNodeId,
                value.Tile,
                value.Role != ClusterRoleKind.Reward,
                value.AnchorId)).ToArray();
            var byId = nodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var edges = new[]
            {
                CreateEdge("EDGE_ENTRY_BUILD", byId["NODE_ENTRY"], byId["NODE_BUILD_UP"],
                    TraversalMovementKind.Walk, byId["NODE_BUILD_UP"].Tile),
                CreateEdge("EDGE_BUILD_CORE", byId["NODE_BUILD_UP"], byId["NODE_CORE"],
                    TraversalMovementKind.Jump, byId["NODE_BUILD_UP"].Tile),
                CreateEdge("EDGE_CORE_RECOVERY", byId["NODE_CORE"], byId["NODE_RECOVERY"],
                    TraversalMovementKind.Drop, byId["NODE_CORE"].Tile),
                CreateEdge("EDGE_RECOVERY_EXIT", byId["NODE_RECOVERY"], byId["NODE_EXIT"],
                    TraversalMovementKind.Slide, byId["NODE_RECOVERY"].Tile),
            };
            var ports = new[]
            {
                new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true, "ANCHOR_ENTRY",
                    roleData[0].Tile, ClusterPortSide.L, new[] { 0, 1, 2, 3, 4 }),
                new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true, "ANCHOR_EXIT",
                    roleData[5].Tile, ClusterPortSide.R, new[] { 1, 2, 3, 4 }),
            };
            return new TerrainClusterContract(
                clusterId,
                new ClusterFootprint(chunkCopy),
                roleData,
                ports,
                new TerrainClusterTraversalContract(new[]
                {
                    new SpineVariant(
                        new SpineVariantId("SPINE_BASELINE"),
                        true,
                        TraversalGraphKind.Traversal,
                        nodes,
                        edges),
                }),
                displayText);
        }

        private static TerrainClusterContract Rebuild(
            TerrainClusterContract source,
            IEnumerable<ClusterChunkCoord> chunks)
        {
            return new TerrainClusterContract(
                source.Id,
                new ClusterFootprint(chunks),
                source.RoleAnchors,
                source.Ports,
                source.Traversal,
                source.DisplayText);
        }

        private static ClusterRoleAnchor Role(
            string anchorId,
            ClusterRoleKind kind,
            int x,
            string nodeId)
        {
            return new ClusterRoleAnchor(anchorId, kind, new LocalTileCoord(x, 1), nodeId);
        }

        private static TraversalEdge CreateEdge(
            string id,
            TraversalNode from,
            TraversalNode to,
            TraversalMovementKind movement,
            LocalTileCoord recovery)
        {
            return new TraversalEdge(
                id,
                from.NodeId,
                to.NodeId,
                movement,
                from.Tile,
                to.Tile,
                1,
                2,
                to.Tile,
                recovery,
                true,
                CreateEnvelope(movement, from.Tile, to.Tile, recovery));
        }

        private static TraversalEnvelope CreateEnvelope(
            TraversalMovementKind movement,
            LocalTileCoord start,
            LocalTileCoord end,
            LocalTileCoord recovery)
        {
            var floor = movement == TraversalMovementKind.Walk ||
                        movement == TraversalMovementKind.Slide
                ? new[] { new LocalTileCoord(start.X, 0) }
                : Array.Empty<LocalTileCoord>();
            var jump = movement == TraversalMovementKind.Jump ||
                       movement == TraversalMovementKind.Bounce
                ? new[] { new LocalTileCoord(
                    (start.X + end.X) / 2,
                    Math.Min(7, Math.Max(start.Y, end.Y) + 2)) }
                : Array.Empty<LocalTileCoord>();
            var drop = movement == TraversalMovementKind.Drop
                ? new[] { new LocalTileCoord(
                    (start.X + end.X) / 2,
                    Math.Min(7, Math.Max(start.Y, end.Y) + 1)) }
                : Array.Empty<LocalTileCoord>();
            return new TraversalEnvelope(
                new[] { start, end },
                floor,
                new[] { start, end },
                jump,
                drop,
                new[] { end },
                new[] { recovery });
        }

        private static ClusterChunkCoord[] LinearChunks(int count)
        {
            return Enumerable.Range(0, Math.Max(0, count))
                .Select(value => new ClusterChunkCoord(value, 0))
                .ToArray();
        }

        private static int CountChunks(
            TerrainClusterFootprintCompileResult result,
            ClusterChunkMaskState state)
        {
            return result.ChunkCells.Count(value => value.State == state);
        }

        private static int CountTiles(
            TerrainClusterFootprintCompileResult result,
            ClusterChunkMaskState state)
        {
            return result.TileCells.Count(value => value.State == state);
        }

        private static string CellSignature(CompiledClusterChunkCell cell)
        {
            return cell.CanonicalIndex + "|" + cell.Coordinate.X + "," + cell.Coordinate.Y +
                   "|" + cell.State + "|" + cell.SourceCoordinate.X + "," + cell.SourceCoordinate.Y;
        }

        private static bool IsConnected(IEnumerable<ClusterChunkCoord> coordinates)
        {
            var active = new HashSet<ClusterChunkCoord>(coordinates);
            if (active.Count == 0) return false;
            var reached = new HashSet<ClusterChunkCoord>();
            var queue = new Queue<ClusterChunkCoord>();
            var first = active.First();
            reached.Add(first);
            queue.Enqueue(first);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                var neighbors = new[]
                {
                    new ClusterChunkCoord(current.X - 1, current.Y),
                    new ClusterChunkCoord(current.X + 1, current.Y),
                    new ClusterChunkCoord(current.X, current.Y - 1),
                    new ClusterChunkCoord(current.X, current.Y + 1),
                };
                foreach (var neighbor in neighbors)
                {
                    if (active.Contains(neighbor) && reached.Add(neighbor)) queue.Enqueue(neighbor);
                }
            }

            return reached.Count == active.Count;
        }

        private static void AssertSuccess(TerrainClusterFootprintCompileResult result)
        {
            Assert.That(result.IsSuccess, Is.True,
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            Assert.That(result.LocalCanvas, Is.Not.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static void AssertFailure(
            TerrainClusterFootprintCompileResult result,
            TerrainClusterFootprintCompileErrorCode expected)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.LocalCanvas, Is.Null);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(expected),
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            Assert.That(result.ChunkCells, Is.Empty);
            Assert.That(result.TileCells, Is.Empty);
            Assert.That(result.ChunkMappingCount, Is.Zero);
            Assert.That(result.TileMappingCount, Is.Zero);
            Assert.That(result.CanonicalDigest, Is.Empty);
        }
    }
}
