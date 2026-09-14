#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5JumpGrabTests
    {
        [Test]
        public void G01_CanonicalDerivedFixturePasses()
        {
            Sv5JumpGrabPlan plan = Canonical();
            Assert.That(plan.Diagnostics, Is.Empty);
            Assert.That(plan.JumpGrabGeometryReady, Is.True);
            Assert.That(plan.Supports.Count, Is.EqualTo(10));
            Assert.That(plan.SupportCells.Count, Is.EqualTo(27));
            Assert.That(plan.RouteLinks.Count, Is.EqualTo(9));
        }

        [Test]
        public void G02_OnlyJs04IsTranslatedOneCellUp()
        {
            Sv5JumpSolidPlan baseline = Sv5JumpSolidGeometry.CreateCanonicalLocalFixture();
            Sv5JumpGrabPlan plan = Canonical();
            foreach (Sv5JumpSupport original in baseline.Supports)
            {
                Sv5JumpSupport actual = plan.Support(original.SupportId);
                Assert.That(actual.X, Is.EqualTo(original.X), original.SupportId);
                Assert.That(actual.Y, Is.EqualTo(original.Y + (original.SupportId == "JS04_SOLID" ? 1 : 0)), original.SupportId);
                Assert.That(actual.Width, Is.EqualTo(original.Width), original.SupportId);
                Assert.That(actual.Height, Is.EqualTo(original.Height), original.SupportId);
                Assert.That(actual.Kind, Is.EqualTo(original.Kind), original.SupportId);
                Assert.That(actual.ActiveRoute, Is.EqualTo(original.ActiveRoute), original.SupportId);
                Assert.That(actual.DecorativeOnly, Is.EqualTo(original.DecorativeOnly), original.SupportId);
            }
        }

        [Test]
        public void G03_MovedAndOldCellsAreExact()
        {
            Sv5JumpGrabPlan plan = Canonical();
            var occupied = plan.SupportCells.ToDictionary(value => value.Point);
            Assert.That(occupied.ContainsKey(new Sv5JumpPoint(14, 5)), Is.False);
            Assert.That(occupied.ContainsKey(new Sv5JumpPoint(15, 5)), Is.False);
            AssertSolidCell(occupied, new Sv5JumpPoint(14, 6));
            AssertSolidCell(occupied, new Sv5JumpPoint(15, 6));
        }

        [Test]
        public void G04_ExactlyOneJumpGrabLinkUsesJsLink03()
        {
            Sv5JumpGrabPlan plan = Canonical();
            Sv5JumpGrabRouteLink[] links = plan.RouteLinks.Where(value => value.Mode == Sv5JumpMode.JumpGrab).ToArray();
            Assert.That(links.Length, Is.EqualTo(1));
            Assert.That(links[0].LinkId, Is.EqualTo("JS_LINK_03"));
            Assert.That(links[0].Source.SupportId, Is.EqualTo("JS03_ONE_WAY"));
            Assert.That(links[0].Target.SupportId, Is.EqualTo("JS04_SOLID"));
        }

        [Test]
        public void G05_GrabLinkCoordinatesAndMeasurementAreExact()
        {
            Sv5JumpGrabRouteLink link = Canonical().Link("JS_LINK_03");
            Assert.That(link.Direction, Is.EqualTo(Sv5JumpDirection.RightToLeft));
            Assert.That(link.Takeoff, Is.EqualTo(new Sv5JumpPoint(18, 5)));
            Assert.That(link.Landing, Is.EqualTo(new Sv5JumpPoint(15, 7)));
            Assert.That(link.GapAir, Is.EqualTo(2));
            Assert.That(link.Rise, Is.EqualTo(2));
            Assert.That(link.ContractAccepted, Is.True);
        }

        [Test]
        public void G06_NormalLinksRetainRequiredRiseRange()
        {
            Sv5JumpGrabPlan plan = Canonical();
            Sv5JumpGrabRouteLink[] normal = plan.RouteLinks.Where(value => value.Mode == Sv5JumpMode.Jump).ToArray();
            Assert.That(normal.Length, Is.EqualTo(8));
            Assert.That(normal.All(value => value.Rise >= 0 && value.Rise <= 1), Is.True);
            Assert.That(plan.Link("JS_LINK_04").Rise, Is.Zero);
            Assert.That(normal.Where(value => value.LinkId != "JS_LINK_04").All(value => value.Rise == 1), Is.True);
            Assert.That(normal.All(value => value.GrabEdge == null), Is.True);
        }

        [Test]
        public void G07_EveryRouteLinkUsesImmutableMeasureContract()
        {
            Sv5JumpGrabPlan plan = Canonical();
            Assert.That(plan.RouteLinks.All(value => value.Measurement != null && value.Measurement.Success), Is.True);
            Assert.That(plan.RouteLinks.Select(value => value.GapAir), Is.EqualTo(new[] { 3, 3, 3, 2, 2, 2, 2, 1, 2 }));
            Assert.That(plan.RouteLinks.Select(value => value.Rise), Is.EqualTo(new[] { 1, 1, 1, 2, 0, 1, 1, 1, 1 }));
        }

        [Test]
        public void G08_ContactIsOwnedByJs04SolidRightFace()
        {
            Sv5JumpGrabPlan plan = Canonical();
            Sv5JumpGrabEdge edge = plan.GrabEdges.Single();
            Sv5JumpSolidCell cell = plan.SupportCells.Single(value => value.Point.Equals(edge.Contact));
            Assert.That(edge.GrabEdgeId, Is.EqualTo("JS04_RIGHT_GRAB"));
            Assert.That(edge.Contact, Is.EqualTo(new Sv5JumpPoint(15, 6)));
            Assert.That(edge.Face, Is.EqualTo(Sv5JumpGrabFace.Right));
            Assert.That(cell.SupportId, Is.EqualTo("JS04_SOLID"));
            Assert.That(cell.Kind, Is.EqualTo(Sv5JumpSupportKind.Solid));
        }

        [Test]
        public void G09_HangAndPullUpCellsAreActualAir()
        {
            Sv5JumpGrabPlan plan = Canonical();
            var occupied = new HashSet<Sv5JumpPoint>(plan.SupportCells.Select(value => value.Point));
            Sv5JumpGrabClearance clearance = plan.Clearances.Single();
            foreach (Sv5JumpPoint point in new[]
            {
                clearance.HangBody, clearance.HangHead, clearance.PullUpFoot, clearance.PullUpHead
            })
            {
                Assert.That(occupied.Contains(point), Is.False, point.ToString());
                Assert.That(point.X, Is.InRange(0, 23));
                Assert.That(point.Y, Is.InRange(0, 31));
            }
            Assert.That(occupied.Contains(new Sv5JumpPoint(15, 6)), Is.True);
            Assert.That(clearance.AllAir, Is.True);
        }

        [Test]
        public void G10_ActionWitnessIsTakeoffContactPullUpLand()
        {
            Sv5JumpGrabPlan plan = Canonical();
            CollectionAssert.AreEqual(new[] { "TAKEOFF", "CONTACT_HANG", "PULL_UP", "LAND" },
                plan.Actions.Select(value => value.Action).ToArray());
            CollectionAssert.AreEqual(new[]
            {
                new Sv5JumpPoint(18, 5), new Sv5JumpPoint(16, 6),
                new Sv5JumpPoint(15, 7), new Sv5JumpPoint(15, 7)
            }, plan.Actions.Select(value => value.Player).ToArray());
        }

        [Test]
        public void G11_OneWayGrabFixtureIsRejected()
        {
            Sv5JumpSupport source = Support("SOURCE", Sv5JumpSupportKind.Solid, 18, 4, 3, 1);
            Sv5JumpSupport target = Support("TARGET_ONE_WAY", Sv5JumpSupportKind.OneWay, 14, 6, 2, 1);
            Sv5JumpGrabEdge edge = Edge("TARGET_ONE_WAY");
            Sv5JumpMeasurementResult result = Measure(source, target, Sv5JumpMode.JumpGrab, edge);
            AssertRejected(result, Sv5JumpRejectionReason.GrabRequiresSolid);
        }

        [Test]
        public void G12_HiddenFaceFixtureIsRejected()
        {
            Sv5JumpGrabPlan canonical = Canonical();
            Sv5JumpGrabEdge hidden = Edge("JS04_SOLID", exposed: false);
            Sv5JumpGrabPlan plan = RebuildWithEdge(canonical, hidden);
            AssertDiagnostic(plan, Sv5JumpGrabDiagnostic.GrabFaceMismatch);
            AssertDiagnostic(plan, Sv5JumpGrabDiagnostic.ContractRejected);
        }

        [Test]
        public void G13_WrongFaceAndApproachAreRejected()
        {
            Sv5JumpGrabPlan plan = Canonical();
            Sv5JumpSupport source = plan.Support("JS03_ONE_WAY");
            Sv5JumpSupport target = plan.Support("JS04_SOLID");
            var edge = new Sv5JumpGrabEdge("WRONG", target.SupportId, new Sv5JumpPoint(14, 6),
                Sv5JumpGrabFace.Left, Sv5JumpDirection.RightToLeft, new Sv5JumpPoint(13, 6),
                new Sv5JumpPoint(14, 7), true, true, true);
            Sv5JumpMeasurementResult result = Measure(source, target, Sv5JumpMode.JumpGrab, edge);
            AssertRejected(result, Sv5JumpRejectionReason.GrabFaceMismatch);
        }

        [Test]
        public void G14_MismatchedContactCornerIsRejected()
        {
            Sv5JumpGrabPlan plan = Canonical();
            Sv5JumpSupport source = plan.Support("JS03_ONE_WAY");
            Sv5JumpSupport target = plan.Support("JS04_SOLID");
            var edge = new Sv5JumpGrabEdge("WRONG_CONTACT", target.SupportId, new Sv5JumpPoint(14, 6),
                Sv5JumpGrabFace.Right, Sv5JumpDirection.RightToLeft, new Sv5JumpPoint(16, 6),
                new Sv5JumpPoint(15, 7), true, true, true);
            AssertRejected(Measure(source, target, Sv5JumpMode.JumpGrab, edge),
                Sv5JumpRejectionReason.GrabContactMismatch);
        }

        [Test]
        public void G15_OccupiedHangBodyCellIsRejectedFromActualCells()
        {
            Sv5JumpGrabPlan plan = WithBlocker(new Sv5JumpPoint(16, 6));
            AssertDiagnostic(plan, Sv5JumpGrabDiagnostic.HangBodyBlocked);
            AssertDiagnostic(plan, Sv5JumpGrabDiagnostic.GrabFaceMismatch);
        }

        [Test]
        public void G16_OccupiedHangHeadCellIsRejectedFromActualCells()
        {
            AssertDiagnostic(WithBlocker(new Sv5JumpPoint(16, 7)), Sv5JumpGrabDiagnostic.HangHeadBlocked);
        }

        [Test]
        public void G17_OccupiedPullUpFootCellIsRejectedFromActualCells()
        {
            AssertDiagnostic(WithBlocker(new Sv5JumpPoint(15, 7)), Sv5JumpGrabDiagnostic.PullUpFootBlocked);
        }

        [Test]
        public void G18_OccupiedPullUpHeadCellIsRejectedFromActualCells()
        {
            AssertDiagnostic(WithBlocker(new Sv5JumpPoint(15, 8)), Sv5JumpGrabDiagnostic.PullUpHeadBlocked);
        }

        [Test]
        public void G19_MissingGrabEdgeIsRejected()
        {
            Sv5JumpGrabPlan canonical = Canonical();
            Sv5JumpGrabRouteLink[] links = ReplaceGrabLink(canonical, null, Sv5JumpMode.JumpGrab);
            Sv5JumpGrabPlan plan = Rebuild(canonical, routeLinks: links, grabEdges: Array.Empty<Sv5JumpGrabEdge>());
            AssertDiagnostic(plan, Sv5JumpGrabDiagnostic.GrabEdgeCount);
            AssertDiagnostic(plan, Sv5JumpGrabDiagnostic.ContractRejected);
        }

        [Test]
        public void G20_NormalJumpCannotCarryGrabEdge()
        {
            Sv5JumpGrabPlan plan = Canonical();
            Sv5JumpGrabRouteLink link = plan.Link("JS_LINK_03");
            Sv5JumpMeasurementResult result = Sv5JumpContract.Measure(
                "NORMAL_WITH_GRAB", link.Source, link.Target, link.Direction, link.Takeoff, link.Landing,
                Sv5JumpMode.Jump, plan.GrabEdges.Single(), Sv5JumpValidationState.Planned);
            AssertRejected(result, Sv5JumpRejectionReason.UnexpectedGrab);
            AssertRejected(result, Sv5JumpRejectionReason.NormalRiseExceeded);
        }

        [Test]
        public void G21_RiseAboveTwoIsRejected()
        {
            Sv5JumpSupport source = Support("SOURCE", Sv5JumpSupportKind.OneWay, 18, 4, 3, 1);
            Sv5JumpSupport target = Support("TARGET", Sv5JumpSupportKind.Solid, 14, 7, 2, 1);
            var edge = new Sv5JumpGrabEdge("HIGH", target.SupportId, new Sv5JumpPoint(15, 7),
                Sv5JumpGrabFace.Right, Sv5JumpDirection.RightToLeft, new Sv5JumpPoint(16, 7),
                new Sv5JumpPoint(15, 8), true, true, true);
            Sv5JumpMeasurementResult result = Sv5JumpContract.Measure(
                "HIGH", source, target, Sv5JumpDirection.RightToLeft,
                new Sv5JumpPoint(18, 5), new Sv5JumpPoint(15, 8),
                Sv5JumpMode.JumpGrab, edge, Sv5JumpValidationState.Planned);
            AssertRejected(result, Sv5JumpRejectionReason.GrabRiseExceeded);
        }

        [Test]
        public void G22_PlayerVerifiedStateIsRejected()
        {
            Sv5JumpGrabPlan plan = Canonical();
            Sv5JumpGrabRouteLink link = plan.Link("JS_LINK_03");
            Sv5JumpMeasurementResult result = Sv5JumpContract.Measure(
                "PLAYER", link.Source, link.Target, link.Direction, link.Takeoff, link.Landing,
                Sv5JumpMode.JumpGrab, plan.GrabEdges.Single(), Sv5JumpValidationState.PlayerVerified);
            AssertRejected(result, Sv5JumpRejectionReason.PrematurePlayerState);
        }

        [Test]
        public void G23_CanonicalFixtureAndArtifactsAreDeterministic()
        {
            Sv5JumpGrabPlan first = Canonical();
            Sv5JumpGrabPlan second = Canonical();
            Assert.That(second.Digest, Is.EqualTo(first.Digest));
            IReadOnlyDictionary<string, string> firstArtifacts = Sv5JumpGrabExport.BuildArtifacts(first);
            IReadOnlyDictionary<string, string> secondArtifacts = Sv5JumpGrabExport.BuildArtifacts(second);
            CollectionAssert.AreEqual(firstArtifacts.Keys, secondArtifacts.Keys);
            foreach (string key in firstArtifacts.Keys)
            {
                Assert.That(secondArtifacts[key], Is.EqualTo(firstArtifacts[key]), key);
            }
        }

        [Test]
        public void G24_ExportRoundTripWritesEveryRequiredArtifact()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(),
                "MapDesign", "MCP", "GENERATED", "SV5_15_JUMP_GRAB");
            Sv5JumpGrabExport.Write(directory, Canonical());
            foreach (string relative in new[]
            {
                "jump_grab.json", "supports.csv", "support_cells.csv", "route_links.csv",
                "grab_edges.csv", "grab_clearance.csv", "grab_action_sequence.csv",
                "jump_grab_validation.json", "preview/jump_grab.svg"
            })
            {
                Assert.That(File.Exists(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar))),
                    Is.True, relative);
            }
        }

        [Test]
        public void G25_ReadinessStopsBeforeCompositionAndPlayerVerification()
        {
            Sv5JumpGrabPlan plan = Canonical();
            Assert.That(plan.JumpContractReady, Is.True);
            Assert.That(plan.JumpSolidGeometryReady, Is.True);
            Assert.That(plan.JumpGrabGeometryReady, Is.True);
            Assert.That(plan.ComposedGeometryReady, Is.False);
            Assert.That(plan.PlayerVerified, Is.False);
            Assert.That(plan.LadderLogicCreated, Is.False);
            Assert.That(plan.OneWayGrabAllowed, Is.False);
        }

        [Test]
        public void G26_LocalFixtureDoesNoWholeWorldBuildOrSearch()
        {
            Sv5JumpGrabPlan plan = Canonical();
            Assert.That(plan.Width, Is.EqualTo(24));
            Assert.That(plan.Height, Is.EqualTo(32));
            Assert.That(plan.WholeWorldBuildsInNewTargetedTests, Is.Zero);
            Assert.That(plan.WholeWorldSearchesInNewTargetedTests, Is.Zero);
            Assert.That(plan.ReverseCompletionRequired, Is.False);
        }

        private static Sv5JumpGrabPlan Canonical()
        {
            return Sv5JumpGrabGeometry.CreateCanonicalLocalFixture();
        }

        private static void AssertSolidCell(
            IReadOnlyDictionary<Sv5JumpPoint, Sv5JumpSolidCell> occupied,
            Sv5JumpPoint point)
        {
            Assert.That(occupied.ContainsKey(point), Is.True, point.ToString());
            Assert.That(occupied[point].SupportId, Is.EqualTo("JS04_SOLID"));
            Assert.That(occupied[point].Kind, Is.EqualTo(Sv5JumpSupportKind.Solid));
        }

        private static void AssertDiagnostic(Sv5JumpGrabPlan plan, string prefix)
        {
            Assert.That(plan.Diagnostics.Any(value => value.StartsWith(prefix, StringComparison.Ordinal)),
                Is.True, string.Join("\n", plan.Diagnostics));
        }

        private static void AssertRejected(Sv5JumpMeasurementResult result, string reason)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.RejectionReasons, Does.Contain(reason));
        }

        private static Sv5JumpGrabPlan WithBlocker(Sv5JumpPoint point)
        {
            Sv5JumpGrabPlan canonical = Canonical();
            var cells = canonical.SupportCells.Concat(new[]
            {
                new Sv5JumpSolidCell("BLOCKER", point, Sv5JumpSupportKind.Solid)
            });
            return Rebuild(canonical, supportCells: cells);
        }

        private static Sv5JumpGrabPlan RebuildWithEdge(Sv5JumpGrabPlan canonical, Sv5JumpGrabEdge edge)
        {
            return Rebuild(canonical, routeLinks: ReplaceGrabLink(canonical, edge, Sv5JumpMode.JumpGrab),
                grabEdges: new[] { edge });
        }

        private static Sv5JumpGrabRouteLink[] ReplaceGrabLink(
            Sv5JumpGrabPlan canonical,
            Sv5JumpGrabEdge edge,
            Sv5JumpMode mode)
        {
            Sv5JumpGrabRouteLink[] links = canonical.RouteLinks.ToArray();
            Sv5JumpGrabRouteLink original = links[3];
            links[3] = new Sv5JumpGrabRouteLink(
                original.Order, original.LinkId, original.Source, original.Target, original.Direction,
                original.Takeoff, original.Landing, mode, edge, true);
            return links;
        }

        private static Sv5JumpGrabPlan Rebuild(
            Sv5JumpGrabPlan canonical,
            IEnumerable<Sv5JumpGrabRouteLink> routeLinks = null,
            IEnumerable<Sv5JumpGrabEdge> grabEdges = null,
            IEnumerable<Sv5JumpSolidCell> supportCells = null)
        {
            return Sv5JumpGrabGeometry.BuildLocal(
                canonical.Supports,
                canonical.RequiredSupportSequence,
                routeLinks ?? canonical.RouteLinks,
                grabEdges ?? canonical.GrabEdges,
                canonical.Clearances,
                canonical.Actions,
                supportCells ?? canonical.SupportCells,
                canonical.EntrySocket,
                canonical.ExitSocket,
                canonical.BaseFixtureDigest);
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

        private static Sv5JumpGrabEdge Edge(string supportId, bool exposed = true)
        {
            return new Sv5JumpGrabEdge("EDGE", supportId, new Sv5JumpPoint(15, 6),
                Sv5JumpGrabFace.Right, Sv5JumpDirection.RightToLeft, new Sv5JumpPoint(16, 6),
                new Sv5JumpPoint(15, 7), exposed, true, true);
        }

        private static Sv5JumpMeasurementResult Measure(
            Sv5JumpSupport source,
            Sv5JumpSupport target,
            Sv5JumpMode mode,
            Sv5JumpGrabEdge edge)
        {
            return Sv5JumpContract.Measure(
                "FIXTURE", source, target, Sv5JumpDirection.RightToLeft,
                new Sv5JumpPoint(18, source.TopY), new Sv5JumpPoint(15, target.TopY),
                mode, edge, Sv5JumpValidationState.Planned);
        }
    }
}
#endif
