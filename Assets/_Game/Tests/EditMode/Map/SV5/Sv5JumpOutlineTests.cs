#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5JumpOutlineTests
    {
        [Test]
        public void O01_CanonicalFixturePassesExactCounts()
        {
            Sv5JumpOutlinePlan plan = Canonical();
            Assert.That(plan.Diagnostics, Is.Empty);
            Assert.That(plan.Supports.Count, Is.EqualTo(10));
            Assert.That(plan.SupportCells.Count, Is.EqualTo(27));
            Assert.That(plan.OutlineCells.Count, Is.EqualTo(17));
            Assert.That(plan.FinalOccupancy.Count, Is.EqualTo(44));
        }

        [Test]
        public void O02_ExactDepthTableIncludesZeroDepthColumn()
        {
            Sv5JumpOutlinePlan plan = Canonical();
            CollectionAssert.AreEquivalent(new[]
            {
                "JS00_ENTRY_SOLID:0:1", "JS00_ENTRY_SOLID:1:1", "JS00_ENTRY_SOLID:2:0",
                "JS02_SOLID:12:1", "JS02_SOLID:13:2", "JS02_SOLID:14:1",
                "JS04_SOLID:14:2", "JS04_SOLID:15:1",
                "JS06_SOLID:4:2", "JS06_SOLID:5:2", "JS06_SOLID:6:1",
                "JS08_SOLID:3:2", "JS08_SOLID:4:1"
            }, plan.DeformationDepths.Select(value => value.OwnerSupportId + ":" + value.X + ":" + value.Depth));
            Assert.That(plan.DeformationDepths.Sum(value => value.Depth), Is.EqualTo(17));
        }

        [Test]
        public void O03_OutlineCellsAreExactDownwardProfile()
        {
            CollectionAssert.AreEquivalent(new[]
            {
                "0:0", "1:0", "12:2", "13:2", "13:1", "14:2",
                "14:5", "14:4", "15:5", "4:6", "4:5", "5:6",
                "5:5", "6:6", "3:8", "3:7", "4:8"
            }, Canonical().OutlineCells.Select(value => value.Point.ToString()));
        }

        [Test]
        public void O04_FinalOccupancyIsDisjointBasePlusOutlineUnion()
        {
            Sv5JumpOutlinePlan plan = Canonical();
            var basePoints = new HashSet<Sv5JumpPoint>(plan.SupportCells.Select(value => value.Point));
            var outlinePoints = new HashSet<Sv5JumpPoint>(plan.OutlineCells.Select(value => value.Point));
            Assert.That(basePoints.Overlaps(outlinePoints), Is.False);
            Assert.That(plan.FinalOccupancy.Count(value => value.Source == "BASE_SUPPORT"), Is.EqualTo(27));
            Assert.That(plan.FinalOccupancy.Count(value => value.Source == "OUTLINE_BACKING"), Is.EqualTo(17));
        }

        [Test]
        public void O05_AllTenSv5FifteenSupportsRemainExact()
        {
            Sv5JumpOutlinePlan plan = Canonical();
            Sv5JumpGrabPlan baseline = Sv5JumpGrabGeometry.CreateCanonicalLocalFixture();
            CollectionAssert.AreEqual(baseline.Supports.Select(SupportToken), plan.Supports.Select(SupportToken));
            CollectionAssert.AreEqual(baseline.SupportCells, plan.SupportCells);
        }

        [Test]
        public void O06_AllNineOrderedRouteLinksRemainByteEquivalentOnExport()
        {
            Sv5JumpOutlinePlan plan = Canonical();
            string expected = Sv5JumpGrabExport.RouteLinksCsv(Sv5JumpGrabGeometry.CreateCanonicalLocalFixture());
            Assert.That(Sv5JumpOutlineExport.RouteLinksCsv(plan), Is.EqualTo(expected));
        }

        [Test]
        public void O07_OnlyExistingGrabEdgeAndCoordinatesRemain()
        {
            Sv5JumpOutlinePlan plan = Canonical();
            Assert.That(plan.GrabEdges.Count, Is.EqualTo(1));
            Sv5JumpGrabEdge edge = plan.GrabEdges.Single();
            Assert.That(edge.GrabEdgeId, Is.EqualTo("JS04_RIGHT_GRAB"));
            Assert.That(edge.Contact, Is.EqualTo(new Sv5JumpPoint(15, 6)));
            Assert.That(edge.HangBody, Is.EqualTo(new Sv5JumpPoint(16, 6)));
            Assert.That(edge.PullUp, Is.EqualTo(new Sv5JumpPoint(15, 7)));
            Assert.That(edge.Face, Is.EqualTo(Sv5JumpGrabFace.Right));
        }

        [Test]
        public void O08_AllTenRouteBodyAndHeadCellsRemainAir()
        {
            Sv5JumpOutlinePlan plan = Canonical();
            var occupied = new HashSet<Sv5JumpPoint>(plan.FinalOccupancy.Select(value => value.Point));
            Assert.That(plan.RouteClearances.Count, Is.EqualTo(10));
            Assert.That(plan.RouteClearances.All(value => value.Clear && value.Body.Equals(value.Foot) &&
                value.Head.Equals(new Sv5JumpPoint(value.Foot.X, value.Foot.Y + 1)) &&
                !occupied.Contains(value.Body) && !occupied.Contains(value.Head)), Is.True);
        }

        [Test]
        public void O09_AcceptedGrabHangAndPullUpCellsRemainAir()
        {
            Sv5JumpOutlinePlan plan = Canonical();
            var occupied = new HashSet<Sv5JumpPoint>(plan.FinalOccupancy.Select(value => value.Point));
            Sv5JumpGrabClearance clearance = plan.GrabClearances.Single();
            foreach (Sv5JumpPoint point in new[]
            {
                clearance.HangBody, clearance.HangHead, clearance.PullUpFoot, clearance.PullUpHead
            })
                Assert.That(occupied.Contains(point), Is.False, point.ToString());
        }

        [Test]
        public void O10_MissingDepthColumnIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            AssertDiagnostic(Rebuild(canonical, depths: canonical.DeformationDepths.Skip(1)),
                Sv5JumpOutlineDiagnostic.DepthMissing);
        }

        [Test]
        public void O11_ExtraDepthColumnIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            var values = canonical.DeformationDepths.Concat(new[]
            {
                new Sv5JumpOutlineDepth("JS00_ENTRY_SOLID", 3, 1, 0, 0)
            });
            AssertDiagnostic(Rebuild(canonical, depths: values), Sv5JumpOutlineDiagnostic.DepthExtra);
        }

        [Test]
        public void O12_WrongDepthIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            Sv5JumpOutlineDepth[] values = canonical.DeformationDepths.ToArray();
            values[0] = new Sv5JumpOutlineDepth(values[0].OwnerSupportId, values[0].X,
                values[0].SupportBottomY, values[0].Depth + 1, values[0].EmittedCellCount + 1);
            AssertDiagnostic(Rebuild(canonical, depths: values), Sv5JumpOutlineDiagnostic.DepthMismatch);
        }

        [Test]
        public void O13_DuplicateDepthColumnIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            AssertDiagnostic(Rebuild(canonical, depths: canonical.DeformationDepths.Concat(
                new[] { canonical.DeformationDepths[0] })), Sv5JumpOutlineDiagnostic.DepthDuplicate);
        }

        [Test]
        public void O14_MissingOutlineCellIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            AssertDiagnostic(Rebuild(canonical, outline: canonical.OutlineCells.Skip(1)),
                Sv5JumpOutlineDiagnostic.OutlineMissing);
        }

        [Test]
        public void O15_ExtraOutlineCellIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            var extra = new Sv5JumpOutlineCell("EXTRA", "JS08_SOLID", new Sv5JumpPoint(23, 20), 1);
            AssertDiagnostic(Rebuild(canonical, outline: canonical.OutlineCells.Concat(new[] { extra })),
                Sv5JumpOutlineDiagnostic.OutlineExtra);
        }

        [Test]
        public void O16_DuplicateOutlineCoordinateIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            Sv5JumpOutlineCell original = canonical.OutlineCells[0];
            var duplicate = new Sv5JumpOutlineCell("DUPLICATE", original.OwnerSupportId, original.Point, original.Depth);
            AssertDiagnostic(Rebuild(canonical, outline: canonical.OutlineCells.Concat(new[] { duplicate })),
                Sv5JumpOutlineDiagnostic.OutlineDuplicate);
        }

        [Test]
        public void O17_OutlineOverlapWithBaseCellIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            var overlap = new Sv5JumpOutlineCell("OVERLAP", "JS00_ENTRY_SOLID", new Sv5JumpPoint(0, 1), 1);
            AssertDiagnostic(Rebuild(canonical, outline: canonical.OutlineCells.Concat(new[] { overlap })),
                Sv5JumpOutlineDiagnostic.OutlineOverlap);
        }

        [Test]
        public void O18_OutOfBoundsOutlineCellIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            var outside = new Sv5JumpOutlineCell("OUTSIDE", "JS00_ENTRY_SOLID", new Sv5JumpPoint(0, -1), 2);
            AssertDiagnostic(Rebuild(canonical, outline: canonical.OutlineCells.Concat(new[] { outside })),
                Sv5JumpOutlineDiagnostic.OutlineOutOfBounds);
        }

        [Test]
        public void O19_OneWayBackingOwnerIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            var oneWay = new Sv5JumpOutlineCell("ONE_WAY_BACKING", "JS01_ONE_WAY", new Sv5JumpPoint(6, 1), 1);
            AssertDiagnostic(Rebuild(canonical, outline: canonical.OutlineCells.Concat(new[] { oneWay })),
                Sv5JumpOutlineDiagnostic.OneWayBacking);
        }

        [Test]
        public void O20_IsolatedOutlineCellIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            var isolated = new Sv5JumpOutlineCell("ISOLATED", "JS08_SOLID", new Sv5JumpPoint(23, 20), 1);
            AssertDiagnostic(Rebuild(canonical, outline: canonical.OutlineCells.Concat(new[] { isolated })),
                Sv5JumpOutlineDiagnostic.IsolatedOutline);
        }

        [Test]
        public void O21_SupportMutationIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            Sv5JumpSupport[] supports = canonical.Supports.ToArray();
            Sv5JumpSupport original = supports[0];
            supports[0] = new Sv5JumpSupport(original.SupportId, original.Kind, original.X + 1, original.Y,
                original.Width, original.Height, original.ActiveRoute, original.DecorativeOnly, original.ValidationState);
            AssertDiagnostic(Rebuild(canonical, supports: supports), Sv5JumpOutlineDiagnostic.BaseSupportMismatch);
        }

        [Test]
        public void O22_RouteLinkMutationIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            Sv5JumpGrabRouteLink[] links = canonical.RouteLinks.ToArray();
            Sv5JumpGrabRouteLink value = links[0];
            links[0] = new Sv5JumpGrabRouteLink(value.Order, value.LinkId, value.Source, value.Target,
                value.Direction, new Sv5JumpPoint(value.Takeoff.X - 1, value.Takeoff.Y), value.Landing,
                value.Mode, value.GrabEdge, value.RequiredRoute);
            AssertDiagnostic(Rebuild(canonical, links: links), Sv5JumpOutlineDiagnostic.BaseRouteMismatch);
        }

        [Test]
        public void O23_BlockedRouteBodyIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            AssertDiagnostic(WithFinalBlocker(canonical, canonical.RouteClearances[0].Body),
                Sv5JumpOutlineDiagnostic.RouteClearanceBlocked);
        }

        [Test]
        public void O24_BlockedRouteHeadIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            AssertDiagnostic(WithFinalBlocker(canonical, canonical.RouteClearances[1].Head),
                Sv5JumpOutlineDiagnostic.RouteClearanceBlocked);
        }

        [Test]
        public void O25_BlockedAcceptedGrabAirIsRejected()
        {
            AssertDiagnostic(WithFinalBlocker(Canonical(), new Sv5JumpPoint(16, 6)),
                Sv5JumpOutlineDiagnostic.GrabClearanceBlocked);
        }

        [Test]
        public void O26_NewAutomaticGrabEdgeIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            var extra = new Sv5JumpGrabEdge("AUTO_OUTLINE_GRAB", "JS02_SOLID", new Sv5JumpPoint(14, 2),
                Sv5JumpGrabFace.Right, Sv5JumpDirection.RightToLeft, new Sv5JumpPoint(15, 2),
                new Sv5JumpPoint(14, 4), true, true, true);
            AssertDiagnostic(Rebuild(canonical, grabEdges: canonical.GrabEdges.Concat(new[] { extra })),
                Sv5JumpOutlineDiagnostic.AutomaticGrab);
        }

        [Test]
        public void O27_FilledSixBySixSolidWindowIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            var cells = new List<Sv5JumpFinalOccupancyCell>();
            for (int y = 20; y < 26; y++)
                for (int x = 17; x < 23; x++)
                    cells.Add(Synthetic(x, y));
            Sv5JumpOutlinePlan plan = Rebuild(canonical, final: cells);
            Assert.That(plan.FilledSolidSixBySixWindows, Is.GreaterThan(0));
            AssertDiagnostic(plan, Sv5JumpOutlineDiagnostic.SixBySix);
        }

        [Test]
        public void O28_RectangularRoomShellIsRejected()
        {
            Sv5JumpOutlinePlan canonical = Canonical();
            var cells = new List<Sv5JumpFinalOccupancyCell>();
            for (int x = 17; x <= 22; x++)
            {
                cells.Add(Synthetic(x, 20));
                cells.Add(Synthetic(x, 25));
            }
            for (int y = 21; y < 25; y++)
            {
                cells.Add(Synthetic(17, y));
                cells.Add(Synthetic(22, y));
            }
            Sv5JumpOutlinePlan plan = Rebuild(canonical, final: cells);
            Assert.That(plan.RectangularRoomShellCreated, Is.True);
            AssertDiagnostic(plan, Sv5JumpOutlineDiagnostic.RectangularShell);
        }

        [Test]
        public void O29_CanonicalFixtureAndArtifactsAreDeterministic()
        {
            Sv5JumpOutlinePlan first = Canonical();
            Sv5JumpOutlinePlan second = Canonical();
            Assert.That(second.Digest, Is.EqualTo(first.Digest));
            IReadOnlyDictionary<string, string> a = Sv5JumpOutlineExport.BuildArtifacts(first);
            IReadOnlyDictionary<string, string> b = Sv5JumpOutlineExport.BuildArtifacts(second);
            CollectionAssert.AreEqual(a.Keys, b.Keys);
            foreach (string key in a.Keys) Assert.That(b[key], Is.EqualTo(a[key]), key);
        }

        [Test]
        public void O30_ExportWritesAllArtifactsAndRequiredSvgLabels()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "MapDesign", "MCP", "GENERATED",
                "SV5_16_JUMP_OUTLINE");
            Sv5JumpOutlineExport.Write(directory, Canonical());
            foreach (string relative in new[]
            {
                "jump_outline.json", "deformation_depths.csv", "outline_cells.csv", "final_occupancy.csv",
                "route_links.csv", "route_clearance.csv", "grab_edges.csv", "grab_clearance.csv",
                "grab_action_sequence.csv", "jump_outline_validation.json", "preview/jump_outline.svg"
            })
                Assert.That(File.Exists(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar))), Is.True, relative);
            string svg = File.ReadAllText(Path.Combine(directory, "preview", "jump_outline.svg"));
            foreach (string label in new[]
            {
                "24x32", "base SOLID", "ONE_WAY", "outline SOLID", "JUMP_GRAB",
                "0-2 cell downward deformation", "ONE_WAY unchanged", "no rectangular room shell",
                "PLAYER verification deferred"
            })
                Assert.That(svg, Does.Contain(label));
        }

        [Test]
        public void O31_ReadinessStopsBeforeCompositionAndPlayerVerification()
        {
            Sv5JumpOutlinePlan plan = Canonical();
            Assert.That(plan.JumpContractReady && plan.JumpSolidGeometryReady &&
                plan.JumpGrabGeometryReady && plan.JumpOutlineReady, Is.True);
            Assert.That(plan.ComposedGeometryReady, Is.False);
            Assert.That(plan.PlayerVerified, Is.False);
            Assert.That(plan.OutlineFacesAutomaticallyGrabbable, Is.False);
        }

        [Test]
        public void O32_LocalFixtureDoesNoWorldWorkAndKeepsOneWayAsymmetric()
        {
            Sv5JumpOutlinePlan plan = Canonical();
            Assert.That(plan.Width, Is.EqualTo(24));
            Assert.That(plan.Height, Is.EqualTo(32));
            Assert.That(plan.WholeWorldBuildsInNewTargetedTests, Is.Zero);
            Assert.That(plan.WholeWorldSearchesInNewTargetedTests, Is.Zero);
            Assert.That(plan.ReverseCompletionRequired, Is.False);
            string[] oneWays = plan.Supports.Where(value => value.Kind == Sv5JumpSupportKind.OneWay)
                .Select(value => value.SupportId).ToArray();
            Assert.That(plan.OutlineCells.Any(value => oneWays.Contains(value.OwnerSupportId)), Is.False);
        }

        private static Sv5JumpOutlinePlan Canonical()
        {
            return Sv5JumpOutlineGeometry.CreateCanonicalLocalFixture();
        }

        private static Sv5JumpOutlinePlan Rebuild(
            Sv5JumpOutlinePlan canonical,
            IEnumerable<Sv5JumpOutlineDepth> depths = null,
            IEnumerable<Sv5JumpOutlineCell> outline = null,
            IEnumerable<Sv5JumpFinalOccupancyCell> final = null,
            IEnumerable<Sv5JumpSupport> supports = null,
            IEnumerable<Sv5JumpGrabRouteLink> links = null,
            IEnumerable<Sv5JumpGrabEdge> grabEdges = null)
        {
            return Sv5JumpOutlineGeometry.BuildLocal(
                Sv5JumpGrabGeometry.CreateCanonicalLocalFixture(), depths ?? canonical.DeformationDepths,
                outline ?? canonical.OutlineCells, final ?? canonical.FinalOccupancy, canonical.RouteClearances,
                supports ?? canonical.Supports, canonical.SupportCells, canonical.RequiredSupportSequence,
                links ?? canonical.RouteLinks, grabEdges ?? canonical.GrabEdges,
                canonical.GrabClearances, canonical.Actions, canonical.BaseFixtureDigest);
        }

        private static Sv5JumpOutlinePlan WithFinalBlocker(Sv5JumpOutlinePlan canonical, Sv5JumpPoint point)
        {
            return Rebuild(canonical, final: canonical.FinalOccupancy.Concat(new[]
            {
                new Sv5JumpFinalOccupancyCell(point, "SOLID", "BLOCKER", "OUTLINE_BACKING", "SOLID")
            }));
        }

        private static Sv5JumpFinalOccupancyCell Synthetic(int x, int y)
        {
            return new Sv5JumpFinalOccupancyCell(new Sv5JumpPoint(x, y), "SOLID", "SHELL",
                "OUTLINE_BACKING", "SOLID");
        }

        private static string SupportToken(Sv5JumpSupport value)
        {
            return value.SupportId + "|" + value.Kind + "|" + value.X + "|" + value.Y + "|" +
                value.Width + "|" + value.Height + "|" + value.ActiveRoute + "|" + value.DecorativeOnly +
                "|" + value.ValidationState;
        }

        private static void AssertDiagnostic(Sv5JumpOutlinePlan plan, string prefix)
        {
            Assert.That(plan.Diagnostics.Any(value => value.StartsWith(prefix, StringComparison.Ordinal)),
                Is.True, string.Join("\n", plan.Diagnostics));
        }
    }
}
#endif
