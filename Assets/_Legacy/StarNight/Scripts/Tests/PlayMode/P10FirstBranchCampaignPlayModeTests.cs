#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Campaign.P10;
using StarNight.Debugging;
using StarNight.Folklore.P9;
using StarNight.Generation.P6;
using StarNight.Maru.P8;
using StarNight.Player;
using StarNight.Stages.P5;
using StarNight.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StarNight.Tests.PlayMode
{
    public sealed class P10FirstBranchCampaignPlayModeTests
    {
        private const string ScenePath =
            "Assets/StarNight/Scenes/Game/"
            + "P10_MoonPalace_FirstBranches.unity";

        [UnityTest]
        public IEnumerator ProductionCampaign_PreservesOneCoreAcrossStages()
        {
            yield return LoadSceneAsync(ScenePath);
            Scene scene = SceneManager.GetActiveScene();
            P10FirstBranchCampaignContract contract =
                FindSingle<P10FirstBranchCampaignContract>(scene);

            Assert.DoesNotThrow(contract.ValidateOrThrow);
            Assert.That(contract.ValidationPassed, Is.True);
            Assert.That(contract.StageNodes.Count, Is.EqualTo(9));
            Assert.That(contract.CorridorReviewPending, Is.True);
            Assert.That(contract.HumanTimingPlaytestPending, Is.True);
            Assert.That(
                contract.HumanBranchFeelPlaytestPending,
                Is.True);

            Assert.That(
                FindAll<PlayerMotor2D>(scene),
                Has.Count.EqualTo(1));
            Assert.That(
                FindAll<PlayerInputAdapter>(scene),
                Has.Count.EqualTo(1));
            Assert.That(
                FindAll<P5StageCoreLoop2D>(scene),
                Has.Count.EqualTo(1));
            Assert.That(
                FindAll<P5MomoShop2D>(scene),
                Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(
                FindAll<P5GoldPickup2D>(scene),
                Has.Count.GreaterThanOrEqualTo(27));
            Assert.That(
                FindAll<Camera>(scene).Count(camera =>
                    camera.CompareTag("MainCamera")),
                Is.EqualTo(1));

            P10StageFlowController2D flow = contract.StageFlow;
            P10CampaignDirector2D director = contract.Director;
            Transform player = flow.PersistentPlayer;
            Camera stageCamera = flow.PersistentCamera;
            PlayerInputAdapter input =
                player.GetComponent<PlayerInputAdapter>();
            PlayerToolInventory2D inventory =
                player.GetComponent<PlayerToolInventory2D>();
            P9FolkloreChainState2D folklore =
                contract.FolkloreState;
            P8MaruStageController2D maru = flow.MaruController;
            P8MaruTimeline2D timeline = maru.Timeline;
            P8MaruPursuer2D pursuer = maru.Pursuer;

            Assert.That(flow.UsesOnePersistentPlayer, Is.True);
            Assert.That(flow.UsesOnePersistentCamera, Is.True);
            Assert.That(flow.InventoryPersistsAcrossStages, Is.True);
            Assert.That(flow.FolkloreStatePersistsAcrossStages, Is.True);
            Assert.That(
                flow.MaruLifecyclePersistsAcrossStages,
                Is.True);
            Assert.That(flow.MaruController.StageActive, Is.True);
            Assert.That(
                flow.MaruController.Timeline.StageSlot,
                Is.EqualTo(P6StageSlot.X1));
            Assert.That(
                flow.MaruController.Timeline.FirstBellSeconds,
                Is.EqualTo(140f));
            Assert.That(flow.ActiveEnvironmentCount, Is.EqualTo(1));
            Assert.That(flow.ActiveNode, Is.Not.Null);
            Assert.That(
                flow.ActiveNode.StageId,
                Is.EqualTo(P10StageId.MoonPalace11));
            Assert.That(
                director.CurrentStage,
                Is.EqualTo(P10StageId.MoonPalace11));

            if (!folklore.HasMoonCake)
            {
                Assert.That(
                    folklore.GrantItem(P9FolkloreItemKind.MoonCake),
                    Is.True);
            }

            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(
                flow.ActiveNode.StageId,
                Is.EqualTo(P10StageId.MoonPalace12));
            Assert.That(
                director.HasCompleted(P10StageId.MoonPalace11),
                Is.True);
            Assert.That(
                flow.MaruController.Timeline.StageSlot,
                Is.EqualTo(P6StageSlot.X2));
            Assert.That(
                flow.MaruController.Timeline.FirstBellSeconds,
                Is.EqualTo(120f));
            Assert.That(maru.gameObject.activeInHierarchy, Is.True);
            Assert.That(
                timeline.gameObject.activeInHierarchy,
                Is.True);
            Assert.That(
                pursuer.gameObject.activeInHierarchy,
                Is.True);
            Assert.That(
                maru.BiteController.gameObject.activeInHierarchy,
                Is.True);
            Assert.That(flow.MaruRuntimeActiveInHierarchy, Is.True);
            Assert.That(flow.MaruIsAtActiveStageAnchor, Is.True);
            Assert.That(pursuer.ReturnPile, Is.Not.Null);
            Assert.That(
                Vector2.Distance(
                    pursuer.ReturnPile.transform.position,
                    flow.ActiveNode.Environment.EntryAnchor.position),
                Is.LessThan(0.001f));

            float elapsedBeforeTick = timeline.ElapsedSeconds;
            yield return null;
            yield return null;
            Assert.That(timeline.IsRunning, Is.True);
            Assert.That(
                timeline.ElapsedSeconds,
                Is.GreaterThan(elapsedBeforeTick));
            pursuer.BeginHunt();
            Assert.That(pursuer.IsHunting, Is.True);
            pursuer.StopHunt();
            AssertPersistentCore(
                flow,
                player,
                stageCamera,
                input,
                inventory,
                folklore);

            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(
                flow.ActiveNode.StageId,
                Is.EqualTo(P10StageId.MoonPalace13));
            Assert.That(
                director.HasCompleted(P10StageId.MoonPalace12),
                Is.True);
            Assert.That(
                flow.TryCompleteActiveStage(),
                Is.False,
                "The Moon Palace boss stage must remain locked "
                + "until Kungtteoki is defeated.");
            Assert.That(
                flow.MaruController.Timeline.PausedForBoss,
                Is.True);
            Assert.That(
                flow.MaruController.Timeline.IsRunning,
                Is.False);
            AssertPersistentCore(
                flow,
                player,
                stageCamera,
                input,
                inventory,
                folklore);
            Assert.DoesNotThrow(contract.ValidateOrThrow);

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ProductionCampaign_CrossRouteSkipsX1AndTunesMaru()
        {
            yield return LoadSceneAsync(ScenePath);
            Scene scene = SceneManager.GetActiveScene();
            P10FirstBranchCampaignContract contract =
                FindSingle<P10FirstBranchCampaignContract>(scene);
            P10StageFlowController2D flow = contract.StageFlow;
            P9FolkloreChainState2D folklore =
                contract.FolkloreState;

            folklore.ResetForTests(true, true);
            contract.Kungtteoki.ResetEncounterForTests();
            foreach (P10BranchBoss2D boss in contract.BranchBosses)
            {
                boss.ResetEncounterForTests();
            }

            flow.ResetFlowForTests();
            Assert.That(
                flow.TryActivateStage(P10StageId.MoonPalace11),
                Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            DefeatKungtteoki(contract.Kungtteoki);
            Assert.That(
                flow.TryCompleteActiveStage(false),
                Is.True);
            Assert.That(
                flow.TryChooseBranchAndEnter(
                    P9BranchKind.MagpieBridge),
                Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);

            P10BranchBoss2D knotSpider =
                contract.BranchBosses.Single(item =>
                    item.BossKind == P10BossKind.KnotSpider);
            DefeatBranchBoss(knotSpider);
            Assert.That(
                flow.TryCompleteActiveStage(false),
                Is.True);
            Assert.That(
                flow.TryOpenCrossRouteAndEnter(
                    P9BranchKind.MagpieBridge),
                Is.True);
            Assert.That(
                flow.ActiveNode.StageId,
                Is.EqualTo(P10StageId.DragonPalace22));
            Assert.That(
                contract.Director.HasCompleted(
                    P10StageId.DragonPalace21),
                Is.False);
            Assert.That(
                contract.Director.IsSecondBranchMode,
                Is.True);
            Assert.That(
                flow.MaruController.Timeline.FirstBellSeconds,
                Is.EqualTo(120f * 0.82f).Within(0.001f));
            Assert.That(
                flow.MaruController.Timeline.NaturalMaruDueSeconds,
                Is.EqualTo(195f * 0.82f).Within(0.001f));

            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            P10BranchBoss2D gatekeeper =
                contract.BranchBosses.Single(item =>
                    item.BossKind == P10BossKind.DragonGatekeeper);
            DefeatBranchBoss(gatekeeper);
            Assert.That(
                flow.TryCompleteActiveStage(false),
                Is.True);
            Assert.That(flow.TryEnterCommonRegion(), Is.True);
            Assert.That(
                contract.Director.CommonRegionEnteredSuccessfully,
                Is.True);
            Assert.That(flow.ActiveNode, Is.Null);
            Assert.That(flow.ActiveEnvironmentCount, Is.Zero);
            Assert.That(flow.MaruRuntimeActiveInHierarchy, Is.True);
            Assert.That(flow.MaruController.StageActive, Is.False);
            Assert.That(
                flow.MaruController.Timeline.IsRunning,
                Is.False);

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertPersistentCore(
            P10StageFlowController2D flow,
            Transform expectedPlayer,
            Camera expectedCamera,
            PlayerInputAdapter expectedInput,
            PlayerToolInventory2D expectedInventory,
            P9FolkloreChainState2D expectedFolklore)
        {
            Assert.That(flow.PersistentPlayer, Is.SameAs(expectedPlayer));
            Assert.That(flow.PersistentCamera, Is.SameAs(expectedCamera));
            Assert.That(
                flow.PersistentPlayer.GetComponent<PlayerInputAdapter>(),
                Is.SameAs(expectedInput));
            Assert.That(
                flow.PersistentPlayer.GetComponent<
                    PlayerToolInventory2D>(),
                Is.SameAs(expectedInventory));
            Assert.That(
                flow.Director.FolkloreState,
                Is.SameAs(expectedFolklore));
            Assert.That(expectedFolklore.HasMoonCake, Is.True);
            Assert.That(flow.ActiveEnvironmentCount, Is.EqualTo(1));
        }

        private static void DefeatKungtteoki(
            P10KungtteokiBoss2D boss)
        {
            Assert.That(boss.BeginEncounter(), Is.True);
            boss.TickEncounter(0f);
            for (int index = 0;
                 index < P10KungtteokiBoss2D.RequiredStarKnots;
                 index++)
            {
                Assert.That(
                    boss.TryBreakCrackedFloor(index),
                    Is.True);
            }

            Assert.That(boss.IsDefeated, Is.True);
        }

        private static void DefeatBranchBoss(
            P10BranchBoss2D boss)
        {
            Assert.That(boss.BeginEncounter(), Is.True);
            for (int index = 0;
                 index < P10BranchBoss2D.RequiredStarKnots;
                 index++)
            {
                Assert.That(
                    boss.TryEnvironmentTarget(index),
                    Is.True);
            }

            Assert.That(boss.IsDefeated, Is.True);
        }

        private static IEnumerator LoadSceneAsync(string path)
        {
            AsyncOperation operation;
#if UNITY_EDITOR
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(path),
                Is.Not.Null,
                $"P10 production scene is missing: {path}");
            operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                path,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            operation = SceneManager.LoadSceneAsync(
                path,
                LoadSceneMode.Single);
#endif
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static T FindSingle<T>(Scene scene)
            where T : Component
        {
            List<T> found = FindAll<T>(scene);
            Assert.That(found, Has.Count.EqualTo(1));
            return found[0];
        }

        private static List<T> FindAll<T>(Scene scene)
            where T : Component
        {
            List<T> found = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                found.AddRange(
                    roots[index].GetComponentsInChildren<T>(true));
            }

            return found;
        }
    }
}

#endif
