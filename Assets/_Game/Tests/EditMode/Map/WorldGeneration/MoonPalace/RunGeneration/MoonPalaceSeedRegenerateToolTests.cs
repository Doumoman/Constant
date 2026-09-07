using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Tests.EditMode.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public sealed class MoonPalaceSeedRegenerateToolTests
    {
        private const string CategoryName = "RUN05";
        private const string BuilderTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration.MoonPalaceSeedRegenerateSceneBuilder";
        private const string WindowTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration.MoonPalaceRunPreviewGeneratorWindow";
        private const string Run04ResultRelativePath = "MapDesign/MCP/REPORTS/RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS_RESULT.md";
        private const string Run04ResultSha256 = "fb632fce9d8905ed92443517e07e152ef3ba59752e7b2255ff4c0298a3302914";
        private const int Seed = 1924737067;
        private MoonPalaceSeededRunResult active;
        private MoonPalaceSeededRunResult repeated;
        private MoonPalaceSeededRunResult differentSeed;
        private MoonPalaceSeededRunResult differentRecipe;
        private object publication;
        private string[] buildSettingsBefore;

        [OneTimeSetUp]
        public void GenerateRun05Publication()
        {
            buildSettingsBefore = EditorBuildSettings.scenes.Select(scene => scene.path).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            active = MoonPalaceSeededRunGenerator.Generate("WIDE_BRANCH_RUN", Seed);
            repeated = MoonPalaceSeededRunGenerator.Generate("WIDE_BRANCH_RUN", Seed);
            differentSeed = MoonPalaceSeededRunGenerator.Generate("WIDE_BRANCH_RUN", Seed + 1);
            differentRecipe = MoonPalaceSeededRunGenerator.Generate("TALL_LOOP_RUN", Seed);
            publication = InvokeBuilder("Publish", ProjectRoot, "WIDE_BRANCH_RUN", Seed);
        }

        [Test, Category(CategoryName)]
        public void Run04PrerequisiteResultShaAndPassStatusAreRecognized()
        {
            var path = Resolve(Run04ResultRelativePath);
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Does.Contain("STATUS: PASS"));
            Assert.That(HashFile(path), Is.EqualTo(Run04ResultSha256));
        }

        [Test, Category(CategoryName)]
        public void RecipeCatalogContainsAtLeastThreeRecipes()
        {
            Assert.That(MoonPalaceSeededRunRecipeCatalog.Recipes.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(MoonPalaceSeededRunRecipeCatalog.Recipes.Select(recipe => recipe.CanonicalDigest).Distinct().Count(), Is.EqualTo(MoonPalaceSeededRunRecipeCatalog.Recipes.Count));
        }

        [Test, Category(CategoryName)]
        public void RequiredRecipeIdsArePresent()
        {
            var ids = MoonPalaceSeededRunRecipeCatalog.Recipes.Select(recipe => recipe.RecipeId).ToArray();
            Assert.That(ids, Does.Contain("WIDE_BRANCH_RUN"));
            Assert.That(ids, Does.Contain("TALL_LOOP_RUN"));
            Assert.That(ids, Does.Contain("COMPACT_SPLIT_RUN"));
        }

        [Test, Category(CategoryName)]
        public void EveryRecipeDerivesTileDimensionsFromPatternDimensionsTimesFour()
        {
            Assert.That(MoonPalaceSeededRunRecipeCatalog.Recipes.All(recipe => recipe.TileWidth == recipe.PatternGridWidth * 4 && recipe.TileHeight == recipe.PatternGridHeight * 4), Is.True);
        }

        [Test, Category(CategoryName)]
        public void NoRecipeUsesTheLegacy48By32SectorAsItsPrimaryDimensions()
        {
            Assert.That(MoonPalaceSeededRunRecipeCatalog.Recipes.All(recipe => recipe.PatternGridWidth != 48 || recipe.PatternGridHeight != 32), Is.True);
            Assert.That(MoonPalaceSeededRunRecipeCatalog.Recipes.All(recipe => recipe.TileWidth != 48 || recipe.TileHeight != 32), Is.True);
        }

        [Test, Category(CategoryName)]
        public void SameRecipeAndSeedProduceIdenticalStableDigests()
        {
            Assert.That(repeated.RoomGraphDigest, Is.EqualTo(active.RoomGraphDigest));
            Assert.That(repeated.PlacementDigest, Is.EqualTo(active.PlacementDigest));
            Assert.That(repeated.CourseDigest, Is.EqualTo(active.CourseDigest));
            Assert.That(repeated.Validation.CanonicalDigest, Is.EqualTo(active.Validation.CanonicalDigest));
        }

        [Test, Category(CategoryName)]
        public void SameRecipeWithDifferentSeedChangesPlacementDigest()
        {
            Assert.That(differentSeed.PlacementDigest, Is.Not.EqualTo(active.PlacementDigest));
            Assert.That(differentSeed.CourseDigest, Is.Not.EqualTo(active.CourseDigest));
        }

        [Test, Category(CategoryName)]
        public void DifferentRecipeWithSameSeedChangesRoomGraphDigest()
        {
            Assert.That(differentRecipe.RoomGraphDigest, Is.Not.EqualTo(active.RoomGraphDigest));
            Assert.That(differentRecipe.Recipe.RecipeId, Is.Not.EqualTo(active.Recipe.RecipeId));
        }

        [Test, Category(CategoryName)]
        public void ActiveGenerationUsesOneActual500PoolCandidateInEveryPatternSlot()
        {
            Assert.That(active.CandidatePoolCount, Is.EqualTo(500));
            Assert.That(active.PatternPlacementCount, Is.EqualTo(active.Recipe.PatternGridWidth * active.Recipe.PatternGridHeight));
            Assert.That(active.Placements.All(placement => active.CandidateSet.Candidates.Any(candidate => candidate.CandidateId == placement.Candidate.CandidateId && candidate.MaskU16Hex == placement.Candidate.MaskU16Hex)), Is.True);
            Assert.That(active.Placements.All(placement => placement.Candidate.MaskU16Hex.Length == 6 && placement.Candidate.MaskU16Hex.StartsWith("0x", StringComparison.Ordinal)), Is.True);
        }

        [Test, Category(CategoryName)]
        public void ActiveGenerationStartAndExitTilesAreOpen()
        {
            Assert.That(active.IsOpen(active.StartTile.x, active.StartTile.y), Is.True);
            Assert.That(active.IsOpen(active.ExitTile.x, active.ExitTile.y), Is.True);
        }

        [Test, Category(CategoryName)]
        public void ActiveGenerationTileLevelBfsReachesExit()
        {
            var route = active.FindRouteToExit();
            Assert.That(active.Validation.StartToExitReachable, Is.True);
            Assert.That(route.First(), Is.EqualTo(active.StartTile));
            Assert.That(route.Last(), Is.EqualTo(active.ExitTile));
            Assert.That(route.Skip(1).Select((tile, index) => active.CanStep(route[index].x, route[index].y, tile.x, tile.y)).All(value => value), Is.True);
        }

        [Test, Category(CategoryName)]
        public void AllActiveRoomFramesAreInsideTheTileGrid()
        {
            Assert.That(active.RoomFrames.All(frame => frame.TileMinX >= 0 && frame.TileMinY >= 0 && frame.TileMinX + frame.TileWidth <= active.TileWidth && frame.TileMinY + frame.TileHeight <= active.TileHeight), Is.True);
            Assert.That(active.Validation.AllRoomBoundsInside, Is.True);
        }

        [Test, Category(CategoryName)]
        public void AllActiveConnectorsAreReciprocalAndOpen()
        {
            Assert.That(active.Connectors.Count, Is.GreaterThan(0));
            Assert.That(active.Connectors.All(connector => connector.IsReciprocal && connector.IsOpen && active.IsOpen(connector.FromGateTile.x, connector.FromGateTile.y) && active.IsOpen(connector.ToGateTile.x, connector.ToGateTile.y)), Is.True);
            Assert.That(active.Validation.AllConnectorsReciprocal && active.Validation.AllConnectorGateTilesOpen, Is.True);
        }

        [Test, Category(CategoryName)]
        public void GalleryPublishesAtLeastFourSuccessfulVisibleGeneratedCourses()
        {
            var gallery = GetPublicationResultList("Gallery");
            Assert.That(gallery.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(gallery.Select(result => result.Seed), Is.EqualTo(new[] { 1924737067, 1924737068, 1924737069, 1924737070 }));
            Assert.That(gallery.All(result => result.Validation.Passed && result.PatternPlacementCount == result.Recipe.PatternGridWidth * result.Recipe.PatternGridHeight), Is.True);
        }

        [Test, Category(CategoryName)]
        public void EditorWindowExposesSeedRecipeGenerateValidateAndGalleryControls()
        {
            var type = EditorType(WindowTypeName);
            Assert.That((string)type.GetField("MenuPath", BindingFlags.Public | BindingFlags.Static).GetRawConstantValue(), Is.EqualTo("Tools/MoonPalace/Run Preview Generator"));
            var window = (UnityEngine.Object)type.GetMethod("OpenForTests", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            try
            {
                var labels = ((IEnumerable)type.GetProperty("ControlLabels", BindingFlags.Public | BindingFlags.Instance).GetValue(window)).Cast<string>().ToArray();
                var recipes = ((IEnumerable)type.GetProperty("RecipeIds", BindingFlags.Public | BindingFlags.Instance).GetValue(window)).Cast<string>().ToArray();
                Assert.That(labels, Is.EquivalentTo(new[] { "Seed", "Recipe", "Generate Preview Scene", "Validate Current", "Rebuild Seed Gallery" }));
                Assert.That(recipes, Does.Contain("WIDE_BRANCH_RUN"));
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        [Test, Category(CategoryName)]
        public void Run05SceneContainsRequiredRootAndVisibleChildGroups()
        {
            var scenePath = (string)EditorType(BuilderTypeName).GetField("SceneRelativePath", BindingFlags.Public | BindingFlags.Static).GetRawConstantValue();
            Assert.That(File.Exists(Resolve(scenePath)), Is.True);
            var scene = SceneManager.GetSceneByPath(scenePath);
            Assert.That(scene.IsValid(), Is.True);
            var root = scene.GetRootGameObjects().Single(gameObject => gameObject.name == "MoonPalace_SeedRegeneratePreview_RUN05");
            var children = root.transform.Cast<Transform>().Select(child => child.name).ToArray();
            Assert.That(children, Does.Contain("ActivePreview")); Assert.That(children, Does.Contain("SeedGallery")); Assert.That(children, Does.Contain("PreviewPlayer")); Assert.That(children, Does.Contain("PreviewCamera")); Assert.That(children, Does.Contain("RouteGhost")); Assert.That(children, Does.Contain("RoomFrames")); Assert.That(children, Does.Contain("ConnectorGates")); Assert.That(children, Does.Contain("Labels")); Assert.That(children, Does.Contain("Metadata"));
            Assert.That(root.transform.Find("PreviewPlayer").GetComponent<MoonPalaceSeededRunPreviewController>(), Is.Not.Null);
            Assert.That(root.transform.Find("RouteGhost").GetComponent<MoonPalaceSeededRunPreviewRouteGhost>(), Is.Not.Null);
        }

        [Test, Category(CategoryName)]
        public void JsonAndCsvArtifactsHaveStableCountsDigestsUtf8WithoutBomAndOneFinalLf()
        {
            var outputs = GetOutputContents();
            Assert.That(outputs.Count, Is.EqualTo(16));
            Assert.That(outputs.Keys.Count(path => path.EndsWith(".json", StringComparison.Ordinal)), Is.EqualTo(9));
            Assert.That(outputs.Keys.Count(path => path.EndsWith(".csv", StringComparison.Ordinal)), Is.EqualTo(7));
            var snapshot = InvokeBuilder("BuildSnapshot", ProjectRoot, "WIDE_BRANCH_RUN", Seed);
            var snapshotOutputs = GetOutputContents(snapshot);
            Assert.That(snapshotOutputs.Keys, Is.EqualTo(outputs.Keys));
            foreach (var output in outputs)
            {
                var bytes = File.ReadAllBytes(Resolve(output.Key));
                Assert.That(bytes.Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }), Is.False, output.Key + " must be UTF-8 without BOM.");
                var text = File.ReadAllText(Resolve(output.Key));
                Assert.That(text, Is.EqualTo(output.Value), output.Key + " must match the current deterministic publication.");
                Assert.That(text, Is.EqualTo(snapshotOutputs[output.Key]));
                Assert.That(text.EndsWith("\n", StringComparison.Ordinal) && !text.EndsWith("\n\n", StringComparison.Ordinal) && !text.Contains("\r"), Is.True, output.Key + " must use one final LF only.");
                Assert.That(HashFile(Resolve(output.Key)), Is.EqualTo(HashText(output.Value)));
            }
        }

        [Test, Category(CategoryName)]
        public void NoForbiddenSelectionsRepairsBuildSettingsMutationOrRun06StartAreRecorded()
        {
            Assert.That(active.FallbackCarveCount, Is.EqualTo(0)); Assert.That(active.SilentRepairCount, Is.EqualTo(0)); Assert.That(active.RotationCount, Is.EqualTo(0));
            Assert.That(EditorBuildSettings.scenes.Select(scene => scene.path).OrderBy(path => path, StringComparer.Ordinal).ToArray(), Is.EqualTo(buildSettingsBefore));
            Assert.That(Directory.Exists(Resolve("MapDesign/MCP/GENERATED/RUN06")), Is.False);
            var generatorSource = File.ReadAllText(Resolve("Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeededRunGenerator.cs"));
            var builderSource = File.ReadAllText(Resolve("Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeedRegenerateSceneBuilder.cs"));
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False);
            Assert.That(generatorSource, Does.Not.Contain("19347")); Assert.That(builderSource, Does.Not.Contain("PlayMode")); Assert.That(builderSource, Does.Not.Contain("RunAll"));
        }

        private static object InvokeBuilder(string method, params object[] arguments) => EditorType(BuilderTypeName).GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, arguments);
        private static Type EditorType(string name) => Assembly.Load("MapAuthoring.Editor").GetType(name, true);
        private List<MoonPalaceSeededRunResult> GetPublicationResultList(string property) => ((IEnumerable)publication.GetType().GetProperty(property).GetValue(publication)).Cast<MoonPalaceSeededRunResult>().ToList();
        private Dictionary<string, string> GetOutputContents(object source = null) { var selected = source ?? publication; return ((IEnumerable)selected.GetType().GetProperty("OutputContents").GetValue(selected)).Cast<object>().ToDictionary(item => (string)item.GetType().GetProperty("Key").GetValue(item), item => (string)item.GetType().GetProperty("Value").GetValue(item), StringComparer.Ordinal); }
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
        private static string Resolve(string relativePath) => Path.Combine(ProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        private static string HashFile(string path) { using (var sha = System.Security.Cryptography.SHA256.Create()) return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value => value.ToString("x2"))); }
        private static string HashText(string text) => BakingCanonicalDigest.HashCanonicalText(text);
    }
}
