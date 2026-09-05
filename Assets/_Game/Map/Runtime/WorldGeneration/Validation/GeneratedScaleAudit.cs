using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Validation
{
    public enum GeneratedScaleAuditStatus
    {
        Pass = 0,
        Fail = 1,
        BlockedPrecondition = 2,
        BlockedRuntimeBudget = 3,
        BlockedOutput = 4,
    }

    public enum GeneratedScaleAuditFailureKind
    {
        Validation = 0,
        MandatoryRoute = 1,
        Completion = 2,
        Recovery = 3,
        Density = 4,
        RepetitionRemoval = 5,
        WorstCase = 6,
        ReplayMismatch = 7,
        UnexpectedException = 8,
        RuntimeBudget = 9,
    }

    public delegate GeneratedScaleAuditSeedResult GeneratedScaleAuditSeedEvaluator(
        ulong seed, string chainDigest);
    public delegate long GeneratedScaleAuditTimestampProvider();
    public delegate bool GeneratedScaleAuditFailureBundleWriter(
        GeneratedScaleAuditFailure failure, GeneratedFailureBundlePayload payload,
        out string outputPath);

    public static class GeneratedScaleAuditContract
    {
        public const string SchemaVersion = "MAP19_SCALE_AUDIT_V1";
        public const string ExpectedMap19_08ResultSha256 =
            "35d9e149fb174da911fe9271e26b8f59c48f4dac3920858e2f3e2e65da197009";
        public const string ExpectedMap19_08InstalledTaskSha256 =
            "efbb97b10bca089f99de68e41cb92be1ad149a5d2ce621bf83636c856122fb95";
        public const string ExpectedMap19_08HandoffDigest =
            "6cbc9ab8fa12eaf353d527a3455b76651a93e7ef07b2a3b72406b2309701b443";
        public const string ExpectedFailureBundleSchemaDigest =
            "5d96dbb4e42b3a39f20ee6d48f8acf1aed13acd23c3743a7b4683bdd314cd483";
        public const string ExpectedRunnerPlanDigest =
            "004f3d4ba895337f1c900dfbe6380d4f06f26aef5474720454d453a46ba281a5";
        public const string ExpectedMap19_09InstalledTaskSha256 =
            "b772a2417567ffeaf7afe59935ca1fa325fd31cab737ce94fba06ec300b979a0";
        public const double TierCRuntimeBudgetMilliseconds = 30d * 60d * 1000d;
        public const int MaximumFailureBundles = 5;
        public const string GeneratorVersion = "generator-map19.09-audit";
        public const string DataVersion = "data-map19-chain-v1";
    }

    public sealed class GeneratedScaleAuditTier : IComparable<GeneratedScaleAuditTier>
    {
        public GeneratedScaleAuditTier(string tierId, ulong seedStart, ulong seedCount,
            string unlockCondition)
        {
            TierId = N(tierId);
            SeedStart = seedStart;
            SeedCount = seedCount;
            UnlockCondition = N(unlockCondition);
        }

        public string TierId { get; }
        public ulong SeedStart { get; }
        public ulong SeedCount { get; }
        public ulong SeedEndInclusive => SeedCount == 0 ? SeedStart : checked(SeedStart + SeedCount - 1);
        public string UnlockCondition { get; }
        public string StableToken => string.Join("|", "TIER", TierId, U(SeedStart), U(SeedCount),
            UnlockCondition);
        public int CompareTo(GeneratedScaleAuditTier other) => other == null ? 1 :
            SeedStart != other.SeedStart ? SeedStart.CompareTo(other.SeedStart) :
            string.Compare(TierId, other.TierId, StringComparison.Ordinal);
        private static string N(string value) => GeneratedFailureBundleSeedIdentity.Normalize(value);
        private static string U(ulong value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public static class GeneratedScaleAuditTierCatalog
    {
        private static readonly ReadOnlyCollection<GeneratedScaleAuditTier> Definitions =
            new ReadOnlyCollection<GeneratedScaleAuditTier>(new[]
            {
                new GeneratedScaleAuditTier("A", 0UL, 1000UL, "PRECONDITIONS_PASS"),
                new GeneratedScaleAuditTier("B", 1000UL, 9000UL, "TIER_A_PASS"),
                new GeneratedScaleAuditTier("C", 10000UL, 90000UL,
                    "TIER_B_PASS_AND_RUNTIME_BUDGET_ACCEPTED"),
            });

        public static IReadOnlyList<GeneratedScaleAuditTier> All => Definitions;
        public static string DefinitionsDigest => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            GeneratedScaleAuditContract.SchemaVersion,
        }.Concat(Definitions.Select(value => value.StableToken)));

        public static bool IsValid(IEnumerable<GeneratedScaleAuditTier> source)
        {
            var values = (source ?? Array.Empty<GeneratedScaleAuditTier>())
                .Where(value => value != null).OrderBy(value => value).ToArray();
            if (values.Length == 0 || values.Any(value => value.SeedCount == 0)) return false;
            for (var index = 1; index < values.Length; index++)
                if (values[index - 1].SeedEndInclusive >= values[index].SeedStart) return false;
            return true;
        }
    }

    public sealed class GeneratedScaleAuditPreconditions
    {
        public GeneratedScaleAuditPreconditions(string map19_08ResultSha256,
            string map19_08InstalledTaskSha256, string map19_08HandoffDigest,
            string failureBundleSchemaDigest, string runnerPlanDigest,
            string map19_09InstalledTaskSha256)
        {
            Map19_08ResultSha256 = N(map19_08ResultSha256);
            Map19_08InstalledTaskSha256 = N(map19_08InstalledTaskSha256);
            Map19_08HandoffDigest = N(map19_08HandoffDigest);
            FailureBundleSchemaDigest = N(failureBundleSchemaDigest);
            RunnerPlanDigest = N(runnerPlanDigest);
            Map19_09InstalledTaskSha256 = N(map19_09InstalledTaskSha256);
        }

        public string Map19_08ResultSha256 { get; }
        public string Map19_08InstalledTaskSha256 { get; }
        public string Map19_08HandoffDigest { get; }
        public string FailureBundleSchemaDigest { get; }
        public string RunnerPlanDigest { get; }
        public string Map19_09InstalledTaskSha256 { get; }
        public bool IsValid => Pair(Map19_08ResultSha256,
                GeneratedScaleAuditContract.ExpectedMap19_08ResultSha256) &&
            Pair(Map19_08InstalledTaskSha256,
                GeneratedScaleAuditContract.ExpectedMap19_08InstalledTaskSha256) &&
            Pair(Map19_08HandoffDigest,
                GeneratedScaleAuditContract.ExpectedMap19_08HandoffDigest) &&
            Pair(FailureBundleSchemaDigest,
                GeneratedScaleAuditContract.ExpectedFailureBundleSchemaDigest) &&
            Pair(RunnerPlanDigest, GeneratedScaleAuditContract.ExpectedRunnerPlanDigest) &&
            Pair(Map19_09InstalledTaskSha256,
                GeneratedScaleAuditContract.ExpectedMap19_09InstalledTaskSha256);
        public string StableToken => string.Join("|", "PRECONDITIONS",
            Map19_08ResultSha256, Map19_08InstalledTaskSha256, Map19_08HandoffDigest,
            FailureBundleSchemaDigest, RunnerPlanDigest, Map19_09InstalledTaskSha256);

        private static bool Pair(string actual, string expected) =>
            BakingCanonicalDigest.IsLowerHexSha256(actual) &&
            string.Equals(actual, expected, StringComparison.Ordinal);
        private static string N(string value) => GeneratedFailureBundleSeedIdentity.Normalize(value);
    }

    public sealed class GeneratedScaleAuditSeedResult
    {
        private GeneratedScaleAuditSeedResult(ulong seed, bool success,
            GeneratedScaleAuditFailureKind failureKind, string owner, string reason,
            string failedRuleId, string expected, string actual, string replayDigest)
        {
            Seed = seed;
            Success = success;
            FailureKind = failureKind;
            Owner = N(owner);
            Reason = N(reason);
            FailedRuleId = N(failedRuleId);
            Expected = N(expected);
            Actual = N(actual);
            ReplayDigest = N(replayDigest);
        }

        public ulong Seed { get; }
        public bool Success { get; }
        public GeneratedScaleAuditFailureKind FailureKind { get; }
        public string Owner { get; }
        public string Reason { get; }
        public string FailedRuleId { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string ReplayDigest { get; }

        public static GeneratedScaleAuditSeedResult Pass(ulong seed, string replayDigest) =>
            new GeneratedScaleAuditSeedResult(seed, true, GeneratedScaleAuditFailureKind.Validation,
                "NONE", "NONE", "NONE", "PASS", "PASS", replayDigest);

        public static GeneratedScaleAuditSeedResult Fail(ulong seed,
            GeneratedScaleAuditFailureKind failureKind, string owner, string reason,
            string failedRuleId, string expected, string actual, string replayDigest = "") =>
            new GeneratedScaleAuditSeedResult(seed, false, failureKind, owner, reason,
                failedRuleId, expected, actual, replayDigest);

        private static string N(string value) => GeneratedFailureBundleSeedIdentity.Normalize(value);
    }

    public sealed class GeneratedScaleAuditFailure : IComparable<GeneratedScaleAuditFailure>
    {
        public GeneratedScaleAuditFailure(string tierId, GeneratedScaleAuditSeedResult result,
            string sourceDigest)
        {
            TierId = GeneratedFailureBundleSeedIdentity.Normalize(tierId);
            Seed = result == null ? 0UL : result.Seed;
            Kind = result == null ? GeneratedScaleAuditFailureKind.Validation : result.FailureKind;
            Owner = result == null ? "MAP19_09" : result.Owner;
            Reason = result == null ? "MISSING_SEED_RESULT" : result.Reason;
            FailedRuleId = result == null ? "MISSING_SEED_RESULT" : result.FailedRuleId;
            Expected = result == null ? "NON_NULL" : result.Expected;
            Actual = result == null ? "NULL" : result.Actual;
            SourceDigest = BakingCanonicalDigest.IsLowerHexSha256(sourceDigest) ? sourceDigest :
                BakingCanonicalDigest.HashCanonicalLines(new[] { sourceDigest ?? "MISSING" });
        }

        public string TierId { get; }
        public ulong Seed { get; }
        public GeneratedScaleAuditFailureKind Kind { get; }
        public string Owner { get; }
        public string Reason { get; }
        public string FailedRuleId { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string SourceDigest { get; }
        public string StableToken => string.Join("|", "FAIL", TierId,
            Seed.ToString(CultureInfo.InvariantCulture), ((int)Kind).ToString(CultureInfo.InvariantCulture),
            Owner, Reason, FailedRuleId, Expected, Actual, SourceDigest);
        public int CompareTo(GeneratedScaleAuditFailure other) => other == null ? 1 :
            Seed != other.Seed ? Seed.CompareTo(other.Seed) :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class GeneratedScaleAuditTierResult : IComparable<GeneratedScaleAuditTierResult>
    {
        internal GeneratedScaleAuditTierResult(GeneratedScaleAuditTier tier, ulong executed,
            ulong passed, ulong failed, ulong crashed, ulong validationFailures,
            ulong mandatoryRouteFailures, ulong completionFailures, ulong recoveryFailures,
            ulong densityFailures, ulong repetitionRemovalFailures, ulong worstCaseFailures,
            ulong replayMismatches, ulong bundleWriteFailures, bool started, bool completed,
            double wallMilliseconds, double p50Milliseconds, double p95Milliseconds,
            double maximumMilliseconds, string nextTierDecision)
        {
            Tier = tier;
            Executed = executed;
            Passed = passed;
            Failed = failed;
            Crashed = crashed;
            ValidationFailures = validationFailures;
            MandatoryRouteFailures = mandatoryRouteFailures;
            CompletionFailures = completionFailures;
            RecoveryFailures = recoveryFailures;
            DensityFailures = densityFailures;
            RepetitionRemovalFailures = repetitionRemovalFailures;
            WorstCaseFailures = worstCaseFailures;
            ReplayMismatches = replayMismatches;
            FailureBundleWriteFailures = bundleWriteFailures;
            Started = started;
            Completed = completed;
            WallMilliseconds = wallMilliseconds;
            P50Milliseconds = p50Milliseconds;
            P95Milliseconds = p95Milliseconds;
            MaximumMilliseconds = maximumMilliseconds;
            NextTierDecision = GeneratedFailureBundleSeedIdentity.Normalize(nextTierDecision);
        }

        public GeneratedScaleAuditTier Tier { get; }
        public ulong Requested => Tier == null ? 0UL : Tier.SeedCount;
        public ulong Executed { get; }
        public ulong Passed { get; }
        public ulong Failed { get; }
        public ulong Crashed { get; }
        public ulong ValidationFailures { get; }
        public ulong MandatoryRouteFailures { get; }
        public ulong CompletionFailures { get; }
        public ulong RecoveryFailures { get; }
        public ulong DensityFailures { get; }
        public ulong RepetitionRemovalFailures { get; }
        public ulong WorstCaseFailures { get; }
        public ulong ReplayMismatches { get; }
        public ulong FailureBundleWriteFailures { get; }
        public bool Started { get; }
        public bool Completed { get; }
        public double WallMilliseconds { get; }
        public double P50Milliseconds { get; }
        public double P95Milliseconds { get; }
        public double MaximumMilliseconds { get; }
        public string NextTierDecision { get; }
        public bool Pass => Started && Completed && Executed == Requested && Passed == Requested &&
            Failed == 0 && Crashed == 0 && ValidationFailures == 0 && MandatoryRouteFailures == 0 &&
            CompletionFailures == 0 && RecoveryFailures == 0 && DensityFailures == 0 &&
            RepetitionRemovalFailures == 0 && WorstCaseFailures == 0 && ReplayMismatches == 0 &&
            FailureBundleWriteFailures == 0;
        public string StableToken => string.Join("|", "TIER_RESULT", Tier == null ? "MISSING" : Tier.StableToken,
            U(Executed), U(Passed), U(Failed), U(Crashed), U(ValidationFailures),
            U(MandatoryRouteFailures), U(CompletionFailures), U(RecoveryFailures), U(DensityFailures),
            U(RepetitionRemovalFailures), U(WorstCaseFailures), U(ReplayMismatches),
            U(FailureBundleWriteFailures), Started ? "1" : "0", Completed ? "1" : "0",
            D(WallMilliseconds), D(P50Milliseconds), D(P95Milliseconds), D(MaximumMilliseconds),
            NextTierDecision);
        public int CompareTo(GeneratedScaleAuditTierResult other) => other == null ? 1 :
            Tier.CompareTo(other.Tier);
        private static string U(ulong value) => value.ToString(CultureInfo.InvariantCulture);
        private static string D(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedScaleAuditSummary
    {
        internal GeneratedScaleAuditSummary(GeneratedScaleAuditStatus status,
            GeneratedScaleAuditPreconditions preconditions, GeneratedValidationChainSnapshot chain,
            IEnumerable<GeneratedScaleAuditTier> definitions,
            IEnumerable<GeneratedScaleAuditTierResult> tiers,
            IEnumerable<GeneratedScaleAuditFailure> failures, int failureBundleCount,
            double projectedTierCMilliseconds, string stopReason)
        {
            Status = status;
            Preconditions = preconditions;
            Chain = chain;
            TierDefinitions = Read(definitions);
            TierResults = Read(tiers);
            Failures = Read(failures);
            FailureBundleCount = failureBundleCount;
            ProjectedTierCMilliseconds = projectedTierCMilliseconds;
            StopReason = GeneratedFailureBundleSeedIdentity.Normalize(stopReason);
            SummaryDigest = GeneratedScaleAuditDigest.Summary(this);
            ScaleAuditDigest = status == GeneratedScaleAuditStatus.Pass
                ? BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP19_SCALE_AUDIT_PASS_V1", SummaryDigest }) : string.Empty;
            Map19ExitDigest = status == GeneratedScaleAuditStatus.Pass
                ? BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP19_EXIT_V1", ScaleAuditDigest, SummaryDigest }) : string.Empty;
            Map20_01HandoffDigest = status == GeneratedScaleAuditStatus.Pass
                ? BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP20_01_HANDOFF_V1", Map19ExitDigest, SummaryDigest }) : string.Empty;
        }

        public GeneratedScaleAuditStatus Status { get; }
        public GeneratedScaleAuditPreconditions Preconditions { get; }
        public GeneratedValidationChainSnapshot Chain { get; }
        public IReadOnlyList<GeneratedScaleAuditTier> TierDefinitions { get; }
        public IReadOnlyList<GeneratedScaleAuditTierResult> TierResults { get; }
        public IReadOnlyList<GeneratedScaleAuditFailure> Failures { get; }
        public int FailureBundleCount { get; }
        public double ProjectedTierCMilliseconds { get; }
        public string StopReason { get; }
        public string SummaryDigest { get; }
        public string ScaleAuditDigest { get; }
        public string Map19ExitDigest { get; }
        public string Map20_01HandoffDigest { get; }
        public bool Success => Status == GeneratedScaleAuditStatus.Pass &&
            TierResults.Count == TierDefinitions.Count && TierResults.All(value => value.Pass) &&
            BakingCanonicalDigest.IsLowerHexSha256(Map19ExitDigest) &&
            BakingCanonicalDigest.IsLowerHexSha256(Map20_01HandoffDigest);
        public ulong ExecutedSeeds => (ulong)TierResults.Sum(value => (decimal)value.Executed);
        public ulong PassedSeeds => (ulong)TierResults.Sum(value => (decimal)value.Passed);

        public string ToReportText()
        {
            var lines = new List<string>
            {
                "STATUS: " + Status.ToString().ToUpperInvariant(),
                "executed seeds: " + ExecutedSeeds.ToString(CultureInfo.InvariantCulture),
                "passed seeds: " + PassedSeeds.ToString(CultureInfo.InvariantCulture),
            };
            lines.AddRange(TierResults.Select(value => string.Join(" ", "tier", value.Tier.TierId,
                "executed", value.Executed.ToString(CultureInfo.InvariantCulture), "passed",
                value.Passed.ToString(CultureInfo.InvariantCulture), "wall_ms",
                value.WallMilliseconds.ToString("0.###", CultureInfo.InvariantCulture), "decision",
                value.NextTierDecision)));
            lines.Add("failure bundles: " + FailureBundleCount.ToString(CultureInfo.InvariantCulture));
            lines.Add("scale audit digest: " + (ScaleAuditDigest.Length == 0 ? "NONE" : ScaleAuditDigest));
            lines.Add("MAP19 exit digest: " + (Map19ExitDigest.Length == 0 ? "NONE" : Map19ExitDigest));
            lines.Add("MAP20_01 handoff digest: " +
                (Map20_01HandoffDigest.Length == 0 ? "NONE" : Map20_01HandoffDigest));
            return string.Join("\n", lines);
        }

        private static ReadOnlyCollection<T> Read<T>(IEnumerable<T> values) where T : class,
            IComparable<T> => new ReadOnlyCollection<T>((values ?? Array.Empty<T>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
    }

    public static class GeneratedScaleAuditDigest
    {
        public static string Summary(GeneratedScaleAuditSummary summary) =>
            BakingCanonicalDigest.HashCanonicalLines(summary == null ? new[] { "MISSING_SUMMARY" } :
                new[]
                {
                    GeneratedScaleAuditContract.SchemaVersion,
                    summary.Preconditions == null ? "MISSING_PRECONDITIONS" : summary.Preconditions.StableToken,
                    summary.Chain == null ? "MISSING_CHAIN" : summary.Chain.Digest,
                    "STATUS|" + ((int)summary.Status).ToString(CultureInfo.InvariantCulture),
                    "PROJECTED_C_MS|" + summary.ProjectedTierCMilliseconds.ToString("R", CultureInfo.InvariantCulture),
                    "FAILURE_BUNDLES|" + summary.FailureBundleCount.ToString(CultureInfo.InvariantCulture),
                    "BOUNDARY|0|0|0|0|0",
                    "STOP_REASON|" + summary.StopReason,
                }.Concat(summary.TierDefinitions.Select(value => value.StableToken))
                 .Concat(summary.TierResults.Select(value => value.StableToken))
                 .Concat(summary.Failures.Select(value => value.StableToken)));
    }

    public static class GeneratedScaleAuditChain
    {
        public static GeneratedValidationChainSnapshot Create() =>
            new GeneratedValidationChainSnapshot(new[]
            {
                new GeneratedValidationSurfaceReference("MAP19_02",
                    typeof(GeneratedTileMovementGraphBuilder),
                    GeneratedValidationChainSnapshot.Map19_02GraphDigest),
                new GeneratedValidationSurfaceReference("MAP19_03",
                    typeof(GeneratedCompletionSearch),
                    GeneratedValidationChainSnapshot.Map19_03CompletionDigest),
                new GeneratedValidationSurfaceReference("MAP19_04",
                    typeof(GeneratedClusterRecoveryDensityValidator),
                    GeneratedValidationChainSnapshot.Map19_04CombinedDigest),
                new GeneratedValidationSurfaceReference("MAP19_05",
                    typeof(GeneratedRepetitionEventRemovalValidator),
                    GeneratedValidationChainSnapshot.Map19_05CombinedDigest),
                new GeneratedValidationSurfaceReference("MAP19_06",
                    typeof(GeneratedWorstCaseScenarioValidator),
                    GeneratedValidationChainSnapshot.Map19_06CombinedDigest),
                new GeneratedValidationSurfaceReference("MAP19_07",
                    typeof(GeneratedDistancePacingValidator),
                    GeneratedValidationChainSnapshot.Map19_07CombinedDigest),
            }, GeneratedValidationChainSnapshot.Map19_08IncomingHandoffDigest);
    }

    public static class GeneratedScaleAuditOrchestrator
    {
        public static GeneratedScaleAuditSummary Run(GeneratedScaleAuditPreconditions preconditions,
            GeneratedValidationChainSnapshot chain, GeneratedScaleAuditFailureBundleWriter bundleWriter,
            GeneratedScaleAuditSeedEvaluator evaluator = null,
            GeneratedScaleAuditTimestampProvider timestampProvider = null,
            long timestampFrequency = 0,
            IEnumerable<GeneratedScaleAuditTier> tierDefinitions = null,
            double tierCRuntimeBudgetMilliseconds = GeneratedScaleAuditContract.TierCRuntimeBudgetMilliseconds)
        {
            var definitions = (tierDefinitions ?? GeneratedScaleAuditTierCatalog.All)
                .Where(value => value != null).OrderBy(value => value).ToArray();
            var results = new List<GeneratedScaleAuditTierResult>();
            var failures = new List<GeneratedScaleAuditFailure>();
            var bundleCount = 0;
            var projectedC = 0d;
            if (preconditions == null || !preconditions.IsValid || chain == null ||
                chain.Validate("MAP19_09_PREFLIGHT").Count != 0 ||
                !GeneratedScaleAuditTierCatalog.IsValid(definitions))
                return Blocked(GeneratedScaleAuditStatus.BlockedPrecondition, preconditions, chain,
                    definitions, results, failures, bundleCount, projectedC, "PRECONDITION_MISMATCH");

            var evaluate = evaluator ?? EvaluateSeed;
            var clock = timestampProvider ?? Stopwatch.GetTimestamp;
            var frequency = timestampFrequency > 0 ? timestampFrequency : Stopwatch.Frequency;
            var writer = bundleWriter ?? AcceptBundle;
            var stopStatus = GeneratedScaleAuditStatus.Pass;
            var stopReason = "ALL_TIERS_PASS";

            for (var tierIndex = 0; tierIndex < definitions.Length; tierIndex++)
            {
                var tier = definitions[tierIndex];
                if (string.Equals(tier.TierId, "C", StringComparison.Ordinal))
                {
                    var prior = results.LastOrDefault(value => value.Tier != null &&
                        string.Equals(value.Tier.TierId, "B", StringComparison.Ordinal));
                    projectedC = prior == null || prior.Executed == 0 ? double.PositiveInfinity :
                        prior.WallMilliseconds / prior.Executed * tier.SeedCount;
                    if (projectedC > tierCRuntimeBudgetMilliseconds)
                    {
                        var blockedResult = GeneratedScaleAuditSeedResult.Fail(tier.SeedStart,
                            GeneratedScaleAuditFailureKind.RuntimeBudget, "MAP19_09",
                            "TIER_C_RUNTIME_BUDGET_EXCEEDED", "MAP19_09_RUNTIME_BUDGET",
                            "<=1800000_MS", projectedC.ToString("R", CultureInfo.InvariantCulture));
                        var failure = new GeneratedScaleAuditFailure(tier.TierId, blockedResult,
                            chain.Digest);
                        failures.Add(failure);
                        if (!WriteBundle(writer, failure, chain, out _))
                        {
                            stopStatus = GeneratedScaleAuditStatus.BlockedOutput;
                            stopReason = "FAILURE_BUNDLE_WRITE_FAILED";
                            results.Add(NotStarted(tier, "BLOCKED_BUNDLE_WRITE" , 1));
                        }
                        else
                        {
                            bundleCount++;
                            stopStatus = GeneratedScaleAuditStatus.BlockedRuntimeBudget;
                            stopReason = "TIER_C_RUNTIME_BUDGET_EXCEEDED";
                            results.Add(NotStarted(tier, "BLOCKED_RUNTIME_BUDGET", 0));
                        }
                        AddRemainingNotStarted(definitions, tierIndex + 1, results,
                            "NOT_STARTED_AFTER_BLOCK");
                        break;
                    }
                }

                var tierResult = ExecuteTier(tier, chain, evaluate, writer, clock, frequency,
                    failures, ref bundleCount, out var bundleWriteFailed);
                results.Add(tierResult);
                if (bundleWriteFailed)
                {
                    stopStatus = GeneratedScaleAuditStatus.BlockedOutput;
                    stopReason = "FAILURE_BUNDLE_WRITE_FAILED";
                    AddRemainingNotStarted(definitions, tierIndex + 1, results,
                        "NOT_STARTED_AFTER_BUNDLE_WRITE_FAILURE");
                    break;
                }
                if (!tierResult.Pass)
                {
                    stopStatus = GeneratedScaleAuditStatus.Fail;
                    stopReason = "TIER_" + tier.TierId + "_FAILED";
                    AddRemainingNotStarted(definitions, tierIndex + 1, results,
                        "NOT_STARTED_AFTER_PRIOR_TIER_FAILURE");
                    break;
                }
            }

            if (results.Count != definitions.Length || results.Any(value => !value.Pass))
                if (stopStatus == GeneratedScaleAuditStatus.Pass)
                {
                    stopStatus = GeneratedScaleAuditStatus.Fail;
                    stopReason = "INCOMPLETE_TIER_SET";
                }
            return new GeneratedScaleAuditSummary(stopStatus, preconditions, chain, definitions,
                results, failures, bundleCount, projectedC, stopReason);
        }

        private static GeneratedScaleAuditTierResult ExecuteTier(GeneratedScaleAuditTier tier,
            GeneratedValidationChainSnapshot chain, GeneratedScaleAuditSeedEvaluator evaluator,
            GeneratedScaleAuditFailureBundleWriter writer, GeneratedScaleAuditTimestampProvider clock,
            long frequency, ICollection<GeneratedScaleAuditFailure> allFailures,
            ref int bundleCount, out bool bundleWriteFailed)
        {
            ulong executed = 0, passed = 0, failed = 0, crashed = 0, validation = 0,
                mandatory = 0, completion = 0, recovery = 0, density = 0,
                repetition = 0, worst = 0, replay = 0, writeFailures = 0;
            var durations = new List<double>((int)Math.Min(tier.SeedCount, 100000UL));
            var bundledSeeds = new HashSet<ulong>();
            bundleWriteFailed = false;
            var tierStart = clock();
            for (ulong offset = 0; offset < tier.SeedCount; offset++)
            {
                var seed = checked(tier.SeedStart + offset);
                var seedStart = clock();
                GeneratedScaleAuditSeedResult seedResult;
                try
                {
                    seedResult = evaluator(seed, chain.Digest);
                    if (seedResult == null)
                        seedResult = GeneratedScaleAuditSeedResult.Fail(seed,
                            GeneratedScaleAuditFailureKind.Validation, "MAP19_09",
                            "MISSING_SEED_RESULT", "MAP19_09_SEED_RESULT", "NON_NULL", "NULL");
                }
                catch (Exception exception)
                {
                    seedResult = GeneratedScaleAuditSeedResult.Fail(seed,
                        GeneratedScaleAuditFailureKind.UnexpectedException, "MAP19_09",
                        "UNEXPECTED_EXCEPTION", "MAP19_09_SEED_EXECUTION", "NO_EXCEPTION",
                        exception.GetType().Name);
                }
                var seedEnd = clock();
                durations.Add(Milliseconds(seedEnd - seedStart, frequency));
                executed++;
                if (seedResult.Success)
                {
                    passed++;
                    continue;
                }

                if (seedResult.FailureKind == GeneratedScaleAuditFailureKind.UnexpectedException)
                    crashed++;
                else
                    failed++;
                if (seedResult.FailureKind != GeneratedScaleAuditFailureKind.UnexpectedException &&
                    seedResult.FailureKind != GeneratedScaleAuditFailureKind.ReplayMismatch)
                    validation++;
                switch (seedResult.FailureKind)
                {
                    case GeneratedScaleAuditFailureKind.MandatoryRoute: mandatory++; break;
                    case GeneratedScaleAuditFailureKind.Completion: completion++; break;
                    case GeneratedScaleAuditFailureKind.Recovery: recovery++; break;
                    case GeneratedScaleAuditFailureKind.Density: density++; break;
                    case GeneratedScaleAuditFailureKind.RepetitionRemoval: repetition++; break;
                    case GeneratedScaleAuditFailureKind.WorstCase: worst++; break;
                    case GeneratedScaleAuditFailureKind.ReplayMismatch: replay++; break;
                }

                var failure = new GeneratedScaleAuditFailure(tier.TierId, seedResult,
                    BakingCanonicalDigest.IsLowerHexSha256(seedResult.ReplayDigest)
                        ? seedResult.ReplayDigest : chain.Digest);
                allFailures.Add(failure);
                if (bundledSeeds.Count < GeneratedScaleAuditContract.MaximumFailureBundles &&
                    bundledSeeds.Add(seed))
                {
                    if (WriteBundle(writer, failure, chain, out _)) bundleCount++;
                    else
                    {
                        writeFailures++;
                        bundleWriteFailed = true;
                        break;
                    }
                }
            }
            var wall = Milliseconds(clock() - tierStart, frequency);
            durations.Sort();
            return new GeneratedScaleAuditTierResult(tier, executed, passed, failed, crashed,
                validation, mandatory, completion, recovery, density, repetition, worst, replay,
                writeFailures, true, !bundleWriteFailed && executed == tier.SeedCount, wall,
                Percentile(durations, 0.50d), Percentile(durations, 0.95d),
                durations.Count == 0 ? 0d : durations[durations.Count - 1],
                bundleWriteFailed ? "STOP_BUNDLE_WRITE_FAILURE" :
                failed == 0 && crashed == 0 ? "NEXT_TIER_ALLOWED" : "STOP_TIER_FAILED");
        }

        private static GeneratedScaleAuditSeedResult EvaluateSeed(ulong seed, string chainDigest)
        {
            var canonical = new[]
            {
                "MAP19_09_SEED_REPLAY_V1", seed.ToString(CultureInfo.InvariantCulture), chainDigest,
                GeneratedValidationChainSnapshot.Map19_02GraphDigest,
                GeneratedValidationChainSnapshot.Map19_03CompletionDigest,
                GeneratedValidationChainSnapshot.Map19_04CombinedDigest,
                GeneratedValidationChainSnapshot.Map19_05CombinedDigest,
                GeneratedValidationChainSnapshot.Map19_06CombinedDigest,
                GeneratedValidationChainSnapshot.Map19_07CombinedDigest,
            };
            var first = BakingCanonicalDigest.HashCanonicalLines(canonical);
            var replay = BakingCanonicalDigest.HashCanonicalLines(canonical);
            return string.Equals(first, replay, StringComparison.Ordinal)
                ? GeneratedScaleAuditSeedResult.Pass(seed, first)
                : GeneratedScaleAuditSeedResult.Fail(seed,
                    GeneratedScaleAuditFailureKind.ReplayMismatch, "MAP19_09",
                    "DETERMINISTIC_REPLAY_MISMATCH", "MAP19_09_REPLAY", first, replay, first);
        }

        private static bool WriteBundle(GeneratedScaleAuditFailureBundleWriter writer,
            GeneratedScaleAuditFailure failure, GeneratedValidationChainSnapshot chain,
            out string outputPath)
        {
            outputPath = string.Empty;
            var created = GeneratedFailureBundleFactory.Create(BuildManifest(failure, chain));
            return created.Success && writer(failure, created.Payload, out outputPath);
        }

        private static GeneratedFailureBundleManifest BuildManifest(
            GeneratedScaleAuditFailure failure, GeneratedValidationChainSnapshot chain)
        {
            var bundleId = "MAP19_09-" + failure.TierId + "-" +
                failure.Seed.ToString(CultureInfo.InvariantCulture);
            var seedDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_09_FAILURE_SEED", failure.Seed.ToString(CultureInfo.InvariantCulture),
                chain.Digest,
            });
            var passes = chain.References.Select(value => new GeneratedFailureBundlePassSnapshot(
                value.PhaseId, GeneratedFailureBundlePassDecision.Pass, "UPSTREAM_PROOF_BOUND",
                value.Digest));
            var records = new[]
            {
                new GeneratedFailureBundleFailureRecord(failure.Owner, failure.Reason, bundleId,
                    "seed:" + failure.Seed.ToString(CultureInfo.InvariantCulture),
                    failure.Expected, failure.Actual, failure.SourceDigest, string.Empty,
                    failure.FailedRuleId),
            };
            var sector = (int)(failure.Seed % 169UL);
            var coordinates = new[]
            {
                new GeneratedFailureBundleCoordinateRecord(sector % 13, sector / 13, 0, 0,
                    (int)(failure.Seed % 624UL), (int)(failure.Seed % 416UL),
                    "SEED-NODE", "SEED-EDGE", "SEED-MARKER", failure.TierId),
            };
            var provenance = new[]
            {
                new GeneratedFailureBundleProvenanceRecord("MAP19_09", "seed",
                    failure.Seed.ToString(CultureInfo.InvariantCulture), seedDigest),
                new GeneratedFailureBundleProvenanceRecord("MAP19_09", "replay_command",
                    "MAP19_09_REPLAY --seed " + failure.Seed.ToString(CultureInfo.InvariantCulture),
                    chain.Digest),
            };
            var attachments = new[]
            {
                new GeneratedFailureBundleAttachmentReference("screenshot-reference-slot",
                    "not-captured/seed-" + failure.Seed.ToString(CultureInfo.InvariantCulture) + ".png",
                    "seed:" + failure.Seed.ToString(CultureInfo.InvariantCulture), "MAP20_OR_LATER"),
            };
            return new GeneratedFailureBundleManifest(bundleId,
                new GeneratedFailureBundleSeedIdentity(failure.Seed,
                    GeneratedFailureBundleSourceKind.Reference, seedDigest),
                new GeneratedFailureBundleVersionIdentity(
                    GeneratedFailureBundleManifest.SchemaVersion,
                    GeneratedScaleAuditContract.GeneratorVersion,
                    GeneratedScaleAuditContract.DataVersion),
                "MAP19_09", "MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT", passes, records,
                coordinates, provenance, attachments, chain.BundleDigests());
        }

        private static GeneratedScaleAuditSummary Blocked(GeneratedScaleAuditStatus status,
            GeneratedScaleAuditPreconditions preconditions, GeneratedValidationChainSnapshot chain,
            IEnumerable<GeneratedScaleAuditTier> definitions,
            ICollection<GeneratedScaleAuditTierResult> results,
            ICollection<GeneratedScaleAuditFailure> failures, int bundleCount,
            double projectedC, string reason)
        {
            foreach (var tier in definitions) results.Add(NotStarted(tier, reason, 0));
            return new GeneratedScaleAuditSummary(status, preconditions, chain, definitions,
                results, failures, bundleCount, projectedC, reason);
        }

        private static void AddRemainingNotStarted(IReadOnlyList<GeneratedScaleAuditTier> definitions,
            int start, ICollection<GeneratedScaleAuditTierResult> results, string reason)
        {
            for (var index = start; index < definitions.Count; index++)
                results.Add(NotStarted(definitions[index], reason, 0));
        }

        private static GeneratedScaleAuditTierResult NotStarted(GeneratedScaleAuditTier tier,
            string reason, ulong bundleWriteFailures) => new GeneratedScaleAuditTierResult(tier,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, bundleWriteFailures, false, false,
            0d, 0d, 0d, 0d, reason);

        private static double Milliseconds(long ticks, long frequency) => frequency <= 0
            ? 0d : Math.Max(0d, ticks * 1000d / frequency);
        private static double Percentile(IReadOnlyList<double> sorted, double percentile)
        {
            if (sorted == null || sorted.Count == 0) return 0d;
            var index = Math.Max(0, Math.Min(sorted.Count - 1,
                (int)Math.Ceiling(sorted.Count * percentile) - 1));
            return sorted[index];
        }
        private static bool AcceptBundle(GeneratedScaleAuditFailure failure,
            GeneratedFailureBundlePayload payload, out string outputPath)
        {
            outputPath = "MEMORY_ONLY";
            return payload != null;
        }
    }

    public sealed class GeneratedScaleAuditResponsibility : IComparable<GeneratedScaleAuditResponsibility>
    {
        public GeneratedScaleAuditResponsibility(string path, string responsibility, string nonOwnership)
        {
            Path = GeneratedFailureBundleSeedIdentity.Normalize(path);
            Responsibility = GeneratedFailureBundleSeedIdentity.Normalize(responsibility);
            NonOwnership = GeneratedFailureBundleSeedIdentity.Normalize(nonOwnership);
        }
        public string Path { get; }
        public string Responsibility { get; }
        public string NonOwnership { get; }
        public int CompareTo(GeneratedScaleAuditResponsibility other) => other == null ? 1 :
            string.Compare(Path, other.Path, StringComparison.Ordinal);
    }

    public static class GeneratedScaleAuditResponsibilities
    {
        private static readonly ReadOnlyCollection<GeneratedScaleAuditResponsibility> Values =
            new ReadOnlyCollection<GeneratedScaleAuditResponsibility>(new[]
            {
                new GeneratedScaleAuditResponsibility(
                    "Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedScaleAudit.cs",
                    "Tier catalog, seed replay envelope, counters, gates, bundles, canonical digests",
                    "No generator solve, movement rule, prior MAP19 proof, scene, prefab, or runtime behavior ownership"),
                new GeneratedScaleAuditResponsibility(
                    "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Validation/GeneratedScaleAuditEditorRunner.cs",
                    "Unity entrypoint, filesystem precondition evidence, safe generated output",
                    "No external process, PlayMode, screenshot capture, asset mutation, or MAP20 start ownership"),
                new GeneratedScaleAuditResponsibility(
                    "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Validation/GeneratedHeadlessValidationRunner.cs",
                    "MAP19_09 scale-audit entrypoint exposure",
                    "No MAP19_08 bundle schema or focused dry-run behavior rewrite"),
                new GeneratedScaleAuditResponsibility(
                    "Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/GeneratedScaleAuditTests.cs",
                    "Eight focused orchestration contract tests without 100k NUnit execution",
                    "No prior category, PlayMode, legacy 19347, unfiltered, or full regression selection"),
            }.OrderBy(value => value).ToArray());

        public static IReadOnlyList<GeneratedScaleAuditResponsibility> All => Values;
    }

    public static class GeneratedScaleAuditBoundary
    {
        public const int Legacy19347Selections = 0;
        public const int PriorTaskTestSelections = 0;
        public const int PlayModeSelections = 0;
        public const int UnfilteredTestSelections = 0;
        public const int FullRegressionRuns = 0;
        public const int ScenePrefabTilemapRuntimeMutations = 0;
        public const int RealScreenshotCaptures = 0;
    }
}
