using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEngine;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Explicit RUN05 entry point for regenerating one isolated seed preview Scene.</summary>
    public sealed class MoonPalaceRunPreviewGeneratorWindow : EditorWindow
    {
        public const string MenuPath = "Tools/MoonPalace/Run Preview Generator";
        private readonly string[] controlLabels = { "Seed", "Recipe", "Generate Preview Scene", "Validate Current", "Rebuild Seed Gallery" };
        private int seed = 1924737067;
        private int recipeIndex;
        private string status = "Choose a seed and recipe, then generate the isolated RUN05 preview Scene.";
        private bool statusPassed;

        public IReadOnlyList<string> ControlLabels => controlLabels;
        public IReadOnlyList<string> RecipeIds => MoonPalaceSeededRunRecipeCatalog.Recipes.Select(recipe => recipe.RecipeId).ToArray();
        public int Seed { get => seed; set => seed = value; }
        public string SelectedRecipeId
        {
            get => MoonPalaceSeededRunRecipeCatalog.Recipes[Mathf.Clamp(recipeIndex, 0, MoonPalaceSeededRunRecipeCatalog.Recipes.Count - 1)].RecipeId;
            set
            {
                var found = MoonPalaceSeededRunRecipeCatalog.Recipes.ToList().FindIndex(recipe => string.Equals(recipe.RecipeId, value, StringComparison.Ordinal));
                if (found < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "RUN05 recipe is not registered.");
                recipeIndex = found;
            }
        }
        public string Status => status;
        public bool StatusPassed => statusPassed;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<MoonPalaceRunPreviewGeneratorWindow>();
            window.titleContent = new GUIContent("Run Preview Generator");
            window.minSize = new Vector2(410f, 245f);
            window.Show();
        }

        internal static MoonPalaceRunPreviewGeneratorWindow OpenForTests()
        {
            var window = CreateInstance<MoonPalaceRunPreviewGeneratorWindow>();
            window.titleContent = new GUIContent("Run Preview Generator");
            return window;
        }

        internal MoonPalaceSeedRegeneratePublication GeneratePreviewScene()
        {
            try
            {
                var publication = MoonPalaceSeedRegenerateSceneBuilder.Publish(ProjectRoot(), SelectedRecipeId, seed);
                statusPassed = publication.Active.Validation.Passed;
                status = "Generated RUN05 Scene: " + MoonPalaceSeedRegenerateSceneBuilder.SceneRelativePath + "\nRecipe=" + publication.Active.Recipe.RecipeId + " seed=" + publication.Active.Seed + " rooms=" + publication.Active.Rooms.Count + " connectors=" + publication.Active.Connectors.Count + " BFS=" + (publication.Active.Validation.StartToExitReachable ? "PASS" : "FAIL") + "\nCourse=" + Short(publication.Active.CourseDigest) + " placement=" + Short(publication.Active.PlacementDigest) + ".";
                Repaint();
                return publication;
            }
            catch (Exception exception)
            {
                statusPassed = false;
                status = "Generation failed visibly: " + exception.Message;
                Repaint();
                throw;
            }
        }

        internal MoonPalaceSeededRunValidation ValidateCurrent()
        {
            try
            {
                var validation = MoonPalaceSeedRegenerateSceneBuilder.ValidateCurrent(SelectedRecipeId, seed);
                statusPassed = validation.Passed;
                status = validation.Passed
                    ? "Validation PASS. Course " + Short(validation.Result.CourseDigest) + ", reachable " + validation.Result.OpenCellCount + " open tiles."
                    : "Validation FAILED visibly: " + validation.FailureReason;
                Repaint();
                return validation;
            }
            catch (Exception exception)
            {
                statusPassed = false;
                status = "Validation failed visibly: " + exception.Message;
                Repaint();
                throw;
            }
        }

        internal MoonPalaceSeedRegeneratePublication RebuildSeedGallery()
        {
            // The gallery is deterministic and fixed-seed. Publishing it also keeps ActivePreview in the selected user configuration.
            return GeneratePreviewScene();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(9f);
            EditorGUILayout.LabelField("Moon Palace RUN05", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Seed and recipe regenerate a direct 4×4 MicroPattern camera-room course.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8f);
            seed = EditorGUILayout.IntField("Seed", seed);
            recipeIndex = EditorGUILayout.Popup("Recipe", recipeIndex, RecipeIds.ToArray());
            EditorGUILayout.Space(9f);
            if (GUILayout.Button("Generate Preview Scene", GUILayout.Height(29f))) GeneratePreviewScene();
            if (GUILayout.Button("Validate Current")) ValidateCurrent();
            if (GUILayout.Button("Rebuild Seed Gallery")) RebuildSeedGallery();
            EditorGUILayout.Space(9f);
            var style = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = statusPassed ? new Color(0.32f, 0.9f, 0.47f) : EditorStyles.label.normal.textColor } };
            EditorGUILayout.LabelField(status, style);
        }

        private static string ProjectRoot() => System.IO.Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
        private static string Short(string digest) => string.IsNullOrEmpty(digest) ? "-" : digest.Substring(0, Math.Min(12, digest.Length));
    }
}
