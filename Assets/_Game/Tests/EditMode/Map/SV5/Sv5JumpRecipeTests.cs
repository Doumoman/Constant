#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5JumpRecipeTests
    {
        [Test]
        public void R01_CanonicalCatalogPassesWithoutDiagnostics()
        {
            Assert.That(Canonical().Diagnostics, Is.Empty);
        }

        [Test]
        public void R02_CatalogContainsExactlyTwoRecipes()
        {
            Assert.That(Canonical().Recipes.Count, Is.EqualTo(2));
        }

        [Test]
        public void R03_RecipeIdsAndTransformsAreExact()
        {
            Sv5JumpRecipeCatalogModel catalog = Canonical();
            Assert.That(catalog.Recipe(Sv5JumpRecipeCatalog.R0RecipeId).ExportTransform, Is.EqualTo("R0"));
            Assert.That(catalog.Recipe(Sv5JumpRecipeCatalog.MirrorRecipeId).ExportTransform, Is.EqualTo("MIRROR_X"));
        }

        [Test]
        public void R04_BaseDigestIsExactSv5SixteenFixture()
        {
            Sv5JumpRecipeCatalogModel catalog = Canonical();
            Assert.That(catalog.BaseFixtureDigest, Is.EqualTo(Sv5JumpRecipeCatalog.BaseFixtureDigest));
            Assert.That(Sv5JumpOutlineGeometry.CreateCanonicalLocalFixture().Digest, Is.EqualTo(catalog.BaseFixtureDigest));
        }

        [Test]
        public void R05_EachRecipeHasTenSupports()
        {
            Assert.That(Canonical().Recipes.All(value => value.Supports.Count == 10), Is.True);
        }

        [Test]
        public void R06_EachRecipeKeepsFiveSolidAndFiveOneWaySupports()
        {
            foreach (Sv5JumpRecipeVariant recipe in Canonical().Recipes)
            {
                Assert.That(recipe.Supports.Count(value => value.Support.Kind == Sv5JumpSupportKind.Solid), Is.EqualTo(5));
                Assert.That(recipe.Supports.Count(value => value.Support.Kind == Sv5JumpSupportKind.OneWay), Is.EqualTo(5));
            }
        }

        [Test]
        public void R07_EachRecipeHasExactTwentySevenPlusSeventeenOccupancy()
        {
            foreach (Sv5JumpRecipeVariant recipe in Canonical().Recipes)
            {
                Assert.That(recipe.BaseSupportCellCount, Is.EqualTo(27));
                Assert.That(recipe.OutlineCellCount, Is.EqualTo(17));
                Assert.That(recipe.Occupancy.Count, Is.EqualTo(44));
            }
        }

        [Test]
        public void R08_EachRecipeHasNineOrderedLinks()
        {
            foreach (Sv5JumpRecipeVariant recipe in Canonical().Recipes)
                CollectionAssert.AreEqual(Enumerable.Range(0, 9), recipe.RouteLinks.Select(value => value.Link.Order));
        }

        [Test]
        public void R09_MovementMixtureIsSevenOneOne()
        {
            foreach (Sv5JumpRecipeVariant recipe in Canonical().Recipes)
            {
                Assert.That(recipe.PlusOneJumpCount, Is.EqualTo(7));
                Assert.That(recipe.PlusTwoJumpGrabCount, Is.EqualTo(1));
                Assert.That(recipe.LevelJumpCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void R10_EachRecipeHasOneExplicitGrab()
        {
            Assert.That(Canonical().Recipes.All(value => value.GrabEdges.Count == 1), Is.True);
        }

        [Test]
        public void R11_EachRecipeHasFourExactSegments()
        {
            foreach (Sv5JumpRecipeVariant recipe in Canonical().Recipes)
            {
                Assert.That(recipe.Segments.Count, Is.EqualTo(4));
                CollectionAssert.AreEqual(Enumerable.Range(0, 9).Select(index => "JS_LINK_" + index.ToString("00")),
                    recipe.Segments.SelectMany(value => value.SourceLinkIds));
            }
        }

        [Test]
        public void R12_R0SupportsMatchSourceRectangles()
        {
            Sv5JumpOutlinePlan source = Source();
            Sv5JumpRecipeVariant recipe = Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId);
            foreach (Sv5JumpRecipeSupport row in recipe.Supports)
            {
                Sv5JumpSupport expected = source.Supports.Single(value => value.SupportId == row.SourceSupportId);
                Assert.That(SupportToken(row.Support), Is.EqualTo(SupportToken(expected)));
            }
        }

        [Test]
        public void R13_MirrorSupportsUseTwentyThreeMinusRightEdge()
        {
            Sv5JumpOutlinePlan source = Source();
            Sv5JumpRecipeVariant recipe = Canonical().Recipe(Sv5JumpRecipeCatalog.MirrorRecipeId);
            foreach (Sv5JumpRecipeSupport row in recipe.Supports)
            {
                Sv5JumpSupport expected = source.Supports.Single(value => value.SupportId == row.SourceSupportId);
                Assert.That(row.Support.X, Is.EqualTo(23 - expected.TopXMax));
                Assert.That(row.Support.TopXMax, Is.EqualTo(23 - expected.TopXMin));
                Assert.That(row.Support.Y, Is.EqualTo(expected.Y));
            }
        }

        [Test]
        public void R14_R0OccupancyMatchesEverySourceCell()
        {
            CollectionAssert.AreEquivalent(Source().FinalOccupancy.Select(value => OccupancyToken(value)),
                Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId).Occupancy.Select(value => OccupancyToken(value)));
        }

        [Test]
        public void R15_MirrorOccupancyTransformsEveryXCoordinate()
        {
            string[] expected = Source().FinalOccupancy.Select(value => OccupancyToken(value, 23 - value.Point.X)).ToArray();
            string[] actual = Canonical().Recipe(Sv5JumpRecipeCatalog.MirrorRecipeId).Occupancy.Select(OccupancyToken).ToArray();
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        public void R16_R0LinkCoordinatesRemainExact()
        {
            Sv5JumpRecipeVariant recipe = Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId);
            foreach (Sv5JumpRecipeLink row in recipe.RouteLinks)
            {
                Sv5JumpGrabRouteLink expected = Source().RouteLinks.Single(value => value.LinkId == row.SourceLinkId);
                Assert.That(row.Link.Takeoff, Is.EqualTo(expected.Takeoff));
                Assert.That(row.Link.Landing, Is.EqualTo(expected.Landing));
                Assert.That(row.Link.Direction, Is.EqualTo(expected.Direction));
            }
        }

        [Test]
        public void R17_MirrorLinkCoordinatesTransformEveryXCoordinate()
        {
            Sv5JumpRecipeVariant recipe = Canonical().Recipe(Sv5JumpRecipeCatalog.MirrorRecipeId);
            foreach (Sv5JumpRecipeLink row in recipe.RouteLinks)
            {
                Sv5JumpGrabRouteLink expected = Source().RouteLinks.Single(value => value.LinkId == row.SourceLinkId);
                Assert.That(row.Link.Takeoff, Is.EqualTo(new Sv5JumpPoint(23 - expected.Takeoff.X, expected.Takeoff.Y)));
                Assert.That(row.Link.Landing, Is.EqualTo(new Sv5JumpPoint(23 - expected.Landing.X, expected.Landing.Y)));
            }
        }

        [Test]
        public void R18_MirrorDirectionsFlipLeftAndRight()
        {
            Sv5JumpRecipeVariant recipe = Canonical().Recipe(Sv5JumpRecipeCatalog.MirrorRecipeId);
            foreach (Sv5JumpRecipeLink row in recipe.RouteLinks)
            {
                Sv5JumpDirection original = Source().RouteLinks.Single(value => value.LinkId == row.SourceLinkId).Direction;
                Assert.That(row.Link.Direction, Is.Not.EqualTo(original));
            }
        }

        [Test]
        public void R19_GapAndRiseScalarsRemainExact()
        {
            foreach (Sv5JumpRecipeVariant recipe in Canonical().Recipes)
                foreach (Sv5JumpRecipeLink row in recipe.RouteLinks)
                {
                    Sv5JumpGrabRouteLink original = Source().RouteLinks.Single(value => value.LinkId == row.SourceLinkId);
                    Assert.That(row.Link.GapAir, Is.EqualTo(original.GapAir));
                    Assert.That(row.Link.Rise, Is.EqualTo(original.Rise));
                }
        }

        [Test]
        public void R20_MaximumRiseIsTwo()
        {
            Assert.That(Canonical().Recipes.All(value => value.RouteLinks.Max(link => link.Link.Rise) == 2), Is.True);
        }

        [Test]
        public void R21_R0GrabCoordinatesRemainExact()
        {
            Sv5JumpRecipeGrab grab = Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId).GrabEdges.Single();
            Assert.That(grab.Edge.Contact, Is.EqualTo(new Sv5JumpPoint(15, 6)));
            Assert.That(grab.Edge.HangBody, Is.EqualTo(new Sv5JumpPoint(16, 6)));
            Assert.That(grab.Edge.PullUp, Is.EqualTo(new Sv5JumpPoint(15, 7)));
            Assert.That(grab.Edge.Face, Is.EqualTo(Sv5JumpGrabFace.Right));
        }

        [Test]
        public void R22_MirrorGrabCoordinatesFaceAndApproachAllFlip()
        {
            Sv5JumpRecipeGrab grab = Canonical().Recipe(Sv5JumpRecipeCatalog.MirrorRecipeId).GrabEdges.Single();
            Assert.That(grab.Edge.Contact, Is.EqualTo(new Sv5JumpPoint(8, 6)));
            Assert.That(grab.Edge.HangBody, Is.EqualTo(new Sv5JumpPoint(7, 6)));
            Assert.That(grab.Edge.PullUp, Is.EqualTo(new Sv5JumpPoint(8, 7)));
            Assert.That(grab.Edge.Face, Is.EqualTo(Sv5JumpGrabFace.Left));
            Assert.That(grab.Edge.ApproachDirection, Is.EqualTo(Sv5JumpDirection.LeftToRight));
        }

        [Test]
        public void R23_MirrorGrabHeadClearanceCoordinatesTransform()
        {
            Sv5JumpRecipeGrab grab = Canonical().Recipe(Sv5JumpRecipeCatalog.MirrorRecipeId).GrabEdges.Single();
            Assert.That(grab.HangHead, Is.EqualTo(new Sv5JumpPoint(7, 7)));
            Assert.That(grab.PullUpHead, Is.EqualTo(new Sv5JumpPoint(8, 8)));
        }

        [Test]
        public void R24_AllTransformedLinksPassJumpContract()
        {
            Assert.That(Canonical().Recipes.SelectMany(value => value.RouteLinks).All(value => value.Link.ContractAccepted), Is.True);
        }

        [Test]
        public void R25_OrderedRouteKeepsSupportIdentityContinuity()
        {
            foreach (Sv5JumpRecipeVariant recipe in Canonical().Recipes)
                for (int index = 0; index < recipe.RouteLinks.Count - 1; index++)
                    Assert.That(recipe.RouteLinks[index].Link.Target.SupportId,
                        Is.EqualTo(recipe.RouteLinks[index + 1].Link.Source.SupportId));
        }

        [Test]
        public void R26_ReverseCompletionIsNotRequired()
        {
            Assert.That(Canonical().Recipes.All(value => !value.ReverseCompletionRequired), Is.True);
        }

        [Test]
        public void R27_ReferenceDigestsMatchPackageProfile()
        {
            Sv5JumpRecipeReference reference = Canonical().Reference012;
            Assert.That(reference.BeforeDigest, Is.EqualTo(Sv5JumpRecipeCatalog.ReferenceBeforeDigest));
            Assert.That(reference.DraftAfterDigest, Is.EqualTo(Sv5JumpRecipeCatalog.ReferenceDraftAfterDigest));
        }

        [Test]
        public void R28_ReferenceCellCountsAreExact()
        {
            Sv5JumpRecipeReference reference = Canonical().Reference012;
            Assert.That(Count(reference.BeforeRows, 'S'), Is.EqualTo(176));
            Assert.That(Count(reference.BeforeRows, 'A'), Is.EqualTo(546));
            Assert.That(Count(reference.BeforeRows, 'O'), Is.EqualTo(46));
            Assert.That(Count(reference.DraftAfterRows, 'S'), Is.EqualTo(249));
            Assert.That(Count(reference.DraftAfterRows, 'A'), Is.EqualTo(491));
            Assert.That(Count(reference.DraftAfterRows, 'O'), Is.EqualTo(28));
        }

        [Test]
        public void R29_ReferenceDiffIsExactlyOneHundredFortyOneCells()
        {
            Assert.That(Canonical().Reference012.Changes.Count, Is.EqualTo(141));
        }

        [Test]
        public void R30_ReferenceHasExactFiveIntentCorrespondences()
        {
            Assert.That(Canonical().Reference012.Correspondences.Select(value => value.IntentId), Is.EquivalentTo(new[]
            {
                "MIXED_SOLID_ONE_WAY", "PLUS_ONE_JUMP", "PLUS_TWO_JUMP_GRAB", "LEVEL_CONNECTION", "IRREGULAR_SOLID_BACKING"
            }));
        }

        [Test]
        public void R31_ReferenceNeverClaimsProductionOrPlayerProof()
        {
            Sv5JumpRecipeReference reference = Canonical().Reference012;
            Assert.That(reference.RuntimeRole, Is.EqualTo("REFERENCE_ONLY_NOT_RUNTIME_GEOMETRY"));
            Assert.That(reference.ProductionEquivalenceClaimed, Is.False);
            Assert.That(reference.PlayerVerified, Is.False);
            Assert.That(reference.Correspondences.All(value => !value.ProductionEquivalence && !value.PlayerVerified), Is.True);
        }

        [Test]
        public void R32_ReadinessStopsBeforeClearanceRecoveryAndPlayer()
        {
            Sv5JumpRecipeCatalogModel catalog = Canonical();
            Assert.That(catalog.JumpRecipeReady && catalog.ComposedGeometryReady, Is.True);
            Assert.That(catalog.SweptClearanceReady || catalog.RecoveryReady || catalog.PlayerVerified, Is.False);
        }

        [Test]
        public void R33_LocalTestsReportZeroWholeWorldWork()
        {
            Assert.That(Canonical().WholeWorldBuildsInNewTargetedTests, Is.Zero);
            Assert.That(Canonical().WholeWorldSearchesInNewTargetedTests, Is.Zero);
        }

        [Test]
        public void R34_CatalogAndArtifactsAreDeterministic()
        {
            Sv5JumpRecipeCatalogModel first = Canonical();
            Sv5JumpRecipeCatalogModel second = Canonical();
            Assert.That(second.Digest, Is.EqualTo(first.Digest));
            IReadOnlyDictionary<string, string> a = Sv5JumpRecipeExport.BuildArtifacts(first);
            IReadOnlyDictionary<string, string> b = Sv5JumpRecipeExport.BuildArtifacts(second);
            CollectionAssert.AreEqual(a.Keys, b.Keys);
            foreach (string key in a.Keys) Assert.That(b[key], Is.EqualTo(a[key]), key);
        }

        [Test]
        public void R35_ExportWritesEveryRequiredArtifactAndSvgLabel()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "MapDesign", "MCP", "GENERATED", "SV5_17_JUMP_RECIPES");
            Sv5JumpRecipeExport.Write(directory, Canonical());
            foreach (string relative in new[]
            {
                "jump_recipes.json", "recipe_catalog.csv", "recipe_segments.csv", "recipe_supports.csv",
                "recipe_occupancy.csv", "recipe_links.csv", "recipe_grab_edges.csv", "reference_012_before.csv",
                "reference_012_draft_after.csv", "reference_012_changes.csv", "reference_012_correspondence.csv",
                "jump_recipe_validation.json", "preview/jump_recipes.svg"
            })
                Assert.That(File.Exists(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar))), Is.True, relative);
            string svg = File.ReadAllText(Path.Combine(directory, "preview", "jump_recipes.svg"));
            foreach (string label in new[]
            {
                "JUMP012_MIXED_R0", "JUMP012_MIXED_MX", "+1 x7", "+2 Grab x1", "level x1",
                "REFERENCE ONLY", "141 cells", "NOT PRODUCTION"
            })
                Assert.That(svg, Does.Contain(label));
        }

        [Test]
        public void R36_ValidationExportReportsPassAndNoErrors()
        {
            string json = Sv5JumpRecipeExport.ValidationJson(Canonical());
            Assert.That(json, Does.Contain("\"status\": \"PASS\""));
            Assert.That(json, Does.Contain("\"errors\": ["));
            Assert.That(json, Does.Not.Contain("CATALOG_MUST_CONTAIN"));
        }

        [Test]
        public void R37_WrongRecipeIdentityIsRejected()
        {
            Sv5JumpRecipeVariant source = Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId);
            AssertDiagnostic(Rebuild(source, recipeId: "WRONG"), Sv5JumpRecipeDiagnostic.RecipeIdentityMismatch);
        }

        [Test]
        public void R38_MissingSupportIsRejected()
        {
            Sv5JumpRecipeVariant source = Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId);
            AssertDiagnostic(Rebuild(source, supports: source.Supports.Skip(1)), Sv5JumpRecipeDiagnostic.SupportMismatch);
        }

        [Test]
        public void R39_MutatedOccupancyIsRejected()
        {
            Sv5JumpRecipeVariant source = Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId);
            Sv5JumpRecipeOccupancy first = source.Occupancy[0];
            var replacement = new Sv5JumpRecipeOccupancy(new Sv5JumpPoint(23, 31), first.Collision,
                first.SourceOwnerId, first.Source, first.SupportKind);
            AssertDiagnostic(Rebuild(source, occupancy: source.Occupancy.Skip(1).Concat(new[] { replacement })),
                Sv5JumpRecipeDiagnostic.OccupancyMismatch);
        }

        [Test]
        public void R40_MutatedRouteCoordinateIsRejected()
        {
            Sv5JumpRecipeVariant source = Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId);
            Sv5JumpRecipeLink[] links = source.RouteLinks.ToArray();
            Sv5JumpGrabRouteLink link = links[0].Link;
            links[0] = new Sv5JumpRecipeLink(links[0].SourceLinkId, new Sv5JumpGrabRouteLink(link.Order, link.LinkId,
                link.Source, link.Target, link.Direction, new Sv5JumpPoint(link.Takeoff.X - 1, link.Takeoff.Y),
                link.Landing, link.Mode, link.GrabEdge, link.RequiredRoute));
            AssertDiagnostic(Rebuild(source, links: links), Sv5JumpRecipeDiagnostic.RouteMismatch);
        }

        [Test]
        public void R41_MovementMixtureMutationIsRejected()
        {
            Sv5JumpRecipeVariant source = Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId);
            Sv5JumpRecipeLink[] links = source.RouteLinks.ToArray();
            Sv5JumpGrabRouteLink link = links[4].Link;
            links[4] = new Sv5JumpRecipeLink(links[4].SourceLinkId, new Sv5JumpGrabRouteLink(link.Order, link.LinkId,
                link.Source, link.Target, link.Direction, link.Takeoff, link.Landing, Sv5JumpMode.JumpGrab,
                source.GrabEdges.Single().Edge, link.RequiredRoute));
            AssertDiagnostic(Rebuild(source, links: links), Sv5JumpRecipeDiagnostic.MovementMixtureMismatch);
        }

        [Test]
        public void R42_GrabFaceMutationIsRejected()
        {
            Sv5JumpRecipeVariant source = Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId);
            Sv5JumpRecipeGrab grab = source.GrabEdges.Single();
            var edge = new Sv5JumpGrabEdge(grab.Edge.GrabEdgeId, grab.Edge.SupportId, grab.Edge.Contact,
                Sv5JumpGrabFace.Left, grab.Edge.ApproachDirection, grab.Edge.HangBody, grab.Edge.PullUp,
                grab.Edge.Exposed, grab.Edge.HangBodyClear, grab.Edge.PullUpClear);
            AssertDiagnostic(Rebuild(source, grabs: new[]
            {
                new Sv5JumpRecipeGrab(grab.SourceGrabEdgeId, edge, grab.HangHead, grab.PullUpHead)
            }), Sv5JumpRecipeDiagnostic.GrabMismatch);
        }

        [Test]
        public void R43_MissingSegmentIsRejected()
        {
            Sv5JumpRecipeVariant source = Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId);
            AssertDiagnostic(Rebuild(source, segments: source.Segments.Take(3)), Sv5JumpRecipeDiagnostic.SegmentMismatch);
        }

        [Test]
        public void R44_WrongBaseDigestIsRejected()
        {
            Sv5JumpRecipeVariant source = Canonical().Recipe(Sv5JumpRecipeCatalog.R0RecipeId);
            AssertDiagnostic(Rebuild(source, baseDigest: "WRONG"), Sv5JumpRecipeDiagnostic.BaseDigestMismatch);
        }

        [Test]
        public void R45_ReferenceGridMutationIsRejected()
        {
            Sv5JumpRecipeReference source = Canonical().Reference012;
            string[] before = source.BeforeRows.ToArray();
            before[0] = "S" + before[0].Substring(1);
            var mutated = new Sv5JumpRecipeReference(before, source.DraftAfterRows, source.Correspondences);
            Assert.That(mutated.Diagnostics.Any(value => value.StartsWith(Sv5JumpRecipeDiagnostic.ReferenceDigestMismatch,
                StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void R46_MissingRecipeIsRejectedByCatalog()
        {
            Sv5JumpRecipeCatalogModel source = Canonical();
            var mutated = new Sv5JumpRecipeCatalogModel(source.BaseFixtureDigest, source.Recipes.Take(1), source.Reference012);
            Assert.That(mutated.Diagnostics.Any(value => value.StartsWith(Sv5JumpRecipeDiagnostic.CatalogMismatch,
                StringComparison.Ordinal)), Is.True);
        }

        private static Sv5JumpRecipeCatalogModel Canonical()
        {
            return Sv5JumpRecipeCatalog.CreateCanonicalCatalog();
        }

        private static Sv5JumpOutlinePlan Source()
        {
            return Sv5JumpOutlineGeometry.CreateCanonicalLocalFixture();
        }

        private static Sv5JumpRecipeVariant Rebuild(
            Sv5JumpRecipeVariant source,
            string recipeId = null,
            string baseDigest = null,
            IEnumerable<Sv5JumpRecipeSupport> supports = null,
            IEnumerable<Sv5JumpRecipeOccupancy> occupancy = null,
            IEnumerable<Sv5JumpRecipeLink> links = null,
            IEnumerable<Sv5JumpRecipeGrab> grabs = null,
            IEnumerable<Sv5JumpRecipeSegment> segments = null)
        {
            return new Sv5JumpRecipeVariant(recipeId ?? source.RecipeId, source.Transform,
                baseDigest ?? source.BaseFixtureDigest, supports ?? source.Supports, occupancy ?? source.Occupancy,
                links ?? source.RouteLinks, grabs ?? source.GrabEdges, segments ?? source.Segments);
        }

        private static void AssertDiagnostic(Sv5JumpRecipeVariant recipe, string prefix)
        {
            Assert.That(recipe.Diagnostics.Any(value => value.StartsWith(prefix, StringComparison.Ordinal)),
                Is.True, string.Join("\n", recipe.Diagnostics));
        }

        private static string SupportToken(Sv5JumpSupport value)
        {
            return value.SupportId + "|" + value.Kind + "|" + value.X + "|" + value.Y + "|" + value.Width +
                "|" + value.Height + "|" + value.ActiveRoute + "|" + value.DecorativeOnly + "|" + value.ValidationState;
        }

        private static string OccupancyToken(Sv5JumpFinalOccupancyCell value)
        {
            return value.Point + "|" + value.Collision + "|" + value.OwnerId + "|" + value.Source + "|" + value.SupportKind;
        }

        private static string OccupancyToken(Sv5JumpFinalOccupancyCell value, int transformedX)
        {
            return new Sv5JumpPoint(transformedX, value.Point.Y) + "|" + value.Collision + "|" + value.OwnerId +
                "|" + value.Source + "|" + value.SupportKind;
        }

        private static string OccupancyToken(Sv5JumpRecipeOccupancy value)
        {
            return value.Point + "|" + value.Collision + "|" + value.SourceOwnerId + "|" + value.Source + "|" + value.SupportKind;
        }

        private static int Count(IEnumerable<string> rows, char cell)
        {
            return rows.Sum(row => row.Count(value => value == cell));
        }
    }
}
#endif
