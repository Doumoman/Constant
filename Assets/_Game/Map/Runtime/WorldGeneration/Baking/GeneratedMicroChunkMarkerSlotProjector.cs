using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class GeneratedMicroChunkMarkerSlotProjector
    {
        public static MarkerSlotProjectionResult Project(GeneratedMicroChunkSliceSet sourceSliceSet)
        {
            if (sourceSliceSet == null)
                return Failure(null, string.Empty,
                    MarkerSlotProjectionFailureCode.MissingSourceSliceSet,
                    "SOURCE", "Generated MicroChunk slice set is required.");
            return Project(sourceSliceSet, Scan(sourceSliceSet));
        }

        public static MarkerSlotProjectionResult Project(
            GeneratedMicroChunkSliceSet sourceSliceSet,
            IEnumerable<GeneratedMarkerSlotProjection> sourceProjections)
        {
            if (sourceSliceSet == null)
                return Failure(null, string.Empty,
                    MarkerSlotProjectionFailureCode.MissingSourceSliceSet,
                    "SOURCE", "Generated MicroChunk slice set is required.");

            var raw = (sourceProjections ?? Array.Empty<GeneratedMarkerSlotProjection>()).ToArray();
            var nullProjectionCount = raw.Count(value => value == null);
            var projections = raw.Where(value => value != null).OrderBy(value => value).ToArray();
            var inputDigest = MarkerSlotProjectionDigest.ComputeInput(
                sourceSliceSet, projections, nullProjectionCount);
            var failures = new List<MarkerSlotProjectionFailure>();
            ValidateSource(sourceSliceSet, failures);
            if (nullProjectionCount > 0)
                Add(failures, MarkerSlotProjectionFailureCode.MissingProjection,
                    "PROJECTIONS", "Marker projections cannot contain null records.");

            var valid = new List<GeneratedMarkerSlotProjection>();
            foreach (var projection in projections)
                if (ValidateProjection(sourceSliceSet, projection, failures)) valid.Add(projection);

            DetectDuplicateSourceKeys(valid, failures);
            var slots = BuildSlots(valid);
            DetectDuplicateSlotIds(slots, failures);
            if (failures.Count > 0)
                return new MarkerSlotProjectionResult(
                    sourceSliceSet, null, failures, inputDigest);

            var set = new GeneratedMicroChunkMarkerSlotSet(
                sourceSliceSet, slots, sourceSliceSet.TotalLayerRecordCount, inputDigest);
            ValidateOutput(set, failures);
            return failures.Count == 0
                ? new MarkerSlotProjectionResult(sourceSliceSet, set, failures, inputDigest)
                : new MarkerSlotProjectionResult(sourceSliceSet, null, failures, inputDigest);
        }

        public static IReadOnlyList<GeneratedMarkerSlotProjection> Scan(
            GeneratedMicroChunkSliceSet sourceSliceSet)
        {
            if (sourceSliceSet == null)
                return new ReadOnlyCollection<GeneratedMarkerSlotProjection>(
                    Array.Empty<GeneratedMarkerSlotProjection>());
            var projections = new List<GeneratedMarkerSlotProjection>();
            foreach (var slice in sourceSliceSet.Slices.OrderBy(value => value))
            foreach (var cell in slice.Cells.OrderBy(value => value))
            foreach (var layer in cell.Layers.OrderBy(value => value))
            {
                GeneratedMarkerSlotKind kind;
                GeneratedMarkerSlotOwner owner;
                string sourceTaskId;
                if (!TryMapOwner(layer.SourceOwner, out kind, out owner, out sourceTaskId)) continue;
                projections.Add(new GeneratedMarkerSlotProjection(
                    slice, cell, layer, kind, owner, sourceTaskId));
            }
            return new ReadOnlyCollection<GeneratedMarkerSlotProjection>(
                projections.OrderBy(value => value).ToArray());
        }

        public static bool TryMapOwner(
            FinalCanvasSourceOwner sourceOwner,
            out GeneratedMarkerSlotKind kind,
            out GeneratedMarkerSlotOwner owner,
            out string sourceTaskId)
        {
            switch (sourceOwner)
            {
                case FinalCanvasSourceOwner.TerrainCluster:
                    kind = GeneratedMarkerSlotKind.TerrainCluster;
                    owner = GeneratedMarkerSlotOwner.TerrainCluster;
                    sourceTaskId = "MAP11_TERRAIN_CLUSTER";
                    return true;
                case FinalCanvasSourceOwner.Activity:
                    kind = GeneratedMarkerSlotKind.Activity;
                    owner = GeneratedMarkerSlotOwner.Activity;
                    sourceTaskId = "MAP12_ACTIVITY";
                    return true;
                case FinalCanvasSourceOwner.SpecialRegion:
                    kind = GeneratedMarkerSlotKind.SpecialRegion;
                    owner = GeneratedMarkerSlotOwner.SpecialRegion;
                    sourceTaskId = "MAP13_SPECIAL_REGION";
                    return true;
                case FinalCanvasSourceOwner.EventOverlay:
                    kind = GeneratedMarkerSlotKind.EventOverlay;
                    owner = GeneratedMarkerSlotOwner.EventOverlay;
                    sourceTaskId = "MAP12_EVENT_OVERLAY";
                    return true;
                case FinalCanvasSourceOwner.Boundary:
                    kind = GeneratedMarkerSlotKind.Boundary;
                    owner = GeneratedMarkerSlotOwner.Boundary;
                    sourceTaskId = "MAP15_02_BOUNDARY";
                    return true;
                case FinalCanvasSourceOwner.MandatoryRoute:
                    kind = GeneratedMarkerSlotKind.RouteRecovery;
                    owner = GeneratedMarkerSlotOwner.RouteRecovery;
                    sourceTaskId = "MAP16_03_ROUTE_RECOVERY";
                    return true;
                default:
                    kind = default(GeneratedMarkerSlotKind);
                    owner = default(GeneratedMarkerSlotOwner);
                    sourceTaskId = string.Empty;
                    return false;
            }
        }

        private static void ValidateSource(
            GeneratedMicroChunkSliceSet source,
            ICollection<MarkerSlotProjectionFailure> failures)
        {
            if (source.Request == null || source.SourceCanvasPlan == null ||
                source.SourceProtectionDensityReport == null ||
                source.SourceRouteRecoveryReport == null || source.SourcePartition == null ||
                source.SliceCount != GeneratedMicroChunkSliceSet.ChunkCount ||
                source.TotalCellCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                source.UniqueSectorCellCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                source.DuplicateSectorCellCount != 0 || source.MissingSectorCellCount != 0 ||
                source.OutOfBoundsSectorCellCount != 0 ||
                source.TotalLayerRecordCount != GeneratedMicroChunkSliceSet.SectorCellCount *
                    GeneratedMicroChunkSliceSet.LayerKindsPerCell ||
                source.LayerRecordsWithSourceOwnerCount != source.TotalLayerRecordCount ||
                source.LayerRecordsWithProvenanceCount != source.TotalLayerRecordCount ||
                source.InvalidSideSignatureCount != 0 || source.InvalidSliceSignatureCount != 0 ||
                source.MissingTraversalSummaryCount != 0 ||
                source.MissingPassableComponentSummaryCount != 0 ||
                source.RotationRequestCount != 0)
                Add(failures, MarkerSlotProjectionFailureCode.InvalidSourceSliceSet,
                    "SOURCE", "Source must be a complete accepted MAP16_05 slice set.");
            if (!MarkerSlotProjectionDigest.IsLowerHexSha256(source.InputDigest) ||
                !MarkerSlotProjectionDigest.IsLowerHexSha256(source.OutputDigest))
                Add(failures, MarkerSlotProjectionFailureCode.InvalidDigest,
                    "SOURCE", "Source input/output digests must be lower-hex SHA-256.");
            if (source.Slices.Any(value => value == null || value.Id == null ||
                value.Cells.Count != GeneratedMicroChunkSliceSet.MicroChunkCellCount ||
                value.Signature == null || value.TraversalSummary == null ||
                value.Cells.Any(cell => cell == null || cell.Layers.Count !=
                    GeneratedMicroChunkSliceSet.LayerKindsPerCell || cell.Layers.Any(layer =>
                        layer == null || string.IsNullOrEmpty(layer.ProvenanceId) ||
                        string.IsNullOrEmpty(layer.ClaimId) ||
                        string.IsNullOrEmpty(layer.SourceCellToken)))))
                Add(failures, MarkerSlotProjectionFailureCode.InvalidSourceSliceSet,
                    "SOURCE_RECORDS", "Every source slice, cell, layer and provenance is required.");
        }

        private static bool ValidateProjection(
            GeneratedMicroChunkSliceSet source,
            GeneratedMarkerSlotProjection projection,
            ICollection<MarkerSlotProjectionFailure> failures)
        {
            var valid = true;
            if (projection.SourceSlice == null)
            {
                Add(failures, MarkerSlotProjectionFailureCode.OrphanSlice,
                    projection.StableToken, "Marker projection requires a source slice.");
                valid = false;
            }
            if (projection.SourceCell == null)
            {
                Add(failures, MarkerSlotProjectionFailureCode.MissingCellReference,
                    projection.StableToken, "Marker projection requires a source cell reference.");
                valid = false;
            }
            if (projection.SourceLayer == null)
            {
                Add(failures, MarkerSlotProjectionFailureCode.MissingLayer,
                    projection.StableToken, "Marker projection requires a source layer.");
                valid = false;
            }
            if (!valid) return false;

            var canonicalSlice = source.Slices.SingleOrDefault(value =>
                value.Id.Value == projection.SourceSlice.Id.Value);
            if (canonicalSlice == null)
            {
                Add(failures, MarkerSlotProjectionFailureCode.OrphanSlice,
                    projection.SourceSlice.Id.Value, "Source slice is absent from the supplied set.");
                valid = false;
            }
            var canonicalCell = canonicalSlice == null ? null : canonicalSlice.Cells
                .SingleOrDefault(value => value.ChunkIndex == projection.SourceCell.ChunkIndex &&
                    value.LocalCoordinate.Equals(projection.SourceCell.LocalCoordinate) &&
                    value.SectorCoordinate.Equals(projection.SourceCell.SectorCoordinate));
            if (canonicalCell == null)
            {
                Add(failures, MarkerSlotProjectionFailureCode.OrphanCell,
                    projection.SourceCell.StableToken,
                    "Source cell is absent from the referenced source slice.");
                valid = false;
            }
            if (string.IsNullOrEmpty(projection.SourceLayer.ProvenanceId) ||
                string.IsNullOrEmpty(projection.SourceLayer.ClaimId) ||
                string.IsNullOrEmpty(projection.SourceLayer.SourceCellToken) ||
                string.IsNullOrEmpty(projection.SourceTaskId) ||
                string.IsNullOrEmpty(projection.SourceKey))
            {
                Add(failures, MarkerSlotProjectionFailureCode.MissingProvenance,
                    projection.StableToken,
                    "Owner, source task, provenance, claim and source-cell identity are required.");
                valid = false;
            }
            var canonicalLayer = canonicalCell == null ? null : canonicalCell.Layers
                .SingleOrDefault(value => value.Layer == projection.SourceLayer.Layer);
            if (canonicalLayer == null || canonicalLayer.StableToken != projection.SourceLayer.StableToken)
            {
                Add(failures, MarkerSlotProjectionFailureCode.SourceLayerMismatch,
                    projection.StableToken,
                    "Projection layer must be the exact public layer record preserved by MAP16_05.");
                valid = false;
            }

            GeneratedMarkerSlotKind expectedKind;
            GeneratedMarkerSlotOwner expectedOwner;
            string expectedTask;
            if (!TryMapOwner(projection.SourceLayer.SourceOwner,
                    out expectedKind, out expectedOwner, out expectedTask) ||
                projection.Kind != expectedKind || projection.Owner != expectedOwner ||
                projection.SourceTaskId != expectedTask)
            {
                Add(failures, MarkerSlotProjectionFailureCode.SourceMappingMismatch,
                    projection.StableToken,
                    "Projection kind, owner and source task must match the public owner mapping.");
                valid = false;
            }
            return valid;
        }

        private static void DetectDuplicateSourceKeys(
            IEnumerable<GeneratedMarkerSlotProjection> projections,
            ICollection<MarkerSlotProjectionFailure> failures)
        {
            foreach (var group in projections.GroupBy(SourceDuplicateKey, StringComparer.Ordinal)
                         .Where(value => value.Count() > 1).OrderBy(value => value.Key,
                             StringComparer.Ordinal))
                Add(failures, MarkerSlotProjectionFailureCode.DuplicateOwnerKindSourceKey,
                    group.Key,
                    "The same cell owner/kind/source key cannot be projected more than once.");
        }

        private static GeneratedMarkerSlot[] BuildSlots(
            IEnumerable<GeneratedMarkerSlotProjection> sourceProjections)
        {
            var slots = new List<GeneratedMarkerSlot>();
            foreach (var group in sourceProjections.GroupBy(CellKey, StringComparer.Ordinal)
                         .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                var projections = group.OrderBy(value => value).ToArray();
                var ordinalKeys = projections.Select(OrdinalKey).Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
                foreach (var projection in projections)
                {
                    var ordinal = Array.IndexOf(ordinalKeys, OrdinalKey(projection));
                    var cellRef = new GeneratedMarkerSlotCellRef(
                        projection.SourceSlice.Id.Value,
                        projection.SourceCell.ChunkIndex,
                        projection.SourceCell.LocalCoordinate.X,
                        projection.SourceCell.LocalCoordinate.Y,
                        projection.SourceCell.SectorCoordinate.X,
                        projection.SourceCell.SectorCoordinate.Y);
                    var id = new GeneratedMarkerSlotId(
                        projection.SourceSlice.Id.SectorId,
                        projection.SourceCell.ChunkIndex,
                        projection.SourceCell.LocalCoordinate.X,
                        projection.SourceCell.LocalCoordinate.Y,
                        projection.Kind, projection.Owner, projection.SourceKey, ordinal);
                    var provenance = new GeneratedMarkerSlotProvenance(
                        projection,
                        SocketIdentity(projection.SourceSlice, projection.SourceCell),
                        SignatureIdentity(projection.SourceSlice, projection.SourceCell),
                        projection.SourceSlice.TraversalSummary.StableToken);
                    slots.Add(new GeneratedMarkerSlot(
                        id, cellRef, provenance, projection.Kind, projection.Owner,
                        projection.SourceKey, ordinal));
                }
            }
            return slots.OrderBy(value => value).ToArray();
        }

        private static void DetectDuplicateSlotIds(
            IEnumerable<GeneratedMarkerSlot> slots,
            ICollection<MarkerSlotProjectionFailure> failures)
        {
            foreach (var group in slots.GroupBy(value => value.Id.Value, StringComparer.Ordinal)
                         .Where(value => value.Count() > 1).OrderBy(value => value.Key,
                             StringComparer.Ordinal))
                Add(failures, MarkerSlotProjectionFailureCode.DuplicateSlotId,
                    group.Key, "Generated marker slot ids must be unique.");
        }

        private static void ValidateOutput(
            GeneratedMicroChunkMarkerSlotSet set,
            ICollection<MarkerSlotProjectionFailure> failures)
        {
            if (set.SourceSliceCount != GeneratedMicroChunkSliceSet.ChunkCount ||
                set.SourceCellCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                set.SourceLayerRecordCount != GeneratedMicroChunkSliceSet.SectorCellCount *
                    GeneratedMicroChunkSliceSet.LayerKindsPerCell ||
                set.SourceSliceIds.Count != GeneratedMicroChunkSliceSet.ChunkCount ||
                set.MarkerLayerRecordsConsumed != set.SlotCount ||
                set.SlotsWithStableLocalIdCount != set.SlotCount ||
                set.SlotsWithCellReferenceCount != set.SlotCount ||
                set.SlotsWithProvenanceCount != set.SlotCount ||
                set.SlotsPreservingSourceLayerIdentityCount != set.SlotCount ||
                set.SlotsPreservingSocketSignatureTraversalIdentityCount != set.SlotCount ||
                set.DuplicateSlotIdCount != 0 || set.DuplicateOwnerKindSourceKeyCount != 0 ||
                set.OrphanMarkerRecordCount != 0 || set.MissingProvenanceCount != 0)
                Add(failures, MarkerSlotProjectionFailureCode.InvalidSourceSliceSet,
                    "OUTPUT", "Projected slot set must preserve all source identities without duplicates.");
            if (!MarkerSlotProjectionDigest.IsLowerHexSha256(set.InputDigest) ||
                !MarkerSlotProjectionDigest.IsLowerHexSha256(set.OutputDigest))
                Add(failures, MarkerSlotProjectionFailureCode.InvalidDigest,
                    "OUTPUT", "Input/output digests must be lower-hex SHA-256.");
        }

        private static string SocketIdentity(
            GeneratedMicroChunkSliceRecord slice,
            GeneratedMicroChunkCell cell) => string.Join(";", slice.SocketBands
            .Where(band => band.Cells.Any(value =>
                value.SectorCoordinate.Equals(cell.SectorCoordinate)))
            .OrderBy(value => value).Select(value => value.StableToken));

        private static string SignatureIdentity(
            GeneratedMicroChunkSliceRecord slice,
            GeneratedMicroChunkCell cell)
        {
            var identities = new List<string> { "SLICE|" + slice.Signature.Digest };
            foreach (var signature in slice.SideSignatures.OrderBy(value => value))
            {
                var side = signature.Side.Value;
                var onSide = side == GeneratedMicroChunkSocketSide.Left
                    ? cell.LocalCoordinate.X == 0
                    : side == GeneratedMicroChunkSocketSide.Right
                        ? cell.LocalCoordinate.X == GeneratedMicroChunkSliceSet.MicroChunkWidth - 1
                        : side == GeneratedMicroChunkSocketSide.Down
                            ? cell.LocalCoordinate.Y == 0
                            : cell.LocalCoordinate.Y == GeneratedMicroChunkSliceSet.MicroChunkHeight - 1;
                if (onSide) identities.Add("SIDE|" + side.ToString().ToUpperInvariant() +
                                           "|" + signature.Digest);
            }
            return string.Join(";", identities);
        }

        private static string SourceDuplicateKey(GeneratedMarkerSlotProjection value) =>
            CellKey(value) + "|" + value.Kind.ToString().ToUpperInvariant() + "|" +
            value.Owner.ToString().ToUpperInvariant() + "|" + value.SourceKey;
        private static string OrdinalKey(GeneratedMarkerSlotProjection value) =>
            value.Kind.ToString().ToUpperInvariant() + "|" +
            value.Owner.ToString().ToUpperInvariant() + "|" + value.SourceKey;
        private static string CellKey(GeneratedMarkerSlotProjection value) => string.Join("|", new[]
        {
            value.SourceSlice == null ? "MISSING_SLICE" : value.SourceSlice.Id.Value,
            value.SourceCell == null ? "MISSING_CELL" : Number(value.SourceCell.ChunkIndex) + "|" +
                value.SourceCell.LocalCoordinate,
        });

        private static MarkerSlotProjectionResult Failure(
            GeneratedMicroChunkSliceSet source,
            string inputDigest,
            MarkerSlotProjectionFailureCode code,
            string subject,
            string reason) => new MarkerSlotProjectionResult(source, null, new[]
        {
            new MarkerSlotProjectionFailure(code, subject, reason),
        }, inputDigest);

        private static void Add(
            ICollection<MarkerSlotProjectionFailure> failures,
            MarkerSlotProjectionFailureCode code,
            string subject,
            string reason) => failures.Add(new MarkerSlotProjectionFailure(
                code, subject, reason));
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
