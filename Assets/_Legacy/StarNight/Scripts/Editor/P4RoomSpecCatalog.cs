#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Editor
{
    internal enum P4RoomLayoutKind
    {
        CrescentStep = 0,
        SoftSoilDip = 1,
        RiceCakeHop = 2,
        PestlePost = 3,
        RootArch = 4,
        CrackedShelf = 5,
        MortarBowl = 6,
        ReturnStep = 7,
        ThreeBeatSteps = 8,
        SafeCrater = 9,
        TwinLedge = 10,
        OverUnder = 11,
        CrescentSwitchback = 12,
        RootBridge = 13,
        DropLoop = 14,
        NarrowGallery = 15,
        LongBridge = 16,
        RollingDoughLane = 17,
        TwinLayerCauseway = 18,
        RootCauseway = 19,
        CrackedShortcut = 20,
        CassiaSwitchback = 21,
        ReturnableWell = 22,
        RopeComparison = 23,
        HookBalcony = 24,
        WindShaft = 25,
        CassiaMillCross = 26,
        MortarAtrium = 27,
        CrescentTerraces = 28,
        MomoShop = 29,
        SupplyRest = 30,
        RecordArchive = 31,
        MaruStatueSanctum = 32
    }

    internal readonly struct P4SocketBuildSpec
    {
        public readonly string Id;
        public readonly RoomSocketSide Side;
        public readonly Vector2Int CellOffset;
        public readonly Vector2Int OpeningSize;
        public readonly RoomTraversalType TraversalType;
        public readonly bool MainRouteAllowed;
        public readonly Vector2Int ValidationAnchor;

        public P4SocketBuildSpec(
            string id,
            RoomSocketSide side,
            Vector2Int cellOffset,
            Vector2Int openingSize,
            RoomTraversalType traversalType,
            bool mainRouteAllowed,
            Vector2Int validationAnchor)
        {
            Id = id;
            Side = side;
            CellOffset = cellOffset;
            OpeningSize = openingSize;
            TraversalType = traversalType;
            MainRouteAllowed = mainRouteAllowed;
            ValidationAnchor = validationAnchor;
        }
    }

    internal readonly struct P4SlotBuildSpec
    {
        public readonly string Id;
        public readonly RoomContentSlotKind Kind;
        public readonly Vector2Int CellOffset;
        public readonly bool VisibleFromMainRoute;

        public P4SlotBuildSpec(
            string id,
            RoomContentSlotKind kind,
            Vector2Int cellOffset,
            bool visibleFromMainRoute)
        {
            Id = id;
            Kind = kind;
            CellOffset = cellOffset;
            VisibleFromMainRoute = visibleFromMainRoute;
        }
    }

    internal sealed class P4RoomBuildSpec
    {
        public readonly string Id;
        public readonly RoomArchetype Archetype;
        public readonly RoomRegion Region;
        public readonly RoomRole Roles;
        public readonly Vector2Int MacroSize;
        public readonly Vector2Int LogicalSize;
        public readonly string CoreActionSentence;
        public readonly RoomValidationProfile ValidationProfile;
        public readonly P4RoomLayoutKind LayoutKind;
        public readonly int HazardBudget;
        public readonly int EnemyBudget;
        public readonly int IntroducedRuleCount;
        public readonly int LandmarkCount;
        public readonly int MovementLayerCount;
        public readonly float ExpectedTraversalSeconds;
        public readonly bool RewardVisible;
        public readonly bool ExitBlockProof;
        public readonly P4SocketBuildSpec[] Sockets;
        public readonly P4SlotBuildSpec[] Slots;

        public P4RoomBuildSpec(
            string id,
            RoomArchetype archetype,
            RoomRole roles,
            Vector2Int macroSize,
            Vector2Int logicalSize,
            string coreActionSentence,
            RoomValidationProfile validationProfile,
            P4RoomLayoutKind layoutKind,
            int hazardBudget,
            int enemyBudget,
            int introducedRuleCount,
            int landmarkCount,
            int movementLayerCount,
            float expectedTraversalSeconds,
            bool rewardVisible,
            bool exitBlockProof,
            P4SocketBuildSpec[] sockets,
            P4SlotBuildSpec[] slots)
        {
            Id = id;
            Archetype = archetype;
            Region = RoomRegion.MoonPalace;
            Roles = roles;
            MacroSize = macroSize;
            LogicalSize = logicalSize;
            CoreActionSentence = coreActionSentence;
            ValidationProfile = validationProfile;
            LayoutKind = layoutKind;
            HazardBudget = hazardBudget;
            EnemyBudget = enemyBudget;
            IntroducedRuleCount = introducedRuleCount;
            LandmarkCount = landmarkCount;
            MovementLayerCount = movementLayerCount;
            ExpectedTraversalSeconds = expectedTraversalSeconds;
            RewardVisible = rewardVisible;
            ExitBlockProof = exitBlockProof;
            Sockets = sockets ?? Array.Empty<P4SocketBuildSpec>();
            Slots = slots ?? Array.Empty<P4SlotBuildSpec>();
        }
    }

    internal static class P4RoomSpecCatalog
    {
        // 문서 22.1은 하한만 규정한다. 방을 더 저작해도 게이트가 막지 않아야 한다.
        private const int MinimumRoomSpecCount = 33;

        private static readonly Vector2Int OneByOneMacro =
            new Vector2Int(1, 1);
        private static readonly Vector2Int TwoByOneMacro =
            new Vector2Int(2, 1);
        private static readonly Vector2Int OneByTwoMacro =
            new Vector2Int(1, 2);
        private static readonly Vector2Int TwoByTwoMacro =
            new Vector2Int(2, 2);
        private static readonly Vector2Int TwelveByEight =
            new Vector2Int(12, 8);
        private static readonly Vector2Int TwentyFourByEight =
            new Vector2Int(24, 8);
        private static readonly Vector2Int TwelveBySixteen =
            new Vector2Int(12, 16);
        private static readonly Vector2Int TwentyFourBySixteen =
            new Vector2Int(24, 16);

        public static P4RoomBuildSpec[] CreateAll()
        {
            P4RoomBuildSpec[] specs =
            {
                Small(
                    "P4_Moon_Small_CrescentStep_01",
                    "한 칸 높이의 초승달 단차를 가볍게 넘어 반대 소켓으로 간다.",
                    P4RoomLayoutKind.CrescentStep,
                    0,
                    0,
                    1,
                    1,
                    6f,
                    false),
                Small(
                    "P4_Moon_Small_SoftSoilDip_01",
                    "부드러운 달 흙의 얕은 홈을 내려갔다가 바로 올라온다.",
                    P4RoomLayoutKind.SoftSoilDip,
                    0,
                    0,
                    1,
                    1,
                    6f,
                    false),
                Small(
                    "P4_Moon_Small_RiceCakeHop_01",
                    "굴러올 달떡 자리를 한 번 뛰어넘어 안전한 바닥에 착지한다.",
                    P4RoomLayoutKind.RiceCakeHop,
                    1,
                    0,
                    1,
                    1,
                    7f,
                    false),
                Small(
                    "P4_Moon_Small_PestlePost_01",
                    "중앙 말뚝 하나를 짧게 넘으며 절굿공이 문법을 눈으로 익힌다.",
                    P4RoomLayoutKind.PestlePost,
                    0,
                    0,
                    1,
                    1,
                    7f,
                    false),
                Small(
                    "P4_Moon_Small_RootArch_01",
                    "계수나무 뿌리 아래의 한 셀 통로를 몸이 걸리지 않게 통과한다.",
                    P4RoomLayoutKind.RootArch,
                    0,
                    0,
                    1,
                    1,
                    7f,
                    false),
                Small(
                    "P4_Moon_Small_CrackedShelf_01",
                    "금 간 선반 아래를 지나며 위쪽 보상을 먼저 발견한다.",
                    P4RoomLayoutKind.CrackedShelf,
                    0,
                    0,
                    1,
                    2,
                    8f,
                    true,
                    Slot(
                        "OptionalReward_00",
                        RoomContentSlotKind.OptionalReward,
                        8,
                        4,
                        true)),
                Small(
                    "P4_Moon_Small_MortarBowl_01",
                    "얕은 절구 모양 구덩이를 건너고 놓쳐도 바닥에서 복귀한다.",
                    P4RoomLayoutKind.MortarBowl,
                    0,
                    0,
                    1,
                    2,
                    8f,
                    false),
                Small(
                    "P4_Moon_Small_ReturnStep_01",
                    "한 층 아래로 안전하게 떨어진 뒤 짧은 계단으로 되돌아간다.",
                    P4RoomLayoutKind.ReturnStep,
                    0,
                    0,
                    1,
                    2,
                    8f,
                    false),

                Standard(
                    "P4_Moon_Standard_ThreeBeatSteps_01",
                    "세 번의 낮은 단차를 일정한 박자로 오르내린다.",
                    P4RoomLayoutKind.ThreeBeatSteps,
                    0,
                    1,
                    1,
                    1,
                    10f,
                    false),
                Standard(
                    "P4_Moon_Standard_SafeCrater_01",
                    "분화구 위의 두 칸 틈을 건너고 실패하면 아래 안전길로 복귀한다.",
                    P4RoomLayoutKind.SafeCrater,
                    0,
                    1,
                    1,
                    2,
                    11f,
                    false),
                Standard(
                    "P4_Moon_Standard_TwinLedge_01",
                    "엇갈린 두 발판을 차례로 밟아 같은 높이의 출구로 간다.",
                    P4RoomLayoutKind.TwinLedge,
                    0,
                    1,
                    1,
                    2,
                    11f,
                    false),
                Standard(
                    "P4_Moon_Standard_OverUnder_01",
                    "아랫길을 통과하면서 위층에 선노출된 보상 경로를 비교한다.",
                    P4RoomLayoutKind.OverUnder,
                    0,
                    1,
                    1,
                    2,
                    12f,
                    true,
                    Slot(
                        "OptionalReward_00",
                        RoomContentSlotKind.OptionalReward,
                        7,
                        5,
                        true)),
                Standard(
                    "P4_Moon_Standard_CrescentSwitchback_01",
                    "완만한 지그재그 계단을 올라 높이가 다른 내부 이동층을 잇는다.",
                    P4RoomLayoutKind.CrescentSwitchback,
                    0,
                    1,
                    1,
                    2,
                    12f,
                    false),
                Standard(
                    "P4_Moon_Standard_RootBridge_01",
                    "계수나무 뿌리 다리를 건너고 아래 안전길로도 같은 출구에 닿는다.",
                    P4RoomLayoutKind.RootBridge,
                    0,
                    2,
                    1,
                    2,
                    12f,
                    false),
                Standard(
                    "P4_Moon_Standard_DropLoop_01",
                    "낮은 낙하로 선택길에 들렀다가 계단을 따라 메인 길에 합류한다.",
                    P4RoomLayoutKind.DropLoop,
                    0,
                    1,
                    1,
                    2,
                    12f,
                    true,
                    Slot(
                        "OptionalReward_00",
                        RoomContentSlotKind.OptionalReward,
                        8,
                        1,
                        true)),
                Standard(
                    "P4_Moon_Standard_NarrowGallery_01",
                    "긴 한 셀 통로를 지난 뒤 열린 공간에서 점프 감각을 다시 확인한다.",
                    P4RoomLayoutKind.NarrowGallery,
                    0,
                    1,
                    1,
                    1,
                    11f,
                    false),

                Wide(
                    "P4_Moon_Wide_LongBridge_01",
                    "긴 달빛 다리를 건너며 아래에 보이는 안전 복귀길을 기억한다.",
                    P4RoomLayoutKind.LongBridge,
                    0,
                    2,
                    1,
                    2,
                    17f,
                    false),
                Wide(
                    "P4_Moon_Wide_RollingDoughLane_01",
                    "완만한 비탈을 굴러오는 달떡의 빈틈에 맞춰 길게 이동한다.",
                    P4RoomLayoutKind.RollingDoughLane,
                    1,
                    2,
                    1,
                    2,
                    18f,
                    false),
                Wide(
                    "P4_Moon_Wide_TwinLayerCauseway_01",
                    "빠른 아랫길과 보상이 보이는 윗길 중 하나를 골라 끝에서 합류한다.",
                    P4RoomLayoutKind.TwinLayerCauseway,
                    0,
                    2,
                    1,
                    2,
                    19f,
                    true,
                    Slot(
                        "Treasure_00",
                        RoomContentSlotKind.Treasure,
                        12,
                        5,
                        true)),
                Wide(
                    "P4_Moon_Wide_RootCauseway_01",
                    "긴 계수나무 뿌리의 끊긴 마디를 연속 점프로 건넌다.",
                    P4RoomLayoutKind.RootCauseway,
                    0,
                    3,
                    1,
                    2,
                    19f,
                    false),
                Wide(
                    "P4_Moon_Wide_CrackedShortcut_01",
                    "금 간 윗지름길을 확인하고 도구 없이 아랫길로 우회해 합류한다.",
                    P4RoomLayoutKind.CrackedShortcut,
                    0,
                    2,
                    1,
                    2,
                    20f,
                    true,
                    Slot(
                        "OptionalReward_00",
                        RoomContentSlotKind.OptionalReward,
                        17,
                        5,
                        true)),

                Tall(
                    "P4_Moon_Tall_CassiaSwitchback_01",
                    "계수나무 벽을 따라 번갈아 놓인 발판을 올라 위쪽 내부층에 닿는다.",
                    P4RoomLayoutKind.CassiaSwitchback,
                    0,
                    2,
                    1,
                    3,
                    20f,
                    false),
                Tall(
                    "P4_Moon_Tall_ReturnableWell_01",
                    "밝게 보이는 낙하축을 따라 내려간 뒤 옆 계단으로 다시 올라온다.",
                    P4RoomLayoutKind.ReturnableWell,
                    0,
                    2,
                    1,
                    3,
                    20f,
                    false),
                Tall(
                    "P4_Moon_Tall_RopeComparison_01",
                    "중앙 로프 지름길을 보면서 바깥 발판만으로 같은 높이를 오른다.",
                    P4RoomLayoutKind.RopeComparison,
                    0,
                    2,
                    1,
                    3,
                    21f,
                    false),
                Tall(
                    "P4_Moon_Tall_HookBalcony_01",
                    "갈고리 고리가 가리키는 보상 발코니를 보면서 지그재그 기본길을 오른다.",
                    P4RoomLayoutKind.HookBalcony,
                    0,
                    2,
                    1,
                    3,
                    21f,
                    true,
                    Slot(
                        "OptionalReward_00",
                        RoomContentSlotKind.OptionalReward,
                        8,
                        11,
                        true)),
                Tall(
                    "P4_Moon_Tall_WindShaft_01",
                    "바람이 보이는 낙하축을 내려가고 측면 발판으로 역방향 복귀한다.",
                    P4RoomLayoutKind.WindShaft,
                    1,
                    1,
                    1,
                    3,
                    22f,
                    false),

                Large(
                    "P4_Moon_Large_CassiaMillCross_01",
                    "계수나무 뿌리를 감아 도는 두 층 길 중 하나를 골라 중앙에서 다시 합류한다.",
                    P4RoomLayoutKind.CassiaMillCross,
                    0,
                    5,
                    1,
                    2,
                    25f,
                    true,
                    Slot(
                        "Landmark_00",
                        RoomContentSlotKind.Landmark,
                        12,
                        7,
                        true),
                    Slot(
                        "OptionalReward_00",
                        RoomContentSlotKind.OptionalReward,
                        18,
                        11,
                        true)),
                Large(
                    "P4_Moon_Large_MortarAtrium_01",
                    "거대한 절구 둘레의 아래 우회로와 위 다리를 오가며 반대편 출구를 찾는다.",
                    P4RoomLayoutKind.MortarAtrium,
                    0,
                    6,
                    1,
                    2,
                    26f,
                    true,
                    Slot(
                        "Landmark_00",
                        RoomContentSlotKind.Landmark,
                        12,
                        6,
                        true),
                    Slot(
                        "Treasure_00",
                        RoomContentSlotKind.Treasure,
                        18,
                        11,
                        true)),
                Large(
                    "P4_Moon_Large_CrescentTerraces_01",
                    "초승달 문을 중심으로 엇갈린 테라스를 오르내려 두 소켓을 서로 잇는다.",
                    P4RoomLayoutKind.CrescentTerraces,
                    0,
                    5,
                    1,
                    3,
                    27f,
                    true,
                    Slot(
                        "Landmark_00",
                        RoomContentSlotKind.Landmark,
                        12,
                        8,
                        true),
                    Slot(
                        "OptionalReward_00",
                        RoomContentSlotKind.OptionalReward,
                        6,
                        11,
                        true)),

                Corridor(
                    "P4_Moon_Corridor_MomoShop_01",
                    RoomRole.ShopCorridor,
                    new Vector2Int(18, 5),
                    "세 받침대의 상품과 골드 아이콘을 한눈에 비교하고 그대로 지나간다.",
                    P4RoomLayoutKind.MomoShop,
                    Slot("Shop_00", RoomContentSlotKind.Shop, 5, 1, true),
                    Slot("Shop_01", RoomContentSlotKind.Shop, 9, 1, true),
                    Slot("Shop_02", RoomContentSlotKind.Shop, 13, 1, true)),
                Corridor(
                    "P4_Moon_Corridor_Supply_01",
                    RoomRole.SupplyCorridor,
                    new Vector2Int(14, 5),
                    "보스 전 회복과 소모품 세 칸을 훑고 필요한 보급을 챙긴다.",
                    P4RoomLayoutKind.SupplyRest,
                    Slot("Supply_00", RoomContentSlotKind.Supply, 4, 1, true),
                    Slot("Supply_01", RoomContentSlotKind.Supply, 7, 1, true),
                    Slot("Supply_02", RoomContentSlotKind.Supply, 10, 1, true)),
                Leaf(
                    "P4_Moon_RecordRoom_01",
                    RoomRole.RecordRoom,
                    "빛나는 이름패를 따라 레버길과 금 간 외벽길 중 하나로 종이빛 기록 슬롯에 닿는다.",
                    P4RoomLayoutKind.RecordArchive,
                    1,
                    2,
                    9f,
                    false,
                    Slot(
                        "RecordGuest_00",
                        RoomContentSlotKind.RecordGuest,
                        8,
                        1,
                        true)),
                Leaf(
                    "P4_Moon_MaruStatueRoom_01",
                    RoomRole.MaruStatue,
                    "가슴의 별눈물이 보이는 귀향상 앞에서 큰 보상과 조기 추적 위험을 저울질한다.",
                    P4RoomLayoutKind.MaruStatueSanctum,
                    1,
                    1,
                    9f,
                    true,
                    Slot(
                        "MaruStatue_00",
                        RoomContentSlotKind.MaruStatue,
                        8,
                        1,
                        true),
                    Slot(
                        "Treasure_00",
                        RoomContentSlotKind.Treasure,
                        6,
                        1,
                        true))
            };

            EnsureCatalogIntegrity(specs);
            return specs;
        }

        private static P4RoomBuildSpec Small(
            string id,
            string sentence,
            P4RoomLayoutKind layout,
            int hazardBudget,
            int enemyBudget,
            int introducedRules,
            int movementLayers,
            float traversalSeconds,
            bool rewardVisible,
            params P4SlotBuildSpec[] extraSlots)
        {
            return Through(
                id,
                RoomArchetype.Small,
                RoomRole.Main,
                OneByOneMacro,
                LogicalSizeFor(layout),
                sentence,
                RoomValidationProfile.ShortInteraction,
                layout,
                hazardBudget,
                enemyBudget,
                introducedRules,
                0,
                movementLayers,
                traversalSeconds,
                rewardVisible,
                extraSlots);
        }

        private static P4RoomBuildSpec Standard(
            string id,
            string sentence,
            P4RoomLayoutKind layout,
            int hazardBudget,
            int enemyBudget,
            int introducedRules,
            int movementLayers,
            float traversalSeconds,
            bool rewardVisible,
            params P4SlotBuildSpec[] extraSlots)
        {
            return Through(
                id,
                RoomArchetype.Standard,
                RoomRole.Main,
                OneByOneMacro,
                LogicalSizeFor(layout),
                sentence,
                movementLayers > 1
                    ? RoomValidationProfile.LayeredTraversal
                    : RoomValidationProfile.StandardTraversal,
                layout,
                hazardBudget,
                enemyBudget,
                introducedRules,
                0,
                movementLayers,
                traversalSeconds,
                rewardVisible,
                extraSlots);
        }

        private static P4RoomBuildSpec Wide(
            string id,
            string sentence,
            P4RoomLayoutKind layout,
            int hazardBudget,
            int enemyBudget,
            int introducedRules,
            int movementLayers,
            float traversalSeconds,
            bool rewardVisible,
            params P4SlotBuildSpec[] extraSlots)
        {
            return Through(
                id,
                RoomArchetype.Wide,
                RoomRole.Main,
                TwoByOneMacro,
                LogicalSizeFor(layout),
                sentence,
                RoomValidationProfile.LayeredTraversal,
                layout,
                hazardBudget,
                enemyBudget,
                introducedRules,
                0,
                movementLayers,
                traversalSeconds,
                rewardVisible,
                extraSlots);
        }

        private static P4RoomBuildSpec Tall(
            string id,
            string sentence,
            P4RoomLayoutKind layout,
            int hazardBudget,
            int enemyBudget,
            int introducedRules,
            int movementLayers,
            float traversalSeconds,
            bool rewardVisible,
            params P4SlotBuildSpec[] extraSlots)
        {
            return Through(
                id,
                RoomArchetype.Tall,
                RoomRole.Main | RoomRole.Shortcut,
                OneByTwoMacro,
                LogicalSizeFor(layout),
                sentence,
                RoomValidationProfile.LayeredTraversal,
                layout,
                hazardBudget,
                enemyBudget,
                introducedRules,
                0,
                movementLayers,
                traversalSeconds,
                rewardVisible,
                extraSlots);
        }

        private static P4RoomBuildSpec Large(
            string id,
            string sentence,
            P4RoomLayoutKind layout,
            int hazardBudget,
            int enemyBudget,
            int introducedRules,
            int movementLayers,
            float traversalSeconds,
            bool rewardVisible,
            params P4SlotBuildSpec[] extraSlots)
        {
            return Through(
                id,
                RoomArchetype.Large,
                RoomRole.Main | RoomRole.Landmark,
                TwoByTwoMacro,
                LogicalSizeFor(layout),
                sentence,
                RoomValidationProfile.LayeredTraversal,
                layout,
                hazardBudget,
                enemyBudget,
                introducedRules,
                1,
                movementLayers,
                traversalSeconds,
                rewardVisible,
                extraSlots);
        }

        private static P4RoomBuildSpec Corridor(
            string id,
            RoomRole role,
            Vector2Int logicalSize,
            string sentence,
            P4RoomLayoutKind layout,
            params P4SlotBuildSpec[] extraSlots)
        {
            return Through(
                id,
                RoomArchetype.HorizontalCorridor,
                role,
                TwoByOneMacro,
                logicalSize,
                sentence,
                RoomValidationProfile.CorridorRest,
                layout,
                0,
                0,
                0,
                0,
                1,
                8f,
                false,
                extraSlots);
        }

        private static P4RoomBuildSpec Leaf(
            string id,
            RoomRole role,
            string sentence,
            P4RoomLayoutKind layout,
            int introducedRules,
            int movementLayers,
            float traversalSeconds,
            bool rewardVisible,
            params P4SlotBuildSpec[] extraSlots)
        {
            Vector2Int logicalSize = LogicalSizeFor(layout);
            return new P4RoomBuildSpec(
                id,
                RoomArchetype.Small,
                role,
                OneByOneMacro,
                logicalSize,
                sentence,
                RoomValidationProfile.OptionalDeadEnd,
                layout,
                0,
                0,
                introducedRules,
                0,
                movementLayers,
                traversalSeconds,
                rewardVisible,
                true,
                // 문서 7.1: 귀향상 방만 '최소 출구 2개'를 물리적으로 저작한다.
                (role & RoomRole.MaruStatue) != 0
                    ? CreateThroughSockets(logicalSize)
                    : CreateLeafSocket(),
                CreateSlots(
                    logicalSize,
                    0,
                    0,
                    extraSlots));
        }

        private static Vector2Int LogicalSizeFor(
            P4RoomLayoutKind layout)
        {
            switch (layout)
            {
                case P4RoomLayoutKind.CrescentStep:
                case P4RoomLayoutKind.SoftSoilDip:
                case P4RoomLayoutKind.PestlePost:
                case P4RoomLayoutKind.CrackedShelf:
                case P4RoomLayoutKind.ThreeBeatSteps:
                    return TwelveByEight;

                case P4RoomLayoutKind.RiceCakeHop:
                    return new Vector2Int(9, 6);
                case P4RoomLayoutKind.RootArch:
                case P4RoomLayoutKind.MortarBowl:
                    return new Vector2Int(10, 6);
                case P4RoomLayoutKind.ReturnStep:
                    return new Vector2Int(9, 7);

                case P4RoomLayoutKind.SafeCrater:
                case P4RoomLayoutKind.TwinLedge:
                case P4RoomLayoutKind.OverUnder:
                case P4RoomLayoutKind.RootBridge:
                case P4RoomLayoutKind.DropLoop:
                    return new Vector2Int(11, 7);
                case P4RoomLayoutKind.CrescentSwitchback:
                    return new Vector2Int(12, 7);
                case P4RoomLayoutKind.NarrowGallery:
                    return new Vector2Int(10, 6);

                case P4RoomLayoutKind.LongBridge:
                    return new Vector2Int(20, 7);
                case P4RoomLayoutKind.RollingDoughLane:
                    return TwentyFourByEight;
                case P4RoomLayoutKind.TwinLayerCauseway:
                case P4RoomLayoutKind.CrackedShortcut:
                    return new Vector2Int(22, 7);
                case P4RoomLayoutKind.RootCauseway:
                    return new Vector2Int(22, 8);

                case P4RoomLayoutKind.CassiaSwitchback:
                    return new Vector2Int(10, 14);
                case P4RoomLayoutKind.ReturnableWell:
                    return new Vector2Int(11, 15);
                case P4RoomLayoutKind.RopeComparison:
                case P4RoomLayoutKind.WindShaft:
                    return new Vector2Int(10, 14);
                case P4RoomLayoutKind.HookBalcony:
                    return TwelveBySixteen;

                case P4RoomLayoutKind.CassiaMillCross:
                    return new Vector2Int(22, 14);
                case P4RoomLayoutKind.MortarAtrium:
                    return TwentyFourBySixteen;
                case P4RoomLayoutKind.CrescentTerraces:
                    return new Vector2Int(20, 14);

                case P4RoomLayoutKind.RecordArchive:
                case P4RoomLayoutKind.MaruStatueSanctum:
                    return new Vector2Int(10, 7);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(layout),
                        layout,
                        "No logical footprint is defined.");
            }
        }

        private static P4RoomBuildSpec Through(
            string id,
            RoomArchetype archetype,
            RoomRole roles,
            Vector2Int macroSize,
            Vector2Int logicalSize,
            string sentence,
            RoomValidationProfile validationProfile,
            P4RoomLayoutKind layout,
            int hazardBudget,
            int enemyBudget,
            int introducedRules,
            int landmarkCount,
            int movementLayers,
            float traversalSeconds,
            bool rewardVisible,
            P4SlotBuildSpec[] extraSlots)
        {
            return new P4RoomBuildSpec(
                id,
                archetype,
                roles,
                macroSize,
                logicalSize,
                sentence,
                validationProfile,
                layout,
                hazardBudget,
                enemyBudget,
                introducedRules,
                landmarkCount,
                movementLayers,
                traversalSeconds,
                rewardVisible,
                true,
                CreateThroughSockets(logicalSize),
                CreateSlots(
                    logicalSize,
                    hazardBudget,
                    enemyBudget,
                    extraSlots));
        }

        private static P4SocketBuildSpec[] CreateThroughSockets(
            Vector2Int logicalSize)
        {
            Vector2Int leftCell = new Vector2Int(0, 1);
            Vector2Int rightCell = new Vector2Int(logicalSize.x - 1, 1);
            return new[]
            {
                new P4SocketBuildSpec(
                    "Left_Walk_00",
                    RoomSocketSide.Left,
                    leftCell,
                    Vector2Int.one,
                    RoomTraversalType.Walk,
                    true,
                    leftCell),
                new P4SocketBuildSpec(
                    "Right_Walk_00",
                    RoomSocketSide.Right,
                    rightCell,
                    Vector2Int.one,
                    RoomTraversalType.Walk,
                    true,
                    rightCell)
            };
        }

        private static P4SocketBuildSpec[] CreateLeafSocket()
        {
            Vector2Int leftCell = new Vector2Int(0, 1);
            return new[]
            {
                new P4SocketBuildSpec(
                    "Left_Walk_00",
                    RoomSocketSide.Left,
                    leftCell,
                    Vector2Int.one,
                    RoomTraversalType.Walk,
                    true,
                    leftCell)
            };
        }

        private static P4SlotBuildSpec[] CreateSlots(
            Vector2Int logicalSize,
            int hazardBudget,
            int enemyBudget,
            P4SlotBuildSpec[] extraSlots)
        {
            List<P4SlotBuildSpec> slots = new List<P4SlotBuildSpec>
            {
                Slot(
                    "SafeCell_00",
                    RoomContentSlotKind.SafeCell,
                    2,
                    1,
                    true),
                Slot(
                    "MaruEntry_00",
                    RoomContentSlotKind.MaruEntry,
                    logicalSize.x - 3,
                    1,
                    true)
            };

            if (hazardBudget > 0)
            {
                slots.Add(
                    Slot(
                        "Trap_00",
                        RoomContentSlotKind.Trap,
                        logicalSize.x / 2 + 1,
                        1,
                        true));
            }

            if (enemyBudget > 0)
            {
                slots.Add(
                    Slot(
                        "Enemy_00",
                        RoomContentSlotKind.Enemy,
                        logicalSize.x / 2 - 1,
                        1,
                        true));
            }

            if (extraSlots != null)
            {
                slots.AddRange(extraSlots);
            }

            return slots.ToArray();
        }

        private static P4SlotBuildSpec Slot(
            string id,
            RoomContentSlotKind kind,
            int x,
            int y,
            bool visibleFromMainRoute)
        {
            return new P4SlotBuildSpec(
                id,
                kind,
                new Vector2Int(x, y),
                visibleFromMainRoute);
        }

        private static void EnsureCatalogIntegrity(P4RoomBuildSpec[] specs)
        {
            if (specs == null || specs.Length < MinimumRoomSpecCount)
            {
                throw new InvalidOperationException(
                    "P4 Moon room catalog must contain at least "
                    + $"{MinimumRoomSpecCount} specs, found {specs?.Length ?? 0}.");
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> sentences =
                new HashSet<string>(StringComparer.Ordinal);
            int small = 0;
            int standard = 0;
            int wide = 0;
            int tall = 0;
            int large = 0;
            int shop = 0;
            int supply = 0;
            int record = 0;
            int maru = 0;

            for (int index = 0; index < specs.Length; index++)
            {
                P4RoomBuildSpec spec = specs[index];
                if (!ids.Add(spec.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate P4 room id: {spec.Id}.");
                }

                if (!sentences.Add(spec.CoreActionSentence))
                {
                    throw new InvalidOperationException(
                        $"Duplicate P4 core action: {spec.CoreActionSentence}");
                }

                if (!RoomPrefabValidator.IsSingleSentence(
                        spec.CoreActionSentence))
                {
                    throw new InvalidOperationException(
                        $"{spec.Id} must have exactly one core-action sentence.");
                }

                if (spec.IntroducedRuleCount < 0
                    || spec.IntroducedRuleCount > 1)
                {
                    throw new InvalidOperationException(
                        $"{spec.Id} introduces more than one rule.");
                }

                ValidateSockets(spec);
                ValidateSlots(spec);

                bool isSpecial =
                    (spec.Roles & RoomRole.ShopCorridor) != 0
                    || (spec.Roles & RoomRole.SupplyCorridor) != 0
                    || (spec.Roles & RoomRole.RecordRoom) != 0
                    || (spec.Roles & RoomRole.MaruStatue) != 0;
                if (!isSpecial)
                {
                    small += spec.Archetype == RoomArchetype.Small ? 1 : 0;
                    standard +=
                        spec.Archetype == RoomArchetype.Standard ? 1 : 0;
                    wide += spec.Archetype == RoomArchetype.Wide ? 1 : 0;
                    tall += spec.Archetype == RoomArchetype.Tall ? 1 : 0;
                    large += spec.Archetype == RoomArchetype.Large ? 1 : 0;
                }

                shop += (spec.Roles & RoomRole.ShopCorridor) != 0 ? 1 : 0;
                supply +=
                    (spec.Roles & RoomRole.SupplyCorridor) != 0 ? 1 : 0;
                record += (spec.Roles & RoomRole.RecordRoom) != 0 ? 1 : 0;
                maru += (spec.Roles & RoomRole.MaruStatue) != 0 ? 1 : 0;
            }

            List<string> shortfalls = new List<string>();
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.MicroStandardRoom,
                small + standard);
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.WideRoom,
                wide);
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.TallRoom,
                tall);
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.LargeRoom,
                large);
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.RestCorridorRoom,
                shop + supply);
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.RecordRoom,
                record);

            // 특수 역할은 종류마다 최소 1종이 있어야 P6가 예약 배치를 할 수 있다.
            RequireMinimum(shortfalls, "shop corridor", shop, 1);
            RequireMinimum(shortfalls, "supply corridor", supply, 1);
            RequireMinimum(shortfalls, "Maru statue", maru, 1);

            if (shortfalls.Count > 0)
            {
                throw new InvalidOperationException(
                    "P4 room category counts fall below the documented "
                    + "minimums: "
                    + string.Join(", ", shortfalls));
            }
        }

        private static void RequireQuotaMinimum(
            List<string> shortfalls,
            RoomContentQuotaCategory category,
            int actual)
        {
            if (!RoomContentQuota.TryGetRule(
                    category,
                    out RoomContentQuotaRule rule)
                || !rule.LibraryHardRequirement)
            {
                return;
            }

            RequireMinimum(shortfalls, rule.DisplayName, actual, rule.Minimum);
        }

        private static void RequireMinimum(
            List<string> shortfalls,
            string label,
            int actual,
            int minimum)
        {
            if (actual < minimum)
            {
                shortfalls.Add($"{label} {actual}/{minimum}");
            }
        }

        private static void ValidateSockets(P4RoomBuildSpec spec)
        {
            // 문서 7.1의 출구 2개 조건 때문에 귀향상 방은 단일 접속 리프에서 제외한다.
            bool isSingleAccessLeaf =
                (spec.Roles & RoomRole.RecordRoom) != 0
                && (spec.Roles & RoomRole.MaruStatue) == 0;
            int expectedCount = isSingleAccessLeaf ? 1 : 2;
            if (spec.Sockets.Length != expectedCount)
            {
                throw new InvalidOperationException(
                    $"{spec.Id} requires {expectedCount} external sockets.");
            }

            for (int index = 0; index < spec.Sockets.Length; index++)
            {
                P4SocketBuildSpec socket = spec.Sockets[index];
                bool horizontal =
                    socket.Side == RoomSocketSide.Left
                    || socket.Side == RoomSocketSide.Right;
                if (!horizontal
                    || socket.CellOffset.y != 1
                    || socket.ValidationAnchor != socket.CellOffset
                    || socket.OpeningSize != Vector2Int.one
                    || socket.TraversalType != RoomTraversalType.Walk
                    || !socket.MainRouteAllowed)
                {
                    throw new InvalidOperationException(
                        $"{spec.Id}/{socket.Id} violates the P4 shared Walk socket lane.");
                }
            }
        }

        private static void ValidateSlots(P4RoomBuildSpec spec)
        {
            bool hasSafeCell = false;
            bool hasMaruEntry = false;
            bool hasReward = false;
            bool hasCore = false;
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < spec.Slots.Length; index++)
            {
                P4SlotBuildSpec slot = spec.Slots[index];
                if (!ids.Add(slot.Id))
                {
                    throw new InvalidOperationException(
                        $"{spec.Id} contains duplicate slot id {slot.Id}.");
                }

                hasSafeCell |= slot.Kind == RoomContentSlotKind.SafeCell;
                hasMaruEntry |= slot.Kind == RoomContentSlotKind.MaruEntry;
                hasReward |= slot.Kind == RoomContentSlotKind.OptionalReward
                    || slot.Kind == RoomContentSlotKind.Treasure;
                hasCore |=
                    ((spec.Roles & RoomRole.RecordRoom) != 0
                        && slot.Kind == RoomContentSlotKind.RecordGuest)
                    || ((spec.Roles & RoomRole.MaruStatue) != 0
                        && slot.Kind == RoomContentSlotKind.MaruStatue);
            }

            if (!hasSafeCell || !hasMaruEntry)
            {
                throw new InvalidOperationException(
                    $"{spec.Id} requires SafeCell and MaruEntry slots.");
            }

            if (hasReward != spec.RewardVisible)
            {
                throw new InvalidOperationException(
                    $"{spec.Id} reward visibility does not match its reward slots.");
            }

            bool isLeaf =
                (spec.Roles & RoomRole.RecordRoom) != 0
                || (spec.Roles & RoomRole.MaruStatue) != 0;
            if (isLeaf && !hasCore)
            {
                throw new InvalidOperationException(
                    $"{spec.Id} requires a standable leaf-room core slot.");
            }
        }
    }
}

#endif
