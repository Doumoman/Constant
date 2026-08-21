using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SiteDistanceRuleKind
    {
        StartToRequiredSite,
        RequiredSiteToRequiredSite
    }

    public sealed class SiteDistanceConstraint
    {
        public SiteDistanceConstraint(
            SiteDistanceRuleKind ruleKind,
            SitePlacementKey first,
            SitePlacementKey second,
            int minimumDistance)
        {
            if (ruleKind != SiteDistanceRuleKind.StartToRequiredSite &&
                ruleKind != SiteDistanceRuleKind.RequiredSiteToRequiredSite)
                throw new ArgumentOutOfRangeException(nameof(ruleKind));
            if (!first.IsValid) throw new ArgumentException("A valid first key is required.", nameof(first));
            if (!second.IsValid) throw new ArgumentException("A valid second key is required.", nameof(second));
            if (first.CompareTo(second) >= 0)
                throw new ArgumentException("Constraint keys must be in canonical order.", nameof(second));
            if (minimumDistance < 1 || minimumDistance > 24)
                throw new ArgumentOutOfRangeException(nameof(minimumDistance));
            if (ruleKind == SiteDistanceRuleKind.StartToRequiredSite &&
                (first.Kind != SiteReservationKind.Start || second.Kind == SiteReservationKind.Start))
                throw new ArgumentException("A start rule must connect Start to a required site.", nameof(ruleKind));
            if (ruleKind == SiteDistanceRuleKind.RequiredSiteToRequiredSite &&
                (first.Kind == SiteReservationKind.Start || second.Kind == SiteReservationKind.Start))
                throw new ArgumentException("A required-site rule cannot contain Start.", nameof(ruleKind));

            RuleKind = ruleKind;
            First = first;
            Second = second;
            MinimumDistance = minimumDistance;
        }

        public SiteDistanceRuleKind RuleKind { get; }
        public SitePlacementKey First { get; }
        public SitePlacementKey Second { get; }
        public int MinimumDistance { get; }
    }

    public sealed class SiteDistancePolicy
    {
        private readonly IReadOnlyList<SitePlacementKey> keys;
        private readonly IReadOnlyList<SiteDistanceConstraint> constraints;
        private readonly IReadOnlyDictionary<SitePlacementPairKey, SiteDistanceConstraint> byPair;

        internal SiteDistancePolicy(
            IEnumerable<SitePlacementKey> keys,
            IEnumerable<SiteDistanceConstraint> constraints)
        {
            var keySnapshot = new List<SitePlacementKey>(keys ?? throw new ArgumentNullException(nameof(keys)));
            keySnapshot.Sort();
            var constraintSnapshot = new List<SiteDistanceConstraint>(
                constraints ?? throw new ArgumentNullException(nameof(constraints)));
            constraintSnapshot.Sort(CompareConstraints);
            var lookup = new Dictionary<SitePlacementPairKey, SiteDistanceConstraint>();
            foreach (var constraint in constraintSnapshot)
            {
                if (constraint == null)
                    throw new ArgumentException("Constraints cannot contain null.", nameof(constraints));
                lookup.Add(new SitePlacementPairKey(constraint.First, constraint.Second), constraint);
            }
            this.keys = new ReadOnlyCollection<SitePlacementKey>(keySnapshot);
            this.constraints = new ReadOnlyCollection<SiteDistanceConstraint>(constraintSnapshot);
            byPair = new ReadOnlyDictionary<SitePlacementPairKey, SiteDistanceConstraint>(lookup);
        }

        public IReadOnlyList<SitePlacementKey> Keys => keys;
        public IReadOnlyList<SiteDistanceConstraint> Constraints => constraints;
        public int ConstraintCount => constraints.Count;

        public bool TryGetConstraint(
            SitePlacementKey first,
            SitePlacementKey second,
            out SiteDistanceConstraint constraint)
        {
            if (!first.IsValid || !second.IsValid || first == second)
            {
                constraint = null;
                return false;
            }
            return byPair.TryGetValue(new SitePlacementPairKey(first, second), out constraint);
        }

        private static int CompareConstraints(SiteDistanceConstraint left, SiteDistanceConstraint right)
        {
            var first = left.First.CompareTo(right.First);
            return first != 0 ? first : left.Second.CompareTo(right.Second);
        }
    }

    public sealed class SiteDistancePolicyBuilder
    {
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";

        private static readonly RequiredSite[] RequiredSites =
        {
            new RequiredSite(BossId, SiteReservationKind.Boss),
            new RequiredSite(ForgeId, SiteReservationKind.Forge),
            new RequiredSite(CassiaId, SiteReservationKind.CoreResource),
            new RequiredSite(YeastId, SiteReservationKind.CoreResource),
            new RequiredSite(MeteorId, SiteReservationKind.CoreResource)
        };

        public SiteDistancePolicyResult BuildRequiredSitePolicy(
            string startSourceDefinitionId,
            IEnumerable<SpecialMapDefinition> specialMaps)
        {
            var errors = new List<SiteDistanceError>();
            if (string.IsNullOrEmpty(startSourceDefinitionId))
            {
                Add(errors, SiteDistanceErrorCode.MissingStartSourceId, string.Empty, string.Empty,
                    "A Start source definition ID is required.");
            }
            else if (!SitePlacementKey.IsCanonicalId(startSourceDefinitionId))
            {
                Add(errors, SiteDistanceErrorCode.InvalidStartSourceId, string.Empty, string.Empty,
                    "The Start source definition ID must be canonical.");
            }

            if (specialMaps == null)
            {
                Add(errors, SiteDistanceErrorCode.MissingSpecialMapInput, string.Empty, string.Empty,
                    "Special-map definitions are required.");
                return SiteDistancePolicyResult.Failure(errors);
            }

            var definitions = new List<SpecialMapDefinition>();
            foreach (var definition in specialMaps) definitions.Add(definition);
            definitions.Sort((left, right) => string.Compare(
                Source(left), Source(right), StringComparison.Ordinal));

            var byId = new Dictionary<string, SpecialMapDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    Add(errors, SiteDistanceErrorCode.NullSpecialMap, string.Empty, string.Empty,
                        "Special-map definitions cannot contain null.");
                    continue;
                }
                var id = CanonicalOrEmpty(definition.SpecialMapId);
                if (!byId.TryAdd(definition.SpecialMapId ?? string.Empty, definition))
                {
                    Add(errors, SiteDistanceErrorCode.DuplicateSpecialMapId, id, string.Empty,
                        "Special-map IDs must be unique.");
                }
            }

            var accepted = new Dictionary<string, SpecialMapDefinition>(StringComparer.Ordinal);
            foreach (var required in RequiredSites)
            {
                if (!byId.TryGetValue(required.SourceId, out var definition))
                {
                    Add(errors, SiteDistanceErrorCode.MissingRequiredSite, required.SourceId, string.Empty,
                        "A required special site is missing.");
                    continue;
                }
                if (!definition.Active)
                {
                    Add(errors, SiteDistanceErrorCode.InactiveRequiredSite, required.SourceId, string.Empty,
                        "A required special site must be active.");
                }
                if (!SiteReservationTokenCodec.TryParseKind(definition.SiteRole, out var kind) ||
                    kind != required.Kind)
                {
                    Add(errors, SiteDistanceErrorCode.SiteRoleMismatch, required.SourceId, string.Empty,
                        "A required special site has the wrong role.");
                }
                if (definition.RequiredCount != 1)
                {
                    Add(errors, SiteDistanceErrorCode.InvalidRequiredCount, required.SourceId, string.Empty,
                        "A required special site must have required count one.");
                }
                if (!ValidDistance(definition.MinGraphDistanceFromStart) ||
                    !ValidDistance(definition.MinGraphDistanceToOtherCoreSites))
                {
                    Add(errors, SiteDistanceErrorCode.InvalidDistanceRule, required.SourceId, string.Empty,
                        "Required-site graph distances must be between one and twenty-four.");
                }
                accepted[required.SourceId] = definition;
            }

            foreach (var definition in definitions)
            {
                if (definition == null || !definition.Active || definition.RequiredCount <= 0) continue;
                if (!SiteReservationTokenCodec.TryParseKind(definition.SiteRole, out var kind)) continue;
                if (kind == SiteReservationKind.Village) continue;
                if (kind != SiteReservationKind.Boss && kind != SiteReservationKind.Forge &&
                    kind != SiteReservationKind.CoreResource) continue;
                if (!IsRequiredId(definition.SpecialMapId))
                {
                    Add(errors, SiteDistanceErrorCode.UnexpectedRequiredSite,
                        CanonicalOrEmpty(definition.SpecialMapId), string.Empty,
                        "An unexpected active required special site was supplied.");
                }
            }

            if (errors.Count != 0) return SiteDistancePolicyResult.Failure(errors);

            var keys = new List<SitePlacementKey>
            {
                new SitePlacementKey(SiteReservationKind.Start, startSourceDefinitionId, 0)
            };
            foreach (var required in RequiredSites)
                keys.Add(new SitePlacementKey(required.Kind, required.SourceId, 0));
            keys.Sort();

            var constraints = new List<SiteDistanceConstraint>();
            for (var first = 0; first < keys.Count; first++)
            {
                for (var second = first + 1; second < keys.Count; second++)
                {
                    var firstKey = keys[first];
                    var secondKey = keys[second];
                    if (firstKey.Kind == SiteReservationKind.Start)
                    {
                        constraints.Add(new SiteDistanceConstraint(
                            SiteDistanceRuleKind.StartToRequiredSite,
                            firstKey, secondKey,
                            accepted[secondKey.SourceDefinitionId].MinGraphDistanceFromStart));
                    }
                    else
                    {
                        constraints.Add(new SiteDistanceConstraint(
                            SiteDistanceRuleKind.RequiredSiteToRequiredSite,
                            firstKey, secondKey,
                            Math.Max(
                                accepted[firstKey.SourceDefinitionId].MinGraphDistanceToOtherCoreSites,
                                accepted[secondKey.SourceDefinitionId].MinGraphDistanceToOtherCoreSites)));
                    }
                }
            }
            return SiteDistancePolicyResult.Success(new SiteDistancePolicy(keys, constraints));
        }

        private static bool IsRequiredId(string sourceId)
        {
            foreach (var required in RequiredSites)
            {
                if (string.Equals(required.SourceId, sourceId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static bool ValidDistance(int value) => value >= 1 && value <= 24;
        private static string Source(SpecialMapDefinition definition) =>
            definition == null ? string.Empty : CanonicalOrEmpty(definition.SpecialMapId);
        private static string CanonicalOrEmpty(string value) =>
            SitePlacementKey.IsCanonicalId(value) ? value : string.Empty;

        private static void Add(
            ICollection<SiteDistanceError> errors,
            SiteDistanceErrorCode code,
            string first,
            string second,
            string message)
        {
            errors.Add(new SiteDistanceError(code, first, second, -1, message));
        }

        private sealed class RequiredSite
        {
            public RequiredSite(string sourceId, SiteReservationKind kind)
            {
                SourceId = sourceId;
                Kind = kind;
            }
            public string SourceId { get; }
            public SiteReservationKind Kind { get; }
        }
    }
}
