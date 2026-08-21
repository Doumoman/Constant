using System.Collections.Generic;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class GridInitializationPass
    {
        public const string PassId = "PASS_GRID";
        public const string OutputArtifactId = "GRID";

        public GridInitializationResult Execute(ulong worldSeed)
        {
            var cells = new List<SectorCell>(WorldGenConstants.SectorCount);
            var neighbors = new List<SectorNeighborIndices>(WorldGenConstants.SectorCount);

            for (var y = 0; y < WorldGenConstants.SectorRows; y++)
            {
                for (var x = 0; x < WorldGenConstants.SectorColumns; x++)
                {
                    var coordinate = new SectorCoord(x, y);
                    var index = WorldGridIndex.ToIndex(coordinate);
                    cells.Add(SectorCell.CreateUnassigned(index, coordinate));
                    neighbors.Add(new SectorNeighborIndices(
                        index,
                        WorldGridIndex.GetLeftIndex(index),
                        WorldGridIndex.GetRightIndex(index),
                        WorldGridIndex.GetUpIndex(index),
                        WorldGridIndex.GetDownIndex(index)));
                }
            }

            var worldData = new GeneratedWorldData(worldSeed, cells);
            return new GridInitializationResult(worldData, neighbors);
        }
    }
}
