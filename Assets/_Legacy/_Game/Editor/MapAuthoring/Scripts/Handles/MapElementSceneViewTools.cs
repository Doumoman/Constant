#if LEGACY_DISABLED
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    [InitializeOnLoad]
    public static class MapElementSceneViewTools
    {
        static MapElementSceneViewTools()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.duringSceneGui += OnSceneGui;
            MapElementAuthoringSession.Changed -= RefreshPreview;
            MapElementAuthoringSession.Changed += RefreshPreview;
        }

        private static void OnSceneGui(SceneView sceneView)
        {
            if (!IsLabSceneOpen())
            {
                return;
            }

            var rig = Object.FindFirstObjectByType<MapElementLabTestRig>();
            var definition = MapElementAuthoringSession.SelectedDefinition != null
                ? MapElementAuthoringSession.SelectedDefinition
                : rig != null ? rig.ActiveDefinition : null;
            var anchorObject = GameObject.Find("ActiveAuthoringElement");
            if (definition == null || anchorObject == null)
            {
                DrawInstructions("Definition을 선택하세요.");
                return;
            }

            var anchor = anchorObject.transform.position;
            var mode = MapElementAuthoringSession.EditMode;
            CellFootprintPaintTool.Draw(
                definition,
                anchor,
                sceneView,
                mode == MapElementEditMode.Footprint);
            VisualOffsetHandle.Draw(definition, anchor, mode == MapElementEditMode.Visual);
            ColliderShapeHandle.Draw(definition, anchor, mode == MapElementEditMode.Collider);
            ElementPathHandle.Draw(definition, anchor, mode == MapElementEditMode.Path);
            DrawInstructions(GetModeInstruction(mode));
        }

        private static void RefreshPreview()
        {
            if (!IsLabSceneOpen())
            {
                return;
            }

            var rig = Object.FindFirstObjectByType<MapElementLabTestRig>();
            var definition = MapElementAuthoringSession.SelectedDefinition;
            if (rig != null && definition != null)
            {
                rig.SetDefinition(definition);
            }
        }

        private static bool IsLabSceneOpen()
        {
            return string.Equals(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                EditorSceneBuildGuard.MapElementLabPath,
                System.StringComparison.OrdinalIgnoreCase);
        }

        private static string GetModeInstruction(MapElementEditMode mode)
        {
            return mode switch
            {
                MapElementEditMode.Footprint =>
                    "Footprint · Click Occupied / Shift Support / Ctrl Clearance / Alt Trigger / F Focus",
                MapElementEditMode.Visual => "Visual · Position Handle (0.05셀 스냅)",
                MapElementEditMode.Collider => "Collider · 첫 Shape Bounds Handle (0.01셀 스냅)",
                MapElementEditMode.Path => "Path · Node Position Handle (0.5셀 스냅)",
                _ => "Signal · Port 데이터는 속성 탭에서 확인",
            };
        }

        private static void DrawInstructions(string message)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(12f, 12f, 560f, 46f), EditorStyles.helpBox);
            GUILayout.Label($"MAP-E03 · {message}", EditorStyles.boldLabel);
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}

#endif
