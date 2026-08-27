using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.Baking
{
    public readonly struct SectorCanvasId : IEquatable<SectorCanvasId>, IComparable<SectorCanvasId>
    {
        private readonly string value;
        public SectorCanvasId(string value) { this.value = value; }
        public string Value => value ?? string.Empty;
        public int CompareTo(SectorCanvasId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(SectorCanvasId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SectorCanvasId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(SectorCanvasId left, SectorCanvasId right) => left.Equals(right);
        public static bool operator !=(SectorCanvasId left, SectorCanvasId right) => !left.Equals(right);
    }

    public enum SectorCanvasLayerKind
    {
        Solid = 1,
        Background = 2,
        Surface = 3,
        Affordance = 4,
        Material = 5,
        Hazard = 6,
        Marker = 7,
        Owner = 8,
    }

    public readonly struct ResolvedLayerValue : IEquatable<ResolvedLayerValue>
    {
        public ResolvedLayerValue(string stableId, bool isExplicitEmpty)
        {
            StableId = stableId ?? string.Empty;
            IsExplicitEmpty = isExplicitEmpty;
        }

        public string StableId { get; }
        public bool IsExplicitEmpty { get; }
        public static ResolvedLayerValue Empty => new ResolvedLayerValue(string.Empty, true);
        public static ResolvedLayerValue FromId(string stableId) => new ResolvedLayerValue(stableId, false);
        public bool Equals(ResolvedLayerValue other) => IsExplicitEmpty == other.IsExplicitEmpty &&
            string.Equals(StableId, other.StableId, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ResolvedLayerValue other && Equals(other);
        public override int GetHashCode()
        {
            unchecked { return (StringComparer.Ordinal.GetHashCode(StableId ?? string.Empty) * 397) ^ IsExplicitEmpty.GetHashCode(); }
        }
        public static bool operator ==(ResolvedLayerValue left, ResolvedLayerValue right) => left.Equals(right);
        public static bool operator !=(ResolvedLayerValue left, ResolvedLayerValue right) => !left.Equals(right);
    }

    public sealed class SectorCanvasLayerSnapshot : IEquatable<SectorCanvasLayerSnapshot>
    {
        public SectorCanvasLayerSnapshot(
            ResolvedLayerValue solid,
            ResolvedLayerValue background,
            ResolvedLayerValue surface,
            ResolvedLayerValue affordance,
            ResolvedLayerValue material,
            ResolvedLayerValue hazard,
            ResolvedLayerValue marker,
            ResolvedLayerValue owner)
        {
            Solid = solid;
            Background = background;
            Surface = surface;
            Affordance = affordance;
            Material = material;
            Hazard = hazard;
            Marker = marker;
            Owner = owner;
        }

        public ResolvedLayerValue Solid { get; }
        public ResolvedLayerValue Background { get; }
        public ResolvedLayerValue Surface { get; }
        public ResolvedLayerValue Affordance { get; }
        public ResolvedLayerValue Material { get; }
        public ResolvedLayerValue Hazard { get; }
        public ResolvedLayerValue Marker { get; }
        public ResolvedLayerValue Owner { get; }

        public ResolvedLayerValue Get(SectorCanvasLayerKind layer)
        {
            switch (layer)
            {
                case SectorCanvasLayerKind.Solid: return Solid;
                case SectorCanvasLayerKind.Background: return Background;
                case SectorCanvasLayerKind.Surface: return Surface;
                case SectorCanvasLayerKind.Affordance: return Affordance;
                case SectorCanvasLayerKind.Material: return Material;
                case SectorCanvasLayerKind.Hazard: return Hazard;
                case SectorCanvasLayerKind.Marker: return Marker;
                case SectorCanvasLayerKind.Owner: return Owner;
                default: return default(ResolvedLayerValue);
            }
        }

        public bool Equals(SectorCanvasLayerSnapshot other)
        {
            return other != null && Solid == other.Solid && Background == other.Background &&
                   Surface == other.Surface && Affordance == other.Affordance &&
                   Material == other.Material && Hazard == other.Hazard && Marker == other.Marker && Owner == other.Owner;
        }

        public override bool Equals(object obj) => Equals(obj as SectorCanvasLayerSnapshot);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Solid.GetHashCode();
                hash = (hash * 397) ^ Background.GetHashCode();
                hash = (hash * 397) ^ Surface.GetHashCode();
                hash = (hash * 397) ^ Affordance.GetHashCode();
                hash = (hash * 397) ^ Material.GetHashCode();
                hash = (hash * 397) ^ Hazard.GetHashCode();
                hash = (hash * 397) ^ Marker.GetHashCode();
                return (hash * 397) ^ Owner.GetHashCode();
            }
        }
    }

    public enum CanvasSourceKind
    {
        Boundary = 1,
        SpecialRegion = 2,
        TerrainCluster = 3,
        MicroPattern = 4,
        Activity = 5,
        EventOverlay = 6,
        Cleanup = 7,
    }

    public sealed class CanvasSourceRef : IEquatable<CanvasSourceRef>
    {
        private readonly ReadOnlyCollection<SectorCanvasLayerKind> ownedLayers;

        public CanvasSourceRef(
            CanvasSourceKind kind,
            string stableId,
            int passOrder,
            bool isProtected,
            IEnumerable<SectorCanvasLayerKind> ownedLayers)
        {
            Kind = kind;
            StableId = stableId ?? string.Empty;
            PassOrder = passOrder;
            IsProtected = isProtected;
            var copy = ownedLayers == null ? Array.Empty<SectorCanvasLayerKind>() : ownedLayers.ToArray();
            Array.Sort(copy);
            this.ownedLayers = new ReadOnlyCollection<SectorCanvasLayerKind>(copy);
        }

        public CanvasSourceKind Kind { get; }
        public string StableId { get; }
        public int PassOrder { get; }
        public bool IsProtected { get; }
        public IReadOnlyList<SectorCanvasLayerKind> OwnedLayers => ownedLayers;

        public bool Equals(CanvasSourceRef other)
        {
            return other != null && Kind == other.Kind && PassOrder == other.PassOrder &&
                   IsProtected == other.IsProtected && string.Equals(StableId, other.StableId, StringComparison.Ordinal) &&
                   ownedLayers.SequenceEqual(other.ownedLayers);
        }
        public override bool Equals(object obj) => Equals(obj as CanvasSourceRef);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(StableId);
                hash = (hash * 397) ^ PassOrder;
                hash = (hash * 397) ^ IsProtected.GetHashCode();
                foreach (var layer in ownedLayers) hash = (hash * 397) ^ (int)layer;
                return hash;
            }
        }
    }

    public sealed class SectorCanvasProvenance : IEquatable<SectorCanvasProvenance>
    {
        private readonly ReadOnlyCollection<CanvasSourceRef> sources;
        private readonly ReadOnlyCollection<SpecialPersistenceKey> persistenceKeys;

        public SectorCanvasProvenance(
            IEnumerable<CanvasSourceRef> sources,
            IEnumerable<SpecialPersistenceKey> persistenceKeys = null)
        {
            var sourceCopy = sources == null ? Array.Empty<CanvasSourceRef>() : sources.ToArray();
            Array.Sort(sourceCopy, CompareSources);
            this.sources = new ReadOnlyCollection<CanvasSourceRef>(sourceCopy);
            var keyCopy = persistenceKeys == null ? Array.Empty<SpecialPersistenceKey>() : persistenceKeys.ToArray();
            Array.Sort(keyCopy);
            this.persistenceKeys = new ReadOnlyCollection<SpecialPersistenceKey>(keyCopy);
        }

        public IReadOnlyList<CanvasSourceRef> Sources => sources;
        public IReadOnlyList<SpecialPersistenceKey> PersistenceKeys => persistenceKeys;

        public bool Equals(SectorCanvasProvenance other)
            => other != null && sources.SequenceEqual(other.sources) && persistenceKeys.SequenceEqual(other.persistenceKeys);
        public override bool Equals(object obj) => Equals(obj as SectorCanvasProvenance);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                foreach (var source in sources) hash = (hash * 397) ^ (source == null ? 0 : source.GetHashCode());
                foreach (var key in persistenceKeys) hash = (hash * 397) ^ key.GetHashCode();
                return hash;
            }
        }

        private static int CompareSources(CanvasSourceRef left, CanvasSourceRef right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            var order = left.PassOrder.CompareTo(right.PassOrder);
            if (order != 0) return order;
            var kind = ((int)left.Kind).CompareTo((int)right.Kind);
            return kind != 0 ? kind : string.Compare(left.StableId, right.StableId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorCanvasCell
    {
        public SectorCanvasCell(
            LocalTileCoord coordinate,
            SectorCanvasLayerSnapshot layers,
            SectorCanvasProvenance provenance)
        {
            Coordinate = coordinate;
            Layers = layers;
            Provenance = provenance;
        }

        public LocalTileCoord Coordinate { get; }
        public SectorCanvasLayerSnapshot Layers { get; }
        public SectorCanvasProvenance Provenance { get; }
        public int CanonicalIndex => Coordinate.Y * WorldGenConstants.SectorWidthTiles + Coordinate.X;
    }

    public enum SectorCanvasValidationState
    {
        Unvalidated = 1,
        Validated = 2,
    }

    public sealed class SectorCanvasValidationStamp
    {
        public SectorCanvasValidationStamp(
            SectorCanvasValidationState state,
            string passCatalogDigest,
            string layerCatalogDigest,
            string sourceArtifactSetDigest,
            string resolvedCellsDigest,
            string validationRulesetVersion)
        {
            State = state;
            PassCatalogDigest = passCatalogDigest ?? string.Empty;
            LayerCatalogDigest = layerCatalogDigest ?? string.Empty;
            SourceArtifactSetDigest = sourceArtifactSetDigest ?? string.Empty;
            ResolvedCellsDigest = resolvedCellsDigest ?? string.Empty;
            ValidationRulesetVersion = validationRulesetVersion ?? string.Empty;
        }

        public SectorCanvasValidationState State { get; }
        public string PassCatalogDigest { get; }
        public string LayerCatalogDigest { get; }
        public string SourceArtifactSetDigest { get; }
        public string ResolvedCellsDigest { get; }
        public string ValidationRulesetVersion { get; }
        public string StableDigest => BakingCanonicalDigest.ComputeStamp(this);
    }

    public sealed class SectorCanvasContract
    {
        private readonly ReadOnlyCollection<SectorCanvasCell> cells;

        public SectorCanvasContract(
            SectorCanvasId id,
            int width,
            int height,
            IEnumerable<SectorCanvasCell> cells,
            SectorCanvasValidationStamp validationStamp)
        {
            Id = id;
            Width = width;
            Height = height;
            var copy = cells == null ? Array.Empty<SectorCanvasCell>() : cells.ToArray();
            Array.Sort(copy, CompareCells);
            this.cells = new ReadOnlyCollection<SectorCanvasCell>(copy);
            ValidationStamp = validationStamp;
        }

        public SectorCanvasId Id { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<SectorCanvasCell> Cells => cells;
        public SectorCanvasValidationStamp ValidationStamp { get; }

        private static int CompareCells(SectorCanvasCell left, SectorCanvasCell right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return left.CanonicalIndex.CompareTo(right.CanonicalIndex);
        }
    }
}
