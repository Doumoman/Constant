using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorClusterRoleZoneBuilder
    {
        public const string ReferencePublicationLabel = "REFERENCE ROLE ZONE";

        public static SectorClusterRoleZoneBuildResult Build(
            SectorClusterRoleZoneBuildRequest request)
        {
            var errors = new List<SectorPatternRenderError>();
            ValidateRequest(request, errors);
            if (errors.Count != 0) return Failure(errors);

            var assignments = request.Assignments.ToDictionary(
                value => Index(value.Coordinate), value => value);
            var sectors = request.Input.Sectors.ToDictionary(value => value.SectorIndex);
            var protectedEvidence = BuildProtectionEvidence(request);
            var protectedBySector = protectedEvidence
                .GroupBy(value => value.SectorIndex)
                .ToDictionary(value => value.Key, value => value.ToArray());

            var roleCells = new List<SectorClusterRoleCell>();
            var roleKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var placement in request.ClusterPlacementPlan.Placements
                         .OrderBy(value => value.SectorIndex))
            {
                if (!sectors.TryGetValue(placement.SectorIndex, out var sector) ||
                    !assignments.TryGetValue(placement.SectorIndex, out var assignment))
                {
                    Add(errors, SectorPatternRenderErrorCode.SectorMismatch,
                        Subject(placement.SectorCoordinate), "Placement sector is missing.");
                    continue;
                }

                var orderedFootprint = placement.Cells.OrderBy(value => value).ToArray();
                for (var footprintOrdinal = 0; footprintOrdinal < orderedFootprint.Length; footprintOrdinal++)
                {
                    var footprintCell = orderedFootprint[footprintOrdinal];
                    var key = placement.SectorIndex + "|" + footprintCell.X + "|" + footprintCell.Y;
                    if (!roleKeys.Add(key))
                    {
                        Add(errors, SectorPatternRenderErrorCode.DuplicateRoleCell,
                            Subject(placement.SectorCoordinate), key);
                        continue;
                    }

                    var rect = footprintCell.ToTileRect();
                    if (!rect.IsInside(sector.CanvasWidth, sector.CanvasHeight))
                    {
                        Add(errors, SectorPatternRenderErrorCode.RoleCellOutOfBounds,
                            Subject(placement.SectorCoordinate), rect.ToString());
                        continue;
                    }

                    var evidence = protectedBySector.TryGetValue(placement.SectorIndex, out var sectorEvidence)
                        ? sectorEvidence.Where(value => rect.Contains(value.Coordinate)).ToArray()
                        : Array.Empty<SectorPatternProtectionEvidence>();
                    var nodes = request.SpineEnvelopePlan.Graph.Nodes
                        .Where(value => value.SectorIndex == placement.SectorIndex && rect.Contains(value.Coordinate))
                        .OrderBy(value => value).ToArray();
                    var edges = request.SpineEnvelopePlan.Graph.Edges
                        .Where(value => value.SectorIndex == placement.SectorIndex &&
                                        value.CenterlineCells.Any(rect.Contains))
                        .OrderBy(value => value).ToArray();
                    var anchors = request.AnchorPlan.Anchors
                        .Where(value => value.SectorIndex == placement.SectorIndex && value.Rect.Overlaps(rect))
                        .OrderByDescending(value => value.Priority)
                        .ThenBy(value => value.AnchorId, StringComparer.Ordinal)
                        .ToArray();
                    var envelope = request.SpineEnvelopePlan.EnvelopeCells
                        .Where(value => value.SectorIndex == placement.SectorIndex && rect.Contains(value.Coordinate))
                        .OrderBy(value => value).ToArray();

                    var kind = RoleKind(
                        sector,
                        assignment.PrimaryRole,
                        footprintOrdinal,
                        orderedFootprint.Length,
                        nodes,
                        edges,
                        anchors,
                        envelope,
                        evidence);
                    roleCells.Add(new SectorClusterRoleCell(
                        placement.SectorCoordinate,
                        placement.SectorIndex,
                        placement.ClusterId,
                        placement.VariantId,
                        footprintCell,
                        sector.Biome.BiomeId,
                        assignment.PrimaryRole,
                        kind,
                        First(nodes.Select(value => value.NodeId)),
                        First(edges.Select(value => value.EdgeId)),
                        First(anchors.Select(value => value.AnchorId)),
                        evidence.Length != 0,
                        evidence.Select(value => value.Coordinate).Distinct().Count()));
                }
            }

            ValidateRoleCoverage(request, roleCells, errors);
            var zones = BuildZones(roleCells, protectedBySector, errors);
            ValidateZones(request, roleCells, zones, errors);
            if (errors.Count != 0) return Failure(errors);

            var graph = request.SpineEnvelopePlan.Graph;
            var protectedIdentity = SectorPatternRenderCanonicalDigest.ComputeProtectedOpenIdentity(
                request.SpineEnvelopePlan.ProtectedOpenCells);
            var draft = new SectorClusterRolePatternPlan(
                request.PublicationLabel,
                roleCells,
                zones,
                protectedEvidence,
                request.Input.Sectors.Count,
                request.ClusterPlacementPlan.Placements.Count,
                request.Input.CanonicalDigest,
                graph.PacingAssignmentDigest,
                request.AnchorPlan.CanonicalDigest,
                request.ClusterPlacementPlan.CanonicalDigest,
                request.SpineEnvelopePlan.CanonicalDigest,
                graph.RouteAccessIdentityDigest,
                graph.ExternalSocketIdentityDigest,
                graph.BoundaryIdentityDigest,
                graph.SpecialIdentityDigest,
                graph.ClusterIdentityDigest,
                protectedIdentity,
                string.Empty);
            var digest = SectorPatternRenderCanonicalDigest.ComputeRoleZone(draft);
            if (request.ExpectedCanonicalDigest.Length != 0 &&
                !string.Equals(request.ExpectedCanonicalDigest, digest, StringComparison.Ordinal))
            {
                Add(errors, SectorPatternRenderErrorCode.NonCanonicalPublication,
                    "roleZonePlan.digest", request.ExpectedCanonicalDigest + "!=" + digest);
                return Failure(errors);
            }

            var plan = new SectorClusterRolePatternPlan(
                request.PublicationLabel,
                roleCells,
                zones,
                protectedEvidence,
                request.Input.Sectors.Count,
                request.ClusterPlacementPlan.Placements.Count,
                request.Input.CanonicalDigest,
                graph.PacingAssignmentDigest,
                request.AnchorPlan.CanonicalDigest,
                request.ClusterPlacementPlan.CanonicalDigest,
                request.SpineEnvelopePlan.CanonicalDigest,
                graph.RouteAccessIdentityDigest,
                graph.ExternalSocketIdentityDigest,
                graph.BoundaryIdentityDigest,
                graph.SpecialIdentityDigest,
                graph.ClusterIdentityDigest,
                protectedIdentity,
                digest);
            return new SectorClusterRoleZoneBuildResult(plan, errors);
        }

        private static void ValidateRequest(
            SectorClusterRoleZoneBuildRequest request,
            ICollection<SectorPatternRenderError> errors)
        {
            if (request == null)
            {
                Add(errors, SectorPatternRenderErrorCode.MissingInput,
                    "request", "Role-zone request is required.");
                return;
            }

            if (request.Input == null)
                Add(errors, SectorPatternRenderErrorCode.MissingInput, "input", "Planner input is required.");
            if (request.AnchorPlan == null)
                Add(errors, SectorPatternRenderErrorCode.MissingInput, "anchorPlan", "Fixed anchor plan is required.");
            if (request.ClusterPlacementPlan == null)
                Add(errors, SectorPatternRenderErrorCode.MissingClusterPlacementPlan,
                    "clusterPlacementPlan", "Cluster placement plan is required.");
            if (request.SpineEnvelopePlan == null)
                Add(errors, SectorPatternRenderErrorCode.MissingSpineEnvelopePlan,
                    "spineEnvelopePlan", "Spine-envelope plan is required.");
            if (!string.Equals(request.PublicationLabel, ReferencePublicationLabel, StringComparison.Ordinal))
                Add(errors, SectorPatternRenderErrorCode.NonCanonicalPublication,
                    "publicationLabel", request.PublicationLabel);

            foreach (var fault in request.ReferenceFaults)
                Add(errors, fault, "referenceFault", fault.ToString());
            AddClaim(errors, request.RouteAccessMutationClaim,
                SectorPatternRenderErrorCode.RouteAccessMutationClaim, "routeAccess");
            AddClaim(errors, request.AnchorMutationClaim,
                SectorPatternRenderErrorCode.AnchorMutationClaim, "anchors");
            AddClaim(errors, request.ClusterMutationClaim,
                SectorPatternRenderErrorCode.ClusterMutationClaim, "clusters");
            AddClaim(errors, request.SpineEnvelopeMutationClaim,
                SectorPatternRenderErrorCode.SpineEnvelopeMutationClaim, "spineEnvelope");
            AddClaim(errors, request.ActivityMutationClaim,
                SectorPatternRenderErrorCode.ActivityMutationClaim, "activityEvent");
            AddClaim(errors, request.OwnershipMutationClaim,
                SectorPatternRenderErrorCode.OwnershipMutationClaim, "ownership");
            AddCount(errors, request.SolverInvocationCount,
                SectorPatternRenderErrorCode.SolverMutationClaim, "solver");
            AddCount(errors, request.RandomDrawCount,
                SectorPatternRenderErrorCode.RngMutationClaim, "rng");
            AddCount(errors, request.RetryCount,
                SectorPatternRenderErrorCode.SolverMutationClaim, "retry");
            AddCount(errors, request.TileWriteCount,
                SectorPatternRenderErrorCode.TileMutationClaim, "tile");
            AddCount(errors, request.SceneMutationCount,
                SectorPatternRenderErrorCode.OwnershipMutationClaim, "scene");

            if (request.Input == null || request.AnchorPlan == null ||
                request.ClusterPlacementPlan == null || request.SpineEnvelopePlan == null)
                return;
            if (!request.ClusterPlacementPlan.Map14_04HandoffReady ||
                !request.SpineEnvelopePlan.Map14_05HandoffReady)
                Add(errors, SectorPatternRenderErrorCode.MissingInput,
                    "handoff", "MAP14_03/04 handoff must be ready.");
            if (!string.Equals(request.AnchorPlan.CanonicalDigest,
                    request.ClusterPlacementPlan.AnchorPlanDigest, StringComparison.Ordinal) ||
                !string.Equals(request.AnchorPlan.CanonicalDigest,
                    request.SpineEnvelopePlan.AnchorPlanDigestBefore, StringComparison.Ordinal) ||
                !string.Equals(request.ClusterPlacementPlan.CanonicalDigest,
                    request.SpineEnvelopePlan.ClusterPlacementPlanDigestBefore, StringComparison.Ordinal) ||
                !string.Equals(request.Input.CanonicalDigest,
                    request.SpineEnvelopePlan.PlannerInputDigestBefore, StringComparison.Ordinal))
                Add(errors, SectorPatternRenderErrorCode.SectorMismatch,
                    "identityChain", "MAP14_01/02/03/04 digest chain must match.");

            var assignmentByIndex = new HashSet<int>();
            foreach (var assignment in request.Assignments)
            {
                var index = Index(assignment.Coordinate);
                if (!assignmentByIndex.Add(index))
                    Add(errors, SectorPatternRenderErrorCode.SectorMismatch,
                        Subject(assignment.Coordinate), "Duplicate pacing assignment.");
            }
            foreach (var sector in request.Input.Sectors)
                if (!assignmentByIndex.Contains(sector.SectorIndex))
                    Add(errors, SectorPatternRenderErrorCode.SectorMismatch,
                        Subject(sector.Coordinate), "Missing pacing assignment.");
        }

        private static List<SectorPatternProtectionEvidence> BuildProtectionEvidence(
            SectorClusterRoleZoneBuildRequest request)
        {
            var result = new List<SectorPatternProtectionEvidence>();
            var coordinates = request.Input.Sectors.ToDictionary(
                value => value.SectorIndex, value => value.Coordinate);
            foreach (var cell in request.SpineEnvelopePlan.ProtectedOpenCells)
            {
                AddProtection(result, coordinates[cell.SectorIndex], cell.SectorIndex,
                    cell.Coordinate, MicroPatternProtectedSourceKind.TraversalEnvelope,
                    "ENVELOPE", cell.EdgeId + "|" + cell.SourceIdentity);
            }

            foreach (var edge in request.SpineEnvelopePlan.Graph.Edges)
            foreach (var coordinate in edge.CenterlineCells)
            {
                AddProtection(result, coordinates[edge.SectorIndex], edge.SectorIndex,
                    coordinate, MicroPatternProtectedSourceKind.RouteSpine,
                    "SPINE", edge.EdgeId + "|" + edge.SourceIdentity);
            }

            foreach (var anchor in request.AnchorPlan.Anchors.Where(value =>
                         value.Kind == SectorFixedAnchorKind.ExternalRouteSocket ||
                         value.Kind == SectorFixedAnchorKind.BoundaryFixedSlice ||
                         value.Kind == SectorFixedAnchorKind.SpecialEntryReturn))
            {
                var sourceKind = anchor.Kind == SectorFixedAnchorKind.SpecialEntryReturn
                    ? MicroPatternProtectedSourceKind.SpecialFixedEntry
                    : MicroPatternProtectedSourceKind.BoundaryProtectedOpen;
                for (var y = anchor.Rect.Y; y < anchor.Rect.YMaxExclusive; y++)
                for (var x = anchor.Rect.X; x < anchor.Rect.XMaxExclusive; x++)
                {
                    AddProtection(result, anchor.SectorCoordinate, anchor.SectorIndex,
                        new LocalTileCoord(x, y), sourceKind,
                        anchor.Kind == SectorFixedAnchorKind.SpecialEntryReturn ? "SPECIAL" : "BOUNDARY",
                        anchor.AnchorId + "|" + anchor.SourceIdentity);
                }
            }

            return result.Distinct().OrderBy(value => value).ToList();
        }

        private static void AddProtection(
            ICollection<SectorPatternProtectionEvidence> result,
            SectorCoord sectorCoordinate,
            int sectorIndex,
            LocalTileCoord coordinate,
            MicroPatternProtectedSourceKind sourceKind,
            string label,
            string sourceIdentity)
        {
            var token = "MP14_" + label + "_S" + Number(sectorIndex) +
                        "_X" + Number(coordinate.X) + "_Y" + Number(coordinate.Y);
            result.Add(new SectorPatternProtectionEvidence(
                sectorCoordinate, sectorIndex, coordinate, sourceKind, token, sourceIdentity));
        }

        private static SectorClusterRoleCellKind RoleKind(
            SectorPlannerSectorSnapshot sector,
            PacingRole pacingRole,
            int footprintOrdinal,
            int footprintCount,
            IReadOnlyCollection<SectorSpineNode> nodes,
            IReadOnlyCollection<SectorSpineEdge> edges,
            IReadOnlyCollection<SectorFixedAnchor> anchors,
            IReadOnlyCollection<SectorTraversalEnvelopeCell> envelope,
            IReadOnlyCollection<SectorPatternProtectionEvidence> protectedEvidence)
        {
            if (pacingRole == PacingRole.Quiet)
                return SectorClusterRoleCellKind.QuietBuffer;
            if (pacingRole == PacingRole.Activity || pacingRole == PacingRole.Discovery ||
                pacingRole == PacingRole.Narrative)
                return SectorClusterRoleCellKind.PatternFill;
            if (sector.Boundaries.Count != 0 && footprintOrdinal == 0)
                return SectorClusterRoleCellKind.BoundaryApproach;
            if (sector.Route.RecoveryNeeded && footprintOrdinal == footprintCount - 1)
                return SectorClusterRoleCellKind.RecoverySupport;
            if ((sector.SpecialRegion.Binding == SectorPlannerSpecialRegionBinding.ReservedMandatory &&
                 footprintOrdinal == 0) ||
                nodes.Any(value => value.Kind == SectorSpineNodeKind.SpecialEntry ||
                                   value.Kind == SectorSpineNodeKind.SpecialReturn) ||
                edges.Any(value => value.Kind == SectorSpineEdgeKind.MandatorySpecialConnector ||
                                   value.Kind == SectorSpineEdgeKind.Return) ||
                anchors.Any(value => value.Kind == SectorFixedAnchorKind.SpecialEntryReturn))
                return SectorClusterRoleCellKind.SpecialApproach;
            if (nodes.Any(value => value.Kind == SectorSpineNodeKind.BoundaryBridge ||
                                   value.Kind == SectorSpineNodeKind.ExternalSocket) ||
                edges.Any(value => value.Kind == SectorSpineEdgeKind.BoundaryConnector) ||
                anchors.Any(value => value.Kind == SectorFixedAnchorKind.BoundaryFixedSlice ||
                                     value.Kind == SectorFixedAnchorKind.ExternalRouteSocket))
                return SectorClusterRoleCellKind.BoundaryApproach;
            if (nodes.Any(value => value.Kind == SectorSpineNodeKind.RecoveryJoin) ||
                edges.Any(value => value.Kind == SectorSpineEdgeKind.Recovery) ||
                envelope.Any(value => value.Kind == SectorTraversalEnvelopeCellKind.Recovery))
                return SectorClusterRoleCellKind.RecoverySupport;
            if (nodes.Any(value => value.Kind == SectorSpineNodeKind.ClusterEntry) ||
                footprintOrdinal == 0)
                return SectorClusterRoleCellKind.ClusterEntry;
            if (nodes.Any(value => value.Kind == SectorSpineNodeKind.ClusterExit) ||
                footprintOrdinal == footprintCount - 1)
                return SectorClusterRoleCellKind.ClusterExit;
            if (envelope.Count != 0)
                return SectorClusterRoleCellKind.RouteShoulder;
            if (protectedEvidence.Count != 0)
                return SectorClusterRoleCellKind.ProtectedOpen;
            return SectorClusterRoleCellKind.ClusterCore;
        }

        private static List<SectorPatternZone> BuildZones(
            IEnumerable<SectorClusterRoleCell> roleCells,
            IReadOnlyDictionary<int, SectorPatternProtectionEvidence[]> protectedBySector,
            ICollection<SectorPatternRenderError> errors)
        {
            var result = new List<SectorPatternZone>();
            foreach (var roleCell in roleCells.OrderBy(value => value))
            {
                var roleRect = roleCell.TileRect;
                for (var row = 0; row < 2; row++)
                for (var column = 0; column < 3; column++)
                {
                    var slot = (row * 3) + column;
                    var rect = new SectorPatternTileRect(
                        roleRect.X + column * MicroPatternDefinition.RequiredWidth,
                        roleRect.Y + row * MicroPatternDefinition.RequiredHeight,
                        MicroPatternDefinition.RequiredWidth,
                        MicroPatternDefinition.RequiredHeight);
                    var protectedCount = protectedBySector.TryGetValue(roleCell.SectorIndex, out var evidence)
                        ? evidence.Where(value => rect.Contains(value.Coordinate))
                            .Select(value => value.Coordinate).Distinct().Count()
                        : 0;
                    var kind = ZoneKind(roleCell.Kind, slot, protectedCount);
                    var zoneId = "SZ_S" + Number(roleCell.SectorIndex) +
                                 "_C" + Number(roleCell.FootprintCell.X) +
                                 "_" + Number(roleCell.FootprintCell.Y) +
                                 "_Z" + Number(slot);
                    result.Add(new SectorPatternZone(
                        zoneId,
                        roleCell.SectorCoordinate,
                        roleCell.SectorIndex,
                        roleCell.ClusterId,
                        roleCell.VariantId,
                        roleCell.FootprintCell,
                        roleCell.BiomeId,
                        roleCell.PacingRole,
                        roleCell.Kind,
                        kind,
                        rect,
                        protectedCount,
                        roleCell.SourceNodeId + "|" + roleCell.SourceEdgeId + "|" +
                        roleCell.SourceAnchorId));
                }
            }
            return result;
        }

        private static SectorPatternZoneKind ZoneKind(
            SectorClusterRoleCellKind role,
            int slot,
            int protectedTileCount)
        {
            switch (role)
            {
                case SectorClusterRoleCellKind.BoundaryApproach:
                    return SectorPatternZoneKind.BoundaryBlend;
                case SectorClusterRoleCellKind.SpecialApproach:
                    return SectorPatternZoneKind.SpecialApproach;
                case SectorClusterRoleCellKind.RecoverySupport:
                    return SectorPatternZoneKind.Recovery;
                case SectorClusterRoleCellKind.QuietBuffer:
                    if (slot == 0 || slot == 5) return SectorPatternZoneKind.QuietBuffer;
                    if (slot == 1 || slot == 4) return SectorPatternZoneKind.ClusterBody;
                    return SectorPatternZoneKind.ClusterEdge;
                case SectorClusterRoleCellKind.RouteShoulder:
                case SectorClusterRoleCellKind.ClusterEntry:
                case SectorClusterRoleCellKind.ClusterExit:
                    return SectorPatternZoneKind.RouteShoulder;
                case SectorClusterRoleCellKind.PatternFill:
                    return slot == 1 || slot == 4
                        ? SectorPatternZoneKind.Detail
                        : SectorPatternZoneKind.ClusterEdge;
                case SectorClusterRoleCellKind.ProtectedOpen:
                    return protectedTileCount > 0
                        ? SectorPatternZoneKind.ProtectedNoWrite
                        : SectorPatternZoneKind.RouteShoulder;
                default:
                    return slot == 1 || slot == 4
                        ? SectorPatternZoneKind.ClusterBody
                        : SectorPatternZoneKind.ClusterEdge;
            }
        }

        private static void ValidateRoleCoverage(
            SectorClusterRoleZoneBuildRequest request,
            IReadOnlyCollection<SectorClusterRoleCell> roleCells,
            ICollection<SectorPatternRenderError> errors)
        {
            var expected = request.ClusterPlacementPlan.Placements
                .SelectMany(placement => placement.Cells.Select(cell =>
                    placement.SectorIndex + "|" + cell.X + "|" + cell.Y))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var actual = roleCells.Select(value =>
                    value.SectorIndex + "|" + value.FootprintCell.X + "|" + value.FootprintCell.Y)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!expected.SequenceEqual(actual))
                Add(errors, SectorPatternRenderErrorCode.PatternZoneTouchesUnplacedFootprint,
                    "roleCells", expected.Length + "!=" + actual.Length);
        }

        private static void ValidateZones(
            SectorClusterRoleZoneBuildRequest request,
            IReadOnlyCollection<SectorClusterRoleCell> roleCells,
            IReadOnlyCollection<SectorPatternZone> zones,
            ICollection<SectorPatternRenderError> errors)
        {
            var roleByKey = roleCells.ToDictionary(value =>
                value.SectorIndex + "|" + value.FootprintCell.X + "|" + value.FootprintCell.Y);
            if (zones.Count != roleCells.Count * 6)
                Add(errors, SectorPatternRenderErrorCode.PatternZoneOutsideCluster,
                    "zones.count", zones.Count + "!=" + roleCells.Count * 6);
            foreach (var zone in zones)
            {
                if (!zone.TileRect.IsInside(WorldGenConstants.SectorWidthTiles,
                        WorldGenConstants.SectorHeightTiles))
                    Add(errors, SectorPatternRenderErrorCode.PatternZoneOutOfBounds,
                        zone.ZoneId, zone.TileRect.ToString());
                var key = zone.SectorIndex + "|" + zone.OwnerCell.X + "|" + zone.OwnerCell.Y;
                if (!roleByKey.TryGetValue(key, out var role) ||
                    zone.TileRect.X < role.TileRect.X ||
                    zone.TileRect.Y < role.TileRect.Y ||
                    zone.TileRect.XMaxExclusive > role.TileRect.XMaxExclusive ||
                    zone.TileRect.YMaxExclusive > role.TileRect.YMaxExclusive)
                    Add(errors, SectorPatternRenderErrorCode.PatternZoneOutsideCluster,
                        zone.ZoneId, key);
            }

            foreach (var sectorGroup in zones.GroupBy(value => value.SectorIndex))
            {
                var ordered = sectorGroup.OrderBy(value => value).ToArray();
                for (var left = 0; left < ordered.Length; left++)
                for (var right = left + 1; right < ordered.Length; right++)
                    if (ordered[left].TileRect.Overlaps(ordered[right].TileRect))
                        Add(errors, SectorPatternRenderErrorCode.PatternZoneOverlap,
                            ordered[left].ZoneId, ordered[right].ZoneId);
            }
        }

        private static bool Contains(this SectorFixedAnchorRect rect, LocalTileCoord coordinate) =>
            coordinate.X >= rect.X && coordinate.X < rect.XMaxExclusive &&
            coordinate.Y >= rect.Y && coordinate.Y < rect.YMaxExclusive;

        private static bool Overlaps(this SectorFixedAnchorRect left, SectorFixedAnchorRect right) =>
            left.Overlaps(right);

        private static int Index(SectorCoord coordinate) =>
            (coordinate.Y * WorldGenConstants.SectorColumns) + coordinate.X;

        private static string Subject(SectorCoord coordinate) =>
            "sector[" + Number(coordinate.X) + "," + Number(coordinate.Y) + "]";

        private static string First(IEnumerable<string> values) =>
            values.Where(value => !string.IsNullOrEmpty(value))
                .OrderBy(value => value, StringComparer.Ordinal).FirstOrDefault() ?? string.Empty;

        private static void AddClaim(
            ICollection<SectorPatternRenderError> errors,
            bool claimed,
            SectorPatternRenderErrorCode code,
            string subject)
        {
            if (claimed) Add(errors, code, subject, "Mutation claim must be false.");
        }

        private static void AddCount(
            ICollection<SectorPatternRenderError> errors,
            int count,
            SectorPatternRenderErrorCode code,
            string subject)
        {
            if (count != 0) Add(errors, code, subject, Number(count));
        }

        private static void Add(
            ICollection<SectorPatternRenderError> errors,
            SectorPatternRenderErrorCode code,
            string subject,
            string detail)
        {
            errors.Add(new SectorPatternRenderError(code, subject, detail));
        }

        private static SectorClusterRoleZoneBuildResult Failure(
            IEnumerable<SectorPatternRenderError> errors) =>
            new SectorClusterRoleZoneBuildResult(null, errors);

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
