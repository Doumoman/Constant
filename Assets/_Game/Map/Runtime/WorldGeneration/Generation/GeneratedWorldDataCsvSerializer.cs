using System;
using System.Globalization;
using System.Text;

namespace StarNight.Map.WorldGeneration.Generation
{
    public static class GeneratedWorldDataCsvSerializer
    {
        public const string FileName = "generated_world_sectors.csv";

        public const string Header =
            "seed,sector_x,sector_y,sector_role,primary_biome_id,secondary_biome_id,patch_id,route_mask_id,special_site_instance_id,boundary_profile_id,sector_recipe_id,shortest_distance_from_start,mandatory_graph_node";

        private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };

        public static byte[] Serialize(GeneratedWorldData world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var builder = new StringBuilder(Header.Length + world.Cells.Count * 96);
            builder.Append(Header).Append("\r\n");

            foreach (var cell in world.Cells)
            {
                AppendField(builder, world.Seed.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, cell.Coordinate.X.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, cell.Coordinate.Y.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, GetRoleToken(cell.Role));
                builder.Append(',');
                AppendField(builder, cell.PrimaryBiomeId);
                builder.Append(',');
                AppendField(builder, cell.SecondaryBiomeId);
                builder.Append(',');
                AppendField(builder, cell.PatchId);
                builder.Append(',');
                AppendField(builder, cell.RouteMaskId);
                builder.Append(',');
                AppendField(builder, cell.SpecialSiteInstanceId);
                builder.Append(',');
                AppendField(builder, cell.BoundaryProfileId);
                builder.Append(',');
                AppendField(builder, cell.SectorRecipeId);
                builder.Append(',');
                AppendField(builder, cell.ShortestDistanceFromStart.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, cell.MandatoryGraphNode ? "1" : "0");
                builder.Append("\r\n");
            }

            var content = new UTF8Encoding(false, true).GetBytes(builder.ToString());
            var result = new byte[Utf8Bom.Length + content.Length];
            Buffer.BlockCopy(Utf8Bom, 0, result, 0, Utf8Bom.Length);
            Buffer.BlockCopy(content, 0, result, Utf8Bom.Length, content.Length);
            return result;
        }

        private static string GetRoleToken(GeneratedSectorRole role)
        {
            switch (role)
            {
                case GeneratedSectorRole.Unassigned:
                    return "UNASSIGNED";
                case GeneratedSectorRole.Mandatory:
                    return "MANDATORY";
                case GeneratedSectorRole.Type0:
                    return "TYPE0";
                case GeneratedSectorRole.ReservedSite:
                    return "RESERVED_SITE";
                case GeneratedSectorRole.InactiveBuffer:
                    return "INACTIVE_BUFFER";
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Undefined generated sector role.");
            }
        }

        private static void AppendField(StringBuilder builder, string value)
        {
            var requiresQuotes = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!requiresQuotes)
            {
                builder.Append(value);
                return;
            }

            builder.Append('"');
            foreach (var character in value)
            {
                if (character == '"')
                {
                    builder.Append("\"\"");
                }
                else
                {
                    builder.Append(character);
                }
            }

            builder.Append('"');
        }
    }
}
