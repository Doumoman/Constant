#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Rooms;

namespace StarNight.Population.P7
{
    public static class P7PopulationValidator
    {
        public static P7PopulationValidationReport Validate(
            P7PopulationRequest request,
            P7PopulationPlan plan)
        {
            var report = new P7PopulationValidationReport();
            Validate(request, plan, report);
            return report;
        }

        internal static void Validate(
            P7PopulationRequest request,
            P7PopulationPlan plan,
            P7PopulationValidationReport report)
        {
            if (request == null || plan == null || report == null)
            {
                throw new ArgumentNullException(
                    request == null
                        ? nameof(request)
                        : plan == null ? nameof(plan) : nameof(report));
            }

            ValidateRoomBudgets(plan, report);
            ValidateStageBudgets(request, plan, report);
            ValidateSlotClaims(plan, report);
            ValidateMainPathEconomy(request, plan, report);
            ValidateRiskReward(plan, report);
            ValidateShop(request, plan, report);
            ValidateExitApproach(request, plan, report);
        }

        private static void ValidateRoomBudgets(
            P7PopulationPlan plan,
            P7PopulationValidationReport report)
        {
            bool satisfied = true;
            for (int index = 0; index < plan.RoomBudgets.Count; index++)
            {
                P7RoomBudgetLedger ledger = plan.RoomBudgets[index];
                if (ledger.EnemySpent <= ledger.EnemyBudget
                    && ledger.HazardSpent <= ledger.HazardBudget)
                {
                    continue;
                }

                satisfied = false;
                report.Add(
                    $"[{ledger.PrefabId}] Population exceeded its authored "
                    + $"budget: enemy {ledger.EnemySpent}/{ledger.EnemyBudget}, "
                    + $"hazard {ledger.HazardSpent}/{ledger.HazardBudget}.");
            }

            report.RoomBudgetsSatisfied = satisfied;
        }

        private static void ValidateStageBudgets(
            P7PopulationRequest request,
            P7PopulationPlan plan,
            P7PopulationValidationReport report)
        {
            int enemySpent =
                plan.RoomBudgets.Sum(ledger => ledger.EnemySpent);
            int hazardSpent =
                plan.RoomBudgets.Sum(ledger => ledger.HazardSpent);
            int enemyBudget =
                request.Profile.EnemyBudget(request.Graph.StageSlot);
            int hazardBudget =
                request.Profile.HazardBudget(request.Graph.StageSlot);
            int enemyCapacity = StageFloorCapacity(request, plan, false);
            int hazardCapacity = StageFloorCapacity(request, plan, true);
            int authoredEnemyFloor =
                request.Profile.EnemyBudgetFloor(request.Graph.StageSlot);
            int authoredHazardFloor =
                request.Profile.HazardBudgetFloor(request.Graph.StageSlot);
            int enemyFloor = Math.Min(authoredEnemyFloor, enemyCapacity);
            int hazardFloor = Math.Min(authoredHazardFloor, hazardCapacity);
            report.StageEnemyFloorCapacity = enemyCapacity;
            report.StageHazardFloorCapacity = hazardCapacity;
            report.StageBudgetFloorClamped =
                authoredEnemyFloor > enemyCapacity
                || authoredHazardFloor > hazardCapacity;
            report.StageBudgetsSatisfied =
                enemySpent <= enemyBudget
                && hazardSpent <= hazardBudget
                && enemySpent >= enemyFloor
                && hazardSpent >= hazardFloor;
            if (!report.StageBudgetsSatisfied)
            {
                report.Add(
                    "P7 stage budget left its band: enemy "
                    + $"{enemySpent}/[{enemyFloor}..{enemyBudget}], hazard "
                    + $"{hazardSpent}/[{hazardFloor}..{hazardBudget}].");
            }
        }

