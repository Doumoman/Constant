using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public enum ClusterFootprintTransform
    {
        R0 = 0,
        MirrorX = 1,
        MirrorY = 2,
        R180 = 3,
    }

    public enum ClusterChunkMaskState
    {
        Active = 1,
        Inactive = 2,
    }

    internal static class ClusterFootprintTransformUtility
    {
        public static bool IsSupported(ClusterFootprintTransform transform)
        {
            return transform == ClusterFootprintTransform.R0 ||
                   transform == ClusterFootprintTransform.MirrorX ||
                   transform == ClusterFootprintTransform.MirrorY ||
                   transform == ClusterFootprintTransform.R180;
        }

        public static ClusterChunkCoord Apply(
            ClusterChunkCoord coordinate,
            int width,
            int height,
            ClusterFootprintTransform transform)
        {
            switch (transform)
            {
                case ClusterFootprintTransform.R0:
                    return coordinate;
                case ClusterFootprintTransform.MirrorX:
                    return new ClusterChunkCoord(width - 1 - coordinate.X, coordinate.Y);
                case ClusterFootprintTransform.MirrorY:
                    return new ClusterChunkCoord(coordinate.X, height - 1 - coordinate.Y);
                case ClusterFootprintTransform.R180:
                    return new ClusterChunkCoord(
                        width - 1 - coordinate.X,
                        height - 1 - coordinate.Y);
                default:
                    return coordinate;
            }
        }

        public static LocalTileCoord Apply(
            LocalTileCoord coordinate,
            int width,
            int height,
            ClusterFootprintTransform transform)
        {
            switch (transform)
            {
                case ClusterFootprintTransform.R0:
                    return coordinate;
                case ClusterFootprintTransform.MirrorX:
                    return new LocalTileCoord(width - 1 - coordinate.X, coordinate.Y);
                case ClusterFootprintTransform.MirrorY:
                    return new LocalTileCoord(coordinate.X, height - 1 - coordinate.Y);
                case ClusterFootprintTransform.R180:
                    return new LocalTileCoord(
                        width - 1 - coordinate.X,
                        height - 1 - coordinate.Y);
                default:
                    return coordinate;
            }
        }
    }
}
