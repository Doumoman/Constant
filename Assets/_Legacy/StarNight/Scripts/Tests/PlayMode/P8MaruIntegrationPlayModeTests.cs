#if LEGACY_DISABLED
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Debugging;
using StarNight.Maru.P8;
using StarNight.Objects;
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
    public sealed class P8MaruIntegrationPlayModeTests
    {
        private const string ProductionScenePath =
            "Assets/StarNight/Scenes/Game/"
            + "P5_MoonPalace_1-1_CraterWorkshop.unity";
        private const string LabScenePath =
            "Assets/StarNight/Scenes/Labs/"
            + "P6_MoonRoomGraphGeneratorLab.unity";

        [UnityTest]
        public IEnumerator ProductionScene_RunsTrackingAndTwoBiteContract()
        {
            yield return LoadSceneAsync(ProductionScenePath);
            Scene scene = SceneManager.GetActiveScene();
            P8MaruProductionContract contract =
                FindSingle<P8MaruProductionContract>(scene);
            Assert.DoesNotThrow(contract.ValidateOrThrow);
            P8MaruRunFeedback2D feedback =
                FindSingle<P8MaruRunFeedback2D>(scene);
            Assert.That(
                feedback.BiteController,
                Is.SameAs(contract.BiteController));

            PlayerRecovery recovery =
                contract.Player.GetComponent<PlayerRecovery>();
            PlayerMotor2D motor =
                contract.Player.GetComponent<PlayerMotor2D>();
            PlayerInputAdapter input =
                contract.Player.GetComponent<PlayerInputAdapter>();
            CarrySystem carry =
                contract.Player.GetComponent<CarrySystem>();
            PlayerToolInventory2D inventory =
                contract.Player.GetComponent<PlayerToolInventory2D>();
            Assert.That(recovery.CurrentHealth, Is.EqualTo(4));
            Assert.That(contract.Pursuer.MoveSpeed, Is.LessThan(3.75f));
            Assert.That(
                contract.Pursuer.RoomGraph.NodeCount,
                Is.EqualTo(7));
            Assert.That(
                contract.StageController.UsesP5ClockCompatibility,
                Is.True);
            Assert.That(
                contract.DormantP8Timeline.IsRunning,
                Is.False);

            if (contract.CoreLoop.State == P5CoreLoopState.Intro)
            {
                Assert.That(
                    contract.CoreLoop.CompleteIntroAndBegin(),
                    Is.True);
            }

            Assert.That(contract.StageController.StageActive, Is.True);
            Assert.That(contract.CompatibilityClock.IsRunning, Is.True);
            contract.CompatibilityClock.Advance(
                contract.CompatibilityClock.MaruDueSeconds);
            Assert.That(contract.Pursuer.IsHunting, Is.True);

            P5StoryPestle2D story =
                FindSingle<P5StoryPestle2D>(scene);
            Assert.That(inventory.TryEquip(story.Pickup), Is.True);
            Assert.That(inventory.HasHeldTool, Is.True);

            Assert.That(
                contract.BiteController.TryBeginBite(),
                Is.True);
            Assert.That(motor.ControlLocked, Is.True);
            Assert.That(input.GameplaySuppressed, Is.True);
            Assert.That(carry.InteractionLocked, Is.True);
            contract.BiteController.TickForTests(
                P8MaruBiteController2D.RequiredHoldSeconds,
                true,
                false);

            Assert.That(
                contract.BiteController.State,
                Is.EqualTo(P8BiteState.Escaped));
            Assert.That(recovery.CurrentHealth, Is.EqualTo(3));
            Assert.That(inventory.HasHeldTool, Is.False);
            Assert.That(motor.ControlLocked, Is.False);
            Assert.That(input.GameplaySuppressed, Is.False);
            Assert.That(carry.InteractionLocked, Is.False);

            Assert.That(
                contract.BiteController.TryBeginBite(),
                Is.True);
            Assert.That(
                contract.BiteController.State,
                Is.EqualTo(P8BiteState.RunEnded));
            Assert.That(
                contract.BiteController.LastDeathReport.RunEndKind,
                Is.EqualTo(P8RunEndKind.SecondMaruBite));
            Assert.That(
                contract.BiteController.LastDeathReport
                    .HasConcreteCausalChain,
                Is.True);
            Assert.That(feedback.IsShowingRunEnd, Is.True);
            Assert.That(
                feedback.LastReport.PrimaryMessage,
                Does.Contain("두 번째"));
            Assert.That(
                feedback.LastReport.TimingMessage,
                Does.Contain("추적"));
            Assert.That(contract.CompatibilityClock.IsRunning, Is.False);
            Assert.That(contract.StageController.StageActive, Is.False);
            Assert.That(motor.ControlLocked, Is.True);
            Assert.That(input.GameplaySuppressed, Is.True);
            Assert.That(carry.InteractionLocked, Is.True);

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator IntegratedLab_RunsStatueTearAndGateContract()
        {
            yield return LoadSceneAsync(LabScenePath);
            Scene scene = SceneManager.GetActiveScene();
            P8MaruLabContract contract =
                FindSingle<P8MaruLabContract>(scene);
            Assert.DoesNotThrow(contract.ValidateOrThrow);
            Assert.That(
                FindSingle<P8MaruRunFeedback2D>(scene)
                    .BiteController,
                Is.SameAs(contract.Pursuer.BiteController));
            Assert.That(contract.StatueExitDistance, Is.InRange(3, 5));
            Assert.That(contract.GateSummary.AppearanceGatePassed, Is.True);
            Assert.That(
                contract.GateSummary.StatueSurvivalGatePassed,
                Is.True);
            Assert.That(contract.GateSummary.DeathCauseGatePassed, Is.True);
            Assert.That(
                contract.MaruGraph.NodeCount,
                Is.EqualTo(contract.GraphContract.Placements.Count));
            Assert.That(contract.MaruGraph.NodeCount, Is.InRange(9, 14));
            Assert.That(
                contract.MaruGraph.Distance(
                    contract.StatueNodeId,
                    contract.ExitNodeId),
                Is.EqualTo(contract.StatueExitDistance));
            Assert.That(contract.Statue.CanBeCarried, Is.False);
            Assert.That(
                contract.Statue.Traits,
                Is.EqualTo(
                    WorldObjectTraits.Heavy
                    | WorldObjectTraits.Breakable
                    | WorldObjectTraits.Pullable));
            Assert.That(contract.StarTear.Value, Is.EqualTo(12));
            Assert.That(contract.StarTear.CanBeLost, Is.True);
            Assert.That(
                P8HomecomingStatue2D.FootprintCells,
                Is.EqualTo(new Vector2Int(1, 2)));

            P8MaruStageController2D stageController =
                FindSingle<P8MaruStageController2D>(scene);
            stageController.BeginStage();
            Assert.That(contract.Timeline.IsRunning, Is.True);
            Assert.That(
                contract.Statue.ApplyImpact(P8StatueImpactKind.Test),
                Is.True);
            Assert.That(
                contract.Statue.State,
                Is.EqualTo(P8StatueState.Cracked));
            Assert.That(
                contract.Statue.ApplyImpact(P8StatueImpactKind.Bomb),
                Is.True);
            Assert.That(
                contract.Statue.State,
                Is.EqualTo(P8StatueState.Destroyed));
            Assert.That(contract.StarTear.gameObject.activeSelf, Is.True);
            Assert.That(contract.Timeline.StatueWasDestroyed, Is.True);
            Assert.That(
                contract.Timeline.TimeUntilMaru,
                Is.EqualTo(20f).Within(0.05f));
            Assert.That(
                contract.Timeline.Phase,
                Is.EqualTo(P8MaruPhase.SecondBell));

            contract.Timeline.Advance(20f);
            Assert.That(
                contract.Timeline.Phase,
                Is.EqualTo(P8MaruPhase.Hunting));
            Assert.That(contract.Pursuer.IsHunting, Is.True);

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator LoadSceneAsync(string path)
        {
            AsyncOperation operation;
#if UNITY_EDITOR
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(path),
                Is.Not.Null,
                $"P8 integration scene is missing: {path}");
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
            List<T> found = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                found.AddRange(
                    roots[index].GetComponentsInChildren<T>(true));
            }

            Assert.That(
                found,
                Has.Count.EqualTo(1),
                $"Expected exactly one {typeof(T).Name} in {scene.path}.");
            return found[0];
        }
    }
}

#endif