        private static int StageFloorCapacity(
            P7PopulationRequest request,
            P7PopulationPlan plan,
            bool hazard)
        {
            int unitCost = hazard
                ? MinimumTrapCost(request.Profile)
                : MinimumEnemyCost(request.Profile);
            if (unitCost <= 0)
            {
                return 0;
            }

            var roles = new Dictionary<int, RoomRole>();
            for (int index = 0; index < request.Graph.Rooms.Count; index++)
            {
                P7StageGraphRoom room = request.Graph.Rooms[index];
                roles[room.NodeId] = room.Role;
            }

            var slotCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            RoomContentSlotKind slotKind = hazard
                ? RoomContentSlotKind.Trap
                : RoomContentSlotKind.Enemy;
            for (int index = 0; index < request.RoomCatalog.Count; index++)
            {
                P7RoomPopulationDescriptor descriptor =
                    request.RoomCatalog[index];
                if (descriptor == null)
                {
                    continue;
                }

                slotCounts[descriptor.PrefabId] =
                    descriptor.EnumerateSlots(slotKind).Count();
            }

            int exitApproach = ExitApproachNodeId(request.Graph);
            int capacity = 0;
            for (int index = 0; index < plan.RoomBudgets.Count; index++)
            {
                P7RoomBudgetLedger ledger = plan.RoomBudgets[index];
                if (!roles.TryGetValue(ledger.NodeId, out RoomRole role)
                    || IsPopulationProtected(role))
                {
                    continue;
                }

                if (hazard
                    && (ledger.NodeId == request.Graph.ExitNodeId
                        || ledger.NodeId == exitApproach))
                {
                    continue;
                }

                if (!slotCounts.TryGetValue(ledger.PrefabId, out int slots))
                {
                    slots = 0;
                }

                capacity += P7EconomyRules.GuaranteedTrackSpend(
                    hazard ? ledger.HazardBudget : ledger.EnemyBudget,
                    slots,
                    unitCost);
            }

            return capacity;
        }

        private static int MinimumEnemyCost(P7RegionPlacementProfile profile)
        {
            int minimum = int.MaxValue;
            for (int index = 0; index < profile.EnemyWeights.Count; index++)
            {
                minimum = Math.Min(minimum, profile.EnemyWeights[index].Cost);
            }

            return minimum == int.MaxValue ? 0 : minimum;
        }

        private static int MinimumTrapCost(P7RegionPlacementProfile profile)
        {
            int minimum = int.MaxValue;
            for (int index = 0; index < profile.TrapWeights.Count; index++)
            {
                minimum = Math.Min(minimum, profile.TrapWeights[index].Cost);
            }

            return minimum == int.MaxValue ? 0 : minimum;
        }

        private static bool IsPopulationProtected(RoomRole role)
        {
            RoomRole protectedRoles =
                RoomRole.Start
                | RoomRole.Exit
                | RoomRole.ShopCorridor
                | RoomRole.SupplyCorridor;
            return (role & protectedRoles) != 0;
        }

        private static int ExitApproachNodeId(P7StageGraphSnapshot graph)
        {
            return graph.MainPathNodeIds.Count >= 2
                ? graph.MainPathNodeIds[graph.MainPathNodeIds.Count - 2]
                : graph.ExitNodeId;
        }

        private static void ValidateSlotClaims(
            P7PopulationPlan plan,
            P7PopulationValidationReport report)
        {
            var claims = new HashSet<string>(StringComparer.Ordinal);
            bool satisfied = true;
            for (int index = 0; index < plan.Placements.Count; index++)
            {
                P7PopulationPlacement placement = plan.Placements[index];
                if (claims.Add(Key(placement.NodeId, placement.SlotId)))
                {
                    continue;
                }

                satisfied = false;
                report.Add(
                    $"P7 slot was claimed twice: "
                    + $"{placement.NodeId}/{placement.SlotId}.");
            }

            for (int index = 0; index < plan.ShopOffers.Count; index++)
            {
                P7ShopOfferPlan offer = plan.ShopOffers[index];
                if (claims.Add(Key(offer.NodeId, offer.SlotId)))
                {
                    continue;
                }

                satisfied = false;
                report.Add(
                    $"P7 shop slot was claimed twice: "
                    + $"{offer.NodeId}/{offer.SlotId}.");
            }

            report.SlotClaimsUnique = satisfied;
        }

