using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEngine;

namespace StarNight.Character.Live.Rmap10.Editor
{
    /// <summary>RMAP10-local input surface; it has no save, network, or legacy RUN05 ownership.</summary>
    public sealed class RmapSmallRunGeneratorWindow : EditorWindow
    {
        private int seed = 1107;
        private int width = 36;
        private int height = 24;
        private RmapSmallRunRecipe recipe = RmapSmallRunRecipe.PortGalleryV1;
        private string outcome = "Choose seed and a supported 12x8-aligned size, then Generate.";

        [MenuItem("Tools/MoonPalace/RMAP10/Small Run Generator")]
        public static void Open() => GetWindow<RmapSmallRunGeneratorWindow>("RMAP10 Small Run");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("RMAP10 Seeded Small Run", EditorStyles.boldLabel);
            seed = EditorGUILayout.IntField("Seed", seed);
            width = EditorGUILayout.IntField("Width", width);
            height = EditorGUILayout.IntField("Height", height);
            recipe = (RmapSmallRunRecipe)EditorGUILayout.EnumPopup("Recipe", recipe);
            EditorGUILayout.HelpBox("Supported: 36x24 or 48x24. Width is a positive multiple of 12; height is a positive multiple of 8.", MessageType.Info);
            if (GUILayout.Button("Generate / Regenerate RMAP10 Scene"))
            {
                var request = new RmapSmallRunRequest(seed, width, height, recipe);
                RmapSmallRunPlan plan = RmapSmallRunHarness.Generate(request);
                if (!plan.Success) outcome = "Rejected: " + plan.FailureSummary;
                else
                {
                    RmapSmallRunSceneBuilder.Build(request);
                    outcome = "Generated " + width + "x" + height + " | digest " + plan.PlanDigest.Substring(0, 12) + " | Scene: " + RmapSmallRunSceneBuilder.ScenePath;
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(outcome, outcome.StartsWith("Rejected") ? MessageType.Error : MessageType.None);
        }
    }
}
