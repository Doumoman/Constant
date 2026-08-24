using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryCandidateKeyTests
    {
        public static IEnumerable KeyCases
        {
            get
            {
                for (var index = 0; index < 220; index++)
                {
                    yield return new TestCaseData(index)
                        .SetName("BoundaryCandidateKeyContract_" + index.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(KeyCases))]
        public void BoundaryCandidateKeyContract(int caseIndex)
        {
            var catalog = MoonpalaceBiomePairCatalog.Canonical;
            var cycle = caseIndex / 20;
            var pair = catalog.Pairs[cycle % catalog.Pairs.Count];
            var profile = new MoonpalaceBoundaryProfileId("PROFILE-" + (cycle % 3).ToString(CultureInfo.InvariantCulture));
            var routeRole = new MoonpalaceBoundaryRouteRole(cycle % 2 == 0 ? "Mandatory" : "Optional");
            var edgeSignature = new MoonpalaceBoundaryEdgeSignature("EDGE-" + (cycle % 4).ToString(CultureInfo.InvariantCulture));
            var orientation = cycle % 2 == 0
                ? MoonpalaceBoundaryOrientation.Horizontal
                : MoonpalaceBoundaryOrientation.Vertical;
            var key = new MoonpalaceBoundaryCandidateKey(
                pair, profile, orientation, routeRole, edgeSignature);

            switch (caseIndex % 20)
            {
                case 0:
                    Assert.That(key.IsDefined, Is.True);
                    Assert.That(key.Pair, Is.EqualTo(pair));
                    Assert.That(key.Profile, Is.EqualTo(profile));
                    Assert.That(key.Orientation, Is.EqualTo(orientation));
                    Assert.That(key.RouteRole, Is.EqualTo(routeRole));
                    Assert.That(key.EdgeSignature, Is.EqualTo(edgeSignature));
                    break;
                case 1:
                    var reversedPair = new MoonpalaceBiomePair(pair.Second, pair.First);
                    var reversed = new MoonpalaceBoundaryCandidateKey(
                        reversedPair, profile, orientation, routeRole, edgeSignature);
                    Assert.That(reversed, Is.EqualTo(key));
                    Assert.That(reversed.GetHashCode(), Is.EqualTo(key.GetHashCode()));
                    Assert.That(reversed.Signature, Is.EqualTo(key.Signature));
                    break;
                case 2:
                    Assert.That(new MoonpalaceBoundaryProfileId(profile.CanonicalId), Is.EqualTo(profile));
                    Assert.That(new MoonpalaceBoundaryProfileId(profile.CanonicalId).GetHashCode(),
                        Is.EqualTo(profile.GetHashCode()));
                    Assert.That(new MoonpalaceBoundaryProfileId(profile.CanonicalId.ToLowerInvariant()),
                        Is.Not.EqualTo(profile));
                    break;
                case 3:
                    Assert.That(new MoonpalaceBoundaryRouteRole(routeRole.CanonicalId), Is.EqualTo(routeRole));
                    Assert.That(new MoonpalaceBoundaryRouteRole(routeRole.CanonicalId.ToLowerInvariant()),
                        Is.Not.EqualTo(routeRole));
                    break;
                case 4:
                    Assert.That(new MoonpalaceBoundaryEdgeSignature(edgeSignature.SignatureId),
                        Is.EqualTo(edgeSignature));
                    Assert.That(new MoonpalaceBoundaryEdgeSignature(edgeSignature.SignatureId + "-R"),
                        Is.Not.EqualTo(edgeSignature));
                    break;
                case 5:
                    var same = new MoonpalaceBoundaryCandidateKey(
                        pair, profile, orientation, routeRole, edgeSignature);
                    Assert.That(same == key, Is.True);
                    Assert.That(same.GetHashCode(), Is.EqualTo(key.GetHashCode()));
                    Assert.That(same.CompareTo(key), Is.Zero);
                    break;
                case 6:
                    var otherProfile = new MoonpalaceBoundaryCandidateKey(
                        pair, new MoonpalaceBoundaryProfileId(profile.CanonicalId + "-B"),
                        orientation, routeRole, edgeSignature);
                    Assert.That(otherProfile, Is.Not.EqualTo(key));
                    Assert.That(key.CompareTo(otherProfile), Is.LessThan(0));
                    break;
                case 7:
                    var otherOrientation = new MoonpalaceBoundaryCandidateKey(
                        pair, profile,
                        orientation == MoonpalaceBoundaryOrientation.Horizontal
                            ? MoonpalaceBoundaryOrientation.Vertical
                            : MoonpalaceBoundaryOrientation.Horizontal,
                        routeRole, edgeSignature);
                    Assert.That(otherOrientation, Is.Not.EqualTo(key));
                    break;
                case 8:
                    var otherRole = new MoonpalaceBoundaryCandidateKey(
                        pair, profile, orientation,
                        new MoonpalaceBoundaryRouteRole(routeRole.CanonicalId + "-B"), edgeSignature);
                    Assert.That(otherRole, Is.Not.EqualTo(key));
                    break;
                case 9:
                    Assert.That(key.Signature, Is.EqualTo(string.Join("|", new[]
                    {
                        pair.PairId,
                        profile.CanonicalId,
                        orientation == MoonpalaceBoundaryOrientation.Horizontal ? "Horizontal" : "Vertical",
                        routeRole.CanonicalId,
                        edgeSignature.SignatureId,
                    })));
                    Assert.That(key.ToString(), Is.EqualTo(key.Signature));
                    break;
                case 10:
                    AssertCultureInvariant(key, cycle);
                    break;
                case 11:
                    Assert.Throws<ArgumentException>(() => new MoonpalaceBoundaryCandidateKey(
                        default, profile, orientation, routeRole, edgeSignature));
                    break;
                case 12:
                    Assert.Throws<ArgumentException>(() => new MoonpalaceBoundaryCandidateKey(
                        pair, default, orientation, routeRole, edgeSignature));
                    break;
                case 13:
                    Assert.Throws<ArgumentOutOfRangeException>(() => new MoonpalaceBoundaryCandidateKey(
                        pair, profile, (MoonpalaceBoundaryOrientation)99, routeRole, edgeSignature));
                    break;
                case 14:
                    Assert.Throws<ArgumentException>(() => new MoonpalaceBoundaryCandidateKey(
                        pair, profile, orientation, default, edgeSignature));
                    break;
                case 15:
                    Assert.Throws<ArgumentException>(() => new MoonpalaceBoundaryCandidateKey(
                        pair, profile, orientation, routeRole, default));
                    break;
                case 16:
                    AssertInvalidTokenWrappers(cycle);
                    break;
                case 17:
                    Assert.That(default(MoonpalaceBoundaryCandidateKey).IsDefined, Is.False);
                    Assert.That(default(MoonpalaceBoundaryProfileId).IsDefined, Is.False);
                    Assert.That(default(MoonpalaceBoundaryRouteRole).IsDefined, Is.False);
                    Assert.That(default(MoonpalaceBoundaryEdgeSignature).IsDefined, Is.False);
                    Assert.Throws<InvalidOperationException>(() => default(MoonpalaceBoundaryCandidateKey).ToString());
                    break;
                case 18:
                    var nextPair = catalog.Pairs[(cycle + 1) % catalog.Pairs.Count];
                    var next = new MoonpalaceBoundaryCandidateKey(
                        nextPair, profile, orientation, routeRole, edgeSignature);
                    Assert.That(Math.Sign(key.CompareTo(next)), Is.EqualTo(Math.Sign(pair.CompareTo(nextPair))));
                    break;
                case 19:
                    var dictionary = new Dictionary<MoonpalaceBoundaryCandidateKey, string> { [key] = "value" };
                    var lookup = new MoonpalaceBoundaryCandidateKey(
                        new MoonpalaceBiomePair(pair.Second, pair.First),
                        profile, orientation, routeRole, edgeSignature);
                    Assert.That(dictionary[lookup], Is.EqualTo("value"));
                    break;
                default:
                    Assert.Fail("Unexpected candidate key contract case.");
                    break;
            }
        }

        private static void AssertInvalidTokenWrappers(int cycle)
        {
            var invalid = cycle % 3 == 0 ? null : cycle % 3 == 1 ? " " : " padded ";
            Assert.Throws<ArgumentException>(() => new MoonpalaceBoundaryProfileId(invalid));
            Assert.Throws<ArgumentException>(() => new MoonpalaceBoundaryRouteRole(invalid));
            Assert.Throws<ArgumentException>(() => new MoonpalaceBoundaryEdgeSignature(invalid));
        }

        private static void AssertCultureInvariant(MoonpalaceBoundaryCandidateKey key, int cycle)
        {
            var expectedSignature = key.Signature;
            var expectedHash = key.GetHashCode();
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var culture = CultureInfo.GetCultureInfo(cycle % 2 == 0 ? "tr-TR" : "ar-SA");
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                Assert.That(key.Signature, Is.EqualTo(expectedSignature));
                Assert.That(key.GetHashCode(), Is.EqualTo(expectedHash));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }
    }
}
