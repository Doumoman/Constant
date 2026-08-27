using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.EventOverlays
{
    internal static class EventOverlayCanonicalDigest
    {
        public static string Compute(EventOverlayContract contract)
        {
            var material = new StringBuilder();
            Append(material, "EVENT", contract.Id.Value, Number((int)contract.Kind),
                contract.TerrainClusterId.Value,
                contract.ActivityStructureId.HasValue ? contract.ActivityStructureId.Value.Value : "NONE");
            foreach (var assignment in contract.Assignments.OrderBy(value => value.TargetMarkerId)
                         .ThenBy(value => (int)value.Operation).ThenBy(value => value.PayloadId, System.StringComparer.Ordinal))
            {
                Append(material, "MARKER", assignment.TargetMarkerId.Value,
                    Number((int)assignment.Operation), assignment.PayloadId);
            }

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(material.ToString());
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
            }
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static void Append(StringBuilder material, params string[] fields)
        {
            if (material.Length != 0) material.Append('\n');
            material.Append(string.Join("|", fields));
        }
    }
}
