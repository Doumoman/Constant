using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryWarningProbeTests
    {
        public static IEnumerable ProbeCases
        {
            get
            {
                for (var index = 0; index < 260; index++)
                {
                    yield return new TestCaseData(index)
                        .SetName("BoundaryWarningProbe_" + index.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(ProbeCases))]
        public void BoundaryWarningProbe(int caseIndex)
        {
            var cycle = caseIndex / 13;
            var fixture = CreateFixture(cycle);
            var probe = new MoonpalaceBoundaryWarningProbe();

            switch (caseIndex % 13)
            {
                case 0:
                    AssertAccepted(probe.Evaluate(CreateRequest(fixture, 2,
                        new[] { "Audio", "Tile" }, fixture.ResolveRequest.ToBiome)));
                    break;
                case 1:
                    AssertIssues(probe.Evaluate(CreateRequest(fixture, 1,
                            new[] { "Tile", "Background" }, fixture.ResolveRequest.ToBiome)),
                        MoonpalaceBoundaryWarningIssue.InsufficientWarningLength);
                    break;
                case 2:
                    var insufficientMarkers = probe.Evaluate(CreateRequest(fixture, 2,
                        new[] { "Tile" }, fixture.ResolveRequest.ToBiome));
                    AssertIssues(insufficientMarkers,
                        MoonpalaceBoundaryWarningIssue.InsufficientMarkerCategories);
                    Assert.That(insufficientMarkers.MissingMarkerCategoryCount, Is.EqualTo(1));
                    break;
                case 3:
                    AssertIssues(probe.Evaluate(CreateRequest(fixture, 2,
                            new[] { "Tile", "unknown" }, fixture.ResolveRequest.ToBiome)),
                        MoonpalaceBoundaryWarningIssue.InsufficientMarkerCategories,
                        MoonpalaceBoundaryWarningIssue.UnknownMarkerCategory);
                    break;
                case 4:
                    var duplicate = probe.Evaluate(CreateRequest(fixture, 2,
                        new[] { "Tile", "Tile", "Background" }, fixture.ResolveRequest.ToBiome));
                    AssertIssues(duplicate, MoonpalaceBoundaryWarningIssue.DuplicateMarkerCategory);
                    Assert.That(duplicate.ObservedDistinctMarkerCategoryCount, Is.EqualTo(2));
                    break;
                case 5:
                    AssertIssues(probe.Evaluate(CreateRequest(fixture, 2,
                            new[] { "Tile", "Background" }, OtherBiome(fixture.ResolveRequest.ToBiome))),
                        MoonpalaceBoundaryWarningIssue.TargetBiomeMismatch);
                    break;
                case 6:
                    AssertIssues(probe.Evaluate(CreateRequest(fixture, -1,
                            new[] { "Tile", "Background" }, fixture.ResolveRequest.ToBiome)),
                        MoonpalaceBoundaryWarningIssue.InvalidWarningLength);
                    break;
                case 7:
                    AssertIssues(probe.Evaluate(new MoonpalaceBoundaryWarningProbeRequest(
                            fixture.ResolveRequest, fixture.Candidate, null, 2,
                            new[] { "Tile", "Background" }, fixture.ResolveRequest.ToBiome)),
                        MoonpalaceBoundaryWarningIssue.MissingBoundaryProfile);
                    break;
                case 8:
                    AssertDeterministicIssueOrdering(fixture, probe);
                    break;
                case 9:
                    AssertInvalidRequestVariant(fixture, probe, cycle);
                    break;
                case 10:
                    AssertInputAndResultCollectionsAreImmutable(fixture, probe);
                    break;
                case 11:
                    AssertIdentityAndSourcePreservation(fixture, probe);
                    break;
                case 12:
                    AssertOwnershipAndDeterminism(fixture, probe);
                    break;
                default:
                    Assert.Fail("Unexpected boundary warning probe case.");
                    break;
            }
        }

        private static void AssertAccepted(MoonpalaceBoundaryWarningProbeResult result)
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IssueList, Is.Empty);
            Assert.That(result.WarningMicrochunkCount, Is.EqualTo(2));
            Assert.That(result.RequiredWarningMicrochunks, Is.EqualTo(2));
            Assert.That(result.ObservedDistinctMarkerCategoryCount, Is.EqualTo(2));
            Assert.That(result.RequiredDistinctMarkerCategoryCount, Is.EqualTo(2));
            Assert.That(result.ObservedMarkerCategories.Select(category => category.Token),
                Is.EqualTo(new[] { "Tile", "Audio" }));
            Assert.That(result.MissingMarkerCategoryCount, Is.Zero);
        }

        private static void AssertDeterministicIssueOrdering(
            Fixture fixture,
            MoonpalaceBoundaryWarningProbe probe)
        {
            var result = probe.Evaluate(CreateRequest(fixture, 0,
                new[] { "Tile", "Tile", " unknown" },
                OtherBiome(fixture.ResolveRequest.ToBiome)));
            AssertIssues(result,
                MoonpalaceBoundaryWarningIssue.InsufficientWarningLength,
                MoonpalaceBoundaryWarningIssue.InsufficientMarkerCategories,
                MoonpalaceBoundaryWarningIssue.UnknownMarkerCategory,
                MoonpalaceBoundaryWarningIssue.DuplicateMarkerCategory,
                MoonpalaceBoundaryWarningIssue.TargetBiomeMismatch);
        }

        private static void AssertInvalidRequestVariant(
            Fixture fixture,
            MoonpalaceBoundaryWarningProbe probe,
            int cycle)
        {
            MoonpalaceBoundaryWarningProbeRequest request;
            switch (cycle % 5)
            {
                case 0:
                    request = null;
                    break;
                case 1:
                    request = new MoonpalaceBoundaryWarningProbeRequest(
                        null, fixture.Candidate, fixture.Requirement, 2,
                        new[] { "Tile", "Background" }, fixture.ResolveRequest.ToBiome);
                    break;
                case 2:
                    request = new MoonpalaceBoundaryWarningProbeRequest(
                        fixture.ResolveRequest, null, fixture.Requirement, 2,
                        new[] { "Tile", "Background" }, fixture.ResolveRequest.ToBiome);
                    break;
                case 3:
                    request = new MoonpalaceBoundaryWarningProbeRequest(
                        fixture.ResolveRequest, fixture.Candidate, fixture.Requirement, 2,
                        null, fixture.ResolveRequest.ToBiome);
                    break;
                default:
                    request = new MoonpalaceBoundaryWarningProbeRequest(
                        fixture.ResolveRequest, fixture.Candidate, fixture.Requirement, 2,
                        new[] { "Tile", "Background" }, default);
                    break;
            }

            Assert.That(probe.Evaluate(request).IssueList,
                Does.Contain(MoonpalaceBoundaryWarningIssue.InvalidRequest));
        }

        private static void AssertInputAndResultCollectionsAreImmutable(
            Fixture fixture,
            MoonpalaceBoundaryWarningProbe probe)
        {
            var source = new List<string> { "Tile", "Background" };
            var request = CreateRequest(fixture, 2, source, fixture.ResolveRequest.ToBiome);
            source.Clear();
            var result = probe.Evaluate(request);
            Assert.That(request.ObservedMarkerCategories,
                Is.EqualTo(new[] { "Tile", "Background" }));
            Assert.That(result.Accepted, Is.True);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MoonpalaceBoundaryWarningMarkerCategory>)result.ObservedMarkerCategories).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MoonpalaceBoundaryWarningIssue>)result.IssueList).Clear());
        }

        private static void AssertIdentityAndSourcePreservation(
            Fixture fixture,
            MoonpalaceBoundaryWarningProbe probe)
        {
            var candidateSignature = fixture.Candidate.Signature;
            var result = probe.Evaluate(CreateRequest(fixture, 2,
                new[] { "Tile", "Background" }, fixture.ResolveRequest.ToBiome));
            Assert.That(result.ProbeRequest.ResolveRequest, Is.SameAs(fixture.ResolveRequest));
            Assert.That(result.ResolveRequest, Is.SameAs(fixture.ResolveRequest));
            Assert.That(result.Candidate, Is.SameAs(fixture.Candidate));
            Assert.That(result.WarningRequirement, Is.SameAs(fixture.Requirement));
            Assert.That(result.TargetBiome, Is.EqualTo(fixture.ResolveRequest.ToBiome));
            Assert.That(fixture.Candidate.Signature, Is.EqualTo(candidateSignature));
            Assert.That(fixture.Candidate.Weight, Is.EqualTo(10));
            Assert.That(fixture.Candidate.EdgeSignature, Is.EqualTo(fixture.ResolveRequest.EdgeSignature));
        }

        private static void AssertOwnershipAndDeterminism(
            Fixture fixture,
            MoonpalaceBoundaryWarningProbe probe)
        {
            var request = CreateRequest(fixture, 0,
                new[] { "Tile", "Tile", "unknown" },
                OtherBiome(fixture.ResolveRequest.ToBiome));
            var first = probe.Evaluate(request);
            var second = probe.Evaluate(request);
            Assert.That(second.IssueList, Is.EqualTo(first.IssueList));
            Assert.That(second.ObservedMarkerCategories, Is.EqualTo(first.ObservedMarkerCategories));
            Assert.That(typeof(MoonpalaceBoundaryWarningProbeResult).GetProperty("ResolvedCandidate"), Is.Null);
            Assert.That(typeof(MoonpalaceBoundaryWarningProbeResult).GetProperty("FilteredCandidateIndex"), Is.Null);
            Assert.That(first.Candidate, Is.SameAs(fixture.Candidate));
        }

        private static void AssertIssues(
            MoonpalaceBoundaryWarningProbeResult result,
            params MoonpalaceBoundaryWarningIssue[] expected)
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IssueList, Is.EqualTo(expected));
        }

        private static Fixture CreateFixture(int cycle)
        {
            var pair = MoonpalaceBiomePairCatalog.Canonical.Pairs[
                cycle % MoonpalaceBiomePairCatalog.Canonical.Pairs.Count];
            var profileId = pair == new MoonpalaceBiomePair(
                    MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.AbandonedMill)
                ? MoonpalaceBoundaryWarningRequirement.RuinProfileId
                : pair == new MoonpalaceBiomePair(
                    MoonpalaceBiomeId.AbandonedMill, MoonpalaceBiomeId.MoonDough)
                    ? MoonpalaceBoundaryWarningRequirement.TunnelProfileId
                    : MoonpalaceBoundaryWarningRequirement.SoftBlendProfileId;
            var profile = new MoonpalaceBoundaryProfileId(profileId);
            var orientation = cycle % 2 == 0
                ? MoonpalaceBoundaryOrientation.Horizontal
                : MoonpalaceBoundaryOrientation.Vertical;
            var role = new MoonpalaceBoundaryRouteRole("Traversal");
            var signature = new MoonpalaceBoundaryEdgeSignature("SIG-WARNING");
            var reverse = cycle % 3 == 0;
            var resolveRequest = new MoonpalaceBoundaryResolveRequest(
                reverse ? pair.Second : pair.First,
                reverse ? pair.First : pair.Second,
                profile,
                orientation,
                role,
                signature,
                (ulong)cycle);
            var candidate = new MoonpalaceBoundaryCandidateDefinition(
                "WARN-CANDIDATE", pair, profile, orientation, role, signature, 10, true,
                MoonpalaceBoundaryToolRequirement.None,
                MoonpalaceBoundaryWarningMarker.Tile |
                MoonpalaceBoundaryWarningMarker.Background |
                MoonpalaceBoundaryWarningMarker.Resource |
                MoonpalaceBoundaryWarningMarker.Audio);
            return new Fixture(
                resolveRequest,
                candidate,
                MoonpalaceBoundaryWarningRequirement.Create(resolveRequest, candidate));
        }

        private static MoonpalaceBoundaryWarningProbeRequest CreateRequest(
            Fixture fixture,
            int warningMicrochunkCount,
            IEnumerable<string> observed,
            MoonpalaceBiomeId targetBiome)
        {
            return new MoonpalaceBoundaryWarningProbeRequest(
                fixture.ResolveRequest,
                fixture.Candidate,
                fixture.Requirement,
                warningMicrochunkCount,
                observed,
                targetBiome);
        }

        private static MoonpalaceBiomeId OtherBiome(MoonpalaceBiomeId biome)
        {
            return biome == MoonpalaceBiomeId.MoonCrater
                ? MoonpalaceBiomeId.CassiaRoot
                : MoonpalaceBiomeId.MoonCrater;
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
