using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.WorldGeneration.Boundaries
{
    [Category("MAP08_04")]
    public sealed class MoonpalaceMandatoryBoundaryFilterTests
    {
        public static IEnumerable FilterCases
        {
            get
            {
                for (var index = 0; index < 320; index++)
                {
                    yield return new TestCaseData(index)
                        .SetName("MandatoryBoundaryFilterContract_" + index.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(FilterCases))]
        public void MandatoryBoundaryFilterContract(int caseIndex)
        {
            var cycle = caseIndex / 16;
            var fixture = CreateFixture(cycle);
            var filter = new MoonpalaceMandatoryBoundaryFilter();
            var mandatoryRequest = new MoonpalaceMandatoryBoundaryFilterRequest(
                fixture.ResolveRequest,
                fixture.Index,
                true);

            switch (caseIndex % 16)
            {
                case 0:
                    Assert.That(mandatoryRequest.ResolveRequest, Is.SameAs(fixture.ResolveRequest));
                    Assert.That(mandatoryRequest.CandidateIndex, Is.SameAs(fixture.Index));
                    Assert.That(mandatoryRequest.MandatoryRouteBoundary, Is.True);
                    break;
                case 1:
                    AssertMandatoryAccepted(filter.Apply(mandatoryRequest));
                    break;
                case 2:
                    var toolRejected = filter.Apply(mandatoryRequest);
                    Assert.That(toolRejected.RejectionSummaryByReason[
                        MoonpalaceMandatoryBoundaryFilterIssue.ToolRequired], Is.EqualTo(1));
                    Assert.That(toolRejected.AcceptedCandidates.Any(candidate =>
                        candidate.ToolRequirement != MoonpalaceBoundaryToolRequirement.None), Is.False);
                    break;
                case 3:
                    var routeRejected = filter.Apply(mandatoryRequest);
                    Assert.That(routeRejected.RejectionSummaryByReason[
                        MoonpalaceMandatoryBoundaryFilterIssue.MandatoryRouteNotAllowed], Is.EqualTo(2));
                    Assert.That(routeRejected.AcceptedCandidates.All(candidate =>
                        candidate.MandatoryRouteAllowed), Is.True);
                    break;
                case 4:
                    var prioritized = filter.Apply(mandatoryRequest);
                    Assert.That(MoonpalaceMandatoryBoundaryFilterPolicy.RejectionPrioritySignature,
                        Is.EqualTo("MandatoryRouteNotAllowed>ToolRequired"));
                    Assert.That(prioritized.RejectionSummaryByReason[
                        MoonpalaceMandatoryBoundaryFilterIssue.MandatoryRouteNotAllowed], Is.EqualTo(2));
                    Assert.That(prioritized.RejectionSummaryByReason[
                        MoonpalaceMandatoryBoundaryFilterIssue.ToolRequired], Is.EqualTo(1));
                    break;
                case 5:
                    var nonMandatoryTools = filter.Apply(new MoonpalaceMandatoryBoundaryFilterRequest(
                        fixture.ResolveRequest, fixture.Index, false));
                    Assert.That(nonMandatoryTools.AcceptedCandidates.Single(candidate =>
                        candidate.CandidateId == "B-TOOL").ToolRequirement,
                        Is.EqualTo(MoonpalaceBoundaryToolRequirement.Pickaxe));
                    Assert.That(nonMandatoryTools.AcceptedCandidateCount, Is.EqualTo(5));
                    break;
                case 6:
                    var nonMandatoryRoutes = filter.Apply(new MoonpalaceMandatoryBoundaryFilterRequest(
                        fixture.ResolveRequest, fixture.Index, false));
                    Assert.That(nonMandatoryRoutes.AcceptedCandidates.Count(candidate =>
                        !candidate.MandatoryRouteAllowed), Is.EqualTo(2));
                    Assert.That(nonMandatoryRoutes.RejectionSummaryByReason, Is.Empty);
                    break;
                case 7:
                    var counts = filter.Apply(mandatoryRequest);
                    Assert.That(counts.OriginalCandidateCount, Is.EqualTo(5));
                    Assert.That(counts.AcceptedCandidateCount, Is.EqualTo(2));
                    Assert.That(counts.RejectedCandidateCount, Is.EqualTo(3));
                    Assert.That(counts.RejectionSummaryByReason.Values.Sum(), Is.EqualTo(3));
                    break;
                case 8:
                    AssertDeterministicOrder(fixture, filter);
                    break;
                case 9:
                    var allRejectedIndex = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(
                        fixture.Candidates.Where(candidate =>
                            candidate.CandidateId != "A-ALLOWED" &&
                            candidate.CandidateId != "E-ALLOWED"));
                    var noCandidates = filter.Apply(new MoonpalaceMandatoryBoundaryFilterRequest(
                        fixture.ResolveRequest, allRejectedIndex, true));
                    AssertFailure(noCandidates,
                        MoonpalaceMandatoryBoundaryFilterIssue.NoCandidatesAfterFilter);
                    Assert.That(noCandidates.OriginalCandidateCount, Is.EqualTo(3));
                    Assert.That(noCandidates.RejectedCandidateCount, Is.EqualTo(3));
                    Assert.That(noCandidates.FilteredCandidateIndex, Is.Null);
                    break;
                case 10:
                    AssertFailure(filter.Apply(null),
                        MoonpalaceMandatoryBoundaryFilterIssue.InvalidRequest);
                    break;
                case 11:
                    AssertFailure(filter.Apply(new MoonpalaceMandatoryBoundaryFilterRequest(
                            null, fixture.Index, true)),
                        MoonpalaceMandatoryBoundaryFilterIssue.InvalidRequest);
                    break;
                case 12:
                    AssertFailure(filter.Apply(new MoonpalaceMandatoryBoundaryFilterRequest(
                            fixture.ResolveRequest, null, true)),
                        MoonpalaceMandatoryBoundaryFilterIssue.InvalidRequest);
                    break;
                case 13:
                    AssertFailure(filter.Apply(new MoonpalaceMandatoryBoundaryFilterRequest(
                            CreateInvalidResolveRequest(fixture, cycle), fixture.Index, true)),
                        MoonpalaceMandatoryBoundaryFilterIssue.InvalidRequest);
                    break;
                case 14:
                    AssertResolverInputBoundary(fixture, filter.Apply(mandatoryRequest));
                    break;
                case 15:
                    AssertSourceIsNotMutated(fixture, filter.Apply(mandatoryRequest));
                    break;
                default:
                    Assert.Fail("Unexpected mandatory-boundary filter contract case.");
                    break;
            }
        }

        private static void AssertMandatoryAccepted(MoonpalaceMandatoryBoundaryFilterResult result)
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IssueList, Is.Empty);
            Assert.That(result.AcceptedCandidates.Select(candidate => candidate.CandidateId),
                Is.EqualTo(new[] { "A-ALLOWED", "E-ALLOWED" }));
            Assert.That(result.AcceptedCandidates.All(candidate =>
                candidate.MandatoryRouteAllowed &&
                candidate.ToolRequirement == MoonpalaceBoundaryToolRequirement.None), Is.True);
            Assert.That(result.FilteredCandidateIndex, Is.Not.Null);
            Assert.That(result.FilteredCandidateIndex.Count, Is.EqualTo(2));
        }

        private static void AssertDeterministicOrder(
            Fixture fixture,
            MoonpalaceMandatoryBoundaryFilter filter)
        {
            var reversed = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(
                fixture.Candidates.AsEnumerable().Reverse());
            var first = filter.Apply(new MoonpalaceMandatoryBoundaryFilterRequest(
                fixture.ResolveRequest, fixture.Index, true));
            var second = filter.Apply(new MoonpalaceMandatoryBoundaryFilterRequest(
                fixture.ResolveRequest, reversed, true));
            Assert.That(second.AcceptedCandidates.Select(candidate => candidate.CandidateId),
                Is.EqualTo(first.AcceptedCandidates.Select(candidate => candidate.CandidateId)));
            Assert.That(second.AcceptedCandidates.Select(candidate => candidate.Signature),
                Is.EqualTo(first.AcceptedCandidates.Select(candidate => candidate.Signature)));
        }

        private static void AssertResolverInputBoundary(
            Fixture fixture,
            MoonpalaceMandatoryBoundaryFilterResult result)
        {
            Assert.That(result.AcceptedCandidateCount, Is.EqualTo(2));
            Assert.That(typeof(MoonpalaceMandatoryBoundaryFilterResult)
                .GetProperty("ResolvedCandidate"), Is.Null);
            Assert.That(result.FilteredCandidateIndex.Count, Is.EqualTo(2));

            var resolved = new MoonpalaceBoundaryChunkResolver().Resolve(
                result.FilteredCandidateIndex,
                fixture.ResolveRequest);
            Assert.That(resolved.IsSuccess, Is.True);
            Assert.That(new[] { "A-ALLOWED", "E-ALLOWED" },
                Does.Contain(resolved.ResolvedCandidate.Candidate.CandidateId));
        }

        private static void AssertSourceIsNotMutated(
            Fixture fixture,
            MoonpalaceMandatoryBoundaryFilterResult result)
        {
            var source = fixture.Candidates.ToDictionary(
                candidate => candidate.CandidateId,
                candidate => new
                {
                    candidate.Signature,
                    candidate.Weight,
                    candidate.Key,
                    candidate.MandatoryRouteAllowed,
                    candidate.ToolRequirement,
                },
                StringComparer.Ordinal);

            Assert.That(result.AcceptedCandidates[0], Is.SameAs(
                fixture.Candidates.Single(candidate => candidate.CandidateId == "A-ALLOWED")));
            foreach (var candidate in fixture.Candidates)
            {
                var expected = source[candidate.CandidateId];
                Assert.That(candidate.Signature, Is.EqualTo(expected.Signature));
                Assert.That(candidate.Weight, Is.EqualTo(expected.Weight));
                Assert.That(candidate.Key, Is.EqualTo(expected.Key));
                Assert.That(candidate.MandatoryRouteAllowed, Is.EqualTo(expected.MandatoryRouteAllowed));
                Assert.That(candidate.ToolRequirement, Is.EqualTo(expected.ToolRequirement));
            }
        }

        private static void AssertFailure(
            MoonpalaceMandatoryBoundaryFilterResult result,
            MoonpalaceMandatoryBoundaryFilterIssue expectedIssue)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IssueList, Is.EqualTo(new[] { expectedIssue }));
            Assert.That(result.AcceptedCandidates, Is.Empty);
        }

        private static MoonpalaceBoundaryResolveRequest CreateInvalidResolveRequest(
            Fixture fixture,
            int cycle)
        {
            var request = fixture.ResolveRequest;
            switch (cycle % 7)
            {
                case 0:
                    return new MoonpalaceBoundaryResolveRequest(
                        default, request.ToBiome, request.Profile, request.Orientation,
                        request.RouteRole, request.EdgeSignature, request.SelectionSeed);
                case 1:
                    return new MoonpalaceBoundaryResolveRequest(
                        request.FromBiome, default, request.Profile, request.Orientation,
                        request.RouteRole, request.EdgeSignature, request.SelectionSeed);
                case 2:
                    return new MoonpalaceBoundaryResolveRequest(
                        request.FromBiome, request.FromBiome, request.Profile, request.Orientation,
                        request.RouteRole, request.EdgeSignature, request.SelectionSeed);
                case 3:
                    return new MoonpalaceBoundaryResolveRequest(
                        request.FromBiome, request.ToBiome, default, request.Orientation,
                        request.RouteRole, request.EdgeSignature, request.SelectionSeed);
                case 4:
                    return new MoonpalaceBoundaryResolveRequest(
                        request.FromBiome, request.ToBiome, request.Profile,
                        (MoonpalaceBoundaryOrientation)99, request.RouteRole,
                        request.EdgeSignature, request.SelectionSeed);
                case 5:
                    return new MoonpalaceBoundaryResolveRequest(
                        request.FromBiome, request.ToBiome, request.Profile, request.Orientation,
                        default, request.EdgeSignature, request.SelectionSeed);
                default:
                    return new MoonpalaceBoundaryResolveRequest(
                        request.FromBiome, request.ToBiome, request.Profile, request.Orientation,
                        request.RouteRole, default, request.SelectionSeed);
            }
        }

        private static Fixture CreateFixture(int cycle)
        {
            var pair = MoonpalaceBiomePairCatalog.Canonical.Pairs[
                cycle % MoonpalaceBiomePairCatalog.Canonical.Pairs.Count];
            var profile = new MoonpalaceBoundaryProfileId("Dense");
            var role = new MoonpalaceBoundaryRouteRole("Mandatory");
            var signature = new MoonpalaceBoundaryEdgeSignature("SIG-MANDATORY");
            var candidates = new List<MoonpalaceBoundaryCandidateDefinition>
            {
                CreateCandidate("D-BOTH", pair, profile, role, signature, 11, false,
                    MoonpalaceBoundaryToolRequirement.Rope),
                CreateCandidate("B-TOOL", pair, profile, role, signature, 5, true,
                    MoonpalaceBoundaryToolRequirement.Pickaxe),
                CreateCandidate("E-ALLOWED", pair, profile, role, signature, 1, true,
                    MoonpalaceBoundaryToolRequirement.None),
                CreateCandidate("C-BLOCKED", pair, profile, role, signature, 7, false,
                    MoonpalaceBoundaryToolRequirement.None),
                CreateCandidate("A-ALLOWED", pair, profile, role, signature, 3, true,
                    MoonpalaceBoundaryToolRequirement.None),
            };
            var index = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(candidates);
            var resolveRequest = new MoonpalaceBoundaryResolveRequest(
                pair.First,
                pair.Second,
                profile,
                MoonpalaceBoundaryOrientation.Horizontal,
                role,
                signature,
                (ulong)cycle);
            return new Fixture(candidates, index, resolveRequest);
        }

        private static MoonpalaceBoundaryCandidateDefinition CreateCandidate(
            string candidateId,
            MoonpalaceBiomePair pair,
            MoonpalaceBoundaryProfileId profile,
            MoonpalaceBoundaryRouteRole role,
            MoonpalaceBoundaryEdgeSignature signature,
            int weight,
            bool mandatoryRouteAllowed,
            MoonpalaceBoundaryToolRequirement toolRequirement)
        {
            return new MoonpalaceBoundaryCandidateDefinition(
                candidateId,
                pair,
                profile,
                MoonpalaceBoundaryOrientation.Horizontal,
                role,
                signature,
                weight,
                mandatoryRouteAllowed,
                toolRequirement,
                MoonpalaceBoundaryWarningMarker.Tile |
                MoonpalaceBoundaryWarningMarker.Background);
        }

        private sealed class Fixture
        {
            public Fixture(
                List<MoonpalaceBoundaryCandidateDefinition> candidates,
                MoonpalaceBoundaryCandidateIndex index,
                MoonpalaceBoundaryResolveRequest resolveRequest)
            {
                Candidates = candidates;
                Index = index;
                ResolveRequest = resolveRequest;
            }

            public List<MoonpalaceBoundaryCandidateDefinition> Candidates { get; }
            public MoonpalaceBoundaryCandidateIndex Index { get; }
            public MoonpalaceBoundaryResolveRequest ResolveRequest { get; }
        }
    }
}
