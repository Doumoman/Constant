using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public struct MoonPalaceRunTileCoordinate : IEquatable<MoonPalaceRunTileCoordinate>
    {
        public MoonPalaceRunTileCoordinate(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public bool Equals(MoonPalaceRunTileCoordinate other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is MoonPalaceRunTileCoordinate other && Equals(other);
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + ":" + Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class MoonPalaceMicroPatternRunPlacement
    {
        internal MoonPalaceMicroPatternRunPlacement(int slotIndex, PatternSlotCoordinate coordinate,
            MoonPalaceMicroPatternCandidate candidate, IEnumerable<RunSocket> requiredSockets, string transform,
            string selectionReason, string presentationFamilyLink, int attemptCount, bool isMainPath, int branchIndex)
        {
            SlotIndex = slotIndex; Coordinate = coordinate; Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            RequiredSockets = ReadOnly(requiredSockets); Transform = transform ?? string.Empty; SelectionReason = selectionReason ?? string.Empty;
            PresentationFamilyLink = presentationFamilyLink ?? string.Empty; AttemptCount = attemptCount; IsMainPath = isMainPath; BranchIndex = branchIndex;
        }
        public int SlotIndex { get; }
        public PatternSlotCoordinate Coordinate { get; }
        public MoonPalaceMicroPatternCandidate Candidate { get; }
        public IReadOnlyList<RunSocket> RequiredSockets { get; }
        public string Transform { get; }
        public string SelectionReason { get; }
        public string PresentationFamilyLink { get; }
        public int AttemptCount { get; }
        public bool IsMainPath { get; }
        public int BranchIndex { get; }
        public bool IsBranch => !IsMainPath && BranchIndex > 0;

        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values)
        {
            return new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());
        }
    }

    public sealed class MoonPalaceMicroPatternRunComposition
    {
        internal MoonPalaceMicroPatternRunComposition(MoonPalaceMicroPatternRunConfig config, MoonPalaceMicroPatternRunGraph graph,
            MoonPalaceMicroPatternCandidateSet candidateSet, IEnumerable<MoonPalaceMicroPatternRunPlacement> placements,
            bool[] openCells, IEnumerable<MoonPalaceRunTileCoordinate> mainRouteTiles,
            IEnumerable<MoonPalaceRunTileCoordinate> branchRouteTiles, IEnumerable<MoonPalaceRunTileCoordinate> requiredWaypoints,
            IEnumerable<MoonPalaceRunTileCoordinate> branchEntryTiles, IDictionary<string, int> rejectionCounts,
            int selectionAttemptCount, string compositionDigest, string mapDigest)
        {
            Config = config; Graph = graph; CandidateSet = candidateSet;
            Placements = ReadOnly(placements); OpenCells = new ReadOnlyCollection<bool>((bool[])openCells.Clone());
            MainRouteTiles = ReadOnly(mainRouteTiles); BranchRouteTiles = ReadOnly(branchRouteTiles);
            RequiredWaypoints = ReadOnly(requiredWaypoints); BranchEntryTiles = ReadOnly(branchEntryTiles);
            RejectionCounts = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(rejectionCounts, StringComparer.Ordinal));
            SelectionAttemptCount = selectionAttemptCount; CompositionDigest = compositionDigest; MapDigest = mapDigest;
        }
        public MoonPalaceMicroPatternRunConfig Config { get; }
        public MoonPalaceMicroPatternRunGraph Graph { get; }
        public MoonPalaceMicroPatternCandidateSet CandidateSet { get; }
        public IReadOnlyList<MoonPalaceMicroPatternRunPlacement> Placements { get; }
        public IReadOnlyList<bool> OpenCells { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> MainRouteTiles { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> BranchRouteTiles { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> RequiredWaypoints { get; }
        public IReadOnlyList<MoonPalaceRunTileCoordinate> BranchEntryTiles { get; }
        public IReadOnlyDictionary<string, int> RejectionCounts { get; }
        public int SelectionAttemptCount { get; }
        public int PlacementCount => Placements.Count;
        public int RoutePlacementCount => Placements.Count(placement => placement.IsMainPath || placement.IsBranch);
        public int BranchPlacementCount => Placements.Count(placement => placement.IsBranch);
        public int FillerDetailPlacementCount => PlacementCount - RoutePlacementCount;
        public int FallbackCarveCount => 0;
        public int SilentRepairCount => 0;
        public string CompositionDigest { get; }
        public string MapDigest { get; }

        public bool IsOpen(int x, int y) => x >= 0 && x < Config.TileWidth && y >= 0 && y < Config.TileHeight && OpenCells[(y * Config.TileWidth) + x];
        public MoonPalaceRunTileCoordinate StartTile => new MoonPalaceRunTileCoordinate(Graph.Start.Slot.X * Config.PatternWidth + Graph.Start.LocalX,
            Graph.Start.Slot.Y * Config.PatternHeight + Graph.Start.LocalY);
        public MoonPalaceRunTileCoordinate ExitTile => new MoonPalaceRunTileCoordinate(Graph.Exit.Slot.X * Config.PatternWidth + Graph.Exit.LocalX,
            Graph.Exit.Slot.Y * Config.PatternHeight + Graph.Exit.LocalY);

        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values)
        {
            return new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());
        }
    }

    /// <summary>Places audited VIS01 candidates directly into a 40x12 grid. It has no sector or MicroChunk dependency.</summary>
    public static class MoonPalaceMicroPatternRunComposer
    {
        public const int RouteSocketBit = 1;
        private const string PresentationLink = "MAP21_02_READ_ONLY_PRESENTATION_FAMILY";

        public static MoonPalaceMicroPatternRunComposition Compose(MoonPalaceMicroPatternRunConfig config,
            MoonPalaceMicroPatternRunGraph graph = null, bool reverseCandidateEnumeration = false)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            graph = graph ?? MoonPalaceMicroPatternRunGraph.Create(config);
            var candidateSet = MoonPalaceMicroPatternCandidateLibrary.Build();
            if (candidateSet.Candidates.Count != config.CandidateCount || candidateSet.Candidates.Count != 500)
                throw new InvalidOperationException("RUN01 requires exactly the 500 accepted VIS01 candidates.");
            var candidates = candidateSet.Candidates.ToList();
            if (reverseCandidateEnumeration) candidates.Reverse();
            var routeNodes = graph.Nodes.ToDictionary(node => node.Coordinate, node => node);
            var rejections = new SortedDictionary<string, int>(StringComparer.Ordinal)
            {
                ["missing_required_socket"] = 0,
                ["route_anchor_solid"] = 0,
                ["not_connected_open_candidate"] = 0,
                ["filler_selection"] = 0,
            };
            var placements = new List<MoonPalaceMicroPatternRunPlacement>();
            var totalAttempts = 0;
            for (var y = 0; y < config.PatternGridHeight; y++)
            for (var x = 0; x < config.PatternGridWidth; x++)
            {
                var coordinate = new PatternSlotCoordinate(x, y);
                var slotIndex = (y * config.PatternGridWidth) + x;
                routeNodes.TryGetValue(coordinate, out var routeNode);
                var required = RequiredSockets(graph, coordinate).ToList();
                MoonPalaceMicroPatternCandidate candidate;
                int attempts;
                string reason;
                if (routeNode != null)
                {
                    candidate = SelectRouteCandidate(candidates, config.SeedValue, slotIndex, required, rejections, out attempts);
                    reason = "ROUTE_SOCKET_" + string.Join("_", required.Select(socket => socket.Direction.ToString()).OrderBy(value => value, StringComparer.Ordinal));
                }
                else
                {
                    candidate = SelectFillerCandidate(candidates, config.SeedValue, slotIndex, out attempts);
                    rejections["filler_selection"]++;
                    reason = "FILLER_DETAIL_FROM_DIVERSE_500_POOL";
                }
                totalAttempts += attempts;
                placements.Add(new MoonPalaceMicroPatternRunPlacement(slotIndex, coordinate, candidate, required,
                    "IDENTITY_NO_ROTATION", reason, PresentationLink, attempts, routeNode != null && routeNode.IsMainPath,
                    routeNode == null ? 0 : routeNode.BranchIndex));
            }
            if (placements.Count != config.PatternGridWidth * config.PatternGridHeight)
                throw new InvalidOperationException("RUN01 did not place every pattern grid slot.");
            var placementByCoord = placements.ToDictionary(placement => placement.Coordinate, placement => placement);
            foreach (var edge in graph.Edges)
                RequireReciprocalSockets(placementByCoord[edge.From].Candidate, placementByCoord[edge.To].Candidate, edge.Direction);

            var open = new bool[config.TileWidth * config.TileHeight];
            foreach (var placement in placements)
            for (var localY = 0; localY < config.PatternHeight; localY++)
            for (var localX = 0; localX < config.PatternWidth; localX++)
            {
                var tileX = placement.Coordinate.X * config.PatternWidth + localX;
                var tileY = placement.Coordinate.Y * config.PatternHeight + localY;
                open[(tileY * config.TileWidth) + tileX] = placement.Candidate.IsOpen(localX, localY);
            }
            var mainTiles = new HashSet<MoonPalaceRunTileCoordinate>();
            var branchTiles = new HashSet<MoonPalaceRunTileCoordinate>();
            foreach (var edge in graph.Edges)
            {
                var target = edge.IsMainPath ? mainTiles : branchTiles;
                foreach (var tile in EdgeTiles(edge, config.PatternWidth, config.PatternHeight)) target.Add(tile);
            }
            var waypoints = graph.Nodes.Select(node => CenterTile(node.Coordinate, config.PatternWidth, config.PatternHeight)).OrderBy(tile => tile.Y).ThenBy(tile => tile.X).ToList();
            var branchEntries = graph.Branches.Select(branch => CenterTile(branch.Nodes[0], config.PatternWidth, config.PatternHeight)).ToList();
            var compositionLines = new List<string> { "RUN01_COMPOSITION", config.CanonicalDigest, graph.Digest.Value, candidateSet.CanonicalDigest };
            compositionLines.AddRange(placements.OrderBy(placement => placement.SlotIndex).Select(CanonicalPlacement));
            compositionLines.AddRange(rejections.Select(pair => "R|" + pair.Key + "|" + pair.Value.ToString(CultureInfo.InvariantCulture)));
            var compositionDigest = BakingCanonicalDigest.HashCanonicalLines(compositionLines);
            var mapDigest = BakingCanonicalDigest.HashCanonicalLines(placements.OrderBy(placement => placement.SlotIndex)
                .Select(placement => placement.Coordinate + "|" + placement.Candidate.MaskU16Hex));
            return new MoonPalaceMicroPatternRunComposition(config, graph, candidateSet, placements, open, mainTiles, branchTiles,
                waypoints, branchEntries, rejections, totalAttempts, compositionDigest, mapDigest);
        }

        public static void RequireReciprocalSockets(MoonPalaceMicroPatternCandidate from, MoonPalaceMicroPatternCandidate to,
            MoonPalaceRunDirection direction)
        {
            if (from == null || to == null || !HasSocket(from, direction) || !HasSocket(to, Opposite(direction)))
                throw new InvalidOperationException("RUN01 rejects an incompatible reciprocal MicroPattern socket; no mask carve or repair is attempted.");
        }

        public static bool HasSocket(MoonPalaceMicroPatternCandidate candidate, MoonPalaceRunDirection direction)
        {
            if (candidate == null) return false;
            var bits = direction == MoonPalaceRunDirection.North ? candidate.NorthSocketBits :
                direction == MoonPalaceRunDirection.South ? candidate.SouthSocketBits :
                direction == MoonPalaceRunDirection.West ? candidate.WestSocketBits : candidate.EastSocketBits;
            return (bits & (1 << RouteSocketBit)) != 0;
        }

        public static MoonPalaceRunDirection Opposite(MoonPalaceRunDirection direction)
        {
            return direction == MoonPalaceRunDirection.North ? MoonPalaceRunDirection.South :
                direction == MoonPalaceRunDirection.South ? MoonPalaceRunDirection.North :
                direction == MoonPalaceRunDirection.East ? MoonPalaceRunDirection.West : MoonPalaceRunDirection.East;
        }

        private static IEnumerable<RunSocket> RequiredSockets(MoonPalaceMicroPatternRunGraph graph, PatternSlotCoordinate coordinate)
        {
            var directions = new HashSet<MoonPalaceRunDirection>();
            foreach (var edge in graph.Edges)
            {
                if (edge.From.Equals(coordinate)) directions.Add(edge.Direction);
                if (edge.To.Equals(coordinate)) directions.Add(Opposite(edge.Direction));
            }
            if (coordinate.Equals(graph.Start.Slot)) directions.Add(MoonPalaceRunDirection.West);
            if (coordinate.Equals(graph.Exit.Slot)) directions.Add(MoonPalaceRunDirection.East);
            return directions.OrderBy(direction => direction).Select(direction => new RunSocket(coordinate, direction, RouteSocketBit));
        }

        private static MoonPalaceMicroPatternCandidate SelectRouteCandidate(IEnumerable<MoonPalaceMicroPatternCandidate> candidates,
            int seed, int slotIndex, IReadOnlyList<RunSocket> required, IDictionary<string, int> rejections, out int attempts)
        {
            attempts = 0;
            foreach (var candidate in OrderedCandidates(candidates, seed, slotIndex))
            {
                attempts++;
                if (!candidate.ChunkPathAllowed) { rejections["not_connected_open_candidate"]++; continue; }
                if (!candidate.IsOpen(1, 1)) { rejections["route_anchor_solid"]++; continue; }
                if (required.Any(socket => !HasSocket(candidate, socket.Direction))) { rejections["missing_required_socket"]++; continue; }
                return candidate;
            }
            throw new InvalidOperationException("RUN01 rejected the composition because no candidate satisfied the recorded required sockets.");
        }

        private static MoonPalaceMicroPatternCandidate SelectFillerCandidate(IEnumerable<MoonPalaceMicroPatternCandidate> candidates,
            int seed, int slotIndex, out int attempts)
        {
            var ordered = OrderedCandidates(candidates, seed, slotIndex).ToList();
            attempts = 1;
            if (ordered.Count == 0) throw new InvalidOperationException("RUN01 candidate pool is empty.");
            return ordered[0];
        }

        private static IEnumerable<MoonPalaceMicroPatternCandidate> OrderedCandidates(IEnumerable<MoonPalaceMicroPatternCandidate> candidates,
            int seed, int slotIndex)
        {
            return candidates.OrderBy(candidate => Stable(seed, slotIndex, candidate.Mask)).ThenBy(candidate => candidate.Mask);
        }

        private static int Stable(int seed, int slotIndex, ushort mask)
        {
            unchecked
            {
                var value = seed;
                value = (value * 397) ^ slotIndex;
                value = (value * 397) ^ mask;
                value ^= value >> 16;
                return value & int.MaxValue;
            }
        }

        private static MoonPalaceRunTileCoordinate CenterTile(PatternSlotCoordinate coordinate, int patternWidth, int patternHeight)
        {
            return new MoonPalaceRunTileCoordinate(coordinate.X * patternWidth + 1, coordinate.Y * patternHeight + 1);
        }

        private static IEnumerable<MoonPalaceRunTileCoordinate> EdgeTiles(RunEdge edge, int patternWidth, int patternHeight)
        {
            var from = CenterTile(edge.From, patternWidth, patternHeight);
            var to = CenterTile(edge.To, patternWidth, patternHeight);
            var xStep = Math.Sign(to.X - from.X);
            var yStep = Math.Sign(to.Y - from.Y);
            var x = from.X;
            var y = from.Y;
            while (true)
            {
                yield return new MoonPalaceRunTileCoordinate(x, y);
                if (x == to.X && y == to.Y) break;
                x += xStep; y += yStep;
            }
        }

        private static string CanonicalPlacement(MoonPalaceMicroPatternRunPlacement placement)
        {
            return "P|" + placement.SlotIndex.ToString(CultureInfo.InvariantCulture) + "|" + placement.Coordinate + "|" +
                placement.Candidate.CandidateId + "|" + placement.Candidate.MaskU16Hex + "|" + placement.Candidate.SocketSignature + "|" +
                placement.Transform + "|" + placement.SelectionReason + "|" + placement.PresentationFamilyLink + "|" +
                placement.AttemptCount.ToString(CultureInfo.InvariantCulture);
        }
    }
}
