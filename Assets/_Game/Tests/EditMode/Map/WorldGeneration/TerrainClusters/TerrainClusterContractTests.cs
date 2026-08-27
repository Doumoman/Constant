using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.TerrainClusters;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.TerrainClusters
{
    [TestFixture]
    [Category("MAP09_04")]
    public sealed class TerrainClusterContractTests
    {
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void StandardConnectedFootprintsPublish(int chunkCount)
        {
            var result = Validate(CreateValid(chunkCount));

            Assert.That(result.IsValid, Is.True, JoinErrors(result));
            Assert.That(result.Contract.Footprint.ActiveChunks, Has.Count.EqualTo(chunkCount));
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void SixChunkFootprintRequiresExactTerrainClusterIdAllowlist()
        {
            var contract = CreateValid(6);

            AssertError(
                TerrainClusterContractValidator.Validate(contract, Array.Empty<TerrainClusterId>()),
                TerrainClusterValidationErrorCode.SixChunkNotAllowlisted);
            Assert.That(
                TerrainClusterContractValidator.Validate(
                    contract,
                    new[] { new TerrainClusterId("TC_SIX_CHUNK") }).IsValid,
                Is.True);
            AssertError(
                TerrainClusterContractValidator.Validate(
                    contract,
                    new[] { new TerrainClusterId("TC_SIX_CHUNK_OTHER") }),
                TerrainClusterValidationErrorCode.SixChunkNotAllowlisted);
        }

        [TestCase(1)]
        [TestCase(7)]
        public void InvalidFootprintCountsAreRejected(int chunkCount)
        {
            AssertError(Validate(CreateValid(chunkCount)), TerrainClusterValidationErrorCode.InvalidFootprintCount);
        }

        [Test]
        public void DuplicateFootprintCellIsRejected()
        {
            var source = CreateValid();
            var chunks = source.Footprint.ActiveChunks.Concat(new[] { new ClusterChunkCoord(0, 0) });

            AssertError(
                Validate(Rebuild(source, footprint: new ClusterFootprint(chunks))),
                TerrainClusterValidationErrorCode.DuplicateOrDisconnectedFootprint);
        }

        [Test]
        public void DiagonalOnlyFootprintIsRejected()
        {
            var source = CreateValid();

            AssertError(
                Validate(Rebuild(
                    source,
                    footprint: new ClusterFootprint(new[]
                    {
                        new ClusterChunkCoord(0, 0),
                        new ClusterChunkCoord(1, 1),
                    }))),
                TerrainClusterValidationErrorCode.DuplicateOrDisconnectedFootprint);
        }

        [Test]
        public void NegativeOrUnnormalizedFootprintIsRejected()
        {
            var negative = CreateValid();
            var shifted = CreateValid();

            AssertError(
                Validate(Rebuild(negative, footprint: new ClusterFootprint(new[]
                {
                    new ClusterChunkCoord(-1, 0),
                    new ClusterChunkCoord(0, 0),
                }))),
                TerrainClusterValidationErrorCode.DuplicateOrDisconnectedFootprint);
            AssertError(
                Validate(Rebuild(shifted, footprint: new ClusterFootprint(new[]
                {
                    new ClusterChunkCoord(1, 1),
                    new ClusterChunkCoord(2, 1),
                }))),
                TerrainClusterValidationErrorCode.DuplicateOrDisconnectedFootprint);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("TC_")]
        [TestCase("tc_VALID")]
        [TestCase("TC-HYPHEN")]
        [TestCase("OTHER_VALID")]
        public void TerrainClusterIdUsesExactGrammar(string value)
        {
            var source = CreateValid();

            AssertError(
                Validate(Rebuild(source, id: new TerrainClusterId(value))),
                TerrainClusterValidationErrorCode.InvalidId);
        }

        [TestCase(ClusterRoleKind.Entry)]
        [TestCase(ClusterRoleKind.BuildUp)]
        [TestCase(ClusterRoleKind.Core)]
        [TestCase(ClusterRoleKind.Recovery)]
        [TestCase(ClusterRoleKind.Exit)]
        public void EveryRequiredRoleIsEnforced(ClusterRoleKind missing)
        {
            var source = CreateValid();
            var roles = source.RoleAnchors.Where(value => value.Role != missing).ToArray();

            AssertError(
                Validate(Rebuild(source, roles: roles)),
                TerrainClusterValidationErrorCode.MissingRequiredRole);
        }

        [Test]
        public void RewardRoleIsOptional()
        {
            var source = CreateValid();
            var roles = source.RoleAnchors.Where(value => value.Role != ClusterRoleKind.Reward).ToArray();
            var variant = source.Traversal.Variants[0];
            var nodes = variant.Nodes.Where(value => value.RoleAnchorId != "ANCHOR_REWARD").ToArray();
            var replacement = new SpineVariant(
                variant.Id,
                variant.IsBaseline,
                variant.GraphKind,
                nodes,
                variant.Edges);

            var result = Validate(Rebuild(source, roles: roles, variants: new[] { replacement }));

            Assert.That(result.IsValid, Is.True, JoinErrors(result));
        }

        [Test]
        public void RoleAnchorMustHaveUniqueStableIdNodeAndOwnedTile()
        {
            var source = CreateValid();
            var roles = source.RoleAnchors.Concat(new[]
            {
                new ClusterRoleAnchor("ANCHOR_ENTRY", (ClusterRoleKind)999, new LocalTileCoord(99, 99), "BAD_NODE"),
            });

            AssertError(
                Validate(Rebuild(source, roles: roles)),
                TerrainClusterValidationErrorCode.InvalidRoleAnchor);
        }

        [Test]
        public void EntryAndExitAnchorsMustBeDistinct()
        {
            var source = CreateValid();
            var entry = source.RoleAnchors.Single(value => value.Role == ClusterRoleKind.Entry);
            var roles = source.RoleAnchors
                .Where(value => value.Role != ClusterRoleKind.Exit)
                .Concat(new[]
                {
                    new ClusterRoleAnchor("ANCHOR_EXIT", ClusterRoleKind.Exit, entry.Tile, entry.TraversalNodeId),
                });

            AssertError(
                Validate(Rebuild(source, roles: roles)),
                TerrainClusterValidationErrorCode.InvalidRoleAnchor);
        }

        [TestCase(ClusterPortKind.Entry)]
        [TestCase(ClusterPortKind.Exit)]
        public void ExactOnePrimaryEntryAndExitPortIsRequired(ClusterPortKind removed)
        {
            var source = CreateValid();
            var ports = source.Ports.Where(value => value.Kind != removed).ToArray();

            AssertError(
                Validate(Rebuild(source, ports: ports)),
                TerrainClusterValidationErrorCode.DuplicatePrimaryPort);
        }

        [Test]
        public void DuplicatePrimaryPortIsRejected()
        {
            var source = CreateValid();
            var entry = source.Ports.Single(value => value.Kind == ClusterPortKind.Entry);
            var ports = source.Ports.Concat(new[]
            {
                new ClusterPort(
                    "PORT_ENTRY_SECOND",
                    ClusterPortKind.Entry,
                    true,
                    entry.RoleAnchorId,
                    entry.Tile,
                    entry.OutwardSide,
                    entry.CompatibleRouteTypes),
            });

            AssertError(
                Validate(Rebuild(source, ports: ports)),
                TerrainClusterValidationErrorCode.DuplicatePrimaryPort);
        }

        [Test]
        public void PortSideMustPointOutwardFromOwnedBoundaryTile()
        {
            var source = CreateValid();
            var entry = source.Ports.Single(value => value.Kind == ClusterPortKind.Entry);
            var replacement = new ClusterPort(
                entry.PortId,
                entry.Kind,
                entry.IsPrimary,
                entry.RoleAnchorId,
                entry.Tile,
                ClusterPortSide.R,
                entry.CompatibleRouteTypes);

            AssertError(
                Validate(Rebuild(source, ports: ReplacePort(source, replacement))),
                TerrainClusterValidationErrorCode.InvalidPortDirection);
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void PortCompatibilityUsesOnlyExistingIntegerRouteTypesZeroThroughFour(int routeType)
        {
            var source = CreateValid();
            var entry = source.Ports.Single(value => value.Kind == ClusterPortKind.Entry);
            var replacement = new ClusterPort(
                entry.PortId,
                entry.Kind,
                entry.IsPrimary,
                entry.RoleAnchorId,
                entry.Tile,
                entry.OutwardSide,
                new[] { routeType });

            AssertError(
                Validate(Rebuild(source, ports: ReplacePort(source, replacement))),
                TerrainClusterValidationErrorCode.InvalidPort);
        }

        [Test]
        public void DuplicateRouteCompatibilityAndRoleMismatchAreRejected()
        {
            var source = CreateValid();
            var entry = source.Ports.Single(value => value.Kind == ClusterPortKind.Entry);
            var replacement = new ClusterPort(
                entry.PortId,
                entry.Kind,
                entry.IsPrimary,
                "ANCHOR_EXIT",
                entry.Tile,
                entry.OutwardSide,
                new[] { 1, 1 });

            AssertError(
                Validate(Rebuild(source, ports: ReplacePort(source, replacement))),
                TerrainClusterValidationErrorCode.InvalidPort);
        }

        [TestCase(TraversalGraphKind.Mechanism)]
        [TestCase(TraversalGraphKind.Progression)]
        [TestCase((TraversalGraphKind)999)]
        public void OnlyTraversalGraphKindCanBePublished(TraversalGraphKind graphKind)
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var replacement = new SpineVariant(
                variant.Id,
                variant.IsBaseline,
                graphKind,
                variant.Nodes,
                variant.Edges);

            AssertError(
                Validate(Rebuild(source, variants: new[] { replacement })),
                TerrainClusterValidationErrorCode.InvalidGraphKind);
        }

        [Test]
        public void MechanismOrProgressionNodeAndEdgeCannotEnterTraversalGraph()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var firstNode = variant.Nodes[0];
            var nodes = variant.Nodes.Select(value => value == firstNode
                ? new TraversalNode(value.NodeId, value.Tile, value.IsMandatory, value.RoleAnchorId, TraversalGraphKind.Mechanism)
                : value).ToArray();
            var firstEdge = variant.Edges[0];
            var edges = variant.Edges.Select(value => value == firstEdge
                ? CopyEdge(value, graphKind: TraversalGraphKind.Progression)
                : value).ToArray();

            AssertError(
                Validate(Rebuild(source, variants: new[]
                {
                    new SpineVariant(variant.Id, true, TraversalGraphKind.Traversal, nodes, edges),
                })),
                TerrainClusterValidationErrorCode.InvalidGraphKind);
        }

        [Test]
        public void MultipleVariantsAreAllowedWithExactlyOneBaseline()
        {
            var source = CreateValid();
            var baseline = source.Traversal.Variants[0];
            var alternate = new SpineVariant(
                new SpineVariantId("SPINE_ALTERNATE"),
                false,
                TraversalGraphKind.Traversal,
                baseline.Nodes,
                baseline.Edges);

            var result = Validate(Rebuild(source, variants: new[] { alternate, baseline }));

            Assert.That(result.IsValid, Is.True, JoinErrors(result));
            Assert.That(result.Contract.Traversal.Variants.Select(value => value.Id.Value),
                Is.EqualTo(new[] { "SPINE_ALTERNATE", "SPINE_BASELINE" }));
        }

        [Test]
        public void DuplicateVariantAndInvalidBaselineCountAreRejected()
        {
            var source = CreateValid();
            var baseline = source.Traversal.Variants[0];
            var duplicate = new SpineVariant(
                baseline.Id,
                true,
                baseline.GraphKind,
                baseline.Nodes,
                baseline.Edges);

            var result = Validate(Rebuild(source, variants: new[] { baseline, duplicate }));

            AssertError(result, TerrainClusterValidationErrorCode.DuplicateVariant);
            AssertError(result, TerrainClusterValidationErrorCode.InvalidBaselineVariant);
        }

        [Test]
        public void EmptyTraversalHasNoBaselineAndCannotPublish()
        {
            AssertError(
                Validate(Rebuild(CreateValid(), variants: Array.Empty<SpineVariant>())),
                TerrainClusterValidationErrorCode.InvalidBaselineVariant);
        }

        [Test]
        public void ExactSixMovementKindsArePublished()
        {
            Assert.That(
                Enum.GetValues(typeof(TraversalMovementKind)).Cast<TraversalMovementKind>(),
                Is.EqualTo(new[]
                {
                    TraversalMovementKind.Walk,
                    TraversalMovementKind.Jump,
                    TraversalMovementKind.Drop,
                    TraversalMovementKind.Climb,
                    TraversalMovementKind.Slide,
                    TraversalMovementKind.Bounce,
                }));
        }

        [TestCase(TraversalMovementKind.Walk)]
        [TestCase(TraversalMovementKind.Jump)]
        [TestCase(TraversalMovementKind.Drop)]
        [TestCase(TraversalMovementKind.Climb)]
        [TestCase(TraversalMovementKind.Slide)]
        [TestCase(TraversalMovementKind.Bounce)]
        public void EveryMovementAcceptsItsExactEnvelopeMatrix(TraversalMovementKind movement)
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var edges = variant.Edges.Select(value => CopyEdge(
                value,
                movement: movement,
                envelope: CreateEnvelope(movement, value.StartTile, value.EndTile, value.RecoveryTile.Value))).ToArray();
            var replacement = new SpineVariant(variant.Id, true, variant.GraphKind, variant.Nodes, edges);

            var result = Validate(Rebuild(source, variants: new[] { replacement }));

            Assert.That(result.IsValid, Is.True, JoinErrors(result));
        }

        [Test]
        public void UndefinedMovementIsRejected()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var edges = ReplaceFirstEdge(variant, CopyEdge(variant.Edges[0], movement: (TraversalMovementKind)999));

            AssertError(
                Validate(WithEdges(source, edges)),
                TerrainClusterValidationErrorCode.InvalidMovement);
        }

        [Test]
        public void DuplicateNodeAndEdgeIdsAreRejected()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var nodes = variant.Nodes.Concat(new[] { variant.Nodes[0] }).ToArray();
            var edges = variant.Edges.Concat(new[] { variant.Edges[0] }).ToArray();
            var replacement = new SpineVariant(variant.Id, true, variant.GraphKind, nodes, edges);

            AssertError(
                Validate(Rebuild(source, variants: new[] { replacement })),
                TerrainClusterValidationErrorCode.DuplicateNodeOrEdge);
        }

        [Test]
        public void MissingNodeReferenceAndSelfEdgeAreRejected()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var first = variant.Edges[0];
            var missing = new TraversalEdge(
                first.EdgeId,
                "NODE_MISSING",
                "NODE_MISSING",
                first.MovementKind,
                first.StartTile,
                first.EndTile,
                first.MinimumClearanceWidth,
                first.MinimumClearanceHeight,
                first.LandingTile,
                first.RecoveryTile,
                first.IsMandatory,
                first.Envelope);

            var result = Validate(WithEdges(source, ReplaceFirstEdge(variant, missing)));

            AssertError(result, TerrainClusterValidationErrorCode.MissingNodeReference);
            AssertError(result, TerrainClusterValidationErrorCode.SelfEdge);
        }

        [Test]
        public void EdgeStartAndEndMustMatchReferencedNodeTiles()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var first = variant.Edges[0];
            var replacement = new TraversalEdge(
                first.EdgeId,
                first.FromNodeId,
                first.ToNodeId,
                first.MovementKind,
                new LocalTileCoord(1, 6),
                first.EndTile,
                first.MinimumClearanceWidth,
                first.MinimumClearanceHeight,
                first.LandingTile,
                first.RecoveryTile,
                first.IsMandatory,
                first.Envelope);

            AssertError(
                Validate(WithEdges(source, ReplaceFirstEdge(variant, replacement))),
                TerrainClusterValidationErrorCode.EdgeAnchorMismatch);
        }

        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(-1, -1)]
        public void ClearanceDimensionsMustBePositive(int width, int height)
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var first = variant.Edges[0];
            var replacement = new TraversalEdge(
                first.EdgeId,
                first.FromNodeId,
                first.ToNodeId,
                first.MovementKind,
                first.StartTile,
                first.EndTile,
                width,
                height,
                first.LandingTile,
                first.RecoveryTile,
                first.IsMandatory,
                first.Envelope);

            AssertError(
                Validate(WithEdges(source, ReplaceFirstEdge(variant, replacement))),
                TerrainClusterValidationErrorCode.InvalidClearance);
        }

        [Test]
        public void LandingAndRecoveryMustBeExplicitAndOwned()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var first = variant.Edges[0];
            var replacement = new TraversalEdge(
                first.EdgeId,
                first.FromNodeId,
                first.ToNodeId,
                first.MovementKind,
                first.StartTile,
                first.EndTile,
                1,
                1,
                null,
                new LocalTileCoord(100, 100),
                true,
                first.Envelope);

            var result = Validate(WithEdges(source, ReplaceFirstEdge(variant, replacement)));

            AssertError(result, TerrainClusterValidationErrorCode.InvalidLanding);
            AssertError(result, TerrainClusterValidationErrorCode.InvalidRecovery);
        }

        [Test]
        public void MandatoryEntryToExitPathIsRequired()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var edges = variant.Edges.Select((value, index) => index == 1
                ? CopyEdge(value, mandatory: false)
                : value).ToArray();

            AssertError(
                Validate(WithEdges(source, edges)),
                TerrainClusterValidationErrorCode.MissingEntryExitPath);
        }

        [Test]
        public void OrphanMandatoryNodeAndEdgeAreRejected()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var a = new TraversalNode("NODE_ORPHAN_A", new LocalTileCoord(2, 6), true, string.Empty);
            var b = new TraversalNode("NODE_ORPHAN_B", new LocalTileCoord(3, 6), true, string.Empty);
            var orphan = CreateEdge(
                "EDGE_ORPHAN",
                a,
                b,
                TraversalMovementKind.Walk,
                true,
                new LocalTileCoord(3, 6));
            var replacement = new SpineVariant(
                variant.Id,
                true,
                variant.GraphKind,
                variant.Nodes.Concat(new[] { a, b }),
                variant.Edges.Concat(new[] { orphan }));

            AssertError(
                Validate(Rebuild(source, variants: new[] { replacement })),
                TerrainClusterValidationErrorCode.UnreachableMandatoryElement);
        }

        [Test]
        public void EnvelopeSetsRejectDuplicatesAndOutOfFootprintTiles()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var first = variant.Edges[0];
            var envelope = new TraversalEnvelope(
                new[] { first.StartTile, first.EndTile, first.EndTile },
                new[] { new LocalTileCoord(0, 0) },
                new[] { first.StartTile, new LocalTileCoord(100, 100) },
                Array.Empty<LocalTileCoord>(),
                Array.Empty<LocalTileCoord>(),
                new[] { first.EndTile },
                new[] { first.RecoveryTile.Value });

            AssertError(
                Validate(WithEdges(source, ReplaceFirstEdge(variant, CopyEdge(first, envelope: envelope)))),
                TerrainClusterValidationErrorCode.InvalidEnvelopeSet);
        }

        [Test]
        public void CenterlineClearanceLandingAndRecoveryProtectionAreRequired()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var first = variant.Edges[0];
            var envelope = new TraversalEnvelope(
                new[] { first.StartTile },
                new[] { new LocalTileCoord(0, 0) },
                Array.Empty<LocalTileCoord>(),
                Array.Empty<LocalTileCoord>(),
                Array.Empty<LocalTileCoord>(),
                Array.Empty<LocalTileCoord>(),
                Array.Empty<LocalTileCoord>());

            AssertError(
                Validate(WithEdges(source, ReplaceFirstEdge(variant, CopyEdge(first, envelope: envelope)))),
                TerrainClusterValidationErrorCode.InvalidEnvelopeSet);
        }

        [Test]
        public void FloorAndClearanceCannotOwnTheSameTile()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var first = variant.Edges[0];
            var conflict = first.StartTile;
            var envelope = new TraversalEnvelope(
                new[] { first.StartTile, first.EndTile },
                new[] { conflict },
                new[] { conflict, first.EndTile },
                Array.Empty<LocalTileCoord>(),
                Array.Empty<LocalTileCoord>(),
                new[] { first.EndTile },
                new[] { first.RecoveryTile.Value });

            AssertError(
                Validate(WithEdges(source, ReplaceFirstEdge(variant, CopyEdge(first, envelope: envelope)))),
                TerrainClusterValidationErrorCode.FloorClearanceConflict);
        }

        [TestCase(TraversalMovementKind.Walk)]
        [TestCase(TraversalMovementKind.Jump)]
        [TestCase(TraversalMovementKind.Drop)]
        [TestCase(TraversalMovementKind.Climb)]
        [TestCase(TraversalMovementKind.Slide)]
        [TestCase(TraversalMovementKind.Bounce)]
        public void MovementEnvelopeMismatchIsRejected(TraversalMovementKind movement)
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var first = variant.Edges[0];
            var forbiddenJumpArc = movement == TraversalMovementKind.Climb
                ? new[] { new LocalTileCoord(first.StartTile.X, first.StartTile.Y + 1) }
                : Array.Empty<LocalTileCoord>();
            var invalid = new TraversalEnvelope(
                new[] { first.StartTile, first.EndTile },
                Array.Empty<LocalTileCoord>(),
                new[] { first.StartTile, first.EndTile },
                forbiddenJumpArc,
                Array.Empty<LocalTileCoord>(),
                new[] { first.EndTile },
                new[] { first.RecoveryTile.Value });

            AssertError(
                Validate(WithEdges(source, ReplaceFirstEdge(
                    variant,
                    CopyEdge(first, movement: movement, envelope: invalid)))),
                TerrainClusterValidationErrorCode.MovementEnvelopeMismatch);
        }

        [Test]
        public void ProtectedTilesAreCanonicalUnionOfAllSevenSets()
        {
            var edge = CreateValid().Traversal.Variants[0].Edges[1];

            Assert.That(edge.Envelope.ProtectedTiles, Is.Ordered.By("Y").Then.By("X"));
            Assert.That(edge.Envelope.ProtectedTiles, Does.Contain(edge.StartTile));
            Assert.That(edge.Envelope.ProtectedTiles, Does.Contain(edge.EndTile));
            Assert.That(edge.Envelope.ProtectedTiles, Does.Contain(edge.LandingTile.Value));
            Assert.That(edge.Envelope.ProtectedTiles, Does.Contain(edge.RecoveryTile.Value));
            Assert.That(edge.Envelope.ProtectedTiles.Distinct().Count(), Is.EqualTo(edge.Envelope.ProtectedTiles.Count));
        }

        [Test]
        public void CallerCollectionsCannotMutatePublishedContractOrDigest()
        {
            var chunks = new List<ClusterChunkCoord>
            {
                new ClusterChunkCoord(0, 0),
                new ClusterChunkCoord(1, 0),
            };
            var source = CreateValid();
            var contract = Rebuild(source, footprint: new ClusterFootprint(chunks));
            var before = Validate(contract);

            chunks.Clear();

            var after = Validate(contract);
            Assert.That(after.IsValid, Is.True, JoinErrors(after));
            Assert.That(after.Contract.Footprint.ActiveChunks, Has.Count.EqualTo(2));
            Assert.That(after.CanonicalDigest, Is.EqualTo(before.CanonicalDigest));
        }

        [Test]
        public void InputOrderCultureAndDisplayTextDoNotAffectDigest()
        {
            var source = CreateValid();
            var reversed = new TerrainClusterContract(
                source.Id,
                new ClusterFootprint(source.Footprint.ActiveChunks.Reverse()),
                source.RoleAnchors.Reverse(),
                source.Ports.Reverse(),
                new TerrainClusterTraversalContract(source.Traversal.Variants.Reverse()),
                "different display text");
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                Assert.That(Validate(reversed).CanonicalDigest, Is.EqualTo(Validate(source).CanonicalDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [Test]
        public void EveryPublishedSemanticChangesCanonicalDigest()
        {
            var source = CreateValid();
            var variant = source.Traversal.Variants[0];
            var first = variant.Edges[0];
            var changed = new TraversalEdge(
                first.EdgeId,
                first.FromNodeId,
                first.ToNodeId,
                first.MovementKind,
                first.StartTile,
                first.EndTile,
                first.MinimumClearanceWidth + 1,
                first.MinimumClearanceHeight,
                first.LandingTile,
                first.RecoveryTile,
                first.IsMandatory,
                first.Envelope);

            Assert.That(
                Validate(WithEdges(source, ReplaceFirstEdge(variant, changed))).CanonicalDigest,
                Is.Not.EqualTo(Validate(source).CanonicalDigest));
        }

        [Test]
        public void ContractObjectsExposeNoWritablePublicProperties()
        {
            var types = new[]
            {
                typeof(ClusterFootprint), typeof(ClusterRoleAnchor), typeof(ClusterPort),
                typeof(TraversalNode), typeof(TraversalEdge), typeof(TraversalEnvelope),
                typeof(SpineVariant), typeof(TerrainClusterTraversalContract), typeof(TerrainClusterContract),
            };

            Assert.That(types.SelectMany(value => value.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                .Where(value => value.CanWrite), Is.Empty);
        }

        [Test]
        public void ValidationErrorsAreAccumulatedStableSortedAndNoPartialContractIsPublished()
        {
            var source = CreateValid();
            var broken = new TerrainClusterContract(
                new TerrainClusterId("bad"),
                new ClusterFootprint(new[] { new ClusterChunkCoord(1, 1) }),
                Array.Empty<ClusterRoleAnchor>(),
                Array.Empty<ClusterPort>(),
                new TerrainClusterTraversalContract(Array.Empty<SpineVariant>()));

            var first = Validate(broken);
            var second = Validate(broken);

            Assert.That(first.IsValid, Is.False);
            Assert.That(first.Contract, Is.Null);
            Assert.That(first.CanonicalDigest, Is.Empty);
            Assert.That(first.Errors, Is.Ordered);
            Assert.That(first.Errors.Distinct().Count(), Is.EqualTo(first.Errors.Count));
            Assert.That(second.Errors.Select(value => value.ToString()),
                Is.EqualTo(first.Errors.Select(value => value.ToString())));
            Assert.That(first.Errors.Count, Is.GreaterThan(5));
        }

        [Test]
        public void RuntimeScopeHasNoRngFileLifecycleOrForbiddenExecutionSymbols()
        {
            var runtimePath = Path.Combine(
                Application.dataPath,
                "_Game/Map/Runtime/WorldGeneration/TerrainClusters");
            var source = string.Join("\n", Directory.GetFiles(runtimePath, "*.cs")
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(File.ReadAllText));

            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "UnityEngine", "System.Random", "File.", "Directory.",
                         "MonoBehaviour", "Awake(", "Start(", "Update(", "StageMapGenerator",
                         "GridWorld", "RoomTemplate", "RoomGridTransform", "TileMutationService",
                         "SectorRecipeResolver", "MechanismGraph", "ProgressionGraph",
                     })
            {
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
            }

            Assert.That(source, Does.Not.Match(@"\benum\s+RouteType\b"));
            Assert.That(source, Does.Not.Match(@"\benum\s+AccessClass\b"));
            Assert.That(source, Does.Not.Match(@"\benum\s+PacingRole\b"));
            Assert.That(source, Does.Not.Match(@"\bstruct\s+LocalTileCoord\b"));
        }

        private static TerrainClusterValidationResult Validate(TerrainClusterContract contract)
        {
            return TerrainClusterContractValidator.Validate(
                contract,
                contract != null && contract.Footprint != null && contract.Footprint.ActiveChunks.Count == 6
                    ? new[] { contract.Id }
                    : Array.Empty<TerrainClusterId>());
        }

        private static TerrainClusterContract CreateValid(int chunkCount = 2)
        {
            var id = new TerrainClusterId(chunkCount == 6 ? "TC_SIX_CHUNK" : "TC_LIVE_BASELINE");
            var chunks = Enumerable.Range(0, Math.Max(0, chunkCount))
                .Select(value => new ClusterChunkCoord(value, 0))
                .ToArray();
            var maxX = Math.Max(1, chunkCount * WorldGenConstants.MicroChunkWidthTiles - 1);
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
                CreateEdge("EDGE_ENTRY_BUILD", byId["NODE_ENTRY"], byId["NODE_BUILD_UP"], TraversalMovementKind.Walk, true, byId["NODE_BUILD_UP"].Tile),
                CreateEdge("EDGE_BUILD_CORE", byId["NODE_BUILD_UP"], byId["NODE_CORE"], TraversalMovementKind.Jump, true, byId["NODE_BUILD_UP"].Tile),
                CreateEdge("EDGE_CORE_RECOVERY", byId["NODE_CORE"], byId["NODE_RECOVERY"], TraversalMovementKind.Drop, true, byId["NODE_CORE"].Tile),
                CreateEdge("EDGE_RECOVERY_EXIT", byId["NODE_RECOVERY"], byId["NODE_EXIT"], TraversalMovementKind.Slide, true, byId["NODE_RECOVERY"].Tile),
            };
            var variant = new SpineVariant(
                new SpineVariantId("SPINE_BASELINE"),
                true,
                TraversalGraphKind.Traversal,
                nodes,
                edges);
            var ports = new[]
            {
                new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true, "ANCHOR_ENTRY",
                    roleData[0].Tile, ClusterPortSide.L, new[] { 0, 1, 2, 3, 4 }),
                new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true, "ANCHOR_EXIT",
                    roleData[5].Tile, ClusterPortSide.R, new[] { 1, 2, 3, 4 }),
            };
            return new TerrainClusterContract(
                id,
                new ClusterFootprint(chunks),
                roleData,
                ports,
                new TerrainClusterTraversalContract(new[] { variant }),
                "Fixture display text");
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
            bool mandatory,
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
                mandatory,
                CreateEnvelope(movement, from.Tile, to.Tile, recovery));
        }

        private static TraversalEnvelope CreateEnvelope(
            TraversalMovementKind movement,
            LocalTileCoord start,
            LocalTileCoord end,
            LocalTileCoord recovery)
        {
            var floor = movement == TraversalMovementKind.Walk || movement == TraversalMovementKind.Slide
                ? new[] { new LocalTileCoord(start.X, 0) }
                : Array.Empty<LocalTileCoord>();
            var jump = movement == TraversalMovementKind.Jump || movement == TraversalMovementKind.Bounce
                ? new[] { new LocalTileCoord((start.X + end.X) / 2, Math.Min(7, Math.Max(start.Y, end.Y) + 2)) }
                : Array.Empty<LocalTileCoord>();
            var drop = movement == TraversalMovementKind.Drop
                ? new[] { new LocalTileCoord((start.X + end.X) / 2, Math.Min(7, Math.Max(start.Y, end.Y) + 1)) }
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

        private static TraversalEdge CopyEdge(
            TraversalEdge source,
            TraversalMovementKind? movement = null,
            bool? mandatory = null,
            TraversalEnvelope envelope = null,
            TraversalGraphKind? graphKind = null)
        {
            return new TraversalEdge(
                source.EdgeId,
                source.FromNodeId,
                source.ToNodeId,
                movement ?? source.MovementKind,
                source.StartTile,
                source.EndTile,
                source.MinimumClearanceWidth,
                source.MinimumClearanceHeight,
                source.LandingTile,
                source.RecoveryTile,
                mandatory ?? source.IsMandatory,
                envelope ?? source.Envelope,
                graphKind ?? source.GraphKind);
        }

        private static IEnumerable<ClusterPort> ReplacePort(
            TerrainClusterContract source,
            ClusterPort replacement)
        {
            return source.Ports.Select(value => value.PortId == replacement.PortId ? replacement : value);
        }

        private static IEnumerable<TraversalEdge> ReplaceFirstEdge(
            SpineVariant variant,
            TraversalEdge replacement)
        {
            var firstId = variant.Edges[0].EdgeId;
            return variant.Edges.Select(value => value.EdgeId == firstId ? replacement : value);
        }

        private static TerrainClusterContract WithEdges(
            TerrainClusterContract source,
            IEnumerable<TraversalEdge> edges)
        {
            var variant = source.Traversal.Variants[0];
            return Rebuild(source, variants: new[]
            {
                new SpineVariant(variant.Id, variant.IsBaseline, variant.GraphKind, variant.Nodes, edges),
            });
        }

        private static TerrainClusterContract Rebuild(
            TerrainClusterContract source,
            TerrainClusterId? id = null,
            ClusterFootprint footprint = null,
            IEnumerable<ClusterRoleAnchor> roles = null,
            IEnumerable<ClusterPort> ports = null,
            IEnumerable<SpineVariant> variants = null)
        {
            return new TerrainClusterContract(
                id ?? source.Id,
                footprint ?? source.Footprint,
                roles ?? source.RoleAnchors,
                ports ?? source.Ports,
                new TerrainClusterTraversalContract(variants ?? source.Traversal.Variants),
                source.DisplayText);
        }

        private static void AssertError(
            TerrainClusterValidationResult result,
            TerrainClusterValidationErrorCode code)
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), JoinErrors(result));
        }

        private static string JoinErrors(TerrainClusterValidationResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }
    }
}
