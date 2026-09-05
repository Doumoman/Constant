using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Tooling
{
    public sealed class GeneratedTerrainRunCoordinator
    {
        public const string RelativeOutputRoot = "MapDesign/MCP/GENERATED/MAP20_01";

        private readonly string projectRoot;
        private readonly string artifactRoot;
        private GeneratedTerrainRunRequest lastRequest;
        private GeneratedTerrainRollbackSnapshotManifest lastSnapshot;

        public GeneratedTerrainRunCoordinator()
            : this(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), RelativeOutputRoot)
        {
        }

        public GeneratedTerrainRunCoordinator(string sourceProjectRoot, string sourceArtifactRootRelative)
        {
            projectRoot = Path.GetFullPath(sourceProjectRoot ?? string.Empty);
            artifactRoot = ResolveTaskOwnedPath(NormalizeRelative(sourceArtifactRootRelative));
        }

        public bool IsRunning { get; private set; }
        public GeneratedTerrainRunArtifact LastArtifact { get; private set; }
        public GeneratedTerrainRollbackResult LastRollbackResult { get; private set; }
        public string LastRunDirectory { get; private set; } = string.Empty;
        public bool RollbackAvailable => LastArtifact != null && LastArtifact.rollback_available &&
                                         lastRequest != null && lastSnapshot != null;

        public static void PublishTaskSampleArtifacts()
        {
            const ulong seed = 731;
            const string runId = "sample-pattern-731";
            const string generatorVersion = "map20-tooling-v1";
            const string dataVersion = "map-v2-authoring";
            const string patternId = "sample-pattern";
            var inputDigest = GeneratedTerrainRunRequest.ComputeInputDigest(seed,
                GeneratedTerrainRunScope.Pattern, generatorVersion, dataVersion, patternId, 6, 6);
            var request = new GeneratedTerrainRunRequest(runId,
                GeneratedTerrainRunScope.Pattern, seed, generatorVersion, dataVersion, inputDigest,
                RelativeOutputRoot + "/sample_outputs", patternId, 6, 6, false, false);
            var plan = new GeneratedTerrainRunPlan(new[]
            {
                new GeneratedTerrainOutputMutation(
                    "patterns/sample-pattern/plan_artifact.json",
                    "{\n  \"scope\": \"Pattern\",\n  \"seed\": 731,\n  \"dry_run\": false\n}\n",
                    false),
            });
            var coordinator = new GeneratedTerrainRunCoordinator();
            var artifact = coordinator.Run(request, plan);
            var rollback = coordinator.RollbackLastRun();
            Debug.Log("MAP20_01 sample artifact=" + artifact.canonical_digest +
                      " rollback=" + rollback.canonical_digest);
        }

        public GeneratedTerrainRunArtifact Run(GeneratedTerrainRunRequest request,
            GeneratedTerrainRunPlan plan)
        {
            if (IsRunning) throw new InvalidOperationException("A generated terrain run is already active.");
            IsRunning = true;
            try
            {
                ValidateRequest(request);
                if (plan == null) throw new ArgumentNullException(nameof(plan));
                if (request.DryRun && plan.Mutations.Count != 0)
                    throw new InvalidOperationException("A dry-run plan cannot mutate generated output.");

                var outputRoot = ResolveTaskOwnedPath(request.OutputRootRelative);
                var boundaries = request.OutputBoundaries.ToArray();
                ValidatePlan(plan, boundaries);
                var runDirectory = ResolveInside(artifactRoot,
                    "runs/" + SafeSegment(request.RunId));
                Directory.CreateDirectory(runDirectory);

                var snapshot = CaptureSnapshot(request, outputRoot, boundaries, runDirectory);
                WriteTextAtomic(Path.Combine(runDirectory, "rollback_snapshot_manifest.json"),
                    snapshot.SealAndSerialize());

                var before = SnapshotMap(snapshot.entries);
                var changed = new SortedSet<string>(StringComparer.Ordinal);
                var created = new SortedSet<string>(StringComparer.Ordinal);
                var deleted = new SortedSet<string>(StringComparer.Ordinal);
                ClassifyMutations(plan.Mutations, before, changed, created, deleted);

                var mutated = false;
                GeneratedTerrainRollbackResult automaticRollback = null;
                if (!plan.IsFailure || plan.FailAfterMutation)
                {
                    ApplyMutations(outputRoot, plan.Mutations);
                    mutated = plan.Mutations.Count != 0;
                }

                if (plan.FailAfterMutation || plan.IsFailure)
                {
                    if (mutated)
                    {
                        automaticRollback = RestoreSnapshot(request, snapshot, outputRoot,
                            runDirectory, "automatic rollback after failed run");
                        WriteTextAtomic(Path.Combine(runDirectory, "rollback_result.json"),
                            automaticRollback.SealAndSerialize());
                    }

                    var failure = BuildArtifact(request, snapshot, outputRoot, plan,
                        "FAIL", changed, created, deleted, automaticRollback != null);
                    PublishRunState(request, snapshot, runDirectory, failure, automaticRollback);
                    return failure;
                }

                var artifact = BuildArtifact(request, snapshot, outputRoot, plan,
                    "PASS", changed, created, deleted, false);
                PublishRunState(request, snapshot, runDirectory, artifact, null);
                return artifact;
            }
            finally
            {
                IsRunning = false;
            }
        }

        public GeneratedTerrainRollbackResult RollbackLastRun()
        {
            if (IsRunning) throw new InvalidOperationException("Rollback is disabled while a run is active.");
            if (!RollbackAvailable) throw new InvalidOperationException("No rollback snapshot is available.");
            IsRunning = true;
            try
            {
                var outputRoot = ResolveTaskOwnedPath(lastRequest.OutputRootRelative);
                var result = RestoreSnapshot(lastRequest, lastSnapshot, outputRoot,
                    LastRunDirectory, "manual rollback requested by user");
                WriteTextAtomic(Path.Combine(LastRunDirectory, "rollback_result.json"),
                    result.SealAndSerialize());
                LastArtifact.rollback_applied = true;
                LastArtifact.post_run_output_digest = result.post_rollback_output_digest;
                WriteTextAtomic(Path.Combine(LastRunDirectory, "run_artifact.json"),
                    LastArtifact.SealAndSerialize());
                LastRollbackResult = result;
                return result;
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void PublishRunState(GeneratedTerrainRunRequest request,
            GeneratedTerrainRollbackSnapshotManifest snapshot, string runDirectory,
            GeneratedTerrainRunArtifact artifact, GeneratedTerrainRollbackResult rollback)
        {
            WriteTextAtomic(Path.Combine(runDirectory, "run_artifact.json"),
                artifact.SealAndSerialize());
            lastRequest = request;
            lastSnapshot = snapshot;
            LastRunDirectory = runDirectory;
            LastArtifact = artifact;
            LastRollbackResult = rollback;
        }

        private GeneratedTerrainRollbackSnapshotManifest CaptureSnapshot(
            GeneratedTerrainRunRequest request, string outputRoot, IReadOnlyList<string> boundaries,
            string runDirectory)
        {
            var entries = new List<GeneratedTerrainRollbackSnapshotEntry>();
            foreach (var boundary in boundaries.OrderBy(value => value, StringComparer.Ordinal))
            {
                var boundaryPath = ResolveInside(outputRoot, boundary);
                if (!Directory.Exists(boundaryPath)) continue;
                foreach (var sourcePath in Directory.GetFiles(boundaryPath, "*", SearchOption.AllDirectories)
                    .OrderBy(value => value, StringComparer.Ordinal))
                {
                    var relative = RelativeTo(outputRoot, sourcePath);
                    var backupRelative = "snapshot_files/" + relative;
                    var backupPath = ResolveInside(runDirectory, backupRelative);
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                    File.Copy(sourcePath, backupPath, true);
                    var bytes = File.ReadAllBytes(backupPath);
                    entries.Add(new GeneratedTerrainRollbackSnapshotEntry
                    {
                        relative_path = relative,
                        content_digest = HashBytes(bytes),
                        byte_count = bytes.LongLength,
                        backup_relative_path = backupRelative,
                    });
                }
            }

            var preDigest = DigestEntries(entries);
            return new GeneratedTerrainRollbackSnapshotManifest
            {
                run_id = request.RunId,
                scope = GeneratedTerrainRunScopeCatalog.Token(request.Scope),
                output_root_relative = request.OutputRootRelative,
                selected_boundaries = boundaries.ToArray(),
                entries = entries.ToArray(),
                pre_run_output_digest = preDigest,
                created_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            };
        }

        private GeneratedTerrainRunArtifact BuildArtifact(GeneratedTerrainRunRequest request,
            GeneratedTerrainRollbackSnapshotManifest snapshot, string outputRoot,
            GeneratedTerrainRunPlan plan, string passState, IEnumerable<string> changed,
            IEnumerable<string> created, IEnumerable<string> deleted, bool rollbackApplied)
        {
            var failed = !string.Equals(passState, "PASS", StringComparison.Ordinal);
            return new GeneratedTerrainRunArtifact
            {
                schema_version = GeneratedTerrainRunPreconditions.SchemaVersion,
                task_id = GeneratedTerrainRunPreconditions.TaskId,
                run_id = request.RunId,
                scope = GeneratedTerrainRunScopeCatalog.Token(request.Scope),
                seed = request.Seed.ToString(CultureInfo.InvariantCulture),
                generator_version = request.GeneratorVersion,
                data_version = request.DataVersion,
                input_digest = request.InputDigest,
                pre_run_output_digest = snapshot.pre_run_output_digest,
                post_run_output_digest = DigestOutput(outputRoot, request.OutputBoundaries),
                pass_state = passState,
                failure_owner = failed ? EmptyFallback(plan.FailureOwner, "GENERATED_TERRAIN_RUN") : string.Empty,
                failure_reason = failed ? EmptyFallback(plan.FailureReason, "run plan reported failure") : string.Empty,
                rollback_available = true,
                rollback_snapshot_digest = snapshot.canonical_digest,
                rollback_applied = rollbackApplied,
                changed_paths = changed.ToArray(),
                created_paths = created.ToArray(),
                deleted_paths = deleted.ToArray(),
                replay_reference = request.ReplayReference,
                failure_bundle_reference = failed
                    ? EmptyFallback(plan.FailureBundleReference,
                        "map19_08://failure-bundle?run_id=" + Uri.EscapeDataString(request.RunId))
                    : string.Empty,
                MAP19_exit_digest = GeneratedTerrainRunPreconditions.Map19ExitDigest,
                MAP20_01_handoff_digest = GeneratedTerrainRunPreconditions.Map2001HandoffDigest,
                created_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                created_utc_excluded_from_canonical_digest = true,
            };
        }

        private GeneratedTerrainRollbackResult RestoreSnapshot(GeneratedTerrainRunRequest request,
            GeneratedTerrainRollbackSnapshotManifest snapshot, string outputRoot,
            string runDirectory, string reason)
        {
            ValidateSnapshot(snapshot, runDirectory);
            var boundaries = snapshot.selected_boundaries.ToArray();
            var guardRoot = ResolveInside(runDirectory, "rollback_guard");
            if (Directory.Exists(guardRoot)) Directory.Delete(guardRoot, true);
            Directory.CreateDirectory(guardRoot);
            CaptureCurrentBoundaries(outputRoot, boundaries, guardRoot);
            var beforeRollback = CurrentFileSet(outputRoot, boundaries);

            try
            {
                ClearBoundaries(outputRoot, boundaries);
                foreach (var entry in snapshot.entries.OrderBy(value => value.relative_path,
                    StringComparer.Ordinal))
                {
                    var source = ResolveInside(runDirectory, entry.backup_relative_path);
                    var destination = ResolveInside(outputRoot, entry.relative_path);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.Copy(source, destination, true);
                }
            }
            catch
            {
                ClearBoundaries(outputRoot, boundaries);
                RestoreGuard(outputRoot, guardRoot);
                throw;
            }

            var afterRollback = CurrentFileSet(outputRoot, boundaries);
            var expected = new SortedSet<string>(snapshot.entries.Select(value => value.relative_path),
                StringComparer.Ordinal);
            if (!afterRollback.SetEquals(expected))
            {
                ClearBoundaries(outputRoot, boundaries);
                RestoreGuard(outputRoot, guardRoot);
                throw new InvalidOperationException("Atomic rollback verification failed; current output was restored.");
            }
            foreach (var entry in snapshot.entries)
            {
                if (!string.Equals(HashBytes(File.ReadAllBytes(ResolveInside(outputRoot,
                        entry.relative_path))), entry.content_digest, StringComparison.Ordinal))
                {
                    ClearBoundaries(outputRoot, boundaries);
                    RestoreGuard(outputRoot, guardRoot);
                    throw new InvalidOperationException("Atomic rollback content verification failed; current output was restored.");
                }
            }

            if (Directory.Exists(guardRoot)) Directory.Delete(guardRoot, true);
            return new GeneratedTerrainRollbackResult
            {
                run_id = request.RunId,
                scope = GeneratedTerrainRunScopeCatalog.Token(request.Scope),
                pass_state = "PASS",
                reason = reason,
                restored_path_count = snapshot.entries.Length,
                removed_path_count = beforeRollback.Except(expected, StringComparer.Ordinal).Count(),
                selected_boundaries = boundaries,
                snapshot_digest = snapshot.canonical_digest,
                post_rollback_output_digest = DigestOutput(outputRoot, boundaries),
                created_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            };
        }

        private static void ValidateSnapshot(GeneratedTerrainRollbackSnapshotManifest snapshot,
            string runDirectory)
        {
            if (snapshot == null || !BakingCanonicalDigest.IsLowerHexSha256(snapshot.canonical_digest))
                throw new InvalidOperationException("Rollback snapshot is missing or unsealed.");
            foreach (var entry in snapshot.entries)
            {
                var backup = ResolveInside(runDirectory, entry.backup_relative_path);
                if (!File.Exists(backup) || !string.Equals(HashBytes(File.ReadAllBytes(backup)),
                        entry.content_digest, StringComparison.Ordinal))
                    throw new InvalidOperationException("Rollback snapshot backup failed digest validation.");
            }
        }

        private static void CaptureCurrentBoundaries(string outputRoot,
            IEnumerable<string> boundaries, string guardRoot)
        {
            foreach (var boundary in boundaries)
            {
                var source = ResolveInside(outputRoot, boundary);
                if (!Directory.Exists(source)) continue;
                foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                {
                    var relative = RelativeTo(outputRoot, file);
                    var destination = ResolveInside(guardRoot, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.Copy(file, destination, true);
                }
            }
        }

        private static void RestoreGuard(string outputRoot, string guardRoot)
        {
            if (!Directory.Exists(guardRoot)) return;
            foreach (var file in Directory.GetFiles(guardRoot, "*", SearchOption.AllDirectories))
            {
                var relative = RelativeTo(guardRoot, file);
                var destination = ResolveInside(outputRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination, true);
            }
        }

        private static void ClearBoundaries(string outputRoot, IEnumerable<string> boundaries)
        {
            foreach (var boundary in boundaries.OrderByDescending(value => value.Length))
            {
                var path = ResolveInside(outputRoot, boundary);
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
        }

        private static void ApplyMutations(string outputRoot,
            IEnumerable<GeneratedTerrainOutputMutation> mutations)
        {
            foreach (var mutation in mutations)
            {
                var destination = ResolveInside(outputRoot, mutation.RelativePath);
                if (mutation.Delete)
                {
                    if (File.Exists(destination)) File.Delete(destination);
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                WriteTextAtomic(destination, mutation.Utf8Content);
            }
        }

        private static void ClassifyMutations(IEnumerable<GeneratedTerrainOutputMutation> mutations,
            IReadOnlyDictionary<string, string> before, ISet<string> changed, ISet<string> created,
            ISet<string> deleted)
        {
            foreach (var mutation in mutations)
            {
                string previousDigest;
                var existed = before.TryGetValue(mutation.RelativePath, out previousDigest);
                if (mutation.Delete)
                {
                    if (existed)
                    {
                        changed.Add(mutation.RelativePath);
                        deleted.Add(mutation.RelativePath);
                    }
                    continue;
                }
                var nextDigest = HashBytes(BakingCanonicalDigest.Utf8NoBomEncoding.GetBytes(
                    mutation.Utf8Content));
                if (!existed)
                {
                    changed.Add(mutation.RelativePath);
                    created.Add(mutation.RelativePath);
                }
                else if (!string.Equals(previousDigest, nextDigest, StringComparison.Ordinal))
                {
                    changed.Add(mutation.RelativePath);
                }
            }
        }

        private static IReadOnlyDictionary<string, string> SnapshotMap(
            IEnumerable<GeneratedTerrainRollbackSnapshotEntry> entries) =>
            (entries ?? Array.Empty<GeneratedTerrainRollbackSnapshotEntry>()).ToDictionary(
                value => value.relative_path, value => value.content_digest, StringComparer.Ordinal);

        private static string DigestOutput(string outputRoot, IEnumerable<string> boundaries)
        {
            var entries = new List<GeneratedTerrainRollbackSnapshotEntry>();
            foreach (var boundary in boundaries.OrderBy(value => value, StringComparer.Ordinal))
            {
                var path = ResolveInside(outputRoot, boundary);
                if (!Directory.Exists(path)) continue;
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    .OrderBy(value => value, StringComparer.Ordinal))
                {
                    var bytes = File.ReadAllBytes(file);
                    entries.Add(new GeneratedTerrainRollbackSnapshotEntry
                    {
                        relative_path = RelativeTo(outputRoot, file),
                        content_digest = HashBytes(bytes),
                        byte_count = bytes.LongLength,
                    });
                }
            }
            return DigestEntries(entries);
        }

        private static string DigestEntries(IEnumerable<GeneratedTerrainRollbackSnapshotEntry> entries) =>
            BakingCanonicalDigest.HashCanonicalLines((entries ??
                Array.Empty<GeneratedTerrainRollbackSnapshotEntry>())
                .OrderBy(value => value.relative_path, StringComparer.Ordinal)
                .Select(value => string.Join("|", new[]
                {
                    value.relative_path ?? string.Empty,
                    value.content_digest ?? string.Empty,
                    value.byte_count.ToString(CultureInfo.InvariantCulture),
                })));

        private static SortedSet<string> CurrentFileSet(string outputRoot,
            IEnumerable<string> boundaries)
        {
            var result = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var boundary in boundaries)
            {
                var path = ResolveInside(outputRoot, boundary);
                if (!Directory.Exists(path)) continue;
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    result.Add(RelativeTo(outputRoot, file));
            }
            return result;
        }

        private void ValidateRequest(GeneratedTerrainRunRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            SafeSegment(request.RunId);
            if (!GeneratedTerrainRunScopeCatalog.Scopes.Contains(request.Scope))
                throw new ArgumentOutOfRangeException(nameof(request.Scope));
            if (!BakingCanonicalDigest.IsLowerHexSha256(request.InputDigest))
                throw new InvalidOperationException("Input digest must be lowercase SHA-256.");
            if (string.IsNullOrWhiteSpace(request.GeneratorVersion) ||
                string.IsNullOrWhiteSpace(request.DataVersion))
                throw new InvalidOperationException("Generator and data versions are required.");
            if (request.Scope == GeneratedTerrainRunScope.World && !request.WorldConfirmed)
                throw new InvalidOperationException("World scope requires explicit confirmation.");
            ResolveTaskOwnedPath(request.OutputRootRelative);
        }

        private static void ValidatePlan(GeneratedTerrainRunPlan plan,
            IEnumerable<string> boundaries)
        {
            var allowed = boundaries.ToArray();
            foreach (var mutation in plan.Mutations)
            {
                var relative = NormalizeRelative(mutation.RelativePath);
                if (!allowed.Any(boundary => IsWithin(relative, boundary)))
                    throw new InvalidOperationException("Mutation is outside the selected generated-output scope: " + relative);
            }
        }

        private string ResolveTaskOwnedPath(string relative)
        {
            var normalized = NormalizeRelative(relative);
            if (!IsWithin(normalized, RelativeOutputRoot))
                throw new InvalidOperationException("Generated output must remain under " + RelativeOutputRoot + ".");
            return ResolveInside(projectRoot, normalized);
        }

        private static bool IsWithin(string candidate, string root) =>
            string.Equals(candidate, root, StringComparison.Ordinal) ||
            candidate.StartsWith(root.TrimEnd('/') + "/", StringComparison.Ordinal);

        private static string NormalizeRelative(string value)
        {
            var normalized = (value ?? string.Empty).Replace('\\', '/').Trim('/');
            if (string.IsNullOrWhiteSpace(normalized) || Path.IsPathRooted(normalized) ||
                normalized.Split('/').Any(segment => segment == "." || segment == ".."))
                throw new InvalidOperationException("A safe project-relative path is required.");
            return normalized;
        }

        private static string SafeSegment(string value)
        {
            var normalized = NormalizeRelative(value);
            if (normalized.Contains("/") || normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidOperationException("Run id must be one safe path segment.");
            return normalized;
        }

        private static string ResolveInside(string root, string relative)
        {
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(Path.Combine(fullRoot,
                NormalizeRelative(relative).Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Resolved path escaped its owned root.");
            return full;
        }

        private static string RelativeTo(string root, string path)
        {
            var rootUri = new Uri(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(new Uri(Path.GetFullPath(path))).ToString())
                .Replace('\\', '/').Trim('/');
        }

        private static string HashBytes(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(bytes ?? Array.Empty<byte>())
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static void WriteTextAtomic(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, contents ?? string.Empty, BakingCanonicalDigest.Utf8NoBomEncoding);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }

        private static string EmptyFallback(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
