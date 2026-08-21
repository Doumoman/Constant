using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StarNight.Map.WorldGeneration.Generation
{
    public static class GeneratedWorldEdgesCsvSerializer
    {
        public const string FileName = "generated_world_edges.csv";
        public const string Header = "seed,from_sector_x,from_sector_y,side,to_sector_x,to_sector_y,edge_layer,traversal_kind,open,edge_signature_id,cost_tiles";
        private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };

        public static byte[] Serialize(IEnumerable<GeneratedWorldEdge> edges)
        {
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            var values = new List<GeneratedWorldEdge>(edges);
            values.Sort(Compare);
            var builder = new StringBuilder(Header.Length + values.Count * 96);
            builder.Append(Header).Append("\r\n");
            foreach (var edge in values)
            {
                if (edge == null) throw new ArgumentException("Edges cannot contain null.", nameof(edges));
                Append(builder, edge.Seed.ToString(CultureInfo.InvariantCulture)); builder.Append(',');
                Append(builder, edge.From.X.ToString(CultureInfo.InvariantCulture)); builder.Append(',');
                Append(builder, edge.From.Y.ToString(CultureInfo.InvariantCulture)); builder.Append(',');
                Append(builder, edge.Side); builder.Append(',');
                Append(builder, edge.To.X.ToString(CultureInfo.InvariantCulture)); builder.Append(',');
                Append(builder, edge.To.Y.ToString(CultureInfo.InvariantCulture)); builder.Append(',');
                Append(builder, edge.EdgeLayer); builder.Append(',');
                Append(builder, edge.TraversalKind); builder.Append(',');
                Append(builder, edge.Open ? "1" : "0"); builder.Append(',');
                Append(builder, edge.EdgeSignatureId); builder.Append(',');
                Append(builder, edge.CostTiles.ToString(CultureInfo.InvariantCulture)); builder.Append("\r\n");
            }
            var content = new UTF8Encoding(false, true).GetBytes(builder.ToString());
            var result = new byte[Utf8Bom.Length + content.Length];
            Buffer.BlockCopy(Utf8Bom, 0, result, 0, Utf8Bom.Length);
            Buffer.BlockCopy(content, 0, result, Utf8Bom.Length, content.Length);
            return result;
        }

        internal static int Compare(GeneratedWorldEdge left, GeneratedWorldEdge right)
        {
            var order = WorldGridIndex.ToIndex(left.From).CompareTo(WorldGridIndex.ToIndex(right.From));
            if (order != 0) return order;
            order = SideOrder(left.Side).CompareTo(SideOrder(right.Side));
            return order != 0 ? order : WorldGridIndex.ToIndex(left.To).CompareTo(WorldGridIndex.ToIndex(right.To));
        }

        private static int SideOrder(string side) => side == "L" ? 0 : side == "R" ? 1 : side == "U" ? 2 : 3;
        private static void Append(StringBuilder builder, string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) { builder.Append(value); return; }
            builder.Append('"');
            foreach (var c in value) builder.Append(c == '"' ? "\"\"" : c.ToString());
            builder.Append('"');
        }
    }
}
