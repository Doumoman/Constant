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
    public enum MicroPatternTransformErrorCode
    {
        MissingDefinition = 1,
        SourceValidationFailed = 2,
        UnsupportedTransform = 3,
        TransformNotAllowed = 4,
        InvalidTransformedCoverage = 5,
    }

    public sealed class MicroPatternTransformError :
        IEquatable<MicroPatternTransformError>,
        IComparable<MicroPatternTransformError>
    {
        public MicroPatternTransformError(
            MicroPatternTransformErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternTransformErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternTransformError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternTransformError other)
        {
            return other != null &&
                   Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as MicroPatternTransformError);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class TransformedMicroPattern
    {
        private readonly ReadOnlyCollection<MicroPatternCell> cells;

        internal TransformedMicroPattern(
            MicroPatternDefinition sourceDefinition,
            string sourceDigest,
            MicroPatternTransform transform,
            IEnumerable<MicroPatternCell> cells,
            string stableDigest)
        {
            SourceDefinition = sourceDefinition ?? throw new ArgumentNullException(nameof(sourceDefinition));
            SourcePatternId = sourceDefinition.Id;
            SourceDigest = sourceDigest ?? string.Empty;
            Transform = transform;
            var copy = (cells ?? throw new ArgumentNullException(nameof(cells))).ToArray();
            Array.Sort(copy, CompareCells);
            this.cells = new ReadOnlyCollection<MicroPatternCell>(copy);
            StableDigest = stableDigest ?? string.Empty;
        }

        public MicroPatternDefinition SourceDefinition { get; }
        public MicroPatternId SourcePatternId { get; }
        public string SourceDigest { get; }
        public MicroPatternTransform Transform { get; }
        public IReadOnlyList<MicroPatternCell> Cells => cells;
        public string StableDigest { get; }

        private static int CompareCells(MicroPatternCell left, MicroPatternCell right)
        {
            var comparison = left.Coordinate.Y.CompareTo(right.Coordinate.Y);
            return comparison != 0 ? comparison : left.Coordinate.X.CompareTo(right.Coordinate.X);
        }
    }

    public sealed class MicroPatternTransformResult
    {
        private readonly ReadOnlyCollection<MicroPatternTransformError> errors;

        internal MicroPatternTransformResult(
            TransformedMicroPattern pattern,
            IEnumerable<MicroPatternTransformError> errors)
        {
            var copy = (errors ?? Array.Empty<MicroPatternTransformError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<MicroPatternTransformError>(copy);
            Pattern = copy.Length == 0 ? pattern : null;
            StableDigest = Pattern == null ? string.Empty : Pattern.StableDigest;
        }

        public bool Success => Pattern != null && errors.Count == 0;
        public TransformedMicroPattern Pattern { get; }
        public IReadOnlyList<MicroPatternTransformError> Errors => errors;
        public string StableDigest { get; }
    }

    public static class MicroPatternTransformer
    {
        public static MicroPatternTransformResult Transform(
            MicroPatternDefinition definition,
            MicroPatternTransform transform)
        {
            var errors = new List<MicroPatternTransformError>();
            if (definition == null)
            {
                errors.Add(new MicroPatternTransformError(
                    MicroPatternTransformErrorCode.MissingDefinition,
                    "definition",
                    "Definition is required."));
                return new MicroPatternTransformResult(null, errors);
            }

            var validation = MicroPatternValidator.Validate(definition);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    errors.Add(new MicroPatternTransformError(
                        MicroPatternTransformErrorCode.SourceValidationFailed,
                        error.Path,
                        error.ToString()));
                }
            }

            if (!IsDefined(transform))
            {
                errors.Add(new MicroPatternTransformError(
                    MicroPatternTransformErrorCode.UnsupportedTransform,
                    "transform",
                    Number((int)transform)));
            }
            else if (!definition.AllowedTransforms.Contains(transform))
            {
                errors.Add(new MicroPatternTransformError(
                    MicroPatternTransformErrorCode.TransformNotAllowed,
                    "transform",
                    transform.ToString()));
            }

            if (errors.Count != 0)
            {
                return new MicroPatternTransformResult(null, errors);
            }

            var cells = definition.Cells
                .Select(cell => new MicroPatternCell(
                    TransformCoordinate(cell.Coordinate, transform),
                    cell.Instructions.Select(CloneInstruction)))
                .OrderBy(cell => cell.Coordinate.Y)
                .ThenBy(cell => cell.Coordinate.X)
                .ToArray();

            if (!HasExactCoverage(cells))
            {
                errors.Add(new MicroPatternTransformError(
                    MicroPatternTransformErrorCode.InvalidTransformedCoverage,
                    "cells",
                    "Transform must publish exactly one cell at every coordinate in 0..3."));
                return new MicroPatternTransformResult(null, errors);
            }

            var digest = ComputeDigest(validation.StableDigest, transform, cells);
            return new MicroPatternTransformResult(
                new TransformedMicroPattern(
                    definition,
                    validation.StableDigest,
                    transform,
                    cells,
                    digest),
                errors);
        }

        private static LocalTileCoord TransformCoordinate(
            LocalTileCoord source,
            MicroPatternTransform transform)
        {
            switch (transform)
            {
                case MicroPatternTransform.R0:
                    return source;
                case MicroPatternTransform.MirrorX:
                    return new LocalTileCoord(3 - source.X, source.Y);
                case MicroPatternTransform.MirrorY:
                    return new LocalTileCoord(source.X, 3 - source.Y);
                case MicroPatternTransform.R180:
                    return new LocalTileCoord(3 - source.X, 3 - source.Y);
                default:
                    throw new ArgumentOutOfRangeException(nameof(transform));
            }
        }

        private static MicroPatternInstruction CloneInstruction(MicroPatternInstruction source)
        {
            return new MicroPatternInstruction(source.Layer, source.Operation, source.PayloadId);
        }

        private static bool HasExactCoverage(IReadOnlyCollection<MicroPatternCell> cells)
        {
            if (cells.Count != MicroPatternDefinition.RequiredCellCount) return false;
            var seen = new HashSet<LocalTileCoord>();
            foreach (var cell in cells)
            {
                if (cell == null ||
                    cell.Coordinate.X < 0 || cell.Coordinate.X >= MicroPatternDefinition.RequiredWidth ||
                    cell.Coordinate.Y < 0 || cell.Coordinate.Y >= MicroPatternDefinition.RequiredHeight ||
                    !seen.Add(cell.Coordinate))
                {
                    return false;
                }
            }

            return seen.Count == MicroPatternDefinition.RequiredCellCount;
        }

        private static bool IsDefined(MicroPatternTransform transform)
        {
            return transform >= MicroPatternTransform.R0 && transform <= MicroPatternTransform.R180;
        }

        private static string ComputeDigest(
            string sourceDigest,
            MicroPatternTransform transform,
            IEnumerable<MicroPatternCell> cells)
        {
            var material = new StringBuilder();
            Append(material, "SOURCE", sourceDigest);
            Append(material, "TRANSFORM", transform.ToString());
            foreach (var cell in cells.OrderBy(value => value.Coordinate.Y)
                         .ThenBy(value => value.Coordinate.X))
            {
                Append(material, "CELL", Number(cell.Coordinate.X), Number(cell.Coordinate.Y));
                foreach (var instruction in cell.Instructions.OrderBy(value => (int)value.Layer))
                {
                    Append(material, "INSTRUCTION", instruction.Layer.ToString(),
                        instruction.Operation.ToString(), instruction.PayloadId);
                }
            }

            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(Encoding.UTF8.GetBytes(material.ToString()))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
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

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
