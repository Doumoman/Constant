#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Interaction.Input;
using StarNight.Interaction.Targeting;
using UnityEngine;

namespace StarNight.Interaction.Tests
{
    public sealed class InteractionTargetingTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("InteractionTargetingTests");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void SelectorUsesPriorityThenFacingDistancePreviousAndStableId()
        {
            InteractionCandidate lowPriority = Candidate(
                "LowPriority",
                new Vector2(0.2f, 0f),
                InteractionTargetKind.Pickup,
                1);
            InteractionCandidate highPriority = Candidate(
                "HighPriority",
                new Vector2(0.7f, 0f),
                InteractionTargetKind.DialogueNpc,
                20);
            InteractionCandidate samePriorityBetterFacing = Candidate(
                "BetterFacing",
                new Vector2(0.7f, 0.1f),
                InteractionTargetKind.DialogueNpc,
                10);
            var selector = new InteractionTargetSelector();
            var query = new ContextReceiverQuery(root, null);

            InteractionCandidate selected = selector.Select(
                new[] { lowPriority, samePriorityBetterFacing, highPriority },
                Vector2.zero,
                Vector2.right,
                query,
                null);

            Assert.That(selected, Is.EqualTo(highPriority));
        }

        [Test]
        public void ProbeSelectsOneVisibleCandidateInsideApprovedCellRange()
        {
            int layer = LayerMask.NameToLayer("Interaction");
            Assert.That(layer, Is.GreaterThanOrEqualTo(0));
            InteractionProbe probe = root.AddComponent<InteractionProbe>();
            probe.ConfigureForTests(1 << layer, 0);

            InteractionCandidate candidate = Candidate(
                "Target",
                new Vector2(0.45f, 0f),
                InteractionTargetKind.InspectObject,
                3);
            candidate.gameObject.layer = layer;
            candidate.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();

            Assert.That(probe.Refresh(0f), Is.EqualTo(candidate));
        }

        [Test]
        public void ActionLockAllowsOnlyOwningActionToTransitionAndRelease()
        {
            PlayerActionLock actionLock = root.AddComponent<PlayerActionLock>();

            Assert.That(actionLock.TryAcquire(11, PlayerActionState.UsingTool), Is.True);
            Assert.That(actionLock.TryAcquire(12, PlayerActionState.Throwing), Is.False);
            Assert.That(actionLock.TryTransition(12, PlayerActionState.Placing), Is.False);
            Assert.That(actionLock.TryTransition(11, PlayerActionState.Placing), Is.True);
            Assert.That(actionLock.TryRelease(12), Is.False);
            Assert.That(actionLock.TryRelease(11), Is.True);
            Assert.That(actionLock.State, Is.EqualTo(PlayerActionState.Free));
        }

        private InteractionCandidate Candidate(
            string name,
            Vector2 position,
            InteractionTargetKind kind,
            int stableId)
        {
            GameObject candidateObject = new GameObject(name);
            candidateObject.transform.SetParent(root.transform);
            candidateObject.transform.position = position;
            InteractionCandidate candidate = candidateObject.AddComponent<InteractionCandidate>();
            candidate.ConfigureForTests(kind, stableId);
            return candidate;
        }
    }
}

#endif
