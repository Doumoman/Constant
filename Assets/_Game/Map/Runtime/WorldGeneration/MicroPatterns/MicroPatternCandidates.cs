using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public sealed class MicroPatternCandidateSource
    {
        public MicroPatternCandidateSource(
            MicroPatternDefinition definition,
            MicroPatternTransform transform,
            MicroPatternApplicationPlan applicationPlan)
        {
            Definition = definition;
            Transform = transform;
            ApplicationPlan = applicationPlan;
        }

        public MicroPatternDefinition Definition { get; }
        public MicroPatternTransform Transform { get; }
        public MicroPatternApplicationPlan ApplicationPlan { get; }
    }

    public readonly struct MicroPatternCandidateKey :
        IEquatable<MicroPatternCandidateKey>,
        IComparable<MicroPatternCandidateKey>
    {
        public MicroPatternCandidateKey(
            MicroPatternId patternId,
            MicroPatternTransform transform,
            string applicationPlanDigest)
        {
            PatternId = patternId;
            Transform = transform;
            ApplicationPlanDigest = applicationPlanDigest ?? string.Empty;
        }

        public MicroPatternId PatternId { get; }
        public MicroPatternTransform Transform { get; }
        public string ApplicationPlanDigest { get; }

        public string CanonicalValue =>
            PatternId.Value + ":" + Transform + ":" + ApplicationPlanDigest;

        public int CompareTo(MicroPatternCandidateKey other)
        {
            var comparison = PatternId.CompareTo(other.PatternId);
            if (comparison != 0) return comparison;
            comparison = string.Compare(
                Transform.ToString(),
                other.Transform.ToString(),
                StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(
                    ApplicationPlanDigest,
                    other.ApplicationPlanDigest,
                    StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternCandidateKey other)
        {
            return CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return obj is MicroPatternCandidateKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = PatternId.GetHashCode();
                hash = (hash * 397) ^ (int)Transform;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ApplicationPlanDigest);
                return hash;
            }
        }

        public override string ToString() => CanonicalValue;
    }

    public sealed class MicroPatternCandidate
    {
        internal MicroPatternCandidate(
            MicroPatternCandidateKey key,
            MoonpalaceBiomeId biome,
            MicroPatternDefinition definition,
            MicroPatternApplicationPlan applicationPlan,
            MicroPatternFeatureSummary featureSummary,
            string sourcePatternDigest)
        {
            Key = key;
            Biome = biome;
            Definition = definition;
            ApplicationPlan = applicationPlan;
            FeatureSummary = featureSummary;
            SourcePatternDigest = sourcePatternDigest;
            Weight = definition.Weight;
        }

        public MicroPatternCandidateKey Key { get; }
        public MoonpalaceBiomeId Biome { get; }
        public MicroPatternDefinition Definition { get; }
        public MicroPatternApplicationPlan ApplicationPlan { get; }
        public MicroPatternFeatureSummary FeatureSummary { get; }
        public string SourcePatternDigest { get; }
        public int Weight { get; }
    }

    public enum MicroPatternCandidateRejectionCode
    {
        MissingInput = 1,
        InvalidBiomeProfile = 2,
        InvalidDefinition = 3,
        BiomeNotAllowed = 4,
        TransformNotAllowed = 5,
        MissingApplicationPlan = 6,
        InvalidApplicationPlanDigest = 7,
        PatternMismatch = 8,
        SourceDigestMismatch = 9,
        TransformMismatch = 10,
        FeatureSummaryFailed = 11,
        UnsupportedSilhouette = 12,
        DuplicateCandidateKey = 13,
    }

    public sealed class MicroPatternCandidateRejection :
        IEquatable<MicroPatternCandidateRejection>,
        IComparable<MicroPatternCandidateRejection>
    {
        public MicroPatternCandidateRejection(
            MicroPatternCandidateRejectionCode code,
            string sourceIdentity,
            string detail)
        {
            Code = code;
            SourceIdentity = sourceIdentity ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternCandidateRejectionCode Code { get; }
        public string SourceIdentity { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternCandidateRejection other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternCandidateRejection other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj) => Equals(obj as MicroPatternCandidateRejection);
        public override int GetHashCode() => ToString().GetHashCode();
        public override string ToString() => Code + "|" + SourceIdentity + "|" + Detail;
    }

    public sealed class MicroPatternCandidateIndex
    {
        private readonly ReadOnlyCollection<MicroPatternCandidate> candidates;

        internal MicroPatternCandidateIndex(
            MoonpalaceBiomeId biome,
            MicroPatternBiomeProfile profile,
            string profileCatalogDigest,
            IEnumerable<MicroPatternCandidate> candidates,
            long totalWeight,
            string stableDigest)
        {
            Biome = biome;
            Profile = profile;
            ProfileCatalogDigest = profileCatalogDigest;
            var copy = candidates.OrderBy(value => value.Key).ToArray();
            this.candidates = new ReadOnlyCollection<MicroPatternCandidate>(copy);
            TotalWeight = totalWeight;
            StableDigest = stableDigest;
        }

        public MoonpalaceBiomeId Biome { get; }
        public MicroPatternBiomeProfile Profile { get; }
        public string ProfileCatalogDigest { get; }
        public IReadOnlyList<MicroPatternCandidate> Candidates => candidates;
        public long TotalWeight { get; }
        public string StableDigest { get; }
    }

    public sealed class MicroPatternCandidateIndexBuildResult
    {
        private readonly ReadOnlyCollection<MicroPatternCandidateRejection> rejections;

        internal MicroPatternCandidateIndexBuildResult(
            MicroPatternCandidateIndex index,
            IEnumerable<MicroPatternCandidateRejection> rejections)
        {
            var copy = (rejections ?? Array.Empty<MicroPatternCandidateRejection>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.rejections = new ReadOnlyCollection<MicroPatternCandidateRejection>(copy);
            Index = index;
        }

        public bool Published => Index != null;
        public MicroPatternCandidateIndex Index { get; }
        public IReadOnlyList<MicroPatternCandidateRejection> Rejections => rejections;
    }

    public static class MicroPatternCandidateIndexBuilder
    {
        public static MicroPatternCandidateIndexBuildResult Build(
            MicroPatternBiomeProfileCatalog profileCatalog,
            MoonpalaceBiomeId requestedBiome,
            IEnumerable<MicroPatternCandidateSource> sources)
        {
            var rejections = new List<MicroPatternCandidateRejection>();
            if (profileCatalog == null ||
                !requestedBiome.IsDefined ||
                !profileCatalog.TryGetProfile(requestedBiome, out var profile))
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.InvalidBiomeProfile,
                    requestedBiome.IsDefined ? requestedBiome.CanonicalId : "UNKNOWN",
                    "A validated exact-four biome profile is required."));
                return new MicroPatternCandidateIndexBuildResult(null, rejections);
            }

            if (sources == null)
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.MissingInput,
                    requestedBiome.CanonicalId,
                    "Candidate source input is required."));
                return new MicroPatternCandidateIndexBuildResult(null, rejections);
            }

            var pending = new List<PendingCandidate>();
            foreach (var source in sources)
            {
                ValidateSource(source, requestedBiome, profile, pending, rejections);
            }

            var accepted = new List<MicroPatternCandidate>();
            foreach (var group in pending.GroupBy(value => value.Candidate.Key))
            {
                var values = group.OrderBy(value => value.SourceIdentity, StringComparer.Ordinal).ToArray();
                if (values.Length > 1)
                {
                    foreach (var value in values)
                    {
                        rejections.Add(Reject(
                            MicroPatternCandidateRejectionCode.DuplicateCandidateKey,
                            value.SourceIdentity,
                            group.Key.CanonicalValue));
                    }
                    continue;
                }

                accepted.Add(values[0].Candidate);
            }

            accepted.Sort((left, right) => left.Key.CompareTo(right.Key));
            long totalWeight = 0;
            foreach (var candidate in accepted)
            {
                totalWeight = checked(totalWeight + candidate.Weight);
            }

            var digest = ComputeIndexDigest(
                profileCatalog.StableDigest,
                profile,
                requestedBiome,
                accepted);
            var index = new MicroPatternCandidateIndex(
                requestedBiome,
                profile,
                profileCatalog.StableDigest,
                accepted,
                totalWeight,
                digest);
            return new MicroPatternCandidateIndexBuildResult(index, rejections);
        }

        private static void ValidateSource(
            MicroPatternCandidateSource source,
            MoonpalaceBiomeId requestedBiome,
            MicroPatternBiomeProfile profile,
            ICollection<PendingCandidate> pending,
            ICollection<MicroPatternCandidateRejection> rejections)
        {
            if (source == null)
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.MissingInput,
                    "<null>",
                    "Candidate source is required."));
                return;
            }

            var identity = SourceIdentity(source);
            if (source.Definition == null)
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.InvalidDefinition,
                    identity,
                    "Definition is required."));
                return;
            }

            var validation = MicroPatternValidator.Validate(source.Definition);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    rejections.Add(Reject(
                        MicroPatternCandidateRejectionCode.InvalidDefinition,
                        identity,
                        error.ToString()));
                }
                return;
            }

            var sourceDigest = validation.StableDigest;
            var sourceValid = true;
            if (!source.Definition.AllowedBiomes.Contains(requestedBiome))
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.BiomeNotAllowed,
                    identity,
                    requestedBiome.CanonicalId));
                sourceValid = false;
            }

            if (!source.Definition.AllowedTransforms.Contains(source.Transform))
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.TransformNotAllowed,
                    identity,
                    source.Transform.ToString()));
                sourceValid = false;
            }

            var plan = source.ApplicationPlan;
            if (plan == null)
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.MissingApplicationPlan,
                    identity,
                    "A successful MAP10_02 plan is required."));
                return;
            }

            if (!MicroPatternContractDigest.IsLowerHexDigest(plan.StableDigest))
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.InvalidApplicationPlanDigest,
                    identity,
                    plan.StableDigest));
                sourceValid = false;
            }

            if (plan.SourcePatternId != source.Definition.Id)
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.PatternMismatch,
                    identity,
                    plan.SourcePatternId.Value));
                sourceValid = false;
            }

            if (!string.Equals(plan.SourceDigest, sourceDigest, StringComparison.Ordinal))
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.SourceDigestMismatch,
                    identity,
                    plan.SourceDigest));
                sourceValid = false;
            }

            if (plan.Transform != source.Transform)
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.TransformMismatch,
                    identity,
                    plan.Transform + "!=" + source.Transform));
                sourceValid = false;
            }

            var featureResult = MicroPatternFeatureSummary.Create(
                source.Definition,
                source.Transform,
                plan);
            if (!featureResult.Success)
            {
                foreach (var error in featureResult.Errors)
                {
                    rejections.Add(Reject(
                        MicroPatternCandidateRejectionCode.FeatureSummaryFailed,
                        identity,
                        error.ToString()));
                }
                sourceValid = false;
            }
            else if (!profile.SilhouetteClasses.Contains(featureResult.Summary.SilhouetteClass))
            {
                rejections.Add(Reject(
                    MicroPatternCandidateRejectionCode.UnsupportedSilhouette,
                    identity,
                    featureResult.Summary.SilhouetteClass.ToString()));
                sourceValid = false;
            }

            if (!sourceValid) return;

            var key = new MicroPatternCandidateKey(
                source.Definition.Id,
                source.Transform,
                plan.StableDigest);
            var candidate = new MicroPatternCandidate(
                key,
                requestedBiome,
                source.Definition,
                plan,
                featureResult.Summary,
                sourceDigest);
            pending.Add(new PendingCandidate(candidate, identity));
        }

        private static string ComputeIndexDigest(
            string profileCatalogDigest,
            MicroPatternBiomeProfile profile,
            MoonpalaceBiomeId biome,
            IEnumerable<MicroPatternCandidate> candidates)
        {
            var material = new StringBuilder();
            MicroPatternContractDigest.Append(material, "RULESET", "MAP10_04_INDEX_V1");
            MicroPatternContractDigest.Append(
                material,
                "PROFILE",
                profileCatalogDigest,
                biome.CanonicalId,
                profile.DensityPolicy.ToString(),
                profile.SafetyMeaning);
            foreach (var motif in profile.MotifMetadata)
            {
                MicroPatternContractDigest.Append(material, "MOTIF", motif);
            }
            foreach (var candidate in candidates.OrderBy(value => value.Key))
            {
                MicroPatternContractDigest.Append(
                    material,
                    "CANDIDATE",
                    candidate.Key.PatternId.Value,
                    candidate.Key.Transform.ToString(),
                    candidate.Key.ApplicationPlanDigest,
                    candidate.SourcePatternDigest,
                    candidate.FeatureSummary.StableDigest,
                    candidate.Weight.ToString(CultureInfo.InvariantCulture));
            }
            return MicroPatternContractDigest.Hash(material);
        }

        private static string SourceIdentity(MicroPatternCandidateSource source)
        {
            var id = source.Definition == null ? "<missing>" : source.Definition.Id.Value;
            var planDigest = source.ApplicationPlan == null
                ? "<missing>"
                : source.ApplicationPlan.StableDigest;
            return id + ":" + source.Transform + ":" + planDigest;
        }

        private static MicroPatternCandidateRejection Reject(
            MicroPatternCandidateRejectionCode code,
            string identity,
            string detail)
        {
            return new MicroPatternCandidateRejection(code, identity, detail);
        }

        private sealed class PendingCandidate
        {
            public PendingCandidate(MicroPatternCandidate candidate, string sourceIdentity)
            {
                Candidate = candidate;
                SourceIdentity = sourceIdentity;
            }

            public MicroPatternCandidate Candidate { get; }
            public string SourceIdentity { get; }
        }
    }
}
