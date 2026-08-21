#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Generation.P6;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Population.P7
{
    public enum P7PopulationKind
    {
        SmallGold = 0,
        BigGold = 1,
        TreasureChest = 2,
        Enemy = 3,
        Trap = 4,
        ShopOffer = 5
    }

    public enum P7PopulationRoute
    {
        Main = 0,
        Optional = 1
    }

    public enum P7EnemyKind
    {
        Walker = 0,
        Hopper = 1,
        Charger = 2,
        Spitter = 3,
        Burrower = 4,
        Flyer = 5,
        Carrier = 6
    }

    public enum P7TrapKind
    {
        RollingRiceCake = 0,
        FallingPestle = 1,
        CrumblingMoonFloor = 2,
        SnappingThread = 3,
        Crosswind = 4,
        SwayingPlatform = 5,
        Floodgate = 6,
        Riptide = 7,
        BubbleLauncher = 8,
        ReturnStamp = 9,
        ParcelLauncher = 10,
        SuctionMailTube = 11,
        RotatingSunlight = 12,
        ShadowSeed = 13,
        OverheatedVine = 14,
        ReturnField = 15,
        StarWeight = 16,
        MixedLegacyTrap = 17
    }

    public enum P7ShopProductKind
    {
        RopeBundle3 = 0,
        BombBundle2 = 1,
        MoonCake = 2,
        HandTool6 = 3,
        HandTool7 = 4,
        HandTool8 = 5,
        LaniLanternRecharge = 6
    }

    [Serializable]
    public struct P7EnemyWeight
    {
        [SerializeField] private P7EnemyKind kind;
        [SerializeField, Min(1)] private int cost;
        [SerializeField, Min(1)] private int weight;

        public P7EnemyWeight(P7EnemyKind enemyKind, int budgetCost, int selectionWeight)
        {
            kind = enemyKind;
            cost = Mathf.Max(1, budgetCost);
            weight = Mathf.Max(1, selectionWeight);
        }

        public P7EnemyKind Kind => kind;
        public int Cost => Mathf.Max(1, cost);
        public int Weight => Mathf.Max(1, weight);
    }

    [Serializable]
    public struct P7TrapWeight
    {
        [SerializeField] private P7TrapKind kind;
        [SerializeField, Min(1)] private int cost;
        [SerializeField, Min(1)] private int weight;

        public P7TrapWeight(P7TrapKind trapKind, int budgetCost, int selectionWeight)
        {
            kind = trapKind;
            cost = Mathf.Max(1, budgetCost);
            weight = Mathf.Max(1, selectionWeight);
        }

        public P7TrapKind Kind => kind;
        public int Cost => Mathf.Max(1, cost);
        public int Weight => Mathf.Max(1, weight);
    }

    public static class P7EconomyRules
    {
        public const int SmallGoldValue = 1;
        public const int BigGoldValue = 3;
        public const int MinimumChestValue = 4;
        public const int MaximumChestValue = 6;
        public const int RequiredWorldOfferCount = 3;
        public const int DefaultMainPathGoldTarget = 4;
        public const int DefaultStageBudgetFloorPercent = 60;
        public const int MaximumSlotUnitCount = 3;

        public static int DefaultStageBudgetFloor(int stageBudget)
        {
            return stageBudget <= 0
                ? 0
                : Mathf.Max(
                    1,
                    stageBudget * DefaultStageBudgetFloorPercent / 100);
        }

        // 한 방의 한 트랙(적/함정)이 반드시 소진할 수 있는 예산 하한.
        // 무리 상한과 최소 코스트 잔돈을 모두 뺀 보수적 값이라
        // 배치기가 어떤 종류를 뽑아도 이 값 이상은 지출된다.
        public static int GuaranteedTrackSpend(
            int roomBudget,
            int slotCount,
            int minimumUnitCost)
        {
            if (roomBudget <= 0
                || slotCount <= 0
                || minimumUnitCost <= 0
                || roomBudget < minimumUnitCost)
            {
                return 0;
            }

            return Mathf.Min(
                roomBudget - (minimumUnitCost - 1),
                slotCount * MaximumSlotUnitCount * minimumUnitCost);
        }

        public static bool IsLegalLooseGoldValue(int value)
        {
            return value == SmallGoldValue || value == BigGoldValue;
        }

        public static bool IsLegalChestValue(int value)
        {
            return value >= MinimumChestValue && value <= MaximumChestValue;
        }

        public static int Price(P7ShopProductKind product)
        {
            switch (product)
            {
                case P7ShopProductKind.RopeBundle3:
                    return 3;
                case P7ShopProductKind.BombBundle2:
                    return 4;
                case P7ShopProductKind.MoonCake:
                    return 3;
                case P7ShopProductKind.HandTool6:
                    return 6;
                case P7ShopProductKind.HandTool7:
                    return 7;
                case P7ShopProductKind.HandTool8:
                    return 8;
                case P7ShopProductKind.LaniLanternRecharge:
                    return 9;
                default:
                    throw new ArgumentOutOfRangeException(nameof(product), product, null);
            }
        }

        public static int RewardValueForRisk(int riskScore)
        {
            if (riskScore <= 2)
            {
                return BigGoldValue;
            }

            return Mathf.Clamp(riskScore, MinimumChestValue, MaximumChestValue);
        }

        public static P7PopulationKind RewardKindForRisk(int riskScore)
        {
            return RewardValueForRisk(riskScore) == BigGoldValue
                ? P7PopulationKind.BigGold
                : P7PopulationKind.TreasureChest;
        }
    }

    [Serializable]
    public sealed class P7RegionPlacementProfile
    {
        private readonly P7EnemyWeight[] enemyWeights;
        private readonly P7TrapWeight[] trapWeights;
        private readonly P7ShopProductKind[] shopProducts;

        public P7RegionPlacementProfile(
            RoomRegion region,
            Vector3Int enemyBudgets,
            Vector3Int hazardBudgets,
            int mainPathGoldTarget,
            IReadOnlyList<P7EnemyWeight> enemies,
            IReadOnlyList<P7TrapWeight> traps,
            IReadOnlyList<P7ShopProductKind> products,
            Vector3Int enemyBudgetFloors = default,
            Vector3Int hazardBudgetFloors = default)
        {
            Region = region;
            EnemyBudgets = new Vector3Int(
                Mathf.Max(0, enemyBudgets.x),
                Mathf.Max(0, enemyBudgets.y),
                Mathf.Max(0, enemyBudgets.z));
            HazardBudgets = new Vector3Int(
                Mathf.Max(0, hazardBudgets.x),
                Mathf.Max(0, hazardBudgets.y),
                Mathf.Max(0, hazardBudgets.z));
            EnemyBudgetFloors = ResolveFloors(EnemyBudgets, enemyBudgetFloors);
            HazardBudgetFloors =
                ResolveFloors(HazardBudgets, hazardBudgetFloors);
            MainPathGoldTarget = Mathf.Max(
                P7EconomyRules.BigGoldValue,
                mainPathGoldTarget);
            enemyWeights = enemies?.ToArray() ?? Array.Empty<P7EnemyWeight>();
            trapWeights = traps?.ToArray() ?? Array.Empty<P7TrapWeight>();
            shopProducts = products?.Distinct().ToArray()
                ?? Array.Empty<P7ShopProductKind>();

            Validate();
        }

        public RoomRegion Region { get; }
        public Vector3Int EnemyBudgets { get; }
        public Vector3Int HazardBudgets { get; }
        public Vector3Int EnemyBudgetFloors { get; }
        public Vector3Int HazardBudgetFloors { get; }
        public int MainPathGoldTarget { get; }
        public IReadOnlyList<P7EnemyWeight> EnemyWeights => enemyWeights;
        public IReadOnlyList<P7TrapWeight> TrapWeights => trapWeights;
        public IReadOnlyList<P7ShopProductKind> ShopProducts => shopProducts;

        public int EnemyBudget(P6StageSlot stageSlot)
        {
            return StageValue(EnemyBudgets, stageSlot);
        }

        public int HazardBudget(P6StageSlot stageSlot)
        {
            return StageValue(HazardBudgets, stageSlot);
        }

        public int EnemyBudgetFloor(P6StageSlot stageSlot)
        {
            return StageValue(EnemyBudgetFloors, stageSlot);
        }

        public int HazardBudgetFloor(P6StageSlot stageSlot)
        {
            return StageValue(HazardBudgetFloors, stageSlot);
        }

        public void Validate()
        {
            if (Region == RoomRegion.Universal)
            {
                throw new InvalidOperationException(
                    "A P7 placement profile must target a concrete region.");
            }

            if (enemyWeights.Length == 0 || trapWeights.Length == 0)
            {
                throw new InvalidOperationException(
                    $"P7 profile {Region} requires enemy and trap choices.");
            }

            if (shopProducts.Length < P7EconomyRules.RequiredWorldOfferCount)
            {
                throw new InvalidOperationException(
                    $"P7 profile {Region} requires at least three shop products.");
            }

            if (!shopProducts.Any(product =>
                    P7EconomyRules.Price(product)
                    <= P7EconomyRules.BigGoldValue))
            {
                throw new InvalidOperationException(
                    $"P7 profile {Region} has no entry-price Momo product.");
            }
        }

        private static Vector3Int ResolveFloors(
            Vector3Int ceilings,
            Vector3Int authored)
        {
            return new Vector3Int(
                ResolveFloor(ceilings.x, authored.x),
                ResolveFloor(ceilings.y, authored.y),
                ResolveFloor(ceilings.z, authored.z));
        }

        private static int ResolveFloor(int ceiling, int authored)
        {
            int value = authored > 0
                ? authored
                : P7EconomyRules.DefaultStageBudgetFloor(ceiling);
            return Mathf.Clamp(value, 0, ceiling);
        }

        private static int StageValue(Vector3Int values, P6StageSlot stageSlot)
        {
            switch (stageSlot)
            {
                case P6StageSlot.X1:
                    return values.x;
                case P6StageSlot.X2:
                    return values.y;
                case P6StageSlot.X3:
                    return values.z;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(stageSlot),
                        stageSlot,
                        null);
            }
        }
    }

    [Serializable]
    public sealed class P7RoomSlotDescriptor
    {
        public P7RoomSlotDescriptor(
            string slotId,
            RoomContentSlotKind kind,
            Vector2Int cellOffset,
            bool visibleFromMainRoute)
        {
            SlotId = slotId ?? string.Empty;
            Kind = kind;
            CellOffset = cellOffset;
            VisibleFromMainRoute = visibleFromMainRoute;
        }

        public string SlotId { get; }
        public RoomContentSlotKind Kind { get; }
        public Vector2Int CellOffset { get; }
        public bool VisibleFromMainRoute { get; }
    }

    [Serializable]
    public sealed class P7RoomPopulationDescriptor
    {
        private readonly P7RoomSlotDescriptor[] slots;

        public P7RoomPopulationDescriptor(
            string prefabId,
            RoomRegion region,
            int enemyBudget,
            int hazardBudget,
            IReadOnlyList<P7RoomSlotDescriptor> contentSlots)
        {
            PrefabId = prefabId ?? string.Empty;
            Region = region;
            EnemyBudget = Mathf.Max(0, enemyBudget);
            HazardBudget = Mathf.Max(0, hazardBudget);
            slots = contentSlots?.ToArray() ?? Array.Empty<P7RoomSlotDescriptor>();
        }

        public string PrefabId { get; }
        public RoomRegion Region { get; }
        public int EnemyBudget { get; }
        public int HazardBudget { get; }
        public IReadOnlyList<P7RoomSlotDescriptor> Slots => slots;

        public IEnumerable<P7RoomSlotDescriptor> EnumerateSlots(
            RoomContentSlotKind kind)
        {
            return slots.Where(slot => slot != null && slot.Kind == kind);
        }

        public P7RoomSlotDescriptor FirstSlot(RoomContentSlotKind kind)
        {
            return slots.FirstOrDefault(slot => slot != null && slot.Kind == kind);
        }

        public static P7RoomPopulationDescriptor Capture(RoomTemplate2D template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var captured = new List<P7RoomSlotDescriptor>(
                template.ContentSlots.Count);
            for (int index = 0; index < template.ContentSlots.Count; index++)
            {
                RoomContentSlot2D slot = template.ContentSlots[index];
                if (slot == null)
                {
                    throw new InvalidOperationException(
                        $"[{template.RoomId}] Content slot {index} is null.");
                }

                captured.Add(new P7RoomSlotDescriptor(
                    slot.SlotId,
                    slot.Kind,
                    slot.CellOffset,
                    slot.VisibleFromMainRoute));
            }

            return new P7RoomPopulationDescriptor(
                template.RoomId,
                template.Region,
                template.EnemyBudget,
                template.HazardBudget,
                captured);
        }
    }

    public sealed class P7StageGraphRoom
    {
        public P7StageGraphRoom(
            int nodeId,
            string prefabId,
            RectInt macroBounds,
            RoomRole role,
            bool onMainPath,
            int mainPathIndex)
        {
            NodeId = nodeId;
            PrefabId = prefabId ?? string.Empty;
            MacroBounds = macroBounds;
            Role = role;
            OnMainPath = onMainPath;
            MainPathIndex = mainPathIndex;
        }

        public int NodeId { get; }
        public string PrefabId { get; }
        public RectInt MacroBounds { get; }
        public RoomRole Role { get; }
        public bool OnMainPath { get; }
        public int MainPathIndex { get; }
    }

    public readonly struct P7StageGraphEdge
    {
        public P7StageGraphEdge(int firstNodeId, int secondNodeId)
        {
            FirstNodeId = Mathf.Min(firstNodeId, secondNodeId);
            SecondNodeId = Mathf.Max(firstNodeId, secondNodeId);
        }

        public int FirstNodeId { get; }
        public int SecondNodeId { get; }
    }

    public sealed class P7StageGraphSnapshot
    {
        public P7StageGraphSnapshot(
            P6StageSlot stageSlot,
            RoomRegion region,
            int startNodeId,
            int exitNodeId,
            IReadOnlyList<P7StageGraphRoom> rooms,
            IReadOnlyList<P7StageGraphEdge> edges,
            IReadOnlyList<int> mainPathNodeIds)
        {
            StageSlot = stageSlot;
            Region = region;
            StartNodeId = startNodeId;
            ExitNodeId = exitNodeId;
            Rooms = rooms?.ToArray() ?? Array.Empty<P7StageGraphRoom>();
            Edges = edges?.ToArray() ?? Array.Empty<P7StageGraphEdge>();
            MainPathNodeIds = mainPathNodeIds?.ToArray() ?? Array.Empty<int>();
        }

        public P6StageSlot StageSlot { get; }
        public RoomRegion Region { get; }
        public int StartNodeId { get; }
        public int ExitNodeId { get; }
        public IReadOnlyList<P7StageGraphRoom> Rooms { get; }
        public IReadOnlyList<P7StageGraphEdge> Edges { get; }
        public IReadOnlyList<int> MainPathNodeIds { get; }

        public static P7StageGraphSnapshot Capture(P6RoomGraphPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var mainIndices = new Dictionary<int, int>();
            for (int index = 0; index < plan.MainPathNodeIds.Count; index++)
            {
                mainIndices[plan.MainPathNodeIds[index]] = index;
            }

            RoomRegion region = plan.Rooms.Count > 0
                ? plan.Rooms[0].Region
                : RoomRegion.Universal;
            P7StageGraphRoom[] rooms = plan.Rooms
                .Select(room => new P7StageGraphRoom(
                    room.Id,
                    room.PrefabId,
                    room.MacroBounds,
                    room.Role,
                    room.OnMainPath,
                    mainIndices.TryGetValue(room.Id, out int mainIndex)
                        ? mainIndex
                        : -1))
                .ToArray();
            P7StageGraphEdge[] edges = plan.Edges
                .Select(edge => new P7StageGraphEdge(
                    edge.FirstNodeId,
                    edge.SecondNodeId))
                .ToArray();

            return new P7StageGraphSnapshot(
                plan.StageSlot,
                region,
                plan.StartNodeId,
                plan.ExitNodeId,
                rooms,
                edges,
                plan.MainPathNodeIds);
        }
    }

    public sealed class P7ShopOfferPlan
    {
        public P7ShopOfferPlan(
            int nodeId,
            string slotId,
            Vector2Int worldCell,
            P7ShopProductKind product)
        {
            NodeId = nodeId;
            SlotId = slotId ?? string.Empty;
            WorldCell = worldCell;
            Product = product;
            Price = P7EconomyRules.Price(product);
        }

        public int NodeId { get; }
        public string SlotId { get; }
        public Vector2Int WorldCell { get; }
        public P7ShopProductKind Product { get; }
        public int Price { get; }
        public int PriceIconCount => Price;
    }

    public sealed class P7PopulationPlacement
    {
        public P7PopulationPlacement(
            int nodeId,
            string prefabId,
            string slotId,
            Vector2Int worldCell,
            P7PopulationKind kind,
            P7PopulationRoute route,
            int value,
            int riskScore,
            P7EnemyKind enemyKind = default,
            P7TrapKind trapKind = default,
            int unitCount = 1)
        {
            NodeId = nodeId;
            PrefabId = prefabId ?? string.Empty;
            SlotId = slotId ?? string.Empty;
            WorldCell = worldCell;
            Kind = kind;
            Route = route;
            Value = Mathf.Max(0, value);
            RiskScore = Mathf.Max(0, riskScore);
            EnemyKind = enemyKind;
            TrapKind = trapKind;
            UnitCount = Mathf.Clamp(
                unitCount,
                1,
                P7EconomyRules.MaximumSlotUnitCount);
        }

        public int NodeId { get; }
        public string PrefabId { get; }
        public string SlotId { get; }
        public Vector2Int WorldCell { get; }
        public P7PopulationKind Kind { get; }
        public P7PopulationRoute Route { get; }
        public int Value { get; }
        public int RiskScore { get; }
        public P7EnemyKind EnemyKind { get; }
        public P7TrapKind TrapKind { get; }
        // 같은 슬롯에 겹쳐 배치되는 동일 계열 무리 수. Value는 무리 전체 코스트다.
        public int UnitCount { get; }
        public bool IsReward =>
            Kind == P7PopulationKind.SmallGold
            || Kind == P7PopulationKind.BigGold
            || Kind == P7PopulationKind.TreasureChest;
    }

    public sealed class P7RoomBudgetLedger
    {
        internal P7RoomBudgetLedger(
            int nodeId,
            string prefabId,
            int enemyBudget,
            int hazardBudget,
            int enemySpent,
            int hazardSpent)
        {
            NodeId = nodeId;
            PrefabId = prefabId ?? string.Empty;
            EnemyBudget = Mathf.Max(0, enemyBudget);
            HazardBudget = Mathf.Max(0, hazardBudget);
            EnemySpent = Mathf.Max(0, enemySpent);
            HazardSpent = Mathf.Max(0, hazardSpent);
        }

        public int NodeId { get; }
        public string PrefabId { get; }
        public int EnemyBudget { get; }
        public int HazardBudget { get; }
        public int EnemySpent { get; }
        public int HazardSpent { get; }
    }

    public sealed class P7PopulationPlan
    {
        internal P7PopulationPlan(
            int seed,
            RoomRegion region,
            P6StageSlot stageSlot,
            IReadOnlyList<P7PopulationPlacement> placements,
            IReadOnlyList<P7ShopOfferPlan> offers,
            IReadOnlyList<P7RoomBudgetLedger> ledgers,
            int mainPathGold,
            int mainPathGoldAvailableForShop,
            int optionalRisk,
            int optionalRewardValue,
            string fingerprint)
        {
            Seed = seed;
            Region = region;
            StageSlot = stageSlot;
            Placements = placements ?? Array.Empty<P7PopulationPlacement>();
            ShopOffers = offers ?? Array.Empty<P7ShopOfferPlan>();
            RoomBudgets = ledgers ?? Array.Empty<P7RoomBudgetLedger>();
            MainPathGold = mainPathGold;
            MainPathGoldAvailableForShop = mainPathGoldAvailableForShop;
            OptionalRiskScore = optionalRisk;
            OptionalRewardValue = optionalRewardValue;
            Fingerprint = fingerprint ?? string.Empty;
        }

        public int Seed { get; }
        public RoomRegion Region { get; }
        public P6StageSlot StageSlot { get; }
        public IReadOnlyList<P7PopulationPlacement> Placements { get; }
        public IReadOnlyList<P7ShopOfferPlan> ShopOffers { get; }
        public IReadOnlyList<P7RoomBudgetLedger> RoomBudgets { get; }
        public int MainPathGold { get; }
        public int MainPathGoldAvailableForShop { get; }
        public int OptionalRiskScore { get; }
        public int OptionalRewardValue { get; }
        public string Fingerprint { get; }
        public int MinimumShopPrice =>
            ShopOffers.Count == 0
                ? 0
                : ShopOffers.Min(offer => offer.Price);
    }

    public sealed class P7PopulationValidationReport
    {
        private readonly List<string> issues = new List<string>();

        public IReadOnlyList<string> Issues => issues;
        public bool Passed => issues.Count == 0;
        public bool RoomBudgetsSatisfied { get; internal set; }
        public bool StageBudgetsSatisfied { get; internal set; }
        public bool StageBudgetFloorClamped { get; internal set; }
        public int StageEnemyFloorCapacity { get; internal set; }
        public int StageHazardFloorCapacity { get; internal set; }
        public bool SlotClaimsUnique { get; internal set; }
        public bool MainPathPurchaseSatisfied { get; internal set; }
        public bool OptionalRiskRewardSatisfied { get; internal set; }
        public bool ShopContractSatisfied { get; internal set; }
        public bool ExitApproachProtected { get; internal set; }

        internal void Add(string issue)
        {
            if (!string.IsNullOrWhiteSpace(issue))
            {
                issues.Add(issue);
            }
        }

        public override string ToString()
        {
            return Passed ? "PASS" : string.Join(Environment.NewLine, issues);
        }
    }

    public sealed class P7PopulationResult
    {
        internal P7PopulationResult(
            P7PopulationPlan plan,
            P7PopulationValidationReport validation,
            string failureReason)
        {
            Plan = plan;
            Validation = validation;
            FailureReason = failureReason ?? string.Empty;
        }

        public P7PopulationPlan Plan { get; }
        public P7PopulationValidationReport Validation { get; }
        public string FailureReason { get; }
        public bool Accepted =>
            Plan != null && Validation != null && Validation.Passed;
    }

    public sealed class P7PopulationRequest
    {
        public P7PopulationRequest(
            int seed,
            P7StageGraphSnapshot graph,
            IReadOnlyList<P7RoomPopulationDescriptor> roomCatalog,
            P7RegionPlacementProfile profile)
        {
            Seed = seed;
            Graph = graph;
            RoomCatalog = roomCatalog ?? Array.Empty<P7RoomPopulationDescriptor>();
            Profile = profile;
        }

        public int Seed { get; }
        public P7StageGraphSnapshot Graph { get; }
        public IReadOnlyList<P7RoomPopulationDescriptor> RoomCatalog { get; }
        public P7RegionPlacementProfile Profile { get; }
    }
}

#endif
