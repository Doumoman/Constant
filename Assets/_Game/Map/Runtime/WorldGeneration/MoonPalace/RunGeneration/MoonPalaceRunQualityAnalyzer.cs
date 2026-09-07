using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Measures an already-generated course without changing any placement, tile, connector, or validation result.</summary>
    public static class MoonPalaceRunQualityAnalyzer
    {
        public static MoonPalaceRunQualityAnalysis Analyze(MoonPalaceSeededRunResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var profile = MoonPalaceRunQualityProfileCatalog.Get(result.Recipe.RecipeId);
            var mainRooms = result.Rooms.Where(room => room.IsMain).OrderBy(room => room.RouteOrder).ToList();
            var mainEdges = result.Edges.Where(edge => edge.Ownership == "MAIN").ToList();
            var branchLengths = result.Edges.Where(edge => edge.Ownership == "BRANCH").GroupBy(edge => edge.OwnerId, StringComparer.Ordinal).Select(group => new { Id = group.Key, Length = group.Count() }).OrderBy(value => value.Id, StringComparer.Ordinal).ToList();
            var splitLengths = result.Edges.Where(edge => edge.Ownership == "SPLIT_REJOIN").GroupBy(edge => edge.OwnerId, StringComparer.Ordinal).Select(group => new { Id = group.Key, Length = group.Count() }).OrderBy(value => value.Id, StringComparer.Ordinal).ToList();
            var maxSameRow = MaxSameRow(mainRooms);
            var openRatio = (double)result.OpenCellCount / (result.TileWidth * result.TileHeight);
            var noise = IsolatedOpenFragments(result);
            var minimumBranch = branchLengths.Count == 0 ? 0 : branchLengths.Min(value => value.Length);
            var maxBranchRatio = mainEdges.Count == 0 || branchLengths.Count == 0 ? 0d : branchLengths.Max(value => (double)value.Length / mainEdges.Count);
            var connectorSpacing = MinimumConnectorSpacing(result);
            var verticalSpan = mainRooms.Count == 0 ? 0 : mainRooms.Max(room => CenterY(room)) - mainRooms.Min(room => CenterY(room));
            var splitSeparation = splitLengths.Count == 0 ? 0 : splitLengths.Min(value => value.Length / 2);
            var unreachableRequired = UnreachableRequiredTileCount(result);
            var repairCount = result.FallbackCarveCount + result.SilentRepairCount + result.RotationCount;
            var findings = new List<MoonPalaceRunQualityFinding>
            {
                Finding(MoonPalaceRunQualityRule.RouteTooStraight, maxSameRow > profile.MaxSameRowMainRooms, maxSameRow, profile.MaxSameRowMainRooms, mainRooms.Where(room => CenterY(room) == DominantRow(mainRooms)).Select(room => room.RoomId), maxSameRow > profile.MaxSameRowMainRooms ? "Main route keeps " + maxSameRow.ToString(CultureInfo.InvariantCulture) + " consecutive room frames on one row; the profile allows " + profile.MaxSameRowMainRooms.ToString(CultureInfo.InvariantCulture) + "." : "Main-route row repetition stays within the recipe profile."),
                Finding(MoonPalaceRunQualityRule.RoomTooEmpty, openRatio > profile.MaxOpenRatio, openRatio, profile.MaxOpenRatio, result.Rooms.Select(room => room.RoomId), openRatio > profile.MaxOpenRatio ? "Open-tile ratio exceeds the readable-room threshold." : "Open-tile ratio remains below the hollow-room threshold."),
                Finding(MoonPalaceRunQualityRule.RoomTooNoisy, noise > profile.MaxNoiseFragments, noise, profile.MaxNoiseFragments, result.Placements.Where(placement => placement.RouteOwnership == "FILLER_DETAIL").Take(8).Select(placement => placement.RoomId), noise > profile.MaxNoiseFragments ? "Isolated open fragments exceed the visual-noise threshold." : "Isolated open fragments remain within the visual-noise threshold."),
                Finding(MoonPalaceRunQualityRule.BranchTooShort, minimumBranch < profile.MinBranchLength, minimumBranch, profile.MinBranchLength, branchLengths.Where(value => value.Length == minimumBranch).Select(value => value.Id), minimumBranch < profile.MinBranchLength ? "A branch has too few direct pattern edges to read as a destination." : "Every branch has the minimum readable direct-route length."),
                Finding(MoonPalaceRunQualityRule.BranchTooDeep, maxBranchRatio > profile.MaxBranchDepthRatio, maxBranchRatio, profile.MaxBranchDepthRatio, branchLengths.Where(value => mainEdges.Count > 0 && (double)value.Length / mainEdges.Count == maxBranchRatio).Select(value => value.Id), maxBranchRatio > profile.MaxBranchDepthRatio ? "A branch consumes too much of the main-route length." : "Branch depth stays subordinate to the main route."),
                Finding(MoonPalaceRunQualityRule.ConnectorCrowding, connectorSpacing < profile.MinConnectorSpacing, connectorSpacing, profile.MinConnectorSpacing, CrowdedConnectorIds(result, connectorSpacing), connectorSpacing < profile.MinConnectorSpacing ? "Connector gates in one room are too close to read distinctly." : "Connector gate spacing remains readable."),
                Finding(MoonPalaceRunQualityRule.VerticalVariationLow, verticalSpan < profile.MinVerticalSpan, verticalSpan, profile.MinVerticalSpan, mainRooms.Select(room => room.RoomId), verticalSpan < profile.MinVerticalSpan ? "Main-room vertical span is below the recipe's required variation." : "Main-room vertical span meets the recipe profile."),
                Finding(MoonPalaceRunQualityRule.BacktrackShapeBad, splitSeparation < profile.MinSplitSeparation, splitSeparation, profile.MinSplitSeparation, splitLengths.Where(value => value.Length / 2 == splitSeparation).Select(value => value.Id), splitSeparation < profile.MinSplitSeparation ? "Split/rejoin detour is too short to read as a deliberate alternate path." : "Split/rejoin detour separation remains legible."),
                Finding(MoonPalaceRunQualityRule.OpenIslandRequired, unreachableRequired > 0, unreachableRequired, 0, RequiredIds(result), unreachableRequired > 0 ? "A required route, connector, or room-center tile is unreachable from start." : "Every required route, connector, and room-center tile is reachable from start."),
                Finding(MoonPalaceRunQualityRule.RepairPolicyViolation, repairCount > 0, repairCount, 0, Array.Empty<string>(), repairCount > 0 ? "Fallback carve, silent repair, or rotation was recorded." : "No fallback carve, silent repair, or rotation was recorded."),
            };
            return new MoonPalaceRunQualityAnalysis(result, profile, findings);
        }

        private static MoonPalaceRunQualityFinding Finding(string id, bool rejected, double measured, double threshold, IEnumerable<string> ids, string reason) => new MoonPalaceRunQualityFinding(id, rejected ? MoonPalaceRunQualitySeverity.Reject : MoonPalaceRunQualitySeverity.Info, measured, threshold, ids, reason);
        private static int CenterY(MoonPalaceSeededRunRoomRecord room) => (room.MinPatternY + room.MaxPatternY) / 2;
        private static int DominantRow(IEnumerable<MoonPalaceSeededRunRoomRecord> rooms) => rooms.GroupBy(CenterY).OrderByDescending(group => group.Count()).ThenBy(group => group.Key).First().Key;
        private static int MaxSameRow(IReadOnlyList<MoonPalaceSeededRunRoomRecord> rooms) { var max = 0; var current = 0; var prior = int.MinValue; foreach (var room in rooms) { var row = CenterY(room); current = row == prior ? current + 1 : 1; prior = row; max = Math.Max(max, current); } return max; }
        private static int IsolatedOpenFragments(MoonPalaceSeededRunResult result)
        {
            var count = 0; var directions = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
            for (var y = 0; y < result.TileHeight; y++) for (var x = 0; x < result.TileWidth; x++) if (result.IsOpen(x, y) && directions.All(direction => !result.IsOpen(x + direction.x, y + direction.y))) count++;
            return count;
        }
        private static int MinimumConnectorSpacing(MoonPalaceSeededRunResult result)
        {
            var values = new List<int>();
            foreach (var room in result.Rooms)
            {
                var gates = result.Connectors.Where(connector => connector.FromRoomId == room.RoomId).Select(connector => connector.FromGateTile).Concat(result.Connectors.Where(connector => connector.ToRoomId == room.RoomId).Select(connector => connector.ToGateTile)).ToList();
                for (var first = 0; first < gates.Count; first++) for (var second = first + 1; second < gates.Count; second++) values.Add(Mathf.Abs(gates[first].x - gates[second].x) + Mathf.Abs(gates[first].y - gates[second].y));
            }
            return values.Count == 0 ? int.MaxValue : values.Min();
        }
        private static IEnumerable<string> CrowdedConnectorIds(MoonPalaceSeededRunResult result, int spacing) => result.Connectors.Where(connector => result.Connectors.Where(other => other != connector).Any(other => Manhattan(connector.FromGateTile, other.FromGateTile) == spacing || Manhattan(connector.ToGateTile, other.ToGateTile) == spacing)).Select(connector => connector.ConnectorId);
        private static int Manhattan(Vector2Int first, Vector2Int second) => Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y);
        private static int UnreachableRequiredTileCount(MoonPalaceSeededRunResult result)
        {
            var reachable = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>(); if (result.IsOpen(result.StartTile.x, result.StartTile.y)) { queue.Enqueue(result.StartTile); reachable.Add(result.StartTile); }
            var directions = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
            while (queue.Count > 0) { var current = queue.Dequeue(); foreach (var direction in directions) { var next = current + direction; if (result.CanStep(current.x, current.y, next.x, next.y) && reachable.Add(next)) queue.Enqueue(next); } }
            var required = new List<Vector2Int> { result.StartTile, result.ExitTile };
            required.AddRange(result.Connectors.SelectMany(connector => new[] { connector.FromGateTile, connector.ToGateTile }));
            required.AddRange(result.Rooms.Where(room => room.IsMain).Select(room => result.Edges.Where(edge => edge.Ownership == "MAIN" && room.Contains(edge.From)).Select(edge => new Vector2Int(edge.From.X * 4 + 1, edge.From.Y * 4 + 1)).FirstOrDefault()));
            required.AddRange(result.Edges.Where(edge => edge.Ownership != "FILLER_DETAIL").Select(edge => new Vector2Int(edge.From.X * 4 + 1, edge.From.Y * 4 + 1)));
            return required.Distinct().Count(tile => !reachable.Contains(tile));
        }
        private static IEnumerable<string> RequiredIds(MoonPalaceSeededRunResult result) => result.Rooms.Where(room => room.IsMain).Select(room => room.RoomId).Concat(result.Connectors.Select(connector => connector.ConnectorId));
    }
}
