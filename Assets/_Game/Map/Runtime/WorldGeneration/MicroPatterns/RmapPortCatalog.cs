using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    internal static class RmapPortText
    {
        public static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    /// <summary>
    /// RMAP08's fixed 12x8 chunk-port contract.  It deliberately references a
    /// selected RMAP07 base pattern but does not infer a port, direction, or
    /// required route from that pattern's tags or edge-air characteristics.
    /// RMAP09 owns automatic 3x2 composition; this catalog owns only named,
    /// inspectable fixture chunks and their explicit directed relationships.
    /// </summary>
    public enum RmapPortSpaceState
    {
        Active = 0,
        Secret = 1,
        InactiveSolid = 2,
        SpecialReserved = 3,
    }

    public enum RmapPortChunkType
    {
        Type0 = 0,
        Type1 = 1,
        Type2 = 2,
        Type3 = 3,
        Type4 = 4,
    }

    public enum RmapPortSide
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3,
    }

    public enum RmapPortTraversalKind
    {
        Walk = 0,
        Jump = 1,
        Drop = 2,
        Climb = 3,
        Hang = 4,
        OneWay = 5,
    }

    public enum RmapPortFlowDirection
    {
        In = 0,
        Out = 1,
        Both = 2,
    }

    /// <summary>Evidence is an expectation label, never a substitute for the Player fixture test.</summary>
    public enum RmapPortEvidenceState
    {
        GeometricCandidate = 0,
        FixturePassExpected = 1,
        FixtureBlockedExpected = 2,
        ProfileContextUnknown = 3,
    }

    public readonly struct RmapPortCell : IEquatable<RmapPortCell>, IComparable<RmapPortCell>
    {
        public RmapPortCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public bool Equals(RmapPortCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is RmapPortCell && Equals((RmapPortCell)obj);
        public override int GetHashCode() => (X * 397) ^ Y;
        public int CompareTo(RmapPortCell other) => Y != other.Y ? Y.CompareTo(other.Y) : X.CompareTo(other.X);
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," +
                                             Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class RmapPortSourcePlacement
    {
        public RmapPortSourcePlacement(string candidateId, int originX, int originY)
        {
            CandidateId = RmapPortText.Normalize(candidateId);
            OriginX = originX;
            OriginY = originY;
        }

        public string CandidateId { get; }
        public int OriginX { get; }
        public int OriginY { get; }
    }

    public sealed class RmapEdgePort
    {
        private readonly ReadOnlyCollection<int> openCells;

        public RmapEdgePort(
            string portId,
            RmapPortSide side,
            IEnumerable<int> sourceOpenCells,
            RmapPortTraversalKind traversalKind,
            RmapPortFlowDirection flowDirection,
            bool required,
            string entranceGroupId)
        {
            PortId = RmapPortText.Normalize(portId);
            Side = side;
            openCells = new ReadOnlyCollection<int>((sourceOpenCells ?? Array.Empty<int>())
                .Distinct().OrderBy(value => value).ToArray());
            TraversalKind = traversalKind;
            FlowDirection = flowDirection;
            Required = required;
            EntranceGroupId = RmapPortText.Normalize(entranceGroupId);
        }

        public string PortId { get; }
        public RmapPortSide Side { get; }
        /// <summary>L/R values are y; U/D values are x. All values are retained, not reduced to a center.</summary>
        public IReadOnlyList<int> OpenCells => openCells;
        public RmapPortTraversalKind TraversalKind { get; }
        public RmapPortFlowDirection FlowDirection { get; }
        public bool Required { get; }
        /// <summary>Independent entrances retain different IDs even when they share one side.</summary>
        public string EntranceGroupId { get; }

        public bool AllowsEntry => FlowDirection == RmapPortFlowDirection.In ||
            FlowDirection == RmapPortFlowDirection.Both;
        public bool AllowsExit => FlowDirection == RmapPortFlowDirection.Out ||
            FlowDirection == RmapPortFlowDirection.Both;
    }

    public sealed class RmapBreakableAccess
    {
        private readonly ReadOnlyCollection<int> openCells;

        public RmapBreakableAccess(string accessId, RmapPortSide side, IEnumerable<int> sourceOpenCells,
            string accessCondition, string evidence)
        {
            AccessId = RmapPortText.Normalize(accessId);
            Side = side;
            openCells = new ReadOnlyCollection<int>((sourceOpenCells ?? Array.Empty<int>())
                .Distinct().OrderBy(value => value).ToArray());
            AccessCondition = RmapPortText.Normalize(accessCondition);
            Evidence = RmapPortText.Normalize(evidence);
        }

        public string AccessId { get; }
        public RmapPortSide Side { get; }
        public IReadOnlyList<int> OpenCells => openCells;
        public string AccessCondition { get; }
        public string Evidence { get; }
    }

    public sealed class RmapPortChunk
    {
        private readonly ReadOnlyCollection<RmapEdgePort> ports;
        private readonly ReadOnlyCollection<RmapBreakableAccess> breakableAccesses;
        private readonly ReadOnlyCollection<RmapPortCell> solidCells;

        public RmapPortChunk(
            string chunkId,
            RmapPortSpaceState spaceState,
            RmapPortChunkType? chunkType,
            RmapPortSourcePlacement sourcePlacement,
            IEnumerable<RmapEdgePort> sourcePorts,
            IEnumerable<RmapBreakableAccess> sourceBreakableAccesses,
            IEnumerable<RmapPortCell> sourceSolidCells,
            string reservationId = "")
        {
            ChunkId = RmapPortText.Normalize(chunkId);
            SpaceState = spaceState;
            ChunkType = chunkType;
            SourcePlacement = sourcePlacement;
            ports = new ReadOnlyCollection<RmapEdgePort>((sourcePorts ?? Array.Empty<RmapEdgePort>())
                .Where(value => value != null).OrderBy(value => value.PortId, StringComparer.Ordinal).ToArray());
            breakableAccesses = new ReadOnlyCollection<RmapBreakableAccess>(
                (sourceBreakableAccesses ?? Array.Empty<RmapBreakableAccess>()).Where(value => value != null)
                .OrderBy(value => value.AccessId, StringComparer.Ordinal).ToArray());
            solidCells = new ReadOnlyCollection<RmapPortCell>((sourceSolidCells ?? Array.Empty<RmapPortCell>())
                .Distinct().OrderBy(value => value).ToArray());
            ReservationId = RmapPortText.Normalize(reservationId);
        }

        public string ChunkId { get; }
        public RmapPortSpaceState SpaceState { get; }
        /// <summary>Null is intentional for INACTIVE_SOLID and SPECIAL_RESERVED space-only records.</summary>
        public RmapPortChunkType? ChunkType { get; }
        public RmapPortSourcePlacement SourcePlacement { get; }
        public IReadOnlyList<RmapEdgePort> Ports => ports;
        public IReadOnlyList<RmapBreakableAccess> BreakableAccesses => breakableAccesses;
        public IReadOnlyList<RmapPortCell> SolidCells => solidCells;
        public string ReservationId { get; }

        public bool IsSolid(int x, int y) => solidCells.Contains(new RmapPortCell(x, y));
        public bool TryGetPort(string portId, out RmapEdgePort port)
        {
            port = ports.FirstOrDefault(value => string.Equals(value.PortId, portId, StringComparison.Ordinal));
            return port != null;
        }
    }

    public sealed class RmapPortAdjacency
    {
        private readonly ReadOnlyCollection<int> sharedCells;

        public RmapPortAdjacency(
            string connectionId,
            string fromChunkId,
            string fromPortId,
            string toChunkId,
            string toPortId,
            IEnumerable<int> sourceSharedCells,
            RmapPortEvidenceState evidenceState,
            string evidence)
        {
            ConnectionId = RmapPortText.Normalize(connectionId);
            FromChunkId = RmapPortText.Normalize(fromChunkId);
            FromPortId = RmapPortText.Normalize(fromPortId);
            ToChunkId = RmapPortText.Normalize(toChunkId);
            ToPortId = RmapPortText.Normalize(toPortId);
            sharedCells = new ReadOnlyCollection<int>((sourceSharedCells ?? Array.Empty<int>())
                .Distinct().OrderBy(value => value).ToArray());
            EvidenceState = evidenceState;
            Evidence = RmapPortText.Normalize(evidence);
        }

        public string ConnectionId { get; }
        public string FromChunkId { get; }
        public string FromPortId { get; }
        public string ToChunkId { get; }
        public string ToPortId { get; }
        public IReadOnlyList<int> SharedCells => sharedCells;
        public RmapPortEvidenceState EvidenceState { get; }
        public string Evidence { get; }
    }

    public sealed class RmapPortInteriorLink
    {
        public RmapPortInteriorLink(
            string connectionId,
            string chunkId,
            string fromPortId,
            string toPortId,
            RmapPortTraversalKind traversalKind,
            bool required,
            string profileDigest,
            RmapPortEvidenceState evidenceState,
            string condition,
            string evidence)
        {
            ConnectionId = RmapPortText.Normalize(connectionId);
            ChunkId = RmapPortText.Normalize(chunkId);
            FromPortId = RmapPortText.Normalize(fromPortId);
            ToPortId = RmapPortText.Normalize(toPortId);
            TraversalKind = traversalKind;
            Required = required;
            ProfileDigest = RmapPortText.Normalize(profileDigest);
            EvidenceState = evidenceState;
            Condition = RmapPortText.Normalize(condition);
            Evidence = RmapPortText.Normalize(evidence);
        }

        public string ConnectionId { get; }
        public string ChunkId { get; }
        public string FromPortId { get; }
        public string ToPortId { get; }
        public RmapPortTraversalKind TraversalKind { get; }
        public bool Required { get; }
        public string ProfileDigest { get; }
        public RmapPortEvidenceState EvidenceState { get; }
        public string Condition { get; }
        public string Evidence { get; }
    }

    public sealed class RmapPortCatalogSnapshot
    {
        private readonly ReadOnlyCollection<RmapPortChunk> chunks;
        private readonly ReadOnlyCollection<RmapPortAdjacency> adjacencyConnections;
        private readonly ReadOnlyCollection<RmapPortInteriorLink> interiorLinks;

        internal RmapPortCatalogSnapshot(
            IEnumerable<RmapPortChunk> sourceChunks,
            IEnumerable<RmapPortAdjacency> sourceAdjacencyConnections,
            IEnumerable<RmapPortInteriorLink> sourceInteriorLinks,
            string profileDigest)
        {
            chunks = new ReadOnlyCollection<RmapPortChunk>((sourceChunks ?? Array.Empty<RmapPortChunk>())
                .OrderBy(value => value.ChunkId, StringComparer.Ordinal).ToArray());
            adjacencyConnections = new ReadOnlyCollection<RmapPortAdjacency>(
                (sourceAdjacencyConnections ?? Array.Empty<RmapPortAdjacency>())
                .OrderBy(value => value.ConnectionId, StringComparer.Ordinal).ToArray());
            interiorLinks = new ReadOnlyCollection<RmapPortInteriorLink>(
                (sourceInteriorLinks ?? Array.Empty<RmapPortInteriorLink>())
                .OrderBy(value => value.ConnectionId, StringComparer.Ordinal).ToArray());
            ProfileDigest = RmapPortText.Normalize(profileDigest);
        }

        public IReadOnlyList<RmapPortChunk> Chunks => chunks;
        public IReadOnlyList<RmapPortAdjacency> AdjacencyConnections => adjacencyConnections;
        public IReadOnlyList<RmapPortInteriorLink> InteriorLinks => interiorLinks;
        public string ProfileDigest { get; }

        public bool TryGetChunk(string chunkId, out RmapPortChunk chunk)
        {
            chunk = chunks.FirstOrDefault(value => string.Equals(value.ChunkId, chunkId,
                StringComparison.Ordinal));
            return chunk != null;
        }

        public bool TryGetPort(string chunkId, string portId, out RmapEdgePort port)
        {
            port = null;
            return TryGetChunk(chunkId, out RmapPortChunk chunk) && chunk.TryGetPort(portId, out port);
        }
    }

    public sealed class RmapPortValidationResult
    {
        internal RmapPortValidationResult(IEnumerable<string> sourceErrors)
        {
            Errors = new ReadOnlyCollection<string>((sourceErrors ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Errors.Count == 0;
    }

    public static class RmapPortCatalog
    {
        public const int ChunkWidth = 12;
        public const int ChunkHeight = 8;
        public const int ChunkCellCount = ChunkWidth * ChunkHeight;
        public const string DataVersion = "RMAP08_PORTS_V1";
        public const string Rmap07SourcePlacementRule =
            "RMAP07_VOID_CLEAR at lower-left offset 4,2; port data remains explicit RMAP08 data";

        private static readonly Lazy<RmapPortCatalogSnapshot> Fixture =
            new Lazy<RmapPortCatalogSnapshot>(BuildFixtureInternal);

        public static RmapPortCatalogSnapshot BuildFixture() => Fixture.Value;

        public static RmapPortValidationResult Validate(RmapPortCatalogSnapshot snapshot)
        {
            var errors = new List<string>();
            if (snapshot == null)
            {
                errors.Add("MISSING_SNAPSHOT");
                return new RmapPortValidationResult(errors);
            }

            Duplicate(errors, snapshot.Chunks.Select(value => value.ChunkId), "DUPLICATE_CHUNK_ID");
            foreach (RmapPortChunk chunk in snapshot.Chunks) ValidateChunk(chunk, errors);
            Duplicate(errors, snapshot.AdjacencyConnections.Select(value => value.ConnectionId),
                "DUPLICATE_ADJACENCY_ID");
            Duplicate(errors, snapshot.InteriorLinks.Select(value => value.ConnectionId), "DUPLICATE_INTERIOR_ID");
            foreach (RmapPortAdjacency adjacency in snapshot.AdjacencyConnections)
                ValidateAdjacency(snapshot, adjacency, errors);
            foreach (RmapPortInteriorLink link in snapshot.InteriorLinks)
                ValidateInteriorLink(snapshot, link, errors);
            if (!string.Equals(snapshot.ProfileDigest, GeneratedTraversalProfileCatalog.Create().Digest,
                StringComparison.Ordinal)) errors.Add("TRAVERSAL_PROFILE_DIGEST_MISMATCH");
            return new RmapPortValidationResult(errors);
        }

        public static bool IsAllowedSideSet(RmapPortChunkType type, IEnumerable<RmapPortSide> sourceSides)
        {
            int mask = SideMask(sourceSides);
            switch (type)
            {
                case RmapPortChunkType.Type1: return mask == (SideBit(RmapPortSide.Left) | SideBit(RmapPortSide.Right));
                case RmapPortChunkType.Type2:
                    return mask == (SideBit(RmapPortSide.Left) | SideBit(RmapPortSide.Down)) ||
                        mask == (SideBit(RmapPortSide.Right) | SideBit(RmapPortSide.Down)) ||
                        mask == (SideBit(RmapPortSide.Left) | SideBit(RmapPortSide.Right) | SideBit(RmapPortSide.Down));
                case RmapPortChunkType.Type3:
                    return mask == (SideBit(RmapPortSide.Left) | SideBit(RmapPortSide.Up)) ||
                        mask == (SideBit(RmapPortSide.Right) | SideBit(RmapPortSide.Up)) ||
                        mask == (SideBit(RmapPortSide.Left) | SideBit(RmapPortSide.Right) | SideBit(RmapPortSide.Up));
                case RmapPortChunkType.Type4:
                    return (mask & SideBit(RmapPortSide.Up)) != 0 && (mask & SideBit(RmapPortSide.Down)) != 0;
                default: return false;
            }
        }

        public static string ExportChunksCsv(RmapPortCatalogSnapshot snapshot)
        {
            var text = new StringBuilder();
            text.AppendLine("ChunkId,SpaceState,Type,SourceCandidateId,SourceOffset,ReservationId,NormalPortCount,BreakableAccessCount,SolidCellCount");
            foreach (RmapPortChunk chunk in snapshot.Chunks)
                text.AppendLine(string.Join(",", new[]
                {
                    chunk.ChunkId, chunk.SpaceState.ToString(), chunk.ChunkType.HasValue ? chunk.ChunkType.Value.ToString() : "NONE",
                    chunk.SourcePlacement.CandidateId, chunk.SourcePlacement.OriginX + ";" + chunk.SourcePlacement.OriginY,
                    Empty(chunk.ReservationId), Number(chunk.Ports.Count), Number(chunk.BreakableAccesses.Count), Number(chunk.SolidCells.Count),
                }));
            return text.ToString();
        }

        public static string ExportPortsCsv(RmapPortCatalogSnapshot snapshot)
        {
            var text = new StringBuilder();
            text.AppendLine("ChunkId,PortId,Side,OpenCells,TraversalKind,FlowDirection,Required,EntranceGroupId");
            foreach (RmapPortChunk chunk in snapshot.Chunks)
            foreach (RmapEdgePort port in chunk.Ports)
                text.AppendLine(string.Join(",", new[]
                {
                    chunk.ChunkId, port.PortId, port.Side.ToString(), Join(port.OpenCells), port.TraversalKind.ToString(),
                    port.FlowDirection.ToString(), port.Required ? "true" : "false", port.EntranceGroupId,
                }));
            return text.ToString();
        }

        public static string ExportAdjacencyCsv(RmapPortCatalogSnapshot snapshot)
        {
            var text = new StringBuilder();
            text.AppendLine("ConnectionId,FromChunkId,FromPortId,ToChunkId,ToPortId,SharedEdgeCells,EvidenceState,Evidence");
            foreach (RmapPortAdjacency value in snapshot.AdjacencyConnections)
                text.AppendLine(string.Join(",", new[]
                {
                    value.ConnectionId, value.FromChunkId, value.FromPortId, value.ToChunkId, Empty(value.ToPortId),
                    Join(value.SharedCells), value.EvidenceState.ToString(), Escape(value.Evidence),
                }));
            return text.ToString();
        }

        public static string ExportInteriorLinksCsv(RmapPortCatalogSnapshot snapshot)
        {
            var text = new StringBuilder();
            text.AppendLine("ConnectionId,ChunkId,FromPortId,ToPortId,TraversalKind,Required,ProfileDigest,EvidenceState,Condition,Evidence");
            foreach (RmapPortInteriorLink value in snapshot.InteriorLinks)
                text.AppendLine(string.Join(",", new[]
                {
                    value.ConnectionId, value.ChunkId, value.FromPortId, value.ToPortId, value.TraversalKind.ToString(),
                    value.Required ? "true" : "false", value.ProfileDigest, value.EvidenceState.ToString(), Escape(value.Condition), Escape(value.Evidence),
                }));
            return text.ToString();
        }

        public static string ExportBreakableAccessCsv(RmapPortCatalogSnapshot snapshot)
        {
            var text = new StringBuilder();
            text.AppendLine("ChunkId,AccessId,Side,OpenCells,AccessCondition,Evidence");
            foreach (RmapPortChunk chunk in snapshot.Chunks)
            foreach (RmapBreakableAccess value in chunk.BreakableAccesses)
                text.AppendLine(string.Join(",", new[]
                {
                    chunk.ChunkId, value.AccessId, value.Side.ToString(), Join(value.OpenCells),
                    Escape(value.AccessCondition), Escape(value.Evidence),
                }));
            return text.ToString();
        }

        public static string ExportSnapshotJson(RmapPortCatalogSnapshot snapshot)
        {
            return "{\n" +
                "  \"data_version\": \"" + DataVersion + "\",\n" +
                "  \"chunk_size\": \"12x8\",\n" +
                "  \"source_pattern_rule\": \"" + Rmap07SourcePlacementRule + "\",\n" +
                "  \"traversal_profile_digest\": \"" + snapshot.ProfileDigest + "\",\n" +
                "  \"chunks\": " + Number(snapshot.Chunks.Count) + ",\n" +
                "  \"edge_ports\": " + Number(snapshot.Chunks.Sum(value => value.Ports.Count)) + ",\n" +
                "  \"adjacency_records\": " + Number(snapshot.AdjacencyConnections.Count) + ",\n" +
                "  \"interior_links\": " + Number(snapshot.InteriorLinks.Count) + "\n" +
                "}\n";
        }

        public static void ValidateCsv(string csv, int expectedColumns, int minimumDataRows)
        {
            string[] rows = (csv ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length - 1 < minimumDataRows) throw new InvalidOperationException("CSV has too few data rows.");
            if (rows.Any(row => row.Split(',').Length != expectedColumns))
                throw new InvalidOperationException("CSV column count is not stable.");
        }

        public static RmapPortCell ToChunkCell(RmapPortSide side, int edgeCoordinate)
        {
            switch (side)
            {
                case RmapPortSide.Left: return new RmapPortCell(0, edgeCoordinate);
                case RmapPortSide.Right: return new RmapPortCell(ChunkWidth - 1, edgeCoordinate);
                case RmapPortSide.Up: return new RmapPortCell(edgeCoordinate, ChunkHeight - 1);
                case RmapPortSide.Down: return new RmapPortCell(edgeCoordinate, 0);
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        private static RmapPortCatalogSnapshot BuildFixtureInternal()
        {
            RmapPatternCatalogSnapshot patterns = RmapPatternCatalog.BuildInitialPool();
            RmapPatternCandidate source = patterns.Candidates.Single(value =>
                value.PrimaryRole == RmapPatternPrimaryRole.VoidClear);
            var placement = new RmapPortSourcePlacement(source.CandidateId, 4, 2);
            string profileDigest = GeneratedTraversalProfileCatalog.Create().Digest;
            var chunks = new List<RmapPortChunk>
            {
                Active("T1_A", RmapPortChunkType.Type1, placement, new[]
                {
                    Port("P_T1A_L", RmapPortSide.Left, new[] { 1, 2 }, RmapPortTraversalKind.Walk, RmapPortFlowDirection.In, true, "ENTRANCE_T1A_L"),
                    Port("P_T1A_R", RmapPortSide.Right, new[] { 1, 2 }, RmapPortTraversalKind.Walk, RmapPortFlowDirection.Out, true, "ENTRANCE_T1A_R"),
                }, null, null),
                Active("T1_B", RmapPortChunkType.Type1, placement, new[]
                {
                    Port("P_T1B_L", RmapPortSide.Left, new[] { 1, 2 }, RmapPortTraversalKind.Walk, RmapPortFlowDirection.In, true, "ENTRANCE_T1B_L"),
                    Port("P_T1B_R", RmapPortSide.Right, new[] { 1, 2 }, RmapPortTraversalKind.Walk, RmapPortFlowDirection.Out, false, "ENTRANCE_T1B_R"),
                }, null, null),
                Active("T1_BLOCKED_SOURCE", RmapPortChunkType.Type1, placement, new[]
                {
                    Port("P_T1BLOCK_L", RmapPortSide.Left, new[] { 1, 2 }, RmapPortTraversalKind.Walk, RmapPortFlowDirection.In, true, "ENTRANCE_T1BLOCK_L"),
                    Port("P_T1BLOCK_R", RmapPortSide.Right, new[] { 1, 2 }, RmapPortTraversalKind.Walk, RmapPortFlowDirection.Out, true, "ENTRANCE_T1BLOCK_R"),
                }, null, null),
                Inactive("INACTIVE_SOLID_WALL", placement),
                Active("T2_DROP", RmapPortChunkType.Type2, placement, new[]
                {
                    Port("P_T2_L", RmapPortSide.Left, new[] { 1, 2 }, RmapPortTraversalKind.Walk, RmapPortFlowDirection.In, true, "ENTRANCE_T2_L"),
                    Port("P_T2_D", RmapPortSide.Down, new[] { 6, 7, 8, 9 }, RmapPortTraversalKind.Drop, RmapPortFlowDirection.Out, true, "EXIT_T2_D"),
                }, null, null),
                Active("T3_CLIMB", RmapPortChunkType.Type3, placement, new[]
                {
                    Port("P_T3_L", RmapPortSide.Left, new[] { 1, 2 }, RmapPortTraversalKind.Walk, RmapPortFlowDirection.In, true, "ENTRANCE_T3_L"),
                    Port("P_T3_U", RmapPortSide.Up, new[] { 5, 6 }, RmapPortTraversalKind.Climb, RmapPortFlowDirection.Out, true, "EXIT_T3_U"),
                }, null, null),
                Active("T4_SPLIT", RmapPortChunkType.Type4, placement, new[]
                {
                    Port("P_T4_L_LOW", RmapPortSide.Left, new[] { 1, 2 }, RmapPortTraversalKind.Walk, RmapPortFlowDirection.In, false, "ENTRANCE_T4_LOW"),
                    Port("P_T4_L_HIGH", RmapPortSide.Left, new[] { 5, 6 }, RmapPortTraversalKind.Hang, RmapPortFlowDirection.In, false, "ENTRANCE_T4_HIGH"),
                    Port("P_T4_R", RmapPortSide.Right, new[] { 1, 2 }, RmapPortTraversalKind.Walk, RmapPortFlowDirection.Out, false, "EXIT_T4_R"),
                    Port("P_T4_U", RmapPortSide.Up, new[] { 4, 5 }, RmapPortTraversalKind.Climb, RmapPortFlowDirection.Both, false, "VERTICAL_T4_U"),
                    Port("P_T4_D", RmapPortSide.Down, new[] { 4, 5 }, RmapPortTraversalKind.Drop, RmapPortFlowDirection.Both, false, "VERTICAL_T4_D"),
                }, null, new[] { new RmapPortCell(0, 3), new RmapPortCell(0, 4) }),
                Active("T0_SINGLE_ENTRANCE", RmapPortChunkType.Type0, placement, new[]
                {
                    Port("P_T0_SINGLE_L", RmapPortSide.Left, new[] { 1, 2 }, RmapPortTraversalKind.Walk, RmapPortFlowDirection.Both, false, "ENTRANCE_T0_SINGLE"),
                }, null, null),
                Active("T0_BREAKABLE_SECRET", RmapPortChunkType.Type0, placement, Array.Empty<RmapEdgePort>(), new[]
                {
                    new RmapBreakableAccess("BREAK_T0_SECRET_R", RmapPortSide.Right, new[] { 2, 3 },
                        "BREAKABLE_TOOL_REQUIRED", "secret shell remains solid until breakable access is resolved"),
                }, new[] { new RmapPortCell(11, 2), new RmapPortCell(11, 3) }, RmapPortSpaceState.Secret),
                Special("SPECIAL_RESERVED_START", placement),
            };
            var adjacency = new[]
            {
                new RmapPortAdjacency("ADJ_T1_A_TO_B", "T1_A", "P_T1A_R", "T1_B", "P_T1B_L", new[] { 1, 2 },
                    RmapPortEvidenceState.FixturePassExpected, "opposite R/L ports share every declared y coordinate"),
                new RmapPortAdjacency("ADJ_BLOCKED_INACTIVE", "T1_BLOCKED_SOURCE", "P_T1BLOCK_R", "INACTIVE_SOLID_WALL", string.Empty,
                    Array.Empty<int>(), RmapPortEvidenceState.FixtureBlockedExpected,
                    "inactive solid has no edge port; a Player collision against its TilemapCollider2D is expected"),
                new RmapPortAdjacency("ADJ_T4_CONTEXT_UNKNOWN", "T4_SPLIT", "P_T4_L_HIGH", "T1_B", "P_T1B_R", Array.Empty<int>(),
                    RmapPortEvidenceState.ProfileContextUnknown,
                    "HANG-to-WALK remains profile/context dependent and is not declared as a pass"),
            };
            var links = new[]
            {
                new RmapPortInteriorLink("INT_T2_LEFT_TO_DOWN", "T2_DROP", "P_T2_L", "P_T2_D", RmapPortTraversalKind.Drop,
                    true, profileDigest, RmapPortEvidenceState.FixturePassExpected, "side entry to downward exit", "actual Player drops through the physical 2-cell floor opening"),
                new RmapPortInteriorLink("INT_T3_LEFT_TO_UP", "T3_CLIMB", "P_T3_L", "P_T3_U", RmapPortTraversalKind.Climb,
                    true, profileDigest, RmapPortEvidenceState.FixturePassExpected, "vertical intent on the RMAP04 climb surface", "actual Player leaves through the upper chunk boundary"),
                new RmapPortInteriorLink("INT_T4_LOW_TO_RIGHT", "T4_SPLIT", "P_T4_L_LOW", "P_T4_R", RmapPortTraversalKind.Walk,
                    false, profileDigest, RmapPortEvidenceState.GeometricCandidate, "lower independent entrance only", "no automatic link is created for the separate high entrance"),
            };
            var snapshot = new RmapPortCatalogSnapshot(chunks, adjacency, links, profileDigest);
            RmapPortValidationResult validation = Validate(snapshot);
            if (!validation.IsValid) throw new InvalidOperationException(string.Join(";", validation.Errors));
            return snapshot;
        }

        private static RmapPortChunk Active(string id, RmapPortChunkType type, RmapPortSourcePlacement placement,
            IEnumerable<RmapEdgePort> ports, IEnumerable<RmapBreakableAccess> breakableAccesses,
            IEnumerable<RmapPortCell> extraSolids, RmapPortSpaceState state = RmapPortSpaceState.Active)
        {
            RmapEdgePort[] portArray = (ports ?? Array.Empty<RmapEdgePort>()).ToArray();
            return new RmapPortChunk(id, state, type, placement, portArray, breakableAccesses,
                Geometry(placement, portArray, extraSolids), string.Empty);
        }

        private static RmapPortChunk Inactive(string id, RmapPortSourcePlacement placement)
        {
            var solids = new List<RmapPortCell>();
            for (var y = 0; y < ChunkHeight; y++)
            for (var x = 0; x < ChunkWidth; x++) solids.Add(new RmapPortCell(x, y));
            return new RmapPortChunk(id, RmapPortSpaceState.InactiveSolid, null, placement,
                Array.Empty<RmapEdgePort>(), Array.Empty<RmapBreakableAccess>(), solids);
        }

        private static RmapPortChunk Special(string id, RmapPortSourcePlacement placement) => new RmapPortChunk(
            id, RmapPortSpaceState.SpecialReserved, null, placement, Array.Empty<RmapEdgePort>(),
            Array.Empty<RmapBreakableAccess>(), Geometry(placement, Array.Empty<RmapEdgePort>(), null), "RESERVED_START_SLOT");

        private static RmapEdgePort Port(string id, RmapPortSide side, IEnumerable<int> cells,
            RmapPortTraversalKind traversal, RmapPortFlowDirection flow, bool required, string group) =>
            new RmapEdgePort(id, side, cells, traversal, flow, required, group);

        private static IEnumerable<RmapPortCell> Geometry(RmapPortSourcePlacement placement,
            IEnumerable<RmapEdgePort> ports, IEnumerable<RmapPortCell> extraSolids)
        {
            var cells = new HashSet<RmapPortCell>();
            // The selected RMAP07 VOID_CLEAR's 4x4 cells are intentionally all air. Its retained source ID and
            // lower-left offset establish provenance without treating an edge characteristic as a port declaration.
            for (var x = 0; x < ChunkWidth; x++) cells.Add(new RmapPortCell(x, 0));
            foreach (RmapPortCell cell in extraSolids ?? Array.Empty<RmapPortCell>()) cells.Add(cell);
            foreach (RmapEdgePort port in ports ?? Array.Empty<RmapEdgePort>())
            foreach (int coordinate in port.OpenCells) cells.Remove(ToChunkCell(port.Side, coordinate));
            return cells;
        }

        private static void ValidateChunk(RmapPortChunk chunk, ICollection<string> errors)
        {
            if (chunk == null) { errors.Add("NULL_CHUNK"); return; }
            if (string.IsNullOrEmpty(chunk.ChunkId)) errors.Add("EMPTY_CHUNK_ID");
            if (chunk.SourcePlacement == null || string.IsNullOrEmpty(chunk.SourcePlacement.CandidateId))
                errors.Add(chunk.ChunkId + ":MISSING_RMAP07_SOURCE");
            else
            {
                RmapPatternCatalogSnapshot pool = RmapPatternCatalog.BuildInitialPool();
                if (!pool.TryGetCandidate(chunk.SourcePlacement.CandidateId, out _))
                    errors.Add(chunk.ChunkId + ":UNKNOWN_RMAP07_SOURCE");
                if (chunk.SourcePlacement.OriginX < 0 || chunk.SourcePlacement.OriginY < 0 ||
                    chunk.SourcePlacement.OriginX + RmapPatternCatalog.Width > ChunkWidth ||
                    chunk.SourcePlacement.OriginY + RmapPatternCatalog.Height > ChunkHeight)
                    errors.Add(chunk.ChunkId + ":RMAP07_SOURCE_OFFSET_OUT_OF_RANGE");
            }
            Duplicate(errors, chunk.Ports.Select(value => value.PortId), chunk.ChunkId + ":DUPLICATE_PORT_ID");
            foreach (RmapEdgePort port in chunk.Ports) ValidatePort(chunk, port, errors);
            foreach (RmapBreakableAccess access in chunk.BreakableAccesses) ValidateBreakable(chunk, access, errors);
            if (chunk.SpaceState == RmapPortSpaceState.InactiveSolid)
            {
                if (chunk.ChunkType.HasValue || chunk.Ports.Count != 0 || chunk.BreakableAccesses.Count != 0 ||
                    chunk.SolidCells.Count != ChunkCellCount) errors.Add(chunk.ChunkId + ":INACTIVE_SOLID_MUST_HAVE_NO_PORTS_AND_FULL_SOLID_GEOMETRY");
                return;
            }
            if (chunk.SpaceState == RmapPortSpaceState.SpecialReserved)
            {
                if (chunk.ChunkType.HasValue || string.IsNullOrEmpty(chunk.ReservationId))
                    errors.Add(chunk.ChunkId + ":SPECIAL_RESERVED_REQUIRES_SEPARATE_RESERVATION");
                return;
            }
            if (!chunk.ChunkType.HasValue) { errors.Add(chunk.ChunkId + ":ACTIVE_OR_SECRET_REQUIRES_TYPE"); return; }
            if (chunk.ChunkType.Value == RmapPortChunkType.Type0)
            {
                if (chunk.Ports.Count > 1) errors.Add(chunk.ChunkId + ":TYPE0_HAS_MORE_THAN_ONE_NORMAL_ENTRANCE");
                if (chunk.Ports.Count == 0 && chunk.BreakableAccesses.Count == 0)
                    errors.Add(chunk.ChunkId + ":TYPE0_ZERO_ENTRANCE_REQUIRES_BREAKABLE_ACCESS");
            }
            else if (!IsAllowedSideSet(chunk.ChunkType.Value, chunk.Ports.Select(value => value.Side)))
                errors.Add(chunk.ChunkId + ":TYPE_SIDE_SET_NOT_ALLOWED");
        }

        private static void ValidatePort(RmapPortChunk chunk, RmapEdgePort port, ICollection<string> errors)
        {
            if (port == null) { errors.Add(chunk.ChunkId + ":NULL_PORT"); return; }
            if (string.IsNullOrEmpty(port.PortId) || string.IsNullOrEmpty(port.EntranceGroupId) || port.OpenCells.Count == 0)
                errors.Add(chunk.ChunkId + ":PORT_MISSING_REQUIRED_FIELD");
            if (!Enum.IsDefined(typeof(RmapPortTraversalKind), port.TraversalKind) ||
                !Enum.IsDefined(typeof(RmapPortFlowDirection), port.FlowDirection)) errors.Add(chunk.ChunkId + ":PORT_ENUM_INVALID");
            foreach (int coordinate in port.OpenCells)
            {
                int maximum = port.Side == RmapPortSide.Left || port.Side == RmapPortSide.Right ? ChunkHeight : ChunkWidth;
                if (coordinate < 0 || coordinate >= maximum) errors.Add(chunk.ChunkId + ":PORT_COORDINATE_OUT_OF_RANGE");
                else if (chunk.IsSolid(ToChunkCell(port.Side, coordinate).X, ToChunkCell(port.Side, coordinate).Y))
                    errors.Add(chunk.ChunkId + ":PORT_OPEN_CELL_IS_SOLID");
            }
        }

        private static void ValidateBreakable(RmapPortChunk chunk, RmapBreakableAccess access, ICollection<string> errors)
        {
            if (access == null || string.IsNullOrEmpty(access.AccessId) || access.OpenCells.Count == 0 ||
                string.IsNullOrEmpty(access.AccessCondition) || string.IsNullOrEmpty(access.Evidence))
                errors.Add(chunk.ChunkId + ":BREAKABLE_ACCESS_MISSING_FIELD");
            else
            {
                foreach (int coordinate in access.OpenCells)
                {
                    int maximum = access.Side == RmapPortSide.Left || access.Side == RmapPortSide.Right
                        ? ChunkHeight : ChunkWidth;
                    if (coordinate < 0 || coordinate >= maximum ||
                        !chunk.IsSolid(ToChunkCell(access.Side, coordinate).X,
                            ToChunkCell(access.Side, coordinate).Y))
                        errors.Add(chunk.ChunkId + ":BREAKABLE_ACCESS_MUST_REFERENCE_SOLID_SHELL");
                }
            }
        }

        private static void ValidateAdjacency(RmapPortCatalogSnapshot snapshot, RmapPortAdjacency value,
            ICollection<string> errors)
        {
            if (!snapshot.TryGetPort(value.FromChunkId, value.FromPortId, out RmapEdgePort from))
            { errors.Add(value.ConnectionId + ":MISSING_FROM_PORT"); return; }
            if (value.EvidenceState != RmapPortEvidenceState.FixturePassExpected) return;
            if (!snapshot.TryGetPort(value.ToChunkId, value.ToPortId, out RmapEdgePort to))
            { errors.Add(value.ConnectionId + ":MISSING_TO_PORT"); return; }
            if (!AreOpposite(from.Side, to.Side)) errors.Add(value.ConnectionId + ":SIDES_NOT_OPPOSITE");
            if (!from.AllowsExit || !to.AllowsEntry) errors.Add(value.ConnectionId + ":FLOW_DIRECTION_NOT_COMPATIBLE");
            int[] intersection = from.OpenCells.Intersect(to.OpenCells).OrderBy(cell => cell).ToArray();
            if (intersection.Length == 0 || !intersection.SequenceEqual(value.SharedCells))
                errors.Add(value.ConnectionId + ":OPEN_CELL_INTERSECTION_MISMATCH");
        }

        private static void ValidateInteriorLink(RmapPortCatalogSnapshot snapshot, RmapPortInteriorLink link,
            ICollection<string> errors)
        {
            if (!snapshot.TryGetPort(link.ChunkId, link.FromPortId, out RmapEdgePort from) ||
                !snapshot.TryGetPort(link.ChunkId, link.ToPortId, out RmapEdgePort to))
            { errors.Add(link.ConnectionId + ":MISSING_INTERIOR_PORT"); return; }
            if (!from.AllowsEntry || !to.AllowsExit) errors.Add(link.ConnectionId + ":INTERIOR_FLOW_DIRECTION_NOT_COMPATIBLE");
            if (string.IsNullOrEmpty(link.ProfileDigest) || !string.Equals(link.ProfileDigest, snapshot.ProfileDigest,
                StringComparison.Ordinal)) errors.Add(link.ConnectionId + ":INTERIOR_PROFILE_MISMATCH");
            if (link.Required && link.EvidenceState == RmapPortEvidenceState.ProfileContextUnknown)
                errors.Add(link.ConnectionId + ":REQUIRED_LINK_CANNOT_BE_UNKNOWN");
        }

        private static bool AreOpposite(RmapPortSide left, RmapPortSide right) =>
            (left == RmapPortSide.Left && right == RmapPortSide.Right) ||
            (left == RmapPortSide.Right && right == RmapPortSide.Left) ||
            (left == RmapPortSide.Up && right == RmapPortSide.Down) ||
            (left == RmapPortSide.Down && right == RmapPortSide.Up);

        private static int SideMask(IEnumerable<RmapPortSide> values) => (values ?? Array.Empty<RmapPortSide>())
            .Distinct().Aggregate(0, (current, value) => current | SideBit(value));
        private static int SideBit(RmapPortSide side) => 1 << (int)side;
        private static void Duplicate(ICollection<string> errors, IEnumerable<string> values, string error) { if ((values ?? Array.Empty<string>()).GroupBy(value => value, StringComparer.Ordinal).Any(group => group.Count() > 1)) errors.Add(error); }
        private static string Join(IEnumerable<int> values) => string.Join(";", (values ?? Array.Empty<int>()).Select(Number));
        private static string Escape(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        private static string Empty(string value) => string.IsNullOrEmpty(value) ? "NONE" : value;
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
