using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorQuietFillPlanner
    {
        public const string ReferencePublicationLabel = "REFERENCE QUIET ACTIVITY EVENT";

        public static SectorQuietFillBuildResult Fill(SectorQuietActivityEventBuildRequest request)
        {
            var errors = new List<SectorQuietActivityEventError>();
            ValidateRequest(request, errors);
            if (errors.Count != 0) return Failure(errors);

            var sectorByIndex = request.Input.Sectors.ToDictionary(value => value.SectorIndex);
            var assignmentByIndex = request.Assignments.ToDictionary(
                value => (value.Coordinate.Y * 13) + value.Coordinate.X);
            var renderByKey = request.PatternRenderPlan.RenderCells
                .GroupBy(value => Key(value.SectorIndex, value.Coordinate))
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            var protectedKeys = new HashSet<string>(request.SpineEnvelopePlan.ProtectedOpenCells
                .Select(value => Key(value.SectorIndex, value.Coordinate)), StringComparer.Ordinal);
            var envelopeKeys = new HashSet<string>(request.SpineEnvelopePlan.EnvelopeCells
                .Select(value => Key(value.SectorIndex, value.Coordinate)), StringComparer.Ordinal);
            var anchorsBySector = request.AnchorPlan.Anchors.GroupBy(value => value.SectorIndex)
                .ToDictionary(value => value.Key, value => value.OrderBy(item => item.AnchorId, StringComparer.Ordinal).ToArray());

            var cells = new List<SectorQuietFillCell>(request.Input.Sectors.Count * 48 * 32);
            foreach (var sector in request.Input.Sectors.OrderBy(value => value.SectorIndex))
            {
                var anchors = anchorsBySector.TryGetValue(sector.SectorIndex, out var values)
                    ? values
                    : Array.Empty<SectorFixedAnchor>();
                var assignment = assignmentByIndex[sector.SectorIndex];
                for (var y = 0; y < sector.CanvasHeight; y++)
                for (var x = 0; x < sector.CanvasWidth; x++)
                {
                    var coordinate = new LocalTileCoord(x, y);
                    var key = Key(sector.SectorIndex, coordinate);
                    var pattern = renderByKey.ContainsKey(key);
                    var protectedOpen = protectedKeys.Contains(key);
                    var owningAnchor = anchors.FirstOrDefault(value => Contains(value.Rect, coordinate));
                    var reserved = owningAnchor != null;
                    var envelope = envelopeKeys.Contains(key);
                    var marginAnchor = owningAnchor ?? anchors.FirstOrDefault(value => IsMargin(value.Rect, coordinate));
                    var classification = Classify(sector, assignment, pattern, protectedOpen,
                        reserved, envelope, marginAnchor);
                    cells.Add(new SectorQuietFillCell(
                        sector.Coordinate, sector.SectorIndex, coordinate,
                        classification.Kind, classification.SourceKind, classification.SourceIdentity,
                        protectedOpen, reserved, pattern,
                        classification.ActivityEligible, classification.EventEligible));
                }
            }

            ValidateCells(request, cells, errors);
            if (errors.Count != 0) return Failure(errors);

            var provisional = new SectorQuietFillPlan(
                request,
                cells,
                protectedKeys.Count,
                cells.Count(value => value.ReservedNoWrite),
                renderByKey.Count,
                string.Empty);
            var digest = SectorQuietActivityEventCanonicalDigest.ComputeQuiet(provisional);
            if (!string.IsNullOrEmpty(request.ExpectedCanonicalDigest) &&
                !string.Equals(request.ExpectedCanonicalDigest, digest, StringComparison.Ordinal))
            {
                Add(errors, SectorQuietActivityEventErrorCode.NonCanonicalPublication,
                    "expectedCanonicalDigest", "Published Quiet fill digest did not match the expected digest.");
                return Failure(errors);
            }

            var plan = new SectorQuietFillPlan(
                request,
                cells,
                protectedKeys.Count,
                cells.Count(value => value.ReservedNoWrite),
                renderByKey.Count,
                digest);
            return new SectorQuietFillBuildResult(plan, digest, Array.Empty<SectorQuietActivityEventError>());
        }

        private static void ValidateRequest(
            SectorQuietActivityEventBuildRequest request,
            ICollection<SectorQuietActivityEventError> errors)
        {
            if (request == null)
            {
                Add(errors, SectorQuietActivityEventErrorCode.MissingInput, "request", "Build request is required.");
                return;
            }
            if (request.Input == null)
                Add(errors, SectorQuietActivityEventErrorCode.MissingInput, "input", "SectorPlannerInput is required.");
            if (request.AnchorPlan == null)
                Add(errors, SectorQuietActivityEventErrorCode.MissingInput, "anchorPlan", "SectorFixedAnchorPlan is required.");
            if (request.ClusterPlacementPlan == null)
                Add(errors, SectorQuietActivityEventErrorCode.MissingInput, "clusterPlacementPlan", "SectorClusterPlacementPlan is required.");
            if (request.SpineEnvelopePlan == null)
                Add(errors, SectorQuietActivityEventErrorCode.MissingSpineEnvelopePlan, "spineEnvelopePlan", "SectorSpineEnvelopePlan is required.");
            if (request.RoleZonePlan == null)
                Add(errors, SectorQuietActivityEventErrorCode.MissingInput, "roleZonePlan", "SectorClusterRolePatternPlan is required.");
            if (request.PatternRenderPlan == null)
                Add(errors, SectorQuietActivityEventErrorCode.MissingPatternRenderPlan, "patternRenderPlan", "SectorPatternRenderPlan is required.");
            if (errors.Count != 0) return;

            if (request.Input.Sectors.Count == 0 ||
                request.Input.Sectors.Any(value => value.CanvasWidth != 48 || value.CanvasHeight != 32))
                Add(errors, SectorQuietActivityEventErrorCode.SectorMismatch, "input.sectors", "Every reference sector must be exactly 48x32.");
            var assignments = request.Assignments.Where(value => value != null).ToArray();
            if (assignments.Length != request.Input.Sectors.Count ||
                assignments.Select(value => value.Coordinate).Distinct().Count() != assignments.Length ||
                request.Input.Sectors.Any(sector => assignments.Count(value => value.Coordinate == sector.Coordinate) != 1))
                Add(errors, SectorQuietActivityEventErrorCode.SectorMismatch, "assignments", "Exactly one pacing assignment is required per sector.");
            if (request.AnchorPlan.SectorCount != request.Input.Sectors.Count ||
                request.ClusterPlacementPlan.SectorCount != request.Input.Sectors.Count ||
                request.SpineEnvelopePlan.SectorCount != request.Input.Sectors.Count ||
                request.RoleZonePlan.SectorCount != request.Input.Sectors.Count ||
                request.PatternRenderPlan.SectorCount != request.Input.Sectors.Count)
                Add(errors, SectorQuietActivityEventErrorCode.SectorMismatch, "plans", "All public plans must cover the same sector set.");
            if (!request.PatternRenderPlan.Map14_06HandoffReady ||
                !string.Equals(request.PatternRenderPlan.RoleZonePlanDigest, request.RoleZonePlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.RoleZonePlan.PlannerInputDigestBefore, request.Input.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.RoleZonePlan.AnchorPlanDigestBefore, request.AnchorPlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.RoleZonePlan.ClusterPlacementPlanDigestBefore, request.ClusterPlacementPlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.RoleZonePlan.SpineEnvelopePlanDigestBefore, request.SpineEnvelopePlan.CanonicalDigest, StringComparison.Ordinal))
                Add(errors, SectorQuietActivityEventErrorCode.NonCanonicalPublication, "handoff", "MAP14_01-05 public digest chain must be exact and render-ready.");
            if (!string.Equals(request.PublicationLabel, ReferencePublicationLabel, StringComparison.Ordinal))
                Add(errors, SectorQuietActivityEventErrorCode.NonCanonicalPublication, "publicationLabel", "Reference publication label is required.");

            foreach (var fault in request.ReferenceFaults)
                Add(errors, fault, "referenceFaults", "Injected reference fault must fail atomically.");
            if (request.PatternCanvasMutationClaim)
                Add(errors, SectorQuietActivityEventErrorCode.PatternCanvasMutationClaim, "mutation.patternCanvas", "Pattern canvas mutation is not owned by MAP14_06.");
            if (request.AnchorMutationClaim)
                Add(errors, SectorQuietActivityEventErrorCode.AnchorMutationClaim, "mutation.anchor", "Anchor mutation is not owned by MAP14_06.");
            if (request.ClusterMutationClaim)
                Add(errors, SectorQuietActivityEventErrorCode.ClusterMutationClaim, "mutation.cluster", "Cluster mutation is not owned by MAP14_06.");
            if (request.SpineEnvelopeMutationClaim)
                Add(errors, SectorQuietActivityEventErrorCode.SpineEnvelopeMutationClaim, "mutation.spineEnvelope", "Spine/envelope mutation is not owned by MAP14_06.");
            if (request.OwnershipMutationClaim)
                Add(errors, SectorQuietActivityEventErrorCode.OwnershipMutationClaim, "mutation.ownership", "Final ownership is owned by MAP14_07.");
            AddCount(errors, SectorQuietActivityEventErrorCode.SolverMutationClaim, "mutation.solver", request.SolverInvocationCount);
            AddCount(errors, SectorQuietActivityEventErrorCode.RngMutationClaim, "mutation.map14Rng", request.Map14RngDrawCount);
            AddCount(errors, SectorQuietActivityEventErrorCode.SolverMutationClaim, "mutation.retry", request.RetryCount);
            AddCount(errors, SectorQuietActivityEventErrorCode.TileMutationClaim, "mutation.tile", request.TileWriteCount);
        }

        private static void ValidateCells(
            SectorQuietActivityEventBuildRequest request,
            IReadOnlyList<SectorQuietFillCell> cells,
            ICollection<SectorQuietActivityEventError> errors)
        {
            var expected = request.Input.Sectors.Count * 48 * 32;
            if (cells.Count != expected)
                Add(errors, SectorQuietActivityEventErrorCode.NonCanonicalPublication, "cells", "Every sector-local tile must be classified exactly once.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cell in cells)
            {
                if (cell.Coordinate.X < 0 || cell.Coordinate.X >= 48 || cell.Coordinate.Y < 0 || cell.Coordinate.Y >= 32)
                    Add(errors, SectorQuietActivityEventErrorCode.QuietCellOutOfBounds,
                        Key(cell.SectorIndex, cell.Coordinate), "Quiet classification is outside 48x32 bounds.");
                if (!keys.Add(Key(cell.SectorIndex, cell.Coordinate)))
                    Add(errors, SectorQuietActivityEventErrorCode.DuplicateQuietCell,
                        Key(cell.SectorIndex, cell.Coordinate), "Quiet classification coordinate is duplicated.");
                if (cell.IsQuietFill && cell.ProtectedNoWrite)
                    Add(errors, SectorQuietActivityEventErrorCode.QuietCellTouchesProtectedOpen,
                        Key(cell.SectorIndex, cell.Coordinate), "Quiet fill cannot overlap ProtectedOpen.");
                if (cell.IsQuietFill && (cell.ReservedNoWrite || cell.PatternRendered))
                    Add(errors, SectorQuietActivityEventErrorCode.QuietCellTouchesFinalOwner,
                        Key(cell.SectorIndex, cell.Coordinate), "Quiet fill cannot overlap reserved or rendered evidence.");
            }
        }

        private static Classification Classify(
            SectorPlannerSectorSnapshot sector,
            SectorPacingAssignment assignment,
            bool pattern,
            bool protectedOpen,
            bool reserved,
            bool envelope,
            SectorFixedAnchor marginAnchor)
        {
            if (pattern)
                return new Classification(SectorQuietFillCellKind.AlreadyPatternRendered,
                    SectorQuietFillSourceKind.ReferencePatternCanvas, "MAP14_05_RENDER", false, false);
            if (protectedOpen)
                return new Classification(SectorQuietFillCellKind.ProtectedNoWrite,
                    SectorQuietFillSourceKind.ProtectedOpen, "MAP14_04_PROTECTED_OPEN", false, false);
            if (reserved)
                return new Classification(SectorQuietFillCellKind.ReservedNoWrite,
                    SourceForAnchor(marginAnchor), marginAnchor.AnchorId, false, false);
            if (envelope)
                return new Classification(SectorQuietFillCellKind.RouteMargin,
                    SectorQuietFillSourceKind.RouteEnvelope, "MAP14_04_ROUTE_ENVELOPE", false, false);
            if (marginAnchor != null)
            {
                if (IsBoundary(marginAnchor.Kind))
                    return new Classification(SectorQuietFillCellKind.BoundaryMargin,
                        SectorQuietFillSourceKind.BoundaryAnchor, marginAnchor.AnchorId, false, true);
                if (IsSpecial(marginAnchor.Kind))
                    return new Classification(SectorQuietFillCellKind.SpecialMargin,
                        marginAnchor.Kind == SectorFixedAnchorKind.SpecialFootprint
                            ? SectorQuietFillSourceKind.SpecialFixedShell
                            : SectorQuietFillSourceKind.SpecialAnchor,
                        marginAnchor.AnchorId, false, true);
            }
            if (assignment.PrimaryRole == PacingRole.Activity && sector.ActivityCatalogAvailable)
                return new Classification(SectorQuietFillCellKind.ActivityCandidate,
                    SectorQuietFillSourceKind.ActivityCompatibility, assignment.CanonicalDigest, true, true);
            if (sector.EventCatalogAvailable || sector.SpecialRegion.Kind != SectorPlannerSpecialRegionKind.None)
                return new Classification(SectorQuietFillCellKind.EventCandidate,
                    SectorQuietFillSourceKind.EventMarkerOpportunity,
                    sector.SpecialRegion.RegionId, true, true);
            if (sector.QuietCompatible)
                return new Classification(SectorQuietFillCellKind.QuietBuffer,
                    SectorQuietFillSourceKind.ManualReferenceFixture, "QUIET_COMPATIBLE", true, true);
            var parity = (sector.SectorIndex + assignment.PrimaryRole.GetHashCode()) & 1;
            return parity == 0
                ? new Classification(SectorQuietFillCellKind.QuietAir,
                    SectorQuietFillSourceKind.ManualReferenceFixture, "QUIET_AIR", true, true)
                : new Classification(SectorQuietFillCellKind.QuietSolid,
                    SectorQuietFillSourceKind.ManualReferenceFixture, "QUIET_SOLID", true, true);
        }

        private static SectorQuietFillSourceKind SourceForAnchor(SectorFixedAnchor anchor)
        {
            if (anchor == null) return SectorQuietFillSourceKind.ManualReferenceFixture;
            if (anchor.Kind == SectorFixedAnchorKind.ReferenceOnlyMarker)
                return SectorQuietFillSourceKind.VillageReference;
            if (IsBoundary(anchor.Kind)) return SectorQuietFillSourceKind.BoundaryAnchor;
            if (anchor.Kind == SectorFixedAnchorKind.ExternalRouteSocket)
                return SectorQuietFillSourceKind.RouteEnvelope;
            return anchor.Kind == SectorFixedAnchorKind.SpecialFootprint
                ? SectorQuietFillSourceKind.SpecialFixedShell
                : SectorQuietFillSourceKind.SpecialAnchor;
        }

        private static bool IsBoundary(SectorFixedAnchorKind kind) =>
            kind == SectorFixedAnchorKind.BoundaryFixedSlice || kind == SectorFixedAnchorKind.BoundaryWarning;

        private static bool IsSpecial(SectorFixedAnchorKind kind) =>
            kind == SectorFixedAnchorKind.SpecialFootprint ||
            kind == SectorFixedAnchorKind.SpecialEntryReturn ||
            kind == SectorFixedAnchorKind.SpecialApronBuffer ||
            kind == SectorFixedAnchorKind.SiteReservation ||
            kind == SectorFixedAnchorKind.ReferenceOnlyMarker;

        private static bool Contains(SectorFixedAnchorRect rect, LocalTileCoord coordinate) =>
            coordinate.X >= rect.X && coordinate.X < rect.XMaxExclusive &&
            coordinate.Y >= rect.Y && coordinate.Y < rect.YMaxExclusive;

        private static bool IsMargin(SectorFixedAnchorRect rect, LocalTileCoord coordinate) =>
            coordinate.X >= Math.Max(0, rect.X - 1) && coordinate.X < Math.Min(48, rect.XMaxExclusive + 1) &&
            coordinate.Y >= Math.Max(0, rect.Y - 1) && coordinate.Y < Math.Min(32, rect.YMaxExclusive + 1);

        private static string Key(int sectorIndex, LocalTileCoord coordinate) =>
            sectorIndex.ToString(CultureInfo.InvariantCulture) + "|" +
            coordinate.X.ToString(CultureInfo.InvariantCulture) + "," +
            coordinate.Y.ToString(CultureInfo.InvariantCulture);

        private static SectorQuietFillBuildResult Failure(IEnumerable<SectorQuietActivityEventError> errors) =>
            new SectorQuietFillBuildResult(null, string.Empty, errors);

        private static void Add(
            ICollection<SectorQuietActivityEventError> errors,
            SectorQuietActivityEventErrorCode code,
            string subject,
            string detail) => errors.Add(new SectorQuietActivityEventError(code, subject, detail));

        private static void AddCount(
            ICollection<SectorQuietActivityEventError> errors,
            SectorQuietActivityEventErrorCode code,
            string subject,
            int count)
        {
            if (count != 0) Add(errors, code, subject, count.ToString(CultureInfo.InvariantCulture));
        }

        private sealed class Classification
        {
            internal Classification(
                SectorQuietFillCellKind kind,
                SectorQuietFillSourceKind sourceKind,
                string sourceIdentity,
                bool activityEligible,
                bool eventEligible)
            {
                Kind = kind;
                SourceKind = sourceKind;
                SourceIdentity = sourceIdentity ?? string.Empty;
                ActivityEligible = activityEligible;
                EventEligible = eventEligible;
            }

            internal SectorQuietFillCellKind Kind { get; }
            internal SectorQuietFillSourceKind SourceKind { get; }
            internal string SourceIdentity { get; }
            internal bool ActivityEligible { get; }
            internal bool EventEligible { get; }
        }
    }
}
