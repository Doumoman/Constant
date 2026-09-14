#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5JumpRecoveryTests
    {
        [Test]
        public void C01_CanonicalRecoveryPassesWithoutDiagnostics()
        {
            Assert.That(Canonical().Diagnostics, Is.Empty);
        }

        [Test]
        public void C02_CanonicalRecoveryContainsTwoLocalRecipes()
        {
            Assert.That(Canonical().Probes.Select(value => value.RecipeId).Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void C03_OverlayContainsThreeGroupsAndEightCellsPerRecipe()
        {
            Sv5JumpRecoveryPlan plan = Canonical();
            foreach (string recipe in Recipes)
            {
                Assert.That(plan.Overlay.Count(value => value.RecipeId == recipe), Is.EqualTo(8));
                Assert.That(plan.Overlay.Where(value => value.RecipeId == recipe).Select(value => value.GroupId).Distinct().Count(), Is.EqualTo(3));
            }
        }

        [Test]
        public void C04_OverlayRunsAreHorizontalContiguousAndTwoToThreeCellsWide()
        {
            foreach (IGrouping<string, Sv5JumpRecoveryCell> group in Canonical().Overlay
                .GroupBy(value => value.RecipeId + "|" + value.GroupId))
            {
                int[] xs = group.Select(value => value.Point.X).OrderBy(value => value).ToArray();
                Assert.That(group.Select(value => value.Point.Y).Distinct().Count(), Is.EqualTo(1));
                Assert.That(xs.Length, Is.InRange(2, 3));
                CollectionAssert.AreEqual(Enumerable.Range(xs[0], xs.Length), xs);
            }
        }

        [Test]
        public void C05_ExactlyOneProbeAndRouteExistsForEveryMainLink()
        {
            Sv5JumpRecoveryPlan plan = Canonical();
            Assert.That(plan.Probes.Count, Is.EqualTo(18));
            Assert.That(plan.Routes.Count, Is.EqualTo(18));
            Assert.That(plan.Probes.Select(value => value.RecipeId + "|" + value.FailedLinkOrder).Distinct().Count(), Is.EqualTo(18));
        }

        [Test]
        public void C06_ProbeOriginsUseLowerMiddleOrderedAirSample()
        {
            Sv5JumpClearancePlan clearance = Clearance();
            foreach (Sv5JumpMissProbe probe in Canonical().Probes)
            {
                Sv5JumpClearanceTrace trace = ClearanceTrace(clearance, probe.RecipeId, probe.FailedLinkOrder);
                Sv5JumpClearanceSample[] air = trace.Samples.Where(value => value.Phase == Sv5JumpClearancePhase.Air)
                    .OrderBy(value => value.SampleOrder).ToArray();
                Sv5JumpClearanceSample expected = air[(air.Length - 1) / 2];
                Assert.That(probe.SourceSampleOrder, Is.EqualTo(expected.SampleOrder));
                Assert.That(probe.Origin, Is.EqualTo(expected.Body));
            }
        }

        [Test]
        public void C07_AllEighteenMissesAreCaughtOnTheFirstSurface()
        {
            Sv5JumpRecoveryPlan plan = Canonical();
            Assert.That(plan.Probes.Count(value => value.Caught), Is.EqualTo(18));
            Assert.That(plan.Probes.All(value => value.CatchPoint.X == value.Origin.X && value.CatchPoint.Y < value.Origin.Y), Is.True);
        }

        [Test]
        public void C08_SixProbesUseRecoveryCatchAndTwelveUseBaseCatch()
        {
            Assert.That(Canonical().Probes.Count(value => value.CatchSource == "RECOVERY"), Is.EqualTo(6));
            Assert.That(Canonical().Probes.Count(value => value.CatchSource == "BASE"), Is.EqualTo(12));
        }

        [Test]
        public void C09_AllLandingBodyAndHeadCellsAreClear()
        {
            Sv5JumpRecoveryPlan plan = Canonical();
            Sv5JumpRecipeCatalogModel catalog = Sv5JumpRecipeCatalog.CreateCanonicalCatalog();
            foreach (Sv5JumpMissProbe probe in plan.Probes)
            {
                var occupied = new HashSet<Sv5JumpPoint>(catalog.Recipe(probe.RecipeId).Occupancy.Select(value => value.Point)
                    .Concat(plan.Overlay.Where(value => value.RecipeId == probe.RecipeId).Select(value => value.Point)));
                Assert.That(occupied.Contains(probe.LandingBody), Is.False);
                Assert.That(occupied.Contains(probe.LandingHead), Is.False);
            }
        }

        [Test]
        public void C10_RoutesUseFiveLinkedAndFourAlreadyPerRecipe()
        {
            foreach (string recipe in Recipes)
            {
                Assert.That(Canonical().Routes.Count(value => value.RecipeId == recipe && value.RouteKind == Sv5JumpRecoveryRouteKind.Linked), Is.EqualTo(5));
                Assert.That(Canonical().Routes.Count(value => value.RecipeId == recipe && value.RouteKind == Sv5JumpRecoveryRouteKind.AlreadyAtCheckpoint), Is.EqualTo(4));
            }
        }

        [Test]
        public void C11_AlreadyAtCheckpointRoutesEmitNoMovement()
        {
            foreach (Sv5JumpRecoveryRoute route in Canonical().Routes.Where(value => value.RouteKind == Sv5JumpRecoveryRouteKind.AlreadyAtCheckpoint))
            {
                Assert.That(route.Start, Is.EqualTo(route.End));
                Assert.That(route.Links, Is.Empty);
            }
        }

        [Test]
        public void C12_LinkedRoutesContainTenItemlessMovementLinksTotal()
        {
            Sv5JumpRecoveryPlan plan = Canonical();
            Assert.That(plan.RecoveryLinkCount, Is.EqualTo(10));
            Assert.That(plan.Routes.SelectMany(value => value.Links).All(value =>
                value.Mode == Sv5JumpRecoveryMode.Walk || value.Mode == Sv5JumpRecoveryMode.Jump || value.Mode == Sv5JumpRecoveryMode.Drop), Is.True);
        }

        [Test]
        public void C13_RecoveryLinksContainThirtyFourAdjacentTraceStates()
        {
            Sv5JumpRecoveryPlan plan = Canonical();
            Assert.That(plan.RecoveryTraceStateCount, Is.EqualTo(34));
            foreach (Sv5JumpRecoveryLink link in plan.Routes.SelectMany(value => value.Links))
                for (int index = 0; index + 1 < link.Trace.Count; index++)
                {
                    int dx = Math.Abs(link.Trace[index + 1].Body.X - link.Trace[index].Body.X);
                    int dy = Math.Abs(link.Trace[index + 1].Body.Y - link.Trace[index].Body.Y);
                    Assert.That(dx <= 1 && dy <= 1 && dx + dy > 0, Is.True);
                }
        }

        [Test]
        public void C14_JumpRiseCapAndDropDirectionAreRespected()
        {
            foreach (Sv5JumpRecoveryLink link in Canonical().Routes.SelectMany(value => value.Links))
            {
                if (link.Mode == Sv5JumpRecoveryMode.Jump) Assert.That(link.Rise, Is.LessThanOrEqualTo(1));
                if (link.Mode == Sv5JumpRecoveryMode.Drop) Assert.That(link.Rise, Is.LessThanOrEqualTo(0));
                if (link.Mode == Sv5JumpRecoveryMode.Walk) Assert.That(link.Rise, Is.Zero);
            }
        }

        [Test]
        public void C15_EveryRouteRejoinsSameOrEarlierCheckpoint()
        {
            Assert.That(Canonical().Routes.All(value => value.CheckpointLinkOrder <= value.FailedLinkOrder), Is.True);
        }

        [Test]
        public void C16_CheckpointDistributionUsesOrdersZeroOneTwoThreeAndSix()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 6 }, Canonical().Routes
                .Select(value => value.CheckpointLinkOrder).Distinct().OrderBy(value => value));
        }

        [Test]
        public void C17_OverlayIsAnExactHorizontalMirror()
        {
            var r0 = new HashSet<string>(Canonical().Overlay.Where(value => value.RecipeId == Recipes[0])
                .Select(value => (23 - value.Point.X) + ":" + value.Point.Y + "|" + value.GroupId));
            var mx = new HashSet<string>(Canonical().Overlay.Where(value => value.RecipeId == Recipes[1])
                .Select(value => value.Point.X + ":" + value.Point.Y + "|" + value.GroupId));
            Assert.That(mx.SetEquals(r0), Is.True);
        }

        [Test]
        public void C18_ProbesAreExactHorizontalMirrors()
        {
            Sv5JumpRecoveryPlan plan = Canonical();
            for (int order = 0; order < 9; order++)
            {
                Sv5JumpMissProbe a = Probe(plan, Recipes[0], order);
                Sv5JumpMissProbe b = Probe(plan, Recipes[1], order);
                AssertMirror(a.Origin, b.Origin);
                AssertMirror(a.CatchPoint, b.CatchPoint);
            }
        }

        [Test]
        public void C19_RoutesAndTraceStatesAreExactHorizontalMirrors()
        {
            Sv5JumpRecoveryPlan plan = Canonical();
            for (int order = 0; order < 9; order++)
            {
                Sv5JumpRecoveryRoute a = Route(plan, Recipes[0], order);
                Sv5JumpRecoveryRoute b = Route(plan, Recipes[1], order);
                AssertMirror(a.Start, b.Start);
                AssertMirror(a.End, b.End);
                Assert.That(b.CheckpointLinkOrder, Is.EqualTo(a.CheckpointLinkOrder));
                Assert.That(b.Links.Count, Is.EqualTo(a.Links.Count));
                for (int index = 0; index < a.Links.Count; index++)
                    for (int sample = 0; sample < a.Links[index].Trace.Count; sample++)
                        AssertMirror(a.Links[index].Trace[sample].Body, b.Links[index].Trace[sample].Body);
            }
        }

        [Test]
        public void C20_AllEighteenMainClearanceProofsRemainPassing()
        {
            Sv5JumpClearancePlan clearance = Clearance();
            Assert.That(clearance.Digest, Is.EqualTo(Sv5JumpRecovery.InputClearanceDigest));
            Assert.That(clearance.Links.Count(value => value.ClearancePass), Is.EqualTo(18));
        }

        [Test]
        public void C21_RecoveryProofAndExportsAreDeterministic()
        {
            Sv5JumpRecoveryPlan a = Canonical();
            Sv5JumpRecoveryPlan b = Canonical();
            Assert.That(b.Digest, Is.EqualTo(a.Digest));
            IReadOnlyDictionary<string, string> left = Sv5JumpRecoveryExport.BuildArtifacts(a);
            IReadOnlyDictionary<string, string> right = Sv5JumpRecoveryExport.BuildArtifacts(b);
            CollectionAssert.AreEqual(left.Keys, right.Keys);
            foreach (string key in left.Keys) Assert.That(right[key], Is.EqualTo(left[key]), key);
        }

        [Test]
        public void C22_ExportWritesEveryRequiredCanonicalArtifact()
        {
            string directory = OutputDirectory();
            Sv5JumpRecoveryExport.Write(directory, Canonical());
            foreach (string relative in new[]
            {
                "jump_recovery.json", "recovery_overlay.csv", "recovery_miss_probes.csv", "recovery_routes.csv",
                "recovery_links.csv", "recovery_trace.csv", "recovery_validation.json", "preview/jump_recovery.svg",
            })
                Assert.That(File.Exists(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar))), Is.True, relative);
        }

        [Test]
        public void C23_ExportsDeclareRecoveryReadyButNotPlayerVerified()
        {
            string json = Sv5JumpRecoveryExport.JumpRecoveryJson(Canonical());
            Assert.That(json, Does.Contain("\"RecoveryReady\": true"));
            Assert.That(json, Does.Contain("\"PlayerVerified\": false"));
            Assert.That(json, Does.Contain("\"reverse_required\": false"));
            Assert.That(json, Does.Contain("\"items_used\": false"));
        }

        [Test]
        public void C24_ValidationExportIsPassWithNoErrors()
        {
            string json = Sv5JumpRecoveryExport.ValidationJson(Canonical()).Replace("\r\n", "\n");
            Assert.That(json, Does.Contain("\"status\": \"PASS\""));
            Assert.That(json, Does.Contain("\"probe_count\": 18"));
            Assert.That(json, Does.Contain("\"errors\": [\n  ]"));
        }

        [Test]
        public void C25_SvgShowsBothRecipesMissCatchRouteAndCheckpoint()
        {
            string svg = Sv5JumpRecoveryExport.PreviewSvg(Canonical());
            foreach (string token in new[]
            {
                Recipes[0], Recipes[1], "18 AIR probes", "recovery catch", "miss ray", "recovery route",
                "rejoin checkpoint", "ALREADY_AT_CHECKPOINT", "RecoveryReady=true", "PlayerVerified=false",
            })
                Assert.That(svg, Does.Contain(token), token);
        }

        [Test]
        public void C26_ReadinessAndLocalWorkCountersAreExact()
        {
            Sv5JumpRecoveryPlan plan = Canonical();
            Assert.That(plan.JumpRecipeReady && plan.ComposedGeometryReady && plan.SweptClearanceReady && plan.RecoveryReady, Is.True);
            Assert.That(plan.PlayerVerified || plan.ReverseRequired, Is.False);
            Assert.That(plan.WholeWorldBuilds + plan.WholeWorldSearches + plan.GlobalEndpointComparisons, Is.Zero);
        }

        [Test]
        public void C27_RecoveryMovementEndpointsHaveOccupiedSupport()
        {
            Sv5JumpRecoveryPlan plan = Canonical();
            Sv5JumpRecipeCatalogModel catalog = Sv5JumpRecipeCatalog.CreateCanonicalCatalog();
            foreach (string recipe in Recipes)
            {
                var occupied = new HashSet<Sv5JumpPoint>(catalog.Recipe(recipe).Occupancy.Select(value => value.Point)
                    .Concat(plan.Overlay.Where(value => value.RecipeId == recipe).Select(value => value.Point)));
                foreach (Sv5JumpRecoveryLink link in plan.Routes.Where(value => value.RecipeId == recipe).SelectMany(value => value.Links))
                {
                    Assert.That(occupied.Contains(new Sv5JumpPoint(link.Source.X, link.Source.Y - 1)), Is.True);
                    Assert.That(occupied.Contains(new Sv5JumpPoint(link.Target.X, link.Target.Y - 1)), Is.True);
                }
            }
        }

        [Test]
        public void C28_ComposedGeometryContainsNoSolidSixBySixFill()
        {
            Assert.That(Canonical().Diagnostics.Any(value => value.Contains(Sv5JumpRecoveryDiagnostic.SolidFillSixBySix)), Is.False);
        }

        [Test]
        public void C29_MissingCatchFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            AssertDiagnostic(Validate(source.Overlay.Where(value => value.RecipeId != Recipes[0] || value.GroupId != "RG_ENTRY_CATCH"),
                source.Probes, source.Routes), Sv5JumpRecoveryDiagnostic.MissingCatch);
        }

        [Test]
        public void C30_SkippedNearerSurfaceFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpMissProbe original = Probe(source, Recipes[0], 2);
            Sv5JumpMissProbe wrong = RebuildProbe(original, catchPoint: new Sv5JumpPoint(14, 5));
            AssertDiagnostic(Validate(source.Overlay, ReplaceProbe(source, wrong), source.Routes), Sv5JumpRecoveryDiagnostic.SkippedNearerCatch);
        }

        [Test]
        public void C31_BlockedLandingBodyFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpMissProbe original = Probe(source, Recipes[0], 0);
            Sv5JumpMissProbe wrong = RebuildProbe(original, landingBody: original.CatchPoint);
            AssertDiagnostic(Validate(source.Overlay, ReplaceProbe(source, wrong), source.Routes), Sv5JumpRecoveryDiagnostic.BlockedLandingBody);
        }

        [Test]
        public void C32_BlockedLandingHeadFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpMissProbe original = Probe(source, Recipes[0], 0);
            Sv5JumpMissProbe wrong = RebuildProbe(original, landingHead: original.CatchPoint);
            AssertDiagnostic(Validate(source.Overlay, ReplaceProbe(source, wrong), source.Routes), Sv5JumpRecoveryDiagnostic.BlockedLandingHead);
        }

        [Test]
        public void C33_OverlayMainTraceOverlapFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            var extra = new Sv5JumpRecoveryCell(Recipes[0], "RG_BAD_TRACE", new Sv5JumpPoint(4, 3), "TOP_ONLY", "RG_BAD_TRACE");
            AssertDiagnostic(Validate(source.Overlay.Concat(new[] { extra }), source.Probes, source.Routes),
                Sv5JumpRecoveryDiagnostic.OverlayMainTraceOverlap);
        }

        [Test]
        public void C34_BadOverlayMirrorFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            AssertDiagnostic(Validate(source.Overlay.Skip(1), source.Probes, source.Routes),
                Sv5JumpRecoveryDiagnostic.OverlayMirrorMismatch);
        }

        [Test]
        public void C35_DisconnectedRouteFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpRecoveryRoute original = Route(source, Recipes[0], 0);
            Sv5JumpRecoveryRoute wrong = RebuildRoute(original, links: Array.Empty<Sv5JumpRecoveryLink>());
            AssertDiagnostic(Validate(source.Overlay, source.Probes, ReplaceRoute(source, wrong)),
                Sv5JumpRecoveryDiagnostic.DisconnectedRoute);
        }

        [Test]
        public void C36_LinkDiscontinuityFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpRecoveryRoute original = Route(source, Recipes[0], 0);
            Sv5JumpRecoveryLink link = original.Links.Single();
            var wrongLink = new Sv5JumpRecoveryLink(link.RecipeId, link.RouteId, 1, link.RecoveryLinkId,
                link.Mode, link.Source, link.Target, link.Trace);
            AssertDiagnostic(Validate(source.Overlay, source.Probes, ReplaceRoute(source, RebuildRoute(original,
                links: new[] { wrongLink }))), Sv5JumpRecoveryDiagnostic.LinkDiscontinuity);
        }

        [Test]
        public void C37_UnsupportedEndpointFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpRecoveryRoute original = Route(source, Recipes[0], 0);
            Sv5JumpRecoveryLink link = original.Links.Single();
            var wrongLink = new Sv5JumpRecoveryLink(link.RecipeId, link.RouteId, 0, link.RecoveryLinkId,
                link.Mode, link.Source, new Sv5JumpPoint(6, 15), link.Trace);
            AssertDiagnostic(Validate(source.Overlay, source.Probes, ReplaceRoute(source, RebuildRoute(original,
                links: new[] { wrongLink }))), Sv5JumpRecoveryDiagnostic.UnsupportedEndpoint);
        }

        [Test]
        public void C38_JumpRiseAboveOneFailureIsReproduced()
        {
            AssertMovementDiagnostic(Sv5JumpRecoveryMode.Jump, new Sv5JumpPoint(2, 4),
                new[] { new Sv5JumpPoint(4, 2), new Sv5JumpPoint(3, 3), new Sv5JumpPoint(2, 4) },
                Sv5JumpRecoveryDiagnostic.JumpRiseExceeded);
        }

        [Test]
        public void C39_RisingDropFailureIsReproduced()
        {
            AssertMovementDiagnostic(Sv5JumpRecoveryMode.Drop, new Sv5JumpPoint(2, 3),
                new[] { new Sv5JumpPoint(4, 2), new Sv5JumpPoint(3, 3), new Sv5JumpPoint(2, 3) },
                Sv5JumpRecoveryDiagnostic.RisingDrop);
        }

        [Test]
        public void C40_LaterCheckpointShortcutFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpRecoveryRoute original = Route(source, Recipes[0], 0);
            Sv5JumpRecoveryRoute wrong = RebuildRoute(original, checkpointOrder: 1);
            AssertDiagnostic(Validate(source.Overlay, source.Probes, ReplaceRoute(source, wrong)),
                Sv5JumpRecoveryDiagnostic.LaterCheckpointShortcut);
        }

        [Test]
        public void C41_SingleCheckpointCollapseFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            IEnumerable<Sv5JumpRecoveryRoute> wrong = source.Routes.Select(value => RebuildRoute(value, checkpointOrder: 0));
            AssertDiagnostic(Validate(source.Overlay, source.Probes, wrong), Sv5JumpRecoveryDiagnostic.CheckpointDiversity);
        }

        [Test]
        public void C42_ReverseRequiredClaimFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpRecoveryRoute original = Route(source, Recipes[0], 0);
            Sv5JumpRecoveryRoute wrong = RebuildRoute(original, reverseRequired: true);
            AssertDiagnostic(Validate(source.Overlay, source.Probes, ReplaceRoute(source, wrong)),
                Sv5JumpRecoveryDiagnostic.ReverseRequired);
        }

        [Test]
        public void C43_SolidSixBySixFillFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            var extra = new List<Sv5JumpRecoveryCell>();
            foreach (string recipe in Recipes)
                for (int y = 20; y < 26; y++)
                    for (int x = 0; x < 6; x++)
                    {
                        int actualX = recipe == Recipes[0] ? x : 23 - x;
                        extra.Add(new Sv5JumpRecoveryCell(recipe, "RG_SOLID_" + y, new Sv5JumpPoint(actualX, y),
                            "SOLID", "RG_SOLID_" + y));
                    }
            AssertDiagnostic(Validate(source.Overlay.Concat(extra), source.Probes, source.Routes),
                Sv5JumpRecoveryDiagnostic.SolidFillSixBySix);
        }

        [Test]
        public void C44_FalseRecoveryReadinessFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            AssertDiagnostic(Sv5JumpRecovery.Validate(Clearance(), source.Overlay, source.Probes, source.Routes.Skip(1),
                recoveryReadyClaim: true), Sv5JumpRecoveryDiagnostic.FalseReadiness);
        }

        [Test]
        public void C45_AlreadyAtCheckpointWithMovementFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpRecoveryRoute direct = Route(source, Recipes[0], 4);
            Sv5JumpRecoveryLink borrowed = Route(source, Recipes[0], 0).Links.Single();
            Sv5JumpRecoveryRoute wrong = RebuildRoute(direct, links: new[] { borrowed });
            AssertDiagnostic(Validate(source.Overlay, source.Probes, ReplaceRoute(source, wrong)),
                Sv5JumpRecoveryDiagnostic.AlreadyAtCheckpointInvalid);
        }

        [Test]
        public void C46_DiagonalRecoveryClipFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpRecoveryRoute original = Route(source, Recipes[0], 2);
            Sv5JumpRecoveryLink link = original.Links.Single();
            Sv5JumpPoint[] points = { new Sv5JumpPoint(14, 7), new Sv5JumpPoint(13, 6),
                new Sv5JumpPoint(13, 5), new Sv5JumpPoint(13, 4) };
            var wrongLink = new Sv5JumpRecoveryLink(link.RecipeId, link.RouteId, 0, link.RecoveryLinkId,
                Sv5JumpRecoveryMode.Drop, points[0], points[points.Length - 1], Trace(points));
            AssertDiagnostic(Validate(source.Overlay, source.Probes, ReplaceRoute(source, RebuildRoute(original,
                links: new[] { wrongLink }))), Sv5JumpRecoveryDiagnostic.TraceDiagonalClip);
        }

        [Test]
        public void C47_PlayerVerifiedClaimFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            AssertDiagnostic(Sv5JumpRecovery.Validate(Clearance(), source.Overlay, source.Probes, source.Routes,
                recoveryReadyClaim: true, playerVerifiedClaim: true), Sv5JumpRecoveryDiagnostic.PlayerVerified);
        }

        [Test]
        public void C48_MissingProbeFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            AssertDiagnostic(Validate(source.Overlay, source.Probes.Skip(1), source.Routes),
                Sv5JumpRecoveryDiagnostic.ProbeSetMismatch);
        }

        [Test]
        public void C49_ProbeOriginMutationFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpMissProbe original = Probe(source, Recipes[0], 0);
            Sv5JumpMissProbe wrong = RebuildProbe(original, origin: new Sv5JumpPoint(5, 3));
            AssertDiagnostic(Validate(source.Overlay, ReplaceProbe(source, wrong), source.Routes),
                Sv5JumpRecoveryDiagnostic.ProbeOriginMismatch);
        }

        [Test]
        public void C50_MainClearanceIdentityFailureIsReproduced()
        {
            Sv5JumpRecoveryPlan source = Canonical();
            AssertDiagnostic(Sv5JumpRecovery.Validate(null, source.Overlay, source.Probes, source.Routes,
                recoveryReadyClaim: true), Sv5JumpRecoveryDiagnostic.ClearanceMismatch);
        }

        private static readonly string[] Recipes =
        {
            Sv5JumpRecipeCatalog.R0RecipeId,
            Sv5JumpRecipeCatalog.MirrorRecipeId,
        };

        private static Sv5JumpRecoveryPlan Canonical()
        {
            return Sv5JumpRecovery.CreateCanonicalLocalProof();
        }

        private static Sv5JumpClearancePlan Clearance()
        {
            return Sv5JumpClearance.CreateCanonicalLocalProof();
        }

        private static string OutputDirectory()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "MapDesign", "MCP", "GENERATED", "SV5_19_JUMP_RECOVERY");
        }

        private static Sv5JumpClearanceTrace ClearanceTrace(Sv5JumpClearancePlan plan, string recipe, int order)
        {
            return plan.Links.Single(value => value.Trace.RecipeId == recipe && value.Trace.Order == order).Trace;
        }

        private static Sv5JumpMissProbe Probe(Sv5JumpRecoveryPlan plan, string recipe, int order)
        {
            return plan.Probes.Single(value => value.RecipeId == recipe && value.FailedLinkOrder == order);
        }

        private static Sv5JumpRecoveryRoute Route(Sv5JumpRecoveryPlan plan, string recipe, int order)
        {
            return plan.Routes.Single(value => value.RecipeId == recipe && value.FailedLinkOrder == order);
        }

        private static Sv5JumpRecoveryPlan Validate(
            IEnumerable<Sv5JumpRecoveryCell> overlay,
            IEnumerable<Sv5JumpMissProbe> probes,
            IEnumerable<Sv5JumpRecoveryRoute> routes)
        {
            return Sv5JumpRecovery.Validate(Clearance(), overlay, probes, routes, recoveryReadyClaim: true);
        }

        private static IEnumerable<Sv5JumpMissProbe> ReplaceProbe(Sv5JumpRecoveryPlan source, Sv5JumpMissProbe replacement)
        {
            return source.Probes.Where(value => value.RecipeId != replacement.RecipeId ||
                value.FailedLinkOrder != replacement.FailedLinkOrder).Concat(new[] { replacement });
        }

        private static IEnumerable<Sv5JumpRecoveryRoute> ReplaceRoute(Sv5JumpRecoveryPlan source, Sv5JumpRecoveryRoute replacement)
        {
            return source.Routes.Where(value => value.RecipeId != replacement.RecipeId ||
                value.FailedLinkOrder != replacement.FailedLinkOrder).Concat(new[] { replacement });
        }

        private static Sv5JumpMissProbe RebuildProbe(
            Sv5JumpMissProbe source,
            Sv5JumpPoint? origin = null,
            Sv5JumpPoint? catchPoint = null,
            Sv5JumpPoint? landingBody = null,
            Sv5JumpPoint? landingHead = null)
        {
            return new Sv5JumpMissProbe(source.RecipeId, source.FailedLinkOrder, source.SourceLinkId,
                source.SourceSampleOrder, origin ?? source.Origin, source.CatchSource, source.CatchGroupId,
                catchPoint ?? source.CatchPoint, landingBody ?? source.LandingBody, landingHead ?? source.LandingHead,
                source.FallDistance, source.Caught);
        }

        private static Sv5JumpRecoveryRoute RebuildRoute(
            Sv5JumpRecoveryRoute source,
            int? checkpointOrder = null,
            IEnumerable<Sv5JumpRecoveryLink> links = null,
            bool? reverseRequired = null)
        {
            return new Sv5JumpRecoveryRoute(source.RecipeId, source.RouteId, source.FailedLinkOrder,
                checkpointOrder ?? source.CheckpointLinkOrder, source.RouteKind, source.Start, source.End,
                links ?? source.Links, source.RecoveryPass, reverseRequired ?? source.ReverseRequired, source.Diagnostic);
        }

        private static IEnumerable<Sv5JumpRecoveryTraceSample> Trace(IReadOnlyList<Sv5JumpPoint> points)
        {
            for (int index = 0; index < points.Count; index++)
                yield return new Sv5JumpRecoveryTraceSample(index,
                    index == 0 ? Sv5JumpRecoveryPhase.Source : index + 1 == points.Count
                        ? Sv5JumpRecoveryPhase.Target : Sv5JumpRecoveryPhase.Transit, points[index]);
        }

        private static void AssertMovementDiagnostic(
            Sv5JumpRecoveryMode mode,
            Sv5JumpPoint target,
            IReadOnlyList<Sv5JumpPoint> points,
            string diagnostic)
        {
            Sv5JumpRecoveryPlan source = Canonical();
            Sv5JumpRecoveryRoute original = Route(source, Recipes[0], 0);
            Sv5JumpRecoveryLink link = original.Links.Single();
            var wrongLink = new Sv5JumpRecoveryLink(link.RecipeId, link.RouteId, 0, link.RecoveryLinkId,
                mode, link.Source, target, Trace(points));
            AssertDiagnostic(Validate(source.Overlay, source.Probes, ReplaceRoute(source,
                RebuildRoute(original, links: new[] { wrongLink }))), diagnostic);
        }

        private static void AssertMirror(Sv5JumpPoint r0, Sv5JumpPoint mirror)
        {
            Assert.That(mirror, Is.EqualTo(new Sv5JumpPoint(23 - r0.X, r0.Y)));
        }

        private static void AssertDiagnostic(Sv5JumpRecoveryPlan plan, string diagnostic)
        {
            Assert.That(plan.Diagnostics.Any(value => value.Contains(diagnostic)), Is.True,
                string.Join("\n", plan.Diagnostics));
            Assert.That(plan.RecoveryReady, Is.False);
        }
    }
}
#endif
