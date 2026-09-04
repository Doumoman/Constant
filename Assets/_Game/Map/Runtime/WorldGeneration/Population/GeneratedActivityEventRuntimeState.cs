using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.EventOverlays;

namespace StarNight.Map.WorldGeneration.Population
{
    public enum GeneratedActivityRuntimePhase
    {
        Cue = 1,
        Active = 2,
        Resolved = 3,
        Resettable = 4,
    }

    public enum GeneratedEventRuntimeVariant
    {
        Empty = 1,
        Active = 2,
    }

    public enum GeneratedActivityCuePolicy
    {
        ExplicitSourceCue = 1,
    }

    public enum GeneratedActivityResetPolicy
    {
        ResolvedThenResettable = 1,
    }

    public enum GeneratedEventActivationPolicy
    {
        StableSourceMarker = 1,
    }

    public enum GeneratedEventResolutionPolicy
    {
        PersistVariantIdentity = 1,
    }

    public enum GeneratedEventReentryPolicy
    {
        RestoreSavedVariant = 1,
    }

    public sealed class GeneratedActivityRuntimeTransition :
        IComparable<GeneratedActivityRuntimeTransition>
    {
        public GeneratedActivityRuntimeTransition(
            GeneratedActivityRuntimePhase from,
            GeneratedActivityRuntimePhase to)
        {
            From = from;
            To = to;
            StableToken = From + "->" + To;
        }

        public GeneratedActivityRuntimePhase From { get; }
        public GeneratedActivityRuntimePhase To { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedActivityRuntimeTransition other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken,
                StringComparison.Ordinal);
    }

    public static class GeneratedActivityRuntimeTransitionCatalog
    {
        public static IReadOnlyList<GeneratedActivityRuntimeTransition> CreateAllowed() =>
            new[]
            {
                new GeneratedActivityRuntimeTransition(
                    GeneratedActivityRuntimePhase.Cue,
                    GeneratedActivityRuntimePhase.Active),
                new GeneratedActivityRuntimeTransition(
                    GeneratedActivityRuntimePhase.Active,
                    GeneratedActivityRuntimePhase.Resolved),
                new GeneratedActivityRuntimeTransition(
                    GeneratedActivityRuntimePhase.Resolved,
                    GeneratedActivityRuntimePhase.Resettable),
                new GeneratedActivityRuntimeTransition(
                    GeneratedActivityRuntimePhase.Resettable,
                    GeneratedActivityRuntimePhase.Cue),
            };
    }

