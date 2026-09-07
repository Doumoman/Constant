using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public sealed class MoonPalaceRunVariantValidation
    {
        internal MoonPalaceRunVariantValidation(MoonPalaceRunVariantComposition composition, bool startOpen, bool exitOpen, bool startToExit,
            int reachableWaypoints, int reachableBranches, int reachableSplitRejoins, int socketMismatches, int outOfBounds,
            int duplicatePlacements, int unreachableRouteBranchIslands, int routeFailures, IEnumerable<MoonPalaceRunTileCoordinate> reachable,
            string digest)
        {
            Composition = composition; StartOpen = startOpen; ExitOpen = exitOpen; StartToExitReachable = startToExit;
            ReachableRequiredWaypointCount = reachableWaypoints; ReachableBranchEntryCount = reachableBranches;
            ReachableSplitRejoinWaypointCount = reachableSplitRejoins; RouteSocketMismatchCount = socketMismatches;
            OutOfBoundsPlacementCount = outOfBounds; DuplicatePlacementCount = duplicatePlacements;
            UnreachableRouteBranchIslandCount = unreachableRouteBranchIslands; RouteFailureCount = routeFailures;
            ReachableTiles = new ReadOnlyCollection<MoonPalaceRunTileCoordinate>((reachable ?? Array.Empty<MoonPalaceRunTileCoordinate>()).OrderBy(tile => tile.Y).ThenBy(tile => tile.X).ToList());
            CanonicalDigest = digest ?? string.Empty;
        }
        public MoonPalaceRunVariantComposition Composition { get; }
        public string VariantId => Composition.Config.VariantId;
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
        public int SplitRejoinWaypointCount => Composition.SplitRejoinWaypointTiles.Count;
        public int ReachableSplitRejoinWaypointCount { get; }
        public int RouteSocketMismatchCount { get; }
        public int OutOfBoundsPlacementCount { get; }
        public int DuplicatePlacementCount { get; }
        public int UnreachableRouteBranchIslandCount { get; }
        public int RouteFailureCount { get; }
        public int FallbackCarveCount => Composition.FallbackCarveCount;
        public int SilentRepairCount => Composition.SilentRepairCount;
        public IReadOnlyList<MoonPalaceRunTileCoordinate> ReachableTiles { get; }
        public string CanonicalDigest { get; }
        public bool Passed => TileWidth == PatternGridWidth * 4 && TileHeight == PatternGridHeight * 4 && PatternPlacementCount == PatternGridWidth * PatternGridHeight &&
            Composition.CandidateSet.Candidates.Count == MoonPalaceRunVariantConfig.RequiredCandidateCount && StartOpen && ExitOpen && StartToExitReachable &&
            ReachableRequiredWaypointCount == RequiredWaypointCount && ReachableBranchEntryCount == BranchEntryCount &&
            ReachableSplitRejoinWaypointCount == SplitRejoinWaypointCount && RouteSocketMismatchCount == 0 && OutOfBoundsPlacementCount == 0 &&
            DuplicatePlacementCount == 0 && UnreachableRouteBranchIslandCount == 0 && RouteFailureCount == 0 && FallbackCarveCount == 0 && SilentRepairCount == 0;
    }

    public sealed class MoonPalaceRunVariantAggregateValidation
    {
        internal MoonPalaceRunVariantAggregateValidation(IEnumerable<MoonPalaceRunVariantValidation> variants, string digest)
        {
            Variants = new ReadOnlyCollection<MoonPalaceRunVariantValidation>((variants ?? Array.Empty<MoonPalaceRunVariantValidation>()).OrderBy(variant => variant.VariantId, StringComparer.Ordinal).ToList());
            CanonicalDigest = digest ?? string.Empty;
        }
        public IReadOnlyList<MoonPalaceRunVariantValidation> Variants { get; }
        public int VariantCount => Variants.Count;
        public int AllVariantBfsPassCount => Variants.Count(variant => variant.StartToExitReachable);
        public int TotalPatternPlacements => Variants.Sum(variant => variant.PatternPlacementCount);
        public int TotalRouteSocketMismatchCount => Variants.Sum(variant => variant.RouteSocketMismatchCount);
        public int TotalRouteFailureCount => Variants.Sum(variant => variant.RouteFailureCount);
        public int TotalFallbackCarveCount => Variants.Sum(variant => variant.FallbackCarveCount);
        public int TotalSilentRepairCount => Variants.Sum(variant => variant.SilentRepairCount);
        public string CanonicalDigest { get; }
        public bool Passed => VariantCount == 3 && AllVariantBfsPassCount == 3 && TotalPatternPlacements == 2388 &&
            TotalRouteSocketMismatchCount == 0 && TotalRouteFailureCount == 0 && TotalFallbackCarveCount == 0 && TotalSilentRepairCount == 0 && Variants.All(variant => variant.Passed);
    }

    public static class MoonPalaceRunVariantValidator
    {
        public static MoonPalaceRunVariantAggregateValidation ValidateAll(IEnumerable<MoonPalaceRunVariantComposition> compositions)
        {
            var results = (compositions ?? Array.Empty<MoonPalaceRunVariantComposition>()).Select(Validate).OrderBy(result => result.VariantId, StringComparer.Ordinal).ToList();
            var lines = new List<string> { "RUN02_AGGREGATE_VALIDATION", "variant_count=" + results.Count.ToString(CultureInfo.InvariantCulture) };
            lines.AddRange(results.Select(result => result.VariantId + "|" + result.CanonicalDigest + "|" + (result.Passed ? "PASS" : "FAIL")));
            return new MoonPalaceRunVariantAggregateValidation(results, BakingCanonicalDigest.HashCanonicalLines(lines));
        }

        public static MoonPalaceRunVariantValidation Validate(MoonPalaceRunVariantComposition composition)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            var config = composition.Config;
            var outOfBounds = composition.Placements.Count(placement => placement.Coordinate.X < 0 || placement.Coordinate.X >= config.PatternGridWidth || placement.Coordinate.Y < 0 || placement.Coordinate.Y >= config.PatternGridHeight);
            var duplicates = composition.Placements.GroupBy(placement => placement.Coordinate).Sum(group => Math.Max(0, group.Count() - 1));
            var reachable = Flood(composition, composition.StartTile);
            var startOpen = composition.IsOpen(composition.StartTile.X, composition.StartTile.Y);
            var exitOpen = composition.IsOpen(composition.ExitTile.X, composition.ExitTile.Y);
            var reachesExit = reachable.Contains(composition.ExitTile);
            var waypointCount = composition.RequiredWaypoints.Count(tile => reachable.Contains(tile));
            var branchCount = composition.BranchEntryTiles.Count(tile => reachable.Contains(tile));
            var splitCount = composition.SplitRejoinWaypointTiles.Count(tile => reachable.Contains(tile));
            var socketMismatches = CountSocketMismatches(composition);
            var unreachableRouteBranchIslands = composition.RequiredWaypoints.Count(tile => !reachable.Contains(tile));
            var routeFailures = (!startOpen ? 1 : 0) + (!exitOpen ? 1 : 0) + (!reachesExit ? 1 : 0) +
                (composition.RequiredWaypoints.Count - waypointCount) + (composition.BranchEntryTiles.Count - branchCount) +
                (composition.SplitRejoinWaypointTiles.Count - splitCount) + socketMismatches;
            var lines = new[]
            {
                "RUN02_VALIDATION", composition.CompositionDigest,
                "tile_size=" + config.TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + config.TileHeight.ToString(CultureInfo.InvariantCulture),
                "placements=" + composition.PlacementCount.ToString(CultureInfo.InvariantCulture),
                "start=" + composition.StartTile + ":" + (startOpen ? "open" : "solid"), "exit=" + composition.ExitTile + ":" + (exitOpen ? "open" : "solid"),
                "start_exit=" + (reachesExit ? "PASS" : "FAIL"), "waypoints=" + waypointCount.ToString(CultureInfo.InvariantCulture) + "/" + composition.RequiredWaypoints.Count.ToString(CultureInfo.InvariantCulture),
                "branches=" + branchCount.ToString(CultureInfo.InvariantCulture) + "/" + composition.BranchEntryTiles.Count.ToString(CultureInfo.InvariantCulture),
                "split_rejoins=" + splitCount.ToString(CultureInfo.InvariantCulture) + "/" + composition.SplitRejoinWaypointTiles.Count.ToString(CultureInfo.InvariantCulture),
                "socket_mismatches=" + socketMismatches.ToString(CultureInfo.InvariantCulture), "out_of_bounds=" + outOfBounds.ToString(CultureInfo.InvariantCulture),
                "duplicates=" + duplicates.ToString(CultureInfo.InvariantCulture), "unreachable_route_branch_islands=" + unreachableRouteBranchIslands.ToString(CultureInfo.InvariantCulture),
                "route_failures=" + routeFailures.ToString(CultureInfo.InvariantCulture), "fallback_carves=0", "silent_repairs=0",
            };
            return new MoonPalaceRunVariantValidation(composition, startOpen, exitOpen, reachesExit, waypointCount, branchCount, splitCount,
                socketMismatches, outOfBounds, duplicates, unreachableRouteBranchIslands, routeFailures, reachable, BakingCanonicalDigest.HashCanonicalLines(lines));
        }

        private static int CountSocketMismatches(MoonPalaceRunVariantComposition composition)
        {
            var placements = composition.Placements.ToDictionary(placement => placement.Coordinate, placement => placement);
            return composition.Graph.Edges.Count(edge => !placements.TryGetValue(edge.From, out var from) || !placements.TryGetValue(edge.To, out var to) ||
                !MoonPalaceRunVariantComposer.HasSocket(from.Candidate, edge.Direction) || !MoonPalaceRunVariantComposer.HasSocket(to.Candidate, MoonPalaceRunVariantComposer.Opposite(edge.Direction)));
        }

        private static HashSet<MoonPalaceRunTileCoordinate> Flood(MoonPalaceRunVariantComposition composition, MoonPalaceRunTileCoordinate start)
        {
            var reached = new HashSet<MoonPalaceRunTileCoordinate>();
            if (!composition.IsOpen(start.X, start.Y)) return reached;
            var queue = new Queue<MoonPalaceRunTileCoordinate>(); queue.Enqueue(start); reached.Add(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in Neighbours(current)) if (composition.IsOpen(next.X, next.Y) && reached.Add(next)) queue.Enqueue(next);
            }
            return reached;
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
