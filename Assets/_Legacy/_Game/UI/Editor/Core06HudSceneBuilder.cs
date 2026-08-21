#if LEGACY_DISABLED
using System;
using StarNight.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.UI.Editor
{
    public static class Core06HudSceneBuilder
    {
        private const string RunShellPath = "Assets/_Game/Scenes/02_RunShell.unity";
        private const string BootPath = "Assets/_Game/Scenes/00_Boot.unity";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";

        [MenuItem("Star Night/Build CORE-06 HUD")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(RunShellPath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("RunShellRoot");
            if (root == null)
            {
                throw new InvalidOperationException("02_RunShell has no RunShellRoot.");
            }

            HUDController hud = root.GetComponent<HUDController>();
            if (hud == null)
            {
                hud = root.AddComponent<HUDController>();
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                throw new InvalidOperationException("The project HUD font could not be loaded.");
            }

            hud.ConfigureFont(font);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BootPath, OpenSceneMode.Single);
            Debug.Log("CORE-06 HUD is attached to 02_RunShell.");
        }
    }
}

#endif
