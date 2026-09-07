using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Pure RUN04 proof over the derived grid. Validation records failed contracts; it never repairs a tile.</summary>
    public sealed class MoonPalaceRunPreviewValidation
    {
        internal MoonPalaceRunPreviewValidation(MoonPalaceRunPreviewGrid grid, bool startOpen, bool exitOpen, bool routeExists, bool framesInside,
            bool connectorsReciprocal, bool connectorTilesOpen, bool destinationsResolvable, bool transitionsReachable, string digest)
        {
            Grid = grid;
            StartOpen = startOpen;
            ExitOpen = exitOpen;
            RouteExists = routeExists;
            AllRoomFramesInside = framesInside;
            AllConnectorsReciprocal = connectorsReciprocal;
            AllConnectorGateTilesOpen = connectorTilesOpen;
            AllConnectorDestinationRoomsResolvable = destinationsResolvable;
            AllConnectorTransitionsReachable = transitionsReachable;
            CanonicalDigest = digest ?? string.Empty;
        }

        public MoonPalaceRunPreviewGrid Grid { get; }
        public bool StartOpen { get; }
        public bool ExitOpen { get; }
        public bool RouteExists { get; }
        public bool AllRoomFramesInside { get; }
        public bool AllConnectorsReciprocal { get; }
        public bool AllConnectorGateTilesOpen { get; }
        public bool AllConnectorDestinationRoomsResolvable { get; }
        public bool AllConnectorTransitionsReachable { get; }
        public bool UsesSectorPrimaryInput => Grid.Config.AllowSectorPrimaryInput;
        public int FallbackCarveCount => Grid.Composition.FallbackCarveCount;
        public int SilentRepairCount => Grid.Composition.SilentRepairCount;
        public bool Passed => StartOpen && ExitOpen && RouteExists && AllRoomFramesInside && AllConnectorsReciprocal && AllConnectorGateTilesOpen && AllConnectorDestinationRoomsResolvable && AllConnectorTransitionsReachable && !UsesSectorPrimaryInput && FallbackCarveCount == 0 && SilentRepairCount == 0;
        public string CanonicalDigest { get; }
    }

    public static class MoonPalaceRunPreviewValidator
    {
        public static MoonPalaceRunPreviewValidation Validate(MoonPalaceRunPreviewGrid grid)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            var startOpen = grid.IsInside(grid.StartTile.x, grid.StartTile.y) && grid.IsOpen(grid.StartTile.x, grid.StartTile.y);
            var exitOpen = grid.IsInside(grid.ExitTile.x, grid.ExitTile.y) && grid.IsOpen(grid.ExitTile.x, grid.ExitTile.y);
            var route = grid.FindRouteToExit();
            var routeExists = route.Count > 0 && route.First().Equals(grid.StartTile) && route.Last().Equals(grid.ExitTile) && route.Zip(route.Skip(1), (from, to) => grid.CanStep(from.x, from.y, to.x, to.y)).All(value => value);
            var framesInside = grid.RoomFrames.Count == grid.Config.ExpectedRoomCount && grid.RoomFrames.All(frame => frame.TileMinX >= 0 && frame.TileMinY >= 0 && frame.TileMinX + frame.TileWidth <= grid.TileWidth && frame.TileMinY + frame.TileHeight <= grid.TileHeight && frame.OrthographicSize > 0f);
            var reciprocal = grid.ConnectorTriggers.Count == grid.Config.ExpectedConnectorCount && grid.ConnectorTriggers.All(trigger => trigger.IsReciprocal && !string.IsNullOrWhiteSpace(trigger.ConnectorId) && !string.Equals(trigger.FromRoomId, trigger.ToRoomId, StringComparison.Ordinal));
            var open = grid.ConnectorTriggers.All(trigger => trigger.IsOpen && grid.IsOpen(trigger.FromGateTile.x, trigger.FromGateTile.y) && grid.IsOpen(trigger.ToGateTile.x, trigger.ToGateTile.y));
            var destinations = grid.ConnectorTriggers.All(trigger => grid.RoomFrames.Any(frame => frame.RoomId == trigger.FromRoomId) && grid.RoomFrames.Any(frame => frame.RoomId == trigger.ToRoomId));
            var reachableCells = ReachableFromStart(grid);
            var reachable = grid.ConnectorTriggers.All(trigger => RouteContainsTransition(trigger, reachableCells));
            var lines = new List<string>
            {
                "RUN04_VALIDATION", grid.CanonicalDigest,
                "start_open=" + Bit(startOpen), "exit_open=" + Bit(exitOpen), "route=" + Bit(routeExists), "frames=" + Bit(framesInside),
                "reciprocal=" + Bit(reciprocal), "gates_open=" + Bit(open), "destinations=" + Bit(destinations), "transitions=" + Bit(reachable),
                "sector_primary=" + Bit(grid.Config.AllowSectorPrimaryInput), "fallback_carve=" + grid.Composition.FallbackCarveCount.ToString(CultureInfo.InvariantCulture), "silent_repair=" + grid.Composition.SilentRepairCount.ToString(CultureInfo.InvariantCulture),
            };
            return new MoonPalaceRunPreviewValidation(grid, startOpen, exitOpen, routeExists, framesInside, reciprocal, open, destinations, reachable, BakingCanonicalDigest.HashCanonicalLines(lines));
        }

        private static bool RouteContainsTransition(MoonPalaceRunPreviewConnectorTrigger trigger, ISet<UnityEngine.Vector2Int> reachableCells)
        {
            return trigger.IsOpen && reachableCells.Contains(trigger.FromGateTile) && reachableCells.Contains(trigger.ToGateTile);
        }

        private static ISet<UnityEngine.Vector2Int> ReachableFromStart(MoonPalaceRunPreviewGrid grid)
        {
            var reached = new HashSet<UnityEngine.Vector2Int>();
            if (!grid.IsOpen(grid.StartTile.x, grid.StartTile.y)) return reached;
            var queue = new Queue<UnityEngine.Vector2Int>();
            queue.Enqueue(grid.StartTile);
            reached.Add(grid.StartTile);
            var directions = new[] { UnityEngine.Vector2Int.right, UnityEngine.Vector2Int.up, UnityEngine.Vector2Int.left, UnityEngine.Vector2Int.down };
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var direction in directions)
                {
                    var next = current + direction;
                    if (grid.CanStep(current.x, current.y, next.x, next.y) && reached.Add(next)) queue.Enqueue(next);
                }
            }
            return reached;
        }

        private static string Bit(bool value) => value ? "1" : "0";
    }
}
