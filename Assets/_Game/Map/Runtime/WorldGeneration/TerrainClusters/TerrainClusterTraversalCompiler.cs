using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public static class TerrainClusterTraversalCompiler
    {
        public const string RulesetVersion = "MAP11_03_ROUTE_SPINE_TRAVERSAL_ENVELOPE_V1";

        public static TerrainClusterTraversalCompileResult Compile(
            TerrainClusterTraversalCompileRequest request)
        {
            var errors = new List<TerrainClusterTraversalCompileError>();
            if (request == null)
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.MissingInput,
                    "request", "Compile request is required.");
                return Failure(errors);
            }

            if (request.SourceContract == null)
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.MissingInput,
                    "request.sourceContract", "Validated MAP09_04 contract is required.");
            }

            if (request.LocalCanvas == null)
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.MissingInput,
                    "request.localCanvas", "Successful MAP11_01 Local Canvas is required.");
            }

            if (request.RoleSocketContract == null)
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.MissingInput,
                    "request.roleSocketContract", "Successful MAP11_02 role/socket contract is required.");
            }

            TerrainClusterValidationResult sourceValidation = null;
            if (request.SourceContract != null)
            {
                sourceValidation = TerrainClusterContractValidator.Validate(
                    request.SourceContract,
                    request.SixChunkAllowlist);
                foreach (var sourceError in sourceValidation.Errors)
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.InvalidSourceContract,
                        "source." + sourceError.Path,
                        sourceError.Code + ":" + sourceError.Detail);
                    AddTranslatedSourceError(errors, sourceError);
                }

                if (sourceValidation.IsValid && !string.Equals(
                        sourceValidation.CanonicalDigest,
                        request.SourceContractCanonicalDigest,
                        StringComparison.Ordinal))
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.ArtifactDigestMismatch,
                        "request.sourceContractCanonicalDigest",
                        "Provided digest differs from the MAP09_04 canonical digest.");
                }
            }

            TerrainClusterFootprintCompileResult expectedCanvasResult = null;
            if (sourceValidation != null && sourceValidation.IsValid && request.LocalCanvas != null)
            {
                expectedCanvasResult = TerrainClusterFootprintCompiler.Compile(
                    new TerrainClusterFootprintCompileRequest(
                        sourceValidation.Contract,
                        request.LocalCanvas.Transform,
                        request.SixChunkAllowlist));
                if (!expectedCanvasResult.IsSuccess)
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.ArtifactIdentityMismatch,
                        "request.localCanvas", "MAP11_01 could not reproduce the provided Local Canvas.");
                }
                else
                {
                    ValidateCanvasBinding(
                        sourceValidation.Contract,
                        expectedCanvasResult.LocalCanvas,
                        request.LocalCanvas,
                        request.LocalCanvasCanonicalDigest,
                        errors);
                }
            }

            if (sourceValidation != null && sourceValidation.IsValid &&
                request.LocalCanvas != null && request.RoleSocketContract != null)
            {
                ValidateRoleSocketBinding(
                    sourceValidation,
                    request.LocalCanvas,
                    request.RoleSocketContract,
                    request.RoleSocketContractCanonicalDigest,
                    request.SixChunkAllowlist,
                    errors);
            }

            if (errors.Count != 0)
            {
                return Failure(errors);
            }

            var variants = new List<CompiledClusterSpineVariant>();
            foreach (var sourceVariant in sourceValidation.Contract.Traversal.Variants
                         .OrderBy(value => value.Id))
            {
                variants.Add(CompileVariant(
                    sourceVariant,
                    request.LocalCanvas,
                    request.RoleSocketContract,
                    errors));
            }

            if (variants.Count == 0)
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.MissingVariant,
                    "variants", "At least one SpineVariant is required.");
            }

            var protectedTiles = CoalesceProtection(
                variants.SelectMany(value => value.ProtectedTiles)
                    .SelectMany(value => value.Provenance));
            ValidateProtection(variants, protectedTiles, request.LocalCanvas, errors);
            ValidateCanonicalPublication(variants, protectedTiles, errors);
            if (errors.Count != 0)
            {
                return Failure(errors);
            }

            var digest = ComputeDigest(
                sourceValidation.Contract.Id,
                sourceValidation.CanonicalDigest,
                request.LocalCanvas.CanonicalDigest,
                request.RoleSocketContract.CanonicalDigest,
                request.LocalCanvas.Transform,
                variants,
                protectedTiles);
            var compilation = new TerrainClusterTraversalCompilation(
                sourceValidation.Contract.Id,
                sourceValidation.CanonicalDigest,
                request.LocalCanvas.CanonicalDigest,
                request.RoleSocketContract.CanonicalDigest,
                request.LocalCanvas.Transform,
                variants,
                protectedTiles,
                digest);
            return new TerrainClusterTraversalCompileResult(compilation, errors);
        }

        private static void AddTranslatedSourceError(
            ICollection<TerrainClusterTraversalCompileError> errors,
            TerrainClusterValidationError sourceError)
        {
            TerrainClusterTraversalCompileErrorCode? code = null;
            switch (sourceError.Code)
            {
                case TerrainClusterValidationErrorCode.InvalidBaselineVariant:
                case TerrainClusterValidationErrorCode.DuplicateVariant:
                    code = TerrainClusterTraversalCompileErrorCode.MissingVariant;
                    break;
                case TerrainClusterValidationErrorCode.DuplicateNodeOrEdge:
                    code = TerrainClusterTraversalCompileErrorCode.DuplicateNodeOrEdge;
                    break;
                case TerrainClusterValidationErrorCode.InvalidRoleAnchor:
                    if (sourceError.Path.IndexOf(".nodes[", StringComparison.Ordinal) >= 0)
                        code = TerrainClusterTraversalCompileErrorCode.NodeOutsideActiveMask;
                    break;
                case TerrainClusterValidationErrorCode.MissingNodeReference:
                    code = TerrainClusterTraversalCompileErrorCode.MissingNodeReference;
                    break;
                case TerrainClusterValidationErrorCode.SelfEdge:
                    code = TerrainClusterTraversalCompileErrorCode.SelfEdge;
                    break;
                case TerrainClusterValidationErrorCode.EdgeAnchorMismatch:
                    code = TerrainClusterTraversalCompileErrorCode.EdgeAnchorMismatch;
                    break;
                case TerrainClusterValidationErrorCode.InvalidMovement:
                    code = TerrainClusterTraversalCompileErrorCode.InvalidMovement;
                    break;
                case TerrainClusterValidationErrorCode.InvalidClearance:
                    code = TerrainClusterTraversalCompileErrorCode.InvalidClearance;
                    break;
                case TerrainClusterValidationErrorCode.InvalidLanding:
                    code = TerrainClusterTraversalCompileErrorCode.LandingProjectionMissing;
                    break;
                case TerrainClusterValidationErrorCode.InvalidRecovery:
                    code = TerrainClusterTraversalCompileErrorCode.RecoveryProjectionMissing;
                    break;
                case TerrainClusterValidationErrorCode.InvalidEnvelopeSet:
                    code = sourceError.Path.IndexOf(".envelope.", StringComparison.Ordinal) >= 0
                        ? TerrainClusterTraversalCompileErrorCode.EnvelopeOutsideActiveMask
                        : TerrainClusterTraversalCompileErrorCode.EnvelopeProjectionMissing;
                    break;
                case TerrainClusterValidationErrorCode.MovementEnvelopeMismatch:
                    code = TerrainClusterTraversalCompileErrorCode.MovementEnvelopeMismatch;
                    break;
                case TerrainClusterValidationErrorCode.FloorClearanceConflict:
                    code = TerrainClusterTraversalCompileErrorCode.FloorClearanceConflict;
                    break;
                case TerrainClusterValidationErrorCode.MissingEntryExitPath:
                    code = TerrainClusterTraversalCompileErrorCode.MissingEntryExitPath;
                    break;
                case TerrainClusterValidationErrorCode.UnreachableMandatoryElement:
                    code = TerrainClusterTraversalCompileErrorCode.UnreachableMandatoryElement;
                    break;
            }

            if (code.HasValue)
            {
                Add(errors, code.Value, "source." + sourceError.Path, sourceError.Detail);
            }
        }

        private static void ValidateCanvasBinding(
            TerrainClusterContract sourceContract,
            TerrainClusterLocalCanvas expected,
            TerrainClusterLocalCanvas actual,
            string providedDigest,
            ICollection<TerrainClusterTraversalCompileError> errors)
        {
            if (sourceContract.Id != actual.ClusterId ||
                !string.Equals(expected.SourceFootprintDigest, actual.SourceFootprintDigest, StringComparison.Ordinal) ||
                !CanvasMappingsEqual(expected, actual))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.ArtifactIdentityMismatch,
                    "request.localCanvas",
                    "Cluster identity, footprint, transform, bounds, or source mapping differs.");
            }

            if (!string.Equals(actual.CanonicalDigest, providedDigest, StringComparison.Ordinal) ||
                !string.Equals(expected.CanonicalDigest, actual.CanonicalDigest, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.ArtifactDigestMismatch,
                    "request.localCanvasCanonicalDigest",
                    "Provided or reproduced MAP11_01 digest differs.");
            }
        }

        private static bool CanvasMappingsEqual(
            TerrainClusterLocalCanvas left,
            TerrainClusterLocalCanvas right)
        {
            if (left.Transform != right.Transform ||
                left.ChunkWidth != right.ChunkWidth || left.ChunkHeight != right.ChunkHeight ||
                left.TileWidth != right.TileWidth || left.TileHeight != right.TileHeight ||
                left.ChunkCells.Count != right.ChunkCells.Count ||
                left.TileCells.Count != right.TileCells.Count)
            {
                return false;
            }

            for (var index = 0; index < left.ChunkCells.Count; index++)
            {
                var a = left.ChunkCells[index];
                var b = right.ChunkCells[index];
                if (a.CanonicalIndex != b.CanonicalIndex || a.Coordinate != b.Coordinate ||
                    a.State != b.State || a.SourceCoordinate != b.SourceCoordinate)
                {
                    return false;
                }
            }

            for (var index = 0; index < left.TileCells.Count; index++)
            {
                var a = left.TileCells[index];
                var b = right.TileCells[index];
                if (a.CanonicalIndex != b.CanonicalIndex || a.Coordinate != b.Coordinate ||
                    a.OwningChunk != b.OwningChunk ||
                    a.WithinChunkCoordinate != b.WithinChunkCoordinate ||
                    a.State != b.State || a.SourceChunkCoordinate != b.SourceChunkCoordinate ||
                    a.SourceCoordinate != b.SourceCoordinate)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidateRoleSocketBinding(
            TerrainClusterValidationResult sourceValidation,
            TerrainClusterLocalCanvas canvas,
            TerrainClusterRoleSocketContract actual,
            string providedDigest,
            IEnumerable<TerrainClusterId> sixChunkAllowlist,
            ICollection<TerrainClusterTraversalCompileError> errors)
        {
            if (actual.ClusterId != sourceValidation.Contract.Id ||
                actual.Transform != canvas.Transform ||
                !string.Equals(actual.SourceContractDigest, sourceValidation.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(actual.LocalCanvasDigest, canvas.CanonicalDigest, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.ArtifactIdentityMismatch,
                    "request.roleSocketContract",
                    "MAP11_02 cluster, source, Canvas, or transform identity differs.");
            }

            if (!string.Equals(actual.CanonicalDigest, providedDigest, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.ArtifactDigestMismatch,
                    "request.roleSocketContractCanonicalDigest",
                    "Provided digest differs from the MAP11_02 published digest.");
            }

            var expectedResult = TerrainClusterRoleSocketCompiler.Compile(
                new TerrainClusterRoleSocketCompileRequest(
                    sourceValidation.Contract,
                    sourceValidation.CanonicalDigest,
                    canvas,
                    canvas.CanonicalDigest,
                    actual.SocketConnections.Select(value => value.Evidence),
                    sixChunkAllowlist));
            if (!expectedResult.IsSuccess)
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.ArtifactIdentityMismatch,
                    "request.roleSocketContract",
                    "MAP11_02 could not reproduce the provided role/socket contract.");
                return;
            }

            if (!string.Equals(expectedResult.CanonicalDigest, actual.CanonicalDigest, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.ArtifactDigestMismatch,
                    "request.roleSocketContractCanonicalDigest",
                    "Reproduced MAP11_02 digest differs.");
            }

            if (!RoleSocketContractsEqual(expectedResult.Contract, actual))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.ArtifactIdentityMismatch,
                    "request.roleSocketContract",
                    "Projected role, port, node link, or socket publication differs.");
            }
        }

        private static bool RoleSocketContractsEqual(
            TerrainClusterRoleSocketContract left,
            TerrainClusterRoleSocketContract right)
        {
            if (left.ClusterId != right.ClusterId || left.Transform != right.Transform ||
                left.Roles.Count != right.Roles.Count || left.Ports.Count != right.Ports.Count ||
                left.RoleSpineLinks.Count != right.RoleSpineLinks.Count ||
                left.SocketConnections.Count != right.SocketConnections.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Roles.Count; index++)
            {
                var a = left.Roles[index]; var b = right.Roles[index];
                if (!string.Equals(a.AnchorId, b.AnchorId, StringComparison.Ordinal) ||
                    a.Role != b.Role || a.SourceCoordinate != b.SourceCoordinate ||
                    a.CompiledCoordinate != b.CompiledCoordinate ||
                    a.OwningCompiledChunk != b.OwningCompiledChunk ||
                    !string.Equals(a.TraversalNodeId, b.TraversalNodeId, StringComparison.Ordinal))
                    return false;
            }

            for (var index = 0; index < left.Ports.Count; index++)
            {
                var a = left.Ports[index]; var b = right.Ports[index];
                if (!string.Equals(a.PortId, b.PortId, StringComparison.Ordinal) ||
                    a.Kind != b.Kind || a.SourceCoordinate != b.SourceCoordinate ||
                    a.CompiledCoordinate != b.CompiledCoordinate ||
                    a.SourceOutwardSide != b.SourceOutwardSide ||
                    a.CompiledOutwardSide != b.CompiledOutwardSide ||
                    !string.Equals(a.RoleAnchorId, b.RoleAnchorId, StringComparison.Ordinal) ||
                    a.RoleKind != b.RoleKind ||
                    !a.CompatibleRouteTypes.SequenceEqual(b.CompatibleRouteTypes))
                    return false;
            }

            for (var index = 0; index < left.RoleSpineLinks.Count; index++)
            {
                var a = left.RoleSpineLinks[index]; var b = right.RoleSpineLinks[index];
                if (a.VariantId != b.VariantId || a.IsBaseline != b.IsBaseline ||
                    !string.Equals(a.RoleAnchorId, b.RoleAnchorId, StringComparison.Ordinal) ||
                    a.RoleKind != b.RoleKind ||
                    !string.Equals(a.TraversalNodeId, b.TraversalNodeId, StringComparison.Ordinal) ||
                    a.SourceCoordinate != b.SourceCoordinate ||
                    a.CompiledCoordinate != b.CompiledCoordinate ||
                    a.ConnectionKind != b.ConnectionKind)
                    return false;
            }

            for (var index = 0; index < left.SocketConnections.Count; index++)
            {
                var a = left.SocketConnections[index]; var b = right.SocketConnections[index];
                if (a.PortKind != b.PortKind ||
                    !string.Equals(a.Evidence.StableIdentity, b.Evidence.StableIdentity, StringComparison.Ordinal) ||
                    a.Evidence.Side != b.Evidence.Side ||
                    a.Evidence.OwningRouteType != b.Evidence.OwningRouteType ||
                    a.Evidence.MandatoryAllowed != b.Evidence.MandatoryAllowed ||
                    !string.Equals(a.PortId, b.PortId, StringComparison.Ordinal) ||
                    !string.Equals(a.RoleAnchorId, b.RoleAnchorId, StringComparison.Ordinal) ||
                    !string.Equals(a.TraversalNodeId, b.TraversalNodeId, StringComparison.Ordinal) ||
                    a.CompiledCoordinate != b.CompiledCoordinate ||
                    a.CompiledOutwardSide != b.CompiledOutwardSide)
                    return false;
            }

            return true;
        }

        private static CompiledClusterSpineVariant CompileVariant(
            SpineVariant source,
            TerrainClusterLocalCanvas canvas,
            TerrainClusterRoleSocketContract roleSocket,
            ICollection<TerrainClusterTraversalCompileError> errors)
        {
            var nodes = new List<CompiledTraversalNode>();
            foreach (var sourceNode in source.Nodes.OrderBy(value => value.NodeId, StringComparer.Ordinal))
            {
                var path = NodePath(source.Id, sourceNode.NodeId);
                LocalTileCoord compiled;
                if (!canvas.TryGetCompiledTile(sourceNode.Tile, out compiled))
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.NodeProjectionMissing,
                        path, Coordinate(sourceNode.Tile));
                    continue;
                }

                CompiledClusterLocalTileCell tileCell;
                CompiledClusterChunkCell chunkCell;
                if (!canvas.TryGetTileCell(compiled, out tileCell))
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.NodeProjectionMissing,
                        path, Coordinate(compiled));
                    continue;
                }

                if (tileCell.State != ClusterChunkMaskState.Active ||
                    !canvas.TryGetChunkCell(tileCell.OwningChunk, out chunkCell) ||
                    chunkCell.State != ClusterChunkMaskState.Active)
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.NodeOutsideActiveMask,
                        path, Coordinate(compiled));
                }

                var roleLinks = roleSocket.RoleSpineLinks
                    .Where(value => value.VariantId == source.Id &&
                        string.Equals(value.TraversalNodeId, sourceNode.NodeId, StringComparison.Ordinal))
                    .OrderBy(value => value.RoleAnchorId, StringComparer.Ordinal)
                    .ToArray();
                if (roleLinks.Any(value => value.SourceCoordinate != sourceNode.Tile ||
                        value.CompiledCoordinate != compiled) ||
                    (sourceNode.RoleAnchorId.Length != 0 &&
                     !roleLinks.Any(value => string.Equals(
                         value.RoleAnchorId, sourceNode.RoleAnchorId, StringComparison.Ordinal))))
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.ArtifactIdentityMismatch,
                        path + ".roleLinks", "MAP11_02 role/node coordinate or identity differs.");
                }

                nodes.Add(new CompiledTraversalNode(
                    source.Id,
                    sourceNode.NodeId,
                    sourceNode.Tile,
                    compiled,
                    tileCell.OwningChunk,
                    sourceNode.IsMandatory,
                    sourceNode.GraphKind,
                    sourceNode.RoleAnchorId,
                    roleLinks));
            }

            var nodeMap = nodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var edges = new List<CompiledTraversalEdge>();
            foreach (var sourceEdge in source.Edges.OrderBy(value => value.EdgeId, StringComparer.Ordinal))
            {
                var edge = CompileEdge(source.Id, sourceEdge, nodeMap, canvas, errors);
                if (edge != null) edges.Add(edge);
            }

            ValidateReachability(source.Id, nodes, edges, roleSocket, errors);
            var protection = BuildVariantProtection(source.Id, nodes, edges);
            return new CompiledClusterSpineVariant(
                source.Id, source.IsBaseline, source.GraphKind, nodes, edges, protection);
        }

        private static CompiledTraversalEdge CompileEdge(
            SpineVariantId variantId,
            TraversalEdge source,
            IReadOnlyDictionary<string, CompiledTraversalNode> nodes,
            TerrainClusterLocalCanvas canvas,
            ICollection<TerrainClusterTraversalCompileError> errors)
        {
            var path = EdgePath(variantId, source.EdgeId);
            CompiledTraversalNode from;
            CompiledTraversalNode to;
            if (!nodes.TryGetValue(source.FromNodeId, out from) ||
                !nodes.TryGetValue(source.ToNodeId, out to))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.MissingNodeReference,
                    path, source.FromNodeId + "->" + source.ToNodeId);
                return null;
            }

            if (string.Equals(source.FromNodeId, source.ToNodeId, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.SelfEdge, path, source.FromNodeId);
            }

            LocalTileCoord compiledStart;
            LocalTileCoord compiledEnd;
            if (!canvas.TryGetCompiledTile(source.StartTile, out compiledStart) ||
                !canvas.TryGetCompiledTile(source.EndTile, out compiledEnd))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.EdgeAnchorMismatch,
                    path, "Start/End projection is missing.");
                return null;
            }

            if (source.StartTile != from.SourceCoordinate || source.EndTile != to.SourceCoordinate ||
                compiledStart != from.CompiledCoordinate || compiledEnd != to.CompiledCoordinate)
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.EdgeAnchorMismatch,
                    path, "Start/End must exactly equal compiled From/To node coordinates.");
            }

            if (source.MovementKind < TraversalMovementKind.Walk ||
                source.MovementKind > TraversalMovementKind.Bounce)
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.InvalidMovement,
                    path, Number((int)source.MovementKind));
            }

            if (source.MinimumClearanceWidth < 1 || source.MinimumClearanceHeight < 1)
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.InvalidClearance,
                    path, Number(source.MinimumClearanceWidth) + "x" + Number(source.MinimumClearanceHeight));
            }

            LocalTileCoord compiledLanding;
            LocalTileCoord compiledRecovery;
            var sourceLanding = source.LandingTile.GetValueOrDefault();
            var sourceRecovery = source.RecoveryTile.GetValueOrDefault();
            if (!source.LandingTile.HasValue ||
                !TryProjectActive(sourceLanding, canvas, out compiledLanding))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.LandingProjectionMissing,
                    path, source.LandingTile.HasValue ? Coordinate(sourceLanding) : "missing");
                compiledLanding = default(LocalTileCoord);
            }

            if (!source.RecoveryTile.HasValue ||
                !TryProjectActive(sourceRecovery, canvas, out compiledRecovery))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.RecoveryProjectionMissing,
                    path, source.RecoveryTile.HasValue ? Coordinate(sourceRecovery) : "missing");
                compiledRecovery = default(LocalTileCoord);
            }

            var envelope = CompileEnvelope(
                variantId,
                source,
                compiledStart,
                compiledEnd,
                compiledLanding,
                compiledRecovery,
                canvas,
                errors);
            return new CompiledTraversalEdge(
                variantId,
                source.EdgeId,
                source.FromNodeId,
                source.ToNodeId,
                source.MovementKind,
                source.StartTile,
                compiledStart,
                source.EndTile,
                compiledEnd,
                source.MinimumClearanceWidth,
                source.MinimumClearanceHeight,
                sourceLanding,
                compiledLanding,
                sourceRecovery,
                compiledRecovery,
                source.IsMandatory,
                source.GraphKind,
                envelope);
        }

        private static CompiledTraversalEnvelope CompileEnvelope(
            SpineVariantId variantId,
            TraversalEdge source,
            LocalTileCoord compiledStart,
            LocalTileCoord compiledEnd,
            LocalTileCoord compiledLanding,
            LocalTileCoord compiledRecovery,
            TerrainClusterLocalCanvas canvas,
            ICollection<TerrainClusterTraversalCompileError> errors)
        {
            var path = EdgePath(variantId, source.EdgeId) + ".envelope";
            if (source.Envelope == null)
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.EnvelopeProjectionMissing,
                    path, "TraversalEnvelope is required.");
                return new CompiledTraversalEnvelope(null, null, null, null, null, null, null);
            }

            var centerline = ProjectEnvelopeSet(source.Envelope.Centerline,
                CompiledTraversalEnvelopeSetKind.Centerline, path, canvas, errors);
            var floor = ProjectEnvelopeSet(source.Envelope.Floor,
                CompiledTraversalEnvelopeSetKind.Floor, path, canvas, errors);
            var clearance = ProjectEnvelopeSet(source.Envelope.Clearance,
                CompiledTraversalEnvelopeSetKind.Clearance, path, canvas, errors);
            var jumpArc = ProjectEnvelopeSet(source.Envelope.JumpArc,
                CompiledTraversalEnvelopeSetKind.JumpArc, path, canvas, errors);
            var dropColumn = ProjectEnvelopeSet(source.Envelope.DropColumn,
                CompiledTraversalEnvelopeSetKind.DropColumn, path, canvas, errors);
            var landing = ProjectEnvelopeSet(source.Envelope.Landing,
                CompiledTraversalEnvelopeSetKind.Landing, path, canvas, errors);
            var recovery = ProjectEnvelopeSet(source.Envelope.Recovery,
                CompiledTraversalEnvelopeSetKind.Recovery, path, canvas, errors);
            var compiled = new CompiledTraversalEnvelope(
                centerline, floor, clearance, jumpArc, dropColumn, landing, recovery);

            if (compiled.Centerline.Count == 0 ||
                !compiled.Centerline.Any(value => value.CompiledCoordinate == compiledStart) ||
                !compiled.Centerline.Any(value => value.CompiledCoordinate == compiledEnd) ||
                compiled.Clearance.Count == 0 ||
                !compiled.Landing.Any(value => value.CompiledCoordinate == compiledLanding) ||
                !compiled.Recovery.Any(value => value.CompiledCoordinate == compiledRecovery))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.EnvelopeProjectionMissing,
                    path, "Centerline, clearance, landing, or recovery publication is incomplete.");
            }

            if (compiled.Floor.Select(value => value.CompiledCoordinate)
                .Intersect(compiled.Clearance.Select(value => value.CompiledCoordinate)).Any())
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.FloorClearanceConflict,
                    path, "Floor and Clearance cannot own the same compiled tile.");
            }

            if (!MatchesMovementEnvelope(source.MovementKind, compiled))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.MovementEnvelopeMismatch,
                    path, source.MovementKind.ToString());
            }

            return compiled;
        }

        private static List<CompiledTraversalEnvelopeTile> ProjectEnvelopeSet(
            IReadOnlyList<LocalTileCoord> source,
            CompiledTraversalEnvelopeSetKind setKind,
            string path,
            TerrainClusterLocalCanvas canvas,
            ICollection<TerrainClusterTraversalCompileError> errors)
        {
            var result = new List<CompiledTraversalEnvelopeTile>();
            var seenSource = new HashSet<LocalTileCoord>();
            var seenCompiled = new HashSet<LocalTileCoord>();
            foreach (var coordinate in source ?? Array.Empty<LocalTileCoord>())
            {
                LocalTileCoord compiled;
                if (!seenSource.Add(coordinate) || !canvas.TryGetCompiledTile(coordinate, out compiled))
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.EnvelopeProjectionMissing,
                        path + "." + setKind, Coordinate(coordinate));
                    continue;
                }

                CompiledClusterLocalTileCell tileCell;
                CompiledClusterChunkCell chunkCell;
                if (!canvas.TryGetTileCell(compiled, out tileCell) ||
                    tileCell.State != ClusterChunkMaskState.Active ||
                    !canvas.TryGetChunkCell(tileCell.OwningChunk, out chunkCell) ||
                    chunkCell.State != ClusterChunkMaskState.Active)
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.EnvelopeOutsideActiveMask,
                        path + "." + setKind, Coordinate(compiled));
                    continue;
                }

                if (!seenCompiled.Add(compiled))
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.EnvelopeProjectionMissing,
                        path + "." + setKind, "Compiled coordinate is duplicated: " + Coordinate(compiled));
                }

                result.Add(new CompiledTraversalEnvelopeTile(
                    setKind, coordinate, compiled, tileCell.OwningChunk));
            }

            if (result.Count != (source == null ? 0 : source.Count))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.EnvelopeProjectionMissing,
                    path + "." + setKind,
                    CountDetail(result.Count, source == null ? 0 : source.Count));
            }

            return result.OrderBy(value => value.CompiledCoordinate.Y)
                .ThenBy(value => value.CompiledCoordinate.X).ToList();
        }

        private static bool MatchesMovementEnvelope(
            TraversalMovementKind movement,
            CompiledTraversalEnvelope envelope)
        {
            var noJumpOrDrop = envelope.JumpArc.Count == 0 && envelope.DropColumn.Count == 0;
            switch (movement)
            {
                case TraversalMovementKind.Walk:
                    return envelope.Floor.Count != 0 && envelope.Clearance.Count != 0 &&
                        envelope.Landing.Count != 0 && envelope.Recovery.Count != 0 && noJumpOrDrop;
                case TraversalMovementKind.Jump:
                    return envelope.Clearance.Count != 0 && envelope.JumpArc.Count != 0 &&
                        envelope.Landing.Count != 0 && envelope.Recovery.Count != 0 &&
                        envelope.DropColumn.Count == 0;
                case TraversalMovementKind.Drop:
                    return envelope.Clearance.Count != 0 && envelope.DropColumn.Count != 0 &&
                        envelope.Landing.Count != 0 && envelope.Recovery.Count != 0 &&
                        envelope.JumpArc.Count == 0;
                case TraversalMovementKind.Climb:
                    return envelope.Clearance.Count != 0 && envelope.Landing.Count != 0 &&
                        envelope.Recovery.Count != 0 && noJumpOrDrop;
                case TraversalMovementKind.Slide:
                    return envelope.Floor.Count != 0 && envelope.Clearance.Count != 0 &&
                        envelope.Landing.Count != 0 && envelope.Recovery.Count != 0 && noJumpOrDrop;
                case TraversalMovementKind.Bounce:
                    return envelope.Clearance.Count != 0 && envelope.JumpArc.Count != 0 &&
                        envelope.Landing.Count != 0 && envelope.Recovery.Count != 0 &&
                        envelope.DropColumn.Count == 0;
                default:
                    return false;
            }
        }

        private static bool TryProjectActive(
            LocalTileCoord source,
            TerrainClusterLocalCanvas canvas,
            out LocalTileCoord compiled)
        {
            if (!canvas.TryGetCompiledTile(source, out compiled)) return false;
            CompiledClusterLocalTileCell tileCell;
            CompiledClusterChunkCell chunkCell;
            return canvas.TryGetTileCell(compiled, out tileCell) &&
                tileCell.State == ClusterChunkMaskState.Active &&
                canvas.TryGetChunkCell(tileCell.OwningChunk, out chunkCell) &&
                chunkCell.State == ClusterChunkMaskState.Active;
        }

        private static void ValidateReachability(
            SpineVariantId variantId,
            IReadOnlyList<CompiledTraversalNode> nodes,
            IReadOnlyList<CompiledTraversalEdge> edges,
            TerrainClusterRoleSocketContract roleSocket,
            ICollection<TerrainClusterTraversalCompileError> errors)
        {
            var entryLinks = roleSocket.RoleSpineLinks.Where(value =>
                value.VariantId == variantId &&
                value.ConnectionKind == ProjectedRoleConnectionKind.EntryPort).ToArray();
            var exitLinks = roleSocket.RoleSpineLinks.Where(value =>
                value.VariantId == variantId &&
                value.ConnectionKind == ProjectedRoleConnectionKind.ExitPort).ToArray();
            if (entryLinks.Length != 1 || exitLinks.Length != 1 ||
                !nodes.Any(value => value.NodeId == entryLinks.FirstOrDefault()?.TraversalNodeId) ||
                !nodes.Any(value => value.NodeId == exitLinks.FirstOrDefault()?.TraversalNodeId))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.MissingEntryExitPath,
                    VariantPath(variantId), "MAP11_02 Entry/Exit node links are missing or ambiguous.");
                return;
            }

            var entryId = entryLinks[0].TraversalNodeId;
            var exitId = exitLinks[0].TraversalNodeId;
            var reachable = ReachableNodes(entryId, edges);
            var mandatoryReachable = ReachableNodes(
                entryId, edges.Where(value => value.IsMandatory).ToArray());
            if (!mandatoryReachable.Contains(exitId))
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.MissingEntryExitPath,
                    VariantPath(variantId), entryId + "->" + exitId);
            }

            foreach (var node in nodes.Where(value => value.IsMandatory))
            {
                if (!reachable.Contains(node.NodeId))
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.UnreachableMandatoryElement,
                        NodePath(variantId, node.NodeId), "Mandatory node is unreachable from Entry.");
                }
            }

            foreach (var edge in edges.Where(value => value.IsMandatory))
            {
                if (!reachable.Contains(edge.FromNodeId) || !reachable.Contains(edge.ToNodeId))
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.UnreachableMandatoryElement,
                        EdgePath(variantId, edge.EdgeId), "Mandatory edge belongs to an orphan component.");
                }
            }
        }

        private static HashSet<string> ReachableNodes(
            string start,
            IEnumerable<CompiledTraversalEdge> edges)
        {
            var outgoing = edges.GroupBy(value => value.FromNodeId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            var reached = new HashSet<string>(StringComparer.Ordinal) { start };
            var queue = new Queue<string>(); queue.Enqueue(start);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                CompiledTraversalEdge[] candidates;
                if (!outgoing.TryGetValue(current, out candidates)) continue;
                foreach (var edge in candidates)
                {
                    if (reached.Add(edge.ToNodeId)) queue.Enqueue(edge.ToNodeId);
                }
            }

            return reached;
        }

        private static IReadOnlyList<ClusterTraversalProtectedTile> BuildVariantProtection(
            SpineVariantId variantId,
            IEnumerable<CompiledTraversalNode> nodes,
            IEnumerable<CompiledTraversalEdge> edges)
        {
            var provenance = new List<ClusterTraversalProtectedTileProvenance>();
            foreach (var node in nodes)
            {
                provenance.Add(new ClusterTraversalProtectedTileProvenance(
                    ClusterTraversalProtectionSourceKind.RouteSpine,
                    variantId, node.NodeId, string.Empty, null,
                    node.SourceCoordinate, node.CompiledCoordinate, node.IsMandatory));
            }

            foreach (var edge in edges)
            {
                provenance.Add(new ClusterTraversalProtectedTileProvenance(
                    ClusterTraversalProtectionSourceKind.RouteSpine,
                    variantId, edge.FromNodeId, edge.EdgeId, null,
                    edge.SourceStartCoordinate, edge.CompiledStartCoordinate, edge.IsMandatory));
                provenance.Add(new ClusterTraversalProtectedTileProvenance(
                    ClusterTraversalProtectionSourceKind.RouteSpine,
                    variantId, edge.ToNodeId, edge.EdgeId, null,
                    edge.SourceEndCoordinate, edge.CompiledEndCoordinate, edge.IsMandatory));

                foreach (var tile in edge.Envelope.Centerline)
                {
                    provenance.Add(new ClusterTraversalProtectedTileProvenance(
                        ClusterTraversalProtectionSourceKind.RouteSpine,
                        variantId, string.Empty, edge.EdgeId,
                        CompiledTraversalEnvelopeSetKind.Centerline,
                        tile.SourceCoordinate, tile.CompiledCoordinate, edge.IsMandatory));
                }

                foreach (var tile in edge.Envelope.AllTiles.Where(value =>
                             value.SetKind != CompiledTraversalEnvelopeSetKind.Centerline))
                {
                    provenance.Add(new ClusterTraversalProtectedTileProvenance(
                        ClusterTraversalProtectionSourceKind.TraversalEnvelope,
                        variantId, string.Empty, edge.EdgeId, tile.SetKind,
                        tile.SourceCoordinate, tile.CompiledCoordinate, edge.IsMandatory));
                }
            }

            return CoalesceProtection(provenance);
        }

        private static IReadOnlyList<ClusterTraversalProtectedTile> CoalesceProtection(
            IEnumerable<ClusterTraversalProtectedTileProvenance> provenance)
        {
            return (provenance ?? Array.Empty<ClusterTraversalProtectedTileProvenance>())
                .Where(value => value != null)
                .GroupBy(value => value.CompiledCoordinate)
                .Select(group => new ClusterTraversalProtectedTile(group.Key, group))
                .OrderBy(value => value.CompiledCoordinate.Y)
                .ThenBy(value => value.CompiledCoordinate.X)
                .ToArray();
        }

        private static void ValidateProtection(
            IReadOnlyList<CompiledClusterSpineVariant> variants,
            IReadOnlyList<ClusterTraversalProtectedTile> protectedTiles,
            TerrainClusterLocalCanvas canvas,
            ICollection<TerrainClusterTraversalCompileError> errors)
        {
            foreach (var tile in protectedTiles)
            {
                if (tile.Provenance.Count == 0 ||
                    !tile.Provenance.SequenceEqual(tile.Provenance.OrderBy(value => value)))
                {
                    Add(errors, TerrainClusterTraversalCompileErrorCode.ProtectionProvenanceMismatch,
                        ProtectionPath(tile.CompiledCoordinate), "Provenance is empty or non-canonical.");
                }

                foreach (var item in tile.Provenance)
                {
                    LocalTileCoord expected;
                    var routeValid = item.SourceKind == ClusterTraversalProtectionSourceKind.RouteSpine &&
                        (item.EnvelopeSetKind == null ||
                         item.EnvelopeSetKind == CompiledTraversalEnvelopeSetKind.Centerline) &&
                        (item.NodeId.Length != 0 || item.EdgeId.Length != 0);
                    var envelopeValid = item.SourceKind == ClusterTraversalProtectionSourceKind.TraversalEnvelope &&
                        item.EdgeId.Length != 0 && item.EnvelopeSetKind.HasValue &&
                        item.EnvelopeSetKind != CompiledTraversalEnvelopeSetKind.Centerline;
                    if (item.CompiledCoordinate != tile.CompiledCoordinate ||
                        !canvas.TryGetCompiledTile(item.SourceCoordinate, out expected) ||
                        expected != item.CompiledCoordinate || (!routeValid && !envelopeValid))
                    {
                        Add(errors, TerrainClusterTraversalCompileErrorCode.ProtectionProvenanceMismatch,
                            ProtectionPath(tile.CompiledCoordinate), "Source kind, identity, or coordinate mapping differs.");
                    }
                }
            }

            foreach (var variant in variants)
            {
                foreach (var node in variant.Nodes)
                {
                    if (!HasProvenance(protectedTiles, node.CompiledCoordinate,
                            ClusterTraversalProtectionSourceKind.RouteSpine,
                            variant.VariantId, node.NodeId, string.Empty, null))
                    {
                        Add(errors, TerrainClusterTraversalCompileErrorCode.ProtectionProvenanceMismatch,
                            NodePath(variant.VariantId, node.NodeId), "RouteSpine node provenance is missing.");
                    }
                }

                foreach (var edge in variant.Edges)
                {
                    if (!HasProvenance(protectedTiles, edge.CompiledStartCoordinate,
                            ClusterTraversalProtectionSourceKind.RouteSpine,
                            variant.VariantId, edge.FromNodeId, edge.EdgeId, null) ||
                        !HasProvenance(protectedTiles, edge.CompiledEndCoordinate,
                            ClusterTraversalProtectionSourceKind.RouteSpine,
                            variant.VariantId, edge.ToNodeId, edge.EdgeId, null))
                    {
                        Add(errors, TerrainClusterTraversalCompileErrorCode.ProtectionProvenanceMismatch,
                            EdgePath(variant.VariantId, edge.EdgeId), "RouteSpine endpoint provenance is missing.");
                    }

                    foreach (var envelopeTile in edge.Envelope.AllTiles)
                    {
                        var sourceKind = envelopeTile.SetKind == CompiledTraversalEnvelopeSetKind.Centerline
                            ? ClusterTraversalProtectionSourceKind.RouteSpine
                            : ClusterTraversalProtectionSourceKind.TraversalEnvelope;
                        if (!HasProvenance(protectedTiles, envelopeTile.CompiledCoordinate,
                                sourceKind, variant.VariantId, string.Empty, edge.EdgeId,
                                envelopeTile.SetKind))
                        {
                            Add(errors, TerrainClusterTraversalCompileErrorCode.ProtectionProvenanceMismatch,
                                EdgePath(variant.VariantId, edge.EdgeId),
                                envelopeTile.SetKind + " provenance is missing.");
                        }
                    }
                }
            }
        }

        private static bool HasProvenance(
            IEnumerable<ClusterTraversalProtectedTile> tiles,
            LocalTileCoord coordinate,
            ClusterTraversalProtectionSourceKind sourceKind,
            SpineVariantId variantId,
            string nodeId,
            string edgeId,
            CompiledTraversalEnvelopeSetKind? setKind)
        {
            var tile = tiles.FirstOrDefault(value => value.CompiledCoordinate == coordinate);
            return tile != null && tile.Provenance.Any(value =>
                value.SourceKind == sourceKind && value.VariantId == variantId &&
                string.Equals(value.NodeId, nodeId, StringComparison.Ordinal) &&
                string.Equals(value.EdgeId, edgeId, StringComparison.Ordinal) &&
                value.EnvelopeSetKind == setKind);
        }

        private static void ValidateCanonicalPublication(
            IReadOnlyList<CompiledClusterSpineVariant> variants,
            IReadOnlyList<ClusterTraversalProtectedTile> protectedTiles,
            ICollection<TerrainClusterTraversalCompileError> errors)
        {
            var valid = variants.SequenceEqual(variants.OrderBy(value => value.VariantId)) &&
                protectedTiles.SequenceEqual(protectedTiles
                    .OrderBy(value => value.CompiledCoordinate.Y)
                    .ThenBy(value => value.CompiledCoordinate.X));
            foreach (var variant in variants)
            {
                valid &= variant.Nodes.SequenceEqual(variant.Nodes
                    .OrderBy(value => value.NodeId, StringComparer.Ordinal));
                valid &= variant.Edges.SequenceEqual(variant.Edges
                    .OrderBy(value => value.EdgeId, StringComparer.Ordinal));
                valid &= variant.ProtectedTiles.SequenceEqual(variant.ProtectedTiles
                    .OrderBy(value => value.CompiledCoordinate.Y)
                    .ThenBy(value => value.CompiledCoordinate.X));
                foreach (var edge in variant.Edges)
                {
                    foreach (var setKind in Enum.GetValues(typeof(CompiledTraversalEnvelopeSetKind))
                                 .Cast<CompiledTraversalEnvelopeSetKind>())
                    {
                        var set = edge.Envelope.GetTiles(setKind);
                        valid &= set.SequenceEqual(set
                            .OrderBy(value => value.CompiledCoordinate.Y)
                            .ThenBy(value => value.CompiledCoordinate.X)
                            .ThenBy(value => value.SourceCoordinate.Y)
                            .ThenBy(value => value.SourceCoordinate.X));
                    }
                }
            }

            if (!valid)
            {
                Add(errors, TerrainClusterTraversalCompileErrorCode.NonCanonicalPublication,
                    "publication", "Variants, graph elements, envelopes, or protection are non-canonical.");
            }
        }

        private static string ComputeDigest(
            TerrainClusterId clusterId,
            string sourceContractDigest,
            string localCanvasDigest,
            string roleSocketContractDigest,
            ClusterFootprintTransform transform,
            IEnumerable<CompiledClusterSpineVariant> variants,
            IEnumerable<ClusterTraversalProtectedTile> protectedTiles)
        {
            var material = new StringBuilder();
            Append(material, "RULESET", RulesetVersion);
            Append(material, "CLUSTER_ID", clusterId.Value);
            Append(material, "SOURCE_CONTRACT_DIGEST", sourceContractDigest);
            Append(material, "LOCAL_CANVAS_DIGEST", localCanvasDigest);
            Append(material, "ROLE_SOCKET_CONTRACT_DIGEST", roleSocketContractDigest);
            Append(material, "TRANSFORM", Number((int)transform));
            foreach (var variant in variants)
            {
                Append(material, "VARIANT", variant.VariantId.Value,
                    variant.IsBaseline ? "1" : "0", Number((int)variant.SourceGraphKind));
                foreach (var node in variant.Nodes)
                {
                    Append(material, "NODE", node.VariantId.Value, node.NodeId,
                        Coordinate(node.SourceCoordinate), Coordinate(node.CompiledCoordinate),
                        Coordinate(node.OwningCompiledChunk), node.IsMandatory ? "1" : "0",
                        Number((int)node.SourceGraphKind), node.SourceRoleAnchorId,
                        string.Join(",", node.LinkedRoleAnchorIds),
                        string.Join(",", node.LinkedRoleKinds.Select(value => Number((int)value))));
                }

                foreach (var edge in variant.Edges)
                {
                    Append(material, "EDGE", edge.VariantId.Value, edge.EdgeId,
                        edge.FromNodeId, edge.ToNodeId, Number((int)edge.MovementKind),
                        Coordinate(edge.SourceStartCoordinate), Coordinate(edge.CompiledStartCoordinate),
                        Coordinate(edge.SourceEndCoordinate), Coordinate(edge.CompiledEndCoordinate),
                        Number(edge.MinimumClearanceWidth), Number(edge.MinimumClearanceHeight),
                        Coordinate(edge.SourceLandingCoordinate), Coordinate(edge.CompiledLandingCoordinate),
                        Coordinate(edge.SourceRecoveryCoordinate), Coordinate(edge.CompiledRecoveryCoordinate),
                        edge.IsMandatory ? "1" : "0", Number((int)edge.SourceGraphKind));
                    foreach (var tile in edge.Envelope.AllTiles)
                    {
                        Append(material, "ENVELOPE", edge.VariantId.Value, edge.EdgeId,
                            Number((int)tile.SetKind), Coordinate(tile.SourceCoordinate),
                            Coordinate(tile.CompiledCoordinate), Coordinate(tile.OwningCompiledChunk));
                    }
                }
            }

            foreach (var tile in protectedTiles)
            {
                Append(material, "PROTECTED_TILE", Coordinate(tile.CompiledCoordinate));
                foreach (var item in tile.Provenance)
                {
                    Append(material, "PROVENANCE", Number((int)item.SourceKind),
                        item.VariantId.Value, item.NodeId, item.EdgeId,
                        item.EnvelopeSetKind.HasValue ? Number((int)item.EnvelopeSetKind.Value) : "0",
                        Coordinate(item.SourceCoordinate), Coordinate(item.CompiledCoordinate),
                        item.IsMandatory ? "1" : "0");
                }
            }

            var bytes = Encoding.UTF8.GetBytes(material.ToString());
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(bytes)
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static TerrainClusterTraversalCompileResult Failure(
            IEnumerable<TerrainClusterTraversalCompileError> errors)
        {
            return new TerrainClusterTraversalCompileResult(null, errors);
        }

        private static void Add(
            ICollection<TerrainClusterTraversalCompileError> errors,
            TerrainClusterTraversalCompileErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new TerrainClusterTraversalCompileError(code, path, detail));
        }

        private static string VariantPath(SpineVariantId variantId) =>
            "variants[" + variantId.Value + "]";
        private static string NodePath(SpineVariantId variantId, string nodeId) =>
            VariantPath(variantId) + ".nodes[" + nodeId + "]";
        private static string EdgePath(SpineVariantId variantId, string edgeId) =>
            VariantPath(variantId) + ".edges[" + edgeId + "]";
        private static string ProtectionPath(LocalTileCoord coordinate) =>
            "protected[" + Coordinate(coordinate) + "]";
        private static string CountDetail(int actual, int expected) =>
            "actual=" + Number(actual) + ",expected=" + Number(expected);
        private static string Coordinate(LocalTileCoord value) =>
            Number(value.X) + "," + Number(value.Y);
        private static string Coordinate(ClusterChunkCoord value) =>
            Number(value.X) + "," + Number(value.Y);
        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);

        private static void Append(StringBuilder material, params string[] fields)
        {
            if (material.Length != 0) material.Append('\n');
            material.Append(string.Join("|", fields));
        }
    }
}
