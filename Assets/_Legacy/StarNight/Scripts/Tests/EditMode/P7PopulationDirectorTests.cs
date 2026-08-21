#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using StarNight.Generation.P6;
using StarNight.Population.P7;
using StarNight.Rooms;
using UnityEditor;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    public sealed class P7PopulationDirectorTests
    {
        private const string MoonLibraryPath =
            "Assets/StarNight/Data/P4/P4_MoonRoomPrefabLibrary.asset";
        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(created[index]);
            }

            created.Clear();
        }

        [Test]
        public void RegionProfiles_CoverEveryRegionAndUseAuthoredTrapFamilies()
        {
            IReadOnlyList<P7RegionPlacementProfile> profiles =
                P7RegionProfileCatalog.CreateAll();
            Assert.That(profiles.Count, Is.EqualTo(7));
            Assert.That(
                profiles.Select(profile => profile.Region).Distinct().Count(),
                Is.EqualTo(7));

            foreach (P7RegionPlacementProfile profile in profiles)
            {
                Assert.That(profile.Region, Is.Not.EqualTo(RoomRegion.Universal));
                Assert.That(profile.EnemyWeights, Is.Not.Empty);
                Assert.That(profile.TrapWeights, Is.Not.Empty);
                AssertStageCurve(
                    profile.Region,
                    "enemy",
                    profile.EnemyBudget(P6StageSlot.X1),
                    profile.EnemyBudget(P6StageSlot.X2),
                    profile.EnemyBudget(P6StageSlot.X3));
                AssertStageCurve(
                    profile.Region,
                    "hazard",
                    profile.HazardBudget(P6StageSlot.X1),
                    profile.HazardBudget(P6StageSlot.X2),
                    profile.HazardBudget(P6StageSlot.X3));
                AssertStageFloors(
                    profile.Region,
                    "enemy",
                    profile.EnemyBudgets,
                    profile.EnemyBudgetFloors);
                AssertStageFloors(
                    profile.Region,
                    "hazard",
                    profile.HazardBudgets,
                    profile.HazardBudgetFloors);
                Assert.That(
                    profile.ShopProducts.Count,
                    Is.GreaterThanOrEqualTo(3));
                Assert.That(
                    profile.ShopProducts.Any(product =>
                        P7EconomyRules.Price(product) <= 3),
                    Is.True);
            }

            AssertProfileTraps(
                profiles,
                RoomRegion.MoonPalace,
                P7TrapKind.RollingRiceCake,
                P7TrapKind.FallingPestle,
                P7TrapKind.CrumblingMoonFloor);
            AssertProfileTraps(
                profiles,
                RoomRegion.MagpieBridge,
                P7TrapKind.SnappingThread,
                P7TrapKind.Crosswind,
                P7TrapKind.SwayingPlatform);
            AssertProfileTraps(
                profiles,
                RoomRegion.DragonPalace,
                P7TrapKind.Floodgate,
                P7TrapKind.Riptide,
                P7TrapKind.BubbleLauncher);
            AssertProfileTraps(
                profiles,
                RoomRegion.StarPostOffice,
                P7TrapKind.ReturnStamp,
                P7TrapKind.ParcelLauncher,
                P7TrapKind.SuctionMailTube);
            AssertProfileTraps(
                profiles,
                RoomRegion.SunriseGarden,
                P7TrapKind.RotatingSunlight,
                P7TrapKind.ShadowSeed,
                P7TrapKind.OverheatedVine);
            AssertProfileTraps(
                profiles,
                RoomRegion.PolarisObservatory,
                P7TrapKind.ReturnField,
                P7TrapKind.StarWeight,
                P7TrapKind.MixedLegacyTrap);
        }

        [Test]
        public void RoomThreatBudget_IsFilledByGroupsUpToTheAuthoredCeiling()
        {
            P7PopulationResult result = GenerateSyntheticStage(3, 3);

            Assert.That(result.Accepted, Is.True, result.FailureReason);
            Assert.That(
                result.Validation.StageEnemyFloorCapacity,
                Is.EqualTo(3));
            Assert.That(
                result.Validation.StageBudgetFloorClamped,
                Is.False);
            P7RoomBudgetLedger ledger = result.Plan.RoomBudgets
                .Single(entry => entry.NodeId == 1);
            Assert.That(ledger.EnemyBudget, Is.EqualTo(3));
            Assert.That(ledger.EnemySpent, Is.EqualTo(3));
            P7PopulationPlacement enemy = result.Plan.Placements
                .Single(placement =>
                    placement.Kind == P7PopulationKind.Enemy);
            Assert.That(enemy.NodeId, Is.EqualTo(1));
            Assert.That(enemy.SlotId, Is.EqualTo("Enemy_00"));
            Assert.That(enemy.UnitCount, Is.EqualTo(3));
            Assert.That(enemy.Value, Is.EqualTo(3));
        }

        [Test]
        public void RoomThreatBudget_StopsAtTheSlotGroupCapAndReportsTheClamp()
        {
            P7PopulationResult result = GenerateSyntheticStage(5, 5);

            Assert.That(result.Accepted, Is.True, result.FailureReason);
            Assert.That(
                result.Validation.StageEnemyFloorCapacity,
                Is.EqualTo(P7EconomyRules.MaximumSlotUnitCount));
            Assert.That(
                result.Validation.StageBudgetFloorClamped,
                Is.True);
            P7PopulationPlacement enemy = result.Plan.Placements
                .Single(placement =>
                    placement.Kind == P7PopulationKind.Enemy);
            Assert.That(
                enemy.UnitCount,
                Is.EqualTo(P7EconomyRules.MaximumSlotUnitCount));
            Assert.That(
                result.Plan.RoomBudgets
                    .Single(entry => entry.NodeId == 1)
                    .EnemySpent,
                Is.EqualTo(P7EconomyRules.MaximumSlotUnitCount));
        }

        [Test]
        public void P4MoonCatalog_ExposesBudgetsAndPopulationSlots()
        {
            RoomPrefabLibrary library = LoadMoonLibrary();
            Assert.That(library.RoomPrefabs.Count, Is.EqualTo(33));
            int enemyRooms = 0;
            int trapRooms = 0;
            int shopRooms = 0;
            foreach (GameObject prefab in library.RoomPrefabs)
            {
                RoomTemplate2D template =
                    prefab.GetComponent<RoomTemplate2D>();
                P7RoomPopulationDescriptor descriptor =
                    P7RoomPopulationDescriptor.Capture(template);
                Assert.That(descriptor.PrefabId, Is.EqualTo(template.RoomId));
                Assert.That(descriptor.Region, Is.EqualTo(template.Region));
                Assert.That(
                    descriptor.EnemyBudget,
                    Is.EqualTo(template.EnemyBudget));
                Assert.That(
                    descriptor.HazardBudget,
                    Is.EqualTo(template.HazardBudget));
                Assert.That(
                    descriptor.FirstSlot(RoomContentSlotKind.SafeCell),
                    Is.Not.Null,
                    template.RoomId);

                if (template.EnemyBudget > 0)
                {
                    enemyRooms++;
                    Assert.That(
                        descriptor.FirstSlot(RoomContentSlotKind.Enemy),
                        Is.Not.Null,
                        template.RoomId);
                }

                if (template.HazardBudget > 0)
                {
                    trapRooms++;
                    Assert.That(
                        descriptor.FirstSlot(RoomContentSlotKind.Trap),
                        Is.Not.Null,
                        template.RoomId);
                }

                if (template.HasRole(RoomRole.ShopCorridor))
                {
                    shopRooms++;
                    Assert.That(
                        descriptor.EnumerateSlots(RoomContentSlotKind.Shop)
                            .Count(),
                        Is.EqualTo(3));
                }
            }

            Assert.That(enemyRooms, Is.GreaterThan(0));
            Assert.That(trapRooms, Is.GreaterThan(0));
            Assert.That(shopRooms, Is.EqualTo(1));
        }

        [Test]
        public void EconomyRuntime_UsesOneAndThreeGoldAndFourToSixChests()
        {
            P7EconomyWallet2D wallet =
                Track("Wallet").AddComponent<P7EconomyWallet2D>();
            P7GoldPickup2D small =
                Track("SmallGold").AddComponent<P7GoldPickup2D>();
            P7GoldPickup2D big =
                Track("BigGold").AddComponent<P7GoldPickup2D>();
            P7TreasureChest2D chest =
                Track("Chest").AddComponent<P7TreasureChest2D>();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => small.Configure(wallet, 2));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => chest.Configure(wallet, 7));

            small.Configure(wallet, 1);
            big.Configure(wallet, 3);
            chest.Configure(wallet, 6);
            Assert.That(small.TryCollect(), Is.True);
            Assert.That(small.TryCollect(), Is.False);
            Assert.That(big.TryCollect(), Is.True);
            Assert.That(chest.TryOpen(), Is.True);
            Assert.That(chest.TryOpen(), Is.False);
            Assert.That(wallet.Gold, Is.EqualTo(10));

            wallet.ResetAtRunEnd();
            Assert.That(wallet.Gold, Is.Zero);
        }

        [Test]
        public void MomoShop_HasThreePhysicalFixedPriceNonStealableOffers()
        {
            P7EconomyWallet2D wallet =
                Track("Wallet").AddComponent<P7EconomyWallet2D>();
            wallet.Configure(10);
            P7ShopInventory2D inventory =
                Track("Inventory").AddComponent<P7ShopInventory2D>();
            P7MomoShop2D shop =
                Track("Momo").AddComponent<P7MomoShop2D>();
            P7MomoOffer2D rope = CreateOffer(
                shop,
                P7ShopProductKind.RopeBundle3);
            P7MomoOffer2D bombs = CreateOffer(
                shop,
                P7ShopProductKind.BombBundle2);
            P7MomoOffer2D cake = CreateOffer(
                shop,
                P7ShopProductKind.MoonCake);
            shop.Configure(
                wallet,
                inventory,
                new[] { rope, bombs, cake });

            Assert.That(shop.OfferCount, Is.EqualTo(3));
            Assert.That(shop.Offers.All(offer =>
                offer.PriceIconCount == offer.Price), Is.True);
            Assert.That(shop.Offers.All(offer =>
                !offer.CanRemoveBeforePurchase), Is.True);
            Assert.That(rope.TryPurchase(), Is.True);
            Assert.That(rope.TryPurchase(), Is.False);
            Assert.That(wallet.Gold, Is.EqualTo(7));
            Assert.That(inventory.RopeCount, Is.EqualTo(3));

            wallet.Configure(3);
            Assert.That(bombs.TryPurchase(), Is.False);
            Assert.That(wallet.Gold, Is.EqualTo(3));
            Assert.That(inventory.BombCount, Is.Zero);
            Assert.That(cake.TryPurchase(), Is.True);
            Assert.That(wallet.Gold, Is.Zero);
            Assert.That(inventory.MoonCakeCount, Is.EqualTo(1));
        }

        [Test]
        public void MoonX2_OneThousandSeeds_FundPurchaseAndMatchOptionalRisk()
        {
            RoomPrefabLibrary library = LoadMoonLibrary();
            P6RoomPrefabDescriptor[] graphCatalog =
                library.RoomPrefabs
                    .Select(prefab =>
                        P6RoomTraversalProofFactory.CreateDescriptor(
                            prefab.GetComponent<RoomTemplate2D>()))
                    .ToArray();
            P7RoomPopulationDescriptor[] populationCatalog =
                library.RoomPrefabs
                    .Select(prefab =>
                        P7RoomPopulationDescriptor.Capture(
                            prefab.GetComponent<RoomTemplate2D>()))
                    .ToArray();
            P7RegionPlacementProfile profile =
                P7RegionProfileCatalog.Create(RoomRegion.MoonPalace);
            var results = new List<P7PopulationResult>(1000);
            var failures = new List<string>();
            var timer = Stopwatch.StartNew();

            for (int seed = 0; seed < 1000; seed++)
            {
                P6GenerationResult graphResult =
                    P6RoomGraphGenerator.Generate(
                        P6GenerationRequest.CreateMoonPalace(
                            seed,
                            P6StageSlot.X2,
                            graphCatalog));
                if (!graphResult.Accepted)
                {
                    failures.Add(
                        $"P6 seed {seed}: {graphResult.FailureReason}");
                    continue;
                }

                P7PopulationResult result = PopulationDirector.Generate(
                    new P7PopulationRequest(
                        P6DeterministicRandom.DeriveSeed(
                            graphResult.AcceptedSeed,
                            7),
                        P7StageGraphSnapshot.Capture(graphResult.Plan),
                        populationCatalog,
                        profile));
                results.Add(result);
                if (!result.Accepted)
                {
                    failures.Add(
                        $"P7 seed {seed}: {result.FailureReason}");
                }
            }

            timer.Stop();
            Assert.That(
                failures,
                Is.Empty,
                string.Join(Environment.NewLine, failures.Take(20)));
            Assert.That(results.Count, Is.EqualTo(1000));
            P7EconomyGateSummary summary =
                P7EconomyGateEvaluator.Evaluate(results);
            Assert.That(summary.Passed, Is.True);
            Assert.That(summary.PurchaseFailures, Is.Zero);
            Assert.That(summary.RiskRewardFailures, Is.Zero);
            Assert.That(
                summary.AverageMainPathGold,
                Is.GreaterThanOrEqualTo(3d));
            Assert.That(
                results.All(result =>
                    result.Plan.ShopOffers.Count == 3
                    && result.Plan.MainPathGoldAvailableForShop
                        >= result.Plan.MinimumShopPrice
                    && result.Validation.RoomBudgetsSatisfied
                    && result.Validation.StageBudgetsSatisfied
                    && result.Validation.ExitApproachProtected),
                Is.True);
            int[] enemySpends = results
                .Select(result =>
                    result.Plan.RoomBudgets.Sum(ledger => ledger.EnemySpent))
                .ToArray();
            int[] hazardSpends = results
                .Select(result =>
                    result.Plan.RoomBudgets.Sum(ledger => ledger.HazardSpent))
                .ToArray();
            Assert.That(
                enemySpends.All(spend =>
                    spend <= profile.EnemyBudget(P6StageSlot.X2)),
                Is.True);
            Assert.That(
                results.Any(result =>
                    result.Plan.Placements.Any(placement =>
                        placement.Kind == P7PopulationKind.Enemy
                        && placement.UnitCount > 1)),
                Is.True,
                "P7 never filled a room threat budget with a group.");
            Assert.That(
                results.All(result =>
                    result.Plan.Placements.All(placement =>
                        placement.UnitCount
                        <= P7EconomyRules.MaximumSlotUnitCount)),
                Is.True);
            TestContext.WriteLine(
                $"P7 1000-seed gate PASS in {timer.Elapsed.TotalSeconds:F3}s; "
                + $"average main gold={summary.AverageMainPathGold:F2}; "
                + $"enemy spend {enemySpends.Min()}..{enemySpends.Max()} "
                + $"(avg {enemySpends.Average():F2}, "
                + $"ceiling {profile.EnemyBudget(P6StageSlot.X2)}); "
                + $"hazard spend {hazardSpends.Min()}..{hazardSpends.Max()} "
                + $"(avg {hazardSpends.Average():F2}, "
                + $"ceiling {profile.HazardBudget(P6StageSlot.X2)}).");
        }

        private static P7PopulationResult GenerateSyntheticStage(
            int roomEnemyBudget,
            int stageEnemyBudget)
        {
            var rooms = new[]
            {
                Room(0, "T_Start", RoomRole.Start, 0, 0),
                Room(1, "T_Enemy", RoomRole.Main, 1, 1),
                Room(2, "T_Plain", RoomRole.Main, 2, 2),
                Room(3, "T_Exit", RoomRole.Exit, 3, 3),
                Room(4, "T_Risky", RoomRole.RiskyChoice, -1, 1, 1)
            };
            var graph = new P7StageGraphSnapshot(
                P6StageSlot.X2,
                RoomRegion.MoonPalace,
                0,
                3,
                rooms,
                new[]
                {
                    new P7StageGraphEdge(0, 1),
                    new P7StageGraphEdge(1, 2),
                    new P7StageGraphEdge(2, 3),
                    new P7StageGraphEdge(1, 4)
                },
                new[] { 0, 1, 2, 3 });
            var catalog = new[]
            {
                Descriptor("T_Start", 0, SafeCell()),
                Descriptor(
                    "T_Enemy",
                    roomEnemyBudget,
                    SafeCell(),
                    new P7RoomSlotDescriptor(
                        "Enemy_00",
                        RoomContentSlotKind.Enemy,
                        new Vector2Int(4, 1),
                        true)),
                Descriptor("T_Plain", 0, SafeCell()),
                Descriptor("T_Exit", 0, SafeCell()),
                Descriptor(
                    "T_Risky",
                    0,
                    SafeCell(),
                    new P7RoomSlotDescriptor(
                        "Treasure_00",
                        RoomContentSlotKind.Treasure,
                        new Vector2Int(6, 1),
                        true))
            };
            var profile = new P7RegionPlacementProfile(
                RoomRegion.MoonPalace,
                new Vector3Int(
                    stageEnemyBudget,
                    stageEnemyBudget,
                    stageEnemyBudget),
                Vector3Int.zero,
                P7EconomyRules.DefaultMainPathGoldTarget,
                new[] { new P7EnemyWeight(P7EnemyKind.Walker, 1, 1) },
                new[] { new P7TrapWeight(P7TrapKind.RollingRiceCake, 1, 1) },
                new[]
                {
                    P7ShopProductKind.RopeBundle3,
                    P7ShopProductKind.BombBundle2,
                    P7ShopProductKind.MoonCake
                },
                new Vector3Int(
                    stageEnemyBudget,
                    stageEnemyBudget,
                    stageEnemyBudget));

            return PopulationDirector.Generate(
                new P7PopulationRequest(4242, graph, catalog, profile));
        }

        private static P7StageGraphRoom Room(
            int nodeId,
            string prefabId,
            RoomRole role,
            int mainPathIndex,
            int macroX,
            int macroY = 0)
        {
            return new P7StageGraphRoom(
                nodeId,
                prefabId,
                new RectInt(macroX, macroY, 1, 1),
                role,
                mainPathIndex >= 0,
                mainPathIndex);
        }

        private static P7RoomPopulationDescriptor Descriptor(
            string prefabId,
            int enemyBudget,
            params P7RoomSlotDescriptor[] slots)
        {
            return new P7RoomPopulationDescriptor(
                prefabId,
                RoomRegion.MoonPalace,
                enemyBudget,
                0,
                slots);
        }

        private static P7RoomSlotDescriptor SafeCell()
        {
            return new P7RoomSlotDescriptor(
                "SafeCell_00",
                RoomContentSlotKind.SafeCell,
                new Vector2Int(2, 1),
                true);
        }

        private static RoomPrefabLibrary LoadMoonLibrary()
        {
            RoomPrefabLibrary library =
                AssetDatabase.LoadAssetAtPath<RoomPrefabLibrary>(
                    MoonLibraryPath);
            Assert.That(
                library,
                Is.Not.Null,
                $"Missing P4 room library: {MoonLibraryPath}");
            return library;
        }

        private static void AssertStageCurve(
            RoomRegion region,
            string track,
            int x1,
            int x2,
            int x3)
        {
            string label = $"{region} {track}";
            Assert.That(x3, Is.LessThan(x1), label);
            Assert.That(x1, Is.LessThan(x2), label);
        }

        private static void AssertStageFloors(
            RoomRegion region,
            string track,
            Vector3Int ceilings,
            Vector3Int floors)
        {
            string label = $"{region} {track} floor";
            Assert.That(floors.x, Is.GreaterThan(0), label);
            Assert.That(floors.y, Is.GreaterThan(0), label);
            Assert.That(floors.z, Is.GreaterThan(0), label);
            Assert.That(floors.x, Is.LessThanOrEqualTo(ceilings.x), label);
            Assert.That(floors.y, Is.LessThanOrEqualTo(ceilings.y), label);
            Assert.That(floors.z, Is.LessThanOrEqualTo(ceilings.z), label);
            Assert.That(floors.z, Is.LessThan(floors.y), label);
        }

        private static void AssertProfileTraps(
            IEnumerable<P7RegionPlacementProfile> profiles,
            RoomRegion region,
            params P7TrapKind[] required)
        {
            var actual = new HashSet<P7TrapKind>(
                profiles
                    .Single(profile => profile.Region == region)
                    .TrapWeights.Select(weight => weight.Kind));
            Assert.That(actual.IsSupersetOf(required), Is.True, region.ToString());
        }

        private P7MomoOffer2D CreateOffer(
            P7MomoShop2D shop,
            P7ShopProductKind product)
        {
            GameObject offerObject = Track(product.ToString());
            GameObject visual = Track(product + "_Visual");
            visual.transform.SetParent(offerObject.transform);
            P7MomoOffer2D offer =
                offerObject.AddComponent<P7MomoOffer2D>();
            int price = P7EconomyRules.Price(product);
            offer.Configure(shop, product, price, visual, price);
            return offer;
        }

        private GameObject Track(string name)
        {
            var value = new GameObject(name);
            created.Add(value);
            return value;
        }
    }
}

#endif
