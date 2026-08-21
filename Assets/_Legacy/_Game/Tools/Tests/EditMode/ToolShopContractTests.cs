#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Core.State;
using StarNight.Interaction.Carry;
using StarNight.Interaction.HandSlot;
using StarNight.Map;
using StarNight.Tools.Bomb;
using StarNight.Tools.Core;
using StarNight.Tools.Pickaxe;
using StarNight.Tools.Rope;
using StarNight.Tools.Shop;
using UnityEditor;
using UnityEngine;

namespace StarNight.Tools.Tests
{
    public sealed class ToolShopContractTests
    {
        [Test]
        public void PurchaseRefillSafeExchangeAndBundlesAreAtomic()
        {
            var manager = new RunManager(() => 11);
            RunState run = manager.StartNewRun();
            run.moneyWon = 1000;

            GameObject player = new GameObject("ShopTestPlayer");
            HandSlotPresenter presenter = player.AddComponent<HandSlotPresenter>();
            presenter.ConfigureForTests(player.transform);
            PlayerHandSlot slot = player.AddComponent<PlayerHandSlot>();
            slot.ConfigureForTests(presenter);
            HandSlotTransferService transfer = player.AddComponent<HandSlotTransferService>();
            transfer.ConfigureForTests(slot, null, null, new PlacementWorld(true));
            BombInventoryState bombs = player.AddComponent<BombInventoryState>();
            RopeInventoryState ropes = player.AddComponent<RopeInventoryState>();
            bombs.EnsureInitialized();
            ropes.EnsureInitialized();

            HandToolDefinition pickaxe = CreateDefinition("TOOL_PICKAXE", 12, 250, out GameObject pickaxePrefab);
            HandToolDefinition shovel = CreateDefinition("TOOL_SHOVEL", 10, 200, out GameObject shovelPrefab);
            GameObject counterObject = new GameObject("ToolShopCounterTest");
            ToolShopCounter counter = counterObject.AddComponent<ToolShopCounter>();
            counter.ConfigureForTests(pickaxe, manager);

            Assert.That(counter.PriceLabel, Is.EqualTo("250원"));
            Assert.That(counter.TryPurchase(player, out ToolPurchaseResult first), Is.True);
            Assert.That(first, Is.EqualTo(ToolPurchaseResult.Purchased));
            Assert.That(run.moneyWon, Is.EqualTo(750));
            HandToolRuntime heldPickaxe = slot.CurrentItem as HandToolRuntime;
            Assert.That(heldPickaxe, Is.Not.Null);
            Assert.That(heldPickaxe.ResourceState.TryConsumeForSuccessfulReaction(true), Is.True);

            Assert.That(counter.TryPurchase(player, out ToolPurchaseResult refill), Is.True);
            Assert.That(refill, Is.EqualTo(ToolPurchaseResult.Refilled));
            Assert.That(heldPickaxe.CurrentResource, Is.EqualTo(12));
            Assert.That(run.moneyWon, Is.EqualTo(500));

            counter.ConfigureForTests(shovel, manager);
            transfer.ConfigureForTests(slot, null, null, new PlacementWorld(false));
            Assert.That(counter.TryPurchase(player, out ToolPurchaseResult blocked), Is.False);
            Assert.That(blocked, Is.EqualTo(ToolPurchaseResult.NoSafeExchangeCell));
            Assert.That(run.moneyWon, Is.EqualTo(500));
            Assert.That(slot.CurrentItem, Is.SameAs(heldPickaxe));

            transfer.ConfigureForTests(slot, null, null, new PlacementWorld(true));
            Assert.That(counter.TryPurchase(player, out ToolPurchaseResult exchanged), Is.True);
            Assert.That(exchanged, Is.EqualTo(ToolPurchaseResult.Purchased));
            Assert.That(run.moneyWon, Is.EqualTo(300));
            Assert.That(((IHandSlotHudSource)slot.CurrentItem).StableItemId, Is.EqualTo("TOOL_SHOVEL"));

            GameObject bundleObject = new GameObject("BundleShopCounterTest");
            ConsumableShopCounter bundle = bundleObject.AddComponent<ConsumableShopCounter>();
            bundle.ConfigureForTests(ConsumableBundleKind.RopeBundle, manager);
            int ropeBefore = ropes.Remaining;
            Assert.That(bundle.TryPurchase(player, out ToolPurchaseResult bundleResult), Is.True);
            Assert.That(bundleResult, Is.EqualTo(ToolPurchaseResult.Purchased));
            Assert.That(ropes.Remaining, Is.EqualTo(ropeBefore + 3));
            Assert.That(run.ropes, Is.EqualTo(ropes.Remaining));
            Assert.That(run.moneyWon, Is.EqualTo(200));
            Assert.That(ConsumableShopCounter.BombBundleQuantity, Is.EqualTo(2));
            Assert.That(ConsumableShopCounter.BombBundlePriceWon, Is.EqualTo(150));
            Assert.That(bombs.Remaining, Is.EqualTo(BombDefinition.ApprovedStartingCount));

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(counterObject);
            Object.DestroyImmediate(bundleObject);
            Object.DestroyImmediate(pickaxePrefab);
            Object.DestroyImmediate(shovelPrefab);
            Object.DestroyImmediate(pickaxe);
            Object.DestroyImmediate(shovel);
            foreach (HandToolRuntime looseTool in Object.FindObjectsByType<HandToolRuntime>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(looseTool.gameObject);
            }
        }

