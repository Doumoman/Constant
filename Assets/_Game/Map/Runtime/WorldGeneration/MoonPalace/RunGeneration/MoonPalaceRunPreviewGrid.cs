using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public sealed class MoonPalaceRunPreviewRoomFrame
    {
        public MoonPalaceRunPreviewRoomFrame(CameraRoomNode room)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            RoomId = room.RoomId;
            Role = room.Role;
            TileMinX = room.Bounds.TileMinX;
            TileMinY = room.Bounds.TileMinY;
            TileWidth = room.Bounds.TileWidth;
            TileHeight = room.Bounds.TileHeight;
            Center = new Vector2(TileMinX + (TileWidth * 0.5f), TileMinY + (TileHeight * 0.5f));
            OrthographicSize = Mathf.Max((TileHeight * 0.5f) + 2f, (TileWidth * 0.5f) + 2f);
        }

        public string RoomId { get; }
        public CameraRoomRole Role { get; }
        public int TileMinX { get; }
        public int TileMinY { get; }
        public int TileWidth { get; }
        public int TileHeight { get; }
        public Vector2 Center { get; }
        public float OrthographicSize { get; }
        public bool Contains(int x, int y) => x >= TileMinX && x < TileMinX + TileWidth && y >= TileMinY && y < TileMinY + TileHeight;
    }

    public sealed class MoonPalaceRunPreviewConnectorTrigger
    {
        public MoonPalaceRunPreviewConnectorTrigger(CameraRoomConnectorGate gate)
        {
            if (gate == null) throw new ArgumentNullException(nameof(gate));
            ConnectorId = gate.ConnectorId;
            FromRoomId = gate.FromRoomId;
            ToRoomId = gate.ToRoomId;
            FromGateTile = new Vector2Int(gate.FromTileGateRect.X, gate.FromTileGateRect.Y);
            ToGateTile = new Vector2Int(gate.ToTileGateRect.X, gate.ToTileGateRect.Y);
            Direction = gate.Direction;
            Kind = gate.TransitionKind == CameraRoomTransitionKind.Branch ? "branch" :
                gate.TransitionKind == CameraRoomTransitionKind.Split ? "split" :
                gate.TransitionKind == CameraRoomTransitionKind.Rejoin ? "rejoin" : "main";
            RequiredSocketBit = gate.RequiredSocketFrom;
            IsReciprocal = true;
            IsOpen = false;
        }

        internal void SetOpen(bool value) { IsOpen = value; }
        public string ConnectorId { get; }
        public string FromRoomId { get; }
        public string ToRoomId { get; }
        public Vector2Int FromGateTile { get; }
        public Vector2Int ToGateTile { get; }
        public MoonPalaceRunDirection Direction { get; }
        public string Kind { get; }
        public int RequiredSocketBit { get; }
        public bool IsReciprocal { get; }
        public bool IsOpen { get; private set; }
    }

    /// <summary>Compact deterministic tile traversal model projected from an actual RUN03 composition; it never carves cells.</summary>
    public sealed class MoonPalaceRunPreviewGrid
    {
        private readonly bool[] openCells;
        private readonly string[] roomIds;
        private readonly string[] connectorIds;
        private readonly string[] routeOwnership;
        private readonly IReadOnlyList<Vector2Int> routeToExit;

        private MoonPalaceRunPreviewGrid(MoonPalaceRunPreviewConfig config, MoonPalaceCameraRoomGraph graph, MoonPalaceCameraRoomConnectorPlan connectors,
            MoonPalaceCameraRoomPatternComposition composition, IEnumerable<MoonPalaceRunPreviewRoomFrame> roomFrames,
            IEnumerable<MoonPalaceRunPreviewConnectorTrigger> connectorTriggers, bool[] open, string[] rooms, string[] connectorIdsByCell,
            string[] ownership, IEnumerable<Vector2Int> route, string canonicalDigest)
        {
            Config = config;
            Graph = graph;
            Connectors = connectors;
            Composition = composition;
            RoomFrames = ReadOnly(roomFrames);
            ConnectorTriggers = ReadOnly(connectorTriggers);
            openCells = open;
            roomIds = rooms;
            connectorIds = connectorIdsByCell;
            routeOwnership = ownership;
            routeToExit = ReadOnly(route);
            StartTile = new Vector2Int(composition.StartTile.X, composition.StartTile.Y);
            ExitTile = new Vector2Int(composition.ExitTile.X, composition.ExitTile.Y);
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public MoonPalaceRunPreviewConfig Config { get; }
        public MoonPalaceCameraRoomGraph Graph { get; }
        public MoonPalaceCameraRoomConnectorPlan Connectors { get; }
        public MoonPalaceCameraRoomPatternComposition Composition { get; }
        public int TileWidth => Config.TileWidth;
        public int TileHeight => Config.TileHeight;
        public int CellCount => TileWidth * TileHeight;
        public Vector2Int StartTile { get; }
        public Vector2Int ExitTile { get; }
        public IReadOnlyList<MoonPalaceRunPreviewRoomFrame> RoomFrames { get; }
        public IReadOnlyList<MoonPalaceRunPreviewConnectorTrigger> ConnectorTriggers { get; }
        public IReadOnlyList<Vector2Int> RouteToExit => routeToExit;
        public string CanonicalDigest { get; }
        public int OpenCellCount => openCells.Count(value => value);

        public static MoonPalaceRunPreviewGrid CreateDefault() => Create(MoonPalaceRunPreviewConfig.CreateDefault());

        public static MoonPalaceRunPreviewGrid Create(MoonPalaceRunPreviewConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            var sourceConfig = MoonPalaceCameraRoomCourseConfig.CreateDefault();
            if (sourceConfig.CourseId != config.SourceCourseId || sourceConfig.SeedId != config.SeedId || sourceConfig.SeedValue != config.SeedValue || sourceConfig.TileWidth != config.TileWidth || sourceConfig.TileHeight != config.TileHeight)
                throw new InvalidOperationException("RUN04 rejects a preview grid that is not directly derived from RUN03.");
            var graph = MoonPalaceCameraRoomGraph.Create(sourceConfig);
            var connectors = MoonPalaceCameraRoomConnectorPlanner.Plan(graph);
            var composition = MoonPalaceCameraRoomPatternComposer.Compose(sourceConfig, graph, connectors);
            if (graph.CameraRoomCount != config.ExpectedRoomCount || connectors.ConnectorCount != config.ExpectedConnectorCount || composition.RotationCount != 0 || composition.FallbackCarveCount != 0 || composition.SilentRepairCount != 0)
                throw new InvalidOperationException("RUN04 refuses a non-reviewed RUN03 composition.");

            var cellCount = config.TileWidth * config.TileHeight;
            var open = new bool[cellCount];
            var rooms = new string[cellCount];
            var gateIds = new string[cellCount];
            var ownership = new string[cellCount];
            for (var y = 0; y < config.TileHeight; y++)
            for (var x = 0; x < config.TileWidth; x++)
            {
                var index = (y * config.TileWidth) + x;
                open[index] = composition.IsOpen(x, y);
                var slot = new PatternSlotCoordinate(x / config.PatternWidth, y / config.PatternHeight);
                var room = graph.Rooms.FirstOrDefault(value => value.Bounds.Contains(slot));
                rooms[index] = room == null ? "OUTSIDE_QUIET" : room.RoomId;
                ownership[index] = composition.Placement(slot).RouteOwnership;
                gateIds[index] = string.Empty;
            }
            var frames = graph.Rooms.OrderBy(room => room.RoomId, StringComparer.Ordinal).Select(room => new MoonPalaceRunPreviewRoomFrame(room)).ToList();
            var triggers = connectors.Gates.OrderBy(gate => gate.ConnectorId, StringComparer.Ordinal).Select(gate => new MoonPalaceRunPreviewConnectorTrigger(gate)).ToList();
            foreach (var trigger in triggers)
            {
                trigger.SetOpen(IsOpen(open, config.TileWidth, config.TileHeight, trigger.FromGateTile.x, trigger.FromGateTile.y) && IsOpen(open, config.TileWidth, config.TileHeight, trigger.ToGateTile.x, trigger.ToGateTile.y));
                gateIds[(trigger.FromGateTile.y * config.TileWidth) + trigger.FromGateTile.x] = trigger.ConnectorId;
                gateIds[(trigger.ToGateTile.y * config.TileWidth) + trigger.ToGateTile.x] = trigger.ConnectorId;
            }
            var start = new Vector2Int(composition.StartTile.X, composition.StartTile.Y);
            var exit = new Vector2Int(composition.ExitTile.X, composition.ExitTile.Y);
            var route = FindRoute(open, config.TileWidth, config.TileHeight, start, exit);
            if (route.Count == 0 || !route[0].Equals(start) || !route[route.Count - 1].Equals(exit))
                throw new InvalidOperationException("RUN04 records a missing BFS route instead of repairing terrain.");
            var lines = new List<string> { "RUN04_TRAVERSAL_GRID", config.CanonicalDigest, composition.CompositionDigest, "open_cells=" + open.Count(value => value).ToString(CultureInfo.InvariantCulture), "start=" + start.x.ToString(CultureInfo.InvariantCulture) + ":" + start.y.ToString(CultureInfo.InvariantCulture), "exit=" + exit.x.ToString(CultureInfo.InvariantCulture) + ":" + exit.y.ToString(CultureInfo.InvariantCulture) };
            lines.AddRange(frames.Select(frame => "ROOM|" + frame.RoomId + "|" + frame.TileMinX.ToString(CultureInfo.InvariantCulture) + ":" + frame.TileMinY.ToString(CultureInfo.InvariantCulture) + ":" + frame.TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + frame.TileHeight.ToString(CultureInfo.InvariantCulture)));
            lines.AddRange(triggers.Select(trigger => "GATE|" + trigger.ConnectorId + "|" + trigger.FromRoomId + "|" + trigger.ToRoomId + "|" + trigger.FromGateTile.x.ToString(CultureInfo.InvariantCulture) + ":" + trigger.FromGateTile.y.ToString(CultureInfo.InvariantCulture) + ">" + trigger.ToGateTile.x.ToString(CultureInfo.InvariantCulture) + ":" + trigger.ToGateTile.y.ToString(CultureInfo.InvariantCulture) + "|" + (trigger.IsOpen ? "1" : "0")));
            lines.AddRange(route.Select((tile, index) => "ROUTE|" + index.ToString(CultureInfo.InvariantCulture) + "|" + tile.x.ToString(CultureInfo.InvariantCulture) + ":" + tile.y.ToString(CultureInfo.InvariantCulture)));
            return new MoonPalaceRunPreviewGrid(config, graph, connectors, composition, frames, triggers, open, rooms, gateIds, ownership, route, BakingCanonicalDigest.HashCanonicalLines(lines));
        }

        public bool IsInside(int x, int y) => x >= 0 && x < TileWidth && y >= 0 && y < TileHeight;
        public bool IsOpen(int x, int y) => IsInside(x, y) && openCells[(y * TileWidth) + x];
        public bool CanStep(int fromX, int fromY, int toX, int toY)
        {
            return IsOpen(fromX, fromY) && IsOpen(toX, toY) && Mathf.Abs(toX - fromX) + Mathf.Abs(toY - fromY) == 1;
        }
        public string GetRoomId(int x, int y) => IsInside(x, y) ? roomIds[(y * TileWidth) + x] : string.Empty;
        public string GetConnectorId(int x, int y) => IsInside(x, y) ? connectorIds[(y * TileWidth) + x] : string.Empty;
        public string GetRouteOwnership(int x, int y) => IsInside(x, y) ? routeOwnership[(y * TileWidth) + x] : string.Empty;
        public IReadOnlyList<Vector2Int> FindRouteToExit() => routeToExit;
        public MoonPalaceRunPreviewRoomFrame GetRoomFrame(string roomId) => RoomFrames.Single(frame => string.Equals(frame.RoomId, roomId, StringComparison.Ordinal));

        public bool TryGetConnectorTransition(string currentRoomId, Vector2Int from, Vector2Int to, out MoonPalaceRunPreviewConnectorTrigger trigger, out string targetRoomId)
        {
            trigger = ConnectorTriggers.FirstOrDefault(value =>
                (string.Equals(value.FromRoomId, currentRoomId, StringComparison.Ordinal) && value.FromGateTile.Equals(from) && value.ToGateTile.Equals(to)) ||
                (string.Equals(value.ToRoomId, currentRoomId, StringComparison.Ordinal) && value.ToGateTile.Equals(from) && value.FromGateTile.Equals(to)));
            if (trigger == null || !trigger.IsOpen) { targetRoomId = string.Empty; return false; }
            targetRoomId = string.Equals(trigger.FromRoomId, currentRoomId, StringComparison.Ordinal) ? trigger.ToRoomId : trigger.FromRoomId;
            return true;
        }

        private static IReadOnlyList<Vector2Int> FindRoute(bool[] open, int width, int height, Vector2Int start, Vector2Int exit)
        {
            var route = new List<Vector2Int>();
            if (!IsOpen(open, width, height, start.x, start.y) || !IsOpen(open, width, height, exit.x, exit.y)) return route;
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var visited = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            var directions = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Equals(exit)) break;
                foreach (var direction in directions)
                {
                    var next = current + direction;
                    if (IsOpen(open, width, height, next.x, next.y) && visited.Add(next)) { parent.Add(next, current); queue.Enqueue(next); }
                }
            }
            if (!visited.Contains(exit)) return route;
            for (var current = exit; ; current = parent[current]) { route.Add(current); if (current.Equals(start)) break; }
            route.Reverse();
            return route;
        }

        private static bool IsOpen(bool[] values, int width, int height, int x, int y) => x >= 0 && x < width && y >= 0 && y < height && values[(y * width) + x];
        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());
    }
}
