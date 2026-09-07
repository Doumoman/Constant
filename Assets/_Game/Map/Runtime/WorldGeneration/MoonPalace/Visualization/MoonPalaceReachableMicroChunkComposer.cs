using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.Visualization
{
    public struct MoonPalaceVisCellCoord : IEquatable<MoonPalaceVisCellCoord>
    {
        public MoonPalaceVisCellCoord(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public bool Equals(MoonPalaceVisCellCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is MoonPalaceVisCellCoord other && Equals(other);
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + ":" + Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class MoonPalaceChunkPatternPlacement
    {
        public MoonPalaceChunkPatternPlacement(int slotIndex, int patternX, int patternY, MoonPalaceMicroPatternCandidate candidate)
        {
            SlotIndex = slotIndex;
            PatternX = patternX;
            PatternY = patternY;
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        }
        public int SlotIndex { get; }
        public int PatternX { get; }
        public int PatternY { get; }
        public MoonPalaceMicroPatternCandidate Candidate { get; }
    }

    public sealed class MoonPalaceChunkValidation
    {
        public MoonPalaceChunkValidation(bool reachable, string failureOwner, int openCount, int componentCount,
            int largestComponent, int fallbackCarveCount, int silentAutoRepairCount)
        {
            Reachable = reachable;
            FailureOwner = failureOwner ?? string.Empty;
            OpenCount = openCount;
            OpenComponentCount = componentCount;
            LargestOpenComponent = largestComponent;
            FallbackCarveCount = fallbackCarveCount;
            SilentAutoRepairCount = silentAutoRepairCount;
        }
        public bool Reachable { get; }
        public string FailureOwner { get; }
        public int OpenCount { get; }
        public int OpenComponentCount { get; }
        public int LargestOpenComponent { get; }
        public int FallbackCarveCount { get; }
        public int SilentAutoRepairCount { get; }
        public string ConnectivitySummary => string.Join(";", "open=" + OpenCount.ToString(CultureInfo.InvariantCulture),
            "components=" + OpenComponentCount.ToString(CultureInfo.InvariantCulture),
            "largest=" + LargestOpenComponent.ToString(CultureInfo.InvariantCulture));
    }

    public sealed class MoonPalaceReachableMicroChunk
    {
        internal MoonPalaceReachableMicroChunk(string id, int index, int x, int y,
            IEnumerable<MoonPalaceChunkPatternPlacement> placements, bool[] openCells,
            IEnumerable<MoonPalaceVisCellCoord> entries, IEnumerable<MoonPalaceVisCellCoord> exits,
            IEnumerable<MoonPalaceVisCellCoord> required, IEnumerable<MoonPalaceVisCellCoord> recovery,
            IEnumerable<MoonPalaceVisCellCoord> protectedCells, IEnumerable<MoonPalaceVisCellCoord> markers,
            MoonPalaceChunkValidation validation, string digest)
        {
            ChunkId = id;
            ChunkIndex = index;
            ChunkX = x;
            ChunkY = y;
            PatternPlacements = ReadOnly(placements);
            OpenCells = new ReadOnlyCollection<bool>((bool[])openCells.Clone());
            EntrySockets = ReadOnly(entries);
            ExitSockets = ReadOnly(exits);
            RequiredPathCells = ReadOnly(required);
            RecoveryPathCells = ReadOnly(recovery);
            ProtectedCells = ReadOnly(protectedCells);
            MarkerSlots = ReadOnly(markers);
            Validation = validation;
            ChunkDigest = digest;
        }
        public string ChunkId { get; }
        public int ChunkIndex { get; }
        public int ChunkX { get; }
        public int ChunkY { get; }
        public IReadOnlyList<MoonPalaceChunkPatternPlacement> PatternPlacements { get; }
        public IReadOnlyList<bool> OpenCells { get; }
        public IReadOnlyList<MoonPalaceVisCellCoord> EntrySockets { get; }
        public IReadOnlyList<MoonPalaceVisCellCoord> ExitSockets { get; }
        public IReadOnlyList<MoonPalaceVisCellCoord> RequiredPathCells { get; }
        public IReadOnlyList<MoonPalaceVisCellCoord> RecoveryPathCells { get; }
        public IReadOnlyList<MoonPalaceVisCellCoord> ProtectedCells { get; }
        public IReadOnlyList<MoonPalaceVisCellCoord> MarkerSlots { get; }
        public MoonPalaceChunkValidation Validation { get; }
        public string ChunkDigest { get; }
        public int CompositionAttemptCount => 1;
        public bool IsOpen(int x, int y) => OpenCells[y * MoonPalaceReachableMicroChunkComposer.ChunkWidth + x];
        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>(values.ToList());
    }

    public static class MoonPalaceReachableMicroChunkComposer
    {
        public const int PatternColumns = 3;
        public const int PatternRows = 2;
        public const int PatternsPerChunk = 6;
        public const int ChunkWidth = 12;
        public const int ChunkHeight = 8;
        public const int ChunkCellCount = 96;

        public static MoonPalaceReachableMicroChunk Compose(
            MoonPalaceMicroPatternCandidateSet candidateSet, int seed, int chunkIndex, int chunkX, int chunkY)
        {
            if (candidateSet == null) throw new ArgumentNullException(nameof(candidateSet));
            if (candidateSet.Candidates.Count != MoonPalaceMicroPatternCandidateLibrary.AcceptedCandidateCount)
                throw new ArgumentException("VIS01 composition requires the complete 500-candidate set.", nameof(candidateSet));
            if (chunkIndex < 0) throw new ArgumentOutOfRangeException(nameof(chunkIndex));

            var placements = new List<MoonPalaceChunkPatternPlacement>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            for (var slot = 0; slot < PatternsPerChunk; slot++)
            {
                var patternX = slot % PatternColumns;
                var patternY = slot / PatternColumns;
                var pool = candidateSet.Candidates.Where(candidate => candidate.ChunkPathAllowed &&
                    (patternY == 0 ? candidate.NorthSocketBits == 15 : candidate.SouthSocketBits == 15) &&
                    (patternX != 1 || ColumnOpen(candidate, 1)) && !used.Contains(candidate.CandidateId))
                    .OrderBy(candidate => candidate.Rank).ToList();
                if (pool.Count == 0)
                    throw new InvalidOperationException("No generated candidate satisfies slot " + slot.ToString(CultureInfo.InvariantCulture) + ".");
                var selected = pool[PositiveModulo(Stable(seed, chunkIndex, slot, chunkX, chunkY), pool.Count)];
                used.Add(selected.CandidateId);
                placements.Add(new MoonPalaceChunkPatternPlacement(slot, patternX, patternY, selected));
            }

            var open = new bool[ChunkCellCount];
            foreach (var placement in placements)
            for (var localY = 0; localY < 4; localY++)
            for (var localX = 0; localX < 4; localX++)
            {
                var x = placement.PatternX * 4 + localX;
                var y = placement.PatternY * 4 + localY;
                open[y * ChunkWidth + x] = placement.Candidate.IsOpen(localX, localY);
            }

            var required = Enumerable.Range(0, ChunkWidth).Select(x => new MoonPalaceVisCellCoord(x, 3))
                .Concat(Enumerable.Range(0, ChunkHeight).Select(y => new MoonPalaceVisCellCoord(5, y)))
                .Distinct().OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray();
            var recovery = Enumerable.Range(0, ChunkWidth).Select(x => new MoonPalaceVisCellCoord(x, 4)).ToArray();
            var protectedCells = required.Concat(recovery).Distinct().OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray();
            var entries = new[] { new MoonPalaceVisCellCoord(0, 3), new MoonPalaceVisCellCoord(0, 4), new MoonPalaceVisCellCoord(5, 0) };
            var exits = new[] { new MoonPalaceVisCellCoord(11, 3), new MoonPalaceVisCellCoord(11, 4), new MoonPalaceVisCellCoord(5, 7) };
            var validation = Validate(open, entries, exits, required, recovery, protectedCells, 0, 0);
            var markers = SelectMarkers(open, protectedCells, seed, chunkIndex);
            var id = "VIS01_CHUNK_" + chunkIndex.ToString("D2", CultureInfo.InvariantCulture);
            var digest = ComputeDigest(id, chunkX, chunkY, placements, open, required, recovery, protectedCells, markers, validation);
            return new MoonPalaceReachableMicroChunk(id, chunkIndex, chunkX, chunkY, placements, open, entries, exits,
                required, recovery, protectedCells, markers, validation, digest);
        }

        public static MoonPalaceChunkValidation Validate(
            IReadOnlyList<bool> openCells,
            IEnumerable<MoonPalaceVisCellCoord> entrySockets,
            IEnumerable<MoonPalaceVisCellCoord> exitSockets,
            IEnumerable<MoonPalaceVisCellCoord> requiredPathCells,
            IEnumerable<MoonPalaceVisCellCoord> recoveryPathCells,
            IEnumerable<MoonPalaceVisCellCoord> protectedCells,
            int fallbackCarveCount,
            int silentAutoRepairCount)
        {
            if (openCells == null || openCells.Count != ChunkCellCount)
                return new MoonPalaceChunkValidation(false, "CHUNK_CELL_COUNT", 0, 0, 0, fallbackCarveCount, silentAutoRepairCount);
            if (fallbackCarveCount != 0 || silentAutoRepairCount != 0)
                return Analyze(openCells, false, fallbackCarveCount != 0 ? "FALLBACK_CARVE_FORBIDDEN" : "SILENT_AUTO_REPAIR_FORBIDDEN",
                    fallbackCarveCount, silentAutoRepairCount);

            var entries = entrySockets?.ToArray() ?? Array.Empty<MoonPalaceVisCellCoord>();
            var exits = exitSockets?.ToArray() ?? Array.Empty<MoonPalaceVisCellCoord>();
            if (entries.Length == 0 || exits.Length == 0)
                return Analyze(openCells, false, "ENTRY_OR_EXIT_SOCKET_MISSING", fallbackCarveCount, silentAutoRepairCount);
            if ((requiredPathCells ?? Array.Empty<MoonPalaceVisCellCoord>()).Any(cell => !Open(openCells, cell)))
                return Analyze(openCells, false, "REQUIRED_PATH_SOLID", fallbackCarveCount, silentAutoRepairCount);
            if ((recoveryPathCells ?? Array.Empty<MoonPalaceVisCellCoord>()).Any(cell => !Open(openCells, cell)))
                return Analyze(openCells, false, "RECOVERY_PATH_SOLID", fallbackCarveCount, silentAutoRepairCount);
            if ((protectedCells ?? Array.Empty<MoonPalaceVisCellCoord>()).Any(cell => !Open(openCells, cell)))
                return Analyze(openCells, false, "PROTECTED_CELL_OVERWRITTEN", fallbackCarveCount, silentAutoRepairCount);

            var reachable = entries.Where(cell => Open(openCells, cell)).SelectMany(cell => Flood(openCells, cell)).Distinct().ToHashSet();
            if (!exits.Any(exit => reachable.Contains(exit)))
                return Analyze(openCells, false, "ENTRY_EXIT_UNREACHABLE", fallbackCarveCount, silentAutoRepairCount);
            return Analyze(openCells, true, string.Empty, fallbackCarveCount, silentAutoRepairCount);
        }

        private static MoonPalaceChunkValidation Analyze(IReadOnlyList<bool> openCells, bool reachable, string owner,
            int fallbackCarveCount, int silentAutoRepairCount)
        {
            var seen = new HashSet<MoonPalaceVisCellCoord>();
            var sizes = new List<int>();
            for (var y = 0; y < ChunkHeight; y++)
            for (var x = 0; x < ChunkWidth; x++)
            {
                var start = new MoonPalaceVisCellCoord(x, y);
                if (!Open(openCells, start) || seen.Contains(start)) continue;
                var component = Flood(openCells, start).ToArray();
                foreach (var cell in component) seen.Add(cell);
                sizes.Add(component.Length);
            }
            return new MoonPalaceChunkValidation(reachable, owner, openCells.Count(value => value), sizes.Count,
                sizes.Count == 0 ? 0 : sizes.Max(), fallbackCarveCount, silentAutoRepairCount);
        }

        private static IEnumerable<MoonPalaceVisCellCoord> Flood(IReadOnlyList<bool> openCells, MoonPalaceVisCellCoord start)
        {
            var queue = new Queue<MoonPalaceVisCellCoord>();
            var seen = new HashSet<MoonPalaceVisCellCoord>();
            queue.Enqueue(start);
            seen.Add(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                yield return cell;
                var neighbors = new[]
                {
                    new MoonPalaceVisCellCoord(cell.X - 1, cell.Y), new MoonPalaceVisCellCoord(cell.X + 1, cell.Y),
                    new MoonPalaceVisCellCoord(cell.X, cell.Y - 1), new MoonPalaceVisCellCoord(cell.X, cell.Y + 1),
                };
                foreach (var next in neighbors)
                {
                    if (!Open(openCells, next) || !seen.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }
        }

        private static IReadOnlyList<MoonPalaceVisCellCoord> SelectMarkers(IReadOnlyList<bool> open,
            IEnumerable<MoonPalaceVisCellCoord> protectedCells, int seed, int chunkIndex)
        {
            var protectedSet = new HashSet<MoonPalaceVisCellCoord>(protectedCells);
            var available = new List<MoonPalaceVisCellCoord>();
            for (var y = 0; y < ChunkHeight; y++)
            for (var x = 0; x < ChunkWidth; x++)
            {
                var cell = new MoonPalaceVisCellCoord(x, y);
                if (Open(open, cell) && !protectedSet.Contains(cell)) available.Add(cell);
            }
            return new ReadOnlyCollection<MoonPalaceVisCellCoord>(available.OrderBy(cell => Stable(seed, chunkIndex, cell.X, cell.Y, 97))
                .ThenBy(cell => cell.Y).ThenBy(cell => cell.X).Take(2).ToList());
        }

        private static string ComputeDigest(string id, int x, int y, IEnumerable<MoonPalaceChunkPatternPlacement> placements,
            IEnumerable<bool> open, IEnumerable<MoonPalaceVisCellCoord> required, IEnumerable<MoonPalaceVisCellCoord> recovery,
            IEnumerable<MoonPalaceVisCellCoord> protectedCells, IEnumerable<MoonPalaceVisCellCoord> markers, MoonPalaceChunkValidation validation)
        {
            var lines = new List<string> { id + "|" + x + "|" + y };
            lines.AddRange(placements.OrderBy(p => p.SlotIndex).Select(p => "P|" + p.SlotIndex + "|" + p.PatternX + "|" + p.PatternY + "|" + p.Candidate.CandidateId));
            lines.Add("OPEN|" + string.Concat(open.Select(value => value ? "1" : "0")));
            lines.Add("REQ|" + string.Join(",", required));
            lines.Add("REC|" + string.Join(",", recovery));
            lines.Add("PRO|" + string.Join(",", protectedCells));
            lines.Add("MARK|" + string.Join(",", markers));
            lines.Add("VALID|" + (validation.Reachable ? "1" : "0") + "|" + validation.FailureOwner + "|0|0");
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static bool ColumnOpen(MoonPalaceMicroPatternCandidate candidate, int x)
        {
            for (var y = 0; y < 4; y++) if (!candidate.IsOpen(x, y)) return false;
            return true;
        }
        private static bool Open(IReadOnlyList<bool> cells, MoonPalaceVisCellCoord cell) =>
            cell.X >= 0 && cell.X < ChunkWidth && cell.Y >= 0 && cell.Y < ChunkHeight && cells[cell.Y * ChunkWidth + cell.X];
        private static int PositiveModulo(int value, int modulus) => (int)((uint)value % (uint)modulus);
        private static int Stable(params int[] values)
        {
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var value in values) { hash ^= value; hash *= 16777619; }
                return hash;
            }
        }
    }
}
