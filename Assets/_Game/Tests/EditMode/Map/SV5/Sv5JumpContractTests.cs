#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5JumpContractTests
    {
        [Test]
        public void J01_CanonicalFixtureUsesActiveSolidAndOneWaySupports()
        {
            Sv5JumpContractFixture fixture = Sv5JumpContract.CreateCanonicalLocalFixture();
            Assert.That(fixture.Supports.Any(value => value.Kind == Sv5JumpSupportKind.Solid && value.ActiveRoute), Is.True);
            Assert.That(fixture.Supports.Any(value => value.Kind == Sv5JumpSupportKind.OneWay && value.ActiveRoute), Is.True);
            Assert.That(fixture.Supports.All(value => !value.DecorativeOnly), Is.True);
        }

        [Test]
        public void J02_LeftToRightGapCountsOnlyEmptyBoundaryColumns()
        {
            Sv5JumpMeasurementResult result = Jump(
                Support("S", Sv5JumpSupportKind.Solid, 0, 0, 2, 1),
                Support("T", Sv5JumpSupportKind.OneWay, 5, 1, 2, 1),
                Sv5JumpDirection.LeftToRight,
                new Sv5JumpPoint(1, 1),
                new Sv5JumpPoint(5, 2));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Link.GapAir, Is.EqualTo(3));
            Assert.That(result.Proof.SourceNearFaceX, Is.EqualTo(1));
            Assert.That(result.Proof.TargetNearFaceX, Is.EqualTo(5));
        }

        [Test]
        public void J03_RightToLeftGapUsesOppositeFacingBoundaries()
        {
            Sv5JumpMeasurementResult result = Jump(
                Support("S", Sv5JumpSupportKind.OneWay, 14, 0, 2, 1),
                Support("T", Sv5JumpSupportKind.Solid, 8, 1, 2, 1),
                Sv5JumpDirection.RightToLeft,
                new Sv5JumpPoint(14, 1),
                new Sv5JumpPoint(9, 2));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Link.GapAir, Is.EqualTo(4));
            Assert.That(result.Proof.SourceNearFaceX, Is.EqualTo(14));
            Assert.That(result.Proof.TargetNearFaceX, Is.EqualTo(9));
        }

        [Test]
        public void J04_HorizontalOverlapClampsGapToZeroInsteadOfNegative()
        {
            Sv5JumpMeasurementResult result = Jump(
                Support("S", Sv5JumpSupportKind.Solid, 0, 0, 4, 1),
                Support("T", Sv5JumpSupportKind.OneWay, 2, 0, 3, 1),
                Sv5JumpDirection.LeftToRight,
                new Sv5JumpPoint(3, 1),
                new Sv5JumpPoint(2, 1));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Link.GapAir, Is.Zero);
        }

        [Test]
        public void J05_RiseUsesTopSupportHeightsRatherThanOriginY()
        {
            Sv5JumpSupport source = Support("S", Sv5JumpSupportKind.Solid, 0, 0, 2, 2);
            Sv5JumpSupport target = Support("T", Sv5JumpSupportKind.OneWay, 4, 0, 2, 3);
            Sv5JumpMeasurementResult result = Jump(source, target, Sv5JumpDirection.LeftToRight,
                new Sv5JumpPoint(1, 2), new Sv5JumpPoint(4, 3));
            Assert.That(source.Y, Is.EqualTo(target.Y));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Link.Rise, Is.EqualTo(1));
            Assert.That(result.Proof.SourceTopY, Is.EqualTo(2));
            Assert.That(result.Proof.TargetTopY, Is.EqualTo(3));
        }

        [Test]
        public void J06_CenterDistanceSubstitutionIsExplicitlyRejected()
        {
            Sv5JumpSupport source = Support("S", Sv5JumpSupportKind.Solid, 0, 0, 2, 1);
            Sv5JumpSupport target = Support("T", Sv5JumpSupportKind.OneWay, 5, 1, 2, 1);
            Sv5JumpMeasurementResult result = Sv5JumpContract.Measure("CENTER_IS_NOT_GAP", source, target,
                Sv5JumpDirection.LeftToRight, new Sv5JumpPoint(1, 1), new Sv5JumpPoint(5, 2),
                Sv5JumpMode.Jump, null, Sv5JumpValidationState.Planned, 5, 1);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Proof.ComputedGapAir, Is.EqualTo(3));
            Assert.That(result.RejectionReasons, Does.Contain(Sv5JumpRejectionReason.DeclaredGapMismatch));
        }

        [Test]
        public void J07_OriginYSubstitutionIsExplicitlyRejected()
        {
            Sv5JumpSupport source = Support("S", Sv5JumpSupportKind.Solid, 0, 0, 2, 2);
            Sv5JumpSupport target = Support("T", Sv5JumpSupportKind.OneWay, 4, 0, 2, 3);
            Sv5JumpMeasurementResult result = Sv5JumpContract.Measure("ORIGIN_IS_NOT_RISE", source, target,
                Sv5JumpDirection.LeftToRight, new Sv5JumpPoint(1, 2), new Sv5JumpPoint(4, 3),
                Sv5JumpMode.Jump, null, Sv5JumpValidationState.Planned, 2, 0);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Proof.ComputedRise, Is.EqualTo(1));
            Assert.That(result.RejectionReasons, Does.Contain(Sv5JumpRejectionReason.DeclaredRiseMismatch));
        }

        [Test]
        public void J08_NormalJumpRiseAboveOneIsRejected()
        {
            Sv5JumpMeasurementResult result = Jump(
                Support("S", Sv5JumpSupportKind.Solid, 0, 0, 2, 1),
                Support("T", Sv5JumpSupportKind.Solid, 4, 2, 2, 1),
                Sv5JumpDirection.LeftToRight,
                new Sv5JumpPoint(1, 1),
                new Sv5JumpPoint(4, 3));
            Assert.That(result.Success, Is.False);
            Assert.That(result.RejectionReasons, Does.Contain(Sv5JumpRejectionReason.NormalRiseExceeded));
        }

        [Test]
        public void J09_JumpGrabRiseAboveTwoIsRejected()
        {
            Sv5JumpSupport source = Support("S", Sv5JumpSupportKind.OneWay, 0, 0, 2, 1);
            Sv5JumpSupport target = Support("T", Sv5JumpSupportKind.Solid, 4, 1, 2, 3);
            Sv5JumpGrabEdge edge = Edge(target, Sv5JumpDirection.LeftToRight, true, true);
            Sv5JumpMeasurementResult result = Sv5JumpContract.Measure("TOO_HIGH", source, target,
                Sv5JumpDirection.LeftToRight, new Sv5JumpPoint(1, 1), new Sv5JumpPoint(4, 4),
                Sv5JumpMode.JumpGrab, edge, Sv5JumpValidationState.Planned);
            Assert.That(result.Success, Is.False);
            Assert.That(result.RejectionReasons, Does.Contain(Sv5JumpRejectionReason.GrabRiseExceeded));
        }

        [Test]
        public void J10_OneWaySideCannotOwnGrabEdge()
        {
            Sv5JumpSupport source = Support("S", Sv5JumpSupportKind.Solid, 0, 0, 2, 1);
            Sv5JumpSupport target = Support("T", Sv5JumpSupportKind.OneWay, 4, 1, 2, 1);
            Sv5JumpGrabEdge edge = Edge(target, Sv5JumpDirection.LeftToRight, true, true);
            Sv5JumpMeasurementResult result = Sv5JumpContract.Measure("ONE_WAY_GRAB", source, target,
                Sv5JumpDirection.LeftToRight, new Sv5JumpPoint(1, 1), new Sv5JumpPoint(4, 2),
                Sv5JumpMode.JumpGrab, edge, Sv5JumpValidationState.Planned);
            Assert.That(result.Success, Is.False);
            Assert.That(result.RejectionReasons, Does.Contain(Sv5JumpRejectionReason.GrabRequiresSolid));
        }

        [Test]
        public void J11_MissingHangBodyClearanceIsRejected()
        {
            Sv5JumpMeasurementResult result = GrabWithClearance(hangClear: false, pullUpClear: true);
            Assert.That(result.Success, Is.False);
            Assert.That(result.RejectionReasons, Does.Contain(Sv5JumpRejectionReason.MissingHangClearance));
        }

        [Test]
        public void J12_MissingPullUpSpaceIsRejected()
        {
            Sv5JumpMeasurementResult result = GrabWithClearance(hangClear: true, pullUpClear: false);
            Assert.That(result.Success, Is.False);
            Assert.That(result.RejectionReasons, Does.Contain(Sv5JumpRejectionReason.MissingPullUpSpace));
        }

        [Test]
        public void J13_DecorativeSupportCannotSatisfyActiveRoute()
        {
            var source = new Sv5JumpSupport("DECOR", Sv5JumpSupportKind.Solid, 0, 0, 2, 1,
                true, true, Sv5JumpValidationState.StaticScreen);
            Sv5JumpMeasurementResult result = Jump(source,
                Support("T", Sv5JumpSupportKind.OneWay, 4, 1, 2, 1),
                Sv5JumpDirection.LeftToRight,
                new Sv5JumpPoint(1, 1),
                new Sv5JumpPoint(4, 2));
            Assert.That(result.Success, Is.False);
            Assert.That(result.RejectionReasons, Does.Contain(Sv5JumpRejectionReason.DecorativeSupport));
        }

        [Test]
        public void J14_PlayerVerifiedPromotionIsRejectedBeforeSv5Twenty()
        {
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
                new Sv5JumpValidationEvidence("J2", "LINK", Sv5JumpValidationState.PlayerVerified,
                    "RUN_PREMATURE", "PASS"));
            Assert.That(error.Message, Does.Contain("SV5_20"));
        }

        [Test]
        public void J15_CanonicalJ1J2J3RoundTripPreservesCoordinatesAndMeasurements()
        {
            Sv5JumpContractFixture fixture = Sv5JumpContract.CreateCanonicalLocalFixture();
            CollectionAssert.AreEqual(new[] { "J1:3:1:JUMP", "J2:4:1:JUMP", "J3:2:2:JUMP_GRAB" },
                fixture.Links.Select(value => value.LinkId + ":" + value.GapAir + ":" + value.Rise + ":" +
                    Sv5JumpContract.ModeName(value.Mode)).ToArray());
            Assert.That(fixture.Link("J1").Takeoff, Is.EqualTo(new Sv5JumpPoint(1, 1)));
            Assert.That(fixture.Link("J2").Landing, Is.EqualTo(new Sv5JumpPoint(9, 2)));
            Assert.That(fixture.Link("J3").Landing, Is.EqualTo(new Sv5JumpPoint(24, 3)));
        }

        [Test]
        public void J16_J3ReferencesRealExposedSolidCornerWithDistinctHangAndPullUpCells()
        {
            Sv5JumpLink link = Sv5JumpContract.CreateCanonicalLocalFixture().Link("J3");
            Assert.That(link.Target.Kind, Is.EqualTo(Sv5JumpSupportKind.Solid));
            Assert.That(link.GrabEdge.Safe, Is.True);
            Assert.That(link.GrabEdge.Contact, Is.Not.EqualTo(link.GrabEdge.HangBody));
            Assert.That(link.GrabEdge.Contact, Is.Not.EqualTo(link.GrabEdge.PullUp));
            Assert.That(link.GrabEdge.HangBody, Is.Not.EqualTo(link.GrabEdge.PullUp));
        }

        [Test]
        public void J17_ValidationStatesDistinguishPlannedAndStaticWithoutPlayerClaim()
        {
            Sv5JumpContractFixture fixture = Sv5JumpContract.CreateCanonicalLocalFixture();
            Assert.That(fixture.ValidationStates.Any(value => value.State == Sv5JumpValidationState.Planned), Is.True);
            Assert.That(fixture.ValidationStates.Any(value => value.State == Sv5JumpValidationState.StaticScreen), Is.True);
            Assert.That(fixture.ValidationStates.Any(value => value.State == Sv5JumpValidationState.PlayerVerified), Is.False);
            Assert.That(fixture.PlayerVerified, Is.False);
        }

        [Test]
        public void J18_OneWayTraversalDoesNotInventReverseCompletionRequirement()
        {
            Sv5JumpContractFixture fixture = Sv5JumpContract.CreateCanonicalLocalFixture();
            Assert.That(fixture.Links.Any(value => value.Source.Kind == Sv5JumpSupportKind.OneWay ||
                value.Target.Kind == Sv5JumpSupportKind.OneWay), Is.True);
            Assert.That(fixture.ReverseCompletionRequired, Is.False);
        }

        [Test]
        public void J19_ExportIsByteDeterministicFromCanonicalObjects()
        {
            var first = Sv5JumpContractExport.BuildArtifacts(Sv5JumpContract.CreateCanonicalLocalFixture());
            var second = Sv5JumpContractExport.BuildArtifacts(Sv5JumpContract.CreateCanonicalLocalFixture());
            CollectionAssert.AreEqual(first.Keys.ToArray(), second.Keys.ToArray());
            foreach (string key in first.Keys)
            {
                Assert.That(second[key], Is.EqualTo(first[key]), key);
            }
        }

        [Test]
        public void J20_LocalFixtureWritesAllCanonicalContractArtifactsWithoutWorldBuild()
        {
            Sv5JumpContractFixture fixture = Sv5JumpContract.CreateCanonicalLocalFixture();
            Assert.That(fixture.WholeWorldBuildsInNewTargetedTests, Is.Zero);
            string directory = Path.Combine(Directory.GetCurrentDirectory(),
                "MapDesign", "MCP", "GENERATED", "SV5_13_JUMP_CONTRACT");
            Sv5JumpContractExport.Write(directory, fixture);
            foreach (string relative in new[]
            {
                "jump_contract.json",
                "supports.csv",
                "grab_edges.csv",
                "links.csv",
                "measurement_proofs.csv",
                "validation_states.csv",
                "contract_validation.json",
                "preview/jump_contract.svg"
            })
            {
                Assert.That(File.Exists(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar))),
                    Is.True, relative);
            }
        }

        private static Sv5JumpSupport Support(
            string id,
            Sv5JumpSupportKind kind,
            int x,
            int y,
            int width,
            int height)
        {
            return new Sv5JumpSupport(id, kind, x, y, width, height, true, false,
                Sv5JumpValidationState.StaticScreen);
        }

        private static Sv5JumpMeasurementResult Jump(
            Sv5JumpSupport source,
            Sv5JumpSupport target,
            Sv5JumpDirection direction,
            Sv5JumpPoint takeoff,
            Sv5JumpPoint landing)
        {
            return Sv5JumpContract.Measure("TEST", source, target, direction, takeoff, landing,
                Sv5JumpMode.Jump, null, Sv5JumpValidationState.Planned);
        }

        private static Sv5JumpGrabEdge Edge(
            Sv5JumpSupport target,
            Sv5JumpDirection direction,
            bool hangClear,
            bool pullUpClear)
        {
            bool left = direction == Sv5JumpDirection.LeftToRight;
            int contactX = left ? target.TopXMin : target.TopXMax;
            return new Sv5JumpGrabEdge("EDGE", target.SupportId,
                new Sv5JumpPoint(contactX, target.TopY - 1),
                left ? Sv5JumpGrabFace.Left : Sv5JumpGrabFace.Right,
                direction,
                new Sv5JumpPoint(contactX + (left ? -1 : 1), target.TopY - 1),
                new Sv5JumpPoint(contactX, target.TopY),
                true,
                hangClear,
                pullUpClear);
        }

        private static Sv5JumpMeasurementResult GrabWithClearance(bool hangClear, bool pullUpClear)
        {
            Sv5JumpSupport source = Support("S", Sv5JumpSupportKind.OneWay, 0, 0, 2, 1);
            Sv5JumpSupport target = Support("T", Sv5JumpSupportKind.Solid, 4, 1, 2, 2);
            return Sv5JumpContract.Measure("GRAB", source, target, Sv5JumpDirection.LeftToRight,
                new Sv5JumpPoint(1, 1), new Sv5JumpPoint(4, 3), Sv5JumpMode.JumpGrab,
                Edge(target, Sv5JumpDirection.LeftToRight, hangClear, pullUpClear),
                Sv5JumpValidationState.Planned);
        }
    }
}
#endif
