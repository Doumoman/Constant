#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Generation.P6;
using StarNight.Maru.P8;
using StarNight.Population.P7;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    public enum P12StageId
    {
        None = 0,
        StarlessSea01 = 701,
        StarlessSea02 = 702,
        StarlessSea03 = 703,
        StarlessSea04 = 704,
        StarlessSea05 = 705,
        StarlessSea06 = 706,
        StarlessSea07 = 707,
        StarlessSea08 = 708,
        StarlessSea09 = 709,
        StarlessSea10 = 710,
        StarlessSea11 = 711,
        StarlessSea12 = 712
    }

    public enum P12ChallengeSegment
    {
        None = 0,
        FirstSea = 1,
        SecondSea = 2,
        ThirdSea = 3,
        DawnPassage = 4
    }

    public enum P12ChallengePhase
    {
        WaitingForEntry = 0,
        FirstSea = 1,
        SecondSea = 2,
        ThirdSea = 3,
        DawnPassage = 4,
        Completed = 5,
        Ended = 6
    }

    public enum P12ChallengeOutcome
    {
        None = 0,
        Completed = 1,
        EndedBySecondFailure = 2
    }

    public enum P12BossEchoKind
    {
        None = 0,
        Kungtteoki = 1,
        FloodgateKeeper = 2,
        Popo = 3
    }

    public enum P12FailureConsequence
    {
        RestartSegment = 0,
        EndChallenge = 1
    }

    [Flags]
    public enum P12StageMechanics
    {
        None = 0,
        Crosswind = 1 << 0,
        Riptide = 1 << 1,
        SwayingPlatform = 1 << 2,
        TimedPlatform = 1 << 3,
        Floodgate = 1 << 4,
        FallingObject = 1 << 5,
        ParcelConveyor = 1 << 6,
        ParcelLauncher = 1 << 7,
        ReturnStamp = 1 << 8,
        ReturnField = 1 << 9,
        RotatingSunRay = 1 << 10,
        GrowingVine = 1 << 11,
        OverheatedPlatform = 1 << 12,
        OrbitPlatform = 1 << 13,
        GravityDial = 1 << 14,
        ConstellationBridge = 1 << 15,
        InvariantStarBlock = 1 << 16,
        HomecomingStatueEconomy = 1 << 17
    }

    [Serializable]
    public sealed class P12StageDefinition
    {
        [SerializeField] private P12StageId stageId;
        [SerializeField] private string displayName;
        [SerializeField] private P12ChallengeSegment segment;
        [SerializeField] private P6StageArchetype archetype;
        [SerializeField] private List<RoomRegion> sourceRegions =
            new List<RoomRegion>();
        [SerializeField] private P12StageMechanics mechanics;
        [SerializeField, Range(0, 3)] private int hazardCount;
        [SerializeField, Range(1, 2)] private int ruleFamiliesPerRoomMax = 1;
        [SerializeField, Min(1)] private int roomCountMin = 1;
        [SerializeField, Min(1)] private int roomCountMax = 1;
        [SerializeField] private bool mercyCorridor;
        [SerializeField] private bool earlyMaru;
        [SerializeField] private P12BossEchoKind bossEcho;
        [SerializeField] private bool isFinalArrival;
        [SerializeField] private bool firstPairingSafeDemonstration;
        [SerializeField] private bool introducesNewMechanic;
        [SerializeField] private string coreActionSentence;

        public P12StageId StageId => stageId;
        public string DisplayName => displayName;
        public P12ChallengeSegment Segment => segment;
        public P6StageArchetype Archetype => archetype;
        public IReadOnlyList<RoomRegion> SourceRegions => sourceRegions;
        public P12StageMechanics Mechanics => mechanics;
        public int HazardCount => hazardCount;
        public int RuleFamiliesPerRoomMax => ruleFamiliesPerRoomMax;
        public int RoomCountMin => roomCountMin;
        public int RoomCountMax => roomCountMax;
        public bool MercyCorridor => mercyCorridor;
        public bool EarlyMaru => earlyMaru;
        public P12BossEchoKind BossEcho => bossEcho;
        public bool IsFinalArrival => isFinalArrival;
        public bool FirstPairingSafeDemonstration =>
            firstPairingSafeDemonstration;
        public bool IntroducesNewMechanic => introducesNewMechanic;
        public string CoreActionSentence => coreActionSentence;
        public bool IsBossEchoStage => bossEcho != P12BossEchoKind.None;

        public void Configure(
            P12StageId id,
            string stageName,
            P12ChallengeSegment stageSegment,
            P6StageArchetype stageArchetype,
            IReadOnlyList<RoomRegion> regions,
            P12StageMechanics stageMechanics,
            int hazards,
            int ruleFamiliesMax,
            int roomsMin,
            int roomsMax,
            bool hasMercyCorridor,
            bool hasEarlyMaru,
            P12BossEchoKind echo,
            bool finalArrival,
            bool safeDemonstrationForFirstPairing,
            string oneSentenceCoreAction,
            bool introducesNew = false)
        {
            stageId = id;
            displayName = stageName ?? string.Empty;
            segment = stageSegment;
            archetype = stageArchetype;
            sourceRegions = regions != null
                ? new List<RoomRegion>(regions)
                : new List<RoomRegion>();
            mechanics = stageMechanics;
            hazardCount = Mathf.Clamp(hazards, 0, 3);
            ruleFamiliesPerRoomMax = Mathf.Clamp(ruleFamiliesMax, 1, 2);
            roomCountMin = Mathf.Max(1, roomsMin);
            roomCountMax = Mathf.Max(roomCountMin, roomsMax);
            mercyCorridor = hasMercyCorridor;
            earlyMaru = hasEarlyMaru;
            bossEcho = echo;
            isFinalArrival = finalArrival;
            firstPairingSafeDemonstration = safeDemonstrationForFirstPairing;
            introducesNewMechanic = introducesNew;
            coreActionSentence = oneSentenceCoreAction ?? string.Empty;
        }
    }

    public sealed class P12ChallengeRouteProof
    {
        private static readonly P12StageId[] RequiredRoute =
        {
            P12StageId.StarlessSea01,
            P12StageId.StarlessSea02,
            P12StageId.StarlessSea03,
            P12StageId.StarlessSea04,
            P12StageId.StarlessSea05,
            P12StageId.StarlessSea06,
            P12StageId.StarlessSea07,
            P12StageId.StarlessSea08,
            P12StageId.StarlessSea09,
            P12StageId.StarlessSea10,
            P12StageId.StarlessSea11,
            P12StageId.StarlessSea12
        };

        public P12ChallengeRouteProof(
            bool allStagesPresent,
            bool segmentQuotasValid,
            bool roomCountRangesValid,
            bool mercyCadenceValid,
            bool earlyMaruPlacementValid,
            bool bossEchoPlacementValid,
            bool finalArrivalValid,
            bool noNewMechanics,
            bool hazardBudgetsValid,
            bool sourceRegionCountsValid,
            bool mechanicsKnownValid,
            bool coreActionSentencesValid,
            bool approvedPairingsValid = true,
            bool safeDemonstrationValid = true)
        {
            AllStagesPresent = allStagesPresent;
            SegmentQuotasValid = segmentQuotasValid;
            RoomCountRangesValid = roomCountRangesValid;
            MercyCadenceValid = mercyCadenceValid;
            EarlyMaruPlacementValid = earlyMaruPlacementValid;
            BossEchoPlacementValid = bossEchoPlacementValid;
            FinalArrivalValid = finalArrivalValid;
            NoNewMechanics = noNewMechanics;
            HazardBudgetsValid = hazardBudgetsValid;
            SourceRegionCountsValid = sourceRegionCountsValid;
            MechanicsKnownValid = mechanicsKnownValid;
            CoreActionSentencesValid = coreActionSentencesValid;
            ApprovedPairingsValid = approvedPairingsValid;
            SafeDemonstrationValid = safeDemonstrationValid;
        }

        public bool AllStagesPresent { get; }
        public bool SegmentQuotasValid { get; }
        public bool RoomCountRangesValid { get; }
        public bool MercyCadenceValid { get; }
        public bool EarlyMaruPlacementValid { get; }
        public bool BossEchoPlacementValid { get; }
        public bool FinalArrivalValid { get; }
        public bool NoNewMechanics { get; }
        public bool HazardBudgetsValid { get; }
        public bool SourceRegionCountsValid { get; }
        public bool MechanicsKnownValid { get; }
        public bool CoreActionSentencesValid { get; }
        public bool ApprovedPairingsValid { get; }
        public bool SafeDemonstrationValid { get; }
        public bool Passed =>
            AllStagesPresent
            && SegmentQuotasValid
            && RoomCountRangesValid
            && MercyCadenceValid
            && EarlyMaruPlacementValid
            && BossEchoPlacementValid
            && FinalArrivalValid
            && NoNewMechanics
            && HazardBudgetsValid
            && SourceRegionCountsValid
            && MechanicsKnownValid
            && CoreActionSentencesValid
            && ApprovedPairingsValid
            && SafeDemonstrationValid;

        public static P12ChallengeRouteProof Evaluate(
            P12ChallengeCatalog catalog)
        {
            if (catalog == null)
            {
                return new P12ChallengeRouteProof(
                    false, false, false, false, false, false,
                    false, false, false, false, false, false,
                    false, false);
            }

            bool allPresent = true;
            bool segments = true;
            bool roomRanges = true;
            bool mercy = true;
            bool earlyMaru = true;
            bool bossEcho = true;
            bool finalArrival = true;
            bool noNew = true;
            bool hazards = true;
            bool regions = true;
            bool known = true;
            bool sentences = true;
            bool pairings = true;
            bool safeDemos = true;
            var seenPairings = new HashSet<int>();
            var pairKeys = new List<int>();
            for (int index = 0; index < RequiredRoute.Length; index++)
            {
                P12StageDefinition stage =
                    catalog.Find(RequiredRoute[index]);
                if (stage == null)
                {
                    allPresent = false;
                    continue;
                }

                var expectedSegment =
                    (P12ChallengeSegment)(index / 3 + 1);
                segments &= stage.Segment == expectedSegment;
                roomRanges &= RoomRangeValid(stage, expectedSegment);
                if ((index + 1) % P12ComboRules.MercyEveryStages == 0)
                {
                    mercy &= stage.MercyCorridor;
                }

                earlyMaru &= !stage.EarlyMaru
                    || expectedSegment >= P12ChallengeSegment.ThirdSea;
                bossEcho &= stage.BossEcho == P12BossEchoKind.None
                    || expectedSegment == P12ChallengeSegment.DawnPassage;
                finalArrival &= stage.IsFinalArrival
                    == (RequiredRoute[index] == P12StageId.StarlessSea12);
                noNew &= !stage.IntroducesNewMechanic;
                hazards &= stage.HazardCount >= 0
                    && stage.HazardCount <= P12ComboRules.MaxHazardsPerRoom
                    && stage.RuleFamiliesPerRoomMax >= 1
                    && stage.RuleFamiliesPerRoomMax
                        <= P12ComboRules.MaxRuleFamiliesPerRoom;
                regions &= RegionCountValid(stage, expectedSegment);
                known &= (stage.Mechanics
                    & ~P12ComboRules.AllKnownMechanics)
                    == P12StageMechanics.None;
                sentences &= !string.IsNullOrWhiteSpace(
                    stage.CoreActionSentence);
                pairings &= !P12ComboRules.TryFindUnapprovedPairing(
                    stage.Mechanics,
                    out _,
                    out _);
                pairKeys.Clear();
                P12ComboRules.CollectPairingKeys(
                    stage.Mechanics,
                    pairKeys);
                bool hasFirstPairing = false;
                for (int key = 0; key < pairKeys.Count; key++)
                {
                    hasFirstPairing |=
                        seenPairings.Add(pairKeys[key]);
                }

                if (hasFirstPairing
                    && P12ComboRules
                        .RequiresSafeDemonstrationForFirstPairing)
                {
                    safeDemos &= stage.FirstPairingSafeDemonstration;
                }
            }

            P12StageDefinition finalStage =
                catalog.Find(P12StageId.StarlessSea12);
            finalArrival &= finalStage != null
                && finalStage.IsFinalArrival
                && finalStage.BossEcho == P12BossEchoKind.None;
            return new P12ChallengeRouteProof(
                allPresent,
                segments,
                roomRanges,
                mercy,
                earlyMaru,
                bossEcho,
                finalArrival,
                noNew,
                hazards,
                regions,
                known,
                sentences,
                pairings,
                safeDemos);
        }

        private static bool RoomRangeValid(
            P12StageDefinition stage,
            P12ChallengeSegment segment)
        {
            if (stage.RoomCountMin > stage.RoomCountMax)
            {
                return false;
            }

            switch (segment)
            {
                case P12ChallengeSegment.FirstSea:
                    return stage.RoomCountMin >= 9
                        && stage.RoomCountMax <= 11;
                case P12ChallengeSegment.SecondSea:
                    return stage.RoomCountMin >= 10
                        && stage.RoomCountMax <= 13;
                case P12ChallengeSegment.ThirdSea:
                    return stage.RoomCountMin >= 12
                        && stage.RoomCountMax <= 14;
                case P12ChallengeSegment.DawnPassage:
                    return stage.RoomCountMin == stage.RoomCountMax
                        && stage.RoomCountMin >= 1;
                default:
                    return false;
            }
        }

        private static bool RegionCountValid(
            P12StageDefinition stage,
            P12ChallengeSegment segment)
        {
            int count = stage.SourceRegions != null
                ? stage.SourceRegions.Count
                : 0;
            if (count < 1)
            {
                return false;
            }

            switch (segment)
            {
                case P12ChallengeSegment.FirstSea:
                    return count == 2;
                case P12ChallengeSegment.SecondSea:
                    return count == 3;
                default:
                    return true;
            }
        }
    }

    public readonly struct P12MechanicPairing
        : IEquatable<P12MechanicPairing>
    {
        public P12MechanicPairing(
            P12StageMechanics first,
            P12StageMechanics second)
        {
            First = first;
            Second = second;
        }

        public P12StageMechanics First { get; }
        public P12StageMechanics Second { get; }
        public P12StageMechanics Combined => First | Second;

        public bool Matches(
            P12StageMechanics a,
            P12StageMechanics b)
        {
            return (First == a && Second == b)
                || (First == b && Second == a);
        }

        public bool Equals(P12MechanicPairing other)
        {
            return Matches(other.First, other.Second);
        }

        public override bool Equals(object obj)
        {
            return obj is P12MechanicPairing other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Combined;
        }
    }

    public static class P12ComboRules
    {
        public const int MaxRuleFamiliesPerRoom = 2;
        public const int MaxHazardsPerRoom = 3;
        public const int MercyEveryStages = 3;
        public const int MechanicFlagCount = 18;

        private static readonly P12MechanicPairing[] Pairings =
        {
            new P12MechanicPairing(
                P12StageMechanics.SwayingPlatform,
                P12StageMechanics.TimedPlatform),
            new P12MechanicPairing(
                P12StageMechanics.Riptide,
                P12StageMechanics.FallingObject),
            new P12MechanicPairing(
                P12StageMechanics.Crosswind,
                P12StageMechanics.ParcelLauncher),
            new P12MechanicPairing(
                P12StageMechanics.RotatingSunRay,
                P12StageMechanics.ConstellationBridge),
            new P12MechanicPairing(
                P12StageMechanics.RotatingSunRay,
                P12StageMechanics.ParcelConveyor),
            new P12MechanicPairing(
                P12StageMechanics.ConstellationBridge,
                P12StageMechanics.ParcelConveyor),
            new P12MechanicPairing(
                P12StageMechanics.Riptide,
                P12StageMechanics.ReturnStamp),
            new P12MechanicPairing(
                P12StageMechanics.Riptide,
                P12StageMechanics.SwayingPlatform),
            new P12MechanicPairing(
                P12StageMechanics.ReturnStamp,
                P12StageMechanics.SwayingPlatform),
            new P12MechanicPairing(
                P12StageMechanics.Crosswind,
                P12StageMechanics.GrowingVine),
            new P12MechanicPairing(
                P12StageMechanics.Crosswind,
                P12StageMechanics.OrbitPlatform),
            new P12MechanicPairing(
                P12StageMechanics.GrowingVine,
                P12StageMechanics.OrbitPlatform),
            new P12MechanicPairing(
                P12StageMechanics.HomecomingStatueEconomy,
                P12StageMechanics.InvariantStarBlock),
            new P12MechanicPairing(
                P12StageMechanics.ReturnField,
                P12StageMechanics.OverheatedPlatform),
            new P12MechanicPairing(
                P12StageMechanics.TimedPlatform,
                P12StageMechanics.GravityDial),
            new P12MechanicPairing(
                P12StageMechanics.FallingObject,
                P12StageMechanics.SwayingPlatform),
            new P12MechanicPairing(
                P12StageMechanics.Floodgate,
                P12StageMechanics.ParcelLauncher)
        };

        public static IReadOnlyList<P12MechanicPairing>
            ApprovedPairings => Pairings;

        public static bool ForbidsAutoChainStatueDestruction => true;
        public static bool ForbidsOffscreenProjectiles => true;
        public static bool RequiresSafeDemonstrationForFirstPairing =>
            true;

        public static P12StageMechanics AllKnownMechanics =>
            P12StageMechanics.Crosswind
            | P12StageMechanics.Riptide
            | P12StageMechanics.SwayingPlatform
            | P12StageMechanics.TimedPlatform
            | P12StageMechanics.Floodgate
            | P12StageMechanics.FallingObject
            | P12StageMechanics.ParcelConveyor
            | P12StageMechanics.ParcelLauncher
            | P12StageMechanics.ReturnStamp
            | P12StageMechanics.ReturnField
            | P12StageMechanics.RotatingSunRay
            | P12StageMechanics.GrowingVine
            | P12StageMechanics.OverheatedPlatform
            | P12StageMechanics.OrbitPlatform
            | P12StageMechanics.GravityDial
            | P12StageMechanics.ConstellationBridge
            | P12StageMechanics.InvariantStarBlock
            | P12StageMechanics.HomecomingStatueEconomy;

        public static bool IsKnownMechanic(P12StageMechanics flag)
        {
            return flag != P12StageMechanics.None
                && (AllKnownMechanics & flag) == flag;
        }

        public static bool IsApprovedPairing(
            P12StageMechanics first,
            P12StageMechanics second)
        {
            for (int index = 0; index < Pairings.Length; index++)
            {
                if (Pairings[index].Matches(first, second))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryFindUnapprovedPairing(
            P12StageMechanics mechanics,
            out P12StageMechanics first,
            out P12StageMechanics second)
        {
            for (int a = 0; a < MechanicFlagCount; a++)
            {
                var left = (P12StageMechanics)(1 << a);
                if ((mechanics & left) != left)
                {
                    continue;
                }

                for (int b = a + 1; b < MechanicFlagCount; b++)
                {
                    var right = (P12StageMechanics)(1 << b);
                    if ((mechanics & right) != right)
                    {
                        continue;
                    }

                    if (!IsApprovedPairing(left, right))
                    {
                        first = left;
                        second = right;
                        return true;
                    }
                }
            }

            first = P12StageMechanics.None;
            second = P12StageMechanics.None;
            return false;
        }

        public static void CollectPairingKeys(
            P12StageMechanics mechanics,
            List<int> buffer)
        {
            if (buffer == null)
            {
                return;
            }

            for (int a = 0; a < MechanicFlagCount; a++)
            {
                var left = (P12StageMechanics)(1 << a);
                if ((mechanics & left) != left)
                {
                    continue;
                }

                for (int b = a + 1; b < MechanicFlagCount; b++)
                {
                    var right = (P12StageMechanics)(1 << b);
                    if ((mechanics & right) == right)
                    {
                        buffer.Add((int)(left | right));
                    }
                }
            }
        }
    }

    public static class P12ReturnCrystalRules
    {
        public const int SmallGoldValue = P7EconomyRules.SmallGoldValue;
        public const int BigGoldValue = P7EconomyRules.BigGoldValue;
        public const int ValueMultiplier = 2;
        public const int SmallCrystalValue =
            SmallGoldValue * ValueMultiplier;
        public const int StandardCrystalValue =
            BigGoldValue * ValueMultiplier;
        public const float DriftCellsPerSecond = 0.5f;

        public static int CrystalValueFor(int goldValue)
        {
            return goldValue <= 0 ? 0 : goldValue * ValueMultiplier;
        }
    }

    public static class P12ChallengeDifficulty
    {
        public const int MaxHealth = 4;
        public const int LanternChargesPerSegment = 1;
        public const int SmallMaruBellCharges = 1;
        public const float ShortenedFirstBellSeconds = 80f;
        public const float ShortenedSecondBellSeconds = 110f;
        public const float ShortenedMaruDueSeconds = 135f;
        public const float EarlyMaruFirstBellSeconds = 55f;
        public const float EarlyMaruSecondBellSeconds = 80f;
        public const float EarlyMaruDueSeconds = 100f;

        public static P8MaruTimelineProfile CreateShortenedProfile(
            P6StageSlot stageSlot,
            bool earlyMaru)
        {
            return earlyMaru
                ? new P8MaruTimelineProfile(
                    stageSlot,
                    EarlyMaruFirstBellSeconds,
                    EarlyMaruSecondBellSeconds,
                    EarlyMaruDueSeconds,
                    false)
                : new P8MaruTimelineProfile(
                    stageSlot,
                    ShortenedFirstBellSeconds,
                    ShortenedSecondBellSeconds,
                    ShortenedMaruDueSeconds,
                    false);
        }
    }

    public static class P12FailurePolicy
    {
        public const int MaxFailures = 2;

        public static P12FailureConsequence Evaluate(int failureCount)
        {
            return failureCount >= MaxFailures
                ? P12FailureConsequence.EndChallenge
                : P12FailureConsequence.RestartSegment;
        }
    }

    public static class P12EntryRequirements
    {
        public static bool IsSatisfied(
            bool hasWeaverThread,
            bool hasDragonPearl,
            bool hasDawnStarCoordinates,
            bool memoryEndingCompleted)
        {
            return hasWeaverThread
                && hasDragonPearl
                && hasDawnStarCoordinates
                && memoryEndingCompleted;
        }
    }
}

#endif
