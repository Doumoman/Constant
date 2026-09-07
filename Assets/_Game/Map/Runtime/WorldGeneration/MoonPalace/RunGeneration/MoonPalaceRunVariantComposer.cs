using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public sealed class MoonPalaceRunVariantPlacement
    {
        internal MoonPalaceRunVariantPlacement(int slotIndex, PatternSlotCoordinate coordinate, MoonPalaceMicroPatternCandidate candidate,
            IEnumerable<RunSocketRequirement> requirements, string transform, string selectionReason, int attemptCount,
            bool isMainRoute, bool isBranchRoute, bool isSplitRejoinRoute)
        {
            SlotIndex = slotIndex; Coordinate = coordinate; Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            Requirements = new ReadOnlyCollection<RunSocketRequirement>((requirements ?? Array.Empty<RunSocketRequirement>()).ToList());
            Transform = transform ?? string.Empty; SelectionReason = selectionReason ?? string.Empty; AttemptCount = attemptCount;
            IsMainRoute = isMainRoute; IsBranchRoute = isBranchRoute; IsSplitRejoinRoute = isSplitRejoinRoute;
        }
        public int SlotIndex { get; }
        public PatternSlotCoordinate Coordinate { get; }
        public MoonPalaceMicroPatternCandidate Candidate { get; }
        public IReadOnlyList<RunSocketRequirement> Requirements { get; }
        public string Transform { get; }
        public string SelectionReason { get; }
        public int AttemptCount { get; }
        public bool IsMainRoute { get; }
        public bool IsBranchRoute { get; }
        public bool IsSplitRejoinRoute { get; }
        public string PresentationFamilyLink => "MAP21_02_READ_ONLY_PRESENTATION_FAMILY";
    }

    public sealed class MoonPalaceRunVariantComposition
    {
        private readonly bool[] openTiles;

        internal MoonPalaceRunVariantComposition(MoonPalaceRunVariantConfig config, RunVariantGraph graph,
            MoonPalaceMicroPatternCandidateSet candidateSet, IEnumerable<MoonPalaceRunVariantPlacement> placements, bool[] open,
            IEnumerable<MoonPalaceRunTileCoordinate> mainRouteTiles, IEnumerable<MoonPalaceRunTileCoordinate> branchRouteTiles,
            IEnumerable<MoonPalaceRunTileCoordinate> splitRejoinRouteTiles, IEnumerable<MoonPalaceRunTileCoordinate> requiredWaypoints,
            IEnumerable<MoonPalaceRunTileCoordinate> branchEntryTiles, IEnumerable<MoonPalaceRunTileCoordinate> splitRejoinWaypointTiles,
            IDictionary<string, int> rejectionCounts, int attempts, string compositionDigest, string mapDigest)
        {
            Config = config; Graph = graph; CandidateSet = candidateSet;
            Placements = ReadOnly(placements); openTiles = open ?? Array.Empty<bool>(); MainRouteTiles = ReadOnly(mainRouteTiles);
            BranchRouteTiles = ReadOnly(branchRouteTiles); SplitRejoinRouteTiles = ReadOnly(splitRejoinRouteTiles);
            RequiredWaypoints = ReadOnly(requiredWaypoints); BranchEntryTiles = ReadOnly(branchEntryTiles);
            SplitRejoinWaypointTiles = ReadOnly(splitRejoinWaypointTiles);
            RejectionCounts = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(rejectionCounts ?? new Dictionary<string, int>(), StringComparer.Ordinal));
            SelectionAttemptCount = attempts; CompositionDigest = compositionDigest ?? string.Empty; MapDigest = mapDigest ?? string.Empty;
        }
        public MoonPalaceRunVariantConfig Config { get; }
        public RunVariantGraph Graph { get; }
        public MoonPalaceMicroPatternCandidateSet CandidateSet { get; }
        public IReadOnlyList<MoonPalaceRunVariantPlacement> Placements { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> MainRouteTiles { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> BranchRouteTiles { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> SplitRejoinRouteTiles { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> RequiredWaypoints { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> BranchEntryTiles { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> SplitRejoinWaypointTiles { get; }
        public IReadOnlyDictionary<string, int> RejectionCounts { get; }
        public int SelectionAttemptCount { get; }
        public int PlacementCount => Placements.Count;
        public int MainRoutePlacementCount => Placements.Count(placement => placement.IsMainRoute);
        public int BranchPlacementCount => Placements.Count(placement => placement.IsBranchRoute);
        public int SplitRejoinPlacementCount => Placements.Count(placement => placement.IsSplitRejoinRoute);
        public int FillerDetailPlacementCount => Placements.Count(placement => !placement.IsMainRoute && !placement.IsBranchRoute && !placement.IsSplitRejoinRoute);
        public int FallbackCarveCount => 0;
        public int SilentRepairCount => 0;
        public string CompositionDigest { get; }
        public string MapDigest { get; }
        public MoonPalaceRunTileCoordinate StartTile => CenterTile(Graph.Start.Slot, Config.PatternWidth, Config.PatternHeight);
        public MoonPalaceRunTileCoordinate ExitTile => CenterTile(Graph.Exit.Slot, Config.PatternWidth, Config.PatternHeight);
        public bool IsOpen(int x, int y) => x >= 0 && x < Config.TileWidth && y >= 0 && y < Config.TileHeight && openTiles[(y * Config.TileWidth) + x];

        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());
        private static MoonPalaceRunTileCoordinate CenterTile(PatternSlotCoordinate coordinate, int patternWidth, int patternHeight) => new MoonPalaceRunTileCoordinate(coordinate.X * patternWidth + 1, coordinate.Y * patternHeight + 1);
    }

    public static class MoonPalaceRunVariantComposer
    {
        public const int RouteSocketBit = 1;

        public static IReadOnlyList<MoonPalaceRunVariantComposition> ComposeDefaults(bool reverseCandidateEnumeration = false)
        {
            return new ReadOnlyCollection<MoonPalaceRunVariantComposition>(MoonPalaceRunVariantConfig.CreateDefaults()
                .Select(config => Compose(config, RunVariantGraph.Create(config), reverseCandidateEnumeration)).ToList());
        }

        public static MoonPalaceRunVariantComposition Compose(MoonPalaceRunVariantConfig config, RunVariantGraph graph,
            bool reverseCandidateEnumeration = false)
        {
            if (config == null || graph == null || !ReferenceEquals(config, graph.Config))
                throw new ArgumentException("RUN02 composition requires its matching config and direct MicroPattern graph.");
            config.Validate();
            var candidateSet = MoonPalaceMicroPatternCandidateLibrary.Build();
            if (candidateSet.RawMaskCount != 65536 || candidateSet.Candidates.Count != MoonPalaceRunVariantConfig.RequiredCandidateCount)
                throw new InvalidOperationException("RUN02 requires the audited 65,536-mask, 500-candidate MicroPattern pool.");
            var candidates = reverseCandidateEnumeration ? candidateSet.Candidates.Reverse().ToList() : candidateSet.Candidates.ToList();
            var requirements = graph.SocketRequirements.GroupBy(requirement => requirement.Coordinate)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<RunSocketRequirement>)group.OrderBy(item => item.Direction).ToList());
            var mainSlots = new HashSet<PatternSlotCoordinate>(graph.MainPathNodes);
            var branchSlots = new HashSet<PatternSlotCoordinate>(graph.Nodes.Where(node => node.Kind == "BRANCH" && node.BranchIndex > 0).Select(node => node.Coordinate));
            var splitSlots = new HashSet<PatternSlotCoordinate>(graph.SplitRejoinWaypointSlots);
            var rejections = new SortedDictionary<string, int>(StringComparer.Ordinal)
            {
                ["not_connected_open_candidate"] = 0, ["route_anchor_solid"] = 0, ["missing_required_socket"] = 0, ["filler_selection"] = 0,
            };
            var placements = new List<MoonPalaceRunVariantPlacement>(); var attempts = 0;
            for (var y = 0; y < config.PatternGridHeight; y++)
            for (var x = 0; x < config.PatternGridWidth; x++)
            {
                var coordinate = new PatternSlotCoordinate(x, y); var slotIndex = (y * config.PatternGridWidth) + x;
                requirements.TryGetValue(coordinate, out var required);
                MoonPalaceMicroPatternCandidate candidate; string reason; int selectionAttempts;
                if (required != null && required.Count > 0)
                {
                    candidate = SelectRouteCandidate(candidates, config.SeedValue, slotIndex, required, rejections, out selectionAttempts);
                    reason = ReasonFor(mainSlots.Contains(coordinate), branchSlots.Contains(coordinate), splitSlots.Contains(coordinate));
                }
                else
                {
                    candidate = SelectFillerCandidate(candidates, config.SeedValue, slotIndex, out selectionAttempts);
                    rejections["filler_selection"]++; reason = "FILLER_DETAIL_FROM_DIVERSE_500_POOL";
                }
                attempts += selectionAttempts;
                placements.Add(new MoonPalaceRunVariantPlacement(slotIndex, coordinate, candidate, required ?? Array.Empty<RunSocketRequirement>(),
                    "IDENTITY_NO_ROTATION", reason, selectionAttempts, mainSlots.Contains(coordinate), branchSlots.Contains(coordinate), splitSlots.Contains(coordinate)));
            }
            if (placements.Count != config.PatternGridWidth * config.PatternGridHeight)
                throw new InvalidOperationException("RUN02 did not place every direct 4x4 MicroPattern slot.");
            var byCoordinate = placements.ToDictionary(placement => placement.Coordinate, placement => placement);
            foreach (var edge in graph.Edges) RequireReciprocalSockets(byCoordinate[edge.From].Candidate, byCoordinate[edge.To].Candidate, edge.Direction);
            var open = BuildOpenTiles(config, placements);
            var mainTiles = EdgeTiles(graph.Edges.Where(edge => edge.Kind == "MAIN"), config.PatternWidth, config.PatternHeight);
            var branchTiles = EdgeTiles(graph.Edges.Where(edge => edge.Kind == "BRANCH"), config.PatternWidth, config.PatternHeight);
            var splitTiles = EdgeTiles(graph.Edges.Where(edge => edge.Kind == "SPLIT_REJOIN"), config.PatternWidth, config.PatternHeight);
            var waypoints = graph.RequiredWaypointSlots.Select(slot => CenterTile(slot, config.PatternWidth, config.PatternHeight));
            var branchEntries = graph.BranchEntrySlots.Select(slot => CenterTile(slot, config.PatternWidth, config.PatternHeight));
            var splitWaypoints = graph.SplitRejoinWaypointSlots.Select(slot => CenterTile(slot, config.PatternWidth, config.PatternHeight));
            var compositionLines = new List<string> { "RUN02_COMPOSITION", config.CanonicalDigest, graph.Digest.Value, candidateSet.CanonicalDigest };
            compositionLines.AddRange(placements.OrderBy(placement => placement.SlotIndex).Select(CanonicalPlacement));
            compositionLines.AddRange(rejections.Select(pair => "REJECTION|" + pair.Key + "|" + pair.Value.ToString(CultureInfo.InvariantCulture)));
            var compositionDigest = BakingCanonicalDigest.HashCanonicalLines(compositionLines);
            var mapDigest = BakingCanonicalDigest.HashCanonicalLines(placements.OrderBy(placement => placement.SlotIndex).Select(placement => placement.Coordinate + "|" + placement.Candidate.MaskU16Hex));
            return new MoonPalaceRunVariantComposition(config, graph, candidateSet, placements, open, mainTiles, branchTiles, splitTiles,
                waypoints, branchEntries, splitWaypoints, rejections, attempts, compositionDigest, mapDigest);
        }

        public static void RequireReciprocalSockets(MoonPalaceMicroPatternCandidate from, MoonPalaceMicroPatternCandidate to, MoonPalaceRunDirection direction)
        {
            if (from == null || to == null || !HasSocket(from, direction) || !HasSocket(to, Opposite(direction)))
                throw new InvalidOperationException("RUN02 rejects incompatible MicroPattern sockets; no static copy, carve, or repair is attempted.");
        }

        public static bool HasSocket(MoonPalaceMicroPatternCandidate candidate, MoonPalaceRunDirection direction)
        {
            if (candidate == null) return false;
            var bits = direction == MoonPalaceRunDirection.North ? candidate.NorthSocketBits : direction == MoonPalaceRunDirection.South ? candidate.SouthSocketBits :
                direction == MoonPalaceRunDirection.West ? candidate.WestSocketBits : candidate.EastSocketBits;
            return (bits & (1 << RouteSocketBit)) != 0;
        }

        public static MoonPalaceRunDirection Opposite(MoonPalaceRunDirection direction) => direction == MoonPalaceRunDirection.North ? MoonPalaceRunDirection.South :
            direction == MoonPalaceRunDirection.South ? MoonPalaceRunDirection.North : direction == MoonPalaceRunDirection.East ? MoonPalaceRunDirection.West : MoonPalaceRunDirection.East;

        private static bool[] BuildOpenTiles(MoonPalaceRunVariantConfig config, IEnumerable<MoonPalaceRunVariantPlacement> placements)
        {
            var open = new bool[config.TileWidth * config.TileHeight];
            foreach (var placement in placements)
            for (var localY = 0; localY < config.PatternHeight; localY++)
            for (var localX = 0; localX < config.PatternWidth; localX++)
            {
                var x = (placement.Coordinate.X * config.PatternWidth) + localX;
                var y = (placement.Coordinate.Y * config.PatternHeight) + localY;
                open[(y * config.TileWidth) + x] = placement.Candidate.IsOpen(localX, localY);
            }
            return open;
        }

        private static MoonPalaceMicroPatternCandidate SelectRouteCandidate(IEnumerable<MoonPalaceMicroPatternCandidate> candidates, int seed, int slotIndex,
            IReadOnlyList<RunSocketRequirement> requirements, IDictionary<string, int> rejections, out int attempts)
        {
            attempts = 0;
            foreach (var candidate in OrderedCandidates(candidates, seed, slotIndex))
            {
                attempts++;
                if (!candidate.ChunkPathAllowed) { rejections["not_connected_open_candidate"]++; continue; }
                if (!candidate.IsOpen(1, 1)) { rejections["route_anchor_solid"]++; continue; }
                if (requirements.Any(requirement => !HasSocket(candidate, requirement.Direction))) { rejections["missing_required_socket"]++; continue; }
                return candidate;
            }
            throw new InvalidOperationException("RUN02 rejected the recorded graph because no filtered 4x4 candidate satisfied its required sockets.");
        }

        private static MoonPalaceMicroPatternCandidate SelectFillerCandidate(IEnumerable<MoonPalaceMicroPatternCandidate> candidates, int seed, int slotIndex, out int attempts)
        {
            var candidate = OrderedCandidates(candidates, seed, slotIndex).FirstOrDefault(); attempts = 1;
            if (candidate == null) throw new InvalidOperationException("RUN02 candidate pool is empty.");
            return candidate;
        }

        private static IEnumerable<MoonPalaceMicroPatternCandidate> OrderedCandidates(IEnumerable<MoonPalaceMicroPatternCandidate> candidates, int seed, int slotIndex) =>
            candidates.OrderBy(candidate => Stable(seed, slotIndex, candidate.Mask)).ThenBy(candidate => candidate.Mask);

        private static int Stable(int seed, int slotIndex, ushort mask)
        {
            unchecked { var value = seed; value = (value * 397) ^ slotIndex; value = (value * 397) ^ mask; value ^= value >> 16; return value & int.MaxValue; }
        }

        private static IEnumerable<MoonPalaceRunTileCoordinate> EdgeTiles(IEnumerable<RunVariantGraphEdge> edges, int patternWidth, int patternHeight)
        {
            return edges.SelectMany(edge => EdgeTiles(edge.From, edge.To, patternWidth, patternHeight)).Distinct().OrderBy(tile => tile.Y).ThenBy(tile => tile.X).ToList();
        }
        private static IEnumerable<MoonPalaceRunTileCoordinate> EdgeTiles(PatternSlotCoordinate from, PatternSlotCoordinate to, int patternWidth, int patternHeight)
        {
            var start = CenterTile(from, patternWidth, patternHeight); var end = CenterTile(to, patternWidth, patternHeight);
            var x = start.X; var y = start.Y; var xStep = Math.Sign(end.X - start.X); var yStep = Math.Sign(end.Y - start.Y);
            while (true) { yield return new MoonPalaceRunTileCoordinate(x, y); if (x == end.X && y == end.Y) yield break; x += xStep; y += yStep; }
        }
        private static MoonPalaceRunTileCoordinate CenterTile(PatternSlotCoordinate coordinate, int patternWidth, int patternHeight) => new MoonPalaceRunTileCoordinate(coordinate.X * patternWidth + 1, coordinate.Y * patternHeight + 1);
        private static string ReasonFor(bool main, bool branch, bool split) => split ? "SPLIT_REJOIN_ROUTE_SOCKET_FILTERED" : branch ? "BRANCH_ROUTE_SOCKET_FILTERED" : main ? "MAIN_ROUTE_SOCKET_FILTERED" : "ROUTE_SOCKET_FILTERED";
        private static string CanonicalPlacement(MoonPalaceRunVariantPlacement placement) => "PLACEMENT|" + placement.SlotIndex.ToString(CultureInfo.InvariantCulture) + "|" + placement.Coordinate + "|" +
            placement.Candidate.CandidateId + "|" + placement.Candidate.MaskU16Hex + "|" + placement.Candidate.SocketSignature + "|" + placement.Transform + "|" + placement.SelectionReason + "|" + placement.AttemptCount.ToString(CultureInfo.InvariantCulture);
    }
}
