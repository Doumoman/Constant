namespace StarNight.Map.WorldGeneration.Generation
{
    public enum InactiveBufferAssignmentStatus
    {
        Completed,
        InvalidInput,
        InvalidSettings,
        InvalidSource,
        InvalidAccounting,
        InvalidTopology
    }

    public enum InactiveBufferAssignmentErrorCode
    {
        NullInput,
        InvalidStatus,
        InvalidDigest,
        SourceMismatch,
        InvalidWorld,
        InvalidSectorIndex,
        DuplicateOwnership,
        OwnershipOverlap,
        InvalidSiteReservation,
        InvalidBiomePublication,
        InvalidMandatoryGraph,
        InvalidType0Assignment,
        InvalidReturnPolicy,
        OpenEdgeToInactive,
        IncompleteAccounting
    }

    public enum InactiveBufferKind
    {
        InteriorInactive,
        DecorativeBoundary
    }
}
