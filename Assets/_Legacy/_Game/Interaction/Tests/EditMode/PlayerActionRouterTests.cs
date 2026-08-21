#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Interaction.Input;
using UnityEngine;

namespace StarNight.Interaction.Tests
{
    public sealed class PlayerActionRouterTests
    {
        private GameObject gameObject;
        private PlayerActionRouter router;
        private PlayerActionLock actionLock;
        private PlayerActionExecutorProbe executor;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("PlayerActionRouterTests");
            actionLock = gameObject.AddComponent<PlayerActionLock>();
            router = gameObject.AddComponent<PlayerActionRouter>();
            executor = gameObject.AddComponent<PlayerActionExecutorProbe>();
            router.ConfigureForTests(null, actionLock, null, executor);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void DownPrimaryDropsBeforeEveryOtherPrimaryCandidate()
        {
            executor.HasItem = true;
            executor.AcceptDrop = true;
            executor.AcceptContext = true;
            executor.AcceptHandUse = true;
            executor.AcceptWorld = true;

            PlayerActionCommand command = router.RoutePrimaryAction(true);

            Assert.That(command, Is.EqualTo(PlayerActionCommand.DropHandSlot));
            Assert.That(executor.DropCalls, Is.EqualTo(1));
            Assert.That(executor.ContextCalls, Is.Zero);
            Assert.That(executor.HandUseCalls, Is.Zero);
            Assert.That(executor.WorldCalls, Is.Zero);
        }

        [Test]
        public void ContextReceiverWinsBeforeHandSlotUseAndWorldInteraction()
        {
            executor.HasItem = true;
            executor.AcceptContext = true;
            executor.AcceptHandUse = true;
            executor.AcceptWorld = true;

            PlayerActionCommand command = router.RoutePrimaryAction(false);

            Assert.That(command, Is.EqualTo(PlayerActionCommand.ContextAction));
            Assert.That(executor.ContextCalls, Is.EqualTo(1));
            Assert.That(executor.HandUseCalls, Is.Zero);
            Assert.That(executor.WorldCalls, Is.Zero);
        }

        [Test]
        public void HookLatchedPrimaryPullsWithoutFallingThrough()
        {
            executor.HasItem = true;
            executor.AcceptHookPull = true;
            executor.AcceptContext = true;
            executor.AcceptHandUse = true;
            executor.AcceptWorld = true;
            actionLock.SetState(PlayerActionState.HookLatched);

            PlayerActionCommand command = router.RoutePrimaryAction(false);

            Assert.That(command, Is.EqualTo(PlayerActionCommand.HookPull));
            Assert.That(executor.HookPullCalls, Is.EqualTo(1));
            Assert.That(executor.ContextCalls + executor.HandUseCalls + executor.WorldCalls, Is.Zero);
        }

        [Test]
        public void UmbrellaOpenPrimaryClosesWithoutFallingThrough()
        {
            executor.HasItem = true;
            executor.AcceptUmbrellaClose = true;
            executor.AcceptHandUse = true;
            actionLock.SetState(PlayerActionState.UmbrellaOpen);

            PlayerActionCommand command = router.RoutePrimaryAction(false);

            Assert.That(command, Is.EqualTo(PlayerActionCommand.CloseUmbrella));
            Assert.That(executor.UmbrellaCloseCalls, Is.EqualTo(1));
            Assert.That(executor.HandUseCalls, Is.Zero);
        }

        [Test]
        public void OccupiedHandSlotNeverFallsThroughToWorldInteraction()
        {
            executor.HasItem = true;
            executor.AcceptWorld = true;

            Assert.That(router.RoutePrimaryAction(false), Is.EqualTo(PlayerActionCommand.None));
            Assert.That(executor.HandUseCalls, Is.EqualTo(1));
            Assert.That(executor.WorldCalls, Is.Zero);
        }