    public sealed class GeneratedRuntimeStateId :
        IEquatable<GeneratedRuntimeStateId>, IComparable<GeneratedRuntimeStateId>
    {
        internal GeneratedRuntimeStateId(IEnumerable<string> canonicalLines)
        {
            Value = BakingCanonicalDigest.HashCanonicalLines(canonicalLines);
        }

        public string Value { get; }
        public int CompareTo(GeneratedRuntimeStateId other) => other == null
            ? -1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(GeneratedRuntimeStateId other) => other != null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedRuntimeStateId);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public sealed class GeneratedRuntimeSaveKey :
        IEquatable<GeneratedRuntimeSaveKey>, IComparable<GeneratedRuntimeSaveKey>
    {
        internal GeneratedRuntimeSaveKey(GeneratedRuntimeStateId runtimeStateId)
        {
            Namespace = "MAP18_RUNTIME_STATE";
            Version = "V1";
            Value = Namespace + "/" + Version + "/" + runtimeStateId.Value;
        }

        public string Namespace { get; }
        public string Version { get; }
        public string Value { get; }
        public int CompareTo(GeneratedRuntimeSaveKey other) => other == null
            ? -1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(GeneratedRuntimeSaveKey other) => other != null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedRuntimeSaveKey);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public sealed class GeneratedActivityRuntimeSource :
        IComparable<GeneratedActivityRuntimeSource>
    {
        public GeneratedActivityRuntimeSource(
            ActivityStructureContract contract,
            GeneratedSectorCoordinate sector,
            string sourceDigest,
            string claimedReservationKey = null)
        {
            Contract = contract;
            Sector = sector;
            SourceDigest = sourceDigest ?? string.Empty;
            ClaimedReservationKey = claimedReservationKey ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "ACTIVITY_RUNTIME_SOURCE_V1",
                contract == null ? "MISSING" : contract.Id.Value,
                sector == null ? "MISSING" : sector.ToString(),
                SourceDigest,
                ClaimedReservationKey,
            });
        }

        public ActivityStructureContract Contract { get; }
        public GeneratedSectorCoordinate Sector { get; }
        public string SourceId => Contract == null ? string.Empty : Contract.Id.Value;
        public string SourceDigest { get; }
        public string ClaimedReservationKey { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedActivityRuntimeSource other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken,
                StringComparison.Ordinal);
    }

    public sealed class GeneratedEventRuntimeSource :
        IComparable<GeneratedEventRuntimeSource>
    {
        public GeneratedEventRuntimeSource(
            EventOverlayContract contract,
            GeneratedSectorCoordinate sector,
            string sourceDigest,
            string claimedReservationKey = null)
        {
            Contract = contract;
            Sector = sector;
            SourceDigest = sourceDigest ?? string.Empty;
            ClaimedReservationKey = claimedReservationKey ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "EVENT_RUNTIME_SOURCE_V1",
                contract == null ? "MISSING" : contract.Id.Value,
                sector == null ? "MISSING" : sector.ToString(),
                SourceDigest,
                ClaimedReservationKey,
            });
        }

        public EventOverlayContract Contract { get; }
        public GeneratedSectorCoordinate Sector { get; }
        public string SourceId => Contract == null ? string.Empty : Contract.Id.Value;
        public string SourceDigest { get; }
        public string ClaimedReservationKey { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedEventRuntimeSource other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken,
                StringComparison.Ordinal);
    }

    public interface IGeneratedRuntimeStateRecord
    {
        GeneratedRuntimeStateId RuntimeStateId { get; }
        GeneratedRuntimeSaveKey SaveKey { get; }
        string SourceId { get; }
        GeneratedSectorCoordinate Sector { get; }
        string StableToken { get; }
    }

    public sealed class GeneratedActivityRuntimeStateRecord :
        IGeneratedRuntimeStateRecord, IComparable<GeneratedActivityRuntimeStateRecord>
    {
        private readonly ReadOnlyCollection<GeneratedActivityRuntimeTransition> transitions;

        internal GeneratedActivityRuntimeStateRecord(
            GeneratedActivityRuntimeSource source,
            GeneratedRuntimeStateId runtimeStateId,
            GeneratedRuntimeSaveKey saveKey,
            IEnumerable<GeneratedActivityRuntimeTransition> allowedTransitions)
        {
            Source = source;
            RuntimeStateId = runtimeStateId;
            SaveKey = saveKey;
            CurrentPhase = GeneratedActivityRuntimePhase.Cue;
            CuePolicy = GeneratedActivityCuePolicy.ExplicitSourceCue;
            ResetPolicy = GeneratedActivityResetPolicy.ResolvedThenResettable;
            transitions = new ReadOnlyCollection<GeneratedActivityRuntimeTransition>(
                allowedTransitions.OrderBy(value => value).ToArray());
            StableToken = string.Join("|", new[]
            {
                "ACTIVITY_RUNTIME_STATE_V1", source.StableToken,
                "CURRENT=" + CurrentPhase,
                "CUE=" + CuePolicy,
                "RESET=" + ResetPolicy,
                "ID=" + RuntimeStateId.Value,
                "SAVE=" + SaveKey.Value,
            }.Concat(transitions.Select(value => value.StableToken)));
        }

        public GeneratedActivityRuntimeSource Source { get; }
        public string SourceId => Source.SourceId;
        public GeneratedSectorCoordinate Sector => Source.Sector;
        public GeneratedActivityRuntimePhase CurrentPhase { get; }
        public GeneratedActivityCuePolicy CuePolicy { get; }
        public GeneratedActivityResetPolicy ResetPolicy { get; }
        public IReadOnlyList<GeneratedActivityRuntimeTransition> AllowedTransitions =>
            transitions;
        public GeneratedRuntimeStateId RuntimeStateId { get; }
        public GeneratedRuntimeSaveKey SaveKey { get; }
        public string StableToken { get; }
        public bool CanTransition(
            GeneratedActivityRuntimePhase from,
            GeneratedActivityRuntimePhase to) => transitions.Any(value =>
                value.From == from && value.To == to);
        public int CompareTo(GeneratedActivityRuntimeStateRecord other) => other == null
            ? -1 : RuntimeStateId.CompareTo(other.RuntimeStateId);
    }

    public sealed class GeneratedEventRuntimeStateRecord :
        IGeneratedRuntimeStateRecord, IComparable<GeneratedEventRuntimeStateRecord>
    {
        internal GeneratedEventRuntimeStateRecord(
            GeneratedEventRuntimeSource source,
            GeneratedEventRuntimeVariant variant,
            GeneratedRuntimeStateId runtimeStateId,
            GeneratedRuntimeSaveKey saveKey)
        {
            Source = source;
            Variant = variant;
            RuntimeStateId = runtimeStateId;
            SaveKey = saveKey;
            ActivationPolicy = GeneratedEventActivationPolicy.StableSourceMarker;
            ResolutionPolicy = GeneratedEventResolutionPolicy.PersistVariantIdentity;
            ReentryPolicy = GeneratedEventReentryPolicy.RestoreSavedVariant;
            StableToken = string.Join("|", new[]
            {
                "EVENT_RUNTIME_STATE_V1", source.StableToken,
                "VARIANT=" + Variant,
                "ACTIVATION=" + ActivationPolicy,
                "RESOLUTION=" + ResolutionPolicy,
                "REENTRY=" + ReentryPolicy,
                "ID=" + RuntimeStateId.Value,
                "SAVE=" + SaveKey.Value,
            });
        }

        public GeneratedEventRuntimeSource Source { get; }
        public string SourceId => Source.SourceId;
        public GeneratedSectorCoordinate Sector => Source.Sector;
        public GeneratedEventRuntimeVariant Variant { get; }
        public GeneratedEventActivationPolicy ActivationPolicy { get; }
        public GeneratedEventResolutionPolicy ResolutionPolicy { get; }
        public GeneratedEventReentryPolicy ReentryPolicy { get; }
        public bool PublishesRuntimeObject => false;
        public bool PublishesStableStateIdentity =>
            Variant == GeneratedEventRuntimeVariant.Active;
        public GeneratedRuntimeStateId RuntimeStateId { get; }
        public GeneratedRuntimeSaveKey SaveKey { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedEventRuntimeStateRecord other) => other == null
            ? -1 : RuntimeStateId.CompareTo(other.RuntimeStateId);
    }

    public sealed class GeneratedActivityEventRuntimeExportRecord :
        IComparable<GeneratedActivityEventRuntimeExportRecord>
    {
        internal GeneratedActivityEventRuntimeExportRecord(
            string kind,
            IGeneratedRuntimeStateRecord state)
        {
            Kind = kind;
            RuntimeStateId = state.RuntimeStateId;
            SaveKey = state.SaveKey;
            SourceId = state.SourceId;
            Sector = state.Sector;
            StableToken = string.Join("|", new[]
            {
                "MAP18_06_RUNTIME_EXPORT_V1", Kind, SourceId,
                Sector.ToString(), RuntimeStateId.Value, SaveKey.Value,
            });
        }

        public string Kind { get; }
        public GeneratedRuntimeStateId RuntimeStateId { get; }
        public GeneratedRuntimeSaveKey SaveKey { get; }
        public string SourceId { get; }
        public GeneratedSectorCoordinate Sector { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedActivityEventRuntimeExportRecord other) =>
            other == null ? -1 : string.Compare(StableToken, other.StableToken,
                StringComparison.Ordinal);
    }

    public sealed class GeneratedActivityEventRuntimeStateSurface
    {
        private readonly ReadOnlyCollection<GeneratedActivityRuntimeStateRecord> activities;
        private readonly ReadOnlyCollection<GeneratedEventRuntimeStateRecord> events;
        private readonly ReadOnlyCollection<IGeneratedRuntimeStateRecord> states;
        private readonly ReadOnlyCollection<GeneratedActivityEventRuntimeExportRecord> exports;

        internal GeneratedActivityEventRuntimeStateSurface(
            GeneratedHazardEnemyPlacementPlan hazardEnemyPlan,
            IEnumerable<GeneratedActivityRuntimeStateRecord> activityRecords,
            IEnumerable<GeneratedEventRuntimeStateRecord> eventRecords)
        {
            HazardEnemyPlan = hazardEnemyPlan;
            activities = new ReadOnlyCollection<GeneratedActivityRuntimeStateRecord>(
                activityRecords.OrderBy(value => value).ToArray());
            events = new ReadOnlyCollection<GeneratedEventRuntimeStateRecord>(
                eventRecords.OrderBy(value => value).ToArray());
            states = new ReadOnlyCollection<IGeneratedRuntimeStateRecord>(activities
                .Cast<IGeneratedRuntimeStateRecord>().Concat(events).OrderBy(value =>
                    value.RuntimeStateId.Value, StringComparer.Ordinal).ToArray());
            exports = new ReadOnlyCollection<GeneratedActivityEventRuntimeExportRecord>(
                activities.Select(value => new GeneratedActivityEventRuntimeExportRecord(
                        "ACTIVITY", value))
                    .Concat(events.Select(value =>
                        new GeneratedActivityEventRuntimeExportRecord(
                            "EVENT_" + value.Variant.ToString().ToUpperInvariant(), value)))
                    .OrderBy(value => value).ToArray());
            SaveKeySetDigest = BakingCanonicalDigest.HashCanonicalLines(states.Select(value =>
                value.SaveKey.Value));
            ExportSurfaceDigest = BakingCanonicalDigest.HashCanonicalLines(exports.Select(value =>
                value.StableToken));
            Digest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                PolicyVersion,
                hazardEnemyPlan.Digest,
                hazardEnemyPlan.OccupiedSurfaceDigest,
                hazardEnemyPlan.BudgetLedger.Digest,
            }.Concat(states.Select(value => value.StableToken))
             .Concat(new[] { SaveKeySetDigest, ExportSurfaceDigest }));
        }

        public const string PolicyVersion = "MAP18_05_ACTIVITY_EVENT_RUNTIME_STATE_V1";
        public const string DownstreamOwner =
            "MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG";
        public const bool OpensDownstreamTask = false;

        public GeneratedHazardEnemyPlacementPlan HazardEnemyPlan { get; }
        public IReadOnlyList<GeneratedHazardEnemyOccupiedReservation> OccupiedSurface =>
            HazardEnemyPlan.OccupiedSurface;
        public GeneratedHazardEnemyBudgetLedger BudgetLedger => HazardEnemyPlan.BudgetLedger;
        public IReadOnlyList<GeneratedActivityRuntimeStateRecord> ActivityRecords => activities;
        public IReadOnlyList<GeneratedEventRuntimeStateRecord> EventRecords => events;
        public IReadOnlyList<IGeneratedRuntimeStateRecord> RuntimeStateRecords => states;
        public IReadOnlyList<GeneratedActivityEventRuntimeExportRecord> Map18_06ExportRecords =>
            exports;
        public int ActivityRuntimeStateRecordCount => activities.Count;
        public int EventRuntimeStateRecordCount => events.Count;
        public int EmptyEventVariantCount => events.Count(value =>
            value.Variant == GeneratedEventRuntimeVariant.Empty);
        public int ActiveEventVariantCount => events.Count(value =>
            value.Variant == GeneratedEventRuntimeVariant.Active);
        public int TotalRuntimeStateRecordCount => states.Count;
        public int UniqueRuntimeStateIdCount => states.Select(value =>
            value.RuntimeStateId.Value).Distinct(StringComparer.Ordinal).Count();
        public int UniqueSaveKeyCount => states.Select(value => value.SaveKey.Value)
            .Distinct(StringComparer.Ordinal).Count();
        public int DuplicateRuntimeStateIdCount => TotalRuntimeStateRecordCount -
            UniqueRuntimeStateIdCount;
        public int DuplicateSaveKeyCount => TotalRuntimeStateRecordCount -
            UniqueSaveKeyCount;
        public int OccupiedSurfaceCount => HazardEnemyPlan.OccupiedSurfaceCount;
        public int RemainingCandidateCount => HazardEnemyPlan.RemainingCandidateCount;
        public int OccupiedConflictCount => 0;
        public int BudgetMutationCount => 0;
        public int Map18_06ExportSurfaceRecordCount => exports.Count;
        public string HazardEnemyPlanDigest => HazardEnemyPlan.Digest;
        public string OccupiedSurfaceDigest => HazardEnemyPlan.OccupiedSurfaceDigest;
        public string BudgetLedgerDigest => HazardEnemyPlan.BudgetLedger.Digest;
        public string SaveKeySetDigest { get; }
        public string ExportSurfaceDigest { get; }
        public string Digest { get; }
        public bool Map18_06Started => false;

        public int RuntimeActivityPrefabSpawnCount => 0;
        public int RuntimeEventPrefabSpawnCount => 0;
        public int CueVfxPlaybackCount => 0;
        public int CueSfxPlaybackCount => 0;
        public int ActualEventActivationCount => 0;
        public int ActualStateTransitionCount => 0;
        public int SaveWriteCount => 0;
        public int SaveReadCount => 0;
        public int PlayerPrefsWriteCount => 0;
        public int PlayerPrefsReadCount => 0;
        public int RuntimeObjectSpawnCount => 0;
        public int GameObjectInstantiateCount => 0;
        public int GameObjectEnableCount => 0;
        public int GameObjectDisableCount => 0;
        public int GameObjectDestroyCount => 0;
        public int SystemIoFileWriteCount => 0;
        public int SystemIoFileReadCount => 0;
        public int DiskSaveFileCreateCount => 0;
        public int DiskLoadFileCreateCount => 0;
        public int UserSaveSlotWriteCount => 0;
        public int PlatformStorageWriteCount => 0;
        public int RewardGrantCount => 0;
        public int DamageExecutionCount => 0;
        public int EnemyAiControllerHookupCount => 0;
        public int HealthComponentCreationCount => 0;
        public int DamageComponentCreationCount => 0;
        public int HitboxComponentCreationCount => 0;
        public int HurtboxComponentCreationCount => 0;
        public int TilemapComponentWriteCount => 0;
        public int TilemapSetTileCallCount => 0;
        public int TilemapSetTilesCallCount => 0;
        public int TilemapSetTilesBlockCallCount => 0;
        public int TilemapClearAllTilesCallCount => 0;
        public int TilemapColliderCreationCount => 0;
        public int CompositeColliderCreationCount => 0;
        public int ColliderCreationCount => 0;
        public int RigidbodyCreationCount => 0;
        public int PhysicsQueryCount => 0;
        public int PhysicsSimulationCount => 0;
        public int NavMeshSetupCount => 0;
        public int PathfindingSetupCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int TilemapMutationCount => 0;
        public int CameraReadCount => 0;
        public int CameraWriteCount => 0;
        public int AddressablesLoadCount => 0;
        public int ResourcesLoadCount => 0;
        public int AssetDatabaseLoadCount => 0;
        public int AuthoringCsvEditCount => 0;
        public int GeneratedCsvCommitCount => 0;
        public int GeneratedAssetCommitCount => 0;
        public int ProductionSeedApprovalCount => 0;
        public int UnityEngineRandomCallCount => 0;
        public int RandomRangeCallCount => 0;
        public int SystemRandomDirectUsageCount => 0;
        public int HiddenRetryLoopCount => 0;
        public int ImplicitSourceCreationCount => 0;
        public int CandidateMutationCount => 0;
        public int PriorTaskTestSelectionCount => 0;
        public int Legacy19347SelectionCount => 0;
        public int PlayModeSelectionCount => 0;
        public int UnfilteredTestSelectionCount => 0;
        public int FullRegressionRunCount => 0;
    }
}
