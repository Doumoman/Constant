using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceActivityEventPreconditions
    {
        public const string TaskId = "MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS";
        public const string SourceMap2104ResultDigest =
            "7cc7c7fc470277b8266eb076e741b1e887353509daf71a0bf135e919d3ef2710";
        public const string SourceMap2104TaskDigest =
            "fc65f3fde6c380b0c3193afd96a8105b905b01ed00129e55d19cd93db111c137";
        public const string SourceMap2105HandoffDigest =
            "585235b77d97087d29d786d733120fbcd4a50a9c998e8e77b007e9c9bb8baff2";
        public const string SourceMap1207ResultDigest =
            "cfc29b7757130f144e3b57198f048d450409f2cb088fd7ab8e7465ee27b6ff06";
        public const string SourceMap12AggregateDigest =
            "46330eb01dd302bf80dab6eacf88dea59f107cbecc9225b2243a395c1d0dbc8b";
        public const string SourceMap12ActivityCatalogDigest =
            "3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a";
        public const string SourceMap12EventCatalogDigest =
            "2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0";
        public const string SourceMap2104AllBiomeClusterDigest =
            "d30baf04fcd6eceeabd7b6f467b24ce11ce7c5f44833887bf7b0457e6fbdda72";
        public const string SourceMap2104DigestManifestDigest =
            "0a4ffb22812a37dbebc0b3e3f61d01888986c99a78088b6cd86ed61db2969aab";
    }

    public sealed class MoonPalaceActivityClusterDescriptor
    {
        public MoonPalaceActivityClusterDescriptor(string clusterId, string biomeId,
            string poolKind)
        {
            ClusterId = Required(clusterId, nameof(clusterId));
            BiomeId = Required(biomeId, nameof(biomeId));
            PoolKind = Required(poolKind, nameof(poolKind));
        }

        public string ClusterId { get; }
        public string BiomeId { get; }
        public string PoolKind { get; }

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name);
            return value.Trim();
        }
    }

    public sealed class MoonPalaceActivityClusterBinding
    {
        public MoonPalaceActivityClusterBinding(string activityId, string biomeId,
            string primaryClusterId, string fallbackClusterId)
        {
            ActivityId = Require(activityId, nameof(activityId));
            BiomeId = Require(biomeId, nameof(biomeId));
            PrimaryClusterId = Require(primaryClusterId, nameof(primaryClusterId));
            FallbackClusterId = Require(fallbackClusterId, nameof(fallbackClusterId));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_05_ACTIVITY_CLUSTER_BINDING_V1", CanonicalLine,
            });
        }

        public string ActivityId { get; }
        public string BiomeId { get; }
        public string PrimaryClusterId { get; }
        public string FallbackClusterId { get; }
        public string PrimaryPoolKind => "Terrain";
        public string FallbackPoolKind => "Terrain";
        public string BindingRole => "PrimaryWithSameBiomeTerrainFallback";
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(ActivityId, BiomeId,
            PrimaryClusterId, FallbackClusterId, PrimaryPoolKind, FallbackPoolKind,
            BindingRole);

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name);
            return value.Trim();
        }
    }

    public sealed class MoonPalaceActivitySlotRecord
    {
        public MoonPalaceActivitySlotRecord(string activityId, string slotId,
            string slotSemantic, int localX, int localY, string sourceSlotId,
            string markerPayloadId)
        {
            ActivityId = Require(activityId, nameof(activityId));
            SlotId = Require(slotId, nameof(slotId));
            SlotSemantic = Require(slotSemantic, nameof(slotSemantic));
            if (localX < 0) throw new ArgumentOutOfRangeException(nameof(localX));
            if (localY < 0) throw new ArgumentOutOfRangeException(nameof(localY));
            LocalX = localX;
            LocalY = localY;
            SourceSlotId = Require(sourceSlotId, nameof(sourceSlotId));
            MarkerPayloadId = Require(markerPayloadId, nameof(markerPayloadId));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_05_ACTIVITY_SLOT_V1", CanonicalLine,
            });
        }

        public string ActivityId { get; }
        public string SlotId { get; }
        public string SlotSemantic { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public string SourceSlotId { get; }
        public string MarkerPayloadId { get; }
        public string ExecutionPolicy => "MarkerOnlyNoExecution";
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(ActivityId, SlotId,
            SlotSemantic, LocalX.ToString(CultureInfo.InvariantCulture),
            LocalY.ToString(CultureInfo.InvariantCulture), SourceSlotId,
            MarkerPayloadId, ExecutionPolicy);

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name);
            return value.Trim();
        }
    }

    public sealed class MoonPalaceActivityRemovalSafetyRecord
    {
        public MoonPalaceActivityRemovalSafetyRecord(string activityId,
            string staticShellDigest, string criticalTargetDigest,
            int safePocketMarkerCount, int recoveryMarkerCount)
        {
            ActivityId = Require(activityId, nameof(activityId));
            StaticShellDigestBeforeRemoval = Digest(staticShellDigest,
                nameof(staticShellDigest));
            StaticShellDigestAfterRemoval = StaticShellDigestBeforeRemoval;
            CriticalTargetDigestBeforeRemoval = Digest(criticalTargetDigest,
                nameof(criticalTargetDigest));
            CriticalTargetDigestAfterRemoval = CriticalTargetDigestBeforeRemoval;
            if (safePocketMarkerCount < 1) throw new ArgumentOutOfRangeException(
                nameof(safePocketMarkerCount));
            if (recoveryMarkerCount < 1) throw new ArgumentOutOfRangeException(
                nameof(recoveryMarkerCount));
            SafePocketMarkerCount = safePocketMarkerCount;
            RecoveryMarkerCount = recoveryMarkerCount;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_05_ACTIVITY_REMOVAL_SAFETY_V1", CanonicalLine,
            });
        }

        public string ActivityId { get; }
        public string StaticShellDigestBeforeRemoval { get; }
        public string StaticShellDigestAfterRemoval { get; }
        public string CriticalTargetDigestBeforeRemoval { get; }
        public string CriticalTargetDigestAfterRemoval { get; }
        public int SafePocketMarkerCount { get; }
        public int RecoveryMarkerCount { get; }
        public bool OverlayRemovedOnly => true;
        public bool ResetExitPreserved => true;
        public int StaticTileDeltaCount => 0;
        public int RuntimeMutationCount => 0;
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(ActivityId,
            StaticShellDigestBeforeRemoval, StaticShellDigestAfterRemoval,
            CriticalTargetDigestBeforeRemoval, CriticalTargetDigestAfterRemoval,
            SafePocketMarkerCount.ToString(CultureInfo.InvariantCulture),
            RecoveryMarkerCount.ToString(CultureInfo.InvariantCulture), "true", "true",
            "0", "0");

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name);
            return value.Trim();
        }

        private static string Digest(string value, string name)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(value))
                throw new ArgumentException("Lower-hex SHA-256 required.", name);
            return value;
        }
    }

    public sealed class MoonPalaceActivityProfile
    {
        public const string SchemaVersion = "map21_05.activity_profile.v1";

        public MoonPalaceActivityProfile(string activityId, string activityKind,
            string biomeId, string strengthClass, int weight,
            MoonPalaceActivityClusterBinding binding,
            IEnumerable<string> requiredSlotSemantics, string staticShellDigest,
            string removalSafetyDigest, string slotDigest)
        {
            ActivityId = Require(activityId, nameof(activityId));
            SourceActivityId = ActivityId;
            ActivityKind = Require(activityKind, nameof(activityKind));
            BiomeId = Require(biomeId, nameof(biomeId));
            StrengthClass = Require(strengthClass, nameof(strengthClass));
            if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight));
            Weight = weight;
            var sourceBinding = binding ?? throw new ArgumentNullException(nameof(binding));
            if (sourceBinding.ActivityId != ActivityId || sourceBinding.BiomeId != BiomeId)
                throw new ArgumentException("Binding identity mismatch.", nameof(binding));
            PrimaryClusterId = sourceBinding.PrimaryClusterId;
            FallbackClusterIds = sourceBinding.FallbackClusterId;
            RequiredSlotSemantics = string.Join("|", (requiredSlotSemantics ??
                throw new ArgumentNullException(nameof(requiredSlotSemantics)))
                .OrderBy(value => value, StringComparer.Ordinal));
            if (RequiredSlotSemantics.Length == 0)
                throw new ArgumentException("Required slot semantics are empty.");
            CuePolicy = "MarkerOnlyBeforeActivation";
            ActivationPolicy = "StaticRequestMarkerOnly";
            RewardPolicy = "RewardIntentOnlyNoGrant";
            RecoveryPolicy = "StaticRecoveryMarkerNoTeleport";
            RemovalPolicy = "RemoveOverlayOnlyPreserveShellCriticalTargets";
            StaticShellDigest = Digest(staticShellDigest, nameof(staticShellDigest));
            RemovalSafetyDigest = Digest(removalSafetyDigest,
                nameof(removalSafetyDigest));
            ClusterBindingDigest = sourceBinding.CanonicalDigest;
            SlotDigest = Digest(slotDigest, nameof(slotDigest));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_05_ACTIVITY_PROFILE_V1", CanonicalLine,
            });
        }

        public string ActivityId { get; }
        public string SourceActivityId { get; }
        public string ActivityKind { get; }
        public string BiomeId { get; }
        public string StrengthClass { get; }
        public int Weight { get; }
        public string PrimaryClusterId { get; }
        public string FallbackClusterIds { get; }
        public string RequiredSlotSemantics { get; }
        public string CuePolicy { get; }
        public string ActivationPolicy { get; }
        public string RewardPolicy { get; }
        public string RecoveryPolicy { get; }
        public string RemovalPolicy { get; }
        public string StaticShellDigest { get; }
        public string RemovalSafetyDigest { get; }
        public string ClusterBindingDigest { get; }
        public string SlotDigest { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(ActivityId, SchemaVersion,
            SourceActivityId, ActivityKind, BiomeId, StrengthClass,
            Weight.ToString(CultureInfo.InvariantCulture), PrimaryClusterId,
            FallbackClusterIds, RequiredSlotSemantics, CuePolicy, ActivationPolicy,
            RewardPolicy, RecoveryPolicy, RemovalPolicy, StaticShellDigest,
            RemovalSafetyDigest, ClusterBindingDigest, SlotDigest);

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name);
            return value.Trim();
        }

        private static string Digest(string value, string name)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(value))
                throw new ArgumentException("Lower-hex SHA-256 required.", name);
            return value;
        }
    }

    public sealed class MoonPalaceEventMarkerRecord
    {
        public MoonPalaceEventMarkerRecord(string eventId, string markerId,
            string markerKind, int localX, int localY, string operation,
            string payloadIdentity, string targetSourceKind, string targetOwnerId,
            string targetSlotSemantic)
        {
            EventId = Require(eventId, nameof(eventId));
            MarkerId = Require(markerId, nameof(markerId));
            MarkerKind = Require(markerKind, nameof(markerKind));
            if (localX < 0) throw new ArgumentOutOfRangeException(nameof(localX));
            if (localY < 0) throw new ArgumentOutOfRangeException(nameof(localY));
            LocalX = localX;
            LocalY = localY;
            Operation = Require(operation, nameof(operation));
            PayloadIdentity = Require(payloadIdentity, nameof(payloadIdentity));
            TargetSourceKind = Require(targetSourceKind, nameof(targetSourceKind));
            TargetOwnerId = Require(targetOwnerId, nameof(targetOwnerId));
            TargetSlotSemantic = Require(targetSlotSemantic,
                nameof(targetSlotSemantic));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_05_EVENT_MARKER_V1", CanonicalLine,
            });
        }

        public string EventId { get; }
        public string MarkerId { get; }
        public string MarkerKind { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public string Operation { get; }
        public string PayloadIdentity { get; }
        public string TargetSourceKind { get; }
        public string TargetOwnerId { get; }
        public string TargetSlotSemantic { get; }
        public string ExecutionPolicy => "MarkerOnlyNoExecution";
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(EventId, MarkerId,
            MarkerKind, LocalX.ToString(CultureInfo.InvariantCulture),
            LocalY.ToString(CultureInfo.InvariantCulture), Operation, PayloadIdentity,
            TargetSourceKind, TargetOwnerId, TargetSlotSemantic, ExecutionPolicy);

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name);
            return value.Trim();
        }
    }

    public sealed class MoonPalaceActivityEventCompatibilityRecord
    {
        public MoonPalaceActivityEventCompatibilityRecord(string eventId,
            string activityId, string clusterScope, string requiredSlotSemantic,
            string compatibilityKind)
        {
            EventId = Require(eventId, nameof(eventId));
            ActivityId = activityId == null ? string.Empty : activityId.Trim();
            ClusterScope = Require(clusterScope, nameof(clusterScope));
            RequiredSlotSemantic = Require(requiredSlotSemantic,
                nameof(requiredSlotSemantic));
            CompatibilityKind = Require(compatibilityKind, nameof(compatibilityKind));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_05_EVENT_COMPATIBILITY_V1", CanonicalLine,
            });
        }

        public string EventId { get; }
        public string ActivityId { get; }
        public string ClusterScope { get; }
        public string RequiredSlotSemantic { get; }
        public string CompatibilityKind { get; }
        public bool StaticOnly => true;
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(EventId, ActivityId,
            ClusterScope, RequiredSlotSemantic, CompatibilityKind, "true");

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name);
            return value.Trim();
        }
    }

    public sealed class MoonPalaceEventOverlayProfile
    {
        public const string SchemaVersion = "map21_05.event_overlay_profile.v1";

        public MoonPalaceEventOverlayProfile(string eventId, string eventKind,
            string operation, int weight, int cooldownGap, int markerCount,
            IEnumerable<string> compatibleActivityIds, string compatibleClusterScope,
            string payloadIdentity, bool emptyVariant, string markerDigest,
            string compatibilityDigest)
        {
            EventId = Require(eventId, nameof(eventId));
            SourceEventId = EventId;
            EventKind = Require(eventKind, nameof(eventKind));
            Operation = Require(operation, nameof(operation));
            if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
            if (cooldownGap < 0) throw new ArgumentOutOfRangeException(nameof(cooldownGap));
            if (markerCount < 0) throw new ArgumentOutOfRangeException(nameof(markerCount));
            Weight = weight;
            CooldownGap = cooldownGap;
            MarkerCount = markerCount;
            CompatibleActivityIds = string.Join("|", (compatibleActivityIds ??
                Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal));
            CompatibleClusterScope = Require(compatibleClusterScope,
                nameof(compatibleClusterScope));
            PayloadIdentity = Require(payloadIdentity, nameof(payloadIdentity));
            EmptyVariant = emptyVariant;
            MarkerDigest = Digest(markerDigest, nameof(markerDigest));
            CompatibilityDigest = Digest(compatibilityDigest,
                nameof(compatibilityDigest));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_05_EVENT_PROFILE_V1", CanonicalLine,
            });
        }

        public string EventId { get; }
        public string SourceEventId { get; }
        public string EventKind { get; }
        public string Operation { get; }
        public int Weight { get; }
        public int CooldownGap { get; }
        public int MarkerCount { get; }
        public string CompatibleActivityIds { get; }
        public string CompatibleClusterScope { get; }
        public string PayloadIdentity { get; }
        public bool EmptyVariant { get; }
        public string MarkerDigest { get; }
        public string CompatibilityDigest { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(EventId, SchemaVersion,
            SourceEventId, EventKind, Operation,
            Weight.ToString(CultureInfo.InvariantCulture),
            CooldownGap.ToString(CultureInfo.InvariantCulture),
            MarkerCount.ToString(CultureInfo.InvariantCulture), CompatibleActivityIds,
            CompatibleClusterScope, PayloadIdentity, EmptyVariant ? "true" : "false",
            MarkerDigest, CompatibilityDigest);

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name);
            return value.Trim();
        }

        private static string Digest(string value, string name)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(value))
                throw new ArgumentException("Lower-hex SHA-256 required.", name);
            return value;
        }
    }

    public sealed class MoonPalaceActivityEventProduction
    {
        public const string SchemaVersion = "map21_05.activity_event_production.v1";
        private static readonly string[] RequiredActivities =
        {
            "ACT_CRATER_BOULDER_CHAIN", "ACT_CRATER_RICOCHET_MINE",
            "ACT_DOUGH_TIME_TRIAL", "ACT_MARU_REWIND_ANOMALY",
            "ACT_MILL_ESCORT_CART", "ACT_MILL_GEAR_GRID",
            "ACT_MILL_PESTLE_WORKSHOP",
        };
        private static readonly string[] RequiredEvents =
        {
            "EVT_EMPTY", "EVT_MARU_INTERVENTION", "EVT_METEOR_FALL",
            "EVT_RARE_CREATURE", "EVT_WANDERING_MERCHANT",
        };
        private static readonly IReadOnlyDictionary<string, string> RequiredKinds =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "ACT_CRATER_BOULDER_CHAIN", "BoulderChain" },
                { "ACT_CRATER_RICOCHET_MINE", "RicochetMine" },
                { "ACT_DOUGH_TIME_TRIAL", "TimeTrial" },
                { "ACT_MARU_REWIND_ANOMALY", "MaruRewindAnomaly" },
                { "ACT_MILL_ESCORT_CART", "EscortCart" },
                { "ACT_MILL_GEAR_GRID", "GearGrid" },
                { "ACT_MILL_PESTLE_WORKSHOP", "PestleWorkshop" },
            };

        private readonly ReadOnlyCollection<MoonPalaceActivityProfile> activities;
        private readonly ReadOnlyCollection<MoonPalaceActivityClusterBinding> bindings;
        private readonly ReadOnlyCollection<MoonPalaceActivitySlotRecord> slots;
        private readonly ReadOnlyCollection<MoonPalaceActivityRemovalSafetyRecord> removals;
        private readonly ReadOnlyCollection<MoonPalaceEventOverlayProfile> events;
        private readonly ReadOnlyCollection<MoonPalaceEventMarkerRecord> markers;
        private readonly ReadOnlyCollection<MoonPalaceActivityEventCompatibilityRecord>
            compatibility;
        private readonly ReadOnlyCollection<MoonPalaceActivityClusterDescriptor> clusters;

        public MoonPalaceActivityEventProduction(
            IEnumerable<MoonPalaceActivityProfile> sourceActivities,
            IEnumerable<MoonPalaceActivityClusterBinding> sourceBindings,
            IEnumerable<MoonPalaceActivitySlotRecord> sourceSlots,
            IEnumerable<MoonPalaceActivityRemovalSafetyRecord> sourceRemovals,
            IEnumerable<MoonPalaceEventOverlayProfile> sourceEvents,
            IEnumerable<MoonPalaceEventMarkerRecord> sourceMarkers,
            IEnumerable<MoonPalaceActivityEventCompatibilityRecord> sourceCompatibility,
            IEnumerable<MoonPalaceActivityClusterDescriptor> sourceClusters,
            string createdUtc)
        {
            activities = Ordered(sourceActivities, value => value.ActivityId,
                nameof(sourceActivities));
            bindings = Ordered(sourceBindings, value => value.ActivityId,
                nameof(sourceBindings));
            slots = Ordered(sourceSlots, value => value.ActivityId + "/" + value.SlotId,
                nameof(sourceSlots));
            removals = Ordered(sourceRemovals, value => value.ActivityId,
                nameof(sourceRemovals));
            events = Ordered(sourceEvents, value => value.EventId, nameof(sourceEvents));
            markers = Ordered(sourceMarkers, value => value.EventId + "/" + value.MarkerId,
                nameof(sourceMarkers));
            compatibility = Ordered(sourceCompatibility,
                value => value.EventId + "/" + value.ActivityId + "/" +
                    value.ClusterScope + "/" + value.RequiredSlotSemantic,
                nameof(sourceCompatibility));
            clusters = Ordered(sourceClusters, value => value.ClusterId,
                nameof(sourceClusters));
            CreatedUtc = createdUtc ?? string.Empty;
            Validate();
            ActivityProfileDigest = SetDigest("MAP21_05_ACTIVITY_PROFILE_SET_V1",
                activities.Select(value => value.CanonicalLine));
            ActivityBindingDigest = SetDigest("MAP21_05_ACTIVITY_BINDING_SET_V1",
                bindings.Select(value => value.CanonicalLine));
            ActivitySlotDigest = SetDigest("MAP21_05_ACTIVITY_SLOT_SET_V1",
                slots.Select(value => value.CanonicalLine));
            RemovalSafetyDigest = SetDigest("MAP21_05_REMOVAL_SAFETY_SET_V1",
                removals.Select(value => value.CanonicalLine));
            EventProfileDigest = SetDigest("MAP21_05_EVENT_PROFILE_SET_V1",
                events.Select(value => value.CanonicalLine));
            EventMarkerDigest = SetDigest("MAP21_05_EVENT_MARKER_SET_V1",
                markers.Select(value => value.CanonicalLine));
            CompatibilityDigest = SetDigest("MAP21_05_COMPATIBILITY_SET_V1",
                compatibility.Select(value => value.CanonicalLine));
            ActivityEventManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_05_ACTIVITY_EVENT_MANIFEST_V1", SchemaVersion,
                MoonPalaceActivityEventPreconditions.SourceMap12AggregateDigest,
                MoonPalaceActivityEventPreconditions.SourceMap2104AllBiomeClusterDigest,
                ActivityProfileDigest, ActivityBindingDigest, ActivitySlotDigest,
                "created_utc_excluded=true",
            });
            RemovalSafetyManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_05_REMOVAL_SAFETY_MANIFEST_V1", SchemaVersion,
                RemovalSafetyDigest, "created_utc_excluded=true",
            });
            EventOverlayManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_05_EVENT_OVERLAY_MANIFEST_V1", SchemaVersion,
                EventProfileDigest, EventMarkerDigest, CompatibilityDigest,
                "created_utc_excluded=true",
            });
        }

        public static IReadOnlyList<string> RequiredActivityIds => RequiredActivities;
        public static IReadOnlyList<string> RequiredEventIds => RequiredEvents;
        public IReadOnlyList<MoonPalaceActivityProfile> Activities => activities;
        public IReadOnlyList<MoonPalaceActivityClusterBinding> Bindings => bindings;
        public IReadOnlyList<MoonPalaceActivitySlotRecord> Slots => slots;
        public IReadOnlyList<MoonPalaceActivityRemovalSafetyRecord> Removals => removals;
        public IReadOnlyList<MoonPalaceEventOverlayProfile> Events => events;
        public IReadOnlyList<MoonPalaceEventMarkerRecord> Markers => markers;
        public IReadOnlyList<MoonPalaceActivityEventCompatibilityRecord> Compatibility =>
            compatibility;
        public IReadOnlyList<MoonPalaceActivityClusterDescriptor> Clusters => clusters;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string ActivityProfileDigest { get; }
        public string ActivityBindingDigest { get; }
        public string ActivitySlotDigest { get; }
        public string RemovalSafetyDigest { get; }
        public string EventProfileDigest { get; }
        public string EventMarkerDigest { get; }
        public string CompatibilityDigest { get; }
        public string ActivityEventManifestDigest { get; }
        public string RemovalSafetyManifestDigest { get; }
        public string EventOverlayManifestDigest { get; }
        public bool CanExecutePayload(string eventId) => false;

        public void RequestPayloadExecution(string eventId)
        {
            throw new InvalidOperationException(
                "MAP21_05 publishes marker identities only; payload execution is forbidden.");
        }

        public string SerializeActivityProfilesCsv() => Csv(
            "activity_id,schema_version,source_activity_id,activity_kind,biome_id,strength_class,weight,primary_cluster_id,fallback_cluster_ids,required_slot_semantics,cue_policy,activation_policy,reward_policy,recovery_policy,removal_policy,static_shell_digest,removal_safety_digest,cluster_binding_digest,slot_digest,canonical_digest",
            activities.Select(value => Row(value.ActivityId,
                MoonPalaceActivityProfile.SchemaVersion, value.SourceActivityId,
                value.ActivityKind, value.BiomeId, value.StrengthClass,
                value.Weight.ToString(CultureInfo.InvariantCulture), value.PrimaryClusterId,
                value.FallbackClusterIds, value.RequiredSlotSemantics, value.CuePolicy,
                value.ActivationPolicy, value.RewardPolicy, value.RecoveryPolicy,
                value.RemovalPolicy, value.StaticShellDigest, value.RemovalSafetyDigest,
                value.ClusterBindingDigest, value.SlotDigest, value.CanonicalDigest)));

        public string SerializeBindingsCsv() => Csv(
            "activity_id,biome_id,primary_cluster_id,fallback_cluster_id,primary_pool_kind,fallback_pool_kind,binding_role,cluster_binding_digest",
            bindings.Select(value => Row(value.ActivityId, value.BiomeId,
                value.PrimaryClusterId, value.FallbackClusterId, value.PrimaryPoolKind,
                value.FallbackPoolKind, value.BindingRole, value.CanonicalDigest)));

        public string SerializeSlotsCsv() => Csv(
            "activity_id,slot_id,slot_semantic,local_x,local_y,source_slot_id,marker_payload_id,execution_policy,slot_digest",
            slots.Select(value => Row(value.ActivityId, value.SlotId, value.SlotSemantic,
                value.LocalX.ToString(CultureInfo.InvariantCulture),
                value.LocalY.ToString(CultureInfo.InvariantCulture), value.SourceSlotId,
                value.MarkerPayloadId, value.ExecutionPolicy, value.CanonicalDigest)));

        public string SerializeEventProfilesCsv() => Csv(
            "event_id,schema_version,source_event_id,event_kind,operation,weight,cooldown_gap,marker_count,compatible_activity_ids,compatible_cluster_scope,payload_identity,empty_variant,marker_digest,compatibility_digest,canonical_digest",
            events.Select(value => Row(value.EventId,
                MoonPalaceEventOverlayProfile.SchemaVersion, value.SourceEventId,
                value.EventKind, value.Operation,
                value.Weight.ToString(CultureInfo.InvariantCulture),
                value.CooldownGap.ToString(CultureInfo.InvariantCulture),
                value.MarkerCount.ToString(CultureInfo.InvariantCulture),
                value.CompatibleActivityIds, value.CompatibleClusterScope,
                value.PayloadIdentity, value.EmptyVariant ? "true" : "false",
                value.MarkerDigest, value.CompatibilityDigest, value.CanonicalDigest)));

        public string SerializeEventMarkersCsv() => Csv(
            "event_id,marker_id,marker_kind,local_x,local_y,operation,payload_identity,target_source_kind,target_owner_id,target_slot_semantic,execution_policy,marker_digest",
            markers.Select(value => Row(value.EventId, value.MarkerId, value.MarkerKind,
                value.LocalX.ToString(CultureInfo.InvariantCulture),
                value.LocalY.ToString(CultureInfo.InvariantCulture), value.Operation,
                value.PayloadIdentity, value.TargetSourceKind, value.TargetOwnerId,
                value.TargetSlotSemantic, value.ExecutionPolicy, value.CanonicalDigest)));

        public string SerializeCompatibilityCsv() => Csv(
            "event_id,activity_id,cluster_scope,required_slot_semantic,compatibility_kind,static_only,compatibility_digest",
            compatibility.Select(value => Row(value.EventId, value.ActivityId,
                value.ClusterScope, value.RequiredSlotSemantic, value.CompatibilityKind,
                "true", value.CanonicalDigest)));

        public string SerializeActivityEventManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceActivityEventManifestDocument.From(this));
        public string SerializeRemovalSafetyManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceRemovalSafetyManifestDocument.From(this));
        public string SerializeEventOverlayManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceEventOverlayManifestDocument.From(this));

        private void Validate()
        {
            RequireExact(activities.Select(value => value.ActivityId), RequiredActivities,
                "Activity");
            RequireExact(events.Select(value => value.EventId), RequiredEvents, "Event");
            RequireUnique(bindings.Select(value => value.ActivityId), "binding");
            RequireUnique(removals.Select(value => value.ActivityId), "removal proof");
            RequireUnique(clusters.Select(value => value.ClusterId), "cluster");
            var clusterById = clusters.ToDictionary(value => value.ClusterId,
                StringComparer.Ordinal);
            if (clusters.Count != 48)
                throw new ArgumentException("Exact MAP21_04 48-cluster manifest required.");

            foreach (var activity in activities)
            {
                if (activity.SourceActivityId != activity.ActivityId ||
                    !RequiredKinds.TryGetValue(activity.ActivityId, out var expectedKind) ||
                    activity.ActivityKind != expectedKind || activity.BiomeId == "CassiaRoot")
                    throw new ArgumentException("Activity identity/kind/biome mismatch.");
                var binding = bindings.SingleOrDefault(value =>
                    value.ActivityId == activity.ActivityId);
                if (binding == null || binding.CanonicalDigest !=
                        activity.ClusterBindingDigest ||
                    !clusterById.TryGetValue(binding.PrimaryClusterId, out var primary) ||
                    !clusterById.TryGetValue(binding.FallbackClusterId, out var fallback) ||
                    primary.BiomeId != activity.BiomeId ||
                    fallback.BiomeId != activity.BiomeId ||
                    primary.PoolKind != "Terrain" || fallback.PoolKind != "Terrain")
                    throw new ArgumentException(
                        "Activity clusters must be known same-biome Terrain records.");
                var activitySlots = slots.Where(value =>
                    value.ActivityId == activity.ActivityId).ToArray();
                var semantics = new HashSet<string>(activitySlots.Select(value =>
                    value.SlotSemantic), StringComparer.Ordinal);
                foreach (var required in new[]
                {
                    "Cue", "Trigger", "Reward", "SafePocket", "Recovery", "Reset",
                    "ExitPreservation",
                })
                    if (!semantics.Contains(required))
                        throw new ArgumentException("Missing required Activity slot: " + required);
                if (!semantics.Contains("Device") && !semantics.Contains("Hazard"))
                    throw new ArgumentException("Missing Activity core slot.");
                var slotDigest = SetDigest("MAP21_05_ACTIVITY_SLOT_SET_V1",
                    activitySlots.Select(value => value.CanonicalLine));
                var removal = removals.SingleOrDefault(value =>
                    value.ActivityId == activity.ActivityId);
                if (activity.SlotDigest != slotDigest || removal == null ||
                    activity.RemovalSafetyDigest != removal.CanonicalDigest ||
                    activity.StaticShellDigest != removal.StaticShellDigestBeforeRemoval ||
                    removal.StaticShellDigestBeforeRemoval !=
                        removal.StaticShellDigestAfterRemoval ||
                    removal.CriticalTargetDigestBeforeRemoval !=
                        removal.CriticalTargetDigestAfterRemoval ||
                    !removal.OverlayRemovedOnly || !removal.ResetExitPreserved)
                    throw new ArgumentException("Activity removal proof mismatch.");
            }

            if (slots.Any(value => !RequiredActivities.Contains(value.ActivityId)) ||
                compatibility.Any(value => value.ActivityId.Length != 0 &&
                    !RequiredActivities.Contains(value.ActivityId)))
                throw new ArgumentException("Unknown Activity reference.");
            var empty = events.Where(value => value.EmptyVariant).ToArray();
            if (empty.Length != 1 || empty[0].EventId != "EVT_EMPTY" ||
                empty[0].Weight != 0 || empty[0].CooldownGap != 0 ||
                empty[0].MarkerCount != 0 || empty[0].PayloadIdentity != "none" ||
                empty[0].Operation != "none")
                throw new ArgumentException("Exactly one zero-valued Empty event is required.");
            foreach (var overlay in events)
            {
                var eventMarkers = markers.Where(value => value.EventId ==
                    overlay.EventId).ToArray();
                var eventCompatibility = compatibility.Where(value => value.EventId ==
                    overlay.EventId).ToArray();
                if (overlay.SourceEventId != overlay.EventId ||
                    eventMarkers.Length != overlay.MarkerCount ||
                    (!overlay.EmptyVariant && eventMarkers.Length != 1) ||
                    eventMarkers.Any(value => value.ExecutionPolicy !=
                        "MarkerOnlyNoExecution") ||
                    overlay.MarkerDigest != SetDigest("MAP21_05_EVENT_MARKER_SET_V1",
                        eventMarkers.Select(value => value.CanonicalLine)) ||
                    overlay.CompatibilityDigest != SetDigest(
                        "MAP21_05_EVENT_COMPATIBILITY_SET_V1",
                        eventCompatibility.Select(value => value.CanonicalLine)))
                    throw new ArgumentException("Event marker/compatibility mismatch.");
            }
            RequireEventCompatibility();
        }

        private void RequireEventCompatibility()
        {
            var meteor = compatibility.Where(value => value.EventId ==
                "EVT_METEOR_FALL").ToArray();
            if (meteor.Length != 1 || meteor[0].ActivityId.Length != 0 ||
                meteor[0].ClusterScope != "Terrain:CORE" ||
                meteor[0].RequiredSlotSemantic != "CORE")
                throw new ArgumentException("Meteor must target Terrain CORE only.");
            foreach (var eventId in new[] { "EVT_WANDERING_MERCHANT", "EVT_RARE_CREATURE" })
            {
                var rows = compatibility.Where(value => value.EventId == eventId).ToArray();
                if (rows.Length != 1 || rows[0].ActivityId != "ACT_MILL_ESCORT_CART" ||
                    rows[0].RequiredSlotSemantic != "Npc")
                    throw new ArgumentException("NPC event compatibility mismatch.");
            }
            var maru = compatibility.Where(value => value.EventId ==
                "EVT_MARU_INTERVENTION").ToArray();
            if (maru.Length != 1 || maru[0].ActivityId !=
                    "ACT_MARU_REWIND_ANOMALY" ||
                maru[0].RequiredSlotSemantic != "Device")
                throw new ArgumentException("Maru event compatibility mismatch.");
            var empty = compatibility.Where(value => value.EventId == "EVT_EMPTY").ToArray();
            if (!RequiredActivities.All(id => empty.Any(value => value.ActivityId == id)) ||
                !empty.Any(value => value.ActivityId.Length == 0 &&
                    value.ClusterScope == "UnassignedClusters"))
                throw new ArgumentException("Empty fallback compatibility is incomplete.");
        }

        private static ReadOnlyCollection<T> Ordered<T>(IEnumerable<T> source,
            Func<T, string> key, string name) where T : class
        {
            var values = (source ?? throw new ArgumentNullException(name)).ToArray();
            if (values.Any(value => value == null)) throw new ArgumentException(name);
            return new ReadOnlyCollection<T>(values.OrderBy(key,
                StringComparer.Ordinal).ToArray());
        }

        private static void RequireExact(IEnumerable<string> actual,
            IEnumerable<string> expected, string label)
        {
            if (!actual.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(
                    expected.OrderBy(value => value, StringComparer.Ordinal),
                    StringComparer.Ordinal))
                throw new ArgumentException("Exact " + label + " inventory required.");
        }

        private static void RequireUnique(IEnumerable<string> values, string label)
        {
            var source = values.ToArray();
            if (source.Distinct(StringComparer.Ordinal).Count() != source.Length)
                throw new ArgumentException("Duplicate " + label + " id.");
        }

        internal static string SetDigest(string prefix, IEnumerable<string> lines) =>
            BakingCanonicalDigest.HashCanonicalLines(new[] { prefix }.Concat(
                (lines ?? Array.Empty<string>()).OrderBy(value => value,
                    StringComparer.Ordinal)));

        private static string Csv(string header, IEnumerable<string> rows) =>
            string.Join("\n", new[] { header }.Concat(rows)) + "\n";

        private static string Row(params string[] values) => string.Join(",",
            values.Select(Escape));

        private static string Escape(string value)
        {
            var text = value ?? string.Empty;
            return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? text :
                "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }

    public sealed class MoonPalaceActivityEventDigestManifest
    {
        public const string SchemaVersion = "map21_05.activity_event_digest_manifest.v1";
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> csvDigests;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> jsonDigests;

        public MoonPalaceActivityEventDigestManifest(
            MoonPalaceActivityEventProduction production,
            IEnumerable<MoonPalaceNamedDigest> sourceCsvDigests,
            IEnumerable<MoonPalaceNamedDigest> sourceJsonDigests, string createdUtc)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            csvDigests = CopyDigests(sourceCsvDigests, 6, "CSV");
            jsonDigests = CopyDigests(sourceJsonDigests, 3, "JSON");
            CreatedUtc = createdUtc ?? string.Empty;
            Map2106HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_06_ACTIVITY_EVENT_HANDOFF_V1",
                MoonPalaceActivityEventPreconditions.SourceMap2105HandoffDigest,
                MoonPalaceActivityEventPreconditions.SourceMap12AggregateDigest,
                MoonPalaceActivityEventPreconditions.SourceMap2104AllBiomeClusterDigest,
                Production.ActivityEventManifestDigest,
                Production.RemovalSafetyManifestDigest,
                Production.EventOverlayManifestDigest,
            }.Concat(csvDigests.Select(value => value.CanonicalLine))
                .Concat(jsonDigests.Select(value => value.CanonicalLine)));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                SchemaVersion, MoonPalaceActivityEventPreconditions.TaskId,
                MoonPalaceActivityEventPreconditions.SourceMap12AggregateDigest,
                MoonPalaceActivityEventPreconditions.SourceMap12ActivityCatalogDigest,
                MoonPalaceActivityEventPreconditions.SourceMap12EventCatalogDigest,
                MoonPalaceActivityEventPreconditions.SourceMap2104AllBiomeClusterDigest,
                MoonPalaceActivityEventPreconditions.SourceMap2104DigestManifestDigest,
                Map2106HandoffDigest, "created_utc_excluded=true",
            }.Concat(csvDigests.Select(value => value.CanonicalLine))
                .Concat(jsonDigests.Select(value => value.CanonicalLine)));
        }

        public MoonPalaceActivityEventProduction Production { get; }
        public IReadOnlyList<MoonPalaceNamedDigest> CsvDigests => csvDigests;
        public IReadOnlyList<MoonPalaceNamedDigest> JsonDigests => jsonDigests;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string Map2106HandoffDigest { get; }
        public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(
            MoonPalaceActivityEventDigestManifestDocument.From(this));

        private static ReadOnlyCollection<MoonPalaceNamedDigest> CopyDigests(
            IEnumerable<MoonPalaceNamedDigest> source, int expected, string label)
        {
            var values = (source ?? throw new ArgumentNullException(nameof(source)))
                .OrderBy(value => value.Name, StringComparer.Ordinal).ToArray();
            if (values.Length != expected || values.Any(value => value == null) ||
                values.Select(value => value.Name).Distinct(StringComparer.Ordinal).Count() !=
                    expected)
                throw new ArgumentException("Exact " + label + " digest inventory required.");
            return new ReadOnlyCollection<MoonPalaceNamedDigest>(values);
        }
    }

    public sealed class MoonPalaceNamedDigest
    {
        public MoonPalaceNamedDigest(string name, string digest)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
            if (!BakingCanonicalDigest.IsLowerHexSha256(digest))
                throw new ArgumentException(nameof(digest));
            Name = name.Trim();
            Digest = digest;
        }

        public string Name { get; }
        public string Digest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(Name, Digest);
    }

    public sealed class MoonPalaceActivityEventForbiddenOperationCounters
    {
        public static MoonPalaceActivityEventForbiddenOperationCounters Zero =>
            new MoonPalaceActivityEventForbiddenOperationCounters();
        public int ActivityStateMachineExecutions => 0;
        public int EventPayloadExecutions => 0;
        public int GenerationRunnerExecutions => 0;
        public int RendererExecutions => 0;
        public int ValidationRunnerExecutions => 0;
        public int ReplayExecutions => 0;
        public int RollbackExecutions => 0;
        public int TilemapWrites => 0;
        public int RuntimeObjectSpawns => 0;
        public int ScenePrefabChanges => 0;
        public int PriorCategorySelections => 0;
        public int PlayModeSelections => 0;
        public int LegacyRegressionSelections => 0;
        public int UnfilteredOrFullRegressionSelections => 0;
        public bool AllZero => ActivityStateMachineExecutions == 0 &&
            EventPayloadExecutions == 0 && GenerationRunnerExecutions == 0 &&
            RendererExecutions == 0 && ValidationRunnerExecutions == 0 &&
            ReplayExecutions == 0 && RollbackExecutions == 0 && TilemapWrites == 0 &&
            RuntimeObjectSpawns == 0 && ScenePrefabChanges == 0 &&
            PriorCategorySelections == 0 && PlayModeSelections == 0 &&
            LegacyRegressionSelections == 0 &&
            UnfilteredOrFullRegressionSelections == 0;
    }

    [Serializable]
    internal sealed class MoonPalaceActivityProfileDocument
    {
        public string activity_id;
        public string schema_version;
        public string source_activity_id;
        public string activity_kind;
        public string biome_id;
        public string strength_class;
        public int weight;
        public string primary_cluster_id;
        public string fallback_cluster_ids;
        public string required_slot_semantics;
        public string cue_policy;
        public string activation_policy;
        public string reward_policy;
        public string recovery_policy;
        public string removal_policy;
        public string static_shell_digest;
        public string removal_safety_digest;
        public string cluster_binding_digest;
        public string slot_digest;
        public string canonical_digest;

        public static MoonPalaceActivityProfileDocument From(MoonPalaceActivityProfile value) =>
            new MoonPalaceActivityProfileDocument
            {
                activity_id = value.ActivityId,
                schema_version = MoonPalaceActivityProfile.SchemaVersion,
                source_activity_id = value.SourceActivityId,
                activity_kind = value.ActivityKind,
                biome_id = value.BiomeId,
                strength_class = value.StrengthClass,
                weight = value.Weight,
                primary_cluster_id = value.PrimaryClusterId,
                fallback_cluster_ids = value.FallbackClusterIds,
                required_slot_semantics = value.RequiredSlotSemantics,
                cue_policy = value.CuePolicy,
                activation_policy = value.ActivationPolicy,
                reward_policy = value.RewardPolicy,
                recovery_policy = value.RecoveryPolicy,
                removal_policy = value.RemovalPolicy,
                static_shell_digest = value.StaticShellDigest,
                removal_safety_digest = value.RemovalSafetyDigest,
                cluster_binding_digest = value.ClusterBindingDigest,
                slot_digest = value.SlotDigest,
                canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceActivityBindingDocument
    {
        public string activity_id;
        public string biome_id;
        public string primary_cluster_id;
        public string fallback_cluster_id;
        public string primary_pool_kind;
        public string fallback_pool_kind;
        public string binding_role;
        public string canonical_digest;
        public static MoonPalaceActivityBindingDocument From(
            MoonPalaceActivityClusterBinding value) => new MoonPalaceActivityBindingDocument
            {
                activity_id = value.ActivityId, biome_id = value.BiomeId,
                primary_cluster_id = value.PrimaryClusterId,
                fallback_cluster_id = value.FallbackClusterId,
                primary_pool_kind = value.PrimaryPoolKind,
                fallback_pool_kind = value.FallbackPoolKind,
                binding_role = value.BindingRole, canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceActivitySlotDocument
    {
        public string activity_id;
        public string slot_id;
        public string slot_semantic;
        public int local_x;
        public int local_y;
        public string source_slot_id;
        public string marker_payload_id;
        public string execution_policy;
        public string canonical_digest;
        public static MoonPalaceActivitySlotDocument From(MoonPalaceActivitySlotRecord value) =>
            new MoonPalaceActivitySlotDocument
            {
                activity_id = value.ActivityId, slot_id = value.SlotId,
                slot_semantic = value.SlotSemantic, local_x = value.LocalX,
                local_y = value.LocalY, source_slot_id = value.SourceSlotId,
                marker_payload_id = value.MarkerPayloadId,
                execution_policy = value.ExecutionPolicy,
                canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceActivityEventManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP12_aggregate_digest;
        public string source_MAP21_04_all_biome_cluster_digest;
        public MoonPalaceActivityProfileDocument[] activity_profiles;
        public MoonPalaceActivityBindingDocument[] cluster_bindings;
        public MoonPalaceActivitySlotDocument[] activity_slots;
        public int activity_profile_count;
        public int activity_binding_count;
        public int activity_slot_count;
        public string activity_profile_digest;
        public string activity_binding_digest;
        public string activity_slot_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;
        public static MoonPalaceActivityEventManifestDocument From(
            MoonPalaceActivityEventProduction value) =>
            new MoonPalaceActivityEventManifestDocument
            {
                schema_version = MoonPalaceActivityEventProduction.SchemaVersion,
                task_id = MoonPalaceActivityEventPreconditions.TaskId,
                source_MAP12_aggregate_digest =
                    MoonPalaceActivityEventPreconditions.SourceMap12AggregateDigest,
                source_MAP21_04_all_biome_cluster_digest =
                    MoonPalaceActivityEventPreconditions.SourceMap2104AllBiomeClusterDigest,
                activity_profiles = value.Activities.Select(
                    MoonPalaceActivityProfileDocument.From).ToArray(),
                cluster_bindings = value.Bindings.Select(
                    MoonPalaceActivityBindingDocument.From).ToArray(),
                activity_slots = value.Slots.Select(MoonPalaceActivitySlotDocument.From)
                    .ToArray(),
                activity_profile_count = value.Activities.Count,
                activity_binding_count = value.Bindings.Count,
                activity_slot_count = value.Slots.Count,
                activity_profile_digest = value.ActivityProfileDigest,
                activity_binding_digest = value.ActivityBindingDigest,
                activity_slot_digest = value.ActivitySlotDigest,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.ActivityEventManifestDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceRemovalSafetyRecordDocument
    {
        public string activity_id;
        public string static_shell_digest_before_removal;
        public string static_shell_digest_after_removal;
        public string critical_target_digest_before_removal;
        public string critical_target_digest_after_removal;
        public int safe_pocket_marker_count;
        public int recovery_marker_count;
        public bool overlay_removed_only;
        public bool reset_exit_preserved;
        public int static_tile_delta_count;
        public int runtime_mutation_count;
        public string canonical_digest;
        public static MoonPalaceRemovalSafetyRecordDocument From(
            MoonPalaceActivityRemovalSafetyRecord value) =>
            new MoonPalaceRemovalSafetyRecordDocument
            {
                activity_id = value.ActivityId,
                static_shell_digest_before_removal = value.StaticShellDigestBeforeRemoval,
                static_shell_digest_after_removal = value.StaticShellDigestAfterRemoval,
                critical_target_digest_before_removal =
                    value.CriticalTargetDigestBeforeRemoval,
                critical_target_digest_after_removal =
                    value.CriticalTargetDigestAfterRemoval,
                safe_pocket_marker_count = value.SafePocketMarkerCount,
                recovery_marker_count = value.RecoveryMarkerCount,
                overlay_removed_only = value.OverlayRemovedOnly,
                reset_exit_preserved = value.ResetExitPreserved,
                static_tile_delta_count = value.StaticTileDeltaCount,
                runtime_mutation_count = value.RuntimeMutationCount,
                canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceRemovalSafetyManifestDocument
    {
        public string schema_version;
        public string task_id;
        public MoonPalaceRemovalSafetyRecordDocument[] removal_safety_proofs;
        public int proof_count;
        public int preserved_static_shell_count;
        public int preserved_critical_target_count;
        public int static_tile_delta_count;
        public int runtime_mutation_count;
        public string removal_safety_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;
        public static MoonPalaceRemovalSafetyManifestDocument From(
            MoonPalaceActivityEventProduction value) =>
            new MoonPalaceRemovalSafetyManifestDocument
            {
                schema_version = "map21_05.removal_safety_manifest.v1",
                task_id = MoonPalaceActivityEventPreconditions.TaskId,
                removal_safety_proofs = value.Removals.Select(
                    MoonPalaceRemovalSafetyRecordDocument.From).ToArray(),
                proof_count = value.Removals.Count,
                preserved_static_shell_count = value.Removals.Count,
                preserved_critical_target_count = value.Removals.Count,
                static_tile_delta_count = 0, runtime_mutation_count = 0,
                removal_safety_digest = value.RemovalSafetyDigest,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.RemovalSafetyManifestDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceEventProfileDocument
    {
        public string event_id;
        public string schema_version;
        public string source_event_id;
        public string event_kind;
        public string operation;
        public int weight;
        public int cooldown_gap;
        public int marker_count;
        public string compatible_activity_ids;
        public string compatible_cluster_scope;
        public string payload_identity;
        public bool empty_variant;
        public string marker_digest;
        public string compatibility_digest;
        public string canonical_digest;
        public static MoonPalaceEventProfileDocument From(MoonPalaceEventOverlayProfile value) =>
            new MoonPalaceEventProfileDocument
            {
                event_id = value.EventId,
                schema_version = MoonPalaceEventOverlayProfile.SchemaVersion,
                source_event_id = value.SourceEventId, event_kind = value.EventKind,
                operation = value.Operation, weight = value.Weight,
                cooldown_gap = value.CooldownGap, marker_count = value.MarkerCount,
                compatible_activity_ids = value.CompatibleActivityIds,
                compatible_cluster_scope = value.CompatibleClusterScope,
                payload_identity = value.PayloadIdentity,
                empty_variant = value.EmptyVariant, marker_digest = value.MarkerDigest,
                compatibility_digest = value.CompatibilityDigest,
                canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceEventMarkerDocument
    {
        public string event_id;
        public string marker_id;
        public string marker_kind;
        public int local_x;
        public int local_y;
        public string operation;
        public string payload_identity;
        public string target_source_kind;
        public string target_owner_id;
        public string target_slot_semantic;
        public string execution_policy;
        public string canonical_digest;
        public static MoonPalaceEventMarkerDocument From(MoonPalaceEventMarkerRecord value) =>
            new MoonPalaceEventMarkerDocument
            {
                event_id = value.EventId, marker_id = value.MarkerId,
                marker_kind = value.MarkerKind, local_x = value.LocalX,
                local_y = value.LocalY, operation = value.Operation,
                payload_identity = value.PayloadIdentity,
                target_source_kind = value.TargetSourceKind,
                target_owner_id = value.TargetOwnerId,
                target_slot_semantic = value.TargetSlotSemantic,
                execution_policy = value.ExecutionPolicy,
                canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceCompatibilityDocument
    {
        public string event_id;
        public string activity_id;
        public string cluster_scope;
        public string required_slot_semantic;
        public string compatibility_kind;
        public bool static_only;
        public string canonical_digest;
        public static MoonPalaceCompatibilityDocument From(
            MoonPalaceActivityEventCompatibilityRecord value) =>
            new MoonPalaceCompatibilityDocument
            {
                event_id = value.EventId, activity_id = value.ActivityId,
                cluster_scope = value.ClusterScope,
                required_slot_semantic = value.RequiredSlotSemantic,
                compatibility_kind = value.CompatibilityKind,
                static_only = value.StaticOnly, canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceEventOverlayManifestDocument
    {
        public string schema_version;
        public string task_id;
        public MoonPalaceEventProfileDocument[] event_profiles;
        public MoonPalaceEventMarkerDocument[] marker_records;
        public MoonPalaceCompatibilityDocument[] compatibility_records;
        public int event_profile_count;
        public int non_empty_marker_count;
        public int empty_variant_count;
        public int event_payload_execution_count;
        public string event_profile_digest;
        public string event_marker_digest;
        public string compatibility_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;
        public static MoonPalaceEventOverlayManifestDocument From(
            MoonPalaceActivityEventProduction value) =>
            new MoonPalaceEventOverlayManifestDocument
            {
                schema_version = "map21_05.event_overlay_manifest.v1",
                task_id = MoonPalaceActivityEventPreconditions.TaskId,
                event_profiles = value.Events.Select(MoonPalaceEventProfileDocument.From)
                    .ToArray(),
                marker_records = value.Markers.Select(MoonPalaceEventMarkerDocument.From)
                    .ToArray(),
                compatibility_records = value.Compatibility.Select(
                    MoonPalaceCompatibilityDocument.From).ToArray(),
                event_profile_count = value.Events.Count,
                non_empty_marker_count = value.Markers.Count,
                empty_variant_count = value.Events.Count(item => item.EmptyVariant),
                event_payload_execution_count = 0,
                event_profile_digest = value.EventProfileDigest,
                event_marker_digest = value.EventMarkerDigest,
                compatibility_digest = value.CompatibilityDigest,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.EventOverlayManifestDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceNamedDigestDocument
    {
        public string name;
        public string digest;
        public static MoonPalaceNamedDigestDocument From(MoonPalaceNamedDigest value) =>
            new MoonPalaceNamedDigestDocument { name = value.Name, digest = value.Digest };
    }

    [Serializable]
    internal sealed class MoonPalaceActivityEventDigestManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP12_aggregate_digest;
        public string source_MAP12_activity_catalog_digest;
        public string source_MAP12_event_catalog_digest;
        public string source_MAP21_04_all_biome_cluster_digest;
        public string source_MAP21_04_digest_manifest_digest;
        public MoonPalaceNamedDigestDocument[] csv_digests;
        public MoonPalaceNamedDigestDocument[] json_digests;
        public string MAP21_06_handoff_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;
        public static MoonPalaceActivityEventDigestManifestDocument From(
            MoonPalaceActivityEventDigestManifest value) =>
            new MoonPalaceActivityEventDigestManifestDocument
            {
                schema_version = MoonPalaceActivityEventDigestManifest.SchemaVersion,
                task_id = MoonPalaceActivityEventPreconditions.TaskId,
                source_MAP12_aggregate_digest =
                    MoonPalaceActivityEventPreconditions.SourceMap12AggregateDigest,
                source_MAP12_activity_catalog_digest =
                    MoonPalaceActivityEventPreconditions.SourceMap12ActivityCatalogDigest,
                source_MAP12_event_catalog_digest =
                    MoonPalaceActivityEventPreconditions.SourceMap12EventCatalogDigest,
                source_MAP21_04_all_biome_cluster_digest =
                    MoonPalaceActivityEventPreconditions.SourceMap2104AllBiomeClusterDigest,
                source_MAP21_04_digest_manifest_digest =
                    MoonPalaceActivityEventPreconditions.SourceMap2104DigestManifestDigest,
                csv_digests = value.CsvDigests.Select(MoonPalaceNamedDigestDocument.From)
                    .ToArray(),
                json_digests = value.JsonDigests.Select(MoonPalaceNamedDigestDocument.From)
                    .ToArray(),
                MAP21_06_handoff_digest = value.Map2106HandoffDigest,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.CanonicalDigest,
            };
    }
}
