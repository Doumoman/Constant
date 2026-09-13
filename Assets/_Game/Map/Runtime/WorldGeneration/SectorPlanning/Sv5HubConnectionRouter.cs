using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5HubConnectionCellRole { Centerline = 1, HeadClearance = 2, Support = 3 }

    public sealed class Sv5HubConnectionCell : IComparable<Sv5HubConnectionCell>
    {
        internal Sv5HubConnectionCell(string connectionId, Sv5HubConnectionCellRole role, int sequence,
            RmapSpecialWorldPoint world, Sv5InfillCellValue beforeValue, Sv5InfillCellValue finalValue,
            Sv5InfillCellValue headValue, Sv5InfillCellValue supportValue)
        {
            ConnectionId = connectionId ?? string.Empty; Role = role; Sequence = sequence; World = world;
            BeforeValue = beforeValue; FinalValue = finalValue; HeadValue = headValue; SupportValue = supportValue;
            Changed = beforeValue != finalValue; Ownership = Changed ? "HUB_CONNECTION_ACTUAL" : string.Empty;
        }

        public string ConnectionId { get; }
        public Sv5HubConnectionCellRole Role { get; }
        public int Sequence { get; }
        public RmapSpecialWorldPoint World { get; }
        public Sv5InfillCellValue BeforeValue { get; }
        public Sv5InfillCellValue FinalValue { get; }
        public Sv5InfillCellValue HeadValue { get; }
        public Sv5InfillCellValue SupportValue { get; }
        public bool Changed { get; }
        public string Ownership { get; }
        public string ExportRole => Role == Sv5HubConnectionCellRole.Centerline ? "CENTERLINE" :
            Role == Sv5HubConnectionCellRole.HeadClearance ? "HEAD_CLEARANCE" : "SUPPORT";
        internal Sv5HubConnectionCell Bind(string connectionId) => new Sv5HubConnectionCell(connectionId, Role,
            Sequence, World, BeforeValue, FinalValue, HeadValue, SupportValue);
        public int CompareTo(Sv5HubConnectionCell other)
        {
            if (other == null) return 1;
            int connection = string.Compare(ConnectionId, other.ConnectionId, StringComparison.Ordinal);
            if (connection != 0) return connection;
            int role = Role.CompareTo(other.Role); if (role != 0) return role;
            int sequence = Sequence.CompareTo(other.Sequence); return sequence != 0 ? sequence : World.CompareTo(other.World);
        }
    }

    public sealed class Sv5HubConnectionRoute
    {
        internal Sv5HubConnectionRoute(IEnumerable<RmapSpecialWorldPoint> centerline,
            IEnumerable<Sv5HubConnectionCell> cells, IEnumerable<RmapSpecialWorldPoint> witness,
            string direction, bool verified, bool protectedOverlap, bool type0Overlap, bool progressionBypass)
        {
            Centerline = new ReadOnlyCollection<RmapSpecialWorldPoint>(centerline.ToArray());
            Cells = new ReadOnlyCollection<Sv5HubConnectionCell>(cells.ToArray());
            MovementWitness = new ReadOnlyCollection<RmapSpecialWorldPoint>(witness.ToArray());
            Direction = direction; RouteVerified = verified; ProtectedOverlap = protectedOverlap;
            Type0Overlap = type0Overlap; ProgressionBypass = progressionBypass;
        }

        public IReadOnlyList<RmapSpecialWorldPoint> Centerline { get; }
        public IReadOnlyList<Sv5HubConnectionCell> Cells { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> MovementWitness { get; }
        public string Direction { get; }
        public bool RouteVerified { get; }
        public bool ProtectedOverlap { get; }
        public bool Type0Overlap { get; }
        public bool ProgressionBypass { get; }
        public int ChangedCells => Cells.Count(value => value.Changed);
    }

    /// <summary>Bounded deterministic routing over immutable occupancy plus sparse desired-cell overlays.</summary>
    public static class Sv5HubConnectionRouter
    {
        public const int MaximumCenterlineCells = 120;
        public const int MaximumSearchWidth = 160;
        public const int MaximumSearchHeight = 128;

        private sealed class Desired
        {
            public Sv5InfillCellValue Value;
            public Sv5HubConnectionCellRole Role;
            public int Sequence;
        }

        public static IReadOnlyList<RmapSpecialWorldPoint> LegacyVerticalThenHorizontal(
            RmapSpecialWorldPoint start, RmapSpecialWorldPoint end)
        {
            var result = new List<RmapSpecialWorldPoint> { start }; int x = start.X, y = start.Y;
            while (y != end.Y) { y += Math.Sign(end.Y - y); result.Add(new RmapSpecialWorldPoint(x, y)); }
            while (x != end.X) { x += Math.Sign(end.X - x); result.Add(new RmapSpecialWorldPoint(x, y)); }
            return Array.AsReadOnly(result.ToArray());
        }

        public static bool BodyIntersects(IEnumerable<RmapSpecialWorldPoint> centerline,
            ISet<RmapSpecialWorldPoint> cells)
        {
            var path = (centerline ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray();
            return path.Skip(1).Take(Math.Max(0, path.Length - 2)).Any(cells.Contains);
        }

        public static bool TryRoute(RmapSpecialWorldPoint start, RmapSpecialWorldPoint end,
            Sv5SpaceBounds shellBounds, IReadOnlyDictionary<RmapSpecialWorldPoint, Sv5LoopOccupancyCell> baseline,
            ISet<RmapSpecialWorldPoint> protectedBody, ISet<RmapSpecialWorldPoint> type0,
            ISet<RmapSpecialWorldPoint> progression, ISet<RmapSpecialWorldPoint> endpointForbidden,
            ISet<RmapSpecialWorldPoint> reserved, out Sv5HubConnectionRoute route, out string reason)
        {
            route = null; reason = string.Empty;
            if (baseline == null || protectedBody == null || type0 == null || progression == null ||
                endpointForbidden == null || reserved == null) throw new ArgumentNullException(nameof(baseline));
            int direct = Math.Abs(start.X - end.X) + Math.Abs(start.Y - end.Y) + 1;
            if (direct > MaximumCenterlineCells) { reason = "LENGTH"; return false; }
            if (endpointForbidden.Contains(end) || Value(baseline, end) != Sv5InfillCellValue.Air ||
                Value(baseline, new RmapSpecialWorldPoint(end.X, end.Y + 1)) != Sv5InfillCellValue.Air)
            { reason = "EXTERNAL_ANCHOR_NOT_READ_ONLY_AIR"; return false; }

            int directMinX = Math.Min(start.X, end.X), directMaxX = Math.Max(start.X, end.X);
            int directMinY = Math.Min(start.Y, end.Y), directMaxY = Math.Max(start.Y, end.Y);
            int marginX = Math.Max(0, Math.Min(24, (MaximumSearchWidth - (directMaxX - directMinX + 1)) / 2));
            int marginY = Math.Max(0, Math.Min(24, (MaximumSearchHeight - (directMaxY - directMinY + 1)) / 2));
            int minX = Math.Max(0, directMinX - marginX);
            int maxX = Math.Min(Sv5SpaceGraphPlanner.WorldWidth - 1, directMaxX + marginX);
            int minY = Math.Max(0, directMinY - marginY);
            int maxY = Math.Min(Sv5SpaceGraphPlanner.WorldHeight - 2, directMaxY + marginY);
            if (maxX - minX + 1 > MaximumSearchWidth || maxY - minY + 1 > MaximumSearchHeight)
            { reason = "LOCAL_SEARCH_BOUNDS"; return false; }

            bool CanUse(RmapSpecialWorldPoint point)
            {
                if (point.X < minX || point.X > maxX || point.Y < minY || point.Y > maxY ||
                    point.Y >= Sv5SpaceGraphPlanner.WorldHeight - 1) return false;
                if (point.Equals(start)) return true;
                if (reserved.Contains(point)) return false;
                if (point.Equals(end)) return true;
                if (shellBounds.Contains(point) || protectedBody.Contains(point) || type0.Contains(point) ||
                    progression.Contains(point)) return false;
                var head = new RmapSpecialWorldPoint(point.X, point.Y + 1);
                bool externalAnchorHead = head.Equals(end);
                return (externalAnchorHead || !reserved.Contains(head)) &&
                    (externalAnchorHead || !protectedBody.Contains(head)) &&
                    (externalAnchorHead || !type0.Contains(head)) &&
                    (externalAnchorHead || !progression.Contains(head)) && !shellBounds.Contains(head);
            }

            if (Neighbors(end).All(point => !CanUse(point)))
            { reason = "EXTERNAL_ANCHOR_INGRESS_BLOCKED"; return false; }

            var parent = new Dictionary<RmapSpecialWorldPoint, RmapSpecialWorldPoint> { { start, start } };
            var distance = new Dictionary<RmapSpecialWorldPoint, int> { { start, 1 } };
            var queue = new Queue<RmapSpecialWorldPoint>(); queue.Enqueue(start);
            while (queue.Count != 0 && !parent.ContainsKey(end))
            {
                var at = queue.Dequeue(); if (distance[at] >= MaximumCenterlineCells) continue;
                foreach (var next in Neighbors(at).OrderBy(p => Manhattan(p, end)).ThenBy(p => p))
                {
                    if (parent.ContainsKey(next) || !CanUse(next)) continue;
                    parent.Add(next, at); distance.Add(next, distance[at] + 1); queue.Enqueue(next);
                }
            }
            if (!parent.ContainsKey(end)) { reason = "NO_ACTUAL_CELL_ROUTE"; return false; }
            var path = new List<RmapSpecialWorldPoint> { end };
            while (!path[path.Count - 1].Equals(start)) path.Add(parent[path[path.Count - 1]]);
            path.Reverse();
            if (path.Count > MaximumCenterlineCells || path.Distinct().Count() != path.Count ||
                path.Zip(path.Skip(1), (a, b) => Manhattan(a, b)).Any(value => value != 1))
            { reason = "CENTERLINE_INVALID"; return false; }

            var desired = new Dictionary<RmapSpecialWorldPoint, Desired>();
            for (int index = 0; index < path.Count; index++)
                desired[path[index]] = new Desired { Value = Sv5InfillCellValue.Air,
                    Role = Sv5HubConnectionCellRole.Centerline, Sequence = index };
            for (int index = 0; index < path.Count - 1; index++)
            {
                var head = new RmapSpecialWorldPoint(path[index].X, path[index].Y + 1);
                if (!desired.ContainsKey(head)) desired.Add(head, new Desired { Value = Sv5InfillCellValue.Air,
                    Role = Sv5HubConnectionCellRole.HeadClearance, Sequence = -1 });
            }
            for (int index = 0; index < path.Count; index++)
            {
                bool horizontal = index > 0 && path[index - 1].Y == path[index].Y ||
                    index + 1 < path.Count && path[index + 1].Y == path[index].Y;
                if (!horizontal) continue;
                var support = new RmapSpecialWorldPoint(path[index].X, path[index].Y - 1);
                if (support.Y < 0 || shellBounds.Contains(support) || desired.ContainsKey(support) ||
                    reserved.Contains(support) || protectedBody.Contains(support) || type0.Contains(support) ||
                    progression.Contains(support) || Value(baseline, support) == Sv5InfillCellValue.Air) continue;
                desired.Add(support, new Desired { Value = Sv5InfillCellValue.Solid,
                    Role = Sv5HubConnectionCellRole.Support, Sequence = -1 });
            }

            Sv5InfillCellValue Final(RmapSpecialWorldPoint point) => desired.TryGetValue(point, out Desired value) ?
                value.Value : Value(baseline, point);
            bool verified = path.All(point => Final(point) == Sv5InfillCellValue.Air &&
                Final(new RmapSpecialWorldPoint(point.X, point.Y + 1)) == Sv5InfillCellValue.Air);
            bool protectedOverlap = BodyIntersects(path, protectedBody);
            bool type0Overlap = BodyIntersects(path, type0);
            bool progressionBypass = BodyIntersects(path, progression);
            if (!verified || protectedOverlap || type0Overlap || progressionBypass)
            { reason = "ROUTE_GEOMETRY_INVALID"; return false; }
            var cells = desired.OrderBy(pair => pair.Value.Role == Sv5HubConnectionCellRole.Centerline ? 0 : 1)
                .ThenBy(pair => pair.Value.Sequence).ThenBy(pair => pair.Key).Select(pair =>
                    new Sv5HubConnectionCell(string.Empty, pair.Value.Role, pair.Value.Sequence, pair.Key,
                        Value(baseline, pair.Key), pair.Value.Value,
                        Final(new RmapSpecialWorldPoint(pair.Key.X, pair.Key.Y + 1)),
                        Final(new RmapSpecialWorldPoint(pair.Key.X, pair.Key.Y - 1)))).ToArray();
            if (cells.Any(cell => cell.Changed && (protectedBody.Contains(cell.World) || type0.Contains(cell.World) ||
                progression.Contains(cell.World))) || cells.Single(cell => cell.Role == Sv5HubConnectionCellRole.Centerline &&
                cell.World.Equals(end)).Changed)
            { reason = "OWNERSHIP_OR_EXTERNAL_ANCHOR"; return false; }
            route = new Sv5HubConnectionRoute(path, cells, path, "HUB_TO_EXTERNAL", true,
                protectedOverlap, type0Overlap, progressionBypass);
            return true;
        }

        private static int Manhattan(RmapSpecialWorldPoint first, RmapSpecialWorldPoint second) =>
            Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
        private static Sv5InfillCellValue Value(
            IReadOnlyDictionary<RmapSpecialWorldPoint, Sv5LoopOccupancyCell> occupancy,
            RmapSpecialWorldPoint point) => Sv5LoopTopology.ValueAt(occupancy, point);
        private static IEnumerable<RmapSpecialWorldPoint> Neighbors(RmapSpecialWorldPoint point)
        {
            yield return new RmapSpecialWorldPoint(point.X - 1, point.Y);
            yield return new RmapSpecialWorldPoint(point.X + 1, point.Y);
            yield return new RmapSpecialWorldPoint(point.X, point.Y - 1);
            yield return new RmapSpecialWorldPoint(point.X, point.Y + 1);
        }
    }
}
