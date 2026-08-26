using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryCoverageCandidateEvidence
    {
        public sealed class TileCell
        {
            public TileCell(
                int localX,
                int localY,
                string groundCode,
                string decorBackCode,
                string markerCode)
            {
                LocalX = localX;
                LocalY = localY;
                GroundCode = groundCode;
                DecorBackCode = decorBackCode;
                MarkerCode = markerCode;
            }

            public int LocalX { get; }
            public int LocalY { get; }
            public string GroundCode { get; }
            public string DecorBackCode { get; }
            public string MarkerCode { get; }
            public int CoordinateKey => LocalY * 12 + LocalX;
        }

        public sealed class Socket
        {
            public Socket(
                string socketId,
                string side,
                string traversalKind,
                bool mandatoryAllowed,
                string toolRequirement,
                string edgeSignatureId,
                string routeLayer,
                int minimumSafeTiles)
            {
                SocketId = socketId;
                Side = side;
                TraversalKind = traversalKind;
                MandatoryAllowed = mandatoryAllowed;
                ToolRequirement = toolRequirement;
                EdgeSignatureId = edgeSignatureId;
                RouteLayer = routeLayer;
                MinimumSafeTiles = minimumSafeTiles;
            }

            public string SocketId { get; }
            public string Side { get; }
            public string TraversalKind { get; }
            public bool MandatoryAllowed { get; }
            public string ToolRequirement { get; }
            public string EdgeSignatureId { get; }
            public string RouteLayer { get; }
            public int MinimumSafeTiles { get; }
        }

        private readonly IReadOnlyList<TileCell> tileCells;
        private readonly IReadOnlyList<Socket> sockets;

        public MoonpalaceBoundaryCoverageCandidateEvidence(
            string candidateId,
            string microchunkId,
            string pairRuleId,
            string biomeAId,
            string biomeBId,
            string profileId,
            MoonpalaceBoundaryOrientation orientation,
            int routeType,
            string entryEdgeSignatureId,
            string exitEdgeSignatureId,
            int weight,
            bool reversible,
            bool active,
            bool mandatoryAllowed,
            string toolRequirement,
            int widthTiles,
            int heightTiles,
            string usageClass,
            string microchunkBiomeIds,
            string routeRoles,
            bool tileDataComplete,
            bool microchunkActive,
            IEnumerable<TileCell> tileCells,
            IEnumerable<Socket> sockets)
        {
            if (tileCells == null) throw new ArgumentNullException(nameof(tileCells));
            if (sockets == null) throw new ArgumentNullException(nameof(sockets));

            CandidateId = candidateId;
            MicrochunkId = microchunkId;
            PairRuleId = pairRuleId;
            BiomeAId = biomeAId;
            BiomeBId = biomeBId;
            ProfileId = profileId;
            Orientation = orientation;
            RouteType = routeType;
            EntryEdgeSignatureId = entryEdgeSignatureId;
            ExitEdgeSignatureId = exitEdgeSignatureId;
            Weight = weight;
            Reversible = reversible;
            Active = active;
            MandatoryAllowed = mandatoryAllowed;
            ToolRequirement = toolRequirement;
            WidthTiles = widthTiles;
            HeightTiles = heightTiles;
            UsageClass = usageClass;
            MicrochunkBiomeIds = microchunkBiomeIds;
            RouteRoles = routeRoles;
            TileDataComplete = tileDataComplete;
            MicrochunkActive = microchunkActive;
            this.tileCells = Snapshot(tileCells, nameof(tileCells));
            this.sockets = Snapshot(sockets, nameof(sockets));
        }

        public string CandidateId { get; }
        public string MicrochunkId { get; }
        public string PairRuleId { get; }
        public string BiomeAId { get; }
        public string BiomeBId { get; }
        public string ProfileId { get; }
        public MoonpalaceBoundaryOrientation Orientation { get; }
        public int RouteType { get; }
        public string EntryEdgeSignatureId { get; }
        public string ExitEdgeSignatureId { get; }
        public int Weight { get; }
        public bool Reversible { get; }
        public bool Active { get; }
        public bool MandatoryAllowed { get; }
        public string ToolRequirement { get; }
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public string UsageClass { get; }
        public string MicrochunkBiomeIds { get; }
        public string RouteRoles { get; }
        public bool TileDataComplete { get; }
        public bool MicrochunkActive { get; }
        public IReadOnlyList<TileCell> TileCells => tileCells;
        public IReadOnlyList<Socket> Sockets => sockets;

        public int WarningMarkerCategoryCount(string foregroundA, string foregroundB, string backgroundA, string backgroundB)
        {
            var tileEvidence = tileCells.Any(cell => cell.GroundCode == foregroundA) &&
                               tileCells.Any(cell => cell.GroundCode == foregroundB);
            var backgroundEvidence = tileCells.Any(cell => cell.DecorBackCode == backgroundA) &&
                                     tileCells.Any(cell => cell.DecorBackCode == backgroundB);
            return (tileEvidence ? 1 : 0) + (backgroundEvidence ? 1 : 0);
        }

        private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> source, string parameterName)
        {
            var values = source.ToArray();
            if (values.Any(value => value == null))
            {
                throw new ArgumentException("Evidence collections cannot contain null values.", parameterName);
            }
            return new ReadOnlyCollection<T>(values);
        }
    }
}
