using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries
{
    [Category("MAP08_14")]
    [Category("MAP08_14_COMPATIBILITY")]
    public sealed class MoonpalaceBoundaryPhaseExitCompatibilityTests
    {
        private MoonpalaceBoundaryPhaseExitFixture fixture;

        public static IEnumerable<TestCaseData> CompatibilityCases
        {
            get
            {
                for (var caseId = 0; caseId < 300; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("MoonpalaceBoundaryPhaseExitCompatibility_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadApprovedEvidence()
        {
            fixture = MoonpalaceBoundaryPhaseExitFixture.GetOrCreate();
        }

        [TestCaseSource(nameof(CompatibilityCases))]
        public void MoonpalaceBoundaryPhaseExitCompatibilityContract(int caseId)
        {
            switch (caseId % 15)
            {
                case 0:
                    ForEveryCandidate((candidate, forward, reverse) =>
                    {
                        Assert.That(forward.IsSuccess, Is.True, candidate.CandidateId + " forward");
                        Assert.That(reverse.IsSuccess, Is.True, candidate.CandidateId + " reverse");
                    });
                    break;
                case 1:
                    ForEveryCandidate((candidate, forward, reverse) =>
                    {
                        Assert.That(forward.ResolvedCandidate.Candidate.CandidateId,
                            Is.EqualTo(candidate.CandidateId));
                        Assert.That(reverse.ResolvedCandidate.Candidate.CandidateId,
                            Is.EqualTo(candidate.CandidateId));
                    });
                    break;
                case 2:
                    ForEveryCandidate((candidate, forward, reverse) =>
                    {
                        Assert.That(forward.ResolvedCandidate.CanonicalPair,
                            Is.EqualTo(reverse.ResolvedCandidate.CanonicalPair), candidate.CandidateId);
                        Assert.That(forward.ResolvedCandidate.SelectedKey,
                            Is.EqualTo(reverse.ResolvedCandidate.SelectedKey), candidate.CandidateId);
                    });
                    break;
                case 3:
                    ForEveryCandidate((candidate, forward, reverse) =>
                    {
                        var first = forward.ResolvedCandidate.Candidate;
                        var second = reverse.ResolvedCandidate.Candidate;
                        Assert.That(first.Profile, Is.EqualTo(second.Profile), candidate.CandidateId);
                        Assert.That(first.Orientation, Is.EqualTo(second.Orientation), candidate.CandidateId);
                        Assert.That(first.RouteRole, Is.EqualTo(second.RouteRole), candidate.CandidateId);
                        Assert.That(first.EdgeSignature, Is.EqualTo(second.EdgeSignature), candidate.CandidateId);
                    });
                    break;
                case 4:
                    ForEveryCandidate((candidate, forward, reverse) =>
                    {
                        Assert.That(forward.ResolvedCandidate.RequestDirection,
                            Is.EqualTo(MoonpalaceBoundaryRequestDirection.Forward));
                        Assert.That(forward.ResolvedCandidate.TransformPolicy.Transform,
                            Is.EqualTo(MicrochunkTransform.R0), candidate.CandidateId);
                    });
                    break;
                case 5:
                    ForEveryCandidate((candidate, forward, reverse) =>
                    {
                        var expected = candidate.Orientation == MoonpalaceBoundaryOrientation.Horizontal
                            ? MicrochunkTransform.MirrorX
                            : MicrochunkTransform.MirrorY;
                        Assert.That(reverse.ResolvedCandidate.RequestDirection,
                            Is.EqualTo(MoonpalaceBoundaryRequestDirection.Reverse));
                        Assert.That(reverse.ResolvedCandidate.TransformPolicy.Transform,
                            Is.EqualTo(expected), candidate.CandidateId);
                    });
                    break;
                case 6:
                    ForEveryCandidate((candidate, forward, reverse) =>
                    {
                        Assert.That(fixture.GetDefinition(candidate).CandidateId,
                            Is.EqualTo(candidate.CandidateId));
                        Assert.That(candidate.MicrochunkId, Is.Not.Null.And.Not.Empty);
                        Assert.That(candidate.CandidateId, Is.Not.Null.And.Not.Empty);
                    });
                    break;
                case 7:
                    AssertEveryEdgeSignature();
                    break;
                case 8:
                    AssertEverySocketPairSymmetric();
                    break;
                case 9:
                    AssertEverySocketTraversalExact();
                    break;
                case 10:
                    AssertEveryMandatoryCandidateNeedsNoTool();
                    break;
                case 11:
                    AssertEveryWarningRequirement();
                    break;
                case 12:
                    AssertEveryWarningProbeDirection();
                    break;
                case 13:
                    AssertWarningEvidenceReferencesEnteringBiome();
                    break;
                case 14:
                    AssertLayerBoundaryIsVerticalOnly();
                    break;
                default:
                    Assert.Fail("Unexpected MAP08_14 compatibility contract case.");
                    break;
            }
        }

        private void ForEveryCandidate(
            Action<MoonpalaceBoundaryCoverageCandidateEvidence,
                MoonpalaceBoundaryResolveResult,
                MoonpalaceBoundaryResolveResult> assertion)
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                assertion(candidate, fixture.Resolve(candidate, false), fixture.Resolve(candidate, true));
            }
        }

        private void AssertEveryEdgeSignature()
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                var expected = candidate.Orientation == MoonpalaceBoundaryOrientation.Horizontal
                    ? "EDGE_H_MID_WALK"
                    : "EDGE_V_CENTER_CLIMB";
                Assert.That(candidate.EntryEdgeSignatureId, Is.EqualTo(expected), candidate.CandidateId);
                Assert.That(candidate.ExitEdgeSignatureId, Is.EqualTo(expected), candidate.CandidateId);
                Assert.That(candidate.Sockets.All(value => value.EdgeSignatureId == expected),
                    Is.True, candidate.CandidateId);
            }
        }

        private void AssertEverySocketPairSymmetric()
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                var expectedSides = candidate.Orientation == MoonpalaceBoundaryOrientation.Horizontal
                    ? new[] { "L", "R" }
                    : new[] { "D", "U" };
                Assert.That(candidate.Sockets.Count, Is.EqualTo(2), candidate.CandidateId);
                Assert.That(candidate.Sockets.Select(value => value.Side).OrderBy(value => value, StringComparer.Ordinal),
                    Is.EqualTo(expectedSides), candidate.CandidateId);
            }
        }

        private void AssertEverySocketTraversalExact()
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                var expected = candidate.Orientation == MoonpalaceBoundaryOrientation.Horizontal ? "WALK" : "CLIMB";
                Assert.That(candidate.Sockets.All(value => value.TraversalKind == expected),
                    Is.True, candidate.CandidateId);
                Assert.That(candidate.Sockets.Select(value => value.TraversalKind).Distinct().Count(),
                    Is.EqualTo(1), candidate.CandidateId);
            }
        }

        private void AssertEveryMandatoryCandidateNeedsNoTool()
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                Assert.That(candidate.MandatoryAllowed, Is.True, candidate.CandidateId);
                Assert.That(candidate.ToolRequirement, Is.EqualTo("NONE"), candidate.CandidateId);
                Assert.That(candidate.RouteType, Is.EqualTo(1), candidate.CandidateId);
                Assert.That(candidate.Sockets.All(value =>
                    value.MandatoryAllowed && value.RouteLayer == "MANDATORY" && value.ToolRequirement == "NONE"),
                    Is.True, candidate.CandidateId);
            }
        }

        private void AssertEveryWarningRequirement()
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                foreach (var reverse in new[] { false, true })
                {
                    var request = fixture.CreateRequest(candidate, reverse);
                    var requirement = MoonpalaceBoundaryWarningRequirement.Create(
                        request, fixture.GetDefinition(candidate));
                    Assert.That(requirement.WarningMicrochunksMinimum, Is.EqualTo(2), candidate.CandidateId);
                    Assert.That(requirement.RequiredDistinctMarkerCategories, Is.EqualTo(2), candidate.CandidateId);
                    Assert.That(requirement.AllowedMarkerCategories.Select(value => value.Token), Is.EqualTo(new[]
                    {
                        "Tile", "Background", "Resource", "Audio",
                    }), candidate.CandidateId);
                }
            }
        }

        private void AssertEveryWarningProbeDirection()
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                var forward = fixture.ProbeWarning(candidate, false);
                var reverse = fixture.ProbeWarning(candidate, true);
                Assert.That(forward.Accepted, Is.True, candidate.CandidateId + " forward");
                Assert.That(reverse.Accepted, Is.True, candidate.CandidateId + " reverse");
                Assert.That(forward.ObservedDistinctMarkerCategoryCount, Is.EqualTo(2));
                Assert.That(reverse.ObservedDistinctMarkerCategoryCount, Is.EqualTo(2));
                Assert.That(forward.ObservedMarkerCategories.Select(value => value.Token),
                    Is.EqualTo(reverse.ObservedMarkerCategories.Select(value => value.Token)));
            }
        }

        private void AssertWarningEvidenceReferencesEnteringBiome()
        {
            foreach (var candidate in fixture.Evidence.Candidates)
            {
                Assert.That(fixture.CountEnteringBiomeEvidenceCategories(candidate, candidate.BiomeBId),
                    Is.GreaterThanOrEqualTo(2), candidate.CandidateId + " A->B");
                Assert.That(fixture.CountEnteringBiomeEvidenceCategories(candidate, candidate.BiomeAId),
                    Is.GreaterThanOrEqualTo(2), candidate.CandidateId + " B->A");
            }
        }

        private void AssertLayerBoundaryIsVerticalOnly()
        {
            var layers = fixture.Evidence.Candidates.Where(value => value.ProfileId == "BOUND_LAYER").ToArray();
            Assert.That(layers.Length, Is.EqualTo(3));
            Assert.That(layers.All(value => value.Orientation == MoonpalaceBoundaryOrientation.Vertical), Is.True);
            Assert.That(fixture.Evidence.Candidates.Any(value =>
                value.ProfileId == "BOUND_LAYER" && value.Orientation == MoonpalaceBoundaryOrientation.Horizontal),
                Is.False);
        }
    }
}
