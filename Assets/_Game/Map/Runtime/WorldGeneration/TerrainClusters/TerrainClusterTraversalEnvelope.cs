using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public enum CompiledTraversalEnvelopeSetKind
    {
        Centerline = 1,
        Floor = 2,
        Clearance = 3,
        JumpArc = 4,
        DropColumn = 5,
        Landing = 6,
        Recovery = 7,
    }

    public sealed class CompiledTraversalEnvelopeTile
    {
        internal CompiledTraversalEnvelopeTile(
            CompiledTraversalEnvelopeSetKind setKind,
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            ClusterChunkCoord owningCompiledChunk)
        {
            SetKind = setKind;
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            OwningCompiledChunk = owningCompiledChunk;
        }

        public CompiledTraversalEnvelopeSetKind SetKind { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public ClusterChunkCoord OwningCompiledChunk { get; }
    }

    public sealed class CompiledTraversalEnvelope
    {
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeTile> centerline;
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeTile> floor;
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeTile> clearance;
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeTile> jumpArc;
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeTile> dropColumn;
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeTile> landing;
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeTile> recovery;
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeTile> allTiles;

        internal CompiledTraversalEnvelope(
            IEnumerable<CompiledTraversalEnvelopeTile> centerline,
            IEnumerable<CompiledTraversalEnvelopeTile> floor,
            IEnumerable<CompiledTraversalEnvelopeTile> clearance,
            IEnumerable<CompiledTraversalEnvelopeTile> jumpArc,
            IEnumerable<CompiledTraversalEnvelopeTile> dropColumn,
            IEnumerable<CompiledTraversalEnvelopeTile> landing,
            IEnumerable<CompiledTraversalEnvelopeTile> recovery)
        {
            this.centerline = Copy(centerline, CompiledTraversalEnvelopeSetKind.Centerline);
            this.floor = Copy(floor, CompiledTraversalEnvelopeSetKind.Floor);
            this.clearance = Copy(clearance, CompiledTraversalEnvelopeSetKind.Clearance);
            this.jumpArc = Copy(jumpArc, CompiledTraversalEnvelopeSetKind.JumpArc);
            this.dropColumn = Copy(dropColumn, CompiledTraversalEnvelopeSetKind.DropColumn);
            this.landing = Copy(landing, CompiledTraversalEnvelopeSetKind.Landing);
            this.recovery = Copy(recovery, CompiledTraversalEnvelopeSetKind.Recovery);
            allTiles = new ReadOnlyCollection<CompiledTraversalEnvelopeTile>(
                this.centerline.Concat(this.floor).Concat(this.clearance)
                    .Concat(this.jumpArc).Concat(this.dropColumn)
                    .Concat(this.landing).Concat(this.recovery)
                    .OrderBy(value => value.SetKind)
                    .ThenBy(value => value.CompiledCoordinate.Y)
                    .ThenBy(value => value.CompiledCoordinate.X)
                    .ToArray());
        }

        public IReadOnlyList<CompiledTraversalEnvelopeTile> Centerline => centerline;
        public IReadOnlyList<CompiledTraversalEnvelopeTile> Floor => floor;
        public IReadOnlyList<CompiledTraversalEnvelopeTile> Clearance => clearance;
        public IReadOnlyList<CompiledTraversalEnvelopeTile> JumpArc => jumpArc;
        public IReadOnlyList<CompiledTraversalEnvelopeTile> DropColumn => dropColumn;
        public IReadOnlyList<CompiledTraversalEnvelopeTile> Landing => landing;
        public IReadOnlyList<CompiledTraversalEnvelopeTile> Recovery => recovery;
        public IReadOnlyList<CompiledTraversalEnvelopeTile> AllTiles => allTiles;

        public IReadOnlyList<CompiledTraversalEnvelopeTile> GetTiles(
            CompiledTraversalEnvelopeSetKind setKind)
        {
            switch (setKind)
            {
                case CompiledTraversalEnvelopeSetKind.Centerline: return centerline;
                case CompiledTraversalEnvelopeSetKind.Floor: return floor;
                case CompiledTraversalEnvelopeSetKind.Clearance: return clearance;
                case CompiledTraversalEnvelopeSetKind.JumpArc: return jumpArc;
                case CompiledTraversalEnvelopeSetKind.DropColumn: return dropColumn;
                case CompiledTraversalEnvelopeSetKind.Landing: return landing;
                case CompiledTraversalEnvelopeSetKind.Recovery: return recovery;
                default: return Array.Empty<CompiledTraversalEnvelopeTile>();
            }
        }

        private static ReadOnlyCollection<CompiledTraversalEnvelopeTile> Copy(
            IEnumerable<CompiledTraversalEnvelopeTile> source,
            CompiledTraversalEnvelopeSetKind expectedKind)
        {
            var copy = (source ?? Array.Empty<CompiledTraversalEnvelopeTile>())
                .Where(value => value != null && value.SetKind == expectedKind)
                .OrderBy(value => value.CompiledCoordinate.Y)
                .ThenBy(value => value.CompiledCoordinate.X)
                .ThenBy(value => value.SourceCoordinate.Y)
                .ThenBy(value => value.SourceCoordinate.X)
                .ToArray();
            return new ReadOnlyCollection<CompiledTraversalEnvelopeTile>(copy);
        }
    }

    public enum ClusterTraversalProtectionSourceKind
    {
        RouteSpine = 1,
        TraversalEnvelope = 2,
    }

    public sealed class ClusterTraversalProtectedTileProvenance :
        IEquatable<ClusterTraversalProtectedTileProvenance>,
        IComparable<ClusterTraversalProtectedTileProvenance>
    {
        internal ClusterTraversalProtectedTileProvenance(
            ClusterTraversalProtectionSourceKind sourceKind,
            SpineVariantId variantId,
            string nodeId,
            string edgeId,
            CompiledTraversalEnvelopeSetKind? envelopeSetKind,
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            bool isMandatory)
        {
            SourceKind = sourceKind;
            VariantId = variantId;
            NodeId = nodeId ?? string.Empty;
            EdgeId = edgeId ?? string.Empty;
            EnvelopeSetKind = envelopeSetKind;
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            IsMandatory = isMandatory;
        }

        public ClusterTraversalProtectionSourceKind SourceKind { get; }
        public SpineVariantId VariantId { get; }
        public string NodeId { get; }
        public string EdgeId { get; }
        public CompiledTraversalEnvelopeSetKind? EnvelopeSetKind { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public bool IsMandatory { get; }

        public int CompareTo(ClusterTraversalProtectedTileProvenance other)
        {
            if (other == null) return -1;
            var comparison = ((int)SourceKind).CompareTo((int)other.SourceKind);
            if (comparison != 0) return comparison;
            comparison = VariantId.CompareTo(other.VariantId);
            if (comparison != 0) return comparison;
            comparison = string.Compare(NodeId, other.NodeId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(EdgeId, other.EdgeId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = Nullable.Compare(EnvelopeSetKind, other.EnvelopeSetKind);
            if (comparison != 0) return comparison;
            comparison = SourceCoordinate.Y.CompareTo(other.SourceCoordinate.Y);
            if (comparison != 0) return comparison;
            comparison = SourceCoordinate.X.CompareTo(other.SourceCoordinate.X);
            if (comparison != 0) return comparison;
            comparison = CompiledCoordinate.Y.CompareTo(other.CompiledCoordinate.Y);
            if (comparison != 0) return comparison;
            comparison = CompiledCoordinate.X.CompareTo(other.CompiledCoordinate.X);
            return comparison != 0 ? comparison : IsMandatory.CompareTo(other.IsMandatory);
        }

        public bool Equals(ClusterTraversalProtectedTileProvenance other)
        {
            return other != null && SourceKind == other.SourceKind &&
                VariantId == other.VariantId &&
                string.Equals(NodeId, other.NodeId, StringComparison.Ordinal) &&
                string.Equals(EdgeId, other.EdgeId, StringComparison.Ordinal) &&
                EnvelopeSetKind == other.EnvelopeSetKind &&
                SourceCoordinate == other.SourceCoordinate &&
                CompiledCoordinate == other.CompiledCoordinate &&
                IsMandatory == other.IsMandatory;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ClusterTraversalProtectedTileProvenance);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)SourceKind;
                hash = (hash * 397) ^ VariantId.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(NodeId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(EdgeId);
                hash = (hash * 397) ^ (EnvelopeSetKind.HasValue ? (int)EnvelopeSetKind.Value : 0);
                hash = (hash * 397) ^ SourceCoordinate.GetHashCode();
                hash = (hash * 397) ^ CompiledCoordinate.GetHashCode();
                return (hash * 397) ^ IsMandatory.GetHashCode();
            }
        }
    }

    public sealed class ClusterTraversalProtectedTile
    {
        private readonly ReadOnlyCollection<ClusterTraversalProtectedTileProvenance> provenance;

        internal ClusterTraversalProtectedTile(
            LocalTileCoord compiledCoordinate,
            IEnumerable<ClusterTraversalProtectedTileProvenance> provenance)
        {
            CompiledCoordinate = compiledCoordinate;
            var copy = (provenance ?? Array.Empty<ClusterTraversalProtectedTileProvenance>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.provenance = new ReadOnlyCollection<ClusterTraversalProtectedTileProvenance>(copy);
        }

        public LocalTileCoord CompiledCoordinate { get; }
        public IReadOnlyList<ClusterTraversalProtectedTileProvenance> Provenance => provenance;
    }

    public sealed class TerrainClusterTraversalCompilation
    {
        private readonly ReadOnlyCollection<CompiledClusterSpineVariant> variants;
        private readonly ReadOnlyCollection<CompiledTraversalNode> nodes;
        private readonly ReadOnlyCollection<CompiledTraversalEdge> edges;
        private readonly ReadOnlyCollection<CompiledTraversalEnvelope> envelopes;
        private readonly ReadOnlyCollection<ClusterTraversalProtectedTile> protectedTiles;
        private readonly ReadOnlyDictionary<SpineVariantId, CompiledClusterSpineVariant> variantsById;

        internal TerrainClusterTraversalCompilation(
            TerrainClusterId clusterId,
            string sourceContractDigest,
            string localCanvasDigest,
            string roleSocketContractDigest,
            ClusterFootprintTransform transform,
            IEnumerable<CompiledClusterSpineVariant> variants,
            IEnumerable<ClusterTraversalProtectedTile> protectedTiles,
            string canonicalDigest)
        {
            ClusterId = clusterId;
            SourceContractDigest = sourceContractDigest ?? string.Empty;
            LocalCanvasDigest = localCanvasDigest ?? string.Empty;
            RoleSocketContractDigest = roleSocketContractDigest ?? string.Empty;
            Transform = transform;
            CanonicalDigest = canonicalDigest ?? string.Empty;

            var variantCopy = (variants ?? Array.Empty<CompiledClusterSpineVariant>())
                .OrderBy(value => value.VariantId)
                .ToArray();
            this.variants = new ReadOnlyCollection<CompiledClusterSpineVariant>(variantCopy);
            nodes = new ReadOnlyCollection<CompiledTraversalNode>(variantCopy
                .SelectMany(value => value.Nodes)
                .OrderBy(value => value.VariantId)
                .ThenBy(value => value.NodeId, StringComparer.Ordinal)
                .ToArray());
            edges = new ReadOnlyCollection<CompiledTraversalEdge>(variantCopy
                .SelectMany(value => value.Edges)
                .OrderBy(value => value.VariantId)
                .ThenBy(value => value.EdgeId, StringComparer.Ordinal)
                .ToArray());
            envelopes = new ReadOnlyCollection<CompiledTraversalEnvelope>(
                this.edges.Select(value => value.Envelope).ToArray());
            var protectedCopy = (protectedTiles ?? Array.Empty<ClusterTraversalProtectedTile>())
                .OrderBy(value => value.CompiledCoordinate.Y)
                .ThenBy(value => value.CompiledCoordinate.X)
                .ToArray();
            this.protectedTiles = new ReadOnlyCollection<ClusterTraversalProtectedTile>(protectedCopy);
            variantsById = new ReadOnlyDictionary<SpineVariantId, CompiledClusterSpineVariant>(
                variantCopy.ToDictionary(value => value.VariantId));
        }

        public TerrainClusterId ClusterId { get; }
        public string SourceContractDigest { get; }
        public string LocalCanvasDigest { get; }
        public string RoleSocketContractDigest { get; }
        public ClusterFootprintTransform Transform { get; }
        public IReadOnlyList<CompiledClusterSpineVariant> Variants => variants;
        public IReadOnlyList<CompiledTraversalNode> Nodes => nodes;
        public IReadOnlyList<CompiledTraversalEdge> Edges => edges;
        public IReadOnlyList<CompiledTraversalEnvelope> Envelopes => envelopes;
        public IReadOnlyList<ClusterTraversalProtectedTile> ProtectedTiles => protectedTiles;
        public string CanonicalDigest { get; }

        public bool TryGetVariant(SpineVariantId variantId, out CompiledClusterSpineVariant variant)
        {
            return variantsById.TryGetValue(variantId, out variant);
        }
    }

    public sealed class TerrainClusterTraversalCompileRequest
    {
        private readonly ReadOnlyCollection<TerrainClusterId> sixChunkAllowlist;

        public TerrainClusterTraversalCompileRequest(
            TerrainClusterContract sourceContract,
            string sourceContractCanonicalDigest,
            TerrainClusterLocalCanvas localCanvas,
            string localCanvasCanonicalDigest,
            TerrainClusterRoleSocketContract roleSocketContract,
            string roleSocketContractCanonicalDigest,
            IEnumerable<TerrainClusterId> sixChunkAllowlist = null)
        {
            SourceContract = sourceContract;
            SourceContractCanonicalDigest = sourceContractCanonicalDigest ?? string.Empty;
            LocalCanvas = localCanvas;
            LocalCanvasCanonicalDigest = localCanvasCanonicalDigest ?? string.Empty;
            RoleSocketContract = roleSocketContract;
            RoleSocketContractCanonicalDigest = roleSocketContractCanonicalDigest ?? string.Empty;
            var copy = (sixChunkAllowlist ?? Array.Empty<TerrainClusterId>())
                .Distinct().OrderBy(value => value).ToArray();
            this.sixChunkAllowlist = new ReadOnlyCollection<TerrainClusterId>(copy);
        }

        public TerrainClusterContract SourceContract { get; }
        public string SourceContractCanonicalDigest { get; }
        public TerrainClusterLocalCanvas LocalCanvas { get; }
        public string LocalCanvasCanonicalDigest { get; }
        public TerrainClusterRoleSocketContract RoleSocketContract { get; }
        public string RoleSocketContractCanonicalDigest { get; }
        public IReadOnlyList<TerrainClusterId> SixChunkAllowlist => sixChunkAllowlist;
    }

    public enum TerrainClusterTraversalCompileErrorCode
    {
        MissingInput = 1,
        ArtifactIdentityMismatch = 2,
        ArtifactDigestMismatch = 3,
        InvalidSourceContract = 4,
        MissingVariant = 5,
        DuplicateNodeOrEdge = 6,
        NodeProjectionMissing = 7,
        NodeOutsideActiveMask = 8,
        MissingNodeReference = 9,
        SelfEdge = 10,
        EdgeAnchorMismatch = 11,
        InvalidMovement = 12,
        InvalidClearance = 13,
        LandingProjectionMissing = 14,
        RecoveryProjectionMissing = 15,
        EnvelopeProjectionMissing = 16,
        EnvelopeOutsideActiveMask = 17,
        MovementEnvelopeMismatch = 18,
        FloorClearanceConflict = 19,
        MissingEntryExitPath = 20,
        UnreachableMandatoryElement = 21,
        ProtectionProvenanceMismatch = 22,
        NonCanonicalPublication = 23,
    }

    public sealed class TerrainClusterTraversalCompileError :
        IEquatable<TerrainClusterTraversalCompileError>,
        IComparable<TerrainClusterTraversalCompileError>
    {
        public TerrainClusterTraversalCompileError(
            TerrainClusterTraversalCompileErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public TerrainClusterTraversalCompileErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(TerrainClusterTraversalCompileError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0 ? comparison :
                string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(TerrainClusterTraversalCompileError other)
        {
            return other != null && Code == other.Code &&
                string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TerrainClusterTraversalCompileError);
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

    public sealed class TerrainClusterTraversalCompileResult
    {
        private static readonly IReadOnlyList<CompiledClusterSpineVariant> EmptyVariants =
            Array.Empty<CompiledClusterSpineVariant>();
        private static readonly IReadOnlyList<CompiledTraversalNode> EmptyNodes =
            Array.Empty<CompiledTraversalNode>();
        private static readonly IReadOnlyList<CompiledTraversalEdge> EmptyEdges =
            Array.Empty<CompiledTraversalEdge>();
        private static readonly IReadOnlyList<CompiledTraversalEnvelope> EmptyEnvelopes =
            Array.Empty<CompiledTraversalEnvelope>();
        private static readonly IReadOnlyList<ClusterTraversalProtectedTile> EmptyProtectedTiles =
            Array.Empty<ClusterTraversalProtectedTile>();
        private readonly ReadOnlyCollection<TerrainClusterTraversalCompileError> errors;

        internal TerrainClusterTraversalCompileResult(
            TerrainClusterTraversalCompilation compilation,
            IEnumerable<TerrainClusterTraversalCompileError> errors)
        {
            var copy = (errors ?? Array.Empty<TerrainClusterTraversalCompileError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<TerrainClusterTraversalCompileError>(copy);
            Compilation = copy.Length == 0 ? compilation : null;
        }

        public bool IsSuccess => Compilation != null && errors.Count == 0;
        public TerrainClusterTraversalCompilation Compilation { get; }
        public IReadOnlyList<TerrainClusterTraversalCompileError> Errors => errors;
        public IReadOnlyList<CompiledClusterSpineVariant> Variants =>
            Compilation == null ? EmptyVariants : Compilation.Variants;
        public IReadOnlyList<CompiledTraversalNode> Nodes =>
            Compilation == null ? EmptyNodes : Compilation.Nodes;
        public IReadOnlyList<CompiledTraversalEdge> Edges =>
            Compilation == null ? EmptyEdges : Compilation.Edges;
        public IReadOnlyList<CompiledTraversalEnvelope> Envelopes =>
            Compilation == null ? EmptyEnvelopes : Compilation.Envelopes;
        public IReadOnlyList<ClusterTraversalProtectedTile> ProtectedTiles =>
            Compilation == null ? EmptyProtectedTiles : Compilation.ProtectedTiles;
        public string CanonicalDigest =>
            Compilation == null ? string.Empty : Compilation.CanonicalDigest;
    }
}
