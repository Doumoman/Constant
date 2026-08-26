using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries
{
    [Category("MAP08_14")]
    [Category("MAP08_14_DETERMINISM")]
    public sealed class MoonpalaceBoundaryPhaseExitDeterminismTests
    {
        private const string ExpectedDigest =
            "f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68";

        private MoonpalaceBoundaryPhaseExitFixture fixture;

        public static IEnumerable<TestCaseData> DeterminismCases
        {
            get
            {
                for (var caseId = 0; caseId < 240; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("MoonpalaceBoundaryPhaseExitDeterminism_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadApprovedEvidence()
        {
            fixture = MoonpalaceBoundaryPhaseExitFixture.GetOrCreate();
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void MoonpalaceBoundaryPhaseExitDeterminismContract(int caseId)
        {
            var variant = caseId / 12;
            var candidate = fixture.Evidence.Candidates[(variant * 7) % fixture.Evidence.Candidates.Count];

            switch (caseId % 12)
            {
                case 0:
                    var repeated = new MoonpalaceBoundaryCoverageValidator().Validate(
                        fixture.Evidence.Requirements,
                        fixture.Evidence.Candidates,
                        fixture.Evidence.SourceChain);
                    Assert.That(repeated.Accepted, Is.True);
                    Assert.That(repeated.StableDigest, Is.EqualTo(ExpectedDigest));
                    break;
                case 1:
                    var reordered = new MoonpalaceBoundaryCoverageValidator().Validate(
                        fixture.Evidence.Requirements.OrderByDescending(value => value.PairRuleId, StringComparer.Ordinal),
                        fixture.Evidence.Candidates.OrderByDescending(value => value.CandidateId, StringComparer.Ordinal),
                        fixture.Evidence.SourceChain);
                    Assert.That(reordered.StableDigest, Is.EqualTo(ExpectedDigest));
                    break;
                case 2:
                    var rebuilt = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(
                        fixture.CandidateDefinitions.Reverse());
                    Assert.That(rebuilt.Candidates.Select(value => value.Signature),
                        Is.EqualTo(fixture.CandidateIndex.Candidates.Select(value => value.Signature)));
                    break;
                case 3:
                    AssertResolveStable(candidate, false);
                    break;
                case 4:
                    AssertResolveStable(candidate, true);
                    break;
                case 5:
                    var forward = fixture.Resolve(candidate, false, (ulong)variant);
                    var reverse = fixture.Resolve(candidate, true, (ulong)variant);
                    Assert.That(forward.ResolvedCandidate.Candidate.Signature,
                        Is.EqualTo(reverse.ResolvedCandidate.Candidate.Signature));
                    Assert.That(forward.ResolvedCandidate.SelectedKey,
                        Is.EqualTo(reverse.ResolvedCandidate.SelectedKey));
                    break;
                case 6:
                    AssertTransformCoordinates(candidate);
                    break;
                case 7:
                    var firstProbe = fixture.ProbeWarning(candidate, false);
                    var secondProbe = fixture.ProbeWarning(candidate, false);
                    Assert.That(firstProbe.Accepted, Is.True);
                    Assert.That(secondProbe.Accepted, Is.True);
                    Assert.That(firstProbe.ObservedMarkerCategories.Select(value => value.Token),
                        Is.EqualTo(secondProbe.ObservedMarkerCategories.Select(value => value.Token)));
                    break;
                case 8:
                    AssertCoverageProjectionSourceStable();
                    break;
                case 9:
                    AssertPreviewProjection(CapturePreviewProjection());
                    break;
                case 10:
                    var firstPreview = CapturePreviewProjection();
                    var secondPreview = CapturePreviewProjection();
                    Assert.That(secondPreview.Signature, Is.EqualTo(firstPreview.Signature));
                    break;
                case 11:
                    var preview = CapturePreviewProjection();
                    Assert.That(preview.OverlayCategories, Is.EqualTo(new[]
                    {
                        "Foreground", "Background", "Route", "Sockets", "Warnings", "BoundaryLayer", "Issues",
                    }));
                    Assert.That(preview.PairRows.All(value =>
                        value.ForwardTransition == value.BiomeAId + " -> " + value.BiomeBId &&
                        value.ReverseTransition == value.BiomeBId + " -> " + value.BiomeAId), Is.True);
                    break;
                default:
                    Assert.Fail("Unexpected MAP08_14 determinism contract case.");
                    break;
            }
        }

        private void AssertResolveStable(
            MoonpalaceBoundaryCoverageCandidateEvidence candidate,
            bool reverse)
        {
            var expected = fixture.Resolve(candidate, reverse, 0UL);
            Assert.That(expected.IsSuccess, Is.True, candidate.CandidateId);
            foreach (var seed in new[] { 0UL, 1UL, 4660UL, ulong.MaxValue })
            {
                var actual = fixture.Resolve(candidate, reverse, seed);
                Assert.That(actual.IsSuccess, Is.True, candidate.CandidateId);
                Assert.That(actual.ResolvedCandidate.Candidate.CandidateId,
                    Is.EqualTo(expected.ResolvedCandidate.Candidate.CandidateId));
                Assert.That(actual.ResolvedCandidate.TransformPolicy.Signature,
                    Is.EqualTo(expected.ResolvedCandidate.TransformPolicy.Signature));
            }
        }

        private static void AssertTransformCoordinates(MoonpalaceBoundaryCoverageCandidateEvidence candidate)
        {
            var direction = MoonpalaceBoundaryTransformPolicy.Create(
                MoonpalaceBoundaryRequestDirection.Reverse,
                candidate.Orientation);
            var transformed = candidate.TileCells.Select(value =>
            {
                Assert.That(MicrochunkLocalCoord.TryCreate(value.LocalX, value.LocalY, out var source), Is.True);
                var target = MicrochunkTransformUtility.TransformCoordinate(source, direction.Transform);
                return target.Y * 12 + target.X;
            }).ToArray();
            Assert.That(transformed.Distinct().Count(), Is.EqualTo(96), candidate.CandidateId);
            Assert.That(transformed.OrderBy(value => value), Is.EqualTo(Enumerable.Range(0, 96)));
        }

        private void AssertCoverageProjectionSourceStable()
        {
            var report = fixture.Evidence.Report;
            Assert.That(report.Accepted, Is.True);
            Assert.That(report.StableDigest, Is.EqualTo(ExpectedDigest));
            Assert.That(report.AuthoringManifestSha256,
                Is.EqualTo(MoonpalaceBoundaryCoverageValidator.ExpectedAuthoringManifestSha256));
            Assert.That(new[]
            {
                report.PairReportCount,
                report.CandidateCountTotal,
                report.MicrochunkCountTotal,
                report.TileRowCountTotal,
                report.SocketRowCountTotal,
            }, Is.EqualTo(new[] { 6, 31, 31, 2976, 62 }));
        }

        private static PreviewProjection CapturePreviewProjection()
        {
            var viewModelType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(value => value.GetType(
                    "StarNight.MapAuthoring.Boundaries.MoonpalaceBoundaryPreviewViewModel", false))
                .FirstOrDefault(value => value != null);
            Assert.That(viewModelType, Is.Not.Null, "MAP08_13 preview ViewModel type must remain available.");
            var load = viewModelType.GetMethod(
                "LoadApprovedAuthoring",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(bool) },
                null);
            Assert.That(load, Is.Not.Null);
            var viewModel = load.Invoke(null, new object[] { false });
            Assert.That(viewModel, Is.Not.Null);
            var currentReport = GetProperty(viewModel, "CurrentReport");
            var pairRows = ((IEnumerable)GetProperty(currentReport, "PairRows"))
                .Cast<object>()
                .Select(value => new PreviewPairRow(
                    (string)GetProperty(value, "PairRuleId"),
                    (string)GetProperty(value, "BiomeAId"),
                    (string)GetProperty(value, "BiomeBId"),
                    (string)GetProperty(value, "ForwardTransition"),
                    (string)GetProperty(value, "ReverseTransition"),
                    (int)GetProperty(value, "CandidateCount"),
                    (int)GetProperty(value, "MicrochunkCount"),
                    (int)GetProperty(value, "TileRowCount"),
                    (int)GetProperty(value, "SocketRowCount")))
                .ToArray();
            var overlayType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(value => value.GetType(
                    "StarNight.MapAuthoring.Boundaries.MoonpalaceBoundaryPreviewOverlayToggle", false))
                .FirstOrDefault(value => value != null);
            Assert.That(overlayType, Is.Not.Null);
            var overlays = Enum.GetNames(overlayType)
                .Where(value => value != "None" && value != "All")
                .ToArray();
            return new PreviewProjection(
                (bool)GetProperty(currentReport, "Accepted"),
                (string)GetProperty(currentReport, "StableDigest"),
                (string)GetProperty(currentReport, "AuthoringManifestSha256"),
                pairRows,
                overlays);
        }

        private static void AssertPreviewProjection(PreviewProjection preview)
        {
            Assert.That(preview.Accepted, Is.True);
            Assert.That(preview.StableDigest, Is.EqualTo(ExpectedDigest));
            Assert.That(preview.AuthoringManifest,
                Is.EqualTo(MoonpalaceBoundaryCoverageValidator.ExpectedAuthoringManifestSha256));
            Assert.That(preview.PairRows.Count, Is.EqualTo(6));
            Assert.That(preview.PairRows.Sum(value => value.CandidateCount), Is.EqualTo(31));
            Assert.That(preview.PairRows.Sum(value => value.MicrochunkCount), Is.EqualTo(31));
            Assert.That(preview.PairRows.Sum(value => value.TileRowCount), Is.EqualTo(2976));
            Assert.That(preview.PairRows.Sum(value => value.SocketRowCount), Is.EqualTo(62));
        }

        private static object GetProperty(object value, string name)
        {
            Assert.That(value, Is.Not.Null);
            var property = value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, value.GetType().FullName + "." + name);
            return property.GetValue(value, null);
        }

        private sealed class PreviewProjection
        {
            public PreviewProjection(
                bool accepted,
                string stableDigest,
                string authoringManifest,
                IEnumerable<PreviewPairRow> pairRows,
                IEnumerable<string> overlayCategories)
            {
                Accepted = accepted;
                StableDigest = stableDigest;
                AuthoringManifest = authoringManifest;
                PairRows = pairRows.ToArray();
                OverlayCategories = overlayCategories.ToArray();
                Signature = string.Join("|", new[]
                {
                    Accepted ? "true" : "false",
                    StableDigest,
                    AuthoringManifest,
                    string.Join(";", PairRows.Select(value => value.Signature)),
                    string.Join(",", OverlayCategories),
                });
            }

            public bool Accepted { get; }
            public string StableDigest { get; }
            public string AuthoringManifest { get; }
            public IReadOnlyList<PreviewPairRow> PairRows { get; }
            public IReadOnlyList<string> OverlayCategories { get; }
            public string Signature { get; }
        }

        private sealed class PreviewPairRow
        {
            public PreviewPairRow(
                string pairRuleId,
                string biomeAId,
                string biomeBId,
                string forwardTransition,
                string reverseTransition,
                int candidateCount,
                int microchunkCount,
                int tileRowCount,
                int socketRowCount)
            {
                PairRuleId = pairRuleId;
                BiomeAId = biomeAId;
                BiomeBId = biomeBId;
                ForwardTransition = forwardTransition;
                ReverseTransition = reverseTransition;
                CandidateCount = candidateCount;
                MicrochunkCount = microchunkCount;
                TileRowCount = tileRowCount;
                SocketRowCount = socketRowCount;
            }

            public string PairRuleId { get; }
            public string BiomeAId { get; }
            public string BiomeBId { get; }
            public string ForwardTransition { get; }
            public string ReverseTransition { get; }
            public int CandidateCount { get; }
            public int MicrochunkCount { get; }
            public int TileRowCount { get; }
            public int SocketRowCount { get; }
            public string Signature => string.Join("/", new[]
            {
                PairRuleId,
                BiomeAId,
                BiomeBId,
                ForwardTransition,
                ReverseTransition,
                CandidateCount.ToString(),
                MicrochunkCount.ToString(),
                TileRowCount.ToString(),
                SocketRowCount.ToString(),
            });
        }
    }
}
