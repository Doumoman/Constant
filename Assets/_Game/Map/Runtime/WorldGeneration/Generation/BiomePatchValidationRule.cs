namespace StarNight.Map.WorldGeneration.Generation
{
    public enum BiomePatchValidationRule
    {
        RequiredBiomeCoverage,
        PatchDefinitionIdentity,
        PatchSizeLimits,
        PatchConnectivity,
        PatchSeedContract,
        NormalPatchCountRange,
        PatchRuleCountRange,
        SameRuleSeedDistance,
        WorldEdgePolicy,
        WorldShareLimits,
        CoreSiteOwnership,
        ReservationAssignment,
        OwnershipExclusivity,
        IntrusionBoundaryContract,
        ExportReproducibility
    }
}
