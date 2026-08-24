using System;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkCsvImportRequest
    {
        public string SelectedMicrochunkId { get; }
        public MicrochunkId SelectedId => new MicrochunkId(SelectedMicrochunkId);

        public MicrochunkCsvImportRequest(string selectedMicrochunkId)
        {
            if (string.IsNullOrWhiteSpace(selectedMicrochunkId))
            {
                throw new ArgumentException(
                    "Exactly one selected microchunk ID is required.",
                    nameof(selectedMicrochunkId));
            }
            if (!string.Equals(
                    selectedMicrochunkId,
                    selectedMicrochunkId.Trim(),
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Selected microchunk ID must be canonical and contain no surrounding whitespace.",
                    nameof(selectedMicrochunkId));
            }

            SelectedMicrochunkId = selectedMicrochunkId;
        }
    }
}
