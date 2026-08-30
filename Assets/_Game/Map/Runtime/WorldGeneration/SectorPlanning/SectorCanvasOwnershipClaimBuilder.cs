using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorCanvasOwnershipClaimBuilder
    {
        public const string ReferencePublicationLabel = "REFERENCE OWNERSHIP CANVAS";

        public static SectorCanvasOwnershipBuildResult BuildClaims(
            SectorCanvasOwnershipBuildRequest request)
        {
            var errors = new List<SectorCanvasOwnershipError>();
            ValidateRequest(request, errors);
            if (errors.Count != 0) return Failure(request, errors);

            var claims = new List<SectorCanvasOwnershipClaim>();
            AddQuietCanvasClaims(request, claims);
            AddClusterClaims(request, claims);
            AddAnchorEvidenceClaims(request, claims);
            AddSpineEvidenceClaims(request, claims);
            AddRoleAndPatternEvidenceClaims(request, claims);
            AddSpecialAndDeferredEvidenceClaims(request, claims);
            AddActivityEventClaims(request, claims);
            claims.AddRange(request.AdditionalClaims);

            ValidateClaims(request, claims, errors);
            if (errors.Count != 0) return Failure(request, errors);

            var ordered = claims.OrderBy(value => value).ToArray();
            var digest = SectorCanvasOwnershipCanonicalDigest.ComputeClaims(request, ordered);
            if (!string.IsNullOrEmpty(request.ExpectedClaimDigest) &&
                !string.Equals(request.ExpectedClaimDigest, digest, StringComparison.Ordinal))
            {
                Add(errors, SectorCanvasOwnershipErrorCode.NonCanonicalPublication,
                    "expectedClaimDigest", "Published claim digest did not match the expected digest.");
                return Failure(request, errors);
            }

            return new SectorCanvasOwnershipBuildResult(
                request, ordered, null, digest, Array.Empty<SectorCanvasOwnershipError>());
        }

        internal static SectorCanvasOwnershipPriority PriorityFor(SectorCanvasOwnerKind ownerKind)
        {
            switch (ownerKind)
            {
                case SectorCanvasOwnerKind.SpecialRegion:
                    return SectorCanvasOwnershipPriority.SpecialRegion;
                case SectorCanvasOwnerKind.Boundary:
                    return SectorCanvasOwnershipPriority.Boundary;
                case SectorCanvasOwnerKind.Spine:
                    return SectorCanvasOwnershipPriority.Spine;
                case SectorCanvasOwnerKind.TerrainCluster:
                    return SectorCanvasOwnershipPriority.TerrainCluster;
                case SectorCanvasOwnerKind.MicroPattern:
                    return SectorCanvasOwnershipPriority.MicroPattern;
                case SectorCanvasOwnerKind.Quiet:
                    return SectorCanvasOwnershipPriority.Quiet;
                case SectorCanvasOwnerKind.ActivityMarker:
                    return SectorCanvasOwnershipPriority.ActivityMarker;
                case SectorCanvasOwnerKind.EventMarker:
                    return SectorCanvasOwnershipPriority.EventMarker;
                case SectorCanvasOwnerKind.ReservedNoWrite:
                case SectorCanvasOwnerKind.ProtectedNoWrite:
                case SectorCanvasOwnerKind.Empty:
                    return SectorCanvasOwnershipPriority.Evidence;
                default:
                    return 0;
            }
        }

        private static void ValidateRequest(
            SectorCanvasOwnershipBuildRequest request,
            ICollection<SectorCanvasOwnershipError> errors)
        {
            if (request == null)
            {
                Add(errors, SectorCanvasOwnershipErrorCode.MissingInput,
                    "request", "Sector canvas ownership request is required.");
                return;
            }

            if (request.Input == null)
                Add(errors, SectorCanvasOwnershipErrorCode.MissingInput,
                    "input", "SectorPlannerInput is required.");
            if (request.FixedAnchorPlan == null || request.ClusterPlacementPlan == null ||
                request.RolePatternPlan == null)
                Add(errors, SectorCanvasOwnershipErrorCode.MissingInput,
                    "plans", "Fixed-anchor, cluster-placement and role-pattern plans are required.");
            if (request.SpineEnvelopePlan == null)
                Add(errors, SectorCanvasOwnershipErrorCode.MissingSpineEnvelopePlan,
                    "spineEnvelopePlan", "SectorSpineEnvelopePlan is required.");
            if (request.PatternRenderPlan == null)
                Add(errors, SectorCanvasOwnershipErrorCode.MissingPatternRenderPlan,
                    "patternRenderPlan", "SectorPatternRenderPlan is required.");
            if (request.QuietActivityEventPlan == null)
                Add(errors, SectorCanvasOwnershipErrorCode.MissingQuietActivityEventPlan,
                    "quietActivityEventPlan", "SectorQuietActivityEventPlan is required.");
            if (errors.Count != 0) return;

            var sectors = request.Input.Sectors;
            if (sectors.Count == 0 || sectors.Any(value => value.CanvasWidth != 48 || value.CanvasHeight != 32))
                Add(errors, SectorCanvasOwnershipErrorCode.SectorMismatch,
                    "input.sectors", "Every reference sector must be exactly 48x32.");
            if (request.Assignments.Count != sectors.Count ||
                request.Assignments.Select(value => value.Coordinate).Distinct().Count() != sectors.Count ||
                sectors.Any(sector => request.Assignments.Count(value => value.Coordinate.Equals(sector.Coordinate)) != 1))
                Add(errors, SectorCanvasOwnershipErrorCode.SectorMismatch,
                    "assignments", "Exactly one public pacing assignment is required per sector.");
            if (request.FixedAnchorPlan.SectorCount != sectors.Count ||
                request.ClusterPlacementPlan.SectorCount != sectors.Count ||
                request.SpineEnvelopePlan.SectorCount != sectors.Count ||
                request.RolePatternPlan.SectorCount != sectors.Count ||
                request.PatternRenderPlan.SectorCount != sectors.Count ||
                request.QuietActivityEventPlan.QuietFillPlan.SectorCount != sectors.Count)
                Add(errors, SectorCanvasOwnershipErrorCode.SectorMismatch,
                    "plans", "All MAP14 public plans must cover the same sector set.");

            var quiet = request.QuietActivityEventPlan;
            var fillRequest = quiet.QuietFillPlan.Request;
            if (!quiet.Map14_07HandoffReady || fillRequest == null ||
                !string.Equals(fillRequest.Input.CanonicalDigest, request.Input.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(fillRequest.AnchorPlan.CanonicalDigest, request.FixedAnchorPlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(fillRequest.ClusterPlacementPlan.CanonicalDigest, request.ClusterPlacementPlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(fillRequest.SpineEnvelopePlan.CanonicalDigest, request.SpineEnvelopePlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(fillRequest.RoleZonePlan.CanonicalDigest, request.RolePatternPlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(fillRequest.PatternRenderPlan.CanonicalDigest, request.PatternRenderPlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.PatternRenderPlan.RoleZonePlanDigest, request.RolePatternPlan.CanonicalDigest, StringComparison.Ordinal))
                Add(errors, SectorCanvasOwnershipErrorCode.NonCanonicalPublication,
                    "handoff", "MAP14_01-06 public digest chain must remain exact and handoff-ready.");
            if (!string.Equals(request.PublicationLabel, ReferencePublicationLabel, StringComparison.Ordinal))
                Add(errors, SectorCanvasOwnershipErrorCode.NonCanonicalPublication,
                    "publicationLabel", "Reference ownership publication label is required.");

            if (request.SpecialPersistenceMutationClaim)
                Add(errors, SectorCanvasOwnershipErrorCode.SpecialPersistenceMutationClaim,
                    "mutation.specialPersistence", "Special persistence mutation is not owned by MAP14_07.");
            if (request.BoundaryMutationClaim)
                Add(errors, SectorCanvasOwnershipErrorCode.BoundaryMutationClaim,
                    "mutation.boundary", "Boundary mutation is not owned by MAP14_07.");
            if (request.SpineEnvelopeMutationClaim)
                Add(errors, SectorCanvasOwnershipErrorCode.SpineEnvelopeMutationClaim,
                    "mutation.spineEnvelope", "Spine/envelope mutation is not owned by MAP14_07.");
            if (request.ClusterMutationClaim)
                Add(errors, SectorCanvasOwnershipErrorCode.ClusterMutationClaim,
                    "mutation.cluster", "Cluster mutation is not owned by MAP14_07.");
            if (request.PatternRenderMutationClaim)
                Add(errors, SectorCanvasOwnershipErrorCode.PatternRenderMutationClaim,
                    "mutation.patternRender", "Pattern render mutation is not owned by MAP14_07.");
            if (request.QuietMutationClaim)
                Add(errors, SectorCanvasOwnershipErrorCode.QuietMutationClaim,
                    "mutation.quiet", "Quiet plan mutation is not owned by MAP14_07.");
            if (request.ActivityMarkerMutationClaim)
                Add(errors, SectorCanvasOwnershipErrorCode.ActivityMarkerMutationClaim,
                    "mutation.activityMarker", "Activity marker mutation is forbidden.");
            if (request.EventMarkerMutationClaim)
                Add(errors, SectorCanvasOwnershipErrorCode.EventMarkerMutationClaim,
                    "mutation.eventMarker", "Event marker mutation is forbidden.");
            AddCount(errors, SectorCanvasOwnershipErrorCode.SolverMutationClaim,
                "mutation.retry", request.RetryCount);
            AddCount(errors, SectorCanvasOwnershipErrorCode.RngMutationClaim,
                "mutation.map14Rng", request.Map14RngDrawCount);
            AddCount(errors, SectorCanvasOwnershipErrorCode.SolverMutationClaim,
                "mutation.solver", request.SolverInvocationCount);
            AddCount(errors, SectorCanvasOwnershipErrorCode.SolverMutationClaim,
                "mutation.reselection", request.PatternClusterReselectionCount);
            AddCount(errors, SectorCanvasOwnershipErrorCode.TileMutationClaim,
                "mutation.tilemap", request.TilemapWriteCount);
            AddCount(errors, SectorCanvasOwnershipErrorCode.SceneMutationClaim,
                "mutation.scene", request.SceneMutationCount + request.PrefabMutationCount + request.GameObjectMutationCount);
            AddCount(errors, SectorCanvasOwnershipErrorCode.ActivityMarkerMutationClaim,
                "mutation.activitySpawn", request.ActivityRuntimeSpawnCount);
            AddCount(errors, SectorCanvasOwnershipErrorCode.EventMarkerMutationClaim,
                "mutation.eventSpawn", request.EventRuntimeSpawnCount);
            AddCount(errors, SectorCanvasOwnershipErrorCode.SceneMutationClaim,
                "mutation.gameplayExecution", request.GameplayExecutionCount);
        }

        private static void AddQuietCanvasClaims(
            SectorCanvasOwnershipBuildRequest request,
            ICollection<SectorCanvasOwnershipClaim> claims)
        {
            var anchorsBySector = request.FixedAnchorPlan.Anchors
                .GroupBy(value => value.SectorIndex)
                .ToDictionary(value => value.Key,
                    value => value.OrderBy(item => item.AnchorId, StringComparer.Ordinal).ToArray());
            foreach (var cell in request.QuietActivityEventPlan.QuietFillPlan.Cells)
            {
                var sectorAnchors = anchorsBySector.TryGetValue(cell.SectorIndex, out var values)
                    ? values
                    : Array.Empty<SectorFixedAnchor>();
                var anchor = sectorAnchors.FirstOrDefault(value => Contains(value.Rect, cell.Coordinate));

                if (cell.ProtectedNoWrite)
                {
                    claims.Add(Claim("PROTECTION", cell, SectorCanvasOwnershipPlane.Protection,
                        SectorCanvasOwnerKind.Spine, "MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE",
                        "MAP14_04_PROTECTED_OPEN", request.SpineEnvelopePlan.CanonicalDigest,
                        "PROTECTED_OPEN", true, false, true, false));
                    claims.Add(Claim("PROTECTED_EVIDENCE", cell, SectorCanvasOwnershipPlane.Evidence,
                        SectorCanvasOwnerKind.ProtectedNoWrite, "MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE",
                        "MAP14_04_PROTECTED_OPEN", request.SpineEnvelopePlan.CanonicalDigest,
                        "NO_TERRAIN_PROTECTED", true, true, true, false,
                        SectorCanvasClaimState.AllowedCoPlaneEvidence));
                }

                if (cell.ReservedNoWrite)
                {
                    var owner = anchor == null
                        ? OwnerForReserved(cell.SourceKind)
                        : OwnerForAnchor(anchor.Kind);
                    var task = owner == SectorCanvasOwnerKind.Boundary
                        ? "MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS"
                        : owner == SectorCanvasOwnerKind.SpecialRegion
                            ? "MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS"
                            : "MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE";
                    var digest = owner == SectorCanvasOwnerKind.Spine
                        ? request.SpineEnvelopePlan.CanonicalDigest
                        : request.FixedAnchorPlan.CanonicalDigest;
                    claims.Add(Claim("RESERVATION", cell, SectorCanvasOwnershipPlane.Reservation,
                        owner, task, anchor == null ? cell.SourceIdentity : anchor.SourceId,
                        digest, "RESERVED_NO_WRITE|" + (anchor == null ? cell.SourceIdentity : anchor.AnchorId),
                        true, false, true, false));
                    claims.Add(Claim("RESERVED_EVIDENCE", cell, SectorCanvasOwnershipPlane.Evidence,
                        SectorCanvasOwnerKind.ReservedNoWrite, task,
                        anchor == null ? cell.SourceIdentity : anchor.AnchorId,
                        digest, "NO_TERRAIN_RESERVED", true, true, true, false,
                        SectorCanvasClaimState.AllowedCoPlaneEvidence));
                    if (!cell.ProtectedNoWrite && owner == SectorCanvasOwnerKind.SpecialRegion &&
                        (anchor == null || anchor.Kind != SectorFixedAnchorKind.ReferenceOnlyMarker))
                        claims.Add(Claim("SPECIAL_TERRAIN", cell, SectorCanvasOwnershipPlane.Terrain,
                            owner, task, anchor == null ? cell.SourceIdentity : anchor.SourceId,
                            digest, "SPECIAL_FIXED_SHELL", true, false, false, false));
                    else if (!cell.ProtectedNoWrite && owner == SectorCanvasOwnerKind.Boundary)
                        claims.Add(Claim("BOUNDARY_TERRAIN", cell, SectorCanvasOwnershipPlane.Terrain,
                            owner, task, anchor == null ? cell.SourceIdentity : anchor.SourceId,
                            digest, "BOUNDARY_FIXED_SLICE", true, false, false, false));
                }

                if (cell.PatternRendered && (cell.ProtectedNoWrite || cell.ReservedNoWrite))
                {
                    var noWriteRender = request.PatternRenderPlan.RenderCells.First(value =>
                        value.SectorIndex == cell.SectorIndex && value.Coordinate.Equals(cell.Coordinate));
                    claims.Add(Claim("PATTERN_NO_WRITE_EVIDENCE", cell,
                        SectorCanvasOwnershipPlane.Evidence, SectorCanvasOwnerKind.MicroPattern,
                        "MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS",
                        PatternCellIdentity(noWriteRender), request.PatternRenderPlan.CanonicalDigest,
                        "NO_WRITE|" + PatternSemantic(noWriteRender), true, true, true, false,
                        SectorCanvasClaimState.AllowedCoPlaneEvidence));
                }

                if (cell.ProtectedNoWrite || cell.ReservedNoWrite)
                    continue;

                if (cell.Kind == SectorQuietFillCellKind.AlreadyPatternRendered)
                {
                    var render = request.PatternRenderPlan.RenderCells.First(value =>
                        value.SectorIndex == cell.SectorIndex && value.Coordinate.Equals(cell.Coordinate));
                    claims.Add(Claim("PATTERN_TERRAIN", cell, SectorCanvasOwnershipPlane.Terrain,
                        SectorCanvasOwnerKind.MicroPattern,
                        "MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS",
                        PatternCellIdentity(render), request.PatternRenderPlan.CanonicalDigest,
                        PatternSemantic(render), true, true, false, false));
                    continue;
                }

                if (cell.Kind == SectorQuietFillCellKind.RouteMargin)
                {
                    claims.Add(Claim("SPINE_TERRAIN", cell, SectorCanvasOwnershipPlane.Terrain,
                        SectorCanvasOwnerKind.Spine,
                        "MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE",
                        cell.SourceIdentity, request.SpineEnvelopePlan.CanonicalDigest,
                        "ROUTE_ENVELOPE", true, false, false, false));
                    continue;
                }

                if (cell.Kind == SectorQuietFillCellKind.BoundaryMargin)
                {
                    claims.Add(Claim("BOUNDARY_MARGIN", cell, SectorCanvasOwnershipPlane.Terrain,
                        SectorCanvasOwnerKind.Boundary,
                        "MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS",
                        cell.SourceIdentity, request.FixedAnchorPlan.CanonicalDigest,
                        "BOUNDARY_MARGIN", true, false, false, false));
                    continue;
                }

                if (cell.Kind == SectorQuietFillCellKind.SpecialMargin)
                {
                    claims.Add(Claim("SPECIAL_MARGIN", cell, SectorCanvasOwnershipPlane.Terrain,
                        SectorCanvasOwnerKind.SpecialRegion,
                        "MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS",
                        cell.SourceIdentity, request.FixedAnchorPlan.CanonicalDigest,
                        "SPECIAL_APPROACH_MARGIN", true, false, false, false));
                    continue;
                }

                claims.Add(Claim("QUIET_TERRAIN", cell, SectorCanvasOwnershipPlane.Terrain,
                    SectorCanvasOwnerKind.Quiet,
                    "MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT",
                    cell.SourceIdentity, request.QuietActivityEventPlan.QuietFillPlan.CanonicalDigest,
                    cell.Kind.ToString(), true, true, false, false));
            }
        }

        private static void AddClusterClaims(
            SectorCanvasOwnershipBuildRequest request,
            ICollection<SectorCanvasOwnershipClaim> claims)
        {
            foreach (var placement in request.ClusterPlacementPlan.Placements.OrderBy(value => value.SectorIndex))
            foreach (var rect in placement.TileRects.OrderBy(value => value))
            for (var y = rect.Y; y < rect.YMaxExclusive; y++)
            for (var x = rect.X; x < rect.XMaxExclusive; x++)
            {
                var coordinate = new LocalTileCoord(x, y);
                claims.Add(new SectorCanvasOwnershipClaim(
                    Id("CLUSTER_TERRAIN", placement.SectorIndex, coordinate,
                        placement.ClusterId.Value + "|" + placement.VariantId.Value),
                    placement.SectorCoordinate, placement.SectorIndex, coordinate,
                    SectorCanvasOwnershipPlane.Terrain, SectorCanvasOwnerKind.TerrainCluster,
                    SectorCanvasOwnershipPriority.TerrainCluster,
                    "MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES",
                    placement.ClusterId.Value + "|" + placement.VariantId.Value,
                    request.ClusterPlacementPlan.CanonicalDigest,
                    placement.MatchedPacingRole + "|" + placement.Transform,
                    true, true, false, false));
            }
        }

        private static void AddAnchorEvidenceClaims(
            SectorCanvasOwnershipBuildRequest request,
            ICollection<SectorCanvasOwnershipClaim> claims)
        {
            foreach (var anchor in request.FixedAnchorPlan.Anchors.OrderBy(value => value.SectorIndex)
                         .ThenBy(value => value.AnchorId, StringComparer.Ordinal))
            for (var y = anchor.Rect.Y; y < anchor.Rect.YMaxExclusive; y++)
            for (var x = anchor.Rect.X; x < anchor.Rect.XMaxExclusive; x++)
            {
                var coordinate = new LocalTileCoord(x, y);
                var owner = OwnerForAnchor(anchor.Kind);
                claims.Add(new SectorCanvasOwnershipClaim(
                    Id("ANCHOR_EVIDENCE", anchor.SectorIndex, coordinate, anchor.AnchorId),
                    anchor.SectorCoordinate, anchor.SectorIndex, coordinate,
                    SectorCanvasOwnershipPlane.Evidence, owner, PriorityFor(owner),
                    "MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS",
                    anchor.AnchorId + "|" + anchor.SourceId,
                    request.FixedAnchorPlan.CanonicalDigest,
                    anchor.Kind + "|" + anchor.CompatibilityGroup,
                    true, true, true, false, SectorCanvasClaimState.AllowedCoPlaneEvidence));
            }
        }

        private static void AddSpineEvidenceClaims(
            SectorCanvasOwnershipBuildRequest request,
            ICollection<SectorCanvasOwnershipClaim> claims)
        {
            var sectorByIndex = request.Input.Sectors.ToDictionary(value => value.SectorIndex);
            foreach (var cell in request.SpineEnvelopePlan.EnvelopeCells)
            {
                var sector = sectorByIndex[cell.SectorIndex];
                claims.Add(new SectorCanvasOwnershipClaim(
                    Id("SPINE_EVIDENCE", cell.SectorIndex, cell.Coordinate,
                        cell.EdgeId + "|" + cell.Kind + "|" + cell.SourceIdentity),
                    sector.Coordinate, cell.SectorIndex, cell.Coordinate,
                    SectorCanvasOwnershipPlane.Evidence, SectorCanvasOwnerKind.Spine,
                    SectorCanvasOwnershipPriority.Spine,
                    "MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE",
                    cell.EdgeId, request.SpineEnvelopePlan.CanonicalDigest,
                    cell.Kind + "|" + cell.SourceIdentity,
                    true, true, true, false, SectorCanvasClaimState.AllowedCoPlaneEvidence));
            }
            foreach (var node in request.SpineEnvelopePlan.Graph.Nodes)
                claims.Add(new SectorCanvasOwnershipClaim(
                    Id("SPINE_NODE", node.SectorIndex, node.Coordinate, node.NodeId),
                    node.SectorCoordinate, node.SectorIndex, node.Coordinate,
                    SectorCanvasOwnershipPlane.Evidence, SectorCanvasOwnerKind.Spine,
                    SectorCanvasOwnershipPriority.Spine,
                    "MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE",
                    node.NodeId, request.SpineEnvelopePlan.SpineGraphDigest,
                    node.Kind + "|" + node.EndpointRole + "|" + node.SourceIdentity,
                    true, true, true, false, SectorCanvasClaimState.AllowedCoPlaneEvidence));
        }

        private static void AddRoleAndPatternEvidenceClaims(
            SectorCanvasOwnershipBuildRequest request,
            ICollection<SectorCanvasOwnershipClaim> claims)
        {
            foreach (var role in request.RolePatternPlan.RoleCells)
            {
                var coordinate = new LocalTileCoord(role.TileRect.X, role.TileRect.Y);
                claims.Add(new SectorCanvasOwnershipClaim(
                    Id("ROLE_CELL", role.SectorIndex, coordinate,
                        role.ClusterId.Value + "|" + role.FootprintCell + "|" + role.Kind),
                    role.SectorCoordinate, role.SectorIndex, coordinate,
                    SectorCanvasOwnershipPlane.Evidence, SectorCanvasOwnerKind.TerrainCluster,
                    SectorCanvasOwnershipPriority.TerrainCluster,
                    "MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS",
                    role.ClusterId.Value + "|" + role.VariantId.Value,
                    request.RolePatternPlan.CanonicalDigest,
                    role.Kind + "|" + role.PacingRole + "|" + role.SourceNodeId + "|" + role.SourceEdgeId,
                    true, true, true, false, SectorCanvasClaimState.AllowedCoPlaneEvidence));
            }
            foreach (var selection in request.PatternRenderPlan.Selections)
            {
                var zone = request.RolePatternPlan.PatternZones.First(value =>
                    value.SectorIndex == selection.SectorIndex &&
                    string.Equals(value.ZoneId, selection.ZoneId, StringComparison.Ordinal));
                var coordinate = new LocalTileCoord(zone.TileRect.X, zone.TileRect.Y);
                claims.Add(new SectorCanvasOwnershipClaim(
                    Id("PATTERN_SELECTION", selection.SectorIndex, coordinate,
                        selection.ZoneId + "|" + selection.PatternId.Value),
                    zone.SectorCoordinate, selection.SectorIndex, coordinate,
                    SectorCanvasOwnershipPlane.Evidence, SectorCanvasOwnerKind.MicroPattern,
                    SectorCanvasOwnershipPriority.MicroPattern,
                    "MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS",
                    selection.PatternId.Value + "|" + selection.ZoneId,
                    selection.ApplicationPlanDigest,
                    selection.Transform + "|" + selection.RendererRequestId,
                    true, true, true, false, SectorCanvasClaimState.AllowedCoPlaneEvidence));
            }
        }

        private static void AddSpecialAndDeferredEvidenceClaims(
            SectorCanvasOwnershipBuildRequest request,
            ICollection<SectorCanvasOwnershipClaim> claims)
        {
            foreach (var sector in request.Input.Sectors)
            {
                var special = sector.SpecialRegion;
                if (special != null && special.Kind != SectorPlannerSpecialRegionKind.None)
                {
                    var deferred = special.Binding == SectorPlannerSpecialRegionBinding.DeferredOptionalLocal;
                    var owner = deferred ? SectorCanvasOwnerKind.Empty : SectorCanvasOwnerKind.SpecialRegion;
                    var coordinate = new LocalTileCoord(24, 16);
                    claims.Add(new SectorCanvasOwnershipClaim(
                        Id("SPECIAL_SNAPSHOT", sector.SectorIndex, coordinate, special.RegionId),
                        sector.Coordinate, sector.SectorIndex, coordinate,
                        SectorCanvasOwnershipPlane.Evidence, owner, PriorityFor(owner),
                        "MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS",
                        special.RegionId, request.RolePatternPlan.SpecialIdentityBefore,
                        special.Kind + "|" + special.Binding + "|" + special.FootprintId,
                        true, true, true, true, SectorCanvasClaimState.AllowedCoPlaneEvidence));
                }
                foreach (var optional in sector.OptionalRegions)
                {
                    var coordinate = new LocalTileCoord(23, 16);
                    claims.Add(new SectorCanvasOwnershipClaim(
                        Id("DEFERRED_OPTIONAL", sector.SectorIndex, coordinate, optional.RegionId),
                        sector.Coordinate, sector.SectorIndex, coordinate,
                        SectorCanvasOwnershipPlane.Evidence, SectorCanvasOwnerKind.Empty,
                        SectorCanvasOwnershipPriority.Evidence,
                        "MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS",
                        optional.RegionId, request.RolePatternPlan.SpecialIdentityBefore,
                        optional.Kind + "|DEFERRED_LOCAL=" + optional.DeferredLocal,
                        false, true, true, true, SectorCanvasClaimState.AllowedCoPlaneEvidence));
                }
            }
        }

        private static void AddActivityEventClaims(
            SectorCanvasOwnershipBuildRequest request,
            ICollection<SectorCanvasOwnershipClaim> claims)
        {
            foreach (var decision in request.QuietActivityEventPlan.ActivityDecisions)
            {
                var opportunity = decision.Opportunity;
                var marker = decision.State == SectorActivityEventPlacementState.Selected;
                var sectorIndex = (opportunity.SectorCoordinate.Y * 13) + opportunity.SectorCoordinate.X;
                claims.Add(new SectorCanvasOwnershipClaim(
                    Id("ACTIVITY_DECISION", sectorIndex,
                        opportunity.MarkerCoordinate, opportunity.OpportunityId),
                    opportunity.SectorCoordinate, sectorIndex,
                    opportunity.MarkerCoordinate,
                    marker ? SectorCanvasOwnershipPlane.Marker : SectorCanvasOwnershipPlane.Evidence,
                    SectorCanvasOwnerKind.ActivityMarker,
                    SectorCanvasOwnershipPriority.ActivityMarker,
                    "MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT",
                    opportunity.OpportunityId,
                    request.QuietActivityEventPlan.ActivityFrequencyPlanDigestBefore,
                    decision.State + "|" + decision.ActivityId.Value + "|" + opportunity.MarkerKind,
                    marker, true, true, true,
                    marker ? SectorCanvasClaimState.Winner : SectorCanvasClaimState.AllowedCoPlaneEvidence));
            }
            foreach (var decision in request.QuietActivityEventPlan.EventDecisions)
            {
                var opportunity = decision.Opportunity;
                var marker = decision.State == SectorActivityEventPlacementState.Assigned;
                var owner = marker ? SectorCanvasOwnerKind.EventMarker : SectorCanvasOwnerKind.Empty;
                var sectorIndex = (opportunity.SectorCoordinate.Y * 13) + opportunity.SectorCoordinate.X;
                claims.Add(new SectorCanvasOwnershipClaim(
                    Id("EVENT_DECISION", sectorIndex,
                        opportunity.MarkerCoordinate, opportunity.OpportunityId),
                    opportunity.SectorCoordinate, sectorIndex,
                    opportunity.MarkerCoordinate,
                    marker ? SectorCanvasOwnershipPlane.Marker : SectorCanvasOwnershipPlane.Evidence,
                    owner, PriorityFor(owner),
                    "MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT",
                    opportunity.OpportunityId + "|" + opportunity.OwnerIdentity,
                    request.QuietActivityEventPlan.EventAssignmentPlanDigestBefore,
                    decision.State + "|" + decision.EventId.Value + "|" + opportunity.MarkerKind,
                    true, true, true, true,
                    marker ? SectorCanvasClaimState.Winner : SectorCanvasClaimState.AllowedCoPlaneEvidence));
            }
        }

        private static void ValidateClaims(
            SectorCanvasOwnershipBuildRequest request,
            IReadOnlyList<SectorCanvasOwnershipClaim> claims,
            ICollection<SectorCanvasOwnershipError> errors)
        {
            var sectors = request.Input.Sectors.ToDictionary(value => value.SectorIndex);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var claim in claims)
            {
                if (string.IsNullOrEmpty(claim.ClaimId) || !ids.Add(claim.ClaimId))
                    Add(errors, SectorCanvasOwnershipErrorCode.DuplicateClaimIdentity,
                        claim.ClaimId, "Every ownership claim ID must be non-empty and unique.");
                if (!sectors.TryGetValue(claim.SectorIndex, out var sector) ||
                    !sector.Coordinate.Equals(claim.SectorCoordinate))
                    Add(errors, SectorCanvasOwnershipErrorCode.SectorMismatch,
                        claim.ClaimId, "Claim sector coordinate and sector index must match public input.");
                if (claim.Coordinate.X < 0 || claim.Coordinate.X >= 48 ||
                    claim.Coordinate.Y < 0 || claim.Coordinate.Y >= 32)
                    Add(errors, SectorCanvasOwnershipErrorCode.ClaimOutOfBounds,
                        claim.ClaimId, "Claim coordinate is outside the 48x32 sector canvas.");
                var priority = PriorityFor(claim.OwnerKind);
                if (priority == 0 || priority != claim.Priority)
                    Add(errors, SectorCanvasOwnershipErrorCode.MissingPriorityRule,
                        claim.ClaimId, "Claim owner must use the declared deterministic priority.");
                if ((claim.OwnerKind == SectorCanvasOwnerKind.ActivityMarker ||
                     claim.OwnerKind == SectorCanvasOwnerKind.EventMarker) &&
                    (!claim.MarkerOnly || (claim.Plane != SectorCanvasOwnershipPlane.Marker &&
                                           claim.Plane != SectorCanvasOwnershipPlane.Evidence)))
                    Add(errors,
                        claim.OwnerKind == SectorCanvasOwnerKind.ActivityMarker
                            ? SectorCanvasOwnershipErrorCode.ActivityMarkerMutationClaim
                            : SectorCanvasOwnershipErrorCode.EventMarkerMutationClaim,
                        claim.ClaimId, "Activity/Event claims must remain marker-only or evidence-only.");
            }

            RequireWhen(errors, claims, request.FixedAnchorPlan.Anchors.Any(IsBoundary),
                SectorCanvasOwnerKind.Boundary, "boundary");
            RequireWhen(errors, claims, request.Input.Sectors.Any(value =>
                    value.SpecialRegion.Kind != SectorPlannerSpecialRegionKind.None &&
                    value.SpecialRegion.Binding != SectorPlannerSpecialRegionBinding.DeferredOptionalLocal),
                SectorCanvasOwnerKind.SpecialRegion, "specialRegion");
            RequireWhen(errors, claims, request.ClusterPlacementPlan.Placements.Count > 0,
                SectorCanvasOwnerKind.TerrainCluster, "clusterPlacement");
            RequireWhen(errors, claims, request.PatternRenderPlan.RenderCells.Count > 0,
                SectorCanvasOwnerKind.MicroPattern, "patternRender");
            RequireWhen(errors, claims, request.QuietActivityEventPlan.QuietFillPlan.QuietFillCellCount > 0,
                SectorCanvasOwnerKind.Quiet, "quietFill");
            RequireWhen(errors, claims, request.QuietActivityEventPlan.ActivitySelectedCount > 0,
                SectorCanvasOwnerKind.ActivityMarker, "activityMarker");
            RequireWhen(errors, claims, request.QuietActivityEventPlan.EventAssignedNonEmptyCount > 0,
                SectorCanvasOwnerKind.EventMarker, "eventMarker");
            RequireWhen(errors, claims, request.QuietActivityEventPlan.EventAssignedEmptyCount > 0,
                SectorCanvasOwnerKind.Empty, "explicitEmpty");
        }

        private static void RequireWhen(
            ICollection<SectorCanvasOwnershipError> errors,
            IEnumerable<SectorCanvasOwnershipClaim> claims,
            bool required,
            SectorCanvasOwnerKind owner,
            string subject)
        {
            if (required && !claims.Any(value => value.OwnerKind == owner))
                Add(errors, SectorCanvasOwnershipErrorCode.MissingRequiredClaim,
                    subject, "Required upstream source did not publish an ownership/evidence claim.");
        }

        private static SectorCanvasOwnerKind OwnerForReserved(SectorQuietFillSourceKind source)
        {
            switch (source)
            {
                case SectorQuietFillSourceKind.BoundaryAnchor:
                    return SectorCanvasOwnerKind.Boundary;
                case SectorQuietFillSourceKind.SpecialAnchor:
                case SectorQuietFillSourceKind.SpecialFixedShell:
                case SectorQuietFillSourceKind.VillageReference:
                    return SectorCanvasOwnerKind.SpecialRegion;
                case SectorQuietFillSourceKind.RouteEnvelope:
                    return SectorCanvasOwnerKind.Spine;
                default:
                    return SectorCanvasOwnerKind.ReservedNoWrite;
            }
        }

        private static SectorCanvasOwnerKind OwnerForAnchor(SectorFixedAnchorKind kind)
        {
            if (IsBoundary(kind)) return SectorCanvasOwnerKind.Boundary;
            if (kind == SectorFixedAnchorKind.ExternalRouteSocket) return SectorCanvasOwnerKind.Spine;
            return SectorCanvasOwnerKind.SpecialRegion;
        }

        private static bool IsBoundary(SectorFixedAnchor anchor) => anchor != null && IsBoundary(anchor.Kind);
        private static bool IsBoundary(SectorFixedAnchorKind kind) =>
            kind == SectorFixedAnchorKind.BoundaryFixedSlice ||
            kind == SectorFixedAnchorKind.BoundaryWarning;

        private static bool Contains(SectorFixedAnchorRect rect, LocalTileCoord coordinate) =>
            rect != null && coordinate.X >= rect.X && coordinate.X < rect.XMaxExclusive &&
            coordinate.Y >= rect.Y && coordinate.Y < rect.YMaxExclusive;

        private static SectorCanvasOwnershipClaim Claim(
            string prefix,
            SectorQuietFillCell cell,
            SectorCanvasOwnershipPlane plane,
            SectorCanvasOwnerKind owner,
            string task,
            string sourceObject,
            string digest,
            string semantic,
            bool required,
            bool allowSuppression,
            bool noWrite,
            bool markerOnly,
            SectorCanvasClaimState state = SectorCanvasClaimState.Winner) =>
            new SectorCanvasOwnershipClaim(
                Id(prefix, cell.SectorIndex, cell.Coordinate, sourceObject),
                cell.SectorCoordinate, cell.SectorIndex, cell.Coordinate, plane, owner,
                PriorityFor(owner), task, sourceObject, digest, semantic,
                required, allowSuppression, noWrite, markerOnly, state);

        private static string PatternCellIdentity(SectorPatternRenderCell cell) =>
            "MAP14_05_RENDER|" + cell.SectorIndex.ToString("D3", CultureInfo.InvariantCulture) + "|" +
            cell.Coordinate.X.ToString("D2", CultureInfo.InvariantCulture) + "," +
            cell.Coordinate.Y.ToString("D2", CultureInfo.InvariantCulture);

        private static string PatternSemantic(SectorPatternRenderCell cell) => string.Join("|", new[]
        {
            cell.Solid ? "SOLID" : "AIR", cell.SurfaceId, cell.AffordanceId,
            cell.MaterialId, cell.HazardId, cell.MarkerId,
            cell.ProvenanceCount.ToString(CultureInfo.InvariantCulture),
        });

        private static string Id(
            string prefix,
            int sectorIndex,
            LocalTileCoord coordinate,
            string source) =>
            prefix + "|" + sectorIndex.ToString("D3", CultureInfo.InvariantCulture) + "|" +
            coordinate.X.ToString("D2", CultureInfo.InvariantCulture) + "," +
            coordinate.Y.ToString("D2", CultureInfo.InvariantCulture) + "|" + (source ?? string.Empty);

        private static SectorCanvasOwnershipBuildResult Failure(
            SectorCanvasOwnershipBuildRequest request,
            IEnumerable<SectorCanvasOwnershipError> errors) =>
            new SectorCanvasOwnershipBuildResult(
                request, Array.Empty<SectorCanvasOwnershipClaim>(), null,
                string.Empty, errors);

        private static void Add(
            ICollection<SectorCanvasOwnershipError> errors,
            SectorCanvasOwnershipErrorCode code,
            string subject,
            string detail) =>
            errors.Add(new SectorCanvasOwnershipError(code, subject, detail));

        private static void AddCount(
            ICollection<SectorCanvasOwnershipError> errors,
            SectorCanvasOwnershipErrorCode code,
            string subject,
            int count)
        {
            if (count != 0)
                Add(errors, code, subject, count.ToString(CultureInfo.InvariantCulture));
        }
    }
}
