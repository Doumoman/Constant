using StarNight.MapAuthoring.Editor.WorldGeneration.Preview;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor.WorldGeneration
{
    public sealed class WorldCoordinateDebugWindow : EditorWindow
    {
        private const string UnavailableText =
            "World: UNAVAILABLE\n" +
            "Sector: -\n" +
            "MicroChunk: -\n" +
            "Local: -";

        private string latestText = UnavailableText;

        [MenuItem("WorldGen/Coordinates")]
        public static void Open()
        {
            var window = GetWindow<WorldCoordinateDebugWindow>();
            window.titleContent = new GUIContent("World Coordinates");
            window.Focus();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("World Coordinates");
            latestText = UnavailableText;
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.duringSceneGui += OnSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Move the mouse over the Scene View.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(latestText, MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "z=0, 1 unit = 1 logical tile",
                EditorStyles.miniLabel);
        }

        private void OnSceneGui(SceneView sceneView)
        {
            var nextText = UnavailableText;
            var currentEvent = Event.current;
            if (currentEvent != null)
            {
                var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                if (!Mathf.Approximately(ray.direction.z, 0f))
                {
                    var distance = -ray.origin.z / ray.direction.z;
                    if (distance >= 0f && !float.IsNaN(distance) && !float.IsInfinity(distance))
                    {
                        var point = ray.GetPoint(distance);
                        nextText = WorldCoordinateDebugDisplay.Format(point.x, point.y);
                    }
                }
            }

            if (latestText != nextText)
            {
                latestText = nextText;
                Repaint();
            }

            Handles.BeginGUI();
            GUI.Box(
                new Rect(12f, 12f, 320f, 100f),
                GUIContent.none,
                EditorStyles.helpBox);
            GUI.Label(
                new Rect(52f, 20f, 272f, 84f),
                latestText,
                EditorStyles.wordWrappedLabel);
            Handles.EndGUI();
        }
    }
}
