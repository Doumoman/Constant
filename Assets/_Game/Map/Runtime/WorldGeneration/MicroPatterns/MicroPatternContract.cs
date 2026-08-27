using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public readonly struct MicroPatternId : IEquatable<MicroPatternId>, IComparable<MicroPatternId>
    {
        private readonly string value;

        public MicroPatternId(string value)
        {
            this.value = value;
        }

        public string Value => value ?? string.Empty;

        public int CompareTo(MicroPatternId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is MicroPatternId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(MicroPatternId left, MicroPatternId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MicroPatternId left, MicroPatternId right)
        {
            return !left.Equals(right);
        }
    }

    public enum MicroPatternLayer
    {
        Geometry = 1,
        Surface = 2,
        Affordance = 3,
        Material = 4,
        Hazard = 5,
        Marker = 6,
    }

    public enum MicroPatternOperation
    {
        NoChange = 1,
        AddSolid = 2,
        CarveAir = 3,
        SetSurface = 4,
        SetAffordance = 5,
        SetMaterial = 6,
        SetHazard = 7,
        SetMarker = 8,
    }

    public enum MicroPatternTransform
    {
        R0 = 1,
        MirrorX = 2,
        MirrorY = 3,
        R180 = 4,
    }

    public enum MicroPatternProtectedPolicy
    {
        ForceNoChange = 1,
        RejectCandidate = 2,
    }

    public sealed class MicroPatternInstruction
    {
        public MicroPatternInstruction(
            MicroPatternLayer layer,
            MicroPatternOperation operation,
            string payloadId = null)
        {
            Layer = layer;
            Operation = operation;
            PayloadId = payloadId ?? string.Empty;
        }

        public MicroPatternLayer Layer { get; }
        public MicroPatternOperation Operation { get; }
        public string PayloadId { get; }
    }

    public sealed class MicroPatternCell
    {
        private readonly ReadOnlyCollection<MicroPatternInstruction> instructions;

        public MicroPatternCell(
            LocalTileCoord coordinate,
            IEnumerable<MicroPatternInstruction> instructions = null)
        {
            Coordinate = coordinate;
            var copy = instructions == null
                ? Array.Empty<MicroPatternInstruction>()
                : instructions.ToArray();
            Array.Sort(copy, CompareInstructions);
            this.instructions = new ReadOnlyCollection<MicroPatternInstruction>(copy);
        }

        public LocalTileCoord Coordinate { get; }
        public IReadOnlyList<MicroPatternInstruction> Instructions => instructions;

        private static int CompareInstructions(
            MicroPatternInstruction left,
            MicroPatternInstruction right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            var comparison = ((int)left.Layer).CompareTo((int)right.Layer);
            if (comparison != 0) return comparison;
            comparison = ((int)left.Operation).CompareTo((int)right.Operation);
            if (comparison != 0) return comparison;
            return string.Compare(left.PayloadId, right.PayloadId, StringComparison.Ordinal);
        }
    }

    public sealed class MicroPatternDefinition
    {
        public const int RequiredWidth = 4;
        public const int RequiredHeight = 4;
        public const int RequiredCellCount = RequiredWidth * RequiredHeight;
        public const int MinimumWeight = 1;
        public const int MaximumWeight = 10000;

        private readonly ReadOnlyCollection<MicroPatternCell> cells;
        private readonly ReadOnlyCollection<MoonpalaceBiomeId> allowedBiomes;
        private readonly ReadOnlyCollection<MicroPatternTransform> allowedTransforms;

        public MicroPatternDefinition(
            MicroPatternId id,
            int width,
            int height,
            IEnumerable<MicroPatternCell> cells,
            int weight,
            IEnumerable<MoonpalaceBiomeId> allowedBiomes,
            IEnumerable<MicroPatternTransform> allowedTransforms,
            MicroPatternProtectedPolicy protectedPolicy,
            string displayId = null)
        {
            Id = id;
            Width = width;
            Height = height;
            Weight = weight;
            ProtectedPolicy = protectedPolicy;
            DisplayId = displayId ?? string.Empty;

            var cellCopy = cells == null ? Array.Empty<MicroPatternCell>() : cells.ToArray();
            Array.Sort(cellCopy, CompareCells);
            this.cells = new ReadOnlyCollection<MicroPatternCell>(cellCopy);

            var biomeCopy = allowedBiomes == null
                ? Array.Empty<MoonpalaceBiomeId>()
                : allowedBiomes.ToArray();
            Array.Sort(biomeCopy, CompareBiomes);
            this.allowedBiomes = new ReadOnlyCollection<MoonpalaceBiomeId>(biomeCopy);

            var transformCopy = allowedTransforms == null
                ? Array.Empty<MicroPatternTransform>()
                : allowedTransforms.ToArray();
            Array.Sort(transformCopy, (left, right) => ((int)left).CompareTo((int)right));
            this.allowedTransforms = new ReadOnlyCollection<MicroPatternTransform>(transformCopy);
        }

        public MicroPatternId Id { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<MicroPatternCell> Cells => cells;
        public int Weight { get; }
        public IReadOnlyList<MoonpalaceBiomeId> AllowedBiomes => allowedBiomes;
        public IReadOnlyList<MicroPatternTransform> AllowedTransforms => allowedTransforms;
        public MicroPatternProtectedPolicy ProtectedPolicy { get; }
        public string DisplayId { get; }

        public string ComputeStableDigest()
        {
            var validation = MicroPatternValidator.Validate(this);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    "Cannot compute a published digest for an invalid MicroPattern definition.");
            }

            return validation.StableDigest;
        }

        public static int CanonicalCellIndex(LocalTileCoord coordinate)
        {
            return (coordinate.Y * RequiredWidth) + coordinate.X;
        }

        private static int CompareCells(MicroPatternCell left, MicroPatternCell right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            var comparison = left.Coordinate.Y.CompareTo(right.Coordinate.Y);
            return comparison != 0
                ? comparison
                : left.Coordinate.X.CompareTo(right.Coordinate.X);
        }

        private static int CompareBiomes(MoonpalaceBiomeId left, MoonpalaceBiomeId right)
        {
            if (left.IsDefined && right.IsDefined)
            {
                return string.Compare(left.CanonicalId, right.CanonicalId, StringComparison.Ordinal);
            }

            if (left.IsDefined) return -1;
            if (right.IsDefined) return 1;
            return left.GetHashCode().CompareTo(right.GetHashCode());
        }
    }
}
