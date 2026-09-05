using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Validation;
using UnityEngine;

namespace StarNight.Map.Editor.WorldGeneration.Validation
{
    public static class GeneratedScaleAuditEditorRunner
    {
        public const string RelativeOutputRoot = "MapDesign/MCP/GENERATED/MAP19_09";

        public static GeneratedScaleAuditSummary RunProjectAudit()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var previousResult = Path.Combine(projectRoot, "MapDesign", "MCP", "REPORTS",
                "MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER_RESULT.md");
            var previousTask = Path.Combine(projectRoot, "MapDesign", "MCP", "TASKS",
                "MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER.md");
            var currentTask = Path.Combine(projectRoot, "MapDesign", "MCP", "TASKS",
                "MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT.md");
            var resultText = File.ReadAllText(previousResult, Encoding.UTF8);
            var preconditions = new GeneratedScaleAuditPreconditions(
                HashFile(previousResult), HashFile(previousTask),
                ReadDigest(resultText, "MAP19_09 handoff digest lower-hex SHA-256:"),
                ReadDigest(resultText, "failure bundle schema digest lower-hex SHA-256:"),
                ReadDigest(resultText, "runner plan digest lower-hex SHA-256:"),
                HashFile(currentTask));
            var chain = GeneratedScaleAuditChain.Create();
            if (!preconditions.IsValid || chain.Validate("MAP19_09_EDITOR_PREFLIGHT").Count != 0)
                return GeneratedScaleAuditOrchestrator.Run(preconditions, chain, null);

            var outputRoot = ResolveOutputRoot(projectRoot);
            if (Directory.Exists(outputRoot) && Directory.EnumerateFileSystemEntries(outputRoot).Any())
                throw new IOException("MAP19_09_OUTPUT_ALREADY_EXISTS");
            Directory.CreateDirectory(outputRoot);
            GeneratedScaleAuditFailureBundleWriter writer =
                delegate(GeneratedScaleAuditFailure failure, GeneratedFailureBundlePayload payload,
                    out string outputPath)
                {
                    return TryWriteFailureBundle(outputRoot, failure, payload, out outputPath);
                };
            var summary = GeneratedScaleAuditOrchestrator.Run(preconditions, chain, writer);
            WriteSummary(outputRoot, summary);
            return summary;
        }

