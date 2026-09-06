using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceTuningPreconditions
    {
        public const string TaskId = "MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING";
        public const string StrictMap2109ResultDigest = "d89be7ffda394341c96eb59196d01c1ec10802c62b82a4bbbc73223e682bf44c";
        public const string StrictMap2109TaskDigest = "597bae41ef9bc8a1d1080a340186f3d10b0306b9d90aac184bc6657877e44b22";
        public const string StrictMap2110HandoffDigest = "ce5e4ebc5e13ad55671a8046ad7641218efbedf9c6b1dff8df9f4cd20110de04";
        public const string SourceMap2101Digest = "631000d63f0f2de9c829e662dbb9ab05f3ad4ad5ada542307058515569b3a735";
        public const string SourceMap2102Digest = "cc91a4555da30aaaca6d0bb7a1821d397f59300d4cfeec94587c70db59e1415f";
        public const string SourceMap2103Digest = "041b104ebc5755a7ad4b1b67f4726dc88ad77ec103b1b86d70c1453b25826831";
        public const string SourceMap2104Digest = "d30baf04fcd6eceeabd7b6f467b24ce11ce7c5f44833887bf7b0457e6fbdda72";
        public const string SourceMap2105Digest = "645f95b8c58757179223b8e5b219b60b5a732cb898a69f99594178c76f166a1e";
        public const string SourceMap2106Digest = "955ad4acb3e2294b3dc7c018c21472bf4ef34bbfc4075685324901179bd652ab";
        public const string SourceMap2107Digest = "0e03c7cb4780b078822297e9877d345480413a2b3b8d9ed0c3d1d455645a4a93";
        public const string SourceMap2108Digest = "9308092afbee03a8c08d5a5fbc2b4eb4a02fc747e5bbc6bd9afe7ce5ec018a73";
        public const string SourceMap2109Digest = "cca0bf5765f2f80c699c658e81d72d73bf5ce5d6b8bd5276db328cb7e1888469";
    }

    public sealed class MoonPalaceDensityWindow
    {
        public MoonPalaceDensityWindow(string windowId, string scope, double minimum,
            double target, double maximum, string meaning, string excludedPopulation)
        {
            WindowId = T.Required(windowId); Scope = T.Required(scope);
            if (minimum < 0 || minimum > target || target > maximum || maximum > 1)
                throw new ArgumentOutOfRangeException(nameof(target), "Density window must satisfy 0 <= min <= target <= max <= 1.");
            Minimum = minimum; Target = target; Maximum = maximum;
            Meaning = T.Required(meaning); ExcludedPopulation = T.Required(excludedPopulation);
            CanonicalDigest = T.Hash("MAP21_10_DENSITY_WINDOW_V1", CanonicalLine);
        }
        public string WindowId { get; } public string Scope { get; }
        public double Minimum { get; } public double Target { get; } public double Maximum { get; }
        public string Meaning { get; } public string ExcludedPopulation { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => T.Join(WindowId, Scope, T.N(Minimum), T.N(Target),
            T.N(Maximum), Meaning, ExcludedPopulation);
    }

    public sealed class MoonPalaceTuningTarget
    {
        public MoonPalaceTuningTarget(string targetKind, string targetId, double quiet,
            double cluster, double activity, double overlay, string pacingNote,
            bool changesSolverBehavior = false)
        {
            TargetKind = T.Required(targetKind); TargetId = T.Required(targetId);
            QuietTarget = T.Ratio(quiet); ClusterTarget = T.Ratio(cluster);
            ActivityTarget = T.Ratio(activity); OverlayTarget = T.Ratio(overlay);
            PacingNote = T.Required(pacingNote); ChangesSolverBehavior = changesSolverBehavior;
            CanonicalDigest = T.Hash("MAP21_10_TUNING_TARGET_V1", CanonicalLine);
        }
        public string TargetKind { get; } public string TargetId { get; }
        public double QuietTarget { get; } public double ClusterTarget { get; }
        public double ActivityTarget { get; } public double OverlayTarget { get; }
        public string PacingNote { get; } public bool ChangesSolverBehavior { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => T.Join(TargetKind, TargetId, T.N(QuietTarget),
            T.N(ClusterTarget), T.N(ActivityTarget), T.N(OverlayTarget), PacingNote,
            T.B(ChangesSolverBehavior));
    }

    public sealed class MoonPalaceRepetitionDistanceRule
    {
        public MoonPalaceRepetitionDistanceRule(string ruleId, string subject,
            int minimumSeparation, string distanceUnit, string enforcementScope,
            string identityField, bool materialColorAudioCountsAsStructural,
            bool mirrorVariantCountsInFamily)
        {
            RuleId = T.Required(ruleId); Subject = T.Required(subject);
            if (minimumSeparation < 1) throw new ArgumentOutOfRangeException(nameof(minimumSeparation));
            MinimumSeparation = minimumSeparation; DistanceUnit = T.Required(distanceUnit);
            EnforcementScope = T.Required(enforcementScope); IdentityField = T.Required(identityField);
            MaterialColorAudioCountsAsStructural = materialColorAudioCountsAsStructural;
            MirrorVariantCountsInFamily = mirrorVariantCountsInFamily;
            CanonicalDigest = T.Hash("MAP21_10_REPETITION_RULE_V1", CanonicalLine);
        }
        public string RuleId { get; } public string Subject { get; }
        public int MinimumSeparation { get; } public string DistanceUnit { get; }
        public string EnforcementScope { get; } public string IdentityField { get; }
        public bool MaterialColorAudioCountsAsStructural { get; }
        public bool MirrorVariantCountsInFamily { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => T.Join(RuleId, Subject, T.N(MinimumSeparation),
            DistanceUnit, EnforcementScope, IdentityField,
            T.B(MaterialColorAudioCountsAsStructural), T.B(MirrorVariantCountsInFamily));
    }

    public sealed class MoonPalaceTuningSourceInventory
    {
        public MoonPalaceTuningSourceInventory(string taskId, string resultPath,
            string observedResultDigest, string semanticManifestPath, string semanticDigest,
            string primaryRecordKind, int primaryRecordCount, string secondaryRecordKind,
            int secondaryRecordCount)
        {
            TaskId = T.Required(taskId); ResultPath = T.Required(resultPath);
            ObservedResultDigest = T.Digest(observedResultDigest, nameof(observedResultDigest));
            SemanticManifestPath = T.Required(semanticManifestPath);
            SemanticDigest = T.Digest(semanticDigest, nameof(semanticDigest));
            PrimaryRecordKind = T.Required(primaryRecordKind);
            if (primaryRecordCount < 1 || secondaryRecordCount < 0)
                throw new ArgumentOutOfRangeException(nameof(primaryRecordCount));
            PrimaryRecordCount = primaryRecordCount;
            SecondaryRecordKind = string.IsNullOrWhiteSpace(secondaryRecordKind) ? "NONE" : secondaryRecordKind.Trim();
            SecondaryRecordCount = secondaryRecordCount;
            CanonicalDigest = T.Hash("MAP21_10_SOURCE_INVENTORY_V1", CanonicalLine);
        }
        public string TaskId { get; } public string ResultPath { get; }
        public string ObservedResultDigest { get; } public string SemanticManifestPath { get; }
        public string SemanticDigest { get; } public string PrimaryRecordKind { get; }
        public int PrimaryRecordCount { get; } public string SecondaryRecordKind { get; }
        public int SecondaryRecordCount { get; } public bool ReadOnlySource => true;
        public bool Regenerated => false; public string CanonicalDigest { get; }
        public string CanonicalLine => T.Join(TaskId, ResultPath, ObservedResultDigest,
            SemanticManifestPath, SemanticDigest, PrimaryRecordKind, T.N(PrimaryRecordCount),
            SecondaryRecordKind, T.N(SecondaryRecordCount), "read_only=true", "regenerated=false");
    }

    public sealed class MoonPalaceTuningProfile
    {
        public const string SchemaVersion = "map21_10.moonpalace_tuning.v1";
        public static readonly string[] RequiredWindowIds = { "DENSITY_QUIET_RATIO", "DENSITY_CLUSTER_RATIO", "DENSITY_ACTIVITY_RATIO", "DENSITY_OVERLAY_RATIO" };
        public static readonly string[] RequiredBiomeIds = { "MoonCrater", "CassiaRoot", "AbandonedMill", "MoonDough" };
        public static readonly string[] RequiredRoleIds = { "StartBuffer", "MainRoute", "BranchRoute", "RecoveryRoute", "QuietBuffer", "SpecialApproach", "BoundarySeam" };
        public static readonly string[] RequiredRepetitionRuleIds = { "REPEAT_PATTERN_EXACT_ID", "REPEAT_PATTERN_MIRROR_FAMILY", "REPEAT_CLUSTER_EXACT_ID", "REPEAT_CLUSTER_STRUCTURAL_SIGNATURE", "REPEAT_CLUSTER_SILHOUETTE_SIGNATURE", "REPEAT_ACTIVITY_EXACT_ID", "REPEAT_EVENT_NON_EMPTY_ID", "REPEAT_BOUNDARY_CANDIDATE_ID" };

        private readonly ReadOnlyCollection<MoonPalaceDensityWindow> windows;
        private readonly ReadOnlyCollection<MoonPalaceTuningTarget> biomes;
        private readonly ReadOnlyCollection<MoonPalaceTuningTarget> roles;
        private readonly ReadOnlyCollection<MoonPalaceRepetitionDistanceRule> repetitionRules;
        private readonly ReadOnlyCollection<MoonPalaceTuningSourceInventory> sources;

        public MoonPalaceTuningProfile(IEnumerable<MoonPalaceDensityWindow> densityWindows,
            IEnumerable<MoonPalaceTuningTarget> biomeTargets,
            IEnumerable<MoonPalaceTuningTarget> pacingRoleTargets,
            IEnumerable<MoonPalaceRepetitionDistanceRule> repetitionDistanceRules,
            IEnumerable<MoonPalaceTuningSourceInventory> sourceInventory, string createdUtc)
        {
            windows = T.Order(densityWindows, x => x.WindowId); biomes = T.Order(biomeTargets, x => x.TargetId);
            roles = T.Order(pacingRoleTargets, x => x.TargetId); repetitionRules = T.Order(repetitionDistanceRules, x => x.RuleId);
            sources = T.Order(sourceInventory, x => x.TaskId); CreatedUtc = createdUtc ?? string.Empty;
            Validate();
            WindowSetDigest = SetDigest("MAP21_10_WINDOW_SET_V1", windows.Select(x => x.CanonicalLine));
            BiomeSetDigest = SetDigest("MAP21_10_BIOME_SET_V1", biomes.Select(x => x.CanonicalLine));
            PacingRoleSetDigest = SetDigest("MAP21_10_ROLE_SET_V1", roles.Select(x => x.CanonicalLine));
            RepetitionPolicyDigest = SetDigest("MAP21_10_REPETITION_SET_V1", repetitionRules.Select(x => x.CanonicalLine));
            SourceInventoryDigest = SetDigest("MAP21_10_SOURCE_SET_V1", sources.Select(x => x.CanonicalLine));
            TuningProfileManifestDigest = T.Hash("MAP21_10_TUNING_PROFILE_MANIFEST_V1", WindowSetDigest,
                BiomeSetDigest, PacingRoleSetDigest, SourceInventoryDigest, "seed_output_count=0", "runtime_state_count=0");
            DensityPacingManifestDigest = T.Hash("MAP21_10_DENSITY_PACING_MANIFEST_V1", WindowSetDigest,
                BiomeSetDigest, PacingRoleSetDigest, "special_region_reserved_cells_excluded=true",
                "boundary_cells_excluded_from_activity=true", "solver_behavior_changes=0");
            RepetitionPolicyManifestDigest = T.Hash("MAP21_10_REPETITION_POLICY_MANIFEST_V1",
                RepetitionPolicyDigest, "structural_identity_field=cluster_structural_signature",
                "silhouette_identity_field=cluster_silhouette_signature", "material_color_audio_structural=false");
            Map2111HandoffDigest = T.Hash("MAP21_11_TUNING_HANDOFF_V1",
                MoonPalaceTuningPreconditions.StrictMap2110HandoffDigest, TuningProfileManifestDigest,
                DensityPacingManifestDigest, RepetitionPolicyManifestDigest, SourceInventoryDigest,
                "focused_MAP21_10_pass_required=true", "seed_locked=false", "playtest_executed=false");
        }

        public IReadOnlyList<MoonPalaceDensityWindow> DensityWindows => windows;
        public IReadOnlyList<MoonPalaceTuningTarget> BiomeTargets => biomes;
        public IReadOnlyList<MoonPalaceTuningTarget> PacingRoleTargets => roles;
        public IReadOnlyList<MoonPalaceRepetitionDistanceRule> RepetitionDistanceRules => repetitionRules;
        public IReadOnlyList<MoonPalaceTuningSourceInventory> SourceInventory => sources;
        public string CreatedUtc { get; } public string WindowSetDigest { get; }
        public string BiomeSetDigest { get; } public string PacingRoleSetDigest { get; }
        public string RepetitionPolicyDigest { get; } public string SourceInventoryDigest { get; }
        public string TuningProfileManifestDigest { get; } public string DensityPacingManifestDigest { get; }
        public string RepetitionPolicyManifestDigest { get; } public string Map2111HandoffDigest { get; }
        public bool CanGenerateOrApproveSeed => false; public bool CanExecuteRuntimeSideEffects => false;
        public bool SpecialRegionReservedCellsExcludedFromQuietCluster => true;
        public bool BoundaryCellsExcludedFromActivity => true;
        public void RequestGenerationOrSeedApproval() => throw new InvalidOperationException("MAP21_10 is static tuning policy only.");
        public void RequestRuntimeExecution() => throw new InvalidOperationException("MAP21_10 cannot execute runtime behavior.");

        public string SerializeDensityWindowsCsv() => Csv("window_id,scope,minimum,target,maximum,meaning,excluded_population,canonical_digest",
            windows.Select(x => Row(x.WindowId, x.Scope, T.N(x.Minimum), T.N(x.Target), T.N(x.Maximum), x.Meaning, x.ExcludedPopulation, x.CanonicalDigest)));
        public string SerializeBiomeTargetsCsv() => TargetCsv(biomes);
        public string SerializePacingRoleTargetsCsv() => TargetCsv(roles);
        public string SerializeRepetitionRulesCsv() => Csv("rule_id,subject,minimum_separation,distance_unit,enforcement_scope,identity_field,material_color_audio_counts_as_structural,mirror_variant_counts_in_family,canonical_digest",
            repetitionRules.Select(x => Row(x.RuleId, x.Subject, T.N(x.MinimumSeparation), x.DistanceUnit, x.EnforcementScope, x.IdentityField, T.B(x.MaterialColorAudioCountsAsStructural), T.B(x.MirrorVariantCountsInFamily), x.CanonicalDigest)));
        public string SerializeSourceInventoryCsv() => Csv("task_id,result_path,observed_result_sha256,semantic_manifest_path,semantic_digest,primary_record_kind,primary_record_count,secondary_record_kind,secondary_record_count,read_only_source,regenerated,canonical_digest",
            sources.Select(x => Row(x.TaskId, x.ResultPath, x.ObservedResultDigest, x.SemanticManifestPath, x.SemanticDigest, x.PrimaryRecordKind, T.N(x.PrimaryRecordCount), x.SecondaryRecordKind, T.N(x.SecondaryRecordCount), "true", "false", x.CanonicalDigest)));
        public string SerializeHandoffCsv() => Csv("handoff_target,source_task,focused_pass_required,global_window_count,biome_target_count,pacing_role_count,repetition_rule_count,source_inventory_count,seed_locked,completion_playtest_executed,handoff_digest",
            new[] { Row("MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS", MoonPalaceTuningPreconditions.TaskId, "true", "4", "4", "7", "8", "9", "false", "false", Map2111HandoffDigest) });
        public string SerializeTuningProfileManifest() => MoonPalaceCanonical.ToJson(MoonPalaceTuningProfileDocument.From(this));
        public string SerializeDensityPacingManifest() => MoonPalaceCanonical.ToJson(MoonPalaceDensityPacingDocument.From(this));
        public string SerializeRepetitionPolicyManifest() => MoonPalaceCanonical.ToJson(MoonPalaceRepetitionPolicyDocument.From(this));

        private string TargetCsv(IEnumerable<MoonPalaceTuningTarget> values) => Csv("target_kind,target_id,quiet_target,cluster_target,activity_target,overlay_target,pacing_note,changes_solver_behavior,canonical_digest",
            values.Select(x => Row(x.TargetKind, x.TargetId, T.N(x.QuietTarget), T.N(x.ClusterTarget), T.N(x.ActivityTarget), T.N(x.OverlayTarget), x.PacingNote, T.B(x.ChangesSolverBehavior), x.CanonicalDigest)));
        private void Validate()
        {
            Exact(windows.Select(x => x.WindowId), RequiredWindowIds, "density window");
            Exact(biomes.Select(x => x.TargetId), RequiredBiomeIds, "biome");
            Exact(roles.Select(x => x.TargetId), RequiredRoleIds, "pacing role");
            Exact(repetitionRules.Select(x => x.RuleId), RequiredRepetitionRuleIds, "repetition rule");
            if (sources.Count != 9 || sources.Select(x => x.TaskId).Distinct(StringComparer.Ordinal).Count() != 9)
                throw new ArgumentException("Exactly nine unique MAP21 source inventory rows required.");
            if (biomes.Any(x => x.TargetKind != "Biome") || roles.Any(x => x.TargetKind != "PacingRole") ||
                biomes.Concat(roles).Any(x => x.ChangesSolverBehavior))
                throw new ArgumentException("Static target kind or solver behavior mismatch.");
            var byWindow = windows.ToDictionary(x => x.WindowId, StringComparer.Ordinal);
            foreach (var target in biomes.Concat(roles))
            {
                InWindow(target.QuietTarget, byWindow["DENSITY_QUIET_RATIO"]);
                InWindow(target.ClusterTarget, byWindow["DENSITY_CLUSTER_RATIO"]);
                InWindow(target.ActivityTarget, byWindow["DENSITY_ACTIVITY_RATIO"]);
                InWindow(target.OverlayTarget, byWindow["DENSITY_OVERLAY_RATIO"]);
            }
            var structural = repetitionRules.Single(x => x.RuleId == "REPEAT_CLUSTER_STRUCTURAL_SIGNATURE");
            var silhouette = repetitionRules.Single(x => x.RuleId == "REPEAT_CLUSTER_SILHOUETTE_SIGNATURE");
            if (structural.IdentityField == silhouette.IdentityField || structural.MaterialColorAudioCountsAsStructural ||
                !repetitionRules.Single(x => x.RuleId == "REPEAT_PATTERN_MIRROR_FAMILY").MirrorVariantCountsInFamily)
                throw new ArgumentException("Structural/silhouette/mirror identity policy mismatch.");
            if (sources.Any(x => !x.ReadOnlySource || x.Regenerated))
                throw new ArgumentException("Source inventory must remain read-only and non-regenerated.");
        }
        private static void Exact(IEnumerable<string> actual, IEnumerable<string> expected, string label)
        {
            if (!actual.OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(expected.OrderBy(x => x, StringComparer.Ordinal)))
                throw new ArgumentException("Exact " + label + " inventory required.");
        }
        private static void InWindow(double value, MoonPalaceDensityWindow window)
        {
            if (value < window.Minimum || value > window.Maximum) throw new ArgumentOutOfRangeException(window.WindowId);
        }
        public static string SetDigest(string prefix, IEnumerable<string> lines) =>
            T.Hash(new[] { prefix }.Concat(lines.OrderBy(x => x, StringComparer.Ordinal)).ToArray());
        private static string Csv(string header, IEnumerable<string> rows) => string.Join("\n", new[] { header }.Concat(rows)) + "\n";
        private static string Row(params string[] values) => string.Join(",", values.Select(T.Escape));
    }

    public sealed class MoonPalaceTuningDigestManifest
    {
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> observed;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> semantics;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> csv;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> json;
        public MoonPalaceTuningDigestManifest(MoonPalaceTuningProfile profile,
            IEnumerable<MoonPalaceNamedDigest> observedResults, IEnumerable<MoonPalaceNamedDigest> semanticSources,
            IEnumerable<MoonPalaceNamedDigest> csvDigests, IEnumerable<MoonPalaceNamedDigest> jsonDigests, string createdUtc)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            observed = Copy(observedResults, 9, "observed Result"); semantics = Copy(semanticSources, 9, "semantic source");
            csv = Copy(csvDigests, 6, "CSV"); json = Copy(jsonDigests, 3, "non-digest JSON"); CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = T.Hash(new[] { "map21_10.tuning_digest_manifest.v1", MoonPalaceTuningPreconditions.TaskId,
                MoonPalaceTuningPreconditions.StrictMap2109ResultDigest, MoonPalaceTuningPreconditions.StrictMap2109TaskDigest,
                MoonPalaceTuningPreconditions.StrictMap2110HandoffDigest, profile.Map2111HandoffDigest,
                "created_utc_excluded=true" }.Concat(observed.Select(x => x.CanonicalLine)).Concat(semantics.Select(x => x.CanonicalLine))
                .Concat(csv.Select(x => x.CanonicalLine)).Concat(json.Select(x => x.CanonicalLine)).ToArray());
        }
        public MoonPalaceTuningProfile Profile { get; }
        public IReadOnlyList<MoonPalaceNamedDigest> ObservedSourceResultDigests => observed;
        public IReadOnlyList<MoonPalaceNamedDigest> SemanticSourceDigests => semantics;
        public IReadOnlyList<MoonPalaceNamedDigest> CsvDigests => csv;
        public IReadOnlyList<MoonPalaceNamedDigest> JsonDigests => json;
        public string CreatedUtc { get; } public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(MoonPalaceTuningDigestDocument.From(this));
        private static ReadOnlyCollection<MoonPalaceNamedDigest> Copy(IEnumerable<MoonPalaceNamedDigest> source, int expected, string label)
        {
            var values = (source ?? throw new ArgumentNullException(nameof(source))).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            if (values.Length != expected || values.Any(x => x == null) || values.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != expected)
                throw new ArgumentException("Exact " + label + " digest inventory required.");
            return new ReadOnlyCollection<MoonPalaceNamedDigest>(values);
        }
    }

    public sealed class MoonPalaceTuningForbiddenOperationCounters
    {
        public static MoonPalaceTuningForbiddenOperationCounters Zero => new MoonPalaceTuningForbiddenOperationCounters();
        public int WorldSectorGenerationRuns => 0; public int WorldSectorPlacementRuns => 0;
        public int SeedLockOrApprovalRuns => 0; public int CompletionPlaytestRuns => 0;
        public int RendererRuns => 0; public int ValidationRunnerRuns => 0; public int ReplayRuns => 0;
        public int RollbackRuns => 0; public int TilemapWrites => 0; public int RuntimeObjectChanges => 0;
        public int ScenePrefabChanges => 0; public int ColliderAddressablesChanges => 0; public int UpstreamRewrites => 0;
        public int PriorCategoryRuns => 0; public int PlayModeRuns => 0; public int Legacy19347Runs => 0;
        public int UnfilteredOrFullRegressionRuns => 0; public int ExternalProcessLaunches => 0;
        public bool AllZero => WorldSectorGenerationRuns + WorldSectorPlacementRuns + SeedLockOrApprovalRuns +
            CompletionPlaytestRuns + RendererRuns + ValidationRunnerRuns + ReplayRuns + RollbackRuns + TilemapWrites +
            RuntimeObjectChanges + ScenePrefabChanges + ColliderAddressablesChanges + UpstreamRewrites + PriorCategoryRuns +
            PlayModeRuns + Legacy19347Runs + UnfilteredOrFullRegressionRuns + ExternalProcessLaunches == 0;
    }

    internal static class T
    {
        public static string Required(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required value."); return value.Trim(); }
        public static double Ratio(double value) { if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); return value; }
        public static string Digest(string value, string name) { if (!BakingCanonicalDigest.IsLowerHexSha256(value)) throw new ArgumentException("Lower-hex SHA-256 required.", name); return value; }
        public static string N(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);
        public static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
        public static string B(bool value) => value ? "true" : "false";
        public static string Join(params string[] values) => MoonPalaceCanonical.Join(values);
        public static string Hash(params string[] values) => BakingCanonicalDigest.HashCanonicalLines(values);
        public static string Escape(string value) { var text = value ?? string.Empty; return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? text : "\"" + text.Replace("\"", "\"\"") + "\""; }
        public static ReadOnlyCollection<TValue> Order<TValue>(IEnumerable<TValue> source, Func<TValue, string> key) { var values = (source ?? throw new ArgumentNullException(nameof(source))).ToArray(); if (values.Any(x => x == null)) throw new ArgumentException("Null record."); return new ReadOnlyCollection<TValue>(values.OrderBy(key, StringComparer.Ordinal).ToArray()); }
    }

    [Serializable] internal sealed class MoonPalaceTuningProfileDocument
    {
        public string schema_version; public string publication_kind; public MoonPalaceDensityWindowDocument[] density_windows;
        public MoonPalaceTuningTargetDocument[] biome_targets; public MoonPalaceTuningTargetDocument[] pacing_role_targets;
        public MoonPalaceTuningSourceDocument[] source_inventory; public int seed_output_count; public int runtime_state_count;
        public string window_set_digest; public string biome_set_digest; public string pacing_role_set_digest;
        public string source_inventory_digest; public string created_utc; public bool created_utc_excluded_from_canonical_digest;
        public string MAP21_11_handoff_digest; public string canonical_digest;
        public static MoonPalaceTuningProfileDocument From(MoonPalaceTuningProfile x) => new MoonPalaceTuningProfileDocument
        { schema_version = MoonPalaceTuningProfile.SchemaVersion, publication_kind = "StaticTuningPolicyNotGeneratedWorld",
            density_windows = x.DensityWindows.Select(MoonPalaceDensityWindowDocument.From).ToArray(), biome_targets = x.BiomeTargets.Select(MoonPalaceTuningTargetDocument.From).ToArray(),
            pacing_role_targets = x.PacingRoleTargets.Select(MoonPalaceTuningTargetDocument.From).ToArray(), source_inventory = x.SourceInventory.Select(MoonPalaceTuningSourceDocument.From).ToArray(),
            seed_output_count = 0, runtime_state_count = 0, window_set_digest = x.WindowSetDigest, biome_set_digest = x.BiomeSetDigest,
            pacing_role_set_digest = x.PacingRoleSetDigest, source_inventory_digest = x.SourceInventoryDigest, created_utc = x.CreatedUtc,
            created_utc_excluded_from_canonical_digest = true, MAP21_11_handoff_digest = x.Map2111HandoffDigest, canonical_digest = x.TuningProfileManifestDigest };
    }
    [Serializable] internal sealed class MoonPalaceDensityPacingDocument
    {
        public string schema_version; public MoonPalaceDensityWindowDocument[] density_windows; public MoonPalaceTuningTargetDocument[] biome_targets;
        public MoonPalaceTuningTargetDocument[] pacing_role_targets; public bool special_region_reserved_cells_excluded_from_quiet_cluster;
        public bool boundary_cells_excluded_from_activity; public int solver_behavior_change_count; public int generated_coordinate_count;
        public string window_set_digest; public string biome_set_digest; public string pacing_role_set_digest; public string canonical_digest;
        public static MoonPalaceDensityPacingDocument From(MoonPalaceTuningProfile x) => new MoonPalaceDensityPacingDocument
        { schema_version = "map21_10.density_pacing_manifest.v1", density_windows = x.DensityWindows.Select(MoonPalaceDensityWindowDocument.From).ToArray(),
            biome_targets = x.BiomeTargets.Select(MoonPalaceTuningTargetDocument.From).ToArray(), pacing_role_targets = x.PacingRoleTargets.Select(MoonPalaceTuningTargetDocument.From).ToArray(),
            special_region_reserved_cells_excluded_from_quiet_cluster = true, boundary_cells_excluded_from_activity = true,
            solver_behavior_change_count = 0, generated_coordinate_count = 0, window_set_digest = x.WindowSetDigest,
            biome_set_digest = x.BiomeSetDigest, pacing_role_set_digest = x.PacingRoleSetDigest, canonical_digest = x.DensityPacingManifestDigest };
    }
    [Serializable] internal sealed class MoonPalaceRepetitionPolicyDocument
    {
        public string schema_version; public MoonPalaceRepetitionRuleDocument[] repetition_rules; public string structural_identity_field;
        public string silhouette_identity_field; public bool material_color_audio_counts_as_structural; public bool mirror_variants_count_in_family;
        public int landmark_uniqueness_rewrite_count; public string repetition_policy_digest; public string canonical_digest;
        public static MoonPalaceRepetitionPolicyDocument From(MoonPalaceTuningProfile x) => new MoonPalaceRepetitionPolicyDocument
        { schema_version = "map21_10.repetition_policy_manifest.v1", repetition_rules = x.RepetitionDistanceRules.Select(MoonPalaceRepetitionRuleDocument.From).ToArray(),
            structural_identity_field = "cluster_structural_signature", silhouette_identity_field = "cluster_silhouette_signature",
            material_color_audio_counts_as_structural = false, mirror_variants_count_in_family = true, landmark_uniqueness_rewrite_count = 0,
            repetition_policy_digest = x.RepetitionPolicyDigest, canonical_digest = x.RepetitionPolicyManifestDigest };
    }
    [Serializable] internal sealed class MoonPalaceTuningDigestDocument
    {
        public string schema_version; public string task_id; public string strict_MAP21_09_result_sha256; public string strict_MAP21_09_task_sha256;
        public string strict_MAP21_10_handoff_digest; public MoonPalaceNamedDigestDocument[] observed_source_result_digests;
        public MoonPalaceNamedDigestDocument[] semantic_source_digests; public MoonPalaceNamedDigestDocument[] csv_digests;
        public MoonPalaceNamedDigestDocument[] json_digests; public string MAP21_11_handoff_digest; public string created_utc;
        public bool created_utc_excluded_from_canonical_digest; public string canonical_digest;
        public static MoonPalaceTuningDigestDocument From(MoonPalaceTuningDigestManifest x) => new MoonPalaceTuningDigestDocument
        { schema_version = "map21_10.tuning_digest_manifest.v1", task_id = MoonPalaceTuningPreconditions.TaskId,
            strict_MAP21_09_result_sha256 = MoonPalaceTuningPreconditions.StrictMap2109ResultDigest, strict_MAP21_09_task_sha256 = MoonPalaceTuningPreconditions.StrictMap2109TaskDigest,
            strict_MAP21_10_handoff_digest = MoonPalaceTuningPreconditions.StrictMap2110HandoffDigest,
            observed_source_result_digests = x.ObservedSourceResultDigests.Select(MoonPalaceNamedDigestDocument.From).ToArray(),
            semantic_source_digests = x.SemanticSourceDigests.Select(MoonPalaceNamedDigestDocument.From).ToArray(), csv_digests = x.CsvDigests.Select(MoonPalaceNamedDigestDocument.From).ToArray(),
            json_digests = x.JsonDigests.Select(MoonPalaceNamedDigestDocument.From).ToArray(), MAP21_11_handoff_digest = x.Profile.Map2111HandoffDigest,
            created_utc = x.CreatedUtc, created_utc_excluded_from_canonical_digest = true, canonical_digest = x.CanonicalDigest };
    }
    [Serializable] internal sealed class MoonPalaceDensityWindowDocument
    {
        public string window_id; public string scope; public double minimum; public double target; public double maximum;
        public string meaning; public string excluded_population; public string canonical_digest;
        public static MoonPalaceDensityWindowDocument From(MoonPalaceDensityWindow x) => new MoonPalaceDensityWindowDocument
        { window_id = x.WindowId, scope = x.Scope, minimum = x.Minimum, target = x.Target, maximum = x.Maximum,
            meaning = x.Meaning, excluded_population = x.ExcludedPopulation, canonical_digest = x.CanonicalDigest };
    }
    [Serializable] internal sealed class MoonPalaceTuningTargetDocument
    {
        public string target_kind; public string target_id; public double quiet_target; public double cluster_target;
        public double activity_target; public double overlay_target; public string pacing_note; public bool changes_solver_behavior; public string canonical_digest;
        public static MoonPalaceTuningTargetDocument From(MoonPalaceTuningTarget x) => new MoonPalaceTuningTargetDocument
        { target_kind = x.TargetKind, target_id = x.TargetId, quiet_target = x.QuietTarget, cluster_target = x.ClusterTarget,
            activity_target = x.ActivityTarget, overlay_target = x.OverlayTarget, pacing_note = x.PacingNote,
            changes_solver_behavior = x.ChangesSolverBehavior, canonical_digest = x.CanonicalDigest };
    }
    [Serializable] internal sealed class MoonPalaceRepetitionRuleDocument
    {
        public string rule_id; public string subject; public int minimum_separation; public string distance_unit;
        public string enforcement_scope; public string identity_field; public bool material_color_audio_counts_as_structural;
        public bool mirror_variant_counts_in_family; public string canonical_digest;
        public static MoonPalaceRepetitionRuleDocument From(MoonPalaceRepetitionDistanceRule x) => new MoonPalaceRepetitionRuleDocument
        { rule_id = x.RuleId, subject = x.Subject, minimum_separation = x.MinimumSeparation, distance_unit = x.DistanceUnit,
            enforcement_scope = x.EnforcementScope, identity_field = x.IdentityField, material_color_audio_counts_as_structural = x.MaterialColorAudioCountsAsStructural,
            mirror_variant_counts_in_family = x.MirrorVariantCountsInFamily, canonical_digest = x.CanonicalDigest };
    }
    [Serializable] internal sealed class MoonPalaceTuningSourceDocument
    {
        public string task_id; public string result_path; public string observed_result_sha256; public string semantic_manifest_path;
        public string semantic_digest; public string primary_record_kind; public int primary_record_count; public string secondary_record_kind;
        public int secondary_record_count; public bool read_only_source; public bool regenerated; public string canonical_digest;
        public static MoonPalaceTuningSourceDocument From(MoonPalaceTuningSourceInventory x) => new MoonPalaceTuningSourceDocument
        { task_id = x.TaskId, result_path = x.ResultPath, observed_result_sha256 = x.ObservedResultDigest,
            semantic_manifest_path = x.SemanticManifestPath, semantic_digest = x.SemanticDigest, primary_record_kind = x.PrimaryRecordKind,
            primary_record_count = x.PrimaryRecordCount, secondary_record_kind = x.SecondaryRecordKind, secondary_record_count = x.SecondaryRecordCount,
            read_only_source = true, regenerated = false, canonical_digest = x.CanonicalDigest };
    }
}
