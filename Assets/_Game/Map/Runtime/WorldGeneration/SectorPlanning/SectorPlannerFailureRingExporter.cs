using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public sealed class SectorPlannerFailureRingSector : IComparable<SectorPlannerFailureRingSector>
    {
        private readonly ReadOnlyCollection<string> externalSockets;
        private readonly ReadOnlyCollection<string> boundaryIdentities;
        private readonly ReadOnlyCollection<string> specialRegionIdentities;
        private readonly ReadOnlyCollection<SectorPlannerDebugToken> tokens;

        internal SectorPlannerFailureRingSector(
            SectorPlannerSectorSnapshot sector,
            bool isCenter,
            IEnumerable<SectorPlannerDebugToken> sourceTokens)
        {
            SectorCoordinate = sector.Coordinate;
            SectorIndex = sector.SectorIndex;
            IsCenter = isCenter;
            RouteType = sector.Route.RouteType;
            AccessClass = sector.Route.AccessClass.ToString();
            BiomeId = sector.Biome.BiomeId;
            externalSockets = Copy(sector.Route.ExternalSockets);
            boundaryIdentities = Copy(sector.Boundaries.Select(value =>
                value.Side + ":" + value.PairId + ":" + value.CandidateId));
            specialRegionIdentities = Copy(
                (sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.None
                    ? Array.Empty<string>()
                    : new[] { sector.SpecialRegion.Kind + ":" + sector.SpecialRegion.Binding + ":" + sector.SpecialRegion.RegionId })
                .Concat(sector.OptionalRegions.Select(value => value.Kind + ":DeferredOptionalLocal:" + value.RegionId)));
            tokens = new ReadOnlyCollection<SectorPlannerDebugToken>((sourceTokens ?? Array.Empty<SectorPlannerDebugToken>()).OrderBy(value => value).ToArray());
        }

        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public bool IsCenter { get; }
        public int RouteType { get; }
        public string AccessClass { get; }
        public string BiomeId { get; }
        public IReadOnlyList<string> ExternalSockets => externalSockets;
        public IReadOnlyList<string> BoundaryIdentities => boundaryIdentities;
        public IReadOnlyList<string> SpecialRegionIdentities => specialRegionIdentities;
        public IReadOnlyList<SectorPlannerDebugToken> Tokens => tokens;

        public int CompareTo(SectorPlannerFailureRingSector other)
        {
            if (other == null) return -1;
            var result = SectorCoordinate.Y.CompareTo(other.SectorCoordinate.Y);
            return result != 0 ? result : SectorCoordinate.X.CompareTo(other.SectorCoordinate.X);
        }

        private static ReadOnlyCollection<string> Copy(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public sealed class SectorPlannerFailureRingSnapshot
    {
        private readonly ReadOnlyCollection<SectorPlannerFailureRingSector> sectors;
        private readonly ReadOnlyCollection<string> missingNeighbors;

        internal SectorPlannerFailureRingSnapshot(
            SectorPlannerRetryNodeTrace trace,
            IEnumerable<SectorPlannerFailureRingSector> sourceSectors,
            IEnumerable<string> sourceMissingNeighbors,
            SectorPlannerDebugSection failureSection)
        {
            Trace = trace;
            sectors = new ReadOnlyCollection<SectorPlannerFailureRingSector>((sourceSectors ?? Array.Empty<SectorPlannerFailureRingSector>()).OrderBy(value => value).ToArray());
            missingNeighbors = new ReadOnlyCollection<string>((sourceMissingNeighbors ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            FailureSection = failureSection;
            CanonicalDigest = SectorPlannerDebugCanonicalDigest.Hash(string.Join("\n", new[]
            {
                trace.AttemptTrace.Failure.ToString(), trace.Stage.ToString(), trace.ResultingDecision.ToString(),
                trace.SelectedCandidateId, trace.RngTrace == null ? string.Empty : trace.RngTrace.CanonicalDigest,
                string.Join("\n", sectors.Select(SectorMaterial)), string.Join("\n", missingNeighbors),
                failureSection.CanonicalDigest,
            }));
        }

        public SectorPlannerRetryNodeTrace Trace { get; }
        public SectorPlannerRetryFailure Failure => Trace.AttemptTrace.Failure;
        public SectorPlannerRetryStage NextStage => Trace.Stage;
        public SectorPlannerRetryDecisionKind RetryDecision => Trace.ResultingDecision;
        public int AttemptOrdinal => Trace.AttemptTrace.AttemptOrdinal;
        public int NodeOrdinal => Trace.AttemptTrace.NodeOrdinal;
        public SectorPlannerRngTrace RngTrace => Trace.RngTrace;
        public IReadOnlyList<SectorPlannerFailureRingSector> Sectors => sectors;
        public SectorPlannerFailureRingSector CenterSector => sectors.Single(value => value.IsCenter);
        public IReadOnlyList<SectorPlannerFailureRingSector> RingSectors => sectors.Where(value => !value.IsCenter).ToArray();
        public IReadOnlyList<string> MissingNeighbors => missingNeighbors;
        public SectorPlannerDebugSection FailureSection { get; }
        public int ExportedSectorCount => sectors.Count;
        public int RingSectorCount => sectors.Count(value => !value.IsCenter);
        public int MissingNeighborCount => missingNeighbors.Count;
        public string CanonicalDigest { get; }
        public int RetryExecutionCount => 0;
        public int NewRngDrawCount => 0;
        public int RepairCount => 0;
        public int FallbackCorridorCarveCount => 0;
        public int ValidationRelaxationCount => 0;
        public int WholeSectorRerandomCount => 0;
        public int WholeWorldRerandomCount => 0;
        public int AnchorMutationCount => 0;
        public int SpecialReservationMutationCount => 0;
        public int DebugFileWriteCount => 0;
        public int EditorWindowOpenCount => 0;

        private static string SectorMaterial(SectorPlannerFailureRingSector value) => string.Join("|", new[]
        {
            value.SectorIndex.ToString(CultureInfo.InvariantCulture), value.SectorCoordinate.X.ToString(CultureInfo.InvariantCulture),
            value.SectorCoordinate.Y.ToString(CultureInfo.InvariantCulture), value.IsCenter.ToString(),
            value.RouteType.ToString(CultureInfo.InvariantCulture), value.AccessClass, value.BiomeId,
            string.Join(",", value.ExternalSockets), string.Join(",", value.BoundaryIdentities),
            string.Join(",", value.SpecialRegionIdentities), string.Join(",", value.Tokens.Select(token => token.Identity)),
        });
    }

    public static class SectorPlannerFailureRingExporter
    {
        public static SectorPlannerDebugExportResult ExportFailureRing(
            SectorPlannerDebugExportRequest request,
            SectorPlannerRetryNodeTrace failureTrace,
            IEnumerable<SectorPlannerSectorSnapshot> sourceContext = null)
        {
            var errors = new List<SectorPlannerDebugExportError>();
            if (request == null)
            {
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.MissingInput, "request", "A debug export request is required.");
                return SectorPlannerDebugExporter.Failed(errors);
            }
            if (request.RetryPlan == null)
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.MissingRetryPlan, "retryPlan", "MAP14_08 retry plan is required.");
            if (failureTrace == null)
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.MissingFailureTrace, "failureTrace", "A failed retry node trace is required.");
            var input = request.RetryPlan?.Request?.OwnershipPlan?.Request?.Input;
            if (input == null && request.RetryPlan != null)
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.MissingPlannerInput, "plannerInput", "Public sector context is required.");
            if (errors.Count != 0) return SectorPlannerDebugExporter.Failed(errors);

            var center = request.RetryPlan.Request.SectorCoordinate;
            var contexts = (sourceContext ?? input.Sectors).Where(value => value != null).ToArray();
            foreach (var duplicate in contexts.GroupBy(value => value.Coordinate).Where(value => value.Count() > 1))
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.RingNeighborMismatch,
                    duplicate.Key.X + "," + duplicate.Key.Y, "Failure-ring context coordinates must be unique.");
            var byCoordinate = contexts.GroupBy(value => value.Coordinate).ToDictionary(value => value.Key, value => value.First());
            if (!byCoordinate.ContainsKey(center))
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.RingCenterMissing,
                    center.X + "," + center.Y, "Failure-ring center is missing from public context.");
            if (failureTrace.AttemptTrace.Failure == null)
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.MissingFailureTrace, "failure", "Failure owner/code/detail are required.");
            if (errors.Count != 0) return SectorPlannerDebugExporter.Failed(errors);

            var ownership = request.RetryPlan.Request.OwnershipPlan;
            var sectors = new List<SectorPlannerFailureRingSector>();
            var missing = new List<string>();
            var offsets = new[]
            {
                new[] {-1, -1}, new[] {0, -1}, new[] {1, -1}, new[] {-1, 0},
                new[] {0, 0}, new[] {1, 0}, new[] {-1, 1}, new[] {0, 1}, new[] {1, 1},
            };
            foreach (var offset in offsets)
            {
                var coordinate = new SectorCoord(center.X + offset[0], center.Y + offset[1]);
                var label = coordinate.X.ToString(CultureInfo.InvariantCulture) + "," + coordinate.Y.ToString(CultureInfo.InvariantCulture);
                if (coordinate.X < 0 || coordinate.X >= 13 || coordinate.Y < 0 || coordinate.Y >= 13)
                {
                    missing.Add(label + ":WORLD_OUT_OF_BOUNDS");
                    continue;
                }
                if (!byCoordinate.TryGetValue(coordinate, out var sector))
                {
                    missing.Add(label + ":PUBLIC_CONTEXT_NOT_AVAILABLE");
                    continue;
                }
                var isCenter = offset[0] == 0 && offset[1] == 0;
                var tokens = ContextTokens(ownership, sector, isCenter, failureTrace);
                sectors.Add(new SectorPlannerFailureRingSector(sector, isCenter, tokens));
            }

            if (sectors.Count(value => value.IsCenter) != 1)
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.RingCenterMissing, "center", "Exactly one failure-ring center is required.");
            if (errors.Count != 0) return SectorPlannerDebugExporter.Failed(errors);

            var facts = new[]
            {
                new SectorPlannerDebugFact("FAILURE_OWNER", failureTrace.AttemptTrace.Failure.Owner.ToString()),
                new SectorPlannerDebugFact("FAILURE_CODE", failureTrace.AttemptTrace.Failure.Code),
                new SectorPlannerDebugFact("FAILURE_SUBJECT", failureTrace.AttemptTrace.Failure.Subject),
                new SectorPlannerDebugFact("FAILURE_DETAIL", failureTrace.AttemptTrace.Failure.Detail),
                new SectorPlannerDebugFact("RETRY_STAGE", failureTrace.Stage.ToString()),
                new SectorPlannerDebugFact("RETRY_DECISION", failureTrace.ResultingDecision.ToString()),
                new SectorPlannerDebugFact("ATTEMPT", failureTrace.AttemptTrace.AttemptOrdinal.ToString(CultureInfo.InvariantCulture)),
                new SectorPlannerDebugFact("NODE", failureTrace.AttemptTrace.NodeOrdinal.ToString(CultureInfo.InvariantCulture)),
                new SectorPlannerDebugFact("RNG_TRACE", failureTrace.RngTrace == null ? "NONE" : failureTrace.RngTrace.CanonicalDigest),
                new SectorPlannerDebugFact("EXPORTED_SECTORS", sectors.Count.ToString(CultureInfo.InvariantCulture)),
                new SectorPlannerDebugFact("MISSING_NEIGHBORS", missing.Count.ToString(CultureInfo.InvariantCulture)),
                new SectorPlannerDebugFact("RETRY_EXECUTION", "0"),
                new SectorPlannerDebugFact("NEW_RNG_DRAW", "0"),
                new SectorPlannerDebugFact("REPAIR", "0"),
            };
            var section = SectorPlannerDebugExporter.Section("MAP14_09_FAILURE_RING",
                SectorPlannerDebugSectionKind.FailureRing, "MAP14_08", request.RetryPlan.CanonicalDigest,
                "Failed attempt and nearest public Moore 1-ring context without repair.",
                facts, sectors.SelectMany(value => value.Tokens));
            var snapshot = new SectorPlannerFailureRingSnapshot(failureTrace, sectors, missing, section);
            return new SectorPlannerDebugExportResult(null, snapshot, null, null, errors);
        }

        public static SectorPlannerDebugExportResult ExportFailureRing(
            SectorPlannerDebugExportRequest request,
            SectorPlannerAttemptTrace failureTrace,
            IEnumerable<SectorPlannerSectorSnapshot> sourceContext = null)
        {
            if (request?.RetryPlan == null || failureTrace == null)
                return ExportFailureRing(request, (SectorPlannerRetryNodeTrace)null, sourceContext);
            var node = request.RetryPlan.NodeTraces.FirstOrDefault(value =>
                value.AttemptTrace.AttemptOrdinal == failureTrace.AttemptOrdinal &&
                value.AttemptTrace.NodeOrdinal == failureTrace.NodeOrdinal &&
                value.AttemptTrace.Failure.Equals(failureTrace.Failure));
            return ExportFailureRing(request, node, sourceContext);
        }

        private static IEnumerable<SectorPlannerDebugToken> ContextTokens(
            SectorCanvasOwnershipPlan ownership,
            SectorPlannerSectorSnapshot sector,
            bool isCenter,
            SectorPlannerRetryNodeTrace trace)
        {
            var result = new List<SectorPlannerDebugToken>
            {
                new SectorPlannerDebugToken(sector.Coordinate, new LocalTileCoord(24, 16),
                    isCenter ? SectorPlannerDebugTokenKind.FailureCenter : SectorPlannerDebugTokenKind.NeighborContext,
                    isCenter ? trace.AttemptTrace.Failure.Code : "NEIGHBOR_" + sector.SectorIndex.ToString("D3", CultureInfo.InvariantCulture),
                    isCenter ? trace.AttemptTrace.Failure.Owner.ToString() : "MAP14_01",
                    isCenter ? trace.AttemptTrace.Failure.Subject : sector.Biome.BiomeId),
            };
            foreach (var group in ownership.OwnedCells.Where(value => value.SectorIndex == sector.SectorIndex &&
                         (value.Plane == SectorCanvasOwnershipPlane.Protection || value.Plane == SectorCanvasOwnershipPlane.Reservation))
                         .GroupBy(value => value.Plane))
            {
                var value = group.OrderBy(item => item).First();
                result.Add(new SectorPlannerDebugToken(sector.Coordinate, value.Coordinate,
                    value.Plane == SectorCanvasOwnershipPlane.Protection
                        ? SectorPlannerDebugTokenKind.ProtectedOpen : SectorPlannerDebugTokenKind.Reservation,
                    value.WinnerClaimId, value.OwnerKind.ToString(), value.SourceObjectId));
            }
            return result;
        }
    }
}
