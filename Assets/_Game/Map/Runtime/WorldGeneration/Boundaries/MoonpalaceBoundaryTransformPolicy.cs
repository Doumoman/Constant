using System;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryTransformPolicy
    {
        private MoonpalaceBoundaryTransformPolicy(
            MoonpalaceBoundaryRequestDirection direction,
            MoonpalaceBoundaryOrientation orientation,
            MicrochunkTransform transform)
        {
            Direction = direction;
            Orientation = orientation;
            Transform = transform;
        }

        public MoonpalaceBoundaryRequestDirection Direction { get; }
        public MoonpalaceBoundaryOrientation Orientation { get; }
        public MicrochunkTransform Transform { get; }
        public bool RequiresTransform => Transform != MicrochunkTransform.R0;

        public string Signature => string.Join("|", new[]
        {
            Direction == MoonpalaceBoundaryRequestDirection.Forward ? "Forward" : "Reverse",
            Orientation == MoonpalaceBoundaryOrientation.Horizontal ? "Horizontal" : "Vertical",
            MicrochunkTransformUtility.ToTransformToken(Transform),
        });

        public static MoonpalaceBoundaryTransformPolicy Create(
            MoonpalaceBoundaryRequestDirection direction,
            MoonpalaceBoundaryOrientation orientation)
        {
            if (direction != MoonpalaceBoundaryRequestDirection.Forward &&
                direction != MoonpalaceBoundaryRequestDirection.Reverse)
            {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }

            if (orientation != MoonpalaceBoundaryOrientation.Horizontal &&
                orientation != MoonpalaceBoundaryOrientation.Vertical)
            {
                throw new ArgumentOutOfRangeException(nameof(orientation));
            }

            var transform = MicrochunkTransform.R0;
            if (direction == MoonpalaceBoundaryRequestDirection.Reverse)
            {
                transform = orientation == MoonpalaceBoundaryOrientation.Horizontal
                    ? MicrochunkTransform.MirrorX
                    : MicrochunkTransform.MirrorY;
            }

            return new MoonpalaceBoundaryTransformPolicy(direction, orientation, transform);
        }
    }
}
