using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration
{
    public sealed class MoonPalaceRunCurationPublication
    {
        internal MoonPalaceRunCurationPublication(MoonPalaceCuratedRunSet curated, IDictionary<string, string> outputs, string sceneDigest, string digest)
        {
            Curated = curated; OutputContents = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(outputs, StringComparer.Ordinal)); SceneManifestDigest = sceneDigest ?? string.Empty; DigestManifestDigest = digest ?? string.Empty;
        }
        public MoonPalaceCuratedRunSet Curated { get; }
        public IReadOnlyDictionary<string, string> OutputContents { get; }
        public string SceneManifestDigest { get; }
        public string DigestManifestDigest { get; }
    }

    /// <summary>RUN06-only publisher: it visualizes retained accepted and rejected records without altering their generator data.</summary>
    public static class MoonPalaceCuratedRunGallerySceneBuilder
    {
        public const string SceneRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN06/MoonPalaceCuratedRunGallery_RUN06.unity";
        public const string SceneDirectoryRelativePath = "Assets/_Game/Map/Scenes/MoonPalace/RUN06";
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN06";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/RUN06";
        public const string SceneRootName = "MoonPalace_CuratedRunGallery_RUN06";
        private static readonly string[] CsvNames = { "moonpalace_run06_curation_summary.csv", "moonpalace_run06_accepted_runs.csv", "moonpalace_run06_rejected_runs.csv", "moonpalace_run06_rule_findings.csv", "moonpalace_run06_recipe_tuning_delta.csv", "moonpalace_run06_gallery_layout.csv" };
        private static readonly string[] JsonNames = { "moonpalace_run06_quality_profile.json", "moonpalace_run06_curation_batch.json", "moonpalace_run06_accepted_runs.json", "moonpalace_run06_rejected_runs.json", "moonpalace_run06_rule_findings.json", "moonpalace_run06_recipe_tuning_delta.json", "moonpalace_run06_gallery_manifest.json", "moonpalace_run06_digest_manifest.json" };
        private static readonly TileDefinition[] TileDefinitions =
        {
            new TileDefinition("Open", new Color(0.12f, 0.18f, 0.25f, 1f)), new TileDefinition("Solid", new Color(0.045f, 0.055f, 0.075f, 1f)), new TileDefinition("Main", new Color(0.22f, 0.95f, 0.44f, 0.96f)), new TileDefinition("Branch", new Color(0.16f, 0.62f, 1f, 0.96f)), new TileDefinition("Split", new Color(1f, 0.77f, 0.20f, 0.96f)), new TileDefinition("Gate", new Color(1f, 0.30f, 0.78f, 1f)), new TileDefinition("AcceptedFrame", new Color(0.20f, 1f, 0.38f, 1f)), new TileDefinition("RejectedFrame", new Color(1f, 0.20f, 0.25f, 1f)), new TileDefinition("Warning", new Color(1f, 0.86f, 0.18f, 1f)), new TileDefinition("Reject", new Color(1f, 0.12f, 0.16f, 1f)),
        };

        public static MoonPalaceRunCurationPublication LastPublished { get; private set; }
        public static IReadOnlyList<string> GetExpectedOutputRelativePaths() => new ReadOnlyCollection<string>(CsvNames.Select(name => Join(AuthoringDirectoryRelativePath, name)).Concat(JsonNames.Select(name => Join(GeneratedDirectoryRelativePath, name))).OrderBy(path => path, StringComparer.Ordinal).ToList());

        public static MoonPalaceRunCurationPublication BuildSnapshot(string projectRoot, MoonPalaceCuratedRunSet curated)
        {
            if (string.IsNullOrWhiteSpace(projectRoot)) throw new ArgumentException("RUN06 requires the project root.", nameof(projectRoot));
            if (curated == null) throw new ArgumentNullException(nameof(curated));
            var sceneDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN06_SCENE", SceneRelativePath, SceneRootName, curated.CurationDigest, "accepted_visible=9", "rejected_visible=6" });
            var outputs = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                [Join(AuthoringDirectoryRelativePath, CsvNames[0])] = SummaryCsv(curated),
                [Join(AuthoringDirectoryRelativePath, CsvNames[1])] = RecordsCsv(curated.Accepted),
                [Join(AuthoringDirectoryRelativePath, CsvNames[2])] = RecordsCsv(curated.Rejected),
                [Join(AuthoringDirectoryRelativePath, CsvNames[3])] = FindingsCsv(curated),
                [Join(AuthoringDirectoryRelativePath, CsvNames[4])] = TuningCsv(curated.TuningDelta),
                [Join(AuthoringDirectoryRelativePath, CsvNames[5])] = LayoutCsv(curated),
                [Join(GeneratedDirectoryRelativePath, JsonNames[0])] = ProfileJson(),
                [Join(GeneratedDirectoryRelativePath, JsonNames[1])] = BatchJson(curated),
                [Join(GeneratedDirectoryRelativePath, JsonNames[2])] = RecordsJson("accepted", curated.Accepted),
                [Join(GeneratedDirectoryRelativePath, JsonNames[3])] = RecordsJson("rejected", curated.Rejected),
                [Join(GeneratedDirectoryRelativePath, JsonNames[4])] = FindingsJson(curated),
                [Join(GeneratedDirectoryRelativePath, JsonNames[5])] = TuningJson(curated.TuningDelta),
                [Join(GeneratedDirectoryRelativePath, JsonNames[6])] = GalleryManifestJson(curated, sceneDigest),
            };
            var digest = BakingCanonicalDigest.HashCanonicalLines(new[] { "RUN06_DIGEST_MANIFEST", sceneDigest }.Concat(outputs.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "|" + BakingCanonicalDigest.HashCanonicalText(pair.Value))));
            outputs.Add(Join(GeneratedDirectoryRelativePath, JsonNames[7]), DigestManifestJson(outputs, sceneDigest, digest));
            return new MoonPalaceRunCurationPublication(curated, outputs, sceneDigest, digest);
        }

        public static MoonPalaceRunCurationPublication Publish(string projectRoot, MoonPalaceCuratedRunSet curated)
        {
            var publication = BuildSnapshot(projectRoot, curated);
            Directory.CreateDirectory(Resolve(projectRoot, SceneDirectoryRelativePath)); Directory.CreateDirectory(Resolve(projectRoot, AuthoringDirectoryRelativePath)); Directory.CreateDirectory(Resolve(projectRoot, GeneratedDirectoryRelativePath));
            foreach (var output in publication.OutputContents) File.WriteAllText(Resolve(projectRoot, output.Key), FinalLf(output.Value), new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); CreateScene(publication); AssetDatabase.SaveAssets(); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); LastPublished = publication; return publication;
        }

        private static void CreateScene(MoonPalaceRunCurationPublication publication)
        {
            var tiles = CreateDebugTiles(); var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); var root = new GameObject(SceneRootName, typeof(Grid));
            var accepted = new GameObject("AcceptedGallery"); accepted.transform.SetParent(root.transform, false);
            var rejected = new GameObject("RejectedGallery"); rejected.transform.SetParent(root.transform, false);
            var qualityLegend = new GameObject("QualityLegend"); qualityLegend.transform.SetParent(root.transform, false);
            var overlay = new GameObject("RuleOverlay"); overlay.transform.SetParent(root.transform, false);
            var labels = new GameObject("SeedLabels"); labels.transform.SetParent(root.transform, false);
            var metadata = new GameObject("Metadata"); metadata.transform.SetParent(root.transform, false);
            var visibleAccepted = new[] { publication.Curated.Recommended }.Concat(publication.Curated.Accepted.Where(record => record.RecipeId != publication.Curated.Recommended.RecipeId || record.Seed != publication.Curated.Recommended.Seed)).Take(9).ToList();
            CreateGalleryEntries(accepted.transform, labels.transform, overlay.transform, visibleAccepted, true, tiles, 0f, publication.Curated.Recommended);
            CreateGalleryEntries(rejected.transform, labels.transform, overlay.transform, publication.Curated.Rejected.Take(6).ToList(), false, tiles, 116f, publication.Curated.Recommended);
            CreateText("Legend", qualityLegend.transform, new Vector3(0f, 17f, -3f), "RUN06 CURATION GALLERY\nGREEN frame = ACCEPTED | RED frame = REJECTED | YELLOW = warning | RED marker = reject hotspot\nMain = green | Branch = blue | Split/Rejoin = gold | Gate = magenta", Color.white, 0.38f, TextAnchor.UpperLeft);
            CreateText("Rules", qualityLegend.transform, new Vector3(0f, 8f, -3f), "Rules: " + string.Join(", ", MoonPalaceRunQualityRule.RequiredIds), new Color(0.78f, 0.86f, 1f, 1f), 0.22f, TextAnchor.UpperLeft);
            CreateText("MetadataText", metadata.transform, new Vector3(0f, -42f, -3f), "seeds=" + MoonPalaceRunCurationBatch.FirstSeed + ".." + MoonPalaceRunCurationBatch.LastSeed + " | recipes=" + publication.Curated.RecipeCount + " | generated=" + publication.Curated.GeneratedCount + " | accepted=" + publication.Curated.Accepted.Count + " | rejected=" + publication.Curated.Rejected.Count + "\nquality_profile=" + MoonPalaceRunQualityProfileCatalog.CanonicalDigest + "\ncuration=" + publication.Curated.CurationDigest + "\nRUN07 recommended active preview: " + publication.Curated.Recommended.RecipeId + " seed=" + publication.Curated.Recommended.Seed + " course=" + publication.Curated.Recommended.Result.CourseDigest, new Color(0.68f, 0.78f, 0.95f, 1f), 0.23f, TextAnchor.UpperLeft);
            EditorSceneManager.SaveScene(scene, SceneRelativePath);
        }

        private static void CreateGalleryEntries(Transform parent, Transform labels, Transform overlay, IReadOnlyList<MoonPalaceCuratedRunRecord> records, bool accepted, IDictionary<string, Tile> tiles, float xOffset, MoonPalaceCuratedRunRecord recommended)
        {
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index]; var column = index % 3; var row = index / 3; var origin = new Vector3(xOffset + column * 37f, -row * 14f, 0f);
                var entry = new GameObject((accepted ? "Accepted" : "Rejected") + "_" + (index + 1).ToString("00", CultureInfo.InvariantCulture) + "_" + record.RecipeId + "_" + record.Seed.ToString(CultureInfo.InvariantCulture)); entry.transform.SetParent(parent, false); entry.transform.localPosition = origin; entry.transform.localScale = new Vector3(0.30f, 0.30f, 1f);
                var terrain = CreateTilemap("GeneratedCourse", entry.transform, 0); var main = CreateTilemap("MainRoute", entry.transform, 1); var branch = CreateTilemap("BranchRoute", entry.transform, 2); var split = CreateTilemap("SplitRejoin", entry.transform, 3); var gates = CreateTilemap("ConnectorGates", entry.transform, 4); var frame = CreateTilemap("QualityFrame", entry.transform, 5);
                foreach (var placement in record.Result.Placements) terrain.SetTile(new Vector3Int(placement.PatternSlot.X, placement.PatternSlot.Y, 0), placement.Candidate.IsOpen(1, 1) ? tiles["Open"] : tiles["Solid"]);
                foreach (var edge in record.Result.Edges) { var map = edge.Ownership == "MAIN" ? main : edge.Ownership == "BRANCH" ? branch : split; var tile = edge.Ownership == "MAIN" ? tiles["Main"] : edge.Ownership == "BRANCH" ? tiles["Branch"] : tiles["Split"]; map.SetTile(new Vector3Int(edge.From.X, edge.From.Y, 0), tile); map.SetTile(new Vector3Int(edge.To.X, edge.To.Y, 0), tile); }
                foreach (var connector in record.Result.Connectors) { gates.SetTile(new Vector3Int(connector.FromPatternSlot.X, connector.FromPatternSlot.Y, 0), tiles["Gate"]); gates.SetTile(new Vector3Int(connector.ToPatternSlot.X, connector.ToPatternSlot.Y, 0), tiles["Gate"]); }
                foreach (var room in record.Result.Rooms) PaintPatternFrame(frame, room, accepted ? tiles["AcceptedFrame"] : tiles["RejectedFrame"]);
                var markerGroup = new GameObject("RuleMarker_" + (index + 1).ToString("00", CultureInfo.InvariantCulture)); markerGroup.transform.SetParent(overlay, false); markerGroup.transform.localPosition = origin; markerGroup.transform.localScale = entry.transform.localScale; var marker = CreateTilemap("PrimaryFinding", markerGroup.transform, 8); var first = record.Result.Edges.First(); marker.SetTile(new Vector3Int(first.From.X, first.From.Y, 0), record.PrimaryFinding.Severity == MoonPalaceRunQualitySeverity.Reject ? tiles["Reject"] : tiles["Warning"]);
                var isRecommended = accepted && record.RecipeId == recommended.RecipeId && record.Seed == recommended.Seed;
                var label = (accepted ? "ACCEPTED" : "REJECTED") + (isRecommended ? " | RUN07 RECOMMENDED" : string.Empty) + " | " + record.RecipeId + " | seed=" + record.Seed.ToString(CultureInfo.InvariantCulture) + "\n" + record.PrimaryFinding.RuleId + " | " + record.Result.CourseDigest.Substring(0, 12); CreateText("Label_" + (accepted ? "A" : "R") + index.ToString("00", CultureInfo.InvariantCulture), labels, new Vector3(origin.x, origin.y - 9.5f, -3f), label, accepted ? new Color(0.35f, 1f, 0.48f, 1f) : new Color(1f, 0.36f, 0.38f, 1f), 0.25f, TextAnchor.UpperLeft);
            }
        }

        private static Dictionary<string, Tile> CreateDebugTiles()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); if (sprite == null) throw new InvalidOperationException("RUN06 requires the built-in UI sprite for isolated gallery debug tiles."); var tiles = new Dictionary<string, Tile>(StringComparer.Ordinal);
            foreach (var definition in TileDefinitions) { var path = Join(SceneDirectoryRelativePath, "RUN06_" + definition.Name + ".asset"); var tile = AssetDatabase.LoadAssetAtPath<Tile>(path); if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); } tile.name = "RUN06_" + definition.Name; tile.sprite = sprite; tile.color = definition.Color; tile.colliderType = Tile.ColliderType.None; EditorUtility.SetDirty(tile); tiles.Add(definition.Name, tile); }
            AssetDatabase.SaveAssets(); return tiles;
        }
        private static Tilemap CreateTilemap(string name, Transform parent, int order) { var gameObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); gameObject.transform.SetParent(parent, false); var renderer = gameObject.GetComponent<TilemapRenderer>(); renderer.sortingOrder = order; renderer.mode = TilemapRenderer.Mode.Chunk; return gameObject.GetComponent<Tilemap>(); }
        private static TextMesh CreateText(string name, Transform parent, Vector3 position, string value, Color color, float size, TextAnchor anchor) { var gameObject = new GameObject(name); gameObject.transform.SetParent(parent, false); gameObject.transform.localPosition = position; var text = gameObject.AddComponent<TextMesh>(); text.text = value; text.color = color; text.characterSize = size; text.fontSize = 28; text.anchor = anchor; return text; }
        private static void PaintPatternFrame(Tilemap map, MoonPalaceSeededRunRoomRecord room, Tile tile) { for (var x = room.MinPatternX; x <= room.MaxPatternX; x++) { map.SetTile(new Vector3Int(x, room.MinPatternY, 0), tile); map.SetTile(new Vector3Int(x, room.MaxPatternY, 0), tile); } for (var y = room.MinPatternY; y <= room.MaxPatternY; y++) { map.SetTile(new Vector3Int(room.MinPatternX, y, 0), tile); map.SetTile(new Vector3Int(room.MaxPatternX, y, 0), tile); } }

        private static string SummaryCsv(MoonPalaceCuratedRunSet curated) => Lines(new[] { "recipe_count,seed_first,seed_last,generated,accepted,rejected,top_rejection_reasons,recommended_recipe,recommended_seed,recommended_course_digest,quality_profile_digest,curation_digest", string.Join(",", I(curated.RecipeCount), I(MoonPalaceRunCurationBatch.FirstSeed), I(MoonPalaceRunCurationBatch.LastSeed), I(curated.GeneratedCount), I(curated.Accepted.Count), I(curated.Rejected.Count), Csv(curated.TopRejectionReasons), curated.Recommended.RecipeId, I(curated.Recommended.Seed), curated.Recommended.Result.CourseDigest, MoonPalaceRunQualityProfileCatalog.CanonicalDigest, curated.CurationDigest) });
        private static string RecordsCsv(IEnumerable<MoonPalaceCuratedRunRecord> records) { var lines = new List<string> { "recipe_id,seed,status,primary_rule,primary_severity,primary_measured,primary_threshold,primary_reason,course_digest,placement_digest,quality_digest" }; lines.AddRange(records.Select(record => string.Join(",", record.RecipeId, I(record.Seed), record.Status, record.PrimaryFinding.RuleId, record.PrimaryFinding.Severity, D(record.PrimaryFinding.MeasuredValue), D(record.PrimaryFinding.Threshold), Csv(record.PrimaryFinding.Reason), record.Result.CourseDigest, record.Result.PlacementDigest, record.Analysis.CanonicalDigest))); return Lines(lines); }
        private static string FindingsCsv(MoonPalaceCuratedRunSet curated) { var lines = new List<string> { "recipe_id,seed,status,rule_id,severity,measured_value,threshold,affected_ids,reason" }; lines.AddRange(curated.Records.SelectMany(record => record.Analysis.Findings.Select(finding => string.Join(",", record.RecipeId, I(record.Seed), record.Status, finding.RuleId, finding.Severity, D(finding.MeasuredValue), D(finding.Threshold), Csv(string.Join(";", finding.AffectedIds)), Csv(finding.Reason))))); return Lines(lines); }
        private static string TuningCsv(MoonPalaceRecipeTuningDelta value) => Lines(new[] { "recipe_id,field,old_value,new_value,reason,motivating_runs", string.Join(",", Csv(value.RecipeId), Csv(value.Field), Csv(value.OldValue), Csv(value.NewValue), Csv(value.Reason), Csv(string.Join(";", value.MotivatingRuns))) });
        private static string LayoutCsv(MoonPalaceCuratedRunSet curated) { var visibleAccepted = new[] { curated.Recommended }.Concat(curated.Accepted.Where(record => record.RecipeId != curated.Recommended.RecipeId || record.Seed != curated.Recommended.Seed)).Take(9).ToList(); var lines = new List<string> { "gallery,status,index,recipe_id,seed,primary_rule,is_run07_recommended" }; lines.AddRange(visibleAccepted.Select((record, index) => "AcceptedGallery,ACCEPTED," + I(index) + "," + record.RecipeId + "," + I(record.Seed) + "," + record.PrimaryFinding.RuleId + "," + (record.RecipeId == curated.Recommended.RecipeId && record.Seed == curated.Recommended.Seed ? "TRUE" : "FALSE"))); lines.AddRange(curated.Rejected.Take(6).Select((record, index) => "RejectedGallery,REJECTED," + I(index) + "," + record.RecipeId + "," + I(record.Seed) + "," + record.PrimaryFinding.RuleId + ",FALSE")); return Lines(lines); }
        private static string ProfileJson() => JsonArray("profiles", MoonPalaceRunQualityProfileCatalog.Profiles.Select(profile => "{\"recipe_id\":\"" + Escape(profile.RecipeId) + "\",\"max_same_row_main_rooms\":" + I(profile.MaxSameRowMainRooms) + ",\"max_open_ratio\":" + D(profile.MaxOpenRatio) + ",\"max_noise_fragments\":" + I(profile.MaxNoiseFragments) + ",\"min_branch_length\":" + I(profile.MinBranchLength) + ",\"max_branch_depth_ratio\":" + D(profile.MaxBranchDepthRatio) + ",\"min_connector_spacing\":" + I(profile.MinConnectorSpacing) + ",\"min_vertical_span\":" + I(profile.MinVerticalSpan) + ",\"min_split_separation\":" + I(profile.MinSplitSeparation) + ",\"digest\":\"" + profile.CanonicalDigest + "\"}"));
        private static string BatchJson(MoonPalaceCuratedRunSet curated) => JsonArray("runs", curated.Records.Select(record => "{\"recipe_id\":\"" + record.RecipeId + "\",\"seed\":" + I(record.Seed) + ",\"status\":\"" + record.Status + "\",\"primary_rule\":\"" + record.PrimaryFinding.RuleId + "\",\"course_digest\":\"" + record.Result.CourseDigest + "\",\"placement_digest\":\"" + record.Result.PlacementDigest + "\"}"));
        private static string RecordsJson(string key, IEnumerable<MoonPalaceCuratedRunRecord> records) => JsonArray(key, records.Select(record => "{\"recipe_id\":\"" + record.RecipeId + "\",\"seed\":" + I(record.Seed) + ",\"primary_rule\":\"" + record.PrimaryFinding.RuleId + "\",\"primary_reason\":\"" + Escape(record.PrimaryFinding.Reason) + "\",\"course_digest\":\"" + record.Result.CourseDigest + "\"}"));
        private static string FindingsJson(MoonPalaceCuratedRunSet curated) => JsonArray("findings", curated.Records.SelectMany(record => record.Analysis.Findings.Select(finding => "{\"recipe_id\":\"" + record.RecipeId + "\",\"seed\":" + I(record.Seed) + ",\"rule_id\":\"" + finding.RuleId + "\",\"severity\":\"" + finding.Severity + "\",\"measured_value\":" + D(finding.MeasuredValue) + ",\"threshold\":" + D(finding.Threshold) + ",\"affected_ids\":\"" + Escape(string.Join(";", finding.AffectedIds)) + "\",\"reason\":\"" + Escape(finding.Reason) + "\"}")));
        private static string TuningJson(MoonPalaceRecipeTuningDelta value) => Json(new[] { Pair("recipe_id", value.RecipeId), Pair("field", value.Field), Pair("old_value", value.OldValue), Pair("new_value", value.NewValue), Pair("reason", value.Reason), Pair("canonical", value.CanonicalLine) });
        private static string GalleryManifestJson(MoonPalaceCuratedRunSet curated, string digest) => Json(new[] { Pair("scene_relative_path", SceneRelativePath), Pair("scene_root", SceneRootName), Pair("scene_manifest_digest", digest), Number("accepted_visible", 9), Number("rejected_visible", 6), Pair("recommended_recipe", curated.Recommended.RecipeId), Number("recommended_seed", curated.Recommended.Seed), Pair("recommended_course_digest", curated.Recommended.Result.CourseDigest), Pair("required_children", "AcceptedGallery;RejectedGallery;QualityLegend;RuleOverlay;SeedLabels;Metadata") });
        private static string DigestManifestJson(IDictionary<string, string> outputs, string sceneDigest, string digest) => JsonArrayWithFields(new[] { Pair("scene_manifest_digest", sceneDigest), Pair("digest_manifest_digest", digest) }, "artifacts", outputs.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => "{\"relative_path\":\"" + Escape(pair.Key) + "\",\"sha256\":\"" + BakingCanonicalDigest.HashCanonicalText(pair.Value) + "\"}"));
        private static string Json(IEnumerable<string> fields) { var values = fields.ToList(); var lines = new List<string> { "{" }; lines.AddRange(values.Select((value, index) => "  " + value + (index == values.Count - 1 ? string.Empty : ","))); lines.Add("}"); return Lines(lines); }
        private static string JsonArray(string name, IEnumerable<string> values) => JsonArrayWithFields(Array.Empty<string>(), name, values);
        private static string JsonArrayWithFields(IEnumerable<string> fields, string name, IEnumerable<string> values) { var prefix = fields.ToList(); var entries = values.ToList(); var lines = new List<string> { "{" }; lines.AddRange(prefix.Select(value => "  " + value + ",")); lines.Add("  \"" + Escape(name) + "\": ["); lines.AddRange(entries.Select((value, index) => "    " + value + (index == entries.Count - 1 ? string.Empty : ","))); lines.Add("  ]"); lines.Add("}"); return Lines(lines); }
        private static string Pair(string name, string value) => "\"" + Escape(name) + "\": \"" + Escape(value) + "\""; private static string Number(string name, int value) => "\"" + Escape(name) + "\": " + I(value); private static string Lines(IEnumerable<string> lines) => string.Join("\n", lines) + "\n"; private static string FinalLf(string value) => BakingCanonicalDigest.NormalizeLineEndingsToLf(value ?? string.Empty).TrimEnd('\n') + "\n"; private static string Csv(string value) { value = value ?? string.Empty; return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value; } private static string I(int value) => value.ToString(CultureInfo.InvariantCulture); private static string D(double value) => value.ToString("0.######", CultureInfo.InvariantCulture); private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n"); private static string Join(string left, string right) => left.TrimEnd('/') + "/" + right.TrimStart('/'); private static string Resolve(string root, string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        private sealed class TileDefinition { public TileDefinition(string name, Color color) { Name = name; Color = color; } public string Name { get; } public Color Color { get; } }
    }
}
