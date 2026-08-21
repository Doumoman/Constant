#if LEGACY_DISABLED
using System;
using StarNight.UI.Menus;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.UI.Editor
{
    public static class Core08PauseSettingsSceneBuilder
    {
        private const string RunShellPath = "Assets/_Game/Scenes/02_RunShell.unity";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";

        [MenuItem("Star Night/Build CORE-08 Pause and Settings")]
        public static void Build()
        {
            string previousScenePath = SceneManager.GetActiveScene().path;
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                throw new InvalidOperationException("The project menu font could not be loaded.");
            }

            Scene scene = EditorSceneManager.OpenScene(RunShellPath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("RunShellRoot");
            if (root == null)
            {
                throw new InvalidOperationException("02_RunShell has no RunShellRoot.");
            }

            PauseMenuController pause = root.GetComponent<PauseMenuController>();
            if (pause == null)
            {
                pause = root.AddComponent<PauseMenuController>();
            }
            pause.Configure(font);
            EditorUtility.SetDirty(pause);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrWhiteSpace(previousScenePath) && previousScenePath != RunShellPath)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
            Debug.Log("CORE-08 PauseMenuController is attached to 02_RunShell; title settings open at runtime.");
        }
    }
}

#endif
