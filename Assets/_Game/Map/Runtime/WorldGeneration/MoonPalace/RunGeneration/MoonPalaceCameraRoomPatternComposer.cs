using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public sealed class MoonPalaceCameraRoomPatternPlacement
    {
        internal MoonPalaceCameraRoomPatternPlacement(int slotIndex, PatternSlotCoordinate slot, string roomId, IEnumerable<string> connectorIds,
            string routeOwnership, MoonPalaceMicroPatternCandidate candidate, IEnumerable<MoonPalaceRunDirection> directions,
            string transform, string reason, int attempts)
        {
            SlotIndex = slotIndex; PatternSlot = slot; RoomId = roomId ?? string.Empty;
            ConnectorIds = new ReadOnlyCollection<string>((connectorIds ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToList());
            RouteOwnership = routeOwnership ?? string.Empty; Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            RequiredDirections = new ReadOnlyCollection<MoonPalaceRunDirection>((directions ?? Array.Empty<MoonPalaceRunDirection>()).Distinct().OrderBy(value => value).ToList());
            Transform = transform ?? string.Empty; SelectionReason = reason ?? string.Empty; AttemptCount = attempts;
        }
        public int SlotIndex { get; }
        public PatternSlotCoordinate PatternSlot { get; }
        public string RoomId { get; }
        public IReadOnlyList<string> ConnectorIds { get; }
        public string RouteOwnership { get; }
        public MoonPalaceMicroPatternCandidate Candidate { get; }
        public IReadOnlyList<MoonPalaceRunDirection> RequiredDirections { get; }
        public string Transform { get; }
        public string SelectionReason { get; }
        public int AttemptCount { get; }
        public string PresentationFamilyLink => "MAP21_02_READ_ONLY_PRESENTATION_FAMILY";
    }

    public sealed class MoonPalaceCameraRoomPatternComposition
    {
        private readonly bool[] openTiles;
        internal MoonPalaceCameraRoomPatternComposition(MoonPalaceCameraRoomCourseConfig config, MoonPalaceCameraRoomGraph graph,
            MoonPalaceCameraRoomConnectorPlan connectors, MoonPalaceMicroPatternCandidateSet candidates,
            IEnumerable<MoonPalaceCameraRoomPatternPlacement> placements, bool[] open, IEnumerable<MoonPalaceRunTileCoordinate> mainTiles,
            IEnumerable<MoonPalaceRunTileCoordinate> branchTiles, IEnumerable<MoonPalaceRunTileCoordinate> splitTiles,
            IDictionary<string, int> rejections, int attempts, string digest, string mapDigest)
        {
            Config = config; Graph = graph; Connectors = connectors; CandidateSet = candidates;
            Placements = ReadOnly(placements); openTiles = open ?? Array.Empty<bool>(); MainRouteTiles = ReadOnly(mainTiles);
            BranchRouteTiles = ReadOnly(branchTiles); SplitRejoinRouteTiles = ReadOnly(splitTiles);
            RejectionCounts = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(rejections ?? new Dictionary<string, int>(), StringComparer.Ordinal));
            SelectionAttemptCount = attempts; CompositionDigest = digest ?? string.Empty; MapDigest = mapDigest ?? string.Empty;
        }
        public MoonPalaceCameraRoomCourseConfig Config { get; }
        public MoonPalaceCameraRoomGraph Graph { get; }
        public MoonPalaceCameraRoomConnectorPlan Connectors { get; }
        public MoonPalaceMicroPatternCandidateSet CandidateSet { get; }
        public IReadOnlyList<MoonPalaceCameraRoomPatternPlacement> Placements { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> MainRouteTiles { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> BranchRouteTiles { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> SplitRejoinRouteTiles { get; }
        public IReadOnlyDictionary<string, int> RejectionCounts { get; }
        public int SelectionAttemptCount { get; }
        public int PlacementCount => Placements.Count;
        public int RotationCount => 0;
        public int FallbackCarveCount => 0;
        public int SilentRepairCount => 0;
        public string CompositionDigest { get; }
        public string MapDigest { get; }
        public MoonPalaceRunTileCoordinate StartTile => CenterTile(Graph.Start.Slot);
        public MoonPalaceRunTileCoordinate ExitTile => CenterTile(Graph.Exit.Slot);
        public bool IsOpen(int x, int y) => x >= 0 && x < Config.TileWidth && y >= 0 && y < Config.TileHeight && openTiles[(y * Config.TileWidth) + x];
        public MoonPalaceCameraRoomPatternPlacement Placement(PatternSlotCoordinate slot) => Placements.Single(item => item.PatternSlot.Equals(slot));
        private MoonPalaceRunTileCoordinate CenterTile(PatternSlotCoordinate slot) => new MoonPalaceRunTileCoordinate(slot.X * Config.PatternWidth + 1, slot.Y * Config.PatternHeight + 1);
        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());
    }

    /// <summary>Selects real audited candidates for every RUN03 slot. It never rotates or carves masks.</summary>
    public static class MoonPalaceCameraRoomPatternComposer
    {
        public const int RouteSocketBit = 1;

        public static MoonPalaceCameraRoomPatternComposition Compose(MoonPalaceCameraRoomCourseConfig config, MoonPalaceCameraRoomGraph graph,
            MoonPalaceCameraRoomConnectorPlan connectors, bool reverseCandidateEnumeration = false)
        {
            if (config == null || graph == null || connectors == null || !ReferenceEquals(config, graph.Config) || !ReferenceEquals(graph, connectors.Graph))
                throw new ArgumentException("RUN03 composition requires matching course config, room graph, and connector plan.");
            config.Validate();
            var candidateSet = MoonPalaceMicroPatternCandidateLibrary.Build();
            if (candidateSet.RawMaskCount != 65536 || candidateSet.Candidates.Count != MoonPalaceCameraRoomCourseConfig.RequiredCandidateCount)
                throw new InvalidOperationException("RUN03 requires the audited 65,536-mask, 500-candidate MicroPattern pool.");
            var candidates = reverseCandidateEnumeration ? candidateSet.Candidates.Reverse().ToList() : candidateSet.Candidates.ToList();
            var requirements = BuildRequirements(graph);
            var connectorsBySlot = connectors.Gates.SelectMany(gate => new[] { new { Slot = gate.FromPatternSlot, Id = gate.ConnectorId }, new { Slot = gate.ToPatternSlot, Id = gate.ConnectorId } })
                .GroupBy(item => item.Slot).ToDictionary(group => group.Key, group => group.Select(item => item.Id).Distinct().ToList());
            var main = new HashSet<PatternSlotCoordinate>(graph.MainRoute.PatternSlots);
            var branch = new HashSet<PatternSlotCoordinate>(graph.BranchRoutes.SelectMany(route => route.PatternSlots));
            var split = new HashSet<PatternSlotCoordinate>(graph.SplitRejoinRoutes.SelectMany(route => route.AlternateSlots.Concat(new[] { route.Split, route.Rejoin })));
            var rejections = new SortedDictionary<string, int>(StringComparer.Ordinal)
            {
                ["not_connected_open_candidate"] = 0, ["route_anchor_solid"] = 0, ["missing_required_socket"] = 0, ["filler_selection"] = 0,
            };
            var placements = new List<MoonPalaceCameraRoomPatternPlacement>(); var attempts = 0;
            for (var y = 0; y < config.CoursePatternGridHeight; y++)
            for (var x = 0; x < config.CoursePatternGridWidth; x++)
            {
                var slot = new PatternSlotCoordinate(x, y); var index = (y * config.CoursePatternGridWidth) + x;
                requirements.TryGetValue(slot, out var directions); connectorsBySlot.TryGetValue(slot, out var gateIds);
                MoonPalaceMicroPatternCandidate candidate; int selectionAttempts; string ownership;
                if (directions != null && directions.Count > 0)
                {
                    candidate = SelectRouteCandidate(candidates, config.SeedValue, index, directions, rejections, out selectionAttempts);
                    ownership = Ownership(slot, main, branch, split);
                }
                else
                {
                    candidate = SelectFillerCandidate(candidates, config.SeedValue, index, out selectionAttempts); rejections["filler_selection"]++;
                    ownership = "FILLER_DETAIL";
                }
                attempts += selectionAttempts;
                var room = graph.Rooms.FirstOrDefault(item => item.Bounds.Contains(slot));
                placements.Add(new MoonPalaceCameraRoomPatternPlacement(index, slot, room == null ? "OUTSIDE_QUIET" : room.RoomId,
                    gateIds ?? new List<string>(), ownership, candidate, directions ?? Array.Empty<MoonPalaceRunDirection>(), "IDENTITY_NO_ROTATION",
                    ownership == "FILLER_DETAIL" ? "FILLER_DETAIL_FROM_DIVERSE_500_POOL" : ownership + "_SOCKET_FILTERED", selectionAttempts));
            }
            if (placements.Count != config.CoursePatternGridWidth * config.CoursePatternGridHeight)
                throw new InvalidOperationException("RUN03 did not place all 1,920 direct MicroPattern slots.");
            var bySlot = placements.ToDictionary(item => item.PatternSlot, item => item);
            foreach (var edge in graph.Edges) RequireReciprocalSockets(bySlot[edge.From].Candidate, bySlot[edge.To].Candidate, edge.Direction);
            var open = BuildOpenTiles(config, placements);
            var compositionLines = new List<string> { "RUN03_COMPOSITION", config.CanonicalDigest, graph.Digest.Value, connectors.CanonicalDigest, candidateSet.CanonicalDigest };
            compositionLines.AddRange(placements.OrderBy(item => item.SlotIndex).Select(CanonicalPlacement));
            compositionLines.AddRange(rejections.Select(item => "REJECTION|" + item.Key + "|" + item.Value.ToString(CultureInfo.InvariantCulture)));
            return new MoonPalaceCameraRoomPatternComposition(config, graph, connectors, candidateSet, placements, open,
                EdgeTiles(graph.Edges.Where(edge => edge.RouteOwnership == "MAIN"), config), EdgeTiles(graph.Edges.Where(edge => edge.RouteOwnership == "BRANCH"), config),
                EdgeTiles(graph.Edges.Where(edge => edge.RouteOwnership == "SPLIT_REJOIN"), config), rejections, attempts,
                BakingCanonicalDigest.HashCanonicalLines(compositionLines), BakingCanonicalDigest.HashCanonicalLines(placements.OrderBy(item => item.SlotIndex).Select(item => item.PatternSlot + "|" + item.Candidate.MaskU16Hex)));
        }

        public static void RequireReciprocalSockets(MoonPalaceMicroPatternCandidate from, MoonPalaceMicroPatternCandidate to, MoonPalaceRunDirection direction)
        {
            if (from == null || to == null || !HasSocket(from, direction) || !HasSocket(to, MoonPalaceCameraRoomConnectorPlanner.Opposite(direction)))
                throw new InvalidOperationException("RUN03 rejects incompatible candidate sockets; no static tile copy, fallback carve, or silent repair is attempted.");
        }
        public static bool HasSocket(MoonPalaceMicroPatternCandidate candidate, MoonPalaceRunDirection direction)
        {
            if (candidate == null) return false;
            var bits = direction == MoonPalaceRunDirection.North ? candidate.NorthSocketBits : direction == MoonPalaceRunDirection.South ? candidate.SouthSocketBits :
                direction == MoonPalaceRunDirection.West ? candidate.WestSocketBits : candidate.EastSocketBits;
            return (bits & (1 << RouteSocketBit)) != 0;
        }

        private static Dictionary<PatternSlotCoordinate, IReadOnlyList<MoonPalaceRunDirection>> BuildRequirements(MoonPalaceCameraRoomGraph graph)
        {
            var values = new Dictionary<PatternSlotCoordinate, List<MoonPalaceRunDirection>>();
            foreach (var edge in graph.Edges)
            {
                Add(values, edge.From, edge.Direction); Add(values, edge.To, MoonPalaceCameraRoomConnectorPlanner.Opposite(edge.Direction));
            }
            return values.ToDictionary(item => item.Key, item => (IReadOnlyList<MoonPalaceRunDirection>)item.Value.Distinct().OrderBy(direction => direction).ToList());
        }
        private static void Add(IDictionary<PatternSlotCoordinate, List<MoonPalaceRunDirection>> values, PatternSlotCoordinate slot, MoonPalaceRunDirection direction)
        {
            if (!values.TryGetValue(slot, out var directions)) { directions = new List<MoonPalaceRunDirection>(); values.Add(slot, directions); }
            directions.Add(direction);
        }
        private static MoonPalaceMicroPatternCandidate SelectRouteCandidate(IEnumerable<MoonPalaceMicroPatternCandidate> candidates, int seed, int index, IReadOnlyList<MoonPalaceRunDirection> directions, IDictionary<string, int> rejections, out int attempts)
        {
            attempts = 0;
            foreach (var candidate in OrderedCandidates(candidates, seed, index))
            {
                attempts++;
                if (!candidate.ChunkPathAllowed) { rejections["not_connected_open_candidate"]++; continue; }
                if (!candidate.IsOpen(1, 1)) { rejections["route_anchor_solid"]++; continue; }
                if (directions.Any(direction => !HasSocket(candidate, direction))) { rejections["missing_required_socket"]++; continue; }
                return candidate;
            }
            throw new InvalidOperationException("RUN03 recorded a failed candidate filter instead of carving a blocked connector.");
        }
        private static MoonPalaceMicroPatternCandidate SelectFillerCandidate(IEnumerable<MoonPalaceMicroPatternCandidate> candidates, int seed, int index, out int attempts)
        {
            var candidate = OrderedCandidates(candidates, seed, index).FirstOrDefault(); attempts = 1;
            if (candidate == null) throw new InvalidOperationException("RUN03 candidate pool is empty.");
            return candidate;
        }
        private static IEnumerable<MoonPalaceMicroPatternCandidate> OrderedCandidates(IEnumerable<MoonPalaceMicroPatternCandidate> candidates, int seed, int index) => candidates.OrderBy(candidate => Stable(seed, index, candidate.Mask)).ThenBy(candidate => candidate.Mask);
        private static int Stable(int seed, int index, ushort mask) { unchecked { var value = seed; value = (value * 397) ^ index; value = (value * 397) ^ mask; value ^= value >> 16; return value & int.MaxValue; } }
        private static bool[] BuildOpenTiles(MoonPalaceCameraRoomCourseConfig config, IEnumerable<MoonPalaceCameraRoomPatternPlacement> placements)
        {
            var open = new bool[config.TileWidth * config.TileHeight];
            foreach (var placement in placements)
            for (var y = 0; y < config.PatternHeight; y++) for (var x = 0; x < config.PatternWidth; x++)
                open[((placement.PatternSlot.Y * config.PatternHeight + y) * config.TileWidth) + placement.PatternSlot.X * config.PatternWidth + x] = placement.Candidate.IsOpen(x, y);
            return open;
        }
        private static IEnumerable<MoonPalaceRunTileCoordinate> EdgeTiles(IEnumerable<CameraRoomEdge> edges, MoonPalaceCameraRoomCourseConfig config)
        {
            return edges.SelectMany(edge => EdgeTiles(edge.From, edge.To, config)).Distinct().OrderBy(tile => tile.Y).ThenBy(tile => tile.X).ToList();
        }
        private static IEnumerable<MoonPalaceRunTileCoordinate> EdgeTiles(PatternSlotCoordinate from, PatternSlotCoordinate to, MoonPalaceCameraRoomCourseConfig config)
        {
            var start = new MoonPalaceRunTileCoordinate(from.X * config.PatternWidth + 1, from.Y * config.PatternHeight + 1);
            var end = new MoonPalaceRunTileCoordinate(to.X * config.PatternWidth + 1, to.Y * config.PatternHeight + 1); var x = start.X; var y = start.Y;
            while (true) { yield return new MoonPalaceRunTileCoordinate(x, y); if (x == end.X && y == end.Y) yield break; x += Math.Sign(end.X - start.X); y += Math.Sign(end.Y - start.Y); }
        }
        private static string Ownership(PatternSlotCoordinate slot, ISet<PatternSlotCoordinate> main, ISet<PatternSlotCoordinate> branch, ISet<PatternSlotCoordinate> split) => split.Contains(slot) ? "SPLIT_REJOIN" : branch.Contains(slot) ? "BRANCH" : main.Contains(slot) ? "MAIN" : "ROUTE";
        private static string CanonicalPlacement(MoonPalaceCameraRoomPatternPlacement item) => "PLACEMENT|" + item.SlotIndex.ToString(CultureInfo.InvariantCulture) + "|" + item.PatternSlot + "|" + item.RoomId + "|" + string.Join(";", item.ConnectorIds) + "|" + item.RouteOwnership + "|" + item.Candidate.CandidateId + "|" + item.Candidate.MaskU16Hex + "|" + item.Candidate.SocketSignature + "|" + item.Transform + "|" + item.SelectionReason + "|" + item.AttemptCount.ToString(CultureInfo.InvariantCulture);
        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());
    }
}
