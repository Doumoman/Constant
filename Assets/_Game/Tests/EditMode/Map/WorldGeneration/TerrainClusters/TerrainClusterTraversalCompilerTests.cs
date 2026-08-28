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
    [Category("MAP11_03")]
    public sealed class TerrainClusterTraversalCompilerTests
    {
        [Test]
        public void CompilesEveryVariantNodeAndEdgeWithoutSelectingAVariant()
        {
            var fixture = BuildFixture();
            var result = CompileSuccess(fixture);

            Assert.That(result.Variants.Count, Is.EqualTo(fixture.Contract.Traversal.Variants.Count));
            Assert.That(result.Nodes.Count, Is.EqualTo(
                fixture.Contract.Traversal.Variants.Sum(value => value.Nodes.Count)));
            Assert.That(result.Edges.Count, Is.EqualTo(
                fixture.Contract.Traversal.Variants.Sum(value => value.Edges.Count)));
            Assert.That(result.Variants.Count(value => value.IsBaseline), Is.EqualTo(1));
            Assert.That(result.Variants.Select(value => value.VariantId),
                Is.EqualTo(fixture.Contract.Traversal.Variants.Select(value => value.Id)));
        }

        [TestCase(ClusterFootprintTransform.R0)]
        [TestCase(ClusterFootprintTransform.MirrorX)]
        [TestCase(ClusterFootprintTransform.MirrorY)]
        [TestCase(ClusterFootprintTransform.R180)]
        public void ProjectsNodesThroughTheExactLocalCanvasMapping(
            ClusterFootprintTransform transform)
        {
            var fixture = BuildFixture(transform);
            var result = CompileSuccess(fixture);
            foreach (var sourceVariant in fixture.Contract.Traversal.Variants)
            {
                CompiledClusterSpineVariant compiledVariant;
                Assert.That(result.Compilation.TryGetVariant(sourceVariant.Id, out compiledVariant), Is.True);
                foreach (var sourceNode in sourceVariant.Nodes)
                {
                    CompiledTraversalNode compiledNode;
                    LocalTileCoord expected;
                    Assert.That(compiledVariant.TryGetNode(sourceNode.NodeId, out compiledNode), Is.True);
                    Assert.That(fixture.Canvas.TryGetCompiledTile(sourceNode.Tile, out expected), Is.True);
                    Assert.That(compiledNode.SourceCoordinate, Is.EqualTo(sourceNode.Tile));
                    Assert.That(compiledNode.CompiledCoordinate, Is.EqualTo(expected));
                    CompiledClusterLocalTileCell tileCell;
                    Assert.That(fixture.Canvas.TryGetTileCell(expected, out tileCell), Is.True);
                    Assert.That(tileCell.State, Is.EqualTo(ClusterChunkMaskState.Active));
                    Assert.That(compiledNode.OwningCompiledChunk, Is.EqualTo(tileCell.OwningChunk));
                }
            }
        }

        [Test]
        public void PreservesAllSixAuthoredMovementKinds()
        {
            var result = CompileSuccess(BuildFixture());
            var actual = result.Edges.Select(value => value.MovementKind).Distinct().OrderBy(value => value);
            var expected = Enum.GetValues(typeof(TraversalMovementKind))
                .Cast<TraversalMovementKind>().OrderBy(value => value);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void BindsFromToAndStartEndExactly()
        {
            var fixture = BuildFixture(ClusterFootprintTransform.MirrorX);
            var result = CompileSuccess(fixture);
            foreach (var edge in result.Edges)
            {
                var variant = result.Variants.Single(value => value.VariantId == edge.VariantId);
                CompiledTraversalNode from;
                CompiledTraversalNode to;
                Assert.That(variant.TryGetNode(edge.FromNodeId, out from), Is.True);
                Assert.That(variant.TryGetNode(edge.ToNodeId, out to), Is.True);
                Assert.That(edge.SourceStartCoordinate, Is.EqualTo(from.SourceCoordinate));
                Assert.That(edge.SourceEndCoordinate, Is.EqualTo(to.SourceCoordinate));
                Assert.That(edge.CompiledStartCoordinate, Is.EqualTo(from.CompiledCoordinate));
                Assert.That(edge.CompiledEndCoordinate, Is.EqualTo(to.CompiledCoordinate));
            }
        }

        [Test]
        public void ProjectsLandingAndRecoveryToActiveTiles()
        {
            var fixture = BuildFixture(ClusterFootprintTransform.R180);
            var result = CompileSuccess(fixture);
            foreach (var edge in result.Edges)
            {
                LocalTileCoord expectedLanding;
                LocalTileCoord expectedRecovery;
                Assert.That(fixture.Canvas.TryGetCompiledTile(
                    edge.SourceLandingCoordinate, out expectedLanding), Is.True);
                Assert.That(fixture.Canvas.TryGetCompiledTile(
                    edge.SourceRecoveryCoordinate, out expectedRecovery), Is.True);
                Assert.That(edge.CompiledLandingCoordinate, Is.EqualTo(expectedLanding));
                Assert.That(edge.CompiledRecoveryCoordinate, Is.EqualTo(expectedRecovery));
                AssertActive(fixture.Canvas, expectedLanding);
                AssertActive(fixture.Canvas, expectedRecovery);
            }
        }

        [Test]
        public void ProjectsAllSevenNamedSetsWithExactCardinalityAndCanonicalOrder()
        {
            var fixture = BuildFixture(ClusterFootprintTransform.MirrorY);
            var result = CompileSuccess(fixture);
            foreach (var compiledEdge in result.Edges)
            {
                var sourceVariant = fixture.Contract.Traversal.Variants
                    .Single(value => value.Id == compiledEdge.VariantId);
                var sourceEdge = sourceVariant.Edges.Single(value => value.EdgeId == compiledEdge.EdgeId);
                foreach (var kind in Enum.GetValues(typeof(CompiledTraversalEnvelopeSetKind))
                             .Cast<CompiledTraversalEnvelopeSetKind>())
                {
                    var sourceSet = GetSourceSet(sourceEdge.Envelope, kind);
                    var compiledSet = compiledEdge.Envelope.GetTiles(kind);
                    Assert.That(compiledSet.Count, Is.EqualTo(sourceSet.Count), kind.ToString());
                    Assert.That(compiledSet.Select(value => value.CompiledCoordinate),
                        Is.EqualTo(compiledSet.Select(value => value.CompiledCoordinate)
                            .OrderBy(value => value.Y).ThenBy(value => value.X)), kind.ToString());
                    foreach (var sourceCoordinate in sourceSet)
                    {
                        LocalTileCoord expected;
                        Assert.That(fixture.Canvas.TryGetCompiledTile(sourceCoordinate, out expected), Is.True);
                        Assert.That(compiledSet.Any(value =>
                            value.SourceCoordinate == sourceCoordinate &&
                            value.CompiledCoordinate == expected), Is.True, kind.ToString());
                    }
                }
            }
        }

        [Test]
        public void PreservesCommonEnvelopeInvariants()
        {
            var result = CompileSuccess(BuildFixture());
            foreach (var edge in result.Edges)
            {
                Assert.That(edge.Envelope.Centerline, Is.Not.Empty);
                Assert.That(edge.Envelope.Centerline.Select(value => value.CompiledCoordinate),
                    Does.Contain(edge.CompiledStartCoordinate));
                Assert.That(edge.Envelope.Centerline.Select(value => value.CompiledCoordinate),
                    Does.Contain(edge.CompiledEndCoordinate));
                Assert.That(edge.Envelope.Clearance, Is.Not.Empty);
                Assert.That(edge.Envelope.Landing.Select(value => value.CompiledCoordinate),
                    Does.Contain(edge.CompiledLandingCoordinate));
                Assert.That(edge.Envelope.Recovery.Select(value => value.CompiledCoordinate),
                    Does.Contain(edge.CompiledRecoveryCoordinate));
                Assert.That(edge.Envelope.Floor.Select(value => value.CompiledCoordinate)
                    .Intersect(edge.Envelope.Clearance.Select(value => value.CompiledCoordinate)), Is.Empty);
            }
        }

        [Test]
        public void EnforcesExactMovementEnvelopeMatrix()
        {
            var result = CompileSuccess(BuildFixture());
            var byKind = result.Edges.GroupBy(value => value.MovementKind)
                .ToDictionary(group => group.Key, group => group.First().Envelope);
            AssertRequiredAndEmpty(byKind[TraversalMovementKind.Walk], true, true, false, false);
            AssertRequiredAndEmpty(byKind[TraversalMovementKind.Jump], false, true, true, false);
            AssertRequiredAndEmpty(byKind[TraversalMovementKind.Drop], false, true, false, true);
            AssertRequiredAndEmpty(byKind[TraversalMovementKind.Climb], false, true, false, false);
            AssertRequiredAndEmpty(byKind[TraversalMovementKind.Slide], true, true, false, false);
            AssertRequiredAndEmpty(byKind[TraversalMovementKind.Bounce], false, true, true, false);
        }

        [Test]
        public void RejectsFloorClearanceConflictAtomically()
        {
            var fixture = BuildFixture();
            var invalid = ReplaceFirstEdge(fixture.Contract, edge =>
            {
                var overlap = edge.Envelope.Clearance[0];
                return CopyEdge(edge, envelope: new TraversalEnvelope(
                    edge.Envelope.Centerline,
                    new[] { overlap },
                    edge.Envelope.Clearance,
                    edge.Envelope.JumpArc,
                    edge.Envelope.DropColumn,
                    edge.Envelope.Landing,
                    edge.Envelope.Recovery));
            });
            AssertFailure(CompileInvalidSource(invalid, fixture),
                TerrainClusterTraversalCompileErrorCode.FloorClearanceConflict);
        }

        [Test]
        public void RejectsOutOfBoundsEnvelopeTileAtomically()
        {
            var fixture = BuildFixture();
            var invalid = ReplaceFirstEdge(fixture.Contract, edge =>
                CopyEdge(edge, envelope: new TraversalEnvelope(
                    edge.Envelope.Centerline,
                    edge.Envelope.Floor,
                    edge.Envelope.Clearance.Concat(new[] { new LocalTileCoord(36, 4) }),
                    edge.Envelope.JumpArc,
                    edge.Envelope.DropColumn,
                    edge.Envelope.Landing,
                    edge.Envelope.Recovery)));
            AssertFailure(CompileInvalidSource(invalid, fixture),
                TerrainClusterTraversalCompileErrorCode.EnvelopeOutsideActiveMask);
        }

        [Test]
        public void PreservesEntryExitAndMandatoryReachability()
        {
            var fixture = BuildFixture();
            var result = CompileSuccess(fixture);
            foreach (var variant in result.Variants)
            {
                var entry = fixture.RoleSocket.RoleSpineLinks.Single(value =>
                    value.VariantId == variant.VariantId &&
                    value.ConnectionKind == ProjectedRoleConnectionKind.EntryPort);
                var exit = fixture.RoleSocket.RoleSpineLinks.Single(value =>
                    value.VariantId == variant.VariantId &&
                    value.ConnectionKind == ProjectedRoleConnectionKind.ExitPort);
                var reached = Reach(entry.TraversalNodeId, variant.Edges.Where(value => value.IsMandatory));
                Assert.That(reached, Does.Contain(exit.TraversalNodeId));
                Assert.That(variant.Nodes.Where(value => value.IsMandatory)
                    .All(value => reached.Contains(value.NodeId)), Is.True);
            }
        }

        [Test]
        public void PreservesMap1102RolePortNodeConnections()
        {
            var fixture = BuildFixture(ClusterFootprintTransform.MirrorX);
            var result = CompileSuccess(fixture);
            foreach (var link in fixture.RoleSocket.RoleSpineLinks)
            {
                var node = result.Nodes.Single(value =>
                    value.VariantId == link.VariantId && value.NodeId == link.TraversalNodeId);
                Assert.That(node.SourceCoordinate, Is.EqualTo(link.SourceCoordinate));
                Assert.That(node.CompiledCoordinate, Is.EqualTo(link.CompiledCoordinate));
                Assert.That(node.LinkedRoleAnchorIds, Does.Contain(link.RoleAnchorId));
                var index = node.LinkedRoleAnchorIds.ToList().IndexOf(link.RoleAnchorId);
                Assert.That(node.LinkedRoleKinds[index], Is.EqualTo(link.RoleKind));
            }
        }

        [Test]
        public void PublishesRouteSpineNodeEndpointAndCenterlineProvenance()
        {
            var result = CompileSuccess(BuildFixture());
            var variant = result.Variants.Single(value => value.IsBaseline);
            var node = variant.Nodes.Single(value => value.NodeId == "NODE_ENTRY");
            var edge = variant.Edges.Single(value => value.FromNodeId == node.NodeId);
            var tile = variant.ProtectedTiles.Single(value =>
                value.CompiledCoordinate == node.CompiledCoordinate);

            Assert.That(tile.Provenance.Any(value =>
                value.SourceKind == ClusterTraversalProtectionSourceKind.RouteSpine &&
                value.NodeId == node.NodeId && value.EdgeId.Length == 0), Is.True);
            Assert.That(tile.Provenance.Any(value =>
                value.SourceKind == ClusterTraversalProtectionSourceKind.RouteSpine &&
                value.EdgeId == edge.EdgeId && value.EnvelopeSetKind == null), Is.True);
            Assert.That(tile.Provenance.Any(value =>
                value.SourceKind == ClusterTraversalProtectionSourceKind.RouteSpine &&
                value.EdgeId == edge.EdgeId &&
                value.EnvelopeSetKind == CompiledTraversalEnvelopeSetKind.Centerline), Is.True);
        }

        [Test]
        public void PublishesSixTraversalEnvelopeProtectionSets()
        {
            var result = CompileSuccess(BuildFixture());
            foreach (var edge in result.Edges)
            {
                foreach (var tile in edge.Envelope.AllTiles.Where(value =>
                             value.SetKind != CompiledTraversalEnvelopeSetKind.Centerline))
                {
                    var protectedTile = result.ProtectedTiles.Single(value =>
                        value.CompiledCoordinate == tile.CompiledCoordinate);
                    Assert.That(protectedTile.Provenance.Any(value =>
                        value.SourceKind == ClusterTraversalProtectionSourceKind.TraversalEnvelope &&
                        value.VariantId == edge.VariantId && value.EdgeId == edge.EdgeId &&
                        value.EnvelopeSetKind == tile.SetKind), Is.True, tile.SetKind.ToString());
                }
            }
        }

        [Test]
        public void CoalescesSameCoordinateWithoutLosingVariantOrSourceProvenance()
        {
            var result = CompileSuccess(BuildFixture());
            var entryNodes = result.Nodes.Where(value => value.NodeId == "NODE_ENTRY").ToArray();
            Assert.That(entryNodes.Length, Is.EqualTo(2));
            var tile = result.ProtectedTiles.Single(value =>
                value.CompiledCoordinate == entryNodes[0].CompiledCoordinate);
            Assert.That(tile.Provenance.Select(value => value.VariantId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(tile.Provenance.Select(value => value.SourceKind).Distinct(),
                Does.Contain(ClusterTraversalProtectionSourceKind.RouteSpine));
            Assert.That(tile.Provenance.Count, Is.GreaterThan(2));
        }

        [Test]
        public void AccumulatesStableErrorsAndPublishesNoPartialArtifact()
        {
            var fixture = BuildFixture();
            var invalid = ReplaceFirstEdge(fixture.Contract, edge => new TraversalEdge(
                edge.EdgeId,
                edge.FromNodeId,
                edge.FromNodeId,
                (TraversalMovementKind)99,
                edge.StartTile,
                edge.EndTile,
                0,
                0,
                null,
                null,
                true,
                null));
            var result = CompileInvalidSource(invalid, fixture);
            Assert.That(result.Errors.Count, Is.GreaterThan(3));
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterTraversalCompileErrorCode.SelfEdge));
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterTraversalCompileErrorCode.InvalidMovement));
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterTraversalCompileErrorCode.InvalidClearance));
            AssertAtomicFailure(result);
        }

        [Test]
        public void MissingInputIsAtomic()
        {
            AssertFailure(TerrainClusterTraversalCompiler.Compile(null),
                TerrainClusterTraversalCompileErrorCode.MissingInput);
        }

        [Test]
        public void ArtifactDigestMismatchIsAtomic()
        {
            var fixture = BuildFixture();
            var request = new TerrainClusterTraversalCompileRequest(
                fixture.Contract,
                fixture.SourceDigest,
                fixture.Canvas,
                "bad-canvas-digest",
                fixture.RoleSocket,
                "bad-role-digest");
            var result = TerrainClusterTraversalCompiler.Compile(request);
            AssertFailure(result, TerrainClusterTraversalCompileErrorCode.ArtifactDigestMismatch);
            Assert.That(result.Errors.Count(value =>
                value.Code == TerrainClusterTraversalCompileErrorCode.ArtifactDigestMismatch),
                Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ArtifactIdentityMismatchIsAtomic()
        {
            var fixture = BuildFixture();
            var other = BuildFixture(id: new TerrainClusterId("TC_TRAVERSAL_OTHER"));
            var request = new TerrainClusterTraversalCompileRequest(
                fixture.Contract,
                fixture.SourceDigest,
                other.Canvas,
                other.Canvas.CanonicalDigest,
                fixture.RoleSocket,
                fixture.RoleSocket.CanonicalDigest);
            AssertFailure(TerrainClusterTraversalCompiler.Compile(request),
                TerrainClusterTraversalCompileErrorCode.ArtifactIdentityMismatch);
        }

        [Test]
        public void PublicationIsImmutableAndCanonical()
        {
            var result = CompileSuccess(BuildFixture(reverseInput: true));
            Assert.That(result.Variants.Select(value => value.VariantId),
                Is.EqualTo(result.Variants.Select(value => value.VariantId).OrderBy(value => value)));
            Assert.That(result.Nodes.Select(value => value.VariantId.Value + "/" + value.NodeId),
                Is.EqualTo(result.Nodes.Select(value => value.VariantId.Value + "/" + value.NodeId)
                    .OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(result.ProtectedTiles.Select(value => value.CompiledCoordinate),
                Is.EqualTo(result.ProtectedTiles.Select(value => value.CompiledCoordinate)
                    .OrderBy(value => value.Y).ThenBy(value => value.X)));
            Assert.That((result.Variants as IList<CompiledClusterSpineVariant>).IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() =>
                (result.Variants as IList<CompiledClusterSpineVariant>).Add(null));
            Assert.Throws<NotSupportedException>(() =>
                (result.ProtectedTiles as IList<ClusterTraversalProtectedTile>).Clear());
        }

        [Test]
        public void ReversedInputAndCultureChangePreserveArtifactAndDigest()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = CompileSuccess(BuildFixture());
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var reversed = CompileSuccess(BuildFixture(reverseInput: true));
                Assert.That(reversed.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(reversed.Nodes.Select(value => value.VariantId.Value + "/" + value.NodeId),
                    Is.EqualTo(first.Nodes.Select(value => value.VariantId.Value + "/" + value.NodeId)));
                Assert.That(reversed.Edges.Select(value => value.VariantId.Value + "/" + value.EdgeId),
                    Is.EqualTo(first.Edges.Select(value => value.VariantId.Value + "/" + value.EdgeId)));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void SemanticChangeChangesDigestWhileDisplayTextDoesNot()
        {
            var first = CompileSuccess(BuildFixture(displayText: "first"));
            var displayOnly = CompileSuccess(BuildFixture(displayText: "second"));
            var clearanceChanged = CompileSuccess(BuildFixture(clearanceWidth: 2));
            Assert.That(displayOnly.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
            Assert.That(clearanceChanged.CanonicalDigest, Is.Not.EqualTo(first.CanonicalDigest));
        }

        [Test]
        public void PublishesEveryRequiredStableErrorDistinction()
        {
            var expected = new[]
            {
                "MissingInput", "ArtifactIdentityMismatch", "ArtifactDigestMismatch",
                "InvalidSourceContract", "MissingVariant", "DuplicateNodeOrEdge",
                "NodeProjectionMissing", "NodeOutsideActiveMask", "MissingNodeReference",
                "SelfEdge", "EdgeAnchorMismatch", "InvalidMovement", "InvalidClearance",
                "LandingProjectionMissing", "RecoveryProjectionMissing",
                "EnvelopeProjectionMissing", "EnvelopeOutsideActiveMask",
                "MovementEnvelopeMismatch", "FloorClearanceConflict", "MissingEntryExitPath",
                "UnreachableMandatoryElement", "ProtectionProvenanceMismatch",
                "NonCanonicalPublication",
            };
            Assert.That(Enum.GetNames(typeof(TerrainClusterTraversalCompileErrorCode)),
                Is.EqualTo(expected));
        }

        [Test]
        public void RuntimeScopeHasNoPhysicsWitnessPatternSectorOrUnitySideEffects()
        {
            var files = new[]
            {
                "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterRouteSpine.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterTraversalEnvelope.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterTraversalCompiler.cs",
            };
            var source = string.Join("\n", files.Select(File.ReadAllText));
            var forbidden = new[]
            {
                "UnityEditor", "StageMapGenerator", "GridWorld", "RoomTemplate",
                "RoomGridTransform", "TileMutationService", "SectorRecipeResolver",
                "System.Random", "UnityEngine.Random", "Physics.", "Physics2D.",
                "MicroPatternPlanner", "MicroPatternOrderedRenderer", "SectorPlacement",
                "SectorCanvas", "Tilemap", "WorldGenerationRoot",
            };
            foreach (var symbol in forbidden)
            {
                Assert.That(source, Does.Not.Contain(symbol), symbol);
            }
        }

        private static Fixture BuildFixture(
            ClusterFootprintTransform transform = ClusterFootprintTransform.R0,
            bool reverseInput = false,
            int clearanceWidth = 1,
            TerrainClusterId? id = null,
            string displayText = "fixture")
        {
            var contract = CreateContract(reverseInput, clearanceWidth,
                id ?? new TerrainClusterId("TC_TRAVERSAL"), displayText);
            var validation = TerrainClusterContractValidator.Validate(contract);
            Assert.That(validation.IsValid, Is.True,
                string.Join("\n", validation.Errors.Select(value => value.ToString())));
            var canvasResult = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(contract, transform));
            Assert.That(canvasResult.IsSuccess, Is.True,
                string.Join("\n", canvasResult.Errors.Select(value => value.ToString())));
            var canvas = canvasResult.LocalCanvas;
            var roleResult = TerrainClusterRoleSocketCompiler.Compile(
                new TerrainClusterRoleSocketCompileRequest(
                    contract,
                    validation.CanonicalDigest,
                    canvas,
                    canvas.CanonicalDigest,
                    SocketEvidence(contract, canvas)));
            Assert.That(roleResult.IsSuccess, Is.True,
                string.Join("\n", roleResult.Errors.Select(value => value.ToString())));
            return new Fixture(contract, validation.CanonicalDigest, canvas, roleResult.Contract);
        }

        private static TerrainClusterTraversalCompileResult CompileSuccess(Fixture fixture)
        {
            var result = TerrainClusterTraversalCompiler.Compile(
                new TerrainClusterTraversalCompileRequest(
                    fixture.Contract,
                    fixture.SourceDigest,
                    fixture.Canvas,
                    fixture.Canvas.CanonicalDigest,
                    fixture.RoleSocket,
                    fixture.RoleSocket.CanonicalDigest));
            Assert.That(result.IsSuccess, Is.True,
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            Assert.That(result.Compilation, Is.Not.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            return result;
        }

        private static TerrainClusterTraversalCompileResult CompileInvalidSource(
            TerrainClusterContract invalid,
            Fixture validArtifacts)
        {
            return TerrainClusterTraversalCompiler.Compile(
                new TerrainClusterTraversalCompileRequest(
                    invalid,
                    validArtifacts.SourceDigest,
                    validArtifacts.Canvas,
                    validArtifacts.Canvas.CanonicalDigest,
                    validArtifacts.RoleSocket,
                    validArtifacts.RoleSocket.CanonicalDigest));
        }

        private static TerrainClusterContract CreateContract(
            bool reverseInput,
            int clearanceWidth,
            TerrainClusterId id,
            string displayText)
        {
            var roles = new[]
            {
                new ClusterRoleAnchor("ANCHOR_ENTRY", ClusterRoleKind.Entry,
                    new LocalTileCoord(0, 1), "NODE_ENTRY"),
                new ClusterRoleAnchor("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp,
                    new LocalTileCoord(5, 1), "NODE_BUILD_UP"),
                new ClusterRoleAnchor("ANCHOR_CORE", ClusterRoleKind.Core,
                    new LocalTileCoord(10, 1), "NODE_CORE"),
                new ClusterRoleAnchor("ANCHOR_RECOVERY", ClusterRoleKind.Recovery,
                    new LocalTileCoord(25, 1), "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_REWARD", ClusterRoleKind.Reward,
                    new LocalTileCoord(30, 1), "NODE_REWARD"),
                new ClusterRoleAnchor("ANCHOR_EXIT", ClusterRoleKind.Exit,
                    new LocalTileCoord(35, 1), "NODE_EXIT"),
            };
            var nodes = roles.Select(value => new TraversalNode(
                value.TraversalNodeId,
                value.Tile,
                value.Role != ClusterRoleKind.Reward,
                value.AnchorId)).Concat(new[]
            {
                new TraversalNode("NODE_STEP_A", new LocalTileCoord(15, 1), true, string.Empty),
                new TraversalNode("NODE_STEP_B", new LocalTileCoord(20, 1), true, string.Empty),
            }).ToArray();
            var byId = nodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var edges = new[]
            {
                CreateEdge("EDGE_01_WALK", byId["NODE_ENTRY"], byId["NODE_BUILD_UP"],
                    TraversalMovementKind.Walk, clearanceWidth),
                CreateEdge("EDGE_02_JUMP", byId["NODE_BUILD_UP"], byId["NODE_CORE"],
                    TraversalMovementKind.Jump, clearanceWidth),
                CreateEdge("EDGE_03_DROP", byId["NODE_CORE"], byId["NODE_STEP_A"],
                    TraversalMovementKind.Drop, clearanceWidth),
                CreateEdge("EDGE_04_CLIMB", byId["NODE_STEP_A"], byId["NODE_STEP_B"],
                    TraversalMovementKind.Climb, clearanceWidth),
                CreateEdge("EDGE_05_SLIDE", byId["NODE_STEP_B"], byId["NODE_RECOVERY"],
                    TraversalMovementKind.Slide, clearanceWidth),
                CreateEdge("EDGE_06_BOUNCE", byId["NODE_RECOVERY"], byId["NODE_EXIT"],
                    TraversalMovementKind.Bounce, clearanceWidth),
            };
            var firstNodes = reverseInput ? nodes.Reverse().ToArray() : nodes;
            var firstEdges = reverseInput ? edges.Reverse().ToArray() : edges;
            var variants = new[]
            {
                new SpineVariant(new SpineVariantId("SPINE_BASELINE"), true,
                    TraversalGraphKind.Traversal, firstNodes, firstEdges),
                new SpineVariant(new SpineVariantId("SPINE_ALTERNATE"), false,
                    TraversalGraphKind.Traversal, firstNodes, firstEdges),
            };
            if (reverseInput) variants = variants.Reverse().ToArray();
            var ports = new[]
            {
                new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true,
                    "ANCHOR_ENTRY", new LocalTileCoord(0, 1), ClusterPortSide.L,
                    new[] { 0, 1, 2, 3, 4 }),
                new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true,
                    "ANCHOR_EXIT", new LocalTileCoord(35, 1), ClusterPortSide.R,
                    new[] { 1, 2, 3, 4 }),
            };
            return new TerrainClusterContract(
                id,
                new ClusterFootprint(new[]
                {
                    new ClusterChunkCoord(0, 0),
                    new ClusterChunkCoord(1, 0),
                    new ClusterChunkCoord(2, 0),
                }),
                reverseInput ? roles.Reverse() : roles,
                reverseInput ? ports.Reverse() : ports,
                new TerrainClusterTraversalContract(variants),
                displayText);
        }

        private static TraversalEdge CreateEdge(
            string edgeId,
            TraversalNode from,
            TraversalNode to,
            TraversalMovementKind movement,
            int clearanceWidth)
        {
            var floor = movement == TraversalMovementKind.Walk ||
                movement == TraversalMovementKind.Slide
                    ? new[] { new LocalTileCoord(from.Tile.X, 0) }
                    : Array.Empty<LocalTileCoord>();
            var clearance = new[] { new LocalTileCoord(from.Tile.X, 4) };
            var jumpArc = movement == TraversalMovementKind.Jump ||
                movement == TraversalMovementKind.Bounce
                    ? new[] { new LocalTileCoord(from.Tile.X + 1, 5) }
                    : Array.Empty<LocalTileCoord>();
            var dropColumn = movement == TraversalMovementKind.Drop
                ? new[] { new LocalTileCoord(from.Tile.X + 1, 0) }
                : Array.Empty<LocalTileCoord>();
            var envelope = new TraversalEnvelope(
                new[] { from.Tile, to.Tile },
                floor,
                clearance,
                jumpArc,
                dropColumn,
                new[] { to.Tile },
                new[] { to.Tile });
            return new TraversalEdge(
                edgeId,
                from.NodeId,
                to.NodeId,
                movement,
                from.Tile,
                to.Tile,
                clearanceWidth,
                2,
                to.Tile,
                to.Tile,
                true,
                envelope);
        }

        private static TraversalEdge CopyEdge(
            TraversalEdge source,
            TraversalEnvelope envelope = null)
        {
            return new TraversalEdge(
                source.EdgeId,
                source.FromNodeId,
                source.ToNodeId,
                source.MovementKind,
                source.StartTile,
                source.EndTile,
                source.MinimumClearanceWidth,
                source.MinimumClearanceHeight,
                source.LandingTile,
                source.RecoveryTile,
                source.IsMandatory,
                envelope ?? source.Envelope,
                source.GraphKind);
        }

        private static TerrainClusterContract ReplaceFirstEdge(
            TerrainClusterContract source,
            Func<TraversalEdge, TraversalEdge> replace)
        {
            var variants = source.Traversal.Variants.Select((variant, variantIndex) =>
            {
                var edges = variant.Edges.Select((edge, edgeIndex) =>
                    variantIndex == 0 && edgeIndex == 0 ? replace(edge) : edge).ToArray();
                return new SpineVariant(
                    variant.Id, variant.IsBaseline, variant.GraphKind, variant.Nodes, edges);
            }).ToArray();
            return new TerrainClusterContract(
                source.Id,
                source.Footprint,
                source.RoleAnchors,
                source.Ports,
                new TerrainClusterTraversalContract(variants),
                source.DisplayText);
        }

        private static ClusterSectorSocketEvidence[] SocketEvidence(
            TerrainClusterContract contract,
            TerrainClusterLocalCanvas canvas)
        {
            return contract.Ports.Where(value => value.IsPrimary).Select(port =>
                new ClusterSectorSocketEvidence(
                    port.Kind == ClusterPortKind.Entry ? "SR_ENTRY" : "SR_EXIT",
                    port.Kind == ClusterPortKind.Entry ? "SOCKET_ENTRY" : "SOCKET_EXIT",
                    TransformSide(port.OutwardSide, canvas.Transform),
                    port.Kind == ClusterPortKind.Entry ? 2 : 3,
                    true,
                    port.Kind)).Reverse().ToArray();
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

        private static IReadOnlyList<LocalTileCoord> GetSourceSet(
            TraversalEnvelope envelope,
            CompiledTraversalEnvelopeSetKind kind)
        {
            switch (kind)
            {
                case CompiledTraversalEnvelopeSetKind.Centerline: return envelope.Centerline;
                case CompiledTraversalEnvelopeSetKind.Floor: return envelope.Floor;
                case CompiledTraversalEnvelopeSetKind.Clearance: return envelope.Clearance;
                case CompiledTraversalEnvelopeSetKind.JumpArc: return envelope.JumpArc;
                case CompiledTraversalEnvelopeSetKind.DropColumn: return envelope.DropColumn;
                case CompiledTraversalEnvelopeSetKind.Landing: return envelope.Landing;
                case CompiledTraversalEnvelopeSetKind.Recovery: return envelope.Recovery;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static void AssertRequiredAndEmpty(
            CompiledTraversalEnvelope envelope,
            bool floorRequired,
            bool clearanceRequired,
            bool jumpRequired,
            bool dropRequired)
        {
            Assert.That(envelope.Floor.Count != 0, Is.EqualTo(floorRequired));
            Assert.That(envelope.Clearance.Count != 0, Is.EqualTo(clearanceRequired));
            Assert.That(envelope.JumpArc.Count != 0, Is.EqualTo(jumpRequired));
            Assert.That(envelope.DropColumn.Count != 0, Is.EqualTo(dropRequired));
            Assert.That(envelope.Landing, Is.Not.Empty);
            Assert.That(envelope.Recovery, Is.Not.Empty);
        }

        private static HashSet<string> Reach(
            string start,
            IEnumerable<CompiledTraversalEdge> edges)
        {
            var outgoing = edges.GroupBy(value => value.FromNodeId)
                .ToDictionary(group => group.Key, group => group.ToArray());
            var reached = new HashSet<string>(StringComparer.Ordinal) { start };
            var queue = new Queue<string>(); queue.Enqueue(start);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                CompiledTraversalEdge[] candidates;
                if (!outgoing.TryGetValue(current, out candidates)) continue;
                foreach (var edge in candidates)
                    if (reached.Add(edge.ToNodeId)) queue.Enqueue(edge.ToNodeId);
            }
            return reached;
        }

        private static void AssertActive(
            TerrainClusterLocalCanvas canvas,
            LocalTileCoord coordinate)
        {
            CompiledClusterLocalTileCell cell;
            Assert.That(canvas.TryGetTileCell(coordinate, out cell), Is.True);
            Assert.That(cell.State, Is.EqualTo(ClusterChunkMaskState.Active));
        }

        private static void AssertFailure(
            TerrainClusterTraversalCompileResult result,
            TerrainClusterTraversalCompileErrorCode expected)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(expected),
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            AssertAtomicFailure(result);
        }

        private static void AssertAtomicFailure(TerrainClusterTraversalCompileResult result)
        {
            Assert.That(result.Compilation, Is.Null);
            Assert.That(result.Variants, Is.Empty);
            Assert.That(result.Nodes, Is.Empty);
            Assert.That(result.Edges, Is.Empty);
            Assert.That(result.Envelopes, Is.Empty);
            Assert.That(result.ProtectedTiles, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        private sealed class Fixture
        {
            public Fixture(
                TerrainClusterContract contract,
                string sourceDigest,
                TerrainClusterLocalCanvas canvas,
                TerrainClusterRoleSocketContract roleSocket)
            {
                Contract = contract;
                SourceDigest = sourceDigest;
                Canvas = canvas;
                RoleSocket = roleSocket;
            }

            public TerrainClusterContract Contract { get; }
            public string SourceDigest { get; }
            public TerrainClusterLocalCanvas Canvas { get; }
            public TerrainClusterRoleSocketContract RoleSocket { get; }
        }
    }
}
