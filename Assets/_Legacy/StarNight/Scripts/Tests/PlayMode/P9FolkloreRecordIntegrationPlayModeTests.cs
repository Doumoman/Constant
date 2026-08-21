#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Debugging;
using StarNight.Folklore.P9;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StarNight.Tests.PlayMode
{
    public sealed class P9FolkloreRecordIntegrationPlayModeTests
    {
        private const string LabScenePath =
            "Assets/StarNight/Scenes/Labs/"
            + "P6_MoonRoomGraphGeneratorLab.unity";

        [UnityTest]
        public IEnumerator IntegratedLab_RunsFolkloreArchiveAndGuestContracts()
        {
            yield return LoadSceneAsync(LabScenePath);
            Scene scene = SceneManager.GetActiveScene();
            P9FolkloreRecordLabContract contract =
                FindSingle<P9FolkloreRecordLabContract>(scene);
            Assert.DoesNotThrow(contract.ValidateOrThrow);
            Assert.That(contract.CorridorReviewPending, Is.True);
            Assert.That(
                contract.HumanComprehensionGatesRequirePlaytest,
                Is.True);

            P9FolkloreChainState2D chain = contract.ChainState;
            P9CorrespondenceEvent2D magpie = contract.Events.Single(
                item => item.EventKind
                    == P9CorrespondenceEventKind.HungryMagpie);
            Assert.That(
                magpie.TryOfferGift(
                    P9FolkloreItemKind.JadeRabbitMedicine),
                Is.False);
            Assert.That(
                magpie.TryOfferGift(P9FolkloreItemKind.MoonCake),
                Is.True);
            Assert.That(
                chain.ResolutionFor(
                    P9CorrespondenceEventKind.HungryMagpie),
                Is.EqualTo(P9CorrespondenceResolution.MatchingGift));

            P9BranchRelicPickup2D redThread =
                contract.BranchRelics.Single(
                    item => item.Branch == P9BranchKind.MagpieBridge);
            Assert.That(redThread.Collect(), Is.True);
            Assert.That(chain.HasRedWeaverThread, Is.True);
            Assert.That(
                chain.CanEnterOppositeBranchAfter(
                    P9BranchKind.MagpieBridge),
                Is.True);

            P9CorrespondenceEvent2D turtle = contract.Events.Single(
                item => item.EventKind
                    == P9CorrespondenceEventKind.InjuredTurtle);
            Assert.That(turtle.TryResolveAlternative(), Is.True);
            P9BranchRelicPickup2D dragonOrb =
                contract.BranchRelics.Single(
                    item => item.Branch == P9BranchKind.DragonPalace);
            Assert.That(dragonOrb.Collect(), Is.True);
            Assert.That(chain.HasBothBranchRelics, Is.True);

            P9RecordGuestDirector2D director = contract.GuestDirector;
            Assert.That(
                director.TryOpenArchiveAndRescue(
                    P9ArchiveUnlockMethods.HookLatch),
                Is.True);
            Assert.That(contract.GuestFollower.IsFollowing, Is.True);
            Assert.That(director.TryUseImmediateSupport(), Is.True);
            Assert.That(director.TryUseImmediateSupport(), Is.False);

            director.NotifyRoomTransition(new Vector3(8f, 6f, 0f));
            director.NotifyMaruBite();
            Assert.That(contract.GuestFollower.IsRescued, Is.False);
            Assert.That(
                director.TryOpenArchiveAndRescue(
                    P9ArchiveUnlockMethods.SealLever),
                Is.True);
            Assert.That(director.NotifyExitReached(), Is.True);
            Assert.That(director.NextStageSupportQueued, Is.True);
            Assert.That(director.ExitProgressBlocked, Is.False);
            Assert.That(director.IgnoringArchiveHasPenalty, Is.False);

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
                $"P9 integration scene is missing: {path}");
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

            Assert.That(found, Has.Count.EqualTo(1));
            return found[0];
        }
    }
}

#endif