        [Test]
        public void LockedStatesDiscardGameplayActions()
        {
            executor.HasItem = true;
            executor.AcceptDrop = true;
            executor.AcceptBomb = true;
            executor.AcceptRope = true;
            actionLock.SetState(PlayerActionState.DialogueLocked);

            Assert.That(router.RoutePrimaryAction(true), Is.EqualTo(PlayerActionCommand.None));
            Assert.That(router.RouteBombAction(), Is.EqualTo(PlayerActionCommand.None));
            Assert.That(router.RouteRopeAction(), Is.EqualTo(PlayerActionCommand.None));
            Assert.That(executor.TotalCalls, Is.Zero);
        }

        [Test]
        public void EachAcceptedButtonPressPublishesOneUniqueActionId()
        {
            executor.AcceptWorld = true;
            executor.AcceptBomb = true;
            executor.AcceptRope = true;
            long previousId = 0;
            int routedCount = 0;
            router.ActionRouted += action =>
            {
                Assert.That(action.Context.ActionId, Is.GreaterThan(previousId));
                previousId = action.Context.ActionId;
                routedCount++;
            };

            Assert.That(router.RoutePrimaryAction(false), Is.EqualTo(PlayerActionCommand.WorldInteraction));
            Assert.That(router.RouteBombAction(), Is.EqualTo(PlayerActionCommand.PlaceBomb));
            Assert.That(router.RouteRopeAction(), Is.EqualTo(PlayerActionCommand.PlaceRope));
            Assert.That(routedCount, Is.EqualTo(3));
            Assert.That(executor.WorldCalls, Is.EqualTo(1));
            Assert.That(executor.BombCalls, Is.EqualTo(1));
            Assert.That(executor.RopeCalls, Is.EqualTo(1));
        }

        [Test]
        public void BufferedInputConsumesOnlyOnceAndExpires()
        {
            BufferedInput buffered = new BufferedInput();
            buffered.Buffer(10f, 0.12f);

            Assert.That(buffered.TryConsume(10.1f), Is.True);
            Assert.That(buffered.TryConsume(10.1f), Is.False);

            buffered.Buffer(20f, 0.08f);
            Assert.That(buffered.TryConsume(20.081f), Is.False);
        }
    }

    public sealed class PlayerActionExecutorProbe : MonoBehaviour, IPlayerActionExecutor, IPlayerSpecialActionExecutor
    {
        public bool HasItem;
        public bool AcceptDrop;
        public bool AcceptContext;
        public bool AcceptHandUse;
        public bool AcceptWorld;
        public bool AcceptBomb;
        public bool AcceptRope;
        public bool AcceptDropPreparation = true;
        public bool AcceptHookPull;
        public bool AcceptUmbrellaClose;
        public int DropCalls;
        public int ContextCalls;
        public int HandUseCalls;
        public int WorldCalls;
        public int BombCalls;
        public int RopeCalls;
        public int DropPreparationCalls;
        public int HookPullCalls;
        public int UmbrellaCloseCalls;

        public bool HasHandSlotItem => HasItem;
        public int TotalCalls => DropCalls + ContextCalls + HandUseCalls + WorldCalls + BombCalls + RopeCalls
            + DropPreparationCalls + HookPullCalls + UmbrellaCloseCalls;

        public bool TryDropHandSlot(PlayerActionContext context) { DropCalls++; return AcceptDrop; }
        public bool TryContextAction(PlayerActionContext context) { ContextCalls++; return AcceptContext; }
        public bool TryHandSlotPrimaryUse(PlayerActionContext context) { HandUseCalls++; return AcceptHandUse; }
        public bool TryWorldInteraction(PlayerActionContext context) { WorldCalls++; return AcceptWorld; }
        public bool TryPlaceBomb(PlayerActionContext context) { BombCalls++; return AcceptBomb; }
        public bool TryPlaceRope(PlayerActionContext context) { RopeCalls++; return AcceptRope; }
        public bool TryPrepareHandSlotDrop(PlayerActionContext context) { DropPreparationCalls++; return AcceptDropPreparation; }
        public bool TryPullHook(PlayerActionContext context) { HookPullCalls++; return AcceptHookPull; }
        public bool TryCloseUmbrella(PlayerActionContext context) { UmbrellaCloseCalls++; return AcceptUmbrellaClose; }
    }
}

#endif
