using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5InfillCellValue { Unknown = 0, Air = 1, Solid = 2 }

    public sealed class Sv5InfillCell
    {
        public Sv5InfillCell(Sv5SpecialWorldPoint world, Sv5InfillCellValue value, string owner,
            string recipe, string parent = "", string host = "", bool shared = false)
        {
            if (value != Sv5InfillCellValue.Air && value != Sv5InfillCellValue.Solid)
                throw new ArgumentException("An owned cell must be AIR or SOLID.");
            World = world; Value = value; Owner = owner; Recipe = recipe; Parent = parent; Host = host; Shared = shared;
        }
        public Sv5SpecialWorldPoint World { get; }
        public Sv5InfillCellValue Value { get; }
        public string Owner { get; }
        public string Recipe { get; }
        public string Parent { get; }
        public string Host { get; }
        public bool Shared { get; }
        public string Token => World + "|" + Value + "|" + Owner + "|" + Recipe + "|" + Parent + "|" + Host + "|" + Shared;
    }

    public sealed class Sv5InfillPattern
    {
        public Sv5InfillPattern(ushort mask, IEnumerable<Sv5InfillCellValue> values)
        {
            var cells = values.ToArray();
            if (cells.Length != 16) throw new ArgumentException("Exactly sixteen row-major cells required.");
            if(cells.Any(c=>c!=Sv5InfillCellValue.Unknown && c!=Sv5InfillCellValue.Air && c!=Sv5InfillCellValue.Solid))
                throw new ArgumentException("Invalid cell value.");
            for (int i = 0; i < 16; i++)
                if (((mask >> i) & 1) == 0 ? cells[i] != Sv5InfillCellValue.Unknown : cells[i] == Sv5InfillCellValue.Unknown)
                    throw new ArgumentException("UNKNOWN and write mask disagree.");
            Mask = mask; Values = Array.AsReadOnly(cells);
            Digest = Sv5WorldDefinition.Hash(mask + "|" + string.Join(",", cells.Select(v => (int)v)));
            Id = "INFILL_" + Digest;
        }
        public ushort Mask { get; }
        public IReadOnlyList<Sv5InfillCellValue> Values { get; }
        public string Digest { get; }
        public string Id { get; }
    }

    public sealed class Sv5InfillInstance
    {
        public Sv5InfillInstance(Sv5InfillPattern pattern, Sv5SpecialWorldPoint origin,
            string owner, string recipe, string parent, string host, bool mirror)
        { Pattern = pattern; Origin = origin; Owner = owner; Recipe = recipe; Parent = parent; Host = host; Mirror = mirror; }
        public Sv5InfillPattern Pattern { get; }
        public Sv5SpecialWorldPoint Origin { get; }
        public string Owner { get; }
        public string Recipe { get; }
        public string Parent { get; }
        public string Host { get; }
        // Payload is already transformed; mirror records provenance and is not applied twice.
        public bool Mirror { get; }
        public string Token => Pattern.Id + "|" + Origin + "|" + Owner + "|" + Recipe + "|" + Parent + "|" + Host + "|" + Mirror;
    }

    public sealed class Sv5InfillStaticProof
    {
        internal Sv5InfillStaticProof(IEnumerable<Sv5SpecialWorldPoint> approach,
            IEnumerable<Sv5SpecialWorldPoint> recovery, string reason)
        { Approach = Array.AsReadOnly(approach.ToArray()); Return = Array.AsReadOnly(recovery.ToArray()); Reason = reason; }
        public IReadOnlyList<Sv5SpecialWorldPoint> Approach { get; }
        public IReadOnlyList<Sv5SpecialWorldPoint> Return { get; }
        public string Reason { get; }
        public bool Success => Approach.Count > 0 && Return.Count > 0;
        public bool PlayerVerified => false;
    }

    public static class Sv5InfillPatterns
    {
        public static readonly IReadOnlyList<string> Recipes = Array.AsReadOnly(new[] { "EMPTY_ROOM", "SMALL_CAVE", "LANDING", "DEAD_END" });
        public static Sv5SpaceBounds Bounds(string recipe, int x = 0, int y = 0)
        {
            if (!Recipes.Contains(recipe)) throw new ArgumentException("Unknown recipe: " + recipe);
            return new Sv5SpaceBounds(x,y,recipe == "SMALL_CAVE" ? 20 : 16,recipe == "LANDING" ? 16 : 12);
        }
        public static IReadOnlyList<Sv5InfillCell> Recipe(string recipe, Sv5SpecialWorldPoint origin,
            bool mirror = false, string owner = "FIXTURE", string parent = "", string host = "", bool childDoor = false)
        {
            var bounds = Bounds(recipe,origin.X,origin.Y);
            var cells = new List<Sv5InfillCell>();
            for (int y = 0; y < bounds.Height; y++)
            for (int x = 0; x < bounds.Width; x++)
            {
                int floor = recipe == "LANDING" ? 3 + Math.Min(2,Math.Max(0,(x-3)/2)) : 3;
                int ceiling = recipe == "SMALL_CAVE" ? (x <= 5 ? 8 : x <= 9 ? 9 : x <= 13 ? 8 : 7) : recipe == "LANDING" ? 12 : 8;
                bool air = x >= 3 && x <= bounds.Width-4 && y >= floor && y <= ceiling;
                if (recipe == "DEAD_END" && x == 12 && y >= 6 && y <= 8) air = false;
                if (x <= 2 && y >= 3 && y <= 4) air = true;
                if (childDoor && x >= bounds.Width-3 && y >= floor && y <= floor+1) air = true;
                cells.Add(new Sv5InfillCell(new Sv5SpecialWorldPoint(origin.X+(mirror ? bounds.Width-1-x : x),origin.Y+y),
                    air ? Sv5InfillCellValue.Air : Sv5InfillCellValue.Solid,owner,recipe,parent,host));
            }
            return Array.AsReadOnly(cells.OrderBy(c => c.World).ToArray());
        }

        public static IReadOnlyList<Sv5InfillInstance> Split(IEnumerable<Sv5InfillCell> source, bool mirror = false)
        {
            var cells = source.ToArray();
            if (cells.GroupBy(c => c.World).Any(g => g.Count() != 1)) throw new ArgumentException("Duplicate owned world cell.");
            var catalog = new Dictionary<string,Sv5InfillPattern>();
            var result = new List<Sv5InfillInstance>();
            foreach (var group in cells.GroupBy(c => new { c.Owner, X = (int)Math.Floor(c.World.X/4.0)*4, Y = (int)Math.Floor(c.World.Y/4.0)*4 })
                         .OrderBy(g => g.Key.Owner,StringComparer.Ordinal).ThenBy(g => g.Key.Y).ThenBy(g => g.Key.X))
            {
                ushort mask = 0; var values = new Sv5InfillCellValue[16]; var first = group.First();
                foreach (var cell in group)
                {
                    int index = (cell.World.Y-group.Key.Y)*4+cell.World.X-group.Key.X;
                    mask |= (ushort)(1 << index); values[index] = cell.Value;
                    if (cell.Recipe != first.Recipe || cell.Host != first.Host || cell.Parent != first.Parent)
                        throw new ArgumentException("Conflicting instance provenance.");
                }
                var pattern = new Sv5InfillPattern(mask,values);
                if (!catalog.TryGetValue(pattern.Id,out var canonical)) catalog.Add(pattern.Id,canonical=pattern);
                result.Add(new Sv5InfillInstance(canonical,new Sv5SpecialWorldPoint(group.Key.X,group.Key.Y),
                    first.Owner,first.Recipe,first.Parent,first.Host,mirror));
            }
            return Array.AsReadOnly(result.ToArray());
        }

        public static IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue> Reconstruct(IEnumerable<Sv5InfillInstance> instances)
        {
            var result = new Dictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue>();
            foreach (var instance in instances)
            for (int i = 0; i < 16; i++)
            {
                if (((instance.Pattern.Mask >> i) & 1) == 0) continue;
                var point = new Sv5SpecialWorldPoint(instance.Origin.X+i%4,instance.Origin.Y+i/4);
                if (result.ContainsKey(point)) throw new ArgumentException("Duplicate pattern ownership at " + point);
                result.Add(point,instance.Pattern.Values[i]);
            }
            return new ReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue>(result);
        }

        public static IReadOnlyList<Sv5SpecialWorldPoint> SolidWindows(
            IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue> cells, int width = 624, int height = 416)
        {
            // Summed-area table inspects every overlapping window, including pattern/chunk boundaries.
            var sums = new int[width+1,height+1];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                sums[x+1,y+1] = sums[x,y+1]+sums[x+1,y]-sums[x,y]+(cells.TryGetValue(new Sv5SpecialWorldPoint(x,y),out var v) && v == Sv5InfillCellValue.Solid ? 1 : 0);
            var failures = new List<Sv5SpecialWorldPoint>();
            for (int y = 0; y <= height-6; y++) for (int x = 0; x <= width-6; x++)
                if (sums[x+6,y+6]-sums[x,y+6]-sums[x+6,y]+sums[x,y] == 36) failures.Add(new Sv5SpecialWorldPoint(x,y));
            return Array.AsReadOnly(failures.ToArray());
        }

        public static Sv5InfillStaticProof Screen(IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue> cells,
            Sv5SpecialWorldPoint start, Sv5SpecialWorldPoint goal)
        {
            bool Is(int x,int y,Sv5InfillCellValue value) => cells.TryGetValue(new Sv5SpecialWorldPoint(x,y),out var v) && v == value;
            bool Stand(Sv5SpecialWorldPoint p) => Is(p.X,p.Y-1,Sv5InfillCellValue.Solid) &&
                Is(p.X,p.Y,Sv5InfillCellValue.Air) && Is(p.X,p.Y+1,Sv5InfillCellValue.Air);
            // Conservative stepped swept volume for a 0.4 x 0.8 feet-pivot body.
            // Raise before translating uphill, translate before lowering downhill; no invented jump physics.
            bool Sweep(Sv5SpecialWorldPoint a,Sv5SpecialWorldPoint b)
            {
                double floor = Math.Max(a.Y,b.Y);
                for (int i = 0; i <= 10; i++)
                {
                    double cx = a.X+0.5+(b.X-a.X)*i/10.0;
                    for (int x = (int)Math.Floor(cx-0.2); x <= (int)Math.Floor(cx+0.2-1e-9); x++)
                    for (int y = (int)Math.Floor(floor); y <= (int)Math.Floor(floor+0.8-1e-9); y++)
                        if (!Is(x,y,Sv5InfillCellValue.Air)) return false;
                }
                return true;
            }
            Sv5SpecialWorldPoint[] Search(Sv5SpecialWorldPoint from,Sv5SpecialWorldPoint to)
            {
                if (!Stand(from) || !Stand(to)) return Array.Empty<Sv5SpecialWorldPoint>();
                var queue = new Queue<Sv5SpecialWorldPoint>(); var previous = new Dictionary<Sv5SpecialWorldPoint,Sv5SpecialWorldPoint>();
                previous[from] = from; queue.Enqueue(from);
                while (queue.Count > 0 && !previous.ContainsKey(to))
                {
                    var a = queue.Dequeue();
                    foreach (int dx in new[] { -1,1 }) foreach (int dy in new[] { 0,1,-1 })
                    {
                        var b = new Sv5SpecialWorldPoint(a.X+dx,a.Y+dy);
                        if (previous.ContainsKey(b) || !Stand(b) || !Sweep(a,b)) continue;
                        previous.Add(b,a); queue.Enqueue(b);
                    }
                }
                if (!previous.ContainsKey(to)) return Array.Empty<Sv5SpecialWorldPoint>();
                var path = new List<Sv5SpecialWorldPoint> { to };
                while (!path.Last().Equals(from)) path.Add(previous[path.Last()]);
                path.Reverse(); return path.ToArray();
            }
            var approach = Search(start,goal); var recovery = Search(goal,start);
            return new Sv5InfillStaticProof(approach,recovery,approach.Length > 0 && recovery.Length > 0 ?
                "STATIC_SCREEN_SUPPORTED_PLUS_ONE_AABB_0.4x0.8_HEADROOM_2;NOT_PLAYER_VERIFIED" : "STATIC_ROUTE_OR_RETURN_BLOCKED");
        }
    }
}
