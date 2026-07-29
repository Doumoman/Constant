using System;

namespace StarFetchingNight
{
    public enum ChapterLoopState
    {
        Arrival,
        RuleIntro,
        RouteOpen,
        RouteProgress,
        GateReady,
        GateActive,
        Bell1,
        Bell2,
        Bell3,
        Departure,
        Intermission,
        ForcedReturn
    }

    public enum GateRouteState
    {
        Locked,
        Available,
        Complete,
        Contributed,
        Invalidated
    }

    public enum GateRouteArchetype
    {
        Cooperation,
        Exploration,
        Appropriation
    }

    public enum StarBellPhase
    {
        None = 0,
        First = 1,
        Second = 2,
        Third = 3
    }

    public enum MaruHuntMode
    {
        Hidden,
        TraceOnly,
        StationHunt,
        PlayerHunt
    }

    public static class StarGateAlertRules
    {
        public const float FirstBellThreshold = 0f;
        public const float SecondBellThreshold = 30f;
        public const float ThirdBellThreshold = 60f;
        public const float PassiveAlertPerSecond = 1f / 3f;
        public const float SecondsToSecondBellWithoutActions =
            (SecondBellThreshold - FirstBellThreshold) / PassiveAlertPerSecond;
        public const float SecondsToThirdBellWithoutActions =
            (ThirdBellThreshold - FirstBellThreshold) / PassiveAlertPerSecond;

        public static float MinimumAlertForPhase(StarBellPhase phase) => phase switch
        {
            StarBellPhase.Second => SecondBellThreshold,
            StarBellPhase.Third => ThirdBellThreshold,
            _ => FirstBellThreshold
        };
    }

    [Serializable]
    public sealed class GateRouteDefinition
    {
        public string id;
        public string displayName;
        public GateRouteArchetype archetype;
        public string contributionId;
        public string contributionDisplayName;
        public int gateValue = 1;
    }

    [Serializable]
    public sealed class GateRouteRuntimeState
    {
        public string id;
        public string displayName;
        public GateRouteArchetype archetype;
        public GateRouteState state;
        public string contributionId;
        public string contributionDisplayName;
        public int gateValue;

        public static GateRouteRuntimeState FromDefinition(GateRouteDefinition definition)
        {
            return new GateRouteRuntimeState
            {
                id = definition.id,
                displayName = definition.displayName,
                archetype = definition.archetype,
                state = GateRouteState.Locked,
                contributionId = definition.contributionId,
                contributionDisplayName = definition.contributionDisplayName,
                gateValue = Math.Max(1, definition.gateValue)
            };
        }
    }

    [Serializable]
    public sealed class GateContribution
    {
        public string id;
        public string displayName;
        public string routeId;
        public int gateValue = 1;

        public GateContribution Copy()
        {
            return new GateContribution
            {
                id = id,
                displayName = displayName,
                routeId = routeId,
                gateValue = Math.Max(1, gateValue)
            };
        }
    }
}
