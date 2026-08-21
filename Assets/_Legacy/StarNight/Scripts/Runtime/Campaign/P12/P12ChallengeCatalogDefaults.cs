#if LEGACY_DISABLED
using StarNight.Generation.P6;
using StarNight.Rooms;

namespace StarNight.Campaign.P12
{
    public static class P12ChallengeCatalogDefaults
    {
        public static P12StageDefinition[] CreateStandardDefinitions()
        {
            return new[]
            {
                Define(
                    P12StageId.StarlessSea01,
                    "달빛 표류장",
                    P12ChallengeSegment.FirstSea,
                    P6StageArchetype.Traverse,
                    new[]
                    {
                        RoomRegion.MoonPalace,
                        RoomRegion.MagpieBridge
                    },
                    P12StageMechanics.SwayingPlatform
                    | P12StageMechanics.TimedPlatform,
                    2, 2, 9, 11,
                    false, false,
                    P12BossEchoKind.None,
                    false, true,
                    "흔들리는 달반죽 발판에서 실다리 시한 발판으로 박자를 이어 건넌다."),
                Define(
                    P12StageId.StarlessSea02,
                    "잠긴 방앗간 물길",
                    P12ChallengeSegment.FirstSea,
                    P6StageArchetype.Traverse,
                    new[]
                    {
                        RoomRegion.MoonPalace,
                        RoomRegion.DragonPalace
                    },
                    P12StageMechanics.Riptide
                    | P12StageMechanics.FallingObject,
                    2, 2, 9, 11,
                    false, false,
                    P12BossEchoKind.None,
                    false, true,
                    "급류를 타고 낙하 절구의 박자를 피해 물길을 빠져나간다."),
                Define(
                    P12StageId.StarlessSea03,
                    "바람 갈림길",
                    P12ChallengeSegment.FirstSea,
                    P6StageArchetype.Fork,
                    new[]
                    {
                        RoomRegion.MagpieBridge,
                        RoomRegion.StarPostOffice
                    },
                    P12StageMechanics.Crosswind
                    | P12StageMechanics.ParcelLauncher,
                    2, 2, 9, 11,
                    true, false,
                    P12BossEchoKind.None,
                    false, true,
                    "횡풍 속에서 소포 발사기의 궤적을 읽어 갈림길을 고른다."),
                Define(
                    P12StageId.StarlessSea04,
                    "해살 관측 항로",
                    P12ChallengeSegment.SecondSea,
                    P6StageArchetype.Ascent,
                    new[]
                    {
                        RoomRegion.SunriseGarden,
                        RoomRegion.PolarisObservatory,
                        RoomRegion.StarPostOffice
                    },
                    P12StageMechanics.RotatingSunRay
                    | P12StageMechanics.ConstellationBridge
                    | P12StageMechanics.ParcelConveyor,
                    2, 2, 10, 13,
                    false, false,
                    P12BossEchoKind.None,
                    false, true,
                    "회전 해살을 피해 별자리 수신기를 켜고 컨베이어로 항로를 오른다."),
                Define(
                    P12StageId.StarlessSea05,
                    "급류 반송 순환로",
                    P12ChallengeSegment.SecondSea,
                    P6StageArchetype.Circuit,
                    new[]
                    {
                        RoomRegion.DragonPalace,
                        RoomRegion.StarPostOffice,
                        RoomRegion.MoonPalace
                    },
                    P12StageMechanics.Riptide
                    | P12StageMechanics.ReturnStamp
                    | P12StageMechanics.SwayingPlatform,
                    3, 2, 10, 13,
                    false, false,
                    P12BossEchoKind.None,
                    false, true,
                    "반송 도장을 피해 급류와 흔들 발판의 순환로를 한 바퀴 돈다."),
                Define(
                    P12StageId.StarlessSea06,
                    "세 바람의 탑",
                    P12ChallengeSegment.SecondSea,
                    P6StageArchetype.Ascent,
                    new[]
                    {
                        RoomRegion.MagpieBridge,
                        RoomRegion.SunriseGarden,
                        RoomRegion.PolarisObservatory
                    },
                    P12StageMechanics.Crosswind
                    | P12StageMechanics.GrowingVine
                    | P12StageMechanics.OrbitPlatform,
                    3, 2, 10, 13,
                    true, false,
                    P12BossEchoKind.None,
                    false, true,
                    "횡풍을 견디며 성장 덩굴과 궤도 발판으로 탑을 오른다."),
                Define(
                    P12StageId.StarlessSea07,
                    "긴 어스름 회랑",
                    P12ChallengeSegment.ThirdSea,
                    P6StageArchetype.Chase,
                    new[]
                    {
                        RoomRegion.MoonPalace,
                        RoomRegion.PolarisObservatory
                    },
                    P12StageMechanics.HomecomingStatueEconomy
                    | P12StageMechanics.InvariantStarBlock,
                    3, 2, 12, 14,
                    false, true,
                    P12BossEchoKind.None,
                    false, true,
                    "일찍 깨어난 마루를 등지고 귀환결정을 모아 어스름 회랑을 달린다."),
                Define(
                    P12StageId.StarlessSea08,
                    "가라앉은 종루",
                    P12ChallengeSegment.ThirdSea,
                    P6StageArchetype.Descent,
                    new[]
                    {
                        RoomRegion.DragonPalace,
                        RoomRegion.StarPostOffice,
                        RoomRegion.SunriseGarden
                    },
                    P12StageMechanics.ReturnField
                    | P12StageMechanics.OverheatedPlatform,
                    3, 2, 12, 14,
                    false, true,
                    P12BossEchoKind.None,
                    false, true,
                    "반송 필드와 과열 발판을 피해 가라앉은 종루 바닥까지 내려간다."),
                Define(
                    P12StageId.StarlessSea09,
                    "새벽 문턱",
                    P12ChallengeSegment.ThirdSea,
                    P6StageArchetype.Chase,
                    new[]
                    {
                        RoomRegion.MagpieBridge,
                        RoomRegion.PolarisObservatory
                    },
                    P12StageMechanics.TimedPlatform
                    | P12StageMechanics.GravityDial,
                    3, 2, 12, 14,
                    true, true,
                    P12BossEchoKind.None,
                    false, true,
                    "중력 다이얼을 돌려 시한 발판을 붙잡고 마루보다 먼저 문턱을 넘는다."),
                Define(
                    P12StageId.StarlessSea10,
                    "떡메 잔향",
                    P12ChallengeSegment.DawnPassage,
                    P6StageArchetype.Traverse,
                    new[]
                    {
                        RoomRegion.MoonPalace,
                        RoomRegion.MagpieBridge
                    },
                    P12StageMechanics.FallingObject
                    | P12StageMechanics.SwayingPlatform,
                    2, 2, 11, 11,
                    false, false,
                    P12BossEchoKind.Kungtteoki,
                    false, true,
                    "떡메 낙하 리듬에 맞춰 흔들 다리를 건너 잔향을 지나간다."),
                Define(
                    P12StageId.StarlessSea11,
                    "수문 잔향",
                    P12ChallengeSegment.DawnPassage,
                    P6StageArchetype.Circuit,
                    new[]
                    {
                        RoomRegion.DragonPalace,
                        RoomRegion.StarPostOffice
                    },
                    P12StageMechanics.Floodgate
                    | P12StageMechanics.ParcelLauncher,
                    2, 2, 12, 12,
                    false, false,
                    P12BossEchoKind.FloodgateKeeper,
                    false, true,
                    "수문의 물결과 소포 발사기의 포화를 순환로에서 흘려보낸다."),
                Define(
                    P12StageId.StarlessSea12,
                    "새벽별 정박지",
                    P12ChallengeSegment.DawnPassage,
                    P6StageArchetype.Traverse,
                    new[]
                    {
                        RoomRegion.PolarisObservatory
                    },
                    P12StageMechanics.ConstellationBridge,
                    0, 1, 9, 9,
                    true, false,
                    P12BossEchoKind.None,
                    true, false,
                    "별자리 다리를 밝히며 새벽별 정박지에 마지막 신호를 보낸다.")
            };
        }

        private static P12StageDefinition Define(
            P12StageId id,
            string stageName,
            P12ChallengeSegment segment,
            P6StageArchetype archetype,
            RoomRegion[] regions,
            P12StageMechanics mechanics,
            int hazards,
            int ruleFamiliesMax,
            int roomsMin,
            int roomsMax,
            bool mercyCorridor,
            bool earlyMaru,
            P12BossEchoKind bossEcho,
            bool finalArrival,
            bool safeDemonstration,
            string coreActionSentence)
        {
            var definition = new P12StageDefinition();
            definition.Configure(
                id,
                stageName,
                segment,
                archetype,
                regions,
                mechanics,
                hazards,
                ruleFamiliesMax,
                roomsMin,
                roomsMax,
                mercyCorridor,
                earlyMaru,
                bossEcho,
                finalArrival,
                safeDemonstration,
                coreActionSentence);
            return definition;
        }
    }
}

#endif
