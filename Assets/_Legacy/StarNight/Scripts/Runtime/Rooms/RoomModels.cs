#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Rooms
{
    public enum RoomArchetype
    {
        Small = 0,
        Standard = 1,
        Wide = 2,
        Tall = 3,
        Large = 4,
        HorizontalCorridor = 5,
        VerticalCorridor = 6,
        Boss = 7
    }

    [Flags]
    public enum RoomRole
    {
        None = 0,
        Start = 1 << 0,
        Main = 1 << 1,
        Landmark = 1 << 2,
        Shortcut = 1 << 3,
        LocalEvent = 1 << 4,
        RecordRoom = 1 << 5,
        MaruStatue = 1 << 6,
        ShopCorridor = 1 << 7,
        SupplyCorridor = 1 << 8,
        RelicRoom = 1 << 9,
        Exit = 1 << 10,
        Boss = 1 << 11,
        RiskyChoice = 1 << 12,
        LandmarkJunction = 1 << 13
    }

    public enum RoomRegion
    {
        Universal = 0,
        MoonPalace = 1,
        MagpieBridge = 2,
        DragonPalace = 3,
        StarPostOffice = 4,
        SunriseGarden = 5,
        PolarisObservatory = 6,
        StarlessSea = 7
    }

    public enum RoomSocketSide
    {
        Left = 0,
        Right = 1,
        Bottom = 2,
        Top = 3
    }

    public enum RoomTraversalType
    {
        Walk = 0,
        Jump = 1,
        ReturnableDrop = 2,
        RopeOptional = 3,
        BreakableOptional = 4,
        HookOptional = 5,
        Water = 6,
        Wind = 7,
        BossEntry = 8,
        ExitOnly = 9
    }

    public enum RoomValidationProfile
    {
        StandardTraversal = 0,
        ShortInteraction = 1,
        LayeredTraversal = 2,
        CorridorRest = 3,
        OptionalDeadEnd = 4
    }

    public enum RoomContentSlotKind
    {
        SafeCell = 0,
        OptionalReward = 1,
        Treasure = 2,
        Trap = 3,
        Enemy = 4,
        Npc = 5,
        Shop = 6,
        Supply = 7,
        RecordGuest = 8,
        MaruStatue = 9,
        MaruEntry = 10,
        Landmark = 11,
        LocalEvent = 12
    }

    [Serializable]
    public sealed class RoomValidationReport
    {
        [SerializeField] private List<string> issues = new List<string>();

        public IReadOnlyList<string> Issues => issues;
        public bool Passed => issues.Count == 0;

        public void Add(string issue)
        {
            if (!string.IsNullOrWhiteSpace(issue))
            {
                issues.Add(issue);
            }
        }

        public void AddRange(RoomValidationReport other)
        {
            if (other == null)
            {
                return;
            }

            for (int index = 0; index < other.issues.Count; index++)
            {
                issues.Add(other.issues[index]);
            }
        }

        public override string ToString()
        {
            return Passed ? "PASS" : string.Join(Environment.NewLine, issues);
        }
    }

    // 문서 22.1 지역별 최소 출시 수량 / 22.2 공용 요소 최소 수량
    public enum RoomContentQuotaCategory
    {
        MicroStandardRoom = 0,
        WideRoom = 1,
        TallRoom = 2,
        LargeRoom = 3,
        LongHallDeepShaftRoom = 4,
        RestCorridorRoom = 5,
        LocalEventRoom = 6,
        MaruRoom = 7,
        RecordRoom = 8,
        LandmarkRoom = 9,
        SecretRoom = 10,
        RegionRoomTotal = 11,
        GlobalElement = 12,
        MaruElement = 13,
        RegionElement = 14,
        RecordGuestElement = 15,
        CorridorModuleElement = 16
    }

    public sealed class RoomContentQuotaRule
    {
        private readonly RoomContentQuotaCategory category;
        private readonly string displayName;
        private readonly int minimum;
        private readonly int recommended;
        private readonly bool roomLibraryMeasurable;
        private readonly bool libraryHardRequirement;

        public RoomContentQuotaRule(
            RoomContentQuotaCategory quotaCategory,
            string quotaDisplayName,
            int quotaMinimum,
            int quotaRecommended,
            bool measurableFromRoomLibrary,
            bool hardRequirement)
        {
            category = quotaCategory;
            displayName = quotaDisplayName;
            minimum = quotaMinimum;
            recommended = quotaRecommended;
            roomLibraryMeasurable = measurableFromRoomLibrary;
            libraryHardRequirement = hardRequirement;
        }

        public RoomContentQuotaCategory Category => category;
        public string DisplayName => displayName;
        public int Minimum => minimum;
        public int Recommended => recommended;
        public bool RoomLibraryMeasurable => roomLibraryMeasurable;

        // 현재 제작량이 문서 최소치를 이미 넘긴 항목만 라이브러리 하드 검증에 참여한다.
        public bool LibraryHardRequirement => libraryHardRequirement;
    }

    public static class RoomContentQuota
    {
        public const int MicroStandardRoomMinimum = 10;
        public const int MicroStandardRoomRecommended = 14;
        public const int WideRoomMinimum = 4;
        public const int WideRoomRecommended = 6;
        public const int TallRoomMinimum = 4;
        public const int TallRoomRecommended = 6;
        public const int LargeRoomMinimum = 2;
        public const int LargeRoomRecommended = 4;
        public const int LongHallDeepShaftRoomMinimum = 1;
        public const int LongHallDeepShaftRoomRecommended = 2;
        public const int RestCorridorRoomMinimum = 2;
        public const int RestCorridorRoomRecommended = 3;
        public const int LocalEventRoomMinimum = 2;
        public const int LocalEventRoomRecommended = 3;
        public const int MaruRoomMinimum = 2;
        public const int MaruRoomRecommended = 3;
        public const int RecordRoomMinimum = 1;
        public const int RecordRoomRecommended = 2;
        public const int LandmarkRoomMinimum = 1;
        public const int LandmarkRoomRecommended = 2;
        public const int SecretRoomMinimum = 3;
        public const int SecretRoomRecommended = 5;
        public const int RegionRoomTotalRecommended = 45;

        public const int GlobalElementMinimum = 20;
        public const int MaruElementMinimum = 5;
        public const int RegionElementMinimum = 6;
        public const int RegionElementRecommended = 8;
        public const int RecordGuestElementMinimum = 6;
        public const int CorridorModuleElementMinimum = 12;

        public const int LongHallDeepShaftMacroExtent = 3;

        private static readonly RoomContentQuotaRule[] AllRules =
        {
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.MicroStandardRoom,
                "Micro·Standard 방",
                MicroStandardRoomMinimum,
                MicroStandardRoomRecommended,
                true,
                true),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.WideRoom,
                "Wide 방",
                WideRoomMinimum,
                WideRoomRecommended,
                true,
                true),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.TallRoom,
                "Tall 방",
                TallRoomMinimum,
                TallRoomRecommended,
                true,
                true),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.LargeRoom,
                "Large 방",
                LargeRoomMinimum,
                LargeRoomRecommended,
                true,
                true),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.LongHallDeepShaftRoom,
                "LongHall·DeepShaft 방",
                LongHallDeepShaftRoomMinimum,
                LongHallDeepShaftRoomRecommended,
                true,
                false),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.RestCorridorRoom,
                "상점·보급 복도",
                RestCorridorRoomMinimum,
                RestCorridorRoomRecommended,
                true,
                true),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.LocalEventRoom,
                "LocalEvent 방",
                LocalEventRoomMinimum,
                LocalEventRoomRecommended,
                true,
                false),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.MaruRoom,
                "마루 방",
                MaruRoomMinimum,
                MaruRoomRecommended,
                true,
                false),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.RecordRoom,
                "기록관 방",
                RecordRoomMinimum,
                RecordRoomRecommended,
                true,
                true),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.LandmarkRoom,
                "랜드마크 방",
                LandmarkRoomMinimum,
                LandmarkRoomRecommended,
                true,
                true),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.SecretRoom,
                "비밀방",
                SecretRoomMinimum,
                SecretRoomRecommended,
                true,
                false),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.RegionRoomTotal,
                "지역 방 총량",
                0,
                RegionRoomTotalRecommended,
                true,
                false),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.GlobalElement,
                "전역 지형·함정·도움 요소",
                GlobalElementMinimum,
                GlobalElementMinimum,
                false,
                false),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.MaruElement,
                "마루 관련 요소",
                MaruElementMinimum,
                MaruElementMinimum,
                false,
                false),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.RegionElement,
                "지역 전용 요소",
                RegionElementMinimum,
                RegionElementRecommended,
                false,
                false),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.RecordGuestElement,
                "기록 길손",
                RecordGuestElementMinimum,
                RecordGuestElementMinimum,
                false,
                false),
            new RoomContentQuotaRule(
                RoomContentQuotaCategory.CorridorModuleElement,
                "복도 모듈",
                CorridorModuleElementMinimum,
                CorridorModuleElementMinimum,
                false,
                false)
        };

        public static IReadOnlyList<RoomContentQuotaRule> Rules => AllRules;

        public static bool TryGetRule(
            RoomContentQuotaCategory category,
            out RoomContentQuotaRule rule)
        {
            for (int index = 0; index < AllRules.Length; index++)
            {
                if (AllRules[index].Category == category)
                {
                    rule = AllRules[index];
                    return true;
                }
            }

            rule = null;
            return false;
        }

        public static int GetMinimum(RoomContentQuotaCategory category)
        {
            return TryGetRule(category, out RoomContentQuotaRule rule)
                ? rule.Minimum
                : 0;
        }

        public static int GetRecommended(RoomContentQuotaCategory category)
        {
            return TryGetRule(category, out RoomContentQuotaRule rule)
                ? rule.Recommended
                : 0;
        }
    }

    [Serializable]
    public sealed class RoomContentQuotaEntry
    {
        [SerializeField] private RoomContentQuotaCategory category;
        [SerializeField] private string displayName;
        [SerializeField] private int minimum;
        [SerializeField] private int recommended;
        [SerializeField] private int current;
        [SerializeField] private bool measured;

        public RoomContentQuotaEntry(
            RoomContentQuotaRule rule,
            int currentCount)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            category = rule.Category;
            displayName = rule.DisplayName;
            minimum = rule.Minimum;
            recommended = rule.Recommended;
            measured = rule.RoomLibraryMeasurable;
            current = measured ? Mathf.Max(0, currentCount) : 0;
        }

        public RoomContentQuotaCategory Category => category;
        public string DisplayName => displayName;
        public int Minimum => minimum;
        public int Recommended => recommended;
        public int Current => current;
        public bool Measured => measured;
        public int Shortfall =>
            measured ? Mathf.Max(0, minimum - current) : 0;
        public int RecommendedShortfall =>
            measured ? Mathf.Max(0, recommended - current) : 0;
        public bool MeetsMinimum => !measured || current >= minimum;
        public bool MeetsRecommended => !measured || current >= recommended;

        public override string ToString()
        {
            if (!measured)
            {
                return $"{displayName}: 최소 {minimum} / 권장 {recommended} (방 라이브러리로 측정 불가)";
            }

            return $"{displayName}: 현재 {current} / 최소 {minimum} / 권장 {recommended} "
                + $"(부족 {Shortfall}, 권장까지 {RecommendedShortfall})";
        }
    }

    [Serializable]
    public sealed class RoomContentQuotaReport
    {
        [SerializeField] private List<RoomContentQuotaEntry> entries =
            new List<RoomContentQuotaEntry>();

        public IReadOnlyList<RoomContentQuotaEntry> Entries => entries;

        public bool MeetsAllMinimums
        {
            get
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    if (!entries[index].MeetsMinimum)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool MeetsAllRecommended
        {
            get
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    if (!entries[index].MeetsRecommended)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void Add(RoomContentQuotaEntry entry)
        {
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        public bool TryGetEntry(
            RoomContentQuotaCategory category,
            out RoomContentQuotaEntry entry)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].Category == category)
                {
                    entry = entries[index];
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public bool TryGetShortfalls(
            out IReadOnlyList<RoomContentQuotaEntry> shortfalls)
        {
            List<RoomContentQuotaEntry> found =
                new List<RoomContentQuotaEntry>();
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].Shortfall > 0)
                {
                    found.Add(entries[index]);
                }
            }

            shortfalls = found;
            return found.Count > 0;
        }

        public override string ToString()
        {
            string[] lines = new string[entries.Count];
            for (int index = 0; index < entries.Count; index++)
            {
                lines[index] = entries[index].ToString();
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    public static class RoomSocketRules
    {
        public static bool IsMainRouteTraversal(RoomTraversalType traversalType)
        {
            return traversalType == RoomTraversalType.Walk
                || traversalType == RoomTraversalType.Jump
                || traversalType == RoomTraversalType.ReturnableDrop;
        }

        public static bool AreOpposite(RoomSocketSide first, RoomSocketSide second)
        {
            return (first == RoomSocketSide.Left && second == RoomSocketSide.Right)
                || (first == RoomSocketSide.Right && second == RoomSocketSide.Left)
                || (first == RoomSocketSide.Bottom && second == RoomSocketSide.Top)
                || (first == RoomSocketSide.Top && second == RoomSocketSide.Bottom);
        }

        public static bool AreCompatible(RoomSocket2D first, RoomSocket2D second)
        {
            if (first == null || second == null || first == second)
            {
                return false;
            }

            if (!AreOpposite(first.Side, second.Side)
                || first.OpeningSize != second.OpeningSize
                || first.TraversalType != second.TraversalType
                || first.TraversalType == RoomTraversalType.ExitOnly)
            {
                return false;
            }

            bool laneMatches =
                (first.Side == RoomSocketSide.Left
                    || first.Side == RoomSocketSide.Right)
                    ? PositiveModulo(first.CellOffset.y, 8)
                        == PositiveModulo(second.CellOffset.y, 8)
                    : PositiveModulo(first.CellOffset.x, 12)
                        == PositiveModulo(second.CellOffset.x, 12);
            if (!laneMatches)
            {
                return false;
            }

            RoomTemplate2D firstRoom =
                first.GetComponentInParent<RoomTemplate2D>();
            RoomTemplate2D secondRoom =
                second.GetComponentInParent<RoomTemplate2D>();
            return firstRoom == null
                || secondRoom == null
                || firstRoom.Region == RoomRegion.Universal
                || secondRoom.Region == RoomRegion.Universal
                || firstRoom.Region == secondRoom.Region;
        }

        private static int PositiveModulo(int value, int divisor)
        {
            int remainder = value % divisor;
            return remainder < 0 ? remainder + divisor : remainder;
        }
    }
}

#endif
