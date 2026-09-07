using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public enum CameraRoomRole { Start, Transit, Vertical, Split, Exit, Branch }
    public enum CameraRoomTransitionKind { Horizontal, VerticalUp, VerticalDown, Branch, Split, Rejoin }

    public sealed class CameraRoomBoundsPattern
    {
        public CameraRoomBoundsPattern(string roomId, int minX, int minY, int maxX, int maxY)
        {
            RoomId = roomId ?? string.Empty; MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY;
        }
        public string RoomId { get; }
        public int MinX { get; }
        public int MinY { get; }
        public int MaxX { get; }
        public int MaxY { get; }
        public int Width => MaxX - MinX + 1;
        public int Height => MaxY - MinY + 1;
        public int TileMinX => MinX * MoonPalaceCameraRoomCourseConfig.PatternSize;
        public int TileMinY => MinY * MoonPalaceCameraRoomCourseConfig.PatternSize;
        public int TileWidth => Width * MoonPalaceCameraRoomCourseConfig.PatternSize;
        public int TileHeight => Height * MoonPalaceCameraRoomCourseConfig.PatternSize;
        public bool Contains(PatternSlotCoordinate slot) => slot.X >= MinX && slot.X <= MaxX && slot.Y >= MinY && slot.Y <= MaxY;
        public bool IsEdge(PatternSlotCoordinate slot) => Contains(slot) && (slot.X == MinX || slot.X == MaxX || slot.Y == MinY || slot.Y == MaxY);
        public bool IsInside(int width, int height) => MinX >= 0 && MinY >= 0 && MaxX >= MinX && MaxY >= MinY && MaxX < width && MaxY < height;
        public bool Overlaps(CameraRoomBoundsPattern other) => other != null && MinX <= other.MaxX && MaxX >= other.MinX && MinY <= other.MaxY && MaxY >= other.MinY;
        public override string ToString() => MinX.ToString(CultureInfo.InvariantCulture) + ":" + MinY.ToString(CultureInfo.InvariantCulture) + "-" + MaxX.ToString(CultureInfo.InvariantCulture) + ":" + MaxY.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class CameraRoomNode
    {
        public CameraRoomNode(string roomId, CameraRoomRole role, CameraRoomBoundsPattern bounds, int routeOrder)
        {
            RoomId = roomId ?? string.Empty; Role = role; Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds)); RouteOrder = routeOrder;
        }
        public string RoomId { get; }
        public CameraRoomRole Role { get; }
        public CameraRoomBoundsPattern Bounds { get; }
        public int RouteOrder { get; }
        public bool IsMainRoute => Role != CameraRoomRole.Branch;
    }

    public sealed class CameraRoomEdge
    {
        public CameraRoomEdge(PatternSlotCoordinate from, PatternSlotCoordinate to, string routeOwnership, string ownerId)
        {
            From = from; To = to; RouteOwnership = routeOwnership ?? string.Empty; OwnerId = ownerId ?? string.Empty;
            Direction = DirectionFrom(from, to);
        }
        public PatternSlotCoordinate From { get; }
        public PatternSlotCoordinate To { get; }
        public MoonPalaceRunDirection Direction { get; }
        public string RouteOwnership { get; }
        public string OwnerId { get; }
        public static MoonPalaceRunDirection DirectionFrom(PatternSlotCoordinate from, PatternSlotCoordinate to)
        {
            if (from.X + 1 == to.X && from.Y == to.Y) return MoonPalaceRunDirection.East;
            if (from.X - 1 == to.X && from.Y == to.Y) return MoonPalaceRunDirection.West;
            if (from.Y + 1 == to.Y && from.X == to.X) return MoonPalaceRunDirection.North;
            if (from.Y - 1 == to.Y && from.X == to.X) return MoonPalaceRunDirection.South;
            throw new InvalidOperationException("RUN03 route edges must join adjacent 4x4 pattern slots.");
        }
    }

    public sealed class CameraRoomTransition
    {
        public CameraRoomTransition(string connectorId, string fromRoomId, string toRoomId, PatternSlotCoordinate fromSlot,
            PatternSlotCoordinate toSlot, MoonPalaceRunDirection direction, CameraRoomTransitionKind kind, bool required)
        {
            ConnectorId = connectorId ?? string.Empty; FromRoomId = fromRoomId ?? string.Empty; ToRoomId = toRoomId ?? string.Empty;
            FromPatternSlot = fromSlot; ToPatternSlot = toSlot; Direction = direction; TransitionKind = kind; IsRequiredForCompletion = required;
        }
        public string ConnectorId { get; }
        public string FromRoomId { get; }
        public string ToRoomId { get; }
        public PatternSlotCoordinate FromPatternSlot { get; }
        public PatternSlotCoordinate ToPatternSlot { get; }
        public MoonPalaceRunDirection Direction { get; }
        public CameraRoomTransitionKind TransitionKind { get; }
        public bool IsRequiredForCompletion { get; }
    }

    public sealed class CameraRoomTileGateRect
    {
        public CameraRoomTileGateRect(int x, int y, int width, int height) { X = x; Y = y; Width = width; Height = height; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + ":" + Y.ToString(CultureInfo.InvariantCulture) + ":" + Width.ToString(CultureInfo.InvariantCulture) + "x" + Height.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class CameraRoomConnectorGate
    {
        public CameraRoomConnectorGate(CameraRoomTransition transition, CameraRoomTileGateRect fromRect, CameraRoomTileGateRect toRect)
        {
            Transition = transition ?? throw new ArgumentNullException(nameof(transition)); FromTileGateRect = fromRect; ToTileGateRect = toRect;
        }
        public CameraRoomTransition Transition { get; }
        public string ConnectorId => Transition.ConnectorId;
        public string FromRoomId => Transition.FromRoomId;
        public string ToRoomId => Transition.ToRoomId;
        public MoonPalaceRunDirection Direction => Transition.Direction;
        public PatternSlotCoordinate FromPatternSlot => Transition.FromPatternSlot;
        public PatternSlotCoordinate ToPatternSlot => Transition.ToPatternSlot;
        public CameraRoomTileGateRect FromTileGateRect { get; }
        public CameraRoomTileGateRect ToTileGateRect { get; }
        public int RequiredSocketFrom => MoonPalaceCameraRoomPatternComposer.RouteSocketBit;
        public int RequiredSocketTo => MoonPalaceCameraRoomPatternComposer.RouteSocketBit;
        public CameraRoomTransitionKind TransitionKind => Transition.TransitionKind;
        public bool IsRequiredForCompletion => Transition.IsRequiredForCompletion;
    }

    public sealed class CameraRoomMainRoute
    {
        public CameraRoomMainRoute(IEnumerable<string> roomIds, IEnumerable<PatternSlotCoordinate> slots)
        {
            RoomIds = ReadOnly(roomIds); PatternSlots = ReadOnly(slots);
        }
        public IReadOnlyList<string> RoomIds { get; }
        public IReadOnlyList<PatternSlotCoordinate> PatternSlots { get; }
        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());
    }

    public sealed class CameraRoomBranchRoute
    {
        public CameraRoomBranchRoute(string roomId, PatternSlotCoordinate entry, IEnumerable<PatternSlotCoordinate> slots)
        {
            RoomId = roomId ?? string.Empty; Entry = entry; PatternSlots = new ReadOnlyCollection<PatternSlotCoordinate>((slots ?? Array.Empty<PatternSlotCoordinate>()).ToList());
        }
        public string RoomId { get; }
        public PatternSlotCoordinate Entry { get; }
        public IReadOnlyList<PatternSlotCoordinate> PatternSlots { get; }
    }

    public sealed class CameraRoomSplitRejoinRoute
    {
        public CameraRoomSplitRejoinRoute(int index, PatternSlotCoordinate split, PatternSlotCoordinate rejoin, IEnumerable<PatternSlotCoordinate> alternateSlots)
        {
            Index = index; Split = split; Rejoin = rejoin; AlternateSlots = new ReadOnlyCollection<PatternSlotCoordinate>((alternateSlots ?? Array.Empty<PatternSlotCoordinate>()).ToList());
        }
        public int Index { get; }
        public PatternSlotCoordinate Split { get; }
        public PatternSlotCoordinate Rejoin { get; }
        public IReadOnlyList<PatternSlotCoordinate> AlternateSlots { get; }
    }

    public sealed class CameraRoomGraphDigest { public CameraRoomGraphDigest(string value) { Value = value ?? string.Empty; } public string Value { get; } }

    /// <summary>Deterministic room-frame course graph; it is a recipe over slots, not a pasted tilemap.</summary>
    public sealed class MoonPalaceCameraRoomGraph
    {
        private MoonPalaceCameraRoomGraph(MoonPalaceCameraRoomCourseConfig config, IEnumerable<CameraRoomNode> rooms,
            IEnumerable<CameraRoomEdge> edges, IEnumerable<CameraRoomTransition> transitions, CameraRoomMainRoute mainRoute,
            IEnumerable<CameraRoomBranchRoute> branches, IEnumerable<CameraRoomSplitRejoinRoute> splitRejoins,
            RunStart start, RunExit exit, CameraRoomGraphDigest digest)
        {
            Config = config; Rooms = ReadOnly(rooms); Edges = ReadOnly(edges); Transitions = ReadOnly(transitions);
            MainRoute = mainRoute; BranchRoutes = ReadOnly(branches); SplitRejoinRoutes = ReadOnly(splitRejoins);
            Start = start; Exit = exit; Digest = digest;
        }
        public MoonPalaceCameraRoomCourseConfig Config { get; }
        public IReadOnlyList<CameraRoomNode> Rooms { get; }
        public IReadOnlyList<CameraRoomEdge> Edges { get; }
        public IReadOnlyList<CameraRoomTransition> Transitions { get; }
        public CameraRoomMainRoute MainRoute { get; }
        public IReadOnlyList<CameraRoomBranchRoute> BranchRoutes { get; }
        public IReadOnlyList<CameraRoomSplitRejoinRoute> SplitRejoinRoutes { get; }
        public RunStart Start { get; }
        public RunExit Exit { get; }
        public CameraRoomGraphDigest Digest { get; }
        public int CameraRoomCount => Rooms.Count;
        public int MainRouteRoomCount => MainRoute.RoomIds.Count;
        public int BranchRoomCount => BranchRoutes.Count;
        public int SplitRejoinCount => SplitRejoinRoutes.Count;
        public int VerticalTransitionSpanPatternRows => MainRoute.PatternSlots.Max(slot => slot.Y) - MainRoute.PatternSlots.Min(slot => slot.Y);
        public IReadOnlyList<PatternSlotCoordinate> RouteSlots => ReadOnly(Edges.SelectMany(edge => new[] { edge.From, edge.To }).Distinct().OrderBy(slot => slot.Y).ThenBy(slot => slot.X));
        public bool Contains(PatternSlotCoordinate slot) => slot.X >= 0 && slot.X < Config.CoursePatternGridWidth && slot.Y >= 0 && slot.Y < Config.CoursePatternGridHeight;
        public CameraRoomNode Room(string id) => Rooms.Single(room => string.Equals(room.RoomId, id, StringComparison.Ordinal));

        public static MoonPalaceCameraRoomGraph Create(MoonPalaceCameraRoomCourseConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            var rooms = new[]
            {
                Room("R01_START", CameraRoomRole.Start, 0, 8, 10, 15, 1),
                Room("R02_TRANSIT", CameraRoomRole.Transit, 11, 8, 20, 15, 2),
                Room("R03_VERTICAL_ASCENT", CameraRoomRole.Vertical, 21, 8, 30, 19, 3),
                Room("R04_SPLIT_GALLERY", CameraRoomRole.Split, 31, 16, 40, 23, 4),
                Room("R05_TRANSIT", CameraRoomRole.Transit, 41, 16, 50, 23, 5),
                Room("R06_SPLIT_GALLERY", CameraRoomRole.Split, 51, 16, 60, 23, 6),
                Room("R07_VERTICAL_DESCENT", CameraRoomRole.Vertical, 61, 8, 70, 17, 7),
                Room("R08_EXIT", CameraRoomRole.Exit, 71, 8, 79, 15, 8),
                Room("B01_LOWER_BRANCH", CameraRoomRole.Branch, 21, 0, 30, 7, 0),
                Room("B02_LOWER_BRANCH", CameraRoomRole.Branch, 51, 8, 60, 15, 0),
            };
            config.ValidateRoomBounds(rooms.Select(room => room.Bounds));
            var draft = new Draft(config, rooms);
            var main = Join(Horizontal(2, 20, 11), Vertical(20, 11, 8), Horizontal(20, 21, 8), Vertical(21, 8, 19),
                Horizontal(21, 60, 19), Vertical(60, 19, 16), Horizontal(60, 61, 16), Vertical(61, 16, 11), Horizontal(61, 77, 11));
            draft.AddPath("MAIN", "MAIN", main);
            draft.AddBranch("B01_LOWER_BRANCH", Join(Horizontal(21, 25, 8), Vertical(25, 8, 2)));
            draft.AddBranch("B02_LOWER_BRANCH", Vertical(55, 19, 12));
            draft.AddSplit(1, new PatternSlotCoordinate(34, 19), new PatternSlotCoordinate(38, 19), new[]
            {
                new PatternSlotCoordinate(34, 20), new PatternSlotCoordinate(34, 21), new PatternSlotCoordinate(35, 21), new PatternSlotCoordinate(36, 21),
                new PatternSlotCoordinate(37, 21), new PatternSlotCoordinate(38, 21), new PatternSlotCoordinate(38, 20),
            });
            draft.AddSplit(2, new PatternSlotCoordinate(54, 19), new PatternSlotCoordinate(58, 19), new[]
            {
                new PatternSlotCoordinate(54, 20), new PatternSlotCoordinate(54, 21), new PatternSlotCoordinate(55, 21), new PatternSlotCoordinate(56, 21),
                new PatternSlotCoordinate(57, 21), new PatternSlotCoordinate(58, 21), new PatternSlotCoordinate(58, 20),
            });
            draft.AddTransition("C01", "R01_START", "R02_TRANSIT", new PatternSlotCoordinate(10, 11), new PatternSlotCoordinate(11, 11), CameraRoomTransitionKind.Horizontal, true);
            draft.AddTransition("C02", "R02_TRANSIT", "R03_VERTICAL_ASCENT", new PatternSlotCoordinate(20, 8), new PatternSlotCoordinate(21, 8), CameraRoomTransitionKind.VerticalUp, true);
            draft.AddTransition("C03", "R03_VERTICAL_ASCENT", "R04_SPLIT_GALLERY", new PatternSlotCoordinate(30, 19), new PatternSlotCoordinate(31, 19), CameraRoomTransitionKind.VerticalUp, true);
            draft.AddTransition("C04", "R04_SPLIT_GALLERY", "R05_TRANSIT", new PatternSlotCoordinate(40, 19), new PatternSlotCoordinate(41, 19), CameraRoomTransitionKind.Horizontal, true);
            draft.AddTransition("C05", "R05_TRANSIT", "R06_SPLIT_GALLERY", new PatternSlotCoordinate(50, 19), new PatternSlotCoordinate(51, 19), CameraRoomTransitionKind.Horizontal, true);
            draft.AddTransition("C06", "R06_SPLIT_GALLERY", "R07_VERTICAL_DESCENT", new PatternSlotCoordinate(60, 16), new PatternSlotCoordinate(61, 16), CameraRoomTransitionKind.VerticalDown, true);
            draft.AddTransition("C07", "R07_VERTICAL_DESCENT", "R08_EXIT", new PatternSlotCoordinate(70, 11), new PatternSlotCoordinate(71, 11), CameraRoomTransitionKind.Horizontal, true);
            draft.AddTransition("C08", "R03_VERTICAL_ASCENT", "B01_LOWER_BRANCH", new PatternSlotCoordinate(25, 8), new PatternSlotCoordinate(25, 7), CameraRoomTransitionKind.Branch, false);
            draft.AddTransition("C09", "R06_SPLIT_GALLERY", "B02_LOWER_BRANCH", new PatternSlotCoordinate(55, 16), new PatternSlotCoordinate(55, 15), CameraRoomTransitionKind.Branch, false);
            return draft.Finish(new RunStart(new PatternSlotCoordinate(2, 11), 1, 1), new RunExit(new PatternSlotCoordinate(77, 11), 1, 1));
        }

        private static CameraRoomNode Room(string id, CameraRoomRole role, int minX, int minY, int maxX, int maxY, int order) => new CameraRoomNode(id, role, new CameraRoomBoundsPattern(id, minX, minY, maxX, maxY), order);
        private static IEnumerable<PatternSlotCoordinate> Horizontal(int from, int to, int y) { var step = from <= to ? 1 : -1; for (var x = from; ; x += step) { yield return new PatternSlotCoordinate(x, y); if (x == to) yield break; } }
        private static IEnumerable<PatternSlotCoordinate> Vertical(int x, int from, int to) { var step = from <= to ? 1 : -1; for (var y = from; ; y += step) { yield return new PatternSlotCoordinate(x, y); if (y == to) yield break; } }
        private static IReadOnlyList<PatternSlotCoordinate> Join(params IEnumerable<PatternSlotCoordinate>[] paths)
        {
            var result = new List<PatternSlotCoordinate>();
            foreach (var path in paths) foreach (var slot in path) if (result.Count == 0 || !result[result.Count - 1].Equals(slot)) result.Add(slot);
            return result;
        }
        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());

        private sealed class Draft
        {
            private readonly MoonPalaceCameraRoomCourseConfig config;
            private readonly IReadOnlyList<CameraRoomNode> rooms;
            private readonly List<CameraRoomEdge> edges = new List<CameraRoomEdge>();
            private readonly HashSet<string> edgeTokens = new HashSet<string>(StringComparer.Ordinal);
            private readonly List<CameraRoomTransition> transitions = new List<CameraRoomTransition>();
            private readonly List<CameraRoomBranchRoute> branches = new List<CameraRoomBranchRoute>();
            private readonly List<CameraRoomSplitRejoinRoute> splits = new List<CameraRoomSplitRejoinRoute>();
            private IReadOnlyList<PatternSlotCoordinate> main;
            public Draft(MoonPalaceCameraRoomCourseConfig courseConfig, IEnumerable<CameraRoomNode> cameraRooms) { config = courseConfig; rooms = ReadOnly(cameraRooms); }
            public void AddPath(string ownership, string owner, IEnumerable<PatternSlotCoordinate> slots)
            {
                var values = (slots ?? Array.Empty<PatternSlotCoordinate>()).ToList();
                if (values.Count < 2 || values.Any(slot => !Contains(slot))) throw new InvalidOperationException("RUN03 route path escaped the camera-room course grid.");
                for (var index = 1; index < values.Count; index++)
                {
                    var edge = new CameraRoomEdge(values[index - 1], values[index], ownership, owner);
                    var token = edge.From + ">" + edge.To;
                    if (!edgeTokens.Add(token)) throw new InvalidOperationException("RUN03 rejects duplicate directed route edges.");
                    edges.Add(edge);
                }
                if (ownership == "MAIN") main = values;
            }
            public void AddBranch(string roomId, IEnumerable<PatternSlotCoordinate> slots)
            {
                var values = (slots ?? Array.Empty<PatternSlotCoordinate>()).ToList(); AddPath("BRANCH", roomId, values);
                var entry = values.FirstOrDefault(slot => Room(roomId).Bounds.Contains(slot));
                branches.Add(new CameraRoomBranchRoute(roomId, entry, values));
            }
            public void AddSplit(int index, PatternSlotCoordinate split, PatternSlotCoordinate rejoin, IEnumerable<PatternSlotCoordinate> alternate)
            {
                var values = (alternate ?? Array.Empty<PatternSlotCoordinate>()).ToList();
                if (values.Count < 2) throw new InvalidOperationException("RUN03 split/rejoin route needs a recorded alternate path.");
                AddPath("SPLIT_REJOIN", "S" + index.ToString(CultureInfo.InvariantCulture), Join(new[] { split }, values, new[] { rejoin }));
                splits.Add(new CameraRoomSplitRejoinRoute(index, split, rejoin, values));
            }
            public void AddTransition(string id, string fromRoom, string toRoom, PatternSlotCoordinate from, PatternSlotCoordinate to, CameraRoomTransitionKind kind, bool required)
            {
                var direction = CameraRoomEdge.DirectionFrom(from, to); transitions.Add(new CameraRoomTransition(id, fromRoom, toRoom, from, to, direction, kind, required));
            }
            public MoonPalaceCameraRoomGraph Finish(RunStart start, RunExit exit)
            {
                if (main == null || !main.Contains(start.Slot) || !main.Contains(exit.Slot)) throw new InvalidOperationException("RUN03 requires distinct start and exit on the main room route.");
                if (rooms.Count < config.CameraRoomMinCount || rooms.Count > config.CameraRoomMaxCount || main == null || branches.Count < config.OptionalBranchRoomMinCount || branches.Count > config.OptionalBranchRoomMaxCount || splits.Count < config.SplitRejoinMinCount || splits.Count > config.SplitRejoinMaxCount)
                    throw new InvalidOperationException("RUN03 graph did not meet its room/branch/split contract.");
                var mainRooms = rooms.Where(room => room.IsMainRoute).OrderBy(room => room.RouteOrder).Select(room => room.RoomId).ToList();
                if (mainRooms.Count < config.RequiredMainRoomMinCount || mainRooms.Count > config.RequiredMainRoomMaxCount || main.Max(slot => slot.Y) - main.Min(slot => slot.Y) < 10)
                    throw new InvalidOperationException("RUN03 requires 7-9 main rooms and a ten-row vertical transition span.");
                if (transitions.Select(item => item.ConnectorId).Distinct(StringComparer.Ordinal).Count() != transitions.Count || transitions.Any(item => !Room(item.FromRoomId).Bounds.IsEdge(item.FromPatternSlot) || !Room(item.ToRoomId).Bounds.IsEdge(item.ToPatternSlot)))
                    throw new InvalidOperationException("RUN03 connector gates must be unique reciprocal room-edge transitions.");
                if (edges.Any(edge => !Contains(edge.From) || !Contains(edge.To))) throw new InvalidOperationException("RUN03 route edge escaped the course grid.");
                var digestLines = new List<string> { "RUN03_GRAPH", config.CanonicalDigest, "start=" + start.Slot, "exit=" + exit.Slot };
                digestLines.AddRange(rooms.OrderBy(room => room.RoomId, StringComparer.Ordinal).Select(room => "ROOM|" + room.RoomId + "|" + room.Role + "|" + room.Bounds + "|" + room.RouteOrder.ToString(CultureInfo.InvariantCulture)));
                digestLines.AddRange(edges.OrderBy(edge => edge.RouteOwnership, StringComparer.Ordinal).ThenBy(edge => edge.OwnerId, StringComparer.Ordinal).ThenBy(edge => edge.From.Y).ThenBy(edge => edge.From.X).Select(edge => "EDGE|" + edge.RouteOwnership + "|" + edge.OwnerId + "|" + edge.From + "|" + edge.To));
                digestLines.AddRange(transitions.OrderBy(item => item.ConnectorId, StringComparer.Ordinal).Select(item => "TRANSITION|" + item.ConnectorId + "|" + item.FromRoomId + "|" + item.ToRoomId + "|" + item.FromPatternSlot + "|" + item.ToPatternSlot + "|" + item.Direction + "|" + item.TransitionKind));
                return new MoonPalaceCameraRoomGraph(config, rooms, edges, transitions, new CameraRoomMainRoute(mainRooms, main), branches, splits, start, exit, new CameraRoomGraphDigest(BakingCanonicalDigest.HashCanonicalLines(digestLines)));
            }
            private bool Contains(PatternSlotCoordinate slot) => slot.X >= 0 && slot.X < config.CoursePatternGridWidth && slot.Y >= 0 && slot.Y < config.CoursePatternGridHeight;
            private CameraRoomNode Room(string id) => rooms.Single(room => string.Equals(room.RoomId, id, StringComparison.Ordinal));
        }
    }
}
