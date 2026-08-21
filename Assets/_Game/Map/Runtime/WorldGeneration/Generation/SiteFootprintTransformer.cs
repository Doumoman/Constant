using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public static class SiteFootprintTransformer
    {
        public static bool TryTransformCoordinate(
            int width,
            int height,
            SiteFootprintTransform transform,
            int sourceX,
            int sourceY,
            out int transformedX,
            out int transformedY)
        {
            transformedX = 0;
            transformedY = 0;
            if (width < 1 || width > WorldGenConstants.SectorColumns ||
                height < 1 || height > WorldGenConstants.SectorRows ||
                sourceX < 0 || sourceX >= width || sourceY < 0 || sourceY >= height)
            {
                return false;
            }

            switch (transform)
            {
                case SiteFootprintTransform.R0:
                    transformedX = sourceX;
                    transformedY = sourceY;
                    return true;
                case SiteFootprintTransform.MirrorX:
                    transformedX = width - 1 - sourceX;
                    transformedY = sourceY;
                    return true;
                case SiteFootprintTransform.MirrorY:
                    transformedX = sourceX;
                    transformedY = height - 1 - sourceY;
                    return true;
                case SiteFootprintTransform.R180:
                    transformedX = width - 1 - sourceX;
                    transformedY = height - 1 - sourceY;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryTransformSide(
            SiteFootprintTransform transform,
            SiteEntrySide sourceSide,
            out SiteEntrySide transformedSide)
        {
            transformedSide = default(SiteEntrySide);
            if (!IsDefined(sourceSide)) return false;

            switch (transform)
            {
                case SiteFootprintTransform.R0:
                    transformedSide = sourceSide;
                    return true;
                case SiteFootprintTransform.MirrorX:
                    transformedSide = MirrorX(sourceSide);
                    return true;
                case SiteFootprintTransform.MirrorY:
                    transformedSide = MirrorY(sourceSide);
                    return true;
                case SiteFootprintTransform.R180:
                    transformedSide = MirrorY(MirrorX(sourceSide));
                    return true;
                default:
                    return false;
            }
        }

        private static SiteEntrySide MirrorX(SiteEntrySide side)
        {
            if (side == SiteEntrySide.L) return SiteEntrySide.R;
            if (side == SiteEntrySide.R) return SiteEntrySide.L;
            return side;
        }

        private static SiteEntrySide MirrorY(SiteEntrySide side)
        {
            if (side == SiteEntrySide.U) return SiteEntrySide.D;
            if (side == SiteEntrySide.D) return SiteEntrySide.U;
            return side;
        }

        private static bool IsDefined(SiteEntrySide side)
        {
            return side == SiteEntrySide.L || side == SiteEntrySide.R ||
                   side == SiteEntrySide.U || side == SiteEntrySide.D;
        }
    }
}
