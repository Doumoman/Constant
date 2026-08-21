using System;
using System.Collections.Generic;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class VerticalGatewayPlanner
    {
        public VerticalGatewayBuildResult Build(
            HorizontalBackbonePlan horizontalPlan,
            MandatoryRouteMaskLookup routeMaskLookup,
            SiteReservationSnapshot siteSnapshot,
            BiomePatchValidationPublication biomePublication)
        {
            var errors = new List<VerticalGatewayBuildError>();
            AddMissing(errors, horizontalPlan, "HORIZONTAL_BACKBONE_PLAN");
            AddMissing(errors, routeMaskLookup, "MANDATORY_ROUTE_MASK_LOOKUP");
            AddMissing(errors, siteSnapshot, "SITE_RESERVATION_SNAPSHOT");
            AddMissing(errors, biomePublication, "BIOME_PATCH_PUBLICATION");
            if (errors.Count != 0) return VerticalGatewayBuildResult.Invalid(errors);

            ValidateSourceIdentity(horizontalPlan, routeMaskLookup, siteSnapshot, biomePublication, errors);
            if (horizontalPlan.SegmentCount != 6)
                errors.Add(Error(VerticalGatewayBuildErrorCode.InvalidHorizontalBackbonePlan, "HORIZONTAL_BACKBONE_PLAN", string.Empty, -1, "Horizontal segment count must be six."));
            if (horizontalPlan.GatewayPendingSegmentCount != 4)
                errors.Add(Error(VerticalGatewayBuildErrorCode.PendingSegmentCountMismatch, "HORIZONTAL_BACKBONE_PLAN", string.Empty, -1, "Pending segment count must be four."));
            if (routeMaskLookup.Count != 3)
                errors.Add(Error(VerticalGatewayBuildErrorCode.InvalidRouteMaskLookup, "MANDATORY_ROUTE_MASK_LOOKUP", string.Empty, -1, "Type1/Type2/Type3 lookup count must remain three."));
            if (errors.Count != 0) return VerticalGatewayBuildResult.Invalid(errors);

            var pairs = new List<VerticalGatewayPair>();
            var pendingOrdinal = 0;
            var reservedEndpoints = 0;
            foreach (var segment in horizontalPlan.Segments)
            {
                if (segment.IsSameRow)
                {
                    if (segment.RequiresVerticalGateway || HasGatewayCell(segment))
                        errors.Add(Error(VerticalGatewayBuildErrorCode.UnsupportedSameRowGateway, segment.SegmentId.Value, string.Empty, -1, "Same-row segments cannot publish gateway anchors."));
                    continue;
                }
                if (!segment.RequiresVerticalGateway) continue;

                var anchors = GatewayCells(segment);
                if (anchors.Count != 2)
                {
                    errors.Add(Error(VerticalGatewayBuildErrorCode.InvalidHorizontalBackbonePlan, segment.SegmentId.Value, string.Empty, -1, "A pending segment must expose exactly two gateway anchors."));
                    continue;
                }
                if (anchors[0].Coord.X != anchors[1].Coord.X)
                {
                    errors.Add(Error(VerticalGatewayBuildErrorCode.InvalidColumnAlignment, segment.SegmentId.Value, string.Empty, -1, "Pending anchors must share one column."));
                    continue;
                }
                if (anchors[0].Coord.Y == anchors[1].Coord.Y)
                {
                    errors.Add(Error(VerticalGatewayBuildErrorCode.InvalidAnchorOrientation, segment.SegmentId.Value, string.Empty, WorldGridIndex.ToIndex(anchors[0].Coord), "Gateway anchors must occupy different rows."));
                    continue;
                }

                var upperCell = anchors[0].Coord.Y > anchors[1].Coord.Y ? anchors[0] : anchors[1];
                var lowerCell = ReferenceEquals(upperCell, anchors[0]) ? anchors[1] : anchors[0];
                var upper = new VerticalGatewayAnchor(upperCell.Coord, true, true, false, true, upperCell.IsReserved, upperCell.StepCost);
                var lower = new VerticalGatewayAnchor(lowerCell.Coord, false, false, true, true, lowerCell.IsReserved, lowerCell.StepCost);
                if (upper.IsReserved) reservedEndpoints++;
                if (lower.IsReserved) reservedEndpoints++;

                var spans = new List<SectorCoord>();
                var junctions = new List<VerticalGatewayJunctionCell>();
                var totalCost = 0;
                for (var y = upper.Coord.Y; y >= lower.Coord.Y; y--)
                {
                    var coord = new SectorCoord(upper.Coord.X, y);
                    if (!IsWorld(coord))
                    {
                        errors.Add(Error(VerticalGatewayBuildErrorCode.WorldBoundsViolation, segment.SegmentId.Value, string.Empty, -1, "Gateway span left the world bounds."));
                        continue;
                    }
                    spans.Add(coord);
                    if (y == upper.Coord.Y) totalCost = checked(totalCost + upper.StepCost);
                    else if (y == lower.Coord.Y) totalCost = checked(totalCost + lower.StepCost);
                    else
                    {
                        var sectorIndex = WorldGridIndex.ToIndex(coord);
                        if (siteSnapshot.GetSector(sectorIndex).IsReserved)
                        {
                            errors.Add(Error(VerticalGatewayBuildErrorCode.Type4ReservationIntrusion, segment.SegmentId.Value, string.Empty, sectorIndex, "A reserved footprint cannot be a Type4 middle cell."));
                            continue;
                        }
                        var stepCost = GetFiniteCost(biomePublication, sectorIndex);
                        totalCost = checked(totalCost + stepCost);
                        junctions.Add(new VerticalGatewayJunctionCell(
                            coord,
                            HasHorizontalAdjacency(horizontalPlan, coord, -1),
                            HasHorizontalAdjacency(horizontalPlan, coord, 1)));
                    }
                }
                if (errors.Count != 0) continue;

                var suffix = segment.SourceTreeEdgeId.Value.Substring(8);
                var idText = "VGW_" + pendingOrdinal.ToString("D2", CultureInfo.InvariantCulture) + "_" + suffix;
                if (!VerticalGatewayId.TryCreate(idText, out var gatewayId))
                {
                    errors.Add(Error(VerticalGatewayBuildErrorCode.InvalidGatewayIdentity, segment.SegmentId.Value, idText, -1, "Gateway identity could not be derived from the source edge."));
                    continue;
                }
                pairs.Add(new VerticalGatewayPair(gatewayId, segment.SegmentId, upper, lower, totalCost, false, spans, junctions));
                pendingOrdinal++;
            }

            if (errors.Count != 0) return VerticalGatewayBuildResult.Invalid(errors);
            if (pendingOrdinal != 4)
                errors.Add(Error(VerticalGatewayBuildErrorCode.PendingSegmentCountMismatch, "HORIZONTAL_BACKBONE_PLAN", string.Empty, -1, "Exactly four pending segments must be consumed."));
            if (pairs.Count != 4)
                errors.Add(Error(VerticalGatewayBuildErrorCode.GatewayPairCountMismatch, "VERTICAL_GATEWAY_PLAN", string.Empty, -1, "Exactly four gateway pairs must be produced."));
            if (errors.Count != 0) return VerticalGatewayBuildResult.Invalid(errors);

            var plan = new VerticalGatewayPlan(horizontalPlan, routeMaskLookup, siteSnapshot, biomePublication, pairs);
            var diagnostics = new VerticalGatewayDiagnostics(
                horizontalPlan.SegmentCount,
                horizontalPlan.GatewayPendingSegmentCount,
                plan.GatewayPairCount,
                plan.Type4JunctionCellCount,
                plan.ConflictPendingCount,
                plan.TotalVerticalSpanCellCount,
                reservedEndpoints,
                plan.TotalCost);
            return VerticalGatewayBuildResult.Completed(plan, diagnostics);
        }

        private static void ValidateSourceIdentity(
            HorizontalBackbonePlan horizontalPlan,
            MandatoryRouteMaskLookup routeMaskLookup,
            SiteReservationSnapshot siteSnapshot,
            BiomePatchValidationPublication biomePublication,
            ICollection<VerticalGatewayBuildError> errors)
        {
            if (!ReferenceEquals(horizontalPlan.SourceRouteMaskLookup, routeMaskLookup))
                errors.Add(Error(VerticalGatewayBuildErrorCode.InvalidRouteMaskLookup, "HORIZONTAL_BACKBONE_PLAN", "MANDATORY_ROUTE_MASK_LOOKUP", -1, "Route-mask lookup identity must be exact."));
            if (!ReferenceEquals(horizontalPlan.SourceSiteSnapshot, siteSnapshot))
                errors.Add(Error(VerticalGatewayBuildErrorCode.InvalidSiteSnapshot, "HORIZONTAL_BACKBONE_PLAN", "SITE_RESERVATION_SNAPSHOT", -1, "Site snapshot identity must be exact."));
            if (!ReferenceEquals(horizontalPlan.SourceBiomePublication, biomePublication))
                errors.Add(Error(VerticalGatewayBuildErrorCode.InvalidBiomePublication, "HORIZONTAL_BACKBONE_PLAN", "BIOME_PATCH_PUBLICATION", -1, "Biome publication identity must be exact."));
            if (!ReferenceEquals(horizontalPlan.SourceConnectorTree.SourceRouteMaskLookup, routeMaskLookup))
                errors.Add(Error(VerticalGatewayBuildErrorCode.InvalidRouteMaskLookup, "MANDATORY_CONNECTOR_TREE", "MANDATORY_ROUTE_MASK_LOOKUP", -1, "Connector-tree lookup identity must remain exact."));
        }

        private static int GetFiniteCost(BiomePatchValidationPublication publication, int sectorIndex)
        {
            var ownership = publication.Snapshot.Sectors[sectorIndex];
            if (!ownership.IsAssigned) return 1;
            return string.IsNullOrEmpty(ownership.SecondaryBiomeId) ? 2 : 4;
        }

        private static bool HasHorizontalAdjacency(HorizontalBackbonePlan plan, SectorCoord coord, int deltaX)
        {
            var neighbor = new SectorCoord(coord.X + deltaX, coord.Y);
            if (!IsWorld(neighbor)) return false;
            foreach (var segment in plan.Segments)
            {
                var hasCoord = false;
                var hasNeighbor = false;
                foreach (var cell in segment.Cells)
                {
                    if (cell.Coord == coord) hasCoord = true;
                    if (cell.Coord == neighbor) hasNeighbor = true;
                }
                if (hasCoord && hasNeighbor) return true;
            }
            return false;
        }

        private static bool HasGatewayCell(HorizontalBackboneSegment segment)
        {
            foreach (var cell in segment.Cells) if (cell.RequiresVerticalGateway) return true;
            return false;
        }

        private static List<HorizontalBackboneRouteCell> GatewayCells(HorizontalBackboneSegment segment)
        {
            var values = new List<HorizontalBackboneRouteCell>();
            foreach (var cell in segment.Cells) if (cell.RequiresVerticalGateway) values.Add(cell);
            values.Sort((left, right) =>
            {
                var y = right.Coord.Y.CompareTo(left.Coord.Y);
                return y != 0 ? y : left.Coord.X.CompareTo(right.Coord.X);
            });
            return values;
        }

        private static bool IsWorld(SectorCoord coord) =>
            coord.X >= 0 && coord.X < WorldGenConstants.SectorColumns && coord.Y >= 0 && coord.Y < WorldGenConstants.SectorRows;

        private static void AddMissing(ICollection<VerticalGatewayBuildError> errors, object value, string id)
        {
            if (value == null) errors.Add(Error(VerticalGatewayBuildErrorCode.MissingInput, id, string.Empty, -1, "Required planner input is missing."));
        }

        private static VerticalGatewayBuildError Error(VerticalGatewayBuildErrorCode code, string firstId, string secondId, int sectorIndex, string message) =>
            new VerticalGatewayBuildError(code, firstId, secondId, sectorIndex, message);
    }
}
