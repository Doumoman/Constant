#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Stage.Data;
using StarNight.Stage.Flow;
using StarNight.Stage.Lab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.Stage.Editor
{
    public static class Core05StageAssetBuilder
    {
        private const string Stage0Path = "Assets/_Game/Stage/Data/Stages/Stage_0_1.asset";
        private const string Stage1Path = "Assets/_Game/Stage/Data/Stages/Stage_1_1.asset";
        private const string ProloguePath = "Assets/_Game/Scenes/10_Prologue_0_1.unity";
        private const string MoonPath = "Assets/_Game/Scenes/11_Moon_1_1.unity";
        private const string BootPath = "Assets/_Game/Scenes/00_Boot.unity";

        [MenuItem("Star Night/Build CORE-05 Stage Assets")]
        public static void Build()
        {
            StageDefinition stage0 = LoadOrCreate(Stage0Path);
            StageDefinition stage1 = LoadOrCreate(Stage1Path);
            ConfigureMoon(stage1);
            ConfigurePrologue(stage0, stage1);
            AssetDatabase.SaveAssets();

            var prologue = EditorSceneManager.OpenScene(ProloguePath, OpenSceneMode.Single);
            ConfigureOpenScene(stage0, "StageContentRoot");
            EditorSceneManager.SaveScene(prologue);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MoonPath) == null)
            {
                if (!AssetDatabase.CopyAsset(ProloguePath, MoonPath))
                {
                    throw new InvalidOperationException("Could not create the 1-1 stage scene.");
                }
                AssetDatabase.Refresh();
            }

            var moon = EditorSceneManager.OpenScene(MoonPath, OpenSceneMode.Single);
            ConfigureOpenScene(stage1, "StageContentRoot_1_1");
            EditorSceneManager.SaveScene(moon);
            AddBuildScene(MoonPath);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BootPath, OpenSceneMode.Single);
            Debug.Log("CORE-05 stage assets and scenes are ready.");
        }

        private static StageDefinition LoadOrCreate(string path)
        {
            StageDefinition definition = AssetDatabase.LoadAssetAtPath<StageDefinition>(path);
            if (definition != null)
            {
                return definition;
            }

            definition = ScriptableObject.CreateInstance<StageDefinition>();
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static void ConfigurePrologue(StageDefinition definition, StageDefinition target)
        {
            ConfigureCommon(definition, "0-1", "별을 물어오는 밤", "10_Prologue_0_1", "prologue", StageKind.Introduction);
            definition.introYarnNode = "Stage_0_1_Intro";
            definition.exitYarnNode = "Stage_0_1_Exit";
            definition.connections = new[]
            {
                new StageConnection
                {
                    connectionId = "0-1_to_1-1",
                    target = target,
                    condition = ConnectionCondition.Always,
                    requiredFlag = string.Empty,
                    requiredItem = string.Empty,
                    visibleWhenLocked = true,
                },
            };
            EditorUtility.SetDirty(definition);
        }

        private static void ConfigureMoon(StageDefinition definition)
        {
            ConfigureCommon(definition, "1-1", "달빛 해안 1-1", "11_Moon_1_1", "moon_coast", StageKind.Exploration);
            definition.introYarnNode = "Stage_1_1_Intro";
            definition.exitYarnNode = string.Empty;
            definition.connections = Array.Empty<StageConnection>();
            EditorUtility.SetDirty(definition);
        }

        private static void ConfigureCommon(
            StageDefinition definition,
            string stageId,
            string displayName,
            string sceneName,
            string regionId,
            StageKind kind)
        {
            definition.stageId = stageId;
            definition.displayNameKey = displayName;
            definition.sceneName = sceneName;
            definition.regionId = regionId;
            definition.kind = kind;
            definition.generationMode = GenerationMode.Fixed;
            definition.minRooms = 2;
            definition.maxRooms = 2;
            definition.bell1Time = 120f;
            definition.bell2Time = 165f;
            definition.maruSpawnTime = 195f;
        }

        private static void ConfigureOpenScene(StageDefinition definition, string rootName)
        {
            Core04TwoRoomLab lab = UnityEngine.Object.FindFirstObjectByType<Core04TwoRoomLab>();
            if (lab == null)
            {
                throw new InvalidOperationException("The open stage scene has no Core04TwoRoomLab.");
            }

            lab.gameObject.name = rootName;
            StageSceneBootstrap bootstrap = lab.GetComponent<StageSceneBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = lab.gameObject.AddComponent<StageSceneBootstrap>();
            }
            bootstrap.Configure(definition);
            EditorUtility.SetDirty(bootstrap);
        }

        private static void AddBuildScene(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(scene => scene.path == scenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

#endif
