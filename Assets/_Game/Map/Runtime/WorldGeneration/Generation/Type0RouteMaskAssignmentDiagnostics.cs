using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class Type0RouteMaskAssignmentDiagnostics
    {
        public Type0RouteMaskAssignmentDiagnostics(
            int sourceRouteMaskDefinitionCount,
            int registeredType0MaskCount,
            int ignoredNonType0DefinitionCount,
            int sourceRegionCount,
            int sourceCellCount,
            int assignmentCount,
            int internalUndirectedEdgeCount,
            int attachmentBoundaryClosedCount,
            int mandatoryBoundaryBaseOpenCount,
            int closedCrossRegionAdjacencyCount,
            int horizontalThroughCount,
            int unsupportedRequiredMaskCount,
            int rngDrawCount,
            int sourceMutationCount)
        {
            var values = new[]
            {
                sourceRouteMaskDefinitionCount, registeredType0MaskCount, ignoredNonType0DefinitionCount,
                sourceRegionCount, sourceCellCount, assignmentCount, internalUndirectedEdgeCount,
                attachmentBoundaryClosedCount, mandatoryBoundaryBaseOpenCount,
                closedCrossRegionAdjacencyCount, horizontalThroughCount,
                unsupportedRequiredMaskCount, rngDrawCount, sourceMutationCount
            };
            foreach (var value in values)
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(sourceRouteMaskDefinitionCount));
            }

            SourceRouteMaskDefinitionCount = sourceRouteMaskDefinitionCount;
            RegisteredType0MaskCount = registeredType0MaskCount;
            IgnoredNonType0DefinitionCount = ignoredNonType0DefinitionCount;
            SourceRegionCount = sourceRegionCount;
            SourceCellCount = sourceCellCount;
            AssignmentCount = assignmentCount;
            InternalUndirectedEdgeCount = internalUndirectedEdgeCount;
            AttachmentBoundaryClosedCount = attachmentBoundaryClosedCount;
            MandatoryBoundaryBaseOpenCount = mandatoryBoundaryBaseOpenCount;
            ClosedCrossRegionAdjacencyCount = closedCrossRegionAdjacencyCount;
            HorizontalThroughCount = horizontalThroughCount;
            UnsupportedRequiredMaskCount = unsupportedRequiredMaskCount;
            RngDrawCount = rngDrawCount;
            SourceMutationCount = sourceMutationCount;
        }

        public int SourceRouteMaskDefinitionCount { get; }
        public int RegisteredType0MaskCount { get; }
        public int IgnoredNonType0DefinitionCount { get; }
        public int SourceRegionCount { get; }
        public int SourceCellCount { get; }
        public int AssignmentCount { get; }
        public int InternalUndirectedEdgeCount { get; }
        public int AttachmentBoundaryClosedCount { get; }
        public int MandatoryBoundaryBaseOpenCount { get; }
        public int ClosedCrossRegionAdjacencyCount { get; }
        public int HorizontalThroughCount { get; }
        public int UnsupportedRequiredMaskCount { get; }
        public int RngDrawCount { get; }
        public int SourceMutationCount { get; }
    }
}
