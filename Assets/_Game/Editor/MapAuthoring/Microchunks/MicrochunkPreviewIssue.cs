using System;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public enum MicrochunkPreviewIssueSeverity
    {
        Error,
        Warning,
        Info
    }

    public enum MicrochunkPreviewIssueCategory
    {
        Transform,
        TileLayer,
        Coverage,
        SocketEdge,
        ObjectSlot,
        Reachability,
        Import,
        Export
    }

    public sealed class MicrochunkPreviewIssue : IComparable<MicrochunkPreviewIssue>
    {
        public MicrochunkPreviewIssueSeverity Severity { get; }
        public int Order { get; }
        public MicrochunkPreviewIssueCategory Category { get; }
        public string Code { get; }
        public string Message { get; }
        public string SelectedMicrochunkId { get; }
        public MicrochunkTransform? Transform { get; }
        public MicrochunkLocalCoord? LocalCoordinate { get; }
        public int SourceOrder { get; }
        public bool IsError => Severity == MicrochunkPreviewIssueSeverity.Error;

        public MicrochunkPreviewIssue(
            MicrochunkPreviewIssueSeverity severity,
            int order,
            MicrochunkPreviewIssueCategory category,
            string code,
            string message,
            string selectedMicrochunkId,
            MicrochunkTransform? transform = null,
            MicrochunkLocalCoord? localCoordinate = null,
            int sourceOrder = 0)
        {
            if (!Enum.IsDefined(typeof(MicrochunkPreviewIssueSeverity), severity))
                throw new ArgumentOutOfRangeException(nameof(severity));
            if (!Enum.IsDefined(typeof(MicrochunkPreviewIssueCategory), category))
                throw new ArgumentOutOfRangeException(nameof(category));
            if (order < 0) throw new ArgumentOutOfRangeException(nameof(order));
            if (sourceOrder < 0) throw new ArgumentOutOfRangeException(nameof(sourceOrder));
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Issue code is required.", nameof(code));
            if (string.IsNullOrWhiteSpace(selectedMicrochunkId))
                throw new ArgumentException("Selected microchunk ID is required.", nameof(selectedMicrochunkId));
            if (transform.HasValue && !MicrochunkPreviewRequest.IsSupportedTransform(transform.Value))
                throw new ArgumentOutOfRangeException(nameof(transform));

            Severity = severity;
            Order = order;
            Category = category;
            Code = code;
            Message = message ?? string.Empty;
            SelectedMicrochunkId = selectedMicrochunkId;
            Transform = transform;
            LocalCoordinate = localCoordinate;
            SourceOrder = sourceOrder;
        }

        public int CompareTo(MicrochunkPreviewIssue other)
        {
            if (other == null) return 1;
            var comparison = Severity.CompareTo(other.Severity);
            if (comparison != 0) return comparison;
            comparison = Order.CompareTo(other.Order);
            if (comparison != 0) return comparison;
            comparison = Category.CompareTo(other.Category);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Code, other.Code, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = NullableTransformOrder(Transform).CompareTo(NullableTransformOrder(other.Transform));
            if (comparison != 0) return comparison;
            comparison = NullableCoordinateOrder(LocalCoordinate).CompareTo(
                NullableCoordinateOrder(other.LocalCoordinate));
            if (comparison != 0) return comparison;
            comparison = SourceOrder.CompareTo(other.SourceOrder);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SelectedMicrochunkId, other.SelectedMicrochunkId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            return string.Compare(Message, other.Message, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            var transform = Transform.HasValue
                ? ":" + MicrochunkTransformUtility.ToTransformToken(Transform.Value)
                : string.Empty;
            var coordinate = LocalCoordinate.HasValue
                ? ":[" + LocalCoordinate.Value.X + "," + LocalCoordinate.Value.Y + "]"
                : string.Empty;
            return Severity + ":" + Category + ":" + Code + transform + coordinate + ": " + Message;
        }

        private static int NullableTransformOrder(MicrochunkTransform? value)
        {
            return value.HasValue ? (int)value.Value : -1;
        }

        private static int NullableCoordinateOrder(MicrochunkLocalCoord? value)
        {
            return value.HasValue ? value.Value.RowMajorIndex : -1;
        }
    }
}
