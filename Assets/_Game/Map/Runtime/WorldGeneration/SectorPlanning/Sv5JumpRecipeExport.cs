using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5JumpRecipeExport
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static IReadOnlyDictionary<string, string> BuildArtifacts(Sv5JumpRecipeCatalogModel catalog)
        {
            RequirePassing(catalog);
            return new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "jump_recipes.json", JumpRecipesJson(catalog) },
                { "recipe_catalog.csv", RecipeCatalogCsv(catalog) },
                { "recipe_segments.csv", RecipeSegmentsCsv(catalog) },
                { "recipe_supports.csv", RecipeSupportsCsv(catalog) },
                { "recipe_occupancy.csv", RecipeOccupancyCsv(catalog) },
                { "recipe_links.csv", RecipeLinksCsv(catalog) },
                { "recipe_grab_edges.csv", RecipeGrabEdgesCsv(catalog) },
                { "reference_012_before.csv", ReferenceGridCsv(catalog.Reference012.BeforeRows) },
                { "reference_012_draft_after.csv", ReferenceGridCsv(catalog.Reference012.DraftAfterRows) },
                { "reference_012_changes.csv", ReferenceChangesCsv(catalog.Reference012) },
                { "reference_012_correspondence.csv", ReferenceCorrespondenceCsv(catalog.Reference012) },
                { "jump_recipe_validation.json", ValidationJson(catalog) },
                { "preview/jump_recipes.svg", PreviewSvg(catalog) },
            };
        }

        public static void WriteCanonical(string directory)
        {
            Write(directory, Sv5JumpRecipeCatalog.CreateCanonicalCatalog());
        }

        public static void Write(string directory, Sv5JumpRecipeCatalogModel catalog)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Export directory is required.", nameof(directory));
            foreach (KeyValuePair<string, string> artifact in BuildArtifacts(catalog))
            {
                string path = Path.Combine(directory, artifact.Key.Replace('/', Path.DirectorySeparatorChar));
                string parent = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                File.WriteAllText(path, Normalize(artifact.Value), Utf8);
            }
        }

        public static string JumpRecipesJson(Sv5JumpRecipeCatalogModel catalog)
        {
            RequirePassing(catalog);
            var text = new StringBuilder();
            text.AppendLine("{");
            text.AppendLine("  \"schema\": \"SV5_17_JUMP_RECIPES/v1\",");
            text.AppendLine("  \"coordinate_scope\": \"24x32_LOCAL_INTEGER_CELLS\",");
            text.AppendLine("  \"base_fixture\": \"SV5_16_JUMP_OUTLINE_CANONICAL\",");
            text.AppendLine("  \"base_fixture_digest\": \"" + Json(catalog.BaseFixtureDigest) + "\",");
            text.AppendLine("  \"catalog_digest\": \"" + Json(catalog.Digest) + "\",");
            text.AppendLine("  \"recipe_count\": " + catalog.Recipes.Count + ",");
            text.AppendLine("  \"catalog_supports\": " + catalog.Recipes.Sum(value => value.Supports.Count) + ",");
            text.AppendLine("  \"catalog_occupancy_cells\": " + catalog.Recipes.Sum(value => value.Occupancy.Count) + ",");
            text.AppendLine("  \"catalog_route_links\": " + catalog.Recipes.Sum(value => value.RouteLinks.Count) + ",");
            text.AppendLine("  \"catalog_grab_edges\": " + catalog.Recipes.Sum(value => value.GrabEdges.Count) + ",");
            text.AppendLine("  \"reference_012_changed_cells\": " + catalog.Reference012.Changes.Count + ",");
            text.AppendLine("  \"recipes\": [");
            for (int i = 0; i < catalog.Recipes.Count; i++)
            {
                Sv5JumpRecipeVariant recipe = catalog.Recipes[i];
                text.AppendLine("    {");
                text.AppendLine("      \"recipe_id\": \"" + Json(recipe.RecipeId) + "\",");
                text.AppendLine("      \"transform\": \"" + recipe.ExportTransform + "\",");
                text.AppendLine("      \"digest\": \"" + recipe.Digest + "\",");
                text.AppendLine("      \"supports\": " + recipe.Supports.Count + ",");
                text.AppendLine("      \"final_occupancy_cells\": " + recipe.Occupancy.Count + ",");
                text.AppendLine("      \"route_links\": " + recipe.RouteLinks.Count + ",");
                text.AppendLine("      \"explicit_grab_edges\": " + recipe.GrabEdges.Count + ",");
                text.AppendLine("      \"static_recipe_only\": true,");
                text.AppendLine("      \"production_equivalence_to_reference_012\": false,");
                text.AppendLine("      \"player_verified\": false");
                text.Append("    }");
                text.AppendLine(i + 1 == catalog.Recipes.Count ? string.Empty : ",");
            }
            text.AppendLine("  ],");
            text.AppendLine("  \"reference_012\": {");
            text.AppendLine("    \"runtime_role\": \"REFERENCE_ONLY_NOT_RUNTIME_GEOMETRY\",");
            text.AppendLine("    \"before_sha256\": \"" + catalog.Reference012.BeforeDigest + "\",");
            text.AppendLine("    \"draft_after_sha256\": \"" + catalog.Reference012.DraftAfterDigest + "\",");
            text.AppendLine("    \"changed_cells\": 141,");
            text.AppendLine("    \"production_equivalence_claimed\": false,");
            text.AppendLine("    \"player_verified\": false");
            text.AppendLine("  },");
            text.AppendLine("  \"readiness\": {");
            text.AppendLine("    \"jump_recipe_ready\": true,");
            text.AppendLine("    \"composed_geometry_ready\": true,");
            text.AppendLine("    \"swept_clearance_ready\": false,");
            text.AppendLine("    \"recovery_ready\": false,");
            text.AppendLine("    \"player_verified\": false");
            text.AppendLine("  },");
            text.AppendLine("  \"performance\": {");
            text.AppendLine("    \"whole_world_builds_in_new_targeted_tests\": 0,");
            text.AppendLine("    \"whole_world_searches_in_new_targeted_tests\": 0");
            text.AppendLine("  }");
            text.AppendLine("}");
            return text.ToString();
        }

        public static string RecipeCatalogCsv(Sv5JumpRecipeCatalogModel catalog)
        {
            return Csv(
                "recipe_id,transform,supports,base_support_cells,outline_cells,final_occupancy_cells,route_links,plus_one_jump_links,plus_two_jump_grab_links,level_jump_links,grab_edges,static_recipe_only,production_equivalence_to_reference_012,player_verified,digest",
                catalog.Recipes.Select(value => Row(value.RecipeId, value.ExportTransform, value.Supports.Count,
                    value.BaseSupportCellCount, value.OutlineCellCount, value.Occupancy.Count, value.RouteLinks.Count,
                    value.PlusOneJumpCount, value.PlusTwoJumpGrabCount, value.LevelJumpCount, value.GrabEdges.Count,
                    value.StaticRecipeOnly, value.ProductionEquivalenceToReference012, value.PlayerVerified, value.Digest)));
        }

        public static string RecipeSegmentsCsv(Sv5JumpRecipeCatalogModel catalog)
        {
            return Csv("recipe_id,segment_order,segment_id,purpose,source_link_ids",
                catalog.Recipes.SelectMany(recipe => recipe.Segments.Select(segment => Row(recipe.RecipeId,
                    segment.SegmentOrder, segment.SegmentId, segment.Purpose, string.Join(";", segment.SourceLinkIds)))));
        }

        public static string RecipeSupportsCsv(Sv5JumpRecipeCatalogModel catalog)
        {
            return Csv("recipe_id,source_support_id,kind,height,x,y,width,top_y,top_x_min,top_x_max,validation_state",
                catalog.Recipes.SelectMany(recipe => recipe.Supports.Select(row => Row(recipe.RecipeId, row.SourceSupportId,
                    row.Support.ExportKind, row.Support.Height, row.Support.X, row.Support.Y, row.Support.Width,
                    row.Support.TopY, row.Support.TopXMin, row.Support.TopXMax, State(row.Support.ValidationState)))));
        }

        public static string RecipeOccupancyCsv(Sv5JumpRecipeCatalogModel catalog)
        {
            return Csv("recipe_id,x,y,collision,source_owner_id,source,support_kind",
                catalog.Recipes.SelectMany(recipe => recipe.Occupancy.Select(row => Row(recipe.RecipeId,
                    row.Point.X, row.Point.Y, row.Collision, row.SourceOwnerId, row.Source, row.SupportKind))));
        }

        public static string RecipeLinksCsv(Sv5JumpRecipeCatalogModel catalog)
        {
            return Csv("recipe_id,order,source_link_id,source_support_id,target_support_id,mode,gap_air,rise,required_route,direction,takeoff_x,takeoff_y,landing_x,landing_y,grab_edge_id,validation_state,contract_accepted",
                catalog.Recipes.SelectMany(recipe => recipe.RouteLinks.Select(row => Row(recipe.RecipeId,
                    row.Link.Order, row.SourceLinkId, row.Link.Source.SupportId, row.Link.Target.SupportId,
                    Mode(row.Link.Mode), row.Link.GapAir, row.Link.Rise, row.Link.RequiredRoute,
                    Direction(row.Link.Direction), row.Link.Takeoff.X, row.Link.Takeoff.Y,
                    row.Link.Landing.X, row.Link.Landing.Y,
                    row.Link.GrabEdge == null ? string.Empty : row.Link.GrabEdge.GrabEdgeId,
                    State(row.Link.ValidationState), row.Link.ContractAccepted))));
        }

        public static string RecipeGrabEdgesCsv(Sv5JumpRecipeCatalogModel catalog)
        {
            return Csv("recipe_id,source_grab_edge_id,target_support_id,face,approach_direction,contact_x,contact_y,hang_body_x,hang_body_y,hang_head_x,hang_head_y,pull_up_x,pull_up_y,pull_up_head_x,pull_up_head_y,exposed,safe",
                catalog.Recipes.SelectMany(recipe => recipe.GrabEdges.Select(row => Row(recipe.RecipeId,
                    row.SourceGrabEdgeId, row.Edge.SupportId, Face(row.Edge.Face), Direction(row.Edge.ApproachDirection),
                    row.Edge.Contact.X, row.Edge.Contact.Y, row.Edge.HangBody.X, row.Edge.HangBody.Y,
                    row.HangHead.X, row.HangHead.Y, row.Edge.PullUp.X, row.Edge.PullUp.Y,
                    row.PullUpHead.X, row.PullUpHead.Y, row.Edge.Exposed, row.Edge.Safe))));
        }

        public static string ReferenceGridCsv(IReadOnlyList<string> rows)
        {
            return Csv("x,y,cell", Enumerable.Range(0, rows.Count).SelectMany(y =>
                Enumerable.Range(0, rows[y].Length).Select(x => Row(x, y, rows[y][x]))));
        }

        public static string ReferenceChangesCsv(Sv5JumpRecipeReference reference)
        {
            return Csv("x,y,before,draft_after", reference.Changes.Select(value =>
                Row(value.X, value.Y, value.Before, value.DraftAfter)));
        }

        public static string ReferenceCorrespondenceCsv(Sv5JumpRecipeReference reference)
        {
            return Csv("intent_id,description,relationship,production_equivalence,player_verified",
                reference.Correspondences.Select(value => Row(value.IntentId, value.Description, value.Relationship,
                    value.ProductionEquivalence, value.PlayerVerified)));
        }

        public static string ValidationJson(Sv5JumpRecipeCatalogModel catalog)
        {
            var errors = catalog == null ? new[] { "CATALOG_NULL" } : catalog.Diagnostics.ToArray();
            var text = new StringBuilder();
            text.AppendLine("{");
            text.AppendLine("  \"schema\": \"SV5_17_JUMP_RECIPE_VALIDATION/v1\",");
            text.AppendLine("  \"status\": \"" + (errors.Length == 0 ? "PASS" : "FAIL") + "\",");
            text.AppendLine("  \"base_fixture_digest\": \"" + Json(catalog == null ? string.Empty : catalog.BaseFixtureDigest) + "\",");
            text.AppendLine("  \"recipe_count\": " + (catalog == null ? 0 : catalog.Recipes.Count) + ",");
            text.AppendLine("  \"reference_012_changed_cells\": " + (catalog == null || catalog.Reference012 == null ? 0 : catalog.Reference012.Changes.Count) + ",");
            text.AppendLine("  \"errors\": [");
            for (int index = 0; index < errors.Length; index++)
            {
                text.Append("    \"").Append(Json(errors[index])).Append('"');
                text.AppendLine(index + 1 == errors.Length ? string.Empty : ",");
            }
            text.AppendLine("  ]");
            text.AppendLine("}");
            return text.ToString();
        }

        public static string PreviewSvg(Sv5JumpRecipeCatalogModel catalog)
        {
            RequirePassing(catalog);
            const int cell = 13;
            const int boardY = 84;
            var text = new StringBuilder();
            text.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"880\" height=\"700\" viewBox=\"0 0 880 700\">");
            text.AppendLine("<rect width=\"880\" height=\"700\" fill=\"#101620\"/>");
            text.AppendLine("<defs><marker id=\"arrow\" markerWidth=\"7\" markerHeight=\"7\" refX=\"5\" refY=\"3.5\" orient=\"auto\"><path d=\"M0,0 L7,3.5 L0,7 z\" fill=\"#ffd166\"/></marker></defs>");
            text.AppendLine("<style>text{font-family:Consolas,monospace;fill:#ecf4ff}.title{font-size:20px;font-weight:bold}.label{font-size:12px}.muted{fill:#9eb0c4}.grid{stroke:#263345;stroke-width:.5}.route{stroke:#ffd166;stroke-width:2;fill:none;marker-end:url(#arrow)}.grab{fill:#ff5d8f;stroke:#fff;stroke-width:1}</style>");
            text.AppendLine("<text x=\"24\" y=\"30\" class=\"title\">SV5_17 deterministic local jump recipes</text>");
            text.AppendLine("<text x=\"24\" y=\"54\" class=\"label muted\">24x32 local cells | +1 x7 | +2 Grab x1 | level x1 | static recipe</text>");
            DrawRecipe(text, catalog.Recipe(Sv5JumpRecipeCatalog.R0RecipeId), 24, boardY, cell);
            DrawRecipe(text, catalog.Recipe(Sv5JumpRecipeCatalog.MirrorRecipeId), 448, boardY, cell);
            text.AppendLine("<text x=\"24\" y=\"535\" class=\"title\">REFERENCE ONLY — #012 before / draft-after</text>");
            text.AppendLine("<text x=\"24\" y=\"558\" class=\"label\">141 cells changed | NOT PRODUCTION | design lineage only | not Player verified</text>");
            DrawReference(text, catalog.Reference012.BeforeRows, 24, 574, 3);
            DrawReference(text, catalog.Reference012.DraftAfterRows, 118, 574, 3);
            text.AppendLine("<text x=\"220\" y=\"596\" class=\"label muted\">Production geometry comes only from the SV5_16 typed fixture.</text>");
            text.AppendLine("<rect x=\"220\" y=\"610\" width=\"12\" height=\"12\" fill=\"#53677d\"/><text x=\"238\" y=\"621\" class=\"label\">base SOLID</text>");
            text.AppendLine("<rect x=\"350\" y=\"610\" width=\"12\" height=\"12\" fill=\"#2f8f83\"/><text x=\"368\" y=\"621\" class=\"label\">ONE_WAY</text>");
            text.AppendLine("<rect x=\"465\" y=\"610\" width=\"12\" height=\"12\" fill=\"#334155\"/><text x=\"483\" y=\"621\" class=\"label\">outline SOLID</text>");
            text.AppendLine("<circle cx=\"620\" cy=\"616\" r=\"6\" class=\"grab\"/><text x=\"633\" y=\"621\" class=\"label\">explicit Grab</text>");
            text.AppendLine("</svg>");
            return text.ToString();
        }

        private static void DrawRecipe(StringBuilder text, Sv5JumpRecipeVariant recipe, int originX, int originY, int cell)
        {
            text.AppendLine("<text x=\"" + originX + "\" y=\"" + (originY - 18) + "\" class=\"title\">" + recipe.RecipeId + "</text>");
            text.AppendLine("<rect x=\"" + originX + "\" y=\"" + originY + "\" width=\"" + (24 * cell) + "\" height=\"" + (32 * cell) + "\" fill=\"#17202c\" stroke=\"#62758c\"/>");
            foreach (Sv5JumpRecipeOccupancy row in recipe.Occupancy)
            {
                int x = originX + row.Point.X * cell;
                int y = originY + (31 - row.Point.Y) * cell;
                string fill = row.SupportKind == "ONE_WAY" ? "#2f8f83" : row.Source == "OUTLINE_BACKING" ? "#334155" : "#53677d";
                text.AppendLine("<rect x=\"" + x + "\" y=\"" + y + "\" width=\"" + cell + "\" height=\"" + cell + "\" fill=\"" + fill + "\" class=\"grid\"/>");
            }
            foreach (Sv5JumpRecipeLink row in recipe.RouteLinks)
            {
                int x1 = originX + row.Link.Takeoff.X * cell + cell / 2;
                int y1 = originY + (31 - row.Link.Takeoff.Y) * cell + cell / 2;
                int x2 = originX + row.Link.Landing.X * cell + cell / 2;
                int y2 = originY + (31 - row.Link.Landing.Y) * cell + cell / 2;
                text.AppendLine("<path d=\"M" + x1 + " " + y1 + " Q" + ((x1 + x2) / 2) + " " + (Math.Min(y1, y2) - 14) + " " + x2 + " " + y2 + "\" class=\"route\"/>");
            }
            Sv5JumpRecipeGrab grab = recipe.GrabEdges.Single();
            int gx = originX + grab.Edge.Contact.X * cell + cell / 2;
            int gy = originY + (31 - grab.Edge.Contact.Y) * cell + cell / 2;
            text.AppendLine("<circle cx=\"" + gx + "\" cy=\"" + gy + "\" r=\"5\" class=\"grab\"/>");
            for (int x = 0; x <= 24; x++)
                text.AppendLine("<line x1=\"" + (originX + x * cell) + "\" y1=\"" + originY + "\" x2=\"" + (originX + x * cell) + "\" y2=\"" + (originY + 32 * cell) + "\" class=\"grid\"/>");
            for (int y = 0; y <= 32; y++)
                text.AppendLine("<line x1=\"" + originX + "\" y1=\"" + (originY + y * cell) + "\" x2=\"" + (originX + 24 * cell) + "\" y2=\"" + (originY + y * cell) + "\" class=\"grid\"/>");
        }

        private static void DrawReference(StringBuilder text, IReadOnlyList<string> rows, int originX, int originY, int cell)
        {
            for (int y = 0; y < rows.Count; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char value = rows[y][x];
                    string fill = value == 'S' ? "#53677d" : value == 'O' ? "#2f8f83" : "#17202c";
                    text.AppendLine("<rect x=\"" + (originX + x * cell) + "\" y=\"" + (originY + (31 - y) * cell) + "\" width=\"" + cell + "\" height=\"" + cell + "\" fill=\"" + fill + "\"/>");
                }
        }

        private static string Csv(string header, IEnumerable<string> rows)
        {
            return header + "\n" + string.Join("\n", rows) + "\n";
        }

        private static string Row(params object[] values)
        {
            return string.Join(",", values.Select(value => Quote(value is bool
                ? ((bool)value ? "true" : "false")
                : Convert.ToString(value, CultureInfo.InvariantCulture))));
        }

        private static string Quote(string value)
        {
            value = value ?? string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Direction(Sv5JumpDirection value)
        {
            return value == Sv5JumpDirection.LeftToRight ? "LEFT_TO_RIGHT" : "RIGHT_TO_LEFT";
        }

        private static string Face(Sv5JumpGrabFace value)
        {
            return value == Sv5JumpGrabFace.Left ? "LEFT" : "RIGHT";
        }

        private static string Mode(Sv5JumpMode value)
        {
            return value == Sv5JumpMode.JumpGrab ? "JUMP_GRAB" : "JUMP";
        }

        private static string State(Sv5JumpValidationState value)
        {
            return value == Sv5JumpValidationState.PlayerVerified ? "PLAYER_VERIFIED" :
                value == Sv5JumpValidationState.StaticScreen ? "STATIC_SCREEN" : "PLANNED";
        }

        private static string Json(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static void RequirePassing(Sv5JumpRecipeCatalogModel catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (catalog.Diagnostics.Count != 0)
                throw new InvalidOperationException("SV5_17 catalog is invalid: " + string.Join("; ", catalog.Diagnostics));
        }
    }
}
