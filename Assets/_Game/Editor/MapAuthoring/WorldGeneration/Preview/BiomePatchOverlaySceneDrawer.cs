using System.Globalization;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Generation;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor.WorldGeneration.Preview
{
    public static class BiomePatchOverlaySceneDrawer
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        public static void DrawBiomePatchOverlay(
            BiomePatchOverlay overlay,
            GizmoType gizmoType)
        {
            var currentEvent = Event.current;
            var sceneView = SceneView.currentDrawingSceneView;
            if (overlay == null || !overlay.enabled || !overlay.gameObject.activeInHierarchy ||
                !overlay.HasSnapshot || currentEvent == null || sceneView == null)
                return;

            Handles.BeginGUI();
            try
            {
                BiomePatchOverlayGui.Draw(
                    overlay.Snapshot,
                    currentEvent.mousePosition,
                    sceneView.position.width,
                    sceneView.position.height);
            }
            finally
            {
                Handles.EndGUI();
            }
        }
    }

    [CustomEditor(typeof(BiomePatchOverlay))]
    internal sealed class BiomePatchOverlayEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var overlay = (BiomePatchOverlay)target;
            EditorGUILayout.HelpBox(
                "Inject a caller-owned completed validation publication by calling SetSnapshot(...). " +
                "This inspector does not run generation or validation.",
                MessageType.Info);

            if (overlay.HasSnapshot)
            {
                var snapshot = overlay.Snapshot;
                EditorGUILayout.LabelField(
                    "Seed",
                    snapshot.WorldSeed.ToString(CultureInfo.InvariantCulture));
                EditorGUILayout.LabelField(
                    "Summary",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} / {1} / {2} / {3} / {4} / {5} / {6}",
                        snapshot.Patches.Count,
                        snapshot.AssignedCount,
                        snapshot.UnassignedCount,
                        snapshot.CoreCount,
                        snapshot.SatelliteCount,
                        snapshot.IntrusionCount,
                        snapshot.PassedValidationRuleCount));
                foreach (var row in snapshot.Patches)
                {
                    EditorGUILayout.LabelField(
                        row.PatchId.Value,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} | {1} | size {2} | perimeter {3} | compact {4}",
                            row.BiomeId,
                            BiomePatchRoleTokenCodec.ToToken(row.Role),
                            row.Size,
                            row.Perimeter,
                            BiomePatchOverlayGui.FormatCompactness(row.CompactnessPermille)));
                }
            }
            else
            {
                EditorGUILayout.LabelField("Snapshot", "NONE");
            }

            EditorGUI.BeginDisabledGroup(!overlay.HasSnapshot);
            try
            {
                if (GUILayout.Button("Clear") && overlay.HasSnapshot)
                {
                    overlay.ClearSnapshot();
                    SceneView.RepaintAll();
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
            finally
            {
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}
