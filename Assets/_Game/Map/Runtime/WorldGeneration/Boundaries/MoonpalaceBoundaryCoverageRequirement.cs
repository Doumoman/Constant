using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryCoverageRequirement
    {
        private static readonly IReadOnlyList<MoonpalaceBoundaryCoverageRequirement> canonical =
            new ReadOnlyCollection<MoonpalaceBoundaryCoverageRequirement>(new[]
            {
                Create(0, "PAIR_CRATER_ROOT", "BIO_MOON_CRATER", "BIO_CASSIA_ROOT",
                    new[] { "BOUND_SOFT_BLEND", "BOUND_CLIFF", "BOUND_TUNNEL" },
                    new[] { 50, 25, 25 }, "BOUND_SOFT_BLEND", 6),
                Create(1, "PAIR_CRATER_MILL", "BIO_MOON_CRATER", "BIO_ABANDONED_MILL",
                    new[] { "BOUND_RUIN", "BOUND_SOFT_BLEND" },
                    new[] { 70, 30 }, "BOUND_RUIN", 4),
                Create(2, "PAIR_CRATER_DOUGH", "BIO_MOON_CRATER", "BIO_MOON_DOUGH",
                    new[] { "BOUND_CLIFF", "BOUND_LAYER", "BOUND_SOFT_BLEND" },
                    new[] { 45, 35, 20 }, "BOUND_CLIFF", 5),
                Create(3, "PAIR_ROOT_MILL", "BIO_CASSIA_ROOT", "BIO_ABANDONED_MILL",
                    new[] { "BOUND_RUIN", "BOUND_TUNNEL", "BOUND_SOFT_BLEND" },
                    new[] { 45, 35, 20 }, "BOUND_RUIN", 6),
                Create(4, "PAIR_ROOT_DOUGH", "BIO_CASSIA_ROOT", "BIO_MOON_DOUGH",
                    new[] { "BOUND_TUNNEL", "BOUND_LAYER", "BOUND_SOFT_BLEND" },
                    new[] { 45, 30, 25 }, "BOUND_TUNNEL", 5),
                Create(5, "PAIR_MILL_DOUGH", "BIO_ABANDONED_MILL", "BIO_MOON_DOUGH",
                    new[] { "BOUND_RUIN", "BOUND_LAYER", "BOUND_TUNNEL" },
                    new[] { 45, 30, 25 }, "BOUND_RUIN", 5),
            });

        private readonly IReadOnlyList<string> allowedProfileIds;
        private readonly IReadOnlyList<int> profileWeights;
        private readonly IReadOnlyList<string> expectedMatrix;

        public MoonpalaceBoundaryCoverageRequirement(
            int pairOrder,
            string pairRuleId,
            string biomeAId,
            string biomeBId,
            IEnumerable<string> allowedProfileIds,
            IEnumerable<int> profileWeights,
            string defaultProfileId,
            int expectedCandidateCount,
            int expectedMicrochunkCount,
            int expectedTileRowCount,
            int expectedSocketRowCount,
            bool active)
        {
            if (allowedProfileIds == null) throw new ArgumentNullException(nameof(allowedProfileIds));
            if (profileWeights == null) throw new ArgumentNullException(nameof(profileWeights));

            PairOrder = pairOrder;
            PairRuleId = pairRuleId;
            BiomeAId = biomeAId;
            BiomeBId = biomeBId;
            this.allowedProfileIds = Snapshot(allowedProfileIds);
            this.profileWeights = new ReadOnlyCollection<int>(profileWeights.ToArray());
            DefaultProfileId = defaultProfileId;
            ExpectedCandidateCount = expectedCandidateCount;
            ExpectedMicrochunkCount = expectedMicrochunkCount;
            ExpectedTileRowCount = expectedTileRowCount;
            ExpectedSocketRowCount = expectedSocketRowCount;
            Active = active;
            expectedMatrix = BuildExpectedMatrix(this.allowedProfileIds);
        }

        public static IReadOnlyList<MoonpalaceBoundaryCoverageRequirement> Canonical => canonical;
        public int PairOrder { get; }
        public string PairRuleId { get; }
        public string BiomeAId { get; }
        public string BiomeBId { get; }
        public IReadOnlyList<string> AllowedProfileIds => allowedProfileIds;
        public IReadOnlyList<int> ProfileWeights => profileWeights;
        public string DefaultProfileId { get; }
        public int ExpectedCandidateCount { get; }
        public int ExpectedMicrochunkCount { get; }
        public int ExpectedTileRowCount { get; }
        public int ExpectedSocketRowCount { get; }
        public bool Active { get; }
        public IReadOnlyList<string> ExpectedMatrix => expectedMatrix;

        public static bool TryGetCanonical(
            string pairRuleId,
            out MoonpalaceBoundaryCoverageRequirement requirement)
        {
            requirement = canonical.FirstOrDefault(value =>
                string.Equals(value.PairRuleId, pairRuleId, StringComparison.Ordinal));
            return requirement != null;
        }

        public int GetProfileOrder(string profileId)
        {
            for (var index = 0; index < allowedProfileIds.Count; index++)
            {
                if (string.Equals(allowedProfileIds[index], profileId, StringComparison.Ordinal)) return index;
            }

            return int.MaxValue;
        }

        public bool Allows(string profileId, MoonpalaceBoundaryOrientation orientation)
        {
            if (!allowedProfileIds.Contains(profileId, StringComparer.Ordinal)) return false;
            return !string.Equals(profileId, "BOUND_LAYER", StringComparison.Ordinal) ||
                   orientation == MoonpalaceBoundaryOrientation.Vertical;
        }

        public static string MatrixKey(string profileId, MoonpalaceBoundaryOrientation orientation)
        {
            var token = orientation == MoonpalaceBoundaryOrientation.Horizontal
                ? "HORIZONTAL"
                : orientation == MoonpalaceBoundaryOrientation.Vertical ? "VERTICAL" : "INVALID";
            return (profileId ?? string.Empty) + "|" + token;
        }

        private static MoonpalaceBoundaryCoverageRequirement Create(
            int order,
            string pairRuleId,
            string biomeAId,
            string biomeBId,
            string[] profiles,
            int[] weights,
            string defaultProfile,
            int candidates)
        {
            return new MoonpalaceBoundaryCoverageRequirement(
                order,
                pairRuleId,
                biomeAId,
                biomeBId,
                profiles,
                weights,
                defaultProfile,
                candidates,
                candidates,
                candidates * 96,
                candidates * 2,
                true);
        }

        private static IReadOnlyList<string> BuildExpectedMatrix(IEnumerable<string> profiles)
        {
            var values = new List<string>();
            foreach (var profile in profiles)
            {
                if (!string.Equals(profile, "BOUND_LAYER", StringComparison.Ordinal))
                {
                    values.Add(MatrixKey(profile, MoonpalaceBoundaryOrientation.Horizontal));
                }
                values.Add(MatrixKey(profile, MoonpalaceBoundaryOrientation.Vertical));
            }
            return new ReadOnlyCollection<string>(values);
        }

        private static IReadOnlyList<string> Snapshot(IEnumerable<string> source)
        {
            return new ReadOnlyCollection<string>(source.ToArray());
        }
    }
}
