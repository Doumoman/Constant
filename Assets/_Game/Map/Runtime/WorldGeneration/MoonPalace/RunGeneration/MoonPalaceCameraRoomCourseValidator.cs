using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Tile-open-cell proof only; it deliberately does not claim player-physics traversal.</summary>
    public sealed class MoonPalaceCameraRoomCourseValidation
    {
        internal MoonPalaceCameraRoomCourseValidation(MoonPalaceCameraRoomPatternComposition composition, bool startOpen, bool exitOpen,
            bool startToExit, bool boundsInside, bool gatesReciprocal, bool gatesOpen, bool mainInOrder, bool branchesReachable,
            bool splitsReachable, int unreachableRooms, int mismatchCount, int blockedCount, string digest)
        {
            Composition = composition; StartOpen = startOpen; ExitOpen = exitOpen; StartToExitReachable = startToExit;
            AllRoomBoundsInside = boundsInside; AllConnectorGatesReciprocal = gatesReciprocal; AllConnectorGatesOpen = gatesOpen;
            AllMainRouteRoomsReachableInOrder = mainInOrder; AllBranchEntriesReachable = branchesReachable; AllSplitRejoinPathsReachable = splitsReachable;
            UnreachableRequiredRoomCount = unreachableRooms; RouteSocketMismatchCount = mismatchCount; ConnectorBlockedCount = blockedCount;
            CanonicalDigest = digest ?? string.Empty;
        }
        public MoonPalaceCameraRoomPatternComposition Composition { get; }
        public bool StartOpen { get; }
        public bool ExitOpen { get; }
        public bool StartToExitReachable { get; }
        public bool AllRoomBoundsInside { get; }
        public bool AllConnectorGatesReciprocal { get; }
        public bool AllConnectorGatesOpen { get; }
        public bool AllMainRouteRoomsReachableInOrder { get; }
        public bool AllBranchEntriesReachable { get; }
        public bool AllSplitRejoinPathsReachable { get; }
        public int UnreachableRequiredRoomCount { get; }
        public int RouteSocketMismatchCount { get; }
        public int ConnectorBlockedCount { get; }
        public int FallbackCarveCount => Composition.FallbackCarveCount;
        public int SilentRepairCount => Composition.SilentRepairCount;
        public int PatternPlacementCount => Composition.PlacementCount;
        public string CanonicalDigest { get; }
        public bool Passed => StartOpen && ExitOpen && StartToExitReachable && AllRoomBoundsInside && AllConnectorGatesReciprocal && AllConnectorGatesOpen &&
            AllMainRouteRoomsReachableInOrder && AllBranchEntriesReachable && AllSplitRejoinPathsReachable && UnreachableRequiredRoomCount == 0 &&
            RouteSocketMismatchCount == 0 && ConnectorBlockedCount == 0 && FallbackCarveCount == 0 && SilentRepairCount == 0;
    }

    public static class MoonPalaceCameraRoomCourseValidator
    {
        public static MoonPalaceCameraRoomCourseValidation Validate(MoonPalaceCameraRoomPatternComposition composition)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            var graph = composition.Graph; var reachable = Bfs(composition, composition.StartTile);
            var startOpen = composition.IsOpen(composition.StartTile.X, composition.StartTile.Y);
            var exitOpen = composition.IsOpen(composition.ExitTile.X, composition.ExitTile.Y);
            var startToExit = reachable.Contains(composition.ExitTile);
            var boundsInside = graph.Rooms.All(room => room.Bounds.IsInside(composition.Config.CoursePatternGridWidth, composition.Config.CoursePatternGridHeight));
            var reciprocal = composition.Connectors.Gates.All(gate => MoonPalaceCameraRoomConnectorPlanner.IsReciprocal(graph, gate));
            var blocked = composition.Connectors.Gates.Count(gate => !RectOpen(composition, gate.FromTileGateRect) || !RectOpen(composition, gate.ToTileGateRect));
            var mainInOrder = MainRoomsInOrder(graph, reachable, composition.Config);
            var branches = graph.BranchRoutes.All(route => reachable.Contains(Center(route.Entry, composition.Config)));
            var splits = graph.SplitRejoinRoutes.All(route => route.AlternateSlots.Concat(new[] { route.Split, route.Rejoin }).All(slot => reachable.Contains(Center(slot, composition.Config))));
            var mismatch = graph.Edges.Count(edge => !MoonPalaceCameraRoomPatternComposer.HasSocket(composition.Placement(edge.From).Candidate, edge.Direction) ||
                !MoonPalaceCameraRoomPatternComposer.HasSocket(composition.Placement(edge.To).Candidate, MoonPalaceCameraRoomConnectorPlanner.Opposite(edge.Direction)));
            var unreachableRooms = graph.Rooms.Count(room => !graph.RouteSlots.Where(room.Bounds.Contains).Select(slot => Center(slot, composition.Config)).Any(reachable.Contains));
            var lines = new[]
            {
                "RUN03_VALIDATION", composition.CompositionDigest,
                "tile_size=" + composition.Config.TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + composition.Config.TileHeight.ToString(CultureInfo.InvariantCulture),
                "placements=" + composition.PlacementCount.ToString(CultureInfo.InvariantCulture),
                "start_open=" + Bit(startOpen), "exit_open=" + Bit(exitOpen), "start_exit=" + Bit(startToExit), "bounds=" + Bit(boundsInside),
                "gates_reciprocal=" + Bit(reciprocal), "gates_open=" + Bit(blocked == 0), "main_order=" + Bit(mainInOrder), "branches=" + Bit(branches), "splits=" + Bit(splits),
                "unreachable_rooms=" + unreachableRooms.ToString(CultureInfo.InvariantCulture), "socket_mismatch=" + mismatch.ToString(CultureInfo.InvariantCulture),
                "connector_blocked=" + blocked.ToString(CultureInfo.InvariantCulture), "fallback_carve=0", "silent_repair=0",
            };
            return new MoonPalaceCameraRoomCourseValidation(composition, startOpen, exitOpen, startToExit, boundsInside, reciprocal, blocked == 0,
                mainInOrder, branches, splits, unreachableRooms, mismatch, blocked, BakingCanonicalDigest.HashCanonicalLines(lines));
        }

        private static HashSet<MoonPalaceRunTileCoordinate> Bfs(MoonPalaceCameraRoomPatternComposition composition, MoonPalaceRunTileCoordinate start)
        {
            var visited = new HashSet<MoonPalaceRunTileCoordinate>();
            if (!composition.IsOpen(start.X, start.Y)) return visited;
            var queue = new Queue<MoonPalaceRunTileCoordinate>(); queue.Enqueue(start); visited.Add(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in new[] { new MoonPalaceRunTileCoordinate(current.X + 1, current.Y), new MoonPalaceRunTileCoordinate(current.X - 1, current.Y), new MoonPalaceRunTileCoordinate(current.X, current.Y + 1), new MoonPalaceRunTileCoordinate(current.X, current.Y - 1) })
                    if (composition.IsOpen(next.X, next.Y) && visited.Add(next)) queue.Enqueue(next);
            }
            return visited;
        }
        private static bool MainRoomsInOrder(MoonPalaceCameraRoomGraph graph, ISet<MoonPalaceRunTileCoordinate> reachable, MoonPalaceCameraRoomCourseConfig config)
        {
            var priorLastIndex = -1;
            foreach (var roomId in graph.MainRoute.RoomIds)
            {
                var room = graph.Room(roomId);
                var indexes = graph.MainRoute.PatternSlots.Select((slot, index) => new { slot, index }).Where(item => room.Bounds.Contains(item.slot)).ToList();
                if (indexes.Count == 0 || indexes[0].index < priorLastIndex || indexes.Any(item => !reachable.Contains(Center(item.slot, config)))) return false;
                priorLastIndex = indexes[indexes.Count - 1].index;
            }
            return true;
        }
        private static MoonPalaceRunTileCoordinate Center(PatternSlotCoordinate slot, MoonPalaceCameraRoomCourseConfig config) => new MoonPalaceRunTileCoordinate(slot.X * config.PatternWidth + 1, slot.Y * config.PatternHeight + 1);
        private static bool RectOpen(MoonPalaceCameraRoomPatternComposition composition, CameraRoomTileGateRect rect)
        {
            if (rect == null) return false;
            for (var y = rect.Y; y < rect.Y + rect.Height; y++) for (var x = rect.X; x < rect.X + rect.Width; x++) if (!composition.IsOpen(x, y)) return false;
            return true;
        }
        private static string Bit(bool value) => value ? "1" : "0";
    }
}
