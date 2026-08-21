using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CorePatchGrowthDiagnostics
    {
        private readonly IReadOnlyList<CorePatchGrowthRecord> records;

        internal CorePatchGrowthDiagnostics(
            ulong worldSeed,
            IEnumerable<CorePatchGrowthRecord> records,
            int corePatchCount,
            int initialAssignedSectorCount,
            int mandatoryAddedSectorCount,
            int supplementalAddedSectorCount,
            int finalAssignedSectorCount,
            int finalUnassignedSectorCount,
            int reservedSectorCount,
            int reservationIntrusionCount,
            int crossPatchOverlapCount)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (corePatchCount < 0 || initialAssignedSectorCount < 0 ||
                mandatoryAddedSectorCount < 0 || supplementalAddedSectorCount < 0 ||
                finalAssignedSectorCount < 0 || finalUnassignedSectorCount < 0 ||
                reservedSectorCount < 0 || reservationIntrusionCount < 0 ||
                crossPatchOverlapCount < 0)
                throw new ArgumentOutOfRangeException(nameof(corePatchCount));

            var values = new List<CorePatchGrowthRecord>(records);
            var sourceIds = new HashSet<SiteReservationId>();
            foreach (var record in values)
            {
                if (record == null) throw new ArgumentException("Records cannot contain null.", nameof(records));
                if (!sourceIds.Add(record.SourceReservationId))
                    throw new ArgumentException("Record source IDs must be unique.", nameof(records));
            }
            values.Sort((left, right) => left.SourceReservationId.CompareTo(right.SourceReservationId));
            if (values.Count != 0 && values.Count != corePatchCount)
                throw new ArgumentException("Successful records must cover every Core patch.", nameof(records));

            var totalAdded = checked(mandatoryAddedSectorCount + supplementalAddedSectorCount);
            if (checked(initialAssignedSectorCount + totalAdded) != finalAssignedSectorCount)
                throw new ArgumentException("Assigned-sector conservation is invalid.");
            if (finalAssignedSectorCount + finalUnassignedSectorCount != WorldGenConstants.SectorCount)
                throw new ArgumentException("Final sector counts must cover the world.");

            WorldSeed = worldSeed;
            CorePatchCount = corePatchCount;
            InitialAssignedSectorCount = initialAssignedSectorCount;
            MandatoryAddedSectorCount = mandatoryAddedSectorCount;
            SupplementalAddedSectorCount = supplementalAddedSectorCount;
            TotalAddedSectorCount = totalAdded;
            FinalAssignedSectorCount = finalAssignedSectorCount;
            FinalUnassignedSectorCount = finalUnassignedSectorCount;
            ReservedSectorCount = reservedSectorCount;
            ReservationIntrusionCount = reservationIntrusionCount;
            CrossPatchOverlapCount = crossPatchOverlapCount;
            RngDrawCount = 0;
            this.records = new ReadOnlyCollection<CorePatchGrowthRecord>(values);
        }

        public ulong WorldSeed { get; }
        public IReadOnlyList<CorePatchGrowthRecord> Records => records;
        public int CorePatchCount { get; }
        public int InitialAssignedSectorCount { get; }
        public int MandatoryAddedSectorCount { get; }
        public int SupplementalAddedSectorCount { get; }
        public int TotalAddedSectorCount { get; }
        public int FinalAssignedSectorCount { get; }
        public int FinalUnassignedSectorCount { get; }
        public int ReservedSectorCount { get; }
        public int ReservationIntrusionCount { get; }
        public int CrossPatchOverlapCount { get; }
        public int RngDrawCount { get; }
    }
}
