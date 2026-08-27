using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    internal static class MicroPatternCanonicalDigest
    {
        public static string Compute(MicroPatternDefinition definition)
        {
            var material = new StringBuilder();
            AppendRecord(material, "ID", definition.Id.Value);
            AppendRecord(material, "SIZE", Number(definition.Width), Number(definition.Height));
            AppendRecord(material, "WEIGHT", Number(definition.Weight));
            AppendRecord(
                material,
                "BIOMES",
                string.Join(",", definition.AllowedBiomes
                    .OrderBy(value => value.CanonicalId, System.StringComparer.Ordinal)
                    .Select(value => value.CanonicalId)));
            AppendRecord(
                material,
                "TRANSFORMS",
                string.Join(",", definition.AllowedTransforms
                    .OrderBy(value => (int)value)
                    .Select(value => value.ToString())));
            AppendRecord(material, "PROTECTED", definition.ProtectedPolicy.ToString());

            var cells = definition.Cells.ToDictionary(value => value.Coordinate);
            for (var index = 0; index < MicroPatternDefinition.RequiredCellCount; index++)
            {
                var coordinate = new LocalTileCoord(
                    index % MicroPatternDefinition.RequiredWidth,
                    index / MicroPatternDefinition.RequiredWidth);
                var instructions = cells[coordinate].Instructions.ToDictionary(value => value.Layer);
                for (var layerValue = (int)MicroPatternLayer.Geometry;
                     layerValue <= (int)MicroPatternLayer.Marker;
                     layerValue++)
                {
                    var layer = (MicroPatternLayer)layerValue;
                    MicroPatternInstruction instruction;
                    var operation = instructions.TryGetValue(layer, out instruction)
                        ? instruction.Operation
                        : MicroPatternOperation.NoChange;
                    var payload = instruction == null ? string.Empty : instruction.PayloadId;
                    AppendRecord(
                        material,
                        "CELL",
                        Number(index),
                        layer.ToString(),
                        operation.ToString(),
                        payload);
                }
            }

            var bytes = Encoding.UTF8.GetBytes(material.ToString());
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
            }
        }

        private static void AppendRecord(StringBuilder material, params string[] fields)
        {
            if (material.Length != 0) material.Append('\n');
            material.Append(string.Join("|", fields));
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
