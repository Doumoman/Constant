using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceCraterDoughBoundaryContentReport
    {
        internal MoonpalaceCraterDoughBoundaryContentReport(
            IEnumerable<string> candidateIds,
            IEnumerable<string> microchunkIds,
            int candidateCount,
            bool profileOrientationMatrixComplete,
            int tileRowCount,
            IDictionary<string, int> rowsPerOwnedMicrochunk,
            int socketCount,
            bool horizontalSocketShapeValid,
            bool verticalSocketShapeValid,
            bool mandatoryAllowed,
            bool toolRequirementNone,
            IDictionary<string, int> warningMarkerCategoriesByMicrochunk,
            int generatedCsvCreated,
            int otherPairRowsModified,
            int craterRootRowsModified,
            int craterMillRowsModified,
            int invalidLayerHorizontalCandidateCount,
            IEnumerable<string> issues)
        {
            CandidateIds = Snapshot(candidateIds);
            MicrochunkIds = Snapshot(microchunkIds);
            CandidateCount = candidateCount;
            ProfileOrientationMatrixComplete = profileOrientationMatrixComplete;
            TileRowCount = tileRowCount;
            RowsPerOwnedMicrochunk = new ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(rowsPerOwnedMicrochunk, StringComparer.Ordinal));
            SocketCount = socketCount;
            HorizontalSocketShapeValid = horizontalSocketShapeValid;
            VerticalSocketShapeValid = verticalSocketShapeValid;
            MandatoryAllowed = mandatoryAllowed;
            ToolRequirementNone = toolRequirementNone;
            WarningMarkerCategoriesByMicrochunk = new ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(warningMarkerCategoriesByMicrochunk, StringComparer.Ordinal));
            GeneratedCsvCreated = generatedCsvCreated;
            OtherPairRowsModified = otherPairRowsModified;
            CraterRootRowsModified = craterRootRowsModified;
            CraterMillRowsModified = craterMillRowsModified;
            InvalidLayerHorizontalCandidateCount = invalidLayerHorizontalCandidateCount;
            Issues = Snapshot(issues);
        }

        public IReadOnlyList<string> CandidateIds { get; }
        public IReadOnlyList<string> MicrochunkIds { get; }
        public int CandidateCount { get; }
        public bool ProfileOrientationMatrixComplete { get; }
        public int TileRowCount { get; }
        public IReadOnlyDictionary<string, int> RowsPerOwnedMicrochunk { get; }
        public int SocketCount { get; }
        public bool HorizontalSocketShapeValid { get; }
        public bool VerticalSocketShapeValid { get; }
        public bool MandatoryAllowed { get; }
        public bool ToolRequirementNone { get; }
        public IReadOnlyDictionary<string, int> WarningMarkerCategoriesByMicrochunk { get; }
        public int GeneratedCsvCreated { get; }
        public int OtherPairRowsModified { get; }
        public int CraterRootRowsModified { get; }
        public int CraterMillRowsModified { get; }
        public int InvalidLayerHorizontalCandidateCount { get; }
        public IReadOnlyList<string> Issues { get; }
        public bool Success => Issues.Count == 0;

        private static IReadOnlyList<string> Snapshot(IEnumerable<string> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new ReadOnlyCollection<string>(source.ToArray());
        }
    }
}
