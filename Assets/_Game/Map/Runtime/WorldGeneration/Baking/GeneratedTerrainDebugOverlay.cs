using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class GeneratedTerrainOverlayCell
    {
        internal GeneratedTerrainOverlayCell(GeneratedTerrainCellRow source, int slotCount)
        {
            SliceId = source.SliceId;
            ChunkIndex = source.ChunkIndex;
            LocalX = source.LocalX;
            LocalY = source.LocalY;
            SectorX = source.SectorX;
            SectorY = source.SectorY;
            IsPassable = source.IsPassable;
            IsProtected = source.IsProtected;
            IsBlocked = source.IsBlocked;
            Protection = source.Protection;
            WitnessCount = source.WitnessCount;
            SlotCount = slotCount;
            LayerDigest = source.LayerDigest;
            WitnessDigest = source.WitnessDigest;
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
        public int WitnessCount { get; }
        public int SlotCount { get; }
        public string LayerDigest { get; }
        public string WitnessDigest { get; }
    }

    public sealed class GeneratedTerrainOverlayLegend
    {
        internal GeneratedTerrainOverlayLegend(IEnumerable<GeneratedTerrainOverlayCell> source)
        {
            var cells = (source ?? Array.Empty<GeneratedTerrainOverlayCell>()).ToArray();
            CellCount = cells.Length;
            PassableCellCount = cells.Count(value => value.IsPassable);
            BlockedCellCount = cells.Count(value => !value.IsPassable || value.IsBlocked);
            ProtectedCellCount = cells.Count(value => value.IsProtected);
            WitnessCellCount = cells.Count(value => value.WitnessCount > 0);
            SlotCellCount = cells.Count(value => value.SlotCount > 0);
        }

        public int CellCount { get; }
        public int PassableCellCount { get; }
        public int BlockedCellCount { get; }
        public int ProtectedCellCount { get; }
        public int WitnessCellCount { get; }
        public int SlotCellCount { get; }
    }

    public sealed class GeneratedTerrainSliceOverlay
    {
        private readonly ReadOnlyCollection<GeneratedTerrainOverlayCell> cells;
        private readonly ReadOnlyCollection<GeneratedTerrainSlotRow> slots;
        private readonly ReadOnlyCollection<GeneratedTerrainSocketRow> sockets;

        internal GeneratedTerrainSliceOverlay(
            GeneratedTerrainSliceRow source,
            IEnumerable<GeneratedTerrainOverlayCell> sourceCells,
            IEnumerable<GeneratedTerrainSlotRow> sourceSlots,
            IEnumerable<GeneratedTerrainSocketRow> sourceSockets)
        {
            SliceId = source.SliceId;
            ChunkIndex = source.ChunkIndex;
            ChunkX = source.ChunkX;
            ChunkY = source.ChunkY;
            SectorOriginX = source.SectorOriginX;
            SectorOriginY = source.SectorOriginY;
            cells = new ReadOnlyCollection<GeneratedTerrainOverlayCell>((sourceCells ??
                Array.Empty<GeneratedTerrainOverlayCell>()).OrderBy(value => value.LocalY)
                .ThenBy(value => value.LocalX).ToArray());
            slots = new ReadOnlyCollection<GeneratedTerrainSlotRow>((sourceSlots ??
                Array.Empty<GeneratedTerrainSlotRow>()).OrderBy(value => value.SlotId,
                    StringComparer.Ordinal).ToArray());
            sockets = new ReadOnlyCollection<GeneratedTerrainSocketRow>((sourceSockets ??
                Array.Empty<GeneratedTerrainSocketRow>()).OrderBy(value => value.Side,
                    StringComparer.Ordinal).ToArray());
            Legend = new GeneratedTerrainOverlayLegend(cells);
            TextGrid = GeneratedTerrainDebugOverlay.RenderGrid(cells,
                GeneratedMicroChunkSliceSet.MicroChunkWidth,
                GeneratedMicroChunkSliceSet.MicroChunkHeight, true);
        }

        public string SliceId { get; }
        public int ChunkIndex { get; }
        public int ChunkX { get; }
        public int ChunkY { get; }
        public int SectorOriginX { get; }
        public int SectorOriginY { get; }
        public int Width => GeneratedMicroChunkSliceSet.MicroChunkWidth;
        public int Height => GeneratedMicroChunkSliceSet.MicroChunkHeight;
        public IReadOnlyList<GeneratedTerrainOverlayCell> Cells => cells;
        public IReadOnlyList<GeneratedTerrainSlotRow> Slots => slots;
        public IReadOnlyList<GeneratedTerrainSocketRow> Sockets => sockets;
        public GeneratedTerrainOverlayLegend Legend { get; }
        public string TextGrid { get; }
    }

    public sealed class GeneratedTerrainCanvasOverlay
    {
        private readonly ReadOnlyCollection<GeneratedTerrainOverlayCell> cells;
        private readonly ReadOnlyCollection<GeneratedTerrainSliceOverlay> slices;
        private readonly ReadOnlyCollection<GeneratedTerrainSlotRow> slots;
        private readonly ReadOnlyCollection<GeneratedTerrainSocketRow> sockets;

        internal GeneratedTerrainCanvasOverlay(
            IEnumerable<GeneratedTerrainOverlayCell> sourceCells,
            IEnumerable<GeneratedTerrainSliceOverlay> sourceSlices,
            IEnumerable<GeneratedTerrainSlotRow> sourceSlots,
            IEnumerable<GeneratedTerrainSocketRow> sourceSockets)
        {
            cells = new ReadOnlyCollection<GeneratedTerrainOverlayCell>((sourceCells ??
                Array.Empty<GeneratedTerrainOverlayCell>()).OrderBy(value => value.SectorY)
                .ThenBy(value => value.SectorX).ToArray());
            slices = new ReadOnlyCollection<GeneratedTerrainSliceOverlay>((sourceSlices ??
                Array.Empty<GeneratedTerrainSliceOverlay>()).OrderBy(value => value.ChunkIndex).ToArray());
            slots = new ReadOnlyCollection<GeneratedTerrainSlotRow>((sourceSlots ??
                Array.Empty<GeneratedTerrainSlotRow>()).OrderBy(value => value.SlotId,
                    StringComparer.Ordinal).ToArray());
            sockets = new ReadOnlyCollection<GeneratedTerrainSocketRow>((sourceSockets ??
                Array.Empty<GeneratedTerrainSocketRow>()).OrderBy(value => value.ChunkIndex)
                .ThenBy(value => value.Side, StringComparer.Ordinal).ToArray());
            Legend = new GeneratedTerrainOverlayLegend(cells);
            TextGrid = GeneratedTerrainDebugOverlay.RenderGrid(cells,
                GeneratedMicroChunkSliceSet.SectorWidth,
                GeneratedMicroChunkSliceSet.SectorHeight, false);
        }

        public int Width => GeneratedMicroChunkSliceSet.SectorWidth;
        public int Height => GeneratedMicroChunkSliceSet.SectorHeight;
        public IReadOnlyList<GeneratedTerrainOverlayCell> Cells => cells;
        public IReadOnlyList<GeneratedTerrainSliceOverlay> Slices => slices;
        public IReadOnlyList<GeneratedTerrainSlotRow> Slots => slots;
        public IReadOnlyList<GeneratedTerrainSocketRow> Sockets => sockets;
        public GeneratedTerrainOverlayLegend Legend { get; }
        public string TextGrid { get; }
    }

    public sealed class GeneratedTerrainOverlayResult
    {
        private readonly ReadOnlyCollection<GeneratedTerrainExportFailure> failures;

        internal GeneratedTerrainOverlayResult(
            GeneratedTerrainCanvasOverlay overlay,
            IEnumerable<GeneratedTerrainExportFailure> sourceFailures)
        {
            Canvas = overlay;
            failures = new ReadOnlyCollection<GeneratedTerrainExportFailure>((sourceFailures ??
                Array.Empty<GeneratedTerrainExportFailure>()).OrderBy(value => value).ToArray());
        }

        public bool Success => Canvas != null && failures.Count == 0;
        public GeneratedTerrainCanvasOverlay Canvas { get; }
        public IReadOnlyList<GeneratedTerrainExportFailure> Failures => failures;
    }

    public static class GeneratedTerrainDebugOverlay
    {
        public static GeneratedTerrainOverlayResult Build(GeneratedTerrainExportPacket packet)
        {
            if (packet == null || packet.CellRows.Count != GeneratedMicroChunkSliceSet.SectorCellCount ||
                packet.SliceRows.Count != GeneratedMicroChunkSliceSet.ChunkCount ||
                packet.SocketRows.Count != GeneratedMicroChunkSliceSet.ChunkCount * 4)
                return new GeneratedTerrainOverlayResult(null, new[]
                {
                    new GeneratedTerrainExportFailure(
                        GeneratedTerrainExportFailureCode.IncompleteSourcePacket,
                        "packet", "A complete generated terrain export packet is required."),
                });

            var slotCounts = packet.SlotRows.GroupBy(value => value.SliceId + "|" +
                value.LocalX + "|" + value.LocalY, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.Count(), StringComparer.Ordinal);
            var cells = packet.CellRows.Select(value =>
            {
                int count;
                slotCounts.TryGetValue(value.SliceId + "|" + value.LocalX + "|" + value.LocalY,
                    out count);
                return new GeneratedTerrainOverlayCell(value, count);
            }).ToArray();
            var slices = packet.SliceRows.OrderBy(value => value.ChunkIndex).Select(value =>
                new GeneratedTerrainSliceOverlay(value,
                    cells.Where(cell => cell.SliceId == value.SliceId),
                    packet.SlotRows.Where(slot => slot.SliceId == value.SliceId),
                    packet.SocketRows.Where(socket => socket.SliceId == value.SliceId))).ToArray();
            var canvas = new GeneratedTerrainCanvasOverlay(cells, slices,
                packet.SlotRows, packet.SocketRows);
            return new GeneratedTerrainOverlayResult(canvas,
                Array.Empty<GeneratedTerrainExportFailure>());
        }

        internal static string RenderGrid(
            IEnumerable<GeneratedTerrainOverlayCell> sourceCells,
            int width,
            int height,
            bool localCoordinates)
        {
            var cells = (sourceCells ?? Array.Empty<GeneratedTerrainOverlayCell>()).ToDictionary(
                value => (localCoordinates ? value.LocalX : value.SectorX) + "|" +
                    (localCoordinates ? value.LocalY : value.SectorY), StringComparer.Ordinal);
            var result = new StringBuilder();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    GeneratedTerrainOverlayCell cell;
                    if (!cells.TryGetValue(x + "|" + y, out cell)) result.Append('?');
                    else if (cell.WitnessCount > 0) result.Append('R');
                    else if (cell.SlotCount > 0) result.Append('S');
                    else if (cell.IsProtected) result.Append('P');
                    else result.Append(cell.IsPassable && !cell.IsBlocked ? '.' : '#');
                }
                result.Append('\n');
            }
            return result.ToString();
        }
    }
}
