using System.Globalization;
using StarNight.Map.WorldGeneration.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor.WorldGeneration.Preview
{
    public static class SiteReservationOverlaySceneDrawer
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        public static void DrawSiteReservationOverlay(
            SiteReservationOverlay overlay,
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
                SiteReservationOverlayGui.Draw(
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

    [CustomEditor(typeof(SiteReservationOverlay))]
    internal sealed class SiteReservationOverlayEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var overlay = (SiteReservationOverlay)target;
            EditorGUILayout.HelpBox(
                "Inject a caller-owned completed generation result by calling SetSnapshot(...). " +
                "This inspector does not run generation.",
                MessageType.Info);

            if (overlay.HasSnapshot)
            {
                var snapshot = overlay.Snapshot;
                EditorGUILayout.LabelField(
                    "Seed",
                    snapshot.Seed.ToString(CultureInfo.InvariantCulture));
                EditorGUILayout.LabelField(
                    "Summary",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} / {1} / {2} / {3} / {4} / {5}",
                        snapshot.ReservationCount,
                        snapshot.ReservedSectorCount,
                        snapshot.EntryArrowCount,
                        snapshot.CoreWitnessCount,
                        snapshot.CoreWitnessSectorCount,
                        snapshot.PassedValidationRuleCount));
                foreach (var row in snapshot.DiagnosticRows)
                {
                    EditorGUILayout.LabelField(
                        row.Label,
                        row.Value.ToString(CultureInfo.InvariantCulture));
                }
            }
            else
            {
                EditorGUILayout.LabelField("Snapshot", "NONE");
            }

            if (GUILayout.Button("Clear"))
            {
                overlay.ClearSnapshot();
                SceneView.RepaintAll();
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }
}
