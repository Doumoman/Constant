using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.TerrainClusters.Authoring
{
    public static class TerrainClusterAuthoringValidation
    {
        private static readonly Regex StableId = new Regex(
            "^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

        public static TerrainClusterAuthoringBuildResult Build(
            IEnumerable<TerrainClusterAuthoringRow> sourceRows)
        {
            var errors = new List<TerrainClusterAuthoringError>();
            var rows = (sourceRows ?? Array.Empty<TerrainClusterAuthoringRow>())
                .Where(value => value != null)
                .ToArray();
            var descriptors = V2AuthoringSchemaRegistry.DescribeDefaultTables()
                .Where(value => value.Owner == V2AuthoringOwner.TerrainCluster)
                .OrderBy(value => value.RelativeAuthoringPath, StringComparer.Ordinal)
                .ToArray();
            ValidateRows(rows, descriptors, errors);
            if (errors.Count > 0) return Failed(errors);

            var entries = new List<TerrainClusterAuthoringEntry>();
            var byPath = rows.GroupBy(value => value.TablePath, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.ToArray(), StringComparer.Ordinal);
            foreach (var catalogRow in byPath[Path("terrain_cluster_catalog_v2.csv")]
                         .OrderBy(value => value.Get("cluster_id"), StringComparer.Ordinal))
            {
                try
                {
                    var entry = BuildEntry(catalogRow, byPath, errors);
                    if (entry != null) entries.Add(entry);
                }
                catch (Exception exception)
                {
                    Add(errors, TerrainClusterAuthoringErrorCode.InvalidCatalog,
                        catalogRow, string.Empty, exception.GetType().Name + ": " + exception.Message);
                }
            }

            if (entries.Select(value => value.Id).Distinct().Count() != entries.Count)
            {
                Add(errors, TerrainClusterAuthoringErrorCode.InvalidCatalog,
                    Path("terrain_cluster_catalog_v2.csv"), 0, "cluster_id",
                    "Catalog cluster IDs must be unique.");
            }

            if (errors.Count > 0) return Failed(errors);
            var canonical = CanonicalContent(rows, descriptors);
            return new TerrainClusterAuthoringBuildResult(
                new TerrainClusterAuthoringCatalog(entries, canonical),
                Array.Empty<TerrainClusterAuthoringError>());
        }

        private static void ValidateRows(
            IReadOnlyList<TerrainClusterAuthoringRow> rows,
            IReadOnlyList<V2AuthoringTableDescriptor> descriptors,
            ICollection<TerrainClusterAuthoringError> errors)
        {
            var expected = descriptors.ToDictionary(
                value => value.RelativeAuthoringPath, value => value, StringComparer.Ordinal);
            foreach (var path in rows.Select(value => value.TablePath).Distinct(StringComparer.Ordinal))
            {
                if (!expected.ContainsKey(path))
                {
                    Add(errors, TerrainClusterAuthoringErrorCode.UnexpectedTable,
                        path, 0, string.Empty, "Only the exact 13 TerrainCluster schema paths are accepted.");
                }
            }

            foreach (var descriptor in descriptors)
            {
                var tableRows = rows.Where(value =>
                    string.Equals(value.TablePath, descriptor.RelativeAuthoringPath, StringComparison.Ordinal))
                    .ToArray();
                if (tableRows.Length == 0)
                {
                    Add(errors, TerrainClusterAuthoringErrorCode.MissingTable,
                        descriptor.RelativeAuthoringPath, 0, string.Empty, "Table has no data rows.");
                    continue;
                }

                foreach (var row in tableRows)
                {
                    foreach (var column in descriptor.Columns)
                    {
                        if (!row.Fields.ContainsKey(column.ColumnName))
                        {
                            Add(errors, TerrainClusterAuthoringErrorCode.InvalidField,
                                row, column.ColumnName, "Column is missing from the parsed row.");
                            continue;
                        }

                        var value = row.Get(column.ColumnName);
                        if (column.IsRequired && value.Length == 0)
                        {
                            Add(errors, TerrainClusterAuthoringErrorCode.InvalidField,
                                row, column.ColumnName, "Required value is empty.");
                            continue;
                        }

                        if (value.Length == 0) continue;
                        if (column.AllowedValues.Count > 0 &&
                            !column.AllowedValues.Contains(value, StringComparer.Ordinal))
                        {
                            Add(errors, TerrainClusterAuthoringErrorCode.InvalidToken,
                                row, column.ColumnName, "Token is not in the registered allowed-value set: " + value);
                        }

                        ValidateScalar(row, column, value, errors);
                    }
                }

                var primary = descriptor.Columns.Where(value => value.PrimaryKeyOrder.HasValue)
                    .OrderBy(value => value.PrimaryKeyOrder.Value).ToArray();
                foreach (var duplicate in tableRows.GroupBy(
                             row => string.Join("\u001f", primary.Select(column => row.Get(column.ColumnName))),
                             StringComparer.Ordinal).Where(value => value.Count() > 1))
                {
                    foreach (var row in duplicate)
                    {
                        Add(errors, TerrainClusterAuthoringErrorCode.DuplicatePrimaryKey,
                            row, string.Join(",", primary.Select(value => value.ColumnName)),
                            "Composite primary key occurs more than once.");
                    }
                }
            }

            // Structural table errors (including absent required tables) make the
            // foreign-key graph incomplete. Preserve atomic rejection instead of
            // indexing an incomplete graph and leaking KeyNotFoundException.
            if (errors.Count > 0) return;

            var descriptorByFile = descriptors.ToDictionary(value => value.FileName, StringComparer.Ordinal);
            var rowsByPath = rows.GroupBy(value => value.TablePath, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.ToArray(), StringComparer.Ordinal);
            foreach (var descriptor in descriptors)
            {
                foreach (var column in descriptor.Columns.Where(value =>
                             value.ForeignKey != null &&
                             value.ForeignKey.TargetDomain == V2AuthoringSchemaDomain.AuthoringV2))
                {
                    V2AuthoringTableDescriptor targetDescriptor;
                    if (!descriptorByFile.TryGetValue(column.ForeignKey.TargetFileName, out targetDescriptor))
                        continue;
                    var targetRows = rowsByPath[targetDescriptor.RelativeAuthoringPath];
                    foreach (var row in rowsByPath[descriptor.RelativeAuthoringPath])
                    {
                        var value = row.Get(column.ColumnName);
                        if (value.Length == 0) continue;
                        var matches = targetRows.Where(target =>
                            string.Equals(target.Get(column.ForeignKey.TargetColumnName), value,
                                StringComparison.Ordinal)).ToArray();
                        if (matches.Length == 0)
                        {
                            Add(errors, TerrainClusterAuthoringErrorCode.MissingForeignKey,
                                row, column.ColumnName, column.ForeignKey.ToString() + "=" + value);
                            continue;
                        }

                        foreach (var target in matches)
                        {
                            ValidateOwner(row, target, targetDescriptor, column.ColumnName, errors);
                        }
                    }
                }
            }
        }

        private static void ValidateScalar(
            TerrainClusterAuthoringRow row,
            V2AuthoringColumnDescriptor column,
            string value,
            ICollection<TerrainClusterAuthoringError> errors)
        {
            int ignored;
            switch (column.DataType)
            {
                case CsvSchemaDataType.Int:
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ignored))
                        Add(errors, TerrainClusterAuthoringErrorCode.InvalidToken,
                            row, column.ColumnName, "Expected invariant integer.");
                    break;
                case CsvSchemaDataType.Bool:
                    if (!string.Equals(value, "0", StringComparison.Ordinal) &&
                        !string.Equals(value, "1", StringComparison.Ordinal))
                        Add(errors, TerrainClusterAuthoringErrorCode.InvalidToken,
                            row, column.ColumnName, "Expected exact Boolean token 0 or 1.");
                    break;
                case CsvSchemaDataType.IntList:
                    var parts = value.Split('|');
                    if (parts.Length == 0 || parts.Any(part =>
                            !int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out ignored)) ||
                        parts.Distinct(StringComparer.Ordinal).Count() != parts.Length)
                        Add(errors, TerrainClusterAuthoringErrorCode.InvalidToken,
                            row, column.ColumnName, "Expected canonical duplicate-free integer list.");
                    break;
                case CsvSchemaDataType.Id:
                    if (!StableId.IsMatch(value))
                        Add(errors, TerrainClusterAuthoringErrorCode.InvalidToken,
                            row, column.ColumnName, "Expected stable ID token.");
                    break;
            }
        }

        private static void ValidateOwner(
            TerrainClusterAuthoringRow source,
            TerrainClusterAuthoringRow target,
            V2AuthoringTableDescriptor targetDescriptor,
            string column,
            ICollection<TerrainClusterAuthoringError> errors)
        {
            var sourceCluster = source.Get("cluster_id");
            var targetCluster = target.Get("cluster_id");
            if (sourceCluster.Length > 0 && targetCluster.Length > 0 &&
                !string.Equals(sourceCluster, targetCluster, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterAuthoringErrorCode.CrossOwnerReference,
                    source, column, sourceCluster + " references " + targetCluster);
            }

            // The catalog's spine_variant_id is the selected baseline, not the
            // catalog row's ownership dimension. Variant ownership applies only
            // when the referenced child table itself is variant-scoped.
            var sourceVariant = source.Get("spine_variant_id");
            var targetVariant = target.Get("spine_variant_id");
            if (!string.Equals(targetDescriptor.FileName,
                    "terrain_cluster_catalog_v2.csv", StringComparison.Ordinal) &&
                sourceVariant.Length > 0 && targetVariant.Length > 0 &&
                !string.Equals(sourceVariant, targetVariant, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterAuthoringErrorCode.CrossOwnerReference,
                    source, column, sourceVariant + " references " + targetVariant);
            }
        }

        private static TerrainClusterAuthoringEntry BuildEntry(
            TerrainClusterAuthoringRow catalogRow,
            IReadOnlyDictionary<string, TerrainClusterAuthoringRow[]> byPath,
            ICollection<TerrainClusterAuthoringError> errors)
        {
            var clusterId = catalogRow.Get("cluster_id");
            PacingRole pacing;
            MoonpalaceBiomeId biome;
            if (!PacingRoleTokenCodec.TryParse(catalogRow.Get("pacing_role"), out pacing) ||
                !MoonpalaceBiomeId.TryParse(catalogRow.Get("biome_id"), out biome))
            {
                Add(errors, TerrainClusterAuthoringErrorCode.InvalidToken,
                    catalogRow, "pacing_role/biome_id", "Typed pacing and biome tokens are required.");
                return null;
            }

            var clusterRows = byPath.ToDictionary(
                value => value.Key,
                value => value.Value.Where(row =>
                    string.Equals(row.Get("cluster_id"), clusterId, StringComparison.Ordinal)).ToArray(),
                StringComparer.Ordinal);
            var cells = clusterRows[Path("terrain_cluster_cells_v2.csv")];
            var chunks = cells.Select(row =>
                    new ClusterChunkCoord(Int(row, "chunk_x"), Int(row, "chunk_y")))
                .ToArray();
            if (chunks.Length < 2 || chunks.Length > 5 ||
                chunks.Distinct().Count() != chunks.Length ||
                chunks.Min(value => value.X) != 0 || chunks.Min(value => value.Y) != 0 ||
                Reachable(chunks).Count != chunks.Length ||
                cells.Any(row => row.Get("cell_role").Length != 0 ||
                                 row.Get("port_id").Length != 0 ||
                                 row.Get("access_class").Length != 0))
            {
                Add(errors, TerrainClusterAuthoringErrorCode.InvalidFootprint,
                    catalogRow, "footprint", "Footprint must be normalized, connected, unique, 2..5 chunks, with empty legacy summaries.");
                return null;
            }

            var variantRows = clusterRows[Path("terrain_cluster_variants_v2.csv")];
            var baselineId = catalogRow.Get("spine_variant_id");
            if (variantRows.Length < 2 ||
                variantRows.Count(value => string.Equals(
                    value.Get("spine_variant_id"), baselineId, StringComparison.Ordinal)) != 1)
            {
                Add(errors, TerrainClusterAuthoringErrorCode.InvalidVariant,
                    catalogRow, "spine_variant_id", "At least two variants and one exact catalog baseline are required.");
                return null;
            }

            var roleRows = clusterRows[Path("terrain_cluster_role_anchors_v2.csv")];
            var linkRows = clusterRows[Path("terrain_cluster_role_variant_links_v2.csv")];
            foreach (var variant in variantRows)
            {
                var variantId = variant.Get("spine_variant_id");
                foreach (var role in roleRows)
                {
                    if (linkRows.Count(link =>
                            string.Equals(link.Get("spine_variant_id"), variantId, StringComparison.Ordinal) &&
                            string.Equals(link.Get("role_anchor_id"), role.Get("role_anchor_id"),
                                StringComparison.Ordinal)) != 1)
                    {
                        Add(errors, TerrainClusterAuthoringErrorCode.InvalidRoleLink,
                            role, "role_anchor_id", "Every role must have one explicit link in every variant.");
                    }
                }
            }
            if (errors.Count > 0) return null;

            var runtimeRoleAnchor = roleRows.ToDictionary(
                row => row.Get("role_anchor_id"),
                row => "ANCHOR_" + TrimRolePrefix(row.Get("role_anchor_id")),
                StringComparer.Ordinal);
            var runtimeRoleNode = roleRows.ToDictionary(
                row => row.Get("role_anchor_id"),
                row => "NODE_" + TrimRolePrefix(row.Get("role_anchor_id")),
                StringComparer.Ordinal);
            var roles = roleRows.Select(row => new ClusterRoleAnchor(
                    runtimeRoleAnchor[row.Get("role_anchor_id")],
                    ParseRole(row.Get("role_kind")),
                    Tile(row, "local_x", "local_y"),
                    runtimeRoleNode[row.Get("role_anchor_id")]))
                .ToArray();

            var portAccess = new List<KeyValuePair<string, AccessClass>>();
            var ports = clusterRows[Path("terrain_cluster_ports_v2.csv")].Select(row =>
            {
                AccessClass access;
                if (!AccessClassTokenCodec.TryParse(row.Get("access_class"), out access))
                    throw new InvalidOperationException("Invalid AccessClass token.");
                portAccess.Add(new KeyValuePair<string, AccessClass>(row.Get("port_id"), access));
                var routeTypes = row.Get("compatible_route_types").Split('|')
                    .Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
                if (!routeTypes.SequenceEqual(routeTypes.OrderBy(value => value)) ||
                    routeTypes.Any(value => value < 0 || value > 4))
                    throw new InvalidOperationException("RouteTypes must be canonical ascending 0..4.");
                return new ClusterPort(
                    row.Get("port_id"), ParsePortKind(row.Get("port_kind")),
                    Bool(row, "is_primary"), runtimeRoleAnchor[row.Get("role_anchor_id")],
                    Tile(row, "local_x", "local_y"), ParseSide(row.Get("outward_side")), routeTypes);
            }).ToArray();
            if (ports.Count(value => value.IsPrimary && value.Kind == ClusterPortKind.Entry) != 1 ||
                ports.Count(value => value.IsPrimary && value.Kind == ClusterPortKind.Exit) != 1)
            {
                Add(errors, TerrainClusterAuthoringErrorCode.InvalidPort,
                    catalogRow, "ports", "Exact one primary Entry and Exit are required.");
                return null;
            }

            var nodeRows = clusterRows[Path("terrain_cluster_nodes_v2.csv")];
            var edgeRows = clusterRows[Path("terrain_cluster_spine_edges_v2.csv")];
            var envelopeRows = clusterRows[Path("terrain_cluster_envelope_cells_v2.csv")];
            var projectedByVariant = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            var variants = new List<SpineVariant>();
            foreach (var variantRow in variantRows)
            {
                var variantId = variantRow.Get("spine_variant_id");
                var variantLinks = linkRows.Where(value =>
                        string.Equals(value.Get("spine_variant_id"), variantId, StringComparison.Ordinal))
                    .ToDictionary(value => value.Get("node_id"), value => value.Get("role_anchor_id"),
                        StringComparer.Ordinal);
                var projection = new Dictionary<string, string>(StringComparer.Ordinal);
                var nodes = nodeRows.Where(value =>
                        string.Equals(value.Get("spine_variant_id"), variantId, StringComparison.Ordinal))
                    .Select(row =>
                    {
                        var authored = row.Get("node_id");
                        string anchor;
                        var projected = variantLinks.TryGetValue(authored, out anchor)
                            ? runtimeRoleNode[anchor]
                            : authored;
                        projection.Add(authored, projected);
                        return new TraversalNode(
                            projected, Tile(row, "local_x", "local_y"), Bool(row, "mandatory"),
                            anchor == null ? string.Empty : runtimeRoleAnchor[anchor]);
                    }).ToArray();
                projectedByVariant.Add(variantId, projection);

                var edges = edgeRows.Where(value =>
                        string.Equals(value.Get("spine_variant_id"), variantId, StringComparison.Ordinal))
                    .Select(row => BuildEdge(row, projection, envelopeRows)).ToArray();
                variants.Add(new SpineVariant(
                    new SpineVariantId(variantId),
                    string.Equals(variantId, baselineId, StringComparison.Ordinal),
                    TraversalGraphKind.Traversal, nodes, edges));
            }

            var contract = new TerrainClusterContract(
                new TerrainClusterId(clusterId), new ClusterFootprint(chunks),
                roles, ports, new TerrainClusterTraversalContract(variants), string.Empty);
            var validation = TerrainClusterContractValidator.Validate(contract);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    Add(errors, TerrainClusterAuthoringErrorCode.InvalidContract,
                        catalogRow, "contract", error.ToString());
                }
                return null;
            }

            var highDefinitions = clusterRows[Path("terrain_cluster_high_routes_v2.csv")]
                .Select(row =>
                {
                    var variantId = row.Get("spine_variant_id");
                    var projection = projectedByVariant[variantId];
                    var authoredHighId = row.Get("high_route_id");
                    var orderedEdges = clusterRows[Path("terrain_cluster_high_route_edges_v2.csv")]
                        .Where(value => string.Equals(value.Get("high_route_id"), authoredHighId, StringComparison.Ordinal))
                        .OrderBy(value => Int(value, "edge_order"))
                        .Select(value => value.Get("edge_id")).ToArray();
                    var benefits = clusterRows[Path("terrain_cluster_high_route_benefits_v2.csv")]
                        .Where(value => string.Equals(value.Get("high_route_id"), authoredHighId, StringComparison.Ordinal))
                        .Select(value => value.Get("benefit_id")).ToArray();
                    var failures = clusterRows[Path("terrain_cluster_high_route_failures_v2.csv")]
                        .Where(value => string.Equals(value.Get("high_route_id"), authoredHighId, StringComparison.Ordinal))
                        .Select(value => projection[value.Get("failure_node_id")]).ToArray();
                    if (orderedEdges.Length == 0 || benefits.Distinct(StringComparer.Ordinal).Count() < 2 ||
                        failures.Length == 0)
                        throw new InvalidOperationException("High routes require ordered edges, two benefits, and failure evidence.");
                    return new TerrainClusterHighRouteDefinition(
                        ProjectHighRouteId(authoredHighId), new SpineVariantId(variantId),
                        projection[row.Get("divergence_node_id")], orderedEdges,
                        projection[row.Get("rejoin_node_id")], projection[row.Get("high_point_node_id")],
                        benefits, failures);
                }).ToArray();
            if (highDefinitions.Length == 0)
            {
                Add(errors, TerrainClusterAuthoringErrorCode.InvalidHighRoute,
                    catalogRow, "high_route", "Every cluster requires an explicit high route.");
                return null;
            }

            var durations = edgeRows.Select(row => new TraversalEdgeDurationEvidence(
                new SpineVariantId(row.Get("spine_variant_id")), row.Get("edge_id"),
                Int(row, "estimated_duration_ms"), row.Get("timing_ruleset_id"))).ToArray();
            var routeIntent = new TerrainClusterRouteWitnessIntent(
                new SpineVariantId(baselineId), highDefinitions, durations);
            var signature = StructuralSignature(
                chunks, ports, variants, highDefinitions, nodeRows, baselineId);
            return new TerrainClusterAuthoringEntry(
                contract, pacing, biome, catalogRow.Get("footprint_variant_id"),
                new SpineVariantId(baselineId), routeIntent, portAccess, signature);
        }

        private static TraversalEdge BuildEdge(
            TerrainClusterAuthoringRow row,
            IReadOnlyDictionary<string, string> projection,
            IReadOnlyList<TerrainClusterAuthoringRow> allEnvelopeRows)
        {
            var envelopeRows = allEnvelopeRows.Where(value =>
                    string.Equals(value.Get("edge_id"), row.Get("edge_id"), StringComparison.Ordinal))
                .ToArray();
            IEnumerable<LocalTileCoord> Kind(string token)
            {
                return envelopeRows.Where(value =>
                        string.Equals(value.Get("envelope_kind"), token, StringComparison.Ordinal))
                    .Select(value => Tile(value, "local_x", "local_y"));
            }

            var envelope = new TraversalEnvelope(
                Kind("CENTERLINE"), Kind("FLOOR"), Kind("CLEARANCE"),
                Kind("JUMP_ARC"), Kind("DROP_COLUMN"), Kind("LANDING"), Kind("RECOVERY"));
            if (!envelope.Centerline.Any() || !envelope.Floor.Any() ||
                !envelope.Clearance.Any() || !envelope.Landing.Any() || !envelope.Recovery.Any())
                throw new InvalidOperationException("Walk edge envelope evidence is incomplete.");
            var landing = Int(row, "landing_width") > 0
                ? (LocalTileCoord?)Tile(row, "landing_x", "landing_y")
                : null;
            var recovery = Int(row, "recovery_width") > 0
                ? (LocalTileCoord?)Tile(row, "recovery_x", "recovery_y")
                : null;
            return new TraversalEdge(
                row.Get("edge_id"), projection[row.Get("from_node_id")],
                projection[row.Get("to_node_id")], ParseMovement(row.Get("movement")),
                Tile(row, "start_x", "start_y"), Tile(row, "end_x", "end_y"),
                Int(row, "clearance_width"), Int(row, "clearance_height"),
                landing, recovery, Bool(row, "mandatory"), envelope, TraversalGraphKind.Traversal);
        }

        private static string StructuralSignature(
            IEnumerable<ClusterChunkCoord> chunks,
            IEnumerable<ClusterPort> ports,
            IEnumerable<SpineVariant> variants,
            IEnumerable<TerrainClusterHighRouteDefinition> highRoutes,
            IEnumerable<TerrainClusterAuthoringRow> nodeRows,
            string baselineId)
        {
            var text = new StringBuilder();
            foreach (var chunk in chunks.OrderBy(value => value))
                text.Append("C:").Append(chunk.X).Append(',').Append(chunk.Y).Append(';');
            foreach (var port in ports.OrderBy(value => value.Kind))
                text.Append("P:").Append(port.Kind).Append(':').Append(port.OutwardSide)
                    .Append(':').Append(port.Tile.X).Append(',').Append(port.Tile.Y).Append(';');
            foreach (var variant in variants.OrderBy(value => value.Id))
            {
                text.Append("V:").Append(variant.Id.Value).Append(':')
                    .Append(string.Equals(variant.Id.Value, baselineId, StringComparison.Ordinal)).Append(';');
                foreach (var edge in variant.Edges)
                    text.Append("E:").Append(edge.MovementKind).Append(':')
                        .Append(edge.StartTile.X).Append(',').Append(edge.StartTile.Y).Append('>')
                        .Append(edge.EndTile.X).Append(',').Append(edge.EndTile.Y).Append(';');
            }
            foreach (var high in highRoutes.OrderBy(value => value.HighRouteId, StringComparer.Ordinal))
                text.Append("H:").Append(high.OrderedEdgeIds.Count).Append(':')
                    .Append(high.BenefitIds.Count).Append(':').Append(high.FailureNodeIds.Count).Append(';');
            var highY = nodeRows.Where(value => value.Get("node_id").EndsWith("_HIGH", StringComparison.Ordinal))
                .Select(value => Int(value, "local_y")).DefaultIfEmpty(0).Max();
            text.Append("Y:").Append(highY);
            using (var sha256 = SHA256.Create())
                return string.Concat(sha256.ComputeHash(new UTF8Encoding(false).GetBytes(text.ToString()))
                    .Select(value => value.ToString("x2")));
        }

        private static string TrimRolePrefix(string roleAnchorId)
        {
            const string prefix = "ROLE_";
            return roleAnchorId != null && roleAnchorId.StartsWith(prefix, StringComparison.Ordinal)
                ? roleAnchorId.Substring(prefix.Length)
                : roleAnchorId ?? string.Empty;
        }

        private static string ProjectHighRouteId(string authoredHighRouteId)
        {
            const string authoredPrefix = "HIGH_";
            const string runtimePrefix = "HIGH_ROUTE_";
            return authoredHighRouteId != null &&
                   authoredHighRouteId.StartsWith(authoredPrefix, StringComparison.Ordinal) &&
                   !authoredHighRouteId.StartsWith(runtimePrefix, StringComparison.Ordinal)
                ? runtimePrefix + authoredHighRouteId.Substring(authoredPrefix.Length)
                : authoredHighRouteId ?? string.Empty;
        }

        private static string CanonicalContent(
            IEnumerable<TerrainClusterAuthoringRow> rows,
            IEnumerable<V2AuthoringTableDescriptor> descriptors)
        {
            var text = new StringBuilder();
            foreach (var descriptor in descriptors.OrderBy(
                         value => value.RelativeAuthoringPath, StringComparer.Ordinal))
            {
                var primary = descriptor.Columns.Where(value => value.PrimaryKeyOrder.HasValue)
                    .OrderBy(value => value.PrimaryKeyOrder.Value).ToArray();
                foreach (var row in rows.Where(value =>
                             string.Equals(value.TablePath, descriptor.RelativeAuthoringPath,
                                 StringComparison.Ordinal))
                         .OrderBy(value => string.Join("\u001f",
                             primary.Select(column => CanonicalScalar(
                                 value.Get(column.ColumnName), column.DataType))), StringComparer.Ordinal))
                {
                    text.Append(descriptor.RelativeAuthoringPath).Append('|');
                    foreach (var column in descriptor.Columns.OrderBy(value => value.ColumnOrder))
                        text.Append(column.ColumnName).Append('=').Append(row.Get(column.ColumnName)).Append('|');
                    text.Append('\n');
                }
            }
            return text.ToString();
        }

        private static string CanonicalScalar(string value, CsvSchemaDataType dataType)
        {
            int parsed;
            return dataType == CsvSchemaDataType.Int &&
                   int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? (parsed < 0 ? "0" : "1") + Math.Abs((long)parsed).ToString("D12", CultureInfo.InvariantCulture)
                : value;
        }

        private static HashSet<ClusterChunkCoord> Reachable(IEnumerable<ClusterChunkCoord> source)
        {
            var active = new HashSet<ClusterChunkCoord>(source);
            var reached = new HashSet<ClusterChunkCoord>();
            if (active.Count == 0) return reached;
            var queue = new Queue<ClusterChunkCoord>();
            queue.Enqueue(active.First());
            reached.Add(active.First());
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in new[]
                         {
                             new ClusterChunkCoord(current.X - 1, current.Y),
                             new ClusterChunkCoord(current.X + 1, current.Y),
                             new ClusterChunkCoord(current.X, current.Y - 1),
                             new ClusterChunkCoord(current.X, current.Y + 1),
                         })
                    if (active.Contains(next) && reached.Add(next)) queue.Enqueue(next);
            }
            return reached;
        }

        private static TerrainClusterAuthoringBuildResult Failed(
            ICollection<TerrainClusterAuthoringError> errors)
        {
            if (!errors.Any(value => value.Code == TerrainClusterAuthoringErrorCode.AtomicPublishRejected))
            {
                Add(errors, TerrainClusterAuthoringErrorCode.AtomicPublishRejected,
                    string.Empty, 0, "catalog", "Errors rejected atomic TerrainCluster publication.");
            }
            return new TerrainClusterAuthoringBuildResult(null, errors);
        }

        private static string Path(string fileName)
        {
            return "TerrainCluster/" + fileName;
        }

        private static int Int(TerrainClusterAuthoringRow row, string column)
        {
            return int.Parse(row.Get(column), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static bool Bool(TerrainClusterAuthoringRow row, string column)
        {
            return string.Equals(row.Get(column), "1", StringComparison.Ordinal);
        }

        private static LocalTileCoord Tile(
            TerrainClusterAuthoringRow row,
            string xColumn,
            string yColumn)
        {
            return new LocalTileCoord(Int(row, xColumn), Int(row, yColumn));
        }

        private static ClusterRoleKind ParseRole(string token)
        {
            switch (token)
            {
                case "ENTRY": return ClusterRoleKind.Entry;
                case "BUILD_UP": return ClusterRoleKind.BuildUp;
                case "CORE": return ClusterRoleKind.Core;
                case "RECOVERY": return ClusterRoleKind.Recovery;
                case "REWARD": return ClusterRoleKind.Reward;
                case "EXIT": return ClusterRoleKind.Exit;
                default: throw new ArgumentOutOfRangeException(nameof(token), token, null);
            }
        }

        private static ClusterPortKind ParsePortKind(string token)
        {
            return token == "ENTRY" ? ClusterPortKind.Entry : ClusterPortKind.Exit;
        }

        private static ClusterPortSide ParseSide(string token)
        {
            switch (token)
            {
                case "L": return ClusterPortSide.L;
                case "R": return ClusterPortSide.R;
                case "U": return ClusterPortSide.U;
                case "D": return ClusterPortSide.D;
                default: throw new ArgumentOutOfRangeException(nameof(token), token, null);
            }
        }

        private static TraversalMovementKind ParseMovement(string token)
        {
            switch (token)
            {
                case "WALK": return TraversalMovementKind.Walk;
                case "JUMP": return TraversalMovementKind.Jump;
                case "DROP": return TraversalMovementKind.Drop;
                case "CLIMB": return TraversalMovementKind.Climb;
                case "SLIDE": return TraversalMovementKind.Slide;
                case "BOUNCE": return TraversalMovementKind.Bounce;
                default: throw new ArgumentOutOfRangeException(nameof(token), token, null);
            }
        }

        private static void Add(
            ICollection<TerrainClusterAuthoringError> errors,
            TerrainClusterAuthoringErrorCode code,
            TerrainClusterAuthoringRow row,
            string column,
            string detail)
        {
            Add(errors, code, row.TablePath, row.RecordNumber, column, detail);
        }

        private static void Add(
            ICollection<TerrainClusterAuthoringError> errors,
            TerrainClusterAuthoringErrorCode code,
            string path,
            int record,
            string column,
            string detail)
        {
            errors.Add(new TerrainClusterAuthoringError(code, path, record, column, detail));
        }
    }
}
