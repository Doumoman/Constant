#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5JumpClearanceTests
    {
        [Test]
        public void C01_CanonicalLocalProofPassesWithoutDiagnostics()
        {
            Assert.That(Canonical().Diagnostics, Is.Empty);
        }

        [Test]
        public void C02_CanonicalProofContainsExactlyTwoRecipesAndEighteenLinks()
        {
            Sv5JumpClearancePlan plan = Canonical();
            Assert.That(plan.Links.Select(value => value.Trace.RecipeId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(plan.Links.Count, Is.EqualTo(18));
        }

        [Test]
        public void C03_AllSixteenNormalJumpLinksPassIndependently()
        {
            Sv5JumpClearanceLinkResult[] normal = Canonical().Links
                .Where(value => value.Trace.Mode == Sv5JumpMode.Jump).ToArray();
            Assert.That(normal.Length, Is.EqualTo(16));
            Assert.That(normal.All(value => value.ClearancePass), Is.True);
        }

        [Test]
        public void C04_BothJumpGrabLinksPassIndependently()
        {
            Sv5JumpClearanceLinkResult[] grab = Canonical().Links
                .Where(value => value.Trace.Mode == Sv5JumpMode.JumpGrab).ToArray();
            Assert.That(grab.Length, Is.EqualTo(2));
            Assert.That(grab.All(value => value.ClearancePass), Is.True);
        }

        [Test]
        public void C05_SourceAndEffectiveTakeoffsRemainDistinctOnlyForFix01Rows()
        {
            Sv5JumpClearanceTrace[] adjusted = Canonical().Links.Select(value => value.Trace)
                .Where(value => value.EndpointAdjusted).ToArray();
            Assert.That(adjusted.Length, Is.EqualTo(4));
            Assert.That(adjusted.All(value => value.CorrectionId == Sv5JumpClearance.EndpointCorrectionId), Is.True);
            Assert.That(Canonical().Links.Select(value => value.Trace).Where(value => !value.EndpointAdjusted)
                .All(value => value.SourceTakeoff.Equals(value.EffectiveTakeoff) && value.CorrectionId == string.Empty), Is.True);
        }

        [Test]
        public void C06_Fix01EffectiveTakeoffPairsAreExact()
        {
            var actual = Canonical().Links.Select(value => value.Trace).Where(value => value.EndpointAdjusted)
                .ToDictionary(value => value.RecipeId + "|" + value.Order,
                    value => value.SourceTakeoff + ">" + value.EffectiveTakeoff, StringComparer.Ordinal);
            Assert.That(actual[Sv5JumpRecipeCatalog.R0RecipeId + "|2"], Is.EqualTo("14:4>13:4"));
            Assert.That(actual[Sv5JumpRecipeCatalog.MirrorRecipeId + "|2"], Is.EqualTo("9:4>10:4"));
            Assert.That(actual[Sv5JumpRecipeCatalog.R0RecipeId + "|6"], Is.EqualTo("4:8>5:8"));
            Assert.That(actual[Sv5JumpRecipeCatalog.MirrorRecipeId + "|6"], Is.EqualTo("19:8>18:8"));
        }

        [Test]
        public void C07_EffectiveEndpointDigestMatchesApprovedCorrection()
        {
            Assert.That(Sv5JumpClearance.EffectiveLinkEndpointDigest,
                Is.EqualTo("9ca58b3070662afece14b812aff892ec8e31bac09938a7445a667f3b2aa8db78"));
        }

        [Test]
        public void C08_EveryTraceBeginsAtEffectiveTakeoffAndEndsAtSourceLanding()
        {
            foreach (Sv5JumpClearanceTrace trace in Canonical().Links.Select(value => value.Trace))
            {
                Assert.That(trace.Samples.First().Body, Is.EqualTo(trace.EffectiveTakeoff), trace.SourceLinkId);
                Assert.That(trace.Samples.Last().Body, Is.EqualTo(trace.Landing), trace.SourceLinkId);
            }
        }

        [Test]
        public void C09_EveryBodyAndHeadCellIsClearAndInLocalBounds()
        {
            Sv5JumpRecipeCatalogModel catalog = Catalog();
            foreach (Sv5JumpClearanceLinkResult result in Canonical().Links)
            {
                var occupied = new HashSet<Sv5JumpPoint>(catalog.Recipe(result.Trace.RecipeId).Occupancy.Select(value => value.Point));
                foreach (Sv5JumpClearanceSample sample in result.Trace.Samples)
                {
                    Assert.That(sample.Body.X, Is.InRange(0, 23));
                    Assert.That(sample.Body.Y, Is.InRange(0, 31));
                    Assert.That(sample.Head, Is.EqualTo(new Sv5JumpPoint(sample.Body.X, sample.Body.Y + 1)));
                    Assert.That(occupied.Contains(sample.Body) || occupied.Contains(sample.Head), Is.False,
                        result.Trace.RecipeId + ":" + result.Trace.SourceLinkId + ":" + sample.SampleOrder);
                }
            }
        }

        [Test]
        public void C10_EveryTraceUsesAtMostSixteenStatesAndThreeCellApexMargin()
        {
            foreach (Sv5JumpClearanceTrace trace in Canonical().Links.Select(value => value.Trace))
            {
                Assert.That(trace.Samples.Count, Is.LessThanOrEqualTo(16));
                Assert.That(trace.Samples.Max(value => value.Body.Y),
                    Is.LessThanOrEqualTo(Math.Max(trace.EffectiveTakeoff.Y, trace.Landing.Y) + 3));
            }
        }

        [Test]
        public void C11_TraceStepsAreLocalUniqueAndDirectionMonotonic()
        {
            foreach (Sv5JumpClearanceTrace trace in Canonical().Links.Select(value => value.Trace))
                for (int index = 0; index + 1 < trace.Samples.Count; index++)
                {
                    Sv5JumpPoint a = trace.Samples[index].Body;
                    Sv5JumpPoint b = trace.Samples[index + 1].Body;
                    int dx = b.X - a.X;
                    int dy = b.Y - a.Y;
                    Assert.That(Math.Abs(dx), Is.LessThanOrEqualTo(1));
                    Assert.That(Math.Abs(dy), Is.LessThanOrEqualTo(1));
                    Assert.That(dx == 0 && dy == 0, Is.False);
                    Assert.That(trace.Direction == Sv5JumpDirection.LeftToRight ? dx >= 0 : dx <= 0, Is.True);
                }
        }

        [Test]
        public void C12_MirrorTraceIsExactForEveryLinkAndSample()
        {
            Sv5JumpClearancePlan plan = Canonical();
            for (int order = 0; order < 9; order++)
            {
                Sv5JumpClearanceTrace r0 = Trace(plan, Sv5JumpRecipeCatalog.R0RecipeId, order);
                Sv5JumpClearanceTrace mx = Trace(plan, Sv5JumpRecipeCatalog.MirrorRecipeId, order);
                Assert.That(mx.Samples.Count, Is.EqualTo(r0.Samples.Count));
                for (int index = 0; index < r0.Samples.Count; index++)
                {
                    Assert.That(mx.Samples[index].Body,
                        Is.EqualTo(new Sv5JumpPoint(23 - r0.Samples[index].Body.X, r0.Samples[index].Body.Y)));
                    Assert.That(mx.Samples[index].Phase, Is.EqualTo(r0.Samples[index].Phase));
                }
            }
        }

        [Test]
        public void C13_GrabSequenceIsTakeoffAirHangPullUp()
        {
            foreach (Sv5JumpClearanceTrace trace in Canonical().Links.Select(value => value.Trace)
                .Where(value => value.Mode == Sv5JumpMode.JumpGrab))
                CollectionAssert.AreEqual(new[]
                {
                    Sv5JumpClearancePhase.Takeoff,
                    Sv5JumpClearancePhase.Air,
                    Sv5JumpClearancePhase.Hang,
                    Sv5JumpClearancePhase.PullUp,
                }, trace.Samples.Select(value => value.Phase));
        }

        [Test]
        public void C14_GrabContactHangAndPullUpCellsRemainExact()
        {
            Sv5JumpClearancePlan plan = Canonical();
            Sv5JumpClearanceGrabContact r0 = plan.GrabContacts.Single(value => value.RecipeId == Sv5JumpRecipeCatalog.R0RecipeId);
            Assert.That(r0.Contact, Is.EqualTo(new Sv5JumpPoint(15, 6)));
            Assert.That(r0.HangBody, Is.EqualTo(new Sv5JumpPoint(16, 6)));
            Assert.That(r0.HangHead, Is.EqualTo(new Sv5JumpPoint(16, 7)));
            Assert.That(r0.PullUp, Is.EqualTo(new Sv5JumpPoint(15, 7)));
            Assert.That(r0.PullUpHead, Is.EqualTo(new Sv5JumpPoint(15, 8)));
        }

        [Test]
        public void C15_GrabContactsAreActualSolidCellsWithExactFaces()
        {
            Sv5JumpRecipeCatalogModel catalog = Catalog();
            foreach (Sv5JumpClearanceGrabContact proof in Canonical().GrabContacts)
            {
                Sv5JumpRecipeVariant recipe = catalog.Recipe(proof.RecipeId);
                Assert.That(recipe.Occupancy.Any(value => value.Point.Equals(proof.Contact) &&
                    value.Collision == "SOLID" && value.SourceOwnerId == proof.TargetSupportId), Is.True);
                Assert.That(proof.Face, Is.EqualTo(recipe.GrabEdges.Single().Edge.Face));
            }
        }

        [Test]
        public void C16_ReadinessStopsBeforeRecoveryAndPlayerVerification()
        {
            Sv5JumpClearancePlan plan = Canonical();
            Assert.That(plan.JumpRecipeReady && plan.ComposedGeometryReady && plan.SweptClearanceReady, Is.True);
            Assert.That(plan.RecoveryReady || plan.PlayerVerified, Is.False);
        }

        [Test]
        public void C17_LocalWorkCountersRemainZero()
        {
            Sv5JumpClearancePlan plan = Canonical();
            Assert.That(plan.WholeWorldBuilds, Is.Zero);
            Assert.That(plan.WholeWorldSearches, Is.Zero);
            Assert.That(plan.GlobalEndpointComparisons, Is.Zero);
        }

        [Test]
        public void C18_CanonicalProofAndExportsAreDeterministic()
        {
            Sv5JumpClearancePlan a = Canonical();
            Sv5JumpClearancePlan b = Canonical();
            Assert.That(b.Digest, Is.EqualTo(a.Digest));
            IReadOnlyDictionary<string, string> left = Sv5JumpClearanceExport.BuildArtifacts(a);
            IReadOnlyDictionary<string, string> right = Sv5JumpClearanceExport.BuildArtifacts(b);
            CollectionAssert.AreEqual(left.Keys, right.Keys);
            foreach (string key in left.Keys) Assert.That(right[key], Is.EqualTo(left[key]), key);
        }

        [Test]
        public void C19_ExportWritesRequiredCanonicalDataAndSvg()
        {
            string directory = OutputDirectory();
            Sv5JumpClearanceExport.Write(directory, Canonical());
            foreach (string relative in new[]
            {
                "jump_clearance.json", "clearance_links.csv", "clearance_trace.csv",
                "clearance_grab_contacts.csv", "clearance_validation.json", "preview/jump_clearance.svg",
            })
                Assert.That(File.Exists(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar))), Is.True, relative);
        }

        [Test]
        public void C20_ExportCarriesCorrectionColumnsAndReadinessBoundary()
        {
            Sv5JumpClearancePlan plan = Canonical();
            string links = Sv5JumpClearanceExport.LinksCsv(plan);
            string summary = Sv5JumpClearanceExport.JumpClearanceJson(plan);
            Assert.That(links, Does.StartWith("recipe_id,order,source_link_id,mode,direction,source_takeoff_x,source_takeoff_y,takeoff_x,takeoff_y"));
            Assert.That(links.Split('\n').Count(value => value.Contains(",true,SV5_18_ENDPOINT_FIX01,")), Is.EqualTo(4));
            Assert.That(summary, Does.Contain("\"RecoveryReady\": false"));
            Assert.That(summary, Does.Contain("\"PlayerVerified\": false"));
        }

        [Test]
        public void C21_ValidationExportReportsPassAndNoErrors()
        {
            string json = Sv5JumpClearanceExport.ValidationJson(Canonical()).Replace("\r\n", "\n");
            Assert.That(json, Does.Contain("\"status\": \"PASS\""));
            Assert.That(json, Does.Contain("\"endpoint_correction_count\": 4"));
            Assert.That(json, Does.Contain("\"errors\": [\n  ]"));
        }

        [Test]
        public void C22_SvgShowsBothRecipesFootprintsCorrectionAndGrab()
        {
            string svg = Sv5JumpClearanceExport.PreviewSvg(Canonical());
            foreach (string label in new[]
            {
                "JUMP012_MIXED_R0", "JUMP012_MIXED_MX", "18 links", "Endpoint Fix01 x4",
                "body/foot trace", "head cell", "effective takeoff", "SOLID Grab contact",
                "HANG → PULL_UP", "RecoveryReady=false", "PlayerVerified=false",
            })
                Assert.That(svg, Does.Contain(label), label);
        }

        [Test]
        public void C23_BlockedBodyFailureIsReproduced()
        {
            AssertDiagnostic(MutateSample(Sv5JumpRecipeCatalog.R0RecipeId, 0, 1,
                new Sv5JumpClearanceSample(1, Sv5JumpClearancePhase.Air, new Sv5JumpPoint(0, 1))),
                Sv5JumpClearanceDiagnostic.BlockedBody);
        }

        [Test]
        public void C24_BlockedHeadFailureIsReproduced()
        {
            AssertDiagnostic(MutateSample(Sv5JumpRecipeCatalog.R0RecipeId, 0, 1,
                new Sv5JumpClearanceSample(1, Sv5JumpClearancePhase.Air, new Sv5JumpPoint(6, 1))),
                Sv5JumpClearanceDiagnostic.BlockedHead);
        }

        [Test]
        public void C25_DiagonalCornerClippingFailureIsReproduced()
        {
            Sv5JumpClearancePlan source = Canonical();
            Sv5JumpClearanceTrace trace = Trace(source, Sv5JumpRecipeCatalog.R0RecipeId, 0);
            Sv5JumpPoint[] points = { new Sv5JumpPoint(2, 2), new Sv5JumpPoint(3, 2), new Sv5JumpPoint(4, 2),
                new Sv5JumpPoint(5, 2), new Sv5JumpPoint(6, 3) };
            AssertDiagnostic(ReplaceTrace(source, Rebuild(trace, Samples(points, false))),
                Sv5JumpClearanceDiagnostic.DiagonalCornerClip);
        }

        [Test]
        public void C26_WrongEndpointFailureIsReproduced()
        {
            AssertDiagnostic(MutateSample(Sv5JumpRecipeCatalog.R0RecipeId, 0, 0,
                new Sv5JumpClearanceSample(0, Sv5JumpClearancePhase.Takeoff, new Sv5JumpPoint(1, 2))),
                Sv5JumpClearanceDiagnostic.WrongEndpoint);
        }

        [Test]
        public void C27_HorizontalReversalFailureIsReproduced()
        {
            Sv5JumpClearancePlan source = Canonical();
            Sv5JumpClearanceTrace trace = Trace(source, Sv5JumpRecipeCatalog.R0RecipeId, 0);
            Sv5JumpPoint[] points = { new Sv5JumpPoint(2, 2), new Sv5JumpPoint(1, 3), new Sv5JumpPoint(2, 3),
                new Sv5JumpPoint(3, 3), new Sv5JumpPoint(4, 3), new Sv5JumpPoint(5, 3), new Sv5JumpPoint(6, 3) };
            AssertDiagnostic(ReplaceTrace(source, Rebuild(trace, Samples(points, false))),
                Sv5JumpClearanceDiagnostic.HorizontalReversal);
        }

        [Test]
        public void C28_OverlongTraceFailureIsReproduced()
        {
            Sv5JumpClearancePlan source = Canonical();
            Sv5JumpClearanceTrace trace = Trace(source, Sv5JumpRecipeCatalog.R0RecipeId, 0);
            var samples = Enumerable.Range(0, 17).Select(index => new Sv5JumpClearanceSample(index,
                index == 0 ? Sv5JumpClearancePhase.Takeoff : index == 16 ? Sv5JumpClearancePhase.Landing : Sv5JumpClearancePhase.Air,
                index == 0 ? trace.EffectiveTakeoff : index == 16 ? trace.Landing : new Sv5JumpPoint(3 + index % 3, 4 + index / 3)));
            AssertDiagnostic(ReplaceTrace(source, Rebuild(trace, samples)), Sv5JumpClearanceDiagnostic.OverlongTrace);
        }

        [Test]
        public void C29_ExcessiveApexFailureIsReproduced()
        {
            AssertDiagnostic(MutateSample(Sv5JumpRecipeCatalog.R0RecipeId, 0, 1,
                new Sv5JumpClearanceSample(1, Sv5JumpClearancePhase.Air, new Sv5JumpPoint(3, 7))),
                Sv5JumpClearanceDiagnostic.ExcessiveApex);
        }

        [Test]
        public void C30_MissingGrabContactFailureIsReproduced()
        {
            Sv5JumpClearancePlan source = Canonical();
            AssertDiagnostic(Sv5JumpClearance.Validate(Catalog(), source.Links.Select(value => value.Trace),
                source.GrabContacts.Where(value => value.RecipeId != Sv5JumpRecipeCatalog.R0RecipeId)),
                Sv5JumpClearanceDiagnostic.MissingGrabContact);
        }

        [Test]
        public void C31_WrongGrabFaceFailureIsReproduced()
        {
            Sv5JumpClearancePlan source = Canonical();
            Sv5JumpClearanceGrabContact original = source.GrabContacts.Single(value => value.RecipeId == Sv5JumpRecipeCatalog.R0RecipeId);
            var wrong = new Sv5JumpClearanceGrabContact(original.RecipeId, original.SourceLinkId,
                original.SourceGrabEdgeId, original.TargetSupportId, original.Contact, Sv5JumpGrabFace.Left,
                original.HangBody, original.HangHead, original.PullUp, original.PullUpHead);
            AssertDiagnostic(Sv5JumpClearance.Validate(Catalog(), source.Links.Select(value => value.Trace),
                source.GrabContacts.Where(value => value.RecipeId != original.RecipeId).Concat(new[] { wrong })),
                Sv5JumpClearanceDiagnostic.WrongGrabFace);
        }

        [Test]
        public void C32_MissingHangPullUpFailureIsReproduced()
        {
            Sv5JumpClearancePlan source = Canonical();
            Sv5JumpClearanceTrace trace = Trace(source, Sv5JumpRecipeCatalog.R0RecipeId, 3);
            var samples = trace.Samples.Select(value => value.Phase == Sv5JumpClearancePhase.Hang
                ? new Sv5JumpClearanceSample(value.SampleOrder, Sv5JumpClearancePhase.Air, value.Body)
                : value);
            AssertDiagnostic(ReplaceTrace(source, Rebuild(trace, samples)), Sv5JumpClearanceDiagnostic.MissingGrabPhases);
        }

        [Test]
        public void C33_BadMirrorFailureIsReproduced()
        {
            AssertDiagnostic(MutateSample(Sv5JumpRecipeCatalog.MirrorRecipeId, 0, 1,
                new Sv5JumpClearanceSample(1, Sv5JumpClearancePhase.Air, new Sv5JumpPoint(21, 3))),
                Sv5JumpClearanceDiagnostic.BadMirror);
        }

        [Test]
        public void C34_FalseRecoveryReadinessFailureIsReproduced()
        {
            Sv5JumpClearancePlan source = Canonical();
            AssertDiagnostic(Sv5JumpClearance.Validate(Catalog(), source.Links.Select(value => value.Trace),
                source.GrabContacts, recoveryReadyClaim: true), Sv5JumpClearanceDiagnostic.FalseReadiness);
        }

        [Test]
        public void C35_FalsePlayerReadinessFailureIsReproduced()
        {
            Sv5JumpClearancePlan source = Canonical();
            AssertDiagnostic(Sv5JumpClearance.Validate(Catalog(), source.Links.Select(value => value.Trace),
                source.GrabContacts, playerVerifiedClaim: true), Sv5JumpClearanceDiagnostic.FalseReadiness);
        }

        [Test]
        public void C36_WrongEffectiveTakeoffFailureIsReproduced()
        {
            Sv5JumpClearancePlan source = Canonical();
            Sv5JumpClearanceTrace trace = Trace(source, Sv5JumpRecipeCatalog.R0RecipeId, 2);
            Sv5JumpClearanceTrace wrong = new Sv5JumpClearanceTrace(trace.RecipeId, trace.Order, trace.SourceLinkId,
                trace.Mode, trace.Direction, trace.SourceTakeoff, trace.SourceTakeoff, trace.Landing, false,
                string.Empty, trace.Samples);
            AssertDiagnostic(ReplaceTrace(source, wrong), Sv5JumpClearanceDiagnostic.EffectiveTakeoffMismatch);
        }

        [Test]
        public void C37_WrongCatalogFailureIsReproduced()
        {
            Sv5JumpRecipeCatalogModel catalog = Catalog();
            var wrong = new Sv5JumpRecipeCatalogModel("WRONG", catalog.Recipes, catalog.Reference012);
            Sv5JumpClearancePlan source = Canonical();
            AssertDiagnostic(Sv5JumpClearance.Validate(wrong, source.Links.Select(value => value.Trace),
                source.GrabContacts), Sv5JumpClearanceDiagnostic.CatalogMismatch);
        }

        private static Sv5JumpRecipeCatalogModel Catalog()
        {
            return Sv5JumpRecipeCatalog.CreateCanonicalCatalog();
        }

        private static Sv5JumpClearancePlan Canonical()
        {
            return Sv5JumpClearance.CreateCanonicalLocalProof();
        }

        private static string OutputDirectory()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "MapDesign", "MCP", "GENERATED", "SV5_18_JUMP_CLEARANCE");
        }

        private static Sv5JumpClearanceTrace Trace(Sv5JumpClearancePlan plan, string recipeId, int order)
        {
            return plan.Links.Single(value => value.Trace.RecipeId == recipeId && value.Trace.Order == order).Trace;
        }

        private static Sv5JumpClearancePlan MutateSample(
            string recipeId,
            int order,
            int sampleOrder,
            Sv5JumpClearanceSample replacement)
        {
            Sv5JumpClearancePlan source = Canonical();
            Sv5JumpClearanceTrace trace = Trace(source, recipeId, order);
            Sv5JumpClearanceSample[] samples = trace.Samples.ToArray();
            samples[sampleOrder] = replacement;
            return ReplaceTrace(source, Rebuild(trace, samples));
        }

        private static Sv5JumpClearancePlan ReplaceTrace(Sv5JumpClearancePlan source, Sv5JumpClearanceTrace replacement)
        {
            IEnumerable<Sv5JumpClearanceTrace> traces = source.Links.Select(value => value.Trace)
                .Where(value => value.RecipeId != replacement.RecipeId || value.Order != replacement.Order)
                .Concat(new[] { replacement });
            return Sv5JumpClearance.Validate(Catalog(), traces, source.GrabContacts);
        }

        private static Sv5JumpClearanceTrace Rebuild(
            Sv5JumpClearanceTrace source,
            IEnumerable<Sv5JumpClearanceSample> samples)
        {
            return new Sv5JumpClearanceTrace(source.RecipeId, source.Order, source.SourceLinkId, source.Mode,
                source.Direction, source.SourceTakeoff, source.EffectiveTakeoff, source.Landing,
                source.EndpointAdjusted, source.CorrectionId, samples);
        }

        private static IEnumerable<Sv5JumpClearanceSample> Samples(IReadOnlyList<Sv5JumpPoint> points, bool grab)
        {
            for (int index = 0; index < points.Count; index++)
                yield return new Sv5JumpClearanceSample(index,
                    index == 0 ? Sv5JumpClearancePhase.Takeoff : index + 1 == points.Count
                        ? (grab ? Sv5JumpClearancePhase.PullUp : Sv5JumpClearancePhase.Landing)
                        : grab && index + 2 == points.Count ? Sv5JumpClearancePhase.Hang : Sv5JumpClearancePhase.Air,
                    points[index]);
        }

        private static void AssertDiagnostic(Sv5JumpClearancePlan plan, string diagnostic)
        {
            Assert.That(plan.Diagnostics.Any(value => value.Contains(diagnostic)), Is.True,
                string.Join("\n", plan.Diagnostics));
            Assert.That(plan.SweptClearanceReady, Is.False);
        }
    }
}
#endif
