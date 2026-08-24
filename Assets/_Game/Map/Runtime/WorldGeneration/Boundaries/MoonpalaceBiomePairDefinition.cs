using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBiomePairDefinition
    {
        public const string NoToolRequirement = "NONE";
        public const int RequiredMinimumWarningMarkerCount = 2;

        private const MoonpalaceBoundaryWarningMarker AllWarningMarkers =
            MoonpalaceBoundaryWarningMarker.Tile |
            MoonpalaceBoundaryWarningMarker.Background |
            MoonpalaceBoundaryWarningMarker.Resource |
            MoonpalaceBoundaryWarningMarker.Audio;

        private readonly IReadOnlyList<MoonpalaceBoundaryOrientation> supportedOrientations;

        public MoonpalaceBiomePairDefinition(
            MoonpalaceBiomePair pair,
            IEnumerable<MoonpalaceBoundaryOrientation> supportedOrientations,
            string mandatoryToolRequirement,
            bool mandatoryRouteAllowed,
            MoonpalaceBoundaryWarningMarker availableWarningMarkers,
            int minimumDistinctWarningMarkerCount)
        {
            if (!pair.IsDefined) throw new ArgumentException("Pair is undefined.", nameof(pair));
            if (supportedOrientations == null) throw new ArgumentNullException(nameof(supportedOrientations));

            var orientationCopy = supportedOrientations.ToArray();
            if (orientationCopy.Any(orientation =>
                    orientation != MoonpalaceBoundaryOrientation.Horizontal &&
                    orientation != MoonpalaceBoundaryOrientation.Vertical))
            {
                throw new ArgumentOutOfRangeException(nameof(supportedOrientations));
            }

            if (orientationCopy.Distinct().Count() != orientationCopy.Length ||
                orientationCopy.Length != 2 ||
                orientationCopy[0] != MoonpalaceBoundaryOrientation.Horizontal ||
                orientationCopy[1] != MoonpalaceBoundaryOrientation.Vertical)
            {
                throw new ArgumentException(
                    "Every Moonpalace pair must explicitly support Horizontal then Vertical exactly once.",
                    nameof(supportedOrientations));
            }

            if (!string.Equals(mandatoryToolRequirement, NoToolRequirement, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Mandatory Moonpalace boundaries must use tool requirement NONE.",
                    nameof(mandatoryToolRequirement));
            }

            if (!mandatoryRouteAllowed)
            {
                throw new ArgumentException(
                    "Mandatory Moonpalace boundaries must explicitly allow mandatory routes.",
                    nameof(mandatoryRouteAllowed));
            }

            if ((availableWarningMarkers & ~AllWarningMarkers) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(availableWarningMarkers));
            }

            var availableCount = CountWarningMarkers(availableWarningMarkers);
            if (minimumDistinctWarningMarkerCount < RequiredMinimumWarningMarkerCount ||
                minimumDistinctWarningMarkerCount > availableCount)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumDistinctWarningMarkerCount));
            }

            Pair = pair;
            this.supportedOrientations = new ReadOnlyCollection<MoonpalaceBoundaryOrientation>(orientationCopy);
            MandatoryToolRequirement = mandatoryToolRequirement;
            MandatoryRouteAllowed = mandatoryRouteAllowed;
            AvailableWarningMarkers = availableWarningMarkers;
            MinimumDistinctWarningMarkerCount = minimumDistinctWarningMarkerCount;
        }

        public MoonpalaceBiomePair Pair { get; }
        public IReadOnlyList<MoonpalaceBoundaryOrientation> SupportedOrientations => supportedOrientations;
        public string MandatoryToolRequirement { get; }
        public bool MandatoryRouteAllowed { get; }
        public MoonpalaceBoundaryWarningMarker AvailableWarningMarkers { get; }
        public int MinimumDistinctWarningMarkerCount { get; }

        public string Signature => string.Join("|", new[]
        {
            Pair.PairId,
            "Horizontal,Vertical",
            MandatoryToolRequirement,
            MandatoryRouteAllowed ? "true" : "false",
            MinimumDistinctWarningMarkerCount.ToString(CultureInfo.InvariantCulture),
            "Tile,Background,Resource,Audio",
        });

        public bool Supports(MoonpalaceBoundaryOrientation orientation)
        {
            return orientation == MoonpalaceBoundaryOrientation.Horizontal ||
                   orientation == MoonpalaceBoundaryOrientation.Vertical;
        }

        public static MoonpalaceBiomePairDefinition CreateCanonical(MoonpalaceBiomePair pair)
        {
            return new MoonpalaceBiomePairDefinition(
                pair,
                new[]
                {
                    MoonpalaceBoundaryOrientation.Horizontal,
                    MoonpalaceBoundaryOrientation.Vertical,
                },
                NoToolRequirement,
                true,
                AllWarningMarkers,
                RequiredMinimumWarningMarkerCount);
        }

        public static int CountWarningMarkers(MoonpalaceBoundaryWarningMarker markers)
        {
            if ((markers & ~AllWarningMarkers) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(markers));
            }

            var value = (int)markers;
            var count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }
    }
}
