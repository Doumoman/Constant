using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public sealed class MicroPatternSilhouetteSignature
    {
        internal MicroPatternSilhouetteSignature(
            ushort addSolidMask,
            ushort carveAirMask,
            MicroPatternTransform canonicalTransform,
            string stableDigest)
        {
            AddSolidMask = addSolidMask;
            CarveAirMask = carveAirMask;
            CanonicalTransform = canonicalTransform;
            StableDigest = stableDigest ?? string.Empty;
        }

        public ushort AddSolidMask { get; }
        public ushort CarveAirMask { get; }
        public MicroPatternTransform CanonicalTransform { get; }
        public string StableDigest { get; }
    }

    public enum MicroPatternSilhouetteSignatureErrorCode
    {
        MissingInput = 1,
        InvalidApplicationPlan = 2,
        DuplicateCoordinate = 3,
        InvalidCoordinate = 4,
    }

    public sealed class MicroPatternSilhouetteSignatureError :
        IEquatable<MicroPatternSilhouetteSignatureError>,
        IComparable<MicroPatternSilhouetteSignatureError>
    {
        public MicroPatternSilhouetteSignatureError(
            MicroPatternSilhouetteSignatureErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternSilhouetteSignatureErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternSilhouetteSignatureError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternSilhouetteSignatureError other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MicroPatternSilhouetteSignatureError);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return Code + "|" + Path + "|" + Detail;
        }
    }

    public sealed class MicroPatternSilhouetteSignatureResult
    {
        private readonly ReadOnlyCollection<MicroPatternSilhouetteSignatureError> errors;

        internal MicroPatternSilhouetteSignatureResult(
            MicroPatternSilhouetteSignature signature,
            IEnumerable<MicroPatternSilhouetteSignatureError> errors)
        {
            var copy = (errors ?? Array.Empty<MicroPatternSilhouetteSignatureError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<MicroPatternSilhouetteSignatureError>(copy);
            Signature = copy.Length == 0 ? signature : null;
            StableDigest = Signature == null ? string.Empty : Signature.StableDigest;
        }

        public bool Success => Signature != null && errors.Count == 0;
        public MicroPatternSilhouetteSignature Signature { get; }
        public IReadOnlyList<MicroPatternSilhouetteSignatureError> Errors => errors;
        public string StableDigest { get; }
    }

    public static class MicroPatternSilhouetteSignatureBuilder
    {
        public static MicroPatternSilhouetteSignatureResult Build(
            MicroPatternApplicationPlan applicationPlan)
        {
            var errors = new List<MicroPatternSilhouetteSignatureError>();
            if (applicationPlan == null)
            {
                errors.Add(Error(
                    MicroPatternSilhouetteSignatureErrorCode.MissingInput,
                    "applicationPlan",
                    "A successful MAP10_02 application plan is required."));
                return new MicroPatternSilhouetteSignatureResult(null, errors);
            }

            if (!MicroPatternContractDigest.IsLowerHexDigest(applicationPlan.StableDigest))
            {
                errors.Add(Error(
                    MicroPatternSilhouetteSignatureErrorCode.InvalidApplicationPlan,
                    "applicationPlan.stableDigest",
                    applicationPlan.StableDigest));
            }

            var seen = new HashSet<LocalTileCoord>();
            ushort addMask = 0;
            ushort carveMask = 0;
            foreach (var cell in applicationPlan.Cells)
            {
                if (cell == null)
                {
                    errors.Add(Error(
                        MicroPatternSilhouetteSignatureErrorCode.InvalidApplicationPlan,
                        "applicationPlan.cells",
                        "Null prepared cell."));
                    continue;
                }

                var coordinate = cell.LocalCoordinate;
                if (coordinate.X < 0 || coordinate.X >= MicroPatternDefinition.RequiredWidth ||
                    coordinate.Y < 0 || coordinate.Y >= MicroPatternDefinition.RequiredHeight)
                {
                    errors.Add(Error(
                        MicroPatternSilhouetteSignatureErrorCode.InvalidCoordinate,
                        CoordinatePath(coordinate),
                        "Expected local coordinate in 0..3."));
                    continue;
                }

                if (!seen.Add(coordinate))
                {
                    errors.Add(Error(
                        MicroPatternSilhouetteSignatureErrorCode.DuplicateCoordinate,
                        CoordinatePath(coordinate),
                        "Prepared coordinate must be unique."));
                    continue;
                }

                var geometry = cell.Instructions
                    .Where(value => value != null && value.Layer == MicroPatternLayer.Geometry)
                    .ToArray();
                if (geometry.Length != 1)
                {
                    errors.Add(Error(
                        MicroPatternSilhouetteSignatureErrorCode.InvalidApplicationPlan,
                        CoordinatePath(coordinate),
                        "Exactly one effective Geometry instruction is required."));
                    continue;
                }

                var bit = (ushort)(1 << (coordinate.Y * 4 + coordinate.X));
                switch (geometry[0].Operation)
                {
                    case MicroPatternOperation.NoChange:
                        break;
                    case MicroPatternOperation.AddSolid:
                        addMask |= bit;
                        break;
                    case MicroPatternOperation.CarveAir:
                        carveMask |= bit;
                        break;
                    default:
                        errors.Add(Error(
                            MicroPatternSilhouetteSignatureErrorCode.InvalidApplicationPlan,
                            CoordinatePath(coordinate),
                            "Invalid effective Geometry operation: " + geometry[0].Operation));
                        break;
                }
            }

            if (seen.Count != MicroPatternDefinition.RequiredCellCount)
            {
                errors.Add(Error(
                    MicroPatternSilhouetteSignatureErrorCode.InvalidApplicationPlan,
                    "applicationPlan.cells",
                    "Exact 4x4 prepared coverage is required."));
            }

            if (errors.Count != 0)
            {
                return new MicroPatternSilhouetteSignatureResult(null, errors);
            }

            var canonical = Canonicalize(addMask, carveMask);
            var digest = MicroPatternSilhouetteCanonicalDigest.Compute(
                canonical.AddSolidMask,
                canonical.CarveAirMask);
            return new MicroPatternSilhouetteSignatureResult(
                new MicroPatternSilhouetteSignature(
                    canonical.AddSolidMask,
                    canonical.CarveAirMask,
                    canonical.Transform,
                    digest),
                errors);
        }

        private static CanonicalPair Canonicalize(ushort addMask, ushort carveMask)
        {
            CanonicalPair best = default;
            var hasBest = false;
            foreach (var transform in new[]
                     {
                         MicroPatternTransform.R0,
                         MicroPatternTransform.MirrorX,
                         MicroPatternTransform.MirrorY,
                         MicroPatternTransform.R180,
                     })
            {
                var candidate = new CanonicalPair(
                    TransformMask(addMask, transform),
                    TransformMask(carveMask, transform),
                    transform);
                if (!hasBest || candidate.PackedValue < best.PackedValue)
                {
                    best = candidate;
                    hasBest = true;
                }
            }
            return best;
        }

        private static ushort TransformMask(ushort mask, MicroPatternTransform transform)
        {
            ushort result = 0;
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    var sourceBit = 1 << (y * 4 + x);
                    if ((mask & sourceBit) == 0) continue;
                    int targetX;
                    int targetY;
                    switch (transform)
                    {
                        case MicroPatternTransform.R0:
                            targetX = x;
                            targetY = y;
                            break;
                        case MicroPatternTransform.MirrorX:
                            targetX = 3 - x;
                            targetY = y;
                            break;
                        case MicroPatternTransform.MirrorY:
                            targetX = x;
                            targetY = 3 - y;
                            break;
                        case MicroPatternTransform.R180:
                            targetX = 3 - x;
                            targetY = 3 - y;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(transform));
                    }
                    result |= (ushort)(1 << (targetY * 4 + targetX));
                }
            }
            return result;
        }

        private static MicroPatternSilhouetteSignatureError Error(
            MicroPatternSilhouetteSignatureErrorCode code,
            string path,
            string detail)
        {
            return new MicroPatternSilhouetteSignatureError(code, path, detail);
        }

        private static string CoordinatePath(LocalTileCoord coordinate)
        {
            return "applicationPlan.cells[" +
                   coordinate.X.ToString(CultureInfo.InvariantCulture) + "," +
                   coordinate.Y.ToString(CultureInfo.InvariantCulture) + "]";
        }

        private readonly struct CanonicalPair
        {
            public CanonicalPair(
                ushort addSolidMask,
                ushort carveAirMask,
                MicroPatternTransform transform)
            {
                AddSolidMask = addSolidMask;
                CarveAirMask = carveAirMask;
                Transform = transform;
            }

            public ushort AddSolidMask { get; }
            public ushort CarveAirMask { get; }
            public MicroPatternTransform Transform { get; }
            public uint PackedValue => ((uint)AddSolidMask << 16) | CarveAirMask;
        }
    }

    public static class MicroPatternSilhouetteCanonicalDigest
    {
        public const string Ruleset = "MAP10_05_SILHOUETTE_V1";

        public static string Compute(ushort addSolidMask, ushort carveAirMask)
        {
            var material = new StringBuilder();
            MicroPatternContractDigest.Append(material, "RULESET", Ruleset);
            MicroPatternContractDigest.Append(
                material,
                "MASKS",
                addSolidMask.ToString("X4", CultureInfo.InvariantCulture),
                carveAirMask.ToString("X4", CultureInfo.InvariantCulture));
            return MicroPatternContractDigest.Hash(material);
        }
    }
}