        public static string ResolveOutputRoot(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("PROJECT_ROOT_REQUIRED", nameof(projectRoot));
            var root = Path.GetFullPath(projectRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var output = Path.GetFullPath(Path.Combine(root, "MapDesign", "MCP", "GENERATED",
                "MAP19_09"));
            var expectedPrefix = Path.Combine(root, "MapDesign", "MCP", "GENERATED") +
                Path.DirectorySeparatorChar;
            if (!output.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(output, Path.Combine(root, RelativeOutputRoot.Replace('/',
                    Path.DirectorySeparatorChar)), StringComparison.OrdinalIgnoreCase))
                throw new IOException("MAP19_09_OUTPUT_ROOT_SAFETY_CHECK_FAILED");
            return output;
        }

        private static bool TryWriteFailureBundle(string outputRoot,
            GeneratedScaleAuditFailure failure, GeneratedFailureBundlePayload payload,
            out string outputPath)
        {
            outputPath = string.Empty;
            if (failure == null || payload == null) return false;
            var failuresRoot = Path.Combine(outputRoot, "failures");
            Directory.CreateDirectory(failuresRoot);
            var name = "seed-" + failure.Seed.ToString(CultureInfo.InvariantCulture);
            var final = Path.Combine(failuresRoot, name);
            var staging = Path.Combine(failuresRoot, "." + name + ".staging");
            if (Directory.Exists(final) || File.Exists(final) || Directory.Exists(staging) ||
                File.Exists(staging)) return false;
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
                    { "replay.txt", "MAP19_09_REPLAY --seed " +
                        failure.Seed.ToString(CultureInfo.InvariantCulture) + "\n" },
                };
                foreach (var pair in files.OrderBy(value => value.Key, StringComparer.Ordinal))
                    File.WriteAllText(Path.Combine(staging, pair.Key), pair.Value,
                        BakingCanonicalDigest.Utf8NoBomEncoding);
                if (Directory.GetFiles(staging).Length != 7) throw new IOException("OUTPUT_COUNT_MISMATCH");
                Directory.Move(staging, final);
                outputPath = final;
                return true;
            }
            catch
            {
                try
                {
                    if (Directory.Exists(staging)) Directory.Delete(staging, true);
                }
                catch
                {
                    // Preserve the original explicit write failure.
                }
                outputPath = string.Empty;
                return false;
            }
        }

        private static void WriteSummary(string outputRoot, GeneratedScaleAuditSummary summary)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            var final = Path.Combine(outputRoot, "scale_audit_summary.json");
            var temporary = final + ".tmp";
            if (File.Exists(final) || File.Exists(temporary))
                throw new IOException("MAP19_09_SUMMARY_ALREADY_EXISTS");
            File.WriteAllText(temporary, SummaryJson(summary),
                BakingCanonicalDigest.Utf8NoBomEncoding);
            File.Move(temporary, final);
        }

        private static string SummaryJson(GeneratedScaleAuditSummary summary)
        {
            var builder = new StringBuilder();
            builder.Append('{')
                .Append("\"schemaVersion\":\"").Append(GeneratedScaleAuditContract.SchemaVersion)
                .Append("\",\"status\":\"").Append(summary.Status)
                .Append("\",\"executedSeeds\":").Append(summary.ExecutedSeeds.ToString(CultureInfo.InvariantCulture))
                .Append(",\"passedSeeds\":").Append(summary.PassedSeeds.ToString(CultureInfo.InvariantCulture))
                .Append(",\"failureBundleCount\":").Append(summary.FailureBundleCount.ToString(CultureInfo.InvariantCulture))
                .Append(",\"projectedTierCMilliseconds\":")
                .Append(summary.ProjectedTierCMilliseconds.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"summaryDigest\":\"").Append(summary.SummaryDigest)
                .Append("\",\"scaleAuditDigest\":\"").Append(summary.ScaleAuditDigest)
                .Append("\",\"map19ExitDigest\":\"").Append(summary.Map19ExitDigest)
                .Append("\",\"map20_01HandoffDigest\":\"").Append(summary.Map20_01HandoffDigest)
                .Append("\",\"tiers\":[");
            builder.Append(string.Join(",", summary.TierResults.Select(TierJson)));
            builder.Append("]}");
            return builder.ToString();
        }

        private static string TierJson(GeneratedScaleAuditTierResult value) => string.Join(string.Empty,
            "{\"tier\":\"", value.Tier.TierId, "\",\"seedStart\":",
            value.Tier.SeedStart.ToString(CultureInfo.InvariantCulture), ",\"seedCount\":",
            value.Requested.ToString(CultureInfo.InvariantCulture), ",\"executed\":",
            value.Executed.ToString(CultureInfo.InvariantCulture), ",\"passed\":",
            value.Passed.ToString(CultureInfo.InvariantCulture), ",\"failed\":",
            value.Failed.ToString(CultureInfo.InvariantCulture), ",\"crashed\":",
            value.Crashed.ToString(CultureInfo.InvariantCulture), ",\"wallMilliseconds\":",
            value.WallMilliseconds.ToString("R", CultureInfo.InvariantCulture), ",\"p50Milliseconds\":",
            value.P50Milliseconds.ToString("R", CultureInfo.InvariantCulture), ",\"p95Milliseconds\":",
            value.P95Milliseconds.ToString("R", CultureInfo.InvariantCulture), ",\"maximumMilliseconds\":",
            value.MaximumMilliseconds.ToString("R", CultureInfo.InvariantCulture),
            ",\"nextTierDecision\":\"", value.NextTierDecision, "\"}");

        private static string HashFile(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream).Select(value =>
                    value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string ReadDigest(string text, string prefix)
        {
            foreach (var line in BakingCanonicalDigest.NormalizeLineEndingsToLf(text ?? string.Empty)
                .Split('\n'))
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                    return line.Substring(prefix.Length).Trim();
            return string.Empty;
        }
    }
}
