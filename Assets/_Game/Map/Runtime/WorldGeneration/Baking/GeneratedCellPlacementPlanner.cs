using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class GeneratedCellPlacementPlanner
    {
        public static GeneratedCellPlacementResult Plan(GeneratedCellPlacementRequest request)
        {
            var failures = ValidateRequest(request).ToList();
            if (request == null)
                return Result(null, null, null, failures);

            var requiredTiles = request.SourceSlices.SelectMany(slice => slice.Cells)
                .SelectMany(cell => cell.Layers).Select(GeneratedTerrainTileCode.FromLayer).ToArray();
            var requiredPrefabs = request.SourceSlots.Select(GeneratedTerrainPrefabId.FromSlot).ToArray();
            var resolution = GeneratedTerrainAssetResolver.Resolve(
                request.AssetRegistry, requiredTiles, requiredPrefabs);
            failures.AddRange(resolution.Failures.Select(MapAssetFailure));
            if (failures.Count > 0)
                return Result(request, null, resolution, failures);

            var resolvedTiles = resolution.ResolvedTiles.ToDictionary(
                value => value.Code.Value, value => value.AssetKey, StringComparer.Ordinal);
            var resolvedPrefabs = resolution.ResolvedPrefabs.ToDictionary(
                value => value.Id.Value, value => value.AssetKey, StringComparer.Ordinal);
            var sourceSlotsByCell = request.SourceSlots.GroupBy(value => CellKey(
                    value.CellReference.ChunkIndex,
                    value.CellReference.LocalX,
                    value.CellReference.LocalY))
                .ToDictionary(group => group.Key, group => group.OrderBy(value => value).ToArray(),
                    StringComparer.Ordinal);
            var records = new List<GeneratedCellPlacementRecord>(request.Geometry.SectorCellCount);

            foreach (var slice in request.SourceSlices.OrderBy(value => value.ChunkIndex))
            {
                foreach (var cell in slice.Cells.OrderBy(value => value))
                {
                    var coordinate = Project(request.SectorIndex, slice, cell, request.Geometry);
                    var layers = cell.Layers.OrderBy(value => value).Select(layer =>
                    {
                        var code = GeneratedTerrainTileCode.FromLayer(layer);
                        return new GeneratedCellPlacementLayer(layer, code, resolvedTiles[code.Value]);
                    }).ToArray();
                    GeneratedMarkerSlot[] cellSlots;
                    if (!sourceSlotsByCell.TryGetValue(CellKey(slice.ChunkIndex,
                            cell.LocalCoordinate.X, cell.LocalCoordinate.Y), out cellSlots))
                        cellSlots = Array.Empty<GeneratedMarkerSlot>();
                    var slots = cellSlots.Select(slot =>
                    {
                        var id = GeneratedTerrainPrefabId.FromSlot(slot);
                        return new GeneratedCellPlacementSlotReference(slot, id, resolvedPrefabs[id.Value]);
                    }).ToArray();
                    records.Add(new GeneratedCellPlacementRecord(
                        new GeneratedCellPlacementId(slice.Id.SectorId, coordinate),
                        coordinate, layers, slots));
                }
            }

            var sockets = request.SourceSlices.OrderBy(value => value.ChunkIndex)
                .SelectMany(slice => slice.SideSignatures
                    .Where(signature => signature != null && signature.Side.HasValue)
                    .Select(signature => new GeneratedCellPlacementSocketReference(slice, signature)))
                .ToArray();
            failures.AddRange(ValidateCoordinateCoverage(records.Select(value => value.Coordinate),
                request.Geometry));
            ValidateProjectionCounts(request, records, sockets, failures);
            if (failures.Count > 0)
                return Result(request, null, resolution, failures);

            var plan = new GeneratedCellPlacementPlan(request, resolution, records, sockets);
            if (!GeneratedCellPlacementDigest.IsLowerHexSha256(plan.InputDigest) ||
                !GeneratedCellPlacementDigest.IsLowerHexSha256(plan.OutputDigest) ||
                !string.Equals(GeneratedCellPlacementDigest.ComputeOutput(plan),
                    plan.OutputDigest, StringComparison.Ordinal))
            {
                failures.Add(Failure(GeneratedCellPlacementFailureCode.InvalidDigest,
                    "placement", "Placement input or output digest is invalid."));
                return Result(request, null, resolution, failures);
            }
            return Result(request, plan, resolution, failures);
        }

        public static IReadOnlyList<GeneratedCellPlacementFailure> ValidateCoordinateCoverage(
            IEnumerable<GeneratedCellPlacementCoordinate> sourceCoordinates,
            GeneratedTerrainGeometrySnapshot geometry)
        {
            var failures = new List<GeneratedCellPlacementFailure>();
            if (geometry == null)
            {
                failures.Add(Failure(GeneratedCellPlacementFailureCode.MissingGeometry,
                    "geometry", "A geometry snapshot is required."));
                return Ordered(failures);
            }
            var raw = (sourceCoordinates ?? Array.Empty<GeneratedCellPlacementCoordinate>()).ToArray();
            var coordinates = raw.Where(value => value != null).ToArray();
            var invalidCount = raw.Count(value => value == null) +
                coordinates.Count(value => !value.IsValid(geometry));
            if (invalidCount > 0)
                failures.Add(Failure(GeneratedCellPlacementFailureCode.OutOfBoundsCoordinate,
                    Number(invalidCount), "Placement coordinates must match canonical slice, sector, and world projection."));
            var valid = coordinates.Where(value => value.IsValid(geometry)).ToArray();
            var groups = valid.GroupBy(value => string.Join(",", new[]
            {
                Number(value.SectorLocalX), Number(value.SectorLocalY),
            }), StringComparer.Ordinal).ToArray();
            var duplicateCount = groups.Sum(group => Math.Max(0, group.Count() - 1));
            if (duplicateCount > 0)
                failures.Add(Failure(GeneratedCellPlacementFailureCode.DuplicateCoordinate,
                    Number(duplicateCount), "Sector-local placement coordinates must be unique."));
            var missingCount = Math.Max(0, geometry.SectorCellCount - groups.Length);
            if (missingCount > 0)
                failures.Add(Failure(GeneratedCellPlacementFailureCode.MissingCoordinate,
                    Number(missingCount), "Every canonical sector cell requires one placement."));
            return Ordered(failures);
        }

        public static GeneratedWorldPlacementProjection ProjectReferenceWorld(
            GeneratedCellPlacementPlan sourcePlan)
        {
            if (sourcePlan == null || sourcePlan.PlacedCellCount !=
                    GeneratedTerrainGeometrySnapshot.CanonicalSectorCellCount ||
                sourcePlan.DuplicateSectorCoordinateCount != 0 ||
                sourcePlan.MissingSectorCoordinateCount != 0 ||
                sourcePlan.OutOfBoundsCoordinateCount != 0)
                return new GeneratedWorldPlacementProjection(sourcePlan, 0, 0, 0, 0, string.Empty);

            var geometry = sourcePlan.Request.Geometry;
            var occupied = new HashSet<int>();
            var outOfBounds = 0;
            var lines = new List<string>
            {
                "POLICY|MAP17_01_REFERENCE_WORLD_PLACEMENT_V1",
                "SOURCE|" + sourcePlan.OutputDigest,
                "GEOMETRY|" + GeneratedCellPlacementDigest.ComputeGeometry(geometry),
            };
            for (var sectorY = 0; sectorY < geometry.WorldSectorRows; sectorY++)
            {
                for (var sectorX = 0; sectorX < geometry.WorldSectorColumns; sectorX++)
                {
                    lines.Add("SECTOR|" + Number(sectorX) + "|" + Number(sectorY) + "|" +
                        sourcePlan.OutputDigest);
                    foreach (var record in sourcePlan.Records)
                    {
                        var worldX = sectorX * geometry.SectorWidth + record.Coordinate.SectorLocalX;
                        var worldY = sectorY * geometry.SectorHeight + record.Coordinate.SectorLocalY;
                        if (worldX < 0 || worldX >= geometry.WorldWidth ||
                            worldY < 0 || worldY >= geometry.WorldHeight)
                            outOfBounds++;
                        else occupied.Add(worldY * geometry.WorldWidth + worldX);
                    }
                }
            }
            var sectorCount = geometry.WorldSectorColumns * geometry.WorldSectorRows;
            var cellCount = sectorCount * sourcePlan.PlacedCellCount;
            lines.Add("COUNTS|" + Number(sectorCount) + "|" + Number(cellCount) + "|" +
                Number(occupied.Count) + "|" + Number(outOfBounds));
            return new GeneratedWorldPlacementProjection(sourcePlan, sectorCount, cellCount,
                occupied.Count, outOfBounds, BakingCanonicalDigest.HashCanonicalLines(lines));
        }

        private static IEnumerable<GeneratedCellPlacementFailure> ValidateRequest(
            GeneratedCellPlacementRequest request)
        {
            var failures = new List<GeneratedCellPlacementFailure>();
            if (request == null)
            {
                failures.Add(Failure(GeneratedCellPlacementFailureCode.MissingRequest,
                    "request", "A placement request is required."));
                return failures;
            }
            if (request.Geometry == null)
                failures.Add(Failure(GeneratedCellPlacementFailureCode.MissingGeometry,
                    "geometry", "The MAP16_09 geometry snapshot is required."));
            else
            {
                GeneratedTerrainGeometrySnapshot canonical;
                IReadOnlyList<string> geometryFailures;
                var actualDigest = GeneratedCellPlacementDigest.ComputeGeometry(request.Geometry);
                if (!GeneratedTerrainGeometrySnapshot.TryCreate(out canonical, out geometryFailures) ||
                    request.Geometry.CanonicalLines.Count != canonical.CanonicalLines.Count ||
                    !request.Geometry.CanonicalLines.SequenceEqual(canonical.CanonicalLines) ||
                    !GeneratedCellPlacementDigest.IsLowerHexSha256(request.ExpectedGeometryDigest) ||
                    !string.Equals(actualDigest, request.ExpectedGeometryDigest, StringComparison.Ordinal))
                    failures.Add(Failure(GeneratedCellPlacementFailureCode.StaleGeometry,
                        "geometry", "The supplied geometry snapshot or expected digest is stale."));
            }
            if (request.SectorIndex == null || !request.SectorIndex.IsInBounds)
                failures.Add(Failure(GeneratedCellPlacementFailureCode.InvalidSectorIndex,
                    request.SectorIndex == null ? "MISSING" : request.SectorIndex.ToString(),
                    "Sector index must be inside the canonical 13x13 world."));
            if (!GeneratedCellPlacementDigest.IsLowerHexSha256(request.Map16ExitAuditDigest))
                failures.Add(Failure(GeneratedCellPlacementFailureCode.InvalidDigest,
                    "MAP16_08", "The MAP16_08 exit audit digest is required."));

            ValidateSlices(request, failures);
            ValidateSlots(request, failures);
            ValidateExportPacket(request, failures);
            if (request.AssetRegistry == null)
                failures.Add(Failure(GeneratedCellPlacementFailureCode.MissingAssetRegistry,
                    "registry", "An immutable registry snapshot is required."));
            return failures;
        }

        private static void ValidateSlices(
            GeneratedCellPlacementRequest request,
            ICollection<GeneratedCellPlacementFailure> failures)
        {
            var set = request.SliceSet;
            if (set == null)
            {
                failures.Add(Failure(GeneratedCellPlacementFailureCode.MissingSliceSet,
                    "slice_set", "A generated micro-chunk slice set is required."));
                return;
            }
            if (request.Geometry == null) return;
            var expected = set.Slices.OrderBy(value => value).ToArray();
            var actual = request.SourceSlices.OrderBy(value => value).ToArray();
            var identityMatches = expected.Length == actual.Length && expected.Zip(actual,
                (left, right) => ReferenceEquals(left, right)).All(value => value);
            if (request.NullSliceCount != 0 || actual.Length != request.Geometry.ChunkCount ||
                actual.Select(value => value.Id.Value).Distinct(StringComparer.Ordinal).Count() != actual.Length ||
                !identityMatches || set.SliceCount != request.Geometry.ChunkCount ||
                set.TotalCellCount != request.Geometry.SectorCellCount ||
                set.TotalLayerRecordCount != request.Geometry.SectorLayerRecordCount ||
                set.DuplicateSectorCellCount != 0 || set.MissingSectorCellCount != 0 ||
                set.OutOfBoundsSectorCellCount != 0 || set.InvalidSideSignatureCount != 0 ||
                set.InvalidSliceSignatureCount != 0 || set.MissingTraversalSummaryCount != 0 ||
                !GeneratedMicroChunkSliceDigest.IsLowerHexSha256(set.InputDigest) ||
                !GeneratedMicroChunkSliceDigest.IsLowerHexSha256(set.OutputDigest) ||
                !string.Equals(GeneratedMicroChunkSliceDigest.ComputeOutput(set),
                    set.OutputDigest, StringComparison.Ordinal))
                failures.Add(Failure(GeneratedCellPlacementFailureCode.InvalidSliceSet,
                    "slice_set", "Slice identity, coverage, layers, signatures, or digest is incomplete."));
        }

        private static void ValidateSlots(
            GeneratedCellPlacementRequest request,
            ICollection<GeneratedCellPlacementFailure> failures)
        {
            var set = request.SlotSet;
            if (set == null)
            {
                failures.Add(Failure(GeneratedCellPlacementFailureCode.MissingSlotSet,
                    "slot_set", "A generated marker-slot set is required."));
                return;
            }
            var expected = set.Slots.OrderBy(value => value).ToArray();
            var actual = request.SourceSlots.OrderBy(value => value).ToArray();
            var identityMatches = expected.Length == actual.Length && expected.Zip(actual,
                (left, right) => ReferenceEquals(left, right)).All(value => value);
            if (request.NullSlotCount != 0 || !ReferenceEquals(request.SliceSet, set.SourceSliceSet) ||
                !identityMatches || actual.Select(value => value.Id.Value)
                    .Distinct(StringComparer.Ordinal).Count() != actual.Length ||
                set.DuplicateSlotIdCount != 0 || set.DuplicateOwnerKindSourceKeyCount != 0 ||
                set.OrphanMarkerRecordCount != 0 || set.MissingProvenanceCount != 0 ||
                !MarkerSlotProjectionDigest.IsLowerHexSha256(set.InputDigest) ||
                !MarkerSlotProjectionDigest.IsLowerHexSha256(set.OutputDigest) ||
                !string.Equals(MarkerSlotProjectionDigest.ComputeOutput(set),
                    set.OutputDigest, StringComparison.Ordinal))
                failures.Add(Failure(GeneratedCellPlacementFailureCode.InvalidSlotSet,
                    "slot_set", "Slot identity, provenance, ownership, or digest is incomplete."));
        }

        private static void ValidateExportPacket(
            GeneratedCellPlacementRequest request,
            ICollection<GeneratedCellPlacementFailure> failures)
        {
            var packet = request.ExportPacket;
            if (packet == null)
            {
                failures.Add(Failure(GeneratedCellPlacementFailureCode.MissingExportPacket,
                    "export_packet", "The immutable MAP16 export packet is required."));
                return;
            }
            if (request.SliceSet == null || request.SlotSet == null) return;
            var rebuilt = GeneratedTerrainCsvExporter.Build(request.SliceSet, request.SlotSet,
                request.SourceSlices, request.SourceSlots);
            if (!rebuilt.Success || packet.Manifest == null ||
                packet.PlanRows.Count != 1 || packet.SliceRows.Count !=
                    GeneratedTerrainGeometrySnapshot.CanonicalChunkCount ||
                packet.CellRows.Count != GeneratedTerrainGeometrySnapshot.CanonicalSectorCellCount ||
                packet.LayerRecordCount != GeneratedTerrainGeometrySnapshot.CanonicalSectorLayerRecordCount ||
                packet.SocketRows.Count != GeneratedTerrainGeometrySnapshot.CanonicalChunkCount * 4 ||
                packet.SlotRows.Count != request.SlotSet.SlotCount ||
                !GeneratedTerrainExportDigest.IsLowerHexSha256(packet.ManifestDigest) ||
                !GeneratedTerrainExportDigest.IsLowerHexSha256(packet.PacketDigest) ||
                !string.Equals(packet.Manifest.SourceSliceSetDigest,
                    request.SliceSet.OutputDigest, StringComparison.Ordinal) ||
                !string.Equals(packet.Manifest.SourceMarkerSlotSetDigest,
                    request.SlotSet.OutputDigest, StringComparison.Ordinal) ||
                !rebuilt.Success || !string.Equals(rebuilt.Packet.ManifestDigest,
                    packet.ManifestDigest, StringComparison.Ordinal) ||
                !string.Equals(rebuilt.Packet.PacketDigest, packet.PacketDigest, StringComparison.Ordinal) ||
                !rebuilt.Packet.Files.Select(value => value.PayloadDigest).SequenceEqual(
                    packet.Files.Select(value => value.PayloadDigest), StringComparer.Ordinal))
                failures.Add(Failure(GeneratedCellPlacementFailureCode.StaleExportPacket,
                    "export_packet", "The export packet does not replay from the supplied slice and slot sets."));
        }

        private static GeneratedCellPlacementCoordinate Project(
            GeneratedSectorIndexCoordinate sectorIndex,
            GeneratedMicroChunkSliceRecord slice,
            GeneratedMicroChunkCell cell,
            GeneratedTerrainGeometrySnapshot geometry)
        {
            var localX = cell.LocalCoordinate.X;
            var localY = cell.LocalCoordinate.Y;
            var sectorX = slice.ChunkX * geometry.MicroChunkWidth + localX;
            var sectorY = slice.ChunkY * geometry.MicroChunkHeight + localY;
            return new GeneratedCellPlacementCoordinate(sectorIndex, slice.ChunkIndex,
                localX, localY, localX, localY, sectorX, sectorY,
                sectorIndex.X * geometry.SectorWidth + sectorX,
                sectorIndex.Y * geometry.SectorHeight + sectorY);
        }

        private static void ValidateProjectionCounts(
            GeneratedCellPlacementRequest request,
            IReadOnlyCollection<GeneratedCellPlacementRecord> records,
            IReadOnlyCollection<GeneratedCellPlacementSocketReference> sockets,
            ICollection<GeneratedCellPlacementFailure> failures)
        {
            if (records.Count != request.Geometry.SectorCellCount ||
                records.Sum(value => value.LayerCount) != request.Geometry.SectorLayerRecordCount ||
                records.Select(value => value.Id.Value).Distinct(StringComparer.Ordinal).Count() != records.Count ||
                records.SelectMany(value => value.Layers).Any(value =>
                    value.Source == null || value.TileCode == null || !value.TileCode.IsValid ||
                    string.IsNullOrEmpty(value.ResolvedAssetKey) ||
                    !string.Equals(value.SourceLayerStableToken,
                        value.Source.StableToken, StringComparison.Ordinal)))
                failures.Add(Failure(GeneratedCellPlacementFailureCode.LayerProjectionMismatch,
                    "layers", "Every cell must publish seven resolved layers with byte-compatible provenance."));
            var slotReferences = records.SelectMany(value => value.SlotReferences).ToArray();
            if (slotReferences.Length != request.SourceSlots.Count ||
                slotReferences.Select(value => value.SlotId).Distinct(StringComparer.Ordinal).Count() !=
                    request.SourceSlots.Count || slotReferences.Any(value => value.Source == null ||
                    value.PrefabId == null || !value.PrefabId.IsValid ||
                    string.IsNullOrEmpty(value.ResolvedAssetKey) ||
                    !string.Equals(value.SourceProvenanceToken,
                        value.Source.Provenance.StableToken, StringComparison.Ordinal)))
                failures.Add(Failure(GeneratedCellPlacementFailureCode.SlotProjectionMismatch,
                    "slots", "Every marker slot must retain its cell and provenance identity."));
            if (sockets.Count != request.Geometry.ChunkCount * 4 || sockets.Any(value =>
                    !GeneratedMicroChunkSliceDigest.IsLowerHexSha256(value.SideSignature) ||
                    !GeneratedMicroChunkSliceDigest.IsLowerHexSha256(value.SliceSignature) ||
                    string.IsNullOrEmpty(value.TraversalToken)))
                failures.Add(Failure(GeneratedCellPlacementFailureCode.SlotProjectionMismatch,
                    "sockets", "Every slice side must preserve signature, band, and traversal identity."));
        }

        private static GeneratedCellPlacementFailure MapAssetFailure(
            GeneratedTerrainAssetResolutionFailure failure)
        {
            GeneratedCellPlacementFailureCode code;
            switch (failure.Code)
            {
                case GeneratedTerrainAssetResolutionFailureCode.MissingRegistry:
                    code = GeneratedCellPlacementFailureCode.MissingAssetRegistry; break;
                case GeneratedTerrainAssetResolutionFailureCode.MissingTileCode:
                    code = GeneratedCellPlacementFailureCode.MissingTileCode; break;
                case GeneratedTerrainAssetResolutionFailureCode.MissingPrefabId:
                    code = GeneratedCellPlacementFailureCode.MissingPrefabId; break;
                case GeneratedTerrainAssetResolutionFailureCode.DuplicateTileCode:
                case GeneratedTerrainAssetResolutionFailureCode.DuplicatePrefabId:
                    code = GeneratedCellPlacementFailureCode.DuplicateAssetReference; break;
                case GeneratedTerrainAssetResolutionFailureCode.InvalidTileCode:
                case GeneratedTerrainAssetResolutionFailureCode.InvalidPrefabId:
                case GeneratedTerrainAssetResolutionFailureCode.InvalidRegistryEntry:
                    code = GeneratedCellPlacementFailureCode.InvalidAssetId; break;
                default:
                    code = GeneratedCellPlacementFailureCode.InvalidAssetRegistry; break;
            }
            return Failure(code, failure.Subject, failure.Reason);
        }

        private static GeneratedCellPlacementResult Result(
            GeneratedCellPlacementRequest request,
            GeneratedCellPlacementPlan plan,
            GeneratedTerrainAssetResolution resolution,
            IEnumerable<GeneratedCellPlacementFailure> failures) =>
            new GeneratedCellPlacementResult(request, plan, resolution, Ordered(failures));

        private static ReadOnlyCollection<GeneratedCellPlacementFailure> Ordered(
            IEnumerable<GeneratedCellPlacementFailure> failures) =>
            new ReadOnlyCollection<GeneratedCellPlacementFailure>((failures ??
                Array.Empty<GeneratedCellPlacementFailure>()).Distinct()
                .OrderBy(value => value).ToArray());

        private static GeneratedCellPlacementFailure Failure(
            GeneratedCellPlacementFailureCode code, string subject, string reason) =>
            new GeneratedCellPlacementFailure(code, subject ?? string.Empty, reason);

        private static string CellKey(int chunkIndex, int localX, int localY) => string.Join("|", new[]
        {
            Number(chunkIndex), Number(localX), Number(localY),
        });
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
