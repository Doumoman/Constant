using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Validation
{
    public enum GeneratedValidationRunnerMode
    {
        Plan = 0,
        Replay = 1,
        Range = 2,
    }

    public enum GeneratedValidationRunnerExitCode
    {
        Success = 0,
        ValidationFailed = 1,
        InvalidArguments = 2,
        PreconditionMismatch = 3,
        OutputWriteFailed = 4,
        InternalError = 5,
    }

    public sealed class GeneratedValidationSurfaceReference : IComparable<GeneratedValidationSurfaceReference>
    {
        public GeneratedValidationSurfaceReference(string phaseId, object surface, string digest)
        {
            PhaseId = N(phaseId);
            Surface = surface;
            Digest = N(digest);
        }

        public string PhaseId { get; }
        public object Surface { get; }
        public string Digest { get; }
        public bool IsValid => GeneratedFailureBundleVersionIdentity.IsToken(PhaseId) && Surface != null &&
            BakingCanonicalDigest.IsLowerHexSha256(Digest);
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join(PhaseId, Digest);
        public int CompareTo(GeneratedValidationSurfaceReference other) => other == null ? 1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        private static string N(string value) => GeneratedFailureBundleSeedIdentity.Normalize(value);
    }

    public sealed class GeneratedValidationChainSnapshot
    {
        public const string Map19_02GraphDigest =
            "bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063";
        public const string Map19_03CompletionDigest =
            "6bc3bf96b766337011fd08d3436fd46e12b30ae7b0af5cfc0598646e8e338fca";
        public const string Map19_04CombinedDigest =
            "6e393133118683be5104af6e4a4f2eff39cee61dc2c1dc8c240fba969512b816";
        public const string Map19_05CombinedDigest =
            "3a3e02bb35f99c052b18588fa4ed6a0bdcf01bc8cac860721f28908115c840fa";
        public const string Map19_06CombinedDigest =
            "9f8e46d45071e7ed2e3b189a0467a1381aa7eb05a516471b4c4337f52ad6420d";
        public const string Map19_07CombinedDigest =
            "b937f9db37715d37cecdee3e313f5ec2c016b7d9bf3c2d8e5423b3c8205429b1";
        public const string Map19_08IncomingHandoffDigest =
            "d732453f06b6dc972f6e7c34ac3f8cf86ccc285f50c8e7afca65018fd0515090";

        private static readonly string[] PhaseIds =
        {
            "MAP19_02", "MAP19_03", "MAP19_04", "MAP19_05", "MAP19_06", "MAP19_07",
        };
        private static readonly string[] ExpectedDigests =
        {
            Map19_02GraphDigest, Map19_03CompletionDigest, Map19_04CombinedDigest,
            Map19_05CombinedDigest, Map19_06CombinedDigest, Map19_07CombinedDigest,
        };
        private readonly ReadOnlyCollection<GeneratedValidationSurfaceReference> references;

        public GeneratedValidationChainSnapshot(IEnumerable<GeneratedValidationSurfaceReference> source,
            string incomingHandoffDigest)
        {
            references = new ReadOnlyCollection<GeneratedValidationSurfaceReference>((source ??
                Array.Empty<GeneratedValidationSurfaceReference>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            IncomingHandoffDigest = GeneratedFailureBundleSeedIdentity.Normalize(incomingHandoffDigest);
            Digest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_08_CHAIN_SNAPSHOT_V1", IncomingHandoffDigest,
            }.Concat(references.Select(value => value.StableToken)));
        }

        public static IReadOnlyList<string> RequiredPhaseIds => Array.AsReadOnly(PhaseIds);
        public IReadOnlyList<GeneratedValidationSurfaceReference> References => references;
        public string IncomingHandoffDigest { get; }
        public string Digest { get; }

        public IReadOnlyList<GeneratedValidationRunnerFailure> Validate(string requestId)
        {
            var failures = new List<GeneratedValidationRunnerFailure>();
            if (!string.Equals(IncomingHandoffDigest, Map19_08IncomingHandoffDigest,
                StringComparison.Ordinal))
                failures.Add(Failure("MAP19_07", "HANDOFF_DIGEST_MISMATCH", requestId,
                    "incomingHandoffDigest", Map19_08IncomingHandoffDigest, IncomingHandoffDigest));
            for (var index = 0; index < PhaseIds.Length; index++)
            {
                var found = references.Where(value => string.Equals(value.PhaseId, PhaseIds[index],
                    StringComparison.Ordinal)).ToArray();
                if (found.Length != 1 || !found[0].IsValid ||
                    !string.Equals(found[0].Digest, ExpectedDigests[index], StringComparison.Ordinal))
                    failures.Add(Failure(PhaseIds[index], "SURFACE_OR_DIGEST_MISMATCH", requestId,
                        PhaseIds[index], ExpectedDigests[index], found.Length == 1 ? found[0].Digest : "MISSING"));
            }
            return new ReadOnlyCollection<GeneratedValidationRunnerFailure>(failures.OrderBy(value => value).ToArray());
        }

        public IReadOnlyList<GeneratedFailureBundleUpstreamDigest> BundleDigests() =>
            new ReadOnlyCollection<GeneratedFailureBundleUpstreamDigest>(references.Select(value =>
                new GeneratedFailureBundleUpstreamDigest(value.PhaseId, value.Digest)).OrderBy(value => value).ToArray());

        private static GeneratedValidationRunnerFailure Failure(string owner, string reason, string id,
            string key, string expected, string actual) => new GeneratedValidationRunnerFailure(owner,
                reason, id, key, expected, string.IsNullOrEmpty(actual) ? "MISSING" : actual,
                BakingCanonicalDigest.HashCanonicalLines(new[] { owner, reason, key }));
    }

    public sealed class GeneratedValidationRunnerFailure : IComparable<GeneratedValidationRunnerFailure>
    {
        public GeneratedValidationRunnerFailure(string owner, string reason, string requestId,
            string offendingKey, string expected, string actual, string sourceDigest, string outputPath = "")
        {
            Owner = N(owner);
            Reason = N(reason);
            RequestId = N(requestId);
            OffendingKey = N(offendingKey);
            Expected = N(expected);
            Actual = N(actual);
            SourceDigest = N(sourceDigest);
            OutputPath = N(outputPath);
        }

        public string Owner { get; }
        public string Reason { get; }
        public string RequestId { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string SourceDigest { get; }
        public string OutputPath { get; }
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join(Owner, Reason, RequestId,
            OffendingKey, Expected, Actual, SourceDigest, OutputPath);
        public int CompareTo(GeneratedValidationRunnerFailure other) => other == null ? 1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        private static string N(string value) => GeneratedFailureBundleSeedIdentity.Normalize(value);
    }

    public sealed class GeneratedValidationRunnerArguments
    {
        internal GeneratedValidationRunnerArguments(GeneratedValidationRunnerMode mode, ulong seed,
            bool hasSeed, ulong seedStart, ulong seedCount, int workerIndex, int workerCount,
            string outputRoot, bool failFast, string profile, string expectedGeneratorVersion,
            string expectedDataVersion)
        {
            Mode = mode;
            Seed = seed;
            HasSeed = hasSeed;
            SeedStart = seedStart;
            SeedCount = seedCount;
            WorkerIndex = workerIndex;
            WorkerCount = workerCount;
            OutputRoot = N(outputRoot);
            FailFast = failFast;
            Profile = N(profile);
            ExpectedGeneratorVersion = N(expectedGeneratorVersion);
            ExpectedDataVersion = N(expectedDataVersion);
        }

        public GeneratedValidationRunnerMode Mode { get; }
        public ulong Seed { get; }
        public bool HasSeed { get; }
        public ulong SeedStart { get; }
        public ulong SeedCount { get; }
        public int WorkerIndex { get; }
        public int WorkerCount { get; }
        public string OutputRoot { get; }
        public bool FailFast { get; }
        public string Profile { get; }
        public string ExpectedGeneratorVersion { get; }
        public string ExpectedDataVersion { get; }
        public string RequestId => BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP19_08_REQUEST_V1", StableToken });
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join(
            GeneratedFailureBundleSeedIdentity.Number((int)Mode),
            GeneratedFailureBundleSeedIdentity.Number(Seed), HasSeed ? "1" : "0",
            GeneratedFailureBundleSeedIdentity.Number(SeedStart),
            GeneratedFailureBundleSeedIdentity.Number(SeedCount),
            GeneratedFailureBundleSeedIdentity.Number(WorkerIndex),
            GeneratedFailureBundleSeedIdentity.Number(WorkerCount), OutputRoot,
            FailFast ? "1" : "0", Profile, ExpectedGeneratorVersion, ExpectedDataVersion);
        private static string N(string value) => GeneratedFailureBundleSeedIdentity.Normalize(value);
    }

    public sealed class GeneratedValidationRunnerParseResult
    {
        internal GeneratedValidationRunnerParseResult(GeneratedValidationRunnerArguments arguments,
            IEnumerable<GeneratedValidationRunnerFailure> failures)
        {
            Arguments = arguments;
            Failures = new ReadOnlyCollection<GeneratedValidationRunnerFailure>((failures ??
                Array.Empty<GeneratedValidationRunnerFailure>()).OrderBy(value => value).ToArray());
        }
        public bool Success => Arguments != null && Failures.Count == 0;
        public GeneratedValidationRunnerArguments Arguments { get; }
        public IReadOnlyList<GeneratedValidationRunnerFailure> Failures { get; }
        public GeneratedValidationRunnerExitCode ExitCode => Success
            ? GeneratedValidationRunnerExitCode.Success : GeneratedValidationRunnerExitCode.InvalidArguments;
    }

    public static class GeneratedValidationRunnerArgumentParser
    {
        public static string SchemaDigest => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            "MAP19_08_RUNNER_ARGUMENTS_V1", "--mode=plan|replay|range", "--seed=uint64",
            "--seed-start=uint64", "--seed-count=uint64", "--worker-index=int32",
            "--worker-count=int32", "--output=text", "--fail-fast=bool", "--profile=text",
            "--expected-generator-version=text", "--expected-data-version=text",
        });

        public static GeneratedValidationRunnerParseResult Parse(IEnumerable<string> source)
        {
            var values = (source ?? Array.Empty<string>()).ToArray();
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var failures = new List<GeneratedValidationRunnerFailure>();
            for (var index = 0; index < values.Length; index++)
            {
                var key = values[index] ?? string.Empty;
                if (!key.StartsWith("--", StringComparison.Ordinal))
                {
                    failures.Add(F("UNEXPECTED_ARGUMENT", key, "--name", key));
                    continue;
                }
                if (string.Equals(key, "--fail-fast", StringComparison.Ordinal))
                {
                    if (map.ContainsKey(key)) failures.Add(F("DUPLICATE_ARGUMENT", key, "ONCE", "DUPLICATE"));
                    else map[key] = "true";
                    continue;
                }
                if (index + 1 >= values.Length || (values[index + 1] ?? string.Empty).StartsWith("--",
                    StringComparison.Ordinal))
                {
                    failures.Add(F("MISSING_ARGUMENT_VALUE", key, "VALUE", "MISSING"));
                    continue;
                }
                if (map.ContainsKey(key)) failures.Add(F("DUPLICATE_ARGUMENT", key, "ONCE", "DUPLICATE"));
                else map[key] = values[++index] ?? string.Empty;
            }

            var supported = new HashSet<string>(new[] { "--mode", "--seed", "--seed-start",
                "--seed-count", "--worker-index", "--worker-count", "--output", "--fail-fast",
                "--profile", "--expected-generator-version", "--expected-data-version" },
                StringComparer.Ordinal);
            foreach (var key in map.Keys.Where(key => !supported.Contains(key)))
                failures.Add(F("UNKNOWN_ARGUMENT", key, string.Join(",", supported.OrderBy(v => v)), key));

            GeneratedValidationRunnerMode mode;
            var modeText = Get(map, "--mode");
            if (!Enum.TryParse(modeText, true, out mode) || !Enum.IsDefined(typeof(GeneratedValidationRunnerMode), mode))
            {
                failures.Add(F("INVALID_MODE", "--mode", "plan|replay|range", modeText));
                mode = GeneratedValidationRunnerMode.Plan;
            }
            ulong seed = 0, seedStart = 0, seedCount = 0;
            int workerIndex = 0, workerCount = 1;
            var hasSeed = map.ContainsKey("--seed") && U64(Get(map, "--seed"), out seed);
            if (map.ContainsKey("--seed") && !hasSeed) failures.Add(F("INVALID_SEED", "--seed", "UINT64", Get(map, "--seed")));
            if (map.ContainsKey("--seed-start") && !U64(Get(map, "--seed-start"), out seedStart))
                failures.Add(F("INVALID_SEED_START", "--seed-start", "UINT64", Get(map, "--seed-start")));
            if (map.ContainsKey("--seed-count") && (!U64(Get(map, "--seed-count"), out seedCount) || seedCount == 0))
                failures.Add(F("INVALID_SEED_COUNT", "--seed-count", "POSITIVE_UINT64", Get(map, "--seed-count")));
            if (map.ContainsKey("--worker-index") && !I32(Get(map, "--worker-index"), out workerIndex))
                failures.Add(F("INVALID_WORKER_INDEX", "--worker-index", "INT32", Get(map, "--worker-index")));
            if (map.ContainsKey("--worker-count") && !I32(Get(map, "--worker-count"), out workerCount))
                failures.Add(F("INVALID_WORKER_COUNT", "--worker-count", "POSITIVE_INT32", Get(map, "--worker-count")));
            if (workerCount <= 0 || workerIndex < 0 || workerIndex >= workerCount)
                failures.Add(F("INVALID_WORKER_PARTITION", "worker", "0<=index<count", workerIndex + "/" + workerCount));
            if (mode == GeneratedValidationRunnerMode.Replay && !hasSeed)
                failures.Add(F("MISSING_REPLAY_SEED", "--seed", "REQUIRED", "MISSING"));
            if ((mode == GeneratedValidationRunnerMode.Range || mode == GeneratedValidationRunnerMode.Plan) &&
                seedCount == 0)
                failures.Add(F("MISSING_RANGE", "--seed-count", "POSITIVE", "MISSING"));
            if (seedCount > 0 && seedStart > ulong.MaxValue - (seedCount - 1))
                failures.Add(F("RANGE_OVERFLOW", "--seed-count", "RANGE_WITHIN_UINT64",
                    seedStart + "+" + seedCount));
            foreach (var required in new[] { "--output", "--profile", "--expected-generator-version",
                "--expected-data-version" })
                if (!GeneratedFailureBundleVersionIdentity.IsToken(Get(map, required)))
                    failures.Add(F("MISSING_REQUIRED_ARGUMENT", required, "STABLE_TOKEN", "MISSING"));
            if (failures.Count != 0) return new GeneratedValidationRunnerParseResult(null, failures);
            return new GeneratedValidationRunnerParseResult(new GeneratedValidationRunnerArguments(mode,
                seed, hasSeed, seedStart, seedCount, workerIndex, workerCount, Get(map, "--output"),
                string.Equals(Get(map, "--fail-fast"), "true", StringComparison.OrdinalIgnoreCase),
                Get(map, "--profile"), Get(map, "--expected-generator-version"),
                Get(map, "--expected-data-version")), Array.Empty<GeneratedValidationRunnerFailure>());
        }

        private static GeneratedValidationRunnerFailure F(string reason, string key, string expected,
            string actual) => new GeneratedValidationRunnerFailure("RunnerArguments", reason, "UNPARSED",
                key, expected, string.IsNullOrEmpty(actual) ? "MISSING" : actual,
                BakingCanonicalDigest.HashCanonicalLines(new[] { reason, key, expected }));
        private static string Get(IDictionary<string, string> values, string key) =>
            values.TryGetValue(key, out var value) ? value : string.Empty;
        private static bool U64(string value, out ulong result) => ulong.TryParse(value,
            NumberStyles.None, CultureInfo.InvariantCulture, out result);
        private static bool I32(string value, out int result) => int.TryParse(value,
            NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    public sealed class GeneratedValidationRunnerRange
    {
        public GeneratedValidationRunnerRange(ulong seedStart, ulong seedCount)
        {
            SeedStart = seedStart;
            SeedCount = seedCount;
        }
        public ulong SeedStart { get; }
        public ulong SeedCount { get; }
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join(
            GeneratedFailureBundleSeedIdentity.Number(SeedStart),
            GeneratedFailureBundleSeedIdentity.Number(SeedCount));
    }

    public sealed class GeneratedValidationRunnerWorkerPartition : IComparable<GeneratedValidationRunnerWorkerPartition>
    {
        public GeneratedValidationRunnerWorkerPartition(int workerIndex, int workerCount,
            ulong seedStart, ulong seedCount)
        {
            WorkerIndex = workerIndex;
            WorkerCount = workerCount;
            Range = new GeneratedValidationRunnerRange(seedStart, seedCount);
        }
        public int WorkerIndex { get; }
        public int WorkerCount { get; }
        public GeneratedValidationRunnerRange Range { get; }
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join(
            GeneratedFailureBundleSeedIdentity.Number(WorkerIndex),
            GeneratedFailureBundleSeedIdentity.Number(WorkerCount), Range.StableToken);
        public int CompareTo(GeneratedValidationRunnerWorkerPartition other) => other == null ? 1 :
            WorkerIndex.CompareTo(other.WorkerIndex);
    }

    public sealed class GeneratedValidationRunnerReplayRequest
    {
        public GeneratedValidationRunnerReplayRequest(ulong seed, string profile, string outputRoot)
        {
            Seed = seed;
            Profile = GeneratedFailureBundleSeedIdentity.Normalize(profile);
            OutputRoot = GeneratedFailureBundleSeedIdentity.Normalize(outputRoot);
        }
        public ulong Seed { get; }
        public string Profile { get; }
        public string OutputRoot { get; }
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join(
            GeneratedFailureBundleSeedIdentity.Number(Seed), Profile, OutputRoot);
    }

    public sealed class GeneratedValidationRunnerPlan
    {
        private readonly ReadOnlyCollection<GeneratedValidationRunnerWorkerPartition> partitions;
        private readonly ReadOnlyCollection<string> phaseOrder;

        internal GeneratedValidationRunnerPlan(GeneratedValidationRunnerArguments arguments,
            IEnumerable<GeneratedValidationRunnerWorkerPartition> workerPartitions,
            GeneratedValidationRunnerReplayRequest replayRequest,
            GeneratedValidationChainSnapshot chain)
        {
            Arguments = arguments;
            partitions = new ReadOnlyCollection<GeneratedValidationRunnerWorkerPartition>(
                workerPartitions.OrderBy(value => value).ToArray());
            ReplayRequest = replayRequest;
            Chain = chain;
            phaseOrder = new ReadOnlyCollection<string>(GeneratedValidationChainSnapshot.RequiredPhaseIds.ToArray());
            PlanDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_08_RUNNER_PLAN_V1", GeneratedValidationRunnerArgumentParser.SchemaDigest,
                arguments.StableToken, chain.Digest,
                replayRequest == null ? "NO_REPLAY" : replayRequest.StableToken,
            }.Concat(phaseOrder).Concat(partitions.Select(value => value.StableToken)));
        }

        public GeneratedValidationRunnerArguments Arguments { get; }
        public IReadOnlyList<GeneratedValidationRunnerWorkerPartition> WorkerPartitions => partitions;
        public GeneratedValidationRunnerReplayRequest ReplayRequest { get; }
        public GeneratedValidationChainSnapshot Chain { get; }
        public IReadOnlyList<string> PhaseOrder => phaseOrder;
        public string PlanDigest { get; }

        public string BuildMap19_09HandoffDigest(GeneratedFailureBundlePayload payload) =>
            BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_09_HANDOFF_V1", GeneratedFailureBundleDigest.Schema,
                payload.Manifest.ManifestDigest, payload.PayloadDigest,
                GeneratedValidationRunnerArgumentParser.SchemaDigest, PlanDigest,
            });
    }

    public sealed class GeneratedValidationRunnerResult
    {
        internal GeneratedValidationRunnerResult(GeneratedValidationRunnerPlan plan,
            GeneratedValidationRunnerExitCode exitCode,
            IEnumerable<GeneratedValidationRunnerFailure> failures)
        {
            Plan = plan;
            ExitCode = exitCode;
            Failures = new ReadOnlyCollection<GeneratedValidationRunnerFailure>((failures ??
                Array.Empty<GeneratedValidationRunnerFailure>()).OrderBy(value => value).ToArray());
        }
        public bool Success => Plan != null && ExitCode == GeneratedValidationRunnerExitCode.Success &&
            Failures.Count == 0;
        public GeneratedValidationRunnerPlan Plan { get; }
        public GeneratedValidationRunnerExitCode ExitCode { get; }
        public IReadOnlyList<GeneratedValidationRunnerFailure> Failures { get; }
        public string SuccessPlanDigest => Success ? Plan.PlanDigest : string.Empty;
    }

    public static class GeneratedValidationRunnerPlanner
    {
        public static GeneratedValidationRunnerResult Build(GeneratedValidationRunnerArguments arguments,
            GeneratedValidationChainSnapshot chain)
        {
            if (arguments == null)
                return Failure(GeneratedValidationRunnerExitCode.InvalidArguments, "MISSING_ARGUMENTS",
                    "arguments", "NON_NULL", "NULL", "MISSING");
            if (chain == null)
                return Failure(GeneratedValidationRunnerExitCode.PreconditionMismatch, "MISSING_CHAIN",
                    "chain", "NON_NULL", "NULL", arguments.RequestId);
            var chainFailures = chain.Validate(arguments.RequestId);
            if (chainFailures.Count != 0)
                return new GeneratedValidationRunnerResult(null,
                    GeneratedValidationRunnerExitCode.PreconditionMismatch, chainFailures);
            var partitions = Partition(arguments.SeedStart, arguments.SeedCount, arguments.WorkerCount).ToArray();
            var selected = partitions.Where(value => value.WorkerIndex == arguments.WorkerIndex).ToArray();
            var replay = arguments.Mode == GeneratedValidationRunnerMode.Replay
                ? new GeneratedValidationRunnerReplayRequest(arguments.Seed, arguments.Profile,
                    arguments.OutputRoot) : null;
            return new GeneratedValidationRunnerResult(new GeneratedValidationRunnerPlan(arguments,
                selected, replay, chain), GeneratedValidationRunnerExitCode.Success,
                Array.Empty<GeneratedValidationRunnerFailure>());
        }

        public static IReadOnlyList<GeneratedValidationRunnerWorkerPartition> Partition(ulong seedStart,
            ulong seedCount, int workerCount)
        {
            if (seedCount == 0 || workerCount <= 0) return Array.Empty<GeneratedValidationRunnerWorkerPartition>();
            var values = new List<GeneratedValidationRunnerWorkerPartition>();
            var count = (ulong)workerCount;
            var baseSize = seedCount / count;
            var remainder = seedCount % count;
            var cursor = seedStart;
            for (var index = 0; index < workerCount; index++)
            {
                var size = baseSize + ((ulong)index < remainder ? 1UL : 0UL);
                values.Add(new GeneratedValidationRunnerWorkerPartition(index, workerCount, cursor, size));
                if (index + 1 < workerCount) cursor = checked(cursor + size);
            }
            return new ReadOnlyCollection<GeneratedValidationRunnerWorkerPartition>(values);
        }

        private static GeneratedValidationRunnerResult Failure(GeneratedValidationRunnerExitCode code,
            string reason, string key, string expected, string actual, string requestId) =>
            new GeneratedValidationRunnerResult(null, code, new[]
            {
                new GeneratedValidationRunnerFailure("RunnerPlan", reason, requestId, key, expected,
                    actual, BakingCanonicalDigest.HashCanonicalLines(new[] { reason, key, expected }))
            });
    }
}
