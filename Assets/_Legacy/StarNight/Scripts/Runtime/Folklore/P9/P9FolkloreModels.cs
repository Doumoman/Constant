#if LEGACY_DISABLED
namespace StarNight.Folklore.P9
{
    public enum P9FolkloreItemKind
    {
        None = 0,
        MoonCake = 1,
        JadeRabbitMedicine = 2,
        RedWeaverThread = 3,
        DragonPalaceOrb = 4
    }

    public enum P9CorrespondenceEventKind
    {
        HungryMagpie = 0,
        InjuredTurtle = 1
    }

    public enum P9CorrespondenceResolution
    {
        None = 0,
        MatchingGift = 1,
        AlternativeRescue = 2
    }

    public enum P9BranchKind
    {
        None = 0,
        MagpieBridge = 1,
        DragonPalace = 2
    }

    public enum P9InferenceCueKind
    {
        VisibleGift = 0,
        MatchingSilhouette = 1,
        NpcAttention = 2,
        RouteResponse = 3
    }

    public enum P9RecordGuestImmediateSupport
    {
        NearestRecoveryAndMedicine = 0,
        SafeMainAndOptionalRoute = 1,
        CurrentAndHighestValueTreasure = 2,
        StageGraphAndOneSecretRoom = 3,
        NextBellCountdown = 4,
        RelicAndMemoryDoorResonance = 5
    }

    public enum P9RecordGuestNextStageSupport
    {
        MoonCakeNearExit = 0,
        RopeAtStart = 1,
        FirstFloodgateOpened = 2,
        StrongerExitDirectionMark = 3,
        DelayNextBellTwelveSeconds = 4,
        IlluminateOneBossChoiceDevice = 5
    }

    [System.Flags]
    public enum P9ArchiveUnlockMethods
    {
        None = 0,
        SealLever = 1 << 0,
        CrackedOuterWall = 1 << 1,
        HookLatch = 1 << 2
    }
}

#endif
