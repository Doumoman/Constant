using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.WorldGeneration.Boundaries
{
    [Category("MAP08_02")]
    public sealed class MoonpalaceBoundaryCandidateIndexTests
    {
        public static IEnumerable IndexCases
        {
            get
            {
                for (var index = 0; index < 360; index++)
                {
                    yield return new TestCaseData(index)
                        .SetName("BoundaryCandidateIndexContract_" + index.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(IndexCases))]
        public void BoundaryCandidateIndexContract(int caseIndex)
        {
            var cycle = caseIndex / 18;
            var source = CreateCandidates(cycle);
            var index = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(source);
            var pair = source[0].Pair;
            var exactKey = source[0].Key;

            switch (caseIndex % 18)
            {
                case 0:
                    var empty = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(
                        Array.Empty<MoonpalaceBoundaryCandidateDefinition>());
                    Assert.That(empty.Count, Is.Zero);
                    Assert.That(empty.Entries, Is.Empty);
                    Assert.That(empty.Keys, Is.Empty);
                    Assert.That(empty.Candidates, Is.Empty);
                    break;
                case 1:
                    Assert.That(index.Count, Is.EqualTo(5));
                    Assert.That(index.Entries, Has.Count.EqualTo(4));
                    Assert.That(index.Keys, Has.Count.EqualTo(4));
                    break;
                case 2:
                    var exact = index.GetCandidates(exactKey);
                    Assert.That(exact.Select(candidate => candidate.CandidateId),
                        Is.EqualTo(new[] { "C-01", "C-02" }));
                    Assert.That(exact.All(candidate => candidate.Key == exactKey), Is.True);
                    break;
                case 3:
                    Assert.That(index.GetCandidates(pair).Select(candidate => candidate.CandidateId),
                        Is.EqualTo(new[] { "C-01", "C-02", "C-03", "C-04" }));
                    break;
                case 4:
                    Assert.That(index.GetCandidates(pair, MoonpalaceBoundaryOrientation.Horizontal)
                            .Select(candidate => candidate.CandidateId),
                        Is.EqualTo(new[] { "C-01", "C-02", "C-04" }));
                    break;
                case 5:
                    Assert.That(index.GetCandidates(
                            pair,
                            source[0].Profile,
                            MoonpalaceBoundaryOrientation.Horizontal)
                            .Select(candidate => candidate.CandidateId),
                        Is.EqualTo(new[] { "C-01", "C-02" }));
                    break;
                case 6:
                    Assert.That(index.GetCandidates(pair, source[0].RouteRole)
                            .Select(candidate => candidate.CandidateId),
                        Is.EqualTo(new[] { "C-01", "C-02", "C-03" }));
                    break;
                case 7:
                    var reversed = new MoonpalaceBiomePair(pair.Second, pair.First);
                    Assert.That(index.GetCandidates(reversed).Select(candidate => candidate.CandidateId),
                        Is.EqualTo(new[] { "C-01", "C-02", "C-03", "C-04" }));
                    Assert.That(index.GetCandidates(new MoonpalaceBoundaryCandidateKey(
                            reversed,
                            exactKey.Profile,
                            exactKey.Orientation,
                            exactKey.RouteRole,
                            exactKey.EdgeSignature)).Count,
                        Is.EqualTo(2));
                    break;
                case 8:
                    var missingKey = new MoonpalaceBoundaryCandidateKey(
                        pair,
                        new MoonpalaceBoundaryProfileId("MISSING"),
                        MoonpalaceBoundaryOrientation.Horizontal,
                        source[0].RouteRole,
                        source[0].EdgeSignature);
                    Assert.That(index.GetCandidates(missingKey), Is.Empty);
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<MoonpalaceBoundaryCandidateDefinition>)index.GetCandidates(missingKey)).Clear());
                    break;
                case 9:
                    Assert.That(index.Keys, Is.Ordered.Using<MoonpalaceBoundaryCandidateKey>(
                        Comparer<MoonpalaceBoundaryCandidateKey>.Default));
                    Assert.That(index.Entries.Select(entry => entry.Key), Is.EqualTo(index.Keys));
                    break;
                case 10:
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<MoonpalaceBoundaryCandidateIndexEntry>)index.Entries).Clear());
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<MoonpalaceBoundaryCandidateKey>)index.Keys).Clear());
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<MoonpalaceBoundaryCandidateDefinition>)index.Candidates).Clear());
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<MoonpalaceBoundaryCandidateDefinition>)index.GetCandidates(exactKey)).Clear());
                    break;
                case 11:
                    var expected = string.Join("\n", index.Entries.Select(entry =>
                        entry.Key.Signature + ":" + string.Join(",", entry.Candidates.Select(value => value.Signature))));
                    source.Reverse();
                    var rebuilt = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(source);
                    var actual = string.Join("\n", rebuilt.Entries.Select(entry =>
                        entry.Key.Signature + ":" + string.Join(",", entry.Candidates.Select(value => value.Signature))));
                    Assert.That(actual, Is.EqualTo(expected));
                    break;
                case 12:
                    var duplicate = CreateCandidate(
                        source[0].CandidateId,
                        source[4].Pair,
                        source[4].Profile,
                        source[4].Orientation,
                        source[4].RouteRole,
                        source[4].EdgeSignature,
                        1);
                    Assert.Throws<ArgumentException>(() =>
                        MoonpalaceBoundaryCandidateIndexer.Canonical.Build(source.Concat(new[] { duplicate })));
                    break;
                case 13:
                    Assert.Throws<ArgumentNullException>(() =>
                        MoonpalaceBoundaryCandidateIndexer.Canonical.Build(null));
                    var withNull = source.Cast<MoonpalaceBoundaryCandidateDefinition>().ToList();
                    withNull.Add(null);
                    Assert.Throws<ArgumentException>(() =>
                        MoonpalaceBoundaryCandidateIndexer.Canonical.Build(withNull));
                    break;
                case 14:
                    AssertCandidateValidation(pair, source[0], cycle);
                    break;
                case 15:
                    Assert.That(MoonpalaceBiomePairCatalog.Canonical.GetDefinition(pair)
                        .Supports(source[0].Orientation), Is.True);
                    Assert.Throws<ArgumentException>(() => new MoonpalaceBoundaryCandidateDefinition(
                        "INVALID-PAIR", default, source[0].Profile, source[0].Orientation,
                        source[0].RouteRole, source[0].EdgeSignature, 1, true, "NONE",
                        MoonpalaceBoundaryWarningMarker.Tile));
                    break;
                case 16:
                    AssertCultureInvariant(source[cycle % source.Count], cycle);
                    break;
                case 17:
                    var first = index.GetCandidates(exactKey);
                    var second = index.GetCandidates(exactKey);
                    Assert.That(second.Select(candidate => candidate.Signature),
                        Is.EqualTo(first.Select(candidate => candidate.Signature)));
                    Assert.That(first, Has.Count.EqualTo(2));
                    Assert.That(first[0], Is.SameAs(second[0]));
                    Assert.That(first[0].EdgeSignature, Is.EqualTo(exactKey.EdgeSignature));
                    break;
                default:
                    Assert.Fail("Unexpected candidate index contract case.");
                    break;
            }
        }

        private static List<MoonpalaceBoundaryCandidateDefinition> CreateCandidates(int cycle)
        {
            var catalog = MoonpalaceBiomePairCatalog.Canonical;
            var pair = catalog.Pairs[cycle % catalog.Pairs.Count];
            var otherPair = catalog.Pairs[(cycle + 1) % catalog.Pairs.Count];
            var dense = new MoonpalaceBoundaryProfileId("Dense");
            var sparse = new MoonpalaceBoundaryProfileId("Sparse");
            var mandatory = new MoonpalaceBoundaryRouteRole("Mandatory");
            var optional = new MoonpalaceBoundaryRouteRole("Optional");
            var signatureA = new MoonpalaceBoundaryEdgeSignature("SIG-A");
            var signatureB = new MoonpalaceBoundaryEdgeSignature("SIG-B");

            return new List<MoonpalaceBoundaryCandidateDefinition>
            {
                CreateCandidate("C-02", pair, dense, MoonpalaceBoundaryOrientation.Horizontal,
                    mandatory, signatureA, 1),
                CreateCandidate("C-01", pair, dense, MoonpalaceBoundaryOrientation.Horizontal,
                    mandatory, signatureA, 9),
                CreateCandidate("C-03", pair, dense, MoonpalaceBoundaryOrientation.Vertical,
                    mandatory, signatureB, 4),
                CreateCandidate("C-04", pair, sparse, MoonpalaceBoundaryOrientation.Horizontal,
                    optional, signatureA, 2),
                CreateCandidate("C-05", otherPair, dense, MoonpalaceBoundaryOrientation.Horizontal,
                    mandatory, signatureA, 3),
            };
        }

        private static MoonpalaceBoundaryCandidateDefinition CreateCandidate(
            string candidateId,
            MoonpalaceBiomePair pair,
            MoonpalaceBoundaryProfileId profile,
            MoonpalaceBoundaryOrientation orientation,
            MoonpalaceBoundaryRouteRole routeRole,
            MoonpalaceBoundaryEdgeSignature edgeSignature,
            int weight)
        {
            return new MoonpalaceBoundaryCandidateDefinition(
                candidateId,
                pair,
                profile,
                orientation,
                routeRole,
                edgeSignature,
                weight,
                true,
                MoonpalaceBiomePairDefinition.NoToolRequirement,
                MoonpalaceBoundaryWarningMarker.Tile | MoonpalaceBoundaryWarningMarker.Background);
        }

        private static void AssertCandidateValidation(
            MoonpalaceBiomePair pair,
            MoonpalaceBoundaryCandidateDefinition sample,
            int cycle)
        {
            if (cycle % 4 == 0)
            {
                Assert.Throws<ArgumentException>(() => CreateCandidate(
                    " ", pair, sample.Profile, sample.Orientation, sample.RouteRole,
                    sample.EdgeSignature, 1));
            }
            else if (cycle % 4 == 1)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => CreateCandidate(
                    "NEGATIVE", pair, sample.Profile, sample.Orientation, sample.RouteRole,
                    sample.EdgeSignature, -1));
            }
            else if (cycle % 4 == 2)
            {
                Assert.Throws<ArgumentException>(() => new MoonpalaceBoundaryCandidateDefinition(
                    "NO-TOOL", pair, sample.Profile, sample.Orientation, sample.RouteRole,
                    sample.EdgeSignature, 1, true, " ", MoonpalaceBoundaryWarningMarker.Tile));
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new MoonpalaceBoundaryCandidateDefinition(
                    "BAD-MARKER", pair, sample.Profile, sample.Orientation, sample.RouteRole,
                    sample.EdgeSignature, 1, true, "NONE",
                    (MoonpalaceBoundaryWarningMarker)(1 << 12)));
            }
        }

        private static void AssertCultureInvariant(
            MoonpalaceBoundaryCandidateDefinition candidate,
            int cycle)
        {
            var expected = candidate.Signature;
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var culture = CultureInfo.GetCultureInfo(cycle % 2 == 0 ? "tr-TR" : "fr-FR");
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                Assert.That(candidate.Signature, Is.EqualTo(expected));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }
    }
}
