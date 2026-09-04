using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class GeneratedTerrainPerformanceOperation
    {
        public const string Placement = "placement";
        public const string LayerBake = "layer_bake";
        public const string SeamValidation = "seam_validation";
        public const string ColliderCache = "collider_cache";
        public const string StreamWindow = "stream_window";
        public const string Transition = "transition";
        public const string ModificationStorage = "modification_storage";
        public const string SaveManifest = "save_manifest";
        public const string RegenApply = "regen_apply";
        public const string HashMismatch = "hash_mismatch";

        private static readonly ReadOnlyCollection<string> OrderedValues =
            new ReadOnlyCollection<string>(new[]
            {
                Placement, LayerBake, SeamValidation, ColliderCache, StreamWindow,
                Transition, ModificationStorage, SaveManifest, RegenApply, HashMismatch,
            });

        public static IReadOnlyList<string> All => OrderedValues;

        public static int IndexOf(string value)
        {
            for (var index = 0; index < OrderedValues.Count; index++)
                if (string.Equals(OrderedValues[index], value, StringComparison.Ordinal)) return index;
            return int.MaxValue;
        }

        public static bool IsSupported(string value) => IndexOf(value) != int.MaxValue;
    }

    public sealed class GeneratedTerrainPerformanceBudget
    {
        private readonly ReadOnlyDictionary<string, int> operationUpperBounds;
        private readonly ReadOnlyDictionary<string, int> structuralUpperBounds;

        public const int ReferenceWarmupIterations = 1;
        public const int ReferenceMeasuredIterations = 3;
        public const int PlacementCellCount = 1536;
        public const int PlacementLayerReferenceCount = 10752;
        public const int LogicalLayerCount = 7;
        public const int MicroPatternSeamCount = 688;
        public const int MicroChunkSeamCount = 240;
        public const int MicroPatternOnlySeamCount = 448;
        public const int CenterPreloadCount = 49;
        public const int CenterActiveCount = 25;
        public const int EdgePreloadCount = 28;
        public const int EdgeActiveCount = 15;
        public const int CornerPreloadCount = 16;
        public const int CornerActiveCount = 9;
        public const int ModifiedSectorCount = 1;
        public const int ModificationRecordCount = 5;
        public const int DirtyRevision = 5;
        public const int UnmodifiedSectorCount = 168;
        public const int OperationGroupCount = 10;
        public const int HashMismatchProbeCount = 6;
        public const int TransitionChangeUpperBound = 169;
        public const int ManifestPayloadByteUpperBound = 65536;

        public GeneratedTerrainPerformanceBudget(
            IEnumerable<KeyValuePair<string, int>> sourceOperationUpperBounds,
            IEnumerable<KeyValuePair<string, int>> sourceStructuralUpperBounds)
        {
            operationUpperBounds = ReadOnly(sourceOperationUpperBounds);
            structuralUpperBounds = ReadOnly(sourceStructuralUpperBounds);
            StableToken = string.Join("\n", new[] { "PERFORMANCE_BUDGET|MAP17_07|1" }
                .Concat(operationUpperBounds.Select(value => "OP|" + value.Key + "|" + Number(value.Value)))
                .Concat(structuralUpperBounds.Select(value => "STRUCTURE|" + value.Key + "|" +
                    Number(value.Value))));
        }

        public static GeneratedTerrainPerformanceBudget Reference { get; } =
            new GeneratedTerrainPerformanceBudget(new[]
            {
                Pair(GeneratedTerrainPerformanceOperation.Placement, PlacementLayerReferenceCount),
                Pair(GeneratedTerrainPerformanceOperation.LayerBake, PlacementLayerReferenceCount),
                Pair(GeneratedTerrainPerformanceOperation.SeamValidation,
                    MicroPatternSeamCount + MicroChunkSeamCount + MicroPatternOnlySeamCount),
                Pair(GeneratedTerrainPerformanceOperation.ColliderCache, 4),
                Pair(GeneratedTerrainPerformanceOperation.StreamWindow,
                    CenterPreloadCount + EdgePreloadCount + CornerPreloadCount),
                Pair(GeneratedTerrainPerformanceOperation.Transition, TransitionChangeUpperBound),
                Pair(GeneratedTerrainPerformanceOperation.ModificationStorage, ModificationRecordCount),
                Pair(GeneratedTerrainPerformanceOperation.SaveManifest, ManifestPayloadByteUpperBound),
                Pair(GeneratedTerrainPerformanceOperation.RegenApply, ModificationRecordCount),
                Pair(GeneratedTerrainPerformanceOperation.HashMismatch, HashMismatchProbeCount),
            }, new[]
            {
                Pair("full_sector_serialization", 0),
                Pair("unmodified_manifest_entries", 0),
                Pair("unity_object_ids", 0),
                Pair("file_paths", 0),
                Pair("timestamps", 0),
                Pair("frame_counts", 0),
                Pair("population_spawn_ids", 0),
                Pair("full_generator_executions", 0),
                Pair("retry_loops", 0),
                Pair("scene_mutations", 0),
                Pair("prefab_mutations", 0),
                Pair("tilemap_mutations", 0),
            });

        public IReadOnlyDictionary<string, int> OperationUpperBounds => operationUpperBounds;
        public IReadOnlyDictionary<string, int> StructuralUpperBounds => structuralUpperBounds;
        public string StableToken { get; }

        public bool TryGetOperationUpperBound(string operation, out int value) =>
            operationUpperBounds.TryGetValue(operation ?? string.Empty, out value);

        private static KeyValuePair<string, int> Pair(string key, int value) =>
            new KeyValuePair<string, int>(key, value);

        private static ReadOnlyDictionary<string, int> ReadOnly(
            IEnumerable<KeyValuePair<string, int>> source)
        {
            var result = (source ?? Array.Empty<KeyValuePair<string, int>>())
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .ToDictionary(value => value.Key ?? string.Empty, value => value.Value,
                    StringComparer.Ordinal);
            return new ReadOnlyDictionary<string, int>(result);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTerrainPerformanceMetric : IComparable<GeneratedTerrainPerformanceMetric>
    {
        public GeneratedTerrainPerformanceMetric(string name, int value)
        {
            Name = name ?? string.Empty;
            Value = value;
            StableToken = "METRIC|" + Name + "|" + Value.ToString(CultureInfo.InvariantCulture);
        }

        public string Name { get; }
        public int Value { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedTerrainPerformanceMetric other) => other == null
            ? -1 : StringComparer.Ordinal.Compare(Name, other.Name);
    }

    public sealed class GeneratedTerrainPerformanceSample : IComparable<GeneratedTerrainPerformanceSample>
    {
        private readonly ReadOnlyCollection<GeneratedTerrainPerformanceMetric> metrics;

        public GeneratedTerrainPerformanceSample(
            string operation,
            int iteration,
            int operationCount,
            long elapsedTicks,
            double elapsedMilliseconds,
            string allocationNote,
            string structuralDigest,
            IEnumerable<GeneratedTerrainPerformanceMetric> sourceMetrics)
        {
            Operation = operation ?? string.Empty;
            Iteration = iteration;
            OperationCount = operationCount;
            ElapsedTicks = Math.Max(0L, elapsedTicks);
            ElapsedMilliseconds = Math.Max(0d, elapsedMilliseconds);
            AllocationNote = allocationNote ?? string.Empty;
            StructuralDigest = structuralDigest ?? string.Empty;
            metrics = new ReadOnlyCollection<GeneratedTerrainPerformanceMetric>((sourceMetrics ??
                Array.Empty<GeneratedTerrainPerformanceMetric>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            DuplicateMetricCount = metrics.Count - metrics.Select(value => value.Name)
                .Distinct(StringComparer.Ordinal).Count();
            ObservationToken = string.Join("\n", new[]
            {
                "OBSERVATION|" + Operation + "|" + Number(OperationCount) + "|" +
                    AllocationNote + "|" + StructuralDigest,
            }.Concat(metrics.Select(value => value.StableToken)));
            ObservationDigest = BakingCanonicalDigest.HashCanonicalLines(
                ObservationToken.Split(new[] { '\n' }, StringSplitOptions.None));
            DeterministicDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "SAMPLE|" + Operation + "|" + Number(Iteration), ObservationDigest,
            });
        }

        public string Operation { get; }
        public int Iteration { get; }
        public int OperationCount { get; }
        public long ElapsedTicks { get; }
        public double ElapsedMilliseconds { get; }
        public string AllocationNote { get; }
        public string StructuralDigest { get; }
        public IReadOnlyList<GeneratedTerrainPerformanceMetric> Metrics => metrics;
        public int DuplicateMetricCount { get; }
        public string ObservationToken { get; }
        public string ObservationDigest { get; }
        public string DeterministicDigest { get; }

        public int Metric(string name) => metrics.Single(value =>
            string.Equals(value.Name, name, StringComparison.Ordinal)).Value;

        public int CompareTo(GeneratedTerrainPerformanceSample other)
        {
            if (other == null) return -1;
            var operation = GeneratedTerrainPerformanceOperation.IndexOf(Operation)
                .CompareTo(GeneratedTerrainPerformanceOperation.IndexOf(other.Operation));
            return operation != 0 ? operation : Iteration.CompareTo(other.Iteration);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTerrainPerformanceAggregate
    {
        internal GeneratedTerrainPerformanceAggregate(
            string operation,
            IEnumerable<GeneratedTerrainPerformanceSample> sourceSamples)
        {
            Operation = operation ?? string.Empty;
            var samples = (sourceSamples ?? Array.Empty<GeneratedTerrainPerformanceSample>())
                .Where(value => value != null).OrderBy(value => value.Iteration).ToArray();
            OperationCount = samples.Length == 0 ? 0 : samples[0].OperationCount;
            var elapsed = samples.Select(value => value.ElapsedMilliseconds).OrderBy(value => value).ToArray();
            MinimumMilliseconds = elapsed.Length == 0 ? 0d : elapsed[0];
            MedianMilliseconds = elapsed.Length == 0 ? 0d : elapsed[elapsed.Length / 2];
            MaximumMilliseconds = elapsed.Length == 0 ? 0d : elapsed[elapsed.Length - 1];
        }

        public string Operation { get; }
        public int OperationCount { get; }
        public double MinimumMilliseconds { get; }
        public double MedianMilliseconds { get; }
        public double MaximumMilliseconds { get; }
        public string TimingText => MinimumMilliseconds.ToString("F6", CultureInfo.InvariantCulture) + "/" +
            MedianMilliseconds.ToString("F6", CultureInfo.InvariantCulture) + "/" +
            MaximumMilliseconds.ToString("F6", CultureInfo.InvariantCulture);
    }

    public enum GeneratedTerrainPerformanceFailureCode
    {
        MissingBudget = 1,
        UnsupportedOperation = 2,
        MissingSample = 3,
        DuplicateIteration = 4,
        InvalidSample = 5,
        CountBudgetExceeded = 6,
        StructuralUpperBoundExceeded = 7,
        NonDeterministicObservation = 8,
    }

    public sealed class GeneratedTerrainPerformanceFailure : IComparable<GeneratedTerrainPerformanceFailure>
    {
        public GeneratedTerrainPerformanceFailure(
            GeneratedTerrainPerformanceFailureCode code,
            string owner,
            string offendingKey,
            string expected,
            string actual,
            string reason)
        {
            Code = code;
            Owner = owner ?? string.Empty;
            OffendingKey = offendingKey ?? string.Empty;
            Expected = expected ?? string.Empty;
            Actual = actual ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public GeneratedTerrainPerformanceFailureCode Code { get; }
        public string Owner { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string Reason { get; }
        public string StableToken => string.Join("|", new[]
            { Code.ToString(), Owner, OffendingKey, Expected, Actual, Reason });
        public int CompareTo(GeneratedTerrainPerformanceFailure other) => other == null
            ? -1 : StringComparer.Ordinal.Compare(StableToken, other.StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedTerrainPerformanceReport
    {
        private readonly ReadOnlyCollection<GeneratedTerrainPerformanceSample> samples;
        private readonly ReadOnlyCollection<GeneratedTerrainPerformanceAggregate> aggregates;
        private readonly ReadOnlyCollection<GeneratedTerrainPerformanceFailure> failures;

        public GeneratedTerrainPerformanceReport(
            GeneratedTerrainPerformanceBudget budget,
            int warmupIterations,
            int measuredIterations,
            IEnumerable<GeneratedTerrainPerformanceSample> sourceSamples)
        {
            Budget = budget;
            WarmupIterations = warmupIterations;
            MeasuredIterations = measuredIterations;
            samples = new ReadOnlyCollection<GeneratedTerrainPerformanceSample>((sourceSamples ??
                Array.Empty<GeneratedTerrainPerformanceSample>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            aggregates = new ReadOnlyCollection<GeneratedTerrainPerformanceAggregate>(
                GeneratedTerrainPerformanceOperation.All.Select(operation =>
                    new GeneratedTerrainPerformanceAggregate(operation, samples.Where(value =>
                        string.Equals(value.Operation, operation, StringComparison.Ordinal)))).ToArray());
            failures = new ReadOnlyCollection<GeneratedTerrainPerformanceFailure>(
                Validate().OrderBy(value => value).ToArray());
            Digest = ComputeDigest();
        }

        public const string SchemaVersion = "MAP17_07_TERRAIN_PERFORMANCE_REPORT_V1";
        public const string DownstreamOwner = "MAP17_08_MAP17_RUNTIME_EXIT_AUDIT";
        public const bool OpensDownstreamTask = false;
        public GeneratedTerrainPerformanceBudget Budget { get; }
        public int WarmupIterations { get; }
        public int MeasuredIterations { get; }
        public IReadOnlyList<GeneratedTerrainPerformanceSample> Samples => samples;
        public IReadOnlyList<GeneratedTerrainPerformanceAggregate> Aggregates => aggregates;
        public IReadOnlyList<GeneratedTerrainPerformanceFailure> Failures => failures;
        public int OperationGroupCount => aggregates.Count(value => value.OperationCount != 0);
        public bool Success => failures.Count == 0;
        public string Digest { get; }
        public int SystemIoFileReadCount => 0;
        public int SystemIoFileWriteCount => 0;
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
        public int GameObjectInstantiationCount => 0;
        public int GameObjectEnableCount => 0;
        public int GameObjectDisableCount => 0;
        public int GameObjectDestroyCount => 0;
        public int CameraReadCount => 0;
        public int CameraWriteCount => 0;
        public int AddressablesLoadCount => 0;
        public int ResourcesLoadCount => 0;
        public int AssetDatabaseLoadCount => 0;
        public int AuthoringCsvEditCount => 0;
        public int GeneratedCsvCommitCount => 0;
        public int GeneratedAssetCommitCount => 0;
        public int RuntimeObjectSpawnCount => 0;
        public int PopulationStableSpawnIdCount => 0;
        public int ProductionSeedApprovalCount => 0;

        public GeneratedTerrainPerformanceAggregate Operation(string operation) => aggregates.Single(value =>
            string.Equals(value.Operation, operation, StringComparison.Ordinal));

        public GeneratedTerrainPerformanceSample Sample(string operation, int iteration = 0) => samples.Single(value =>
            string.Equals(value.Operation, operation, StringComparison.Ordinal) && value.Iteration == iteration);

        private IEnumerable<GeneratedTerrainPerformanceFailure> Validate()
        {
            var result = new List<GeneratedTerrainPerformanceFailure>();
            if (Budget == null)
            {
                Add(result, GeneratedTerrainPerformanceFailureCode.MissingBudget, "report", "budget",
                    "NON_NULL", "NULL", "A performance budget is required.");
                return result;
            }
            foreach (var sample in samples)
            {
                if (!GeneratedTerrainPerformanceOperation.IsSupported(sample.Operation))
                {
                    Add(result, GeneratedTerrainPerformanceFailureCode.UnsupportedOperation, "sample",
                        sample.Operation, "SUPPORTED_OPERATION", sample.Operation,
                        "Unsupported performance operation.");
                    continue;
                }
                if (sample.Iteration < 0 || sample.OperationCount < 0 || sample.DuplicateMetricCount != 0 ||
                    !BakingCanonicalDigest.IsLowerHexSha256(sample.StructuralDigest) ||
                    !BakingCanonicalDigest.IsLowerHexSha256(sample.DeterministicDigest))
                    Add(result, GeneratedTerrainPerformanceFailureCode.InvalidSample, "sample",
                        sample.Operation, "VALID", "INVALID", "Sample fields or metrics are invalid.");
                int upperBound;
                if (!Budget.TryGetOperationUpperBound(sample.Operation, out upperBound))
                    Add(result, GeneratedTerrainPerformanceFailureCode.UnsupportedOperation, "budget",
                        sample.Operation, "BOUND", "MISSING", "Operation budget is missing.");
                else if (sample.OperationCount > upperBound)
                    Add(result, GeneratedTerrainPerformanceFailureCode.CountBudgetExceeded, "sample",
                        sample.Operation, Number(upperBound), Number(sample.OperationCount),
                        "Operation count exceeds its structural budget.");
                foreach (var bound in Budget.StructuralUpperBounds)
                {
                    var metric = sample.Metrics.SingleOrDefault(value =>
                        string.Equals(value.Name, bound.Key, StringComparison.Ordinal));
                    if (metric != null && metric.Value > bound.Value)
                        Add(result, GeneratedTerrainPerformanceFailureCode.StructuralUpperBoundExceeded,
                            "sample", sample.Operation + "/" + bound.Key, Number(bound.Value),
                            Number(metric.Value), "Structural upper bound exceeded.");
                }
            }
            foreach (var operation in GeneratedTerrainPerformanceOperation.All)
            {
                var operationSamples = samples.Where(value => string.Equals(value.Operation, operation,
                    StringComparison.Ordinal)).ToArray();
                if (operationSamples.Length != MeasuredIterations)
                    Add(result, GeneratedTerrainPerformanceFailureCode.MissingSample, "report", operation,
                        Number(MeasuredIterations), Number(operationSamples.Length),
                        "Each operation requires the configured measured iteration count.");
                if (operationSamples.Select(value => value.Iteration).Distinct().Count() !=
                    operationSamples.Length)
                    Add(result, GeneratedTerrainPerformanceFailureCode.DuplicateIteration, "report", operation,
                        "UNIQUE", "DUPLICATE", "Operation iterations must be unique.");
                if (operationSamples.Select(value => value.ObservationDigest)
                    .Distinct(StringComparer.Ordinal).Count() > 1)
                    Add(result, GeneratedTerrainPerformanceFailureCode.NonDeterministicObservation, "report",
                        operation, "ONE_OBSERVATION_DIGEST", "MULTIPLE",
                        "Measured iterations returned different structural observations.");
            }
            return result;
        }

        private string ComputeDigest()
        {
            var lines = new List<string>
            {
                "PERFORMANCE_REPORT|" + SchemaVersion + "|" + Number(WarmupIterations) + "|" +
                    Number(MeasuredIterations),
                Budget == null ? "BUDGET|MISSING" : Budget.StableToken,
            };
            lines.AddRange(samples.Select(value => "SAMPLE_DIGEST|" + value.DeterministicDigest));
            lines.AddRange(failures.Select(value => "FAILURE|" + value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static void Add(ICollection<GeneratedTerrainPerformanceFailure> target,
            GeneratedTerrainPerformanceFailureCode code, string owner, string key,
            string expected, string actual, string reason) => target.Add(
                new GeneratedTerrainPerformanceFailure(code, owner, key, expected, actual, reason));

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
