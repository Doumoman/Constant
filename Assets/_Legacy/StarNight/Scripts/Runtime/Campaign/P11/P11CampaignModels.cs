#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Generation.P6;
using StarNight.Maru.P8;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    public enum P11StageId
    {
        None = 0,
        StarPostOffice31 = 301,
        StarPostOffice32 = 302,
        StarPostOffice33 = 303,
        SunriseGarden41 = 401,
        SunriseGarden42 = 402,
        SunriseGarden43 = 403,
        PolarisObservatory51 = 501,
        PolarisObservatory52 = 502,
        PolarisObservatory53 = 503
    }

    public enum P11CampaignPhase
    {
        WaitingForCommonRegion = 0,
        StarPostOffice = 1,
        SunriseGarden = 2,
        PolarisObservatory = 3,
        EndingChoice = 4,
        Ended = 5
    }

    public enum P11BossKind
    {
        None = 0,
        Popo = 1,
        SunFlower = 2,
        Maru = 3
    }

    public enum P11EndingKind
    {
        None = 0,
        Normal = 1,
        EnvironmentalDisarm = 2,
        Memory = 3
    }

    public enum P11BossPhase
    {
        Idle = 0,
        SafeDemonstration = 1,
        Active = 2,
        Vulnerable = 3,
        Defeated = 4
    }

    public enum P11BossSolutionInput
    {
        BasicWeakPoint = 0,
        Bomb = 1,
        Pickaxe = 2,
        Pestle = 3,
        Grapple = 4,
        Water = 5,
        ParcelReflection = 6,
        ReturnStamp = 7,
        CrossedLightAndShadow = 8,
        GrowingVine = 9
    }

    public enum P11TraversalAxis
    {
        Horizontal = 0,
        Vertical = 1,
        Circuit = 2
    }

    [Flags]
    public enum P11StageMechanics
    {
        None = 0,
        ParcelConveyor = 1 << 0,
        SortingArm = 1 << 1,
        MailTube = 1 << 2,
        ReturnStamp = 1 << 3,
        InkLabel = 1 << 4,
        ParcelLauncher = 1 << 5,
        RotatingSunRay = 1 << 6,
        ShadowSeed = 1 << 7,
        SunFlowerPlatform = 1 << 8,
        GrowingVine = 1 << 9,
        OverheatedPlatform = 1 << 10,
        DewBubble = 1 << 11,
        ReturnField = 1 << 12,
        InvariantStarBlock = 1 << 13,
        OrbitPlatform = 1 << 14,
        GravityDial = 1 << 15,
        ConstellationBridge = 1 << 16,
        MemoryBell = 1 << 17,
        BossArena = 1 << 18
    }

    [Flags]
    public enum P11StageGuarantees
    {
        None = 0,
        Landmark = 1 << 0,
        ClosedLoop = 1 << 1,
        MomoShop = 1 << 2,
        HomecomingStatue = 1 << 3,
        StarArchive = 1 << 4,
        SupplyCorridor = 1 << 5,
        LocalEvent = 1 << 6,
        Boss = 1 << 7,
        LetterStateDisplay = 1 << 8,
        SunEmberStateDisplay = 1 << 9,
        DawnMapStateDisplay = 1 << 10,
        MaruStoryTableau = 1 << 11
    }

    [Serializable]
    public sealed class P11StageDefinition
    {
        [SerializeField] private P11StageId stageId;
        [SerializeField] private string displayName;
        [SerializeField] private RoomRegion region;
        [SerializeField] private P6StageSlot stageSlot;
        [SerializeField] private P6StageArchetype archetype;
        [SerializeField] private P11TraversalAxis traversalAxis;
        [SerializeField] private P11StageMechanics mechanics;
        [SerializeField] private P11StageGuarantees guarantees;
        [SerializeField] private P11BossKind boss;
        [SerializeField] private string coreActionSentence;
        [SerializeField, Min(0)] private int recommendedMinutesMin;
        [SerializeField, Min(0)] private int recommendedMinutesMax;
        [SerializeField] private bool mainPathToolFree = true;
        [SerializeField] private bool optionalStoryNeverGatesExit = true;

        public P11StageId StageId => stageId;
        public string DisplayName => displayName;
        public RoomRegion Region => region;
        public P6StageSlot StageSlot => stageSlot;
        public P6StageArchetype Archetype => archetype;
        public P11TraversalAxis TraversalAxis => traversalAxis;
        public P11StageMechanics Mechanics => mechanics;
        public P11StageGuarantees Guarantees => guarantees;
        public P11BossKind Boss => boss;
        public string CoreActionSentence => coreActionSentence;
        public int RecommendedMinutesMin => recommendedMinutesMin;
        public int RecommendedMinutesMax => recommendedMinutesMax;
        public bool MainPathToolFree => mainPathToolFree;
        public bool OptionalStoryNeverGatesExit =>
            optionalStoryNeverGatesExit;
        public bool IsBossStage => boss != P11BossKind.None;

        public void Configure(
            P11StageId id,
            string stageName,
            RoomRegion stageRegion,
            P6StageSlot slot,
            P6StageArchetype stageArchetype,
            P11TraversalAxis axis,
            P11StageMechanics stageMechanics,
            P11StageGuarantees stageGuarantees,
            P11BossKind bossKind,
            string oneSentenceCoreAction,
            int minutesMin,
            int minutesMax,
            bool toolFreeMainPath = true,
            bool storyNeverGatesExit = true)
        {
            stageId = id;
            displayName = stageName ?? string.Empty;
            region = stageRegion;
            stageSlot = slot;
            archetype = stageArchetype;
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
            optionalStoryNeverGatesExit = storyNeverGatesExit;
        }
    }

    public sealed class P11CampaignRouteProof
    {
        private static readonly P11StageId[] RequiredRoute =
        {
            P11StageId.StarPostOffice31,
            P11StageId.StarPostOffice32,
            P11StageId.StarPostOffice33,
            P11StageId.SunriseGarden41,
            P11StageId.SunriseGarden42,
            P11StageId.SunriseGarden43,
            P11StageId.PolarisObservatory51,
            P11StageId.PolarisObservatory52,
            P11StageId.PolarisObservatory53
        };

        public P11CampaignRouteProof(
            bool allStagesPresent,
            bool regionTripletsValid,
            bool stageRhythmValid,
            bool mainPathToolFree,
            bool optionalStoryNeverGatesExit,
            bool endingsReachable)
        {
            AllStagesPresent = allStagesPresent;
            RegionTripletsValid = regionTripletsValid;
            StageRhythmValid = stageRhythmValid;
            MainPathToolFree = mainPathToolFree;
            OptionalStoryNeverGatesExit =
                optionalStoryNeverGatesExit;
            EndingsReachable = endingsReachable;
        }

        public bool AllStagesPresent { get; }
        public bool RegionTripletsValid { get; }
        public bool StageRhythmValid { get; }
        public bool MainPathToolFree { get; }
        public bool OptionalStoryNeverGatesExit { get; }
        public bool EndingsReachable { get; }
        public bool Passed =>
            AllStagesPresent
            && RegionTripletsValid
            && StageRhythmValid
            && MainPathToolFree
            && OptionalStoryNeverGatesExit
            && EndingsReachable;

        public static P11CampaignRouteProof Evaluate(
            P11CampaignCatalog catalog)
        {
            if (catalog == null)
            {
                return new P11CampaignRouteProof(
                    false, false, false, false, false, false);
            }

            bool allPresent = true;
            bool rhythm = true;
            bool toolFree = true;
            bool optional = true;
            var regionCounts = new Dictionary<RoomRegion, int>();
            for (int index = 0; index < RequiredRoute.Length; index++)
            {
                P11StageDefinition stage =
                    catalog.Find(RequiredRoute[index]);
                if (stage == null)
                {
                    allPresent = false;
                    continue;
                }

                if (!regionCounts.ContainsKey(stage.Region))
                {
                    regionCounts[stage.Region] = 0;
                }

                regionCounts[stage.Region]++;
                int expectedSlot = index % 3 + 1;
                rhythm &= (int)stage.StageSlot == expectedSlot;
                toolFree &= stage.MainPathToolFree;
                optional &= stage.OptionalStoryNeverGatesExit;
            }

            bool triplets =
                Count(regionCounts, RoomRegion.StarPostOffice) == 3
                && Count(regionCounts, RoomRegion.SunriseGarden) == 3
                && Count(regionCounts, RoomRegion.PolarisObservatory) == 3;
            P11StageDefinition finalStage =
                catalog.Find(P11StageId.PolarisObservatory53);
            bool endings = finalStage != null
                && finalStage.Boss == P11BossKind.Maru;
            return new P11CampaignRouteProof(
                allPresent,
                triplets,
                rhythm,
                toolFree,
                optional,
                endings);
        }

        private static int Count(
            IReadOnlyDictionary<RoomRegion, int> counts,
            RoomRegion region)
        {
            return counts.TryGetValue(region, out int value)
                ? value
                : 0;
        }
    }

    public sealed class P11BellPattern
    {
        private static readonly P8BellSignal[] NaraePattern =
        {
            P8BellSignal.Short,
            P8BellSignal.Short,
            P8BellSignal.Long
        };

        private int progress;

        public int Progress => progress;
        public int Length => NaraePattern.Length;
        public bool Completed => progress >= NaraePattern.Length;
        public IReadOnlyList<P8BellSignal> Pattern => NaraePattern;

        public bool Push(P8BellSignal signal)
        {
            if (Completed)
            {
                return true;
            }

            if (signal == NaraePattern[progress])
            {
                progress++;
                return Completed;
            }

            progress = signal == NaraePattern[0] ? 1 : 0;
            return false;
        }

        public void Reset()
        {
            progress = 0;
        }
    }
}

#endif
