using System;
using System.Collections.Generic;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class HorizontalBackboneRouter
    {
        public HorizontalBackboneBuildResult Build(
            MandatoryConnectorTree connectorTree,
            MandatoryRouteMaskLookup routeMaskLookup,
            SiteReservationSnapshot siteSnapshot,
            BiomePatchValidationPublication biomePublication)
        {
            var errors = new List<HorizontalBackboneBuildError>();
            if (connectorTree == null) errors.Add(Error(HorizontalBackboneBuildErrorCode.MissingInput, "CONNECTOR_TREE", string.Empty, -1, "Connector tree is required."));
            if (routeMaskLookup == null) errors.Add(Error(HorizontalBackboneBuildErrorCode.MissingInput, "ROUTE_MASK_LOOKUP", string.Empty, -1, "Route mask lookup is required."));
            if (siteSnapshot == null) errors.Add(Error(HorizontalBackboneBuildErrorCode.MissingInput, "SITE_SNAPSHOT", string.Empty, -1, "Site snapshot is required."));
            if (biomePublication == null) errors.Add(Error(HorizontalBackboneBuildErrorCode.MissingInput, "BIOME_PUBLICATION", string.Empty, -1, "Biome publication is required."));
            if (connectorTree != null) ValidateTree(connectorTree, errors);
            if (routeMaskLookup != null) ValidateLookup(routeMaskLookup, errors);
            if (siteSnapshot != null) ValidateSite(siteSnapshot, errors);
            if (biomePublication != null) ValidateBiome(biomePublication, errors);
            if (connectorTree != null && routeMaskLookup != null && !ReferenceEquals(connectorTree.SourceRouteMaskLookup, routeMaskLookup))
                errors.Add(Error(HorizontalBackboneBuildErrorCode.InvalidRouteMaskLookup, "SOURCE_ROUTE_MASK_LOOKUP", string.Empty, -1, "Connector tree must preserve the exact route-mask lookup."));
            if (connectorTree != null && siteSnapshot != null && !ReferenceEquals(connectorTree.SourceTerminalSet.SourceSiteSnapshot, siteSnapshot))
                errors.Add(Error(HorizontalBackboneBuildErrorCode.InvalidSiteSnapshot, "SOURCE_SITE_SNAPSHOT", string.Empty, -1, "Connector tree terminals must preserve the exact site snapshot."));
            if (connectorTree != null && biomePublication != null && !ReferenceEquals(connectorTree.SourceTerminalSet.SourceBiomePublication, biomePublication))
                errors.Add(Error(HorizontalBackboneBuildErrorCode.InvalidBiomePublication, "SOURCE_BIOME_PUBLICATION", string.Empty, -1, "Connector tree terminals must preserve the exact biome publication."));
            if (errors.Count > 0) return Invalid(errors);

            var sourceTreeEdgeCount = connectorTree.TreeEdgeCount;
            var sourceSiteSectorCount = siteSnapshot.Sectors.Count;
            var sourceBiomeSectorCount = biomePublication.Snapshot.Sectors.Count;
            var segments = new List<HorizontalBackboneSegment>();
            for (var edgeOrder = 0; edgeOrder < connectorTree.TreeEdges.Count; edgeOrder++)
            {
                var edge = connectorTree.TreeEdges[edgeOrder];
                if (!TryBuildSegment(edge, edgeOrder, connectorTree, siteSnapshot, biomePublication, out var segment, out var error))
                {
                    errors.Add(error);
                    continue;
                }
                segments.Add(segment);
            }

            if (segments.Count != connectorTree.TreeEdgeCount)
                errors.Add(Error(HorizontalBackboneBuildErrorCode.SegmentCountMismatch,
                    segments.Count.ToString(CultureInfo.InvariantCulture), connectorTree.TreeEdgeCount.ToString(CultureInfo.InvariantCulture), -1,
                    "Every connector-tree edge must produce exactly one segment."));
            if (sourceTreeEdgeCount != connectorTree.TreeEdgeCount || sourceSiteSectorCount != siteSnapshot.Sectors.Count ||
                sourceBiomeSectorCount != biomePublication.Snapshot.Sectors.Count)
                errors.Add(Error(HorizontalBackboneBuildErrorCode.SourceMutationDetected, string.Empty, string.Empty, -1, "A source collection changed during routing."));
            if (errors.Count > 0) return Invalid(errors);

            try
            {
                var plan = new HorizontalBackbonePlan(connectorTree, routeMaskLookup, siteSnapshot, biomePublication, segments);
                var reservedEndpoints = 0;
                foreach (var segment in plan.Segments)
                    foreach (var cell in segment.Cells)
                        if (cell.IsReserved && cell.IsEndpoint) reservedEndpoints++;
                var diagnostics = new HorizontalBackboneDiagnostics(
                    connectorTree.TreeEdgeCount, plan.SegmentCount, plan.SameRowSegmentCount,
                    plan.GatewayPendingSegmentCount, plan.TotalHorizontalCellCount, reservedEndpoints,
                    0, 0, 0, 0, 0, 0, 0);
                return new HorizontalBackboneBuildResult(HorizontalBackboneBuildStatus.Completed, plan, diagnostics, Array.Empty<HorizontalBackboneBuildError>());
            }
            catch (OverflowException)
            {
                errors.Add(Error(HorizontalBackboneBuildErrorCode.InvalidHorizontalRun, string.Empty, string.Empty, -1, "Horizontal route cost overflowed checked arithmetic."));
            }
            catch (ArgumentException exception)
            {
                errors.Add(Error(HorizontalBackboneBuildErrorCode.InvalidHorizontalRun, string.Empty, string.Empty, -1, exception.Message));
            }
            return Invalid(errors);
        }

        internal static List<HorizontalBackboneBuildError> SortAndDedupe(IEnumerable<HorizontalBackboneBuildError> source)
        {
            var values = new List<HorizontalBackboneBuildError>();
            foreach (var error in source) if (error != null) values.Add(error);
            values.Sort(HorizontalBackboneBuildError.Compare);
            var result = new List<HorizontalBackboneBuildError>();
            foreach (var error in values)
                if (result.Count == 0 || HorizontalBackboneBuildError.Compare(result[result.Count - 1], error) != 0) result.Add(error);
            return result;
        }

        private static bool TryBuildSegment(
            MandatoryConnectorCandidateEdge edge,
            int edgeOrder,
            MandatoryConnectorTree tree,
            SiteReservationSnapshot site,
            BiomePatchValidationPublication biome,
            out HorizontalBackboneSegment segment,
            out HorizontalBackboneBuildError error)
        {
            segment = null;
            error = null;
            if (edge == null || !edge.IsTreeEdge || !edge.EdgeId.IsValid || edgeOrder < 0 || edgeOrder > 99)
            {
                error = Error(HorizontalBackboneBuildErrorCode.InvalidSegmentIdentity, edge == null ? string.Empty : edge.EdgeId.Value, string.Empty, -1, "Tree edge identity cannot produce a segment identity.");
                return false;
            }
            var from = edge.FromApproachSector;
            var to = edge.ToApproachSector;
            if (!IsWorld(from) || !IsWorld(to))
            {
                error = Error(HorizontalBackboneBuildErrorCode.WorldBoundsViolation, edge.EdgeId.Value, string.Empty, -1, "Approach sector is outside the world.");
                return false;
            }
            var suffix = edge.EdgeId.Value.Substring(8);
            var segmentId = new HorizontalBackboneSegmentId("HSEG_" + edgeOrder.ToString("D2", CultureInfo.InvariantCulture) + "_" + suffix);
            List<HorizontalBackboneRouteCell> cells;
            int distance;
            int totalCost;
            if (from.Y == to.Y)
            {
                var coords = CreateLeg(from, to.X);
                if (!TryCreateCells(coords, from, to, false, -1, tree, edge, site, biome, out cells, out totalCost))
                {
                    error = Error(HorizontalBackboneBuildErrorCode.ForbiddenReservationIntrusion, edge.EdgeId.Value, string.Empty, FirstForbiddenIndex(coords, from, to, tree, edge, site), "Straight horizontal run crosses a forbidden reservation footprint.");
                    return false;
                }
                distance = Math.Abs(from.X - to.X);
            }
            else
            {
                Candidate best = null;
                for (var gatewayX = 0; gatewayX < WorldGenConstants.SectorColumns; gatewayX++)
                {
                    var coords = CreateLeg(from, gatewayX);
                    coords.AddRange(CreateLeg(to, gatewayX, true));
                    if (!TryCreateCells(coords, from, to, true, gatewayX, tree, edge, site, biome, out var candidateCells, out var candidateCost)) continue;
                    var candidateDistance = checked(Math.Abs(from.X - gatewayX) + Math.Abs(to.X - gatewayX));
                    var candidate = new Candidate(gatewayX, candidateDistance, candidateCost, candidateCells);
                    if (best == null || Candidate.Compare(candidate, best) < 0) best = candidate;
                }
                if (best == null)
                {
                    error = Error(HorizontalBackboneBuildErrorCode.ForbiddenReservationIntrusion, edge.EdgeId.Value, string.Empty, -1, "No horizontal-leg gateway candidate avoids reservation footprints.");
                    return false;
                }
                cells = best.Cells;
                distance = best.Distance;
                totalCost = best.Cost;
            }
            try
            {
                segment = new HorizontalBackboneSegment(segmentId, edge.EdgeId, edge.FromTerminalId, edge.ToTerminalId,
                    cells, from, to, from.Y == to.Y, from.Y != to.Y, distance, totalCost);
                return true;
            }
            catch (ArgumentException exception)
            {
                error = Error(HorizontalBackboneBuildErrorCode.InvalidHorizontalRun, edge.EdgeId.Value, segmentId.Value, -1, exception.Message);
                return false;
            }
        }

        private static List<SectorCoord> CreateLeg(SectorCoord start, int targetX, bool reverse = false)
        {
            var values = new List<SectorCoord>();
            var step = start.X <= targetX ? 1 : -1;
            for (var x = start.X; ; x += step)
            {
                values.Add(new SectorCoord(x, start.Y));
                if (x == targetX) break;
            }
            if (reverse) values.Reverse();
            return values;
        }

        private static bool TryCreateCells(
            IReadOnlyList<SectorCoord> coords,
            SectorCoord from,
            SectorCoord to,
            bool pendingGateway,
            int gatewayX,
            MandatoryConnectorTree tree,
            MandatoryConnectorCandidateEdge edge,
            SiteReservationSnapshot site,
            BiomePatchValidationPublication biome,
            out List<HorizontalBackboneRouteCell> cells,
            out int totalCost)
        {
            cells = new List<HorizontalBackboneRouteCell>();
            totalCost = 0;
            for (var ordinal = 0; ordinal < coords.Count; ordinal++)
            {
                var coord = coords[ordinal];
                if (!IsWorld(coord)) return false;
                var endpoint = IsEndpointAdapter(coord, from, to, tree, edge);
                var sector = site.GetSector(coord);
                if (sector.IsReserved && !endpoint) return false;
                var gateway = pendingGateway && coord.X == gatewayX && (coord.Y == from.Y || coord.Y == to.Y);
                var cost = StepCost(coord, edge, tree, site, biome);
                totalCost = checked(totalCost + cost);
                cells.Add(new HorizontalBackboneRouteCell(coord, ordinal, true, true, endpoint, sector.IsReserved, gateway, cost));
            }
            return true;
        }

        private static int StepCost(
            SectorCoord coord,
            MandatoryConnectorCandidateEdge edge,
            MandatoryConnectorTree tree,
            SiteReservationSnapshot site,
            BiomePatchValidationPublication biome)
        {
            tree.SourceTerminalSet.TryGet(edge.FromTerminalId, out var fromTerminal);
            tree.SourceTerminalSet.TryGet(edge.ToTerminalId, out var toTerminal);
            var ownReservations = new HashSet<SiteReservationId> { fromTerminal.ReservationId, toTerminal.ReservationId };
            var adjacentOwn = false;
            var adjacentOther = false;
            foreach (var neighbor in CardinalNeighbors(coord))
            {
                if (!IsWorld(neighbor)) continue;
                var sector = site.GetSector(neighbor);
                if (!sector.IsReserved || !sector.ReservationId.HasValue) continue;
                if (ownReservations.Contains(sector.ReservationId.Value)) adjacentOwn = true;
                else adjacentOther = true;
            }
            if (adjacentOther) return 8;
            if (adjacentOwn) return 4;
            var ownership = biome.Snapshot.GetSector(WorldGridIndex.ToIndex(coord));
            if (!ownership.IsAssigned) return 4;
            if (!string.IsNullOrEmpty(ownership.SecondaryBiomeId)) return 2;
            foreach (var neighbor in HorizontalNeighbors(coord))
            {
                if (!IsWorld(neighbor)) continue;
                var other = biome.Snapshot.GetSector(WorldGridIndex.ToIndex(neighbor));
                if (other.IsAssigned && !string.Equals(ownership.PrimaryBiomeId, other.PrimaryBiomeId, StringComparison.Ordinal)) return 2;
            }
            return 1;
        }

        private static IEnumerable<SectorCoord> CardinalNeighbors(SectorCoord coord)
        {
            yield return new SectorCoord(coord.X - 1, coord.Y);
            yield return new SectorCoord(coord.X + 1, coord.Y);
            yield return new SectorCoord(coord.X, coord.Y - 1);
            yield return new SectorCoord(coord.X, coord.Y + 1);
        }

        private static IEnumerable<SectorCoord> HorizontalNeighbors(SectorCoord coord)
        {
            yield return new SectorCoord(coord.X - 1, coord.Y);
            yield return new SectorCoord(coord.X + 1, coord.Y);
        }

        private static int FirstForbiddenIndex(
            IReadOnlyList<SectorCoord> coords,
            SectorCoord from,
            SectorCoord to,
            MandatoryConnectorTree tree,
            MandatoryConnectorCandidateEdge edge,
            SiteReservationSnapshot site)
        {
            foreach (var coord in coords)
                if (!IsEndpointAdapter(coord, from, to, tree, edge) && site.GetSector(coord).IsReserved) return WorldGridIndex.ToIndex(coord);
            return -1;
        }

        private static bool IsEndpointAdapter(
            SectorCoord coord,
            SectorCoord from,
            SectorCoord to,
            MandatoryConnectorTree tree,
            MandatoryConnectorCandidateEdge edge)
        {
            if (coord == from || coord == to) return true;
            if (!tree.SourceTerminalSet.TryGet(edge.FromTerminalId, out var fromTerminal) ||
                !tree.SourceTerminalSet.TryGet(edge.ToTerminalId, out var toTerminal)) return false;
            return coord == fromTerminal.AnchorSector || coord == toTerminal.AnchorSector;
        }

        private static void ValidateTree(MandatoryConnectorTree tree, ICollection<HorizontalBackboneBuildError> errors)
        {
            if (tree.SourceTerminalSet == null || tree.TreeEdges == null || tree.NodeCount != 7 || tree.TreeEdgeCount != 6 ||
                !tree.IsConnected || !tree.IsAcyclic || !tree.CoversAllTerminals)
                errors.Add(Error(HorizontalBackboneBuildErrorCode.InvalidConnectorTree, string.Empty, string.Empty, -1, "Connector tree must contain seven nodes and six connected acyclic edges."));
        }

        private static void ValidateLookup(MandatoryRouteMaskLookup lookup, ICollection<HorizontalBackboneBuildError> errors)
        {
            if (lookup.Records == null || lookup.Count != 3 || lookup.Type1 == null || lookup.Type2 == null || lookup.Type3 == null ||
                lookup.Type1.OpenMask != MandatoryRouteOpenMask.Type1Horizontal)
                errors.Add(Error(HorizontalBackboneBuildErrorCode.InvalidRouteMaskLookup, string.Empty, string.Empty, -1, "Route-mask lookup must contain the exact three mandatory masks."));
        }

        private static void ValidateSite(SiteReservationSnapshot site, ICollection<HorizontalBackboneBuildError> errors)
        {
            if (site.Reservations == null || site.Sectors == null || site.Sectors.Count != WorldGenConstants.SectorCount)
                errors.Add(Error(HorizontalBackboneBuildErrorCode.InvalidSiteSnapshot, string.Empty, string.Empty, -1, "Site snapshot must cover all 169 sectors."));
        }

        private static void ValidateBiome(BiomePatchValidationPublication biome, ICollection<HorizontalBackboneBuildError> errors)
        {
            if (biome.Snapshot == null || biome.Snapshot.Sectors == null || biome.Snapshot.Sectors.Count != WorldGenConstants.SectorCount)
                errors.Add(Error(HorizontalBackboneBuildErrorCode.InvalidBiomePublication, string.Empty, string.Empty, -1, "Biome publication must cover all 169 sectors."));
        }

        private static bool IsWorld(SectorCoord coord) => coord.X >= 0 && coord.X < WorldGenConstants.SectorColumns && coord.Y >= 0 && coord.Y < WorldGenConstants.SectorRows;
        private static HorizontalBackboneBuildError Error(HorizontalBackboneBuildErrorCode code, string firstId, string secondId, int sectorIndex, string message) =>
            new HorizontalBackboneBuildError(code, firstId, secondId, sectorIndex, message);
        private static HorizontalBackboneBuildResult Invalid(IEnumerable<HorizontalBackboneBuildError> errors) =>
            new HorizontalBackboneBuildResult(HorizontalBackboneBuildStatus.InvalidInput, null, null, SortAndDedupe(errors));

        private sealed class Candidate
        {
            public Candidate(int gatewayX, int distance, int cost, List<HorizontalBackboneRouteCell> cells)
            {
                GatewayX = gatewayX;
                Distance = distance;
                Cost = cost;
                Cells = cells;
            }
            public int GatewayX { get; }
            public int Distance { get; }
            public int Cost { get; }
            public List<HorizontalBackboneRouteCell> Cells { get; }
            public static int Compare(Candidate left, Candidate right)
            {
                var value = left.Cost.CompareTo(right.Cost);
                if (value != 0) return value;
                value = left.Distance.CompareTo(right.Distance);
                return value != 0 ? value : left.GatewayX.CompareTo(right.GatewayX);
            }
        }
    }
}
