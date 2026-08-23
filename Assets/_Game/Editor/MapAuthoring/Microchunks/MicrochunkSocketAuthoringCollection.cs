using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkSocketAuthoringCollection
    {
        private readonly List<MicrochunkSocketAuthoringRow> sockets = new List<MicrochunkSocketAuthoringRow>();
        private readonly List<MicrochunkSocketBandAuthoringRow> bands = new List<MicrochunkSocketBandAuthoringRow>();

        public IReadOnlyList<MicrochunkSocketAuthoringRow> Sockets =>
            new ReadOnlyCollection<MicrochunkSocketAuthoringRow>(new List<MicrochunkSocketAuthoringRow>(sockets));

        public IReadOnlyList<MicrochunkSocketBandAuthoringRow> Bands =>
            new ReadOnlyCollection<MicrochunkSocketBandAuthoringRow>(new List<MicrochunkSocketBandAuthoringRow>(bands));

        public void AddSocket(MicrochunkSocketAuthoringRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            RejectDuplicateSocketId(row.SocketId, -1);
            sockets.Add(row);
            sockets.Sort((left, right) => string.Compare(left.SocketId, right.SocketId, StringComparison.Ordinal));
        }

        public void DuplicateSocket(string sourceId, string duplicateId)
        {
            AddSocket(FindSocket(sourceId).Duplicate(duplicateId));
        }

        public bool RemoveSocket(string socketId)
        {
            var index = sockets.FindIndex(row => string.Equals(row.SocketId, socketId, StringComparison.Ordinal));
            if (index < 0) return false;
            sockets.RemoveAt(index);
            return true;
        }

        public void MoveSocket(int sourceIndex, int destinationIndex)
        {
            Move(sockets, sourceIndex, destinationIndex);
        }

        public void ReplaceSocket(int index, MicrochunkSocketAuthoringRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            RequireIndex(index, sockets.Count, nameof(index));
            RejectDuplicateSocketId(row.SocketId, index);
            sockets[index] = row;
        }

        public void AddBand(MicrochunkSocketBandAuthoringRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            RejectDuplicateBandId(row.BandId, -1);
            bands.Add(row);
            bands.Sort((left, right) => string.Compare(left.BandId, right.BandId, StringComparison.Ordinal));
        }

        public void DuplicateBand(string sourceId, string duplicateId)
        {
            AddBand(FindBand(sourceId).Duplicate(duplicateId));
        }

        public bool RemoveBand(string bandId)
        {
            var index = bands.FindIndex(row => string.Equals(row.BandId, bandId, StringComparison.Ordinal));
            if (index < 0) return false;
            bands.RemoveAt(index);
            return true;
        }

        public void MoveBand(int sourceIndex, int destinationIndex)
        {
            Move(bands, sourceIndex, destinationIndex);
        }

        public void ReplaceBand(int index, MicrochunkSocketBandAuthoringRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            RequireIndex(index, bands.Count, nameof(index));
            RejectDuplicateBandId(row.BandId, index);
            bands[index] = row;
        }

        public IReadOnlyDictionary<string, MicrochunkSocketBandDefinition> ProjectBandsById()
        {
            var projected = new SortedDictionary<string, MicrochunkSocketBandDefinition>(StringComparer.Ordinal);
            foreach (var band in bands.OrderBy(value => value.BandId, StringComparer.Ordinal))
            {
                projected.Add(band.BandId, band.ToRuntimeDefinition());
            }
            return new ReadOnlyDictionary<string, MicrochunkSocketBandDefinition>(projected);
        }

        public IReadOnlyList<MicrochunkSocketDefinition> ProjectSockets()
        {
            var bandLookup = bands.ToDictionary(row => row.BandId, StringComparer.Ordinal);
            var projected = new List<MicrochunkSocketDefinition>();
            foreach (var socket in sockets.OrderBy(value => value.SocketId, StringComparer.Ordinal))
            {
                var minimumSafeTiles = bandLookup.TryGetValue(socket.BandId, out var band)
                    ? band.MinimumClearanceTiles
                    : 0;
                projected.Add(socket.ToRuntimeDefinition(minimumSafeTiles));
            }
            return new ReadOnlyCollection<MicrochunkSocketDefinition>(projected);
        }

        private MicrochunkSocketAuthoringRow FindSocket(string socketId)
        {
            var row = sockets.FirstOrDefault(value => string.Equals(value.SocketId, socketId, StringComparison.Ordinal));
            if (row == null) throw new KeyNotFoundException("Socket ID was not found: " + socketId);
            return row;
        }

        private MicrochunkSocketBandAuthoringRow FindBand(string bandId)
        {
            var row = bands.FirstOrDefault(value => string.Equals(value.BandId, bandId, StringComparison.Ordinal));
            if (row == null) throw new KeyNotFoundException("Band ID was not found: " + bandId);
            return row;
        }

        private void RejectDuplicateSocketId(string socketId, int ignoredIndex)
        {
            for (var index = 0; index < sockets.Count; index++)
            {
                if (index != ignoredIndex && string.Equals(sockets[index].SocketId, socketId, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Socket IDs must be unique.", nameof(socketId));
                }
            }
        }

        private void RejectDuplicateBandId(string bandId, int ignoredIndex)
        {
            for (var index = 0; index < bands.Count; index++)
            {
                if (index != ignoredIndex && string.Equals(bands[index].BandId, bandId, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Band IDs must be unique.", nameof(bandId));
                }
            }
        }

        private static void Move<T>(IList<T> rows, int sourceIndex, int destinationIndex)
        {
            RequireIndex(sourceIndex, rows.Count, nameof(sourceIndex));
            RequireIndex(destinationIndex, rows.Count, nameof(destinationIndex));
            if (sourceIndex == destinationIndex) return;
            var value = rows[sourceIndex];
            rows.RemoveAt(sourceIndex);
            rows.Insert(destinationIndex, value);
        }

        private static void RequireIndex(int index, int count, string parameterName)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
