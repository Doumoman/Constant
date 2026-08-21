#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarNight.Generation.P6;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Population.P7
{
    public static class PopulationDirector
    {
        public static P7PopulationResult Generate(P7PopulationRequest request)
        {
            var report = new P7PopulationValidationReport();
            if (!TryValidateRequest(request, report, out string reason))
            {
                return new P7PopulationResult(null, report, reason);
            }

            try
            {
                P7PopulationPlan plan = BuildPlan(request);
                P7PopulationValidator.Validate(request, plan, report);
                return new P7PopulationResult(
                    plan,
                    report,
                    report.Passed ? string.Empty : report.ToString());
            }
            catch (Exception exception)
            {
                report.Add(exception.Message);
                return new P7PopulationResult(
                    null,
                    report,
                    exception.Message);
            }
        }

        private static P7PopulationPlan BuildPlan(P7PopulationRequest request)
        {
            P7StageGraphSnapshot graph = request.Graph;
            P7RegionPlacementProfile profile = request.Profile;
            var random = new P6DeterministicRandom(request.Seed);
            Dictionary<string, P7RoomPopulationDescriptor> catalog =
                request.RoomCatalog.ToDictionary(
                    room => room.PrefabId,
                    StringComparer.Ordinal);
            Dictionary<int, MutableRoomAllocation> allocations =
                graph.Rooms.ToDictionary(
                    room => room.NodeId,
                    room => new MutableRoomAllocation(
                        room,
                        catalog[room.PrefabId]));
            var placements = new List<P7PopulationPlacement>();
            var offers = new List<P7ShopOfferPlan>();

            int exitApproachNodeId =
                graph.MainPathNodeIds.Count >= 2
                    ? graph.MainPathNodeIds[
                        graph.MainPathNodeIds.Count - 2]
                    : graph.ExitNodeId;
            int enemyCeiling = profile.EnemyBudget(graph.StageSlot);
            int hazardCeiling = profile.HazardBudget(graph.StageSlot);
            int enemyTarget = ResolveStageThreatTarget(
                profile.EnemyBudgetFloor(graph.StageSlot),
                enemyCeiling,
                ref random);
            int hazardTarget = ResolveStageThreatTarget(
                profile.HazardBudgetFloor(graph.StageSlot),
                hazardCeiling,
                ref random);
            int enemySpent = 0;
            int hazardSpent = 0;
            var enemyGroups = new List<MutableThreatGroup>();
            var hazardGroups = new List<MutableThreatGroup>();

            MutableRoomAllocation[] populationOrder = allocations.Values
                .OrderBy(allocation =>
                    HasRole(allocation.Room.Role, RoomRole.RiskyChoice)
                        ? 0
                        : allocation.Room.OnMainPath ? 2 : 1)
                .ThenBy(allocation => allocation.Room.NodeId)
                .ToArray();
            for (int index = 0; index < populationOrder.Length; index++)
            {
                MutableRoomAllocation allocation = populationOrder[index];
                if (IsPopulationProtected(allocation.Room.Role))
                {
                    continue;
                }

                SeedTrack(
                    allocation,
                    profile,
                    false,
                    enemyGroups,
                    enemyCeiling,
                    ref enemySpent,
                    ref random);

                bool protectExitApproach =
                    allocation.Room.NodeId == graph.ExitNodeId
                    || allocation.Room.NodeId == exitApproachNodeId;
                if (protectExitApproach)
                {
                    continue;
                }

                SeedTrack(
                    allocation,
                    profile,
                    true,
                    hazardGroups,
                    hazardCeiling,
                    ref hazardSpent,
                    ref random);
            }

            TopUpTrack(
                enemyGroups,
                profile,
                false,
                enemyTarget,
                enemyCeiling,
                ref enemySpent,
                ref random);
            TopUpTrack(
                hazardGroups,
                profile,
                true,
                hazardTarget,
                hazardCeiling,
                ref hazardSpent,
                ref random);
            AppendGroups(enemyGroups, placements);
            AppendGroups(hazardGroups, placements);

            P7StageGraphRoom shopRoom = graph.Rooms.SingleOrDefault(room =>
                HasRole(room.Role, RoomRole.ShopCorridor));
            int shopMainIndex =
                shopRoom != null && shopRoom.MainPathIndex >= 0
                    ? shopRoom.MainPathIndex
                    : int.MaxValue;
            int mainPathGold = PlaceMainPathGold(
                graph,
                allocations,
                profile.MainPathGoldTarget,
                shopMainIndex,
                placements,
                ref random);
            int mainPathGoldAvailableForShop = mainPathGold;

            if (shopRoom != null)
            {
                BuildShopOffers(
                    shopRoom,
                    allocations[shopRoom.NodeId].Descriptor,
                    profile.ShopProducts,
                    offers,
                    ref random);
            }

            int optionalRisk = 0;
            int optionalRewardValue = 0;
            P7StageGraphRoom riskyRoom = graph.Rooms.SingleOrDefault(room =>
                HasRole(room.Role, RoomRole.RiskyChoice));
            if (riskyRoom != null)
            {
                MutableRoomAllocation allocation =
                    allocations[riskyRoom.NodeId];
                int branchDepth = DistanceToMainPath(graph, riskyRoom.NodeId);
                optionalRisk = Mathf.Clamp(
                    Mathf.Max(1, branchDepth)
                    + allocation.EnemySpent
                    + allocation.HazardSpent,
                    1,
                    P7EconomyRules.MaximumChestValue);
                optionalRewardValue =
                    P7EconomyRules.RewardValueForRisk(optionalRisk);
                P7RoomSlotDescriptor rewardSlot =
                    FindRewardSlot(allocation.Descriptor);
                if (rewardSlot == null)
                {
                    throw new InvalidOperationException(
                        $"[{riskyRoom.PrefabId}] RiskyChoice has no reward-capable slot.");
                }

                placements.Add(new P7PopulationPlacement(
                    riskyRoom.NodeId,
                    riskyRoom.PrefabId,
                    rewardSlot.SlotId,
                    WorldCell(riskyRoom, rewardSlot),
                    P7EconomyRules.RewardKindForRisk(optionalRisk),
                    P7PopulationRoute.Optional,
                    optionalRewardValue,
                    optionalRisk));
            }

            P7RoomBudgetLedger[] ledgers = allocations.Values
                .OrderBy(allocation => allocation.Room.NodeId)
                .Select(allocation => new P7RoomBudgetLedger(
                    allocation.Room.NodeId,
                    allocation.Room.PrefabId,
                    allocation.Descriptor.EnemyBudget,
                    allocation.Descriptor.HazardBudget,
                    allocation.EnemySpent,
                    allocation.HazardSpent))
                .ToArray();
            placements.Sort(ComparePlacements);
            offers.Sort((first, second) =>
                string.CompareOrdinal(first.SlotId, second.SlotId));
            string fingerprint = BuildFingerprint(
                request.Seed,
                graph,
                placements,
                offers,
                ledgers);

            return new P7PopulationPlan(
                request.Seed,
                graph.Region,
                graph.StageSlot,
                placements,
                offers,
                ledgers,
                mainPathGold,
                mainPathGoldAvailableForShop,
                optionalRisk,
                optionalRewardValue,
                fingerprint);
        }

        private static int ResolveStageThreatTarget(
            int floor,
            int ceiling,
            ref P6DeterministicRandom random)
        {
            if (ceiling <= 0)
            {
                return 0;
            }

            int low = Mathf.Clamp(floor, 0, ceiling);
            return low >= ceiling
                ? ceiling
                : low + random.NextInt(ceiling - low + 1);
        }

        private static void SeedTrack(
            MutableRoomAllocation allocation,
            P7RegionPlacementProfile profile,
            bool hazard,
            List<MutableThreatGroup> groups,
            int ceiling,
            ref int spent,
            ref P6DeterministicRandom random)
        {
            P7RoomSlotDescriptor[] slots = allocation.Descriptor
                .EnumerateSlots(
                    hazard
                        ? RoomContentSlotKind.Trap
                        : RoomContentSlotKind.Enemy)
                .OrderBy(slot => slot.SlotId, StringComparer.Ordinal)
                .ToArray();
            for (int index = 0; index < slots.Length; index++)
            {
                int allowance = Mathf.Min(
                    RoomAllowance(allocation, hazard),
                    ceiling - spent);
                if (allowance <= 0
                    || !TryBuyUnit(
                        profile,
                        hazard,
                        allowance,
                        ref random,
                        out int cost,
                        out P7EnemyKind enemyKind,
                        out P7TrapKind trapKind))
                {
                    return;
                }

                Charge(allocation, hazard, cost);
                spent += cost;
                groups.Add(new MutableThreatGroup(
                    allocation,
                    slots[index],
                    hazard,
                    cost,
                    enemyKind,
                    trapKind));
            }
        }

        private static void TopUpTrack(
            List<MutableThreatGroup> groups,
            P7RegionPlacementProfile profile,
            bool hazard,
            int target,
            int ceiling,
            ref int spent,
            ref P6DeterministicRandom random)
        {
            bool progressed = groups.Count > 0;
            while (progressed && spent < target && spent < ceiling)
            {
                progressed = false;
                for (int index = 0;
                     index < groups.Count && spent < target;
                     index++)
                {
                    MutableThreatGroup group = groups[index];
                    if (group.UnitCount
                        >= P7EconomyRules.MaximumSlotUnitCount)
                    {
                        continue;
                    }

                    int allowance = Mathf.Min(
                        RoomAllowance(group.Allocation, hazard),
                        ceiling - spent);
                    if (allowance <= 0
                        || !TryBuyUnit(
                            profile,
                            hazard,
                            allowance,
                            ref random,
                            out int cost,
                            out _,
                            out _))
                    {
                        continue;
                    }

                    Charge(group.Allocation, hazard, cost);
                    spent += cost;
                    group.Cost += cost;
                    group.UnitCount++;
                    progressed = true;
                }
            }
        }

        private static bool TryBuyUnit(
            P7RegionPlacementProfile profile,
            bool hazard,
            int allowance,
            ref P6DeterministicRandom random,
            out int cost,
            out P7EnemyKind enemyKind,
            out P7TrapKind trapKind)
        {
            if (hazard)
            {
                enemyKind = default;
                bool bought = TrySelectTrap(
                    profile.TrapWeights,
                    allowance,
                    ref random,
                    out P7TrapWeight trap);
                trapKind = bought ? trap.Kind : default;
                cost = bought ? trap.Cost : 0;
                return bought;
            }

            trapKind = default;
            bool selected = TrySelectEnemy(
                profile.EnemyWeights,
                allowance,
                ref random,
                out P7EnemyWeight enemy);
            enemyKind = selected ? enemy.Kind : default;
            cost = selected ? enemy.Cost : 0;
            return selected;
        }

        private static int RoomAllowance(
            MutableRoomAllocation allocation,
            bool hazard)
        {
            return hazard
                ? allocation.Descriptor.HazardBudget
                    - allocation.HazardSpent
                : allocation.Descriptor.EnemyBudget
                    - allocation.EnemySpent;
        }

        private static void Charge(
            MutableRoomAllocation allocation,
            bool hazard,
            int cost)
        {
            if (hazard)
            {
                allocation.HazardSpent += cost;
                return;
            }

            allocation.EnemySpent += cost;
        }

        private static void AppendGroups(
            List<MutableThreatGroup> groups,
            List<P7PopulationPlacement> placements)
        {
            for (int index = 0; index < groups.Count; index++)
            {
                MutableThreatGroup group = groups[index];
                P7StageGraphRoom room = group.Allocation.Room;
                placements.Add(new P7PopulationPlacement(
                    room.NodeId,
                    room.PrefabId,
                    group.Slot.SlotId,
                    WorldCell(room, group.Slot),
                    group.Hazard
                        ? P7PopulationKind.Trap
                        : P7PopulationKind.Enemy,
                    room.OnMainPath
                        ? P7PopulationRoute.Main
                        : P7PopulationRoute.Optional,
                    group.Cost,
                    0,
                    group.EnemyKind,
                    group.TrapKind,
                    group.UnitCount));
            }
        }

        private static int PlaceMainPathGold(
            P7StageGraphSnapshot graph,
            IReadOnlyDictionary<int, MutableRoomAllocation> allocations,
            int targetValue,
            int shopMainIndex,
            List<P7PopulationPlacement> placements,
            ref P6DeterministicRandom random)
        {
            MutableRoomAllocation[] candidates = allocations.Values
                .Where(allocation =>
                    allocation.Room.OnMainPath
                    && allocation.Room.NodeId != graph.StartNodeId
                    && allocation.Room.NodeId != graph.ExitNodeId
                    && allocation.Descriptor.FirstSlot(
                        RoomContentSlotKind.SafeCell) != null)
                .OrderBy(allocation =>
                    allocation.Room.MainPathIndex < shopMainIndex ? 0 : 1)
                .ThenBy(allocation => allocation.Room.MainPathIndex)
                .ToArray();
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException(
                    "P7 cannot place gold on the generated main route: "
                    + $"rooms={graph.Rooms.Count}, "
                    + $"mainIds={graph.MainPathNodeIds.Count}, "
                    + $"shopIndex={shopMainIndex}, "
                    + $"mainFlags={allocations.Values.Count(value => value.Room.OnMainPath)}, "
                    + $"safeSlots={allocations.Values.Count(value => value.Descriptor.FirstSlot(RoomContentSlotKind.SafeCell) != null)}.");
            }

            int[] order = Enumerable.Range(0, candidates.Length).ToArray();
            if (order.Length > 2)
            {
                int[] middle = order.Skip(1).ToArray();
                random.Shuffle(middle);
                for (int index = 0; index < middle.Length; index++)
                {
                    order[index + 1] = middle[index];
                }
            }

            int remaining = Mathf.Max(
                P7EconomyRules.BigGoldValue,
                targetValue);
            int total = 0;
            for (int placementIndex = 0;
                 placementIndex < order.Length && remaining > 0;
                 placementIndex++)
            {
                int slotsLeft = order.Length - placementIndex;
                int value =
                    remaining > slotsLeft
                    && remaining >= P7EconomyRules.BigGoldValue
                        ? P7EconomyRules.BigGoldValue
                        : P7EconomyRules.SmallGoldValue;
                MutableRoomAllocation allocation =
                    candidates[order[placementIndex]];
                P7RoomSlotDescriptor slot =
                    allocation.Descriptor.FirstSlot(
                        RoomContentSlotKind.SafeCell);
                placements.Add(new P7PopulationPlacement(
                    allocation.Room.NodeId,
                    allocation.Room.PrefabId,
                    slot.SlotId,
                    WorldCell(allocation.Room, slot),
                    value == P7EconomyRules.BigGoldValue
                        ? P7PopulationKind.BigGold
                        : P7PopulationKind.SmallGold,
                    P7PopulationRoute.Main,
                    value,
                    0));
                remaining -= value;
                total += value;
            }

            return total;
        }

        private static void BuildShopOffers(
            P7StageGraphRoom shopRoom,
            P7RoomPopulationDescriptor descriptor,
            IReadOnlyList<P7ShopProductKind> productPool,
            List<P7ShopOfferPlan> offers,
            ref P6DeterministicRandom random)
        {
            P7RoomSlotDescriptor[] shopSlots = descriptor
                .EnumerateSlots(RoomContentSlotKind.Shop)
                .OrderBy(slot => slot.SlotId, StringComparer.Ordinal)
                .ToArray();
            if (shopSlots.Length != P7EconomyRules.RequiredWorldOfferCount)
            {
                throw new InvalidOperationException(
                    $"[{shopRoom.PrefabId}] Momo requires exactly three world shop slots.");
            }

            P7ShopProductKind[] uniquePool = productPool.Distinct().ToArray();
            P7ShopProductKind[] entryProducts = uniquePool
                .Where(product =>
                    P7EconomyRules.Price(product)
                    <= P7EconomyRules.BigGoldValue)
                .ToArray();
            P7ShopProductKind entry =
                entryProducts[random.NextInt(entryProducts.Length)];
            var selected = new List<P7ShopProductKind> { entry };
            P7ShopProductKind[] remainder = uniquePool
                .Where(product => product != entry)
                .ToArray();
            random.Shuffle(remainder);
            for (int index = 0;
                 index < remainder.Length
                 && selected.Count < P7EconomyRules.RequiredWorldOfferCount;
                 index++)
            {
                selected.Add(remainder[index]);
            }

            if (selected.Count != P7EconomyRules.RequiredWorldOfferCount)
            {
                throw new InvalidOperationException(
                    $"[{shopRoom.PrefabId}] Momo product pool is insufficient.");
            }

            for (int index = 0; index < shopSlots.Length; index++)
            {
                P7RoomSlotDescriptor slot = shopSlots[index];
                offers.Add(new P7ShopOfferPlan(
                    shopRoom.NodeId,
                    slot.SlotId,
                    WorldCell(shopRoom, slot),
                    selected[index]));
            }
        }

        private static bool TrySelectEnemy(
            IReadOnlyList<P7EnemyWeight> choices,
            int allowance,
            ref P6DeterministicRandom random,
            out P7EnemyWeight selected)
        {
            P7EnemyWeight[] eligible = choices
                .Where(choice => choice.Cost <= allowance)
                .ToArray();
            return TryWeightedSelect(
                eligible,
                choice => choice.Weight,
                ref random,
                out selected);
        }

        private static bool TrySelectTrap(
            IReadOnlyList<P7TrapWeight> choices,
            int allowance,
            ref P6DeterministicRandom random,
            out P7TrapWeight selected)
        {
            P7TrapWeight[] eligible = choices
                .Where(choice => choice.Cost <= allowance)
                .ToArray();
            return TryWeightedSelect(
                eligible,
                choice => choice.Weight,
                ref random,
                out selected);
        }

        private static bool TryWeightedSelect<T>(
            IReadOnlyList<T> choices,
            Func<T, int> weight,
            ref P6DeterministicRandom random,
            out T selected)
        {
            int total = 0;
            for (int index = 0; index < choices.Count; index++)
            {
                total += Mathf.Max(1, weight(choices[index]));
            }

            if (total <= 0)
            {
                selected = default;
                return false;
            }

            int roll = random.NextInt(total);
            for (int index = 0; index < choices.Count; index++)
            {
                roll -= Mathf.Max(1, weight(choices[index]));
                if (roll < 0)
                {
                    selected = choices[index];
                    return true;
                }
            }

            selected = choices[choices.Count - 1];
            return true;
        }

        private static P7RoomSlotDescriptor FindRewardSlot(
            P7RoomPopulationDescriptor descriptor)
        {
            return descriptor.FirstSlot(RoomContentSlotKind.Treasure)
                ?? descriptor.FirstSlot(RoomContentSlotKind.OptionalReward)
                ?? descriptor.FirstSlot(RoomContentSlotKind.SafeCell);
        }

        private static int DistanceToMainPath(
            P7StageGraphSnapshot graph,
            int origin)
        {
            var main = new HashSet<int>(graph.MainPathNodeIds);
            if (main.Contains(origin))
            {
                return 0;
            }

            Dictionary<int, List<int>> adjacency = graph.Rooms.ToDictionary(
                room => room.NodeId,
                _ => new List<int>());
            for (int index = 0; index < graph.Edges.Count; index++)
            {
                P7StageGraphEdge edge = graph.Edges[index];
                adjacency[edge.FirstNodeId].Add(edge.SecondNodeId);
                adjacency[edge.SecondNodeId].Add(edge.FirstNodeId);
            }

            var visited = new HashSet<int> { origin };
            var frontier = new Queue<(int Node, int Distance)>();
            frontier.Enqueue((origin, 0));
            while (frontier.Count > 0)
            {
                (int current, int distance) = frontier.Dequeue();
                if (main.Contains(current))
                {
                    return distance;
                }

                List<int> neighbours = adjacency[current];
                for (int index = 0; index < neighbours.Count; index++)
                {
                    if (visited.Add(neighbours[index]))
                    {
                        frontier.Enqueue((neighbours[index], distance + 1));
                    }
                }
            }

            return 1;
        }

        private static Vector2Int WorldCell(
            P7StageGraphRoom room,
            P7RoomSlotDescriptor slot)
        {
            Vector2Int macro = RoomTemplate2D.MacroCellSize;
            return new Vector2Int(
                room.MacroBounds.xMin * macro.x + slot.CellOffset.x,
                room.MacroBounds.yMin * macro.y + slot.CellOffset.y);
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

        private static bool HasRole(RoomRole value, RoomRole role)
        {
            return (value & role) == role;
        }

        private static int ComparePlacements(
            P7PopulationPlacement first,
            P7PopulationPlacement second)
        {
            int node = first.NodeId.CompareTo(second.NodeId);
            if (node != 0)
            {
                return node;
            }

            int slot = string.CompareOrdinal(first.SlotId, second.SlotId);
            return slot != 0 ? slot : first.Kind.CompareTo(second.Kind);
        }

        private static string BuildFingerprint(
            int seed,
            P7StageGraphSnapshot graph,
            IReadOnlyList<P7PopulationPlacement> placements,
            IReadOnlyList<P7ShopOfferPlan> offers,
            IReadOnlyList<P7RoomBudgetLedger> ledgers)
        {
            var source = new StringBuilder();
            source.Append(seed)
                .Append('|').Append(graph.Region)
                .Append('|').Append(graph.StageSlot);
            for (int index = 0; index < placements.Count; index++)
            {
                P7PopulationPlacement placement = placements[index];
                source.Append("|P:")
                    .Append(placement.NodeId).Append(':')
                    .Append(placement.SlotId).Append(':')
                    .Append(placement.Kind).Append(':')
                    .Append(placement.Value).Append(':')
                    .Append(placement.UnitCount).Append(':')
                    .Append(placement.EnemyKind).Append(':')
                    .Append(placement.TrapKind);
            }

            for (int index = 0; index < offers.Count; index++)
            {
                P7ShopOfferPlan offer = offers[index];
                source.Append("|S:")
                    .Append(offer.NodeId).Append(':')
                    .Append(offer.SlotId).Append(':')
                    .Append(offer.Product).Append(':')
                    .Append(offer.Price);
            }

            for (int index = 0; index < ledgers.Count; index++)
            {
                P7RoomBudgetLedger ledger = ledgers[index];
                source.Append("|B:")
                    .Append(ledger.NodeId).Append(':')
                    .Append(ledger.EnemySpent).Append(':')
                    .Append(ledger.HazardSpent);
            }

            ulong hash = 1469598103934665603UL;
            string text = source.ToString();
            for (int index = 0; index < text.Length; index++)
            {
                hash ^= text[index];
                hash *= 1099511628211UL;
            }

            return hash.ToString("X16");
        }

        private static bool TryValidateRequest(
            P7PopulationRequest request,
            P7PopulationValidationReport report,
            out string reason)
        {
            if (request == null)
            {
                reason = "P7 population request is null.";
                report.Add(reason);
                return false;
            }

            if (request.Graph == null || request.Profile == null)
            {
                reason = "P7 graph and region profile are required.";
                report.Add(reason);
                return false;
            }

            if (request.Graph.Region != request.Profile.Region)
            {
                reason =
                    $"P7 graph region {request.Graph.Region} does not match "
                    + $"profile {request.Profile.Region}.";
                report.Add(reason);
                return false;
            }

            if (request.Graph.Rooms.Count == 0)
            {
                reason = "P7 graph contains no rooms.";
                report.Add(reason);
                return false;
            }

            var catalog = new Dictionary<string, P7RoomPopulationDescriptor>(
                StringComparer.Ordinal);
            for (int index = 0; index < request.RoomCatalog.Count; index++)
            {
                P7RoomPopulationDescriptor descriptor =
                    request.RoomCatalog[index];
                if (descriptor == null
                    || string.IsNullOrWhiteSpace(descriptor.PrefabId)
                    || !catalog.TryAdd(descriptor.PrefabId, descriptor))
                {
                    reason = "P7 room catalog contains a null or duplicate id.";
                    report.Add(reason);
                    return false;
                }
            }

            for (int index = 0; index < request.Graph.Rooms.Count; index++)
            {
                P7StageGraphRoom room = request.Graph.Rooms[index];
                if (!catalog.TryGetValue(
                        room.PrefabId,
                        out P7RoomPopulationDescriptor descriptor)
                    || descriptor.Region != request.Graph.Region)
                {
                    reason =
                        $"P7 room descriptor is missing or has the wrong region: "
                        + room.PrefabId;
                    report.Add(reason);
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private sealed class MutableRoomAllocation
        {
            public MutableRoomAllocation(
                P7StageGraphRoom room,
                P7RoomPopulationDescriptor descriptor)
            {
                Room = room;
                Descriptor = descriptor;
            }

            public P7StageGraphRoom Room { get; }
            public P7RoomPopulationDescriptor Descriptor { get; }
            public int EnemySpent { get; set; }
            public int HazardSpent { get; set; }
        }

        private sealed class MutableThreatGroup
        {
            public MutableThreatGroup(
                MutableRoomAllocation allocation,
                P7RoomSlotDescriptor slot,
                bool hazard,
                int cost,
                P7EnemyKind enemyKind,
                P7TrapKind trapKind)
            {
                Allocation = allocation;
                Slot = slot;
                Hazard = hazard;
                Cost = cost;
                EnemyKind = enemyKind;
                TrapKind = trapKind;
                UnitCount = 1;
            }

            public MutableRoomAllocation Allocation { get; }
            public P7RoomSlotDescriptor Slot { get; }
            public bool Hazard { get; }
            public P7EnemyKind EnemyKind { get; }
            public P7TrapKind TrapKind { get; }
            public int Cost { get; set; }
            public int UnitCount { get; set; }
        }
    }
}

#endif
