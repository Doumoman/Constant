using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Activities
{
    public enum ActivityCompatibilityErrorCode
    {
        MissingInput,
        InvalidProfile,
        InvalidOpportunity,
        IdentityMismatch,
        ArtifactDigestMismatch,
        PatchOwnershipMismatch,
        InvalidClearance,
        DuplicateCandidate,
        EmptyCandidateIndex,
        InvalidFrequencyPolicy,
        InvalidStrongCap,
        InvalidRngBinding,
        BudgetMismatch,
        TargetUnsatisfied,
        StrongCapUnsatisfiable,
        NonCanonicalPublication,
    }

    public sealed class ActivityCompatibilityError
    {
        public ActivityCompatibilityError(
            ActivityCompatibilityErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public ActivityCompatibilityErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }
    }

    public sealed class ActivityPlacementCandidate
    {
        internal ActivityPlacementCandidate(
            ActivityPlacementOpportunity opportunity,
            ActivityPlacementProfile profile,
            string candidateKey)
        {
            Opportunity = opportunity;
            Profile = profile;
            CandidateKey = candidateKey ?? string.Empty;
        }

        public ActivityPlacementOpportunity Opportunity { get; }
        public ActivityPlacementProfile Profile { get; }
        public string CandidateKey { get; }
        public string OpportunityId => Opportunity.OpportunityId;
        public ActivityStructureId ActivityId => Profile.ActivityId;
        public TerrainClusterId TerrainClusterId => Opportunity.TerrainClusterId;
        public SpineVariantId SpineVariantId => Opportunity.SpineVariantId;
        public MoonpalaceBiomeId Biome => Opportunity.PrimaryBiome;
        public PacingRole PacingRole => Opportunity.PacingRole;
        public AccessClass AccessClass => Opportunity.AccessClass;
        public int ActiveChunkCount => Opportunity.ActiveChunkCount;
        public int ClearanceWidth => Opportunity.Clearance.Width;
        public int ClearanceHeight => Opportunity.Clearance.Height;
        public int Weight => Profile.Weight;
        public ActivityStrengthClass Strength => Profile.Strength;
        public string ActivityDigest => Profile.ActivityDigest;
        public string ShellDigest => Profile.ShellDigest;
        public string RemovalSafetyDigest => Profile.RemovalSafetyDigest;
    }

    public sealed class ActivityCandidateIndex
    {
        private readonly ReadOnlyCollection<ActivityPlacementCandidate> candidates;
        private readonly ReadOnlyCollection<ActivityCompatibilityRejection> rejections;
        private readonly ReadOnlyDictionary<string, IReadOnlyList<ActivityPlacementCandidate>> byOpportunity;

        internal ActivityCandidateIndex(
            IEnumerable<ActivityPlacementCandidate> candidates,
            IEnumerable<ActivityCompatibilityRejection> rejections,
            string canonicalDigest)
        {
            var candidateCopy = (candidates ?? Array.Empty<ActivityPlacementCandidate>())
                .OrderBy(value => value.OpportunityId, StringComparer.Ordinal)
                .ThenBy(value => value.ActivityId.Value, StringComparer.Ordinal)
                .ThenBy(value => value.CandidateKey, StringComparer.Ordinal).ToArray();
            var rejectionCopy = (rejections ?? Array.Empty<ActivityCompatibilityRejection>())
                .OrderBy(value => value.OpportunityId, StringComparer.Ordinal)
                .ThenBy(value => value.ActivityId.Value, StringComparer.Ordinal)
                .ThenBy(value => value.Code)
                .ThenBy(value => value.Path, StringComparer.Ordinal)
                .ThenBy(value => value.Detail, StringComparer.Ordinal).ToArray();
            this.candidates = new ReadOnlyCollection<ActivityPlacementCandidate>(candidateCopy);
            this.rejections = new ReadOnlyCollection<ActivityCompatibilityRejection>(rejectionCopy);
            var lookup = new SortedDictionary<string, IReadOnlyList<ActivityPlacementCandidate>>(StringComparer.Ordinal);
            foreach (var group in candidateCopy.GroupBy(value => value.OpportunityId, StringComparer.Ordinal))
                lookup[group.Key] = new ReadOnlyCollection<ActivityPlacementCandidate>(group.ToArray());
            byOpportunity = new ReadOnlyDictionary<string, IReadOnlyList<ActivityPlacementCandidate>>(lookup);
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public IReadOnlyList<ActivityPlacementCandidate> Candidates => candidates;
        public IReadOnlyList<ActivityCompatibilityRejection> Rejections => rejections;
        public int CandidateCount => candidates.Count;
        public int RejectionCount => rejections.Count;
        public int RngStreamCreationCount => 0;
        public int RngDrawCount => 0;
        public string CanonicalDigest { get; }

        public IReadOnlyList<ActivityPlacementCandidate> GetCandidates(string opportunityId)
        {
            return opportunityId != null && byOpportunity.TryGetValue(opportunityId, out var values)
                ? values
                : Array.Empty<ActivityPlacementCandidate>();
        }
    }

    public sealed class ActivityCandidateIndexCompileRequest
    {
        private readonly ReadOnlyCollection<ActivityPlacementProfile> profiles;
        private readonly ReadOnlyCollection<ActivityPlacementOpportunity> opportunities;

        public ActivityCandidateIndexCompileRequest(
            IEnumerable<ActivityPlacementProfile> profiles,
            IEnumerable<ActivityPlacementOpportunity> opportunities,
            BiomePatchSnapshot patchOwnership,
            string expectedMap11CatalogDigest,
            string expectedMap11SignatureSetDigest,
            string expectedAuthoringManifestDigest)
        {
            this.profiles = new ReadOnlyCollection<ActivityPlacementProfile>(
                profiles == null ? Array.Empty<ActivityPlacementProfile>() : profiles.ToArray());
            this.opportunities = new ReadOnlyCollection<ActivityPlacementOpportunity>(
                opportunities == null ? Array.Empty<ActivityPlacementOpportunity>() : opportunities.ToArray());
            PatchOwnership = patchOwnership;
            ExpectedMap11CatalogDigest = expectedMap11CatalogDigest ?? string.Empty;
            ExpectedMap11SignatureSetDigest = expectedMap11SignatureSetDigest ?? string.Empty;
            ExpectedAuthoringManifestDigest = expectedAuthoringManifestDigest ?? string.Empty;
        }

        public IReadOnlyList<ActivityPlacementProfile> Profiles => profiles;
        public IReadOnlyList<ActivityPlacementOpportunity> Opportunities => opportunities;
        public BiomePatchSnapshot PatchOwnership { get; }
        public string ExpectedMap11CatalogDigest { get; }
        public string ExpectedMap11SignatureSetDigest { get; }
        public string ExpectedAuthoringManifestDigest { get; }
    }

    public sealed class ActivityCandidateIndexCompileResult
    {
        private readonly ReadOnlyCollection<ActivityCompatibilityError> errors;

        internal ActivityCandidateIndexCompileResult(
            ActivityCandidateIndex index,
            IEnumerable<ActivityCompatibilityError> errors)
        {
            Index = index;
            this.errors = new ReadOnlyCollection<ActivityCompatibilityError>(
                ActivityCompatibilityCanonical.SortErrors(errors).ToArray());
        }

        public bool Success => Index != null && errors.Count == 0;
        public ActivityCandidateIndex Index { get; }
        public IReadOnlyList<ActivityCompatibilityError> Errors => errors;
        public int RngStreamCreationCount => 0;
        public int RngDrawCount => 0;
    }

    public static class ActivityCandidateIndexCompiler
    {
        private const string RulesetVersion = "MAP12_03_COMPATIBILITY_V1";

        public static ActivityCandidateIndexCompileResult Compile(ActivityCandidateIndexCompileRequest request)
        {
            var errors = new List<ActivityCompatibilityError>();
            if (request == null)
            {
                Add(errors, ActivityCompatibilityErrorCode.MissingInput, "request", "Compile request is required.");
                return Failure(errors);
            }
            if (request.PatchOwnership == null)
                Add(errors, ActivityCompatibilityErrorCode.MissingInput, "patchOwnership", "BiomePatchSnapshot is required.");
            if (request.Profiles.Count == 0)
                Add(errors, ActivityCompatibilityErrorCode.MissingInput, "profiles", "At least one profile is required.");
            if (request.Opportunities.Count == 0)
                Add(errors, ActivityCompatibilityErrorCode.MissingInput, "opportunities", "At least one opportunity is required.");
            ValidateDigest(request.ExpectedMap11CatalogDigest, "expected.map11CatalogDigest", errors);
            ValidateDigest(request.ExpectedMap11SignatureSetDigest, "expected.map11SignatureSetDigest", errors);
            ValidateDigest(request.ExpectedAuthoringManifestDigest, "expected.authoringManifestDigest", errors);

            for (var index = 0; index < request.Profiles.Count; index++)
                ValidateProfile(request.Profiles[index], "profiles[" + Number(index) + "]", errors);
            for (var index = 0; index < request.Opportunities.Count; index++)
                ValidateOpportunity(request.Opportunities[index], request, "opportunities[" + Number(index) + "]", errors);
            if (errors.Count != 0) return Failure(errors);

            var candidates = new List<ActivityPlacementCandidate>();
            var rejections = new List<ActivityCompatibilityRejection>();
            foreach (var opportunity in request.Opportunities.OrderBy(value => value.OpportunityId, StringComparer.Ordinal))
            {
                foreach (var profile in request.Profiles.OrderBy(value => value.ActivityId.Value, StringComparer.Ordinal))
                {
                    var pairRejections = Evaluate(opportunity, profile).ToArray();
                    if (pairRejections.Length == 0)
                    {
                        candidates.Add(new ActivityPlacementCandidate(
                            opportunity, profile, CandidateKey(opportunity, profile)));
                    }
                    else
                    {
                        rejections.AddRange(pairRejections);
                    }
                }
            }

            var duplicates = candidates.GroupBy(value => value.CandidateKey, StringComparer.Ordinal)
                .Where(group => group.Count() > 1).OrderBy(group => group.Key, StringComparer.Ordinal).ToArray();
            if (duplicates.Length != 0)
            {
                var duplicateKeys = new HashSet<string>(duplicates.Select(value => value.Key), StringComparer.Ordinal);
                foreach (var group in duplicates)
                {
                    foreach (var candidate in group)
                    {
                        rejections.Add(new ActivityCompatibilityRejection(
                            candidate.OpportunityId, candidate.ActivityId,
                            ActivityCompatibilityRejectionCode.DuplicateCandidate,
                            "candidate[" + group.Key + "]", "All duplicate sources were excluded."));
                    }
                }
                candidates.RemoveAll(value => duplicateKeys.Contains(value.CandidateKey));
                Add(errors, ActivityCompatibilityErrorCode.DuplicateCandidate, "candidates",
                    Number(duplicates.Length) + " duplicate candidate key group(s) were excluded.");
            }
            if (candidates.Count == 0)
                Add(errors, ActivityCompatibilityErrorCode.EmptyCandidateIndex, "candidates", "No compatible candidate can be published.");
            if (errors.Count != 0) return Failure(errors);

            var digest = ComputeDigest(request, candidates, rejections);
            return new ActivityCandidateIndexCompileResult(
                new ActivityCandidateIndex(candidates, rejections, digest), Array.Empty<ActivityCompatibilityError>());
        }

        private static IEnumerable<ActivityCompatibilityRejection> Evaluate(
            ActivityPlacementOpportunity opportunity,
            ActivityPlacementProfile profile)
        {
            var result = new List<ActivityCompatibilityRejection>();
            void Reject(ActivityCompatibilityRejectionCode code, string path, string detail)
            {
                result.Add(new ActivityCompatibilityRejection(
                    opportunity.OpportunityId, profile.ActivityId, code, path, detail));
            }

            if (opportunity.TerrainClusterId != profile.TerrainClusterId)
                Reject(ActivityCompatibilityRejectionCode.TerrainClusterMismatch, "terrainClusterId", opportunity.TerrainClusterId.Value);
            if (opportunity.SpineVariantId != profile.SpineVariantId)
                Reject(ActivityCompatibilityRejectionCode.SpineVariantMismatch, "spineVariantId", opportunity.SpineVariantId.Value);
            if (!profile.AllowedBiomes.Contains(opportunity.PrimaryBiome))
                Reject(ActivityCompatibilityRejectionCode.BiomeMismatch, "primaryBiome", opportunity.PrimaryBiome.CanonicalId);
            if (!profile.AllowedPacingRoles.Contains(opportunity.PacingRole))
                Reject(ActivityCompatibilityRejectionCode.PacingRoleMismatch, "pacingRole", Number((int)opportunity.PacingRole));
            if (!profile.AllowedAccessClasses.Contains(opportunity.AccessClass))
                Reject(ActivityCompatibilityRejectionCode.AccessClassMismatch, "accessClass", Number((int)opportunity.AccessClass));
            if (opportunity.ActiveChunkCount < profile.MinimumActiveChunkCount ||
                opportunity.ActiveChunkCount > profile.MaximumActiveChunkCount)
                Reject(ActivityCompatibilityRejectionCode.ActiveChunkCountMismatch, "activeChunkCount", Number(opportunity.ActiveChunkCount));
            if (!string.Equals(opportunity.ActivityShellDigest, profile.ShellDigest, StringComparison.Ordinal))
                Reject(ActivityCompatibilityRejectionCode.ActivityShellDigestMismatch, "activityShellDigest", opportunity.ActivityShellDigest);
            if (!string.Equals(opportunity.RemovalSafetyDigest, profile.RemovalSafetyDigest, StringComparison.Ordinal))
                Reject(ActivityCompatibilityRejectionCode.RemovalSafetyDigestMismatch, "removalSafetyDigest", opportunity.RemovalSafetyDigest);
            ValidateClearance(opportunity.Clearance, profile, Reject);
            return result;
        }

        private static void ValidateClearance(
            ActivityPlacementClearanceEvidence clearance,
            ActivityPlacementProfile profile,
            Action<ActivityCompatibilityRejectionCode, string, string> reject)
        {
            if (clearance.Width < profile.RequiredOpenClearanceWidth ||
                clearance.Height < profile.RequiredOpenClearanceHeight)
                reject(ActivityCompatibilityRejectionCode.ClearanceTooSmall, "clearance.size",
                    Number(clearance.Width) + "x" + Number(clearance.Height));

            var coordinates = new HashSet<LocalTileCoord>(clearance.Coordinates);
            var rectangular = clearance.Width > 0 && clearance.Height > 0 &&
                              clearance.Coordinates.Count == clearance.Width * clearance.Height &&
                              coordinates.Count == clearance.Coordinates.Count;
            if (rectangular)
            {
                for (var y = clearance.Origin.Y; y < clearance.Origin.Y + clearance.Height && rectangular; y++)
                    for (var x = clearance.Origin.X; x < clearance.Origin.X + clearance.Width; x++)
                        if (!coordinates.Contains(new LocalTileCoord(x, y))) { rectangular = false; break; }
            }
            if (!rectangular)
                reject(ActivityCompatibilityRejectionCode.ClearanceNotRectangular, "clearance.coordinates", "Rectangle coordinates must be exact and unique.");

            var air = new HashSet<LocalTileCoord>(clearance.FinalWorkingCanvasAirCoordinates);
            if (air.Count != clearance.FinalWorkingCanvasAirCoordinates.Count || coordinates.Any(value => !air.Contains(value)))
                reject(ActivityCompatibilityRejectionCode.ClearanceNotAir, "clearance.air", "Every clearance coordinate must be unique final working Canvas Air.");
            var reserved = new HashSet<LocalTileCoord>(clearance.DeviceHazardProjectileReservedCoordinates);
            if (coordinates.Any(reserved.Contains))
                reject(ActivityCompatibilityRejectionCode.ClearanceReserved, "clearance.reserved", "Clearance overlaps a Device/Hazard/Projectile reservation.");
            var protectedSet = new HashSet<LocalTileCoord>(clearance.AbsoluteProtectedCoordinates);
            if (coordinates.Any(protectedSet.Contains))
                reject(ActivityCompatibilityRejectionCode.ClearanceAbsoluteProtected, "clearance.absoluteProtected", "Clearance overlaps AbsoluteProtected.");
        }

        private static void ValidateProfile(
            ActivityPlacementProfile profile,
            string path,
            ICollection<ActivityCompatibilityError> errors)
        {
            if (profile == null)
            {
                Add(errors, ActivityCompatibilityErrorCode.InvalidProfile, path, "Profile cannot be null.");
                return;
            }
            if (!ActivityCompatibilityCanonical.IsCanonicalId(profile.ActivityId.Value) ||
                !ActivityCompatibilityCanonical.IsCanonicalId(profile.TerrainClusterId.Value) ||
                !ActivityCompatibilityCanonical.IsCanonicalId(profile.SpineVariantId.Value))
                Add(errors, ActivityCompatibilityErrorCode.InvalidProfile, path + ".identity", "Canonical Activity/cluster/variant IDs are required.");
            ValidateDigest(profile.ActivityDigest, path + ".activityDigest", errors, ActivityCompatibilityErrorCode.InvalidProfile);
            ValidateDigest(profile.ShellDigest, path + ".shellDigest", errors, ActivityCompatibilityErrorCode.InvalidProfile);
            ValidateDigest(profile.RemovalSafetyDigest, path + ".removalSafetyDigest", errors, ActivityCompatibilityErrorCode.InvalidProfile);
            if (profile.AllowedBiomes.Count == 0 || profile.AllowedBiomes.Any(value => !value.IsDefined) ||
                profile.AllowedPacingRoles.Count == 0 || profile.AllowedPacingRoles.Any(value => value == PacingRole.None) ||
                profile.AllowedAccessClasses.Count == 0 || profile.AllowedAccessClasses.Any(value => value == AccessClass.Unspecified) ||
                profile.MinimumActiveChunkCount < 0 || profile.MaximumActiveChunkCount < profile.MinimumActiveChunkCount ||
                profile.RequiredOpenClearanceWidth <= 0 || profile.RequiredOpenClearanceHeight <= 0 ||
                profile.Weight < 1 || profile.Weight > 10000 || !Enum.IsDefined(typeof(ActivityStrengthClass), profile.Strength))
                Add(errors, ActivityCompatibilityErrorCode.InvalidProfile, path + ".contract", "Profile membership, ranges, clearance, weight, or strength is invalid.");
        }

        private static void ValidateOpportunity(
            ActivityPlacementOpportunity opportunity,
            ActivityCandidateIndexCompileRequest request,
            string path,
            ICollection<ActivityCompatibilityError> errors)
        {
            if (opportunity == null)
            {
                Add(errors, ActivityCompatibilityErrorCode.InvalidOpportunity, path, "Opportunity cannot be null.");
                return;
            }
            if (!ActivityCompatibilityCanonical.IsCanonicalId(opportunity.OpportunityId) ||
                !opportunity.PatchId.IsValid || !opportunity.PrimaryBiome.IsDefined ||
                !ActivityCompatibilityCanonical.IsCanonicalId(opportunity.TerrainClusterId.Value) ||
                !ActivityCompatibilityCanonical.IsCanonicalId(opportunity.SpineVariantId.Value) ||
                opportunity.PacingRole == PacingRole.None || opportunity.AccessClass == AccessClass.Unspecified ||
                opportunity.ActiveChunkCount < 0 || opportunity.Clearance == null)
            {
                Add(errors, ActivityCompatibilityErrorCode.InvalidOpportunity, path + ".contract", "Opportunity identity or physical evidence is invalid.");
                return;
            }
            if (!string.Equals(opportunity.Map11CatalogDigest, request.ExpectedMap11CatalogDigest, StringComparison.Ordinal) ||
                !string.Equals(opportunity.Map11SignatureSetDigest, request.ExpectedMap11SignatureSetDigest, StringComparison.Ordinal) ||
                !string.Equals(opportunity.AuthoringManifestDigest, request.ExpectedAuthoringManifestDigest, StringComparison.Ordinal))
                Add(errors, ActivityCompatibilityErrorCode.ArtifactDigestMismatch, path + ".artifactDigests", "MAP11 artifact digest identity mismatch.");

            if (request.PatchOwnership != null)
            {
                if (!request.PatchOwnership.TryGetSector(opportunity.Sector, out var ownership) ||
                    !ownership.IsAssigned || !ownership.PatchId.HasValue || ownership.PatchId.Value != opportunity.PatchId ||
                    !string.Equals(ownership.PrimaryBiomeId, BiomeToken(opportunity.PrimaryBiome), StringComparison.Ordinal))
                    Add(errors, ActivityCompatibilityErrorCode.PatchOwnershipMismatch, path + ".patchOwnership", "Sector, patch, and primary biome must match BiomePatchSnapshot ownership.");
            }
        }

        private static string BiomeToken(MoonpalaceBiomeId biome)
        {
            if (biome == MoonpalaceBiomeId.MoonCrater) return "BIO_MOON_CRATER";
            if (biome == MoonpalaceBiomeId.CassiaRoot) return "BIO_CASSIA_ROOT";
            if (biome == MoonpalaceBiomeId.AbandonedMill) return "BIO_ABANDONED_MILL";
            if (biome == MoonpalaceBiomeId.MoonDough) return "BIO_MOON_DOUGH";
            return string.Empty;
        }

        private static string CandidateKey(ActivityPlacementOpportunity opportunity, ActivityPlacementProfile profile)
        {
            return ActivityCompatibilityCanonical.Sha256(
                opportunity.OpportunityId + "\n" + profile.ActivityId.Value + "\n" +
                profile.ActivityDigest + "\n" + profile.ShellDigest + "\n" + profile.RemovalSafetyDigest);
        }

        private static string ComputeDigest(
            ActivityCandidateIndexCompileRequest request,
            IEnumerable<ActivityPlacementCandidate> candidates,
            IEnumerable<ActivityCompatibilityRejection> rejections)
        {
            var material = new StringBuilder();
            ActivityCompatibilityCanonical.Append(material, "RULESET", RulesetVersion);
            ActivityCompatibilityCanonical.Append(material, "ARTIFACTS", request.ExpectedMap11CatalogDigest,
                request.ExpectedMap11SignatureSetDigest, request.ExpectedAuthoringManifestDigest);
            foreach (var profile in request.Profiles.OrderBy(value => value.ActivityId.Value, StringComparer.Ordinal))
            {
                ActivityCompatibilityCanonical.Append(material, "PROFILE", profile.ActivityId.Value,
                    profile.TerrainClusterId.Value, profile.SpineVariantId.Value, profile.ActivityDigest,
                    profile.ShellDigest, profile.RemovalSafetyDigest,
                    string.Join(",", profile.AllowedBiomes.Select(value => value.CanonicalId)),
                    string.Join(",", profile.AllowedPacingRoles.Select(value => Number((int)value))),
                    string.Join(",", profile.AllowedAccessClasses.Select(value => Number((int)value))),
                    Number(profile.MinimumActiveChunkCount), Number(profile.MaximumActiveChunkCount),
                    Number(profile.RequiredOpenClearanceWidth), Number(profile.RequiredOpenClearanceHeight),
                    Number(profile.Weight), Number((int)profile.Strength));
            }
            foreach (var opportunity in request.Opportunities.OrderBy(value => value.OpportunityId, StringComparer.Ordinal))
            {
                ActivityCompatibilityCanonical.Append(material, "OPPORTUNITY", opportunity.OpportunityId,
                    Number(opportunity.Sector.X), Number(opportunity.Sector.Y), opportunity.PatchId.Value,
                    opportunity.PrimaryBiome.CanonicalId, opportunity.TerrainClusterId.Value,
                    opportunity.SpineVariantId.Value, Number((int)opportunity.PacingRole),
                    Number((int)opportunity.AccessClass), Number(opportunity.ActiveChunkCount),
                    Number(opportunity.Clearance.Origin.X), Number(opportunity.Clearance.Origin.Y),
                    Number(opportunity.Clearance.Width), Number(opportunity.Clearance.Height),
                    string.Join(",", opportunity.Clearance.Coordinates.Select(ActivityCompatibilityCanonical.Coordinate)),
                    opportunity.Map11CatalogDigest, opportunity.Map11SignatureSetDigest,
                    opportunity.AuthoringManifestDigest, opportunity.ActivityShellDigest,
                    opportunity.RemovalSafetyDigest);
            }
            foreach (var candidate in candidates.OrderBy(value => value.CandidateKey, StringComparer.Ordinal))
                ActivityCompatibilityCanonical.Append(material, "CANDIDATE", candidate.CandidateKey,
                    candidate.OpportunityId, candidate.ActivityId.Value, Number(candidate.Weight), Number((int)candidate.Strength));
            foreach (var rejection in rejections.OrderBy(value => value.OpportunityId, StringComparer.Ordinal)
                         .ThenBy(value => value.ActivityId.Value, StringComparer.Ordinal).ThenBy(value => value.Code)
                         .ThenBy(value => value.Path, StringComparer.Ordinal).ThenBy(value => value.Detail, StringComparer.Ordinal))
                ActivityCompatibilityCanonical.Append(material, "REJECTION", rejection.OpportunityId,
                    rejection.ActivityId.Value, Number((int)rejection.Code), rejection.Path, rejection.Detail);
            return ActivityCompatibilityCanonical.Sha256(material.ToString());
        }

        private static void ValidateDigest(
            string value,
            string path,
            ICollection<ActivityCompatibilityError> errors,
            ActivityCompatibilityErrorCode code = ActivityCompatibilityErrorCode.ArtifactDigestMismatch)
        {
            if (!ActivityCompatibilityCanonical.IsSha256(value))
                Add(errors, code, path, "A lowercase 64-character SHA-256 digest is required.");
        }

        private static ActivityCandidateIndexCompileResult Failure(IEnumerable<ActivityCompatibilityError> errors)
        {
            return new ActivityCandidateIndexCompileResult(null, errors);
        }

        private static void Add(
            ICollection<ActivityCompatibilityError> errors,
            ActivityCompatibilityErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new ActivityCompatibilityError(code, path, detail));
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    internal static class ActivityCompatibilityCanonical
    {
        public static IEnumerable<ActivityCompatibilityError> SortErrors(IEnumerable<ActivityCompatibilityError> errors)
        {
            return (errors ?? Array.Empty<ActivityCompatibilityError>())
                .Where(value => value != null)
                .GroupBy(value => ((int)value.Code).ToString(CultureInfo.InvariantCulture) + "|" + value.Path + "|" + value.Detail,
                    StringComparer.Ordinal).Select(value => value.First())
                .OrderBy(value => value.Code).ThenBy(value => value.Path, StringComparer.Ordinal)
                .ThenBy(value => value.Detail, StringComparer.Ordinal);
        }

        public static bool IsCanonicalId(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (!((character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9') || character == '_'))
                    return false;
            }
            return true;
        }

        public static bool IsSha256(string value)
        {
            if (value == null || value.Length != 64) return false;
            for (var index = 0; index < value.Length; index++)
                if (!((value[index] >= '0' && value[index] <= '9') || (value[index] >= 'a' && value[index] <= 'f')))
                    return false;
            return true;
        }

        public static string Coordinate(LocalTileCoord value)
        {
            return value.X.ToString(CultureInfo.InvariantCulture) + "," + value.Y.ToString(CultureInfo.InvariantCulture);
        }

        public static string Sha256(string material)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(material ?? string.Empty))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        public static void Append(StringBuilder target, params string[] fields)
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
    }
}
