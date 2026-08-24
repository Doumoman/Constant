using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryWarningRequirement
    {
        public const string SoftBlendProfileId = "BOUND_SOFT_BLEND";
        public const string CliffProfileId = "BOUND_CLIFF";
        public const string TunnelProfileId = "BOUND_TUNNEL";
        public const string LayerProfileId = "BOUND_LAYER";
        public const string RuinProfileId = "BOUND_RUIN";
        public const string HardStarstoneProfileId = "BOUND_HARD_STARSTONE";

        private readonly IReadOnlyList<MoonpalaceBoundaryWarningMarkerCategory> allowedMarkerCategories;

        private MoonpalaceBoundaryWarningRequirement(
            MoonpalaceBoundaryProfileId boundaryProfileId,
            MoonpalaceBoundaryOrientation orientation,
            int warningMicrochunksMinimum,
            int requiredDistinctMarkerCategories,
            IEnumerable<MoonpalaceBoundaryWarningMarkerCategory> allowedMarkerCategories)
        {
            var categoryCopy = allowedMarkerCategories.ToArray();
            if (warningMicrochunksMinimum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(warningMicrochunksMinimum));
            }

            if (requiredDistinctMarkerCategories < MoonpalaceBiomePairDefinition.RequiredMinimumWarningMarkerCount ||
                requiredDistinctMarkerCategories > categoryCopy.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredDistinctMarkerCategories));
            }

            if (categoryCopy.Any(category => !category.IsDefined) ||
                categoryCopy.Distinct().Count() != categoryCopy.Length)
            {
                throw new ArgumentException("Allowed warning marker categories must be defined and distinct.",
                    nameof(allowedMarkerCategories));
            }

            BoundaryProfileId = boundaryProfileId;
            Orientation = orientation;
            WarningMicrochunksMinimum = warningMicrochunksMinimum;
            RequiredDistinctMarkerCategories = requiredDistinctMarkerCategories;
            this.allowedMarkerCategories =
                new ReadOnlyCollection<MoonpalaceBoundaryWarningMarkerCategory>(categoryCopy);
        }

        public MoonpalaceBoundaryProfileId BoundaryProfileId { get; }
        public MoonpalaceBoundaryOrientation Orientation { get; }
        public int WarningMicrochunksMinimum { get; }
        public int RequiredDistinctMarkerCategories { get; }
        public IReadOnlyList<MoonpalaceBoundaryWarningMarkerCategory> AllowedMarkerCategories =>
            allowedMarkerCategories;

        public string Signature => string.Join("|", new[]
        {
            BoundaryProfileId.CanonicalId,
            Orientation.ToString(),
            WarningMicrochunksMinimum.ToString(CultureInfo.InvariantCulture),
            RequiredDistinctMarkerCategories.ToString(CultureInfo.InvariantCulture),
            string.Join(",", allowedMarkerCategories.Select(category => category.Token)),
        });

        public static MoonpalaceBoundaryWarningRequirement Create(
            MoonpalaceBoundaryResolveRequest resolveRequest,
            MoonpalaceBoundaryCandidateDefinition candidate)
        {
            if (!TryCreate(resolveRequest, candidate, out var requirement))
            {
                throw new ArgumentException(
                    "The resolve request and candidate do not identify an active Moonpalace pair warning contract.");
            }

            return requirement;
        }

        public static bool TryCreate(
            MoonpalaceBoundaryResolveRequest resolveRequest,
            MoonpalaceBoundaryCandidateDefinition candidate,
            out MoonpalaceBoundaryWarningRequirement requirement)
        {
            requirement = null;
            if (!IsStructurallyValid(resolveRequest, candidate)) return false;

            var pair = new MoonpalaceBiomePair(resolveRequest.FromBiome, resolveRequest.ToBiome);
            if (candidate.Pair != pair ||
                candidate.Profile != resolveRequest.Profile ||
                candidate.Orientation != resolveRequest.Orientation ||
                candidate.RouteRole != resolveRequest.RouteRole ||
                candidate.EdgeSignature != resolveRequest.EdgeSignature)
            {
                return false;
            }

            if (!MoonpalaceBiomePairCatalog.Canonical.TryGetDefinition(pair, out var pairDefinition) ||
                !pairDefinition.Supports(resolveRequest.Orientation) ||
                !IsProfileAllowedForPair(pair, resolveRequest.Profile))
            {
                return false;
            }

            if (!TryGetProfileWarningLength(
                    resolveRequest.Profile,
                    resolveRequest.Orientation,
                    out var warningMicrochunksMinimum))
            {
                return false;
            }

            var allowed = MoonpalaceBoundaryWarningMarkerCategory.CanonicalValues
                .Where(category => (pairDefinition.AvailableWarningMarkers & category.Marker) != 0)
                .ToArray();
            requirement = new MoonpalaceBoundaryWarningRequirement(
                resolveRequest.Profile,
                resolveRequest.Orientation,
                warningMicrochunksMinimum,
                pairDefinition.MinimumDistinctWarningMarkerCount,
                allowed);
            return true;
        }

        public bool IsCompatible(
            MoonpalaceBoundaryResolveRequest resolveRequest,
            MoonpalaceBoundaryCandidateDefinition candidate)
        {
            return TryCreate(resolveRequest, candidate, out var expected) &&
                   string.Equals(Signature, expected.Signature, StringComparison.Ordinal);
        }

        private static bool IsStructurallyValid(
            MoonpalaceBoundaryResolveRequest resolveRequest,
            MoonpalaceBoundaryCandidateDefinition candidate)
        {
            if (resolveRequest == null || candidate == null ||
                !resolveRequest.FromBiome.IsDefined ||
                !resolveRequest.ToBiome.IsDefined ||
                resolveRequest.FromBiome == resolveRequest.ToBiome ||
                !resolveRequest.Profile.IsDefined ||
                !resolveRequest.RouteRole.IsDefined ||
                !resolveRequest.EdgeSignature.IsDefined)
            {
                return false;
            }

            return resolveRequest.Orientation == MoonpalaceBoundaryOrientation.Horizontal ||
                   resolveRequest.Orientation == MoonpalaceBoundaryOrientation.Vertical;
        }

        private static bool TryGetProfileWarningLength(
            MoonpalaceBoundaryProfileId profile,
            MoonpalaceBoundaryOrientation orientation,
            out int warningMicrochunksMinimum)
        {
            warningMicrochunksMinimum = 0;
            var profileId = profile.CanonicalId;
            if (string.Equals(profileId, LayerProfileId, StringComparison.Ordinal))
            {
                if (orientation != MoonpalaceBoundaryOrientation.Vertical) return false;
                warningMicrochunksMinimum = 2;
                return true;
            }

            if (string.Equals(profileId, SoftBlendProfileId, StringComparison.Ordinal) ||
                string.Equals(profileId, CliffProfileId, StringComparison.Ordinal) ||
                string.Equals(profileId, TunnelProfileId, StringComparison.Ordinal) ||
                string.Equals(profileId, RuinProfileId, StringComparison.Ordinal))
            {
                warningMicrochunksMinimum = 2;
                return true;
            }

            return false;
        }

        private static bool IsProfileAllowedForPair(
            MoonpalaceBiomePair pair,
            MoonpalaceBoundaryProfileId profile)
        {
            var profileId = profile.CanonicalId;
            if (IsPair(pair, MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.CassiaRoot))
            {
                return IsOneOf(profileId, SoftBlendProfileId, CliffProfileId, TunnelProfileId);
            }

            if (IsPair(pair, MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.AbandonedMill))
            {
                return IsOneOf(profileId, RuinProfileId, SoftBlendProfileId);
            }

            if (IsPair(pair, MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.MoonDough))
            {
                return IsOneOf(profileId, CliffProfileId, LayerProfileId, SoftBlendProfileId);
            }

            if (IsPair(pair, MoonpalaceBiomeId.CassiaRoot, MoonpalaceBiomeId.AbandonedMill))
            {
                return IsOneOf(profileId, RuinProfileId, TunnelProfileId, SoftBlendProfileId);
            }

            if (IsPair(pair, MoonpalaceBiomeId.CassiaRoot, MoonpalaceBiomeId.MoonDough))
            {
                return IsOneOf(profileId, TunnelProfileId, LayerProfileId, SoftBlendProfileId);
            }

            if (IsPair(pair, MoonpalaceBiomeId.AbandonedMill, MoonpalaceBiomeId.MoonDough))
            {
                return IsOneOf(profileId, RuinProfileId, LayerProfileId, TunnelProfileId);
            }

            return false;
        }

        private static bool IsPair(
            MoonpalaceBiomePair pair,
            MoonpalaceBiomeId first,
            MoonpalaceBiomeId second)
        {
            return pair == new MoonpalaceBiomePair(first, second);
        }

        private static bool IsOneOf(string value, params string[] accepted)
        {
            return accepted.Any(item => string.Equals(value, item, StringComparison.Ordinal));
        }
    }
}
