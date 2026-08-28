using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public sealed class ClusterSectorSocketEvidence
    {
        public ClusterSectorSocketEvidence(
            string sectorRecipeId,
            string socketId,
            ClusterPortSide side,
            int owningRouteType,
            bool mandatoryAllowed,
            ClusterPortKind boundPortKind)
        {
            SectorRecipeId = sectorRecipeId ?? string.Empty;
            SocketId = socketId ?? string.Empty;
            Side = side;
            OwningRouteType = owningRouteType;
            MandatoryAllowed = mandatoryAllowed;
            BoundPortKind = boundPortKind;
        }

        public string SectorRecipeId { get; }
        public string SocketId { get; }
        public string StableIdentity => SectorRecipeId + "/" + SocketId;
        public ClusterPortSide Side { get; }
        public int OwningRouteType { get; }
        public bool MandatoryAllowed { get; }
        public ClusterPortKind BoundPortKind { get; }
    }

    public sealed class ClusterSectorSocketConnection
    {
        public ClusterSectorSocketConnection(
            ClusterSectorSocketEvidence evidence,
            string portId,
            string roleAnchorId,
            string traversalNodeId,
            LocalTileCoord compiledCoordinate,
            ClusterPortSide compiledOutwardSide)
        {
            Evidence = evidence;
            PortId = portId ?? string.Empty;
            RoleAnchorId = roleAnchorId ?? string.Empty;
            TraversalNodeId = traversalNodeId ?? string.Empty;
            CompiledCoordinate = compiledCoordinate;
            CompiledOutwardSide = compiledOutwardSide;
        }

        public ClusterSectorSocketEvidence Evidence { get; }
        public ClusterPortKind PortKind => Evidence.BoundPortKind;
        public string PortId { get; }
        public string RoleAnchorId { get; }
        public string TraversalNodeId { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public ClusterPortSide CompiledOutwardSide { get; }
    }

    public sealed class TerrainClusterRoleSocketContract
    {
        private readonly ReadOnlyCollection<ProjectedClusterRoleAnchor> roles;
        private readonly ReadOnlyCollection<ProjectedClusterPort> ports;
        private readonly ReadOnlyCollection<ProjectedRoleSpineLink> roleSpineLinks;
        private readonly ReadOnlyCollection<ClusterSectorSocketConnection> socketConnections;
        private readonly ReadOnlyDictionary<string, ProjectedClusterRoleAnchor> rolesById;
        private readonly ReadOnlyDictionary<ClusterPortKind, ProjectedClusterPort> portsByKind;

        internal TerrainClusterRoleSocketContract(
            TerrainClusterId clusterId,
            string sourceContractDigest,
            string localCanvasDigest,
            ClusterFootprintTransform transform,
            IEnumerable<ProjectedClusterRoleAnchor> roles,
            IEnumerable<ProjectedClusterPort> ports,
            IEnumerable<ProjectedRoleSpineLink> roleSpineLinks,
            IEnumerable<ClusterSectorSocketConnection> socketConnections,
            string canonicalDigest)
        {
            ClusterId = clusterId;
            SourceContractDigest = sourceContractDigest ?? string.Empty;
            LocalCanvasDigest = localCanvasDigest ?? string.Empty;
            Transform = transform;
            CanonicalDigest = canonicalDigest ?? string.Empty;

            var roleCopy = roles.OrderBy(value => value.AnchorId, StringComparer.Ordinal).ToArray();
            this.roles = new ReadOnlyCollection<ProjectedClusterRoleAnchor>(roleCopy);
            var portCopy = ports.OrderBy(value => value.Kind).ThenBy(value => value.PortId, StringComparer.Ordinal).ToArray();
            this.ports = new ReadOnlyCollection<ProjectedClusterPort>(portCopy);
            var linkCopy = roleSpineLinks.OrderBy(value => value.VariantId)
                .ThenBy(value => value.RoleAnchorId, StringComparer.Ordinal).ToArray();
            this.roleSpineLinks = new ReadOnlyCollection<ProjectedRoleSpineLink>(linkCopy);
            var connectionCopy = socketConnections.OrderBy(value => value.PortKind)
                .ThenBy(value => value.Evidence.StableIdentity, StringComparer.Ordinal).ToArray();
            this.socketConnections = new ReadOnlyCollection<ClusterSectorSocketConnection>(connectionCopy);
            rolesById = new ReadOnlyDictionary<string, ProjectedClusterRoleAnchor>(
                roleCopy.ToDictionary(value => value.AnchorId, StringComparer.Ordinal));
            portsByKind = new ReadOnlyDictionary<ClusterPortKind, ProjectedClusterPort>(
                portCopy.ToDictionary(value => value.Kind));
        }

        public TerrainClusterId ClusterId { get; }
        public string SourceContractDigest { get; }
        public string LocalCanvasDigest { get; }
        public ClusterFootprintTransform Transform { get; }
        public IReadOnlyList<ProjectedClusterRoleAnchor> Roles => roles;
        public IReadOnlyList<ProjectedClusterPort> Ports => ports;
        public IReadOnlyList<ProjectedRoleSpineLink> RoleSpineLinks => roleSpineLinks;
        public IReadOnlyList<ClusterSectorSocketConnection> SocketConnections => socketConnections;
        public string CanonicalDigest { get; }

        public bool TryGetRole(string anchorId, out ProjectedClusterRoleAnchor role)
        {
            return rolesById.TryGetValue(anchorId ?? string.Empty, out role);
        }

        public bool TryGetPrimaryPort(ClusterPortKind kind, out ProjectedClusterPort port)
        {
            return portsByKind.TryGetValue(kind, out port);
        }
    }

    public sealed class TerrainClusterRoleSocketCompileRequest
    {
        private readonly ReadOnlyCollection<TerrainClusterId> sixChunkAllowlist;
        private readonly ReadOnlyCollection<ClusterSectorSocketEvidence> socketEvidence;

        public TerrainClusterRoleSocketCompileRequest(
            TerrainClusterContract sourceContract,
            string sourceContractCanonicalDigest,
            TerrainClusterLocalCanvas localCanvas,
            string localCanvasCanonicalDigest,
            IEnumerable<ClusterSectorSocketEvidence> socketEvidence,
            IEnumerable<TerrainClusterId> sixChunkAllowlist = null)
        {
            SourceContract = sourceContract;
            SourceContractCanonicalDigest = sourceContractCanonicalDigest ?? string.Empty;
            LocalCanvas = localCanvas;
            LocalCanvasCanonicalDigest = localCanvasCanonicalDigest ?? string.Empty;
            var allowlistCopy = (sixChunkAllowlist ?? Array.Empty<TerrainClusterId>())
                .Distinct().OrderBy(value => value).ToArray();
            this.sixChunkAllowlist = new ReadOnlyCollection<TerrainClusterId>(allowlistCopy);
            var evidenceCopy = (socketEvidence ?? Array.Empty<ClusterSectorSocketEvidence>()).ToArray();
            Array.Sort(evidenceCopy, CompareEvidence);
            this.socketEvidence = new ReadOnlyCollection<ClusterSectorSocketEvidence>(evidenceCopy);
        }

        public TerrainClusterContract SourceContract { get; }
        public string SourceContractCanonicalDigest { get; }
        public TerrainClusterLocalCanvas LocalCanvas { get; }
        public string LocalCanvasCanonicalDigest { get; }
        public IReadOnlyList<ClusterSectorSocketEvidence> SocketEvidence => socketEvidence;
        public IReadOnlyList<TerrainClusterId> SixChunkAllowlist => sixChunkAllowlist;

        private static int CompareEvidence(
            ClusterSectorSocketEvidence left,
            ClusterSectorSocketEvidence right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            var comparison = left.BoundPortKind.CompareTo(right.BoundPortKind);
            return comparison != 0 ? comparison :
                string.Compare(left.StableIdentity, right.StableIdentity, StringComparison.Ordinal);
        }
    }

    public enum TerrainClusterRoleSocketCompileErrorCode
    {
        MissingInput = 1,
        InvalidSourceContract = 2,
        LocalCanvasIdentityMismatch = 3,
        LocalCanvasDigestMismatch = 4,
        MissingRequiredRole = 5,
        RoleProjectionMissing = 6,
        RoleOutsideActiveMask = 7,
        MissingOrDuplicatePrimaryPort = 8,
        PortRoleMismatch = 9,
        InvalidTransformedPortSide = 10,
        PortNotOutward = 11,
        MissingVariantRoleNode = 12,
        RoleNodeCoordinateMismatch = 13,
        MissingSocketBinding = 14,
        DuplicateSocketBinding = 15,
        SocketSideMismatch = 16,
        RouteTypeIncompatible = 17,
        MandatorySocketRejected = 18,
        EntryExitConnectionMismatch = 19,
        NonCanonicalPublication = 20,
    }

    public sealed class TerrainClusterRoleSocketCompileError :
        IEquatable<TerrainClusterRoleSocketCompileError>,
        IComparable<TerrainClusterRoleSocketCompileError>
    {
        public TerrainClusterRoleSocketCompileError(
            TerrainClusterRoleSocketCompileErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public TerrainClusterRoleSocketCompileErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(TerrainClusterRoleSocketCompileError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0 ? comparison :
                string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(TerrainClusterRoleSocketCompileError other)
        {
            return other != null && Code == other.Code &&
                string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TerrainClusterRoleSocketCompileError);
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

    public sealed class TerrainClusterRoleSocketCompileResult
    {
        private static readonly IReadOnlyList<ProjectedClusterRoleAnchor> EmptyRoles =
            Array.Empty<ProjectedClusterRoleAnchor>();
        private static readonly IReadOnlyList<ProjectedClusterPort> EmptyPorts =
            Array.Empty<ProjectedClusterPort>();
        private static readonly IReadOnlyList<ProjectedRoleSpineLink> EmptyLinks =
            Array.Empty<ProjectedRoleSpineLink>();
        private static readonly IReadOnlyList<ClusterSectorSocketConnection> EmptyConnections =
            Array.Empty<ClusterSectorSocketConnection>();
        private readonly ReadOnlyCollection<TerrainClusterRoleSocketCompileError> errors;

        internal TerrainClusterRoleSocketCompileResult(
            TerrainClusterRoleSocketContract contract,
            IEnumerable<TerrainClusterRoleSocketCompileError> errors)
        {
            var copy = (errors ?? Array.Empty<TerrainClusterRoleSocketCompileError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<TerrainClusterRoleSocketCompileError>(copy);
            Contract = copy.Length == 0 ? contract : null;
        }

        public bool IsSuccess => Contract != null && errors.Count == 0;
        public TerrainClusterRoleSocketContract Contract { get; }
        public IReadOnlyList<TerrainClusterRoleSocketCompileError> Errors => errors;
        public IReadOnlyList<ProjectedClusterRoleAnchor> Roles => Contract == null ? EmptyRoles : Contract.Roles;
        public IReadOnlyList<ProjectedClusterPort> Ports => Contract == null ? EmptyPorts : Contract.Ports;
        public IReadOnlyList<ProjectedRoleSpineLink> RoleSpineLinks => Contract == null ? EmptyLinks : Contract.RoleSpineLinks;
        public IReadOnlyList<ClusterSectorSocketConnection> SocketConnections =>
            Contract == null ? EmptyConnections : Contract.SocketConnections;
        public string CanonicalDigest => Contract == null ? string.Empty : Contract.CanonicalDigest;
    }
}
