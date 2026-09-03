using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum GeneratedTerrainExportFailureCode
    {
        MissingSliceSet = 1,
        MissingSlotSet = 2,
        SourcePacketMismatch = 3,
        IncompleteSourcePacket = 4,
        InvalidSourceDigest = 5,
        InvalidOutputDirectory = 6,
        RefusedProjectPath = 7,
        OutputDirectoryExists = 8,
        WriteFailed = 9,
        MissingFile = 10,
        ExtraFile = 11,
        DuplicateFile = 12,
        MalformedCsv = 13,
        HeaderMismatch = 14,
        RowCountMismatch = 15,
        PayloadDigestMismatch = 16,
        ManifestDigestMismatch = 17,
        PacketDigestMismatch = 18,
    }

    public sealed class GeneratedTerrainExportFailure :
        IComparable<GeneratedTerrainExportFailure>, IEquatable<GeneratedTerrainExportFailure>
    {
        public GeneratedTerrainExportFailure(
            GeneratedTerrainExportFailureCode code, string subject, string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public GeneratedTerrainExportFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public string StableToken => Code + "|" + Subject + "|" + Reason;

        public int CompareTo(GeneratedTerrainExportFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }

        public bool Equals(GeneratedTerrainExportFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as GeneratedTerrainExportFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedTerrainPlanRow
    {
        internal GeneratedTerrainPlanRow(
            string sectorId, string sourceSliceDigest, string sourceSlotDigest)
        {
            SectorId = sectorId ?? string.Empty;
            SourceSliceSetDigest = sourceSliceDigest ?? string.Empty;
            SourceMarkerSlotSetDigest = sourceSlotDigest ?? string.Empty;
        }

        public string SectorId { get; }
        public string SourceSliceSetDigest { get; }
        public string SourceMarkerSlotSetDigest { get; }
        public int SectorWidth => GeneratedMicroChunkSliceSet.SectorWidth;
        public int SectorHeight => GeneratedMicroChunkSliceSet.SectorHeight;
        public int SectorCellCount => GeneratedMicroChunkSliceSet.SectorCellCount;
        public int ChunkGridWidth => GeneratedMicroChunkSliceSet.ChunkGridWidth;
        public int ChunkGridHeight => GeneratedMicroChunkSliceSet.ChunkGridHeight;
        public int ChunkCount => GeneratedMicroChunkSliceSet.ChunkCount;
        public int MicroChunkWidth => GeneratedMicroChunkSliceSet.MicroChunkWidth;
        public int MicroChunkHeight => GeneratedMicroChunkSliceSet.MicroChunkHeight;
        public int MicroChunkCellCount => GeneratedMicroChunkSliceSet.MicroChunkCellCount;
        public int MicroPatternWidth => GeneratedMicroChunkSliceSet.MicroPatternWidth;
        public int MicroPatternHeight => GeneratedMicroChunkSliceSet.MicroPatternHeight;
    }

    public sealed class GeneratedTerrainSliceRow
    {
        internal GeneratedTerrainSliceRow(GeneratedMicroChunkSliceRecord value)
        {
            SliceId = value.Id.Value;
            ChunkIndex = value.ChunkIndex;
            ChunkX = value.ChunkX;
            ChunkY = value.ChunkY;
            SectorOriginX = value.SectorOrigin.X;
            SectorOriginY = value.SectorOrigin.Y;
            CellCount = value.CellCount;
            LayerRecordCount = value.LayerRecordCount;
            SocketBandCount = value.SocketBands.Count;
            PassableCellCount = value.TraversalSummary.PassableCellCount;
            BlockedCellCount = value.TraversalSummary.BlockedCellCount;
            RouteRecoveryWitnessCellCount = value.TraversalSummary.RouteRecoveryWitnessCellCount;
            SliceSignature = value.Signature.Digest;
            TraversalDigest = GeneratedTerrainExportDigest.Hash(value.TraversalSummary.StableToken);
        }

        public string SliceId { get; }
        public int ChunkIndex { get; }
        public int ChunkX { get; }
        public int ChunkY { get; }
        public int SectorOriginX { get; }
        public int SectorOriginY { get; }
        public int CellCount { get; }
        public int LayerRecordCount { get; }
        public int SocketBandCount { get; }
        public int PassableCellCount { get; }
        public int BlockedCellCount { get; }
        public int RouteRecoveryWitnessCellCount { get; }
        public string SliceSignature { get; }
        public string TraversalDigest { get; }
    }

    public sealed class GeneratedTerrainCellRow
    {
        internal GeneratedTerrainCellRow(string sliceId, GeneratedMicroChunkCell value)
        {
            SliceId = sliceId ?? string.Empty;
            ChunkIndex = value.ChunkIndex;
            LocalX = value.LocalCoordinate.X;
            LocalY = value.LocalCoordinate.Y;
            SectorX = value.SectorCoordinate.X;
            SectorY = value.SectorCoordinate.Y;
            IsPassable = value.IsPassable;
            IsProtected = value.IsProtected;
            IsBlocked = value.IsSolid || value.IsHazardBlocked || value.IsProtectionBlocked;
            Protection = value.ProtectionKind.ToString().ToUpperInvariant();
            LayerCount = value.LayerCount;
            WitnessCount = value.WitnessMembershipCount;
            LayerSummary = string.Join(";", value.Layers.OrderBy(layer => layer)
                .Select(layer => layer.StableToken));
            WitnessSummary = string.Join(";", value.WitnessMemberships.OrderBy(item => item)
                .Select(item => item.StableToken));
            LayerDigest = GeneratedTerrainExportDigest.Hash(LayerSummary);
            WitnessDigest = GeneratedTerrainExportDigest.Hash(WitnessSummary);
        }

        public string SliceId { get; }
        public int ChunkIndex { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public int SectorX { get; }
        public int SectorY { get; }
        public bool IsPassable { get; }
        public bool IsProtected { get; }
        public bool IsBlocked { get; }
        public string Protection { get; }
        public int LayerCount { get; }
        public int WitnessCount { get; }
        public string LayerSummary { get; }
        public string WitnessSummary { get; }
        public string LayerDigest { get; }
        public string WitnessDigest { get; }
    }

    public sealed class GeneratedTerrainSocketRow
    {
        internal GeneratedTerrainSocketRow(
            GeneratedMicroChunkSliceRecord slice,
            GeneratedMicroChunkSocketSignature signature)
        {
            SliceId = slice.Id.Value;
            ChunkIndex = slice.ChunkIndex;
            Side = signature.Side.Value.ToString().ToUpperInvariant();
            SideSignature = signature.Digest;
            SliceSignature = slice.Signature.Digest;
            var bands = slice.Bands(signature.Side.Value).ToArray();
            BandCount = bands.Length;
            BandRanges = string.Join(";", bands.Select(value => string.Join(":", new[]
            {
                value.Start.ToString(), value.End.ToString(),
                value.Length.ToString(CultureInfo.InvariantCulture),
                value.TouchesPassableComponent ? "1" : "0",
            })));
            BandDigest = GeneratedTerrainExportDigest.Hash(string.Join("\n",
                bands.Select(value => value.StableToken)));
        }

        public string SliceId { get; }
        public int ChunkIndex { get; }
        public string Side { get; }
        public int BandCount { get; }
        public string BandRanges { get; }
        public string SideSignature { get; }
        public string SliceSignature { get; }
        public string BandDigest { get; }
    }

    public sealed class GeneratedTerrainSlotRow
    {
        internal GeneratedTerrainSlotRow(GeneratedMarkerSlot value)
        {
            SlotId = value.Id.Value;
            Kind = value.Kind.ToString().ToUpperInvariant();
            Owner = value.Owner.ToString().ToUpperInvariant();
            SourceKey = value.SourceKey;
            SliceId = value.CellReference.SourceSliceId;
            ChunkIndex = value.CellReference.ChunkIndex;
            LocalX = value.CellReference.LocalX;
            LocalY = value.CellReference.LocalY;
            SectorX = value.CellReference.SectorX;
            SectorY = value.CellReference.SectorY;
            ProjectionOrdinal = value.ProjectionOrdinal;
            SourceTaskId = value.Provenance.SourceTaskId;
            SourceLayer = value.Provenance.SourceLayerKind.ToString().ToUpperInvariant();
            SourceLayerToken = value.Provenance.SourceLayerToken;
            SourceClaimOrEvidenceId = value.Provenance.SourceClaimOrEvidenceId;
            SourceSocketIdentity = value.Provenance.SourceSocketIdentity;
            SourceSignatureIdentity = value.Provenance.SourceSignatureIdentity;
            SourceTraversalIdentity = value.Provenance.SourceTraversalIdentity;
            ProvenanceDigest = GeneratedTerrainExportDigest.Hash(value.Provenance.StableToken);
        }

        public string SlotId { get; }
        public string Kind { get; }
        public string Owner { get; }
        public string SourceKey { get; }
        public string SliceId { get; }
        public int ChunkIndex { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public int SectorX { get; }
        public int SectorY { get; }
        public int ProjectionOrdinal { get; }
        public string SourceTaskId { get; }
        public string SourceLayer { get; }
        public string SourceLayerToken { get; }
        public string SourceClaimOrEvidenceId { get; }
        public string SourceSocketIdentity { get; }
        public string SourceSignatureIdentity { get; }
        public string SourceTraversalIdentity { get; }
        public string ProvenanceDigest { get; }
    }

    public sealed class GeneratedTerrainExportFile
    {
        internal GeneratedTerrainExportFile(
            string fileName, string header, int rowCount, string payload)
        {
            FileName = fileName ?? string.Empty;
            Header = header ?? string.Empty;
            RowCount = rowCount;
            Payload = GeneratedTerrainExportDigest.Canonicalize(payload);
            PayloadDigest = GeneratedTerrainExportDigest.Hash(Payload);
        }

        public string FileName { get; }
        public string Header { get; }
        public int RowCount { get; }
        public string Payload { get; }
        public string PayloadDigest { get; }
    }

    public sealed class GeneratedTerrainExportManifest
    {
        private readonly ReadOnlyDictionary<string, string> fileDigests;
        private readonly ReadOnlyDictionary<string, int> fileRowCounts;

        internal GeneratedTerrainExportManifest(
            string sourceSliceDigest,
            string sourceSlotDigest,
            IEnumerable<GeneratedTerrainExportFile> dataFiles,
            string packetDigest)
        {
            FormatVersion = GeneratedTerrainExportPacket.FormatVersion;
            TaskId = GeneratedTerrainExportPacket.TaskId;
            SourceSliceSetDigest = sourceSliceDigest ?? string.Empty;
            SourceMarkerSlotSetDigest = sourceSlotDigest ?? string.Empty;
            PacketDigest = packetDigest ?? string.Empty;
            var files = (dataFiles ?? Array.Empty<GeneratedTerrainExportFile>()).ToArray();
            fileDigests = new ReadOnlyDictionary<string, string>(files.ToDictionary(
                value => value.FileName, value => value.PayloadDigest, StringComparer.Ordinal));
            fileRowCounts = new ReadOnlyDictionary<string, int>(files.ToDictionary(
                value => value.FileName, value => value.RowCount, StringComparer.Ordinal));
        }

        public string FormatVersion { get; }
        public string TaskId { get; }
        public string SourceSliceSetDigest { get; }
        public string SourceMarkerSlotSetDigest { get; }
        public string PacketDigest { get; }
        public IReadOnlyDictionary<string, string> FileDigests => fileDigests;
        public IReadOnlyDictionary<string, int> FileRowCounts => fileRowCounts;
        public int SectorWidth => GeneratedMicroChunkSliceSet.SectorWidth;
        public int SectorHeight => GeneratedMicroChunkSliceSet.SectorHeight;
        public int SectorCellCount => GeneratedMicroChunkSliceSet.SectorCellCount;
        public int ChunkGridWidth => GeneratedMicroChunkSliceSet.ChunkGridWidth;
        public int ChunkGridHeight => GeneratedMicroChunkSliceSet.ChunkGridHeight;
        public int ChunkCount => GeneratedMicroChunkSliceSet.ChunkCount;
        public int MicroChunkWidth => GeneratedMicroChunkSliceSet.MicroChunkWidth;
        public int MicroChunkHeight => GeneratedMicroChunkSliceSet.MicroChunkHeight;
        public int MicroChunkCellCount => GeneratedMicroChunkSliceSet.MicroChunkCellCount;
        public int MicroPatternSize => GeneratedMicroChunkSliceSet.MicroPatternWidth;
    }

    public sealed class GeneratedTerrainExportPacket
    {
        private readonly ReadOnlyCollection<GeneratedTerrainPlanRow> planRows;
        private readonly ReadOnlyCollection<GeneratedTerrainSliceRow> sliceRows;
        private readonly ReadOnlyCollection<GeneratedTerrainCellRow> cellRows;
        private readonly ReadOnlyCollection<GeneratedTerrainSocketRow> socketRows;
        private readonly ReadOnlyCollection<GeneratedTerrainSlotRow> slotRows;
        private readonly ReadOnlyCollection<GeneratedTerrainExportFile> files;

        internal GeneratedTerrainExportPacket(
            GeneratedTerrainExportManifest manifest,
            IEnumerable<GeneratedTerrainPlanRow> plans,
            IEnumerable<GeneratedTerrainSliceRow> slices,
            IEnumerable<GeneratedTerrainCellRow> cells,
            IEnumerable<GeneratedTerrainSocketRow> sockets,
            IEnumerable<GeneratedTerrainSlotRow> slots,
            IEnumerable<GeneratedTerrainExportFile> sourceFiles,
            string manifestDigest,
            string packetDigest)
        {
            Manifest = manifest;
            planRows = new ReadOnlyCollection<GeneratedTerrainPlanRow>((plans ??
                Array.Empty<GeneratedTerrainPlanRow>()).ToArray());
            sliceRows = new ReadOnlyCollection<GeneratedTerrainSliceRow>((slices ??
                Array.Empty<GeneratedTerrainSliceRow>()).ToArray());
            cellRows = new ReadOnlyCollection<GeneratedTerrainCellRow>((cells ??
                Array.Empty<GeneratedTerrainCellRow>()).ToArray());
            socketRows = new ReadOnlyCollection<GeneratedTerrainSocketRow>((sockets ??
                Array.Empty<GeneratedTerrainSocketRow>()).ToArray());
            slotRows = new ReadOnlyCollection<GeneratedTerrainSlotRow>((slots ??
                Array.Empty<GeneratedTerrainSlotRow>()).ToArray());
            files = new ReadOnlyCollection<GeneratedTerrainExportFile>((sourceFiles ??
                Array.Empty<GeneratedTerrainExportFile>()).ToArray());
            ManifestDigest = manifestDigest ?? string.Empty;
            PacketDigest = packetDigest ?? string.Empty;
        }

        public const string FormatVersion = "MAP16_07_GENERATED_TERRAIN_EXPORT_V1";
        public const string TaskId = "MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN";
        public const string ReferencePublicationLabel = "REFERENCE GENERATED TERRAIN EXPORT";
        public const string DownstreamOwner = "MAP16_08_MAP16_SLICE_AND_OUTPUT_EXIT_TESTS";
        public const bool OpensDownstreamTask = false;

        public GeneratedTerrainExportManifest Manifest { get; }
        public IReadOnlyList<GeneratedTerrainPlanRow> PlanRows => planRows;
        public IReadOnlyList<GeneratedTerrainSliceRow> SliceRows => sliceRows;
        public IReadOnlyList<GeneratedTerrainCellRow> CellRows => cellRows;
        public IReadOnlyList<GeneratedTerrainSocketRow> SocketRows => socketRows;
        public IReadOnlyList<GeneratedTerrainSlotRow> SlotRows => slotRows;
        public IReadOnlyList<GeneratedTerrainExportFile> Files => files;
        public string ManifestDigest { get; }
        public string PacketDigest { get; }
        public int LogicalFileCount => files.Count;
        public int LayerRecordCount => cellRows.Sum(value => value.LayerCount);
        public int SocketBandCount => socketRows.Sum(value => value.BandCount);
    }

    public sealed class GeneratedTerrainExportResult
    {
        private readonly ReadOnlyCollection<GeneratedTerrainExportFailure> failures;

        internal GeneratedTerrainExportResult(
            GeneratedTerrainExportPacket packet,
            IEnumerable<GeneratedTerrainExportFailure> sourceFailures,
            string outputDirectory = null)
        {
            Packet = packet;
            failures = new ReadOnlyCollection<GeneratedTerrainExportFailure>((sourceFailures ??
                Array.Empty<GeneratedTerrainExportFailure>()).OrderBy(value => value).ToArray());
            OutputDirectory = outputDirectory ?? string.Empty;
        }

        public bool Success => Packet != null && failures.Count == 0;
        public GeneratedTerrainExportPacket Packet { get; }
        public IReadOnlyList<GeneratedTerrainExportFailure> Failures => failures;
        public string OutputDirectory { get; }
        public int WrittenFileCount => Success && !string.IsNullOrEmpty(OutputDirectory)
            ? Packet.Files.Count : 0;
    }

    public static class GeneratedTerrainExportDigest
    {
        public static string Canonicalize(string value) =>
            BakingCanonicalDigest.NormalizeLineEndingsToLf(value ?? string.Empty);

        public static string Hash(string value) =>
            BakingCanonicalDigest.HashCanonicalText(value ?? string.Empty);

        public static bool IsLowerHexSha256(string value) =>
            BakingCanonicalDigest.IsLowerHexSha256(value);

        internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    }
}
