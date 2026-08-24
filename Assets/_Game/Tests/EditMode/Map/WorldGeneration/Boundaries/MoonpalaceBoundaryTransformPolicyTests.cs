using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.Tests.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryTransformPolicyTests
    {
        public static IEnumerable TransformCases
        {
            get
            {
                for (var index = 0; index < 260; index++)
                {
                    yield return new TestCaseData(index)
                        .SetName("BoundaryTransformPolicyContract_" + index.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(TransformCases))]
        public void BoundaryTransformPolicyContract(int caseIndex)
        {
            var cycle = caseIndex / 13;
            switch (caseIndex % 13)
            {
                case 0:
                    AssertPolicy(MoonpalaceBoundaryRequestDirection.Forward,
                        MoonpalaceBoundaryOrientation.Horizontal, MicrochunkTransform.R0, false);
                    break;
                case 1:
                    AssertPolicy(MoonpalaceBoundaryRequestDirection.Forward,
                        MoonpalaceBoundaryOrientation.Vertical, MicrochunkTransform.R0, false);
                    break;
                case 2:
                    AssertPolicy(MoonpalaceBoundaryRequestDirection.Reverse,
                        MoonpalaceBoundaryOrientation.Horizontal, MicrochunkTransform.MirrorX, true);
                    break;
                case 3:
                    AssertPolicy(MoonpalaceBoundaryRequestDirection.Reverse,
                        MoonpalaceBoundaryOrientation.Vertical, MicrochunkTransform.MirrorY, true);
                    break;
                case 4:
                    var forward = MoonpalaceBoundaryTransformPolicy.Create(
                        MoonpalaceBoundaryRequestDirection.Forward,
                        MoonpalaceBoundaryOrientation.Horizontal);
                    Assert.That(forward.Direction, Is.EqualTo(MoonpalaceBoundaryRequestDirection.Forward));
                    Assert.That(forward.Orientation, Is.EqualTo(MoonpalaceBoundaryOrientation.Horizontal));
                    break;
                case 5:
                    var reverse = MoonpalaceBoundaryTransformPolicy.Create(
                        MoonpalaceBoundaryRequestDirection.Reverse,
                        MoonpalaceBoundaryOrientation.Vertical);
                    Assert.That(reverse.Direction, Is.EqualTo(MoonpalaceBoundaryRequestDirection.Reverse));
                    Assert.That(reverse.Orientation, Is.EqualTo(MoonpalaceBoundaryOrientation.Vertical));
                    break;
                case 6:
                    Assert.That(MoonpalaceBoundaryTransformPolicy.Create(
                            MoonpalaceBoundaryRequestDirection.Reverse,
                            MoonpalaceBoundaryOrientation.Horizontal).Signature,
                        Is.EqualTo("Reverse|Horizontal|MIRROR_X"));
                    break;
                case 7:
                    AssertCultureInvariant(cycle);
                    break;
                case 8:
                    Assert.Throws<ArgumentOutOfRangeException>(() =>
                        MoonpalaceBoundaryTransformPolicy.Create(
                            (MoonpalaceBoundaryRequestDirection)99,
                            MoonpalaceBoundaryOrientation.Horizontal));
                    break;
                case 9:
                    Assert.Throws<ArgumentOutOfRangeException>(() =>
                        MoonpalaceBoundaryTransformPolicy.Create(
                            MoonpalaceBoundaryRequestDirection.Forward,
                            (MoonpalaceBoundaryOrientation)99));
                    break;
                case 10:
                    Assert.That((int)MoonpalaceBoundaryRequestDirection.Forward, Is.Zero);
                    Assert.That((int)MoonpalaceBoundaryRequestDirection.Reverse, Is.EqualTo(1));
                    break;
                case 11:
                    var reverseHorizontal = MoonpalaceBoundaryTransformPolicy.Create(
                        MoonpalaceBoundaryRequestDirection.Reverse,
                        MoonpalaceBoundaryOrientation.Horizontal);
                    Assert.That(reverseHorizontal.Transform, Is.Not.EqualTo(MicrochunkTransform.R180));
                    Assert.That(MicrochunkTransformUtility.TransformSide(
                            MicrochunkSide.Left, reverseHorizontal.Transform),
                        Is.EqualTo(MicrochunkSide.Right));
                    break;
                case 12:
                    var reverseVertical = MoonpalaceBoundaryTransformPolicy.Create(
                        MoonpalaceBoundaryRequestDirection.Reverse,
                        MoonpalaceBoundaryOrientation.Vertical);
                    Assert.That(reverseVertical.Transform, Is.Not.EqualTo(MicrochunkTransform.R180));
                    Assert.That(MicrochunkTransformUtility.TransformSide(
                            MicrochunkSide.Up, reverseVertical.Transform),
                        Is.EqualTo(MicrochunkSide.Down));
                    break;
                default:
                    Assert.Fail("Unexpected transform-policy contract case.");
                    break;
            }
        }

        private static void AssertPolicy(
            MoonpalaceBoundaryRequestDirection direction,
            MoonpalaceBoundaryOrientation orientation,
            MicrochunkTransform expectedTransform,
            bool expectedRequiresTransform)
        {
            var policy = MoonpalaceBoundaryTransformPolicy.Create(direction, orientation);
            Assert.That(policy.Direction, Is.EqualTo(direction));
            Assert.That(policy.Orientation, Is.EqualTo(orientation));
            Assert.That(policy.Transform, Is.EqualTo(expectedTransform));
            Assert.That(policy.RequiresTransform, Is.EqualTo(expectedRequiresTransform));
        }

        private static void AssertCultureInvariant(int cycle)
        {
            var policy = MoonpalaceBoundaryTransformPolicy.Create(
                MoonpalaceBoundaryRequestDirection.Reverse,
                cycle % 2 == 0
                    ? MoonpalaceBoundaryOrientation.Horizontal
                    : MoonpalaceBoundaryOrientation.Vertical);
            var expected = policy.Signature;
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var culture = CultureInfo.GetCultureInfo(cycle % 2 == 0 ? "tr-TR" : "ar-SA");
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                Assert.That(policy.Signature, Is.EqualTo(expected));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }
    }
}
