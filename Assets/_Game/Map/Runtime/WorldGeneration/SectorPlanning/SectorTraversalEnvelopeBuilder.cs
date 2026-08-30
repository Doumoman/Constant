using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorTraversalEnvelopeBuilder
    {
        public const string ReferenceEnvelopePublicationLabel = "REFERENCE TRAVERSAL ENVELOPE";

        public static SectorSpineEnvelopeBuildResult Build(SectorSpineEnvelopeBuildRequest request, SectorSpineGraph graph)
        {
            var errors = new List<SectorSpineEnvelopeError>();
            ValidateInput(request, graph, errors);
            if (request == null || request.AnchorPlan == null || request.ClusterPlacementPlan == null || graph == null)
                return Failure(errors);

            var cells = new Dictionary<string, SectorTraversalEnvelopeCell>(StringComparer.Ordinal);
            foreach (var edge in graph.Edges)
            {
                var blocking = SectorSpineEnvelopeAnchorUtility.BlockingCells(request.AnchorPlan, edge.SectorIndex);
                foreach (var coordinate in edge.CenterlineCells)
                {
                    AddCell(cells, edge, coordinate, SectorTraversalEnvelopeCellKind.Centerline);
                    TryAddDerived(cells, edge, new LocalTileCoord(coordinate.X, coordinate.Y + 1), SectorTraversalEnvelopeCellKind.Floor, blocking);
                    TryAddDerived(cells, edge, new LocalTileCoord(coordinate.X, coordinate.Y - edge.ClearanceHeight), SectorTraversalEnvelopeCellKind.Clearance, blocking);
                    if (edge.Kind == SectorSpineEdgeKind.Recovery)
                        AddCell(cells, edge, coordinate, SectorTraversalEnvelopeCellKind.Recovery);
                    if (request.AnchorPlan.Anchors.Any(anchor => anchor.SectorIndex == edge.SectorIndex
                                                                && SectorSpineEnvelopeAnchorUtility.IsCompatible(anchor)
                                                                && SectorSpineEnvelopeAnchorUtility.Contains(anchor, coordinate)))
                        AddCell(cells, edge, coordinate, SectorTraversalEnvelopeCellKind.ProtectedAnchorBridge);
                }
                if (edge.CenterlineCells.Count > 0)
                {
                    AddCell(cells, edge, edge.CenterlineCells[0], SectorTraversalEnvelopeCellKind.Landing);
                    AddCell(cells, edge, edge.CenterlineCells[edge.CenterlineCells.Count - 1], SectorTraversalEnvelopeCellKind.Landing);
                }
            }

            ValidateDerivedCells(request, graph, cells.Values, errors);
            if (errors.Count > 0) return Failure(errors);

            var protectedSource = cells.Values.Where(value => value.Kind == SectorTraversalEnvelopeCellKind.Centerline
                                                               || value.Kind == SectorTraversalEnvelopeCellKind.Clearance
                                                               || value.Kind == SectorTraversalEnvelopeCellKind.Landing
                                                               || value.Kind == SectorTraversalEnvelopeCellKind.Recovery
                                                               || value.Kind == SectorTraversalEnvelopeCellKind.ProtectedAnchorBridge)
                .OrderBy(value => value).ToArray();
            var protectedByCoordinate = new Dictionary<string, SectorTraversalEnvelopeCell>(StringComparer.Ordinal);
            foreach (var source in protectedSource)
            {
                var key = CoordinateKey(source.SectorIndex, source.Coordinate);
                if (!protectedByCoordinate.ContainsKey(key))
                    protectedByCoordinate.Add(key, new SectorTraversalEnvelopeCell(source.SectorIndex, source.Coordinate,
                        SectorTraversalEnvelopeCellKind.ProtectedOpen, source.EdgeId, source.SourceIdentity));
            }
            foreach (var value in protectedByCoordinate.Values) cells[CellKey(value.SectorIndex, value.Coordinate, value.Kind)] = value;

            var allCells = cells.Values.OrderBy(value => value).ToArray();
            var protectedCells = protectedByCoordinate.Values.OrderBy(value => value).ToArray();
            ValidateProtectedSet(request, allCells, protectedCells, errors);
            if (errors.Count > 0) return Failure(errors);

            var compatibleOverlaps = protectedCells.Count(cell => request.AnchorPlan.Anchors.Any(anchor => anchor.SectorIndex == cell.SectorIndex
                && SectorSpineEnvelopeAnchorUtility.IsCompatible(anchor)
                && SectorSpineEnvelopeAnchorUtility.Contains(anchor, cell.Coordinate)));
            var blockingOverlaps = protectedCells.Count(cell => SectorSpineEnvelopeAnchorUtility.BlockingCells(request.AnchorPlan, cell.SectorIndex).Contains(cell.Coordinate));
            if (blockingOverlaps != 0)
            {
                Add(errors, SectorSpineEnvelopeErrorCode.EnvelopeOverlapsBlockingAnchor, "protectedOpen", "ProtectedOpen cannot overlap blocking fixed anchors.");
                return Failure(errors);
            }

            var envelopeDigest = SectorSpineEnvelopeCanonicalDigest.ComputeEnvelope(allCells, protectedCells);
            var provisional = new SectorSpineEnvelopePlan(graph, request.EnvelopePublicationLabel, allCells, protectedCells,
                compatibleOverlaps, blockingOverlaps, envelopeDigest, string.Empty);
            var planDigest = SectorSpineEnvelopeCanonicalDigest.ComputePlan(provisional);
            if (request.ExpectedCanonicalDigest.Length != 0 && !string.Equals(request.ExpectedCanonicalDigest, planDigest, StringComparison.Ordinal))
            {
                Add(errors, SectorSpineEnvelopeErrorCode.NonCanonicalPublication, "digest", "Spine-envelope plan digest does not match the expected canonical digest.");
                return Failure(errors);
            }
            var plan = new SectorSpineEnvelopePlan(graph, request.EnvelopePublicationLabel, allCells, protectedCells,
                compatibleOverlaps, blockingOverlaps, envelopeDigest, planDigest);
            return new SectorSpineEnvelopeBuildResult(plan, Array.Empty<SectorSpineEnvelopeError>());
        }

        private static void ValidateInput(
            SectorSpineEnvelopeBuildRequest request,
            SectorSpineGraph graph,
            ICollection<SectorSpineEnvelopeError> errors)
        {
            if (request == null)
            {
                Add(errors, SectorSpineEnvelopeErrorCode.MissingInput, "request", "A spine-envelope build request is required.");
                return;
            }
            if (request.AnchorPlan == null) Add(errors, SectorSpineEnvelopeErrorCode.MissingAnchorPlan, "anchorPlan", "SectorFixedAnchorPlan is required.");
            if (request.ClusterPlacementPlan == null) Add(errors, SectorSpineEnvelopeErrorCode.MissingClusterPlacementPlan, "clusterPlacementPlan", "SectorClusterPlacementPlan is required.");
            if (graph == null) Add(errors, SectorSpineEnvelopeErrorCode.MissingInput, "graph", "A successful spine graph is required.");
            if (!string.Equals(request.EnvelopePublicationLabel, ReferenceEnvelopePublicationLabel, StringComparison.Ordinal))
                Add(errors, SectorSpineEnvelopeErrorCode.NonCanonicalPublication, "envelopePublicationLabel", "Envelope publication must be marked REFERENCE TRAVERSAL ENVELOPE.");
            if (graph != null)
            {
                if (!string.Equals(graph.PublicationLabel, SectorSpineGraphBuilder.ReferenceGraphPublicationLabel, StringComparison.Ordinal)
                    || !string.Equals(graph.CanonicalDigest, SectorSpineEnvelopeCanonicalDigest.ComputeGraph(graph), StringComparison.Ordinal))
                    Add(errors, SectorSpineEnvelopeErrorCode.NonCanonicalPublication, "graph", "Spine graph must rebuild its canonical digest exactly.");
                if (request.Input == null || !string.Equals(graph.PlannerInputDigest, request.Input.CanonicalDigest, StringComparison.Ordinal)
                    || request.AnchorPlan == null || !string.Equals(graph.AnchorPlanDigest, request.AnchorPlan.CanonicalDigest, StringComparison.Ordinal)
                    || request.ClusterPlacementPlan == null || !string.Equals(graph.ClusterPlacementPlanDigest, request.ClusterPlacementPlan.CanonicalDigest, StringComparison.Ordinal))
                    Add(errors, SectorSpineEnvelopeErrorCode.SectorMismatch, "graph", "Graph source identities must match the build request.");
            }
        }

        private static void ValidateDerivedCells(
            SectorSpineEnvelopeBuildRequest request,
            SectorSpineGraph graph,
            IEnumerable<SectorTraversalEnvelopeCell> sourceCells,
            ICollection<SectorSpineEnvelopeError> errors)
        {
            var cells = sourceCells.ToArray();
            foreach (var cell in cells.Where(value => !Inside(value.Coordinate)))
                Add(errors, SectorSpineEnvelopeErrorCode.EnvelopeOutOfBounds, CellKey(cell.SectorIndex, cell.Coordinate, cell.Kind), "Envelope cells must remain inside 48x32.");
            foreach (var cell in cells)
            {
                var blocking = SectorSpineEnvelopeAnchorUtility.BlockingCells(request.AnchorPlan, cell.SectorIndex);
                if (blocking.Contains(cell.Coordinate))
                    Add(errors, SectorSpineEnvelopeErrorCode.EnvelopeOverlapsBlockingAnchor, CellKey(cell.SectorIndex, cell.Coordinate, cell.Kind), "Envelope cannot overlap SpecialFootprint, SiteReservation, or another incompatible anchor.");
            }
            foreach (var edge in graph.Edges)
            {
                if (!cells.Any(value => value.EdgeId == edge.EdgeId && value.Kind == SectorTraversalEnvelopeCellKind.Clearance))
                    Add(errors, SectorSpineEnvelopeErrorCode.EnvelopeMissingClearance, edge.EdgeId, "Every reference edge requires at least one in-bounds clearance cell.");
                var landings = cells.Count(value => value.EdgeId == edge.EdgeId && value.Kind == SectorTraversalEnvelopeCellKind.Landing);
                if (landings == 0)
                    Add(errors, SectorSpineEnvelopeErrorCode.EnvelopeMissingLanding, edge.EdgeId, "Every reference edge requires endpoint landing evidence.");
            }
        }

        private static void ValidateProtectedSet(
            SectorSpineEnvelopeBuildRequest request,
            IReadOnlyList<SectorTraversalEnvelopeCell> allCells,
            IReadOnlyList<SectorTraversalEnvelopeCell> protectedCells,
            ICollection<SectorSpineEnvelopeError> errors)
        {
            var required = new HashSet<string>(allCells.Where(value => value.Kind == SectorTraversalEnvelopeCellKind.Centerline
                                                                       || value.Kind == SectorTraversalEnvelopeCellKind.Clearance
                                                                       || value.Kind == SectorTraversalEnvelopeCellKind.Landing
                                                                       || value.Kind == SectorTraversalEnvelopeCellKind.Recovery
                                                                       || value.Kind == SectorTraversalEnvelopeCellKind.ProtectedAnchorBridge)
                .Select(value => CoordinateKey(value.SectorIndex, value.Coordinate)), StringComparer.Ordinal);
            var actual = new HashSet<string>(protectedCells.Select(value => CoordinateKey(value.SectorIndex, value.Coordinate)), StringComparer.Ordinal);
            if (!required.SetEquals(actual) || protectedCells.Any(value => value.Kind != SectorTraversalEnvelopeCellKind.ProtectedOpen))
                Add(errors, SectorSpineEnvelopeErrorCode.ProtectedSetMismatch, "protectedOpen", "ProtectedOpen must equal centerline + clearance + landing + recovery + anchor bridge coordinates.");

            foreach (var placement in request.ClusterPlacementPlan.Placements)
            {
                var chosen = new HashSet<LocalTileCoord>(placement.TileRects.SelectMany(rect =>
                    Enumerable.Range(rect.Y, rect.Height).SelectMany(y => Enumerable.Range(rect.X, rect.Width).Select(x => new LocalTileCoord(x, y)))));
                var clusterNodes = allCells.Where(value => value.SectorIndex == placement.SectorIndex && value.Kind == SectorTraversalEnvelopeCellKind.Centerline).ToArray();
                if (clusterNodes.Length == 0 || !clusterNodes.Any(value => chosen.Contains(value.Coordinate)))
                    Add(errors, SectorSpineEnvelopeErrorCode.EdgeCrossesUnplacedCluster, Subject(placement.SectorIndex), "Reference route must enter the selected cluster footprint and cannot claim an unplaced cluster footprint.");
            }
        }

        private static void TryAddDerived(
            IDictionary<string, SectorTraversalEnvelopeCell> cells,
            SectorSpineEdge edge,
            LocalTileCoord coordinate,
            SectorTraversalEnvelopeCellKind kind,
            ISet<LocalTileCoord> blocking)
        {
            if (Inside(coordinate) && !blocking.Contains(coordinate)) AddCell(cells, edge, coordinate, kind);
        }

        private static void AddCell(
            IDictionary<string, SectorTraversalEnvelopeCell> cells,
            SectorSpineEdge edge,
            LocalTileCoord coordinate,
            SectorTraversalEnvelopeCellKind kind)
        {
            var key = EdgeCellKey(edge, coordinate, kind);
            if (!cells.ContainsKey(key)) cells.Add(key, new SectorTraversalEnvelopeCell(edge.SectorIndex, coordinate, kind, edge.EdgeId, edge.SourceIdentity));
        }

        private static bool Inside(LocalTileCoord value)
            => value.X >= 0 && value.X < WorldGenConstants.SectorWidthTiles && value.Y >= 0 && value.Y < WorldGenConstants.SectorHeightTiles;

        private static string CoordinateKey(int sectorIndex, LocalTileCoord coordinate)
            => sectorIndex.ToString("D3", CultureInfo.InvariantCulture) + ":" + coordinate.X.ToString(CultureInfo.InvariantCulture) + "," + coordinate.Y.ToString(CultureInfo.InvariantCulture);

        private static string CellKey(int sectorIndex, LocalTileCoord coordinate, SectorTraversalEnvelopeCellKind kind)
            => CoordinateKey(sectorIndex, coordinate) + ":" + kind;

        private static string EdgeCellKey(SectorSpineEdge edge, LocalTileCoord coordinate, SectorTraversalEnvelopeCellKind kind)
            => CellKey(edge.SectorIndex, coordinate, kind) + ":" + edge.EdgeId;

        private static string Subject(int sectorIndex) => sectorIndex.ToString("D3", CultureInfo.InvariantCulture);
        private static void Add(ICollection<SectorSpineEnvelopeError> errors, SectorSpineEnvelopeErrorCode code, string subject, string detail)
            => errors.Add(new SectorSpineEnvelopeError(code, subject, detail));
        private static SectorSpineEnvelopeBuildResult Failure(IEnumerable<SectorSpineEnvelopeError> errors)
            => new SectorSpineEnvelopeBuildResult(null, errors);
    }
}
