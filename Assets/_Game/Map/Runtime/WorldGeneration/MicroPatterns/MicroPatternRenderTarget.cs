using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public readonly struct MicroPatternRenderRequestId :
        IEquatable<MicroPatternRenderRequestId>,
        IComparable<MicroPatternRenderRequestId>
    {
        private readonly string value;

        public MicroPatternRenderRequestId(string value)
        {
            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public int CompareTo(MicroPatternRenderRequestId other) =>
            string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(MicroPatternRenderRequestId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) =>
            obj is MicroPatternRenderRequestId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(MicroPatternRenderRequestId left, MicroPatternRenderRequestId right) =>
            left.Equals(right);
        public static bool operator !=(MicroPatternRenderRequestId left, MicroPatternRenderRequestId right) =>
            !left.Equals(right);
    }

    public sealed class MicroPatternRenderRequest
    {
        public MicroPatternRenderRequest(
            MicroPatternRenderRequestId id,
            MicroPatternApplicationPlan applicationPlan)
        {
            Id = id;
            ApplicationPlan = applicationPlan;
        }

        public MicroPatternRenderRequestId Id { get; }
        public MicroPatternApplicationPlan ApplicationPlan { get; }
    }

    public sealed class MicroPatternRenderSourceEvidence :
        IEquatable<MicroPatternRenderSourceEvidence>,
        IComparable<MicroPatternRenderSourceEvidence>
    {
        private readonly ReadOnlyCollection<MicroPatternProtectedCell> protectedProvenance;

        public MicroPatternRenderSourceEvidence(
            MicroPatternLayer layer,
            string stableSourceId)
            : this(
                layer,
                stableSourceId,
                default(MicroPatternRenderRequestId),
                default(MicroPatternId),
                string.Empty,
                string.Empty,
                Array.Empty<MicroPatternProtectedCell>())
        {
        }

        internal MicroPatternRenderSourceEvidence(
            MicroPatternLayer layer,
            string stableSourceId,
            MicroPatternRenderRequestId requestId,
            MicroPatternId sourcePatternId,
            string sourceDigest,
            string planDigest,
            IEnumerable<MicroPatternProtectedCell> protectedProvenance)
        {
            Layer = layer;
            StableSourceId = stableSourceId ?? string.Empty;
            RequestId = requestId;
            SourcePatternId = sourcePatternId;
            SourceDigest = sourceDigest ?? string.Empty;
            PlanDigest = planDigest ?? string.Empty;
            var copy = protectedProvenance == null
                ? Array.Empty<MicroPatternProtectedCell>()
                : protectedProvenance.Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.protectedProvenance = new ReadOnlyCollection<MicroPatternProtectedCell>(copy);
        }

        public MicroPatternLayer Layer { get; }
        public string StableSourceId { get; }
        public MicroPatternRenderRequestId RequestId { get; }
        public MicroPatternId SourcePatternId { get; }
        public string SourceDigest { get; }
        public string PlanDigest { get; }
        public IReadOnlyList<MicroPatternProtectedCell> ProtectedProvenance => protectedProvenance;
        public bool IsPatternWrite => RequestId.Value.Length != 0;

        public int CompareTo(MicroPatternRenderSourceEvidence other)
        {
            if (other == null) return -1;
            var comparison = ((int)Layer).CompareTo((int)other.Layer);
            if (comparison != 0) return comparison;
            comparison = string.Compare(StableSourceId, other.StableSourceId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = RequestId.CompareTo(other.RequestId);
            if (comparison != 0) return comparison;
            comparison = SourcePatternId.CompareTo(other.SourcePatternId);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SourceDigest, other.SourceDigest, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(PlanDigest, other.PlanDigest, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            return string.Compare(ProtectedKey(), other.ProtectedKey(), StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternRenderSourceEvidence other) =>
            other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as MicroPatternRenderSourceEvidence);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());

        public override string ToString()
        {
            return Layer + "|" + StableSourceId + "|" + RequestId.Value + "|" +
                   SourcePatternId.Value + "|" + SourceDigest + "|" + PlanDigest + "|" + ProtectedKey();
        }

        private string ProtectedKey() =>
            string.Join(";", protectedProvenance.Select(value => value.ToString()));
    }

    public sealed class MicroPatternRenderCellState
    {
        private readonly ReadOnlyCollection<MicroPatternRenderSourceEvidence> provenance;

        public MicroPatternRenderCellState(
            LocalTileCoord targetCoordinate,
            bool solid,
            string surfaceId,
            string affordanceId,
            string materialId,
            string hazardId,
            string markerId,
            IEnumerable<MicroPatternRenderSourceEvidence> provenance = null)
        {
            TargetCoordinate = targetCoordinate;
            Solid = solid;
            SurfaceId = surfaceId ?? string.Empty;
            AffordanceId = affordanceId ?? string.Empty;
            MaterialId = materialId ?? string.Empty;
            HazardId = hazardId ?? string.Empty;
            MarkerId = markerId ?? string.Empty;
            var copy = provenance == null
                ? Array.Empty<MicroPatternRenderSourceEvidence>()
                : provenance.ToArray();
            Array.Sort(copy, CompareEvidence);
            this.provenance = new ReadOnlyCollection<MicroPatternRenderSourceEvidence>(copy);
        }

        public LocalTileCoord TargetCoordinate { get; }
        public bool Solid { get; }
        public string SurfaceId { get; }
        public string AffordanceId { get; }
        public string MaterialId { get; }
        public string HazardId { get; }
        public string MarkerId { get; }
        public IReadOnlyList<MicroPatternRenderSourceEvidence> Provenance => provenance;

        public string GetSemanticValue(MicroPatternLayer layer)
        {
            switch (layer)
            {
                case MicroPatternLayer.Geometry: return Solid ? "SOLID" : "AIR";
                case MicroPatternLayer.Surface: return SurfaceId;
                case MicroPatternLayer.Affordance: return AffordanceId;
                case MicroPatternLayer.Material: return MaterialId;
                case MicroPatternLayer.Hazard: return HazardId;
                case MicroPatternLayer.Marker: return MarkerId;
                default: return string.Empty;
            }
        }

        public bool ValuesEqual(MicroPatternRenderCellState other)
        {
            return other != null && Solid == other.Solid &&
                   string.Equals(SurfaceId, other.SurfaceId, StringComparison.Ordinal) &&
                   string.Equals(AffordanceId, other.AffordanceId, StringComparison.Ordinal) &&
                   string.Equals(MaterialId, other.MaterialId, StringComparison.Ordinal) &&
                   string.Equals(HazardId, other.HazardId, StringComparison.Ordinal) &&
                   string.Equals(MarkerId, other.MarkerId, StringComparison.Ordinal);
        }

        internal MicroPatternRenderCellState Apply(MicroPatternLayerWrite write)
        {
            var solid = Solid;
            var surface = SurfaceId;
            var affordance = AffordanceId;
            var material = MaterialId;
            var hazard = HazardId;
            var marker = MarkerId;
            switch (write.Layer)
            {
                case MicroPatternLayer.Geometry:
                    solid = string.Equals(write.SemanticValue, "SOLID", StringComparison.Ordinal);
                    break;
                case MicroPatternLayer.Surface:
                    surface = write.SemanticValue;
                    break;
                case MicroPatternLayer.Affordance:
                    affordance = write.SemanticValue;
                    break;
                case MicroPatternLayer.Material:
                    material = write.SemanticValue;
                    break;
                case MicroPatternLayer.Hazard:
                    hazard = write.SemanticValue;
                    break;
                case MicroPatternLayer.Marker:
                    marker = write.SemanticValue;
                    break;
            }

            var combined = provenance.Concat(write.Provenance)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            return new MicroPatternRenderCellState(
                TargetCoordinate,
                solid,
                surface,
                affordance,
                material,
                hazard,
                marker,
                combined);
        }

        private static int CompareEvidence(
            MicroPatternRenderSourceEvidence left,
            MicroPatternRenderSourceEvidence right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return left.CompareTo(right);
        }
    }

    public sealed class MicroPatternRenderTarget
    {
        private readonly ReadOnlyCollection<MicroPatternRenderCellState> cells;

        public MicroPatternRenderTarget(IEnumerable<MicroPatternRenderCellState> cells)
        {
            var copy = cells == null
                ? Array.Empty<MicroPatternRenderCellState>()
                : cells.ToArray();
            Array.Sort(copy, CompareCells);
            this.cells = new ReadOnlyCollection<MicroPatternRenderCellState>(copy);
        }

        public IReadOnlyList<MicroPatternRenderCellState> Cells => cells;

        private static int CompareCells(
            MicroPatternRenderCellState left,
            MicroPatternRenderCellState right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            var comparison = left.TargetCoordinate.Y.CompareTo(right.TargetCoordinate.Y);
            return comparison != 0
                ? comparison
                : left.TargetCoordinate.X.CompareTo(right.TargetCoordinate.X);
        }
    }
}
