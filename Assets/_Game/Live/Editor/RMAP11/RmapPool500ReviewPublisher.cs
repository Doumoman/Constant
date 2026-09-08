#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Character.Live.Rmap10.Editor;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEngine;

namespace StarNight.Character.Live.Rmap11.Editor
{
    /// <summary>Creates RMAP11-owned, inspectable source/role/usage material.
    /// It never records approval: that decision belongs to an identified human reviewer.</summary>
    public static class RmapPool500ReviewPublisher
    {
        public const string GeneratedDirectory = "MapDesign/MCP/GENERATED/RMAP11/FIX25";
        public const string ReviewDirectory = GeneratedDirectory + "/review";
        public const string ScenePath = "Assets/_Game/Map/Scenes/MoonPalace/RMAP11/MoonPalacePool500_FIX25_RMAP11.unity";

        [MenuItem("Tools/MoonPalace/RMAP11/Build Pool 500 Review Package")]
        public static void Publish()
        {
            RmapPatternPool500Snapshot pool = RmapPatternPool500.BuildFinalPool();
            RmapPatternPool500.ValidateFinalPool(pool);
            var request = new RmapSmallRunRequest(1107, 36, 24, RmapSmallRunRecipe.PortGalleryV1);
            RmapSmallRunPlan plan = RmapSmallRunHarness.Generate(request);
            if (!plan.Success) throw new InvalidOperationException("RMAP11 representative run failed: " + plan.FailureSummary);
            RmapSmallRunPatternSelection[] externalSelections = plan.Chunks.SelectMany(chunk => chunk.Selections)
                .Where(selection => pool.TryGetCandidate(selection.CandidateId, out RmapPatternPool500Entry entry) && !entry.IsInitialPool)
                .ToArray();
            if (externalSelections.Length == 0)
                throw new InvalidOperationException("RMAP11 representative run did not use an entry outside the first 48.");

            RmapSmallRunSceneBuilder.BuildPool500Fix25Review(request);
            string root = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(root, GeneratedDirectory.Replace('/', Path.DirectorySeparatorChar));
            string review = Path.Combine(root, ReviewDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(output);
            Directory.CreateDirectory(review);
            RmapPatternPool500Entry[] representatives = Representatives(pool);

            Write(output, "rmap11_final_pool.csv", RmapPatternPool500.ExportFinalPoolCsv(pool));
            Write(output, "rmap11_role_summary.csv", RoleSummaryCsv(pool));
            Write(output, "rmap11_provenance.csv", ProvenanceCsv(pool));
            Write(output, "rmap11_run06_comparison.csv", Run06ComparisonCsv(pool));
            Write(output, "rmap11_actual_usage.csv", ActualUsageCsv(pool, plan));
            Write(output, "rmap11_review_manifest.csv", ReviewManifestCsv(representatives));
            Write(output, "rmap11_fix25_fixture_manifest.csv", Fix25FixtureManifestCsv(pool));
            Write(output, "rmap11_human_review_request.md", HumanReviewRequest(pool, plan, representatives));
            Write(output, "rmap11_manifest.json", Manifest(pool, plan, representatives));
            foreach (RmapPatternPool500Entry representative in representatives)
                WriteImage(Path.Combine(review, representative.CandidateId + ".png"), representative.BaseCells);
            WriteBoard(Path.Combine(review, "rmap11_representative_board.png"), representatives);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("RMAP11 review package ready: " + GeneratedDirectory + " | pool=" + pool.Digest +
                " | external-selected=" + string.Join(",", externalSelections.Select(value => value.CandidateId).Distinct(StringComparer.Ordinal)));
        }

        private static RmapPatternPool500Entry[] Representatives(RmapPatternPool500Snapshot pool) =>
            Enum.GetValues(typeof(RmapPatternPrimaryRole)).Cast<RmapPatternPrimaryRole>().Select(role =>
                pool.Candidates.Where(candidate => candidate.PrimaryRole == role)
                    .OrderBy(candidate => candidate.IsInitialPool ? 1 : 0)
                    .ThenBy(candidate => candidate.HasOnlyAirBorder ? 0 : 1)
                    .ThenBy(candidate => candidate.PoolIndex).First()).ToArray();

        private static string RoleSummaryCsv(RmapPatternPool500Snapshot pool)
        {
            var text = new StringBuilder("PrimaryRole,Target,Actual,Difference,AdjustmentReason\n");
            foreach (RmapPatternPool500RoleSummary value in pool.RoleSummary)
                text.Append(Csv(value.Role.ToString(), value.Target.ToString(CultureInfo.InvariantCulture),
                    value.Actual.ToString(CultureInfo.InvariantCulture), value.Difference.ToString(CultureInfo.InvariantCulture),
                    value.AdjustmentReason)).Append('\n');
            return text.ToString();
        }

        private static string ProvenanceCsv(RmapPatternPool500Snapshot pool)
        {
            var text = new StringBuilder("SourceId,SourceMaskReference,OriginalReference,Transform,FinalCandidateId,Decision\n");
            foreach (RmapPatternPool500SourceRecord value in pool.Provenance)
                text.Append(Csv(value.SourceId, value.SourceMaskReference, value.OriginalReference,
                    value.Transform.ToString(), value.FinalCandidateId, value.Decision)).Append('\n');
            return text.ToString();
        }

        private static string Run06ComparisonCsv(RmapPatternPool500Snapshot pool)
        {
            var text = new StringBuilder("ComparisonScope,SourceId,SourceMaskReference,FinalCandidateId,Decision\n");
            foreach (RmapPatternPool500SourceRecord value in pool.Provenance.Where(value =>
                value.SourceId.StartsWith("VIS01_MP_", StringComparison.Ordinal)))
                text.Append(Csv("RUN06_VIS01_500_MASK_TO_RMAP11_FINAL_16_CELL", value.SourceId,
                    value.SourceMaskReference, value.FinalCandidateId, value.Decision)).Append('\n');
            return text.ToString();
        }

        private static string ActualUsageCsv(RmapPatternPool500Snapshot pool, RmapSmallRunPlan plan)
        {
            var text = new StringBuilder("ScenePath,PoolVersion,PoolDigest,Seed,Size,Chunk,SlotOrigin,CandidateId,PoolIndex,InitialPool,Role,Transform,FinalCells16,TilemapTarget,ActualPlayerExit\n");
            foreach (RmapSmallRunChunk chunk in plan.Chunks)
            foreach (RmapSmallRunPatternSelection selection in chunk.Selections)
            {
                if (!pool.TryGetCandidate(selection.CandidateId, out RmapPatternPool500Entry candidate))
                    throw new InvalidOperationException("RMAP11 small run selected an ID outside final pool: " + selection.CandidateId);
                text.Append(Csv(ScenePath, plan.PoolVersion, pool.Digest,
                    plan.Request.Seed.ToString(CultureInfo.InvariantCulture), plan.Request.Width + "x" + plan.Request.Height,
                    chunk.InstanceId, selection.SlotX + ";" + selection.SlotY, selection.CandidateId,
                    candidate.PoolIndex.ToString(CultureInfo.InvariantCulture), candidate.IsInitialPool ? "true" : "false",
                    candidate.PrimaryRole.ToString(), selection.Transform.ToString(), selection.FinalCells16,
                    "RMAP11_BaseTerrain/RMAP11_OneWayOverlay", "RMAP11_Player->RMAP11_Exit")).Append('\n');
            }
            return text.ToString();
        }

        private static string ReviewManifestCsv(IEnumerable<RmapPatternPool500Entry> representatives)
        {
            var text = new StringBuilder("PrimaryRole,CandidateId,PoolIndex,InitialPool,BaseCells16,SourceId,SourceMaskReference,Transform,RuleId,SelectionReason,BorderSafe,TagConflicts,ContextRequired,ImagePath\n");
            foreach (RmapPatternPool500Entry value in representatives)
                text.Append(Csv(value.PrimaryRole.ToString(), value.CandidateId,
                    value.PoolIndex.ToString(CultureInfo.InvariantCulture), value.IsInitialPool ? "true" : "false",
                    value.BaseCells16, value.SourceId, value.SourceMaskReference, value.Transform.ToString(), value.RuleId,
                    value.SelectionReason, value.HasOnlyAirBorder ? "true" : "false", string.Join(";", value.TagConflicts),
                    string.Join(";", value.Characteristics.ContextRequired), ReviewDirectory + "/" + value.CandidateId + ".png")).Append('\n');
            return text.ToString();
        }

        private static string Fix25FixtureManifestCsv(RmapPatternPool500Snapshot pool)
        {
            var text = new StringBuilder("AtlasNo,PoolIndex,OldCandidateId,OldBaseCells16,NewCandidateId,NewBaseCells16,ActualRole,InitialPool,RuleId,PhysicalFixture,GrabVerification\n");
            foreach (RmapPatternPool500Replacement replacement in RmapPatternPool500.Fix25Replacements)
            {
                RmapPatternPool500Entry entry = pool.Candidates[replacement.PoolIndex];
                text.Append(Csv(replacement.AtlasNo.ToString(CultureInfo.InvariantCulture),
                    replacement.PoolIndex.ToString(CultureInfo.InvariantCulture), replacement.OldCandidateId,
                    replacement.OldBaseCells16, entry.CandidateId, entry.BaseCells16, entry.PrimaryRole.ToString(),
                    entry.IsInitialPool ? "true" : "false", entry.RuleId,
                    "RmapPool500Fix25PlayModeTests", replacement.AtlasNo == 2 || replacement.AtlasNo == 13 ||
                    replacement.AtlasNo == 14 || replacement.AtlasNo == 19 || replacement.AtlasNo == 36 || replacement.AtlasNo == 49
                        ? "JUMP" : "JUMP_GRAB_JUMP")).Append('\n');
            }
            return text.ToString();
        }

        private static string HumanReviewRequest(RmapPatternPool500Snapshot pool, RmapSmallRunPlan plan,
            IEnumerable<RmapPatternPool500Entry> representatives)
        {
            RmapPatternPool500Entry[] actual = plan.Chunks.SelectMany(chunk => chunk.Selections)
                .Select(selection => pool.TryGetCandidate(selection.CandidateId, out RmapPatternPool500Entry candidate) ? candidate : null)
                .Where(candidate => candidate != null && !candidate.IsInitialPool).Distinct().ToArray();
            return "# RMAP11 Human Review Request\n\n" +
                "Status: HUMAN_REVIEW_PENDING. Automatic validation and image generation are not human approval.\n\n" +
                "Review the ten 4x4 images under `" + ReviewDirectory + "` with `rmap11_review_manifest.csv`. " +
                "Legend: S=solid (gray), A=air (blue), O=one-way platform (gold). Each PNG contains the actual 16 typed base cells; " +
                "`rmap11_representative_board.png` is the 10-role comparison board.\n\n" +
                "Also inspect `" + ScenePath + "` (Seed 1107, 36x24, " + plan.PoolVersion + ") and confirm the actual Production Player reaches RMAP11_Exit. " +
                "The final-pool candidates used outside the old first 48 are: `" + string.Join("`, `", actual.Select(value => value.CandidateId)) + "`.\n\n" +
                "Please confirm: (1) all 10 representatives show the stated geometry/role, including both slopes, walls, ceilings, void, one-way and vertical passage; " +
                "(2) source/transform/context and tag-conflict notes are acceptable; (3) the visible small run uses the listed non-first-48 candidate and remains understandable/playable; " +
                "(4) approve or list requested changes with candidate IDs.\n\n" +
                "Pool version: `" + RmapPatternPool500.DataVersion + "`\n" +
                "Pool digest: `" + pool.Digest + "`\n";
        }

        private static string Manifest(RmapPatternPool500Snapshot pool, RmapSmallRunPlan plan,
            IEnumerable<RmapPatternPool500Entry> representatives)
        {
            return "{\n" +
                "  \"pool_version\": \"" + RmapPatternPool500.DataVersion + "\",\n" +
                "  \"pool_digest\": \"" + pool.Digest + "\",\n" +
                "  \"candidate_count\": " + pool.Candidates.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"void_clear_count\": " + pool.Candidates.Count(value => value.PrimaryRole == RmapPatternPrimaryRole.VoidClear).ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"legacy_source\": \"" + RmapPatternPool500.LegacySource + "\",\n" +
                "  \"run06_comparison\": \"rmap11_run06_comparison.csv\",\n" +
                "  \"review_status\": \"HUMAN_REVIEW_PENDING\",\n" +
                "  \"scene_path\": \"" + ScenePath + "\",\n" +
                "  \"representative_seed\": " + plan.Request.Seed.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"plan_digest\": \"" + plan.PlanDigest + "\",\n" +
                "  \"review_candidates\": \"" + string.Join(";", representatives.Select(value => value.CandidateId)) + "\",\n" +
                "  \"review_board\": \"review/rmap11_representative_board.png\"\n" +
                "}\n";
        }

        private static void WriteImage(string path, IReadOnlyList<RmapPatternBaseCell> cells)
        {
            const int cellPixels = 64;
            var texture = new Texture2D(RmapPatternCatalog.Width * cellPixels, RmapPatternCatalog.Height * cellPixels,
                TextureFormat.RGBA32, false);
            DrawCells(texture, 0, 0, cells, cellPixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void WriteBoard(string path, IReadOnlyList<RmapPatternPool500Entry> entries)
        {
            const int cellPixels = 32;
            const int columns = 5;
            int width = columns * RmapPatternCatalog.Width * cellPixels;
            int height = ((entries.Count + columns - 1) / columns) * RmapPatternCatalog.Height * cellPixels;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] background = Enumerable.Repeat(new Color32(20, 23, 31, 255), width * height).ToArray();
            texture.SetPixels32(background);
            for (var index = 0; index < entries.Count; index++)
            {
                int originX = (index % columns) * RmapPatternCatalog.Width * cellPixels;
                int originY = (1 - (index / columns)) * RmapPatternCatalog.Height * cellPixels;
                DrawCells(texture, originX, originY, entries[index].BaseCells, cellPixels);
            }
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void DrawCells(Texture2D texture, int originX, int originY,
            IReadOnlyList<RmapPatternBaseCell> cells, int cellPixels)
        {
            for (var y = 0; y < RmapPatternCatalog.Height; y++)
            for (var x = 0; x < RmapPatternCatalog.Width; x++)
            {
                Color32 color = ColorFor(cells[(y * RmapPatternCatalog.Width) + x]);
                for (var pixelY = 0; pixelY < cellPixels; pixelY++)
                for (var pixelX = 0; pixelX < cellPixels; pixelX++)
                {
                    bool grid = pixelX < 2 || pixelY < 2 || pixelX >= cellPixels - 2 || pixelY >= cellPixels - 2;
                    texture.SetPixel(originX + (x * cellPixels) + pixelX, originY + (y * cellPixels) + pixelY,
                        grid ? new Color32(238, 242, 250, 255) : color);
                }
            }
            texture.Apply(false, false);
        }

        private static Color32 ColorFor(RmapPatternBaseCell cell)
        {
            switch (cell)
            {
                case RmapPatternBaseCell.Solid: return new Color32(115, 123, 138, 255);
                case RmapPatternBaseCell.OneWayPlatform: return new Color32(234, 185, 59, 255);
                default: return new Color32(50, 75, 117, 255);
            }
        }

        private static void Write(string directory, string name, string content) =>
            File.WriteAllText(Path.Combine(directory, name), content, new UTF8Encoding(false));

        private static string Csv(params string[] fields) => string.Join(",", fields.Select(field => "\"" +
            (field ?? string.Empty).Replace("\"", "\"\"") + "\""));
    }
}
#endif
