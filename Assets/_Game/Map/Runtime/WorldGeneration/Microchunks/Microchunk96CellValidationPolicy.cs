namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class Microchunk96CellValidationPolicy
    {
        public static Microchunk96CellValidationPolicy Default { get; } = new Microchunk96CellValidationPolicy(true);
        public static Microchunk96CellValidationPolicy Complete { get; } = Default;
        public static Microchunk96CellValidationPolicy Partial { get; } = new Microchunk96CellValidationPolicy(false);
        public static Microchunk96CellValidationPolicy Draft { get; } = Partial;

        public bool RequireCompleteCoverage { get; }
        public bool RequiresCompleteCoverage => RequireCompleteCoverage;

        public Microchunk96CellValidationPolicy(bool requireCompleteCoverage)
        {
            RequireCompleteCoverage = requireCompleteCoverage;
        }

        public static Microchunk96CellValidationPolicy ForDefinition(MicrochunkDefinition definition)
        {
            if (definition == null)
            {
                throw new System.ArgumentNullException(nameof(definition));
            }

            return definition.TileDataComplete ? Complete : Partial;
        }
    }
}
