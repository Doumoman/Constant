using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    internal static class Sv5PortText
    {
        public static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    /// <summary>
    /// SV5's fixed 12x8 chunk-port contract.  It deliberately references a
    /// selected SV5 base pattern but does not infer a port, direction, or
    /// required route from that pattern's tags or edge-air characteristics.
    /// SV5 owns automatic 3x2 composition; this catalog owns only named,
    /// inspectable fixture chunks and their explicit directed relationships.
    /// </summary>
    public enum Sv5PortSpaceState
    {
        Active = 0,
        Secret = 1,
        InactiveSolid = 2,
        SpecialReserved = 3,
    }

    public enum Sv5PortChunkType
    {
        Type0 = 0,
        Type1 = 1,
        Type2 = 2,
        Type3 = 3,
        Type4 = 4,
    }

    public enum Sv5PortSide
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3,
    }

    public enum Sv5PortTraversalKind
    {
        Walk = 0,
        Jump = 1,
        Drop = 2,
        Climb = 3,
        Hang = 4,
        OneWay = 5,
    }

    public enum Sv5PortFlowDirection
    {
        In = 0,
        Out = 1,
        Both = 2,
    }

    /// <summary>Evidence is an expectation label, never a substitute for the Player fixture test.</summary>
    public enum Sv5PortEvidenceState
    {
        GeometricCandidate = 0,
        FixturePassExpected = 1,
        FixtureBlockedExpected = 2,
        ProfileContextUnknown = 3,
    }

    public readonly struct Sv5PortCell : IEquatable<Sv5PortCell>, IComparable<Sv5PortCell>
    {
        public Sv5PortCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public bool Equals(Sv5PortCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Sv5PortCell && Equals((Sv5PortCell)obj);
        public override int GetHashCode() => (X * 397) ^ Y;
        public int CompareTo(Sv5PortCell other) => Y != other.Y ? Y.CompareTo(other.Y) : X.CompareTo(other.X);
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," +
                                             Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class Sv5PortSourcePlacement
    {
        public Sv5PortSourcePlacement(string candidateId, int originX, int originY)
        {
            CandidateId = Sv5PortText.Normalize(candidateId);
            OriginX = originX;
            OriginY = originY;
        }

        public string CandidateId { get; }
        public int OriginX { get; }
        public int OriginY { get; }
    }

    public sealed class Sv5EdgePort
    {
        private readonly ReadOnlyCollection<int> openCells;

        public Sv5EdgePort(
            string portId,
            Sv5PortSide side,
            IEnumerable<int> sourceOpenCells,
            Sv5PortTraversalKind traversalKind,
            Sv5PortFlowDirection flowDirection,
            bool required,
            string entranceGroupId)
        {
            PortId = Sv5PortText.Normalize(portId);
            Side = side;
            openCells = new ReadOnlyCollection<int>((sourceOpenCells ?? Array.Empty<int>())
                .Distinct().OrderBy(value => value).ToArray());
            TraversalKind = traversalKind;
            FlowDirection = flowDirection;
            Required = required;
            EntranceGroupId = Sv5PortText.Normalize(entranceGroupId);
        }

        public string PortId { get; }
        public Sv5PortSide Side { get; }
        /// <summary>L/R values are y; U/D values are x. All values are retained, not reduced to a center.</summary>
        public IReadOnlyList<int> OpenCells => openCells;
        public Sv5PortTraversalKind TraversalKind { get; }
        public Sv5PortFlowDirection FlowDirection { get; }
        public bool Required { get; }
        /// <summary>Independent entrances retain different IDs even when they share one side.</summary>
        public string EntranceGroupId { get; }

        public bool AllowsEntry => FlowDirection == Sv5PortFlowDirection.In ||
            FlowDirection == Sv5PortFlowDirection.Both;
        public bool AllowsExit => FlowDirection == Sv5PortFlowDirection.Out ||
            FlowDirection == Sv5PortFlowDirection.Both;
    }

    public sealed class Sv5BreakableAccess
    {
        private readonly ReadOnlyCollection<int> openCells;

        public Sv5BreakableAccess(string accessId, Sv5PortSide side, IEnumerable<int> sourceOpenCells,
            string accessCondition, string evidence)
        {
            AccessId = Sv5PortText.Normalize(accessId);
            Side = side;
            openCells = new ReadOnlyCollection<int>((sourceOpenCells ?? Array.Empty<int>())
                .Distinct().OrderBy(value => value).ToArray());
            AccessCondition = Sv5PortText.Normalize(accessCondition);
            Evidence = Sv5PortText.Normalize(evidence);
        }

        public string AccessId { get; }
        public Sv5PortSide Side { get; }
        public IReadOnlyList<int> OpenCells => openCells;
        public string AccessCondition { get; }
        public string Evidence { get; }
    }

    public sealed class Sv5PortChunk
    {
        private readonly ReadOnlyCollection<Sv5EdgePort> ports;
        private readonly ReadOnlyCollection<Sv5BreakableAccess> breakableAccesses;
        private readonly ReadOnlyCollection<Sv5PortCell> solidCells;

        public Sv5PortChunk(
            string chunkId,
            Sv5PortSpaceState spaceState,
            Sv5PortChunkType? chunkType,
            Sv5PortSourcePlacement sourcePlacement,
            IEnumerable<Sv5EdgePort> sourcePorts,
            IEnumerable<Sv5BreakableAccess> sourceBreakableAccesses,
            IEnumerable<Sv5PortCell> sourceSolidCells,
            string reservationId = "")
        {
            ChunkId = Sv5PortText.Normalize(chunkId);
            SpaceState = spaceState;
            ChunkType = chunkType;
            SourcePlacement = sourcePlacement;
            ports = new ReadOnlyCollection<Sv5EdgePort>((sourcePorts ?? Array.Empty<Sv5EdgePort>())
                .Where(value => value != null).OrderBy(value => value.PortId, StringComparer.Ordinal).ToArray());
            breakableAccesses = new ReadOnlyCollection<Sv5BreakableAccess>(
                (sourceBreakableAccesses ?? Array.Empty<Sv5BreakableAccess>()).Where(value => value != null)
                .OrderBy(value => value.AccessId, StringComparer.Ordinal).ToArray());
            solidCells = new ReadOnlyCollection<Sv5PortCell>((sourceSolidCells ?? Array.Empty<Sv5PortCell>())
                .Distinct().OrderBy(value => value).ToArray());
            ReservationId = Sv5PortText.Normalize(reservationId);
        }

        public string ChunkId { get; }
        public Sv5PortSpaceState SpaceState { get; }
        /// <summary>Null is intentional for INACTIVE_SOLID and SPECIAL_RESERVED space-only records.</summary>
        public Sv5PortChunkType? ChunkType { get; }
        public Sv5PortSourcePlacement SourcePlacement { get; }
        public IReadOnlyList<Sv5EdgePort> Ports => ports;
        public IReadOnlyList<Sv5BreakableAccess> BreakableAccesses => breakableAccesses;
        public IReadOnlyList<Sv5PortCell> SolidCells => solidCells;
        public string ReservationId { get; }

        public bool IsSolid(int x, int y) => solidCells.Contains(new Sv5PortCell(x, y));
        public bool TryGetPort(string portId, out Sv5EdgePort port)
        {
            port = ports.FirstOrDefault(value => string.Equals(value.PortId, portId, StringComparison.Ordinal));
            return port != null;
        }
    }

    public sealed class Sv5PortAdjacency
    {
        private readonly ReadOnlyCollection<int> sharedCells;

        public Sv5PortAdjacency(
            string connectionId,
            string fromChunkId,
            string fromPortId,
            string toChunkId,
            string toPortId,
            IEnumerable<int> sourceSharedCells,
            Sv5PortEvidenceState evidenceState,
            string evidence)
        {
            ConnectionId = Sv5PortText.Normalize(connectionId);
            FromChunkId = Sv5PortText.Normalize(fromChunkId);
            FromPortId = Sv5PortText.Normalize(fromPortId);
            ToChunkId = Sv5PortText.Normalize(toChunkId);
            ToPortId = Sv5PortText.Normalize(toPortId);
            sharedCells = new ReadOnlyCollection<int>((sourceSharedCells ?? Array.Empty<int>())
                .Distinct().OrderBy(value => value).ToArray());
            EvidenceState = evidenceState;
            Evidence = Sv5PortText.Normalize(evidence);
        }

        public string ConnectionId { get; }
        public string FromChunkId { get; }
        public string FromPortId { get; }
        public string ToChunkId { get; }
        public string ToPortId { get; }
        public IReadOnlyList<int> SharedCells => sharedCells;
        public Sv5PortEvidenceState EvidenceState { get; }
        public string Evidence { get; }
    }

    public sealed class Sv5PortInteriorLink
    {
        public Sv5PortInteriorLink(
            string connectionId,
            string chunkId,
            string fromPortId,
            string toPortId,
            Sv5PortTraversalKind traversalKind,
            bool required,
            string profileDigest,
            Sv5PortEvidenceState evidenceState,
            string condition,
            string evidence)
        {
            ConnectionId = Sv5PortText.Normalize(connectionId);
            ChunkId = Sv5PortText.Normalize(chunkId);
            FromPortId = Sv5PortText.Normalize(fromPortId);
            ToPortId = Sv5PortText.Normalize(toPortId);
            TraversalKind = traversalKind;
            Required = required;
            ProfileDigest = Sv5PortText.Normalize(profileDigest);
            EvidenceState = evidenceState;
            Condition = Sv5PortText.Normalize(condition);
            Evidence = Sv5PortText.Normalize(evidence);
        }

        public string ConnectionId { get; }
        public string ChunkId { get; }
        public string FromPortId { get; }
        public string ToPortId { get; }
        public Sv5PortTraversalKind TraversalKind { get; }
        public bool Required { get; }
        public string ProfileDigest { get; }
        public Sv5PortEvidenceState EvidenceState { get; }
        public string Condition { get; }
        public string Evidence { get; }
    }

    public sealed class Sv5PortCatalogSnapshot
    {
        private readonly ReadOnlyCollection<Sv5PortChunk> chunks;
        private readonly ReadOnlyCollection<Sv5PortAdjacency> adjacencyConnections;
        private readonly ReadOnlyCollection<Sv5PortInteriorLink> interiorLinks;

        internal Sv5PortCatalogSnapshot(
            IEnumerable<Sv5PortChunk> sourceChunks,
            IEnumerable<Sv5PortAdjacency> sourceAdjacencyConnections,
            IEnumerable<Sv5PortInteriorLink> sourceInteriorLinks,
            string profileDigest)
        {
            chunks = new ReadOnlyCollection<Sv5PortChunk>((sourceChunks ?? Array.Empty<Sv5PortChunk>())
                .OrderBy(value => value.ChunkId, StringComparer.Ordinal).ToArray());
            adjacencyConnections = new ReadOnlyCollection<Sv5PortAdjacency>(
                (sourceAdjacencyConnections ?? Array.Empty<Sv5PortAdjacency>())
                .OrderBy(value => value.ConnectionId, StringComparer.Ordinal).ToArray());
            interiorLinks = new ReadOnlyCollection<Sv5PortInteriorLink>(
                (sourceInteriorLinks ?? Array.Empty<Sv5PortInteriorLink>())
                .OrderBy(value => value.ConnectionId, StringComparer.Ordinal).ToArray());
            ProfileDigest = Sv5PortText.Normalize(profileDigest);
        }

        public IReadOnlyList<Sv5PortChunk> Chunks => chunks;
        public IReadOnlyList<Sv5PortAdjacency> AdjacencyConnections => adjacencyConnections;
        public IReadOnlyList<Sv5PortInteriorLink> InteriorLinks => interiorLinks;
        public string ProfileDigest { get; }

        public bool TryGetChunk(string chunkId, out Sv5PortChunk chunk)
        {
            chunk = chunks.FirstOrDefault(value => string.Equals(value.ChunkId, chunkId,
                StringComparison.Ordinal));
            return chunk != null;
        }

        public bool TryGetPort(string chunkId, string portId, out Sv5EdgePort port)
        {
            port = null;
            return TryGetChunk(chunkId, out Sv5PortChunk chunk) && chunk.TryGetPort(portId, out port);
        }
    }

    public sealed class Sv5PortValidationResult
    {
        internal Sv5PortValidationResult(IEnumerable<string> sourceErrors)
        {
            Errors = new ReadOnlyCollection<string>((sourceErrors ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Errors.Count == 0;
    }

    public static class Sv5PortCatalog
    {
        public const int ChunkWidth = 12;
        public const int ChunkHeight = 8;
        public const int ChunkCellCount = ChunkWidth * ChunkHeight;
        public const string DataVersion = "SV5_PORTS_V1";
        public const string Sv5SourcePlacementRule =
            "SV5_VOID_CLEAR at lower-left offset 4,2; port data remains explicit SV5 data";

        private static readonly Lazy<Sv5PortCatalogSnapshot> Fixture =
            new Lazy<Sv5PortCatalogSnapshot>(BuildFixtureInternal);

        public static Sv5PortCatalogSnapshot BuildFixture() => Fixture.Value;

        public static Sv5PortValidationResult Validate(Sv5PortCatalogSnapshot snapshot)
        {
            var errors = new List<string>();
            if (snapshot == null)
            {
                errors.Add("MISSING_SNAPSHOT");
                return new Sv5PortValidationResult(errors);
            }

            Duplicate(errors, snapshot.Chunks.Select(value => value.ChunkId), "DUPLICATE_CHUNK_ID");
            foreach (Sv5PortChunk chunk in snapshot.Chunks) ValidateChunk(chunk, errors);
            Duplicate(errors, snapshot.AdjacencyConnections.Select(value => value.ConnectionId),
                "DUPLICATE_ADJACENCY_ID");
            Duplicate(errors, snapshot.InteriorLinks.Select(value => value.ConnectionId), "DUPLICATE_INTERIOR_ID");
            foreach (Sv5PortAdjacency adjacency in snapshot.AdjacencyConnections)
                ValidateAdjacency(snapshot, adjacency, errors);
            foreach (Sv5PortInteriorLink link in snapshot.InteriorLinks)
                ValidateInteriorLink(snapshot, link, errors);
            if (!string.Equals(snapshot.ProfileDigest, GeneratedTraversalProfileCatalog.Create().Digest,
                StringComparison.Ordinal)) errors.Add("TRAVERSAL_PROFILE_DIGEST_MISMATCH");
            return new Sv5PortValidationResult(errors);
        }

        public static bool IsAllowedSideSet(Sv5PortChunkType type, IEnumerable<Sv5PortSide> sourceSides)
        {
            int mask = SideMask(sourceSides);
            switch (type)
            {
                case Sv5PortChunkType.Type1: return mask == (SideBit(Sv5PortSide.Left) | SideBit(Sv5PortSide.Right));
                case Sv5PortChunkType.Type2:
                    return mask == (SideBit(Sv5PortSide.Left) | SideBit(Sv5PortSide.Down)) ||
                        mask == (SideBit(Sv5PortSide.Right) | SideBit(Sv5PortSide.Down)) ||
                        mask == (SideBit(Sv5PortSide.Left) | SideBit(Sv5PortSide.Right) | SideBit(Sv5PortSide.Down));
                case Sv5PortChunkType.Type3:
                    return mask == (SideBit(Sv5PortSide.Left) | SideBit(Sv5PortSide.Up)) ||
                        mask == (SideBit(Sv5PortSide.Right) | SideBit(Sv5PortSide.Up)) ||
                        mask == (SideBit(Sv5PortSide.Left) | SideBit(Sv5PortSide.Right) | SideBit(Sv5PortSide.Up));
                case Sv5PortChunkType.Type4:
                    return (mask & SideBit(Sv5PortSide.Up)) != 0 && (mask & SideBit(Sv5PortSide.Down)) != 0;
                default: return false;
            }
        }

        public static string ExportChunksCsv(Sv5PortCatalogSnapshot snapshot)
        {
            var text = new StringBuilder();
            text.AppendLine("ChunkId,SpaceState,Type,SourceCandidateId,SourceOffset,ReservationId,NormalPortCount,BreakableAccessCount,SolidCellCount");
            foreach (Sv5PortChunk chunk in snapshot.Chunks)
                text.AppendLine(string.Join(",", new[]
                {
                    chunk.ChunkId, chunk.SpaceState.ToString(), chunk.ChunkType.HasValue ? chunk.ChunkType.Value.ToString() : "NONE",
                    chunk.SourcePlacement.CandidateId, chunk.SourcePlacement.OriginX + ";" + chunk.SourcePlacement.OriginY,
                    Empty(chunk.ReservationId), Number(chunk.Ports.Count), Number(chunk.BreakableAccesses.Count), Number(chunk.SolidCells.Count),
                }));
            return text.ToString();
        }

        public static string ExportPortsCsv(Sv5PortCatalogSnapshot snapshot)
        {
            var text = new StringBuilder();
            text.AppendLine("ChunkId,PortId,Side,OpenCells,TraversalKind,FlowDirection,Required,EntranceGroupId");
            foreach (Sv5PortChunk chunk in snapshot.Chunks)
            foreach (Sv5EdgePort port in chunk.Ports)
                text.AppendLine(string.Join(",", new[]
                {
                    chunk.ChunkId, port.PortId, port.Side.ToString(), Join(port.OpenCells), port.TraversalKind.ToString(),
                    port.FlowDirection.ToString(), port.Required ? "true" : "false", port.EntranceGroupId,
                }));
            return text.ToString();
        }

        public static string ExportAdjacencyCsv(Sv5PortCatalogSnapshot snapshot)
        {
            var text = new StringBuilder();
            text.AppendLine("ConnectionId,FromChunkId,FromPortId,ToChunkId,ToPortId,SharedEdgeCells,EvidenceState,Evidence");
            foreach (Sv5PortAdjacency value in snapshot.AdjacencyConnections)
                text.AppendLine(string.Join(",", new[]
                {
                    value.ConnectionId, value.FromChunkId, value.FromPortId, value.ToChunkId, Empty(value.ToPortId),
                    Join(value.SharedCells), value.EvidenceState.ToString(), Escape(value.Evidence),
                }));
            return text.ToString();
        }

        public static string ExportInteriorLinksCsv(Sv5PortCatalogSnapshot snapshot)
        {
            var text = new StringBuilder();
            text.AppendLine("ConnectionId,ChunkId,FromPortId,ToPortId,TraversalKind,Required,ProfileDigest,EvidenceState,Condition,Evidence");
            foreach (Sv5PortInteriorLink value in snapshot.InteriorLinks)
                text.AppendLine(string.Join(",", new[]
                {
                    value.ConnectionId, value.ChunkId, value.FromPortId, value.ToPortId, value.TraversalKind.ToString(),
                    value.Required ? "true" : "false", value.ProfileDigest, value.EvidenceState.ToString(), Escape(value.Condition), Escape(value.Evidence),
                }));
            return text.ToString();
        }

        public static string ExportBreakableAccessCsv(Sv5PortCatalogSnapshot snapshot)
        {
            var text = new StringBuilder();
            text.AppendLine("ChunkId,AccessId,Side,OpenCells,AccessCondition,Evidence");
            foreach (Sv5PortChunk chunk in snapshot.Chunks)
            foreach (Sv5BreakableAccess value in chunk.BreakableAccesses)
                text.AppendLine(string.Join(",", new[]
                {
                    chunk.ChunkId, value.AccessId, value.Side.ToString(), Join(value.OpenCells),
                    Escape(value.AccessCondition), Escape(value.Evidence),
                }));
            return text.ToString();
        }

        public static string ExportSnapshotJson(Sv5PortCatalogSnapshot snapshot)
        {
            return "{\n" +
                "  \"data_version\": \"" + DataVersion + "\",\n" +
                "  \"chunk_size\": \"12x8\",\n" +
                "  \"source_pattern_rule\": \"" + Sv5SourcePlacementRule + "\",\n" +
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

        public static Sv5PortCell ToChunkCell(Sv5PortSide side, int edgeCoordinate)
        {
            switch (side)
            {
                case Sv5PortSide.Left: return new Sv5PortCell(0, edgeCoordinate);
                case Sv5PortSide.Right: return new Sv5PortCell(ChunkWidth - 1, edgeCoordinate);
                case Sv5PortSide.Up: return new Sv5PortCell(edgeCoordinate, ChunkHeight - 1);
                case Sv5PortSide.Down: return new Sv5PortCell(edgeCoordinate, 0);
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        private static Sv5PortCatalogSnapshot BuildFixtureInternal()
        {
            Sv5PatternCatalogSnapshot patterns = Sv5PatternCatalog.BuildInitialPool();
            Sv5PatternCandidate source = patterns.Candidates.Single(value =>
                value.PrimaryRole == Sv5PatternPrimaryRole.VoidClear);
            var placement = new Sv5PortSourcePlacement(source.CandidateId, 4, 2);
            string profileDigest = GeneratedTraversalProfileCatalog.Create().Digest;
            var chunks = new List<Sv5PortChunk>
            {
                Active("T1_A", Sv5PortChunkType.Type1, placement, new[]
                {
                    Port("P_T1A_L", Sv5PortSide.Left, new[] { 1, 2 }, Sv5PortTraversalKind.Walk, Sv5PortFlowDirection.In, true, "ENTRANCE_T1A_L"),
                    Port("P_T1A_R", Sv5PortSide.Right, new[] { 1, 2 }, Sv5PortTraversalKind.Walk, Sv5PortFlowDirection.Out, true, "ENTRANCE_T1A_R"),
                }, null, null),
                Active("T1_B", Sv5PortChunkType.Type1, placement, new[]
                {
                    Port("P_T1B_L", Sv5PortSide.Left, new[] { 1, 2 }, Sv5PortTraversalKind.Walk, Sv5PortFlowDirection.In, true, "ENTRANCE_T1B_L"),
                    Port("P_T1B_R", Sv5PortSide.Right, new[] { 1, 2 }, Sv5PortTraversalKind.Walk, Sv5PortFlowDirection.Out, false, "ENTRANCE_T1B_R"),
                }, null, null),
                Active("T1_BLOCKED_SOURCE", Sv5PortChunkType.Type1, placement, new[]
                {
                    Port("P_T1BLOCK_L", Sv5PortSide.Left, new[] { 1, 2 }, Sv5PortTraversalKind.Walk, Sv5PortFlowDirection.In, true, "ENTRANCE_T1BLOCK_L"),
                    Port("P_T1BLOCK_R", Sv5PortSide.Right, new[] { 1, 2 }, Sv5PortTraversalKind.Walk, Sv5PortFlowDirection.Out, true, "ENTRANCE_T1BLOCK_R"),
                }, null, null),
                Inactive("INACTIVE_SOLID_WALL", placement),
                Active("T2_DROP", Sv5PortChunkType.Type2, placement, new[]
                {
                    Port("P_T2_L", Sv5PortSide.Left, new[] { 1, 2 }, Sv5PortTraversalKind.Walk, Sv5PortFlowDirection.In, true, "ENTRANCE_T2_L"),
                    Port("P_T2_D", Sv5PortSide.Down, new[] { 6, 7, 8, 9 }, Sv5PortTraversalKind.Drop, Sv5PortFlowDirection.Out, true, "EXIT_T2_D"),
                }, null, null),
                Active("T3_CLIMB", Sv5PortChunkType.Type3, placement, new[]
                {
                    Port("P_T3_L", Sv5PortSide.Left, new[] { 1, 2 }, Sv5PortTraversalKind.Walk, Sv5PortFlowDirection.In, true, "ENTRANCE_T3_L"),
                    Port("P_T3_U", Sv5PortSide.Up, new[] { 5, 6 }, Sv5PortTraversalKind.Climb, Sv5PortFlowDirection.Out, true, "EXIT_T3_U"),
                }, null, null),
                Active("T4_SPLIT", Sv5PortChunkType.Type4, placement, new[]
                {
                    Port("P_T4_L_LOW", Sv5PortSide.Left, new[] { 1, 2 }, Sv5PortTraversalKind.Walk, Sv5PortFlowDirection.In, false, "ENTRANCE_T4_LOW"),
                    Port("P_T4_L_HIGH", Sv5PortSide.Left, new[] { 5, 6 }, Sv5PortTraversalKind.Hang, Sv5PortFlowDirection.In, false, "ENTRANCE_T4_HIGH"),
                    Port("P_T4_R", Sv5PortSide.Right, new[] { 1, 2 }, Sv5PortTraversalKind.Walk, Sv5PortFlowDirection.Out, false, "EXIT_T4_R"),
                    Port("P_T4_U", Sv5PortSide.Up, new[] { 4, 5 }, Sv5PortTraversalKind.Climb, Sv5PortFlowDirection.Both, false, "VERTICAL_T4_U"),
                    Port("P_T4_D", Sv5PortSide.Down, new[] { 4, 5 }, Sv5PortTraversalKind.Drop, Sv5PortFlowDirection.Both, false, "VERTICAL_T4_D"),
                }, null, new[] { new Sv5PortCell(0, 3), new Sv5PortCell(0, 4) }),
                Active("T0_SINGLE_ENTRANCE", Sv5PortChunkType.Type0, placement, new[]
                {
                    Port("P_T0_SINGLE_L", Sv5PortSide.Left, new[] { 1, 2 }, Sv5PortTraversalKind.Walk, Sv5PortFlowDirection.Both, false, "ENTRANCE_T0_SINGLE"),
                }, null, null),
                Active("T0_BREAKABLE_SECRET", Sv5PortChunkType.Type0, placement, Array.Empty<Sv5EdgePort>(), new[]
                {
                    new Sv5BreakableAccess("BREAK_T0_SECRET_R", Sv5PortSide.Right, new[] { 2, 3 },
                        "BREAKABLE_TOOL_REQUIRED", "secret shell remains solid until breakable access is resolved"),
                }, new[] { new Sv5PortCell(11, 2), new Sv5PortCell(11, 3) }, Sv5PortSpaceState.Secret),
                Special("SPECIAL_RESERVED_START", placement),
            };
            var adjacency = new[]
            {
                new Sv5PortAdjacency("ADJ_T1_A_TO_B", "T1_A", "P_T1A_R", "T1_B", "P_T1B_L", new[] { 1, 2 },
                    Sv5PortEvidenceState.FixturePassExpected, "opposite R/L ports share every declared y coordinate"),
                new Sv5PortAdjacency("ADJ_BLOCKED_INACTIVE", "T1_BLOCKED_SOURCE", "P_T1BLOCK_R", "INACTIVE_SOLID_WALL", string.Empty,
                    Array.Empty<int>(), Sv5PortEvidenceState.FixtureBlockedExpected,
                    "inactive solid has no edge port; a Player collision against its TilemapCollider2D is expected"),
                new Sv5PortAdjacency("ADJ_T4_CONTEXT_UNKNOWN", "T4_SPLIT", "P_T4_L_HIGH", "T1_B", "P_T1B_R", Array.Empty<int>(),
                    Sv5PortEvidenceState.ProfileContextUnknown,
                    "HANG-to-WALK remains profile/context dependent and is not declared as a pass"),
            };
            var links = new[]
            {
                new Sv5PortInteriorLink("INT_T2_LEFT_TO_DOWN", "T2_DROP", "P_T2_L", "P_T2_D", Sv5PortTraversalKind.Drop,
                    true, profileDigest, Sv5PortEvidenceState.FixturePassExpected, "side entry to downward exit", "actual Player drops through the physical 2-cell floor opening"),
                new Sv5PortInteriorLink("INT_T3_LEFT_TO_UP", "T3_CLIMB", "P_T3_L", "P_T3_U", Sv5PortTraversalKind.Climb,
                    true, profileDigest, Sv5PortEvidenceState.FixturePassExpected, "vertical intent on the SV5 climb surface", "actual Player leaves through the upper chunk boundary"),
                new Sv5PortInteriorLink("INT_T4_LOW_TO_RIGHT", "T4_SPLIT", "P_T4_L_LOW", "P_T4_R", Sv5PortTraversalKind.Walk,
                    false, profileDigest, Sv5PortEvidenceState.GeometricCandidate, "lower independent entrance only", "no automatic link is created for the separate high entrance"),
            };
            var snapshot = new Sv5PortCatalogSnapshot(chunks, adjacency, links, profileDigest);
            Sv5PortValidationResult validation = Validate(snapshot);
            if (!validation.IsValid) throw new InvalidOperationException(string.Join(";", validation.Errors));
            return snapshot;
        }

        private static Sv5PortChunk Active(string id, Sv5PortChunkType type, Sv5PortSourcePlacement placement,
            IEnumerable<Sv5EdgePort> ports, IEnumerable<Sv5BreakableAccess> breakableAccesses,
            IEnumerable<Sv5PortCell> extraSolids, Sv5PortSpaceState state = Sv5PortSpaceState.Active)
        {
            Sv5EdgePort[] portArray = (ports ?? Array.Empty<Sv5EdgePort>()).ToArray();
            return new Sv5PortChunk(id, state, type, placement, portArray, breakableAccesses,
                Geometry(placement, portArray, extraSolids), string.Empty);
        }

        private static Sv5PortChunk Inactive(string id, Sv5PortSourcePlacement placement)
        {
            var solids = new List<Sv5PortCell>();
            for (var y = 0; y < ChunkHeight; y++)
            for (var x = 0; x < ChunkWidth; x++) solids.Add(new Sv5PortCell(x, y));
            return new Sv5PortChunk(id, Sv5PortSpaceState.InactiveSolid, null, placement,
                Array.Empty<Sv5EdgePort>(), Array.Empty<Sv5BreakableAccess>(), solids);
        }

        private static Sv5PortChunk Special(string id, Sv5PortSourcePlacement placement) => new Sv5PortChunk(
            id, Sv5PortSpaceState.SpecialReserved, null, placement, Array.Empty<Sv5EdgePort>(),
            Array.Empty<Sv5BreakableAccess>(), Geometry(placement, Array.Empty<Sv5EdgePort>(), null), "RESERVED_START_SLOT");

        private static Sv5EdgePort Port(string id, Sv5PortSide side, IEnumerable<int> cells,
            Sv5PortTraversalKind traversal, Sv5PortFlowDirection flow, bool required, string group) =>
            new Sv5EdgePort(id, side, cells, traversal, flow, required, group);

        private static IEnumerable<Sv5PortCell> Geometry(Sv5PortSourcePlacement placement,
            IEnumerable<Sv5EdgePort> ports, IEnumerable<Sv5PortCell> extraSolids)
        {
            var cells = new HashSet<Sv5PortCell>();
            // The selected SV5 VOID_CLEAR's 4x4 cells are intentionally all air. Its retained source ID and
            // lower-left offset establish provenance without treating an edge characteristic as a port declaration.
            for (var x = 0; x < ChunkWidth; x++) cells.Add(new Sv5PortCell(x, 0));
            foreach (Sv5PortCell cell in extraSolids ?? Array.Empty<Sv5PortCell>()) cells.Add(cell);
            foreach (Sv5EdgePort port in ports ?? Array.Empty<Sv5EdgePort>())
            foreach (int coordinate in port.OpenCells) cells.Remove(ToChunkCell(port.Side, coordinate));
            return cells;
        }

        private static void ValidateChunk(Sv5PortChunk chunk, ICollection<string> errors)
        {
            if (chunk == null) { errors.Add("NULL_CHUNK"); return; }
            if (string.IsNullOrEmpty(chunk.ChunkId)) errors.Add("EMPTY_CHUNK_ID");
            if (chunk.SourcePlacement == null || string.IsNullOrEmpty(chunk.SourcePlacement.CandidateId))
                errors.Add(chunk.ChunkId + ":MISSING_SV507_SOURCE");
            else
            {
                Sv5PatternCatalogSnapshot pool = Sv5PatternCatalog.BuildInitialPool();
                if (!pool.TryGetCandidate(chunk.SourcePlacement.CandidateId, out _))
                    errors.Add(chunk.ChunkId + ":UNKNOWN_SV507_SOURCE");
                if (chunk.SourcePlacement.OriginX < 0 || chunk.SourcePlacement.OriginY < 0 ||
                    chunk.SourcePlacement.OriginX + Sv5PatternCatalog.Width > ChunkWidth ||
                    chunk.SourcePlacement.OriginY + Sv5PatternCatalog.Height > ChunkHeight)
                    errors.Add(chunk.ChunkId + ":SV5_SOURCE_OFFSET_OUT_OF_RANGE");
            }
            Duplicate(errors, chunk.Ports.Select(value => value.PortId), chunk.ChunkId + ":DUPLICATE_PORT_ID");
            foreach (Sv5EdgePort port in chunk.Ports) ValidatePort(chunk, port, errors);
            foreach (Sv5BreakableAccess access in chunk.BreakableAccesses) ValidateBreakable(chunk, access, errors);
            if (chunk.SpaceState == Sv5PortSpaceState.InactiveSolid)
            {
                if (chunk.ChunkType.HasValue || chunk.Ports.Count != 0 || chunk.BreakableAccesses.Count != 0 ||
                    chunk.SolidCells.Count != ChunkCellCount) errors.Add(chunk.ChunkId + ":INACTIVE_SOLID_MUST_HAVE_NO_PORTS_AND_FULL_SOLID_GEOMETRY");
                return;
            }
            if (chunk.SpaceState == Sv5PortSpaceState.SpecialReserved)
            {
                if (chunk.ChunkType.HasValue || string.IsNullOrEmpty(chunk.ReservationId))
                    errors.Add(chunk.ChunkId + ":SPECIAL_RESERVED_REQUIRES_SEPARATE_RESERVATION");
                return;
            }
            if (!chunk.ChunkType.HasValue) { errors.Add(chunk.ChunkId + ":ACTIVE_OR_SECRET_REQUIRES_TYPE"); return; }
            if (chunk.ChunkType.Value == Sv5PortChunkType.Type0)
            {
                if (chunk.Ports.Count > 1) errors.Add(chunk.ChunkId + ":TYPE0_HAS_MORE_THAN_ONE_NORMAL_ENTRANCE");
                if (chunk.Ports.Count == 0 && chunk.BreakableAccesses.Count == 0)
                    errors.Add(chunk.ChunkId + ":TYPE0_ZERO_ENTRANCE_REQUIRES_BREAKABLE_ACCESS");
            }
            else if (!IsAllowedSideSet(chunk.ChunkType.Value, chunk.Ports.Select(value => value.Side)))
                errors.Add(chunk.ChunkId + ":TYPE_SIDE_SET_NOT_ALLOWED");
        }

        private static void ValidatePort(Sv5PortChunk chunk, Sv5EdgePort port, ICollection<string> errors)
        {
            if (port == null) { errors.Add(chunk.ChunkId + ":NULL_PORT"); return; }
            if (string.IsNullOrEmpty(port.PortId) || string.IsNullOrEmpty(port.EntranceGroupId) || port.OpenCells.Count == 0)
                errors.Add(chunk.ChunkId + ":PORT_MISSING_REQUIRED_FIELD");
            if (!Enum.IsDefined(typeof(Sv5PortTraversalKind), port.TraversalKind) ||
                !Enum.IsDefined(typeof(Sv5PortFlowDirection), port.FlowDirection)) errors.Add(chunk.ChunkId + ":PORT_ENUM_INVALID");
            foreach (int coordinate in port.OpenCells)
            {
                int maximum = port.Side == Sv5PortSide.Left || port.Side == Sv5PortSide.Right ? ChunkHeight : ChunkWidth;
                if (coordinate < 0 || coordinate >= maximum) errors.Add(chunk.ChunkId + ":PORT_COORDINATE_OUT_OF_RANGE");
                else if (chunk.IsSolid(ToChunkCell(port.Side, coordinate).X, ToChunkCell(port.Side, coordinate).Y))
                    errors.Add(chunk.ChunkId + ":PORT_OPEN_CELL_IS_SOLID");
            }
        }

        private static void ValidateBreakable(Sv5PortChunk chunk, Sv5BreakableAccess access, ICollection<string> errors)
        {
            if (access == null || string.IsNullOrEmpty(access.AccessId) || access.OpenCells.Count == 0 ||
                string.IsNullOrEmpty(access.AccessCondition) || string.IsNullOrEmpty(access.Evidence))
                errors.Add(chunk.ChunkId + ":BREAKABLE_ACCESS_MISSING_FIELD");
            else
            {
                foreach (int coordinate in access.OpenCells)
                {
                    int maximum = access.Side == Sv5PortSide.Left || access.Side == Sv5PortSide.Right
                        ? ChunkHeight : ChunkWidth;
                    if (coordinate < 0 || coordinate >= maximum ||
                        !chunk.IsSolid(ToChunkCell(access.Side, coordinate).X,
                            ToChunkCell(access.Side, coordinate).Y))
                        errors.Add(chunk.ChunkId + ":BREAKABLE_ACCESS_MUST_REFERENCE_SOLID_SHELL");
                }
            }
        }

        private static void ValidateAdjacency(Sv5PortCatalogSnapshot snapshot, Sv5PortAdjacency value,
            ICollection<string> errors)
        {
            if (!snapshot.TryGetPort(value.FromChunkId, value.FromPortId, out Sv5EdgePort from))
            { errors.Add(value.ConnectionId + ":MISSING_FROM_PORT"); return; }
            if (value.EvidenceState != Sv5PortEvidenceState.FixturePassExpected) return;
            if (!snapshot.TryGetPort(value.ToChunkId, value.ToPortId, out Sv5EdgePort to))
            { errors.Add(value.ConnectionId + ":MISSING_TO_PORT"); return; }
            if (!AreOpposite(from.Side, to.Side)) errors.Add(value.ConnectionId + ":SIDES_NOT_OPPOSITE");
            if (!from.AllowsExit || !to.AllowsEntry) errors.Add(value.ConnectionId + ":FLOW_DIRECTION_NOT_COMPATIBLE");
            int[] intersection = from.OpenCells.Intersect(to.OpenCells).OrderBy(cell => cell).ToArray();
            if (intersection.Length == 0 || !intersection.SequenceEqual(value.SharedCells))
                errors.Add(value.ConnectionId + ":OPEN_CELL_INTERSECTION_MISMATCH");
        }

        private static void ValidateInteriorLink(Sv5PortCatalogSnapshot snapshot, Sv5PortInteriorLink link,
            ICollection<string> errors)
        {
            if (!snapshot.TryGetPort(link.ChunkId, link.FromPortId, out Sv5EdgePort from) ||
                !snapshot.TryGetPort(link.ChunkId, link.ToPortId, out Sv5EdgePort to))
            { errors.Add(link.ConnectionId + ":MISSING_INTERIOR_PORT"); return; }
            if (!from.AllowsEntry || !to.AllowsExit) errors.Add(link.ConnectionId + ":INTERIOR_FLOW_DIRECTION_NOT_COMPATIBLE");
            if (string.IsNullOrEmpty(link.ProfileDigest) || !string.Equals(link.ProfileDigest, snapshot.ProfileDigest,
                StringComparison.Ordinal)) errors.Add(link.ConnectionId + ":INTERIOR_PROFILE_MISMATCH");
            if (link.Required && link.EvidenceState == Sv5PortEvidenceState.ProfileContextUnknown)
                errors.Add(link.ConnectionId + ":REQUIRED_LINK_CANNOT_BE_UNKNOWN");
        }

        private static bool AreOpposite(Sv5PortSide left, Sv5PortSide right) =>
            (left == Sv5PortSide.Left && right == Sv5PortSide.Right) ||
            (left == Sv5PortSide.Right && right == Sv5PortSide.Left) ||
            (left == Sv5PortSide.Up && right == Sv5PortSide.Down) ||
            (left == Sv5PortSide.Down && right == Sv5PortSide.Up);

        private static int SideMask(IEnumerable<Sv5PortSide> values) => (values ?? Array.Empty<Sv5PortSide>())
            .Distinct().Aggregate(0, (current, value) => current | SideBit(value));
        private static int SideBit(Sv5PortSide side) => 1 << (int)side;
        private static void Duplicate(ICollection<string> errors, IEnumerable<string> values, string error) { if ((values ?? Array.Empty<string>()).GroupBy(value => value, StringComparer.Ordinal).Any(group => group.Count() > 1)) errors.Add(error); }
        private static string Join(IEnumerable<int> values) => string.Join(";", (values ?? Array.Empty<int>()).Select(Number));
        private static string Escape(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        private static string Empty(string value) => string.IsNullOrEmpty(value) ? "NONE" : value;
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
