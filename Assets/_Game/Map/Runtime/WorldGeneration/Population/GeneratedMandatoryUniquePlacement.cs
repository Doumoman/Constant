using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.Population
{
    public enum GeneratedMandatoryContentKind
    {
        RequiredProgressTrigger = 1,
        MoonCore = 2,
        CassiaSap = 3,
        StarNuruk = 4,
    }

    public sealed class GeneratedMandatoryContentKey :
        IEquatable<GeneratedMandatoryContentKey>, IComparable<GeneratedMandatoryContentKey>
    {
        public GeneratedMandatoryContentKey(
            GeneratedMandatoryContentKind kind,
            string value,
            CoreResourceKind? coreResource,
            SpecialPersistenceKey authoritativePersistenceKey)
        {
            Kind = kind;
            Value = GeneratedContentPoolKey.Normalize(value);
            CoreResource = coreResource;
            AuthoritativePersistenceKey = authoritativePersistenceKey;
            StableToken = string.Join("|", new[]
            {
                "MANDATORY_CONTENT_KEY_V1", Kind.ToString().ToUpperInvariant(),
                GeneratedContentPoolKey.Part(Value),
                CoreResource.HasValue ? CoreResource.Value.ToString().ToUpperInvariant() : "NO_CORE_RESOURCE",
                GeneratedContentPoolKey.Part(AuthoritativePersistenceKey.Value),
            });
        }

        public GeneratedMandatoryContentKind Kind { get; }
        public string Value { get; }
        public CoreResourceKind? CoreResource { get; }
        public SpecialPersistenceKey AuthoritativePersistenceKey { get; }
        public bool IsCoreResource => CoreResource.HasValue;
        public string StableToken { get; }

        public int CompareTo(GeneratedMandatoryContentKey other) => other == null
            ? -1 : Kind.CompareTo(other.Kind);
        public bool Equals(GeneratedMandatoryContentKey other) => other != null &&
            string.Equals(StableToken, other.StableToken, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedMandatoryContentKey);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => Value;
    }

    public static class GeneratedMandatoryContentCatalog
    {
        public const string RequiredProgressTriggerValue = "REQUIRED_PROGRESS_TRIGGER";

        public static IReadOnlyList<GeneratedMandatoryContentKey> CreateAuthoritative()
        {
            var keys = new List<GeneratedMandatoryContentKey>
            {
                new GeneratedMandatoryContentKey(
                    GeneratedMandatoryContentKind.RequiredProgressTrigger,
                    RequiredProgressTriggerValue, null, default(SpecialPersistenceKey)),
            };
            keys.AddRange(CoreResourceRegionStarterCatalog.Entries.Select(ForCoreResource));
            return new ReadOnlyCollection<GeneratedMandatoryContentKey>(keys
                .OrderBy(value => value).ToArray());
        }

        public static GeneratedMandatoryContentKey ForCoreResource(
            CoreResourceRegionDefinition definition)
        {
            var resource = definition == null ? (CoreResourceKind)0 : definition.Resource;
            return new GeneratedMandatoryContentKey(Kind(resource),
                resource.ToString().ToUpperInvariant(), resource,
                definition == null || definition.RequiredReward == null
                    ? default(SpecialPersistenceKey)
                    : definition.RequiredReward.PersistenceKey);
        }

        public static GeneratedMandatoryContentKind Kind(CoreResourceKind resource)
        {
            switch (resource)
            {
                case CoreResourceKind.MoonCore: return GeneratedMandatoryContentKind.MoonCore;
                case CoreResourceKind.CassiaSap: return GeneratedMandatoryContentKind.CassiaSap;
                case CoreResourceKind.StarNuruk: return GeneratedMandatoryContentKind.StarNuruk;
                default: return (GeneratedMandatoryContentKind)0;
            }
        }
    }

    public sealed class GeneratedMandatoryUniqueRule :
        IComparable<GeneratedMandatoryUniqueRule>
    {
        private readonly ReadOnlyCollection<GeneratedContentSlotCategory> categoryPreference;

        public GeneratedMandatoryUniqueRule(
            GeneratedMandatoryContentKey contentKey,
            IEnumerable<GeneratedContentSlotCategory> preferredCategories,
            int maxWorldCount = 1,
            bool required = true,
            bool exactlyOne = true,
            bool worldUnique = true,
            bool excludesDownstream = true)
        {
            ContentKey = contentKey;
            var raw = (preferredCategories ?? Array.Empty<GeneratedContentSlotCategory>()).ToArray();
            categoryPreference = new ReadOnlyCollection<GeneratedContentSlotCategory>(raw);
            MaxWorldCount = maxWorldCount;
            Required = required;
            ExactlyOne = exactlyOne;
            WorldUnique = worldUnique;
            ExcludesDownstream = excludesDownstream;
            StableToken = string.Join("|", new[]
            {
                "MANDATORY_UNIQUE_RULE_V1",
                ContentKey == null ? "MISSING" : ContentKey.StableToken,
                "REQUIRED=" + (Required ? "1" : "0"),
                "EXACTLY_ONE=" + (ExactlyOne ? "1" : "0"),
                "WORLD_UNIQUE=" + (WorldUnique ? "1" : "0"),
                "MAX=" + MaxWorldCount.ToString(CultureInfo.InvariantCulture),
                "EXCLUDES=" + (ExcludesDownstream ? "1" : "0"),
                "PREFERENCE=" + string.Join(",", categoryPreference.Select(value =>
                    value.ToString().ToUpperInvariant())),
            });
        }

        public GeneratedMandatoryContentKey ContentKey { get; }
        public IReadOnlyList<GeneratedContentSlotCategory> CategoryPreference => categoryPreference;
        public int MaxWorldCount { get; }
        public bool Required { get; }
        public bool ExactlyOne { get; }
        public bool WorldUnique { get; }
        public bool ExcludesDownstream { get; }
        public string StableToken { get; }

        public int Suitability(GeneratedContentSlotIndexEntry entry)
        {
            if (entry == null) return int.MaxValue;
            for (var i = 0; i < categoryPreference.Count; i++)
                if (categoryPreference[i] == entry.Address.Category) return i;
            return int.MaxValue;
        }

        public int CompareTo(GeneratedMandatoryUniqueRule other) => other == null
            ? -1 : (ContentKey == null ? (other.ContentKey == null ? 0 : 1)
                : (other.ContentKey == null ? -1 : ContentKey.CompareTo(other.ContentKey)));

        public static GeneratedMandatoryUniqueRule CreateDefault(
            GeneratedMandatoryContentKey key)
        {
            var preferences = key != null &&
                key.Kind == GeneratedMandatoryContentKind.RequiredProgressTrigger
                ? new[]
                {
                    GeneratedContentSlotCategory.Event,
                    GeneratedContentSlotCategory.Activity,
                    GeneratedContentSlotCategory.Device,
                    GeneratedContentSlotCategory.Special,
                }
                : new[]
                {
                    GeneratedContentSlotCategory.Special,
                    GeneratedContentSlotCategory.Device,
                    GeneratedContentSlotCategory.Activity,
                    GeneratedContentSlotCategory.Event,
                };
            return new GeneratedMandatoryUniqueRule(key, preferences);
        }
    }

    public sealed class GeneratedMandatoryUniquePlacementEntry :
        IComparable<GeneratedMandatoryUniquePlacementEntry>
    {
        internal GeneratedMandatoryUniquePlacementEntry(
            GeneratedMandatoryUniqueRule rule,
            GeneratedContentSlotIndexEntry selectedSlot)
        {
            Rule = rule;
            ContentKey = rule.ContentKey;
            SelectedSlot = selectedSlot;
            StableSpawnId = selectedSlot.StableSpawnId;
            ReservationKey = selectedSlot.ReservationKey;
            SourceProof = selectedSlot.Address.CanonicalLine;
            StableToken = string.Join("|", new[]
            {
                "MANDATORY_UNIQUE_PLACEMENT_V1", ContentKey.StableToken,
                "SPAWN_ID=" + StableSpawnId.Value,
                "RESERVATION=" + ReservationKey,
                "SOURCE=" + SourceProof,
                rule.StableToken,
            });
        }

        public GeneratedMandatoryUniqueRule Rule { get; }
        public GeneratedMandatoryContentKey ContentKey { get; }
        public GeneratedContentSlotIndexEntry SelectedSlot { get; }
        public GeneratedStableSpawnId StableSpawnId { get; }
        public string ReservationKey { get; }
        public string SourceProof { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedMandatoryUniquePlacementEntry other) => other == null
            ? -1 : ContentKey.CompareTo(other.ContentKey);
    }

    public sealed class GeneratedMandatoryUniqueExclusion
    {
        internal GeneratedMandatoryUniqueExclusion(GeneratedMandatoryUniquePlacementEntry entry)
        {
            ReservationKey = entry.ReservationKey;
            ContentKey = entry.ContentKey;
            StableSpawnId = entry.StableSpawnId;
            StableToken = "EXCLUSION|" + ReservationKey + "|" + ContentKey.StableToken +
                "|" + StableSpawnId.Value;
        }

        public string ReservationKey { get; }
        public GeneratedMandatoryContentKey ContentKey { get; }
        public GeneratedStableSpawnId StableSpawnId { get; }
        public string StableToken { get; }
    }

    public sealed class GeneratedMandatoryUniquePlacementPlan
    {
        private readonly ReadOnlyCollection<GeneratedMandatoryUniquePlacementEntry> entries;
        private readonly ReadOnlyCollection<GeneratedContentSlotIndexEntry> remainingCandidates;
        private readonly ReadOnlyCollection<GeneratedMandatoryUniqueExclusion> exclusions;

        internal GeneratedMandatoryUniquePlacementPlan(
            GeneratedContentSlotIndex sourceIndex,
            IEnumerable<GeneratedMandatoryUniquePlacementEntry> sourceEntries,
            IEnumerable<GeneratedContentSlotIndexEntry> sourceRemainingCandidates)
        {
            SourceIndex = sourceIndex;
            entries = new ReadOnlyCollection<GeneratedMandatoryUniquePlacementEntry>((sourceEntries ??
                Array.Empty<GeneratedMandatoryUniquePlacementEntry>()).OrderBy(value => value).ToArray());
            remainingCandidates = new ReadOnlyCollection<GeneratedContentSlotIndexEntry>(
                (sourceRemainingCandidates ?? Array.Empty<GeneratedContentSlotIndexEntry>())
                .OrderBy(value => value).ToArray());
            exclusions = new ReadOnlyCollection<GeneratedMandatoryUniqueExclusion>(entries
                .Select(value => new GeneratedMandatoryUniqueExclusion(value))
                .OrderBy(value => value.ReservationKey, StringComparer.Ordinal).ToArray());
            StableIdSetDigest = BakingCanonicalDigest.HashCanonicalLines(entries
                .Select(value => value.StableSpawnId.Value)
                .OrderBy(value => value, StringComparer.Ordinal));
            Digest = GeneratedMandatoryUniquePlacementDigest.Compute(this);
        }

        public const string PolicyVersion = "MAP18_02_MANDATORY_UNIQUE_PREPLACEMENT_V1";
        public const string DownstreamOwner = "MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS";
        public const bool OpensDownstreamTask = false;

        public GeneratedContentSlotIndex SourceIndex { get; }
        public IReadOnlyList<GeneratedMandatoryUniquePlacementEntry> Entries => entries;
        public IReadOnlyList<GeneratedContentSlotIndexEntry> RemainingCandidates => remainingCandidates;
        public IReadOnlyList<GeneratedMandatoryUniqueExclusion> Exclusions => exclusions;
        public int EntryCount => entries.Count;
        public int RequiredTriggerCount => entries.Count(value =>
            value.ContentKey.Kind == GeneratedMandatoryContentKind.RequiredProgressTrigger);
        public int CoreResourceCount => entries.Count(value => value.ContentKey.IsCoreResource);
        public int UniqueContentKeyCount => entries.Select(value => value.ContentKey.StableToken)
            .Distinct(StringComparer.Ordinal).Count();
        public int UniqueStableSpawnIdCount => entries.Select(value => value.StableSpawnId.Value)
            .Distinct(StringComparer.Ordinal).Count();
        public int UniqueReservationKeyCount => entries.Select(value => value.ReservationKey)
            .Distinct(StringComparer.Ordinal).Count();
        public int RemainingCandidateCount => remainingCandidates.Count;
        public int ExclusionCount => exclusions.Count;
        public string StableIdSetDigest { get; }
        public string Digest { get; }

        public GeneratedMandatoryUniquePlacementEntry Entry(GeneratedMandatoryContentKind kind) =>
            entries.Single(value => value.ContentKey.Kind == kind);
        public bool IsReserved(string reservationKey) => exclusions.Any(value => string.Equals(
            value.ReservationKey, reservationKey, StringComparison.Ordinal));
        public bool TryGetExclusion(
            string reservationKey,
            out GeneratedMandatoryUniqueExclusion exclusion)
        {
            exclusion = exclusions.SingleOrDefault(value => string.Equals(value.ReservationKey,
                reservationKey, StringComparison.Ordinal));
            return exclusion != null;
        }

        public int LogicalPreplacementEntryCount => entries.Count;
        public int RuntimeContentPlacementCount => 0;
        public int WeightedPoolRollCount => 0;
        public int BudgetSpendCount => 0;
        public int RewardGrantCount => 0;
        public int InventoryMutationCount => 0;
        public int DeviceExecutionCount => 0;
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
        public int ImplicitSlotCreationCount => 0;
        public int CandidateMutationCount => 0;
        public int RetryLoopCount => 0;
        public bool Map18_03Started => false;
    }

    public static class GeneratedMandatoryUniquePlacementDigest
    {
        public static string Compute(GeneratedMandatoryUniquePlacementPlan plan)
        {
            if (plan == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedMandatoryUniquePlacementPlan.PolicyVersion,
                "SOURCE_INDEX|" + plan.SourceIndex.Digest + "|" +
                    plan.SourceIndex.StableIdSetDigest,
                "COUNTS|" + string.Join("|", new[]
                {
                    plan.EntryCount.ToString(CultureInfo.InvariantCulture),
                    plan.RequiredTriggerCount.ToString(CultureInfo.InvariantCulture),
                    plan.CoreResourceCount.ToString(CultureInfo.InvariantCulture),
                    plan.UniqueContentKeyCount.ToString(CultureInfo.InvariantCulture),
                    plan.UniqueStableSpawnIdCount.ToString(CultureInfo.InvariantCulture),
                    plan.UniqueReservationKeyCount.ToString(CultureInfo.InvariantCulture),
                    plan.RemainingCandidateCount.ToString(CultureInfo.InvariantCulture),
                    plan.ExclusionCount.ToString(CultureInfo.InvariantCulture),
                }),
                "STABLE_ID_SET|" + plan.StableIdSetDigest,
                "DOWNSTREAM|" + GeneratedMandatoryUniquePlacementPlan.DownstreamOwner + "|0",
            };
            lines.AddRange(plan.Entries.Select(value => value.StableToken));
            lines.AddRange(plan.RemainingCandidates.Select(value =>
                "REMAINING|" + value.DeterministicOrderKey));
            lines.AddRange(plan.Exclusions.Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }
    }
}
