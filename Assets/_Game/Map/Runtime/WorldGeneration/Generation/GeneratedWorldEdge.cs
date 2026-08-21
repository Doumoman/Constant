using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class GeneratedWorldEdge
    {
        public GeneratedWorldEdge(ulong seed, SectorCoord from, string side, SectorCoord to, string edgeLayer,
            string traversalKind, bool open, string edgeSignatureId, int costTiles)
        {
            if (side != "L" && side != "R" && side != "U" && side != "D") throw new ArgumentException("Side must be L/R/U/D.", nameof(side));
            if (costTiles < 0) throw new ArgumentOutOfRangeException(nameof(costTiles));
            Seed = seed; From = from; Side = side; To = to;
            EdgeLayer = edgeLayer ?? throw new ArgumentNullException(nameof(edgeLayer));
            TraversalKind = traversalKind ?? throw new ArgumentNullException(nameof(traversalKind));
            Open = open;
            EdgeSignatureId = edgeSignatureId ?? throw new ArgumentNullException(nameof(edgeSignatureId));
            CostTiles = costTiles;
        }

        public ulong Seed { get; }
        public SectorCoord From { get; }
        public string Side { get; }
        public SectorCoord To { get; }
        public string EdgeLayer { get; }
        public string TraversalKind { get; }
        public bool Open { get; }
        public string EdgeSignatureId { get; }
        public int CostTiles { get; }
    }
}
