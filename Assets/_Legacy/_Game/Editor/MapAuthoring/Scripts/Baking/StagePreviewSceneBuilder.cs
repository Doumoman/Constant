#if LEGACY_DISABLED
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.MapAuthoring.Editor
{
    public static class StagePreviewSceneBuilder
    {
        public const string PreviewFolder = "Assets/_Game/Editor/MapAuthoring/GeneratedPreviews";

        public static string BuildCurrentScene(StageMapProfile profile, int seed, StageLayoutSnapshot snapshot)
        {
            string stageId = profile != null && !string.IsNullOrWhiteSpace(profile.StageId)
                ? profile.StageId
                : snapshot != null ? snapshot.StageId : "MAP-E10-PREVIEW";
            string path = GetPreviewScenePath(stageId, seed);
            Scene scene = SceneManager.GetActiveScene();
            StageLayoutSimulationController controller = Object.FindFirstObjectByType<StageLayoutSimulationController>(FindObjectsInactive.Include);
            if (controller == null)
                throw new UnityException("Stage Preview Scene requires a configured StageLayoutSimulationController.");

            controller.ShowGraphMode();
            GameObject marker = new GameObject($"__STAGE_PREVIEW__{stageId}_{seed}");
            marker.tag = "EditorOnly";
            TextMesh info = marker.AddComponent<TextMesh>();
            info.text = $"{stageId} · Seed {seed} · Snapshot {(snapshot != null ? snapshot.ValidationHash : "NONE")}";
            info.characterSize = 0.12f;
            info.fontSize = 36;
            info.color = new Color(0.65f, 0.85f, 1f);
            marker.transform.position = new Vector3(0f, -1f, -0.3f);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path, true))
            {
                Object.DestroyImmediate(marker);
                throw new UnityException($"Could not save Stage Preview Scene at {path}.");
            }

            Object.DestroyImmediate(marker);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        public static string GetPreviewScenePath(string stageId, int seed)
        {
            return $"{PreviewFolder}/{StageLayoutSnapshotBaker.SanitizeFileName(stageId)}_{seed}.unity";
        }
    }
}

#endif
