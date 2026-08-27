using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public enum MicroPatternDensityPolicy
    {
        Uncalibrated = 1,
    }

    public enum MicroPatternSilhouetteClass
    {
        NoGeometry = 1,
        AddOnly = 2,
        CarveOnly = 3,
        Mixed = 4,
    }

    public sealed class MicroPatternBiomeProfile
    {
        private readonly ReadOnlyCollection<string> motifMetadata;
        private readonly ReadOnlyCollection<MicroPatternSilhouetteClass> silhouetteClasses;

        public MicroPatternBiomeProfile(
            MoonpalaceBiomeId biome,
            IEnumerable<string> motifMetadata,
            string safetyMeaning,
            MicroPatternDensityPolicy densityPolicy,
            IEnumerable<MicroPatternSilhouetteClass> silhouetteClasses)
        {
            Biome = biome;
            SafetyMeaning = safetyMeaning ?? string.Empty;
            DensityPolicy = densityPolicy;

            var motifCopy = motifMetadata == null
                ? Array.Empty<string>()
                : motifMetadata.Select(value => value ?? string.Empty).ToArray();
            Array.Sort(motifCopy, StringComparer.Ordinal);
            this.motifMetadata = new ReadOnlyCollection<string>(motifCopy);

            var silhouetteCopy = silhouetteClasses == null
                ? Array.Empty<MicroPatternSilhouetteClass>()
                : silhouetteClasses.ToArray();
            Array.Sort(silhouetteCopy, (left, right) => ((int)left).CompareTo((int)right));
            this.silhouetteClasses =
                new ReadOnlyCollection<MicroPatternSilhouetteClass>(silhouetteCopy);
        }

        public MoonpalaceBiomeId Biome { get; }
        public IReadOnlyList<string> MotifMetadata => motifMetadata;
        public string SafetyMeaning { get; }
        public MicroPatternDensityPolicy DensityPolicy { get; }
        public IReadOnlyList<MicroPatternSilhouetteClass> SilhouetteClasses => silhouetteClasses;
    }

    public enum MicroPatternProfileValidationErrorCode
    {
        MissingInput = 1,
        MissingBiome = 2,
        DuplicateBiome = 3,
        UnknownBiome = 4,
        MissingMotif = 5,
        DuplicateMotif = 6,
        InvalidMotifToken = 7,
        MissingSafetyMeaning = 8,
        InvalidDensityPolicy = 9,
        MissingSilhouetteClass = 10,
        DuplicateSilhouetteClass = 11,
        InvalidSilhouetteClass = 12,
    }

    public sealed class MicroPatternProfileValidationError :
        IEquatable<MicroPatternProfileValidationError>,
        IComparable<MicroPatternProfileValidationError>
    {
        public MicroPatternProfileValidationError(
            MicroPatternProfileValidationErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternProfileValidationErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternProfileValidationError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternProfileValidationError other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj) => Equals(obj as MicroPatternProfileValidationError);
        public override int GetHashCode() => ToString().GetHashCode();
        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class MicroPatternProfileValidationResult
    {
        private readonly ReadOnlyCollection<MicroPatternProfileValidationError> errors;

        internal MicroPatternProfileValidationResult(
            MicroPatternBiomeProfileCatalog catalog,
            IEnumerable<MicroPatternProfileValidationError> errors)
        {
            var copy = (errors ?? Array.Empty<MicroPatternProfileValidationError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<MicroPatternProfileValidationError>(copy);
            Catalog = copy.Length == 0 ? catalog : null;
        }

        public bool IsValid => Catalog != null && errors.Count == 0;
        public MicroPatternBiomeProfileCatalog Catalog { get; }
        public IReadOnlyList<MicroPatternProfileValidationError> Errors => errors;
    }

    public sealed class MicroPatternBiomeProfileCatalog
    {
        private readonly ReadOnlyCollection<MicroPatternBiomeProfile> profiles;

        private MicroPatternBiomeProfileCatalog(
            IEnumerable<MicroPatternBiomeProfile> profiles,
            string stableDigest)
        {
            var copy = profiles.OrderBy(value => value.Biome.Order).ToArray();
            this.profiles = new ReadOnlyCollection<MicroPatternBiomeProfile>(copy);
            StableDigest = stableDigest;
        }

        public IReadOnlyList<MicroPatternBiomeProfile> Profiles => profiles;
        public string StableDigest { get; }

        public bool TryGetProfile(
            MoonpalaceBiomeId biome,
            out MicroPatternBiomeProfile profile)
        {
            profile = profiles.FirstOrDefault(value => value.Biome == biome);
            return profile != null;
        }

        public static MicroPatternBiomeProfileCatalog CreateBuiltIn()
        {
            var result = Validate(new[]
            {
                Profile(
                    MoonpalaceBiomeId.MoonCrater,
                    new[] { "BrokenSlope", "Bowl", "RockShelf" },
                    "Protect wide view and projectile lanes; reduce meaningless large flats."),
                Profile(
                    MoonpalaceBiomeId.CassiaRoot,
                    new[] { "RootArch", "VerticalTunnel", "HollowPocket" },
                    "Preserve vertical movement and small hollows; never shrink protected routes."),
                Profile(
                    MoonpalaceBiomeId.AbandonedMill,
                    new[] { "BrokenPillar", "BeamOverhang", "OrthogonalCarve" },
                    "Keep orthogonal structure; gears and ladders are not pattern-owned."),
                Profile(
                    MoonpalaceBiomeId.MoonDough,
                    new[] { "BounceCup", "SoftPocket", "StickyShelf" },
                    "Keep rounded pockets and recovery floors traversable without bounce."),
            });

            if (!result.IsValid)
            {
                throw new InvalidOperationException("Built-in MicroPattern biome profiles are invalid.");
            }

            return result.Catalog;
        }

        public static MicroPatternProfileValidationResult Validate(
            IEnumerable<MicroPatternBiomeProfile> source)
        {
            var errors = new List<MicroPatternProfileValidationError>();
            if (source == null)
            {
                errors.Add(Error(
                    MicroPatternProfileValidationErrorCode.MissingInput,
                    "profiles",
                    "Profile input is required."));
                return new MicroPatternProfileValidationResult(null, errors);
            }

            var values = source.ToArray();
            foreach (var profile in values)
            {
                if (profile == null)
                {
                    errors.Add(Error(
                        MicroPatternProfileValidationErrorCode.MissingInput,
                        "profiles",
                        "Profile entry is required."));
                    continue;
                }

                var biomePath = profile.Biome.IsDefined
                    ? "profiles[" + profile.Biome.CanonicalId + "]"
                    : "profiles[UNKNOWN]";
                if (!profile.Biome.IsDefined)
                {
                    errors.Add(Error(
                        MicroPatternProfileValidationErrorCode.UnknownBiome,
                        biomePath,
                        "Biome must be one of the exact four typed Moonpalace biomes."));
                }

                if (profile.MotifMetadata.Count == 0)
                {
                    errors.Add(Error(
                        MicroPatternProfileValidationErrorCode.MissingMotif,
                        biomePath + ".motifs",
                        "At least one stable motif token is required."));
                }

                foreach (var motif in profile.MotifMetadata)
                {
                    if (!IsMotifToken(motif))
                    {
                        errors.Add(Error(
                            MicroPatternProfileValidationErrorCode.InvalidMotifToken,
                            biomePath + ".motifs",
                            motif));
                    }
                }

                foreach (var duplicate in profile.MotifMetadata
                             .GroupBy(value => value, StringComparer.Ordinal)
                             .Where(group => group.Count() > 1))
                {
                    errors.Add(Error(
                        MicroPatternProfileValidationErrorCode.DuplicateMotif,
                        biomePath + ".motifs",
                        duplicate.Key));
                }

                if (profile.SafetyMeaning.Length == 0)
                {
                    errors.Add(Error(
                        MicroPatternProfileValidationErrorCode.MissingSafetyMeaning,
                        biomePath + ".safety",
                        "Safety meaning is required."));
                }

                if (profile.DensityPolicy != MicroPatternDensityPolicy.Uncalibrated)
                {
                    errors.Add(Error(
                        MicroPatternProfileValidationErrorCode.InvalidDensityPolicy,
                        biomePath + ".densityPolicy",
                        Number((int)profile.DensityPolicy)));
                }

                ValidateSilhouettes(profile, biomePath, errors);
            }

            foreach (var expected in ExactBiomes())
            {
                var count = values.Count(value => value != null && value.Biome == expected);
                if (count == 0)
                {
                    errors.Add(Error(
                        MicroPatternProfileValidationErrorCode.MissingBiome,
                        "profiles[" + expected.CanonicalId + "]",
                        expected.CanonicalId));
                }
                else if (count > 1)
                {
                    errors.Add(Error(
                        MicroPatternProfileValidationErrorCode.DuplicateBiome,
                        "profiles[" + expected.CanonicalId + "]",
                        Number(count)));
                }
            }

            if (errors.Count != 0)
            {
                return new MicroPatternProfileValidationResult(null, errors);
            }

            var ordered = values.OrderBy(value => value.Biome.Order).ToArray();
            var material = new StringBuilder();
            MicroPatternContractDigest.Append(material, "RULESET", "MAP10_04_PROFILE_V1");
            foreach (var profile in ordered)
            {
                MicroPatternContractDigest.Append(
                    material,
                    "PROFILE",
                    profile.Biome.CanonicalId,
                    profile.DensityPolicy.ToString(),
                    profile.SafetyMeaning);
                foreach (var motif in profile.MotifMetadata)
                {
                    MicroPatternContractDigest.Append(material, "MOTIF", motif);
                }
                foreach (var silhouette in profile.SilhouetteClasses)
                {
                    MicroPatternContractDigest.Append(material, "SILHOUETTE", silhouette.ToString());
                }
            }

            var catalog = new MicroPatternBiomeProfileCatalog(
                ordered,
                MicroPatternContractDigest.Hash(material));
            return new MicroPatternProfileValidationResult(catalog, errors);
        }

        private static MicroPatternBiomeProfile Profile(
            MoonpalaceBiomeId biome,
            IEnumerable<string> motifs,
            string safetyMeaning)
        {
            return new MicroPatternBiomeProfile(
                biome,
                motifs,
                safetyMeaning,
                MicroPatternDensityPolicy.Uncalibrated,
                ExactSilhouettes());
        }

        private static MoonpalaceBiomeId[] ExactBiomes()
        {
            return new[]
            {
                MoonpalaceBiomeId.MoonCrater,
                MoonpalaceBiomeId.CassiaRoot,
                MoonpalaceBiomeId.AbandonedMill,
                MoonpalaceBiomeId.MoonDough,
            };
        }

        private static void ValidateSilhouettes(
            MicroPatternBiomeProfile profile,
            string path,
            ICollection<MicroPatternProfileValidationError> errors)
        {
            foreach (var silhouette in profile.SilhouetteClasses)
            {
                if (silhouette < MicroPatternSilhouetteClass.NoGeometry ||
                    silhouette > MicroPatternSilhouetteClass.Mixed)
                {
                    errors.Add(Error(
                        MicroPatternProfileValidationErrorCode.InvalidSilhouetteClass,
                        path + ".silhouettes",
                        Number((int)silhouette)));
                }
            }

            foreach (var duplicate in profile.SilhouetteClasses
                         .GroupBy(value => value)
                         .Where(group => group.Count() > 1))
            {
                errors.Add(Error(
                    MicroPatternProfileValidationErrorCode.DuplicateSilhouetteClass,
                    path + ".silhouettes",
                    duplicate.Key.ToString()));
            }

            foreach (var expected in ExactSilhouettes())
            {
                if (!profile.SilhouetteClasses.Contains(expected))
                {
                    errors.Add(Error(
                        MicroPatternProfileValidationErrorCode.MissingSilhouetteClass,
                        path + ".silhouettes",
                        expected.ToString()));
                }
            }
        }

        private static bool IsMotifToken(string value)
        {
            if (string.IsNullOrEmpty(value) || value[0] < 'A' || value[0] > 'Z') return false;
            for (var index = 1; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < 'a' || character > 'z') &&
                    (character < '0' || character > '9'))
                {
                    return false;
                }
            }
            return true;
        }

        private static MicroPatternSilhouetteClass[] ExactSilhouettes()
        {
            return new[]
            {
                MicroPatternSilhouetteClass.NoGeometry,
                MicroPatternSilhouetteClass.AddOnly,
                MicroPatternSilhouetteClass.CarveOnly,
                MicroPatternSilhouetteClass.Mixed,
            };
        }

        private static MicroPatternProfileValidationError Error(
            MicroPatternProfileValidationErrorCode code,
            string path,
            string detail)
        {
            return new MicroPatternProfileValidationError(code, path, detail);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public enum MicroPatternFeatureSummaryErrorCode
    {
        MissingPlan = 1,
        InvalidPlanDigest = 2,
        InvalidCoverage = 3,
        InvalidInstruction = 4,
    }

    public sealed class MicroPatternFeatureSummaryError :
        IEquatable<MicroPatternFeatureSummaryError>,
        IComparable<MicroPatternFeatureSummaryError>
    {
        public MicroPatternFeatureSummaryError(
            MicroPatternFeatureSummaryErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternFeatureSummaryErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternFeatureSummaryError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternFeatureSummaryError other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj) => Equals(obj as MicroPatternFeatureSummaryError);
        public override int GetHashCode() => ToString().GetHashCode();
        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class MicroPatternFeatureSummaryResult
    {
        private readonly ReadOnlyCollection<MicroPatternFeatureSummaryError> errors;

        internal MicroPatternFeatureSummaryResult(
            MicroPatternFeatureSummary summary,
            IEnumerable<MicroPatternFeatureSummaryError> errors)
        {
            var copy = (errors ?? Array.Empty<MicroPatternFeatureSummaryError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<MicroPatternFeatureSummaryError>(copy);
            Summary = copy.Length == 0 ? summary : null;
        }

        public bool Success => Summary != null && errors.Count == 0;
        public MicroPatternFeatureSummary Summary { get; }
        public IReadOnlyList<MicroPatternFeatureSummaryError> Errors => errors;
    }

    public sealed class MicroPatternFeatureSummary
    {
        private MicroPatternFeatureSummary(
            int addSolidCellCount,
            int carveAirCellCount,
            int geometryWriteCellCount,
            int totalWriteCount,
            int protectedOverlapCount,
            int forcedNoChangeCount,
            MicroPatternSilhouetteClass silhouetteClass,
            string stableDigest)
        {
            AddSolidCellCount = addSolidCellCount;
            CarveAirCellCount = carveAirCellCount;
            GeometryWriteCellCount = geometryWriteCellCount;
            TotalWriteCount = totalWriteCount;
            ProtectedOverlapCount = protectedOverlapCount;
            ForcedNoChangeCount = forcedNoChangeCount;
            SilhouetteClass = silhouetteClass;
            StableDigest = stableDigest;
        }

        public int AddSolidCellCount { get; }
        public int CarveAirCellCount { get; }
        public int GeometryWriteCellCount { get; }
        public int GeometryDensityNumerator => GeometryWriteCellCount;
        public int GeometryDensityDenominator => MicroPatternDefinition.RequiredCellCount;
        public int TotalWriteCount { get; }
        public int ProtectedOverlapCount { get; }
        public int ForcedNoChangeCount { get; }
        public MicroPatternSilhouetteClass SilhouetteClass { get; }
        public string StableDigest { get; }

        public static MicroPatternFeatureSummaryResult Create(
            MicroPatternDefinition definition,
            MicroPatternTransform transform,
            MicroPatternApplicationPlan plan)
        {
            var errors = new List<MicroPatternFeatureSummaryError>();
            var transformed = MicroPatternTransformer.Transform(definition, transform);
            if (!transformed.Success)
            {
                foreach (var error in transformed.Errors)
                {
                    errors.Add(new MicroPatternFeatureSummaryError(
                        MicroPatternFeatureSummaryErrorCode.InvalidInstruction,
                        error.Path,
                        error.ToString()));
                }
            }

            if (plan == null)
            {
                errors.Add(new MicroPatternFeatureSummaryError(
                    MicroPatternFeatureSummaryErrorCode.MissingPlan,
                    "plan",
                    "A successful application plan is required."));
                return new MicroPatternFeatureSummaryResult(null, errors);
            }

            if (transformed.Success &&
                (plan.SourcePatternId != transformed.Pattern.SourcePatternId ||
                 !string.Equals(plan.SourceDigest, transformed.Pattern.SourceDigest, StringComparison.Ordinal) ||
                 plan.Transform != transform))
            {
                errors.Add(new MicroPatternFeatureSummaryError(
                    MicroPatternFeatureSummaryErrorCode.InvalidInstruction,
                    "plan.source",
                    "Definition, transform, and application plan do not match."));
            }

            if (!MicroPatternContractDigest.IsLowerHexDigest(plan.StableDigest))
            {
                errors.Add(new MicroPatternFeatureSummaryError(
                    MicroPatternFeatureSummaryErrorCode.InvalidPlanDigest,
                    "plan.stableDigest",
                    plan.StableDigest));
            }

            var planCells = plan.Cells.ToArray();
            if (planCells.Length != MicroPatternDefinition.RequiredCellCount ||
                planCells.Any(value => value == null) ||
                planCells.Select(value => value.LocalCoordinate).Distinct().Count() !=
                MicroPatternDefinition.RequiredCellCount)
            {
                errors.Add(new MicroPatternFeatureSummaryError(
                    MicroPatternFeatureSummaryErrorCode.InvalidCoverage,
                    "plan.cells",
                    planCells.Length.ToString(CultureInfo.InvariantCulture)));
            }

            var addSolid = 0;
            var carveAir = 0;
            var totalWrites = 0;
            var featureCells = transformed.Success
                ? transformed.Pattern.Cells
                : Array.Empty<MicroPatternCell>();
            foreach (var cell in featureCells.Where(value => value != null))
            {
                var cellGeometryWrites = cell.Instructions.Where(value =>
                    value != null &&
                    value.Layer == MicroPatternLayer.Geometry &&
                    value.Operation != MicroPatternOperation.NoChange).ToArray();
                if (cellGeometryWrites.Length > 1)
                {
                    errors.Add(new MicroPatternFeatureSummaryError(
                        MicroPatternFeatureSummaryErrorCode.InvalidInstruction,
                        CoordinatePath(cell.Coordinate),
                        "More than one Geometry write exists."));
                }

                foreach (var instruction in cell.Instructions)
                {
                    if (instruction == null)
                    {
                        errors.Add(new MicroPatternFeatureSummaryError(
                            MicroPatternFeatureSummaryErrorCode.InvalidInstruction,
                            CoordinatePath(cell.Coordinate),
                            "Instruction is required."));
                        continue;
                    }

                    if (instruction.Operation != MicroPatternOperation.NoChange) totalWrites++;
                    if (instruction.Layer == MicroPatternLayer.Geometry &&
                        instruction.Operation == MicroPatternOperation.AddSolid) addSolid++;
                    if (instruction.Layer == MicroPatternLayer.Geometry &&
                        instruction.Operation == MicroPatternOperation.CarveAir) carveAir++;
                }
            }

            if (errors.Count != 0)
            {
                return new MicroPatternFeatureSummaryResult(null, errors);
            }

            var geometryWrites = addSolid + carveAir;
            var silhouette = geometryWrites == 0
                ? MicroPatternSilhouetteClass.NoGeometry
                : carveAir == 0
                    ? MicroPatternSilhouetteClass.AddOnly
                    : addSolid == 0
                        ? MicroPatternSilhouetteClass.CarveOnly
                        : MicroPatternSilhouetteClass.Mixed;
            var forcedNoChange = plan.ProtectedPolicy == MicroPatternProtectedPolicy.ForceNoChange
                ? plan.ProtectedHits.Sum(value => value.RemovedWriteCount)
                : 0;

            var material = new StringBuilder();
            MicroPatternContractDigest.Append(material, "RULESET", "MAP10_04_FEATURE_V1");
            MicroPatternContractDigest.Append(material, "PLAN", plan.StableDigest);
            MicroPatternContractDigest.Append(
                material,
                "COUNTS",
                Number(addSolid),
                Number(carveAir),
                Number(geometryWrites),
                Number(MicroPatternDefinition.RequiredCellCount),
                Number(totalWrites),
                Number(plan.ProtectedHits.Count),
                Number(forcedNoChange),
                silhouette.ToString());

            var summary = new MicroPatternFeatureSummary(
                addSolid,
                carveAir,
                geometryWrites,
                totalWrites,
                plan.ProtectedHits.Count,
                forcedNoChange,
                silhouette,
                MicroPatternContractDigest.Hash(material));
            return new MicroPatternFeatureSummaryResult(summary, errors);
        }

        private static string CoordinatePath(LocalTileCoord coordinate)
        {
            return "plan.cells[" + Number(coordinate.X) + "," + Number(coordinate.Y) + "]";
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    internal static class MicroPatternContractDigest
    {
        internal static void Append(StringBuilder target, params string[] fields)
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

        internal static string Hash(StringBuilder material)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(Encoding.UTF8.GetBytes(material.ToString()))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        internal static bool IsLowerHexDigest(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var character in value)
            {
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f')) return false;
            }
            return true;
        }
    }
}
