using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteGraphEdge
    {
        internal MandatoryRouteGraphEdge(MandatoryRouteGraphEdgeId edgeId, MandatoryRouteGraphNodeId fromNodeId,
            MandatoryRouteGraphNodeId toNodeId, int fromSectorIndex, int toSectorIndex, string side, string reverseSide,
            string traversalKind, string edgeSignatureId, int costTiles, string sourceArtifactId)
        {
            if (costTiles < 0) throw new ArgumentOutOfRangeException(nameof(costTiles));
            EdgeId = edgeId; FromNodeId = fromNodeId; ToNodeId = toNodeId;
            FromSectorIndex = fromSectorIndex; ToSectorIndex = toSectorIndex;
            Side = side ?? throw new ArgumentNullException(nameof(side));
            ReverseSide = reverseSide ?? throw new ArgumentNullException(nameof(reverseSide));
            TraversalKind = traversalKind ?? throw new ArgumentNullException(nameof(traversalKind));
            EdgeSignatureId = edgeSignatureId ?? throw new ArgumentNullException(nameof(edgeSignatureId));
            CostTiles = costTiles;
            SourceArtifactId = sourceArtifactId ?? throw new ArgumentNullException(nameof(sourceArtifactId));
        }

        public MandatoryRouteGraphEdgeId EdgeId { get; }
        public MandatoryRouteGraphNodeId FromNodeId { get; }
        public MandatoryRouteGraphNodeId ToNodeId { get; }
        public int FromSectorIndex { get; }
        public int ToSectorIndex { get; }
        public string Side { get; }
        public string ReverseSide { get; }
        public string Layer => "MANDATORY";
        public string TraversalKind { get; }
        public string EdgeSignatureId { get; }
        public int CostTiles { get; }
        public string SourceArtifactId { get; }
        public bool Open => true;
    }
}
