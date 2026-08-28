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
    [Category("MAP11_02")]
    public sealed class TerrainClusterRoleSocketCompilerTests
    {
        [TestCase(true, 6)]
        [TestCase(false, 5)]
        public void RequiredRolesAndRewardZeroOrMoreProjectExactly(bool includeReward, int expectedRoles)
        {
            var result = CompileSuccess(CreateContract(includeReward: includeReward));

            Assert.That(result.Roles.Count, Is.EqualTo(expectedRoles));
            Assert.That(result.Roles.Select(value => value.Role).Distinct(),
                includeReward
                    ? Is.EquivalentTo(Enum.GetValues(typeof(ClusterRoleKind)))
                    : Is.EquivalentTo(new[]
                    {
                        ClusterRoleKind.Entry,
                        ClusterRoleKind.BuildUp,
                        ClusterRoleKind.Core,
                        ClusterRoleKind.Recovery,
                        ClusterRoleKind.Exit,
                    }));
            Assert.That(result.RoleSpineLinks.Count, Is.EqualTo(expectedRoles * 2));
        }

        [TestCase(ClusterFootprintTransform.R0)]
        [TestCase(ClusterFootprintTransform.MirrorX)]
        [TestCase(ClusterFootprintTransform.MirrorY)]
        [TestCase(ClusterFootprintTransform.R180)]
        public void EveryRoleUsesMap1101TileMappingAndActiveOwnership(
            ClusterFootprintTransform transform)
        {
            var contract = CreateContract();
            var canvas = CompileCanvas(contract, transform);
            var result = CompileSuccess(contract, canvas);

            foreach (var role in result.Roles)
            {
                LocalTileCoord expected;
                CompiledClusterLocalTileCell tileCell;
                CompiledClusterChunkCell chunkCell;
                Assert.That(canvas.TryGetCompiledTile(role.SourceCoordinate, out expected), Is.True);
                Assert.That(role.CompiledCoordinate, Is.EqualTo(expected));
                Assert.That(canvas.TryGetTileCell(expected, out tileCell), Is.True);
                Assert.That(tileCell.State, Is.EqualTo(ClusterChunkMaskState.Active));
                Assert.That(role.OwningCompiledChunk, Is.EqualTo(tileCell.OwningChunk));
                Assert.That(canvas.TryGetChunkCell(role.OwningCompiledChunk, out chunkCell), Is.True);
                Assert.That(chunkCell.State, Is.EqualTo(ClusterChunkMaskState.Active));
            }
        }

        [Test]
        public void InvalidSourceRoleOutsideFootprintIsRejectedAtomically()
        {
            var baseline = CreateContract();
            var canvas = CompileCanvas(baseline, ClusterFootprintTransform.R0);
            var roles = baseline.RoleAnchors.Select(value => value.Role == ClusterRoleKind.Core
                ? new ClusterRoleAnchor(
                    value.AnchorId,
                    value.Role,
                    new LocalTileCoord(999, 999),
                    value.TraversalNodeId)
                : value).ToArray();
            var invalid = new TerrainClusterContract(
                baseline.Id,
                baseline.Footprint,
                roles,
                baseline.Ports,
                baseline.Traversal,
                baseline.DisplayText);
            var result = TerrainClusterRoleSocketCompiler.Compile(
                BuildRequest(invalid, canvas, SocketEvidence(baseline, canvas), string.Empty));

            AssertFailure(result, TerrainClusterRoleSocketCompileErrorCode.InvalidSourceContract);
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterRoleSocketCompileErrorCode.RoleOutsideActiveMask));
        }

        [Test]
        public void PortMayPointToExplicitInactiveChunkInsideLocalBounds()
        {
            var chunks = new[]
            {
                new ClusterChunkCoord(0, 0),
                new ClusterChunkCoord(1, 0),
                new ClusterChunkCoord(0, 1),
            };
            var contract = CreateContract(
                chunks: chunks,
                exitSide: ClusterPortSide.U,
                exitTile: new LocalTileCoord(23, 7));
            var canvas = CompileCanvas(contract, ClusterFootprintTransform.R0);
            var result = CompileSuccess(contract, canvas);
            var exit = result.Ports.Single(value => value.Kind == ClusterPortKind.Exit);
            var neighbor = new LocalTileCoord(exit.CompiledCoordinate.X, exit.CompiledCoordinate.Y + 1);
            CompiledClusterLocalTileCell neighborCell;

            Assert.That(canvas.TryGetTileCell(neighbor, out neighborCell), Is.True);
            Assert.That(neighborCell.State, Is.EqualTo(ClusterChunkMaskState.Inactive));
            Assert.That(exit.CompiledOutwardSide, Is.EqualTo(ClusterPortSide.U));
        }

        [TestCase(ClusterFootprintTransform.R0,
            ClusterPortSide.L, ClusterPortSide.R, ClusterPortSide.U, ClusterPortSide.D)]
        [TestCase(ClusterFootprintTransform.MirrorX,
            ClusterPortSide.R, ClusterPortSide.L, ClusterPortSide.U, ClusterPortSide.D)]
        [TestCase(ClusterFootprintTransform.MirrorY,
            ClusterPortSide.L, ClusterPortSide.R, ClusterPortSide.D, ClusterPortSide.U)]
        [TestCase(ClusterFootprintTransform.R180,
            ClusterPortSide.R, ClusterPortSide.L, ClusterPortSide.D, ClusterPortSide.U)]
        public void OutwardSideMatrixIsExact(
            ClusterFootprintTransform transform,
            ClusterPortSide expectedL,
            ClusterPortSide expectedR,
            ClusterPortSide expectedU,
            ClusterPortSide expectedD)
        {
            var horizontal = CreateContract(
                entrySide: ClusterPortSide.L,
                exitSide: ClusterPortSide.R);
            var horizontalResult = CompileSuccess(
                horizontal,
                CompileCanvas(horizontal, transform));
            Assert.That(horizontalResult.Ports.Single(value => value.Kind == ClusterPortKind.Entry)
                .CompiledOutwardSide, Is.EqualTo(expectedL));
            Assert.That(horizontalResult.Ports.Single(value => value.Kind == ClusterPortKind.Exit)
                .CompiledOutwardSide, Is.EqualTo(expectedR));

            var vertical = CreateContract(
                entrySide: ClusterPortSide.U,
                exitSide: ClusterPortSide.D);
            var verticalResult = CompileSuccess(
                vertical,
                CompileCanvas(vertical, transform));
            Assert.That(verticalResult.Ports.Single(value => value.Kind == ClusterPortKind.Entry)
                .CompiledOutwardSide, Is.EqualTo(expectedU));
            Assert.That(verticalResult.Ports.Single(value => value.Kind == ClusterPortKind.Exit)
                .CompiledOutwardSide, Is.EqualTo(expectedD));
        }

        [Test]
        public void PrimaryPortsPreserveRoleTileAndCompatibleRouteTypes()
        {
            var result = CompileSuccess(CreateContract());
            Assert.That(result.Ports.Count, Is.EqualTo(2));
            foreach (var port in result.Ports)
            {
                var role = result.Roles.Single(value => value.AnchorId == port.RoleAnchorId);
                Assert.That(port.CompiledCoordinate, Is.EqualTo(role.CompiledCoordinate));
                Assert.That(port.RoleKind, Is.EqualTo(port.Kind == ClusterPortKind.Entry
                    ? ClusterRoleKind.Entry
                    : ClusterRoleKind.Exit));
            }

            Assert.That(result.Ports.Single(value => value.Kind == ClusterPortKind.Entry)
                .CompatibleRouteTypes, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(result.Ports.Single(value => value.Kind == ClusterPortKind.Exit)
                .CompatibleRouteTypes, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void EveryVariantPublishesEveryAuthoredRoleNodeLink()
        {
            var result = CompileSuccess(CreateContract());

            Assert.That(result.RoleSpineLinks.Select(value => value.VariantId.Value).Distinct(),
                Is.EqualTo(new[] { "SPINE_ALTERNATE", "SPINE_BASELINE" }));
            foreach (var variant in result.RoleSpineLinks.GroupBy(value => value.VariantId))
            {
                Assert.That(variant.Count(), Is.EqualTo(result.Roles.Count));
                Assert.That(variant.Count(value =>
                    value.ConnectionKind == ProjectedRoleConnectionKind.EntryPort), Is.EqualTo(1));
                Assert.That(variant.Count(value =>
                    value.ConnectionKind == ProjectedRoleConnectionKind.ExitPort), Is.EqualTo(1));
                Assert.That(variant.Select(value => value.RoleAnchorId),
                    Is.EqualTo(result.Roles.Select(value => value.AnchorId)));
            }
        }

        [Test]
        public void RoleNodeCoordinatesAndEntryExitChainsRemainExact()
        {
            var result = CompileSuccess(CreateContract(), ClusterFootprintTransform.R180);
            foreach (var link in result.RoleSpineLinks)
            {
                var role = result.Roles.Single(value => value.AnchorId == link.RoleAnchorId);
                Assert.That(link.TraversalNodeId, Is.EqualTo(role.TraversalNodeId));
                Assert.That(link.SourceCoordinate, Is.EqualTo(role.SourceCoordinate));
                Assert.That(link.CompiledCoordinate, Is.EqualTo(role.CompiledCoordinate));
            }

            Assert.That(result.SocketConnections.Single(value => value.PortKind == ClusterPortKind.Entry)
                .RoleAnchorId, Is.EqualTo("ANCHOR_ENTRY"));
            Assert.That(result.SocketConnections.Single(value => value.PortKind == ClusterPortKind.Exit)
                .RoleAnchorId, Is.EqualTo("ANCHOR_EXIT"));
        }

        [Test]
        public void ExternalSocketsBindExactlyOnceWithSideRouteAndMandatoryEvidence()
        {
            var result = CompileSuccess(CreateContract());

            Assert.That(result.SocketConnections.Count, Is.EqualTo(2));
            Assert.That(result.SocketConnections.Select(value => value.Evidence.StableIdentity),
                Is.EqualTo(new[] { "SR_ENTRY/SOCKET_ENTRY", "SR_EXIT/SOCKET_EXIT" }));
            foreach (var connection in result.SocketConnections)
            {
                var port = result.Ports.Single(value => value.Kind == connection.PortKind);
                Assert.That(connection.Evidence.Side, Is.EqualTo(port.CompiledOutwardSide));
                Assert.That(port.CompatibleRouteTypes,
                    Does.Contain(connection.Evidence.OwningRouteType));
                Assert.That(connection.Evidence.MandatoryAllowed, Is.True);
                Assert.That(connection.CompiledCoordinate, Is.EqualTo(port.CompiledCoordinate));
            }
        }

        [Test]
        public void MissingSocketBindingsFailAtomically()
        {
            var contract = CreateContract();
            var canvas = CompileCanvas(contract, ClusterFootprintTransform.R0);
            var request = BuildRequest(
                contract,
                canvas,
                Array.Empty<ClusterSectorSocketEvidence>());
            var result = TerrainClusterRoleSocketCompiler.Compile(request);

            AssertFailure(result, TerrainClusterRoleSocketCompileErrorCode.MissingSocketBinding);
            Assert.That(result.Errors.Count(value =>
                value.Code == TerrainClusterRoleSocketCompileErrorCode.MissingSocketBinding), Is.EqualTo(2));
        }

        [Test]
        public void DuplicateSocketBindingsFailAtomically()
        {
            var contract = CreateContract();
            var canvas = CompileCanvas(contract, ClusterFootprintTransform.R0);
            var evidence = SocketEvidence(contract, canvas).Concat(new[]
            {
                new ClusterSectorSocketEvidence(
                    "SR_ENTRY_ALT", "SOCKET_ENTRY_ALT", ClusterPortSide.L,
                    2, true, ClusterPortKind.Entry),
            });
            var result = TerrainClusterRoleSocketCompiler.Compile(
                BuildRequest(contract, canvas, evidence));

            AssertFailure(result, TerrainClusterRoleSocketCompileErrorCode.DuplicateSocketBinding);
        }

        [Test]
        public void SideRouteAndMandatorySocketMismatchesAccumulateAtomically()
        {
            var contract = CreateContract();
            var canvas = CompileCanvas(contract, ClusterFootprintTransform.R0);
            var evidence = new[]
            {
                new ClusterSectorSocketEvidence(
                    "SR_ENTRY", "SOCKET_ENTRY", ClusterPortSide.R,
                    99, false, ClusterPortKind.Entry),
                SocketEvidence(contract, canvas).Single(value =>
                    value.BoundPortKind == ClusterPortKind.Exit),
            };
            var result = TerrainClusterRoleSocketCompiler.Compile(
                BuildRequest(contract, canvas, evidence));

            AssertFailure(result, TerrainClusterRoleSocketCompileErrorCode.SocketSideMismatch);
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterRoleSocketCompileErrorCode.RouteTypeIncompatible));
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterRoleSocketCompileErrorCode.MandatorySocketRejected));
        }

        [Test]
        public void InvalidSourcePrimaryPortAndVariantNodeAreDistinguishedAtomically()
        {
            var baseline = CreateContract();
            var canvas = CompileCanvas(baseline, ClusterFootprintTransform.R0);
            var missingPort = new TerrainClusterContract(
                baseline.Id,
                baseline.Footprint,
                baseline.RoleAnchors,
                baseline.Ports.Where(value => value.Kind != ClusterPortKind.Entry),
                baseline.Traversal,
                baseline.DisplayText);
            var portResult = TerrainClusterRoleSocketCompiler.Compile(
                BuildRequest(missingPort, canvas, SocketEvidence(baseline, canvas), string.Empty));
            AssertFailure(portResult, TerrainClusterRoleSocketCompileErrorCode.MissingOrDuplicatePrimaryPort);

            var variants = baseline.Traversal.Variants.Select(variant =>
                new SpineVariant(
                    variant.Id,
                    variant.IsBaseline,
                    variant.GraphKind,
                    variant.Nodes.Where(value => value.NodeId != "NODE_CORE"),
                    variant.Edges)).ToArray();
            var missingNode = new TerrainClusterContract(
                baseline.Id,
                baseline.Footprint,
                baseline.RoleAnchors,
                baseline.Ports,
                new TerrainClusterTraversalContract(variants),
                baseline.DisplayText);
            var nodeResult = TerrainClusterRoleSocketCompiler.Compile(
                BuildRequest(missingNode, canvas, SocketEvidence(baseline, canvas), string.Empty));
            AssertFailure(nodeResult, TerrainClusterRoleSocketCompileErrorCode.MissingVariantRoleNode);
        }

        [Test]
        public void LocalCanvasIdentityMismatchIsAtomic()
        {
            var source = CreateContract(id: new TerrainClusterId("TC_SOURCE"));
            var other = CreateContract(id: new TerrainClusterId("TC_OTHER"));
            var otherCanvas = CompileCanvas(other, ClusterFootprintTransform.R0);
            var result = TerrainClusterRoleSocketCompiler.Compile(BuildRequest(
                source,
                otherCanvas,
                SocketEvidence(other, otherCanvas)));

            AssertFailure(result, TerrainClusterRoleSocketCompileErrorCode.LocalCanvasIdentityMismatch);
        }

        [Test]
        public void SuppliedLocalCanvasDigestMismatchIsAtomic()
        {
            var contract = CreateContract();
            var canvas = CompileCanvas(contract, ClusterFootprintTransform.R0);
            var result = TerrainClusterRoleSocketCompiler.Compile(BuildRequest(
                contract,
                canvas,
                SocketEvidence(contract, canvas),
                localCanvasDigest: new string('0', 64)));

            AssertFailure(result, TerrainClusterRoleSocketCompileErrorCode.LocalCanvasDigestMismatch);
        }

        [Test]
        public void CollectionsInputsCultureAndEnumerationAreDeterministicAndImmutable()
        {
            var firstContract = CreateContract(displayText: "First display");
            var secondContract = CreateContract(displayText: "다른 표시 문자열");
            var firstCanvas = CompileCanvas(firstContract, ClusterFootprintTransform.MirrorY);
            var secondCanvas = CompileCanvas(secondContract, ClusterFootprintTransform.MirrorY);
            var firstEvidence = SocketEvidence(firstContract, firstCanvas);
            var secondEvidence = SocketEvidence(secondContract, secondCanvas).Reverse().ToArray();
            var oldCulture = CultureInfo.CurrentCulture;
            var oldUiCulture = CultureInfo.CurrentUICulture;
            TerrainClusterRoleSocketCompileResult first;
            TerrainClusterRoleSocketCompileResult second;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                first = CompileSuccess(firstContract, firstCanvas, firstEvidence);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ko-KR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ko-KR");
                second = CompileSuccess(secondContract, secondCanvas, secondEvidence);
            }
            finally
            {
                CultureInfo.CurrentCulture = oldCulture;
                CultureInfo.CurrentUICulture = oldUiCulture;
            }

            Assert.That(second.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
            Assert.That(first.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ProjectedClusterRoleAnchor>)first.Contract.Roles)[0] = first.Contract.Roles[0]);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ProjectedClusterPort>)first.Contract.Ports)[0] = first.Contract.Ports[0]);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ProjectedRoleSpineLink>)first.Contract.RoleSpineLinks)[0] = first.Contract.RoleSpineLinks[0]);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ClusterSectorSocketConnection>)first.Contract.SocketConnections)[0] =
                    first.Contract.SocketConnections[0]);
        }

        [Test]
        public void SemanticSocketChangeChangesDigest()
        {
            var contract = CreateContract();
            var canvas = CompileCanvas(contract, ClusterFootprintTransform.R0);
            var first = CompileSuccess(contract, canvas);
            var changedEvidence = SocketEvidence(contract, canvas).Select(value =>
                value.BoundPortKind == ClusterPortKind.Entry
                    ? new ClusterSectorSocketEvidence(
                        value.SectorRecipeId,
                        "SOCKET_ENTRY_ALTERNATE",
                        value.Side,
                        value.OwningRouteType,
                        value.MandatoryAllowed,
                        value.BoundPortKind)
                    : value).ToArray();
            var changed = CompileSuccess(contract, canvas, changedEvidence);

            Assert.That(changed.CanonicalDigest, Is.Not.EqualTo(first.CanonicalDigest));
        }

        [Test]
        public void RuntimeScopeHasNoEdgeEnvelopePatternPlacementOrUnitySideEffects()
        {
            var files = new[]
            {
                "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterRoleProjection.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterSocketConnection.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterRoleSocketCompiler.cs",
            };
            var source = string.Join("\n", files.Select(File.ReadAllText));
            var forbidden = new[]
            {
                "UnityEditor", "StageMapGenerator", "GridWorld", "RoomTemplate",
                "RoomGridTransform", "TileMutationService", "SectorRecipeResolver",
                "System.Random", "UnityEngine.Random", "TraversalEdge", "TraversalEnvelope",
                "Centerline", "JumpArc", "MicroPattern", "SectorCanvasContract", "Tilemap",
                "WorldGenerationRoot",
            };

            foreach (var symbol in forbidden)
            {
                Assert.That(source, Does.Not.Contain(symbol), symbol);
            }
        }

        private static TerrainClusterRoleSocketCompileResult CompileSuccess(
            TerrainClusterContract contract,
            ClusterFootprintTransform transform = ClusterFootprintTransform.R0)
        {
            return CompileSuccess(contract, CompileCanvas(contract, transform));
        }

        private static TerrainClusterRoleSocketCompileResult CompileSuccess(
            TerrainClusterContract contract,
            TerrainClusterLocalCanvas canvas,
            IEnumerable<ClusterSectorSocketEvidence> evidence = null)
        {
            var result = TerrainClusterRoleSocketCompiler.Compile(BuildRequest(
                contract,
                canvas,
                evidence ?? SocketEvidence(contract, canvas)));
            AssertSuccess(result);
            return result;
        }

        private static TerrainClusterLocalCanvas CompileCanvas(
            TerrainClusterContract contract,
            ClusterFootprintTransform transform)
        {
            var validation = TerrainClusterContractValidator.Validate(contract);
            Assert.That(validation.IsValid, Is.True,
                string.Join("\n", validation.Errors.Select(value => value.ToString())));
            var result = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(contract, transform));
            Assert.That(result.IsSuccess, Is.True,
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            return result.LocalCanvas;
        }

        private static TerrainClusterRoleSocketCompileRequest BuildRequest(
            TerrainClusterContract contract,
            TerrainClusterLocalCanvas canvas,
            IEnumerable<ClusterSectorSocketEvidence> evidence,
            string sourceDigest = null,
            string localCanvasDigest = null)
        {
            var validation = TerrainClusterContractValidator.Validate(contract);
            return new TerrainClusterRoleSocketCompileRequest(
                contract,
                sourceDigest ?? validation.CanonicalDigest,
                canvas,
                localCanvasDigest ?? canvas.CanonicalDigest,
                evidence);
        }

        private static ClusterSectorSocketEvidence[] SocketEvidence(
            TerrainClusterContract contract,
            TerrainClusterLocalCanvas canvas)
        {
            return contract.Ports.Where(value => value.IsPrimary).Select(port =>
            {
                var projectedSide = TransformSide(port.OutwardSide, canvas.Transform);
                return new ClusterSectorSocketEvidence(
                    port.Kind == ClusterPortKind.Entry ? "SR_ENTRY" : "SR_EXIT",
                    port.Kind == ClusterPortKind.Entry ? "SOCKET_ENTRY" : "SOCKET_EXIT",
                    projectedSide,
                    port.Kind == ClusterPortKind.Entry ? 2 : 3,
                    true,
                    port.Kind);
            }).Reverse().ToArray();
        }

        private static ClusterPortSide TransformSide(
            ClusterPortSide source,
            ClusterFootprintTransform transform)
        {
            switch (transform)
            {
                case ClusterFootprintTransform.R0: return source;
                case ClusterFootprintTransform.MirrorX:
                    return source == ClusterPortSide.L ? ClusterPortSide.R :
                        source == ClusterPortSide.R ? ClusterPortSide.L : source;
                case ClusterFootprintTransform.MirrorY:
                    return source == ClusterPortSide.U ? ClusterPortSide.D :
                        source == ClusterPortSide.D ? ClusterPortSide.U : source;
                case ClusterFootprintTransform.R180:
                    return source == ClusterPortSide.L ? ClusterPortSide.R :
                        source == ClusterPortSide.R ? ClusterPortSide.L :
                        source == ClusterPortSide.U ? ClusterPortSide.D : ClusterPortSide.U;
                default: throw new ArgumentOutOfRangeException(nameof(transform));
            }
        }

        private static TerrainClusterContract CreateContract(
            bool includeReward = true,
            ClusterPortSide entrySide = ClusterPortSide.L,
            ClusterPortSide exitSide = ClusterPortSide.R,
            IEnumerable<ClusterChunkCoord> chunks = null,
            LocalTileCoord? entryTile = null,
            LocalTileCoord? exitTile = null,
            TerrainClusterId? id = null,
            string displayText = "Fixture display text")
        {
            var chunkCopy = (chunks ?? new[]
            {
                new ClusterChunkCoord(0, 0),
                new ClusterChunkCoord(1, 0),
                new ClusterChunkCoord(2, 0),
            }).ToArray();
            var maxX = (chunkCopy.Max(value => value.X) + 1) *
                WorldGenConstants.MicroChunkWidthTiles - 1;
            var maxY = (chunkCopy.Max(value => value.Y) + 1) *
                WorldGenConstants.MicroChunkHeightTiles - 1;
            var actualEntryTile = entryTile ?? BoundaryTile(entrySide, maxX, maxY, false);
            var actualExitTile = exitTile ?? BoundaryTile(exitSide, maxX, maxY, true);
            var roles = new List<ClusterRoleAnchor>
            {
                new ClusterRoleAnchor("ANCHOR_ENTRY", ClusterRoleKind.Entry,
                    actualEntryTile, "NODE_ENTRY"),
                new ClusterRoleAnchor("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp,
                    new LocalTileCoord(4, 2), "NODE_BUILD_UP"),
                new ClusterRoleAnchor("ANCHOR_CORE", ClusterRoleKind.Core,
                    new LocalTileCoord(9, 2), "NODE_CORE"),
                new ClusterRoleAnchor("ANCHOR_RECOVERY", ClusterRoleKind.Recovery,
                    new LocalTileCoord(maxX - 8, 2), "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_EXIT", ClusterRoleKind.Exit,
                    actualExitTile, "NODE_EXIT"),
            };
            if (includeReward)
            {
                roles.Add(new ClusterRoleAnchor("ANCHOR_REWARD", ClusterRoleKind.Reward,
                    new LocalTileCoord(maxX - 5, 3), "NODE_REWARD"));
            }

            var nodes = roles.Select(value => new TraversalNode(
                value.TraversalNodeId,
                value.Tile,
                value.Role != ClusterRoleKind.Reward,
                value.AnchorId)).ToArray();
            var byId = nodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var edges = new[]
            {
                CreateEdge("EDGE_ENTRY_BUILD", byId["NODE_ENTRY"], byId["NODE_BUILD_UP"]),
                CreateEdge("EDGE_BUILD_CORE", byId["NODE_BUILD_UP"], byId["NODE_CORE"]),
                CreateEdge("EDGE_CORE_RECOVERY", byId["NODE_CORE"], byId["NODE_RECOVERY"]),
                CreateEdge("EDGE_RECOVERY_EXIT", byId["NODE_RECOVERY"], byId["NODE_EXIT"]),
            };
            var variants = new[]
            {
                new SpineVariant(new SpineVariantId("SPINE_BASELINE"), true,
                    TraversalGraphKind.Traversal, nodes, edges),
                new SpineVariant(new SpineVariantId("SPINE_ALTERNATE"), false,
                    TraversalGraphKind.Traversal, nodes, edges),
            };
            var ports = new[]
            {
                new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true,
                    "ANCHOR_ENTRY", actualEntryTile, entrySide, new[] { 0, 1, 2, 3, 4 }),
                new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true,
                    "ANCHOR_EXIT", actualExitTile, exitSide, new[] { 1, 2, 3, 4 }),
            };
            return new TerrainClusterContract(
                id ?? new TerrainClusterId("TC_ROLE_SOCKET"),
                new ClusterFootprint(chunkCopy),
                roles,
                ports,
                new TerrainClusterTraversalContract(variants),
                displayText);
        }

        private static LocalTileCoord BoundaryTile(
            ClusterPortSide side,
            int maxX,
            int maxY,
            bool second)
        {
            switch (side)
            {
                case ClusterPortSide.L: return new LocalTileCoord(0, second ? 2 : 1);
                case ClusterPortSide.R: return new LocalTileCoord(maxX, second ? 2 : 1);
                case ClusterPortSide.U: return new LocalTileCoord(second ? 2 : 1, maxY);
                case ClusterPortSide.D: return new LocalTileCoord(second ? 2 : 1, 0);
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        private static TraversalEdge CreateEdge(
            string id,
            TraversalNode from,
            TraversalNode to)
        {
            var envelope = new TraversalEnvelope(
                new[] { from.Tile, to.Tile },
                Array.Empty<LocalTileCoord>(),
                new[] { from.Tile, to.Tile },
                Array.Empty<LocalTileCoord>(),
                Array.Empty<LocalTileCoord>(),
                new[] { to.Tile },
                new[] { to.Tile });
            return new TraversalEdge(
                id,
                from.NodeId,
                to.NodeId,
                TraversalMovementKind.Climb,
                from.Tile,
                to.Tile,
                1,
                2,
                to.Tile,
                to.Tile,
                true,
                envelope);
        }

        private static void AssertSuccess(TerrainClusterRoleSocketCompileResult result)
        {
            Assert.That(result.IsSuccess, Is.True,
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            Assert.That(result.Contract, Is.Not.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static void AssertFailure(
            TerrainClusterRoleSocketCompileResult result,
            TerrainClusterRoleSocketCompileErrorCode expected)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Contract, Is.Null);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(expected),
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            Assert.That(result.Roles, Is.Empty);
            Assert.That(result.Ports, Is.Empty);
            Assert.That(result.RoleSpineLinks, Is.Empty);
            Assert.That(result.SocketConnections, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }
    }
}
