using System.Globalization;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Generation;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor.WorldGeneration.Preview
{
    public static class WorldTopologyOverlaySceneDrawer
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        public static void DrawTopologyOverlay(
            WorldTopologyOverlay overlay,
            GizmoType gizmoType)
        {
            var currentEvent = Event.current;
            var sceneView = SceneView.currentDrawingSceneView;
            if (overlay == null ||
                !overlay.enabled ||
                !overlay.gameObject.activeInHierarchy ||
                !overlay.HasSnapshot ||
                currentEvent == null ||
                sceneView == null)
            {
                return;
            }

            Handles.BeginGUI();
            try
            {
                WorldTopologyOverlayGui.Draw(
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

    [CustomEditor(typeof(WorldTopologyOverlay))]
    internal sealed class WorldTopologyOverlayEditor : UnityEditor.Editor
    {
        private string seedText = "0";

        public override void OnInspectorGUI()
        {
            var overlay = (WorldTopologyOverlay)target;
            EditorGUILayout.HelpBox(
                "Preview the immutable P00 13 x 13 topology in Game and Scene views.",
                MessageType.Info);
            seedText = EditorGUILayout.TextField("Seed", seedText);

            var hasCanonicalSeed = TryParseCanonicalSeed(seedText, out var seed);
            if (!hasCanonicalSeed)
            {
                EditorGUILayout.HelpBox(
                    "Seed must be a canonical unsigned decimal ulong.",
                    MessageType.Error);
            }

            if (GUILayout.Button("Preview P00 Grid") && hasCanonicalSeed)
            {
                var result = new GridInitializationPass().Execute(seed);
                overlay.SetSnapshot(result);
                SceneView.RepaintAll();
                EditorApplication.QueuePlayerLoopUpdate();
            }

            if (GUILayout.Button("Clear"))
            {
                overlay.ClearSnapshot();
                SceneView.RepaintAll();
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        internal static bool TryParseCanonicalSeed(string text, out ulong seed)
        {
            if (!ulong.TryParse(
                    text,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out seed))
            {
                seed = 0;
                return false;
            }

            if (seed.ToString(CultureInfo.InvariantCulture) != text)
            {
                seed = 0;
                return false;
            }

            return true;
        }
    }
}
