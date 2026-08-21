namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class GridInitializationPassAdapter : IWorldGenerationPass
    {
        private readonly GridInitializationPass pass = new GridInitializationPass();

        public string PassId => GridInitializationPass.PassId;
        public string ClassName => nameof(GridInitializationPass);

        public WorldGenerationPassResult Execute(WorldGenerationPassContext context)
        {
            return WorldGenerationPassResult.Success(
                GridInitializationPass.OutputArtifactId,
                pass.Execute(context.WorldSeed));
        }
    }
}
