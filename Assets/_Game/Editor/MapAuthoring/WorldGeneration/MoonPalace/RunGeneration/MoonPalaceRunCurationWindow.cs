using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEngine;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Small RUN06 curation entry point. It rebuilds the bounded batch rather than selecting pre-approved seeds.</summary>
    public sealed class MoonPalaceRunCurationWindow : EditorWindow
    {
        public const string MenuPath = "Tools/MoonPalace/Run Curation";
        private readonly string[] controlLabels = { "Run Curation Batch", "Build Curation Gallery Scene", "Select Next Rejected", "Select Next Accepted" };
        private MoonPalaceCuratedRunSet curated;
        private int acceptedIndex = -1;
        private int rejectedIndex = -1;
        private string status = "Run the bounded 36-course curation batch to inspect accepted and rejected generated courses.";
        private bool statusPassed;

        public IReadOnlyList<string> ControlLabels => controlLabels;
        public MoonPalaceCuratedRunSet CurrentCuration => curated;
        public string Status => status;
        public bool StatusPassed => statusPassed;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<MoonPalaceRunCurationWindow>(); window.titleContent = new GUIContent("Run Curation"); window.minSize = new Vector2(470f, 265f); window.Show();
        }

        internal static MoonPalaceRunCurationWindow OpenForTests() { var window = CreateInstance<MoonPalaceRunCurationWindow>(); window.titleContent = new GUIContent("Run Curation"); return window; }

        internal MoonPalaceCuratedRunSet RunCurationBatch()
        {
            try
            {
                curated = MoonPalaceRunCurationBatch.Build(); acceptedIndex = -1; rejectedIndex = -1; statusPassed = true;
                status = Summary(curated) + "\nTop rejection reasons: " + curated.TopRejectionReasons + "\nRecommended RUN07 preview: " + curated.Recommended.RecipeId + " seed=" + curated.Recommended.Seed + " course=" + Short(curated.Recommended.Result.CourseDigest); Repaint(); return curated;
            }
            catch (Exception exception) { statusPassed = false; status = "Curation failed visibly: " + exception.Message; Repaint(); throw; }
        }

        internal MoonPalaceRunCurationPublication BuildCurationGalleryScene()
        {
            try
            {
                if (curated == null) RunCurationBatch(); var publication = MoonPalaceCuratedRunGallerySceneBuilder.Publish(ProjectRoot(), curated); statusPassed = true;
                status = Summary(curated) + "\nGallery Scene: " + MoonPalaceCuratedRunGallerySceneBuilder.SceneRelativePath + "\nSelected recommendation: " + curated.Recommended.RecipeId + " / " + curated.Recommended.Seed; Repaint(); return publication;
            }
            catch (Exception exception) { statusPassed = false; status = "Gallery build failed visibly: " + exception.Message; Repaint(); throw; }
        }

        internal MoonPalaceCuratedRunRecord SelectNextRejected() => SelectNext(false);
        internal MoonPalaceCuratedRunRecord SelectNextAccepted() => SelectNext(true);

        private MoonPalaceCuratedRunRecord SelectNext(bool accepted)
        {
            if (curated == null) RunCurationBatch(); var records = accepted ? curated.Accepted : curated.Rejected; if (records.Count == 0) throw new InvalidOperationException("RUN06 curation has no " + (accepted ? "accepted" : "rejected") + " records.");
            if (accepted) acceptedIndex = (acceptedIndex + 1) % records.Count; else rejectedIndex = (rejectedIndex + 1) % records.Count; var selected = records[accepted ? acceptedIndex : rejectedIndex]; statusPassed = true;
            status = Summary(curated) + "\nSelected " + selected.Status + ": " + selected.RecipeId + " seed=" + selected.Seed + "\nDigest=" + selected.Result.CourseDigest + "\nPrimary finding=" + selected.PrimaryFinding.RuleId + ": " + selected.PrimaryFinding.Reason; Repaint(); return selected;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(9f); EditorGUILayout.LabelField("Moon Palace RUN06 Curation", EditorStyles.boldLabel); EditorGUILayout.LabelField("Bounded 3 recipes × 12 seeds. Rejected runs are retained with their measured reason codes.", EditorStyles.wordWrappedMiniLabel); EditorGUILayout.Space(8f);
            if (GUILayout.Button("Run Curation Batch", GUILayout.Height(29f))) RunCurationBatch();
            if (GUILayout.Button("Build Curation Gallery Scene")) BuildCurationGalleryScene();
            EditorGUILayout.BeginHorizontal(); if (GUILayout.Button("Select Next Rejected")) SelectNextRejected(); if (GUILayout.Button("Select Next Accepted")) SelectNextAccepted(); EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Ping RUN06 Scene")) { var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MoonPalaceCuratedRunGallerySceneBuilder.SceneRelativePath); if (scene != null) EditorGUIUtility.PingObject(scene); }
            EditorGUILayout.Space(8f); var style = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = statusPassed ? new Color(0.34f, 0.93f, 0.50f) : EditorStyles.label.normal.textColor } }; EditorGUILayout.LabelField(status, style);
        }

        private static string ProjectRoot() => System.IO.Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
        private static string Summary(MoonPalaceCuratedRunSet value) => "Generated=" + value.GeneratedCount + " | Accepted=" + value.Accepted.Count + " | Rejected=" + value.Rejected.Count + " | Profile=" + Short(MoonPalaceRunQualityProfileCatalog.CanonicalDigest);
        private static string Short(string value) => string.IsNullOrEmpty(value) ? "-" : value.Substring(0, Math.Min(12, value.Length));
    }
}
