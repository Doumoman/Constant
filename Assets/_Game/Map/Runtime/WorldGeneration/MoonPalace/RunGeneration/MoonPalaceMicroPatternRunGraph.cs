using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public enum MoonPalaceRunDirection { North = 0, East = 1, South = 2, West = 3 }

    public struct PatternSlotCoordinate : IEquatable<PatternSlotCoordinate>
    {
        public PatternSlotCoordinate(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public bool Equals(PatternSlotCoordinate other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is PatternSlotCoordinate other && Equals(other);
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + ":" + Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class RunSocket
    {
        public RunSocket(PatternSlotCoordinate coordinate, MoonPalaceRunDirection direction, int edgeBit)
        {
            Coordinate = coordinate; Direction = direction; EdgeBit = edgeBit;
        }
        public PatternSlotCoordinate Coordinate { get; }
        public MoonPalaceRunDirection Direction { get; }
        public int EdgeBit { get; }
        public string StableToken => Coordinate + ":" + Direction + ":" + EdgeBit.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class RunNode
    {
        public RunNode(PatternSlotCoordinate coordinate, bool isMainPath, int branchIndex)
        {
            Coordinate = coordinate; IsMainPath = isMainPath; BranchIndex = branchIndex;
        }
        public PatternSlotCoordinate Coordinate { get; }
        public bool IsMainPath { get; }
        public int BranchIndex { get; }
    }

    public sealed class RunEdge
    {
        public RunEdge(PatternSlotCoordinate from, PatternSlotCoordinate to, bool isMainPath, int branchIndex)
        {
            From = from; To = to; IsMainPath = isMainPath; BranchIndex = branchIndex;
            Direction = DirectionFrom(from, to);
        }
        public PatternSlotCoordinate From { get; }
        public PatternSlotCoordinate To { get; }
        public MoonPalaceRunDirection Direction { get; }
        public bool IsMainPath { get; }
        public int BranchIndex { get; }

        public static MoonPalaceRunDirection DirectionFrom(PatternSlotCoordinate from, PatternSlotCoordinate to)
        {
            if (to.X == from.X + 1 && to.Y == from.Y) return MoonPalaceRunDirection.East;
            if (to.X == from.X - 1 && to.Y == from.Y) return MoonPalaceRunDirection.West;
            if (to.X == from.X && to.Y == from.Y + 1) return MoonPalaceRunDirection.North;
            if (to.X == from.X && to.Y == from.Y - 1) return MoonPalaceRunDirection.South;
            throw new InvalidOperationException("RUN01 graph edges must join cardinally adjacent pattern slots.");
        }
    }

    public sealed class RunBranch
    {
        public RunBranch(int branchIndex, PatternSlotCoordinate attachment, IEnumerable<PatternSlotCoordinate> nodes,
            PatternSlotCoordinate rewardMarker)
        {
            BranchIndex = branchIndex;
            Attachment = attachment;
            Nodes = ReadOnly(nodes);
            RewardMarker = rewardMarker;
        }
        public int BranchIndex { get; }
        public PatternSlotCoordinate Attachment { get; }
        public IReadOnlyList<PatternSlotCoordinate> Nodes { get; }
        public PatternSlotCoordinate RewardMarker { get; }

        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values)
        {
            return new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());
        }
    }

    public sealed class RunStart
    {
        public RunStart(PatternSlotCoordinate slot, int localX, int localY) { Slot = slot; LocalX = localX; LocalY = localY; }
        public PatternSlotCoordinate Slot { get; }
        public int LocalX { get; }
        public int LocalY { get; }
    }

    public sealed class RunExit
    {
        public RunExit(PatternSlotCoordinate slot, int localX, int localY) { Slot = slot; LocalX = localX; LocalY = localY; }
        public PatternSlotCoordinate Slot { get; }
        public int LocalX { get; }
        public int LocalY { get; }
    }

    public sealed class RunGraphDigest
    {
        public RunGraphDigest(string value) { Value = value ?? string.Empty; }
        public string Value { get; }
    }

    public sealed class MoonPalaceMicroPatternRunGraph
    {
        internal MoonPalaceMicroPatternRunGraph(MoonPalaceMicroPatternRunConfig config, IEnumerable<RunNode> nodes,
            IEnumerable<RunEdge> edges, IEnumerable<PatternSlotCoordinate> mainPathNodes, IEnumerable<RunBranch> branches,
            RunStart start, RunExit exit, RunGraphDigest digest)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Nodes = ReadOnly(nodes);
            Edges = ReadOnly(edges);
            MainPathNodes = ReadOnly(mainPathNodes);
            Branches = ReadOnly(branches);
            Start = start ?? throw new ArgumentNullException(nameof(start));
            Exit = exit ?? throw new ArgumentNullException(nameof(exit));
            Digest = digest ?? throw new ArgumentNullException(nameof(digest));
        }

        public MoonPalaceMicroPatternRunConfig Config { get; }
        public IReadOnlyList<RunNode> Nodes { get; }
        public IReadOnlyList<RunEdge> Edges { get; }
        public IReadOnlyList<PatternSlotCoordinate> MainPathNodes { get; }
        public IReadOnlyList<RunBranch> Branches { get; }
        public RunStart Start { get; }
        public RunExit Exit { get; }
        public RunGraphDigest Digest { get; }

        public bool Contains(PatternSlotCoordinate coordinate) => coordinate.X >= 0 && coordinate.X < Config.PatternGridWidth &&
            coordinate.Y >= 0 && coordinate.Y < Config.PatternGridHeight;

        public static MoonPalaceMicroPatternRunGraph Create(MoonPalaceMicroPatternRunConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            var mainY = config.PatternGridHeight / 2;
            var main = Enumerable.Range(0, config.PatternGridWidth).Select(x => new PatternSlotCoordinate(x, mainY)).ToList();
            var edges = new List<RunEdge>();
            for (var index = 1; index < main.Count; index++) edges.Add(new RunEdge(main[index - 1], main[index], true, 0));

            var branchColumns = new[] { 5, 11, 18, 25, 32, config.PatternGridWidth - 3 }
                .Where(x => x > 0 && x < config.PatternGridWidth - 1).Distinct().OrderBy(x => x).ToArray();
            var branches = new List<RunBranch>();
            for (var index = 0; index < branchColumns.Length; index++)
            {
                var attachment = new PatternSlotCoordinate(branchColumns[index], mainY);
                var vertical = index % 2 == 0 ? 1 : -1;
                var nodes = new List<PatternSlotCoordinate>();
                for (var step = 1; step <= 3; step++)
                {
                    var coordinate = new PatternSlotCoordinate(attachment.X, attachment.Y + (vertical * step));
                    if (coordinate.Y < 0 || coordinate.Y >= config.PatternGridHeight)
                        throw new InvalidOperationException("RUN01 branch plan escaped its pattern grid.");
                    nodes.Add(coordinate);
                }
                var branchIndex = index + 1;
                var prior = attachment;
                foreach (var node in nodes)
                {
                    edges.Add(new RunEdge(prior, node, false, branchIndex));
                    prior = node;
                }
                branches.Add(new RunBranch(branchIndex, attachment, nodes, nodes[nodes.Count - 1]));
            }

            if (main.Count < config.MainPathMinSteps || main.Count > config.MainPathMaxSteps ||
                branches.Count < config.BranchMinCount || branches.Count > config.BranchMaxCount)
                throw new InvalidOperationException("RUN01 deterministic graph does not satisfy its configured path or branch bounds.");

            var nodeMap = new Dictionary<PatternSlotCoordinate, RunNode>();
            foreach (var coordinate in main) nodeMap.Add(coordinate, new RunNode(coordinate, true, 0));
            foreach (var branch in branches)
            foreach (var coordinate in branch.Nodes)
                nodeMap.Add(coordinate, new RunNode(coordinate, false, branch.BranchIndex));
            var orderedNodes = nodeMap.Values.OrderBy(node => node.Coordinate.Y).ThenBy(node => node.Coordinate.X).ToList();
            var start = new RunStart(main[0], 0, 1);
            var exit = new RunExit(main[main.Count - 1], config.PatternWidth - 1, 1);
            var lines = new List<string> { "RUN01_GRAPH", config.CanonicalDigest, "start=" + start.Slot, "exit=" + exit.Slot };
            lines.AddRange(main.Select((node, index) => "M|" + index.ToString(CultureInfo.InvariantCulture) + "|" + node));
            lines.AddRange(branches.Select(branch => "B|" + branch.BranchIndex.ToString(CultureInfo.InvariantCulture) + "|" + branch.Attachment + "|" +
                string.Join(";", branch.Nodes.Select(node => node.ToString())) + "|reward=" + branch.RewardMarker));
            lines.AddRange(edges.OrderBy(edge => edge.BranchIndex).ThenBy(edge => edge.From.Y).ThenBy(edge => edge.From.X)
                .Select(edge => "E|" + edge.From + "|" + edge.To + "|" + edge.Direction + "|" + (edge.IsMainPath ? "M" : "B")));
            return new MoonPalaceMicroPatternRunGraph(config, orderedNodes, edges, main, branches, start, exit,
                new RunGraphDigest(BakingCanonicalDigest.HashCanonicalLines(lines)));
        }

        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values)
        {
            return new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());
        }
    }
}
