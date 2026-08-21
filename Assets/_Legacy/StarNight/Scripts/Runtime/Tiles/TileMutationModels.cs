#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Grid;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Tiles
{
    public enum TileMutationKind
    {
        Destroy,
        Set
    }

    public enum TileMutationRejection
    {
        None,
        Superseded,
        OutOfBounds,
        NoTile,
        UndefinedTile,
        Indestructible,
        ProtectedExit,
        PlayerOverlap,
        Occupied,
        ExitBlocked,
        MissingReplacement
    }

    public sealed class TileMutationRequest
    {
        public TileMutationRequest(
            long sequence,
            GridPos cell,
            TileMutationKind kind,
            TileBreakMethod method,
            TileBase replacement,
            Object requester)
        {
            Sequence = sequence;
            Cell = cell;
            Kind = kind;
            Method = method;
            Replacement = replacement;
            Requester = requester;
        }

        public long Sequence { get; }
        public GridPos Cell { get; }
        public TileMutationKind Kind { get; }
        public TileBreakMethod Method { get; }
        public TileBase Replacement { get; }
        public Object Requester { get; }
    }

    public sealed class TileMutationRecord
    {
        public TileMutationRecord(
            TileMutationRequest request,
            TileBase previous,
            TileBase next,
            TileMutationRejection rejection)
        {
            Request = request;
            Previous = previous;
            Next = next;
            Rejection = rejection;
        }

        public TileMutationRequest Request { get; }
        public TileBase Previous { get; }
        public TileBase Next { get; }
        public TileMutationRejection Rejection { get; }
        public bool Committed => Rejection == TileMutationRejection.None;
    }

    public sealed class TileMutationBatchReport
    {
        private static readonly TileMutationBatchReport empty =
            new TileMutationBatchReport(0, new TileMutationRecord[0], 0);

        public TileMutationBatchReport(
            int tick,
            IReadOnlyList<TileMutationRecord> records,
            int pathVersion)
        {
            Tick = tick;
            Records = records;
            PathVersion = pathVersion;

            int committed = 0;
            for (int index = 0; index < records.Count; index++)
            {
                if (records[index].Committed)
                {
                    committed++;
                }
            }

            CommittedCount = committed;
            RejectedCount = records.Count - committed;
        }

        public static TileMutationBatchReport Empty => empty;

        public int Tick { get; }
        public IReadOnlyList<TileMutationRecord> Records { get; }
        public int PathVersion { get; }
        public int CommittedCount { get; }
        public int RejectedCount { get; }
    }
}

#endif
