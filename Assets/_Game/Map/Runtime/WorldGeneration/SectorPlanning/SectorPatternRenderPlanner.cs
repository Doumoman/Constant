using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorPatternRenderPlanner
    {
        public const string ReferencePublicationLabel = "REFERENCE PATTERN CANVAS";

        public static SectorPatternRenderBuildResult Render(SectorPatternRenderRequest request)
        {
            var errors = new List<SectorPatternRenderError>();
            ValidateRequest(request, errors);
            if (errors.Count != 0) return Failure(errors);

            string sourceCatalogDigest;
            try
            {
                sourceCatalogDigest = SectorPatternRenderCanonicalDigest.ComputePatternCatalog(
                    request.PatternSources);
            }
            catch (Exception exception)
            {
                Add(errors, SectorPatternRenderErrorCode.MissingPatternCandidate,
                    "patternSources", exception.GetType().Name + "|" + exception.Message);
                return Failure(errors);
            }

            var pending = new List<PendingApplication>();
            var selections = new List<SectorPatternSelection>();
            var applicationDigests = new List<string>();
            var signatureUse = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var zone in request.RoleZonePlan.PatternZones.OrderBy(value => value))
            {
                var source = SelectSource(zone, request.PatternSources, signatureUse, errors);
                if (source == null) continue;

                var transformed = MicroPatternTransformer.Transform(source.Definition, source.Transform);
                if (!transformed.Success)
                {
                    Add(errors, SectorPatternRenderErrorCode.MicroPatternApplicationRejected,
                        zone.ZoneId, string.Join(";", transformed.Errors.Select(value => value.ToString())));
                    continue;
                }

                var protectedCells = request.RoleZonePlan.ProtectionEvidence
                    .Where(value => value.SectorIndex == zone.SectorIndex)
                    .Select(value => new MicroPatternProtectedCell(
                        value.Coordinate, value.SourceKind, value.Map10SourceId))
                    .ToArray();
                var application = MicroPatternApplicationPlanner.Plan(
                    transformed.Pattern,
                    new MicroPatternPlacement(new LocalTileCoord(zone.TileRect.X, zone.TileRect.Y)),
                    protectedCells);
                if (!application.Success)
                {
                    var protectedRejected = application.Errors.Any(value =>
                        value.Code == MicroPatternApplicationErrorCode.ProtectedWriteRejected);
                    Add(errors,
                        protectedRejected
                            ? SectorPatternRenderErrorCode.ProtectedWriteAttempt
                            : SectorPatternRenderErrorCode.MicroPatternApplicationRejected,
                        zone.ZoneId,
                        string.Join(";", application.Errors.Select(value => value.ToString())));
                    continue;
                }

                var requestId = "MPR_" + zone.ZoneId;
                pending.Add(new PendingApplication(zone, source, application.Plan, requestId));
                applicationDigests.Add(application.Plan.StableDigest);
                selections.Add(new SectorPatternSelection(
                    zone.ZoneId,
                    zone.SectorIndex,
                    source.Definition.Id,
                    source.Definition.ComputeStableDigest(),
                    source.Transform,
                    source.RepetitionSignature,
                    application.Plan.StableDigest,
                    requestId));
                signatureUse[source.RepetitionSignature] = Usage(signatureUse, source.RepetitionSignature) + 1;
            }

            if (errors.Count != 0) return Failure(errors);
            if (pending.Count != request.RoleZonePlan.PatternZones.Count)
            {
                Add(errors, SectorPatternRenderErrorCode.MissingPatternCandidate,
                    "selections", pending.Count + "!=" + request.RoleZonePlan.PatternZones.Count);
                return Failure(errors);
            }

            var renderCells = new List<SectorPatternRenderCell>();
            var rendererDigests = new List<string>();
            var writeCountByLayer = Enum.GetValues(typeof(SectorPatternRenderLayer))
                .Cast<SectorPatternRenderLayer>().ToDictionary(value => value, value => 0);
            var rendererInvocationCount = 0;
            var protectedMaskHitCount = pending.Sum(value => value.ApplicationPlan.ProtectedHits.Count);
            var protectedPreventedWriteCount = pending.Sum(value =>
                value.ApplicationPlan.ProtectedHits.Sum(hit => hit.RemovedWriteCount));
            var protectedWriteCount = 0;
            var rendererConflictCount = 0;

            foreach (var sectorGroup in pending.GroupBy(value => value.Zone.SectorIndex)
                         .OrderBy(value => value.Key))
            {
                var applications = sectorGroup.OrderBy(value => value.Zone).ToArray();
                var renderRequests = applications.Select(value => new MicroPatternRenderRequest(
                    new MicroPatternRenderRequestId(value.RequestId), value.ApplicationPlan)).ToArray();
                var targetCoordinates = applications.SelectMany(value => value.ApplicationPlan.Cells)
                    .Select(value => value.TargetCoordinate).Distinct()
                    .OrderBy(value => value.Y).ThenBy(value => value.X).ToArray();
                var target = new MicroPatternRenderTarget(targetCoordinates.Select(value =>
                    new MicroPatternRenderCellState(
                        value, false, string.Empty, string.Empty, string.Empty,
                        string.Empty, string.Empty)));

                rendererInvocationCount++;
                var rendered = MicroPatternOrderedRenderer.Render(renderRequests, target);
                rendererConflictCount += rendered.Conflicts.Count;
                if (!rendered.Success)
                {
                    var conflict = rendered.Conflicts.Count != 0 || rendered.Errors.Any(value =>
                        value.Code == MicroPatternRenderErrorCode.ConflictingLayerWrite);
                    Add(errors,
                        conflict
                            ? SectorPatternRenderErrorCode.RendererConflict
                            : SectorPatternRenderErrorCode.MicroPatternRendererRejected,
                        "sector[" + Number(sectorGroup.Key) + "]",
                        string.Join(";", rendered.Errors.Select(value => value.ToString())));
                    continue;
                }

                rendererDigests.Add(rendered.Delta.StableDigest);
                var protectedCoordinates = new HashSet<LocalTileCoord>(
                    request.RoleZonePlan.ProtectionEvidence
                        .Where(value => value.SectorIndex == sectorGroup.Key)
                        .Select(value => value.Coordinate));
                var writesAtProtected = rendered.Delta.Writes.Count(value =>
                    protectedCoordinates.Contains(value.TargetCoordinate));
                protectedWriteCount += writesAtProtected;
                if (writesAtProtected != 0)
                    Add(errors, SectorPatternRenderErrorCode.ProtectedWriteAttempt,
                        "sector[" + Number(sectorGroup.Key) + "]", Number(writesAtProtected));

                foreach (var write in rendered.Delta.Writes)
                    writeCountByLayer[(SectorPatternRenderLayer)(int)write.Layer]++;

                var deltaByCoordinate = rendered.Delta.Cells.ToDictionary(
                    value => value.TargetCoordinate);
                var writesByCoordinate = rendered.Delta.Writes
                    .GroupBy(value => value.TargetCoordinate)
                    .ToDictionary(value => value.Key, value => value.ToArray());
                var sectorCoordinate = applications[0].Zone.SectorCoordinate;
                foreach (var before in target.Cells)
                {
                    var after = deltaByCoordinate.TryGetValue(before.TargetCoordinate, out var delta)
                        ? delta.After
                        : before;
                    var writes = writesByCoordinate.TryGetValue(before.TargetCoordinate, out var values)
                        ? values
                        : Array.Empty<MicroPatternLayerWrite>();
                    renderCells.Add(new SectorPatternRenderCell(
                        sectorCoordinate,
                        sectorGroup.Key,
                        before.TargetCoordinate,
                        after.Solid,
                        after.SurfaceId,
                        after.AffordanceId,
                        after.MaterialId,
                        after.HazardId,
                        after.MarkerId,
                        !before.ValuesEqual(after),
                        writes.Length,
                        writes.Count(value => value.IsIdempotent),
                        after.Provenance.Count));
                }
            }

            ValidateRenderedCells(request.RoleZonePlan, renderCells, errors);
            if (errors.Count != 0) return Failure(errors);

            var draft = new SectorPatternRenderPlan(
                request.RoleZonePlan,
                request.PublicationLabel,
                sourceCatalogDigest,
                selections,
                renderCells,
                applicationDigests,
                rendererDigests,
                writeCountByLayer,
                rendererInvocationCount,
                protectedMaskHitCount,
                protectedPreventedWriteCount,
                protectedWriteCount,
                rendererConflictCount,
                string.Empty);
            var digest = SectorPatternRenderCanonicalDigest.ComputeRender(draft);
            if (request.ExpectedCanonicalDigest.Length != 0 &&
                !string.Equals(request.ExpectedCanonicalDigest, digest, StringComparison.Ordinal))
            {
                Add(errors, SectorPatternRenderErrorCode.NonCanonicalPublication,
                    "renderPlan.digest", request.ExpectedCanonicalDigest + "!=" + digest);
                return Failure(errors);
            }

            var plan = new SectorPatternRenderPlan(
                request.RoleZonePlan,
                request.PublicationLabel,
                sourceCatalogDigest,
                selections,
                renderCells,
                applicationDigests,
                rendererDigests,
                writeCountByLayer,
                rendererInvocationCount,
                protectedMaskHitCount,
                protectedPreventedWriteCount,
                protectedWriteCount,
                rendererConflictCount,
                digest);
            return new SectorPatternRenderBuildResult(plan, errors);
        }

        private static void ValidateRequest(
            SectorPatternRenderRequest request,
            ICollection<SectorPatternRenderError> errors)
        {
            if (request == null)
            {
                Add(errors, SectorPatternRenderErrorCode.MissingInput,
                    "request", "Pattern render request is required.");
                return;
            }

            if (request.RoleZonePlan == null)
                Add(errors, SectorPatternRenderErrorCode.MissingInput,
                    "roleZonePlan", "Successful role-zone plan is required.");
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

            if (request.RoleZonePlan == null) return;
            if (!request.RoleZonePlan.Map14_05RenderReady)
                Add(errors, SectorPatternRenderErrorCode.MissingInput,
                    "roleZonePlan.handoff", "Role-zone handoff must be ready.");
            if (!string.Equals(request.RoleZonePlan.CanonicalDigest,
                    SectorPatternRenderCanonicalDigest.ComputeRoleZone(request.RoleZonePlan),
                    StringComparison.Ordinal))
                Add(errors, SectorPatternRenderErrorCode.NonCanonicalPublication,
                    "roleZonePlan.digest", request.RoleZonePlan.CanonicalDigest);
        }

        private static SectorPatternSourceProjection SelectSource(
            SectorPatternZone zone,
            IEnumerable<SectorPatternSourceProjection> sources,
            IReadOnlyDictionary<string, int> signatureUse,
            ICollection<SectorPatternRenderError> errors)
        {
            if (!MoonpalaceBiomeId.TryParse(zone.BiomeId, out var biome))
            {
                Add(errors, SectorPatternRenderErrorCode.SectorMismatch,
                    zone.ZoneId, zone.BiomeId);
                return null;
            }

            var candidates = (sources ?? Array.Empty<SectorPatternSourceProjection>())
                .Where(value => value != null && value.Definition != null &&
                                value.Definition.AllowedBiomes.Contains(biome) &&
                                value.CompatibleZoneKinds.Contains(zone.Kind) &&
                                value.CompatibleRoleKinds.Contains(zone.OwnerRole) &&
                                value.CompatiblePacingRoles.Contains(zone.PacingRole))
                .OrderBy(value => Usage(signatureUse, value.RepetitionSignature))
                .ThenBy(value => value.CatalogOrder)
                .ThenBy(value => value.RepetitionSignature, StringComparer.Ordinal)
                .ThenBy(value => value.Definition.Id.Value, StringComparer.Ordinal)
                .ThenBy(value => value.Transform)
                .ToArray();
            if (candidates.Length != 0) return candidates[0];
            Add(errors, SectorPatternRenderErrorCode.MissingPatternCandidate,
                zone.ZoneId, zone.BiomeId + "|" + zone.Kind + "|" +
                             zone.OwnerRole + "|" + zone.PacingRole);
            return null;
        }

        private static void ValidateRenderedCells(
            SectorClusterRolePatternPlan roleZonePlan,
            IReadOnlyCollection<SectorPatternRenderCell> cells,
            ICollection<SectorPatternRenderError> errors)
        {
            var zonesBySector = roleZonePlan.PatternZones.GroupBy(value => value.SectorIndex)
                .ToDictionary(value => value.Key, value => value.ToArray());
            var duplicate = cells.GroupBy(value => value.SectorIndex + "|" +
                                                       value.Coordinate.X + "|" + value.Coordinate.Y)
                .FirstOrDefault(value => value.Count() != 1);
            if (duplicate != null)
                Add(errors, SectorPatternRenderErrorCode.RenderTargetMismatch,
                    duplicate.Key, Number(duplicate.Count()));
            foreach (var cell in cells)
            {
                if (cell.Coordinate.X < 0 || cell.Coordinate.X >= WorldGenConstants.SectorWidthTiles ||
                    cell.Coordinate.Y < 0 || cell.Coordinate.Y >= WorldGenConstants.SectorHeightTiles)
                    Add(errors, SectorPatternRenderErrorCode.RenderTargetMismatch,
                        "sector[" + Number(cell.SectorIndex) + "]",
                        cell.Coordinate.X + "," + cell.Coordinate.Y);
                if (!zonesBySector.TryGetValue(cell.SectorIndex, out var zones) ||
                    !zones.Any(value => value.TileRect.Contains(cell.Coordinate)))
                    Add(errors, SectorPatternRenderErrorCode.RenderTargetMismatch,
                        "sector[" + Number(cell.SectorIndex) + "]",
                        "Rendered cell is outside every pattern zone.");
            }

            var expected = roleZonePlan.PatternZones.Count * MicroPatternDefinition.RequiredCellCount;
            if (cells.Count != expected)
                Add(errors, SectorPatternRenderErrorCode.RenderTargetMismatch,
                    "target.count", cells.Count + "!=" + expected);
        }

        private static int Usage(IReadOnlyDictionary<string, int> usage, string signature) =>
            usage.TryGetValue(signature ?? string.Empty, out var value) ? value : 0;

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

        private static SectorPatternRenderBuildResult Failure(
            IEnumerable<SectorPatternRenderError> errors) =>
            new SectorPatternRenderBuildResult(null, errors);

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private sealed class PendingApplication
        {
            public PendingApplication(
                SectorPatternZone zone,
                SectorPatternSourceProjection source,
                MicroPatternApplicationPlan applicationPlan,
                string requestId)
            {
                Zone = zone;
                Source = source;
                ApplicationPlan = applicationPlan;
                RequestId = requestId ?? string.Empty;
            }

            public SectorPatternZone Zone { get; }
            public SectorPatternSourceProjection Source { get; }
            public MicroPatternApplicationPlan ApplicationPlan { get; }
            public string RequestId { get; }
        }
    }
}
