using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Deterministic bounded RUN05 course generator. Candidate selection is the only bounded search; it never repairs a generated mask.</summary>
    public static class MoonPalaceSeededRunGenerator
    {
        public static MoonPalaceSeededRunResult Generate(string recipeId, int seed) => Generate(MoonPalaceSeededRunRecipeCatalog.Get(recipeId), seed);

        public static MoonPalaceSeededRunResult Generate(MoonPalaceSeededRunRecipe recipe, int seed)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            recipe.Validate();
            var topology = CreateTopology(recipe);
            var candidates = MoonPalaceMicroPatternCandidateLibrary.Build();
            if (candidates.RawMaskCount != 65536 || candidates.Candidates.Count != 500) throw new InvalidOperationException("RUN05 requires the audited 65,536-mask / 500-candidate library.");
            var requirements = Requirements(topology.Edges);
            var ownership = Ownership(topology.Edges);
            var placements = new List<MoonPalaceSeededRunPatternPlacement>();
            var selected = new Dictionary<PatternSlotCoordinate, MoonPalaceMicroPatternCandidate>();
            var totalAttempts = 0;
            for (var y = 0; y < recipe.PatternGridHeight; y++)
            for (var x = 0; x < recipe.PatternGridWidth; x++)
            {
                var slot = new PatternSlotCoordinate(x, y);
                requirements.TryGetValue(slot, out var directions);
                var index = y * recipe.PatternGridWidth + x;
                MoonPalaceMicroPatternCandidate candidate;
                int attempts;
                if (directions == null || directions.Count == 0) candidate = SelectFiller(candidates.Candidates, seed, index, out attempts);
                else candidate = SelectRoute(candidates.Candidates, seed, index, directions, out attempts);
                totalAttempts += attempts;
                selected.Add(slot, candidate);
                var room = topology.Rooms.FirstOrDefault(value => value.Contains(slot));
                placements.Add(new MoonPalaceSeededRunPatternPlacement(index, slot, room == null ? "OUTSIDE_QUIET" : room.RoomId, ownership.TryGetValue(slot, out var value) ? value : "FILLER_DETAIL", candidate, directions ?? Array.Empty<MoonPalaceRunDirection>(), attempts));
            }
            if (placements.Count != recipe.PatternGridWidth * recipe.PatternGridHeight) throw new InvalidOperationException("RUN05 must place one direct candidate in every pattern slot.");
            foreach (var edge in topology.Edges) RequireReciprocalSockets(selected[edge.From], selected[edge.To], edge.Direction);
            var open = OpenTiles(recipe, placements);
            var roomCells = new string[recipe.TileWidth * recipe.TileHeight];
            var connectorCells = new string[roomCells.Length];
            var ownershipCells = new string[roomCells.Length];
            for (var y = 0; y < recipe.TileHeight; y++)
            for (var x = 0; x < recipe.TileWidth; x++)
            {
                var index = y * recipe.TileWidth + x;
                var slot = new PatternSlotCoordinate(x / 4, y / 4);
                roomCells[index] = topology.Rooms.FirstOrDefault(room => room.Contains(slot))?.RoomId ?? "OUTSIDE_QUIET";
                connectorCells[index] = string.Empty;
                ownershipCells[index] = ownership.TryGetValue(slot, out var routeOwnership) ? routeOwnership : "FILLER_DETAIL";
            }
            foreach (var connector in topology.Connectors)
            {
                connector.IsReciprocal = HasSocket(selected[connector.FromPatternSlot], connector.Direction) && HasSocket(selected[connector.ToPatternSlot], Opposite(connector.Direction));
                connector.IsOpen = connector.IsReciprocal && IsOpen(open, recipe.TileWidth, recipe.TileHeight, connector.FromGateTile) && IsOpen(open, recipe.TileWidth, recipe.TileHeight, connector.ToGateTile);
                connectorCells[connector.FromGateTile.y * recipe.TileWidth + connector.FromGateTile.x] = connector.ConnectorId;
                connectorCells[connector.ToGateTile.y * recipe.TileWidth + connector.ToGateTile.x] = connector.ConnectorId;
            }
            var route = Bfs(open, recipe.TileWidth, recipe.TileHeight, topology.StartTile, topology.ExitTile);
            if (route.Count == 0) throw new InvalidOperationException("RUN05 records a reachable-course failure instead of carving a tunnel.");
            var roomDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN05_ROOM_GRAPH", recipe.CanonicalDigest }.Concat(topology.Rooms.OrderBy(room => room.RoomId, StringComparer.Ordinal).Select(RoomLine)).Concat(topology.Edges.OrderBy(edge => edge.Ownership, StringComparer.Ordinal).ThenBy(edge => edge.OwnerId, StringComparer.Ordinal).ThenBy(edge => edge.From.Y).ThenBy(edge => edge.From.X).Select(EdgeLine)).Concat(topology.Connectors.OrderBy(connector => connector.ConnectorId, StringComparer.Ordinal).Select(ConnectorLine)));
            var placementDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN05_PLACEMENTS", recipe.CanonicalDigest, "seed=" + seed.ToString(CultureInfo.InvariantCulture), candidates.CanonicalDigest }.Concat(placements.OrderBy(item => item.SlotIndex).Select(PlacementLine)));
            var courseDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN05_COURSE", roomDigest, placementDigest, "start=" + Point(topology.StartTile), "exit=" + Point(topology.ExitTile) }.Concat(route.Select((tile, index) => "ROUTE|" + index.ToString(CultureInfo.InvariantCulture) + "|" + Point(tile))));
            var result = new MoonPalaceSeededRunResult(recipe, seed, candidates, topology.Rooms, topology.Connectors, placements, topology.Edges, open, roomCells, connectorCells, ownershipCells, topology.StartTile, topology.ExitTile, route, roomDigest, placementDigest, courseDigest, totalAttempts);
            result.Validation = MoonPalaceSeededRunValidator.Validate(result);
            if (!result.Validation.Passed) throw new InvalidOperationException("RUN05 produced a visible validation failure: " + result.Validation.FailureReason);
            return result;
        }

        public static bool HasSocket(MoonPalaceMicroPatternCandidate candidate, MoonPalaceRunDirection direction)
        {
            if (candidate == null) return false;
            var bits = direction == MoonPalaceRunDirection.North ? candidate.NorthSocketBits : direction == MoonPalaceRunDirection.South ? candidate.SouthSocketBits : direction == MoonPalaceRunDirection.West ? candidate.WestSocketBits : candidate.EastSocketBits;
            return (bits & (1 << MoonPalaceCameraRoomPatternComposer.RouteSocketBit)) != 0;
        }

        public static MoonPalaceRunDirection Opposite(MoonPalaceRunDirection direction) => direction == MoonPalaceRunDirection.North ? MoonPalaceRunDirection.South : direction == MoonPalaceRunDirection.South ? MoonPalaceRunDirection.North : direction == MoonPalaceRunDirection.East ? MoonPalaceRunDirection.West : MoonPalaceRunDirection.East;

        private static Topology CreateTopology(MoonPalaceSeededRunRecipe recipe)
        {
            if (recipe.RecipeId == "WIDE_BRANCH_RUN") return Wide(recipe);
            if (recipe.RecipeId == "TALL_LOOP_RUN") return Tall(recipe);
            if (recipe.RecipeId == "COMPACT_SPLIT_RUN") return Compact(recipe);
            throw new InvalidOperationException("RUN05 recipe topology is not registered: " + recipe.RecipeId);
        }

        private static Topology Wide(MoonPalaceSeededRunRecipe recipe)
        {
            var topology = new Topology(recipe, new Vector2Int(9, 45), new Vector2Int(373, 45));
            for (var index = 0; index < 9; index++)
            {
                var min = index * 11;
                var max = index == 8 ? 95 : min + 10;
                topology.Room("R" + (index + 1).ToString("00", CultureInfo.InvariantCulture) + (index == 0 ? "_START" : index == 8 ? "_EXIT" : "_WIDE"), index == 0 ? MoonPalaceSeededRunRoomRole.Start : index == 8 ? MoonPalaceSeededRunRoomRole.Exit : MoonPalaceSeededRunRoomRole.Transit, min, 8, max, 15, index + 1);
            }
            topology.Room("B01_NORTH", MoonPalaceSeededRunRoomRole.Branch, 24, 0, 32, 7, 0);
            topology.Room("B02_SOUTH", MoonPalaceSeededRunRoomRole.Branch, 47, 16, 54, 23, 0);
            topology.Room("B03_NORTH", MoonPalaceSeededRunRoomRole.Branch, 70, 0, 76, 7, 0);
            topology.Line(new PatternSlotCoordinate(2, 11), new PatternSlotCoordinate(93, 11), "MAIN", "MAIN");
            topology.Line(new PatternSlotCoordinate(26, 11), new PatternSlotCoordinate(26, 3), "BRANCH", "B01_NORTH");
            topology.Line(new PatternSlotCoordinate(49, 11), new PatternSlotCoordinate(49, 20), "BRANCH", "B02_SOUTH");
            topology.Line(new PatternSlotCoordinate(71, 11), new PatternSlotCoordinate(71, 3), "BRANCH", "B03_NORTH");
            topology.Path(new[] { new PatternSlotCoordinate(36, 11), new PatternSlotCoordinate(36, 13), new PatternSlotCoordinate(40, 13), new PatternSlotCoordinate(40, 11) }, "SPLIT_REJOIN", "S1");
            topology.Path(new[] { new PatternSlotCoordinate(58, 11), new PatternSlotCoordinate(58, 9), new PatternSlotCoordinate(62, 9), new PatternSlotCoordinate(62, 11) }, "SPLIT_REJOIN", "S2");
            for (var index = 1; index < 9; index++) topology.Connector("C" + index.ToString("00", CultureInfo.InvariantCulture), topology.Rooms[index - 1].RoomId, topology.Rooms[index].RoomId, new PatternSlotCoordinate(index * 11 - 1, 11), new PatternSlotCoordinate(index * 11, 11), "main", true);
            topology.Connector("C10", topology.Rooms[2].RoomId, "B01_NORTH", new PatternSlotCoordinate(26, 8), new PatternSlotCoordinate(26, 7), "branch", false);
            topology.Connector("C11", topology.Rooms[4].RoomId, "B02_SOUTH", new PatternSlotCoordinate(49, 15), new PatternSlotCoordinate(49, 16), "branch", false);
            topology.Connector("C12", topology.Rooms[6].RoomId, "B03_NORTH", new PatternSlotCoordinate(71, 8), new PatternSlotCoordinate(71, 7), "branch", false);
            topology.AssertInside(); return topology;
        }

        private static Topology Tall(MoonPalaceSeededRunRecipe recipe)
        {
            var topology = new Topology(recipe, new Vector2Int(9, 53), new Vector2Int(277, 53));
            topology.Room("R01_START", MoonPalaceSeededRunRoomRole.Start, 0, 10, 8, 17, 1);
            topology.Room("R02_TRANSIT", MoonPalaceSeededRunRoomRole.Transit, 9, 10, 17, 17, 2);
            topology.Room("R03_ASCENT", MoonPalaceSeededRunRoomRole.Vertical, 18, 10, 26, 27, 3);
            topology.Room("R04_UPPER", MoonPalaceSeededRunRoomRole.Split, 27, 19, 35, 27, 4);
            topology.Room("R05_UPPER", MoonPalaceSeededRunRoomRole.Transit, 36, 19, 44, 27, 5);
            topology.Room("R06_UPPER", MoonPalaceSeededRunRoomRole.Split, 45, 19, 53, 27, 6);
            topology.Room("R07_DESCENT", MoonPalaceSeededRunRoomRole.Vertical, 54, 5, 62, 20, 7);
            topology.Room("R08_EXIT", MoonPalaceSeededRunRoomRole.Exit, 63, 10, 71, 17, 8);
            topology.Room("B01_LOWER", MoonPalaceSeededRunRoomRole.Branch, 18, 0, 26, 9, 0);
            topology.Room("B02_MID", MoonPalaceSeededRunRoomRole.Branch, 45, 10, 53, 18, 0);
            topology.Path(new[] { new PatternSlotCoordinate(2, 13), new PatternSlotCoordinate(20, 13), new PatternSlotCoordinate(20, 23), new PatternSlotCoordinate(53, 23), new PatternSlotCoordinate(53, 19), new PatternSlotCoordinate(56, 19), new PatternSlotCoordinate(56, 13), new PatternSlotCoordinate(69, 13) }, "MAIN", "MAIN");
            topology.Line(new PatternSlotCoordinate(22, 13), new PatternSlotCoordinate(22, 3), "BRANCH", "B01_LOWER");
            topology.Line(new PatternSlotCoordinate(49, 23), new PatternSlotCoordinate(49, 14), "BRANCH", "B02_MID");
            topology.Path(new[] { new PatternSlotCoordinate(30, 23), new PatternSlotCoordinate(30, 25), new PatternSlotCoordinate(34, 25), new PatternSlotCoordinate(34, 23) }, "SPLIT_REJOIN", "S1");
            topology.Path(new[] { new PatternSlotCoordinate(47, 23), new PatternSlotCoordinate(47, 25), new PatternSlotCoordinate(51, 25), new PatternSlotCoordinate(51, 23) }, "SPLIT_REJOIN", "S2");
            topology.Connector("C01", "R01_START", "R02_TRANSIT", new PatternSlotCoordinate(8, 13), new PatternSlotCoordinate(9, 13), "main", true);
            topology.Connector("C02", "R02_TRANSIT", "R03_ASCENT", new PatternSlotCoordinate(17, 13), new PatternSlotCoordinate(18, 13), "main", true);
            topology.Connector("C03", "R03_ASCENT", "R04_UPPER", new PatternSlotCoordinate(26, 23), new PatternSlotCoordinate(27, 23), "main", true);
            topology.Connector("C04", "R04_UPPER", "R05_UPPER", new PatternSlotCoordinate(35, 23), new PatternSlotCoordinate(36, 23), "main", true);
            topology.Connector("C05", "R05_UPPER", "R06_UPPER", new PatternSlotCoordinate(44, 23), new PatternSlotCoordinate(45, 23), "main", true);
            topology.Connector("C06", "R06_UPPER", "R07_DESCENT", new PatternSlotCoordinate(53, 19), new PatternSlotCoordinate(54, 19), "main", true);
            topology.Connector("C07", "R07_DESCENT", "R08_EXIT", new PatternSlotCoordinate(62, 13), new PatternSlotCoordinate(63, 13), "main", true);
            topology.Connector("C08", "R03_ASCENT", "B01_LOWER", new PatternSlotCoordinate(22, 10), new PatternSlotCoordinate(22, 9), "branch", false);
            topology.Connector("C09", "R06_UPPER", "B02_MID", new PatternSlotCoordinate(49, 19), new PatternSlotCoordinate(49, 18), "branch", false);
            topology.AssertInside(); return topology;
        }

        private static Topology Compact(MoonPalaceSeededRunRecipe recipe)
        {
            var topology = new Topology(recipe, new Vector2Int(9, 37), new Vector2Int(245, 37));
            for (var index = 0; index < 7; index++)
            {
                var min = index * 9;
                var max = index == 6 ? 63 : min + 8;
                topology.Room("R" + (index + 1).ToString("00", CultureInfo.InvariantCulture) + (index == 0 ? "_START" : index == 6 ? "_EXIT" : "_COMPACT"), index == 0 ? MoonPalaceSeededRunRoomRole.Start : index == 6 ? MoonPalaceSeededRunRoomRole.Exit : index == 3 ? MoonPalaceSeededRunRoomRole.Split : MoonPalaceSeededRunRoomRole.Transit, min, 5, max, 14, index + 1);
            }
            topology.Room("B01_NORTH", MoonPalaceSeededRunRoomRole.Branch, 18, 0, 26, 4, 0);
            topology.Room("B02_SOUTH", MoonPalaceSeededRunRoomRole.Branch, 45, 15, 53, 19, 0);
            topology.Line(new PatternSlotCoordinate(2, 9), new PatternSlotCoordinate(61, 9), "MAIN", "MAIN");
            topology.Line(new PatternSlotCoordinate(22, 9), new PatternSlotCoordinate(22, 2), "BRANCH", "B01_NORTH");
            topology.Line(new PatternSlotCoordinate(49, 9), new PatternSlotCoordinate(49, 17), "BRANCH", "B02_SOUTH");
            topology.Path(new[] { new PatternSlotCoordinate(30, 9), new PatternSlotCoordinate(30, 11), new PatternSlotCoordinate(34, 11), new PatternSlotCoordinate(34, 9) }, "SPLIT_REJOIN", "S1");
            for (var index = 1; index < 7; index++) topology.Connector("C" + index.ToString("00", CultureInfo.InvariantCulture), topology.Rooms[index - 1].RoomId, topology.Rooms[index].RoomId, new PatternSlotCoordinate(index * 9 - 1, 9), new PatternSlotCoordinate(index * 9, 9), "main", true);
            topology.Connector("C07", topology.Rooms[2].RoomId, "B01_NORTH", new PatternSlotCoordinate(22, 5), new PatternSlotCoordinate(22, 4), "branch", false);
            topology.Connector("C08", topology.Rooms[5].RoomId, "B02_SOUTH", new PatternSlotCoordinate(49, 14), new PatternSlotCoordinate(49, 15), "branch", false);
            topology.AssertInside(); return topology;
        }

        private static Dictionary<PatternSlotCoordinate, IReadOnlyList<MoonPalaceRunDirection>> Requirements(IEnumerable<MoonPalaceSeededRunRouteEdge> edges)
        {
            var requirements = new Dictionary<PatternSlotCoordinate, List<MoonPalaceRunDirection>>();
            foreach (var edge in edges) { AddRequirement(requirements, edge.From, edge.Direction); AddRequirement(requirements, edge.To, Opposite(edge.Direction)); }
            return requirements.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<MoonPalaceRunDirection>)pair.Value.Distinct().OrderBy(direction => direction).ToList());
        }
        private static void AddRequirement(IDictionary<PatternSlotCoordinate, List<MoonPalaceRunDirection>> map, PatternSlotCoordinate slot, MoonPalaceRunDirection direction) { if (!map.TryGetValue(slot, out var values)) { values = new List<MoonPalaceRunDirection>(); map.Add(slot, values); } values.Add(direction); }
        private static Dictionary<PatternSlotCoordinate, string> Ownership(IEnumerable<MoonPalaceSeededRunRouteEdge> edges)
        {
            var map = new Dictionary<PatternSlotCoordinate, string>();
            foreach (var edge in edges) { SetOwnership(map, edge.From, edge.Ownership); SetOwnership(map, edge.To, edge.Ownership); }
            return map;
        }
        private static void SetOwnership(IDictionary<PatternSlotCoordinate, string> map, PatternSlotCoordinate slot, string value) { if (!map.TryGetValue(slot, out var current) || Rank(value) > Rank(current)) map[slot] = value; }
        private static int Rank(string value) => value == "SPLIT_REJOIN" ? 3 : value == "BRANCH" ? 2 : value == "MAIN" ? 1 : 0;
        private static MoonPalaceMicroPatternCandidate SelectRoute(IEnumerable<MoonPalaceMicroPatternCandidate> candidates, int seed, int index, IReadOnlyList<MoonPalaceRunDirection> requirements, out int attempts)
        {
            attempts = 0;
            foreach (var candidate in Ordered(candidates, seed, index))
            {
                attempts++;
                if (!candidate.ChunkPathAllowed || !candidate.IsOpen(1, 1) || requirements.Any(direction => !HasSocket(candidate, direction))) continue;
                return candidate;
            }
            throw new InvalidOperationException("RUN05 bounded candidate selection exhausted without a compatible 4x4 mask; no fallback carve was applied.");
        }
        private static MoonPalaceMicroPatternCandidate SelectFiller(IEnumerable<MoonPalaceMicroPatternCandidate> candidates, int seed, int index, out int attempts) { var selected = Ordered(candidates, seed, index).FirstOrDefault(); attempts = selected == null ? 0 : 1; if (selected == null) throw new InvalidOperationException("RUN05 candidate library is empty."); return selected; }
        private static IEnumerable<MoonPalaceMicroPatternCandidate> Ordered(IEnumerable<MoonPalaceMicroPatternCandidate> candidates, int seed, int index) => candidates.OrderBy(candidate => Stable(seed, index, candidate.Mask)).ThenBy(candidate => candidate.Mask);
        private static int Stable(int seed, int index, ushort mask) { unchecked { var value = seed; value = (value * 486187739) ^ index; value = (value * 397) ^ mask; value ^= value >> 16; return value & int.MaxValue; } }
        private static void RequireReciprocalSockets(MoonPalaceMicroPatternCandidate from, MoonPalaceMicroPatternCandidate to, MoonPalaceRunDirection direction) { if (!HasSocket(from, direction) || !HasSocket(to, Opposite(direction))) throw new InvalidOperationException("RUN05 rejected a non-reciprocal candidate socket rather than repairing a route."); }
        private static bool[] OpenTiles(MoonPalaceSeededRunRecipe recipe, IEnumerable<MoonPalaceSeededRunPatternPlacement> placements)
        {
            var open = new bool[recipe.TileWidth * recipe.TileHeight];
            foreach (var placement in placements) for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) open[((placement.PatternSlot.Y * 4 + y) * recipe.TileWidth) + placement.PatternSlot.X * 4 + x] = placement.Candidate.IsOpen(x, y);
            return open;
        }
        private static Vector2Int GateTile(PatternSlotCoordinate slot, MoonPalaceRunDirection direction) { var x = slot.X * 4; var y = slot.Y * 4; return direction == MoonPalaceRunDirection.North ? new Vector2Int(x + 1, y + 3) : direction == MoonPalaceRunDirection.South ? new Vector2Int(x + 1, y) : direction == MoonPalaceRunDirection.East ? new Vector2Int(x + 3, y + 1) : new Vector2Int(x, y + 1); }
        private static IReadOnlyList<Vector2Int> Bfs(bool[] open, int width, int height, Vector2Int start, Vector2Int exit)
        {
            var path = new List<Vector2Int>(); if (!IsOpen(open, width, height, start) || !IsOpen(open, width, height, exit)) return path;
            var parents = new Dictionary<Vector2Int, Vector2Int>(); var visited = new HashSet<Vector2Int> { start }; var queue = new Queue<Vector2Int>(); queue.Enqueue(start);
            var directions = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
            while (queue.Count > 0) { var current = queue.Dequeue(); if (current.Equals(exit)) break; foreach (var direction in directions) { var next = current + direction; if (IsOpen(open, width, height, next) && visited.Add(next)) { parents.Add(next, current); queue.Enqueue(next); } } }
            if (!visited.Contains(exit)) return path;
            for (var current = exit; ; current = parents[current]) { path.Add(current); if (current.Equals(start)) break; } path.Reverse(); return path;
        }
        private static bool IsOpen(bool[] open, int width, int height, Vector2Int tile) => tile.x >= 0 && tile.x < width && tile.y >= 0 && tile.y < height && open[tile.y * width + tile.x];
        private static string RoomLine(MoonPalaceSeededRunRoomRecord room) => "ROOM|" + room.RoomId + "|" + room.Role + "|" + room.MinPatternX + ":" + room.MinPatternY + "-" + room.MaxPatternX + ":" + room.MaxPatternY + "|" + room.RouteOrder;
        private static string EdgeLine(MoonPalaceSeededRunRouteEdge edge) => "EDGE|" + edge.Ownership + "|" + edge.OwnerId + "|" + edge.From + ">" + edge.To;
        private static string ConnectorLine(MoonPalaceSeededRunConnectorRecord connector) => "CONNECTOR|" + connector.ConnectorId + "|" + connector.FromRoomId + ">" + connector.ToRoomId + "|" + connector.FromPatternSlot + ">" + connector.ToPatternSlot + "|" + connector.Kind;
        private static string PlacementLine(MoonPalaceSeededRunPatternPlacement placement) => "PLACEMENT|" + placement.SlotIndex + "|" + placement.PatternSlot + "|" + placement.RoomId + "|" + placement.RouteOwnership + "|" + placement.Candidate.CandidateId + "|" + placement.Candidate.MaskU16Hex + "|" + placement.Candidate.SocketSignature + "|" + placement.AttemptCount;
        private static string Point(Vector2Int point) => point.x.ToString(CultureInfo.InvariantCulture) + ":" + point.y.ToString(CultureInfo.InvariantCulture);

        private sealed class Topology
        {
            private readonly MoonPalaceSeededRunRecipe recipe;
            public Topology(MoonPalaceSeededRunRecipe source, Vector2Int start, Vector2Int exit) { recipe = source; StartTile = start; ExitTile = exit; }
            public List<MoonPalaceSeededRunRoomRecord> Rooms { get; } = new List<MoonPalaceSeededRunRoomRecord>();
            public List<MoonPalaceSeededRunConnectorRecord> Connectors { get; } = new List<MoonPalaceSeededRunConnectorRecord>();
            public List<MoonPalaceSeededRunRouteEdge> Edges { get; } = new List<MoonPalaceSeededRunRouteEdge>();
            public Vector2Int StartTile { get; }
            public Vector2Int ExitTile { get; }
            public void Room(string id, MoonPalaceSeededRunRoomRole role, int minX, int minY, int maxX, int maxY, int order) => Rooms.Add(new MoonPalaceSeededRunRoomRecord(id, role, minX, minY, maxX, maxY, order));
            public void Line(PatternSlotCoordinate from, PatternSlotCoordinate to, string ownership, string owner) { var current = from; while (!current.Equals(to)) { var next = current.X != to.X ? new PatternSlotCoordinate(current.X + Math.Sign(to.X - current.X), current.Y) : new PatternSlotCoordinate(current.X, current.Y + Math.Sign(to.Y - current.Y)); Edges.Add(new MoonPalaceSeededRunRouteEdge(current, next, ownership, owner)); current = next; } }
            public void Path(IEnumerable<PatternSlotCoordinate> points, string ownership, string owner) { var list = points.ToList(); for (var index = 1; index < list.Count; index++) Line(list[index - 1], list[index], ownership, owner); }
            public void Connector(string id, string fromRoom, string toRoom, PatternSlotCoordinate from, PatternSlotCoordinate to, string kind, bool required) { var direction = MoonPalaceSeededRunRouteEdge.DirectionFor(from, to); Connectors.Add(new MoonPalaceSeededRunConnectorRecord(id, fromRoom, toRoom, from, to, direction, kind, required, GateTile(from, direction), GateTile(to, Opposite(direction)))); }
            public void AssertInside()
            {
                if (Rooms.Select(room => room.RoomId).Distinct(StringComparer.Ordinal).Count() != Rooms.Count || Connectors.Select(connector => connector.ConnectorId).Distinct(StringComparer.Ordinal).Count() != Connectors.Count || Rooms.Any(room => room.MinPatternX < 0 || room.MinPatternY < 0 || room.MaxPatternX >= recipe.PatternGridWidth || room.MaxPatternY >= recipe.PatternGridHeight) || Edges.Any(edge => edge.From.X < 0 || edge.From.X >= recipe.PatternGridWidth || edge.From.Y < 0 || edge.From.Y >= recipe.PatternGridHeight || edge.To.X < 0 || edge.To.X >= recipe.PatternGridWidth || edge.To.Y < 0 || edge.To.Y >= recipe.PatternGridHeight)) throw new InvalidOperationException("RUN05 recipe topology escaped its direct pattern grid.");
            }
        }
    }
}
