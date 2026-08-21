#if LEGACY_DISABLED
using System;
using StarNight.Stage.Data;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

namespace StarNight.Narrative.Editor
{
    public static class Core07NarrativeSceneBuilder
    {
        private const string RunShellPath = "Assets/_Game/Scenes/02_RunShell.unity";
        private const string ProjectPath = "Assets/_Game/Narrative/Data/StarNightNarrative.yarnproject";
        private const string CharacterDatabasePath = "Assets/_Game/Narrative/Data/CharacterDatabase.asset";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";
        private const string StageZeroPath = "Assets/_Game/Stage/Data/Stages/Stage_0_1.asset";
        private const string StageOnePath = "Assets/_Game/Stage/Data/Stages/Stage_1_1.asset";

        [MenuItem("Star Night/Build CORE-07 Narrative")]
        public static void Build()
        {
            string previousScenePath = SceneManager.GetActiveScene().path;
            AssetDatabase.ImportAsset(ProjectPath, ImportAssetOptions.ForceUpdate);

            YarnProject project = AssetDatabase.LoadAssetAtPath<YarnProject>(ProjectPath);
            if (project == null || project.NodeNames.Length == 0)
            {
                throw new InvalidOperationException("CORE-07 Yarn Project did not compile any nodes.");
            }

            CharacterDatabase database = BuildCharacterDatabase();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                throw new InvalidOperationException("The project dialogue font could not be loaded.");
            }

            var scene = EditorSceneManager.OpenScene(RunShellPath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("RunShellRoot");
            if (root == null)
            {
                throw new InvalidOperationException("02_RunShell has no RunShellRoot.");
            }

            NarrativeSystemController controller = root.GetComponent<NarrativeSystemController>();
            if (controller == null)
            {
                controller = root.AddComponent<NarrativeSystemController>();
            }
            controller.Configure(project, database, font);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.SaveScene(scene);

            StageDefinition stageZero = AssetDatabase.LoadAssetAtPath<StageDefinition>(StageZeroPath);
            if (stageZero != null)
            {
                stageZero.introYarnNode = string.Empty;
                EditorUtility.SetDirty(stageZero);
            }
            StageDefinition stageOne = AssetDatabase.LoadAssetAtPath<StageDefinition>(StageOnePath);
            if (stageOne != null)
            {
                stageOne.introYarnNode = "STG.MOON_1_1.Intro";
                EditorUtility.SetDirty(stageOne);
            }

            AssetDatabase.SaveAssets();
            if (!string.IsNullOrWhiteSpace(previousScenePath) && previousScenePath != RunShellPath)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
            Debug.Log("CORE-07 NarrativeSystem is attached to 02_RunShell with one DialogueRunner and four presenters.");
        }

        private static CharacterDatabase BuildCharacterDatabase()
        {
            CharacterDatabase database = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(CharacterDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<CharacterDatabase>();
                AssetDatabase.CreateAsset(database, CharacterDatabasePath);
            }

            database.Configure(new[]
            {
                Character("NARRATOR", "character.narrator", string.Empty, new Color32(34, 48, 66, 255), "text.none"),
                Character("RANI", "character.rani", "라니", new Color32(40, 69, 88, 255), "text.rani"),
                Character("MARU", "character.maru", "마루", new Color32(76, 57, 91, 255), "text.maru"),
                Character("DABOK", "character.dabok", "다복", new Color32(47, 82, 74, 255), "text.dabok"),
            });
            EditorUtility.SetDirty(database);
            return database;
        }

        private static CharacterPresentation Character(string id, string nameKey, string displayName, Color color, string textSoundId)
        {
            return new CharacterPresentation
            {
                characterId = id,
                nameKey = nameKey,
                displayName = displayName,
                bubbleColor = color,
                textSoundId = textSoundId,
            };
        }
    }
}

#endif
