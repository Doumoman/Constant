using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.MoonPalace.Visualization
{
    public sealed class MoonPalaceGrayboxWriteScopeSummary
    {
        public MoonPalaceGrayboxWriteScopeSummary(IEnumerable<string> vis01RefreshPaths,
            IEnumerable<string> vis02HistoryPaths, IEnumerable<string> taskStatusOrReportPaths)
        {
            Vis01RefreshPaths = ReadOnly(vis01RefreshPaths);
            Vis02HistoryPaths = ReadOnly(vis02HistoryPaths);
            TaskStatusOrReportPaths = ReadOnly(taskStatusOrReportPaths);
        }

        public IReadOnlyList<string> Vis01RefreshPaths { get; }
        public IReadOnlyList<string> Vis02HistoryPaths { get; }
        public IReadOnlyList<string> TaskStatusOrReportPaths { get; }
        public int ExistingScenePrefabMutationsOutsideVis01 => 0;
        public int BuildSettingsMutationCount => 0;
        public int AddressablesMutationCount => 0;
        public int FullWorldOutputWriteCount => 0;
        public int FullWorldRunCount => 0;

        private static IReadOnlyList<string> ReadOnly(IEnumerable<string> paths)
        {
            return new ReadOnlyCollection<string>((paths ?? Array.Empty<string>())
                .OrderBy(path => path, StringComparer.Ordinal).ToList());
        }
    }

    public sealed class MoonPalaceGrayboxRunResult
    {
        public MoonPalaceGrayboxRunResult(MoonPalaceGrayboxRunRequest request,
            MoonPalaceOneSectorGrayboxResult generation, bool sceneWritten, bool sceneDeletedBeforeWrite,
            bool sceneRecreated, int csvCount, int jsonCount, int sceneCount,
            MoonPalaceGrayboxWriteScopeSummary writeScopeSummary, string sceneManifestDigest, string failureReason)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Generation = generation;
            SceneWritten = sceneWritten;
            SceneDeletedBeforeWrite = sceneDeletedBeforeWrite;
            SceneRecreated = sceneRecreated;
            CsvCount = csvCount;
            JsonCount = jsonCount;
            SceneCount = sceneCount;
            WriteScopeSummary = writeScopeSummary ?? new MoonPalaceGrayboxWriteScopeSummary(
                Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
            SceneManifestDigest = sceneManifestDigest ?? string.Empty;
            FailureReason = failureReason ?? string.Empty;
        }

        public MoonPalaceGrayboxRunRequest Request { get; }
        public MoonPalaceGrayboxRunMode Mode => Request.Mode;
        public bool Success => Generation != null && string.IsNullOrEmpty(FailureReason) && Generation.Validation.Passed;
        public bool SceneWritten { get; }
        public bool SceneDeletedBeforeWrite { get; }
        public bool SceneRecreated { get; }
        public string CatalogDigest => Generation == null ? string.Empty : Generation.Catalog.CanonicalDigest;
        public string CandidateDigest => Generation == null ? string.Empty : Generation.CandidateSet.CanonicalDigest;
        public string LogicalMapDigest => Generation == null ? string.Empty : Generation.LogicalMapDigest;
        public string SceneManifestDigest { get; }
        public int CsvCount { get; }
        public int JsonCount { get; }
        public int SceneCount { get; }
        public MoonPalaceGrayboxValidationSummary ValidationSummary => Generation == null ? null : Generation.Validation;
        public MoonPalaceGrayboxWriteScopeSummary WriteScopeSummary { get; }
        public string NonExecutionSummary =>
            "full_world=0;live_traversal=0;production_art=0;npc_combat_shop_save=0;player_build=0";
        public string FailureReason { get; }
        public MoonPalaceOneSectorGrayboxResult Generation { get; }

        public string ToHistoryRow(int sequence)
        {
            var validation = ValidationSummary;
            return string.Join(",", new[]
            {
                sequence.ToString(CultureInfo.InvariantCulture),
                Mode.ToString(),
                Success ? "TRUE" : "FALSE",
                Request.RequestDigest,
                CatalogDigest,
                CandidateDigest,
                LogicalMapDigest,
                SceneManifestDigest,
                SceneWritten ? "TRUE" : "FALSE",
                SceneRecreated ? "TRUE" : "FALSE",
                (validation == null ? -1 : validation.RouteFailureCount).ToString(CultureInfo.InvariantCulture),
                (validation == null ? -1 : validation.RecoveryFailureCount).ToString(CultureInfo.InvariantCulture),
                (validation == null ? -1 : validation.SeamFailureCount).ToString(CultureInfo.InvariantCulture),
                (validation == null ? -1 : validation.UnreachableMicroChunkCount).ToString(CultureInfo.InvariantCulture),
            });
        }
    }
}