        private static void ValidateMainPathEconomy(
            P7PopulationRequest request,
            P7PopulationPlan plan,
            P7PopulationValidationReport report)
        {
            bool legalValues = plan.Placements
                .Where(placement =>
                    placement.Route == P7PopulationRoute.Main
                    && (placement.Kind == P7PopulationKind.SmallGold
                        || placement.Kind == P7PopulationKind.BigGold))
                .All(placement =>
                    P7EconomyRules.IsLegalLooseGoldValue(placement.Value));
            int required = plan.ShopOffers.Count > 0
                ? plan.MinimumShopPrice
                : P7EconomyRules.BigGoldValue;
            int available = plan.MainPathGoldAvailableForShop;
            report.MainPathPurchaseSatisfied =
                legalValues
                && available >= required
                && plan.MainPathGold
                    >= P7EconomyRules.BigGoldValue;
            if (!report.MainPathPurchaseSatisfied)
            {
                report.Add(
                    "P7 main route does not fund one entry-price purchase: "
                    + $"available={available}, required={required}, "
                    + $"total={plan.MainPathGold}.");
            }

            if (plan.MainPathGold > request.Profile.MainPathGoldTarget + 2)
            {
                report.MainPathPurchaseSatisfied = false;
                report.Add(
                    "P7 main route gold exceeds the small-number profile band.");
            }
        }

        private static void ValidateRiskReward(
            P7PopulationPlan plan,
            P7PopulationValidationReport report)
        {
            P7PopulationPlacement[] balancedRewards = plan.Placements
                .Where(placement =>
                    placement.Route == P7PopulationRoute.Optional
                    && placement.IsReward
                    && placement.RiskScore > 0)
                .ToArray();
            report.OptionalRiskRewardSatisfied =
                plan.OptionalRiskScore > 0
                && balancedRewards.Length == 1
                && balancedRewards[0].RiskScore
                    == plan.OptionalRiskScore
                && balancedRewards[0].Value
                    == P7EconomyRules.RewardValueForRisk(
                        plan.OptionalRiskScore)
                && plan.OptionalRewardValue
                    == balancedRewards[0].Value
                && (balancedRewards[0].Kind
                    == P7EconomyRules.RewardKindForRisk(
                        plan.OptionalRiskScore));
            if (!report.OptionalRiskRewardSatisfied)
            {
                report.Add(
                    "P7 optional-route reward does not match its risk band.");
            }
        }

        private static void ValidateShop(
            P7PopulationRequest request,
            P7PopulationPlan plan,
            P7PopulationValidationReport report)
        {
            int shopRoomCount = request.Graph.Rooms.Count(room =>
                (room.Role & RoomRole.ShopCorridor)
                == RoomRole.ShopCorridor);
            bool countSatisfied =
                shopRoomCount <= 1
                && (shopRoomCount == 0
                    ? plan.ShopOffers.Count == 0
                    : plan.ShopOffers.Count
                        == P7EconomyRules.RequiredWorldOfferCount);
            bool productsUnique =
                plan.ShopOffers.Select(offer => offer.Product)
                    .Distinct()
                    .Count() == plan.ShopOffers.Count;
            bool pricesCorrect = plan.ShopOffers.All(offer =>
                offer.Price == P7EconomyRules.Price(offer.Product)
                && offer.PriceIconCount == offer.Price);
            bool entryOffer = plan.ShopOffers.Count == 0
                || plan.ShopOffers.Any(offer =>
                    offer.Price <= P7EconomyRules.BigGoldValue);
            report.ShopContractSatisfied =
                countSatisfied
                && productsUnique
                && pricesCorrect
                && entryOffer;
            if (!report.ShopContractSatisfied)
            {
                report.Add(
                    "P7 Momo contract requires at most one corridor, exactly "
                    + "three unique world offers, fixed prices, and an "
                    + "entry-price product.");
            }
        }

        private static void ValidateExitApproach(
            P7PopulationRequest request,
            P7PopulationPlan plan,
            P7PopulationValidationReport report)
        {
            int exitApproach = ExitApproachNodeId(request.Graph);
            report.ExitApproachProtected = plan.Placements.All(placement =>
                placement.Kind != P7PopulationKind.Trap
                || (placement.NodeId != request.Graph.ExitNodeId
                    && placement.NodeId != exitApproach));
            if (!report.ExitApproachProtected)
            {
                report.Add(
                    "P7 placed a trap in the exit room or its approach room.");
            }
        }

        private static string Key(int nodeId, string slotId)
        {
            return $"{nodeId}:{slotId}";
        }
    }
}

#endif
