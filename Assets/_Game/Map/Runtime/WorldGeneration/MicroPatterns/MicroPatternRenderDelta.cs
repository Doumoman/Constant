using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public enum MicroPatternRenderStage
    {
        Geometry = 10,
        Surface = 20,
        Affordance = 30,
        Material = 40,
        Hazard = 50,
        Marker = 60,
    }

    public sealed class MicroPatternLayerWrite :
        IComparable<MicroPatternLayerWrite>
    {
        private readonly ReadOnlyCollection<MicroPatternRenderSourceEvidence> provenance;

        internal MicroPatternLayerWrite(
            LocalTileCoord targetCoordinate,
            MicroPatternRenderStage stage,
            MicroPatternLayer layer,
            MicroPatternOperation operation,
            string semanticValue,
            IEnumerable<MicroPatternRenderSourceEvidence> provenance,
            bool isIdempotent)
        {
            TargetCoordinate = targetCoordinate;
            Stage = stage;
            Layer = layer;
            Operation = operation;
            SemanticValue = semanticValue ?? string.Empty;
            var copy = provenance.Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.provenance = new ReadOnlyCollection<MicroPatternRenderSourceEvidence>(copy);
            IsIdempotent = isIdempotent;
        }

        public LocalTileCoord TargetCoordinate { get; }
        public MicroPatternRenderStage Stage { get; }
        public MicroPatternLayer Layer { get; }
        public MicroPatternOperation Operation { get; }
        public string SemanticValue { get; }
        public IReadOnlyList<MicroPatternRenderSourceEvidence> Provenance => provenance;
        public bool IsIdempotent { get; }
        public bool IsCoalesced => provenance.Count > 1;

        public int CompareTo(MicroPatternLayerWrite other)
        {
            if (other == null) return -1;
            var comparison = ((int)Stage).CompareTo((int)other.Stage);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.Y.CompareTo(other.TargetCoordinate.Y);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.X.CompareTo(other.TargetCoordinate.X);
            if (comparison != 0) return comparison;
            comparison = ((int)Layer).CompareTo((int)other.Layer);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SemanticValue, other.SemanticValue, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            return ((int)Operation).CompareTo((int)other.Operation);
        }

        internal MicroPatternLayerWrite WithIdempotence(bool isIdempotent)
        {
            return new MicroPatternLayerWrite(
                TargetCoordinate,
                Stage,
                Layer,
                Operation,
                SemanticValue,
                provenance,
                isIdempotent);
        }
    }

    public sealed class MicroPatternRenderConflict :
        IComparable<MicroPatternRenderConflict>
    {
        private readonly ReadOnlyCollection<MicroPatternLayerWrite> alternatives;

        internal MicroPatternRenderConflict(
            LocalTileCoord targetCoordinate,
            MicroPatternLayer layer,
            IEnumerable<MicroPatternLayerWrite> alternatives)
        {
            TargetCoordinate = targetCoordinate;
            Layer = layer;
            var copy = alternatives.OrderBy(value => value.SemanticValue, StringComparer.Ordinal)
                .ThenBy(value => (int)value.Operation)
                .ToArray();
            this.alternatives = new ReadOnlyCollection<MicroPatternLayerWrite>(copy);
        }

        public LocalTileCoord TargetCoordinate { get; }
        public MicroPatternLayer Layer { get; }
        public IReadOnlyList<MicroPatternLayerWrite> Alternatives => alternatives;

        public int CompareTo(MicroPatternRenderConflict other)
        {
            if (other == null) return -1;
            var comparison = TargetCoordinate.Y.CompareTo(other.TargetCoordinate.Y);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.X.CompareTo(other.TargetCoordinate.X);
            return comparison != 0 ? comparison : ((int)Layer).CompareTo((int)other.Layer);
        }
    }

    public sealed class MicroPatternRenderedCellDelta
    {
        private readonly ReadOnlyCollection<MicroPatternLayerWrite> writes;

        internal MicroPatternRenderedCellDelta(
            LocalTileCoord targetCoordinate,
            MicroPatternRenderCellState before,
            MicroPatternRenderCellState after,
            IEnumerable<MicroPatternLayerWrite> writes)
        {
            TargetCoordinate = targetCoordinate;
            Before = before;
            After = after;
            var copy = writes.OrderBy(value => value).ToArray();
            this.writes = new ReadOnlyCollection<MicroPatternLayerWrite>(copy);
        }

        public LocalTileCoord TargetCoordinate { get; }
        public MicroPatternRenderCellState Before { get; }
        public MicroPatternRenderCellState After { get; }
        public IReadOnlyList<MicroPatternLayerWrite> Writes => writes;
        public bool ValuesEqual => Before.ValuesEqual(After);
    }

    public sealed class MicroPatternRenderDelta
    {
        public const string RulesetVersion = "MAP10_03_RENDER_V1";

        private readonly ReadOnlyCollection<MicroPatternRenderRequest> requests;
        private readonly ReadOnlyCollection<MicroPatternLayerWrite> writes;
        private readonly ReadOnlyCollection<MicroPatternRenderedCellDelta> cells;

        internal MicroPatternRenderDelta(
            IEnumerable<MicroPatternRenderRequest> requests,
            MicroPatternRenderTarget inputTarget,
            IEnumerable<MicroPatternLayerWrite> writes,
            IEnumerable<MicroPatternRenderedCellDelta> cells,
            string stableDigest)
        {
            var requestCopy = requests.OrderBy(value => value.Id).ToArray();
            this.requests = new ReadOnlyCollection<MicroPatternRenderRequest>(requestCopy);
            InputTarget = new MicroPatternRenderTarget(inputTarget.Cells);
            var writeCopy = writes.OrderBy(value => value).ToArray();
            this.writes = new ReadOnlyCollection<MicroPatternLayerWrite>(writeCopy);
            var cellCopy = cells.OrderBy(value => value.TargetCoordinate.Y)
                .ThenBy(value => value.TargetCoordinate.X)
                .ToArray();
            this.cells = new ReadOnlyCollection<MicroPatternRenderedCellDelta>(cellCopy);
            StableDigest = stableDigest ?? string.Empty;
        }

        public string RenderRulesetVersion => RulesetVersion;
        public IReadOnlyList<MicroPatternRenderRequest> Requests => requests;
        public MicroPatternRenderTarget InputTarget { get; }
        public IReadOnlyList<MicroPatternLayerWrite> Writes => writes;
        public IReadOnlyList<MicroPatternRenderedCellDelta> Cells => cells;
        public string StableDigest { get; }
    }

    public enum MicroPatternRenderErrorCode
    {
        MissingInput = 1,
        InvalidRequestId = 2,
        DuplicateRequestId = 3,
        InvalidApplicationPlan = 4,
        PlanDigestMismatch = 5,
        MissingTargetCell = 6,
        DuplicateTargetCell = 7,
        ExtraTargetCell = 8,
        InvalidLayerState = 9,
        InvalidExistingProvenance = 10,
        UnsupportedOperation = 11,
        LayerOperationMismatch = 12,
        ConflictingLayerWrite = 13,
        AtomicRenderRejected = 14,
    }

    public sealed class MicroPatternRenderError :
        IEquatable<MicroPatternRenderError>,
        IComparable<MicroPatternRenderError>
    {
        public MicroPatternRenderError(
            MicroPatternRenderErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternRenderErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternRenderError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternRenderError other)
        {
            return other != null && Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as MicroPatternRenderError);
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

    public sealed class MicroPatternRenderResult
    {
        private readonly ReadOnlyCollection<MicroPatternRenderError> errors;
        private readonly ReadOnlyCollection<MicroPatternRenderConflict> conflicts;

        internal MicroPatternRenderResult(
            MicroPatternRenderDelta delta,
            IEnumerable<MicroPatternRenderError> errors,
            IEnumerable<MicroPatternRenderConflict> conflicts = null)
        {
            var errorCopy = (errors ?? Array.Empty<MicroPatternRenderError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<MicroPatternRenderError>(errorCopy);
            var conflictCopy = conflicts == null
                ? Array.Empty<MicroPatternRenderConflict>()
                : conflicts.Where(value => value != null).OrderBy(value => value).ToArray();
            this.conflicts = new ReadOnlyCollection<MicroPatternRenderConflict>(conflictCopy);
            Delta = errorCopy.Length == 0 && conflictCopy.Length == 0 ? delta : null;
            StableDigest = Delta == null ? string.Empty : Delta.StableDigest;
        }

        public bool Success => Delta != null && errors.Count == 0 && conflicts.Count == 0;
        public MicroPatternRenderDelta Delta { get; }
        public IReadOnlyList<MicroPatternRenderError> Errors => errors;
        public IReadOnlyList<MicroPatternRenderConflict> Conflicts => conflicts;
        public string StableDigest { get; }
    }
}
