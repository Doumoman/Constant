using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum GeneratedCollisionMaskKind
    {
        Solid = 1,
        Platform = 2,
        Hazard = 3,
        Protection = 4,
        NonCollidingDebug = 5,
    }

    public sealed class GeneratedColliderCellMask : IComparable<GeneratedColliderCellMask>
    {
        private readonly ReadOnlyCollection<bool> occupancy;
        private readonly ReadOnlyCollection<int> occupiedIndices;

        internal GeneratedColliderCellMask(
            GeneratedCollisionMaskKind kind,
            int width,
            int height,
            IEnumerable<int> sourceOccupiedIndices,
            int duplicateSourceCellCount,
            int outOfBoundsSourceCellCount)
        {
            Kind = kind;
            Width = width;
            Height = height;
            DuplicateSourceCellCount = duplicateSourceCellCount;
            OutOfBoundsSourceCellCount = outOfBoundsSourceCellCount;
            var cellCount = Math.Max(0, width * height);
            var ordered = (sourceOccupiedIndices ?? Array.Empty<int>())
                .Where(value => value >= 0 && value < cellCount).Distinct().OrderBy(value => value).ToArray();
            occupiedIndices = new ReadOnlyCollection<int>(ordered);
            var values = new bool[cellCount];
            foreach (var index in ordered) values[index] = true;
            occupancy = new ReadOnlyCollection<bool>(values);
            StableToken = string.Join("|", new[]
            {
                "COLLIDER_MASK", Number((int)Kind), Kind.ToString().ToUpperInvariant(),
                Number(Width), Number(Height), Number(CellCount), Number(OccupiedCellCount),
                Number(DuplicateSourceCellCount), Number(OutOfBoundsSourceCellCount),
                string.Join(",", occupiedIndices.Select(Number)),
            });
        }

        public GeneratedCollisionMaskKind Kind { get; }
        public int Width { get; }
        public int Height { get; }
        public int CellCount => occupancy.Count;
        public IReadOnlyList<bool> Occupancy => occupancy;
        public IReadOnlyList<int> OccupiedIndices => occupiedIndices;
        public int OccupiedCellCount => occupiedIndices.Count;
        public int DuplicateSourceCellCount { get; }
        public int OutOfBoundsSourceCellCount { get; }
        public bool IsAdapterCollisionMask => Kind != GeneratedCollisionMaskKind.NonCollidingDebug;
        public string StableToken { get; }
        public bool IsOccupied(int sectorLocalIndex) => sectorLocalIndex >= 0 &&
            sectorLocalIndex < occupancy.Count && occupancy[sectorLocalIndex];
        public int CompareTo(GeneratedColliderCellMask other) => other == null
            ? -1 : Kind.CompareTo(other.Kind);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedColliderSpan : IComparable<GeneratedColliderSpan>
    {
        public GeneratedColliderSpan(
            GeneratedCollisionMaskKind maskKind,
            int startX,
            int y,
            int width,
            int sectorWidth)
        {
            MaskKind = maskKind;
            StartX = startX;
            Y = y;
            Width = width;
            SectorWidth = sectorWidth;
            StableToken = string.Join("|", new[]
            {
                "COLLIDER_SPAN", Number((int)MaskKind), Number(StartX), Number(Y),
                Number(Width), Number(SectorWidth),
            });
        }

        public GeneratedCollisionMaskKind MaskKind { get; }
        public int StartX { get; }
        public int EndXExclusive => StartX + Width;
        public int Y { get; }
        public int Width { get; }
        public int SectorWidth { get; }
        public int CellCount => Math.Max(0, Width);
        public IEnumerable<int> CellIndices
        {
            get
            {
                for (var x = StartX; x < EndXExclusive; x++)
                    yield return Y * SectorWidth + x;
            }
        }
        public bool IsInBounds(int width, int height) => Width > 0 && StartX >= 0 &&
            EndXExclusive <= width && Y >= 0 && Y < height && SectorWidth == width;
        public string StableToken { get; }
        public int CompareTo(GeneratedColliderSpan other)
        {
            if (other == null) return -1;
            var comparison = MaskKind.CompareTo(other.MaskKind);
            if (comparison != 0) return comparison;
            comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : StartX.CompareTo(other.StartX);
        }
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedColliderAdapterCommand :
        IComparable<GeneratedColliderAdapterCommand>
    {
        internal GeneratedColliderAdapterCommand(GeneratedColliderSpan span, int ordinal)
        {
            Span = span ?? throw new ArgumentNullException(nameof(span));
            Ordinal = ordinal;
            StableToken = "COLLIDER_ADAPTER_COMMAND|" + Number(Ordinal) + "|" + span.StableToken;
        }

        public GeneratedColliderSpan Span { get; }
        public int Ordinal { get; }
        public bool WasExecuted => false;
        public string StableToken { get; }
        public int CompareTo(GeneratedColliderAdapterCommand other) => other == null
            ? -1 : Ordinal.CompareTo(other.Ordinal);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedColliderRebuildRequest
    {
        private readonly ReadOnlyCollection<GeneratedTilemapCellBakeRecord> sourceRecords;

        public GeneratedColliderRebuildRequest(
            GeneratedTilemapBakePlan bakePlan,
            int mutationRevision = 0,
            string dirtyReason = "INITIAL_BAKE",
            IEnumerable<GeneratedTilemapCellBakeRecord> records = null)
        {
            BakePlan = bakePlan;
            MutationRevision = mutationRevision;
            DirtyReason = dirtyReason ?? string.Empty;
            var raw = (records ?? (bakePlan == null
                ? Array.Empty<GeneratedTilemapCellBakeRecord>()
                : bakePlan.LayerBuffers.SelectMany(value => value.Records))).ToArray();
            NullRecordCount = raw.Count(value => value == null);
            sourceRecords = new ReadOnlyCollection<GeneratedTilemapCellBakeRecord>(raw
                .Where(value => value != null).OrderBy(value => value).ToArray());
            CanonicalDigest = GeneratedColliderRebuildDigest.ComputeInput(this);
        }

        public GeneratedTilemapBakePlan BakePlan { get; }
        public int MutationRevision { get; }
        public string DirtyReason { get; }
        public IReadOnlyList<GeneratedTilemapCellBakeRecord> SourceRecords => sourceRecords;
        public int NullRecordCount { get; }
        public string CanonicalDigest { get; }
    }

    public enum GeneratedColliderRebuildFailureCode
    {
        MissingRequest = 1,
        MissingBakePlan = 2,
        StaleBakePlan = 3,
        InvalidMutationRevision = 4,
        InvalidDirtyReason = 5,
        InvalidSourceRecord = 6,
        DuplicateMaskCell = 7,
        OutOfBoundsMaskCell = 8,
        MaskSpanMismatch = 9,
        InvalidDigest = 10,
    }

    public sealed class GeneratedColliderRebuildFailure :
        IEquatable<GeneratedColliderRebuildFailure>, IComparable<GeneratedColliderRebuildFailure>
    {
        public GeneratedColliderRebuildFailure(
            GeneratedColliderRebuildFailureCode code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public GeneratedColliderRebuildFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public string StableToken => Code + "|" + Subject + "|" + Reason;
        public int CompareTo(GeneratedColliderRebuildFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison :
                string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
        public bool Equals(GeneratedColliderRebuildFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as GeneratedColliderRebuildFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedColliderRebuildPlan
    {
        private readonly ReadOnlyCollection<GeneratedColliderCellMask> masks;
        private readonly ReadOnlyCollection<GeneratedColliderSpan> spans;
        private readonly ReadOnlyCollection<GeneratedColliderAdapterCommand> commands;

        internal GeneratedColliderRebuildPlan(
            GeneratedColliderRebuildRequest request,
            IEnumerable<GeneratedColliderCellMask> sourceMasks,
            IEnumerable<GeneratedColliderSpan> sourceSpans,
            IEnumerable<GeneratedColliderAdapterCommand> sourceCommands,
            int nonCollidingRecordCount)
        {
            Request = request;
            masks = new ReadOnlyCollection<GeneratedColliderCellMask>((sourceMasks ??
                Array.Empty<GeneratedColliderCellMask>()).OrderBy(value => value).ToArray());
            spans = new ReadOnlyCollection<GeneratedColliderSpan>((sourceSpans ??
                Array.Empty<GeneratedColliderSpan>()).OrderBy(value => value).ToArray());
            commands = new ReadOnlyCollection<GeneratedColliderAdapterCommand>((sourceCommands ??
                Array.Empty<GeneratedColliderAdapterCommand>()).OrderBy(value => value).ToArray());
            NonCollidingRecordCount = nonCollidingRecordCount;
            OutputDigest = GeneratedColliderRebuildDigest.ComputeOutput(this);
        }

        public const string CollisionPolicyVersion = "MAP17_03_LOGICAL_COLLIDER_POLICY_V1";
        public GeneratedColliderRebuildRequest Request { get; }
        public GeneratedTilemapBakePlan SourceBakePlan => Request.BakePlan;
        public IReadOnlyList<GeneratedColliderCellMask> Masks => masks;
        public IReadOnlyList<GeneratedColliderSpan> Spans => spans;
        public IReadOnlyList<GeneratedColliderAdapterCommand> AdapterCommands => commands;
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int MutationRevision => Request.MutationRevision;
        public string DirtyReason => Request.DirtyReason;
        public int SourceLayerCount => SourceBakePlan.LayerCount;
        public int SourceLayerRecordCount => Request.SourceRecords.Count;
        public int SourceSectorCellCoverageCount => Request.SourceRecords
            .Select(value => value.SectorLocalIndex).Distinct().Count();
        public int SourceSeamPairCount => SourceBakePlan.SeamReport.Exposures.Count;
        public int SourceSocketReferenceCount => SourceBakePlan.SocketReferenceCount;
        public int SourceMarkerSlotCount => SourceBakePlan.SlotReferenceCount;
        public int MaskKindCount => masks.Count;
        public int SolidMaskCellCount => Count(GeneratedCollisionMaskKind.Solid);
        public int PlatformMaskCellCount => Count(GeneratedCollisionMaskKind.Platform);
        public int HazardMaskCellCount => Count(GeneratedCollisionMaskKind.Hazard);
        public int ProtectionMaskCellCount => Count(GeneratedCollisionMaskKind.Protection);
        public int DebugNonCollidingCellCount => Count(GeneratedCollisionMaskKind.NonCollidingDebug);
        public int NonCollidingRecordCount { get; }
        public int DuplicateMaskCellCount => masks.Sum(value => value.DuplicateSourceCellCount);
        public int OutOfBoundsMaskCellCount => masks.Sum(value => value.OutOfBoundsSourceCellCount);
        public int SpanCount => spans.Count;
        public int SpanCellCount => spans.Sum(value => value.CellCount);
        public int SpanOutOfBoundsCellCount => spans.Where(value => !value.IsInBounds(
                GeneratedTerrainGeometrySnapshot.CanonicalSectorWidth,
                GeneratedTerrainGeometrySnapshot.CanonicalSectorHeight))
            .Sum(value => value.CellCount);
        public bool SpanCellsExactlyMatchMasks => masks.All(mask =>
        {
            var fromSpans = spans.Where(value => value.MaskKind == mask.Kind)
                .SelectMany(value => value.CellIndices).OrderBy(value => value).ToArray();
            return fromSpans.SequenceEqual(mask.OccupiedIndices);
        });
        public int AdapterCommandCount => commands.Count;
        public int ExecutedAdapterCommandCount => commands.Count(value => value.WasExecuted);
        public int TilemapComponentWriteCount => 0;
        public int ColliderCreationCount => 0;
        public int RigidbodyCreationCount => 0;
        public int PhysicsQueryCount => 0;
        public int PhysicsSimulationCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int TilemapMutationCount => 0;
        public int GameObjectInstantiationCount => 0;
        public int PrefabInstantiationCount => 0;
        public int GeneratedCsvCommitCount => 0;
        public int GeneratedAssetCommitCount => 0;
        public int StableSpawnIdCount => 0;
        public int RuntimeObjectSpawnCount => 0;
        public int ProductionSeedApprovalCount => 0;
        private int Count(GeneratedCollisionMaskKind kind) =>
            masks.Single(value => value.Kind == kind).OccupiedCellCount;
    }

    public sealed class GeneratedColliderRebuildResult
    {
        private readonly ReadOnlyCollection<GeneratedColliderRebuildFailure> failures;

        internal GeneratedColliderRebuildResult(
            GeneratedColliderRebuildRequest request,
            GeneratedColliderRebuildPlan plan,
            IEnumerable<GeneratedColliderRebuildFailure> sourceFailures)
        {
            Request = request;
            Plan = plan;
            failures = new ReadOnlyCollection<GeneratedColliderRebuildFailure>((sourceFailures ??
                Array.Empty<GeneratedColliderRebuildFailure>()).Distinct()
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0;
        public GeneratedColliderRebuildRequest Request { get; }
        public GeneratedColliderRebuildPlan Plan { get; }
        public IReadOnlyList<GeneratedColliderRebuildFailure> Failures => failures;
        public string InputDigest => Plan == null
            ? (Request == null ? string.Empty : Request.CanonicalDigest) : Plan.InputDigest;
        public string OutputDigest => Plan == null ? string.Empty : Plan.OutputDigest;
    }

    public static class GeneratedColliderRebuildDigest
    {
        public static string ComputeInput(GeneratedColliderRebuildRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedColliderRebuildPlan.CollisionPolicyVersion,
                "BAKE|" + (request.BakePlan == null ? string.Empty : request.BakePlan.OutputDigest),
                "SEAM|" + (request.BakePlan == null || request.BakePlan.SeamReport == null
                    ? string.Empty : request.BakePlan.SeamReport.OutputDigest),
                "REVISION|" + Number(request.MutationRevision),
                "DIRTY_REASON|" + request.DirtyReason,
                "NULLS|" + Number(request.NullRecordCount),
            };
            lines.AddRange(request.SourceRecords.OrderBy(value => value)
                .Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static string ComputeOutput(GeneratedColliderRebuildPlan plan)
        {
            if (plan == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedColliderRebuildPlan.CollisionPolicyVersion,
                "INPUT|" + plan.InputDigest,
                "SOURCE|" + Number(plan.SourceLayerCount) + "|" +
                    Number(plan.SourceLayerRecordCount) + "|" +
                    Number(plan.SourceSectorCellCoverageCount) + "|" +
                    Number(plan.SourceSeamPairCount) + "|" +
                    Number(plan.SourceSocketReferenceCount) + "|" +
                    Number(plan.SourceMarkerSlotCount),
                "MASKS|" + Number(plan.MaskKindCount) + "|" +
                    Number(plan.SolidMaskCellCount) + "|" + Number(plan.PlatformMaskCellCount) + "|" +
                    Number(plan.HazardMaskCellCount) + "|" + Number(plan.ProtectionMaskCellCount) + "|" +
                    Number(plan.DebugNonCollidingCellCount) + "|" +
                    Number(plan.NonCollidingRecordCount) + "|" +
                    Number(plan.DuplicateMaskCellCount) + "|" + Number(plan.OutOfBoundsMaskCellCount),
                "SPANS|" + Number(plan.SpanCount) + "|" + Number(plan.SpanCellCount) + "|" +
                    Number(plan.SpanOutOfBoundsCellCount) + "|" +
                    (plan.SpanCellsExactlyMatchMasks ? "1" : "0"),
                "COMMANDS|" + Number(plan.AdapterCommandCount) + "|0",
                "MUTATIONS|0|0|0|0|0|0|0|0|0|0|0|0|0",
            };
            lines.AddRange(plan.Masks.OrderBy(value => value).Select(value => value.StableToken));
            lines.AddRange(plan.Spans.OrderBy(value => value).Select(value => value.StableToken));
            lines.AddRange(plan.AdapterCommands.OrderBy(value => value)
                .Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static bool IsLowerHexSha256(string value) =>
            BakingCanonicalDigest.IsLowerHexSha256(value);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
