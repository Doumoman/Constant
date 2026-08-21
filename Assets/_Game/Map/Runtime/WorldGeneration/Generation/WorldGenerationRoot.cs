using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class WorldGenerationRoot
    {
        private readonly StaticDataRegistry staticData;
        private readonly WorldGenerationPassRegistry passRegistry;
        private readonly IWorldGenerationClock clock;

        public WorldGenerationRoot(
            StaticDataRegistry staticData,
            WorldGenerationPassRegistry passRegistry)
            : this(staticData, passRegistry, SystemWorldGenerationClock.Instance)
        {
        }

        public WorldGenerationRoot(
            StaticDataRegistry staticData,
            WorldGenerationPassRegistry passRegistry,
            IWorldGenerationClock clock)
        {
            this.staticData = staticData ?? throw new ArgumentNullException(nameof(staticData));
            this.passRegistry = passRegistry ?? throw new ArgumentNullException(nameof(passRegistry));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public WorldGenerationRootResult Execute(string generationProfileId, ulong worldSeed)
        {
            return ExecuteRecorded(generationProfileId, worldSeed).Result;
        }

        public WorldGenerationExecutionResult ExecuteRecorded(string generationProfileId, ulong worldSeed)
        {
            return ExecuteInternal(generationProfileId, worldSeed, null);
        }

        public WorldGenerationRootResult ExecuteThrough(
            string generationProfileId,
            ulong worldSeed,
            string inclusivePassId)
        {
            if (inclusivePassId == null) throw new ArgumentNullException(nameof(inclusivePassId));
            return ExecuteThroughRecorded(generationProfileId, worldSeed, inclusivePassId).Result;
        }

        public WorldGenerationExecutionResult ExecuteThroughRecorded(
            string generationProfileId,
            ulong worldSeed,
            string inclusivePassId)
        {
            if (inclusivePassId == null) throw new ArgumentNullException(nameof(inclusivePassId));
            return ExecuteInternal(generationProfileId, worldSeed, inclusivePassId);
        }

        private WorldGenerationExecutionResult ExecuteInternal(
            string generationProfileId,
            ulong worldSeed,
            string inclusivePassId)
        {
            var rootStartedUtc = GetUtcNow();
            var rootStartTimestamp = clock.GetTimestamp();
            var passRecords = new List<WorldGenerationPassExecutionRecord>();
            var plan = BuildPlan(generationProfileId, inclusivePassId);
            if (plan.Issues.Count > 0)
                return CompleteExecution(
                    generationProfileId,
                    plan.GenerationProfile,
                    worldSeed,
                    inclusivePassId,
                    rootStartedUtc,
                    rootStartTimestamp,
                    passRecords,
                    PlanFailure(plan.Issues));

            var artifacts = new WorldGenerationArtifactStore();
            var issues = new List<WorldGenerationRootIssue>();
            var lastCompletedPassId = string.Empty;

            foreach (var entry in plan.Entries)
            {
                foreach (var inputId in entry.InputIds)
                {
                    if (!artifacts.Contains(inputId))
                    {
                        issues.Add(Issue(
                            entry.Definition.PassId,
                            "MISSING_INPUT_ARTIFACT",
                            "Required input artifact is unavailable: " + inputId,
                            0,
                            string.Empty,
                            true));
                        return CompleteExecution(
                            generationProfileId,
                            plan.GenerationProfile,
                            worldSeed,
                            inclusivePassId,
                            rootStartedUtc,
                            rootStartTimestamp,
                            passRecords,
                            Failure(artifacts, issues, lastCompletedPassId));
                    }
                }

                var passStartedUtc = GetUtcNow();
                var passStartTimestamp = clock.GetTimestamp();
                var attemptRecords = new List<WorldGenerationAttemptRecord>();
                var inputs = artifacts.Select(entry.InputIds);
                var attemptOrdinal = 0;
                var retryScopeId = string.Empty;
                var passCompleted = false;
                while (!passCompleted)
                {
                    var attemptStartedUtc = GetUtcNow();
                    var attemptStartTimestamp = clock.GetTimestamp();
                    WorldGenerationPassResult passResult = null;
                    Exception passException = null;
                    try
                    {
                        var context = new WorldGenerationPassContext(
                            worldSeed,
                            staticData,
                            plan.GenerationProfile,
                            entry.Definition,
                            inputs,
                            new WorldGenerationRngStreams(staticData),
                            attemptOrdinal,
                            retryScopeId);
                        passResult = entry.Implementation.Execute(context);
                    }
                    catch (Exception exception)
                    {
                        passException = exception;
                    }

                    var attemptDurationMilliseconds = GetDurationMilliseconds(
                        attemptStartTimestamp,
                        clock.GetTimestamp());

                    if (passException != null)
                    {
                        var issue = Issue(
                            entry.Definition.PassId,
                            "UNHANDLED_PASS_EXCEPTION",
                            "Pass threw " + passException.GetType().FullName + ".",
                            attemptOrdinal,
                            retryScopeId,
                            true);
                        issues.Add(issue);
                        attemptRecords.Add(AttemptRecord(
                            entry,
                            worldSeed,
                            attemptOrdinal,
                            retryScopeId,
                            attemptStartedUtc,
                            attemptDurationMilliseconds,
                            false,
                            issue.Code,
                            issue.Message,
                            string.Empty));
                        passRecords.Add(CompletePass(
                            entry,
                            worldSeed,
                            passStartedUtc,
                            passStartTimestamp,
                            attemptRecords,
                            false,
                            true,
                            issue.Code,
                            issue.Message,
                            issue.RetryScopeId));
                        return CompleteExecution(
                            generationProfileId,
                            plan.GenerationProfile,
                            worldSeed,
                            inclusivePassId,
                            rootStartedUtc,
                            rootStartTimestamp,
                            passRecords,
                            Failure(artifacts, issues, lastCompletedPassId));
                    }

                    if (passResult == null)
                    {
                        var issue = Issue(
                            entry.Definition.PassId,
                            "NULL_PASS_RESULT",
                            "Pass returned a null result.",
                            attemptOrdinal,
                            retryScopeId,
                            true);
                        issues.Add(issue);
                        attemptRecords.Add(AttemptRecord(
                            entry,
                            worldSeed,
                            attemptOrdinal,
                            retryScopeId,
                            attemptStartedUtc,
                            attemptDurationMilliseconds,
                            false,
                            issue.Code,
                            issue.Message,
                            string.Empty));
                        passRecords.Add(CompletePass(
                            entry,
                            worldSeed,
                            passStartedUtc,
                            passStartTimestamp,
                            attemptRecords,
                            false,
                            true,
                            issue.Code,
                            issue.Message,
                            issue.RetryScopeId));
                        return CompleteExecution(
                            generationProfileId,
                            plan.GenerationProfile,
                            worldSeed,
                            inclusivePassId,
                            rootStartedUtc,
                            rootStartTimestamp,
                            passRecords,
                            Failure(artifacts, issues, lastCompletedPassId));
                    }

                    if (passResult.Succeeded)
                    {
                        if (!OutputSetMatches(entry.OutputIds, passResult.Outputs))
                        {
                            var issue = Issue(
                                entry.Definition.PassId,
                                "OUTPUT_SET_MISMATCH",
                                "Pass output identifiers do not match the declared output set.",
                                attemptOrdinal,
                                retryScopeId,
                                true);
                            issues.Add(issue);
                            attemptRecords.Add(AttemptRecord(
                                entry,
                                worldSeed,
                                attemptOrdinal,
                                retryScopeId,
                                attemptStartedUtc,
                                attemptDurationMilliseconds,
                                false,
                                issue.Code,
                                issue.Message,
                                string.Empty));
                            passRecords.Add(CompletePass(
                                entry,
                                worldSeed,
                                passStartedUtc,
                                passStartTimestamp,
                                attemptRecords,
                                false,
                                true,
                                issue.Code,
                                issue.Message,
                                issue.RetryScopeId));
                            return CompleteExecution(
                                generationProfileId,
                                plan.GenerationProfile,
                                worldSeed,
                                inclusivePassId,
                                rootStartedUtc,
                                rootStartTimestamp,
                                passRecords,
                                Failure(artifacts, issues, lastCompletedPassId));
                        }

                        try
                        {
                            artifacts = artifacts.Commit(passResult.Outputs);
                        }
                        catch (InvalidOperationException)
                        {
                            var issue = Issue(
                                entry.Definition.PassId,
                                "ARTIFACT_OWNERSHIP_CONFLICT",
                                "Pass attempted to replace an artifact owned by an earlier pass.",
                                attemptOrdinal,
                                retryScopeId,
                                true);
                            issues.Add(issue);
                            attemptRecords.Add(AttemptRecord(
                                entry,
                                worldSeed,
                                attemptOrdinal,
                                retryScopeId,
                                attemptStartedUtc,
                                attemptDurationMilliseconds,
                                false,
                                issue.Code,
                                issue.Message,
                                string.Empty));
                            passRecords.Add(CompletePass(
                                entry,
                                worldSeed,
                                passStartedUtc,
                                passStartTimestamp,
                                attemptRecords,
                                false,
                                true,
                                issue.Code,
                                issue.Message,
                                issue.RetryScopeId));
                            return CompleteExecution(
                                generationProfileId,
                                plan.GenerationProfile,
                                worldSeed,
                                inclusivePassId,
                                rootStartedUtc,
                                rootStartTimestamp,
                                passRecords,
                                Failure(artifacts, issues, lastCompletedPassId));
                        }

                        attemptRecords.Add(AttemptRecord(
                            entry,
                            worldSeed,
                            attemptOrdinal,
                            retryScopeId,
                            attemptStartedUtc,
                            attemptDurationMilliseconds,
                            true,
                            string.Empty,
                            string.Empty,
                            string.Empty));
                        passRecords.Add(CompletePass(
                            entry,
                            worldSeed,
                            passStartedUtc,
                            passStartTimestamp,
                            attemptRecords,
                            true,
                            false,
                            string.Empty,
                            string.Empty,
                            string.Empty));
                        lastCompletedPassId = entry.Definition.PassId;
                        passCompleted = true;
                        continue;
                    }

                    attemptRecords.Add(AttemptRecord(
                        entry,
                        worldSeed,
                        attemptOrdinal,
                        retryScopeId,
                        attemptStartedUtc,
                        attemptDurationMilliseconds,
                        false,
                        passResult.FailureCode,
                        passResult.FailureMessage,
                        passResult.RetryScopeId));

                    switch (entry.Policy)
                    {
                        case WorldGenerationFailurePolicy.FailWorld:
                        {
                            var issue = Issue(
                                entry.Definition.PassId,
                                "PASS_FAILED",
                                passResult.FailureMessage,
                                attemptOrdinal,
                                passResult.RetryScopeId,
                                true);
                            issues.Add(issue);
                            passRecords.Add(CompletePass(
                                entry,
                                worldSeed,
                                passStartedUtc,
                                passStartTimestamp,
                                attemptRecords,
                                false,
                                true,
                                issue.Code,
                                issue.Message,
                                issue.RetryScopeId));
                            return CompleteExecution(
                                generationProfileId,
                                plan.GenerationProfile,
                                worldSeed,
                                inclusivePassId,
                                rootStartedUtc,
                                rootStartTimestamp,
                                passRecords,
                                Failure(artifacts, issues, lastCompletedPassId));
                        }

                        case WorldGenerationFailurePolicy.ReportOnly:
                        {
                            var issue = Issue(
                                entry.Definition.PassId,
                                "PASS_FAILED",
                                passResult.FailureMessage,
                                attemptOrdinal,
                                passResult.RetryScopeId,
                                false);
                            issues.Add(issue);
                            passRecords.Add(CompletePass(
                                entry,
                                worldSeed,
                                passStartedUtc,
                                passStartTimestamp,
                                attemptRecords,
                                false,
                                false,
                                issue.Code,
                                issue.Message,
                                issue.RetryScopeId));
                            passCompleted = true;
                            continue;
                        }

                        case WorldGenerationFailurePolicy.RetryPass:
                            if (attemptOrdinal >= entry.Definition.MaxRetryCount)
                            {
                                var issue = Issue(
                                    entry.Definition.PassId,
                                    "RETRY_EXHAUSTED",
                                    passResult.FailureMessage,
                                    attemptOrdinal,
                                    string.Empty,
                                    true);
                                issues.Add(issue);
                                passRecords.Add(CompletePass(
                                    entry,
                                    worldSeed,
                                    passStartedUtc,
                                    passStartTimestamp,
                                    attemptRecords,
                                    false,
                                    true,
                                    issue.Code,
                                    issue.Message,
                                    issue.RetryScopeId));
                                return CompleteExecution(
                                    generationProfileId,
                                    plan.GenerationProfile,
                                    worldSeed,
                                    inclusivePassId,
                                    rootStartedUtc,
                                    rootStartTimestamp,
                                    passRecords,
                                    Failure(artifacts, issues, lastCompletedPassId));
                            }
                            attemptOrdinal++;
                            retryScopeId = string.Empty;
                            continue;

                        case WorldGenerationFailurePolicy.RetryScope:
                            if (attemptOrdinal >= entry.Definition.MaxRetryCount)
                            {
                                var issue = Issue(
                                    entry.Definition.PassId,
                                    "RETRY_EXHAUSTED",
                                    passResult.FailureMessage,
                                    attemptOrdinal,
                                    passResult.RetryScopeId,
                                    true);
                                issues.Add(issue);
                                passRecords.Add(CompletePass(
                                    entry,
                                    worldSeed,
                                    passStartedUtc,
                                    passStartTimestamp,
                                    attemptRecords,
                                    false,
                                    true,
                                    issue.Code,
                                    issue.Message,
                                    issue.RetryScopeId));
                                return CompleteExecution(
                                    generationProfileId,
                                    plan.GenerationProfile,
                                    worldSeed,
                                    inclusivePassId,
                                    rootStartedUtc,
                                    rootStartTimestamp,
                                    passRecords,
                                    Failure(artifacts, issues, lastCompletedPassId));
                            }
                            if (string.IsNullOrEmpty(passResult.RetryScopeId))
                            {
                                var issue = Issue(
                                    entry.Definition.PassId,
                                    "MISSING_RETRY_SCOPE",
                                    "Retry-scope failure did not identify a retry scope.",
                                    attemptOrdinal,
                                    string.Empty,
                                    true);
                                issues.Add(issue);
                                passRecords.Add(CompletePass(
                                    entry,
                                    worldSeed,
                                    passStartedUtc,
                                    passStartTimestamp,
                                    attemptRecords,
                                    false,
                                    true,
                                    issue.Code,
                                    issue.Message,
                                    issue.RetryScopeId));
                                return CompleteExecution(
                                    generationProfileId,
                                    plan.GenerationProfile,
                                    worldSeed,
                                    inclusivePassId,
                                    rootStartedUtc,
                                    rootStartTimestamp,
                                    passRecords,
                                    Failure(artifacts, issues, lastCompletedPassId));
                            }
                            attemptOrdinal++;
                            retryScopeId = passResult.RetryScopeId;
                            continue;

                        default:
                            throw new InvalidOperationException("Validated failure policy is undefined.");
                    }
                }
            }

            return CompleteExecution(
                generationProfileId,
                plan.GenerationProfile,
                worldSeed,
                inclusivePassId,
                rootStartedUtc,
                rootStartTimestamp,
                passRecords,
                new WorldGenerationRootResult(true, artifacts, issues, lastCompletedPassId));
        }

        private DateTimeOffset GetUtcNow()
        {
            var value = clock.GetUtcNow();
            if (value.Offset != TimeSpan.Zero)
                throw new InvalidOperationException("The injected world-generation clock returned a non-UTC timestamp.");
            return value;
        }

        private long GetDurationMilliseconds(long startTimestamp, long endTimestamp)
        {
            return WorldGenerationExecutionRecordValidation.ToDurationMilliseconds(
                clock.GetElapsedTime(startTimestamp, endTimestamp));
        }

        private static WorldGenerationAttemptRecord AttemptRecord(
            PlanEntry entry,
            ulong worldSeed,
            int attemptOrdinal,
            string retryScopeId,
            DateTimeOffset startedUtc,
            long durationMilliseconds,
            bool succeeded,
            string failureCode,
            string failureMessage,
            string returnedRetryScopeId)
        {
            return new WorldGenerationAttemptRecord(
                entry.Definition.PassId,
                entry.Definition.PassOrder,
                attemptOrdinal,
                retryScopeId,
                worldSeed,
                startedUtc,
                durationMilliseconds,
                succeeded,
                failureCode,
                failureMessage,
                returnedRetryScopeId);
        }

        private WorldGenerationPassExecutionRecord CompletePass(
            PlanEntry entry,
            ulong worldSeed,
            DateTimeOffset startedUtc,
            long startTimestamp,
            IReadOnlyList<WorldGenerationAttemptRecord> attempts,
            bool succeeded,
            bool terminal,
            string failureCode,
            string failureMessage,
            string finalRetryScopeId)
        {
            var durationMilliseconds = GetDurationMilliseconds(startTimestamp, clock.GetTimestamp());
            return new WorldGenerationPassExecutionRecord(
                entry.Definition.PassId,
                entry.Definition.ClassName,
                entry.Definition.PassOrder,
                entry.Definition.FailurePolicy,
                worldSeed,
                startedUtc,
                durationMilliseconds,
                attempts,
                attempts.Count,
                attempts.Count - 1,
                succeeded,
                terminal,
                failureCode,
                failureMessage,
                finalRetryScopeId);
        }

        private WorldGenerationExecutionResult CompleteExecution(
            string generationProfileId,
            GenerationProfileDefinition generationProfile,
            ulong worldSeed,
            string inclusivePassId,
            DateTimeOffset startedUtc,
            long startTimestamp,
            IReadOnlyList<WorldGenerationPassExecutionRecord> passes,
            WorldGenerationRootResult result)
        {
            var durationMilliseconds = GetDurationMilliseconds(startTimestamp, clock.GetTimestamp());
            var terminalIssue = result.Issues.SingleOrDefault(item => item.Terminal);
            var record = new WorldGenerationExecutionRecord(
                generationProfileId ?? string.Empty,
                generationProfile?.WorldProfileId ?? string.Empty,
                worldSeed,
                inclusivePassId ?? string.Empty,
                startedUtc,
                durationMilliseconds,
                passes,
                passes.Count,
                passes.Sum(item => item.AttemptCount),
                passes.Sum(item => item.RetryCount),
                result.Succeeded,
                result.LastCompletedPassId,
                terminalIssue?.PassId ?? string.Empty,
                terminalIssue?.Code ?? string.Empty,
                terminalIssue?.Message ?? string.Empty);
            return new WorldGenerationExecutionResult(result, record);
        }

        private Plan BuildPlan(string generationProfileId, string inclusivePassId)
        {
            var issues = new List<WorldGenerationRootIssue>();
            var definitions = staticData.WorldRouteDefinitions;
            if (definitions == null ||
                generationProfileId == null ||
                !definitions.GenerationProfiles.TryGetValue(generationProfileId, out var profile))
            {
                issues.Add(PlanIssue("", "MISSING_PROFILE", "Generation profile was not found."));
                return new Plan(null, Array.Empty<PlanEntry>(), issues);
            }
            if (!profile.Active)
                issues.Add(PlanIssue("", "INACTIVE_PROFILE", "Generation profile is inactive."));

            if (string.IsNullOrEmpty(profile.WorldProfileId) ||
                !definitions.WorldProfiles.TryGetValue(profile.WorldProfileId, out var worldProfile))
                issues.Add(PlanIssue("", "MISSING_WORLD_PROFILE", "World profile was not found."));
            else if (!worldProfile.Active)
                issues.Add(PlanIssue("", "INACTIVE_WORLD_PROFILE", "World profile is inactive."));

            var enabled = definitions.GetGenerationPasses(generationProfileId)
                .Where(item => item != null && item.Enabled)
                .OrderBy(item => item.PassOrder)
                .ThenBy(item => item.PassId, StringComparer.Ordinal)
                .ToList();

            if (inclusivePassId != null)
            {
                var throughIndex = enabled.FindIndex(item =>
                    string.Equals(item.PassId, inclusivePassId, StringComparison.Ordinal));
                if (throughIndex < 0)
                {
                    issues.Add(PlanIssue("", "UNKNOWN_THROUGH_PASS", "Through-pass target is not an enabled profile pass."));
                    return new Plan(profile, Array.Empty<PlanEntry>(), issues);
                }
                enabled = enabled.Take(throughIndex + 1).ToList();
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var orders = new HashSet<int>();
            var owners = new Dictionary<string, string>(StringComparer.Ordinal);
            var entries = new List<PlanEntry>();
            foreach (var definition in enabled)
            {
                var passId = definition.PassId ?? string.Empty;
                var invalidDefinition = false;
                if (string.IsNullOrEmpty(definition.GenerationProfileId) ||
                    !string.Equals(definition.GenerationProfileId, generationProfileId, StringComparison.Ordinal) ||
                    string.IsNullOrEmpty(passId) ||
                    string.IsNullOrEmpty(definition.ClassName) ||
                    definition.MaxRetryCount < 0 ||
                    definition.MaxRetryCount == int.MaxValue)
                {
                    issues.Add(PlanIssue(passId, "INVALID_PASS_DEFINITION", "Pass definition has invalid scalar fields."));
                    invalidDefinition = true;
                }
                if (!ids.Add(passId))
                {
                    issues.Add(PlanIssue(passId, "INVALID_PASS_DEFINITION", "Enabled pass identifier is duplicated."));
                    invalidDefinition = true;
                }
                if (!orders.Add(definition.PassOrder))
                {
                    issues.Add(PlanIssue(passId, "INVALID_PASS_DEFINITION", "Enabled pass order is duplicated."));
                    invalidDefinition = true;
                }

                WorldGenerationFailurePolicy policy = default(WorldGenerationFailurePolicy);
                try
                {
                    policy = WorldGenerationFailurePolicyToken.Parse(definition.FailurePolicy);
                }
                catch (ArgumentException)
                {
                    issues.Add(PlanIssue(passId, "INVALID_PASS_DEFINITION", "Pass failure policy is invalid."));
                    invalidDefinition = true;
                }

                var inputs = ValidateArtifactList(definition.InputArtifacts, passId, "input", issues);
                var outputs = ValidateArtifactList(definition.OutputArtifacts, passId, "output", issues);
                if (inputs.Intersect(outputs, StringComparer.Ordinal).Any())
                    issues.Add(PlanIssue(passId, "INVALID_ARTIFACT_PLAN", "A pass cannot consume and produce the same artifact."));

                foreach (var input in inputs)
                {
                    if (!owners.ContainsKey(input))
                        issues.Add(PlanIssue(passId, "INVALID_ARTIFACT_PLAN", "Input has no exact earlier producer: " + input));
                }
                foreach (var output in outputs)
                {
                    if (owners.ContainsKey(output))
                        issues.Add(PlanIssue(passId, "ARTIFACT_OWNERSHIP_CONFLICT", "Artifact has more than one producer: " + output));
                    else
                        owners.Add(output, passId);
                }

                if (!string.IsNullOrEmpty(definition.RngStreamId) &&
                    !IsValidRngDefinition(definitions, definition.RngStreamId))
                    issues.Add(PlanIssue(passId, "INVALID_RNG_DEFINITION", "RNG definition is missing, inactive, or invalid."));

                IWorldGenerationPass implementation = null;
                if (string.IsNullOrEmpty(passId) || !passRegistry.TryGet(passId, out implementation))
                    issues.Add(PlanIssue(passId, "MISSING_PASS_IMPLEMENTATION", "Pass implementation is not registered."));
                else if (!string.Equals(implementation.PassId, passId, StringComparison.Ordinal) ||
                         !string.Equals(implementation.ClassName, definition.ClassName, StringComparison.Ordinal))
                    issues.Add(PlanIssue(passId, "PASS_CLASS_MISMATCH", "Registered pass class does not exactly match the definition."));

                if (!invalidDefinition && implementation != null)
                    entries.Add(new PlanEntry(definition, policy, inputs, outputs, implementation));
            }

            return new Plan(profile, entries, issues);
        }

        private static IReadOnlyList<string> ValidateArtifactList(
            IReadOnlyList<string> source,
            string passId,
            string kind,
            ICollection<WorldGenerationRootIssue> issues)
        {
            if (source == null)
            {
                issues.Add(PlanIssue(passId, "INVALID_ARTIFACT_PLAN", "Pass " + kind + " artifact list is null."));
                return Array.Empty<string>();
            }
            var copy = source.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in copy)
            {
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                    issues.Add(PlanIssue(passId, "INVALID_ARTIFACT_PLAN", "Pass " + kind + " artifacts contain an empty or duplicate identifier."));
            }
            return copy;
        }

        private static bool IsValidRngDefinition(WorldRouteDefinitionSet definitions, string rngStreamId)
        {
            if (!definitions.RngStreams.TryGetValue(rngStreamId, out var definition) ||
                definition == null ||
                !definition.Active ||
                !string.Equals(definition.RngStreamId, rngStreamId, StringComparison.Ordinal) ||
                definition.SaltHex == null ||
                definition.SaltHex.Bytes == null ||
                definition.SaltHex.Bytes.Count != 8)
                return false;
            try
            {
                RngResetScopeToken.Parse(definition.ResetScope);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool OutputSetMatches(
            IReadOnlyList<string> expected,
            IReadOnlyDictionary<string, object> actual)
        {
            if (actual == null || actual.Count != expected.Count) return false;
            return expected.All(actual.ContainsKey);
        }

        private static WorldGenerationRootResult PlanFailure(IReadOnlyList<WorldGenerationRootIssue> planIssues)
        {
            var final = new List<WorldGenerationRootIssue>(planIssues.Count);
            for (var index = 0; index < planIssues.Count; index++)
                final.Add(index == planIssues.Count - 1 ? planIssues[index].AsTerminal() : planIssues[index]);
            return new WorldGenerationRootResult(
                false,
                new WorldGenerationArtifactStore(),
                final,
                string.Empty);
        }

        private static WorldGenerationRootResult Failure(
            WorldGenerationArtifactStore artifacts,
            IEnumerable<WorldGenerationRootIssue> issues,
            string lastCompletedPassId)
        {
            return new WorldGenerationRootResult(false, artifacts, issues, lastCompletedPassId);
        }

        private static WorldGenerationRootIssue PlanIssue(string passId, string code, string message)
        {
            return Issue(passId, code, message, 0, string.Empty, false);
        }

        private static WorldGenerationRootIssue Issue(
            string passId,
            string code,
            string message,
            int attemptOrdinal,
            string retryScopeId,
            bool terminal)
        {
            return new WorldGenerationRootIssue(
                passId, code, message, attemptOrdinal, retryScopeId, terminal);
        }

        private sealed class Plan
        {
            public Plan(
                GenerationProfileDefinition generationProfile,
                IReadOnlyList<PlanEntry> entries,
                IReadOnlyList<WorldGenerationRootIssue> issues)
            {
                GenerationProfile = generationProfile;
                Entries = entries;
                Issues = issues;
            }

            public GenerationProfileDefinition GenerationProfile { get; }
            public IReadOnlyList<PlanEntry> Entries { get; }
            public IReadOnlyList<WorldGenerationRootIssue> Issues { get; }
        }

        private sealed class PlanEntry
        {
            public PlanEntry(
                GenerationPassDefinition definition,
                WorldGenerationFailurePolicy policy,
                IReadOnlyList<string> inputIds,
                IReadOnlyList<string> outputIds,
                IWorldGenerationPass implementation)
            {
                Definition = definition;
                Policy = policy;
                InputIds = inputIds;
                OutputIds = outputIds;
                Implementation = implementation;
            }

            public GenerationPassDefinition Definition { get; }
            public WorldGenerationFailurePolicy Policy { get; }
            public IReadOnlyList<string> InputIds { get; }
            public IReadOnlyList<string> OutputIds { get; }
            public IWorldGenerationPass Implementation { get; }
        }
    }
}
