using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public static class MicrochunkTransformUtility
    {
        public static bool TryParseTransformToken(string token, out MicrochunkTransform transform)
        {
            switch (token)
            {
                case "R0":
                    transform = MicrochunkTransform.R0;
                    return true;
                case "MIRROR_X":
                    transform = MicrochunkTransform.MirrorX;
                    return true;
                case "MIRROR_Y":
                    transform = MicrochunkTransform.MirrorY;
                    return true;
                case "R180":
                    transform = MicrochunkTransform.R180;
                    return true;
                default:
                    transform = default;
                    return false;
            }
        }

        public static string ToTransformToken(MicrochunkTransform transform)
        {
            ValidateTransform(transform);
            switch (transform)
            {
                case MicrochunkTransform.R0: return "R0";
                case MicrochunkTransform.MirrorX: return "MIRROR_X";
                case MicrochunkTransform.MirrorY: return "MIRROR_Y";
                case MicrochunkTransform.R180: return "R180";
                default: throw new ArgumentOutOfRangeException(nameof(transform));
            }
        }

        public static MicrochunkLocalCoord TransformCoordinate(
            MicrochunkLocalCoord coordinate,
            MicrochunkTransform transform)
        {
            ValidateTransform(transform);
            switch (transform)
            {
                case MicrochunkTransform.R0:
                    return coordinate;
                case MicrochunkTransform.MirrorX:
                    return new MicrochunkLocalCoord(
                        MicrochunkConstants.WidthTiles - 1 - coordinate.X,
                        coordinate.Y);
                case MicrochunkTransform.MirrorY:
                    return new MicrochunkLocalCoord(
                        coordinate.X,
                        MicrochunkConstants.HeightTiles - 1 - coordinate.Y);
                case MicrochunkTransform.R180:
                    return new MicrochunkLocalCoord(
                        MicrochunkConstants.WidthTiles - 1 - coordinate.X,
                        MicrochunkConstants.HeightTiles - 1 - coordinate.Y);
                default:
                    throw new ArgumentOutOfRangeException(nameof(transform));
            }
        }

        public static MicrochunkSide TransformSide(
            MicrochunkSide side,
            MicrochunkTransform transform)
        {
            ValidateSide(side);
            ValidateTransform(transform);

            switch (transform)
            {
                case MicrochunkTransform.R0:
                    return side;
                case MicrochunkTransform.MirrorX:
                    return SwapHorizontal(side);
                case MicrochunkTransform.MirrorY:
                    return SwapVertical(side);
                case MicrochunkTransform.R180:
                    return SwapVertical(SwapHorizontal(side));
                default:
                    throw new ArgumentOutOfRangeException(nameof(transform));
            }
        }

        public static MicrochunkObjectOrientation TransformOrientation(
            MicrochunkObjectOrientation orientation,
            MicrochunkTransform transform)
        {
            ValidateOrientation(orientation);
            ValidateTransform(transform);

            if (orientation == MicrochunkObjectOrientation.None)
            {
                return MicrochunkObjectOrientation.None;
            }

            switch (transform)
            {
                case MicrochunkTransform.R0:
                    return orientation;
                case MicrochunkTransform.MirrorX:
                    return SwapHorizontal(orientation);
                case MicrochunkTransform.MirrorY:
                    return SwapVertical(orientation);
                case MicrochunkTransform.R180:
                    return SwapVertical(SwapHorizontal(orientation));
                default:
                    throw new ArgumentOutOfRangeException(nameof(transform));
            }
        }

        public static bool TryParseOrientationToken(
            string token,
            out MicrochunkObjectOrientation orientation)
        {
            switch (token)
            {
                case "NONE":
                    orientation = MicrochunkObjectOrientation.None;
                    return true;
                case "L":
                    orientation = MicrochunkObjectOrientation.Left;
                    return true;
                case "R":
                    orientation = MicrochunkObjectOrientation.Right;
                    return true;
                case "U":
                    orientation = MicrochunkObjectOrientation.Up;
                    return true;
                case "D":
                    orientation = MicrochunkObjectOrientation.Down;
                    return true;
                default:
                    orientation = default;
                    return false;
            }
        }

        public static string ToOrientationToken(MicrochunkObjectOrientation orientation)
        {
            ValidateOrientation(orientation);
            switch (orientation)
            {
                case MicrochunkObjectOrientation.None: return "NONE";
                case MicrochunkObjectOrientation.Left: return "L";
                case MicrochunkObjectOrientation.Right: return "R";
                case MicrochunkObjectOrientation.Up: return "U";
                case MicrochunkObjectOrientation.Down: return "D";
                default: throw new ArgumentOutOfRangeException(nameof(orientation));
            }
        }

        public static void ValidateTransform(MicrochunkTransform transform)
        {
            if (!Enum.IsDefined(typeof(MicrochunkTransform), transform))
            {
                throw new ArgumentOutOfRangeException(nameof(transform));
            }
        }

        private static MicrochunkSide SwapHorizontal(MicrochunkSide side)
        {
            if (side == MicrochunkSide.Left) return MicrochunkSide.Right;
            if (side == MicrochunkSide.Right) return MicrochunkSide.Left;
            return side;
        }

        private static MicrochunkSide SwapVertical(MicrochunkSide side)
        {
            if (side == MicrochunkSide.Up) return MicrochunkSide.Down;
            if (side == MicrochunkSide.Down) return MicrochunkSide.Up;
            return side;
        }

        private static MicrochunkObjectOrientation SwapHorizontal(
            MicrochunkObjectOrientation orientation)
        {
            if (orientation == MicrochunkObjectOrientation.Left) return MicrochunkObjectOrientation.Right;
            if (orientation == MicrochunkObjectOrientation.Right) return MicrochunkObjectOrientation.Left;
            return orientation;
        }

        private static MicrochunkObjectOrientation SwapVertical(
            MicrochunkObjectOrientation orientation)
        {
            if (orientation == MicrochunkObjectOrientation.Up) return MicrochunkObjectOrientation.Down;
            if (orientation == MicrochunkObjectOrientation.Down) return MicrochunkObjectOrientation.Up;
            return orientation;
        }

        private static void ValidateSide(MicrochunkSide side)
        {
            if (!Enum.IsDefined(typeof(MicrochunkSide), side))
            {
                throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        private static void ValidateOrientation(MicrochunkObjectOrientation orientation)
        {
            if (!Enum.IsDefined(typeof(MicrochunkObjectOrientation), orientation))
            {
                throw new ArgumentOutOfRangeException(nameof(orientation));
            }
        }
    }
}
