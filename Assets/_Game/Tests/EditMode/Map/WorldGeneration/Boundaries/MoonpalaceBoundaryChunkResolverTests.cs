using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.Tests.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryChunkResolverTests
    {
        public static IEnumerable ResolverCases
        {
            get
            {
                for (var index = 0; index < 420; index++)
                {
                    yield return new TestCaseData(index)
                        .SetName("BoundaryChunkResolverContract_" + index.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(ResolverCases))]
        public void BoundaryChunkResolverContract(int caseIndex)
        {
            var cycle = caseIndex / 21;
            var fixture = CreateFixture(cycle);
            var resolver = new MoonpalaceBoundaryChunkResolver();
            var forwardRequest = CreateRequest(fixture, false, MoonpalaceBoundaryOrientation.Horizontal,
                fixture.SignatureA, (ulong)cycle);

            switch (caseIndex % 21)
            {
                case 0:
                    AssertRequestFields(forwardRequest, fixture, cycle);
                    break;
                case 1:
                    AssertForwardSuccess(resolver.Resolve(fixture.Index, forwardRequest), fixture);
                    break;
                case 2:
                    var reverseResult = resolver.Resolve(fixture.Index,
                        CreateRequest(fixture, true, MoonpalaceBoundaryOrientation.Horizontal,
                            fixture.SignatureA, (ulong)cycle));
                    Assert.That(reverseResult.IsSuccess, Is.True);
                    Assert.That(reverseResult.ResolvedCandidate.RequestDirection,
                        Is.EqualTo(MoonpalaceBoundaryRequestDirection.Reverse));
                    Assert.That(reverseResult.ResolvedCandidate.CanonicalPair, Is.EqualTo(fixture.Pair));
                    break;
                case 3:
                    var horizontalReverse = resolver.Resolve(fixture.Index,
                        CreateRequest(fixture, true, MoonpalaceBoundaryOrientation.Horizontal,
                            fixture.SignatureA, (ulong)cycle));
                    Assert.That(horizontalReverse.ResolvedCandidate.TransformPolicy.Transform,
                        Is.EqualTo(MicrochunkTransform.MirrorX));
                    break;
                case 4:
                    var verticalReverse = resolver.Resolve(fixture.Index,
                        CreateRequest(fixture, true, MoonpalaceBoundaryOrientation.Vertical,
                            fixture.SignatureB, (ulong)cycle));
                    Assert.That(verticalReverse.IsSuccess, Is.True);
                    Assert.That(verticalReverse.ResolvedCandidate.TransformPolicy.Transform,
                        Is.EqualTo(MicrochunkTransform.MirrorY));
                    break;
                case 5:
                    var first = resolver.Resolve(fixture.Index, forwardRequest);
                    var second = resolver.Resolve(fixture.Index, forwardRequest);
                    Assert.That(first.ResolvedCandidate.Candidate.CandidateId,
                        Is.EqualTo(second.ResolvedCandidate.Candidate.CandidateId));
                    Assert.That(first.ResolvedCandidate.Candidate.Signature,
                        Is.EqualTo(second.ResolvedCandidate.Candidate.Signature));
                    break;
                case 6:
                    AssertSourceOrderIndependent(fixture, resolver);
                    break;
                case 7:
                    AssertWeightedSelectionUsesSeedRange(fixture, resolver);
                    break;
                case 8:
                    AssertZeroWeightNeverWinsAgainstPositive(fixture, resolver);
                    break;
                case 9:
                    AssertAllZeroUsesStableTieBreak(fixture, resolver);
                    break;
                case 10:
                    var missing = resolver.Resolve(fixture.Index,
                        new MoonpalaceBoundaryResolveRequest(
                            fixture.Pair.First, fixture.Pair.Second,
                            new MoonpalaceBoundaryProfileId("Missing"),
                            MoonpalaceBoundaryOrientation.Horizontal,
                            fixture.RouteRole, fixture.SignatureA, (ulong)cycle));
                    AssertFailure(missing, MoonpalaceBoundaryResolveIssue.NoCandidates);
                    break;
                case 11:
                    AssertFailure(resolver.Resolve(null, forwardRequest),
                        MoonpalaceBoundaryResolveIssue.MissingIndex);
                    break;
                case 12:
                    AssertFailure(resolver.Resolve(fixture.Index, null),
                        MoonpalaceBoundaryResolveIssue.MissingRequest);
                    break;
                case 13:
                    AssertFailure(resolver.Resolve(fixture.Index, new MoonpalaceBoundaryResolveRequest(
                            default, fixture.Pair.Second, fixture.Profile,
                            MoonpalaceBoundaryOrientation.Horizontal, fixture.RouteRole,
                            fixture.SignatureA, (ulong)cycle)),
                        MoonpalaceBoundaryResolveIssue.InvalidFromBiome);
                    break;
                case 14:
                    AssertFailure(resolver.Resolve(fixture.Index, new MoonpalaceBoundaryResolveRequest(
                            fixture.Pair.First, default, fixture.Profile,
                            MoonpalaceBoundaryOrientation.Horizontal, fixture.RouteRole,
                            fixture.SignatureA, (ulong)cycle)),
                        MoonpalaceBoundaryResolveIssue.InvalidToBiome);
                    break;
                case 15:
                    AssertFailure(resolver.Resolve(fixture.Index, new MoonpalaceBoundaryResolveRequest(
                            fixture.Pair.First, fixture.Pair.First, fixture.Profile,
                            MoonpalaceBoundaryOrientation.Horizontal, fixture.RouteRole,
                            fixture.SignatureA, (ulong)cycle)),
                        MoonpalaceBoundaryResolveIssue.SelfPair);
                    break;
                case 16:
                    AssertFailure(resolver.Resolve(fixture.Index, new MoonpalaceBoundaryResolveRequest(
                            fixture.Pair.First, fixture.Pair.Second, default,
                            MoonpalaceBoundaryOrientation.Horizontal, fixture.RouteRole,
                            fixture.SignatureA, (ulong)cycle)),
                        MoonpalaceBoundaryResolveIssue.InvalidProfile);
                    break;
                case 17:
                    AssertFailure(resolver.Resolve(fixture.Index, new MoonpalaceBoundaryResolveRequest(
                            fixture.Pair.First, fixture.Pair.Second, fixture.Profile,
                            (MoonpalaceBoundaryOrientation)99, fixture.RouteRole,
                            fixture.SignatureA, (ulong)cycle)),
                        MoonpalaceBoundaryResolveIssue.InvalidOrientation);
                    break;
                case 18:
                    AssertFailure(resolver.Resolve(fixture.Index, new MoonpalaceBoundaryResolveRequest(
                            fixture.Pair.First, fixture.Pair.Second, fixture.Profile,
                            MoonpalaceBoundaryOrientation.Horizontal, default,
                            fixture.SignatureA, (ulong)cycle)),
                        MoonpalaceBoundaryResolveIssue.InvalidRouteRole);
                    break;
                case 19:
                    AssertFailure(resolver.Resolve(fixture.Index, new MoonpalaceBoundaryResolveRequest(
                            fixture.Pair.First, fixture.Pair.Second, fixture.Profile,
                            MoonpalaceBoundaryOrientation.Horizontal, fixture.RouteRole,
                            default, (ulong)cycle)),
                        MoonpalaceBoundaryResolveIssue.InvalidEdgeSignature);
                    break;
                case 20:
                    AssertCandidateDataIsNotMutated(fixture, resolver, cycle);
                    break;
                default:
                    Assert.Fail("Unexpected resolver contract case.");
                    break;
            }
        }

        private static void AssertRequestFields(
            MoonpalaceBoundaryResolveRequest request,
            Fixture fixture,
            int cycle)
        {
            Assert.That(request.FromBiome, Is.EqualTo(fixture.Pair.First));
            Assert.That(request.ToBiome, Is.EqualTo(fixture.Pair.Second));
            Assert.That(request.Profile, Is.EqualTo(fixture.Profile));
            Assert.That(request.Orientation, Is.EqualTo(MoonpalaceBoundaryOrientation.Horizontal));
            Assert.That(request.RouteRole, Is.EqualTo(fixture.RouteRole));
            Assert.That(request.EdgeSignature, Is.EqualTo(fixture.SignatureA));
            Assert.That(request.SelectionSeed, Is.EqualTo((ulong)cycle));
        }

        private static void AssertForwardSuccess(
            MoonpalaceBoundaryResolveResult result,
            Fixture fixture)
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Issue, Is.EqualTo(MoonpalaceBoundaryResolveIssue.None));
            Assert.That(result.ResolvedCandidate, Is.Not.Null);
            Assert.That(result.ResolvedCandidate.CanonicalPair, Is.EqualTo(fixture.Pair));
            Assert.That(result.ResolvedCandidate.RequestDirection,
                Is.EqualTo(MoonpalaceBoundaryRequestDirection.Forward));
            Assert.That(result.ResolvedCandidate.TransformPolicy.Transform,
                Is.EqualTo(MicrochunkTransform.R0));
            Assert.That(result.ResolvedCandidate.SelectedKey, Is.EqualTo(fixture.HorizontalKey));
        }

        private static void AssertSourceOrderIndependent(Fixture fixture, MoonpalaceBoundaryChunkResolver resolver)
        {
            var reversedIndex = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(
                fixture.Candidates.AsEnumerable().Reverse());
            for (ulong seed = 0; seed < 32; seed++)
            {
                var request = CreateRequest(fixture, false, MoonpalaceBoundaryOrientation.Horizontal,
                    fixture.SignatureA, seed);
                var expected = resolver.Resolve(fixture.Index, request);
                var actual = resolver.Resolve(reversedIndex, request);
                Assert.That(actual.ResolvedCandidate.Candidate.CandidateId,
                    Is.EqualTo(expected.ResolvedCandidate.Candidate.CandidateId));
            }
        }

        private static void AssertWeightedSelectionUsesSeedRange(
            Fixture fixture,
            MoonpalaceBoundaryChunkResolver resolver)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (ulong seed = 0; seed < 128; seed++)
            {
                var result = resolver.Resolve(fixture.Index,
                    CreateRequest(fixture, false, MoonpalaceBoundaryOrientation.Horizontal,
                        fixture.SignatureA, seed));
                var id = result.ResolvedCandidate.Candidate.CandidateId;
                counts[id] = counts.TryGetValue(id, out var count) ? count + 1 : 1;
            }

            Assert.That(counts.Keys, Is.EquivalentTo(new[] { "C-01", "C-02" }));
            Assert.That(counts["C-01"], Is.GreaterThan(counts["C-02"]));
        }

        private static void AssertZeroWeightNeverWinsAgainstPositive(
            Fixture fixture,
            MoonpalaceBoundaryChunkResolver resolver)
        {
            for (ulong seed = 0; seed < 128; seed++)
            {
                var result = resolver.Resolve(fixture.Index,
                    CreateRequest(fixture, false, MoonpalaceBoundaryOrientation.Horizontal,
                        fixture.SignatureA, seed));
                Assert.That(result.ResolvedCandidate.Candidate.CandidateId, Is.Not.EqualTo("C-03"));
            }
        }

        private static void AssertAllZeroUsesStableTieBreak(
            Fixture fixture,
            MoonpalaceBoundaryChunkResolver resolver)
        {
            var zeroA = CreateCandidate("Z-02", fixture.Pair, fixture.Profile,
                MoonpalaceBoundaryOrientation.Horizontal, fixture.RouteRole, fixture.SignatureA, 0);
            var zeroB = CreateCandidate("Z-01", fixture.Pair, fixture.Profile,
                MoonpalaceBoundaryOrientation.Horizontal, fixture.RouteRole, fixture.SignatureA, 0);
            var zeroIndex = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(new[] { zeroA, zeroB });
            for (ulong seed = 0; seed < 16; seed++)
            {
                var result = resolver.Resolve(zeroIndex,
                    CreateRequest(fixture, false, MoonpalaceBoundaryOrientation.Horizontal,
                        fixture.SignatureA, seed));
                Assert.That(result.ResolvedCandidate.Candidate.CandidateId, Is.EqualTo("Z-01"));
            }
        }

        private static void AssertCandidateDataIsNotMutated(
            Fixture fixture,
            MoonpalaceBoundaryChunkResolver resolver,
            int cycle)
        {
            var before = fixture.Candidates.ToDictionary(candidate => candidate.CandidateId,
                candidate => candidate.Signature, StringComparer.Ordinal);
            var request = CreateRequest(fixture, true, MoonpalaceBoundaryOrientation.Horizontal,
                fixture.SignatureA, (ulong)cycle);
            var result = resolver.Resolve(fixture.Index, request);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.ResolvedCandidate.Candidate.EdgeSignature, Is.EqualTo(request.EdgeSignature));
            Assert.That(result.ResolvedCandidate.SelectedKey.EdgeSignature, Is.EqualTo(request.EdgeSignature));
            Assert.That(fixture.Candidates.ToDictionary(candidate => candidate.CandidateId,
                    candidate => candidate.Signature, StringComparer.Ordinal),
                Is.EqualTo(before));
        }

        private static void AssertFailure(
            MoonpalaceBoundaryResolveResult result,
            MoonpalaceBoundaryResolveIssue expectedIssue)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Issue, Is.EqualTo(expectedIssue));
            Assert.That(result.ResolvedCandidate, Is.Null);
        }

        private static Fixture CreateFixture(int cycle)
        {
            var catalog = MoonpalaceBiomePairCatalog.Canonical;
            var pair = catalog.Pairs[cycle % catalog.Pairs.Count];
            var profile = new MoonpalaceBoundaryProfileId("Dense");
            var routeRole = new MoonpalaceBoundaryRouteRole("Traversal");
            var signatureA = new MoonpalaceBoundaryEdgeSignature("SIG-A");
            var signatureB = new MoonpalaceBoundaryEdgeSignature("SIG-B");
            var candidates = new List<MoonpalaceBoundaryCandidateDefinition>
            {
                CreateCandidate("C-03", pair, profile, MoonpalaceBoundaryOrientation.Horizontal,
                    routeRole, signatureA, 0),
                CreateCandidate("C-02", pair, profile, MoonpalaceBoundaryOrientation.Horizontal,
                    routeRole, signatureA, 1),
                CreateCandidate("C-01", pair, profile, MoonpalaceBoundaryOrientation.Horizontal,
                    routeRole, signatureA, 3),
                CreateCandidate("C-04", pair, profile, MoonpalaceBoundaryOrientation.Vertical,
                    routeRole, signatureB, 2),
            };
            return new Fixture(pair, profile, routeRole, signatureA, signatureB, candidates,
                MoonpalaceBoundaryCandidateIndexer.Canonical.Build(candidates));
        }

        private static MoonpalaceBoundaryResolveRequest CreateRequest(
            Fixture fixture,
            bool reverse,
            MoonpalaceBoundaryOrientation orientation,
            MoonpalaceBoundaryEdgeSignature edgeSignature,
            ulong seed)
        {
            return new MoonpalaceBoundaryResolveRequest(
                reverse ? fixture.Pair.Second : fixture.Pair.First,
                reverse ? fixture.Pair.First : fixture.Pair.Second,
                fixture.Profile,
                orientation,
                fixture.RouteRole,
                edgeSignature,
                seed);
        }

        private static MoonpalaceBoundaryCandidateDefinition CreateCandidate(
            string id,
            MoonpalaceBiomePair pair,
            MoonpalaceBoundaryProfileId profile,
            MoonpalaceBoundaryOrientation orientation,
            MoonpalaceBoundaryRouteRole role,
            MoonpalaceBoundaryEdgeSignature signature,
            int weight)
        {
            return new MoonpalaceBoundaryCandidateDefinition(
                id, pair, profile, orientation, role, signature, weight, true,
                MoonpalaceBiomePairDefinition.NoToolRequirement,
                MoonpalaceBoundaryWarningMarker.Tile | MoonpalaceBoundaryWarningMarker.Background);
        }

        private sealed class Fixture
        {
            public Fixture(
                MoonpalaceBiomePair pair,
                MoonpalaceBoundaryProfileId profile,
                MoonpalaceBoundaryRouteRole routeRole,
                MoonpalaceBoundaryEdgeSignature signatureA,
                MoonpalaceBoundaryEdgeSignature signatureB,
                List<MoonpalaceBoundaryCandidateDefinition> candidates,
                MoonpalaceBoundaryCandidateIndex index)
            {
                Pair = pair;
                Profile = profile;
                RouteRole = routeRole;
                SignatureA = signatureA;
                SignatureB = signatureB;
                Candidates = candidates;
                Index = index;
                HorizontalKey = candidates[0].Key;
            }

            public MoonpalaceBiomePair Pair { get; }
            public MoonpalaceBoundaryProfileId Profile { get; }
            public MoonpalaceBoundaryRouteRole RouteRole { get; }
            public MoonpalaceBoundaryEdgeSignature SignatureA { get; }
            public MoonpalaceBoundaryEdgeSignature SignatureB { get; }
            public List<MoonpalaceBoundaryCandidateDefinition> Candidates { get; }
            public MoonpalaceBoundaryCandidateIndex Index { get; }
            public MoonpalaceBoundaryCandidateKey HorizontalKey { get; }
        }
    }
}
