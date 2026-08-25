using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public static class MoonpalaceMillDoughBoundaryAuthoringContract
    {
        public const string PairRuleId = "PAIR_MILL_DOUGH";
        public const string BiomeAId = "BIO_ABANDONED_MILL";
        public const string BiomeBId = "BIO_MOON_DOUGH";
        public const string HorizontalEdgeSignatureId = "EDGE_H_MID_WALK";
        public const string VerticalEdgeSignatureId = "EDGE_V_CENTER_CLIMB";
        public const string NoToolRequirement = "NONE";
        public const int CandidateCount = 5;
        public const int CellsPerMicrochunk = 96;
        public const int TileRowCount = CandidateCount * CellsPerMicrochunk;
        public const int SocketCount = CandidateCount * 2;

        public static MoonpalaceBiomePair Pair { get; } = new MoonpalaceBiomePair(
            MoonpalaceBiomeId.AbandonedMill,
            MoonpalaceBiomeId.MoonDough);

        public static IReadOnlyList<string> CandidateIds { get; } =
            new ReadOnlyCollection<string>(new[]
            {
                "BCH_MILL_DOUGH_H_TUNNEL_01",
                "BCH_MILL_DOUGH_V_TUNNEL_01",
                "BCH_MILL_DOUGH_V_LAYER_01",
                "BCH_MILL_DOUGH_H_RUIN_01",
                "BCH_MILL_DOUGH_V_RUIN_01",
            });

        public static IReadOnlyList<string> MicrochunkIds { get; } =
            new ReadOnlyCollection<string>(new[]
            {
                "MC_BOUND_MILL_DOUGH_H_TUNNEL_01",
                "MC_BOUND_MILL_DOUGH_V_TUNNEL_01",
                "MC_BOUND_MILL_DOUGH_V_LAYER_01",
                "MC_BOUND_MILL_DOUGH_H_RUIN_01",
                "MC_BOUND_MILL_DOUGH_V_RUIN_01",
            });

        public static IReadOnlyList<string> ProfileIds { get; } =
            new ReadOnlyCollection<string>(new[]
            {
                "BOUND_TUNNEL",
                "BOUND_LAYER",
                "BOUND_RUIN",
            });

        public static bool IsOwnedCandidate(string candidateId)
        {
            return CandidateIds.Contains(candidateId, StringComparer.Ordinal);
        }

        public static bool IsOwnedMicrochunk(string microchunkId)
        {
            return MicrochunkIds.Contains(microchunkId, StringComparer.Ordinal);
        }
    }

    public sealed class MoonpalaceMillDoughBoundaryCandidateRow
    {
        public MoonpalaceMillDoughBoundaryCandidateRow(
            string candidateId,
            string microchunkId,
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
            string toolRequirement)
        {
            CandidateId = candidateId;
            MicrochunkId = microchunkId;
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
        }

        public string CandidateId { get; }
        public string MicrochunkId { get; }
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
    }

    public sealed class MoonpalaceMillDoughMicrochunkRow
    {
        public MoonpalaceMillDoughMicrochunkRow(
            string microchunkId,
            int widthTiles,
            int heightTiles,
            string usageClass,
            string biomeIds,
            string routeRoles,
            bool tileDataComplete,
            bool active)
        {
            MicrochunkId = microchunkId;
            WidthTiles = widthTiles;
            HeightTiles = heightTiles;
            UsageClass = usageClass;
            BiomeIds = biomeIds;
            RouteRoles = routeRoles;
            TileDataComplete = tileDataComplete;
            Active = active;
        }

        public string MicrochunkId { get; }
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public string UsageClass { get; }
        public string BiomeIds { get; }
        public string RouteRoles { get; }
        public bool TileDataComplete { get; }
        public bool Active { get; }
    }

    public sealed class MoonpalaceMillDoughTileRow
    {
        public MoonpalaceMillDoughTileRow(
            string microchunkId,
            int localX,
            int localY,
            string groundCode,
            string decorBackCode,
            string markerCode)
        {
            MicrochunkId = microchunkId;
            LocalX = localX;
            LocalY = localY;
            GroundCode = groundCode;
            DecorBackCode = decorBackCode;
            MarkerCode = markerCode;
        }

        public string MicrochunkId { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public string GroundCode { get; }
        public string DecorBackCode { get; }
        public string MarkerCode { get; }
    }

    public sealed class MoonpalaceMillDoughSocketRow
    {
        public MoonpalaceMillDoughSocketRow(
            string microchunkId,
            string socketId,
            string side,
            string traversalKind,
            bool mandatoryAllowed,
            string toolRequirement,
            string edgeSignatureId,
            string routeLayer,
            int minimumSafeTiles)
        {
            MicrochunkId = microchunkId;
            SocketId = socketId;
            Side = side;
            TraversalKind = traversalKind;
            MandatoryAllowed = mandatoryAllowed;
            ToolRequirement = toolRequirement;
            EdgeSignatureId = edgeSignatureId;
            RouteLayer = routeLayer;
            MinimumSafeTiles = minimumSafeTiles;
        }

        public string MicrochunkId { get; }
        public string SocketId { get; }
        public string Side { get; }
        public string TraversalKind { get; }
        public bool MandatoryAllowed { get; }
        public string ToolRequirement { get; }
        public string EdgeSignatureId { get; }
        public string RouteLayer { get; }
        public int MinimumSafeTiles { get; }
    }

    public sealed class MoonpalaceMillDoughBoundaryAuthoringData
    {
        public MoonpalaceMillDoughBoundaryAuthoringData(
            IEnumerable<MoonpalaceMillDoughBoundaryCandidateRow> candidates,
            IEnumerable<MoonpalaceMillDoughMicrochunkRow> microchunks,
            IEnumerable<MoonpalaceMillDoughTileRow> tiles,
            IEnumerable<MoonpalaceMillDoughSocketRow> sockets,
            int generatedCsvCreated = 0,
            int existingRowsModified = 0,
            int otherPairRowsModified = 0,
            int craterRootRowsModified = 0,
            int craterMillRowsModified = 0,
            int craterDoughRowsModified = 0,
            int rootMillRowsModified = 0,
            int rootDoughRowsModified = 0)
        {
            Candidates = Snapshot(candidates, nameof(candidates));
            Microchunks = Snapshot(microchunks, nameof(microchunks));
            Tiles = Snapshot(tiles, nameof(tiles));
            Sockets = Snapshot(sockets, nameof(sockets));
            GeneratedCsvCreated = generatedCsvCreated;
            ExistingRowsModified = existingRowsModified;
            OtherPairRowsModified = otherPairRowsModified;
            CraterRootRowsModified = craterRootRowsModified;
            CraterMillRowsModified = craterMillRowsModified;
            CraterDoughRowsModified = craterDoughRowsModified;
            RootMillRowsModified = rootMillRowsModified;
            RootDoughRowsModified = rootDoughRowsModified;
        }

        public IReadOnlyList<MoonpalaceMillDoughBoundaryCandidateRow> Candidates { get; }
        public IReadOnlyList<MoonpalaceMillDoughMicrochunkRow> Microchunks { get; }
        public IReadOnlyList<MoonpalaceMillDoughTileRow> Tiles { get; }
        public IReadOnlyList<MoonpalaceMillDoughSocketRow> Sockets { get; }
        public int GeneratedCsvCreated { get; }
        public int ExistingRowsModified { get; }
        public int OtherPairRowsModified { get; }
        public int CraterRootRowsModified { get; }
        public int CraterMillRowsModified { get; }
        public int CraterDoughRowsModified { get; }
        public int RootMillRowsModified { get; }
        public int RootDoughRowsModified { get; }

        private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> source, string parameterName)
        {
            if (source == null) throw new ArgumentNullException(parameterName);
            return new ReadOnlyCollection<T>(source.ToArray());
        }
    }
}
