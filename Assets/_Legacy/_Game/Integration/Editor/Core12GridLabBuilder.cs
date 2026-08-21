#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Narrative;
using StarNight.Stage.Data;
using StarNight.Stage.Flow;
using StarNight.Stage.Lab;
using StarNight.UI.HUD;
using StarNight.UI.Menus;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

namespace StarNight.Integration.Editor
{
    public static class Core12GridLabBuilder
    {
        public const string GridLabScenePath = "Assets/_Game/Scenes/99_GridLab.unity";
        public const string GridLabStagePath = "Assets/_Game/Integration/Data/Stage_GridLab.asset";
        public const string NarrativeProjectPath = "Assets/_Game/Narrative/Data/StarNightNarrative.yarnproject";
        public const string CharacterDatabasePath = "Assets/_Game/Narrative/Data/CharacterDatabase.asset";
        public const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";
        public const string MoonProfilePath = "Assets/_Game/ArtAdapters/Profiles/RegionArt_Moon.asset";
        public const string NextStagePath = "Assets/_Game/Stage/Data/Stages/Stage_1_1.asset";
        public const string DialogueNode = "STG_MOON_1_1_Intro";

        [MenuItem("Star Night/Build CORE-12 Grid Lab")]
        public static void Build()
        {
            string previousScenePath = SceneManager.GetActiveScene().path;
            AssetDatabase.ImportAsset(NarrativeProjectPath, ImportAssetOptions.ForceUpdate);

            StageDefinition definition = BuildDefinition();
            YarnProject project = AssetDatabase.LoadAssetAtPath<YarnProject>(NarrativeProjectPath);
            CharacterDatabase characters = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(CharacterDatabasePath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (project == null || Array.IndexOf(project.NodeNames, DialogueNode) < 0)
            {
                throw new InvalidOperationException("CORE-12 dialogue node is missing from the Yarn project.");
            }
            if (characters == null || font == null)
            {
                throw new InvalidOperationException("CORE-12 common narrative or font assets are missing.");
            }

            Scene scene = EditorSceneManager.OpenScene(GridLabScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("GridLabRoot");
            if (root == null)
            {
                throw new InvalidOperationException("99_GridLab has no GridLabRoot.");
            }

            Core04TwoRoomLab lab = GetOrAdd<Core04TwoRoomLab>(root);
            lab.ConfigurePrototypeLayout(Core12GridLab.RequiredRoomCount, Core12GridLab.ExitRoomIndex);
            StageSceneBootstrap stageBootstrap = GetOrAdd<StageSceneBootstrap>(root);
            stageBootstrap.Configure(definition);

            HUDController hud = GetOrAdd<HUDController>(root);
            hud.ConfigureFont(font);
            NarrativeSystemController narrative = GetOrAdd<NarrativeSystemController>(root);
            narrative.Configure(project, characters, font);
            PauseMenuController pause = GetOrAdd<PauseMenuController>(root);
            pause.Configure(font);
            Core12GridLab integration = GetOrAdd<Core12GridLab>(root);
            integration.Configure(DialogueNode);
            GetOrAdd<GridLabSoakMonitor>(root);

            Transform generatedRuntime = root.transform.Find("Core04TwoRoomRuntime");
            if (generatedRuntime != null)
            {
                UnityEngine.Object.DestroyImmediate(generatedRuntime.gameObject);
            }

            EditorUtility.SetDirty(lab);
            EditorUtility.SetDirty(stageBootstrap);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(narrative);
            EditorUtility.SetDirty(pause);
            EditorUtility.SetDirty(integration);
            EditorSceneManager.SaveScene(scene);
            AddBuildScene(GridLabScenePath);
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrWhiteSpace(previousScenePath) && previousScenePath != GridLabScenePath)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
            Debug.Log("CORE-12 GridLab is ready with four common-system rooms.");
        }

        private static StageDefinition BuildDefinition()
        {
            StageDefinition definition = AssetDatabase.LoadAssetAtPath<StageDefinition>(GridLabStagePath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<StageDefinition>();
                AssetDatabase.CreateAsset(definition, GridLabStagePath);
            }

            StageDefinition nextStage = AssetDatabase.LoadAssetAtPath<StageDefinition>(NextStagePath);
            RegionArtProfile profile = AssetDatabase.LoadAssetAtPath<RegionArtProfile>(MoonProfilePath);
            definition.stageId = "grid-lab";
            definition.displayNameKey = "CORE-12 COMMON SYSTEM GRID LAB";
            definition.sceneName = "99_GridLab";
            definition.regionId = "integration_lab";
            definition.kind = StageKind.Exploration;
            definition.generationMode = GenerationMode.Fixed;
            definition.minRooms = Core12GridLab.RequiredRoomCount;
            definition.maxRooms = Core12GridLab.RequiredRoomCount;
            definition.bell1Time = 600f;
            definition.bell2Time = 1200f;
            definition.maruSpawnTime = 1800f;
            definition.introYarnNode = string.Empty;
            definition.exitYarnNode = string.Empty;
            definition.artProfile = profile;
            definition.connections = nextStage == null
                ? Array.Empty<StageConnection>()
                : new[]
                {
                    new StageConnection
                    {
                        connectionId = "grid-lab_exit",
                        target = nextStage,
                        condition = ConnectionCondition.Always,
                        requiredFlag = string.Empty,
                        requiredItem = string.Empty,
                        visibleWhenLocked = true,
                    },
                };
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void AddBuildScene(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(scene => scene.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}

#endif
