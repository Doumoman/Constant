using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.Editor.WorldGeneration.Validation
{
    public delegate bool GeneratedValidationTextWriter(string path, string text);

    public sealed class GeneratedHeadlessValidationRunResult
    {
        internal GeneratedHeadlessValidationRunResult(GeneratedValidationRunnerExitCode exitCode,
            GeneratedFailureBundlePayload payload, GeneratedValidationRunnerPlan plan,
            string handoffDigest, int filesWritten, int syntheticCallbacks,
            IEnumerable<GeneratedValidationRunnerFailure> failures)
        {
            ExitCode = exitCode;
            Payload = payload;
            Plan = plan;
            HandoffDigest = handoffDigest ?? string.Empty;
            FilesWritten = filesWritten;
            SyntheticCallbacks = syntheticCallbacks;
            Failures = new ReadOnlyCollection<GeneratedValidationRunnerFailure>((failures ??
                Array.Empty<GeneratedValidationRunnerFailure>()).OrderBy(value => value).ToArray());
        }

        public bool Success => ExitCode == GeneratedValidationRunnerExitCode.Success && Payload != null &&
            Plan != null && BakingCanonicalDigest.IsLowerHexSha256(HandoffDigest) && Failures.Count == 0;
        public GeneratedValidationRunnerExitCode ExitCode { get; }
        public GeneratedFailureBundlePayload Payload { get; }
        public GeneratedValidationRunnerPlan Plan { get; }
        public string HandoffDigest { get; }
        public int FilesWritten { get; }
        public int SyntheticCallbacks { get; }
        public IReadOnlyList<GeneratedValidationRunnerFailure> Failures { get; }
        public string SuccessManifestDigest => Success ? Payload.Manifest.ManifestDigest : string.Empty;
        public string SuccessPayloadDigest => Success ? Payload.PayloadDigest : string.Empty;
        public string SuccessPlanDigest => Success ? Plan.PlanDigest : string.Empty;
        public string SuccessHandoffDigest => Success ? HandoffDigest : string.Empty;
    }

    public static class GeneratedHeadlessValidationRunner
    {
        public const int RequiredOutputFileCount = 7;
        public const ulong MaximumFocusedSeedCount = 8UL;

        public static GeneratedValidationRunnerParseResult ParseCurrentArguments() =>
            GeneratedValidationRunnerArgumentParser.Parse(Environment.GetCommandLineArgs());

        public static GeneratedHeadlessValidationRunResult RunFocusedDryRun(
            GeneratedValidationRunnerArguments arguments,
            GeneratedValidationChainSnapshot chain,
            Func<GeneratedValidationRunnerPlan, GeneratedFailureBundleManifest> syntheticValidation,
            string explicitlyAllowedTemporaryRoot,
            GeneratedValidationTextWriter writer = null)
        {
            if (arguments == null || syntheticValidation == null)
                return Failed(GeneratedValidationRunnerExitCode.InvalidArguments, "INVALID_DRY_RUN_INPUT",
                    "dryRun", "ARGUMENTS_AND_CALLBACK", "MISSING", "MISSING");
            if (arguments.Mode != GeneratedValidationRunnerMode.Replay &&
                arguments.SeedCount > MaximumFocusedSeedCount)
                return Failed(GeneratedValidationRunnerExitCode.ValidationFailed,
                    "PRODUCTION_RANGE_ATTEMPT_REJECTED", "seedCount", "0..8",
                    arguments.SeedCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    arguments.RequestId);

            var planned = GeneratedValidationRunnerPlanner.Build(arguments, chain);
            if (!planned.Success)
                return new GeneratedHeadlessValidationRunResult(planned.ExitCode, null, null,
                    string.Empty, 0, 0, planned.Failures);

            GeneratedFailureBundleManifest manifest;
            try
            {
                manifest = syntheticValidation(planned.Plan);
            }
            catch (Exception exception)
            {
                return Failed(GeneratedValidationRunnerExitCode.ValidationFailed,
                    "SYNTHETIC_CALLBACK_FAILED", "callback", "SUCCESS",
                    exception.GetType().Name, arguments.RequestId);
            }

            var bundle = GeneratedFailureBundleFactory.Create(manifest);
            if (!bundle.Success)
            {
                var failures = bundle.Failures.Select(value => new GeneratedValidationRunnerFailure(
                    value.Owner, value.Reason, arguments.RequestId, value.OffendingKey, value.Expected,
                    value.Actual, value.SourceDigest, value.OutputPath));
                return new GeneratedHeadlessValidationRunResult(
                    GeneratedValidationRunnerExitCode.ValidationFailed, null, null, string.Empty,
                    0, 1, failures);
            }

            if (!TryWriteBundle(bundle.Payload, planned.Plan, arguments.OutputRoot,
                explicitlyAllowedTemporaryRoot, writer, out var writeFailure))
                return new GeneratedHeadlessValidationRunResult(
                    GeneratedValidationRunnerExitCode.OutputWriteFailed, null, null, string.Empty,
                    0, 1, new[] { writeFailure });

            var handoff = planned.Plan.BuildMap19_09HandoffDigest(bundle.Payload);
            return new GeneratedHeadlessValidationRunResult(GeneratedValidationRunnerExitCode.Success,
                bundle.Payload, planned.Plan, handoff, RequiredOutputFileCount, 1,
                Array.Empty<GeneratedValidationRunnerFailure>());
        }

        public static bool TryWriteBundle(GeneratedFailureBundlePayload payload,
            GeneratedValidationRunnerPlan plan, string outputRoot,
            string explicitlyAllowedTemporaryRoot, GeneratedValidationTextWriter writer,
            out GeneratedValidationRunnerFailure failure)
        {
            failure = null;
            if (payload == null || plan == null)
            {
                failure = F("MISSING_OUTPUT_PAYLOAD", outputRoot, "PAYLOAD_AND_PLAN", "MISSING");
                return false;
            }
            if (!TryResolveSafeOutput(outputRoot, explicitlyAllowedTemporaryRoot,
                out var resolvedOutput, out var reason))
            {
                failure = F("UNSAFE_OUTPUT_ROOT", outputRoot, "EXPLICIT_TEMP_CHILD", reason);
                return false;
            }
            if (Directory.Exists(resolvedOutput) || File.Exists(resolvedOutput))
            {
                failure = F("OUTPUT_ALREADY_EXISTS", resolvedOutput, "ABSENT", "EXISTS");
                return false;
            }

            var staging = resolvedOutput + ".map19_08_staging";
            if (Directory.Exists(staging) || File.Exists(staging))
            {
                failure = F("STAGING_ALREADY_EXISTS", staging, "ABSENT", "EXISTS");
                return false;
            }

            try
            {
                Directory.CreateDirectory(staging);
                var files = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "manifest.json", payload.ManifestJson },
                    { "pass_snapshots.csv", payload.PassSnapshotsCsv },
                    { "failures.csv", payload.FailuresCsv },
                    { "coordinates.csv", payload.CoordinatesCsv },
                    { "provenance.csv", payload.ProvenanceCsv },
                    { "screenshot_references.csv", payload.ScreenshotReferencesCsv },
                    { "runner_plan.json", RunnerJson(plan) },
                };
                var write = writer ?? DefaultWrite;
                foreach (var pair in files.OrderBy(value => value.Key, StringComparer.Ordinal))
                {
                    var path = Path.Combine(staging, pair.Key);
                    if (!write(path, pair.Value)) throw new IOException("WRITER_REJECTED");
                }
                if (Directory.GetFiles(staging).Length != RequiredOutputFileCount)
                    throw new IOException("OUTPUT_COUNT_MISMATCH");
                Directory.Move(staging, resolvedOutput);
                return true;
            }
            catch (Exception exception)
            {
                try
                {
                    if (Directory.Exists(staging)) Directory.Delete(staging, true);
                }
                catch
                {
                    // The failure remains explicit and no success digest is published.
                }
                failure = F("OUTPUT_WRITE_FAILED", resolvedOutput, "ATOMIC_SEVEN_FILES",
                    exception.GetType().Name);
                return false;
            }
        }

        public static bool TryResolveSafeOutput(string outputRoot,
            string explicitlyAllowedTemporaryRoot, out string resolvedOutput, out string reason)
        {
            resolvedOutput = string.Empty;
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(outputRoot) ||
                string.IsNullOrWhiteSpace(explicitlyAllowedTemporaryRoot))
            {
                reason = "EMPTY_ROOT";
                return false;
            }
            if (HasParentTraversal(outputRoot) || HasParentTraversal(explicitlyAllowedTemporaryRoot))
            {
                reason = "PARENT_TRAVERSAL";
                return false;
            }
            try
            {
                var allowed = Path.GetFullPath(explicitlyAllowedTemporaryRoot)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                resolvedOutput = Path.GetFullPath(outputRoot)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var temporary = Path.GetFullPath(Path.GetTempPath())
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!string.Equals(allowed, temporary, StringComparison.OrdinalIgnoreCase) &&
                    !allowed.StartsWith(temporary + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase))
                {
                    reason = "NOT_EXPLICIT_TEMP_ROOT";
                    return false;
                }
                if (ContainsReservedProjectSegment(allowed) ||
                    ContainsReservedProjectSegment(resolvedOutput))
                {
                    reason = "RESERVED_PROJECT_ROOT";
                    return false;
                }
                var prefix = allowed + Path.DirectorySeparatorChar;
                if (!resolvedOutput.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "OUTSIDE_ALLOWED_ROOT";
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                reason = exception.GetType().Name;
                resolvedOutput = string.Empty;
                return false;
            }
        }

        private static bool ContainsReservedProjectSegment(string path)
        {
            var reserved = new HashSet<string>(new[]
            {
                "Assets", "ProjectSettings", "Packages", "Library", "Logs",
            }, StringComparer.OrdinalIgnoreCase);
            return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(reserved.Contains);
        }

        private static bool HasParentTraversal(string path) => path
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(value => string.Equals(value, "..", StringComparison.Ordinal));

        private static bool DefaultWrite(string path, string text)
        {
            File.WriteAllText(path, text, BakingCanonicalDigest.Utf8NoBomEncoding);
            return true;
        }

        private static string RunnerJson(GeneratedValidationRunnerPlan plan)
        {
            var builder = new StringBuilder();
            builder.Append('{').Append("\"requestId\":\"").Append(plan.Arguments.RequestId)
                .Append("\",\"mode\":\"").Append(plan.Arguments.Mode)
                .Append("\",\"planDigest\":\"").Append(plan.PlanDigest)
                .Append("\",\"phaseOrder\":[");
            builder.Append(string.Join(",", plan.PhaseOrder.Select(value => "\"" + value + "\"")));
            builder.Append("]}");
            return builder.ToString();
        }

        private static GeneratedHeadlessValidationRunResult Failed(GeneratedValidationRunnerExitCode code,
            string reason, string key, string expected, string actual, string requestId) =>
            new GeneratedHeadlessValidationRunResult(code, null, null, string.Empty, 0, 0,
                new[] { new GeneratedValidationRunnerFailure("HeadlessRunner", reason, requestId,
                    key, expected, actual, BakingCanonicalDigest.HashCanonicalLines(new[]
                    { reason, key, expected })) });

        private static GeneratedValidationRunnerFailure F(string reason, string path,
            string expected, string actual) => new GeneratedValidationRunnerFailure("OutputWriter",
                reason, BakingCanonicalDigest.HashCanonicalLines(new[] { reason, path ?? string.Empty }),
                "output", expected, actual, BakingCanonicalDigest.HashCanonicalLines(new[]
                { reason, expected }), path);
    }
}
