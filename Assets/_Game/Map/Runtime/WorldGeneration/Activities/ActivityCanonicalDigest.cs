using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Activities
{
    internal static class ActivityCanonicalDigest
    {
        public static string Compute(ActivityStructureContract contract)
        {
            var material = new StringBuilder();
            Append(material, "ACTIVITY", contract.Id.Value, contract.TerrainClusterId.Value,
                contract.CompatibleSpineVariantId.Value);
            Append(material, "PACING", string.Join(",", contract.CompatiblePacingRoles
                .OrderBy(value => (int)value).Select(value => Number((int)value))));
            Append(material, "ACCESS", string.Join(",", contract.CompatibleAccessClasses
                .OrderBy(value => (int)value).Select(value => Number((int)value))));

            foreach (var slot in contract.Slots.OrderBy(value => value.Id))
                Append(material, "SLOT", slot.Id.Value, Number((int)slot.Kind), Coordinate(slot.Tile), slot.MarkerId);
            foreach (var cue in contract.Cues.OrderBy(value => (int)value.Kind).ThenBy(value => value.SlotId))
                Append(material, "CUE", Number((int)cue.Kind), cue.SlotId.Value,
                    cue.DetectableBeforeActivation ? "1" : "0");

            Append(material, "MECHANISM", Number((int)contract.MechanismGraph.GraphKind));
            foreach (var node in contract.MechanismGraph.Nodes.OrderBy(value => value.NodeId, System.StringComparer.Ordinal))
                Append(material, "MECH_NODE", node.NodeId, Number((int)node.Kind), node.SlotId.Value,
                    Number((int)node.GraphKind));
            foreach (var edge in contract.MechanismGraph.Edges.OrderBy(value => value.EdgeId, System.StringComparer.Ordinal))
                Append(material, "MECH_EDGE", edge.EdgeId, edge.FromNodeId, edge.ToNodeId,
                    Number((int)edge.Relation), Number((int)edge.GraphKind));

            Append(material, "PROGRESSION", contract.ProgressionGraph.StartNodeId,
                contract.ProgressionGraph.TerminalNodeId, Number((int)contract.ProgressionGraph.GraphKind));
            foreach (var node in contract.ProgressionGraph.Nodes.OrderBy(value => value.NodeId, System.StringComparer.Ordinal))
                Append(material, "PROG_NODE", node.NodeId, Number((int)node.Phase), Number((int)node.GraphKind));
            foreach (var edge in contract.ProgressionGraph.Edges.OrderBy(value => value.EdgeId, System.StringComparer.Ordinal))
                Append(material, "PROG_EDGE", edge.EdgeId, edge.FromNodeId, edge.ToNodeId,
                    Number((int)edge.Kind), Number((int)edge.GraphKind));

            var safety = contract.RemovalSafety;
            Append(material, "REMOVAL", safety.BaselineSpineVariantId.Value,
                safety.EntryTraversalNodeId, safety.ExitTraversalNodeId,
                safety.PreserveStaticTraversal ? "1" : "0", safety.PreserveAccessClass ? "1" : "0",
                safety.PermanentSolidMutationAllowed ? "1" : "0",
                safety.MandatoryExitDestructionAllowed ? "1" : "0",
                Number(safety.RouteTypeBeforeRemoval), Number(safety.RouteTypeAfterRemoval),
                Number((int)safety.AccessClassBeforeRemoval), Number((int)safety.AccessClassAfterRemoval),
                safety.TraversalDigestBeforeRemoval, safety.TraversalDigestAfterRemoval);
            AppendSet(material, "SAFE_POCKET", safety.SafePocketTiles);
            AppendSet(material, "RECOVERY", safety.RecoveryTiles);
            AppendSet(material, "PERMANENT_SOLID_WRITES", safety.PermanentSolidWriteTiles);

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(material.ToString());
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
            }
        }

        private static void AppendSet(StringBuilder material, string name, System.Collections.Generic.IEnumerable<LocalTileCoord> tiles)
        {
            Append(material, name, string.Join(",", tiles.OrderBy(value => value.Y).ThenBy(value => value.X).Select(Coordinate)));
        }

        private static string Coordinate(LocalTileCoord value) => Number(value.X) + "," + Number(value.Y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static void Append(StringBuilder material, params string[] fields)
        {
            if (material.Length != 0) material.Append('\n');
            material.Append(string.Join("|", fields));
        }
    }
}
