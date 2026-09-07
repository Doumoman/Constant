using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.Visualization
{
    public sealed class MoonPalaceMicroPatternCandidate
    {
        internal MoonPalaceMicroPatternCandidate(
            ushort mask, int openCount, int largestOpenComponent, int openComponentCount,
            int north, int south, int west, int east, int floorSupport, int ceilingGap,
            int leftWall, int rightWall, string silhouette, string socket, string mirror,
            string densityBucket, string edgeSocketBucket, string supportClass,
            bool detailEligible, bool chunkPathAllowed, int score, int rank)
        {
            Mask = mask;
            CandidateId = "VIS01_MP_" + mask.ToString("X4", CultureInfo.InvariantCulture);
            MaskU16Hex = "0x" + mask.ToString("X4", CultureInfo.InvariantCulture);
            OpenCount = openCount;
            SolidCount = 16 - openCount;
            Density = SolidCount / 16.0;
            LargestOpenComponent = largestOpenComponent;
            OpenComponentCount = openComponentCount;
            NorthSocketBits = north;
            SouthSocketBits = south;
            WestSocketBits = west;
            EastSocketBits = east;
            FloorSupportBits = floorSupport;
            CeilingGapBits = ceilingGap;
            LeftWallBits = leftWall;
            RightWallBits = rightWall;
            SilhouetteSignature = silhouette;
            SocketSignature = socket;
            MirrorFamilySignature = mirror;
            DensityBucket = densityBucket;
            EdgeSocketBucket = edgeSocketBucket;
            SupportAffordanceClass = supportClass;
            HazardDetailEligibility = detailEligible ? "DETAIL_ELIGIBLE" : "STRUCTURE_ONLY";
            ChunkPathAllowed = chunkPathAllowed;
            RoleTags = BuildRoleTags(mask, openCount, openComponentCount, north, south, west, east, detailEligible, chunkPathAllowed);
            Score = score;
            Rank = rank;
        }

        public string CandidateId { get; }
        public ushort Mask { get; }
        public string MaskU16Hex { get; }
        public int OpenCount { get; }
        public int SolidCount { get; }
        public double Density { get; }
        public int LargestOpenComponent { get; }
        public int OpenComponentCount { get; }
        public int NorthSocketBits { get; }
        public int SouthSocketBits { get; }
        public int WestSocketBits { get; }
        public int EastSocketBits { get; }
        public int FloorSupportBits { get; }
        public int CeilingGapBits { get; }
        public int LeftWallBits { get; }
        public int RightWallBits { get; }
        public string SilhouetteSignature { get; }
        public string SocketSignature { get; }
        public string MirrorFamilySignature { get; }
        public string DensityBucket { get; }
        public string EdgeSocketBucket { get; }
        public string SupportAffordanceClass { get; }
        public string HazardDetailEligibility { get; }
        public string RoleTags { get; }
        public bool ChunkPathAllowed { get; }
        public int Score { get; }
        public int Rank { get; }

        public bool IsOpen(int x, int y)
        {
            if (x < 0 || x > 3 || y < 0 || y > 3) throw new ArgumentOutOfRangeException();
            return (Mask & (1 << (y * 4 + x))) == 0;
        }

        internal MoonPalaceMicroPatternCandidate WithRank(int rank) => new MoonPalaceMicroPatternCandidate(
            Mask, OpenCount, LargestOpenComponent, OpenComponentCount, NorthSocketBits, SouthSocketBits,
            WestSocketBits, EastSocketBits, FloorSupportBits, CeilingGapBits, LeftWallBits, RightWallBits,
            SilhouetteSignature, SocketSignature, MirrorFamilySignature, DensityBucket, EdgeSocketBucket,
            SupportAffordanceClass, HazardDetailEligibility == "DETAIL_ELIGIBLE", ChunkPathAllowed, Score, rank);

        private static string BuildRoleTags(ushort mask, int open, int components, int north, int south, int west, int east,
            bool detail, bool path)
        {
            var tags = new List<string>();
            if (mask == 0) tags.Add("QUIET_OPEN_ARCHETYPE");
            if (path) tags.Add("CHUNK_PATH");
            if (north == 15 && south == 15) tags.Add("VERTICAL_THROUGH");
            if (west == 15 && east == 15) tags.Add("HORIZONTAL_THROUGH");
            if (components == 1) tags.Add("CONNECTED_OPEN");
            if (open >= 12) tags.Add("LOW_DENSITY");
            if (detail) tags.Add("DETAIL_ELIGIBLE");
            return string.Join(";", tags);
        }
    }

    public sealed class MoonPalaceMicroPatternCandidateSet
    {
        internal MoonPalaceMicroPatternCandidateSet(int eligibleCount, IEnumerable<MoonPalaceMicroPatternCandidate> candidates,
            IReadOnlyDictionary<string, int> rejectionCounts, string digest)
        {
            EligibleMaskCount = eligibleCount;
            Candidates = new ReadOnlyCollection<MoonPalaceMicroPatternCandidate>(candidates.ToList());
            RejectionCounts = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(rejectionCounts, StringComparer.Ordinal));
            CanonicalDigest = digest;
        }

        public int RawMaskCount => MoonPalaceMicroPatternCandidateLibrary.RawMaskCount;
        public int EligibleMaskCount { get; }
        public IReadOnlyList<MoonPalaceMicroPatternCandidate> Candidates { get; }
        public IReadOnlyDictionary<string, int> RejectionCounts { get; }
        public string CanonicalDigest { get; }
    }

    public static class MoonPalaceMicroPatternCandidateLibrary
    {
        public const int RawMaskCount = 65536;
        public const int AcceptedCandidateCount = 500;

        public static MoonPalaceMicroPatternCandidateSet Build()
        {
            var eligible = new List<MoonPalaceMicroPatternCandidate>();
            var rejections = new SortedDictionary<string, int>(StringComparer.Ordinal)
            {
                ["all_solid"] = 0,
                ["open_count_out_of_range"] = 0,
                ["largest_open_component_below_4"] = 0,
                ["isolated_one_cell_open_pocket"] = 0,
                ["no_socket_or_support_affordance"] = 0,
                ["duplicate_mask_id"] = 0,
                ["coordinate_bounds"] = 0,
            };
            var seen = new HashSet<ushort>();
            for (var value = 0; value < RawMaskCount; value++)
            {
                var mask = (ushort)value;
                if (!seen.Add(mask)) { rejections["duplicate_mask_id"]++; continue; }
                if (mask == ushort.MaxValue) { rejections["all_solid"]++; continue; }

                var components = OpenComponentSizes(mask);
                var open = 16 - BitCount(mask);
                if (mask != 0 && (open < 4 || open > 14)) { rejections["open_count_out_of_range"]++; continue; }
                var largest = components.Count == 0 ? 0 : components.Max();
                if (largest < 4) { rejections["largest_open_component_below_4"]++; continue; }
                if (components.Any(size => size == 1)) { rejections["isolated_one_cell_open_pocket"]++; continue; }

                var north = EdgeBits(mask, 0, 3, true);
                var south = EdgeBits(mask, 0, 0, true);
                var west = EdgeBits(mask, 0, 0, false);
                var east = EdgeBits(mask, 3, 0, false);
                var floor = FloorSupport(mask);
                var support = SupportClass(mask, floor);
                if ((north | south | west | east) == 0 && support == "NO_AFFORDANCE")
                {
                    rejections["no_socket_or_support_affordance"]++;
                    continue;
                }

                var leftWall = WallBits(mask, 0);
                var rightWall = WallBits(mask, 3);
                var detail = components.Count > 1 || open <= 6;
                var chunkPath = components.Count == 1 && largest == open && CountNonZero(north, south, west, east) >= 2;
                var densityBucket = DensityBucket(open);
                var edgeBucket = EdgeBucket(north, south, west, east);
                var silhouette = Silhouette(mask);
                var mirrored = HorizontalMirror(mask);
                var mirrorFamily = "MF_" + Math.Min(mask, mirrored).ToString("X4", CultureInfo.InvariantCulture);
                var score = Score(open, largest, components.Count, north, south, west, east, floor, detail, chunkPath);
                eligible.Add(new MoonPalaceMicroPatternCandidate(mask, open, largest, components.Count, north, south, west,
                    east, floor, north, leftWall, rightWall, silhouette,
                    string.Format(CultureInfo.InvariantCulture, "N{0:X1}-S{1:X1}-W{2:X1}-E{3:X1}", north, south, west, east),
                    mirrorFamily, densityBucket, edgeBucket, support, detail, chunkPath, score, 0));
            }

            var selected = SelectDiverse(eligible);
            if (selected.Count != AcceptedCandidateCount)
                throw new InvalidOperationException("Deterministic candidate selection produced " + selected.Count + " records instead of 500.");
            var ranked = selected.Select((candidate, index) => candidate.WithRank(index + 1)).ToList();
            var digest = BakingCanonicalDigest.HashCanonicalLines(ranked.Select(CanonicalLine));
            return new MoonPalaceMicroPatternCandidateSet(eligible.Count, ranked, rejections, digest);
        }

        private static List<MoonPalaceMicroPatternCandidate> SelectDiverse(IReadOnlyList<MoonPalaceMicroPatternCandidate> eligible)
        {
            var selected = new List<MoonPalaceMicroPatternCandidate>();
            var masks = new HashSet<ushort>();
            var mirrors = new Dictionary<string, int>(StringComparer.Ordinal);

            Func<MoonPalaceMicroPatternCandidate, bool>[] routeCapabilities =
            {
                candidate => candidate.NorthSocketBits == 15 && ColumnOpen(candidate, 1),
                candidate => candidate.SouthSocketBits == 15 && ColumnOpen(candidate, 1),
                candidate => candidate.NorthSocketBits == 15 && !ColumnOpen(candidate, 1),
                candidate => candidate.SouthSocketBits == 15 && !ColumnOpen(candidate, 1),
            };
            foreach (var capability in routeCapabilities)
            {
                foreach (var candidate in eligible.Where(candidate => candidate.ChunkPathAllowed && capability(candidate))
                             .OrderByDescending(candidate => candidate.Score)
                             .ThenBy(candidate => StableOrder(candidate.Mask)).Take(32))
                    TryAdd(candidate, selected, masks, mirrors, 2);
            }

            var buckets = eligible.GroupBy(DiversityKey, StringComparer.Ordinal)
                .Select(group => new Bucket(group.Key, group.OrderByDescending(candidate => candidate.Score)
                    .ThenBy(candidate => StableOrder(candidate.Mask)).ToList()))
                .OrderBy(bucket => StableOrder(bucket.Key)).ThenBy(bucket => bucket.Key, StringComparer.Ordinal).ToList();

            for (var mirrorLimit = 1; mirrorLimit <= 2 && selected.Count < AcceptedCandidateCount; mirrorLimit++)
            {
                var progressed = true;
                while (selected.Count < AcceptedCandidateCount && progressed)
                {
                    progressed = false;
                    foreach (var bucket in buckets)
                    {
                        while (bucket.Cursor < bucket.Values.Count)
                        {
                            var candidate = bucket.Values[bucket.Cursor++];
                            if (!TryAdd(candidate, selected, masks, mirrors, mirrorLimit)) continue;
                            progressed = true;
                            break;
                        }
                        if (selected.Count == AcceptedCandidateCount) break;
                    }
                }
            }

            if (selected.Count < AcceptedCandidateCount)
            {
                foreach (var candidate in eligible.OrderByDescending(x => x.Score).ThenBy(x => StableOrder(x.Mask)))
                {
                    TryAdd(candidate, selected, masks, mirrors, int.MaxValue);
                    if (selected.Count == AcceptedCandidateCount) break;
                }
            }
            return selected;
        }

        private static bool TryAdd(MoonPalaceMicroPatternCandidate candidate, ICollection<MoonPalaceMicroPatternCandidate> selected,
            ISet<ushort> masks, IDictionary<string, int> mirrors, int mirrorLimit)
        {
            if (masks.Contains(candidate.Mask)) return false;
            mirrors.TryGetValue(candidate.MirrorFamilySignature, out var mirrorCount);
            if (mirrorCount >= mirrorLimit) return false;
            masks.Add(candidate.Mask);
            mirrors[candidate.MirrorFamilySignature] = mirrorCount + 1;
            selected.Add(candidate);
            return true;
        }

        private static string DiversityKey(MoonPalaceMicroPatternCandidate candidate) => string.Join("|",
            candidate.DensityBucket, candidate.EdgeSocketBucket, candidate.SilhouetteSignature,
            candidate.SupportAffordanceClass, candidate.HazardDetailEligibility);
        private static string CanonicalLine(MoonPalaceMicroPatternCandidate c) => string.Join("|",
            c.Rank.ToString(CultureInfo.InvariantCulture), c.CandidateId, c.MaskU16Hex,
            c.OpenCount.ToString(CultureInfo.InvariantCulture), c.SilhouetteSignature, c.SocketSignature,
            c.MirrorFamilySignature, c.DensityBucket, c.EdgeSocketBucket, c.SupportAffordanceClass,
            c.HazardDetailEligibility, c.ChunkPathAllowed ? "1" : "0", c.Score.ToString(CultureInfo.InvariantCulture));

        private static List<int> OpenComponentSizes(ushort mask)
        {
            var sizes = new List<int>();
            var seen = new bool[16];
            for (var start = 0; start < 16; start++)
            {
                if (seen[start] || (mask & (1 << start)) != 0) continue;
                var queue = new Queue<int>();
                queue.Enqueue(start);
                seen[start] = true;
                var size = 0;
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    size++;
                    var x = cell % 4;
                    var y = cell / 4;
                    Visit(x - 1, y, mask, seen, queue);
                    Visit(x + 1, y, mask, seen, queue);
                    Visit(x, y - 1, mask, seen, queue);
                    Visit(x, y + 1, mask, seen, queue);
                }
                sizes.Add(size);
            }
            return sizes;
        }

        private static void Visit(int x, int y, ushort mask, bool[] seen, Queue<int> queue)
        {
            if (x < 0 || x > 3 || y < 0 || y > 3) return;
            var index = y * 4 + x;
            if (seen[index] || (mask & (1 << index)) != 0) return;
            seen[index] = true;
            queue.Enqueue(index);
        }

        private static int BitCount(ushort mask)
        {
            var count = 0;
            var value = mask;
            while (value != 0) { count += value & 1; value >>= 1; }
            return count;
        }
        private static int EdgeBits(ushort mask, int fixedX, int fixedY, bool horizontal)
        {
            var bits = 0;
            for (var index = 0; index < 4; index++)
            {
                var x = horizontal ? index : fixedX;
                var y = horizontal ? fixedY : index;
                if ((mask & (1 << (y * 4 + x))) == 0) bits |= 1 << index;
            }
            return bits;
        }
        private static int FloorSupport(ushort mask)
        {
            var bits = 0;
            for (var x = 0; x < 4; x++)
            for (var y = 0; y < 4; y++)
            {
                if ((mask & (1 << (y * 4 + x))) != 0) continue;
                if (y == 0 || (mask & (1 << ((y - 1) * 4 + x))) != 0) bits |= 1 << x;
            }
            return bits;
        }
        private static int WallBits(ushort mask, int x)
        {
            var bits = 0;
            for (var y = 0; y < 4; y++) if ((mask & (1 << (y * 4 + x))) != 0) bits |= 1 << y;
            return bits;
        }
        private static bool ColumnOpen(MoonPalaceMicroPatternCandidate candidate, int x)
        {
            for (var y = 0; y < 4; y++) if (!candidate.IsOpen(x, y)) return false;
            return true;
        }
        private static ushort HorizontalMirror(ushort mask)
        {
            ushort result = 0;
            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
                if ((mask & (1 << (y * 4 + x))) != 0) result |= (ushort)(1 << (y * 4 + (3 - x)));
            return result;
        }
        private static string Silhouette(ushort mask)
        {
            var rows = new int[4];
            var columns = new int[4];
            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
                if ((mask & (1 << (y * 4 + x))) != 0) { rows[y]++; columns[x]++; }
            return "R" + string.Join(string.Empty, rows) + "-C" + string.Join(string.Empty, columns);
        }
        private static string DensityBucket(int open) => open >= 13 ? "QUIET_13_16" : open >= 10 ? "LIGHT_10_12" : open >= 7 ? "BALANCED_7_9" : "DENSE_4_6";
        private static string EdgeBucket(int north, int south, int west, int east)
        {
            var sides = CountNonZero(north, south, west, east);
            var full = new[] { north, south, west, east }.Count(value => value == 15);
            return "SIDES_" + sides.ToString(CultureInfo.InvariantCulture) + "_FULL_" + full.ToString(CultureInfo.InvariantCulture);
        }
        private static string SupportClass(ushort mask, int floor)
        {
            if (floor == 15) return "FULL_FLOOR_SUPPORT";
            if (floor != 0) return "PARTIAL_FLOOR_SUPPORT";
            return mask == 0 ? "OPEN_AFFORDANCE" : "NO_AFFORDANCE";
        }
        private static int CountNonZero(params int[] values) => values.Count(value => value != 0);
        private static int Score(int open, int largest, int components, int north, int south, int west, int east,
            int floor, bool detail, bool path) => largest * 100 + (components == 1 ? 80 : 0) +
            CountNonZero(north, south, west, east) * 25 + BitCount((ushort)floor) * 5 - Math.Abs(open - 10) * 6 +
            (path ? 100 : 0) + (detail ? 7 : 0);
        private static uint StableOrder(ushort mask) => StableOrder(mask.ToString("X4", CultureInfo.InvariantCulture));
        private static uint StableOrder(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var character in value) { hash ^= character; hash *= 16777619; }
                return hash;
            }
        }

        private sealed class Bucket
        {
            public Bucket(string key, List<MoonPalaceMicroPatternCandidate> values) { Key = key; Values = values; }
            public string Key { get; }
            public List<MoonPalaceMicroPatternCandidate> Values { get; }
            public int Cursor { get; set; }
        }
    }
}
