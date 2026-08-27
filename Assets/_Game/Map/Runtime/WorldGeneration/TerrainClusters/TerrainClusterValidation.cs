using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public enum TerrainClusterValidationErrorCode
    {
        MissingInput = 1,
        InvalidId = 2,
        InvalidFootprintCount = 3,
        SixChunkNotAllowlisted = 4,
        DuplicateOrDisconnectedFootprint = 5,
        InvalidRoleAnchor = 6,
        MissingRequiredRole = 7,
        InvalidPort = 8,
        InvalidPortDirection = 9,
        DuplicatePrimaryPort = 10,
        InvalidGraphKind = 11,
        DuplicateVariant = 12,
        InvalidBaselineVariant = 13,
        DuplicateNodeOrEdge = 14,
        MissingNodeReference = 15,
        SelfEdge = 16,
        InvalidMovement = 17,
        EdgeAnchorMismatch = 18,
        InvalidClearance = 19,
        InvalidLanding = 20,
        InvalidRecovery = 21,
        MissingEntryExitPath = 22,
        UnreachableMandatoryElement = 23,
        InvalidEnvelopeSet = 24,
        FloorClearanceConflict = 25,
        MovementEnvelopeMismatch = 26,
    }

    public sealed class TerrainClusterValidationError :
        IEquatable<TerrainClusterValidationError>,
        IComparable<TerrainClusterValidationError>
    {
        public TerrainClusterValidationError(
            TerrainClusterValidationErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public TerrainClusterValidationErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(TerrainClusterValidationError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(TerrainClusterValidationError other)
        {
            return other != null &&
                   Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TerrainClusterValidationError);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString()
        {
            return Code + "|" + Path + "|" + Detail;
        }
    }

    public sealed class TerrainClusterValidationResult
    {
        private readonly ReadOnlyCollection<TerrainClusterValidationError> errors;

        internal TerrainClusterValidationResult(
            TerrainClusterContract contract,
            IEnumerable<TerrainClusterValidationError> errors,
            string canonicalDigest)
        {
            var copy = errors
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<TerrainClusterValidationError>(copy);
            Contract = copy.Length == 0 ? contract : null;
            CanonicalDigest = copy.Length == 0 ? canonicalDigest ?? string.Empty : string.Empty;
        }

        public bool IsValid => errors.Count == 0 && Contract != null;
        public TerrainClusterContract Contract { get; }
        public IReadOnlyList<TerrainClusterValidationError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class TerrainClusterContractValidator
    {
        public static TerrainClusterValidationResult Validate(
            TerrainClusterContract contract,
            IEnumerable<TerrainClusterId> sixChunkAllowlist = null)
        {
            var errors = new List<TerrainClusterValidationError>();
            if (contract == null)
            {
                Add(errors, TerrainClusterValidationErrorCode.MissingInput, "contract", "Contract is required.");
                return new TerrainClusterValidationResult(null, errors, string.Empty);
            }

            ValidateIdentity(contract, errors);
            var activeChunks = ValidateFootprint(contract, sixChunkAllowlist, errors);
            var roles = ValidateRoles(contract, activeChunks, errors);
            var primaryPorts = ValidatePorts(contract, activeChunks, roles, errors);
            ValidateTraversal(contract, activeChunks, roles, primaryPorts, errors);

            if (errors.Count != 0)
            {
                return new TerrainClusterValidationResult(null, errors, string.Empty);
            }

            return new TerrainClusterValidationResult(
                contract,
                errors,
                TerrainClusterCanonicalDigest.Compute(contract));
        }

        private static void ValidateIdentity(
            TerrainClusterContract contract,
            ICollection<TerrainClusterValidationError> errors)
        {
            if (!IsStableId(contract.Id.Value, "TC_"))
            {
                Add(errors, TerrainClusterValidationErrorCode.InvalidId, "id", contract.Id.Value);
            }
        }

        private static HashSet<ClusterChunkCoord> ValidateFootprint(
            TerrainClusterContract contract,
            IEnumerable<TerrainClusterId> sixChunkAllowlist,
            ICollection<TerrainClusterValidationError> errors)
        {
            var active = new HashSet<ClusterChunkCoord>();
            if (contract.Footprint == null)
            {
                Add(
                    errors,
                    TerrainClusterValidationErrorCode.MissingInput,
                    "footprint",
                    "Footprint is required.");
                return active;
            }

            var chunks = contract.Footprint.ActiveChunks;
            if (chunks.Count < 2 || chunks.Count > 6)
            {
                Add(
                    errors,
                    TerrainClusterValidationErrorCode.InvalidFootprintCount,
                    "footprint.activeChunks",
                    Number(chunks.Count));
            }

            if (chunks.Count == 6)
            {
                var allowlist = new HashSet<TerrainClusterId>(sixChunkAllowlist ?? Array.Empty<TerrainClusterId>());
                if (!allowlist.Contains(contract.Id))
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.SixChunkNotAllowlisted,
                        "footprint.activeChunks",
                        contract.Id.Value);
                }
            }

            var hasNegative = false;
            foreach (var chunk in chunks)
            {
                if (!active.Add(chunk))
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.DuplicateOrDisconnectedFootprint,
                        ChunkPath(chunk),
                        "Active chunk occurs more than once.");
                }

                if (chunk.X < 0 || chunk.Y < 0)
                {
                    hasNegative = true;
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.DuplicateOrDisconnectedFootprint,
                        ChunkPath(chunk),
                        "Normalized active chunk coordinates must be nonnegative.");
                }
            }

            if (active.Count != 0 && !hasNegative &&
                (active.Min(value => value.X) != 0 || active.Min(value => value.Y) != 0))
            {
                Add(
                    errors,
                    TerrainClusterValidationErrorCode.DuplicateOrDisconnectedFootprint,
                    "footprint.activeChunks",
                    "Normalized footprint minima must be x=0 and y=0.");
            }

            if (active.Count != 0 && ReachableChunks(active).Count != active.Count)
            {
                Add(
                    errors,
                    TerrainClusterValidationErrorCode.DuplicateOrDisconnectedFootprint,
                    "footprint.activeChunks",
                    "Active chunks must form one 4-neighbor connected component.");
            }

            return active;
        }

        private static Dictionary<string, ClusterRoleAnchor> ValidateRoles(
            TerrainClusterContract contract,
            ISet<ClusterChunkCoord> activeChunks,
            ICollection<TerrainClusterValidationError> errors)
        {
            var roles = new Dictionary<string, ClusterRoleAnchor>(StringComparer.Ordinal);
            foreach (var role in contract.RoleAnchors)
            {
                if (role == null)
                {
                    Add(errors, TerrainClusterValidationErrorCode.InvalidRoleAnchor, "roles", "Role anchor is required.");
                    continue;
                }

                var path = "roles[" + role.AnchorId + "]";
                if (!IsStableId(role.AnchorId, "ANCHOR_") ||
                    !IsStableId(role.TraversalNodeId, "NODE_") ||
                    !IsDefinedRole(role.Role) ||
                    !IsInsideFootprint(role.Tile, activeChunks))
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.InvalidRoleAnchor,
                        path,
                        "Stable IDs, exact role, and an active-footprint tile are required.");
                }

                if (!roles.ContainsKey(role.AnchorId))
                {
                    roles.Add(role.AnchorId, role);
                }
                else
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.InvalidRoleAnchor,
                        path,
                        "Role anchor ID occurs more than once.");
                }
            }

            var required = new[]
            {
                ClusterRoleKind.Entry,
                ClusterRoleKind.BuildUp,
                ClusterRoleKind.Core,
                ClusterRoleKind.Recovery,
                ClusterRoleKind.Exit,
            };
            foreach (var kind in required)
            {
                if (!contract.RoleAnchors.Any(value => value != null && value.Role == kind))
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.MissingRequiredRole,
                        "roles[" + kind + "]",
                        "At least one role anchor is required.");
                }
            }

            var entry = contract.RoleAnchors.FirstOrDefault(value => value != null && value.Role == ClusterRoleKind.Entry);
            var exit = contract.RoleAnchors.FirstOrDefault(value => value != null && value.Role == ClusterRoleKind.Exit);
            if (entry != null && exit != null &&
                (entry.Tile == exit.Tile ||
                 string.Equals(entry.AnchorId, exit.AnchorId, StringComparison.Ordinal) ||
                 string.Equals(entry.TraversalNodeId, exit.TraversalNodeId, StringComparison.Ordinal)))
            {
                Add(
                    errors,
                    TerrainClusterValidationErrorCode.InvalidRoleAnchor,
                    "roles.entryExit",
                    "Entry and Exit anchors must be distinct.");
            }

            return roles;
        }

        private static Dictionary<ClusterPortKind, ClusterPort> ValidatePorts(
            TerrainClusterContract contract,
            ISet<ClusterChunkCoord> activeChunks,
            IReadOnlyDictionary<string, ClusterRoleAnchor> roles,
            ICollection<TerrainClusterValidationError> errors)
        {
            var portIds = new HashSet<string>(StringComparer.Ordinal);
            var primaryPorts = new Dictionary<ClusterPortKind, ClusterPort>();
            foreach (var port in contract.Ports)
            {
                if (port == null)
                {
                    Add(errors, TerrainClusterValidationErrorCode.InvalidPort, "ports", "Port is required.");
                    continue;
                }

                var path = "ports[" + port.PortId + "]";
                ClusterRoleAnchor role;
                var expectedRole = port.Kind == ClusterPortKind.Entry
                    ? ClusterRoleKind.Entry
                    : ClusterRoleKind.Exit;
                if (!IsStableId(port.PortId, "PORT_") ||
                    !IsDefinedPortKind(port.Kind) ||
                    !portIds.Add(port.PortId) ||
                    !roles.TryGetValue(port.RoleAnchorId, out role) ||
                    role.Role != expectedRole ||
                    role.Tile != port.Tile ||
                    !IsInsideFootprint(port.Tile, activeChunks) ||
                    port.CompatibleRouteTypes.Count == 0 ||
                    port.CompatibleRouteTypes.Any(value => value < 0 || value > 4) ||
                    port.CompatibleRouteTypes.Distinct().Count() != port.CompatibleRouteTypes.Count)
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.InvalidPort,
                        path,
                        "Port identity, role, tile, and unique existing RouteType compatibility 0..4 are required.");
                }

                if (!IsDefinedPortSide(port.OutwardSide) ||
                    !PointsOutsideFootprint(port.Tile, port.OutwardSide, activeChunks))
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.InvalidPortDirection,
                        path,
                        "The side must point from an owned boundary tile to outside the footprint.");
                }

                if (port.IsPrimary && IsDefinedPortKind(port.Kind) && !primaryPorts.ContainsKey(port.Kind))
                {
                    primaryPorts.Add(port.Kind, port);
                }
            }

            foreach (var kind in new[] { ClusterPortKind.Entry, ClusterPortKind.Exit })
            {
                var count = contract.Ports.Count(value => value != null && value.IsPrimary && value.Kind == kind);
                if (count != 1)
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.DuplicatePrimaryPort,
                        "ports.primary[" + kind + "]",
                        "Expected exactly one; actual " + Number(count) + ".");
                }
            }

            return primaryPorts;
        }

        private static void ValidateTraversal(
            TerrainClusterContract contract,
            ISet<ClusterChunkCoord> activeChunks,
            IReadOnlyDictionary<string, ClusterRoleAnchor> roles,
            IReadOnlyDictionary<ClusterPortKind, ClusterPort> primaryPorts,
            ICollection<TerrainClusterValidationError> errors)
        {
            if (contract.Traversal == null)
            {
                Add(errors, TerrainClusterValidationErrorCode.MissingInput, "traversal", "Traversal contract is required.");
                return;
            }

            var variants = contract.Traversal.Variants;
            var seenVariants = new HashSet<SpineVariantId>();
            var baselineCount = 0;
            foreach (var variant in variants)
            {
                if (variant == null)
                {
                    Add(errors, TerrainClusterValidationErrorCode.MissingInput, "variants", "SpineVariant is required.");
                    continue;
                }

                var path = "variants[" + variant.Id.Value + "]";
                if (!IsStableId(variant.Id.Value, "SPINE_"))
                {
                    Add(errors, TerrainClusterValidationErrorCode.InvalidId, path, variant.Id.Value);
                }

                if (!seenVariants.Add(variant.Id))
                {
                    Add(errors, TerrainClusterValidationErrorCode.DuplicateVariant, path, "Variant ID occurs more than once.");
                }

                if (variant.IsBaseline) baselineCount++;
                ValidateVariant(variant, path, activeChunks, roles, primaryPorts, errors);
            }

            if (variants.Count == 0 || baselineCount != 1)
            {
                Add(
                    errors,
                    TerrainClusterValidationErrorCode.InvalidBaselineVariant,
                    "variants.baseline",
                    "At least one variant and exactly one baseline are required; actual " + Number(baselineCount) + ".");
            }
        }

        private static void ValidateVariant(
            SpineVariant variant,
            string path,
            ISet<ClusterChunkCoord> activeChunks,
            IReadOnlyDictionary<string, ClusterRoleAnchor> roles,
            IReadOnlyDictionary<ClusterPortKind, ClusterPort> primaryPorts,
            ICollection<TerrainClusterValidationError> errors)
        {
            if (variant.GraphKind != TraversalGraphKind.Traversal)
            {
                Add(errors, TerrainClusterValidationErrorCode.InvalidGraphKind, path, Number((int)variant.GraphKind));
            }

            var nodes = new Dictionary<string, TraversalNode>(StringComparer.Ordinal);
            foreach (var node in variant.Nodes)
            {
                if (node == null)
                {
                    Add(errors, TerrainClusterValidationErrorCode.MissingInput, path + ".nodes", "Node is required.");
                    continue;
                }

                var nodePath = path + ".nodes[" + node.NodeId + "]";
                if (!IsStableId(node.NodeId, "NODE_"))
                {
                    Add(errors, TerrainClusterValidationErrorCode.InvalidId, nodePath, node.NodeId);
                }

                if (node.GraphKind != TraversalGraphKind.Traversal)
                {
                    Add(errors, TerrainClusterValidationErrorCode.InvalidGraphKind, nodePath, Number((int)node.GraphKind));
                }

                if (!nodes.ContainsKey(node.NodeId)) nodes.Add(node.NodeId, node);
                else Add(errors, TerrainClusterValidationErrorCode.DuplicateNodeOrEdge, nodePath, "Node ID occurs more than once.");

                ClusterRoleAnchor role;
                if (!IsInsideFootprint(node.Tile, activeChunks) ||
                    (node.RoleAnchorId.Length != 0 &&
                     (!roles.TryGetValue(node.RoleAnchorId, out role) ||
                      !string.Equals(role.TraversalNodeId, node.NodeId, StringComparison.Ordinal) ||
                      role.Tile != node.Tile)))
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.InvalidRoleAnchor,
                        nodePath,
                        "Node tile and optional role link must match an active-footprint role anchor.");
                }
            }

            foreach (var role in roles.Values)
            {
                TraversalNode node;
                if (!nodes.TryGetValue(role.TraversalNodeId, out node) ||
                    !string.Equals(node.RoleAnchorId, role.AnchorId, StringComparison.Ordinal) ||
                    node.Tile != role.Tile)
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.MissingNodeReference,
                        path + ".roles[" + role.AnchorId + "]",
                        role.TraversalNodeId);
                }
            }

            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            var usableEdges = new List<TraversalEdge>();
            foreach (var edge in variant.Edges)
            {
                if (edge == null)
                {
                    Add(errors, TerrainClusterValidationErrorCode.MissingInput, path + ".edges", "Edge is required.");
                    continue;
                }

                var edgePath = path + ".edges[" + edge.EdgeId + "]";
                if (!IsStableId(edge.EdgeId, "EDGE_"))
                {
                    Add(errors, TerrainClusterValidationErrorCode.InvalidId, edgePath, edge.EdgeId);
                }

                if (!edgeIds.Add(edge.EdgeId))
                {
                    Add(errors, TerrainClusterValidationErrorCode.DuplicateNodeOrEdge, edgePath, "Edge ID occurs more than once.");
                }

                if (edge.GraphKind != TraversalGraphKind.Traversal)
                {
                    Add(errors, TerrainClusterValidationErrorCode.InvalidGraphKind, edgePath, Number((int)edge.GraphKind));
                }

                var fromExists = nodes.ContainsKey(edge.FromNodeId);
                var toExists = nodes.ContainsKey(edge.ToNodeId);
                if (!fromExists || !toExists)
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.MissingNodeReference,
                        edgePath,
                        edge.FromNodeId + "->" + edge.ToNodeId);
                }
                else
                {
                    usableEdges.Add(edge);
                    if (nodes[edge.FromNodeId].Tile != edge.StartTile || nodes[edge.ToNodeId].Tile != edge.EndTile)
                    {
                        Add(
                            errors,
                            TerrainClusterValidationErrorCode.EdgeAnchorMismatch,
                            edgePath,
                            "Start/End must exactly equal From/To node tiles.");
                    }
                }

                if (string.Equals(edge.FromNodeId, edge.ToNodeId, StringComparison.Ordinal))
                {
                    Add(errors, TerrainClusterValidationErrorCode.SelfEdge, edgePath, edge.FromNodeId);
                }

                if (!IsDefinedMovement(edge.MovementKind))
                {
                    Add(errors, TerrainClusterValidationErrorCode.InvalidMovement, edgePath, Number((int)edge.MovementKind));
                }

                if (edge.MinimumClearanceWidth < 1 || edge.MinimumClearanceHeight < 1)
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.InvalidClearance,
                        edgePath,
                        Number(edge.MinimumClearanceWidth) + "x" + Number(edge.MinimumClearanceHeight));
                }

                if (!edge.LandingTile.HasValue || !IsInsideFootprint(edge.LandingTile.Value, activeChunks))
                {
                    Add(errors, TerrainClusterValidationErrorCode.InvalidLanding, edgePath, "Explicit owned landing tile is required.");
                }

                if (!edge.RecoveryTile.HasValue || !IsInsideFootprint(edge.RecoveryTile.Value, activeChunks))
                {
                    Add(errors, TerrainClusterValidationErrorCode.InvalidRecovery, edgePath, "Explicit owned recovery tile is required.");
                }

                ValidateEnvelope(edge, edgePath, activeChunks, errors);
            }

            ValidateReachability(variant, path, roles, primaryPorts, nodes, usableEdges, errors);
        }

        private static void ValidateEnvelope(
            TraversalEdge edge,
            string path,
            ISet<ClusterChunkCoord> activeChunks,
            ICollection<TerrainClusterValidationError> errors)
        {
            if (edge.Envelope == null)
            {
                Add(errors, TerrainClusterValidationErrorCode.InvalidEnvelopeSet, path + ".envelope", "Envelope is required.");
                return;
            }

            ValidateEnvelopeSet(edge.Envelope.Centerline, "centerline", path, activeChunks, errors);
            ValidateEnvelopeSet(edge.Envelope.Floor, "floor", path, activeChunks, errors);
            ValidateEnvelopeSet(edge.Envelope.Clearance, "clearance", path, activeChunks, errors);
            ValidateEnvelopeSet(edge.Envelope.JumpArc, "jumpArc", path, activeChunks, errors);
            ValidateEnvelopeSet(edge.Envelope.DropColumn, "dropColumn", path, activeChunks, errors);
            ValidateEnvelopeSet(edge.Envelope.Landing, "landing", path, activeChunks, errors);
            ValidateEnvelopeSet(edge.Envelope.Recovery, "recovery", path, activeChunks, errors);

            if (edge.Envelope.Centerline.Count == 0 ||
                !edge.Envelope.Centerline.Contains(edge.StartTile) ||
                !edge.Envelope.Centerline.Contains(edge.EndTile) ||
                edge.Envelope.Clearance.Count == 0 ||
                !edge.LandingTile.HasValue ||
                !edge.Envelope.Landing.Contains(edge.LandingTile.Value) ||
                !edge.RecoveryTile.HasValue ||
                !edge.Envelope.Recovery.Contains(edge.RecoveryTile.Value))
            {
                Add(
                    errors,
                    TerrainClusterValidationErrorCode.InvalidEnvelopeSet,
                    path + ".envelope",
                    "Centerline, clearance, landing, and recovery protection are incomplete.");
            }

            if (edge.Envelope.Floor.Intersect(edge.Envelope.Clearance).Any())
            {
                Add(
                    errors,
                    TerrainClusterValidationErrorCode.FloorClearanceConflict,
                    path + ".envelope",
                    "Floor and Clearance cannot own the same tile.");
            }

            if (IsDefinedMovement(edge.MovementKind) && !MatchesMovementEnvelope(edge))
            {
                Add(
                    errors,
                    TerrainClusterValidationErrorCode.MovementEnvelopeMismatch,
                    path + ".envelope",
                    edge.MovementKind.ToString());
            }
        }

        private static void ValidateEnvelopeSet(
            IReadOnlyList<LocalTileCoord> coordinates,
            string setName,
            string path,
            ISet<ClusterChunkCoord> activeChunks,
            ICollection<TerrainClusterValidationError> errors)
        {
            var seen = new HashSet<LocalTileCoord>();
            foreach (var coordinate in coordinates)
            {
                if (!seen.Add(coordinate) || !IsInsideFootprint(coordinate, activeChunks))
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.InvalidEnvelopeSet,
                        path + ".envelope." + setName,
                        Coordinate(coordinate));
                }
            }
        }

        private static bool MatchesMovementEnvelope(TraversalEdge edge)
        {
            var envelope = edge.Envelope;
            var noJumpOrDrop = envelope.JumpArc.Count == 0 && envelope.DropColumn.Count == 0;
            switch (edge.MovementKind)
            {
                case TraversalMovementKind.Walk:
                    return envelope.Floor.Count != 0 && envelope.Clearance.Count != 0 && noJumpOrDrop;
                case TraversalMovementKind.Jump:
                    return envelope.JumpArc.Count != 0 && envelope.Landing.Count != 0 &&
                           envelope.Recovery.Count != 0 && envelope.DropColumn.Count == 0;
                case TraversalMovementKind.Drop:
                    return envelope.DropColumn.Count != 0 && envelope.Landing.Count != 0 &&
                           envelope.Recovery.Count != 0 && envelope.JumpArc.Count == 0;
                case TraversalMovementKind.Climb:
                    return envelope.Clearance.Count != 0 && noJumpOrDrop;
                case TraversalMovementKind.Slide:
                    return envelope.Floor.Count != 0 && envelope.Clearance.Count != 0 &&
                           envelope.Recovery.Count != 0 && noJumpOrDrop;
                case TraversalMovementKind.Bounce:
                    return envelope.JumpArc.Count != 0 && envelope.Landing.Count != 0 &&
                           envelope.Recovery.Count != 0 && envelope.DropColumn.Count == 0;
                default:
                    return false;
            }
        }

        private static void ValidateReachability(
            SpineVariant variant,
            string path,
            IReadOnlyDictionary<string, ClusterRoleAnchor> roles,
            IReadOnlyDictionary<ClusterPortKind, ClusterPort> primaryPorts,
            IReadOnlyDictionary<string, TraversalNode> nodes,
            IReadOnlyList<TraversalEdge> edges,
            ICollection<TerrainClusterValidationError> errors)
        {
            ClusterPort entryPort;
            ClusterPort exitPort;
            ClusterRoleAnchor entryRole;
            ClusterRoleAnchor exitRole;
            if (!primaryPorts.TryGetValue(ClusterPortKind.Entry, out entryPort) ||
                !primaryPorts.TryGetValue(ClusterPortKind.Exit, out exitPort) ||
                !roles.TryGetValue(entryPort.RoleAnchorId, out entryRole) ||
                !roles.TryGetValue(exitPort.RoleAnchorId, out exitRole) ||
                !nodes.ContainsKey(entryRole.TraversalNodeId) ||
                !nodes.ContainsKey(exitRole.TraversalNodeId))
            {
                return;
            }

            var reachable = ReachableNodes(entryRole.TraversalNodeId, edges);
            var mandatoryReachable = ReachableNodes(
                entryRole.TraversalNodeId,
                edges.Where(value => value.IsMandatory).ToArray());
            if (!mandatoryReachable.Contains(exitRole.TraversalNodeId))
            {
                Add(
                    errors,
                    TerrainClusterValidationErrorCode.MissingEntryExitPath,
                    path,
                    entryRole.TraversalNodeId + "->" + exitRole.TraversalNodeId);
            }

            foreach (var kind in new[]
                     {
                         ClusterRoleKind.Entry,
                         ClusterRoleKind.BuildUp,
                         ClusterRoleKind.Core,
                         ClusterRoleKind.Recovery,
                         ClusterRoleKind.Exit,
                     })
            {
                if (!roles.Values.Any(value => value.Role == kind && reachable.Contains(value.TraversalNodeId)))
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.MissingEntryExitPath,
                        path + ".roles[" + kind + "]",
                        "Required role is not reachable from Entry.");
                }
            }

            foreach (var node in variant.Nodes.Where(value => value != null && value.IsMandatory))
            {
                if (!reachable.Contains(node.NodeId))
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.UnreachableMandatoryElement,
                        path + ".nodes[" + node.NodeId + "]",
                        "Mandatory node is unreachable from Entry.");
                }
            }

            foreach (var edge in edges.Where(value => value.IsMandatory))
            {
                if (!reachable.Contains(edge.FromNodeId) || !reachable.Contains(edge.ToNodeId))
                {
                    Add(
                        errors,
                        TerrainClusterValidationErrorCode.UnreachableMandatoryElement,
                        path + ".edges[" + edge.EdgeId + "]",
                        "Mandatory edge belongs to an orphan component.");
                }
            }
        }

        private static HashSet<ClusterChunkCoord> ReachableChunks(ISet<ClusterChunkCoord> active)
        {
            var visited = new HashSet<ClusterChunkCoord>();
            var queue = new Queue<ClusterChunkCoord>();
            var first = active.First();
            visited.Add(first);
            queue.Enqueue(first);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in new[]
                         {
                             new ClusterChunkCoord(current.X - 1, current.Y),
                             new ClusterChunkCoord(current.X + 1, current.Y),
                             new ClusterChunkCoord(current.X, current.Y - 1),
                             new ClusterChunkCoord(current.X, current.Y + 1),
                         })
                {
                    if (active.Contains(neighbor) && visited.Add(neighbor)) queue.Enqueue(neighbor);
                }
            }

            return visited;
        }

        private static HashSet<string> ReachableNodes(
            string entryNodeId,
            IEnumerable<TraversalEdge> edges)
        {
            var adjacency = edges
                .Where(value => value != null)
                .GroupBy(value => value.FromNodeId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(value => value.ToNodeId).ToArray(),
                    StringComparer.Ordinal);
            var reachable = new HashSet<string>(StringComparer.Ordinal) { entryNodeId };
            var queue = new Queue<string>();
            queue.Enqueue(entryNodeId);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                string[] neighbors;
                if (!adjacency.TryGetValue(current, out neighbors)) continue;
                foreach (var neighbor in neighbors)
                {
                    if (reachable.Add(neighbor)) queue.Enqueue(neighbor);
                }
            }

            return reachable;
        }

        private static bool PointsOutsideFootprint(
            LocalTileCoord tile,
            ClusterPortSide side,
            ISet<ClusterChunkCoord> activeChunks)
        {
            if (!IsInsideFootprint(tile, activeChunks)) return false;
            LocalTileCoord neighbor;
            switch (side)
            {
                case ClusterPortSide.L: neighbor = new LocalTileCoord(tile.X - 1, tile.Y); break;
                case ClusterPortSide.R: neighbor = new LocalTileCoord(tile.X + 1, tile.Y); break;
                case ClusterPortSide.U: neighbor = new LocalTileCoord(tile.X, tile.Y + 1); break;
                case ClusterPortSide.D: neighbor = new LocalTileCoord(tile.X, tile.Y - 1); break;
                default: return false;
            }

            return !IsInsideFootprint(neighbor, activeChunks);
        }

        private static bool IsInsideFootprint(
            LocalTileCoord tile,
            ISet<ClusterChunkCoord> activeChunks)
        {
            if (tile.X < 0 || tile.Y < 0) return false;
            var chunk = new ClusterChunkCoord(
                tile.X / WorldGenConstants.MicroChunkWidthTiles,
                tile.Y / WorldGenConstants.MicroChunkHeightTiles);
            return activeChunks.Contains(chunk);
        }

        private static bool IsStableId(string value, string requiredPrefix)
        {
            if (string.IsNullOrEmpty(value) ||
                !value.StartsWith(requiredPrefix, StringComparison.Ordinal) ||
                value.Length <= requiredPrefix.Length)
            {
                return false;
            }

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') &&
                    character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsDefinedRole(ClusterRoleKind value)
        {
            return value >= ClusterRoleKind.Entry && value <= ClusterRoleKind.Exit;
        }

        private static bool IsDefinedPortKind(ClusterPortKind value)
        {
            return value == ClusterPortKind.Entry || value == ClusterPortKind.Exit;
        }

        private static bool IsDefinedPortSide(ClusterPortSide value)
        {
            return value >= ClusterPortSide.L && value <= ClusterPortSide.D;
        }

        private static bool IsDefinedMovement(TraversalMovementKind value)
        {
            return value >= TraversalMovementKind.Walk && value <= TraversalMovementKind.Bounce;
        }

        private static string ChunkPath(ClusterChunkCoord coordinate)
        {
            return "footprint.activeChunks[" + Number(coordinate.X) + "," + Number(coordinate.Y) + "]";
        }

        private static string Coordinate(LocalTileCoord coordinate)
        {
            return Number(coordinate.X) + "," + Number(coordinate.Y);
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static void Add(
            ICollection<TerrainClusterValidationError> errors,
            TerrainClusterValidationErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new TerrainClusterValidationError(code, path, detail));
        }
    }
}
