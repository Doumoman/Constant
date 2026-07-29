using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    public enum StarChapterId { Prologue, MoonRabbitMill, MagpieBridge, CloudWhaleRanch, StarPostOffice, SleepingSunGarden, PolarisObservatory }
    public enum FableVerb { Resize, Link, Float, Deliver, Awaken }
    public enum ResizeIntent { Enlarge, Shrink }
    public enum FableModification { Large, Small, Linked, Floating, DeliveryPending, Awakened, Overloaded }

    [Flags]
    public enum FableTraits
    {
        None = 0,
        Carryable = 1 << 0,
        Resizable = 1 << 1,
        Linkable = 1 << 2,
        Floatable = 1 << 3,
        Deliverable = 1 << 4,
        LightReactive = 1 << 5,
        Living = 1 << 6,
        Flammable = 1 << 7,
        Breakable = 1 << 8,
        Bouncy = 1 << 9,
        Explosive = 1 << 10,
        ResidentProperty = 1 << 11,
        DepartureSupply = 1 << 12,
        RareToy = 1 << 13,
        MoonCake = 1 << 14,
        BridgeAnchor = 1 << 15,
        ThreadReinforcement = 1 << 16,
        RainCloud = 1 << 17,
        WeightReservoir = 1 << 18,
        CloudWhale = 1 << 19,
        PostalAddress = 1 << 20,
        PostalParcel = 1 << 21,
        LastLetter = 1 << 22,
        RouteStamp = 1 << 23,
        SunlightSource = 1 << 24,
        GrowthNode = 1 << 25,
        StarPathTree = 1 << 26,
        SleepingCreature = 1 << 27,
        GardenPlant = 1 << 28,
        BrightSource = 1 << 29
    }

    public enum StarItemKind { General, ResidentProperty, DepartureSupply, RareToy }
    public enum StarScentStage { Quiet, Scent, Footprints, Bell, ReturnTime }
    public enum StarNpcState { Calm, Tired, Moved, Distrustful, Dependent, Autonomous, Missing }

    public enum StarActionType
    {
        ObjectInspected, ObjectTaken, ObjectReturned, ObjectDismantled,
        NpcWarningHeard, NpcForcedReturn, NpcAllowedChoice,
        DamageCaused, DamageRepaired, DepartureReady, DepartedWithUnresolvedEvent,
        RareRoomEntered, EnteredTemptationRoom, MaruLured, MaruTookNpc,
        PlayerSacrificedCoreItem, ToolApplied, ToolOverloaded, FuelDelivered,
        ChapterDeparted, ScentStageChanged, DroppedItem, PlayerCaught,
        LinkEndpointSelected, LinkCreated, LinkCut, LinkSnapped,
        BridgeAnchorRestored, OldBridgeCut, OldBridgeRestored,
        MagpiesForced, ThreadCapacityUpgraded, ChapterTransitioned,
        WeightCollected, WeightTransferred, WeightReturned, CloudBottleOverpressured,
        RainCloudDelivered, CalfReturnedByMaru, GuruAwakened, GuruReleased, GuruReturned,
        RainSystemRebuilt, StormDamageRecorded, AccidentReplayed,
        ParcelSelected, ParcelDelivered, ParcelMisdelivered, ParcelIntercepted,
        PlayerDelivered, RouteStampRecovered, LetterOpened, LetterDelivered,
        LetterDismantled, LetterPreserved, LetterSealCopied, LetterSealDamaged,
        SorterOverloaded, SorterRepaired,
        RaniDisconnected, ReturnVaultEntered,
        SunlightCollected, SunlightApplied, GrowthAdvanced, GrowthOverloaded,
        HaoreumForcedAwake, HaoreumNaturalAwake, GardenWaited,
        GardenOverheated, GardenRestored, PocketSunTaken,
        StarPathGrown, StarPathStabilized, StarPathOvergrown, StarPathBurned,
        MaruBlinded, PreservedPotFound,
        RouteObjectiveCompleted, GateContributionAdded, GateReady, GateActivated,
        BellPhaseChanged, GateClosing, ChapterLoopStateChanged, GateContributionReturned,
        ReturnCakeFueled, MaruRescuedShip, GuideStarTaken, TravelTicketUnlocked,
        PolarisRecordReplayed, PolarisTruthRevealed, PolarisToolRestored,
        PolarisCenterReached, PolarisEndingChosen, RaniCommandWithdrawn, MaruReachedPolaris
    }

    public enum StarRunEndReason { None, HealthLost, ForcedReturnByMaru, Departed, JourneyComplete }

    [Serializable]
    public sealed class StarActionContext
    {
        public StarActionType actionType;
        public string actorId;
        public string targetId;
        public string routeId;
        public FableVerb tool;
        [TextArea] public string detail;
        public float scentDelta;
        public bool causedAccident;
        public bool helpedResident;
        public bool witnessed;
        public int gateContributions;
        public bool gateReady;
        public bool gateActivated;
        public int bellPhase;
    }

    [Serializable]
    public sealed class StarActionResult
    {
        public bool shortcutOpened;
        public bool routeClosed;
        public bool repaired;
        public bool irreversible;
        public int repairCost;
        public float scentAdded;
        public string stateChange;
    }

    [Serializable]
    public sealed class StarActionRecord
    {
        public int sequence;
        public float time;
        public StarActionType actionType;
        public string actorId;
        public string targetId;
        public string routeId;
        public FableVerb tool;
        public StarChapterId chapter;
        public string detail;
        public float scentDelta;
        public bool causedAccident;
        public bool helpedResident;
        public bool witnessed;
        public int gateContributions;
        public bool gateReady;
        public bool gateActivated;
        public int bellPhase;

        public int BasePriority
        {
            get
            {
                if (actionType == StarActionType.PlayerCaught || actionType == StarActionType.MaruTookNpc) return 100;
                if (actionType == StarActionType.ParcelIntercepted) return 100;
                if (actionType == StarActionType.PlayerSacrificedCoreItem || actionType == StarActionType.DamageRepaired) return 90;
                if (actionType == StarActionType.LetterDelivered || actionType == StarActionType.LetterPreserved) return 90;
                if (actionType == StarActionType.LetterSealCopied) return 90;
                if (actionType == StarActionType.LetterOpened) return 88;
                if (actionType == StarActionType.LetterSealDamaged) return 87;
                if (actionType == StarActionType.LetterDismantled) return 86;
                if (actionType == StarActionType.DepartedWithUnresolvedEvent || actionType == StarActionType.NpcForcedReturn) return 85;
                if (actionType == StarActionType.CalfReturnedByMaru) return 92;
                if (actionType == StarActionType.GuruReleased || actionType == StarActionType.RainSystemRebuilt) return 84;
                if (actionType == StarActionType.OldBridgeCut || actionType == StarActionType.LinkSnapped) return 82;
                if (actionType == StarActionType.CloudBottleOverpressured || actionType == StarActionType.StormDamageRecorded) return 82;
                if (causedAccident || actionType == StarActionType.ToolOverloaded) return 80;
                if (actionType == StarActionType.BridgeAnchorRestored || actionType == StarActionType.NpcAllowedChoice) return 76;
                if (actionType == StarActionType.RainCloudDelivered || actionType == StarActionType.GuruReturned) return 76;
                if (actionType == StarActionType.RouteStampRecovered || actionType == StarActionType.SorterRepaired) return 76;
                if (actionType == StarActionType.GardenRestored || actionType == StarActionType.StarPathStabilized) return 90;
                if (actionType == StarActionType.HaoreumForcedAwake || actionType == StarActionType.StarPathBurned) return 88;
                if (actionType == StarActionType.HaoreumNaturalAwake || actionType == StarActionType.StarPathGrown) return 84;
                if (actionType == StarActionType.GrowthOverloaded || actionType == StarActionType.GardenOverheated) return 82;
                if (actionType == StarActionType.PocketSunTaken || actionType == StarActionType.StarPathOvergrown) return 78;
                if (actionType == StarActionType.GateActivated || actionType == StarActionType.GateClosing) return 88;
                if (actionType == StarActionType.GateReady || actionType == StarActionType.BellPhaseChanged) return 78;
                if (actionType == StarActionType.MaruRescuedShip || actionType == StarActionType.GuideStarTaken) return 95;
                if (actionType == StarActionType.ReturnCakeFueled || actionType == StarActionType.TravelTicketUnlocked) return 86;
                if (actionType == StarActionType.PolarisEndingChosen || actionType == StarActionType.RaniCommandWithdrawn) return 100;
                if (actionType == StarActionType.PolarisCenterReached || actionType == StarActionType.MaruReachedPolaris) return 96;
                if (actionType == StarActionType.PolarisTruthRevealed || actionType == StarActionType.PolarisToolRestored) return 92;
                if (actionType == StarActionType.PolarisRecordReplayed) return 84;
                if (actionType == StarActionType.RouteObjectiveCompleted ||
                    actionType == StarActionType.GateContributionAdded ||
                    actionType == StarActionType.GateContributionReturned) return 72;
                if (actionType == StarActionType.ObjectTaken || actionType == StarActionType.DroppedItem) return 70;
                if (actionType == StarActionType.EnteredTemptationRoom) return 65;
                if (helpedResident) return 60;
                return 20;
            }
        }
    }

    [Serializable]
    public sealed class AccidentStep
    {
        public string subject;
        public string verb;
        public string result;
        public int actionSequence;
        public float time;
        public bool gateActivated;
        public int bellPhase;
    }

    [Serializable]
    public sealed class ConsequenceModifier
    {
        public string id;
        public string description;
        public float scentMultiplier = 1f;
        public int chapterOffset;
        public StarChapterId sourceChapter;
        public StarChapterId targetChapter;
    }

    [Serializable]
    public sealed class FableToolResult
    {
        public bool success;
        public bool overloaded;
        public bool awaitingSecondTarget;
        public bool awaitingWeightTarget;
        public bool awaitingDestination;
        public bool connectionChanged;
        public bool weightChanged;
        public bool deliveryChanged;
        public bool growthChanged;
        public string sentence;
        public string failureReason;
        public float scentAdded;
        public List<string> secondaryEffects = new();

        public static FableToolResult Fail(string reason) =>
            new() { success = false, failureReason = reason, sentence = reason };
    }

    [Serializable]
    public sealed class StarChapterDefinition
    {
        public StarChapterId chapter;
        public string displayName;
        public FableVerb coreVerb;
        [TextArea] public string oneSentenceRule;
        public int requiredDepartureItems = 3;
        public bool useGateLoop;
        public int gateContributionRequired = 2;
        public string objectiveNoun = "출항 물품";
        [TextArea] public string objectiveInstruction;
        public List<string> guaranteedRooms = new();
        public List<string> optionalRooms = new();
        public List<GateRouteDefinition> gateRoutes = new();
    }

    [Serializable]
    public sealed class StarChapterReport
    {
        public StarChapterId chapter;
        [TextArea] public string raniSummary;
        public int finalActionSequence;
    }

    public static class StarScentRules
    {
        public const float MaxScent = 100f;

        public static StarScentStage FromValue(float scent)
        {
            if (scent >= 100f) return StarScentStage.ReturnTime;
            if (scent >= 75f) return StarScentStage.Bell;
            if (scent >= 50f) return StarScentStage.Footprints;
            if (scent >= 25f) return StarScentStage.Scent;
            return StarScentStage.Quiet;
        }

        public static string DisplayName(StarScentStage stage) => stage switch
        {
            StarScentStage.Scent => "별내음",
            StarScentStage.Footprints => "발자국",
            StarScentStage.Bell => "방울소리",
            StarScentStage.ReturnTime => "돌아갈 시간",
            _ => "고요"
        };
    }
}
