using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public readonly struct MicroPatternPlacement : IEquatable<MicroPatternPlacement>
    {
        public MicroPatternPlacement(LocalTileCoord origin)
        {
            Origin = origin;
        }

        public LocalTileCoord Origin { get; }

        public bool Equals(MicroPatternPlacement other) => Origin.Equals(other.Origin);
        public override bool Equals(object obj) => obj is MicroPatternPlacement other && Equals(other);
        public override int GetHashCode() => Origin.GetHashCode();
    }

    public enum MicroPatternProtectedSourceKind
    {
        RouteSpine = 1,
        TraversalEnvelope = 2,
        BoundaryProtectedOpen = 3,
        SpecialFixedEntry = 4,
    }

    public sealed class MicroPatternProtectedCell :
        IEquatable<MicroPatternProtectedCell>,
        IComparable<MicroPatternProtectedCell>
    {
        public MicroPatternProtectedCell(
            LocalTileCoord targetCoordinate,
            MicroPatternProtectedSourceKind sourceKind,
            string sourceId)
        {
            TargetCoordinate = targetCoordinate;
            SourceKind = sourceKind;
            SourceId = sourceId ?? string.Empty;
        }

        public LocalTileCoord TargetCoordinate { get; }
        public MicroPatternProtectedSourceKind SourceKind { get; }
        public string SourceId { get; }

        public int CompareTo(MicroPatternProtectedCell other)
        {
            if (other == null) return -1;
            var comparison = TargetCoordinate.Y.CompareTo(other.TargetCoordinate.Y);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.X.CompareTo(other.TargetCoordinate.X);
            if (comparison != 0) return comparison;
            comparison = ((int)SourceKind).CompareTo((int)other.SourceKind);
            return comparison != 0
                ? comparison
                : string.Compare(SourceId, other.SourceId, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternProtectedCell other)
        {
            return other != null &&
                   TargetCoordinate.Equals(other.TargetCoordinate) &&
                   SourceKind == other.SourceKind &&
                   string.Equals(SourceId, other.SourceId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as MicroPatternProtectedCell);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = TargetCoordinate.GetHashCode();
                hash = (hash * 397) ^ (int)SourceKind;
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SourceId);
            }
        }

        public override string ToString()
        {
            return Coordinate(TargetCoordinate) + "|" + SourceKind + "|" + SourceId;
        }

        private static string Coordinate(LocalTileCoord value)
        {
            return value.X.ToString(CultureInfo.InvariantCulture) + "," +
                   value.Y.ToString(CultureInfo.InvariantCulture);
        }
    }

    public sealed class MicroPatternProtectedMaskEntry
    {
        private readonly ReadOnlyCollection<MicroPatternProtectedCell> provenance;

        internal MicroPatternProtectedMaskEntry(
            LocalTileCoord targetCoordinate,
            IEnumerable<MicroPatternProtectedCell> provenance)
        {
            TargetCoordinate = targetCoordinate;
            var copy = provenance.Distinct().OrderBy(value => value).ToArray();
            this.provenance = new ReadOnlyCollection<MicroPatternProtectedCell>(copy);
        }

        public LocalTileCoord TargetCoordinate { get; }
        public IReadOnlyList<MicroPatternProtectedCell> Provenance => provenance;
    }

    public sealed class MicroPatternProtectedMask
    {
        private readonly ReadOnlyCollection<MicroPatternProtectedMaskEntry> entries;
        private readonly IReadOnlyDictionary<LocalTileCoord, MicroPatternProtectedMaskEntry> byCoordinate;

        internal MicroPatternProtectedMask(
            IEnumerable<MicroPatternProtectedMaskEntry> entries,
            string stableDigest)
        {
            var copy = entries.OrderBy(value => value.TargetCoordinate.Y)
                .ThenBy(value => value.TargetCoordinate.X)
                .ToArray();
            this.entries = new ReadOnlyCollection<MicroPatternProtectedMaskEntry>(copy);
            byCoordinate = new ReadOnlyDictionary<LocalTileCoord, MicroPatternProtectedMaskEntry>(
                copy.ToDictionary(value => value.TargetCoordinate));
            StableDigest = stableDigest ?? string.Empty;
        }

        public IReadOnlyList<MicroPatternProtectedMaskEntry> Entries => entries;
        public string StableDigest { get; }

        public bool TryGetEntry(
            LocalTileCoord targetCoordinate,
            out MicroPatternProtectedMaskEntry entry)
        {
            return byCoordinate.TryGetValue(targetCoordinate, out entry);
        }
    }

    public enum MicroPatternProtectedMaskErrorCode
    {
        CoordinateOverflow = 1,
        MissingProtectedCell = 2,
        InvalidSourceKind = 3,
        InvalidSourceId = 4,
    }

    public sealed class MicroPatternProtectedMaskError :
        IEquatable<MicroPatternProtectedMaskError>,
        IComparable<MicroPatternProtectedMaskError>
    {
        public MicroPatternProtectedMaskError(
            MicroPatternProtectedMaskErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternProtectedMaskErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternProtectedMaskError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternProtectedMaskError other)
        {
            return other != null && Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as MicroPatternProtectedMaskError);
        public override int GetHashCode() => ToString().GetHashCode();
        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class MicroPatternProtectedMaskResult
    {
        private readonly ReadOnlyCollection<MicroPatternProtectedMaskError> errors;

        internal MicroPatternProtectedMaskResult(
            MicroPatternProtectedMask mask,
            IEnumerable<MicroPatternProtectedMaskError> errors)
        {
            var copy = (errors ?? Array.Empty<MicroPatternProtectedMaskError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<MicroPatternProtectedMaskError>(copy);
            Mask = copy.Length == 0 ? mask : null;
            StableDigest = Mask == null ? string.Empty : Mask.StableDigest;
        }

        public bool Success => Mask != null && errors.Count == 0;
        public MicroPatternProtectedMask Mask { get; }
        public IReadOnlyList<MicroPatternProtectedMaskError> Errors => errors;
        public string StableDigest { get; }
    }

    public static class MicroPatternProtectedMaskBuilder
    {
        public static MicroPatternProtectedMaskResult Build(
            MicroPatternPlacement placement,
            IEnumerable<MicroPatternProtectedCell> protectedCells)
        {
            var errors = new List<MicroPatternProtectedMaskError>();
            int maximumX;
            int maximumY;
            try
            {
                maximumX = checked(placement.Origin.X + MicroPatternDefinition.RequiredWidth - 1);
                maximumY = checked(placement.Origin.Y + MicroPatternDefinition.RequiredHeight - 1);
            }
            catch (OverflowException)
            {
                errors.Add(new MicroPatternProtectedMaskError(
                    MicroPatternProtectedMaskErrorCode.CoordinateOverflow,
                    "placement.origin",
                    Coordinate(placement.Origin)));
                return new MicroPatternProtectedMaskResult(null, errors);
            }

            var snapshot = protectedCells == null
                ? Array.Empty<MicroPatternProtectedCell>()
                : protectedCells.ToArray();
            for (var index = 0; index < snapshot.Length; index++)
            {
                var cell = snapshot[index];
                if (cell == null)
                {
                    errors.Add(new MicroPatternProtectedMaskError(
                        MicroPatternProtectedMaskErrorCode.MissingProtectedCell,
                        "protectedCells[" + Number(index) + "]",
                        "Protected cell is required."));
                    continue;
                }

                if (!IsDefined(cell.SourceKind))
                {
                    errors.Add(new MicroPatternProtectedMaskError(
                        MicroPatternProtectedMaskErrorCode.InvalidSourceKind,
                        "protectedCells[" + Number(index) + "].sourceKind",
                        Number((int)cell.SourceKind)));
                }

                if (!IsStableId(cell.SourceId))
                {
                    errors.Add(new MicroPatternProtectedMaskError(
                        MicroPatternProtectedMaskErrorCode.InvalidSourceId,
                        "protectedCells[" + Number(index) + "].sourceId",
                        cell.SourceId));
                }
            }

            if (errors.Count != 0)
            {
                return new MicroPatternProtectedMaskResult(null, errors);
            }

            var intersecting = snapshot
                .Where(cell => cell.TargetCoordinate.X >= placement.Origin.X &&
                               cell.TargetCoordinate.X <= maximumX &&
                               cell.TargetCoordinate.Y >= placement.Origin.Y &&
                               cell.TargetCoordinate.Y <= maximumY)
                .Distinct()
                .OrderBy(cell => cell)
                .ToArray();
            var entries = intersecting
                .GroupBy(cell => cell.TargetCoordinate)
                .Select(group => new MicroPatternProtectedMaskEntry(group.Key, group))
                .OrderBy(entry => entry.TargetCoordinate.Y)
                .ThenBy(entry => entry.TargetCoordinate.X)
                .ToArray();
            var digest = ComputeDigest(placement, entries);
            return new MicroPatternProtectedMaskResult(
                new MicroPatternProtectedMask(entries, digest),
                errors);
        }

        private static string ComputeDigest(
            MicroPatternPlacement placement,
            IEnumerable<MicroPatternProtectedMaskEntry> entries)
        {
            var material = new StringBuilder();
            Append(material, "ORIGIN", Number(placement.Origin.X), Number(placement.Origin.Y));
            foreach (var entry in entries)
            {
                Append(material, "CELL", Number(entry.TargetCoordinate.X),
                    Number(entry.TargetCoordinate.Y));
                foreach (var source in entry.Provenance)
                {
                    Append(material, "SOURCE", source.SourceKind.ToString(), source.SourceId);
                }
            }

            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(Encoding.UTF8.GetBytes(material.ToString()))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static bool IsDefined(MicroPatternProtectedSourceKind value)
        {
            return value >= MicroPatternProtectedSourceKind.RouteSpine &&
                   value <= MicroPatternProtectedSourceKind.SpecialFixedEntry;
        }

        private static bool IsStableId(string value)
        {
            if (string.IsNullOrEmpty(value) || value[0] < 'A' || value[0] > 'Z') return false;
            for (var index = 1; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') &&
                    character != '_') return false;
            }
            return true;
        }

        private static void Append(StringBuilder target, params string[] fields)
        {
            foreach (var field in fields)
            {
                var value = field ?? string.Empty;
                target.Append(value.Length.ToString(CultureInfo.InvariantCulture));
                target.Append(':');
                target.Append(value);
            }
            target.Append('\n');
        }

        private static string Coordinate(LocalTileCoord value)
        {
            return Number(value.X) + "," + Number(value.Y);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
