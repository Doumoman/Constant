using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public static class TerrainClusterRoleSocketCompiler
    {
        public const string RulesetVersion = "MAP11_02_CLUSTER_ROLE_SOCKET_V1";

        public static TerrainClusterRoleSocketCompileResult Compile(
            TerrainClusterRoleSocketCompileRequest request)
        {
            var errors = new List<TerrainClusterRoleSocketCompileError>();
            if (request == null)
            {
                Add(errors, TerrainClusterRoleSocketCompileErrorCode.MissingInput,
                    "request", "Compile request is required.");
                return Failure(errors);
            }

            if (request.SourceContract == null)
            {
                Add(errors, TerrainClusterRoleSocketCompileErrorCode.MissingInput,
                    "request.sourceContract", "Source TerrainCluster contract is required.");
            }

            if (request.LocalCanvas == null)
            {
                Add(errors, TerrainClusterRoleSocketCompileErrorCode.MissingInput,
                    "request.localCanvas", "Successful MAP11_01 Local Canvas is required.");
            }

            TerrainClusterValidationResult sourceValidation = null;
            if (request.SourceContract != null)
            {
                sourceValidation = TerrainClusterContractValidator.Validate(
                    request.SourceContract,
                    request.SixChunkAllowlist);
                foreach (var sourceError in sourceValidation.Errors)
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.InvalidSourceContract,
                        "source." + sourceError.Path,
                        sourceError.Code + ":" + sourceError.Detail);
                    AddTranslatedSourceError(errors, sourceError);
                }

                if (sourceValidation.IsValid &&
                    !string.Equals(
                        sourceValidation.CanonicalDigest,
                        request.SourceContractCanonicalDigest,
                        StringComparison.Ordinal))
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.InvalidSourceContract,
                        "request.sourceContractCanonicalDigest",
                        "Provided digest does not match the MAP09_04 canonical digest.");
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
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.LocalCanvasIdentityMismatch,
                        "request.localCanvas", "MAP11_01 could not reproduce the provided Canvas.");
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

            if (errors.Count != 0)
            {
                return Failure(errors);
            }

            var roles = ProjectRoles(sourceValidation.Contract, request.LocalCanvas, errors);
            var ports = ProjectPorts(sourceValidation.Contract, request.LocalCanvas, roles, errors);
            var links = ProjectRoleSpineLinks(
                sourceValidation.Contract,
                request.LocalCanvas,
                roles,
                ports,
                errors);
            var connections = ConnectSockets(
                request.SocketEvidence,
                ports,
                roles,
                links,
                errors);

            ValidateCanonicalPublication(roles, ports, links, connections, errors);
            if (errors.Count != 0)
            {
                return Failure(errors);
            }

            var digest = ComputeDigest(
                sourceValidation.Contract.Id,
                sourceValidation.CanonicalDigest,
                request.LocalCanvas.CanonicalDigest,
                request.LocalCanvas.Transform,
                roles,
                ports,
                links,
                connections);
            var contract = new TerrainClusterRoleSocketContract(
                sourceValidation.Contract.Id,
                sourceValidation.CanonicalDigest,
                request.LocalCanvas.CanonicalDigest,
                request.LocalCanvas.Transform,
                roles,
                ports,
                links,
                connections,
                digest);
            return new TerrainClusterRoleSocketCompileResult(contract, errors);
        }

        private static void AddTranslatedSourceError(
            ICollection<TerrainClusterRoleSocketCompileError> errors,
            TerrainClusterValidationError sourceError)
        {
            TerrainClusterRoleSocketCompileErrorCode? code = null;
            switch (sourceError.Code)
            {
                case TerrainClusterValidationErrorCode.MissingRequiredRole:
                    code = TerrainClusterRoleSocketCompileErrorCode.MissingRequiredRole;
                    break;
                case TerrainClusterValidationErrorCode.InvalidRoleAnchor:
                    code = TerrainClusterRoleSocketCompileErrorCode.RoleOutsideActiveMask;
                    break;
                case TerrainClusterValidationErrorCode.DuplicatePrimaryPort:
                    code = TerrainClusterRoleSocketCompileErrorCode.MissingOrDuplicatePrimaryPort;
                    break;
                case TerrainClusterValidationErrorCode.InvalidPort:
                    code = TerrainClusterRoleSocketCompileErrorCode.PortRoleMismatch;
                    break;
                case TerrainClusterValidationErrorCode.InvalidPortDirection:
                    code = TerrainClusterRoleSocketCompileErrorCode.PortNotOutward;
                    break;
                case TerrainClusterValidationErrorCode.MissingNodeReference:
                    code = TerrainClusterRoleSocketCompileErrorCode.MissingVariantRoleNode;
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
            ICollection<TerrainClusterRoleSocketCompileError> errors)
        {
            if (sourceContract.Id != actual.ClusterId ||
                !string.Equals(expected.SourceFootprintDigest, actual.SourceFootprintDigest, StringComparison.Ordinal) ||
                !CanvasMappingsEqual(expected, actual))
            {
                Add(errors, TerrainClusterRoleSocketCompileErrorCode.LocalCanvasIdentityMismatch,
                    "request.localCanvas", "Cluster identity, footprint, bounds, or source mapping differs.");
            }

            if (!string.Equals(actual.CanonicalDigest, providedDigest, StringComparison.Ordinal) ||
                !string.Equals(expected.CanonicalDigest, actual.CanonicalDigest, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterRoleSocketCompileErrorCode.LocalCanvasDigestMismatch,
                    "request.localCanvasCanonicalDigest",
                    "Provided or reproduced MAP11_01 canonical digest differs.");
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
                    a.State != b.State ||
                    a.SourceChunkCoordinate != b.SourceChunkCoordinate ||
                    a.SourceCoordinate != b.SourceCoordinate)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<ProjectedClusterRoleAnchor> ProjectRoles(
            TerrainClusterContract source,
            TerrainClusterLocalCanvas canvas,
            ICollection<TerrainClusterRoleSocketCompileError> errors)
        {
            var roles = new List<ProjectedClusterRoleAnchor>();
            foreach (var role in source.RoleAnchors.OrderBy(value => value.AnchorId, StringComparer.Ordinal))
            {
                LocalTileCoord compiled;
                if (!canvas.TryGetCompiledTile(role.Tile, out compiled))
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.RoleProjectionMissing,
                        RolePath(role.AnchorId), Coordinate(role.Tile));
                    continue;
                }

                CompiledClusterLocalTileCell tileCell;
                CompiledClusterChunkCell chunkCell;
                if (!canvas.TryGetTileCell(compiled, out tileCell))
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.RoleProjectionMissing,
                        RolePath(role.AnchorId), Coordinate(compiled));
                    continue;
                }

                if (tileCell.State != ClusterChunkMaskState.Active ||
                    !canvas.TryGetChunkCell(tileCell.OwningChunk, out chunkCell) ||
                    chunkCell.State != ClusterChunkMaskState.Active)
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.RoleOutsideActiveMask,
                        RolePath(role.AnchorId), Coordinate(compiled));
                }

                roles.Add(new ProjectedClusterRoleAnchor(
                    role.AnchorId,
                    role.Role,
                    role.Tile,
                    compiled,
                    tileCell.OwningChunk,
                    role.TraversalNodeId));
            }

            foreach (var required in new[]
                     {
                         ClusterRoleKind.Entry,
                         ClusterRoleKind.BuildUp,
                         ClusterRoleKind.Core,
                         ClusterRoleKind.Recovery,
                         ClusterRoleKind.Exit,
                     })
            {
                if (!roles.Any(value => value.Role == required))
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.MissingRequiredRole,
                        "roles[" + required + "]", "At least one projected role is required.");
                }
            }

            return roles.OrderBy(value => value.AnchorId, StringComparer.Ordinal).ToList();
        }

        private static List<ProjectedClusterPort> ProjectPorts(
            TerrainClusterContract source,
            TerrainClusterLocalCanvas canvas,
            IReadOnlyList<ProjectedClusterRoleAnchor> roles,
            ICollection<TerrainClusterRoleSocketCompileError> errors)
        {
            var ports = new List<ProjectedClusterPort>();
            foreach (var kind in new[] { ClusterPortKind.Entry, ClusterPortKind.Exit })
            {
                var sourcePorts = source.Ports.Where(value => value.IsPrimary && value.Kind == kind).ToArray();
                if (sourcePorts.Length != 1)
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.MissingOrDuplicatePrimaryPort,
                        "ports.primary[" + kind + "]", CountDetail(sourcePorts.Length, 1));
                    continue;
                }

                var port = sourcePorts[0];
                var role = roles.FirstOrDefault(value =>
                    string.Equals(value.AnchorId, port.RoleAnchorId, StringComparison.Ordinal));
                var expectedRole = kind == ClusterPortKind.Entry
                    ? ClusterRoleKind.Entry
                    : ClusterRoleKind.Exit;
                LocalTileCoord compiled;
                if (role == null || role.Role != expectedRole ||
                    role.SourceCoordinate != port.Tile ||
                    !canvas.TryGetCompiledTile(port.Tile, out compiled) ||
                    role.CompiledCoordinate != compiled)
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.PortRoleMismatch,
                        PortPath(port.PortId), port.RoleAnchorId);
                    continue;
                }

                ClusterPortSide compiledSide;
                if (!ClusterRoleSocketTransformUtility.TryTransformSide(
                        port.OutwardSide,
                        canvas.Transform,
                        out compiledSide))
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.InvalidTransformedPortSide,
                        PortPath(port.PortId), Number((int)port.OutwardSide));
                    continue;
                }

                CompiledClusterLocalTileCell tileCell;
                if (!canvas.TryGetTileCell(compiled, out tileCell) ||
                    tileCell.State != ClusterChunkMaskState.Active)
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.RoleOutsideActiveMask,
                        PortPath(port.PortId), Coordinate(compiled));
                }

                LocalTileCoord neighbor;
                if (!ClusterRoleSocketTransformUtility.TryNeighbor(compiled, compiledSide, out neighbor))
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.InvalidTransformedPortSide,
                        PortPath(port.PortId), Number((int)compiledSide));
                }
                else
                {
                    CompiledClusterLocalTileCell neighborCell;
                    if (canvas.TryGetTileCell(neighbor, out neighborCell) &&
                        neighborCell.State == ClusterChunkMaskState.Active)
                    {
                        Add(errors, TerrainClusterRoleSocketCompileErrorCode.PortNotOutward,
                            PortPath(port.PortId), Coordinate(neighbor));
                    }
                }

                ports.Add(new ProjectedClusterPort(
                    port.PortId,
                    port.Kind,
                    port.Tile,
                    compiled,
                    port.OutwardSide,
                    compiledSide,
                    role.AnchorId,
                    role.Role,
                    port.CompatibleRouteTypes));
            }

            return ports.OrderBy(value => value.Kind)
                .ThenBy(value => value.PortId, StringComparer.Ordinal).ToList();
        }

        private static List<ProjectedRoleSpineLink> ProjectRoleSpineLinks(
            TerrainClusterContract source,
            TerrainClusterLocalCanvas canvas,
            IReadOnlyList<ProjectedClusterRoleAnchor> roles,
            IReadOnlyList<ProjectedClusterPort> ports,
            ICollection<TerrainClusterRoleSocketCompileError> errors)
        {
            var links = new List<ProjectedRoleSpineLink>();
            var entryPort = ports.FirstOrDefault(value => value.Kind == ClusterPortKind.Entry);
            var exitPort = ports.FirstOrDefault(value => value.Kind == ClusterPortKind.Exit);
            foreach (var variant in source.Traversal.Variants.OrderBy(value => value.Id))
            {
                var nodes = variant.Nodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
                foreach (var role in roles.OrderBy(value => value.AnchorId, StringComparer.Ordinal))
                {
                    TraversalNode node;
                    if (!nodes.TryGetValue(role.TraversalNodeId, out node))
                    {
                        Add(errors, TerrainClusterRoleSocketCompileErrorCode.MissingVariantRoleNode,
                            LinkPath(variant.Id, role.AnchorId), role.TraversalNodeId);
                        continue;
                    }

                    LocalTileCoord compiledNode;
                    if (!canvas.TryGetCompiledTile(node.Tile, out compiledNode))
                    {
                        Add(errors, TerrainClusterRoleSocketCompileErrorCode.RoleProjectionMissing,
                            LinkPath(variant.Id, role.AnchorId), Coordinate(node.Tile));
                        continue;
                    }

                    if (!string.Equals(node.RoleAnchorId, role.AnchorId, StringComparison.Ordinal) ||
                        node.Tile != role.SourceCoordinate || compiledNode != role.CompiledCoordinate)
                    {
                        Add(errors, TerrainClusterRoleSocketCompileErrorCode.RoleNodeCoordinateMismatch,
                            LinkPath(variant.Id, role.AnchorId), node.NodeId);
                    }

                    var connectionKind = entryPort != null &&
                        string.Equals(entryPort.RoleAnchorId, role.AnchorId, StringComparison.Ordinal)
                            ? ProjectedRoleConnectionKind.EntryPort
                            : exitPort != null &&
                              string.Equals(exitPort.RoleAnchorId, role.AnchorId, StringComparison.Ordinal)
                                ? ProjectedRoleConnectionKind.ExitPort
                                : ProjectedRoleConnectionKind.InternalRole;
                    links.Add(new ProjectedRoleSpineLink(
                        variant.Id,
                        variant.IsBaseline,
                        role.AnchorId,
                        role.Role,
                        node.NodeId,
                        node.Tile,
                        compiledNode,
                        connectionKind));
                }

                if (links.Count(value => value.VariantId == variant.Id &&
                        value.ConnectionKind == ProjectedRoleConnectionKind.EntryPort) != 1 ||
                    links.Count(value => value.VariantId == variant.Id &&
                        value.ConnectionKind == ProjectedRoleConnectionKind.ExitPort) != 1)
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.EntryExitConnectionMismatch,
                        "variants[" + variant.Id.Value + "]", "Primary Entry/Exit role-node chains are required.");
                }
            }

            return links.OrderBy(value => value.VariantId)
                .ThenBy(value => value.RoleAnchorId, StringComparer.Ordinal).ToList();
        }

        private static List<ClusterSectorSocketConnection> ConnectSockets(
            IReadOnlyList<ClusterSectorSocketEvidence> evidence,
            IReadOnlyList<ProjectedClusterPort> ports,
            IReadOnlyList<ProjectedClusterRoleAnchor> roles,
            IReadOnlyList<ProjectedRoleSpineLink> links,
            ICollection<TerrainClusterRoleSocketCompileError> errors)
        {
            var connections = new List<ClusterSectorSocketConnection>();
            var nonNull = evidence.Where(value => value != null).ToArray();
            if (nonNull.Length != evidence.Count)
            {
                Add(errors, TerrainClusterRoleSocketCompileErrorCode.MissingInput,
                    "socketEvidence", "Socket evidence records cannot be null.");
            }

            foreach (var duplicate in nonNull.GroupBy(value => value.StableIdentity, StringComparer.Ordinal)
                         .Where(group => group.Count() > 1))
            {
                Add(errors, TerrainClusterRoleSocketCompileErrorCode.DuplicateSocketBinding,
                    "socketEvidence[" + duplicate.Key + "]", Number(duplicate.Count()));
            }

            foreach (var kind in new[] { ClusterPortKind.Entry, ClusterPortKind.Exit })
            {
                var matches = nonNull.Where(value => value.BoundPortKind == kind).ToArray();
                if (matches.Length == 0)
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.MissingSocketBinding,
                        "socketEvidence[" + kind + "]", "Expected exactly one binding.");
                    continue;
                }

                if (matches.Length != 1)
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.DuplicateSocketBinding,
                        "socketEvidence[" + kind + "]", CountDetail(matches.Length, 1));
                    continue;
                }

                var item = matches[0];
                var port = ports.FirstOrDefault(value => value.Kind == kind);
                if (port == null || item.SectorRecipeId.Length == 0 || item.SocketId.Length == 0)
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.MissingSocketBinding,
                        "socketEvidence[" + kind + "]", item.StableIdentity);
                    continue;
                }

                if (item.Side != port.CompiledOutwardSide)
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.SocketSideMismatch,
                        "socketEvidence[" + item.StableIdentity + "]",
                        item.Side + "!=" + port.CompiledOutwardSide);
                }

                if (item.OwningRouteType < 0 || item.OwningRouteType > 4 ||
                    !port.CompatibleRouteTypes.Contains(item.OwningRouteType))
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.RouteTypeIncompatible,
                        "socketEvidence[" + item.StableIdentity + "]", Number(item.OwningRouteType));
                }

                if (!item.MandatoryAllowed)
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.MandatorySocketRejected,
                        "socketEvidence[" + item.StableIdentity + "]", "MandatoryAllowed must be true.");
                }

                var role = roles.First(value =>
                    string.Equals(value.AnchorId, port.RoleAnchorId, StringComparison.Ordinal));
                var requiredConnectionKind = kind == ClusterPortKind.Entry
                    ? ProjectedRoleConnectionKind.EntryPort
                    : ProjectedRoleConnectionKind.ExitPort;
                if (role.Role != (kind == ClusterPortKind.Entry ? ClusterRoleKind.Entry : ClusterRoleKind.Exit) ||
                    !links.Any(value => value.RoleAnchorId == role.AnchorId &&
                        value.TraversalNodeId == role.TraversalNodeId &&
                        value.ConnectionKind == requiredConnectionKind))
                {
                    Add(errors, TerrainClusterRoleSocketCompileErrorCode.EntryExitConnectionMismatch,
                        "socketEvidence[" + item.StableIdentity + "]", role.AnchorId);
                }

                connections.Add(new ClusterSectorSocketConnection(
                    item,
                    port.PortId,
                    role.AnchorId,
                    role.TraversalNodeId,
                    port.CompiledCoordinate,
                    port.CompiledOutwardSide));
            }

            return connections.OrderBy(value => value.PortKind)
                .ThenBy(value => value.Evidence.StableIdentity, StringComparer.Ordinal).ToList();
        }

        private static void ValidateCanonicalPublication(
            IReadOnlyList<ProjectedClusterRoleAnchor> roles,
            IReadOnlyList<ProjectedClusterPort> ports,
            IReadOnlyList<ProjectedRoleSpineLink> links,
            IReadOnlyList<ClusterSectorSocketConnection> connections,
            ICollection<TerrainClusterRoleSocketCompileError> errors)
        {
            if (!roles.Select(value => value.AnchorId).SequenceEqual(
                    roles.Select(value => value.AnchorId).OrderBy(value => value, StringComparer.Ordinal)) ||
                !ports.SequenceEqual(ports.OrderBy(value => value.Kind)
                    .ThenBy(value => value.PortId, StringComparer.Ordinal)) ||
                !links.SequenceEqual(links.OrderBy(value => value.VariantId)
                    .ThenBy(value => value.RoleAnchorId, StringComparer.Ordinal)) ||
                !connections.SequenceEqual(connections.OrderBy(value => value.PortKind)
                    .ThenBy(value => value.Evidence.StableIdentity, StringComparer.Ordinal)))
            {
                Add(errors, TerrainClusterRoleSocketCompileErrorCode.NonCanonicalPublication,
                    "publication", "Roles, ports, links, and connections must use canonical order.");
            }
        }

        private static string ComputeDigest(
            TerrainClusterId clusterId,
            string sourceContractDigest,
            string localCanvasDigest,
            ClusterFootprintTransform transform,
            IEnumerable<ProjectedClusterRoleAnchor> roles,
            IEnumerable<ProjectedClusterPort> ports,
            IEnumerable<ProjectedRoleSpineLink> links,
            IEnumerable<ClusterSectorSocketConnection> connections)
        {
            var material = new StringBuilder();
            Append(material, "RULESET", RulesetVersion);
            Append(material, "CLUSTER_ID", clusterId.Value);
            Append(material, "SOURCE_CONTRACT_DIGEST", sourceContractDigest);
            Append(material, "LOCAL_CANVAS_DIGEST", localCanvasDigest);
            Append(material, "TRANSFORM", Number((int)transform));
            foreach (var role in roles)
            {
                Append(material, "ROLE", role.AnchorId, Number((int)role.Role),
                    Coordinate(role.SourceCoordinate), Coordinate(role.CompiledCoordinate),
                    Coordinate(role.OwningCompiledChunk), role.TraversalNodeId);
            }

            foreach (var port in ports)
            {
                Append(material, "PORT", port.PortId, Number((int)port.Kind),
                    Coordinate(port.SourceCoordinate), Coordinate(port.CompiledCoordinate),
                    Number((int)port.SourceOutwardSide), Number((int)port.CompiledOutwardSide),
                    port.RoleAnchorId, Number((int)port.RoleKind),
                    string.Join(",", port.CompatibleRouteTypes.Select(Number)));
            }

            foreach (var link in links)
            {
                Append(material, "ROLE_NODE_LINK", link.VariantId.Value,
                    link.IsBaseline ? "1" : "0", link.RoleAnchorId,
                    Number((int)link.RoleKind), link.TraversalNodeId,
                    Coordinate(link.SourceCoordinate), Coordinate(link.CompiledCoordinate),
                    Number((int)link.ConnectionKind));
            }

            foreach (var connection in connections)
            {
                var item = connection.Evidence;
                Append(material, "SOCKET_CONNECTION", item.SectorRecipeId, item.SocketId,
                    Number((int)item.Side), Number(item.OwningRouteType),
                    item.MandatoryAllowed ? "1" : "0", Number((int)item.BoundPortKind),
                    connection.PortId, connection.RoleAnchorId, connection.TraversalNodeId,
                    Coordinate(connection.CompiledCoordinate),
                    Number((int)connection.CompiledOutwardSide));
            }

            var bytes = Encoding.UTF8.GetBytes(material.ToString());
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(bytes)
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static TerrainClusterRoleSocketCompileResult Failure(
            IEnumerable<TerrainClusterRoleSocketCompileError> errors)
        {
            return new TerrainClusterRoleSocketCompileResult(null, errors);
        }

        private static void Add(
            ICollection<TerrainClusterRoleSocketCompileError> errors,
            TerrainClusterRoleSocketCompileErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new TerrainClusterRoleSocketCompileError(code, path, detail));
        }

        private static string RolePath(string id) => "roles[" + id + "]";
        private static string PortPath(string id) => "ports[" + id + "]";
        private static string LinkPath(SpineVariantId variantId, string roleId) =>
            "variants[" + variantId.Value + "].roles[" + roleId + "]";
        private static string CountDetail(int actual, int expected) =>
            "actual=" + Number(actual) + ",expected=" + Number(expected);
        private static string Coordinate(LocalTileCoord value) =>
            Number(value.X) + "," + Number(value.Y);
        private static string Coordinate(ClusterChunkCoord value) =>
            Number(value.X) + "," + Number(value.Y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static void Append(StringBuilder material, params string[] fields)
        {
            if (material.Length != 0) material.Append('\n');
            material.Append(string.Join("|", fields));
        }
    }
}
