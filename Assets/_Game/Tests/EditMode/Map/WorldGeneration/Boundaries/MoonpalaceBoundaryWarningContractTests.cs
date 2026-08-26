using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.WorldGeneration.Boundaries
{
    [Category("MAP08_05")]
    public sealed class MoonpalaceBoundaryWarningContractTests
    {
        private static readonly PairProfileRule[] PairProfileRules =
        {
            Rule(MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.CassiaRoot,
                MoonpalaceBoundaryWarningRequirement.SoftBlendProfileId),
            Rule(MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.CassiaRoot,
                MoonpalaceBoundaryWarningRequirement.CliffProfileId),
            Rule(MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.CassiaRoot,
                MoonpalaceBoundaryWarningRequirement.TunnelProfileId),
            Rule(MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.AbandonedMill,
                MoonpalaceBoundaryWarningRequirement.RuinProfileId),
            Rule(MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.AbandonedMill,
                MoonpalaceBoundaryWarningRequirement.SoftBlendProfileId),
            Rule(MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.MoonDough,
                MoonpalaceBoundaryWarningRequirement.CliffProfileId),
            Rule(MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.MoonDough,
                MoonpalaceBoundaryWarningRequirement.LayerProfileId),
            Rule(MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.MoonDough,
                MoonpalaceBoundaryWarningRequirement.SoftBlendProfileId),
            Rule(MoonpalaceBiomeId.CassiaRoot, MoonpalaceBiomeId.AbandonedMill,
                MoonpalaceBoundaryWarningRequirement.RuinProfileId),
            Rule(MoonpalaceBiomeId.CassiaRoot, MoonpalaceBiomeId.AbandonedMill,
                MoonpalaceBoundaryWarningRequirement.TunnelProfileId),
            Rule(MoonpalaceBiomeId.CassiaRoot, MoonpalaceBiomeId.AbandonedMill,
                MoonpalaceBoundaryWarningRequirement.SoftBlendProfileId),
            Rule(MoonpalaceBiomeId.CassiaRoot, MoonpalaceBiomeId.MoonDough,
                MoonpalaceBoundaryWarningRequirement.TunnelProfileId),
            Rule(MoonpalaceBiomeId.CassiaRoot, MoonpalaceBiomeId.MoonDough,
                MoonpalaceBoundaryWarningRequirement.LayerProfileId),
            Rule(MoonpalaceBiomeId.CassiaRoot, MoonpalaceBiomeId.MoonDough,
                MoonpalaceBoundaryWarningRequirement.SoftBlendProfileId),
            Rule(MoonpalaceBiomeId.AbandonedMill, MoonpalaceBiomeId.MoonDough,
                MoonpalaceBoundaryWarningRequirement.RuinProfileId),
            Rule(MoonpalaceBiomeId.AbandonedMill, MoonpalaceBiomeId.MoonDough,
                MoonpalaceBoundaryWarningRequirement.LayerProfileId),
            Rule(MoonpalaceBiomeId.AbandonedMill, MoonpalaceBiomeId.MoonDough,
                MoonpalaceBoundaryWarningRequirement.TunnelProfileId),
        };

        public static IEnumerable ContractCases
        {
            get
            {
                for (var index = 0; index < 260; index++)
                {
                    yield return new TestCaseData(index)
                        .SetName("BoundaryWarningContract_" + index.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void BoundaryWarningContract(int caseIndex)
        {
            var cycle = caseIndex / 13;
            var fixture = CreateFixture(cycle);

            switch (caseIndex % 13)
            {
                case 0:
                    Assert.That(MoonpalaceBoundaryWarningMarkerCategory.CanonicalValues
                            .Select(category => category.Token),
                        Is.EqualTo(new[] { "Tile", "Background", "Resource", "Audio" }));
                    break;
                case 1:
                    AssertStrictMarkerParsing(cycle);
                    break;
                case 2:
                    AssertTypedMarkerContract();
                    break;
                case 3:
                    AssertRequirementFields(fixture);
                    break;
                case 4:
                    Assert.That(MoonpalaceBoundaryWarningRequirement.TryCreate(
                        fixture.ResolveRequest, fixture.Candidate, out var requirement), Is.True);
                    Assert.That(requirement.Signature, Is.EqualTo(fixture.Requirement.Signature));
                    break;
                case 5:
                    AssertEveryActivePairProfile();
                    break;
                case 6:
                    AssertRejectedProfile(fixture, MoonpalaceBoundaryWarningRequirement.HardStarstoneProfileId);
                    break;
                case 7:
                    AssertRejectedProfile(fixture, "BOUND_UNKNOWN");
                    break;
                case 8:
                    AssertDisallowedPairProfile();
                    break;
                case 9:
                    AssertLayerOrientationContract();
                    break;
                case 10:
                    Assert.That(fixture.Requirement.IsCompatible(
                        fixture.ResolveRequest, fixture.Candidate), Is.True);
                    Assert.That(MoonpalaceBoundaryWarningRequirement.Create(
                        fixture.ResolveRequest, fixture.Candidate).Signature,
                        Is.EqualTo(fixture.Requirement.Signature));
                    break;
                case 11:
                    AssertAllowedCategoriesAreImmutable(fixture.Requirement);
                    break;
                case 12:
                    AssertSourceIdentityPreserved(fixture);
                    break;
                default:
                    Assert.Fail("Unexpected boundary warning contract case.");
                    break;
            }
        }

        private static void AssertStrictMarkerParsing(int cycle)
        {
            var invalid = new string[]
            {
                null, string.Empty, " ", " Tile", "Tile ", "tile", "TILE",
                "Unknown", "\t", "Background\n", "Resource ", "Audio ", "NONE",
            };
            var value = invalid[cycle % invalid.Length];
            Assert.That(MoonpalaceBoundaryWarningMarkerCategory.TryParse(value, out var parsed), Is.False);
            Assert.That(parsed.IsDefined, Is.False);
            Assert.Throws<ArgumentException>(() => MoonpalaceBoundaryWarningMarkerCategory.Parse(value));
        }

        private static void AssertTypedMarkerContract()
        {
            var parsed = MoonpalaceBoundaryWarningMarkerCategory.Parse("Background");
            Assert.That(parsed, Is.EqualTo(MoonpalaceBoundaryWarningMarkerCategory.Background));
            Assert.That(parsed.Marker, Is.EqualTo(MoonpalaceBoundaryWarningMarker.Background));
            Assert.That(parsed.ToString(), Is.EqualTo("Background"));
            Assert.That(MoonpalaceBoundaryWarningMarkerCategory.Tile.CompareTo(parsed), Is.LessThan(0));
            Assert.That(parsed.GetHashCode(),
                Is.EqualTo(MoonpalaceBoundaryWarningMarkerCategory.Background.GetHashCode()));
            Assert.Throws<InvalidOperationException>(() => default(MoonpalaceBoundaryWarningMarkerCategory).ToString());
        }

        private static void AssertRequirementFields(Fixture fixture)
        {
            Assert.That(fixture.Requirement.BoundaryProfileId, Is.EqualTo(fixture.ResolveRequest.Profile));
            Assert.That(fixture.Requirement.Orientation, Is.EqualTo(fixture.ResolveRequest.Orientation));
            Assert.That(fixture.Requirement.WarningMicrochunksMinimum, Is.EqualTo(2));
            Assert.That(fixture.Requirement.RequiredDistinctMarkerCategories, Is.EqualTo(2));
            Assert.That(fixture.Requirement.AllowedMarkerCategories.Select(category => category.Token),
                Is.EqualTo(new[] { "Tile", "Background", "Resource", "Audio" }));
        }

        private static void AssertEveryActivePairProfile()
        {
            var validated = 0;
            foreach (var rule in PairProfileRules)
            {
                var orientations = string.Equals(rule.ProfileId,
                    MoonpalaceBoundaryWarningRequirement.LayerProfileId,
                    StringComparison.Ordinal)
                    ? new[] { MoonpalaceBoundaryOrientation.Vertical }
                    : new[]
                    {
                        MoonpalaceBoundaryOrientation.Horizontal,
                        MoonpalaceBoundaryOrientation.Vertical,
                    };
                foreach (var orientation in orientations)
                {
                    var fixture = CreateFixture(rule, orientation, false);
                    Assert.That(fixture.Requirement.WarningMicrochunksMinimum, Is.EqualTo(2));
                    Assert.That(fixture.Requirement.RequiredDistinctMarkerCategories, Is.EqualTo(2));
                    validated++;
                }
            }

            Assert.That(validated, Is.EqualTo(31));
        }

        private static void AssertRejectedProfile(Fixture fixture, string profileId)
        {
            var request = CopyRequest(fixture.ResolveRequest,
                new MoonpalaceBoundaryProfileId(profileId), fixture.ResolveRequest.Orientation);
            var candidate = CopyCandidate(fixture.Candidate, request.Profile, request.Orientation);
            Assert.That(MoonpalaceBoundaryWarningRequirement.TryCreate(request, candidate, out var requirement),
                Is.False);
            Assert.That(requirement, Is.Null);
        }

        private static void AssertDisallowedPairProfile()
        {
            var rule = Rule(MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.AbandonedMill,
                MoonpalaceBoundaryWarningRequirement.CliffProfileId);
            var fixture = CreateFixture(rule, MoonpalaceBoundaryOrientation.Horizontal, true);
            Assert.That(MoonpalaceBoundaryWarningRequirement.TryCreate(
                fixture.ResolveRequest, fixture.Candidate, out var requirement), Is.False);
            Assert.That(requirement, Is.Null);
        }

        private static void AssertLayerOrientationContract()
        {
            var rule = Rule(MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.MoonDough,
                MoonpalaceBoundaryWarningRequirement.LayerProfileId);
            var vertical = CreateFixture(rule, MoonpalaceBoundaryOrientation.Vertical, false);
            var horizontal = CreateFixture(rule, MoonpalaceBoundaryOrientation.Horizontal, true);
            Assert.That(vertical.Requirement.WarningMicrochunksMinimum, Is.EqualTo(2));
            Assert.That(MoonpalaceBoundaryWarningRequirement.TryCreate(
                horizontal.ResolveRequest, horizontal.Candidate, out _), Is.False);
        }

        private static void AssertAllowedCategoriesAreImmutable(
            MoonpalaceBoundaryWarningRequirement requirement)
        {
            Assert.That(requirement.AllowedMarkerCategories.Count, Is.EqualTo(4));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MoonpalaceBoundaryWarningMarkerCategory>)requirement.AllowedMarkerCategories).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MoonpalaceBoundaryWarningMarkerCategory>)
                    MoonpalaceBoundaryWarningMarkerCategory.CanonicalValues).Clear());
        }

        private static void AssertSourceIdentityPreserved(Fixture fixture)
        {
            var candidateSignature = fixture.Candidate.Signature;
            var requestFields = new object[]
            {
                fixture.ResolveRequest.FromBiome,
                fixture.ResolveRequest.ToBiome,
                fixture.ResolveRequest.Profile,
                fixture.ResolveRequest.Orientation,
                fixture.ResolveRequest.RouteRole,
                fixture.ResolveRequest.EdgeSignature,
                fixture.ResolveRequest.SelectionSeed,
            };
            var requirement = MoonpalaceBoundaryWarningRequirement.Create(
                fixture.ResolveRequest, fixture.Candidate);
            Assert.That(requirement.Signature, Is.EqualTo(fixture.Requirement.Signature));
            Assert.That(fixture.Candidate.Signature, Is.EqualTo(candidateSignature));
            Assert.That(new object[]
            {
                fixture.ResolveRequest.FromBiome,
                fixture.ResolveRequest.ToBiome,
                fixture.ResolveRequest.Profile,
                fixture.ResolveRequest.Orientation,
                fixture.ResolveRequest.RouteRole,
                fixture.ResolveRequest.EdgeSignature,
                fixture.ResolveRequest.SelectionSeed,
            }, Is.EqualTo(requestFields));
        }

        private static Fixture CreateFixture(int cycle)
        {
            var rule = PairProfileRules[cycle % PairProfileRules.Length];
            var orientation = string.Equals(rule.ProfileId,
                MoonpalaceBoundaryWarningRequirement.LayerProfileId,
                StringComparison.Ordinal)
                ? MoonpalaceBoundaryOrientation.Vertical
                : cycle % 2 == 0
                    ? MoonpalaceBoundaryOrientation.Horizontal
                    : MoonpalaceBoundaryOrientation.Vertical;
            return CreateFixture(rule, orientation, false);
        }

        private static Fixture CreateFixture(
            PairProfileRule rule,
            MoonpalaceBoundaryOrientation orientation,
            bool allowInvalid)
        {
            var pair = new MoonpalaceBiomePair(rule.First, rule.Second);
            var profile = new MoonpalaceBoundaryProfileId(rule.ProfileId);
            var role = new MoonpalaceBoundaryRouteRole("Traversal");
            var signature = new MoonpalaceBoundaryEdgeSignature("SIG-WARNING");
            var request = new MoonpalaceBoundaryResolveRequest(
                pair.First, pair.Second, profile, orientation, role, signature, 0x805UL);
            var candidate = new MoonpalaceBoundaryCandidateDefinition(
                "WARN-CANDIDATE", pair, profile, orientation, role, signature, 10, true,
                MoonpalaceBoundaryToolRequirement.None,
                MoonpalaceBoundaryWarningMarker.Tile |
                MoonpalaceBoundaryWarningMarker.Background |
                MoonpalaceBoundaryWarningMarker.Resource |
                MoonpalaceBoundaryWarningMarker.Audio);
            MoonpalaceBoundaryWarningRequirement requirement = null;
            if (!allowInvalid)
            {
                requirement = MoonpalaceBoundaryWarningRequirement.Create(request, candidate);
            }

            return new Fixture(request, candidate, requirement);
        }

        private static MoonpalaceBoundaryResolveRequest CopyRequest(
            MoonpalaceBoundaryResolveRequest source,
            MoonpalaceBoundaryProfileId profile,
            MoonpalaceBoundaryOrientation orientation)
        {
            return new MoonpalaceBoundaryResolveRequest(
                source.FromBiome, source.ToBiome, profile, orientation,
                source.RouteRole, source.EdgeSignature, source.SelectionSeed);
        }

        private static MoonpalaceBoundaryCandidateDefinition CopyCandidate(
            MoonpalaceBoundaryCandidateDefinition source,
            MoonpalaceBoundaryProfileId profile,
            MoonpalaceBoundaryOrientation orientation)
        {
            return new MoonpalaceBoundaryCandidateDefinition(
                source.CandidateId, source.Pair, profile, orientation, source.RouteRole,
                source.EdgeSignature, source.Weight, source.MandatoryRouteAllowed,
                source.ToolRequirement, source.WarningMarkers);
        }

        private static PairProfileRule Rule(
            MoonpalaceBiomeId first,
            MoonpalaceBiomeId second,
            string profileId)
        {
            return new PairProfileRule(first, second, profileId);
        }

        private sealed class PairProfileRule
        {
            public PairProfileRule(
                MoonpalaceBiomeId first,
                MoonpalaceBiomeId second,
                string profileId)
            {
                First = first;
                Second = second;
                ProfileId = profileId;
            }

            public MoonpalaceBiomeId First { get; }
            public MoonpalaceBiomeId Second { get; }
            public string ProfileId { get; }
        }

        private sealed class Fixture
        {
            public Fixture(
                MoonpalaceBoundaryResolveRequest resolveRequest,
                MoonpalaceBoundaryCandidateDefinition candidate,
                MoonpalaceBoundaryWarningRequirement requirement)
            {
                ResolveRequest = resolveRequest;
                Candidate = candidate;
                Requirement = requirement;
            }

            public MoonpalaceBoundaryResolveRequest ResolveRequest { get; }
            public MoonpalaceBoundaryCandidateDefinition Candidate { get; }
            public MoonpalaceBoundaryWarningRequirement Requirement { get; }
        }
    }
}
