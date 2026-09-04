using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.Population
{
    public enum GeneratedSpecialStateExportKind
    {
        CoreResource = 1,
        Forge = 2,
        Boss = 3,
        Village = 4,
        ActivityEventRuntime = 5,
        SpawnState = 6,
    }

    public enum GeneratedSpecialStateSourceStatus
    {
        Active = 1,
        AbsentButDeclared = 2,
    }

    public sealed class GeneratedDeclaredSpecialStateSource :
        IComparable<GeneratedDeclaredSpecialStateSource>
    {
        public GeneratedDeclaredSpecialStateSource(
            GeneratedSpecialStateExportKind kind,
            string sourceOwner,
            string regionSiteId,
            SpecialPersistenceKey persistenceKey,
            GeneratedSpecialStateSourceStatus status,
            string stateKind,
            string sourceDigest)
        {
            Kind = kind;
            SourceOwner = sourceOwner ?? string.Empty;
            RegionSiteId = regionSiteId ?? string.Empty;
            PersistenceKey = persistenceKey;
            Status = status;
            StateKind = stateKind ?? string.Empty;
            SourceDigest = sourceDigest ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "DECLARED_SPECIAL_STATE_SOURCE_V1", Kind.ToString(), SourceOwner,
                RegionSiteId, PersistenceKey.Value, Status.ToString(), StateKind,
                SourceDigest,
            });
        }

        public GeneratedSpecialStateExportKind Kind { get; }
        public string SourceOwner { get; }
        public string RegionSiteId { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
        public GeneratedSpecialStateSourceStatus Status { get; }
        public string StateKind { get; }
        public string SourceDigest { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedDeclaredSpecialStateSource other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken,
                StringComparison.Ordinal);
    }

    public sealed class GeneratedSpecialStateExportRow :
        IComparable<GeneratedSpecialStateExportRow>
    {
        internal GeneratedSpecialStateExportRow(
            GeneratedSpecialStateExportKind kind,
            string regionSiteId,
            string sourceOwner,
            GeneratedSpecialStateSourceStatus sourceStatus,
            SpecialPersistenceKey persistenceKey,
            string stableSpawnId,
            string runtimeStateId,
            string saveKey,
            string stateKind,
            string sourceDigest)
        {
            Kind = kind;
            RegionSiteId = regionSiteId ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
            SourceStatus = sourceStatus;
            PersistenceKey = persistenceKey;
            StableSpawnId = stableSpawnId ?? string.Empty;
            RuntimeStateId = runtimeStateId ?? string.Empty;
            SaveKey = saveKey ?? string.Empty;
            StateKind = stateKind ?? string.Empty;
            SourceDigest = sourceDigest ?? string.Empty;
            RowVersion = "V1";
            CanonicalLine = string.Join("|", new[]
            {
                "SPECIAL_STATE_EXPORT_ROW_V1", Kind.ToString(), RegionSiteId,
                SourceOwner, SourceStatus.ToString(), PersistenceKey.Value,
                StableSpawnId, RuntimeStateId, SaveKey, StateKind, SourceDigest,
                RowVersion,
            });
            RowKey = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "SPECIAL_STATE_EXPORT_ROW_KEY_V1", Kind.ToString(), RegionSiteId,
                SourceOwner, PersistenceKey.Value, StableSpawnId, RuntimeStateId,
                SaveKey, StateKind,
            });
            RowDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { CanonicalLine });
            StableToken = CanonicalLine + "|ROW_KEY=" + RowKey +
                "|ROW_DIGEST=" + RowDigest;
        }

        public GeneratedSpecialStateExportKind Kind { get; }
        public string RegionSiteId { get; }
        public string SourceOwner { get; }
        public GeneratedSpecialStateSourceStatus SourceStatus { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
        public string StableSpawnId { get; }
        public string RuntimeStateId { get; }
        public string SaveKey { get; }
        public string StateKind { get; }
        public string SourceDigest { get; }
        public string RowVersion { get; }
        public string RowKey { get; }
        public string RowDigest { get; }
        public string CanonicalLine { get; }
        public string StableToken { get; }
        public bool HasPersistenceKey => !string.IsNullOrEmpty(PersistenceKey.Value);
        public bool HasStableSpawnId => !string.IsNullOrEmpty(StableSpawnId);
        public bool HasRuntimeStateId => !string.IsNullOrEmpty(RuntimeStateId);
        public bool HasSaveKey => !string.IsNullOrEmpty(SaveKey);

        public int CompareTo(GeneratedSpecialStateExportRow other)
        {
            if (other == null) return -1;
            var comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = string.Compare(RegionSiteId, other.RegionSiteId,
                StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SourceOwner, other.SourceOwner,
                StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = PersistenceKey.CompareTo(other.PersistenceKey);
            if (comparison != 0) return comparison;
            comparison = string.Compare(StableSpawnId, other.StableSpawnId,
                StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(RuntimeStateId, other.RuntimeStateId,
                StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SaveKey, other.SaveKey,
                StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(RowVersion,
                other.RowVersion, StringComparison.Ordinal);
        }
    }

    public sealed class GeneratedSpawnStateCsvMaterial
    {
        public const string CanonicalHeader =
            "export_kind,region_site_id,source_owner,source_status,persistence_key," +
            "stable_spawn_id,runtime_state_id,save_key,state_kind,row_version,row_digest";

        internal GeneratedSpawnStateCsvMaterial(
            string header,
            IEnumerable<GeneratedSpecialStateExportRow> sourceRows)
        {
            Header = header ?? string.Empty;
            var rows = (sourceRows ?? Array.Empty<GeneratedSpecialStateExportRow>())
                .OrderBy(value => value).ToArray();
            var lines = new[] { Header }.Concat(rows.Select(CsvLine));
            Text = BakingCanonicalDigest.NormalizeLineEndingsToLf(
                string.Join("\n", lines) + "\n");
            Utf8Bytes = new UTF8Encoding(false).GetBytes(Text);
            RowCount = rows.Length;
            HeaderColumnCount = Header.Split(',').Length;
            Digest = BakingCanonicalDigest.HashCanonicalLines(new[] { Text });
        }

        public string Header { get; }
        public string Text { get; }
        public byte[] Utf8Bytes { get; }
        public int RowCount { get; }
        public int HeaderColumnCount { get; }
        public bool IsLfNormalized => !Text.Contains("\r");
        public bool HasUtf8Bom => Utf8Bytes.Length >= 3 && Utf8Bytes[0] == 0xef &&
            Utf8Bytes[1] == 0xbb && Utf8Bytes[2] == 0xbf;
        public string Digest { get; }
        public int ActualFileWriteCount => 0;
        public int ActualFileReadCount => 0;
        public int GeneratedCsvFileCommitCount => 0;

        private static string CsvLine(GeneratedSpecialStateExportRow row) => string.Join(",",
            new[]
            {
                row.Kind.ToString(), row.RegionSiteId, row.SourceOwner,
                row.SourceStatus.ToString(), row.PersistenceKey.Value,
                row.StableSpawnId, row.RuntimeStateId, row.SaveKey,
                row.StateKind, row.RowVersion, row.RowDigest,
            }.Select(Escape));

        private static string Escape(string source)
        {
            var value = BakingCanonicalDigest.NormalizeLineEndingsToLf(source ?? string.Empty);
            return value.IndexOfAny(new[] { ',', '"', '\n' }) < 0
                ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }

    public sealed class GeneratedSelectionBudgetDebugSection :
        IComparable<GeneratedSelectionBudgetDebugSection>
    {
        private readonly ReadOnlyCollection<string> lines;

        internal GeneratedSelectionBudgetDebugSection(
            string name,
            IEnumerable<string> sourceLines)
        {
            Name = name ?? string.Empty;
            lines = new ReadOnlyCollection<string>((sourceLines ?? Array.Empty<string>())
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Digest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "DEBUG_SECTION_V1", Name }.Concat(lines));
        }

        public string Name { get; }
        public IReadOnlyList<string> Lines => lines;
        public string Digest { get; }
        public int CompareTo(GeneratedSelectionBudgetDebugSection other) => other == null
            ? -1 : string.Compare(Name, other.Name, StringComparison.Ordinal);
    }

    public sealed class GeneratedSelectionBudgetDebugSnapshot
    {
        private readonly ReadOnlyCollection<GeneratedSelectionBudgetDebugSection> sections;
        private readonly ReadOnlyCollection<string> digestReferences;

        internal GeneratedSelectionBudgetDebugSnapshot(
            GeneratedActivityEventRuntimeStateSurface runtimeSurface,
            IEnumerable<GeneratedSpecialStateExportRow> exportRows,
            string specialExportSurfaceDigest)
        {
            var rows = exportRows.OrderBy(value => value).ToArray();
            var mandatory = runtimeSurface.HazardEnemyPlan.PopulationPlan.MandatoryPlan;
            var population = runtimeSurface.HazardEnemyPlan.PopulationPlan;
            var hazardEnemy = runtimeSurface.HazardEnemyPlan;
            digestReferences = new ReadOnlyCollection<string>(new[]
            {
                "MAP18_02=" + mandatory.Digest,
                "MAP18_03=" + population.Digest,
                "MAP18_04_PLAN=" + hazardEnemy.Digest,
                "MAP18_04_OCCUPIED=" + hazardEnemy.OccupiedSurfaceDigest,
                "MAP18_04_BUDGET=" + hazardEnemy.BudgetLedger.Digest,
                "MAP18_05_RUNTIME=" + runtimeSurface.Digest,
                "MAP18_05_SAVE_KEYS=" + runtimeSurface.SaveKeySetDigest,
                "MAP18_05_EXPORT=" + runtimeSurface.ExportSurfaceDigest,
                "MAP18_06_EXPORT=" + specialExportSurfaceDigest,
            });
            sections = new ReadOnlyCollection<GeneratedSelectionBudgetDebugSection>(new[]
            {
                new GeneratedSelectionBudgetDebugSection("Selection", new[]
                {
                    "MANDATORY=" + mandatory.EntryCount,
                    "POPULATION=" + population.EntryCount,
                    "HAZARD_ENEMY=" + hazardEnemy.EntryCount,
                }),
                new GeneratedSelectionBudgetDebugSection("Occupied", new[]
                {
                    "COUNT=" + hazardEnemy.OccupiedSurfaceCount,
                    "DIGEST=" + hazardEnemy.OccupiedSurfaceDigest,
                }),
                new GeneratedSelectionBudgetDebugSection("Budget", new[]
                {
                    "SCOPES=" + hazardEnemy.BudgetLedger.ScopeCount,
                    "SPENDS=" + hazardEnemy.BudgetLedger.SpendEntryCount,
                    "DIGEST=" + hazardEnemy.BudgetLedger.Digest,
                }),
                new GeneratedSelectionBudgetDebugSection("RuntimeState", new[]
                {
                    "RECORDS=" + runtimeSurface.TotalRuntimeStateRecordCount,
                    "DIGEST=" + runtimeSurface.Digest,
                    "SAVE_KEYS=" + runtimeSurface.SaveKeySetDigest,
                    "EXPORT=" + runtimeSurface.ExportSurfaceDigest,
                }),
                new GeneratedSelectionBudgetDebugSection("Persistence", new[]
                {
                    "ROWS=" + rows.Length,
                    "KEYS=" + rows.Count(value => value.HasPersistenceKey),
                    "EXPORT=" + specialExportSurfaceDigest,
                }),
            }.OrderBy(value => value).ToArray());
            Digest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP18_06_DEBUG_SNAPSHOT_V1" }
                .Concat(digestReferences.OrderBy(value => value,
                    StringComparer.Ordinal))
                .Concat(sections.Select(value => value.Name + "=" + value.Digest)));
        }

        public IReadOnlyList<GeneratedSelectionBudgetDebugSection> Sections => sections;
        public IReadOnlyList<string> DigestReferences => digestReferences;
        public int SectionCount => sections.Count;
        public int UpstreamDigestCount => digestReferences.Count;
        public string Digest { get; }
        public bool HasSection(string name) => sections.Any(value =>
            string.Equals(value.Name, name, StringComparison.Ordinal));
    }

    public sealed class GeneratedSpecialStateExportSurface
    {
        private readonly ReadOnlyCollection<GeneratedSpecialStateExportRow> rows;

        internal GeneratedSpecialStateExportSurface(
            GeneratedActivityEventRuntimeStateSurface runtimeSurface,
            IEnumerable<GeneratedSpecialStateExportRow> sourceRows,
            GeneratedSpawnStateCsvMaterial csvMaterial)
        {
            RuntimeStateSurface = runtimeSurface;
            rows = new ReadOnlyCollection<GeneratedSpecialStateExportRow>(sourceRows
                .OrderBy(value => value).ToArray());
            CsvMaterial = csvMaterial;
            Digest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                PolicyVersion,
                runtimeSurface.Digest,
                runtimeSurface.SaveKeySetDigest,
                runtimeSurface.ExportSurfaceDigest,
                runtimeSurface.OccupiedSurfaceDigest,
                runtimeSurface.BudgetLedgerDigest,
                csvMaterial.Digest,
            }.Concat(rows.Select(value => value.StableToken)));
            DebugSnapshot = new GeneratedSelectionBudgetDebugSnapshot(runtimeSurface,
                rows, Digest);
            Map18_07AuditSurfaceDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP18_07_POPULATION_AUDIT_SURFACE_V1", Digest,
                csvMaterial.Digest, DebugSnapshot.Digest,
            });
        }

        public const string PolicyVersion = "MAP18_06_SPECIAL_STATE_EXPORT_V1";
        public const string DownstreamOwner = "MAP18_07_MAP18_POPULATION_EXIT_TESTS";
        public const bool OpensDownstreamTask = false;

        public GeneratedActivityEventRuntimeStateSurface RuntimeStateSurface { get; }
        public GeneratedHazardEnemyPlacementPlan HazardEnemyPlan =>
            RuntimeStateSurface.HazardEnemyPlan;
        public IReadOnlyList<GeneratedHazardEnemyOccupiedReservation> OccupiedSurface =>
            RuntimeStateSurface.OccupiedSurface;
        public GeneratedHazardEnemyBudgetLedger BudgetLedger =>
            RuntimeStateSurface.BudgetLedger;
        public IReadOnlyList<GeneratedSpecialStateExportRow> Rows => rows;
        public GeneratedSpawnStateCsvMaterial CsvMaterial { get; }
        public GeneratedSelectionBudgetDebugSnapshot DebugSnapshot { get; }
        public int ExportGroupCount => rows.Select(value => value.Kind).Distinct().Count();
        public int TotalExportRowCount => rows.Count;
        public int CoreResourceExportRowCount => Count(GeneratedSpecialStateExportKind.CoreResource);
        public int ForgeExportRowCount => Count(GeneratedSpecialStateExportKind.Forge);
        public int BossExportRowCount => Count(GeneratedSpecialStateExportKind.Boss);
        public int VillageExportRowCount => Count(GeneratedSpecialStateExportKind.Village);
        public int ActivityEventRuntimeExportRowCount =>
            Count(GeneratedSpecialStateExportKind.ActivityEventRuntime);
        public int SpawnStateExportRowCount => Count(GeneratedSpecialStateExportKind.SpawnState);
        public int AbsentOptionalSpecialSourceCount => rows.Count(value =>
            value.SourceStatus == GeneratedSpecialStateSourceStatus.AbsentButDeclared);
        public int UniqueExportRowKeyCount => Unique(value => value.RowKey);
        public int UniquePersistenceKeyCount => Unique(value => value.PersistenceKey.Value);
        public int UniqueRuntimeStateIdCount => Unique(value => value.RuntimeStateId);
        public int UniqueSaveKeyCount => Unique(value => value.SaveKey);
        public int UniqueStableSpawnIdCount => Unique(value => value.StableSpawnId);
        public int DuplicateExportRowKeyCount => Duplicate(value => value.RowKey);
        public int DuplicatePersistenceKeyCount => Duplicate(value => value.PersistenceKey.Value);
        public int DuplicateRuntimeStateIdCount => Duplicate(value => value.RuntimeStateId);
        public int DuplicateSaveKeyCount => Duplicate(value => value.SaveKey);
        public int DuplicateStableSpawnIdCount => Duplicate(value => value.StableSpawnId);
        public string Digest { get; }
        public string Map18_07AuditSurfaceDigest { get; }
        public bool Map18_07Started => false;

        public int ActualCsvFileWriteCount => 0;
        public int ActualCsvFileReadCount => 0;
        public int GeneratedCsvFileCommitCount => 0;
        public int SaveWriteCount => 0;
        public int SaveReadCount => 0;
        public int PlayerPrefsWriteCount => 0;
        public int PlayerPrefsReadCount => 0;
        public int RuntimeSpecialRegionSpawnCount => 0;
        public int RuntimeVillageSpawnCount => 0;
        public int RuntimeResourceSpawnCount => 0;
        public int RuntimeForgeSpawnCount => 0;
        public int RuntimeBossSpawnCount => 0;
        public int RuntimeActivityPrefabSpawnCount => 0;
        public int RuntimeEventPrefabSpawnCount => 0;
        public int ActualEventActivationCount => 0;
        public int RewardGrantCount => 0;
        public int InventoryMutationCount => 0;
        public int ResourceMutationCount => 0;
        public int DamageExecutionCount => 0;
        public int EnemyAiControllerHookupCount => 0;
        public int RuntimeObjectSpawnCount => 0;
        public int GameObjectInstantiateCount => 0;
        public int GameObjectEnableCount => 0;
        public int GameObjectDisableCount => 0;
        public int GameObjectDestroyCount => 0;
        public int SystemIoFileWriteCount => 0;
        public int SystemIoFileReadCount => 0;
        public int DiskSaveFileCreateCount => 0;
        public int DiskLoadFileCreateCount => 0;
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
        public int ProductionSeedApprovalCount => 0;
        public int UnityEngineRandomCallCount => 0;
        public int RandomRangeCallCount => 0;
        public int SystemRandomDirectUsageCount => 0;
        public int HiddenRetryLoopCount => 0;
        public int ImplicitSpecialSourceCreationCount => 0;
        public int PriorTaskTestSelectionCount => 0;
        public int Legacy19347SelectionCount => 0;
        public int PlayModeSelectionCount => 0;
        public int UnfilteredTestSelectionCount => 0;
        public int FullRegressionRunCount => 0;

        private int Count(GeneratedSpecialStateExportKind kind) =>
            rows.Count(value => value.Kind == kind);
        private int Unique(Func<GeneratedSpecialStateExportRow, string> selector) => rows
            .Select(selector).Where(value => !string.IsNullOrEmpty(value))
            .Distinct(StringComparer.Ordinal).Count();
        private int Duplicate(Func<GeneratedSpecialStateExportRow, string> selector) => rows
            .Select(selector).Where(value => !string.IsNullOrEmpty(value))
            .GroupBy(value => value, StringComparer.Ordinal)
            .Count(value => value.Count() > 1);
    }
}