        [Test]
        public void AuthoredToolHudPriceAndPromptReflectRuntimeDefinitions()
        {
            string[] assetPaths =
            {
                "Assets/_Game/Tools/Data/HandTools/TOOL_PICKAXE.asset",
                "Assets/_Game/Tools/Data/HandTools/TOOL_SHOVEL.asset",
                "Assets/_Game/Tools/Data/HandTools/TOOL_WATERING_CAN.asset",
                "Assets/_Game/Tools/Data/HandTools/TOOL_POUNDER.asset",
                "Assets/_Game/Tools/Data/HandTools/TOOL_HOOK_LAUNCHER.asset",
                "Assets/_Game/Tools/Data/HandTools/TOOL_WIND_UMBRELLA.asset",
            };
            var created = new List<Object>();
            var manager = new RunManager(() => 12);
            manager.StartNewRun();
            try
            {
                foreach (string assetPath in assetPaths)
                {
                    HandToolDefinition definition = AssetDatabase.LoadAssetAtPath<HandToolDefinition>(assetPath);
                    Assert.That(definition, Is.Not.Null, assetPath);
                    Assert.That(definition.RuntimePrefab, Is.Not.Null, definition.ToolId);
                    Assert.That(definition.ShopPriceWon, Is.GreaterThan(0), definition.ToolId);
                    Assert.That(definition.ShopPriceWon % 10, Is.Zero, definition.ToolId);

                    GameObject runtimeObject = Object.Instantiate(definition.RuntimePrefab);
                    created.Add(runtimeObject);
                    HandToolRuntime runtime = runtimeObject.GetComponent<HandToolRuntime>();
                    Assert.That(runtime, Is.Not.Null, definition.ToolId);
                    Assert.That(runtime.Definition, Is.SameAs(definition), definition.ToolId);
                    IHandSlotHudSource hud = runtime;
                    Assert.That(hud.StableItemId, Is.EqualTo(definition.ToolId));
                    Assert.That(hud.PrimaryActionLabel, Is.Not.Empty, definition.ToolId);
                    Assert.That(hud.ShowResource,
                        Is.EqualTo(definition.ResourceMode != ToolResourceMode.Infinite),
                        definition.ToolId);
                    Assert.That(hud.CurrentResource,
                        Is.EqualTo(definition.ResourceMode == ToolResourceMode.Infinite ? 0 : definition.MaxResource),
                        definition.ToolId);
                    Assert.That(hud.MaximumResource, Is.EqualTo(hud.CurrentResource), definition.ToolId);

                    GameObject counterObject = new GameObject(definition.ToolId + "_ApprovalCounter");
                    created.Add(counterObject);
                    ToolShopCounter counter = counterObject.AddComponent<ToolShopCounter>();
                    counter.ConfigureForTests(definition, manager);
                    Assert.That(counter.PriceWon, Is.EqualTo(definition.ShopPriceWon), definition.ToolId);
                    Assert.That(counter.PriceLabel, Is.EqualTo(definition.ShopPriceWon + "원"), definition.ToolId);
                    Assert.That(counter.PromptLabel, Is.EqualTo("구매  " + counter.PriceLabel), definition.ToolId);
                }
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                {
                    if (created[index] != null)
                    {
                        Object.DestroyImmediate(created[index]);
                    }
                }
            }
        }

        private static HandToolDefinition CreateDefinition(
            string id,
            int maximum,
            int price,
            out GameObject prefab)
        {
            HandToolDefinition definition = ScriptableObject.CreateInstance<HandToolDefinition>();
            definition.Configure(
                id,
                id,
                ToolTag.Pickaxe,
                ToolResourceMode.Durability,
                maximum,
                price,
                new ToolActionProfile(),
                new ToolActionProfile(),
                new[] { Vector2Int.right });
            prefab = new GameObject(id + "_ShopPrefab");
            PickaxeRuntime runtime = prefab.AddComponent<PickaxeRuntime>();
            runtime.Configure(definition);
            definition.AssignRuntimePrefab(prefab);
            return definition;
        }

        private sealed class PlacementWorld : ICarryPlacementWorld
        {
            private readonly bool safe;

            public PlacementWorld(bool safeCell)
            {
                safe = safeCell;
            }

            public bool IsInsideRoom(RectInt footprint) => safe;
            public bool IsFootprintClear(RectInt footprint) => safe;
            public bool HasStableSupport(RectInt footprint) => safe;
            public bool IsPortalGap(RectInt footprint) => false;
            public bool IsVoid(RectInt footprint) => false;
        }
    }
}

#endif
