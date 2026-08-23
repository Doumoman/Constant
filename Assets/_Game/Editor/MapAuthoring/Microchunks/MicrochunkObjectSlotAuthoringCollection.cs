using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkObjectSlotAuthoringCollection
    {
        private readonly List<MicrochunkObjectSlotAuthoringRow> rows = new List<MicrochunkObjectSlotAuthoringRow>();

        public IReadOnlyList<MicrochunkObjectSlotAuthoringRow> Rows =>
            new ReadOnlyCollection<MicrochunkObjectSlotAuthoringRow>(new List<MicrochunkObjectSlotAuthoringRow>(rows));

        public void Add(MicrochunkObjectSlotAuthoringRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            RejectDuplicateId(row.SlotId, -1);
            rows.Add(row);
            rows.Sort((left, right) => string.Compare(left.SlotId, right.SlotId, StringComparison.Ordinal));
        }

        public void Duplicate(string sourceId, string duplicateId)
        {
            var source = rows.FirstOrDefault(value => string.Equals(value.SlotId, sourceId, StringComparison.Ordinal));
            if (source == null) throw new KeyNotFoundException("Object-slot ID was not found: " + sourceId);
            Add(source.Duplicate(duplicateId));
        }

        public bool Remove(string slotId)
        {
            var index = rows.FindIndex(value => string.Equals(value.SlotId, slotId, StringComparison.Ordinal));
            if (index < 0) return false;
            rows.RemoveAt(index);
            return true;
        }

        public void Move(int sourceIndex, int destinationIndex)
        {
            RequireIndex(sourceIndex, nameof(sourceIndex));
            RequireIndex(destinationIndex, nameof(destinationIndex));
            if (sourceIndex == destinationIndex) return;
            var value = rows[sourceIndex];
            rows.RemoveAt(sourceIndex);
            rows.Insert(destinationIndex, value);
        }

        public void Replace(int index, MicrochunkObjectSlotAuthoringRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            RequireIndex(index, nameof(index));
            RejectDuplicateId(row.SlotId, index);
            rows[index] = row;
        }

        public IReadOnlyList<MicrochunkObjectSlotDefinition> ProjectDefinitions()
        {
            var projected = new List<MicrochunkObjectSlotDefinition>();
            foreach (var row in rows.OrderBy(value => value.SlotId, StringComparer.Ordinal))
            {
                projected.Add(row.ToRuntimeDefinition());
            }
            return new ReadOnlyCollection<MicrochunkObjectSlotDefinition>(projected);
        }

        private void RejectDuplicateId(string slotId, int ignoredIndex)
        {
            for (var index = 0; index < rows.Count; index++)
            {
                if (index != ignoredIndex && string.Equals(rows[index].SlotId, slotId, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Object-slot IDs must be unique.", nameof(slotId));
                }
            }
        }

        private void RequireIndex(int index, string parameterName)
        {
            if (index < 0 || index >= rows.Count) throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
