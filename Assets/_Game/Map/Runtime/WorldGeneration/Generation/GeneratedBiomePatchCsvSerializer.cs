using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StarNight.Map.WorldGeneration.Generation
{
    public static class GeneratedBiomePatchCsvSerializer
    {
        public const string FileName = "generated_biome_patches.csv";

        public const string Header =
            "seed,patch_instance_id,biome_id,patch_role,seed_sector_x,seed_sector_y,sector_count,min_x,min_y,max_x,max_y,perimeter_edges,special_map_instance_ids";

        private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };

        public static byte[] Serialize(IEnumerable<GeneratedBiomePatchRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var ordered = new List<GeneratedBiomePatchRow>(rows);
            ordered.Sort((left, right) =>
            {
                if (left == null) return right == null ? 0 : -1;
                if (right == null) return 1;
                return left.PatchInstanceId.CompareTo(right.PatchInstanceId);
            });
            for (var index = 0; index < ordered.Count; index++)
            {
                if (ordered[index] == null)
                    throw new ArgumentException("Patch rows cannot contain null.", nameof(rows));
                if (index > 0 && ordered[index].PatchInstanceId == ordered[index - 1].PatchInstanceId)
                    throw new ArgumentException("Patch row IDs must be unique.", nameof(rows));
                if (index > 0 && ordered[index].Seed != ordered[0].Seed)
                    throw new ArgumentException("Patch rows must share one world seed.", nameof(rows));
            }

            var builder = new StringBuilder(Header.Length + ordered.Count * 128);
            builder.Append(Header).Append("\r\n");
            foreach (var row in ordered)
            {
                AppendField(builder, row.Seed.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, row.PatchInstanceId.Value);
                builder.Append(',');
                AppendField(builder, row.BiomeId);
                builder.Append(',');
                AppendField(builder, BiomePatchRoleTokenCodec.ToToken(row.PatchRole));
                builder.Append(',');
                AppendField(builder, row.SeedSectorX.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, row.SeedSectorY.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, row.SectorCount.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, row.MinX.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, row.MinY.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, row.MaxX.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, row.MaxY.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, row.PerimeterEdges.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                AppendField(builder, JoinSpecialMapIds(row.SpecialMapInstanceIds));
                builder.Append("\r\n");
            }

            var content = new UTF8Encoding(false, true).GetBytes(builder.ToString());
            var result = new byte[Utf8Bom.Length + content.Length];
            Buffer.BlockCopy(Utf8Bom, 0, result, 0, Utf8Bom.Length);
            Buffer.BlockCopy(content, 0, result, Utf8Bom.Length, content.Length);
            return result;
        }

        private static string JoinSpecialMapIds(IReadOnlyList<SiteReservationId> ids)
        {
            if (ids.Count == 0) return string.Empty;
            var builder = new StringBuilder();
            for (var index = 0; index < ids.Count; index++)
            {
                if (index > 0) builder.Append('|');
                builder.Append(ids[index].Value);
            }
            return builder.ToString();
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
                if (character == '"') builder.Append("\"\"");
                else builder.Append(character);
            }
            builder.Append('"');
        }
    }
}
