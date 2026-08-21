#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using StarNight.Stage.CameraSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.MapAuthoring.Editor
{
    public static class StageLayoutLabBuilder
    {
        [MenuItem("Tools/Star Night/Map E08/Open Stage Layout Lab", priority = 110)]
        public static void OpenOrCreateLab()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorSceneBuildGuard.StageLayoutLabPath) == null)
            {
                RebuildLabScene();
                return;
            }

            if (!string.Equals(SceneManager.GetActiveScene().path, EditorSceneBuildGuard.StageLayoutLabPath, StringComparison.OrdinalIgnoreCase))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(EditorSceneBuildGuard.StageLayoutLabPath, OpenSceneMode.Single);
            }

            StageLayoutWorkbenchWindow.OpenWindow();
            FrameRooms();
        }

        [MenuItem("Tools/Star Night/Map E08/Rebuild Stage Layout Lab", priority = 111)]
        public static void RebuildLabScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            bool isInterruptedUnsavedLab = string.IsNullOrEmpty(activeScene.path) &&
                                          string.Equals(activeScene.name, "01_StageLayoutLab", StringComparison.Ordinal);
            if (!isInterruptedUnsavedLab && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            IReadOnlyList<RoomTemplate> templates = RoomTemplateSampleFactory.EnsureSamples();
            StageMapProfile stageProfile = StageMapProfileSampleFactory.EnsureSample();
            EnsureSceneFolder();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "01_StageLayoutLab";

            GameObject markerRoot = Create("__EDITOR_SCENE_MARKER");
            markerRoot.tag = "EditorOnly";
            Create("EditorOnlySceneMarker", markerRoot.transform);

            GameObject layoutRoot = Create("LayoutLabRoot");
            Create("PlacementGrid_2CellSnap", layoutRoot.transform).AddComponent<StageLayoutGridGuide>();
            Create("LayoutCanvasRoot", layoutRoot.transform);
            Create("RoomProxyRoot", layoutRoot.transform);
            Create("CorridorProxyRoot", layoutRoot.transform);
            Create("SocketProxyRoot", layoutRoot.transform);
            Create("GraphLineRoot", layoutRoot.transform);
            Create("ElementSlotPreviewRoot", layoutRoot.transform);
            Create("FullRoomPreviewRoot", layoutRoot.transform);

            Transform simulationRoot = Create("CameraSimulationRoot", layoutRoot.transform).transform;
            GameObject previewCameraObject = Create("PreviewCamera", simulationRoot);
            Camera previewCamera = previewCameraObject.AddComponent<Camera>();
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = CameraTileProfile.DefaultVisibleHeightTiles *
                                             StageRoomProxy.PreviewCellScale * 0.5f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
            previewCameraObject.transform.position = new Vector3(2f, 2f, -20f);
            previewCameraObject.tag = "MainCamera";
            previewCameraObject.AddComponent<CameraCriticalFrame>()
                .Configure(new CameraTileProfile(), StageRoomProxy.PreviewCellScale);
            Create("GhostPlayer", simulationRoot).transform.position = new Vector3(-5f, 1f, 0f);

            Create("MaruPathPreviewRoot", layoutRoot.transform);
            Create("ValidationMarkerRoot", layoutRoot.transform);
            Create("SeedThumbnailRoot", layoutRoot.transform);

            GameObject lightObject = Create("Main Light", layoutRoot.transform);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.7f;
            lightObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            GameObject canvas = Create("LayoutCanvas");
            CreateLabel("ModeLabel", canvas.transform, "MAP-E08 · GRAPH / ROOM", new Vector3(-8.5f, 8.4f, 0f));
            CreateLabel("SeedLabel", canvas.transform, "SEED 10801", new Vector3(-8.5f, 7.8f, 0f));
            CreateLabel("ValidationSummary", canvas.transform, "VALID · SOCKETS READY", new Vector3(3.5f, 8.4f, 0f));
            CreateLabel("SimulationControls", canvas.transform, "GRAPH  ROOM  ELEMENT SLOTS  SIMULATION", new Vector3(-8.5f, -5.4f, 0f));

            StageGeneratedLayout generated = StageMapGenerator.Generate(stageProfile, templates, 10801);
            StageLayoutPreviewApplier.Apply(generated, false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EditorSceneBuildGuard.StageLayoutLabPath);
            StageLayoutWorkbenchWindow.OpenWindow();
            FrameRooms();
            Debug.Log($"[MAP-E09] Stage Layout Lab rebuilt: Seed {generated.Seed}, {generated.Family}, Rooms {generated.Rooms.Count}, Main {generated.HasValidMainRoute}.");
        }

        private static GameObject Create(string name, Transform parent = null)
        {
            var created = new GameObject(name);
            if (parent != null) created.transform.SetParent(parent, false);
            return created;
        }

        private static TextMesh CreateLabel(string name, Transform parent, string text, Vector3 position, float size = 0.22f)
        {
            GameObject labelObject = Create(name, parent);
            labelObject.transform.position = position;
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 42;
            label.characterSize = size;
            label.color = new Color(0.82f, 0.9f, 1f);
            label.anchor = TextAnchor.UpperLeft;
            label.alignment = TextAlignment.Left;
            return label;
        }

        private static void EnsureSceneFolder()
        {
            const string folder = "Assets/_Game/Editor/MapAuthoring/Scenes";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Editor/MapAuthoring", "Scenes");
            }
        }

        private static void FrameRooms()
        {
            StageRoomProxy[] rooms = UnityEngine.Object.FindObjectsByType<StageRoomProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            UnityEngine.Object[] objects = new UnityEngine.Object[rooms.Length];
            for (int index = 0; index < rooms.Length; index++) objects[index] = rooms[index].gameObject;
            Selection.objects = objects;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        }
    }
}

#endif
