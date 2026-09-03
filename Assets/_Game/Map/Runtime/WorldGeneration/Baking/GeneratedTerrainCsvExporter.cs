using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class GeneratedTerrainCsvExporter
    {
        public const string ManifestFileName = "generated_terrain_manifest.csv";
        public const string PlanFileName = "generated_terrain_plan.csv";
        public const string SlicesFileName = "generated_terrain_slices.csv";
        public const string CellsFileName = "generated_terrain_cells.csv";
        public const string SocketsFileName = "generated_terrain_sockets.csv";
        public const string SlotsFileName = "generated_terrain_slots.csv";

        public static readonly IReadOnlyList<string> RequiredFileNames = new[]
        {
            ManifestFileName, PlanFileName, SlicesFileName,
            CellsFileName, SocketsFileName, SlotsFileName,
        };

        public const string ManifestHeader =
            "format_version,task_id,source_slice_digest,source_slot_digest,sector_width,sector_height,sector_cells,chunk_grid_width,chunk_grid_height,chunk_count,micro_chunk_width,micro_chunk_height,micro_chunk_cells,micro_pattern_size,plan_rows,plan_digest,slice_rows,slice_digest,cell_rows,cell_digest,socket_rows,socket_digest,slot_rows,slot_digest,packet_digest";
        public const string PlanHeader =
            "sector_id,source_slice_digest,source_slot_digest,sector_width,sector_height,sector_cells,chunk_grid_width,chunk_grid_height,chunk_count,micro_chunk_width,micro_chunk_height,micro_chunk_cells,micro_pattern_width,micro_pattern_height";
        public const string SlicesHeader =
            "slice_id,chunk_index,chunk_x,chunk_y,sector_origin_x,sector_origin_y,cell_count,layer_record_count,socket_band_count,passable_cell_count,blocked_cell_count,route_recovery_witness_cell_count,slice_signature,traversal_digest";
        public const string CellsHeader =
            "slice_id,chunk_index,local_x,local_y,sector_x,sector_y,is_passable,is_protected,is_blocked,protection,layer_count,witness_count,layer_summary,witness_summary,layer_digest,witness_digest";
        public const string SocketsHeader =
            "slice_id,chunk_index,side,band_count,band_ranges,side_signature,slice_signature,band_digest";
        public const string SlotsHeader =
            "slot_id,kind,owner,source_key,slice_id,chunk_index,local_x,local_y,sector_x,sector_y,projection_ordinal,source_task_id,source_layer,source_layer_token,source_claim_or_evidence_id,source_socket_identity,source_signature_identity,source_traversal_identity,provenance_digest";

        public static GeneratedTerrainExportResult Build(
            GeneratedMicroChunkSliceSet sliceSet,
            GeneratedMicroChunkMarkerSlotSet slotSet,
            IEnumerable<GeneratedMicroChunkSliceRecord> sourceSlices = null,
            IEnumerable<GeneratedMarkerSlot> sourceSlots = null)
        {
            var failures = ValidateSources(sliceSet, slotSet).ToList();
            var slices = (sourceSlices ?? (sliceSet == null
                ? Array.Empty<GeneratedMicroChunkSliceRecord>() : sliceSet.Slices))
                .Where(value => value != null).OrderBy(value => value.ChunkIndex).ToArray();
            var slots = (sourceSlots ?? (slotSet == null
                ? Array.Empty<GeneratedMarkerSlot>() : slotSet.Slots))
                .Where(value => value != null).OrderBy(value => value).ToArray();

            if (sliceSet != null && !slices.Select(value => value.Id.Value)
                    .SequenceEqual(sliceSet.Slices.OrderBy(value => value.ChunkIndex)
                        .Select(value => value.Id.Value), StringComparer.Ordinal))
                failures.Add(Failure(GeneratedTerrainExportFailureCode.SourcePacketMismatch,
                    "slices", "The supplied slice enumeration does not match the source packet."));
            if (slotSet != null && !slots.Select(value => value.Id.Value)
                    .SequenceEqual(slotSet.Slots.OrderBy(value => value)
                        .Select(value => value.Id.Value), StringComparer.Ordinal))
                failures.Add(Failure(GeneratedTerrainExportFailureCode.SourcePacketMismatch,
                    "slots", "The supplied slot enumeration does not match the source packet."));
            if (failures.Count > 0)
                return new GeneratedTerrainExportResult(null, failures);

            var plans = new[] { new GeneratedTerrainPlanRow(
                slotSet.SourceSliceSetId, sliceSet.OutputDigest, slotSet.OutputDigest) };
            var sliceRows = slices.Select(value => new GeneratedTerrainSliceRow(value)).ToArray();
            var cellRows = slices.SelectMany(slice => slice.Cells.Select(cell =>
                new GeneratedTerrainCellRow(slice.Id.Value, cell)))
                .OrderBy(value => value.SectorY).ThenBy(value => value.SectorX).ToArray();
            var socketRows = slices.SelectMany(slice => slice.SideSignatures.Select(signature =>
                new GeneratedTerrainSocketRow(slice, signature)))
                .OrderBy(value => value.ChunkIndex).ThenBy(value => value.Side,
                    StringComparer.Ordinal).ToArray();
            var slotRows = slots.Select(value => new GeneratedTerrainSlotRow(value)).ToArray();

            var dataFiles = new[]
            {
                File(PlanFileName, PlanHeader, plans.Select(PlanFields)),
                File(SlicesFileName, SlicesHeader, sliceRows.Select(SliceFields)),
                File(CellsFileName, CellsHeader, cellRows.Select(CellFields)),
                File(SocketsFileName, SocketsHeader, socketRows.Select(SocketFields)),
                File(SlotsFileName, SlotsHeader, slotRows.Select(SlotFields)),
            };
            var packetDigest = ComputePacketDigest(sliceSet.OutputDigest,
                slotSet.OutputDigest, dataFiles);
            var manifest = new GeneratedTerrainExportManifest(sliceSet.OutputDigest,
                slotSet.OutputDigest, dataFiles, packetDigest);
            var manifestFile = File(ManifestFileName, ManifestHeader,
                new[] { ManifestFields(manifest) });
            var files = new[] { manifestFile }.Concat(dataFiles).ToArray();
            var packet = new GeneratedTerrainExportPacket(manifest, plans, sliceRows,
                cellRows, socketRows, slotRows, files, manifestFile.PayloadDigest, packetDigest);
            return new GeneratedTerrainExportResult(packet,
                Array.Empty<GeneratedTerrainExportFailure>());
        }

        public static GeneratedTerrainExportResult Export(
            GeneratedMicroChunkSliceSet sliceSet,
            GeneratedMicroChunkMarkerSlotSet slotSet,
            string outputDirectory)
        {
            var build = Build(sliceSet, slotSet);
            return build.Success ? Write(build.Packet, outputDirectory) : build;
        }

        public static GeneratedTerrainExportResult Write(
            GeneratedTerrainExportPacket packet, string outputDirectory)
        {
            if (packet == null)
                return Failed(GeneratedTerrainExportFailureCode.IncompleteSourcePacket,
                    "packet", "An export packet is required.");
            string fullPath;
            try
            {
                fullPath = string.IsNullOrWhiteSpace(outputDirectory)
                    ? string.Empty : Path.GetFullPath(outputDirectory);
            }
            catch (Exception exception)
            {
                return Failed(GeneratedTerrainExportFailureCode.InvalidOutputDirectory,
                    outputDirectory, exception.GetType().Name);
            }
            if (string.IsNullOrEmpty(fullPath))
                return Failed(GeneratedTerrainExportFailureCode.InvalidOutputDirectory,
                    outputDirectory, "The output directory is empty.");
            if (IsRefusedPath(fullPath))
                return Failed(GeneratedTerrainExportFailureCode.RefusedProjectPath,
                    fullPath, "Project asset and package paths are not valid export destinations.");
            if (Directory.Exists(fullPath) || System.IO.File.Exists(fullPath))
                return Failed(GeneratedTerrainExportFailureCode.OutputDirectoryExists,
                    fullPath, "The output destination must not already exist.");

            var parent = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(parent))
                return Failed(GeneratedTerrainExportFailureCode.InvalidOutputDirectory,
                    fullPath, "The output directory has no parent.");
            var staging = Path.Combine(parent, ".map16_07_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(staging);
                foreach (var file in packet.Files)
                    System.IO.File.WriteAllText(Path.Combine(staging, file.FileName), file.Payload,
                        new UTF8Encoding(false));
                Directory.Move(staging, fullPath);
                return new GeneratedTerrainExportResult(packet,
                    Array.Empty<GeneratedTerrainExportFailure>(), fullPath);
            }
            catch (Exception exception)
            {
                try { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
                catch { }
                return Failed(GeneratedTerrainExportFailureCode.WriteFailed,
                    fullPath, exception.GetType().Name + ": " + exception.Message);
            }
        }

        internal static string ComputePacketDigest(
            string sourceSliceDigest,
            string sourceSlotDigest,
            IEnumerable<GeneratedTerrainExportFile> dataFiles)
        {
            var lines = new List<string>
            {
                "FORMAT|" + GeneratedTerrainExportPacket.FormatVersion,
                "TASK|" + GeneratedTerrainExportPacket.TaskId,
                "SOURCE|" + (sourceSliceDigest ?? string.Empty) + "|" +
                    (sourceSlotDigest ?? string.Empty),
                string.Join("|", new[]
                {
                    "GEOMETRY", Number(GeneratedTerrainGeometrySnapshot.CanonicalSectorWidth),
                    Number(GeneratedTerrainGeometrySnapshot.CanonicalSectorHeight),
                    Number(GeneratedTerrainGeometrySnapshot.CanonicalSectorCellCount),
                    Number(GeneratedTerrainGeometrySnapshot.CanonicalChunkGridWidth),
                    Number(GeneratedTerrainGeometrySnapshot.CanonicalChunkGridHeight),
                    Number(GeneratedTerrainGeometrySnapshot.CanonicalChunkCount),
                    Number(GeneratedTerrainGeometrySnapshot.CanonicalMicroChunkWidth),
                    Number(GeneratedTerrainGeometrySnapshot.CanonicalMicroChunkHeight),
                    Number(GeneratedTerrainGeometrySnapshot.CanonicalMicroChunkCellCount),
                    Number(GeneratedTerrainGeometrySnapshot.CanonicalMicroPatternWidth),
                }),
            };
            lines.AddRange((dataFiles ?? Array.Empty<GeneratedTerrainExportFile>())
                .OrderBy(value => FileOrder(value.FileName)).Select(value => string.Join("|", new[]
                {
                    "FILE", value.FileName, Number(value.RowCount), value.PayloadDigest,
                })));
            return GeneratedTerrainExportDigest.Hash(string.Join("\n", lines));
        }

        internal static GeneratedTerrainExportFile File(
            string fileName, string header, IEnumerable<IEnumerable<string>> sourceRows)
        {
            var rows = (sourceRows ?? Array.Empty<IEnumerable<string>>()).Select(CsvLine).ToArray();
            var payload = header + "\n" + (rows.Length == 0
                ? string.Empty : string.Join("\n", rows) + "\n");
            return new GeneratedTerrainExportFile(fileName, header, rows.Length, payload);
        }

        internal static string CsvLine(IEnumerable<string> values) => string.Join(",",
            (values ?? Array.Empty<string>()).Select(Escape));

        internal static string Escape(string value)
        {
            var text = value ?? string.Empty;
            return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0
                ? text : "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        internal static int FileOrder(string fileName)
        {
            for (var index = 0; index < RequiredFileNames.Count; index++)
                if (string.Equals(RequiredFileNames[index], fileName, StringComparison.Ordinal))
                    return index;
            return int.MaxValue;
        }

        private static IEnumerable<GeneratedTerrainExportFailure> ValidateSources(
            GeneratedMicroChunkSliceSet sliceSet,
            GeneratedMicroChunkMarkerSlotSet slotSet)
        {
            if (sliceSet == null)
                yield return Failure(GeneratedTerrainExportFailureCode.MissingSliceSet,
                    "slice_set", "A source slice set is required.");
            if (slotSet == null)
                yield return Failure(GeneratedTerrainExportFailureCode.MissingSlotSet,
                    "slot_set", "A marker slot set is required.");
            if (sliceSet == null || slotSet == null) yield break;
            if (!ReferenceEquals(sliceSet, slotSet.SourceSliceSet) ||
                !string.Equals(sliceSet.OutputDigest, slotSet.SourceSliceSetDigest,
                    StringComparison.Ordinal))
                yield return Failure(GeneratedTerrainExportFailureCode.SourcePacketMismatch,
                    "source", "The slot packet does not reference the supplied slice packet.");
            if (sliceSet.SliceCount != GeneratedMicroChunkSliceSet.ChunkCount ||
                sliceSet.TotalCellCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                sliceSet.TotalLayerRecordCount != GeneratedMicroChunkSliceSet.SectorCellCount *
                    GeneratedMicroChunkSliceSet.LayerKindsPerCell ||
                sliceSet.SocketSideSignatureCount != GeneratedMicroChunkSliceSet.ChunkCount * 4 ||
                slotSet.SlotCount == 0 || slotSet.MissingProvenanceCount != 0)
                yield return Failure(GeneratedTerrainExportFailureCode.IncompleteSourcePacket,
                    "source", "The source packets do not publish complete terrain data.");
            if (!GeneratedTerrainExportDigest.IsLowerHexSha256(sliceSet.InputDigest) ||
                !GeneratedTerrainExportDigest.IsLowerHexSha256(sliceSet.OutputDigest) ||
                !GeneratedTerrainExportDigest.IsLowerHexSha256(slotSet.InputDigest) ||
                !GeneratedTerrainExportDigest.IsLowerHexSha256(slotSet.OutputDigest) ||
                !string.Equals(GeneratedMicroChunkSliceDigest.ComputeOutput(sliceSet),
                    sliceSet.OutputDigest, StringComparison.Ordinal) ||
                !string.Equals(MarkerSlotProjectionDigest.ComputeOutput(slotSet),
                    slotSet.OutputDigest, StringComparison.Ordinal))
                yield return Failure(GeneratedTerrainExportFailureCode.InvalidSourceDigest,
                    "source", "A source packet digest is missing or does not replay.");
        }

        private static bool IsRefusedPath(string path)
        {
            var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(value => !string.IsNullOrEmpty(value)).ToArray();
            var refused = new[] { "Assets", "Packages", "Authoring", "Generated", "Scenes", "Prefabs" };
            return segments.Any(segment => refused.Any(value =>
                string.Equals(segment, value, StringComparison.OrdinalIgnoreCase)));
        }

        private static IEnumerable<string> PlanFields(GeneratedTerrainPlanRow value) => new[]
        {
            value.SectorId, value.SourceSliceSetDigest, value.SourceMarkerSlotSetDigest,
            Number(value.SectorWidth), Number(value.SectorHeight), Number(value.SectorCellCount),
            Number(value.ChunkGridWidth), Number(value.ChunkGridHeight), Number(value.ChunkCount),
            Number(value.MicroChunkWidth), Number(value.MicroChunkHeight),
            Number(value.MicroChunkCellCount), Number(value.MicroPatternWidth),
            Number(value.MicroPatternHeight),
        };

        private static IEnumerable<string> SliceFields(GeneratedTerrainSliceRow value) => new[]
        {
            value.SliceId, Number(value.ChunkIndex), Number(value.ChunkX), Number(value.ChunkY),
            Number(value.SectorOriginX), Number(value.SectorOriginY), Number(value.CellCount),
            Number(value.LayerRecordCount), Number(value.SocketBandCount),
            Number(value.PassableCellCount), Number(value.BlockedCellCount),
            Number(value.RouteRecoveryWitnessCellCount), value.SliceSignature, value.TraversalDigest,
        };

        private static IEnumerable<string> CellFields(GeneratedTerrainCellRow value) => new[]
        {
            value.SliceId, Number(value.ChunkIndex), Number(value.LocalX), Number(value.LocalY),
            Number(value.SectorX), Number(value.SectorY), Flag(value.IsPassable),
            Flag(value.IsProtected), Flag(value.IsBlocked), value.Protection,
            Number(value.LayerCount), Number(value.WitnessCount), value.LayerSummary,
            value.WitnessSummary, value.LayerDigest, value.WitnessDigest,
        };

        private static IEnumerable<string> SocketFields(GeneratedTerrainSocketRow value) => new[]
        {
            value.SliceId, Number(value.ChunkIndex), value.Side, Number(value.BandCount),
            value.BandRanges, value.SideSignature, value.SliceSignature, value.BandDigest,
        };

        private static IEnumerable<string> SlotFields(GeneratedTerrainSlotRow value) => new[]
        {
            value.SlotId, value.Kind, value.Owner, value.SourceKey, value.SliceId,
            Number(value.ChunkIndex), Number(value.LocalX), Number(value.LocalY),
            Number(value.SectorX), Number(value.SectorY), Number(value.ProjectionOrdinal),
            value.SourceTaskId, value.SourceLayer, value.SourceLayerToken,
            value.SourceClaimOrEvidenceId, value.SourceSocketIdentity,
            value.SourceSignatureIdentity, value.SourceTraversalIdentity, value.ProvenanceDigest,
        };

        private static IEnumerable<string> ManifestFields(GeneratedTerrainExportManifest value)
        {
            var plan = PlanFileName;
            var slices = SlicesFileName;
            var cells = CellsFileName;
            var sockets = SocketsFileName;
            var slots = SlotsFileName;
            return new[]
            {
                value.FormatVersion, value.TaskId, value.SourceSliceSetDigest,
                value.SourceMarkerSlotSetDigest, Number(value.SectorWidth),
                Number(value.SectorHeight), Number(value.SectorCellCount),
                Number(value.ChunkGridWidth), Number(value.ChunkGridHeight),
                Number(value.ChunkCount), Number(value.MicroChunkWidth),
                Number(value.MicroChunkHeight), Number(value.MicroChunkCellCount),
                Number(value.MicroPatternSize), Number(value.FileRowCounts[plan]),
                value.FileDigests[plan], Number(value.FileRowCounts[slices]),
                value.FileDigests[slices], Number(value.FileRowCounts[cells]),
                value.FileDigests[cells], Number(value.FileRowCounts[sockets]),
                value.FileDigests[sockets], Number(value.FileRowCounts[slots]),
                value.FileDigests[slots], value.PacketDigest,
            };
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Flag(bool value) => value ? "1" : "0";
        private static GeneratedTerrainExportFailure Failure(
            GeneratedTerrainExportFailureCode code, string subject, string reason) =>
            new GeneratedTerrainExportFailure(code, subject, reason);
        private static GeneratedTerrainExportResult Failed(
            GeneratedTerrainExportFailureCode code, string subject, string reason) =>
            new GeneratedTerrainExportResult(null, new[] { Failure(code, subject, reason) });
    }
}
