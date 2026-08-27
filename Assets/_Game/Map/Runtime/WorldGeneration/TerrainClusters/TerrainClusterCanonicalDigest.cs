using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    internal static class TerrainClusterCanonicalDigest
    {
        public static string Compute(TerrainClusterContract contract)
        {
            var material = new StringBuilder();
            Append(material, "ID", contract.Id.Value);
            Append(
                material,
                "MICRO_CHUNK_SIZE",
                Number(WorldGenConstants.MicroChunkWidthTiles),
                Number(WorldGenConstants.MicroChunkHeightTiles));

            foreach (var chunk in contract.Footprint.ActiveChunks.OrderBy(value => value))
            {
                Append(material, "CHUNK", Number(chunk.X), Number(chunk.Y));
            }

            foreach (var role in contract.RoleAnchors.OrderBy(value => value.AnchorId, System.StringComparer.Ordinal))
            {
                Append(
                    material,
                    "ROLE",
                    role.AnchorId,
                    Number((int)role.Role),
                    Coordinate(role.Tile),
                    role.TraversalNodeId);
            }

            foreach (var port in contract.Ports.OrderBy(value => value.PortId, System.StringComparer.Ordinal))
            {
                Append(
                    material,
                    "PORT",
                    port.PortId,
                    Number((int)port.Kind),
                    port.IsPrimary ? "1" : "0",
                    port.RoleAnchorId,
                    Coordinate(port.Tile),
                    Number((int)port.OutwardSide),
                    string.Join(",", port.CompatibleRouteTypes.OrderBy(value => value).Select(Number)));
            }

            foreach (var variant in contract.Traversal.Variants.OrderBy(value => value.Id))
            {
                Append(
                    material,
                    "VARIANT",
                    variant.Id.Value,
                    variant.IsBaseline ? "1" : "0",
                    Number((int)variant.GraphKind));

                foreach (var node in variant.Nodes.OrderBy(value => value.NodeId, System.StringComparer.Ordinal))
                {
                    Append(
                        material,
                        "NODE",
                        variant.Id.Value,
                        node.NodeId,
                        Number((int)node.GraphKind),
                        Coordinate(node.Tile),
                        node.IsMandatory ? "1" : "0",
                        node.RoleAnchorId);
                }

                foreach (var edge in variant.Edges.OrderBy(value => value.EdgeId, System.StringComparer.Ordinal))
                {
                    Append(
                        material,
                        "EDGE",
                        variant.Id.Value,
                        edge.EdgeId,
                        Number((int)edge.GraphKind),
                        edge.FromNodeId,
                        edge.ToNodeId,
                        Number((int)edge.MovementKind),
                        Coordinate(edge.StartTile),
                        Coordinate(edge.EndTile),
                        Number(edge.MinimumClearanceWidth),
                        Number(edge.MinimumClearanceHeight),
                        NullableCoordinate(edge.LandingTile),
                        NullableCoordinate(edge.RecoveryTile),
                        edge.IsMandatory ? "1" : "0");
                    AppendSet(material, variant.Id.Value, edge.EdgeId, "CENTERLINE", edge.Envelope.Centerline);
                    AppendSet(material, variant.Id.Value, edge.EdgeId, "FLOOR", edge.Envelope.Floor);
                    AppendSet(material, variant.Id.Value, edge.EdgeId, "CLEARANCE", edge.Envelope.Clearance);
                    AppendSet(material, variant.Id.Value, edge.EdgeId, "JUMP_ARC", edge.Envelope.JumpArc);
                    AppendSet(material, variant.Id.Value, edge.EdgeId, "DROP_COLUMN", edge.Envelope.DropColumn);
                    AppendSet(material, variant.Id.Value, edge.EdgeId, "LANDING", edge.Envelope.Landing);
                    AppendSet(material, variant.Id.Value, edge.EdgeId, "RECOVERY", edge.Envelope.Recovery);
                }
            }

            var bytes = Encoding.UTF8.GetBytes(material.ToString());
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
            }
        }

        private static void AppendSet(
            StringBuilder material,
            string variantId,
            string edgeId,
            string setName,
            IEnumerable<LocalTileCoord> coordinates)
        {
            Append(
                material,
                "ENVELOPE",
                variantId,
                edgeId,
                setName,
                string.Join(",", coordinates
                    .OrderBy(value => value.Y)
                    .ThenBy(value => value.X)
                    .Select(Coordinate)));
        }

        private static string NullableCoordinate(LocalTileCoord? coordinate)
        {
            return coordinate.HasValue ? Coordinate(coordinate.Value) : "NONE";
        }

        private static string Coordinate(LocalTileCoord coordinate)
        {
            return Number(coordinate.X) + "," + Number(coordinate.Y);
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static void Append(StringBuilder material, params string[] fields)
        {
            if (material.Length != 0) material.Append('\n');
            material.Append(string.Join("|", fields));
        }
    }
}
