using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.Visualization
{
    /// <summary>
    /// VIS02 entrypoint. Both modes call the VIS01 generator; only Run calls the VIS01 publisher/scene builder.
    /// </summary>
    public static class MoonPalaceGrayboxRunCoordinator
    {
        private static readonly List<MoonPalaceGrayboxRunResult> RunHistory = new List<MoonPalaceGrayboxRunResult>();

        public static MoonPalaceGrayboxRunResult LastRun { get; private set; }
        public static IReadOnlyList<MoonPalaceGrayboxRunResult> GetRunHistory()
        {
            return new ReadOnlyCollection<MoonPalaceGrayboxRunResult>(RunHistory.ToList());
        }

        public static MoonPalaceGrayboxRunResult DryRun()
        {
            return Execute(MoonPalaceGrayboxRunRequest.CreateDefault(MoonPalaceGrayboxRunMode.DryRun));
        }

        public static MoonPalaceGrayboxRunResult Run()
        {
            return Execute(MoonPalaceGrayboxRunRequest.CreateDefault(MoonPalaceGrayboxRunMode.Run));
        }

        public static MoonPalaceGrayboxRunResult Execute(MoonPalaceGrayboxRunRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();
            var root = Directory.GetParent(Application.dataPath).FullName;

            if (request.Mode == MoonPalaceGrayboxRunMode.DryRun)
            {
                // The real VIS01 flow executes in memory. No VIS01 output path or Scene is touched here.
                var publication = MoonPalaceGrayboxExampleSceneBuilder.BuildSnapshot(root, false);
                var result = CreateResult(request, publication.Generation, publication.SceneManifestDigest,
                    false, false, false, 0, 0, 0,
                    new MoonPalaceGrayboxWriteScopeSummary(Array.Empty<string>(),
                        new[] { MoonPalaceGrayboxRunHistoryPublisher.DryRunResultRelativePath }, Array.Empty<string>()));
                RecordAndPublish(root, result);
                return result;
            }

            EnsureDryRunPreviewForRun(root);
            var activeTargetWasReplaced = PrepareTargetSceneForSafeRegeneration(request.OutputScenePath);
            // Publish is the existing VIS01 generator and builder: it regenerates logical artifacts and Scene together.
            var publicationForRun = MoonPalaceGrayboxExampleSceneBuilder.Publish(root);
            if (!File.Exists(Resolve(root, request.OutputScenePath)))
                throw new IOException("VIS02 Run did not recreate the isolated VIS01 Scene.");
            if (activeTargetWasReplaced)
                EditorSceneManager.OpenScene(request.OutputScenePath, OpenSceneMode.Single);

            var vis01Paths = publicationForRun.OutputContents.Keys
                .Concat(new[] { request.OutputScenePath })
                .Concat(MoonPalaceGrayboxExampleSceneBuilder.GetDebugTileAssetRelativePaths())
                .Distinct(StringComparer.Ordinal).ToArray();
            var runResult = CreateResult(request, publicationForRun.Generation, publicationForRun.SceneManifestDigest,
                true, false, true, 7, 6, 1,
                new MoonPalaceGrayboxWriteScopeSummary(vis01Paths,
                    MoonPalaceGrayboxRunHistoryPublisher.GetAllVis02RelativePaths(), Array.Empty<string>()));
            RecordAndPublish(root, runResult);
            return runResult;
        }

        public static void OpenVis01Scene()
        {
            var path = MoonPalaceGrayboxRunRequest.RequiredOutputScenePath;
            var existing = SceneManager.GetSceneByPath(path);
            if (existing.IsValid() && existing.isLoaded)
            {
                SceneManager.SetActiveScene(existing);
                return;
            }
            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByPath(path));
        }

        private static MoonPalaceGrayboxRunResult CreateResult(MoonPalaceGrayboxRunRequest request,
            MoonPalaceOneSectorGrayboxResult generation, string sceneManifestDigest, bool sceneWritten,
            bool sceneDeletedBeforeWrite, bool sceneRecreated, int csvCount, int jsonCount, int sceneCount,
            MoonPalaceGrayboxWriteScopeSummary scope)
        {
            ValidateGenerationContract(generation);
            return new MoonPalaceGrayboxRunResult(request, generation, sceneWritten, sceneDeletedBeforeWrite,
                sceneRecreated, csvCount, jsonCount, sceneCount, scope, sceneManifestDigest, string.Empty);
        }

        private static void RecordAndPublish(string projectRoot, MoonPalaceGrayboxRunResult result)
        {
            RunHistory.Add(result);
            LastRun = result;
            MoonPalaceGrayboxRunHistoryPublisher.Publish(projectRoot, result, GetRunHistory());
        }

        private static void EnsureDryRunPreviewForRun(string projectRoot)
        {
            if (RunHistory.Any(item => item.Mode == MoonPalaceGrayboxRunMode.DryRun)) return;
            var request = MoonPalaceGrayboxRunRequest.CreateDefault(MoonPalaceGrayboxRunMode.DryRun);
            var publication = MoonPalaceGrayboxExampleSceneBuilder.BuildSnapshot(projectRoot, false);
            var preview = CreateResult(request, publication.Generation, publication.SceneManifestDigest,
                false, false, false, 0, 0, 0,
                new MoonPalaceGrayboxWriteScopeSummary(Array.Empty<string>(),
                    new[] { MoonPalaceGrayboxRunHistoryPublisher.DryRunResultRelativePath }, Array.Empty<string>()));
            RecordAndPublish(projectRoot, preview);
        }

        private static bool PrepareTargetSceneForSafeRegeneration(string targetPath)
        {
            var target = SceneManager.GetSceneByPath(targetPath);
            if (!target.IsValid() || !target.isLoaded) return false;
            if (target.isDirty)
                throw new InvalidOperationException("VIS02 refuses to discard unsaved changes in the isolated VIS01 Scene.");
            var wasActive = SceneManager.GetActiveScene() == target;
            if (wasActive)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
                EditorSceneManager.CloseScene(target, true);
            return wasActive;
        }

        private static void ValidateGenerationContract(MoonPalaceOneSectorGrayboxResult generation)
        {
            if (generation == null || generation.CandidateSet.RawMaskCount != 65536 ||
                generation.CandidateSet.Candidates.Count != 500 || generation.PatternPlacements.Count != 96 ||
                generation.MicroChunks.Count != 16 || generation.Cells.Count != 1536 ||
                generation.LogicalLayers.Count != 10752 || !generation.Validation.Passed ||
                generation.Validation.FallbackCarveCount != 0 || generation.Validation.SilentAutoRepairCount != 0)
                throw new InvalidOperationException("VIS02 rejected a generation result outside the audited VIS01 contract.");
        }

        private static string Resolve(string root, string relativePath)
        {
            return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
