using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum GeneratedMarkerSlotKind
    {
        TerrainCluster = 1,
        Activity = 2,
        SpecialRegion = 3,
        EventOverlay = 4,
        Boundary = 5,
        RouteRecovery = 6,
        Decoration = 7,
    }

    public enum GeneratedMarkerSlotOwner
    {
        TerrainCluster = 1,
        Activity = 2,
        SpecialRegion = 3,
        EventOverlay = 4,
        Boundary = 5,
        RouteRecovery = 6,
        Decoration = 7,
    }

    public sealed class GeneratedMarkerSlotId :
        IEquatable<GeneratedMarkerSlotId>, IComparable<GeneratedMarkerSlotId>
    {
        public GeneratedMarkerSlotId(
            string sectorId,
            int chunkIndex,
            int localX,
            int localY,
            GeneratedMarkerSlotKind kind,
            GeneratedMarkerSlotOwner owner,
            string sourceKey,
            int projectionOrdinal)
        {
            SectorId = sectorId ?? string.Empty;
            ChunkIndex = chunkIndex;
            LocalX = localX;
            LocalY = localY;
            Kind = kind;
            Owner = owner;
            SourceKey = sourceKey ?? string.Empty;
            ProjectionOrdinal = projectionOrdinal;
            var sourceDigest = MarkerSlotProjectionDigest.HashCanonicalText(SourceKey);
            Value = string.Join(":", new[]
            {
                SectorId, "CHUNK", Number(ChunkIndex, "D2"), "CELL",
                Number(LocalX, "D2") + "," + Number(LocalY, "D2"),
                Kind.ToString().ToUpperInvariant(), Owner.ToString().ToUpperInvariant(),
                Number(ProjectionOrdinal, "D2"), sourceDigest.Substring(0, 16),
            });
        }

        public string SectorId { get; }
        public int ChunkIndex { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public GeneratedMarkerSlotKind Kind { get; }
        public GeneratedMarkerSlotOwner Owner { get; }
        public string SourceKey { get; }
        public int ProjectionOrdinal { get; }
        public string Value { get; }

        public int CompareTo(GeneratedMarkerSlotId other)
        {
            if (other == null) return -1;
            var comparison = ChunkIndex.CompareTo(other.ChunkIndex);
            if (comparison != 0) return comparison;
            comparison = LocalY.CompareTo(other.LocalY);
            if (comparison != 0) return comparison;
            comparison = LocalX.CompareTo(other.LocalX);
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = Owner.CompareTo(other.Owner);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SourceKey, other.SourceKey, StringComparison.Ordinal);
            return comparison != 0 ? comparison : ProjectionOrdinal.CompareTo(other.ProjectionOrdinal);
        }

        public bool Equals(GeneratedMarkerSlotId other) => other != null && Value == other.Value;
        public override bool Equals(object obj) => Equals(obj as GeneratedMarkerSlotId);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        private static string Number(int value, string format) =>
            value.ToString(format, CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedMarkerSlotCellRef :
        IEquatable<GeneratedMarkerSlotCellRef>, IComparable<GeneratedMarkerSlotCellRef>
    {
        public GeneratedMarkerSlotCellRef(
            string sourceSliceId,
            int chunkIndex,
            int localX,
            int localY,
            int sectorX,
            int sectorY)
        {
            SourceSliceId = sourceSliceId ?? string.Empty;
            ChunkIndex = chunkIndex;
            LocalX = localX;
            LocalY = localY;
            SectorX = sectorX;
            SectorY = sectorY;
            StableToken = string.Join("|", new[]
            {
                "CELL_REF", SourceSliceId, Number(ChunkIndex),
                Number(LocalX), Number(LocalY), Number(SectorX), Number(SectorY),
            });
        }

        public string SourceSliceId { get; }
        public int ChunkIndex { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public int SectorX { get; }
        public int SectorY { get; }
        public string StableToken { get; }
        public bool IsComplete => !string.IsNullOrEmpty(SourceSliceId) &&
            ChunkIndex >= 0 && ChunkIndex < GeneratedMicroChunkSliceSet.ChunkCount &&
            LocalX >= 0 && LocalX < GeneratedMicroChunkSliceSet.MicroChunkWidth &&
            LocalY >= 0 && LocalY < GeneratedMicroChunkSliceSet.MicroChunkHeight &&
            SectorX >= 0 && SectorX < GeneratedMicroChunkSliceSet.SectorWidth &&
            SectorY >= 0 && SectorY < GeneratedMicroChunkSliceSet.SectorHeight;

        public int CompareTo(GeneratedMarkerSlotCellRef other)
        {
            if (other == null) return -1;
            var comparison = ChunkIndex.CompareTo(other.ChunkIndex);
            if (comparison != 0) return comparison;
            comparison = LocalY.CompareTo(other.LocalY);
            return comparison != 0 ? comparison : LocalX.CompareTo(other.LocalX);
        }

        public bool Equals(GeneratedMarkerSlotCellRef other) => other != null &&
            SourceSliceId == other.SourceSliceId && ChunkIndex == other.ChunkIndex &&
            LocalX == other.LocalX && LocalY == other.LocalY &&
            SectorX == other.SectorX && SectorY == other.SectorY;
        public override bool Equals(object obj) => Equals(obj as GeneratedMarkerSlotCellRef);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedMarkerSlotProjection : IComparable<GeneratedMarkerSlotProjection>
    {
        public GeneratedMarkerSlotProjection(
            GeneratedMicroChunkSliceRecord sourceSlice,
            GeneratedMicroChunkCell sourceCell,
            GeneratedMicroChunkLayerRecord sourceLayer,
            GeneratedMarkerSlotKind kind,
            GeneratedMarkerSlotOwner owner,
            string sourceTaskId)
        {
            SourceSlice = sourceSlice;
            SourceCell = sourceCell;
            SourceLayer = sourceLayer;
            Kind = kind;
            Owner = owner;
            SourceTaskId = sourceTaskId ?? string.Empty;
            SourceKey = sourceLayer == null ? string.Empty : string.Join("|", new[]
            {
                sourceLayer.SourceOwner.ToString().ToUpperInvariant(),
                sourceLayer.Layer.ToString().ToUpperInvariant(),
                sourceLayer.ProvenanceId, sourceLayer.ClaimId, sourceLayer.SourceCellToken,
            });
            StableToken = string.Join("|", new[]
            {
                "PROJECTION", sourceSlice == null ? "MISSING_SLICE" : sourceSlice.Id.Value,
                sourceCell == null ? "MISSING_CELL" : sourceCell.StableToken,
                sourceLayer == null ? "MISSING_LAYER" : sourceLayer.StableToken,
                Kind.ToString().ToUpperInvariant(), Owner.ToString().ToUpperInvariant(),
                SourceTaskId, SourceKey,
            });
        }

        public GeneratedMicroChunkSliceRecord SourceSlice { get; }
        public GeneratedMicroChunkCell SourceCell { get; }
        public GeneratedMicroChunkLayerRecord SourceLayer { get; }
        public GeneratedMarkerSlotKind Kind { get; }
        public GeneratedMarkerSlotOwner Owner { get; }
        public string SourceTaskId { get; }
        public string SourceKey { get; }
        public string StableToken { get; }

        public int CompareTo(GeneratedMarkerSlotProjection other)
        {
            if (other == null) return -1;
            var leftChunk = SourceSlice == null ? int.MaxValue : SourceSlice.ChunkIndex;
            var rightChunk = other.SourceSlice == null ? int.MaxValue : other.SourceSlice.ChunkIndex;
            var comparison = leftChunk.CompareTo(rightChunk);
            if (comparison != 0) return comparison;
            var leftY = SourceCell == null ? int.MaxValue : SourceCell.LocalCoordinate.Y;
            var rightY = other.SourceCell == null ? int.MaxValue : other.SourceCell.LocalCoordinate.Y;
            comparison = leftY.CompareTo(rightY);
            if (comparison != 0) return comparison;
            var leftX = SourceCell == null ? int.MaxValue : SourceCell.LocalCoordinate.X;
            var rightX = other.SourceCell == null ? int.MaxValue : other.SourceCell.LocalCoordinate.X;
            comparison = leftX.CompareTo(rightX);
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = Owner.CompareTo(other.Owner);
            return comparison != 0
                ? comparison
                : string.Compare(SourceKey, other.SourceKey, StringComparison.Ordinal);
        }
    }

    public sealed class GeneratedMarkerSlotProvenance
    {
        internal GeneratedMarkerSlotProvenance(
            GeneratedMarkerSlotProjection projection,
            string sourceSocketIdentity,
            string sourceSignatureIdentity,
            string sourceTraversalIdentity)
        {
            SourceOwnerToken = projection.SourceLayer.SourceOwner.ToString();
            SourceTaskId = projection.SourceTaskId;
            SourceLayerKind = projection.SourceLayer.Layer;
            SourceLayerToken = projection.SourceLayer.StableToken;
            SourceClaimOrEvidenceId = !string.IsNullOrEmpty(projection.SourceLayer.ClaimId)
                ? projection.SourceLayer.ClaimId : projection.SourceLayer.ProvenanceId;
            SourceProvenanceId = projection.SourceLayer.ProvenanceId;
            SourceCellToken = projection.SourceLayer.SourceCellToken;
            SourceSliceId = projection.SourceSlice.Id.Value;
            SourceSocketIdentity = sourceSocketIdentity ?? string.Empty;
            SourceSignatureIdentity = sourceSignatureIdentity ?? string.Empty;
            SourceTraversalIdentity = sourceTraversalIdentity ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "PROVENANCE", SourceOwnerToken, SourceTaskId,
                SourceLayerKind.ToString().ToUpperInvariant(), SourceLayerToken,
                SourceClaimOrEvidenceId,
                SourceProvenanceId, SourceCellToken, SourceSliceId,
                SourceSocketIdentity, SourceSignatureIdentity, SourceTraversalIdentity,
            });
        }

        public string SourceOwnerToken { get; }
        public string SourceTaskId { get; }
        public FinalCanvasLayerKind SourceLayerKind { get; }
        public string SourceLayerToken { get; }
        public string SourceClaimOrEvidenceId { get; }
        public string SourceProvenanceId { get; }
        public string SourceCellToken { get; }
        public string SourceSliceId { get; }
        public string SourceSocketIdentity { get; }
        public string SourceSignatureIdentity { get; }
        public string SourceTraversalIdentity { get; }
        public string StableToken { get; }
        public bool IsComplete => !string.IsNullOrEmpty(SourceOwnerToken) &&
            !string.IsNullOrEmpty(SourceTaskId) && !string.IsNullOrEmpty(SourceClaimOrEvidenceId) &&
            !string.IsNullOrEmpty(SourceLayerToken) &&
            !string.IsNullOrEmpty(SourceProvenanceId) && !string.IsNullOrEmpty(SourceCellToken) &&
            !string.IsNullOrEmpty(SourceSliceId) && !string.IsNullOrEmpty(SourceSignatureIdentity) &&
            !string.IsNullOrEmpty(SourceTraversalIdentity);
    }

    public sealed class GeneratedMarkerSlot : IComparable<GeneratedMarkerSlot>
    {
        internal GeneratedMarkerSlot(
            GeneratedMarkerSlotId id,
            GeneratedMarkerSlotCellRef cellReference,
            GeneratedMarkerSlotProvenance provenance,
            GeneratedMarkerSlotKind kind,
            GeneratedMarkerSlotOwner owner,
            string sourceKey,
            int projectionOrdinal)
        {
            Id = id;
            CellReference = cellReference;
            Provenance = provenance;
            Kind = kind;
            Owner = owner;
            SourceKey = sourceKey ?? string.Empty;
            ProjectionOrdinal = projectionOrdinal;
            StableToken = string.Join("|", new[]
            {
                "SLOT", Id == null ? "MISSING" : Id.Value,
                Kind.ToString().ToUpperInvariant(), Owner.ToString().ToUpperInvariant(),
                SourceKey, Number(ProjectionOrdinal),
                CellReference == null ? "MISSING" : CellReference.StableToken,
                Provenance == null ? "MISSING" : Provenance.StableToken,
            });
        }

        public GeneratedMarkerSlotId Id { get; }
        public GeneratedMarkerSlotKind Kind { get; }
        public GeneratedMarkerSlotOwner Owner { get; }
        public string SourceKey { get; }
        public GeneratedMarkerSlotCellRef CellReference { get; }
        public GeneratedMarkerSlotProvenance Provenance { get; }
        public int ProjectionOrdinal { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedMarkerSlot other) => other == null
            ? -1 : (Id == null ? (other.Id == null ? 0 : 1)
                : (other.Id == null ? -1 : Id.CompareTo(other.Id)));
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedMicroChunkMarkerSlotSet
    {
        private static readonly GeneratedMarkerSlotKind[] RequiredKinds =
        {
            GeneratedMarkerSlotKind.TerrainCluster,
            GeneratedMarkerSlotKind.Activity,
            GeneratedMarkerSlotKind.SpecialRegion,
            GeneratedMarkerSlotKind.EventOverlay,
        };

        private readonly ReadOnlyCollection<GeneratedMarkerSlot> slots;
        private readonly ReadOnlyCollection<string> sourceSliceIds;
        private readonly ReadOnlyCollection<GeneratedMarkerSlotKind> optionalKinds;

        internal GeneratedMicroChunkMarkerSlotSet(
            GeneratedMicroChunkSliceSet sourceSliceSet,
            IEnumerable<GeneratedMarkerSlot> sourceSlots,
            int markerLayerRecordsScanned,
            string inputDigest)
        {
            SourceSliceSet = sourceSliceSet;
            slots = new ReadOnlyCollection<GeneratedMarkerSlot>((sourceSlots ??
                Array.Empty<GeneratedMarkerSlot>()).OrderBy(value => value).ToArray());
            sourceSliceIds = new ReadOnlyCollection<string>(sourceSliceSet.Slices
                .OrderBy(value => value).Select(value => value.Id.Value).ToArray());
            optionalKinds = new ReadOnlyCollection<GeneratedMarkerSlotKind>(slots
                .Select(value => value.Kind).Distinct().Where(value => !RequiredKinds.Contains(value))
                .OrderBy(value => value).ToArray());
            MarkerLayerRecordsScanned = markerLayerRecordsScanned;
            InputDigest = inputDigest ?? string.Empty;
            OutputDigest = MarkerSlotProjectionDigest.ComputeOutput(this);
        }

        public const string PolicyVersion = "MAP16_06_GENERATED_MARKER_SLOT_POLICY_V1";
        public const string ReferencePublicationLabel = "REFERENCE GENERATED MICROCHUNK MARKER SLOT SET";
        public const string DownstreamOwner = "MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN";
        public const bool OpensDownstreamTask = false;

        public GeneratedMicroChunkSliceSet SourceSliceSet { get; }
        public string SourceSliceSetId => SourceSliceSet.Request.CanvasPlan.Request.SectorId;
        public string SourceSliceSetDigest => SourceSliceSet.OutputDigest;
        public IReadOnlyList<string> SourceSliceIds => sourceSliceIds;
        public IReadOnlyList<GeneratedMarkerSlot> Slots => slots;
        public IReadOnlyList<GeneratedMarkerSlotKind> OptionalMarkerOwnerFamilies => optionalKinds;
        public string InputDigest { get; }
        public string OutputDigest { get; }
        public int SourceSliceCount => SourceSliceSet.SliceCount;
        public int SourceCellCount => SourceSliceSet.TotalCellCount;
        public int SourceLayerRecordCount => SourceSliceSet.TotalLayerRecordCount;
        public int MarkerLayerRecordsScanned { get; }
        public int MarkerLayerRecordsConsumed => slots.Count;
        public int SlotCount => slots.Count;
        public int SlotsWithStableLocalIdCount => slots.Count(value => value.Id != null &&
            !string.IsNullOrEmpty(value.Id.Value));
        public int SlotsWithCellReferenceCount => slots.Count(value => value.CellReference != null &&
            value.CellReference.IsComplete);
        public int SlotsWithProvenanceCount => slots.Count(value => value.Provenance != null &&
            value.Provenance.IsComplete);
        public int SlotsPreservingSourceLayerIdentityCount => slots.Count(value =>
            value.Provenance != null && Enum.IsDefined(typeof(FinalCanvasLayerKind),
                value.Provenance.SourceLayerKind) &&
            !string.IsNullOrEmpty(value.Provenance.SourceLayerToken));
        public int SlotsPreservingSocketSignatureTraversalIdentityCount => slots.Count(value =>
            value.Provenance != null && !string.IsNullOrEmpty(value.Provenance.SourceSignatureIdentity) &&
            !string.IsNullOrEmpty(value.Provenance.SourceTraversalIdentity));
        public int SlotsWithSocketBandIdentityCount => slots.Count(value => value.Provenance != null &&
            !string.IsNullOrEmpty(value.Provenance.SourceSocketIdentity));
        public int CompatibleMultiMarkerCellCount => slots.GroupBy(value =>
            value.CellReference.StableToken).Count(group => group.Select(value => value.Kind)
                .Distinct().Count() > 1);
        public int RequiredMarkerOwnerFamilyCount => RequiredKinds.Length;
        public int CoveredRequiredMarkerOwnerFamilyCount => RequiredKinds.Count(kind =>
            slots.Any(value => value.Kind == kind));
        public int MissingRequiredMarkerOwnerFamilyCount => RequiredMarkerOwnerFamilyCount -
            CoveredRequiredMarkerOwnerFamilyCount;
        public int DuplicateSlotIdCount => slots.GroupBy(value => value.Id.Value,
            StringComparer.Ordinal).Sum(group => Math.Max(0, group.Count() - 1));
        public int DuplicateOwnerKindSourceKeyCount => slots.GroupBy(value => string.Join("|", new[]
            {
                value.CellReference.StableToken, value.Kind.ToString(), value.Owner.ToString(),
                value.SourceKey,
            }), StringComparer.Ordinal).Sum(group => Math.Max(0, group.Count() - 1));
        public int OrphanMarkerRecordCount => 0;
        public int MissingProvenanceCount => slots.Count(value => value.Provenance == null ||
            !value.Provenance.IsComplete);
        public int StableSpawnIdCount => 0;
        public int RuntimeObjectSpawnCount => 0;
        public int CsvGeneratedFileCount => 0;
        public int JsonGeneratedFileCount => 0;
        public int TilemapBakeCount => 0;
        public int TilemapMutationCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int GameObjectMutationCount => 0;
        public int SourceSliceMutationCount => 0;
        public int ProductionSeedApprovalCount => 0;
    }

    public enum MarkerSlotProjectionFailureCode
    {
        MissingSourceSliceSet = 1,
        InvalidSourceSliceSet = 2,
        MissingProjection = 3,
        MissingCellReference = 4,
        OrphanSlice = 5,
        OrphanCell = 6,
        MissingLayer = 7,
        MissingProvenance = 8,
        SourceLayerMismatch = 9,
        SourceMappingMismatch = 10,
        DuplicateOwnerKindSourceKey = 11,
        DuplicateSlotId = 12,
        MissingRequiredOwnerFamily = 13,
        InvalidDigest = 14,
    }

    public sealed class MarkerSlotProjectionFailure :
        IEquatable<MarkerSlotProjectionFailure>, IComparable<MarkerSlotProjectionFailure>
    {
        public MarkerSlotProjectionFailure(
            MarkerSlotProjectionFailureCode code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public MarkerSlotProjectionFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public string StableToken => Code + "|" + Subject + "|" + Reason;
        public int CompareTo(MarkerSlotProjectionFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0
                ? comparison : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
        public bool Equals(MarkerSlotProjectionFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as MarkerSlotProjectionFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class MarkerSlotProjectionResult
    {
        private readonly ReadOnlyCollection<MarkerSlotProjectionFailure> failures;

        internal MarkerSlotProjectionResult(
            GeneratedMicroChunkSliceSet sourceSliceSet,
            GeneratedMicroChunkMarkerSlotSet slotSet,
            IEnumerable<MarkerSlotProjectionFailure> sourceFailures,
            string inputDigest)
        {
            SourceSliceSet = sourceSliceSet;
            SlotSet = slotSet;
            failures = new ReadOnlyCollection<MarkerSlotProjectionFailure>((sourceFailures ??
                Array.Empty<MarkerSlotProjectionFailure>()).OrderBy(value => value).ToArray());
            InputDigest = inputDigest ?? string.Empty;
        }

        public bool Success => SlotSet != null && failures.Count == 0;
        public GeneratedMicroChunkSliceSet SourceSliceSet { get; }
        public GeneratedMicroChunkMarkerSlotSet SlotSet { get; }
        public IReadOnlyList<MarkerSlotProjectionFailure> Failures => failures;
        public string InputDigest { get; }
        public string OutputDigest => SlotSet == null ? string.Empty : SlotSet.OutputDigest;
    }

    public static class MarkerSlotProjectionDigest
    {
        public static string ComputeInput(
            GeneratedMicroChunkSliceSet sourceSliceSet,
            IEnumerable<GeneratedMarkerSlotProjection> sourceProjections,
            int nullProjectionCount)
        {
            if (sourceSliceSet == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedMicroChunkMarkerSlotSet.PolicyVersion,
                "SOURCE|" + sourceSliceSet.InputDigest + "|" + sourceSliceSet.OutputDigest,
                "SOURCE_COUNTS|" + string.Join("|", new[]
                {
                    Number(sourceSliceSet.SliceCount), Number(sourceSliceSet.TotalCellCount),
                    Number(sourceSliceSet.TotalLayerRecordCount), Number(nullProjectionCount),
                }),
            };
            lines.AddRange((sourceProjections ?? Array.Empty<GeneratedMarkerSlotProjection>())
                .Where(value => value != null).OrderBy(value => value)
                .Select(value => value.StableToken));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeOutput(GeneratedMicroChunkMarkerSlotSet set)
        {
            if (set == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedMicroChunkMarkerSlotSet.PolicyVersion,
                "INPUT|" + set.InputDigest,
                "SOURCE|" + set.SourceSliceSetId + "|" + set.SourceSliceSetDigest,
                "COUNTS|" + string.Join("|", new[]
                {
                    Number(set.SourceSliceCount), Number(set.SourceCellCount),
                    Number(set.SourceLayerRecordCount), Number(set.MarkerLayerRecordsScanned),
                    Number(set.MarkerLayerRecordsConsumed), Number(set.SlotCount),
                    Number(set.CompatibleMultiMarkerCellCount),
                    Number(set.CoveredRequiredMarkerOwnerFamilyCount),
                }),
                "VALIDATION|" + string.Join("|", new[]
                {
                    Number(set.DuplicateSlotIdCount),
                    Number(set.DuplicateOwnerKindSourceKeyCount),
                    Number(set.OrphanMarkerRecordCount), Number(set.MissingProvenanceCount),
                }),
                "DOWNSTREAM|" + GeneratedMicroChunkMarkerSlotSet.DownstreamOwner + "|" +
                    (GeneratedMicroChunkMarkerSlotSet.OpensDownstreamTask ? "1" : "0"),
            };
            lines.AddRange(set.SourceSliceIds.OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => "SLICE_ID|" + value));
            lines.AddRange(set.Slots.OrderBy(value => value).Select(value => value.StableToken));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string HashCanonicalText(string text)
        {
            var canonical = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(canonical))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        public static bool IsLowerHexSha256(string value) => value != null && value.Length == 64 &&
            value.All(character => (character >= '0' && character <= '9') ||
                                   (character >= 'a' && character <= 'f'));
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
