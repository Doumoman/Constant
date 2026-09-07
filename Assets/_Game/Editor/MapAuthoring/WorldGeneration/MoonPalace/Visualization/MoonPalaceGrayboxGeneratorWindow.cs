using System;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;
using UnityEditor;
using UnityEngine;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.Visualization
{
    public sealed class MoonPalaceGrayboxGeneratorWindow : EditorWindow
    {
        private string lastSummary = "No VIS02 operation has run in this editor session.";

        [MenuItem("MapDesign/MoonPalace/Graybox Generator")]
        public static void OpenWindow()
        {
            GetWindow<MoonPalaceGrayboxGeneratorWindow>("MoonPalace Graybox");
        }

        [MenuItem("MapDesign/MoonPalace/Graybox/Dry Run VIS01 Example")]
        public static void DryRunFromMenu()
        {
            RunAndShow(MoonPalaceGrayboxRunCoordinator.DryRun());
        }

        [MenuItem("MapDesign/MoonPalace/Graybox/Run VIS01 Example Scene")]
        public static void RunFromMenu()
        {
            RunAndShow(MoonPalaceGrayboxRunCoordinator.Run());
        }

        [MenuItem("MapDesign/MoonPalace/Graybox/Open VIS01 Example Scene")]
        public static void OpenVis01SceneFromMenu()
        {
            MoonPalaceGrayboxRunCoordinator.OpenVis01Scene();
        }

        private static void RunAndShow(MoonPalaceGrayboxRunResult result)
        {
            var window = GetWindow<MoonPalaceGrayboxGeneratorWindow>("MoonPalace Graybox");
            window.lastSummary = Summary(result);
            window.Repaint();
            Debug.Log(window.lastSummary);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("MoonPalace VIS01 Graybox Generator", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Seed Id", MoonPalaceGrayboxRunRequest.RequiredSeedId);
            EditorGUILayout.IntField("Seed Value", MoonPalaceGrayboxRunRequest.RequiredSeedValue);
            EditorGUILayout.Vector2IntField("Sector", new Vector2Int(6, 6));
            EditorGUILayout.IntField("4x4 Candidates", MoonPalaceGrayboxRunRequest.RequiredCandidateCount);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            if (GUILayout.Button("Dry-run (generation/count/digest only)"))
                lastSummary = Summary(MoonPalaceGrayboxRunCoordinator.DryRun());
            if (GUILayout.Button("Run (regenerate isolated VIS01 Scene)"))
                lastSummary = Summary(MoonPalaceGrayboxRunCoordinator.Run());
            if (GUILayout.Button("Open VIS01 Example Scene")) MoonPalaceGrayboxRunCoordinator.OpenVis01Scene();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Run Summary", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(lastSummary, EditorStyles.textArea, GUILayout.MinHeight(70f));
        }

        public static string GetLastRunSummary()
        {
            return MoonPalaceGrayboxRunCoordinator.LastRun == null
                ? "No VIS02 operation has run in this editor session."
                : Summary(MoonPalaceGrayboxRunCoordinator.LastRun);
        }

        private static string Summary(MoonPalaceGrayboxRunResult result)
        {
            return "VIS02 " + result.Mode + " success=" + result.Success + " scene_written=" + result.SceneWritten +
                   " candidates=" + result.Request.CandidateCount + " chunks=" + result.Generation.MicroChunks.Count +
                   " digest=" + result.LogicalMapDigest;
        }
    }
}
