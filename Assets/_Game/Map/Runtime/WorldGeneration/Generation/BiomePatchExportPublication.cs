using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomePatchExportPublication
    {
        private readonly IReadOnlyList<GeneratedBiomePatchRow> patchRows;
        private readonly byte[] generatedBiomePatchesCsv;
        private readonly byte[] generatedWorldSectorsCsv;

        internal BiomePatchExportPublication(
            PatchCleanupPublication sourceCleanup,
            GeneratedWorldData sourceWorld,
            GeneratedWorldData worldWithBiomeAssignments,
            IEnumerable<GeneratedBiomePatchRow> patchRows,
            byte[] generatedBiomePatchesCsv,
            byte[] generatedWorldSectorsCsv,
            int assignedSectorCount,
            int unassignedSectorCount)
        {
            SourceCleanup = sourceCleanup ?? throw new ArgumentNullException(nameof(sourceCleanup));
            SourceWorld = sourceWorld ?? throw new ArgumentNullException(nameof(sourceWorld));
            WorldWithBiomeAssignments = worldWithBiomeAssignments ??
                throw new ArgumentNullException(nameof(worldWithBiomeAssignments));
            if (patchRows == null) throw new ArgumentNullException(nameof(patchRows));
            if (generatedBiomePatchesCsv == null) throw new ArgumentNullException(nameof(generatedBiomePatchesCsv));
            if (generatedWorldSectorsCsv == null) throw new ArgumentNullException(nameof(generatedWorldSectorsCsv));

            var rows = new List<GeneratedBiomePatchRow>(patchRows);
            rows.Sort((left, right) => left.PatchInstanceId.CompareTo(right.PatchInstanceId));
            if (rows.Count != sourceCleanup.Snapshot.Patches.Count)
                throw new ArgumentException("Patch row count must match the cleanup snapshot.", nameof(patchRows));
            if (sourceWorld.Seed != sourceCleanup.Snapshot.Seed ||
                worldWithBiomeAssignments.Seed != sourceCleanup.Snapshot.Seed)
                throw new ArgumentException("Published artifacts must share one world seed.");
            if (worldWithBiomeAssignments.Cells.Count != WorldGenConstants.SectorCount)
                throw new ArgumentException("Published world must contain exactly 169 sectors.");
            if (assignedSectorCount < 0 || unassignedSectorCount < 0 ||
                assignedSectorCount + unassignedSectorCount != WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(assignedSectorCount));

            this.patchRows = new ReadOnlyCollection<GeneratedBiomePatchRow>(rows);
            this.generatedBiomePatchesCsv = (byte[])generatedBiomePatchesCsv.Clone();
            this.generatedWorldSectorsCsv = (byte[])generatedWorldSectorsCsv.Clone();
            BiomePatchFileName = GeneratedBiomePatchCsvSerializer.FileName;
            WorldSectorFileName = GeneratedWorldDataCsvSerializer.FileName;
            PatchRowCount = rows.Count;
            WorldSectorRowCount = worldWithBiomeAssignments.Cells.Count;
            AssignedSectorCount = assignedSectorCount;
            UnassignedSectorCount = unassignedSectorCount;
        }

        public PatchCleanupPublication SourceCleanup { get; }
        public GeneratedWorldData SourceWorld { get; }
        public GeneratedWorldData WorldWithBiomeAssignments { get; }
        public IReadOnlyList<GeneratedBiomePatchRow> PatchRows => patchRows;
        public byte[] GeneratedBiomePatchesCsv => (byte[])generatedBiomePatchesCsv.Clone();
        public byte[] GeneratedWorldSectorsCsv => (byte[])generatedWorldSectorsCsv.Clone();
        public string BiomePatchFileName { get; }
        public string WorldSectorFileName { get; }
        public int PatchRowCount { get; }
        public int WorldSectorRowCount { get; }
        public int AssignedSectorCount { get; }
        public int UnassignedSectorCount { get; }
    }
}
