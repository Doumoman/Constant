#if LEGACY_DISABLED
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Debugging;
using StarNight.Player;
using StarNight.Stages.P5;
using StarNight.Tools;
using StarNight.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StarNight.Tests.PlayMode
{
    public sealed class P5MoonPalaceSlicePlayModeTests
    {
        private const string ScenePath =
            "Assets/StarNight/Scenes/Game/P5_MoonPalace_1-1_CraterWorkshop.unity";
        private const string PlayerPrefabPath =
            "Assets/StarNight/Prefabs/Gameplay/P3_Player.prefab";
        private const string CorePrefabPath =
            "Assets/StarNight/Prefabs/Core/CoreStageRoot.prefab";

        [UnityTest]
        public IEnumerator ProductionSlice_RunsCompleteP5CoreLoopWithoutTextCues()
        {
            yield return LoadProductionSceneAsync();
            Scene scene = SceneManager.GetActiveScene();

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(scene.isLoaded, Is.True);
            Assert.That(scene.path, Is.EqualTo(ScenePath));

            P5MoonStageContract contract =
                FindSingle<P5MoonStageContract>(scene);
            Assert.That(
                contract.RefreshValidation(),
                Is.True,
                contract.LastValidation);
            Assert.That(contract.IsFixedHandmadeLayout, Is.True);
            Assert.That(
                contract.ProceduralSeed,
                Is.EqualTo(P5MoonStageContract.NoProceduralSeed));
            Assert.That(
                contract.RoomPlacements.Count,
                Is.EqualTo(P5MoonStageContract.RequiredRoomCount));

            AssertSingleRuntimeContract(scene);
            AssertPlayerAndCorePrefabSources(contract);
            AssertWorldContract(contract);
            AssertNoTextComponents(scene);
            AssertNarrativeSurfacesWhenPresent(scene);
            AssertMainCameraAndLight(scene);
            Assert.That(
                CountMissingMonoBehaviours(scene),
                Is.Zero,
                "P5 production scene contains missing scripts.");

            P5StageCoreLoop2D core =
                FindSingle<P5StageCoreLoop2D>(scene);
            P5MaruBellClock2D bell =
                FindSingle<P5MaruBellClock2D>(scene);
            float timeout = Time.realtimeSinceStartup + 3f;
            while (core.State == P5CoreLoopState.Intro
                && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(
                core.State,
                Is.EqualTo(P5CoreLoopState.Active),
                "The one-second entry cue did not return control to the player.");
            Assert.That(bell.IsRunning, Is.True);
            Assert.That(
                contract.Player.GetComponent<PlayerMotor2D>().enabled,
                Is.True,
                "Player movement was not restored after entry guidance.");

            P5RunState2D runState =
                FindSingle<P5RunState2D>(scene);
            P5GoldPickup2D[] gold =
                FindSceneComponents<P5GoldPickup2D>(scene);
            Assert.That(
                gold,
                Has.Length.EqualTo(
                    P5MoonStageContract.RequiredGuaranteedGoldCount + 1));
            Array.Sort(
                gold,
                (left, right) => left.transform.position.x
                    .CompareTo(right.transform.position.x));
            for (int index = 0; index < 3; index++)
            {
                Assert.That(gold[index].CollectForTests(), Is.True);
            }

            Assert.That(runState.Gold, Is.EqualTo(3));
            P5MomoShop2D shop =
                FindSingle<P5MomoShop2D>(scene);
            P5MomoShopOffer2D ropeOffer = FindOffer(
                shop,
                P5ShopProductKind.RopeBundle3);
            PlayerConsumableTools2D consumables =
                contract.Player.GetComponent<PlayerConsumableTools2D>();
            Assert.That(
                consumables.RopeStock,
                Is.EqualTo(PlayerConsumableTools2D.DefaultRopeStock));
            Assert.That(ropeOffer.TryPurchaseForTests(), Is.True);
            Assert.That(runState.Gold, Is.Zero);
            Assert.That(
                consumables.RopeStock,
                Is.EqualTo(
                    PlayerConsumableTools2D.DefaultRopeStock + 3));

            P5StoryPestle2D story =
                FindSingle<P5StoryPestle2D>(scene);
            PlayerToolInventory2D inventory =
                contract.Player.GetComponent<PlayerToolInventory2D>();
            P5MoonRabbitPestleEvent2D rabbit =
                FindSingle<P5MoonRabbitPestleEvent2D>(scene);
            Assert.That(inventory.TryEquip(story.Pickup), Is.True);
            Assert.That(rabbit.TryResolveForTests(inventory), Is.True);
            Assert.That(runState.MoonCakes, Is.EqualTo(1));
            Assert.That(
                rabbit.State,
                Is.EqualTo(P5MoonRabbitPestleState.Completed));
            Assert.That(rabbit.TryResolveForTests(inventory), Is.False);
            Assert.That(runState.MoonCakes, Is.EqualTo(1));

            P5StageExit2D stageExit =
                FindSingle<P5StageExit2D>(scene);
            stageExit.TickForTests(0f, true, false);
            stageExit.TickForTests(0.5f, true, true);
            Assert.That(
                stageExit.State,
                Is.EqualTo(P5StageExitState.Departed));
            Assert.That(core.State, Is.EqualTo(P5CoreLoopState.Departed));
            Assert.That(bell.IsRunning, Is.False);

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator LoadProductionSceneAsync()
        {
            AsyncOperation operation;
#if UNITY_EDITOR
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath),
                Is.Not.Null,
                $"P5 production scene is missing: {ScenePath}");
            operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            operation = SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
#endif
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static void AssertSingleRuntimeContract(Scene scene)
        {
            Assert.That(
                FindSceneComponents<P5StageCoreLoop2D>(scene),
                Has.Length.EqualTo(1));
            Assert.That(
                FindSceneComponents<P5RunState2D>(scene),
                Has.Length.EqualTo(1));
            Assert.That(
                FindSceneComponents<P5MoonRabbitPestleEvent2D>(scene),
                Has.Length.EqualTo(1));
            Assert.That(
                FindSceneComponents<P5MomoShop2D>(scene),
                Has.Length.EqualTo(1));
            Assert.That(
                FindSceneComponents<P5MomoShopOffer2D>(scene),
                Has.Length.EqualTo(3));
            Assert.That(
                FindSceneComponents<P5StageExit2D>(scene),
                Has.Length.EqualTo(1));
            Assert.That(
                FindSceneComponents<P5MaruBellClock2D>(scene),
                Has.Length.EqualTo(1));
            Assert.That(
                FindSceneComponents<P5SliceTelemetry2D>(scene),
                Has.Length.EqualTo(1));
            Assert.That(
                FindSceneComponents<P5ExitGuidance2D>(scene),
                Has.Length.EqualTo(1));
            Assert.That(
                FindSceneComponents<P5StoryPestle2D>(scene),
                Has.Length.EqualTo(1));
        }

        private static void AssertPlayerAndCorePrefabSources(
            P5MoonStageContract contract)
        {
#if UNITY_EDITOR
            string[] dependencies =
                AssetDatabase.GetDependencies(ScenePath, true);
            CollectionAssert.Contains(
                dependencies,
                PlayerPrefabPath,
                "P5 scene must serialize a dependency on P3_Player.prefab.");
            CollectionAssert.Contains(
                dependencies,
                CorePrefabPath,
                "P5 scene must serialize a dependency on CoreStageRoot.prefab.");

            UnityEngine.Object playerSource =
                PrefabUtility.GetCorrespondingObjectFromSource(
                    contract.Player.gameObject);
            if (playerSource != null)
            {
                Assert.That(
                    AssetDatabase.GetAssetPath(playerSource),
                    Is.EqualTo(PlayerPrefabPath),
                    "P5 must directly accumulate on P3_Player.prefab.");
            }

            GameObject core = GameObject.Find("CoreStageRoot");
            Assert.That(core, Is.Not.Null);
            UnityEngine.Object coreSource =
                PrefabUtility.GetCorrespondingObjectFromSource(core);
            if (coreSource != null)
            {
                Assert.That(
                    AssetDatabase.GetAssetPath(coreSource),
                    Is.EqualTo(CorePrefabPath));
            }
#endif
        }

        private static void AssertWorldContract(
            P5MoonStageContract contract)
        {
            Assert.That(contract.GridWorld, Is.Not.Null);
            Assert.That(
                contract.GridWorld.Size,
                Is.EqualTo(
                    new Vector2Int(
                        P5MoonStageContract.Width,
                        P5MoonStageContract.Height)));
            Assert.That(contract.Terrain.GetUsedTilesCount(), Is.GreaterThan(0));
            Assert.That(contract.OneWay.GetUsedTilesCount(), Is.GreaterThan(0));

            TilemapCollider2D oneWayCollider =
                contract.OneWay.GetComponent<TilemapCollider2D>();
            PlatformEffector2D effector =
                contract.OneWay.GetComponent<PlatformEffector2D>();
            Assert.That(oneWayCollider, Is.Not.Null);
            Assert.That(oneWayCollider.usedByEffector, Is.True);
            Assert.That(effector, Is.Not.Null);
            Assert.That(effector.useOneWay, Is.True);
        }

        private static void AssertNoTextComponents(Scene scene)
        {
            Component[] components = FindSceneComponents<Component>(scene);
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component == null)
                {
                    continue;
                }

                Type type = component.GetType();
                string fullName = type.FullName ?? type.Name;
                bool isTextComponent =
                    fullName.Contains("TextMeshPro")
                    || fullName == "UnityEngine.UI.Text"
                    || fullName == "UnityEngine.TextMesh";
                if (!isTextComponent)
                {
                    continue;
                }

                Assert.That(
                    IsDeclaredUiSurface(component.transform),
                    Is.True,
                    "Gameplay cues must stay text-free; text is only allowed "
                    + "under the HUD or dialogue roots. Found on "
                    + $"{component.name}: {fullName}");
            }
        }

        private static bool IsDeclaredUiSurface(Transform target)
        {
            for (Transform current = target;
                current != null;
                current = current.parent)
            {
                if (current.GetComponent<StarNightUiRoot2D>() != null)
                {
                    return true;
                }

                if (current.name.StartsWith("Hud", StringComparison.Ordinal)
                    || current.name.StartsWith(
                        "Dialogue",
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // The HUD and the Maru rule dialogue only exist after the P5 builder
        // has regenerated CoreStageRoot.prefab and the production scene, so
        // these surfaces are verified when present instead of demanded.
        private static void AssertNarrativeSurfacesWhenPresent(Scene scene)
        {
            StarNightHudView2D[] huds =
                FindSceneComponents<StarNightHudView2D>(scene);
            for (int index = 0; index < huds.Length; index++)
            {
                StarNightHudView2D hud = huds[index];
                Assert.That(
                    StarNightUiRoot2D.IsInsideUiRoot(hud.transform),
                    Is.True);
                Assert.That(hud.HasLabels, Is.True);
                Assert.That(hud.HealthText, Does.StartWith("체력"));
                Assert.That(hud.RopeText, Does.StartWith("로프"));
                Assert.That(hud.BombText, Does.StartWith("폭탄"));
                Assert.That(hud.GoldText, Does.StartWith("금"));
                Assert.That(hud.MaruText, Does.StartWith("마루"));
            }

            MaruRuleDialogue2D[] dialogues =
                FindSceneComponents<MaruRuleDialogue2D>(scene);
            for (int index = 0; index < dialogues.Length; index++)
            {
                MaruRuleDialogue2D dialogue = dialogues[index];
                Assert.That(
                    StarNightUiRoot2D.IsInsideUiRoot(dialogue.transform),
                    Is.True);
                Assert.That(dialogue.Runner, Is.Not.Null);
                Assert.That(dialogue.LinePresenter, Is.Not.Null);
                Assert.That(dialogue.RequestedCount, Is.Zero);
                Assert.That(
                    dialogue.HasPlayed(MaruRuleTopic.FirstBell),
                    Is.False);
                Assert.That(
                    dialogue.HasPlayed(MaruRuleTopic.StatueBroken),
                    Is.False);
                Assert.That(
                    dialogue.HasPlayed(MaruRuleTopic.BiteEscape),
                    Is.False);
                Assert.That(
                    MaruRuleDialogue2D.NodeFor(MaruRuleTopic.FirstBell),
                    Is.EqualTo(MaruRuleDialogue2D.FirstBellNode));
            }
        }

        private static void AssertMainCameraAndLight(Scene scene)
        {
            Camera main = Camera.main;
            Assert.That(main, Is.Not.Null);
            Assert.That(main.orthographic, Is.True);
            Assert.That(main.gameObject.scene, Is.EqualTo(scene));

            Light[] lights = FindSceneComponents<Light>(scene);
            int directionalCount = 0;
            for (int index = 0; index < lights.Length; index++)
            {
                if (lights[index].type == LightType.Directional
                    && lights[index].enabled
                    && lights[index].gameObject.activeInHierarchy)
                {
                    directionalCount++;
                }
            }

            Assert.That(directionalCount, Is.EqualTo(1));
        }

        private static P5MomoShopOffer2D FindOffer(
            P5MomoShop2D shop,
            P5ShopProductKind product)
        {
            for (int index = 0; index < shop.Offers.Length; index++)
            {
                if (shop.Offers[index].Product == product)
                {
                    return shop.Offers[index];
                }
            }

            Assert.Fail($"P5 shop is missing {product}.");
            return null;
        }

        private static T FindSingle<T>(Scene scene)
            where T : Component
        {
            T[] matches = FindSceneComponents<T>(scene);
            Assert.That(
                matches,
                Has.Length.EqualTo(1),
                $"Expected exactly one {typeof(T).Name} in P5 scene.");
            return matches[0];
        }

        private static T[] FindSceneComponents<T>(Scene scene)
            where T : Component
        {
            List<T> components = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                components.AddRange(
                    roots[index].GetComponentsInChildren<T>(true));
            }

            return components.ToArray();
        }

        private static int CountMissingMonoBehaviours(Scene scene)
        {
            int missing = 0;
#if UNITY_EDITOR
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] transforms =
                    roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < transforms.Length; index++)
                {
                    missing += GameObjectUtility
                        .GetMonoBehavioursWithMissingScriptCount(
                            transforms[index].gameObject);
                }
            }
#endif
            return missing;
        }
    }
}

#endif
