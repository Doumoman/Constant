using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Pure proof of a complete RUN05 course. Failure becomes an explicit result reason; no tile mutation occurs here.</summary>
    public sealed class MoonPalaceSeededRunValidation
    {
        internal MoonPalaceSeededRunValidation(MoonPalaceSeededRunResult result, bool startOpen, bool exitOpen, bool bfs, bool bounds, bool reciprocal, bool gatesOpen, bool main, bool branches, bool splits, string failureReason, string digest)
        {
            Result = result; StartOpen = startOpen; ExitOpen = exitOpen; StartToExitReachable = bfs; AllRoomBoundsInside = bounds; AllConnectorsReciprocal = reciprocal; AllConnectorGateTilesOpen = gatesOpen; AllMainRoomsReachableInOrder = main; AllBranchEntriesReachable = branches; AllSplitRejoinPathsReachable = splits; FailureReason = failureReason ?? string.Empty; CanonicalDigest = digest ?? string.Empty;
        }
        public MoonPalaceSeededRunResult Result { get; }
        public bool StartOpen { get; }
        public bool ExitOpen { get; }
        public bool StartToExitReachable { get; }
        public bool AllRoomBoundsInside { get; }
        public bool AllConnectorsReciprocal { get; }
        public bool AllConnectorGateTilesOpen { get; }
        public bool AllMainRoomsReachableInOrder { get; }
        public bool AllBranchEntriesReachable { get; }
        public bool AllSplitRejoinPathsReachable { get; }
        public int FallbackCarveCount => Result.FallbackCarveCount;
        public int SilentRepairCount => Result.SilentRepairCount;
        public int RotationCount => Result.RotationCount;
        public string FailureReason { get; }
        public string CanonicalDigest { get; }
        public bool Passed => StartOpen && ExitOpen && StartToExitReachable && AllRoomBoundsInside && AllConnectorsReciprocal && AllConnectorGateTilesOpen && AllMainRoomsReachableInOrder && AllBranchEntriesReachable && AllSplitRejoinPathsReachable && FallbackCarveCount == 0 && SilentRepairCount == 0 && RotationCount == 0 && string.IsNullOrEmpty(FailureReason);
    }

    public static class MoonPalaceSeededRunValidator
    {
        public static MoonPalaceSeededRunValidation Validate(MoonPalaceSeededRunResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var reached = Reachable(result);
            var startOpen = result.IsOpen(result.StartTile.x, result.StartTile.y);
            var exitOpen = result.IsOpen(result.ExitTile.x, result.ExitTile.y);
            var bfs = reached.Contains(result.ExitTile) && result.FindRouteToExit().Count > 0;
            var bounds = result.Rooms.All(room => room.MinPatternX >= 0 && room.MinPatternY >= 0 && room.MaxPatternX < result.Recipe.PatternGridWidth && room.MaxPatternY < result.Recipe.PatternGridHeight && room.PatternWidth > 0 && room.PatternHeight > 0);
            var reciprocal = result.Connectors.All(connector => connector.IsReciprocal && result.GetRoomId(connector.FromGateTile.x, connector.FromGateTile.y) == connector.FromRoomId && result.GetRoomId(connector.ToGateTile.x, connector.ToGateTile.y) == connector.ToRoomId);
            var gatesOpen = result.Connectors.All(connector => connector.IsOpen && result.IsOpen(connector.FromGateTile.x, connector.FromGateTile.y) && result.IsOpen(connector.ToGateTile.x, connector.ToGateTile.y) && reached.Contains(connector.FromGateTile) && reached.Contains(connector.ToGateTile));
            var mainRooms = result.Rooms.Where(room => room.IsMain).OrderBy(room => room.RouteOrder).ToList();
            var main = mainRooms.Select(room => result.Edges.Where(edge => edge.Ownership == "MAIN" && room.Contains(edge.From)).Select(edge => Center(edge.From)).FirstOrDefault()).All(tile => reached.Contains(tile));
            var branches = result.Connectors.Where(connector => connector.Kind == "branch").All(connector => reached.Contains(connector.FromGateTile) && reached.Contains(connector.ToGateTile) && result.Rooms.Any(room => room.RoomId == connector.ToRoomId && room.Role == MoonPalaceSeededRunRoomRole.Branch));
            var splits = result.Edges.Where(edge => edge.Ownership == "SPLIT_REJOIN").SelectMany(edge => new[] { Center(edge.From), Center(edge.To) }).All(reached.Contains);
            var failures = new List<string>();
            if (!startOpen) failures.Add("start_closed"); if (!exitOpen) failures.Add("exit_closed"); if (!bfs) failures.Add("bfs_missing"); if (!bounds) failures.Add("room_bounds"); if (!reciprocal) failures.Add("connector_reciprocal"); if (!gatesOpen) failures.Add("connector_open_or_reachable"); if (!main) failures.Add("main_rooms"); if (!branches) failures.Add("branches"); if (!splits) failures.Add("splits"); if (result.FallbackCarveCount != 0) failures.Add("fallback_carve"); if (result.SilentRepairCount != 0) failures.Add("silent_repair"); if (result.RotationCount != 0) failures.Add("rotation");
            var reason = string.Join(";", failures);
            var digest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN05_VALIDATION", result.CourseDigest, "start_open=" + Bit(startOpen), "exit_open=" + Bit(exitOpen), "bfs=" + Bit(bfs), "bounds=" + Bit(bounds), "reciprocal=" + Bit(reciprocal), "gates_open=" + Bit(gatesOpen), "main=" + Bit(main), "branches=" + Bit(branches), "splits=" + Bit(splits), "fallback=0", "silent=0", "rotation=0", "failure=" + reason });
            return new MoonPalaceSeededRunValidation(result, startOpen, exitOpen, bfs, bounds, reciprocal, gatesOpen, main, branches, splits, reason, digest);
        }

        private static ISet<Vector2Int> Reachable(MoonPalaceSeededRunResult result)
        {
            var reached = new HashSet<Vector2Int>(); if (!result.IsOpen(result.StartTile.x, result.StartTile.y)) return reached;
            var queue = new Queue<Vector2Int>(); queue.Enqueue(result.StartTile); reached.Add(result.StartTile);
            var directions = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
            while (queue.Count > 0) { var current = queue.Dequeue(); foreach (var direction in directions) { var next = current + direction; if (result.CanStep(current.x, current.y, next.x, next.y) && reached.Add(next)) queue.Enqueue(next); } }
            return reached;
        }
        private static Vector2Int Center(PatternSlotCoordinate slot) => new Vector2Int(slot.X * 4 + 1, slot.Y * 4 + 1);
        private static string Bit(bool value) => value ? "1" : "0";
    }
}
