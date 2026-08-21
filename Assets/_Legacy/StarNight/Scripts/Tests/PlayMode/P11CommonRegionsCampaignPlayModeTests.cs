#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Campaign.P11;
using StarNight.Debugging;
using StarNight.Folklore.P9;
using StarNight.MapHarness.P11;
using StarNight.Maru.P8;
using StarNight.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StarNight.Tests.PlayMode
{
    public sealed class P11CommonRegionsCampaignPlayModeTests
    {
        private const string ScenePath =
            "Assets/StarNight/Scenes/Game/"
            + "P11_FullJourney_CommonEndings.unity";

        [UnityTest]
        public IEnumerator ProductionCampaign_HasNineStagesAndOneCore()
        {
            yield return LoadSceneAsync(ScenePath);
            Scene scene = SceneManager.GetActiveScene();
            P11CommonRegionsCampaignContract contract =
                FindSingle<P11CommonRegionsCampaignContract>(scene);

            Assert.DoesNotThrow(contract.ValidateOrThrow);
            Assert.That(contract.AutomatedValidationPassed, Is.True);
            Assert.That(contract.StageNodeCount, Is.EqualTo(9));
            Assert.That(contract.EnvironmentCount, Is.EqualTo(9));
            Assert.That(contract.MapHarnessCount, Is.EqualTo(9));
            Assert.That(contract.StarPostOfficeStageCount, Is.EqualTo(3));
            Assert.That(contract.SunriseGardenStageCount, Is.EqualTo(3));
            Assert.That(
                contract.PolarisObservatoryStageCount,
                Is.EqualTo(3));
            Assert.That(contract.X2StageCount, Is.EqualTo(3));

            Assert.That(
                FindAll<PlayerMotor2D>(scene),
                Has.Count.EqualTo(1));
            Assert.That(
                FindAll<Camera>(scene).Count(camera =>
                    camera.CompareTag("MainCamera")),
                Is.EqualTo(1));
            Assert.That(contract.PersistentPlayerCount, Is.EqualTo(1));
            Assert.That(contract.MainCameraCount, Is.EqualTo(1));
            Assert.That(
                contract.P10StageFlow.PersistentPlayer,
                Is.SameAs(contract.StageFlow.PersistentPlayer));
            Assert.That(
                contract.P10StageFlow.PersistentCamera,
                Is.SameAs(contract.StageFlow.PersistentCamera));
            Assert.That(
                contract.P10StageFlow.MaruController,
                Is.SameAs(contract.StageFlow.MaruController));
            Assert.That(
                contract.P10StageFlow.CommonRegionContinuation,
                Is.SameAs(contract.StageFlow));

            Assert.That(contract.HumanSampleCount, Is.Zero);
            Assert.That(contract.HumanGatesPending, Is.True);
            Assert.That(contract.HumanGatesPassed, Is.False);
            Assert.That(
                contract.Telemetry.AutomatedStructureCanSatisfyHumanGate,
                Is.False);
            Assert.That(
                contract.Telemetry
                    .CanAutomatedStructureSatisfyHumanGate(true),
                Is.False);

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CommonRegionFlow_PreservesCoreAndLocksX3Bosses()
        {
            yield return LoadSceneAsync(ScenePath);
            Scene scene = SceneManager.GetActiveScene();
            P11CommonRegionsCampaignContract contract =
                FindSingle<P11CommonRegionsCampaignContract>(scene);
            P11StageFlowController2D flow = contract.StageFlow;
            Transform player = flow.PersistentPlayer;
            Camera stageCamera = flow.PersistentCamera;
            P8MaruStageController2D maru = flow.MaruController;

            flow.ResetFlowForTests();
            Assert.That(flow.BeginAtCommonRegionForTests(), Is.True);
            AssertActiveStage(
                contract,
                P11StageId.StarPostOffice31,
                player,
                stageCamera,
                maru);

            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            AssertActiveStage(
                contract,
                P11StageId.StarPostOffice32,
                player,
                stageCamera,
                maru);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            AssertActiveStage(
                contract,
                P11StageId.StarPostOffice33,
                player,
                stageCamera,
                maru);
            Assert.That(
                flow.TryCompleteActiveStage(),
                Is.False,
                "3-3 must remain locked until Popo is defeated.");
            DefeatRegionalBoss(
                FindRegionalBoss(contract, P11BossKind.Popo));
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            AssertActiveStage(
                contract,
                P11StageId.SunriseGarden41,
                player,
                stageCamera,
                maru);

            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            AssertActiveStage(
                contract,
                P11StageId.SunriseGarden42,
                player,
                stageCamera,
                maru);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            AssertActiveStage(
                contract,
                P11StageId.SunriseGarden43,
                player,
                stageCamera,
                maru);
            Assert.That(
                flow.TryCompleteActiveStage(),
                Is.False,
                "4-3 must remain locked until Sun Flower is defeated.");
            DefeatRegionalBoss(
                FindRegionalBoss(contract, P11BossKind.SunFlower));
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            AssertActiveStage(
                contract,
                P11StageId.PolarisObservatory51,
                player,
                stageCamera,
                maru);

            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            AssertActiveStage(
                contract,
                P11StageId.PolarisObservatory52,
                player,
                stageCamera,
                maru);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            AssertActiveStage(
                contract,
                P11StageId.PolarisObservatory53,
                player,
                stageCamera,
                maru);
            Assert.That(
                flow.TryCompleteActiveStage(),
                Is.False,
                "5-3 must remain locked until a Maru ending resolves.");
            Assert.That(contract.MaruBoss.IsDefeated, Is.False);

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NormalEnding_DoesNotRequireOptionalStory()
        {
            yield return LoadSceneAsync(ScenePath);
            Scene scene = SceneManager.GetActiveScene();
            P11CommonRegionsCampaignContract contract =
                FindSingle<P11CommonRegionsCampaignContract>(scene);
            P11StageFlowController2D flow = contract.StageFlow;
            P11StoryState2D story = contract.StoryState;

            story.ResetStoryForTests();
            flow.ResetFlowForTests();
            Assert.That(flow.BeginAtCommonRegionForTests(), Is.True);
            AdvanceToFinalBoss(contract);

            Assert.That(story.HasNaraeLetter, Is.False);
            Assert.That(story.HasSunEmber, Is.False);
            Assert.That(
                contract.Director.NormalEndingAvailableWithoutLetterOrEmber,
                Is.True);
            DefeatMaruDirectly(contract.MaruBoss);

            Assert.That(contract.MaruBoss.IsDefeated, Is.True);
            Assert.That(contract.Director.HasEnded, Is.True);
            Assert.That(
                contract.Director.Ending,
                Is.EqualTo(P11EndingKind.Normal));
            Assert.That(story.MemoryRouteCompleted, Is.False);

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MemoryEnding_UsesLetterEmberAndShortShortLong()
        {
            yield return LoadSceneAsync(ScenePath);
            Scene scene = SceneManager.GetActiveScene();
            P11CommonRegionsCampaignContract contract =
                FindSingle<P11CommonRegionsCampaignContract>(scene);
            P11StageFlowController2D flow = contract.StageFlow;
            P11StoryState2D story = contract.StoryState;

            story.ResetStoryForTests();
            story.FolkloreState.ResetForTests();
            Assert.That(
                story.FolkloreState.GrantBossRelic(
                    P9BranchKind.MagpieBridge),
                Is.True);
            flow.ResetFlowForTests();
            Assert.That(flow.BeginAtCommonRegionForTests(), Is.True);

            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            DefeatRegionalBoss(
                FindRegionalBoss(contract, P11BossKind.Popo));
            Assert.That(contract.Mailbox.TryOpen(), Is.True);
            Assert.That(story.HasNaraeLetter, Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);

            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(contract.YoungCrow.TryPresentLetter(), Is.True);
            Assert.That(contract.CrowNest.ApplyWater(), Is.True);
            Assert.That(contract.CrowNest.ApplyHook(), Is.True);
            Assert.That(story.CrowNestRestored, Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);

            DefeatRegionalBoss(
                FindRegionalBoss(contract, P11BossKind.SunFlower));
            Assert.That(contract.SunEmber.TryCollect(), Is.True);
            Assert.That(story.HasSunEmber, Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);

            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(
                flow.ActiveNode.StageId,
                Is.EqualTo(P11StageId.PolarisObservatory53));

            P11MaruFinalBoss2D maruBoss = contract.MaruBoss;
            Assert.That(maruBoss.TryLightFirstMemoryBell(), Is.True);
            Assert.That(
                maruBoss.TryRingMemoryBell(P8BellSignal.Short),
                Is.False);
            Assert.That(
                maruBoss.TryRingMemoryBell(P8BellSignal.Short),
                Is.False);
            Assert.That(
                maruBoss.TryRingMemoryBell(P8BellSignal.Long),
                Is.True);
            Assert.That(story.NaraeBellPatternCompleted, Is.True);
            Assert.That(maruBoss.TryCompleteMemoryRoute(), Is.True);

            Assert.That(story.MemoryRouteCompleted, Is.True);
            Assert.That(contract.Director.HasEnded, Is.True);
            Assert.That(
                contract.Director.Ending,
                Is.EqualTo(P11EndingKind.Memory));

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MapHarnesses_PassWithSeparatedTextFreeCues()
        {
            yield return LoadSceneAsync(ScenePath);
            Scene scene = SceneManager.GetActiveScene();
            P11CommonRegionsCampaignContract contract =
                FindSingle<P11CommonRegionsCampaignContract>(scene);
            List<P11MapStageHarness2D> harnesses =
                FindAll<P11MapStageHarness2D>(scene);

            Assert.That(harnesses, Has.Count.EqualTo(9));
            Assert.That(
                harnesses.Select(item => item.Region).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(
                harnesses.Select(item => item.StageId).Distinct().Count(),
                Is.EqualTo(9));

            bool foundMainCue = false;
            bool foundOptionalCue = false;
            foreach (P11MapStageHarness2D harness in harnesses)
            {
                P11MapValidationReport report = harness.ValidateNow();
                Assert.That(
                    report.Passed,
                    Is.True,
                    $"{harness.StageId}: {report}");
                Assert.That(harness.MainPathToolFree, Is.True);
                Assert.That(
                    harness.ExitDirectionContractSatisfied,
                    Is.True);
                Assert.That(harness.CueRoutesSeparated, Is.True);

                foreach (P11MapDirectionCue2D cue in harness.Cues)
                {
                    Assert.That(cue, Is.Not.Null, harness.StageId);
                    Assert.That(
                        cue.RouteContractSatisfied,
                        Is.True,
                        $"{harness.StageId}/{cue.name}");
                    Assert.That(
                        cue.TextFreeVisualReady,
                        Is.True,
                        $"{harness.StageId}/{cue.name}");
                    Assert.That(
                        ContainsTextComponent(cue.gameObject),
                        Is.False,
                        $"Direction cue {harness.StageId}/{cue.name} "
                        + "must communicate without text.");
                    foundMainCue |=
                        cue.Route == P11MapRouteKind.Main;
                    foundOptionalCue |=
                        cue.Route == P11MapRouteKind.Optional;
                }
            }

            Assert.That(foundMainCue, Is.True);
            Assert.That(foundOptionalCue, Is.True);
            Assert.That(contract.MapHarnessCount, Is.EqualTo(9));

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static void AdvanceToFinalBoss(
            P11CommonRegionsCampaignContract contract)
        {
            P11StageFlowController2D flow = contract.StageFlow;
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            DefeatRegionalBoss(
                FindRegionalBoss(contract, P11BossKind.Popo));
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            DefeatRegionalBoss(
                FindRegionalBoss(contract, P11BossKind.SunFlower));
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(flow.TryCompleteActiveStage(), Is.True);
            Assert.That(
                flow.ActiveNode.StageId,
                Is.EqualTo(P11StageId.PolarisObservatory53));
        }

        private static P11RegionalBoss2D FindRegionalBoss(
            P11CommonRegionsCampaignContract contract,
            P11BossKind kind)
        {
            return contract.RegionalBosses.Single(item =>
                item.BossKind == kind);
        }

        private static void DefeatRegionalBoss(
            P11RegionalBoss2D boss)
        {
            Assert.That(boss.BeginEncounter(), Is.True);
            P11BossSolutionInput solution =
                boss.BossKind == P11BossKind.Popo
                    ? P11BossSolutionInput.ParcelReflection
                    : P11BossSolutionInput.GrowingVine;
            for (int index = 0;
                 index < P11RegionalBoss2D.RequiredStarKnots;
                 index++)
            {
                if (boss.IsDefeated)
                {
                    break;
                }

                bool applied = boss.TryEnvironmentTarget(
                    index,
                    solution);
                Assert.That(
                    applied || (index == 0
                        && boss.PriorEventSupportApplied),
                    Is.True,
                    $"{boss.BossKind} target {index} did not resolve.");
            }

            Assert.That(boss.IsDefeated, Is.True);
        }

        private static void DefeatMaruDirectly(
            P11MaruFinalBoss2D boss)
        {
            Assert.That(boss.BeginEncounter(), Is.True);
            Assert.That(boss.AdvanceSafeDemonstration(), Is.True);
            for (int index = 0;
                 index < P11MaruFinalBoss2D.RequiredStarKnots;
                 index++)
            {
                Assert.That(boss.ExposeWeakPoint(), Is.True);
                Assert.That(boss.TryDirectStarKnotHit(), Is.True);
            }
        }

        private static void AssertActiveStage(
            P11CommonRegionsCampaignContract contract,
            P11StageId expectedStage,
            Transform expectedPlayer,
            Camera expectedCamera,
            P8MaruStageController2D expectedMaru)
        {
            P11StageFlowController2D flow = contract.StageFlow;
            Assert.That(flow.ActiveNode, Is.Not.Null);
            Assert.That(flow.ActiveNode.StageId, Is.EqualTo(expectedStage));
            Assert.That(contract.Director.CurrentStage,
                Is.EqualTo(expectedStage));
            Assert.That(flow.ActiveEnvironmentCount, Is.EqualTo(1));
            Assert.That(contract.P10StageFlow.ActiveEnvironmentCount,
                Is.Zero);
            Assert.That(contract.ActiveEnvironmentCount, Is.EqualTo(1));
            Assert.That(flow.PersistentPlayer, Is.SameAs(expectedPlayer));
            Assert.That(flow.PersistentCamera, Is.SameAs(expectedCamera));
            Assert.That(flow.MaruController, Is.SameAs(expectedMaru));
            Assert.That(expectedMaru.gameObject.activeInHierarchy, Is.True);
            Assert.That(flow.MaruLifecyclePersistsAcrossStages, Is.True);
        }

        private static bool ContainsTextComponent(GameObject root)
        {
            Component[] components =
                root.GetComponentsInChildren<Component>(true);
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().FullName;
                if (typeName == "UnityEngine.TextMesh"
                    || typeName == "UnityEngine.UI.Text"
                    || (typeName != null
                        && typeName.StartsWith("TMPro.")))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerator LoadSceneAsync(string path)
        {
            AsyncOperation operation;
#if UNITY_EDITOR
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(path),
                Is.Not.Null,
                $"P11 production scene is missing: {path}");
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
            var found = new List<T>();
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
