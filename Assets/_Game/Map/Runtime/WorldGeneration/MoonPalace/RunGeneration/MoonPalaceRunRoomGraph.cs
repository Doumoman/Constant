using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public abstract class RunVariantGraphNode
    {
        protected RunVariantGraphNode(PatternSlotCoordinate coordinate, string kind, bool isMainPath, int branchIndex, string regionId)
        {
            Coordinate = coordinate; Kind = kind; IsMainPath = isMainPath; BranchIndex = branchIndex; RegionId = regionId ?? string.Empty;
        }
        public PatternSlotCoordinate Coordinate { get; }
        public string Kind { get; }
        public bool IsMainPath { get; }
        public int BranchIndex { get; }
        public string RegionId { get; }
    }

    public sealed class RunMainPathNode : RunVariantGraphNode
    {
        public RunMainPathNode(PatternSlotCoordinate coordinate, string regionId) : base(coordinate, "MAIN", true, 0, regionId) { }
    }
    public sealed class RunBranchNode : RunVariantGraphNode
    {
        public RunBranchNode(PatternSlotCoordinate coordinate, int branchIndex, string regionId) : base(coordinate, "BRANCH", false, branchIndex, regionId) { }
    }
    public sealed class RunSplitNode : RunVariantGraphNode
    {
        public RunSplitNode(PatternSlotCoordinate coordinate, int splitIndex, string regionId) : base(coordinate, "SPLIT", true, splitIndex, regionId) { }
    }
    public sealed class RunRejoinNode : RunVariantGraphNode
    {
        public RunRejoinNode(PatternSlotCoordinate coordinate, int splitIndex, string regionId) : base(coordinate, "REJOIN", true, splitIndex, regionId) { }
    }

    public sealed class RunRoomRegion
    {
        public RunRoomRegion(string regionId, int minX, int maxX, int minY, int maxY, string pacing, int intendedDensity)
        {
            RegionId = regionId ?? string.Empty; MinX = minX; MaxX = maxX; MinY = minY; MaxY = maxY; Pacing = pacing ?? string.Empty; IntendedDensity = intendedDensity;
        }
        public string RegionId { get; }
        public int MinX { get; }
        public int MaxX { get; }
        public int MinY { get; }
        public int MaxY { get; }
        public string Pacing { get; }
        public int IntendedDensity { get; }
        public bool Contains(PatternSlotCoordinate coordinate) => coordinate.X >= MinX && coordinate.X <= MaxX && coordinate.Y >= MinY && coordinate.Y <= MaxY;
    }

    public sealed class RunVariantGraphEdge
    {
        public RunVariantGraphEdge(PatternSlotCoordinate from, PatternSlotCoordinate to, string kind, bool isMainPath, int ownerIndex)
        {
            From = from; To = to; Kind = kind ?? string.Empty; IsMainPath = isMainPath; OwnerIndex = ownerIndex;
            Direction = RunEdge.DirectionFrom(from, to);
        }
        public PatternSlotCoordinate From { get; }
        public PatternSlotCoordinate To { get; }
        public MoonPalaceRunDirection Direction { get; }
        public string Kind { get; }
        public bool IsMainPath { get; }
        public int OwnerIndex { get; }
    }

    public sealed class RunVerticalLink
    {
        public RunVerticalLink(PatternSlotCoordinate from, PatternSlotCoordinate to, string purpose)
        {
            From = from; To = to; Purpose = purpose ?? string.Empty;
        }
        public PatternSlotCoordinate From { get; }
        public PatternSlotCoordinate To { get; }
        public string Purpose { get; }
        public int SpanRows => Math.Abs(To.Y - From.Y);
    }

    public sealed class RunSocketRequirement
    {
        public RunSocketRequirement(PatternSlotCoordinate coordinate, MoonPalaceRunDirection direction, int edgeBit)
        {
            Coordinate = coordinate; Direction = direction; EdgeBit = edgeBit;
        }
        public PatternSlotCoordinate Coordinate { get; }
        public MoonPalaceRunDirection Direction { get; }
        public int EdgeBit { get; }
    }

    public sealed class RunSplitRejoinPath
    {
        public RunSplitRejoinPath(int index, PatternSlotCoordinate split, PatternSlotCoordinate rejoin, IEnumerable<PatternSlotCoordinate> routeNodes)
        {
            Index = index; Split = split; Rejoin = rejoin;
            RouteNodes = new ReadOnlyCollection<PatternSlotCoordinate>((routeNodes ?? Array.Empty<PatternSlotCoordinate>()).ToList());
        }
        public int Index { get; }
        public PatternSlotCoordinate Split { get; }
        public PatternSlotCoordinate Rejoin { get; }
        public IReadOnlyList<PatternSlotCoordinate> RouteNodes { get; }
    }

    public sealed class RunVariantDigest
    {
        public RunVariantDigest(string value) { Value = value ?? string.Empty; }
        public string Value { get; }
    }

    /// <summary>Deterministic room/branch graph over direct 4x4 MicroPattern slots for all three RUN02 variants.</summary>
    public sealed class RunVariantGraph
    {
        internal RunVariantGraph(MoonPalaceRunVariantConfig config, IEnumerable<RunVariantGraphNode> nodes,
            IEnumerable<RunVariantGraphEdge> edges, IEnumerable<PatternSlotCoordinate> mainPathNodes,
            IEnumerable<PatternSlotCoordinate> branchEntrySlots, IEnumerable<RunSplitNode> splitNodes,
            IEnumerable<RunRejoinNode> rejoinNodes, IEnumerable<RunSplitRejoinPath> splitRejoinPaths,
            IEnumerable<RunVerticalLink> verticalLinks, IEnumerable<RunRoomRegion> roomRegions,
            IEnumerable<RunSocketRequirement> socketRequirements, RunStart start, RunExit exit, RunVariantDigest digest)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Nodes = ReadOnly(nodes); Edges = ReadOnly(edges); MainPathNodes = ReadOnly(mainPathNodes);
            BranchEntrySlots = ReadOnly(branchEntrySlots); SplitNodes = ReadOnly(splitNodes); RejoinNodes = ReadOnly(rejoinNodes);
            SplitRejoinPaths = ReadOnly(splitRejoinPaths); VerticalLinks = ReadOnly(verticalLinks); RoomRegions = ReadOnly(roomRegions);
            SocketRequirements = ReadOnly(socketRequirements); Start = start ?? throw new ArgumentNullException(nameof(start));
            Exit = exit ?? throw new ArgumentNullException(nameof(exit)); Digest = digest ?? throw new ArgumentNullException(nameof(digest));
        }

        public MoonPalaceRunVariantConfig Config { get; }
        public IReadOnlyList<RunVariantGraphNode> Nodes { get; }
        public IReadOnlyList<RunVariantGraphEdge> Edges { get; }
        public IReadOnlyList<PatternSlotCoordinate> MainPathNodes { get; }
        public IReadOnlyList<PatternSlotCoordinate> BranchEntrySlots { get; }
        public IReadOnlyList<RunSplitNode> SplitNodes { get; }
        public IReadOnlyList<RunRejoinNode> RejoinNodes { get; }
        public IReadOnlyList<RunSplitRejoinPath> SplitRejoinPaths { get; }
        public IReadOnlyList<RunVerticalLink> VerticalLinks { get; }
        public IReadOnlyList<RunRoomRegion> RoomRegions { get; }
        public IReadOnlyList<RunSocketRequirement> SocketRequirements { get; }
        public RunStart Start { get; }
        public RunExit Exit { get; }
        public RunVariantDigest Digest { get; }
        public int BranchCount => BranchEntrySlots.Count;
        public int SplitRejoinCount => SplitRejoinPaths.Count;
        public int VerticalSpanRows => VerticalLinks.Count == 0 ? 0 : VerticalLinks.Max(link => link.SpanRows);
        public IReadOnlyList<PatternSlotCoordinate> RequiredWaypointSlots => ReadOnly(Nodes.Select(node => node.Coordinate).Distinct().OrderBy(c => c.Y).ThenBy(c => c.X));
        public IReadOnlyList<PatternSlotCoordinate> SplitRejoinWaypointSlots => ReadOnly(SplitRejoinPaths.SelectMany(path => new[] { path.Split }.Concat(path.RouteNodes).Concat(new[] { path.Rejoin })).Distinct().OrderBy(c => c.Y).ThenBy(c => c.X));

        public bool Contains(PatternSlotCoordinate coordinate) => coordinate.X >= 0 && coordinate.X < Config.PatternGridWidth && coordinate.Y >= 0 && coordinate.Y < Config.PatternGridHeight;

        public static RunVariantGraph Create(MoonPalaceRunVariantConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            var draft = new Draft(config);
            if (config.Topology == MoonPalaceRunVariantTopology.BranchingLowland) BuildLowland(draft);
            else if (config.Topology == MoonPalaceRunVariantTopology.VerticalLoop) BuildVerticalLoop(draft);
            else if (config.Topology == MoonPalaceRunVariantTopology.MultiRoomSplit) BuildMultiRoomSplit(draft);
            else throw new InvalidOperationException("RUN02 has no direct MicroPattern graph recipe for this topology.");
            return draft.Finish();
        }

        private static void BuildLowland(Draft draft)
        {
            draft.AddRoom(new RunRoomRegion("LOWLAND_WEST", 0, 15, 2, 11, "LOW", 3));
            draft.AddRoom(new RunRoomRegion("LOWLAND_MIDDLE", 16, 31, 2, 11, "LOW", 4));
            draft.AddRoom(new RunRoomRegion("LOWLAND_EAST", 32, 47, 2, 11, "LOW", 3));
            draft.AddMainPath(Horizontal(0, 47, 6));
            var columns = new[] { 4, 9, 14, 20, 27, 33, 39, 44 };
            for (var index = 0; index < columns.Length; index++)
            {
                var attachment = new PatternSlotCoordinate(columns[index], 6);
                var direction = index % 2 == 0 ? 1 : -1;
                draft.AddBranch(index + 1, attachment, new[]
                {
                    new PatternSlotCoordinate(columns[index], 6 + direction), new PatternSlotCoordinate(columns[index], 6 + (direction * 2)),
                });
            }
            draft.SetStartExit(new PatternSlotCoordinate(0, 6), new PatternSlotCoordinate(47, 6));
        }

        private static void BuildVerticalLoop(Draft draft)
        {
            draft.AddRoom(new RunRoomRegion("ASCENT_WEST", 0, 14, 0, 17, "VERTICAL", 6));
            draft.AddRoom(new RunRoomRegion("VERTICAL_MIDDLE", 15, 29, 0, 17, "VERTICAL", 8));
            draft.AddRoom(new RunRoomRegion("DESCENT_EAST", 30, 41, 0, 17, "VERTICAL", 6));
            draft.AddMainPath(JoinPaths(
                Horizontal(0, 7, 4), Vertical(7, 4, 12), Horizontal(7, 15, 12), Vertical(15, 12, 4),
                Horizontal(15, 24, 4), Vertical(24, 4, 12), Horizontal(24, 33, 12), Vertical(33, 12, 4), Horizontal(33, 41, 4)));
            draft.AddVerticalLink(new PatternSlotCoordinate(7, 4), new PatternSlotCoordinate(7, 12), "MAIN_ASCENT");
            draft.AddVerticalLink(new PatternSlotCoordinate(15, 12), new PatternSlotCoordinate(15, 4), "MAIN_DROP");
            draft.AddVerticalLink(new PatternSlotCoordinate(24, 4), new PatternSlotCoordinate(24, 12), "MAIN_ASCENT");
            draft.AddVerticalLink(new PatternSlotCoordinate(33, 12), new PatternSlotCoordinate(33, 4), "MAIN_DROP");
            draft.AddSplitRejoin(1, new PatternSlotCoordinate(9, 12), new PatternSlotCoordinate(12, 12), new[]
            {
                new PatternSlotCoordinate(9, 13), new PatternSlotCoordinate(9, 14), new PatternSlotCoordinate(10, 14),
                new PatternSlotCoordinate(11, 14), new PatternSlotCoordinate(12, 14), new PatternSlotCoordinate(12, 13),
            });
            draft.AddSplitRejoin(2, new PatternSlotCoordinate(35, 4), new PatternSlotCoordinate(38, 4), new[]
            {
                new PatternSlotCoordinate(35, 3), new PatternSlotCoordinate(35, 2), new PatternSlotCoordinate(36, 2),
                new PatternSlotCoordinate(37, 2), new PatternSlotCoordinate(38, 2), new PatternSlotCoordinate(38, 3),
            });
            var branches = new[]
            {
                new { X = 3, Y = 4, Delta = 1 }, new { X = 5, Y = 4, Delta = -1 }, new { X = 10, Y = 12, Delta = -1 },
                new { X = 14, Y = 12, Delta = 1 }, new { X = 18, Y = 4, Delta = 1 }, new { X = 22, Y = 4, Delta = -1 },
                new { X = 27, Y = 12, Delta = 1 }, new { X = 31, Y = 12, Delta = -1 }, new { X = 37, Y = 4, Delta = 1 },
            };
            for (var index = 0; index < branches.Length; index++)
            {
                var branch = branches[index]; var attachment = new PatternSlotCoordinate(branch.X, branch.Y);
                draft.AddBranch(index + 1, attachment, new[] { new PatternSlotCoordinate(branch.X, branch.Y + branch.Delta), new PatternSlotCoordinate(branch.X, branch.Y + (branch.Delta * 2)) });
            }
            draft.SetStartExit(new PatternSlotCoordinate(0, 4), new PatternSlotCoordinate(41, 4));
        }

        private static void BuildMultiRoomSplit(Draft draft)
        {
            draft.AddRoom(new RunRoomRegion("ENTRY_GALLERY", 0, 17, 0, 15, "LOW_DENSITY_ENTRY", 4));
            draft.AddRoom(new RunRoomRegion("SPLIT_SANCTUM", 18, 39, 0, 15, "CHOICE_DENSE", 9));
            draft.AddRoom(new RunRoomRegion("EXIT_CHAMBERS", 40, 59, 0, 15, "MULTI_ROOM_EXIT", 7));
            draft.AddMainPath(JoinPaths(
                Horizontal(0, 18, 7), Vertical(18, 7, 10), Horizontal(18, 30, 10), Vertical(30, 10, 7),
                Horizontal(30, 37, 7), Vertical(37, 7, 3), Horizontal(37, 49, 3), Vertical(49, 3, 7), Horizontal(49, 59, 7)));
            draft.AddSplitRejoin(1, new PatternSlotCoordinate(18, 7), new PatternSlotCoordinate(30, 7), new[]
            {
                new PatternSlotCoordinate(18, 6), new PatternSlotCoordinate(18, 5), new PatternSlotCoordinate(18, 4),
                new PatternSlotCoordinate(19, 4), new PatternSlotCoordinate(20, 4), new PatternSlotCoordinate(21, 4),
                new PatternSlotCoordinate(22, 4), new PatternSlotCoordinate(23, 4), new PatternSlotCoordinate(24, 4),
                new PatternSlotCoordinate(25, 4), new PatternSlotCoordinate(26, 4), new PatternSlotCoordinate(27, 4),
                new PatternSlotCoordinate(28, 4), new PatternSlotCoordinate(29, 4), new PatternSlotCoordinate(30, 4),
                new PatternSlotCoordinate(30, 5), new PatternSlotCoordinate(30, 6),
            });
            draft.AddSplitRejoin(2, new PatternSlotCoordinate(37, 7), new PatternSlotCoordinate(49, 7), new[]
            {
                new PatternSlotCoordinate(37, 8), new PatternSlotCoordinate(37, 9), new PatternSlotCoordinate(37, 10), new PatternSlotCoordinate(37, 11),
                new PatternSlotCoordinate(38, 11), new PatternSlotCoordinate(39, 11), new PatternSlotCoordinate(40, 11), new PatternSlotCoordinate(41, 11),
                new PatternSlotCoordinate(42, 11), new PatternSlotCoordinate(43, 11), new PatternSlotCoordinate(44, 11), new PatternSlotCoordinate(45, 11),
                new PatternSlotCoordinate(46, 11), new PatternSlotCoordinate(47, 11), new PatternSlotCoordinate(48, 11), new PatternSlotCoordinate(49, 11),
                new PatternSlotCoordinate(49, 10), new PatternSlotCoordinate(49, 9), new PatternSlotCoordinate(49, 8),
            });
            var branches = new[]
            {
                new { X = 3, Y = 7, Delta = 1 }, new { X = 6, Y = 7, Delta = -1 }, new { X = 10, Y = 7, Delta = 1 },
                new { X = 14, Y = 7, Delta = -1 }, new { X = 17, Y = 7, Delta = 1 }, new { X = 20, Y = 10, Delta = 1 },
                new { X = 24, Y = 10, Delta = -1 }, new { X = 28, Y = 10, Delta = 1 }, new { X = 33, Y = 7, Delta = 1 },
                new { X = 40, Y = 3, Delta = -1 }, new { X = 44, Y = 3, Delta = -1 }, new { X = 53, Y = 7, Delta = 1 },
            };
            for (var index = 0; index < branches.Length; index++)
            {
                var branch = branches[index]; var attachment = new PatternSlotCoordinate(branch.X, branch.Y);
                draft.AddBranch(index + 1, attachment, new[] { new PatternSlotCoordinate(branch.X, branch.Y + branch.Delta), new PatternSlotCoordinate(branch.X, branch.Y + (branch.Delta * 2)) });
            }
            draft.SetStartExit(new PatternSlotCoordinate(0, 7), new PatternSlotCoordinate(59, 7));
        }

        private static IEnumerable<PatternSlotCoordinate> Horizontal(int fromX, int toX, int y)
        {
            var step = fromX <= toX ? 1 : -1;
            for (var x = fromX; ; x += step) { yield return new PatternSlotCoordinate(x, y); if (x == toX) yield break; }
        }

        private static IEnumerable<PatternSlotCoordinate> Vertical(int x, int fromY, int toY)
        {
            var step = fromY <= toY ? 1 : -1;
            for (var y = fromY; ; y += step) { yield return new PatternSlotCoordinate(x, y); if (y == toY) yield break; }
        }

        private static IEnumerable<PatternSlotCoordinate> JoinPaths(params IEnumerable<PatternSlotCoordinate>[] paths)
        {
            var result = new List<PatternSlotCoordinate>();
            foreach (var path in paths)
            foreach (var coordinate in path)
                if (result.Count == 0 || !result[result.Count - 1].Equals(coordinate)) result.Add(coordinate);
            return result;
        }

        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());

        private sealed class Draft
        {
            private readonly Dictionary<PatternSlotCoordinate, RunVariantGraphNode> nodes = new Dictionary<PatternSlotCoordinate, RunVariantGraphNode>();
            private readonly List<RunVariantGraphEdge> edges = new List<RunVariantGraphEdge>();
            private readonly HashSet<string> edgeTokens = new HashSet<string>(StringComparer.Ordinal);
            private readonly List<PatternSlotCoordinate> mainPath = new List<PatternSlotCoordinate>();
            private readonly List<PatternSlotCoordinate> branchEntries = new List<PatternSlotCoordinate>();
            private readonly List<RunSplitNode> splits = new List<RunSplitNode>();
            private readonly List<RunRejoinNode> rejoins = new List<RunRejoinNode>();
            private readonly List<RunSplitRejoinPath> splitPaths = new List<RunSplitRejoinPath>();
            private readonly List<RunVerticalLink> verticalLinks = new List<RunVerticalLink>();
            private readonly List<RunRoomRegion> rooms = new List<RunRoomRegion>();
            private RunStart start;
            private RunExit exit;

            public Draft(MoonPalaceRunVariantConfig config) { Config = config; }
            public MoonPalaceRunVariantConfig Config { get; }

            public void AddRoom(RunRoomRegion room)
            {
                if (room == null || room.MinX < 0 || room.MaxX >= Config.PatternGridWidth || room.MinY < 0 || room.MaxY >= Config.PatternGridHeight || room.MinX > room.MaxX || room.MinY > room.MaxY)
                    throw new InvalidOperationException("RUN02 room region escaped its pattern grid.");
                if (rooms.Any(existing => string.Equals(existing.RegionId, room.RegionId, StringComparison.Ordinal)))
                    throw new InvalidOperationException("RUN02 rejects duplicate room-region identifiers.");
                rooms.Add(room);
            }

            public void AddMainPath(IEnumerable<PatternSlotCoordinate> path)
            {
                var values = (path ?? Array.Empty<PatternSlotCoordinate>()).ToList();
                if (values.Count < 2) throw new InvalidOperationException("RUN02 main path requires at least two pattern slots.");
                foreach (var coordinate in values) AddNode(new RunMainPathNode(coordinate, RegionFor(coordinate)), false);
                for (var index = 1; index < values.Count; index++) AddEdge(values[index - 1], values[index], "MAIN", true, 0);
                mainPath.AddRange(values);
            }

            public void AddBranch(int branchIndex, PatternSlotCoordinate attachment, IEnumerable<PatternSlotCoordinate> branchNodes)
            {
                var values = (branchNodes ?? Array.Empty<PatternSlotCoordinate>()).ToList();
                if (branchIndex < 1 || values.Count == 0 || !nodes.ContainsKey(attachment))
                    throw new InvalidOperationException("RUN02 branch requires a reachable main-path attachment and at least one slot.");
                var prior = attachment;
                foreach (var coordinate in values)
                {
                    AddNode(new RunBranchNode(coordinate, branchIndex, RegionFor(coordinate)), false);
                    AddEdge(prior, coordinate, "BRANCH", false, branchIndex); prior = coordinate;
                }
                branchEntries.Add(values[0]);
            }

            public void AddSplitRejoin(int index, PatternSlotCoordinate split, PatternSlotCoordinate rejoin, IEnumerable<PatternSlotCoordinate> alternateNodes)
            {
                var values = (alternateNodes ?? Array.Empty<PatternSlotCoordinate>()).ToList();
                if (index < 1 || values.Count < 2 || !nodes.ContainsKey(split) || !nodes.ContainsKey(rejoin))
                    throw new InvalidOperationException("RUN02 split-rejoin requires existing main-path endpoints and a recorded alternate route.");
                var splitNode = new RunSplitNode(split, index, RegionFor(split)); var rejoinNode = new RunRejoinNode(rejoin, index, RegionFor(rejoin));
                AddNode(splitNode, true); AddNode(rejoinNode, true); splits.Add(splitNode); rejoins.Add(rejoinNode);
                var prior = split;
                foreach (var coordinate in values)
                {
                    AddNode(new RunBranchNode(coordinate, -index, RegionFor(coordinate)), false);
                    AddEdge(prior, coordinate, "SPLIT_REJOIN", false, index); prior = coordinate;
                }
                AddEdge(prior, rejoin, "SPLIT_REJOIN", false, index);
                splitPaths.Add(new RunSplitRejoinPath(index, split, rejoin, values));
            }

            public void AddVerticalLink(PatternSlotCoordinate from, PatternSlotCoordinate to, string purpose)
            {
                if (from.X != to.X || Math.Abs(from.Y - to.Y) < 1) throw new InvalidOperationException("RUN02 vertical link must span cardinal pattern slots.");
                verticalLinks.Add(new RunVerticalLink(from, to, purpose));
            }

            public void SetStartExit(PatternSlotCoordinate startSlot, PatternSlotCoordinate exitSlot)
            {
                if (!nodes.ContainsKey(startSlot) || !nodes.ContainsKey(exitSlot) || startSlot.X == exitSlot.X)
                    throw new InvalidOperationException("RUN02 start and exit must be graph nodes on different horizontal sides.");
                start = new RunStart(startSlot, 0, 1); exit = new RunExit(exitSlot, Config.PatternWidth - 1, 1);
            }

            public RunVariantGraph Finish()
            {
                if (start == null || exit == null || mainPath.Count < Config.MainPathMinSteps || mainPath.Count > Config.MainPathMaxSteps)
                    throw new InvalidOperationException("RUN02 graph did not meet its main-path contract.");
                if (branchEntries.Count < Config.BranchMinCount || branchEntries.Count > Config.BranchMaxCount || branchEntries.Distinct().Count() != branchEntries.Count)
                    throw new InvalidOperationException("RUN02 graph did not meet its branch-count contract.");
                if (splitPaths.Count < Config.SplitRejoinMinCount || splitPaths.Count > Config.SplitRejoinMaxCount)
                    throw new InvalidOperationException("RUN02 graph did not meet its split-rejoin contract.");
                if (Config.Topology == MoonPalaceRunVariantTopology.VerticalLoop && VerticalSpan < 8)
                    throw new InvalidOperationException("RUN02 vertical-loop graph did not span eight pattern rows.");
                if (Config.Topology == MoonPalaceRunVariantTopology.MultiRoomSplit && rooms.Count < 3)
                    throw new InvalidOperationException("RUN02 multi-room graph did not record three room regions.");
                if (nodes.Keys.Any(coordinate => !Contains(coordinate)) || edges.Any(edge => !Contains(edge.From) || !Contains(edge.To)))
                    throw new InvalidOperationException("RUN02 graph node or edge escaped its pattern grid.");
                if (nodes.Count != nodes.Keys.Distinct().Count()) throw new InvalidOperationException("RUN02 rejects accidental duplicate graph nodes.");
                if (!Reachable(start.Slot, exit.Slot) || branchEntries.Any(entry => !Reachable(start.Slot, entry)) || splitPaths.Any(path => !Reachable(path.Split, path.Rejoin)))
                    throw new InvalidOperationException("RUN02 graph has an unreachable branch, split, rejoin, or exit.");
                var socketRequirements = BuildSocketRequirements();
                var lines = new List<string> { "RUN02_GRAPH", Config.CanonicalDigest, "start=" + start.Slot, "exit=" + exit.Slot };
                lines.AddRange(rooms.OrderBy(room => room.RegionId, StringComparer.Ordinal).Select(room => "ROOM|" + room.RegionId + "|" + room.MinX + ":" + room.MaxX + "|" + room.MinY + ":" + room.MaxY + "|" + room.Pacing + "|" + room.IntendedDensity));
                lines.AddRange(nodes.Values.OrderBy(node => node.Coordinate.Y).ThenBy(node => node.Coordinate.X).Select(node => "NODE|" + node.Kind + "|" + node.Coordinate + "|" + node.BranchIndex + "|" + node.RegionId));
                lines.AddRange(edges.OrderBy(edge => edge.Kind, StringComparer.Ordinal).ThenBy(edge => edge.OwnerIndex).ThenBy(edge => edge.From.Y).ThenBy(edge => edge.From.X).Select(edge => "EDGE|" + edge.Kind + "|" + edge.OwnerIndex + "|" + edge.From + "|" + edge.To + "|" + edge.Direction));
                lines.AddRange(splitPaths.Select(path => "SPLIT|" + path.Index + "|" + path.Split + "|" + path.Rejoin + "|" + string.Join(";", path.RouteNodes.Select(node => node.ToString()))));
                lines.AddRange(verticalLinks.Select(link => "VERTICAL|" + link.From + "|" + link.To + "|" + link.Purpose));
                return new RunVariantGraph(Config, nodes.Values.OrderBy(node => node.Coordinate.Y).ThenBy(node => node.Coordinate.X), edges,
                    mainPath.Distinct(), branchEntries, splits, rejoins, splitPaths, verticalLinks, rooms, socketRequirements, start, exit,
                    new RunVariantDigest(BakingCanonicalDigest.HashCanonicalLines(lines)));
            }

            private int VerticalSpan => verticalLinks.Count == 0 ? 0 : verticalLinks.Max(link => link.SpanRows);
            private bool Contains(PatternSlotCoordinate coordinate) => coordinate.X >= 0 && coordinate.X < Config.PatternGridWidth && coordinate.Y >= 0 && coordinate.Y < Config.PatternGridHeight;
            private string RegionFor(PatternSlotCoordinate coordinate) => rooms.FirstOrDefault(room => room.Contains(coordinate))?.RegionId ?? "UNASSIGNED";
            private void AddNode(RunVariantGraphNode node, bool replace)
            {
                if (!Contains(node.Coordinate)) throw new InvalidOperationException("RUN02 graph node escaped its pattern grid.");
                if (nodes.ContainsKey(node.Coordinate) && !replace)
                    throw new InvalidOperationException("RUN02 rejects an accidental duplicate graph node at " + node.Coordinate + ".");
                nodes[node.Coordinate] = node;
            }
            private void AddEdge(PatternSlotCoordinate from, PatternSlotCoordinate to, string kind, bool isMain, int owner)
            {
                var edge = new RunVariantGraphEdge(from, to, kind, isMain, owner);
                var token = from + ">" + to;
                if (!edgeTokens.Add(token)) throw new InvalidOperationException("RUN02 rejects duplicate graph edges.");
                edges.Add(edge);
            }
            private bool Reachable(PatternSlotCoordinate from, PatternSlotCoordinate target)
            {
                var visited = new HashSet<PatternSlotCoordinate> { from }; var queue = new Queue<PatternSlotCoordinate>(); queue.Enqueue(from);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue(); if (current.Equals(target)) return true;
                    foreach (var edge in edges.Where(edge => edge.From.Equals(current)).Concat(edges.Where(edge => edge.To.Equals(current))))
                    {
                        var next = edge.From.Equals(current) ? edge.To : edge.From;
                        if (visited.Add(next)) queue.Enqueue(next);
                    }
                }
                return false;
            }
            private IReadOnlyList<RunSocketRequirement> BuildSocketRequirements()
            {
                var requirements = new List<RunSocketRequirement>();
                foreach (var edge in edges)
                {
                    requirements.Add(new RunSocketRequirement(edge.From, edge.Direction, 1));
                    requirements.Add(new RunSocketRequirement(edge.To, Opposite(edge.Direction), 1));
                }
                requirements.Add(new RunSocketRequirement(start.Slot, MoonPalaceRunDirection.West, 1));
                requirements.Add(new RunSocketRequirement(exit.Slot, MoonPalaceRunDirection.East, 1));
                return ReadOnly(requirements.GroupBy(requirement => requirement.Coordinate + ":" + requirement.Direction + ":" + requirement.EdgeBit, StringComparer.Ordinal)
                    .Select(group => group.First()).OrderBy(requirement => requirement.Coordinate.Y).ThenBy(requirement => requirement.Coordinate.X).ThenBy(requirement => requirement.Direction));
            }
            private static MoonPalaceRunDirection Opposite(MoonPalaceRunDirection direction) => direction == MoonPalaceRunDirection.North ? MoonPalaceRunDirection.South : direction == MoonPalaceRunDirection.South ? MoonPalaceRunDirection.North : direction == MoonPalaceRunDirection.East ? MoonPalaceRunDirection.West : MoonPalaceRunDirection.East;
        }
    }
}
