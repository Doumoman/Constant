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
    public sealed class MicroPatternPreparedCell
    {
        private readonly ReadOnlyCollection<MicroPatternInstruction> instructions;

        internal MicroPatternPreparedCell(
            LocalTileCoord localCoordinate,
            LocalTileCoord targetCoordinate,
            IEnumerable<MicroPatternInstruction> instructions)
        {
            LocalCoordinate = localCoordinate;
            TargetCoordinate = targetCoordinate;
            var copy = instructions.Select(CloneInstruction)
                .OrderBy(value => (int)value.Layer)
                .ToArray();
            this.instructions = new ReadOnlyCollection<MicroPatternInstruction>(copy);
        }

        public LocalTileCoord LocalCoordinate { get; }
        public LocalTileCoord TargetCoordinate { get; }
        public IReadOnlyList<MicroPatternInstruction> Instructions => instructions;

        private static MicroPatternInstruction CloneInstruction(MicroPatternInstruction source)
        {
            return new MicroPatternInstruction(source.Layer, source.Operation, source.PayloadId);
        }
    }

    public sealed class MicroPatternProtectedHit
    {
        private readonly ReadOnlyCollection<MicroPatternProtectedCell> provenance;

        internal MicroPatternProtectedHit(
            LocalTileCoord localCoordinate,
            LocalTileCoord targetCoordinate,
            int removedWriteCount,
            IEnumerable<MicroPatternProtectedCell> provenance)
        {
            LocalCoordinate = localCoordinate;
            TargetCoordinate = targetCoordinate;
            RemovedWriteCount = removedWriteCount;
            var copy = provenance.Distinct().OrderBy(value => value).ToArray();
            this.provenance = new ReadOnlyCollection<MicroPatternProtectedCell>(copy);
        }

        public LocalTileCoord LocalCoordinate { get; }
        public LocalTileCoord TargetCoordinate { get; }
        public int RemovedWriteCount { get; }
        public IReadOnlyList<MicroPatternProtectedCell> Provenance => provenance;
    }

    public enum MicroPatternApplicationErrorCode
    {
        MissingTransformedPattern = 1,
        InvalidProtectedPolicy = 2,
        CoordinateOverflow = 3,
        ProtectedMaskInvalid = 4,
        ProtectedWriteRejected = 5,
        InvalidPreparedCoverage = 6,
    }

    public sealed class MicroPatternApplicationError :
        IEquatable<MicroPatternApplicationError>,
        IComparable<MicroPatternApplicationError>
    {
        private readonly ReadOnlyCollection<MicroPatternProtectedCell> provenance;

        public MicroPatternApplicationError(
            MicroPatternApplicationErrorCode code,
            string path,
            string detail,
            LocalTileCoord? targetCoordinate = null,
            IEnumerable<MicroPatternProtectedCell> provenance = null)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
            TargetCoordinate = targetCoordinate;
            var copy = provenance == null
                ? Array.Empty<MicroPatternProtectedCell>()
                : provenance.Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.provenance = new ReadOnlyCollection<MicroPatternProtectedCell>(copy);
        }

        public MicroPatternApplicationErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }
        public LocalTileCoord? TargetCoordinate { get; }
        public IReadOnlyList<MicroPatternProtectedCell> Provenance => provenance;

        public int CompareTo(MicroPatternApplicationError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Detail, other.Detail, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            return string.Compare(ProvenanceKey(), other.ProvenanceKey(), StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternApplicationError other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj) => Equals(obj as MicroPatternApplicationError);
        public override int GetHashCode() => ToString().GetHashCode();

        public override string ToString()
        {
            return Code + "|" + Path + "|" + Detail + "|" + ProvenanceKey();
        }

        private string ProvenanceKey()
        {
            return string.Join(";", provenance.Select(value => value.ToString()));
        }
    }

    public sealed class MicroPatternApplicationPlan
    {
        private readonly ReadOnlyCollection<MicroPatternPreparedCell> cells;
        private readonly ReadOnlyCollection<MicroPatternProtectedHit> protectedHits;

        internal MicroPatternApplicationPlan(
            TransformedMicroPattern transformedPattern,
            MicroPatternPlacement placement,
            MicroPatternProtectedMask protectedMask,
            IEnumerable<MicroPatternPreparedCell> cells,
            IEnumerable<MicroPatternProtectedHit> protectedHits,
            string stableDigest)
        {
            SourcePatternId = transformedPattern.SourcePatternId;
            SourceDigest = transformedPattern.SourceDigest;
            Transform = transformedPattern.Transform;
            Placement = placement;
            ProtectedPolicy = transformedPattern.SourceDefinition.ProtectedPolicy;
            ProtectedMask = protectedMask;
            var cellCopy = cells.OrderBy(value => value.LocalCoordinate.Y)
                .ThenBy(value => value.LocalCoordinate.X)
                .ToArray();
            this.cells = new ReadOnlyCollection<MicroPatternPreparedCell>(cellCopy);
            var hitCopy = protectedHits.OrderBy(value => value.TargetCoordinate.Y)
                .ThenBy(value => value.TargetCoordinate.X)
                .ToArray();
            this.protectedHits = new ReadOnlyCollection<MicroPatternProtectedHit>(hitCopy);
            StableDigest = stableDigest ?? string.Empty;
        }

        public MicroPatternId SourcePatternId { get; }
        public string SourceDigest { get; }
        public MicroPatternTransform Transform { get; }
        public MicroPatternPlacement Placement { get; }
        public LocalTileCoord Origin => Placement.Origin;
        public MicroPatternProtectedPolicy ProtectedPolicy { get; }
        public IReadOnlyList<MicroPatternPreparedCell> Cells => cells;
        public MicroPatternProtectedMask ProtectedMask { get; }
        public IReadOnlyList<MicroPatternProtectedHit> ProtectedHits => protectedHits;
        public string StableDigest { get; }
    }

    public sealed class MicroPatternApplicationResult
    {
        private readonly ReadOnlyCollection<MicroPatternApplicationError> errors;
        private readonly ReadOnlyCollection<MicroPatternProtectedHit> rejectedHits;

        internal MicroPatternApplicationResult(
            MicroPatternApplicationPlan plan,
            IEnumerable<MicroPatternApplicationError> errors,
            IEnumerable<MicroPatternProtectedHit> rejectedHits = null)
        {
            var errorCopy = (errors ?? Array.Empty<MicroPatternApplicationError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<MicroPatternApplicationError>(errorCopy);
            var hitCopy = rejectedHits == null
                ? Array.Empty<MicroPatternProtectedHit>()
                : rejectedHits.OrderBy(value => value.TargetCoordinate.Y)
                    .ThenBy(value => value.TargetCoordinate.X)
                    .ToArray();
            this.rejectedHits = new ReadOnlyCollection<MicroPatternProtectedHit>(hitCopy);
            Plan = errorCopy.Length == 0 ? plan : null;
            StableDigest = Plan == null ? string.Empty : Plan.StableDigest;
        }

        public bool Success => Plan != null && errors.Count == 0;
        public MicroPatternApplicationPlan Plan { get; }
        public IReadOnlyList<MicroPatternApplicationError> Errors => errors;
        public IReadOnlyList<MicroPatternProtectedHit> RejectedHits => rejectedHits;
        public string StableDigest { get; }
    }

    public static class MicroPatternApplicationPlanner
    {
        public static MicroPatternApplicationResult Plan(
            TransformedMicroPattern transformedPattern,
            MicroPatternPlacement placement,
            IEnumerable<MicroPatternProtectedCell> protectedCells)
        {
            var errors = new List<MicroPatternApplicationError>();
            if (transformedPattern == null)
            {
                errors.Add(new MicroPatternApplicationError(
                    MicroPatternApplicationErrorCode.MissingTransformedPattern,
                    "transformedPattern",
                    "Transformed pattern is required."));
                return new MicroPatternApplicationResult(null, errors);
            }

            var policy = transformedPattern.SourceDefinition.ProtectedPolicy;
            if (policy != MicroPatternProtectedPolicy.ForceNoChange &&
                policy != MicroPatternProtectedPolicy.RejectCandidate)
            {
                errors.Add(new MicroPatternApplicationError(
                    MicroPatternApplicationErrorCode.InvalidProtectedPolicy,
                    "protectedPolicy",
                    ((int)policy).ToString(CultureInfo.InvariantCulture)));
            }

            var maskResult = MicroPatternProtectedMaskBuilder.Build(placement, protectedCells);
            foreach (var maskError in maskResult.Errors)
            {
                var code = maskError.Code == MicroPatternProtectedMaskErrorCode.CoordinateOverflow
                    ? MicroPatternApplicationErrorCode.CoordinateOverflow
                    : MicroPatternApplicationErrorCode.ProtectedMaskInvalid;
                errors.Add(new MicroPatternApplicationError(
                    code,
                    maskError.Path,
                    maskError.ToString()));
            }

            if (errors.Count != 0)
            {
                return new MicroPatternApplicationResult(null, errors);
            }

            var prepared = new List<MicroPatternPreparedCell>();
            var hits = new List<MicroPatternProtectedHit>();
            foreach (var cell in transformedPattern.Cells)
            {
                LocalTileCoord target;
                try
                {
                    target = new LocalTileCoord(
                        checked(placement.Origin.X + cell.Coordinate.X),
                        checked(placement.Origin.Y + cell.Coordinate.Y));
                }
                catch (OverflowException)
                {
                    errors.Add(new MicroPatternApplicationError(
                        MicroPatternApplicationErrorCode.CoordinateOverflow,
                        CoordinatePath(cell.Coordinate),
                        Coordinate(placement.Origin)));
                    continue;
                }

                var canonical = CanonicalInstructions(cell.Instructions);
                var writeCount = canonical.Count(value => value.Operation != MicroPatternOperation.NoChange);
                MicroPatternProtectedMaskEntry maskEntry = null;
                var protectedWrite = writeCount != 0 && maskResult.Mask.TryGetEntry(target, out maskEntry);
                if (protectedWrite)
                {
                    hits.Add(new MicroPatternProtectedHit(
                        cell.Coordinate,
                        target,
                        writeCount,
                        maskEntry.Provenance));
                    if (policy == MicroPatternProtectedPolicy.ForceNoChange)
                    {
                        canonical = NoChangeInstructions();
                    }
                }

                prepared.Add(new MicroPatternPreparedCell(cell.Coordinate, target, canonical));
            }

            if (prepared.Count != MicroPatternDefinition.RequiredCellCount ||
                prepared.Select(value => value.LocalCoordinate).Distinct().Count() !=
                MicroPatternDefinition.RequiredCellCount)
            {
                errors.Add(new MicroPatternApplicationError(
                    MicroPatternApplicationErrorCode.InvalidPreparedCoverage,
                    "cells",
                    prepared.Count.ToString(CultureInfo.InvariantCulture)));
            }

            if (policy == MicroPatternProtectedPolicy.RejectCandidate)
            {
                foreach (var hit in hits)
                {
                    errors.Add(new MicroPatternApplicationError(
                        MicroPatternApplicationErrorCode.ProtectedWriteRejected,
                        CoordinatePath(hit.LocalCoordinate),
                        Coordinate(hit.TargetCoordinate),
                        hit.TargetCoordinate,
                        hit.Provenance));
                }
            }

            if (errors.Count != 0)
            {
                return new MicroPatternApplicationResult(null, errors, hits);
            }

            var digest = MicroPatternApplicationCanonicalDigest.Compute(
                transformedPattern,
                placement,
                maskResult.Mask,
                prepared,
                hits);
            var plan = new MicroPatternApplicationPlan(
                transformedPattern,
                placement,
                maskResult.Mask,
                prepared,
                hits,
                digest);
            return new MicroPatternApplicationResult(plan, errors);
        }

        private static MicroPatternInstruction[] CanonicalInstructions(
            IEnumerable<MicroPatternInstruction> source)
        {
            var byLayer = source.ToDictionary(value => value.Layer);
            var result = new MicroPatternInstruction[6];
            for (var index = 0; index < result.Length; index++)
            {
                var layer = (MicroPatternLayer)(index + 1);
                MicroPatternInstruction instruction;
                result[index] = byLayer.TryGetValue(layer, out instruction)
                    ? new MicroPatternInstruction(layer, instruction.Operation, instruction.PayloadId)
                    : new MicroPatternInstruction(layer, MicroPatternOperation.NoChange);
            }
            return result;
        }

        private static MicroPatternInstruction[] NoChangeInstructions()
        {
            var result = new MicroPatternInstruction[6];
            for (var index = 0; index < result.Length; index++)
            {
                result[index] = new MicroPatternInstruction(
                    (MicroPatternLayer)(index + 1),
                    MicroPatternOperation.NoChange);
            }
            return result;
        }

        private static string CoordinatePath(LocalTileCoord value)
        {
            return "cells[" + Coordinate(value) + "]";
        }

        private static string Coordinate(LocalTileCoord value)
        {
            return value.X.ToString(CultureInfo.InvariantCulture) + "," +
                   value.Y.ToString(CultureInfo.InvariantCulture);
        }
    }

    public static class MicroPatternApplicationCanonicalDigest
    {
        internal static string Compute(
            TransformedMicroPattern transformedPattern,
            MicroPatternPlacement placement,
            MicroPatternProtectedMask protectedMask,
            IEnumerable<MicroPatternPreparedCell> cells,
            IEnumerable<MicroPatternProtectedHit> hits)
        {
            var material = new StringBuilder();
            Append(material, "SOURCE_ID", transformedPattern.SourcePatternId.Value);
            Append(material, "SOURCE_DIGEST", transformedPattern.SourceDigest);
            Append(material, "TRANSFORM", transformedPattern.Transform.ToString());
            Append(material, "ORIGIN", Number(placement.Origin.X), Number(placement.Origin.Y));
            Append(material, "POLICY", transformedPattern.SourceDefinition.ProtectedPolicy.ToString());

            foreach (var cell in cells.OrderBy(value => value.LocalCoordinate.Y)
                         .ThenBy(value => value.LocalCoordinate.X))
            {
                Append(material, "CELL",
                    Number(cell.LocalCoordinate.X), Number(cell.LocalCoordinate.Y),
                    Number(cell.TargetCoordinate.X), Number(cell.TargetCoordinate.Y));
                foreach (var instruction in cell.Instructions.OrderBy(value => (int)value.Layer))
                {
                    Append(material, "INSTRUCTION", instruction.Layer.ToString(),
                        instruction.Operation.ToString(), instruction.PayloadId);
                }
            }

            foreach (var entry in protectedMask.Entries)
            {
                Append(material, "MASK", Number(entry.TargetCoordinate.X),
                    Number(entry.TargetCoordinate.Y));
                foreach (var source in entry.Provenance)
                {
                    Append(material, "PROVENANCE", source.SourceKind.ToString(), source.SourceId);
                }
            }

            foreach (var hit in hits.OrderBy(value => value.TargetCoordinate.Y)
                         .ThenBy(value => value.TargetCoordinate.X))
            {
                Append(material, "HIT", Number(hit.LocalCoordinate.X), Number(hit.LocalCoordinate.Y),
                    Number(hit.TargetCoordinate.X), Number(hit.TargetCoordinate.Y),
                    Number(hit.RemovedWriteCount));
                foreach (var source in hit.Provenance)
                {
                    Append(material, "HIT_SOURCE", source.SourceKind.ToString(), source.SourceId);
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
