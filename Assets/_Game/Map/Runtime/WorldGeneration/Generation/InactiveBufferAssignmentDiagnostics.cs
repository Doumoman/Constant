using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class InactiveBufferAssignmentDiagnostics
    {
        public InactiveBufferAssignmentDiagnostics(
            int worldSectorCount,
            int siteReservationCount,
            int reservedSiteSectorCount,
            int mandatoryRouteCellCount,
            int mandatoryExclusiveSectorCount,
            int type0CellCount,
            int siteMandatoryOverlapCount,
            int approvedReservedAdapterOverlapCount,
            int protectedUnionCount,
            int assignmentCount,
            int decorativeBoundaryCount,
            int interiorInactiveCount,
            int worldEdgeInactiveCount,
            int protectedToInactiveCardinalEdgeCount,
            int inactiveToInactiveUndirectedEdgeCount,
            int unassignedSectorCount,
            int illegalOwnershipOverlapCount,
            int duplicateSectorCount,
            int openEdgeToInactiveCount,
            int rngDrawCount,
            int sourceMutationCount)
        {
            var values = new[]
            {
                worldSectorCount, siteReservationCount, reservedSiteSectorCount,
                mandatoryRouteCellCount, mandatoryExclusiveSectorCount, type0CellCount,
                siteMandatoryOverlapCount, approvedReservedAdapterOverlapCount, protectedUnionCount,
                assignmentCount, decorativeBoundaryCount, interiorInactiveCount,
                worldEdgeInactiveCount, protectedToInactiveCardinalEdgeCount,
                inactiveToInactiveUndirectedEdgeCount, unassignedSectorCount,
                illegalOwnershipOverlapCount, duplicateSectorCount, openEdgeToInactiveCount,
                rngDrawCount, sourceMutationCount
            };
            foreach (var value in values)
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(worldSectorCount));
            }
            if (decorativeBoundaryCount + interiorInactiveCount != assignmentCount)
                throw new ArgumentException("Inactive classifications must equal the assignment count.");

            WorldSectorCount = worldSectorCount;
            SiteReservationCount = siteReservationCount;
            ReservedSiteSectorCount = reservedSiteSectorCount;
            MandatoryRouteCellCount = mandatoryRouteCellCount;
            MandatoryExclusiveSectorCount = mandatoryExclusiveSectorCount;
            Type0CellCount = type0CellCount;
            SiteMandatoryOverlapCount = siteMandatoryOverlapCount;
            ApprovedReservedAdapterOverlapCount = approvedReservedAdapterOverlapCount;
            ProtectedUnionCount = protectedUnionCount;
            AssignmentCount = assignmentCount;
            DecorativeBoundaryCount = decorativeBoundaryCount;
            InteriorInactiveCount = interiorInactiveCount;
            WorldEdgeInactiveCount = worldEdgeInactiveCount;
            ProtectedToInactiveCardinalEdgeCount = protectedToInactiveCardinalEdgeCount;
            InactiveToInactiveUndirectedEdgeCount = inactiveToInactiveUndirectedEdgeCount;
            UnassignedSectorCount = unassignedSectorCount;
            IllegalOwnershipOverlapCount = illegalOwnershipOverlapCount;
            DuplicateSectorCount = duplicateSectorCount;
            OpenEdgeToInactiveCount = openEdgeToInactiveCount;
            RngDrawCount = rngDrawCount;
            SourceMutationCount = sourceMutationCount;
        }

        public int WorldSectorCount { get; }
        public int SiteReservationCount { get; }
        public int ReservedSiteSectorCount { get; }
        public int MandatoryRouteCellCount { get; }
        public int MandatoryExclusiveSectorCount { get; }
        public int Type0CellCount { get; }
        public int SiteMandatoryOverlapCount { get; }
        public int ApprovedReservedAdapterOverlapCount { get; }
        public int ProtectedUnionCount { get; }
        public int AssignmentCount { get; }
        public int DecorativeBoundaryCount { get; }
        public int InteriorInactiveCount { get; }
        public int WorldEdgeInactiveCount { get; }
        public int ProtectedToInactiveCardinalEdgeCount { get; }
        public int InactiveToInactiveUndirectedEdgeCount { get; }
        public int UnassignedSectorCount { get; }
        public int IllegalOwnershipOverlapCount { get; }
        public int DuplicateSectorCount { get; }
        public int OpenEdgeToInactiveCount { get; }
        public int RngDrawCount { get; }
        public int SourceMutationCount { get; }
    }
}
