#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5JumpSolidTests
    {
        [Test]
        public void J01_CanonicalLocalFixtureIsPassingMixedSupportAscent()
        {
            Sv5JumpSolidPlan plan = Canonical();
            Assert.That(plan.Diagnostics, Is.Empty);
            Assert.That(plan.Supports.Count, Is.EqualTo(10));
            Assert.That(plan.RouteLinks.Count, Is.EqualTo(9));
            Assert.That(plan.Supports.Count(value => value.Kind == Sv5JumpSupportKind.Solid), Is.EqualTo(5));
            Assert.That(plan.Supports.Count(value => value.Kind == Sv5JumpSupportKind.OneWay), Is.EqualTo(5));
            Assert.That(plan.RouteVerticalSpan, Is.EqualTo(9));
        }

        [Test]
        public void J02_TypedSupportsEmitEveryActualOneByOneCell()
        {
            Sv5JumpSolidPlan plan = Canonical();
            int rectangleArea = plan.Supports.Sum(value => value.Width * value.Height);
            Assert.That(plan.SupportCells.Count, Is.EqualTo(rectangleArea));
            Assert.That(plan.SupportCells.All(value => value.Collision ==
                (value.Kind == Sv5JumpSupportKind.Solid ? "SOLID" : "TOP_ONLY")), Is.True);
        }

        [Test]
        public void J03_AllOneWayFixtureIsRejectedByMixedSupportRule()
        {
            Sv5JumpSolidPlan plan = Sv5JumpSolidGeometry.CreateAllOneWayFixture();
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.TooFewSolids);
            Assert.That(plan.JumpSolidGeometryReady, Is.False);
        }

        [Test]
        public void J04_DecorativeSolidCannotSatisfyRequiredRoute()
        {
            Sv5JumpSolidPlan allOneWay = Sv5JumpSolidGeometry.CreateAllOneWayFixture();
            var supports = allOneWay.Supports.Concat(new[]
            {
                new Sv5JumpSupport("DECORATIVE_SOLID", Sv5JumpSupportKind.Solid, 21, 0, 2, 1,
                    true, true, Sv5JumpValidationState.StaticScreen)
            });
            Sv5JumpSolidPlan plan = Sv5JumpSolidGeometry.BuildLocal(supports, allOneWay.RequiredSupportSequence);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.TooFewSolids);
        }

        [Test]
        public void J05_UnreachableSolidOutsideSequenceCannotSatisfyRequiredRoute()
        {
            Sv5JumpSolidPlan allOneWay = Sv5JumpSolidGeometry.CreateAllOneWayFixture();
            var supports = allOneWay.Supports.Concat(new[]
            {
                new Sv5JumpSupport("UNUSED_SOLID", Sv5JumpSupportKind.Solid, 21, 0, 2, 1,
                    true, false, Sv5JumpValidationState.StaticScreen)
            });
            Sv5JumpSolidPlan plan = Sv5JumpSolidGeometry.BuildLocal(supports, allOneWay.RequiredSupportSequence);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.TooFewSolids);
            Assert.That(plan.SolidUses.Any(value => value.SupportId == "UNUSED_SOLID"), Is.False);
        }

        [Test]
        public void J06_MissingSupportCellIsRejected()
        {
            Sv5JumpSolidPlan canonical = Canonical();
            Sv5JumpSolidPlan plan = Rebuild(canonical, supportCells: canonical.SupportCells.Skip(1));
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.MissingSupportCell);
        }

        [Test]
        public void J07_ExtraSupportCellIsRejected()
        {
            Sv5JumpSolidPlan canonical = Canonical();
            var cells = canonical.SupportCells.Concat(new[]
            {
                new Sv5JumpSolidCell("JS00_ENTRY_SOLID", new Sv5JumpPoint(23, 31), Sv5JumpSupportKind.Solid)
            });
            Sv5JumpSolidPlan plan = Rebuild(canonical, supportCells: cells);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.ExtraSupportCell);
        }

        [Test]
        public void J08_OverlappingSupportRectanglesAreRejected()
        {
            Sv5JumpSupport[] supports =
            {
                Support("A", Sv5JumpSupportKind.Solid, 0, 0, 3, 2),
                Support("B", Sv5JumpSupportKind.OneWay, 2, 1, 3, 1)
            };
            Sv5JumpSolidPlan plan = Sv5JumpSolidGeometry.BuildLocal(supports, new[] { "A", "B" });
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.SupportOverlap);
        }

        [Test]
        public void J09_OutOfBoundsEmittedCellIsRejected()
        {
            Sv5JumpSolidPlan canonical = Canonical();
            var cells = canonical.SupportCells.Concat(new[]
            {
                new Sv5JumpSolidCell("JS09_EXIT_ONE_WAY", new Sv5JumpPoint(24, 31), Sv5JumpSupportKind.OneWay)
            });
            Sv5JumpSolidPlan plan = Rebuild(canonical, supportCells: cells);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.CellOutOfBounds);
        }

        [Test]
        public void J10_OccupiedSixBySixAllSolidWindowIsRejected()
        {
            Sv5JumpSupport[] supports =
            {
                Support("A", Sv5JumpSupportKind.Solid, 0, 0, 3, 3),
                Support("B", Sv5JumpSupportKind.Solid, 3, 0, 3, 3),
                Support("C", Sv5JumpSupportKind.Solid, 0, 3, 3, 3),
                Support("D", Sv5JumpSupportKind.Solid, 3, 3, 3, 3)
            };
            Sv5JumpSolidPlan plan = Sv5JumpSolidGeometry.BuildLocal(supports, Array.Empty<string>());
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.SolidSixBySix);
        }

        [Test]
        public void J11_BrokenOrderedRouteIsRejected()
        {
            Sv5JumpSolidPlan canonical = Canonical();
            Sv5JumpSupport source = canonical.Supports.Single(value => value.SupportId == canonical.RequiredSupportSequence[1]);
            Sv5JumpSupport target = canonical.Supports.Single(value => value.SupportId == canonical.RequiredSupportSequence[2]);
            var broken = canonical.RouteLinks.ToArray();
            broken[0] = Link(0, source, target, source.TopXMax, target.TopXMin);
            Sv5JumpSolidPlan plan = Rebuild(canonical, routeLinks: broken);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.RouteOrderBroken);
        }

        [Test]
        public void J12_UnsupportedTakeoffIsRejected()
        {
            Sv5JumpSolidPlan canonical = Canonical();
            Sv5JumpSolidRouteLink original = canonical.RouteLinks[0];
            var broken = canonical.RouteLinks.ToArray();
            broken[0] = new Sv5JumpSolidRouteLink(0, original.LinkId, original.Source, original.Target,
                original.Direction, new Sv5JumpPoint(-1, original.Source.TopY), original.Landing, true);
            Sv5JumpSolidPlan plan = Rebuild(canonical, routeLinks: broken);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.UnsupportedTakeoff);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.ContractRejected);
        }

        [Test]
        public void J13_UnsupportedLandingIsRejected()
        {
            Sv5JumpSolidPlan canonical = Canonical();
            Sv5JumpSolidRouteLink original = canonical.RouteLinks[0];
            var broken = canonical.RouteLinks.ToArray();
            broken[0] = new Sv5JumpSolidRouteLink(0, original.LinkId, original.Source, original.Target,
                original.Direction, original.Takeoff, new Sv5JumpPoint(23, original.Target.TopY), true);
            Sv5JumpSolidPlan plan = Rebuild(canonical, routeLinks: broken);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.UnsupportedLanding);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.ContractRejected);
        }

        [Test]
        public void J14_GapAboveThreeIsRejected()
        {
            Sv5JumpSupport source = Support("A", Sv5JumpSupportKind.Solid, 0, 0, 2, 1);
            Sv5JumpSupport target = Support("B", Sv5JumpSupportKind.OneWay, 6, 0, 2, 1);
            Sv5JumpSolidPlan plan = Sv5JumpSolidGeometry.BuildLocal(new[] { source, target }, new[] { "A", "B" });
            Assert.That(plan.RouteLinks.Single().GapAir, Is.EqualTo(4));
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.GapOutOfRange);
        }

        [Test]
        public void J15_RiseAboveOneIsRejectedByImmutableContract()
        {
            Sv5JumpSupport source = Support("A", Sv5JumpSupportKind.OneWay, 0, 0, 2, 1);
            Sv5JumpSupport target = Support("B", Sv5JumpSupportKind.Solid, 4, 2, 2, 1);
            Sv5JumpSolidPlan plan = Sv5JumpSolidGeometry.BuildLocal(new[] { source, target }, new[] { "A", "B" });
            Assert.That(plan.RouteLinks.Single().Rise, Is.EqualTo(2));
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.RiseOutOfRange);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.ContractRejected);
        }

        [Test]
        public void J16_InsufficientVerticalSpanIsRejected()
        {
            Sv5JumpSupport source = Support("A", Sv5JumpSupportKind.Solid, 0, 0, 2, 1);
            Sv5JumpSupport target = Support("B", Sv5JumpSupportKind.OneWay, 4, 0, 2, 1);
            Sv5JumpSolidPlan plan = Sv5JumpSolidGeometry.BuildLocal(new[] { source, target }, new[] { "A", "B" });
            Assert.That(plan.RouteVerticalSpan, Is.Zero);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.VerticalSpan);
        }

        [Test]
        public void J17_BlockedReservedClearanceIsRejected()
        {
            Sv5JumpSolidPlan canonical = Canonical();
            Sv5JumpSolidClearance[] clearances = canonical.Clearances.ToArray();
            Sv5JumpSolidClearance original = clearances[0];
            clearances[0] = new Sv5JumpSolidClearance(
                original.SupportId, original.Foot, original.Body, original.Head, false);
            Sv5JumpSolidPlan plan = Rebuild(canonical, clearances: clearances);
            AssertDiagnostic(plan, Sv5JumpSolidDiagnostic.ClearanceBlocked);
        }

        [Test]
        public void J18_CanonicalFixtureAndExportsAreByteDeterministic()
        {
            Sv5JumpSolidPlan first = Canonical();
            Sv5JumpSolidPlan second = Canonical();
            Assert.That(second.Digest, Is.EqualTo(first.Digest));
            IReadOnlyDictionary<string, string> firstArtifacts = Sv5JumpSolidExport.BuildArtifacts(first);
            IReadOnlyDictionary<string, string> secondArtifacts = Sv5JumpSolidExport.BuildArtifacts(second);
            CollectionAssert.AreEqual(firstArtifacts.Keys, secondArtifacts.Keys);
            foreach (string key in firstArtifacts.Keys)
            {
                Assert.That(secondArtifacts[key], Is.EqualTo(firstArtifacts[key]), key);
            }
        }

        [Test]
        public void J19_ExportRoundTripWritesAllCanonicalFiles()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(),
                "MapDesign", "MCP", "GENERATED", "SV5_14_JUMP_SOLID");
            Sv5JumpSolidExport.Write(directory, Canonical());
            foreach (string relative in new[]
            {
                "jump_solid.json",
                "supports.csv",
                "support_cells.csv",
                "route_support_sequence.csv",
                "route_links.csv",
                "support_clearance.csv",
                "solid_use.csv",
                "jump_solid_validation.json",
                "preview/jump_solid.svg"
            })
            {
                Assert.That(File.Exists(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar))),
                    Is.True, relative);
            }
        }

        [Test]
        public void J20_LocalFixturePerformsNoWholeWorldBuildOrSearch()
        {
            Sv5JumpSolidPlan plan = Canonical();
            Assert.That(plan.Width, Is.EqualTo(24));
            Assert.That(plan.Height, Is.EqualTo(32));
            Assert.That(plan.WholeWorldBuildsInNewTargetedTests, Is.Zero);
            Assert.That(plan.WholeWorldSearchesInNewTargetedTests, Is.Zero);
            Assert.That(plan.JumpContractReady, Is.True);
            Assert.That(plan.JumpSolidGeometryReady, Is.True);
            Assert.That(plan.JumpGrabGeometryReady, Is.False);
            Assert.That(plan.ComposedGeometryReady, Is.False);
            Assert.That(plan.PlayerVerified, Is.False);
        }

        [Test]
        public void J21_AllRequiredLinksUseJumpContractWithoutGrab()
        {
            Sv5JumpSolidPlan plan = Canonical();
            Assert.That(plan.RouteLinks.All(value => value.ContractAccepted), Is.True);
            Assert.That(plan.RouteLinks.All(value => value.Mode == Sv5JumpMode.Jump), Is.True);
            Assert.That(plan.RouteLinks.All(value => value.GapAir >= 0 && value.GapAir <= 3), Is.True);
            Assert.That(plan.RouteLinks.All(value => value.Rise == 1), Is.True);
            Assert.That(plan.GrabEdgeCount, Is.Zero);
        }

        private static Sv5JumpSolidPlan Canonical()
        {
            return Sv5JumpSolidGeometry.CreateCanonicalLocalFixture();
        }

        private static void AssertDiagnostic(Sv5JumpSolidPlan plan, string prefix)
        {
            Assert.That(plan.Diagnostics.Any(value => value.StartsWith(prefix, StringComparison.Ordinal)),
                Is.True, string.Join("\n", plan.Diagnostics));
        }

        private static Sv5JumpSolidPlan Rebuild(
            Sv5JumpSolidPlan canonical,
            IEnumerable<Sv5JumpSolidRouteLink> routeLinks = null,
            IEnumerable<Sv5JumpSolidClearance> clearances = null,
            IEnumerable<Sv5JumpSolidCell> supportCells = null)
        {
            return Sv5JumpSolidGeometry.BuildLocal(
                canonical.Supports,
                canonical.RequiredSupportSequence,
                routeLinks ?? canonical.RouteLinks,
                clearances ?? canonical.Clearances,
                supportCells ?? canonical.SupportCells,
                canonical.EntrySocket,
                canonical.ExitSocket);
        }

        private static Sv5JumpSolidRouteLink Link(
            int order,
            Sv5JumpSupport source,
            Sv5JumpSupport target,
            int takeoffX,
            int landingX)
        {
            Sv5JumpDirection direction = target.X >= source.X
                ? Sv5JumpDirection.LeftToRight
                : Sv5JumpDirection.RightToLeft;
            return new Sv5JumpSolidRouteLink(
                order,
                "BROKEN_" + order,
                source,
                target,
                direction,
                new Sv5JumpPoint(takeoffX, source.TopY),
                new Sv5JumpPoint(landingX, target.TopY),
                true);
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
    }
}
#endif
