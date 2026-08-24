namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryResolveRequest
    {
        public MoonpalaceBoundaryResolveRequest(
            MoonpalaceBiomeId fromBiome,
            MoonpalaceBiomeId toBiome,
            MoonpalaceBoundaryProfileId profile,
            MoonpalaceBoundaryOrientation orientation,
            MoonpalaceBoundaryRouteRole routeRole,
            MoonpalaceBoundaryEdgeSignature edgeSignature,
            ulong selectionSeed)
        {
            FromBiome = fromBiome;
            ToBiome = toBiome;
            Profile = profile;
            Orientation = orientation;
            RouteRole = routeRole;
            EdgeSignature = edgeSignature;
            SelectionSeed = selectionSeed;
        }

        public MoonpalaceBiomeId FromBiome { get; }
        public MoonpalaceBiomeId ToBiome { get; }
        public MoonpalaceBoundaryProfileId Profile { get; }
        public MoonpalaceBoundaryOrientation Orientation { get; }
        public MoonpalaceBoundaryRouteRole RouteRole { get; }
        public MoonpalaceBoundaryEdgeSignature EdgeSignature { get; }
        public ulong SelectionSeed { get; }
    }
}
