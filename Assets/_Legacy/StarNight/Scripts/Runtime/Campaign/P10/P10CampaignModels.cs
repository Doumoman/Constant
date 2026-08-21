#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Folklore.P9;
using StarNight.Generation.P6;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    public enum P10StageId
    {
        None = 0,
        MoonPalace11 = 11,
        MoonPalace12 = 12,
        MoonPalace13 = 13,
        MagpieBridge21 = 21,
        MagpieBridge22 = 22,
        MagpieBridge23 = 23,
        DragonPalace21 = 31,
        DragonPalace22 = 32,
        DragonPalace23 = 33
    }

    public enum P10CampaignPhase
    {
        MoonPalace = 0,
        BranchChoice = 1,
        FirstBranch = 2,
        CrossRouteOffered = 3,
        SecondBranch = 4,
        CommonRegionReady = 5,
        DepartedToCommonRegion = 6
    }

    public enum P10TraversalAxis
    {
        Mixed = 0,
        Horizontal = 1,
        Vertical = 2
    }

    public enum P10BossKind
    {
        None = 0,
        Kungtteoki = 1,
        KnotSpider = 2,
        DragonGatekeeper = 3
    }

    public enum P10BossSolutionInput
    {
        BasicWeakPoint = 0,
        Bomb = 1,
        Pickaxe = 2,
        Pestle = 3,
        MoonCake = 4,
        Hook = 5,
        Environment = 6
    }

    public enum P10BranchEventKind
    {
        RepairMagpieNest = 0,
        RestoreCarpWaterway = 1
    }

    public enum P10RoutePortalKind
    {
        FirstBranchChoice = 0,
        CrossRoute = 1,
        CommonRegion = 2
    }

    public enum P10BossPhase
    {
        Idle = 0,
        SafeDemonstration = 1,
        Active = 2,
        Vulnerable = 3,
        Defeated = 4
    }

    [Flags]
    public enum P10StageMechanics
    {
        None = 0,
        SoftMoonSoil = 1 << 0,
        RollingMoonCake = 1 << 1,
        MortarStakeRoots = 1 << 2,
        BombPickaxePestleContrast = 1 << 3,
        HorizontalLongRooms = 1 << 4,
        ReturnableLowerRoute = 1 << 5,
        RopeHookUmbrella = 1 << 6,
        TemporaryMagpiePlatforms = 1 << 7,
        WindAndSwayingBridge = 1 << 8,
        VerticalWaterTank = 1 << 9,
        CellWaterCurrent = 1 << 10,
        FloodgateAndDrain = 1 << 11,
        ShovelBombHook = 1 << 12,
        CurrentTraversal = 1 << 13,
        BossArena = 1 << 14
    }

    [Flags]
    public enum P10StageGuarantees
    {
        None = 0,
        LargeJunction = 1 << 0,
        ClosedLoop = 1 << 1,
        MomoShop = 1 << 2,
        HomecomingStatue = 1 << 3,
        StarArchive = 1 << 4,
        SupplyCorridor = 1 << 5,
        Boss = 1 << 6,
        TwoBranchExits = 1 << 7,
        BranchRelic = 1 << 8
    }

    [Serializable]
    public sealed class P10StageDefinition
    {
        [SerializeField] private P10StageId stageId;
        [SerializeField] private string displayName;
        [SerializeField] private RoomRegion region;
        [SerializeField] private P6StageSlot stageSlot;
        [SerializeField] private P6StageArchetype archetype;
        [SerializeField] private P9BranchKind branch;
        [SerializeField] private P10TraversalAxis traversalAxis;
        [SerializeField] private P10StageMechanics mechanics;
        [SerializeField] private P10StageGuarantees guarantees;
        [SerializeField] private P10BossKind boss;
        [SerializeField] private string coreActionSentence;
        [SerializeField, Min(0)] private int recommendedMinutesMin;
        [SerializeField, Min(0)] private int recommendedMinutesMax;
        [SerializeField] private bool mainPathToolFree = true;
        [SerializeField] private bool optionalEventsNeverGateExit = true;

        public P10StageId StageId => stageId;
        public string DisplayName => displayName;
        public RoomRegion Region => region;
        public P6StageSlot StageSlot => stageSlot;
        public P6StageArchetype Archetype => archetype;
        public P9BranchKind Branch => branch;
        public P10TraversalAxis TraversalAxis => traversalAxis;
        public P10StageMechanics Mechanics => mechanics;
        public P10StageGuarantees Guarantees => guarantees;
        public P10BossKind Boss => boss;
        public string CoreActionSentence => coreActionSentence;
        public int RecommendedMinutesMin => recommendedMinutesMin;
        public int RecommendedMinutesMax => recommendedMinutesMax;
        public bool MainPathToolFree => mainPathToolFree;
        public bool OptionalEventsNeverGateExit =>
            optionalEventsNeverGateExit;
        public bool IsBossStage => boss != P10BossKind.None;

        public void Configure(
            P10StageId id,
            string stageName,
            RoomRegion stageRegion,
            P6StageSlot slot,
            P6StageArchetype stageArchetype,
            P9BranchKind stageBranch,
            P10TraversalAxis axis,
            P10StageMechanics stageMechanics,
            P10StageGuarantees stageGuarantees,
            P10BossKind bossKind,
            string oneSentenceCoreAction,
            int minutesMin,
            int minutesMax,
            bool toolFreeMainPath = true,
            bool optionalEventsDoNotGate = true)
        {
            stageId = id;
            displayName = stageName ?? string.Empty;
            region = stageRegion;
            stageSlot = slot;
            archetype = stageArchetype;
            branch = stageBranch;
            traversalAxis = axis;
            mechanics = stageMechanics;
            guarantees = stageGuarantees;
            boss = bossKind;
            coreActionSentence = oneSentenceCoreAction ?? string.Empty;
            recommendedMinutesMin = Mathf.Max(0, minutesMin);
            recommendedMinutesMax = Mathf.Max(
                recommendedMinutesMin,
                minutesMax);
            mainPathToolFree = toolFreeMainPath;
            optionalEventsNeverGateExit = optionalEventsDoNotGate;
        }
    }

    [Serializable]
    public sealed class P10BranchFeelDefinition
    {
        [SerializeField] private P9BranchKind branch;
        [SerializeField] private RoomRegion region;
        [SerializeField] private P10TraversalAxis primaryAxis;
        [SerializeField] private P10StageMechanics signatureMechanics;
        [SerializeField, Min(0)] private int wideRoomWeight;
        [SerializeField, Min(0)] private int tallRoomWeight;
        [SerializeField, Min(0)] private int largeRoomWeight;
        [SerializeField] private string feelSentence;

        public P9BranchKind Branch => branch;
        public RoomRegion Region => region;
        public P10TraversalAxis PrimaryAxis => primaryAxis;
        public P10StageMechanics SignatureMechanics =>
            signatureMechanics;
        public int WideRoomWeight => wideRoomWeight;
        public int TallRoomWeight => tallRoomWeight;
        public int LargeRoomWeight => largeRoomWeight;
        public string FeelSentence => feelSentence;

        public void Configure(
            P9BranchKind branchKind,
            RoomRegion branchRegion,
            P10TraversalAxis axis,
            P10StageMechanics mechanics,
            int wideWeight,
            int tallWeight,
            int largeWeight,
            string oneSentenceFeel)
        {
            branch = branchKind;
            region = branchRegion;
            primaryAxis = axis;
            signatureMechanics = mechanics;
            wideRoomWeight = Mathf.Max(0, wideWeight);
            tallRoomWeight = Mathf.Max(0, tallWeight);
            largeRoomWeight = Mathf.Max(0, largeWeight);
            feelSentence = oneSentenceFeel ?? string.Empty;
        }

        public bool IsDistinctFrom(P10BranchFeelDefinition other)
        {
            return other != null
                && branch != other.branch
                && primaryAxis != other.primaryAxis
                && (signatureMechanics & other.signatureMechanics)
                    == P10StageMechanics.None;
        }
    }

    public sealed class P10CampaignRouteProof
    {
        private static readonly P10StageId[] NormalMagpieRoute =
        {
            P10StageId.MoonPalace11,
            P10StageId.MoonPalace12,
            P10StageId.MoonPalace13,
            P10StageId.MagpieBridge21,
            P10StageId.MagpieBridge22,
            P10StageId.MagpieBridge23
        };

        private static readonly P10StageId[] NormalDragonRoute =
        {
            P10StageId.MoonPalace11,
            P10StageId.MoonPalace12,
            P10StageId.MoonPalace13,
            P10StageId.DragonPalace21,
            P10StageId.DragonPalace22,
            P10StageId.DragonPalace23
        };

        private static readonly P10StageId[] MagpieToDragonCrossRoute =
        {
            P10StageId.MoonPalace11,
            P10StageId.MoonPalace12,
            P10StageId.MoonPalace13,
            P10StageId.MagpieBridge21,
            P10StageId.MagpieBridge22,
            P10StageId.MagpieBridge23,
            P10StageId.DragonPalace22,
            P10StageId.DragonPalace23
        };

        private static readonly P10StageId[] DragonToMagpieCrossRoute =
        {
            P10StageId.MoonPalace11,
            P10StageId.MoonPalace12,
            P10StageId.MoonPalace13,
            P10StageId.DragonPalace21,
            P10StageId.DragonPalace22,
            P10StageId.DragonPalace23,
            P10StageId.MagpieBridge22,
            P10StageId.MagpieBridge23
        };

        public P10CampaignRouteProof(
            bool normalMagpieValid,
            bool normalDragonValid,
            bool magpieToDragonValid,
            bool dragonToMagpieValid,
            bool branchFeelDistinct)
        {
            NormalMagpieValid = normalMagpieValid;
            NormalDragonValid = normalDragonValid;
            MagpieToDragonValid = magpieToDragonValid;
            DragonToMagpieValid = dragonToMagpieValid;
            BranchFeelDistinct = branchFeelDistinct;
        }

        public bool NormalMagpieValid { get; }
        public bool NormalDragonValid { get; }
        public bool MagpieToDragonValid { get; }
        public bool DragonToMagpieValid { get; }
        public bool BranchFeelDistinct { get; }
        public bool SingleBranchCommonEntryValid =>
            NormalMagpieValid && NormalDragonValid;
        public bool CrossRoutesSkipOppositeX1 =>
            MagpieToDragonCrossRoute[
                MagpieToDragonCrossRoute.Length - 2]
                == P10StageId.DragonPalace22
            && DragonToMagpieCrossRoute[
                DragonToMagpieCrossRoute.Length - 2]
                == P10StageId.MagpieBridge22;
        public bool Passed =>
            SingleBranchCommonEntryValid
            && MagpieToDragonValid
            && DragonToMagpieValid
            && CrossRoutesSkipOppositeX1
            && BranchFeelDistinct;

        public static P10CampaignRouteProof Evaluate(
            P10CampaignCatalog catalog)
        {
            if (catalog == null)
            {
                return new P10CampaignRouteProof(
                    false,
                    false,
                    false,
                    false,
                    false);
            }

            return new P10CampaignRouteProof(
                ContainsAll(catalog, NormalMagpieRoute),
                ContainsAll(catalog, NormalDragonRoute),
                ContainsAll(catalog, MagpieToDragonCrossRoute),
                ContainsAll(catalog, DragonToMagpieCrossRoute),
                catalog.BranchesAreMechanicallyDistinct);
        }

        private static bool ContainsAll(
            P10CampaignCatalog catalog,
            IReadOnlyList<P10StageId> route)
        {
            for (int index = 0; index < route.Count; index++)
            {
                if (catalog.Find(route[index]) == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

#endif
