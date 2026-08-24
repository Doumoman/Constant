using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public enum MicrochunkPreviewReachabilityState
    {
        Disabled,
        Unreachable,
        Reachable,
        PathWitness,
        SocketEntry,
        SocketExit,
        BlockedSolid
    }

    public sealed class MicrochunkPreviewCellOverlay
    {
        private readonly IReadOnlyList<string> socketIds;
        private readonly IReadOnlyList<string> objectSlotIds;

        public MicrochunkTransform Transform { get; }
        public MicrochunkLocalCoord Coordinate { get; }
        public MicrochunkTileCell TileCell { get; }
        public IReadOnlyList<string> SocketIds => socketIds;
        public IReadOnlyList<string> ObjectSlotIds => objectSlotIds;
        public MicrochunkPreviewReachabilityState ReachabilityState { get; }
        public bool IsReachable { get; }
        public bool IsPathWitness { get; }
        public bool IsSocketEntry { get; }
        public bool IsSocketExit { get; }
        public bool IsBlockedSolid { get; }

        public MicrochunkPreviewCellOverlay(
            MicrochunkTransform transform,
            MicrochunkLocalCoord coordinate,
            MicrochunkTileCell tileCell,
            IEnumerable<string> socketIds,
            IEnumerable<string> objectSlotIds,
            MicrochunkPreviewReachabilityState reachabilityState,
            bool isReachable,
            bool isPathWitness,
            bool isSocketEntry,
            bool isSocketExit,
            bool isBlockedSolid)
        {
            if (!MicrochunkPreviewRequest.IsSupportedTransform(transform))
                throw new ArgumentOutOfRangeException(nameof(transform));
            if (!Enum.IsDefined(typeof(MicrochunkPreviewReachabilityState), reachabilityState))
                throw new ArgumentOutOfRangeException(nameof(reachabilityState));
            if (tileCell != null && tileCell.Coordinate != coordinate)
                throw new ArgumentException("Tile overlay coordinate must match the preview cell.", nameof(tileCell));

            Transform = transform;
            Coordinate = coordinate;
            TileCell = tileCell;
            this.socketIds = FreezeIds(socketIds, nameof(socketIds));
            this.objectSlotIds = FreezeIds(objectSlotIds, nameof(objectSlotIds));
            ReachabilityState = reachabilityState;
            IsReachable = isReachable;
            IsPathWitness = isPathWitness;
            IsSocketEntry = isSocketEntry;
            IsSocketExit = isSocketExit;
            IsBlockedSolid = isBlockedSolid;
        }

        private static IReadOnlyList<string> FreezeIds(IEnumerable<string> source, string parameterName)
        {
            if (source == null) throw new ArgumentNullException(parameterName);
            var values = source.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList();
            if (values.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Overlay IDs must be canonical non-blank tokens.", parameterName);
            return new ReadOnlyCollection<string>(values);
        }
    }
}
