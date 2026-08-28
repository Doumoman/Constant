using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public enum TerrainClusterPatternZoneKind
    {
        GeometryAdd = 1,
        GeometryCarve = 2,
        Affordance = 3,
        Marker = 4,
        AbsoluteProtected = 5,
    }

    public enum TerrainClusterPatternProtectionEvidenceKind
    {
        RouteSpine = 1,
        TraversalEnvelope = 2,
        BaselineWitness = 3,
        HighRouteWitness = 4,
        RecoveryWitness = 5,
        EntryAnchor = 6,
        ExitAnchor = 7,
        RecoveryAnchor = 8,
    }

    public sealed class TerrainClusterPatternProtectionEvidence :
        IEquatable<TerrainClusterPatternProtectionEvidence>,
        IComparable<TerrainClusterPatternProtectionEvidence>
    {
        internal TerrainClusterPatternProtectionEvidence(
            LocalTileCoord coordinate,
            TerrainClusterPatternProtectionEvidenceKind kind,
            string sourceIdentity,
            string stableSourceId,
            ClusterTraversalProtectedTileProvenance traversalProvenance)
        {
            Coordinate = coordinate;
            Kind = kind;
            SourceIdentity = sourceIdentity ?? string.Empty;
            StableSourceId = stableSourceId ?? string.Empty;
            TraversalProvenance = traversalProvenance;
        }

        public LocalTileCoord Coordinate { get; }
        public TerrainClusterPatternProtectionEvidenceKind Kind { get; }
        public string SourceIdentity { get; }
        public string StableSourceId { get; }
        public ClusterTraversalProtectedTileProvenance TraversalProvenance { get; }
        public bool IsTraversalAuthority => TraversalProvenance != null;

        public int CompareTo(TerrainClusterPatternProtectionEvidence other)
        {
            if (other == null) return -1;
            var comparison = Coordinate.Y.CompareTo(other.Coordinate.Y);
            if (comparison != 0) return comparison;
            comparison = Coordinate.X.CompareTo(other.Coordinate.X);
            if (comparison != 0) return comparison;
            comparison = ((int)Kind).CompareTo((int)other.Kind);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(StableSourceId, other.StableSourceId, StringComparison.Ordinal);
        }

        public bool Equals(TerrainClusterPatternProtectionEvidence other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj) => Equals(obj as TerrainClusterPatternProtectionEvidence);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Kind + "|" + SourceIdentity + "|" + StableSourceId;
    }

    public sealed class TerrainClusterPatternZoneCell
    {
        private readonly ReadOnlyCollection<TerrainClusterPatternZoneKind> kinds;
        private readonly ReadOnlyCollection<TerrainClusterPatternProtectionEvidence> protectionEvidence;

        public TerrainClusterPatternZoneCell(
            LocalTileCoord coordinate,
            TerrainClusterPatternZoneKind kind)
            : this(coordinate, new[] { kind }, Array.Empty<TerrainClusterPatternProtectionEvidence>())
        {
        }

        internal TerrainClusterPatternZoneCell(
            LocalTileCoord coordinate,
            IEnumerable<TerrainClusterPatternZoneKind> kinds,
            IEnumerable<TerrainClusterPatternProtectionEvidence> protectionEvidence)
        {
            Coordinate = coordinate;
            this.kinds = new ReadOnlyCollection<TerrainClusterPatternZoneKind>(
                (kinds ?? Array.Empty<TerrainClusterPatternZoneKind>()).Distinct().OrderBy(value => value).ToArray());
            this.protectionEvidence = new ReadOnlyCollection<TerrainClusterPatternProtectionEvidence>(
                (protectionEvidence ?? Array.Empty<TerrainClusterPatternProtectionEvidence>())
                    .Where(value => value != null).Distinct().OrderBy(value => value).ToArray());
        }

        public LocalTileCoord Coordinate { get; }
        public IReadOnlyList<TerrainClusterPatternZoneKind> Kinds => kinds;
        public IReadOnlyList<TerrainClusterPatternProtectionEvidence> ProtectionEvidence => protectionEvidence;
        public bool HasKind(TerrainClusterPatternZoneKind kind) => kinds.Contains(kind);
    }

    public sealed class PatternZoneMap
    {
        private readonly ReadOnlyCollection<TerrainClusterPatternZoneCell> cells;
        private readonly ReadOnlyCollection<MicroPatternProtectedCell> protectedCells;
        private readonly ReadOnlyDictionary<LocalTileCoord, TerrainClusterPatternZoneCell> byCoordinate;

        internal PatternZoneMap(
            TerrainClusterId clusterId,
            string localCanvasDigest,
            string traversalCompilationDigest,
            string routeWitnessDigest,
            IEnumerable<TerrainClusterPatternZoneCell> cells,
            IEnumerable<MicroPatternProtectedCell> protectedCells,
            string canonicalDigest)
        {
            ClusterId = clusterId;
            LocalCanvasDigest = localCanvasDigest ?? string.Empty;
            TraversalCompilationDigest = traversalCompilationDigest ?? string.Empty;
            RouteWitnessDigest = routeWitnessDigest ?? string.Empty;
            var copy = (cells ?? Array.Empty<TerrainClusterPatternZoneCell>())
                .Where(value => value != null)
                .OrderBy(value => value.Coordinate.Y).ThenBy(value => value.Coordinate.X).ToArray();
            this.cells = new ReadOnlyCollection<TerrainClusterPatternZoneCell>(copy);
            byCoordinate = new ReadOnlyDictionary<LocalTileCoord, TerrainClusterPatternZoneCell>(
                copy.ToDictionary(value => value.Coordinate));
            var protectedCopy = (protectedCells ?? Array.Empty<MicroPatternProtectedCell>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.protectedCells = new ReadOnlyCollection<MicroPatternProtectedCell>(protectedCopy);
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public TerrainClusterId ClusterId { get; }
        public string LocalCanvasDigest { get; }
        public string TraversalCompilationDigest { get; }
        public string RouteWitnessDigest { get; }
        public IReadOnlyList<TerrainClusterPatternZoneCell> Cells => cells;
        public IReadOnlyList<MicroPatternProtectedCell> MicroPatternProtectedCells => protectedCells;
        public string CanonicalDigest { get; }
        public int AbsoluteProtectedCoordinateCount => cells.Count(value => value.HasKind(TerrainClusterPatternZoneKind.AbsoluteProtected));

        public bool TryGetCell(LocalTileCoord coordinate, out TerrainClusterPatternZoneCell cell)
        {
            return byCoordinate.TryGetValue(coordinate, out cell);
        }
    }

    public sealed class TerrainClusterPatternPlacementIntent
    {
        public TerrainClusterPatternPlacementIntent(
            string placementId,
            MicroPatternId patternId,
            MicroPatternTransform transform,
            LocalTileCoord origin,
            string expectedDefinitionDigest)
        {
            PlacementId = placementId ?? string.Empty;
            PatternId = patternId;
            Transform = transform;
            Origin = origin;
            ExpectedDefinitionDigest = expectedDefinitionDigest ?? string.Empty;
        }

        public string PlacementId { get; }
        public MicroPatternId PatternId { get; }
        public MicroPatternTransform Transform { get; }
        public LocalTileCoord Origin { get; }
        public string ExpectedDefinitionDigest { get; }
        public string ApplicationIdentity => PlacementId + "/" + PatternId.Value + "/" + Transform + "/" +
            Origin.X.ToString(CultureInfo.InvariantCulture) + "," + Origin.Y.ToString(CultureInfo.InvariantCulture);
    }

    internal sealed class TerrainClusterPatternZoneBuildResult
    {
        public TerrainClusterPatternZoneBuildResult(
            PatternZoneMap map,
            IEnumerable<TerrainClusterPatternRenderError> errors)
        {
            Errors = (errors ?? Array.Empty<TerrainClusterPatternRenderError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            Map = Errors.Count == 0 ? map : null;
        }

        public PatternZoneMap Map { get; }
        public IReadOnlyList<TerrainClusterPatternRenderError> Errors { get; }
    }

    internal static class TerrainClusterPatternZoneBuilder
    {
        public static TerrainClusterPatternZoneBuildResult Build(
            TerrainClusterLocalCanvas localCanvas,
            TerrainClusterTraversalCompilation traversal,
            TerrainClusterRouteWitnessReport routeWitness,
            IEnumerable<TerrainClusterPatternZoneCell> authoredZones)
        {
            var errors = new List<TerrainClusterPatternRenderError>();
            var active = localCanvas.TileCells
                .Where(value => value.State == ClusterChunkMaskState.Active)
                .Select(value => value.Coordinate).ToHashSet();
            var shell = routeWitness.StaticShell.Cells.ToDictionary(value => value.CompiledCoordinate);
            foreach (var coordinate in active.Where(value => !shell.ContainsKey(value)).OrderBy(value => value.Y).ThenBy(value => value.X))
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.WorkingCanvasCoverageMismatch,
                    CoordinatePath(coordinate), "Active Local Canvas coordinate is missing from Static Shell.");
            }
            foreach (var coordinate in shell.Keys.Where(value => !active.Contains(value)).OrderBy(value => value.Y).ThenBy(value => value.X))
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.WorkingCanvasCoverageMismatch,
                    CoordinatePath(coordinate), "Static Shell coordinate is not active in Local Canvas.");
            }

            var evidenceByCoordinate = new Dictionary<LocalTileCoord, List<TerrainClusterPatternProtectionEvidence>>();
            var microProtected = new List<MicroPatternProtectedCell>();
            foreach (var protectedTile in traversal.ProtectedTiles)
            {
                if (!active.Contains(protectedTile.CompiledCoordinate))
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.InvalidZoneCoordinate,
                        CoordinatePath(protectedTile.CompiledCoordinate), "MAP11_03 protected coordinate is outside active Local Canvas.");
                    continue;
                }

                foreach (var provenance in protectedTile.Provenance)
                {
                    var kind = provenance.SourceKind == ClusterTraversalProtectionSourceKind.RouteSpine
                        ? TerrainClusterPatternProtectionEvidenceKind.RouteSpine
                        : TerrainClusterPatternProtectionEvidenceKind.TraversalEnvelope;
                    var identity = TraversalIdentity(provenance);
                    var stableId = StableSourceId("TCPS", identity);
                    AddEvidence(evidenceByCoordinate, new TerrainClusterPatternProtectionEvidence(
                        protectedTile.CompiledCoordinate, kind, identity, stableId, provenance));
                    var microKind = provenance.SourceKind == ClusterTraversalProtectionSourceKind.RouteSpine
                        ? MicroPatternProtectedSourceKind.RouteSpine
                        : MicroPatternProtectedSourceKind.TraversalEnvelope;
                    microProtected.Add(new MicroPatternProtectedCell(
                        protectedTile.CompiledCoordinate, microKind, stableId));
                }
            }

            var protectedCoordinates = evidenceByCoordinate.Keys.ToHashSet();
            AddWitnessEvidence(routeWitness, evidenceByCoordinate, protectedCoordinates, errors);

            var authoredByCoordinate = new Dictionary<LocalTileCoord, HashSet<TerrainClusterPatternZoneKind>>();
            var snapshot = authoredZones == null ? Array.Empty<TerrainClusterPatternZoneCell>() : authoredZones.ToArray();
            for (var index = 0; index < snapshot.Length; index++)
            {
                var authored = snapshot[index];
                if (authored == null)
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.MissingInput,
                        "authoredZones[" + Number(index) + "]", "Zone cell is required.");
                    continue;
                }

                if (!active.Contains(authored.Coordinate))
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.InvalidZoneCoordinate,
                        CoordinatePath(authored.Coordinate), "Authored zone coordinate is not active.");
                }

                foreach (var kind in authored.Kinds)
                {
                    if (kind < TerrainClusterPatternZoneKind.GeometryAdd || kind > TerrainClusterPatternZoneKind.Marker)
                    {
                        Add(errors, TerrainClusterPatternRenderErrorCode.InvalidZoneCoordinate,
                            CoordinatePath(authored.Coordinate), "AbsoluteProtected is compiled, not authored: " + Number((int)kind));
                        continue;
                    }
                    if (!authoredByCoordinate.TryGetValue(authored.Coordinate, out var kinds))
                    {
                        kinds = new HashSet<TerrainClusterPatternZoneKind>();
                        authoredByCoordinate.Add(authored.Coordinate, kinds);
                    }
                    kinds.Add(kind);
                }
            }

            foreach (var pair in authoredByCoordinate.OrderBy(value => value.Key.Y).ThenBy(value => value.Key.X))
            {
                if (protectedCoordinates.Contains(pair.Key))
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.ProtectedZoneOverlap,
                        CoordinatePath(pair.Key), "Authored zone overlaps AbsoluteProtected.");
                }
                if (pair.Value.Contains(TerrainClusterPatternZoneKind.GeometryAdd) &&
                    pair.Value.Contains(TerrainClusterPatternZoneKind.GeometryCarve))
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.ConflictingGeometryZone,
                        CoordinatePath(pair.Key), "GeometryAdd and GeometryCarve are mutually exclusive.");
                }
                if (pair.Value.Contains(TerrainClusterPatternZoneKind.GeometryAdd) &&
                    shell.TryGetValue(pair.Key, out var shellCell) &&
                    shellCell.Occupancy != TerrainClusterShellOccupancy.Air)
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.InvalidZoneCoordinate,
                        CoordinatePath(pair.Key), "GeometryAdd requires pre-render Static Shell Air.");
                }
            }

            if (errors.Count != 0) return new TerrainClusterPatternZoneBuildResult(null, errors);

            var allCoordinates = authoredByCoordinate.Keys.Concat(protectedCoordinates).Distinct()
                .OrderBy(value => value.Y).ThenBy(value => value.X).ToArray();
            var cells = new List<TerrainClusterPatternZoneCell>();
            foreach (var coordinate in allCoordinates)
            {
                var kinds = authoredByCoordinate.TryGetValue(coordinate, out var authoredKinds)
                    ? authoredKinds.ToList()
                    : new List<TerrainClusterPatternZoneKind>();
                if (protectedCoordinates.Contains(coordinate)) kinds.Add(TerrainClusterPatternZoneKind.AbsoluteProtected);
                var evidence = evidenceByCoordinate.TryGetValue(coordinate, out var sources)
                    ? sources
                    : new List<TerrainClusterPatternProtectionEvidence>();
                cells.Add(new TerrainClusterPatternZoneCell(coordinate, kinds, evidence));
            }

            var digest = ComputeDigest(localCanvas, traversal, routeWitness, cells, microProtected);
            return new TerrainClusterPatternZoneBuildResult(
                new PatternZoneMap(localCanvas.ClusterId, localCanvas.CanonicalDigest,
                    traversal.CanonicalDigest, routeWitness.CanonicalDigest, cells, microProtected, digest),
                errors);
        }

        private static void AddWitnessEvidence(
            TerrainClusterRouteWitnessReport report,
            IDictionary<LocalTileCoord, List<TerrainClusterPatternProtectionEvidence>> target,
            ISet<LocalTileCoord> protectedCoordinates,
            ICollection<TerrainClusterPatternRenderError> errors)
        {
            AddWitnessCoordinates(target, protectedCoordinates, errors,
                report.BaselineRoute.CoveredProtectedTiles,
                TerrainClusterPatternProtectionEvidenceKind.BaselineWitness,
                "BASELINE|" + report.BaselineRoute.VariantId.Value);
            foreach (var high in report.HighRoutes)
            {
                AddWitnessCoordinates(target, protectedCoordinates, errors, high.CoveredProtectedTiles,
                    TerrainClusterPatternProtectionEvidenceKind.HighRouteWitness,
                    "HIGH|" + high.HighRouteId + "|" + high.VariantId.Value);
            }
            foreach (var recovery in report.RecoveryRoutes)
            {
                var identity = "RECOVERY|" + recovery.HighRouteId + "|" + recovery.FailureNodeId + "|" + recovery.TargetBaselineNodeId;
                AddWitnessCoordinates(target, protectedCoordinates, errors, recovery.CoveredProtectedTiles,
                    TerrainClusterPatternProtectionEvidenceKind.RecoveryWitness, identity);
                if (recovery.CompiledCoordinates.Count != 0)
                {
                    AddWitnessCoordinates(target, protectedCoordinates, errors,
                        new[] { recovery.CompiledCoordinates[0], recovery.CompiledCoordinates[recovery.CompiledCoordinates.Count - 1] },
                        TerrainClusterPatternProtectionEvidenceKind.RecoveryAnchor, identity + "|ANCHOR");
                }
            }
            if (report.BaselineRoute.CompiledCoordinates.Count != 0)
            {
                AddWitnessCoordinates(target, protectedCoordinates, errors,
                    new[] { report.BaselineRoute.CompiledCoordinates[0] },
                    TerrainClusterPatternProtectionEvidenceKind.EntryAnchor,
                    "ENTRY|" + report.BaselineRoute.EntryPortId + "|" + report.BaselineRoute.EntryNodeId);
                AddWitnessCoordinates(target, protectedCoordinates, errors,
                    new[] { report.BaselineRoute.CompiledCoordinates[report.BaselineRoute.CompiledCoordinates.Count - 1] },
                    TerrainClusterPatternProtectionEvidenceKind.ExitAnchor,
                    "EXIT|" + report.BaselineRoute.ExitPortId + "|" + report.BaselineRoute.ExitNodeId);
            }
        }

        private static void AddWitnessCoordinates(
            IDictionary<LocalTileCoord, List<TerrainClusterPatternProtectionEvidence>> target,
            ISet<LocalTileCoord> protectedCoordinates,
            ICollection<TerrainClusterPatternRenderError> errors,
            IEnumerable<LocalTileCoord> coordinates,
            TerrainClusterPatternProtectionEvidenceKind kind,
            string identity)
        {
            foreach (var coordinate in (coordinates ?? Array.Empty<LocalTileCoord>()).Distinct()
                         .OrderBy(value => value.Y).ThenBy(value => value.X))
            {
                if (!protectedCoordinates.Contains(coordinate))
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.ProtectedEvidenceMismatch,
                        CoordinatePath(coordinate), kind + " witness has no MAP11_03 protection evidence.");
                    continue;
                }
                AddEvidence(target, new TerrainClusterPatternProtectionEvidence(
                    coordinate, kind, identity, StableSourceId("TCPW", kind + "|" + identity + "|" + Coordinate(coordinate)), null));
            }
        }

        private static void AddEvidence(
            IDictionary<LocalTileCoord, List<TerrainClusterPatternProtectionEvidence>> target,
            TerrainClusterPatternProtectionEvidence evidence)
        {
            if (!target.TryGetValue(evidence.Coordinate, out var list))
            {
                list = new List<TerrainClusterPatternProtectionEvidence>();
                target.Add(evidence.Coordinate, list);
            }
            list.Add(evidence);
        }

        private static string ComputeDigest(
            TerrainClusterLocalCanvas localCanvas,
            TerrainClusterTraversalCompilation traversal,
            TerrainClusterRouteWitnessReport routeWitness,
            IEnumerable<TerrainClusterPatternZoneCell> cells,
            IEnumerable<MicroPatternProtectedCell> protectedCells)
        {
            var material = new StringBuilder();
            Append(material, "CLUSTER", localCanvas.ClusterId.Value);
            Append(material, "CANVAS", localCanvas.CanonicalDigest);
            Append(material, "TRAVERSAL", traversal.CanonicalDigest);
            Append(material, "WITNESS", routeWitness.CanonicalDigest);
            foreach (var cell in cells.OrderBy(value => value.Coordinate.Y).ThenBy(value => value.Coordinate.X))
            {
                Append(material, "CELL", Number(cell.Coordinate.X), Number(cell.Coordinate.Y),
                    string.Join(",", cell.Kinds.Select(value => value.ToString())));
                foreach (var evidence in cell.ProtectionEvidence) Append(material, "EVIDENCE", evidence.ToString());
            }
            foreach (var cell in protectedCells.Distinct().OrderBy(value => value)) Append(material, "MAP10_PROTECTED", cell.ToString());
            return Sha256(material.ToString());
        }

        private static string TraversalIdentity(ClusterTraversalProtectedTileProvenance value)
        {
            return value.SourceKind + "|" + value.VariantId.Value + "|" + value.NodeId + "|" +
                value.EdgeId + "|" + (value.EnvelopeSetKind.HasValue ? value.EnvelopeSetKind.Value.ToString() : string.Empty) + "|" +
                Coordinate(value.SourceCoordinate) + "|" + Coordinate(value.CompiledCoordinate) + "|" +
                (value.IsMandatory ? "MANDATORY" : "OPTIONAL");
        }

        private static string StableSourceId(string prefix, string material)
        {
            return prefix + "_" + Sha256(material).Substring(0, 24).ToUpperInvariant();
        }

        private static string Sha256(string material)
        {
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(material ?? string.Empty))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static void Append(StringBuilder target, params string[] fields)
        {
            foreach (var field in fields)
            {
                var value = field ?? string.Empty;
                target.Append(value.Length.ToString(CultureInfo.InvariantCulture));
                target.Append(':');
                target.Append(value);
            }
            target.Append('\n');
        }

        private static void Add(
            ICollection<TerrainClusterPatternRenderError> errors,
            TerrainClusterPatternRenderErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new TerrainClusterPatternRenderError(code, path, detail));
        }

        private static string CoordinatePath(LocalTileCoord value) => "zones[" + Coordinate(value) + "]";
        private static string Coordinate(LocalTileCoord value) => Number(value.X) + "," + Number(value.Y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
