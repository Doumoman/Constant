using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public sealed class MoonPalaceMicroPatternRunValidation
    {
        internal MoonPalaceMicroPatternRunValidation(MoonPalaceMicroPatternRunComposition composition, bool startOpen, bool exitOpen,
            bool startToExitReachable, int reachableWaypoints, int reachableBranchEntries, int routeSocketMismatches,
            int outOfBoundsPlacements, int duplicatePlacements, int unreachableOpenIslands, int routeFailures,
            IEnumerable<MoonPalaceRunTileCoordinate> reachableTiles, string digest)
        {
            Composition = composition; StartOpen = startOpen; ExitOpen = exitOpen; StartToExitReachable = startToExitReachable;
            ReachableRequiredWaypointCount = reachableWaypoints; ReachableBranchEntryCount = reachableBranchEntries;
            RouteSocketMismatchCount = routeSocketMismatches; OutOfBoundsPlacementCount = outOfBoundsPlacements;
            DuplicatePlacementCount = duplicatePlacements; UnreachableOpenIslandCount = unreachableOpenIslands; RouteFailureCount = routeFailures;
            ReachableTiles = new ReadOnlyCollection<MoonPalaceRunTileCoordinate>((reachableTiles ?? Array.Empty<MoonPalaceRunTileCoordinate>()).OrderBy(tile => tile.Y).ThenBy(tile => tile.X).ToList());
            CanonicalDigest = digest;
        }
        public MoonPalaceMicroPatternRunComposition Composition { get; }
        public int TileWidth => Composition.Config.TileWidth;
        public int TileHeight => Composition.Config.TileHeight;
        public int PatternGridWidth => Composition.Config.PatternGridWidth;
        public int PatternGridHeight => Composition.Config.PatternGridHeight;
        public int PatternPlacementCount => Composition.PlacementCount;
        public MoonPalaceRunTileCoordinate StartTile => Composition.StartTile;
        public MoonPalaceRunTileCoordinate ExitTile => Composition.ExitTile;
        public bool StartOpen { get; }
        public bool ExitOpen { get; }
        public bool StartToExitReachable { get; }
        public int RequiredWaypointCount => Composition.RequiredWaypoints.Count;
        public int ReachableRequiredWaypointCount { get; }
        public int BranchEntryCount => Composition.BranchEntryTiles.Count;
        public int ReachableBranchEntryCount { get; }
        public int RouteSocketMismatchCount { get; }
        public int OutOfBoundsPlacementCount { get; }
        public int DuplicatePlacementCount { get; }
        public int UnreachableOpenIslandCount { get; }
        public int RouteFailureCount { get; }
        public int FallbackCarveCount => Composition.FallbackCarveCount;
        public int SilentRepairCount => Composition.SilentRepairCount;
        public IReadOnlyList<MoonPalaceRunTileCoordinate> ReachableTiles { get; }
        public string CanonicalDigest { get; }
        public bool Passed => TileWidth == 160 && TileHeight == 48 && PatternGridWidth == 40 && PatternGridHeight == 12 &&
            PatternPlacementCount == 480 && StartOpen && ExitOpen && StartToExitReachable &&
            ReachableRequiredWaypointCount == RequiredWaypointCount && ReachableBranchEntryCount == BranchEntryCount &&
            RouteSocketMismatchCount == 0 && OutOfBoundsPlacementCount == 0 && DuplicatePlacementCount == 0 &&
            RouteFailureCount == 0 && FallbackCarveCount == 0 && SilentRepairCount == 0;
    }

    public static class MoonPalaceMicroPatternRunValidator
    {
        public static MoonPalaceMicroPatternRunValidation Validate(MoonPalaceMicroPatternRunComposition composition)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            var config = composition.Config;
            var outOfBounds = composition.Placements.Count(placement => placement.Coordinate.X < 0 || placement.Coordinate.X >= config.PatternGridWidth ||
                placement.Coordinate.Y < 0 || placement.Coordinate.Y >= config.PatternGridHeight);
            var duplicates = composition.Placements.GroupBy(placement => placement.Coordinate).Sum(group => Math.Max(0, group.Count() - 1));
            var reached = Flood(composition, composition.StartTile);
            var startOpen = composition.IsOpen(composition.StartTile.X, composition.StartTile.Y);
            var exitOpen = composition.IsOpen(composition.ExitTile.X, composition.ExitTile.Y);
            var reachExit = reached.Contains(composition.ExitTile);
            var waypointCount = composition.RequiredWaypoints.Count(tile => reached.Contains(tile));
            var branchCount = composition.BranchEntryTiles.Count(tile => reached.Contains(tile));
            var mismatchCount = CountSocketMismatches(composition);
            var openIslands = CountUnreachableOpenIslands(composition, reached);
            var routeFailures = (!startOpen ? 1 : 0) + (!exitOpen ? 1 : 0) + (!reachExit ? 1 : 0) +
                (composition.RequiredWaypoints.Count - waypointCount) + (composition.BranchEntryTiles.Count - branchCount) + mismatchCount;
            var lines = new[]
            {
                "RUN01_VALIDATION", composition.CompositionDigest,
                "tile_size=" + config.TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + config.TileHeight.ToString(CultureInfo.InvariantCulture),
                "placements=" + composition.PlacementCount.ToString(CultureInfo.InvariantCulture),
                "start=" + composition.StartTile + ":" + (startOpen ? "open" : "solid"),
                "exit=" + composition.ExitTile + ":" + (exitOpen ? "open" : "solid"),
                "start_exit=" + (reachExit ? "PASS" : "FAIL"),
                "waypoints=" + waypointCount.ToString(CultureInfo.InvariantCulture) + "/" + composition.RequiredWaypoints.Count.ToString(CultureInfo.InvariantCulture),
                "branches=" + branchCount.ToString(CultureInfo.InvariantCulture) + "/" + composition.BranchEntryTiles.Count.ToString(CultureInfo.InvariantCulture),
                "socket_mismatches=" + mismatchCount.ToString(CultureInfo.InvariantCulture),
                "out_of_bounds=" + outOfBounds.ToString(CultureInfo.InvariantCulture),
                "duplicates=" + duplicates.ToString(CultureInfo.InvariantCulture),
                "unreachable_open_islands=" + openIslands.ToString(CultureInfo.InvariantCulture),
                "route_failures=" + routeFailures.ToString(CultureInfo.InvariantCulture),
                "fallback_carves=0", "silent_repairs=0",
            };
            return new MoonPalaceMicroPatternRunValidation(composition, startOpen, exitOpen, reachExit, waypointCount, branchCount,
                mismatchCount, outOfBounds, duplicates, openIslands, routeFailures, reached, BakingCanonicalDigest.HashCanonicalLines(lines));
        }

        private static int CountSocketMismatches(MoonPalaceMicroPatternRunComposition composition)
        {
            var placements = composition.Placements.ToDictionary(placement => placement.Coordinate, placement => placement);
            var mismatch = 0;
            foreach (var edge in composition.Graph.Edges)
            {
                if (!placements.TryGetValue(edge.From, out var from) || !placements.TryGetValue(edge.To, out var to) ||
                    !MoonPalaceMicroPatternRunComposer.HasSocket(from.Candidate, edge.Direction) ||
                    !MoonPalaceMicroPatternRunComposer.HasSocket(to.Candidate, MoonPalaceMicroPatternRunComposer.Opposite(edge.Direction))) mismatch++;
            }
            return mismatch;
        }

        private static HashSet<MoonPalaceRunTileCoordinate> Flood(MoonPalaceMicroPatternRunComposition composition,
            MoonPalaceRunTileCoordinate start)
        {
            var reached = new HashSet<MoonPalaceRunTileCoordinate>();
            if (!composition.IsOpen(start.X, start.Y)) return reached;
            var queue = new Queue<MoonPalaceRunTileCoordinate>();
            queue.Enqueue(start); reached.Add(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in Neighbours(current))
                {
                    if (!composition.IsOpen(next.X, next.Y) || !reached.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }
            return reached;
        }

        private static int CountUnreachableOpenIslands(MoonPalaceMicroPatternRunComposition composition,
            ISet<MoonPalaceRunTileCoordinate> reachable)
        {
            var visited = new HashSet<MoonPalaceRunTileCoordinate>(reachable);
            var islands = 0;
            for (var y = 0; y < composition.Config.TileHeight; y++)
            for (var x = 0; x < composition.Config.TileWidth; x++)
            {
                var start = new MoonPalaceRunTileCoordinate(x, y);
                if (!composition.IsOpen(x, y) || !visited.Add(start)) continue;
                islands++;
                var queue = new Queue<MoonPalaceRunTileCoordinate>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var next in Neighbours(current))
                    {
                        if (!composition.IsOpen(next.X, next.Y) || !visited.Add(next)) continue;
                        queue.Enqueue(next);
                    }
                }
            }
            return islands;
        }

        private static IEnumerable<MoonPalaceRunTileCoordinate> Neighbours(MoonPalaceRunTileCoordinate coordinate)
        {
            yield return new MoonPalaceRunTileCoordinate(coordinate.X + 1, coordinate.Y);
            yield return new MoonPalaceRunTileCoordinate(coordinate.X - 1, coordinate.Y);
            yield return new MoonPalaceRunTileCoordinate(coordinate.X, coordinate.Y + 1);
            yield return new MoonPalaceRunTileCoordinate(coordinate.X, coordinate.Y - 1);
        }
    }
}
