#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Debugging;
using StarNight.Objects;
using StarNight.Player;
using StarNight.Stages.P5;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    public sealed class P5MoonPalaceSliceTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(created[index]);
                }
            }

            created.Clear();
        }

        [Test]
        public void FixedLayout_IsSevenAuthoredRoomsWithNoSeed()
        {
            Assert.That(
                P5MoonStageContract.LayoutId,
                Is.EqualTo("P5_MoonPalace_1-1_CraterWorkshop_Fixed_v1"));
            Assert.That(P5MoonStageContract.Width, Is.EqualTo(102));
            Assert.That(P5MoonStageContract.Height, Is.EqualTo(18));
            Assert.That(P5MoonStageContract.NoProceduralSeed, Is.EqualTo(-1));
            Assert.That(
                P5MoonStageContract.RequiredRoomIds,
                Has.Length.EqualTo(P5MoonStageContract.RequiredRoomCount));
            Assert.That(
                P5MoonStageContract.RequiredRoomOrigins,
                Has.Length.EqualTo(P5MoonStageContract.RequiredRoomCount));
            Assert.That(
                P5MoonStageContract.RequiredRoomSizes,
                Has.Length.EqualTo(P5MoonStageContract.RequiredRoomCount));

            int expectedOriginX = 0;
            for (int index = 0;
                index < P5MoonStageContract.RequiredRoomCount;
                index++)
            {
                Assert.That(
                    P5MoonStageContract.RequiredRoomOrigins[index],
                    Is.EqualTo(new Vector2Int(expectedOriginX, 0)),
                    $"Room {index} is not part of the fixed contiguous layout.");
                expectedOriginX +=
                    P5MoonStageContract.RequiredRoomSizes[index].x;
            }

            Assert.That(expectedOriginX, Is.EqualTo(P5MoonStageContract.Width));
            Assert.That(
                P5MoonStageContract.StartCell,
                Is.EqualTo(new Vector2Int(2, 1)));
            Assert.That(
                P5MoonStageContract.ExitSafeCell,
                Is.EqualTo(new Vector2Int(100, 1)));
        }

        [Test]
        public void RunState_SpendIsAtomicAndNeverNegative()
        {
            P5RunState2D runState =
                Track("RunState").AddComponent<P5RunState2D>();
            runState.Configure(4, 0);

            Assert.That(runState.TrySpendGold(5), Is.False);
            Assert.That(runState.Gold, Is.EqualTo(4));
            Assert.That(runState.TrySpendGold(-1), Is.False);
            Assert.That(runState.Gold, Is.EqualTo(4));
            Assert.That(runState.TrySpendGold(4), Is.True);
            Assert.That(runState.Gold, Is.Zero);
            Assert.That(runState.TrySpendGold(1), Is.False);
            Assert.That(runState.Gold, Is.Zero);
        }

        [Test]
        public void GoldPickup_AllowsOnlyOneOrThreeAndCollectsOnce()
        {
            P5RunState2D runState =
                Track("RunState").AddComponent<P5RunState2D>();
            P5GoldPickup2D pickup =
                Track("SmallGold").AddComponent<P5GoldPickup2D>();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => pickup.Configure(runState, 2));

            pickup.Configure(runState, P5GoldPickup2D.SmallGoldValue);
            Assert.That(pickup.CollectForTests(), Is.True);
            Assert.That(pickup.CollectForTests(), Is.False);
            Assert.That(runState.Gold, Is.EqualTo(1));

            P5GoldPickup2D bigPickup =
                Track("BigGold").AddComponent<P5GoldPickup2D>();
            bigPickup.Configure(runState, P5GoldPickup2D.BigGoldValue);
            Assert.That(bigPickup.CollectForTests(), Is.True);
            Assert.That(runState.Gold, Is.EqualTo(4));
        }

        [Test]
        public void MomoShop_ThreeWorldOffersChargeAndGrantExactlyOnce()
        {
            P5RunState2D runState =
                Track("RunState").AddComponent<P5RunState2D>();
            runState.Configure(10, 0);

            GameObject player = Track("Player");
            PlayerInputAdapter input =
                player.AddComponent<PlayerInputAdapter>();
            player.AddComponent<Rigidbody2D>();
            PlayerConsumableTools2D consumables =
                player.AddComponent<PlayerConsumableTools2D>();
            consumables.SetStockForTests(0, 0);

            P5MomoShop2D shop =
                Track("MomoShop").AddComponent<P5MomoShop2D>();
            P5MomoShopOffer2D rope =
                CreateOffer(
                    shop,
                    P5ShopProductKind.RopeBundle3,
                    "Rope3");
            P5MomoShopOffer2D bombs =
                CreateOffer(
                    shop,
                    P5ShopProductKind.BombBundle2,
                    "Bomb2");
            P5MomoShopOffer2D moonCake =
                CreateOffer(
                    shop,
                    P5ShopProductKind.MoonCake,
                    "MoonCake");
            shop.Configure(
                runState,
                consumables,
                new[] { rope, moonCake, bombs });

            Assert.That(shop.OfferCount, Is.EqualTo(3));
            Assert.That(rope.TryPurchaseForTests(), Is.True);
            Assert.That(runState.Gold, Is.EqualTo(7));
            Assert.That(consumables.RopeStock, Is.EqualTo(3));
            Assert.That(rope.TryPurchaseForTests(), Is.False);
            Assert.That(runState.Gold, Is.EqualTo(7));
            Assert.That(consumables.RopeStock, Is.EqualTo(3));

            runState.Configure(3, 0);
            Assert.That(bombs.TryPurchaseForTests(), Is.False);
            Assert.That(runState.Gold, Is.EqualTo(3));
            Assert.That(consumables.BombStock, Is.Zero);
            Assert.That(moonCake.TryPurchaseForTests(), Is.True);
            Assert.That(runState.Gold, Is.Zero);
            Assert.That(runState.MoonCakes, Is.EqualTo(1));
        }

        [Test]
        public void StageExit_RequiresContinuousHalfSecondAndDepartsOnce()
        {
            P5StageExit2D stageExit =
                Track("StageExit").AddComponent<P5StageExit2D>();
            stageExit.Configure(
                null,
                null,
                null,
                null,
                stageExit.transform);

            int firstReachCount = 0;
            int departureCount = 0;
            stageExit.FirstReached += () => firstReachCount++;
            stageExit.Departed += () => departureCount++;

            stageExit.TickForTests(0.49f, true, true);
            Assert.That(
                stageExit.State,
                Is.EqualTo(P5StageExitState.Confirming));
            Assert.That(stageExit.HoldElapsedSeconds, Is.EqualTo(0.49f));
            Assert.That(departureCount, Is.Zero);

            stageExit.TickForTests(0f, true, false);
            Assert.That(stageExit.HoldElapsedSeconds, Is.Zero);
            Assert.That(
                stageExit.State,
                Is.EqualTo(P5StageExitState.Reached));

            stageExit.TickForTests(0.5f, true, true);
            Assert.That(
                stageExit.State,
                Is.EqualTo(P5StageExitState.Departed));
            Assert.That(firstReachCount, Is.EqualTo(1));
            Assert.That(departureCount, Is.EqualTo(1));

            stageExit.TickForTests(5f, true, true);
            Assert.That(departureCount, Is.EqualTo(1));
        }

        [Test]
        public void MaruBellClock_Uses140_185_215AndEmitsShortShortLongOnce()
        {
            P5MaruBellClock2D clock =
                Track("MaruBellClock").AddComponent<P5MaruBellClock2D>();
            clock.Configure();

            Assert.That(clock.FirstBellSeconds, Is.EqualTo(140f));
            Assert.That(clock.SecondBellSeconds, Is.EqualTo(185f));
            Assert.That(clock.MaruDueSeconds, Is.EqualTo(215f));

            List<P5BellSignal> signals = new List<P5BellSignal>();
            List<P5MaruBellPhase> phases =
                new List<P5MaruBellPhase>();
            clock.BellRang += (signal, phase) =>
            {
                signals.Add(signal);
                phases.Add(phase);
            };

            clock.StartClock();
            clock.Advance(216f);
            CollectionAssert.AreEqual(
                new[]
                {
                    P5BellSignal.Short,
                    P5BellSignal.Short,
                    P5BellSignal.Long
                },
                signals);
            CollectionAssert.AreEqual(
                new[]
                {
                    P5MaruBellPhase.FirstBell,
                    P5MaruBellPhase.SecondBell,
                    P5MaruBellPhase.MaruDue
                },
                phases);
            Assert.That(
                clock.Phase,
                Is.EqualTo(P5MaruBellPhase.MaruDue));

            clock.Advance(1000f);
            Assert.That(signals, Has.Count.EqualTo(3));
        }

        [Test]
        public void CoreLoop_StartsBellAfterIntroAndStopsItAtExit()
        {
            P5RunState2D runState =
                Track("RunState").AddComponent<P5RunState2D>();
            P5MaruBellClock2D clock =
                Track("MaruBellClock").AddComponent<P5MaruBellClock2D>();
            clock.Configure();
            P5StageCoreLoop2D core =
                Track("CoreLoop").AddComponent<P5StageCoreLoop2D>();
            P5StageExit2D stageExit =
                Track("StageExit").AddComponent<P5StageExit2D>();

            stageExit.Configure(
                null,
                null,
                core,
                null,
                stageExit.transform);
            core.Configure(
                runState,
                clock,
                stageExit,
                null,
                null,
                null,
                1f,
                false);

            Assert.That(core.State, Is.EqualTo(P5CoreLoopState.Intro));
            Assert.That(clock.IsRunning, Is.False);
            Assert.That(core.CompleteIntroAndBegin(), Is.True);
            Assert.That(core.State, Is.EqualTo(P5CoreLoopState.Active));
            Assert.That(clock.IsRunning, Is.True);

            stageExit.TickForTests(0f, true, false);
            Assert.That(
                core.State,
                Is.EqualTo(P5CoreLoopState.ExitReached));
            stageExit.TickForTests(0.5f, true, true);
            Assert.That(core.State, Is.EqualTo(P5CoreLoopState.Departed));
            Assert.That(clock.IsRunning, Is.False);
            Assert.That(clock.Phase, Is.EqualTo(P5MaruBellPhase.Stopped));
        }

        [Test]
        public void MoonRabbit_OnlyDesignatedPestleGrantsOneMoonCakeOnce()
        {
            P5RunState2D runState =
                Track("RunState").AddComponent<P5RunState2D>();
            PlayerToolInventory2D inventory =
                CreateToolInventory(out Transform holdAnchor);

            HandToolPickup2D storyPickup = CreatePestle("StoryPestle");
            GameObject recovery = Track("StoryRecovery");
            P5StoryPestle2D story =
                storyPickup.gameObject.AddComponent<P5StoryPestle2D>();
            story.Configure(storyPickup, recovery.transform, null);

            GameObject returnedAnchor = Track("ReturnedPestleAnchor");
            GameObject rewardVisual = Track("MoonCakeRewardVisual");
            P5MoonRabbitPestleEvent2D rabbit =
                Track("MoonRabbit").AddComponent<P5MoonRabbitPestleEvent2D>();
            rabbit.Configure(
                story,
                runState,
                returnedAnchor.transform,
                rewardVisual,
                rabbit.transform);

            HandToolPickup2D wrongPickup = CreatePestle("WrongPestle");
            Assert.That(inventory.TryEquip(wrongPickup), Is.True);
            Assert.That(rabbit.TryResolveForTests(inventory), Is.False);
            Assert.That(runState.MoonCakes, Is.Zero);
            Assert.That(inventory.DropHeldTool(), Is.True);

            Assert.That(inventory.TryEquip(storyPickup), Is.True);
            Assert.That(story.WasDiscovered, Is.True);
            Assert.That(rabbit.TryResolveForTests(inventory), Is.True);
            Assert.That(
                rabbit.State,
                Is.EqualTo(P5MoonRabbitPestleState.Completed));
            Assert.That(runState.MoonCakes, Is.EqualTo(1));
            Assert.That(inventory.HeldTool, Is.Null);
            Assert.That(
                storyPickup.transform.parent,
                Is.EqualTo(returnedAnchor.transform));
            Assert.That(rewardVisual.activeSelf, Is.True);

            Assert.That(rabbit.TryResolveForTests(inventory), Is.False);
            Assert.That(runState.MoonCakes, Is.EqualTo(1));
            Assert.That(story.IsReturned, Is.True);
            Assert.That(holdAnchor, Is.Not.Null);
        }

        [Test]
        public void PlayerInput_InteractHoldRequiresExplicitRelease()
        {
            PlayerInputAdapter input =
                Track("Input").AddComponent<PlayerInputAdapter>();
            input.EnableTestInput(true);

            input.PressInteractForTests();
            Assert.That(input.InteractHeld, Is.True);
            Assert.That(input.ConsumeInteractPressed(), Is.True);
            Assert.That(input.ConsumeInteractPressed(), Is.False);
            Assert.That(input.InteractHeld, Is.True);

            input.ReleaseInteractForTests();
            Assert.That(input.InteractHeld, Is.False);
        }

        private P5MomoShopOffer2D CreateOffer(
            P5MomoShop2D shop,
            P5ShopProductKind product,
            string name)
        {
            P5MomoShopOffer2D offer =
                Track(name).AddComponent<P5MomoShopOffer2D>();
            offer.Configure(
                shop,
                product,
                P5MomoShopOffer2D.DefaultPrice(product),
                offer.transform);
            return offer;
        }

        private PlayerToolInventory2D CreateToolInventory(
            out Transform holdAnchor)
        {
            GameObject player = Track("ToolPlayer");
            PlayerInputAdapter input =
                player.AddComponent<PlayerInputAdapter>();
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            CapsuleCollider2D collider =
                player.AddComponent<CapsuleCollider2D>();
            CarrySystem carry = player.AddComponent<CarrySystem>();
            GameObject anchor = Track("CarryAnchor");
            holdAnchor = anchor.transform;
            holdAnchor.SetParent(player.transform, false);
            carry.Configure(input, body, holdAnchor, null);

            PlayerToolInventory2D inventory =
                player.AddComponent<PlayerToolInventory2D>();
            inventory.Configure(
                input,
                carry,
                null,
                body,
                collider,
                null,
                holdAnchor,
                null,
                null,
                null,
                null,
                null);
            return inventory;
        }

        private HandToolPickup2D CreatePestle(string name)
        {
            GameObject tool = Track(name);
            BoxCollider2D collider = tool.AddComponent<BoxCollider2D>();
            HandToolPickup2D pickup =
                tool.AddComponent<HandToolPickup2D>();
            pickup.Configure(HandToolKind.Pestle, 0, collider);
            return pickup;
        }

        private GameObject Track(string name)
        {
            GameObject instance = new GameObject(name);
            created.Add(instance);
            return instance;
        }
    }
}

#endif
