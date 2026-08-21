using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomePatchValidationPublication
    {
        private readonly IReadOnlyList<GeneratedBiomePatchRow> patchRows;
        private readonly byte[] generatedBiomePatchesCsv;
        private readonly byte[] generatedWorldSectorsCsv;

        internal BiomePatchValidationPublication(
            BiomePatchExportPublication sourceExport,
            BiomePatchValidationDiagnostics diagnostics)
        {
            SourceExport = sourceExport ?? throw new ArgumentNullException(nameof(sourceExport));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            Snapshot = sourceExport.SourceCleanup.Snapshot;
            WorldWithBiomeAssignments = sourceExport.WorldWithBiomeAssignments;
            var rows = new List<GeneratedBiomePatchRow>(sourceExport.PatchRows);
            rows.Sort((left, right) => left.PatchInstanceId.CompareTo(right.PatchInstanceId));
            patchRows = new ReadOnlyCollection<GeneratedBiomePatchRow>(rows);
            generatedBiomePatchesCsv = sourceExport.GeneratedBiomePatchesCsv;
            generatedWorldSectorsCsv = sourceExport.GeneratedWorldSectorsCsv;
            BiomePatchFileName = sourceExport.BiomePatchFileName;
            WorldSectorFileName = sourceExport.WorldSectorFileName;
        }

        public BiomePatchExportPublication SourceExport { get; }
        public BiomePatchSnapshot Snapshot { get; }
        public GeneratedWorldData WorldWithBiomeAssignments { get; }
        public IReadOnlyList<GeneratedBiomePatchRow> PatchRows => patchRows;
        public byte[] GeneratedBiomePatchesCsv => (byte[])generatedBiomePatchesCsv.Clone();
        public byte[] GeneratedWorldSectorsCsv => (byte[])generatedWorldSectorsCsv.Clone();
        public string BiomePatchFileName { get; }
        public string WorldSectorFileName { get; }
        public BiomePatchValidationDiagnostics Diagnostics { get; }
    }
}
