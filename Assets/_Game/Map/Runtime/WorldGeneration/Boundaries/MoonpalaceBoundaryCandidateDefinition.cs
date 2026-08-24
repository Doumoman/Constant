using System;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryCandidateDefinition
    {
        private const MoonpalaceBoundaryWarningMarker AllWarningMarkers =
            MoonpalaceBoundaryWarningMarker.Tile |
            MoonpalaceBoundaryWarningMarker.Background |
            MoonpalaceBoundaryWarningMarker.Resource |
            MoonpalaceBoundaryWarningMarker.Audio;

        public MoonpalaceBoundaryCandidateDefinition(
            string candidateId,
            MoonpalaceBiomePair pair,
            MoonpalaceBoundaryProfileId profile,
            MoonpalaceBoundaryOrientation orientation,
            MoonpalaceBoundaryRouteRole routeRole,
            MoonpalaceBoundaryEdgeSignature edgeSignature,
            int weight,
            bool mandatoryRouteAllowed,
            string toolRequirement,
            MoonpalaceBoundaryWarningMarker warningMarkers)
        {
            if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
            if ((warningMarkers & ~AllWarningMarkers) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(warningMarkers));
            }

            CandidateId = RequireStableToken(candidateId, nameof(candidateId));
            Key = new MoonpalaceBoundaryCandidateKey(pair, profile, orientation, routeRole, edgeSignature);
            Weight = weight;
            MandatoryRouteAllowed = mandatoryRouteAllowed;
            ToolRequirement = RequireStableToken(toolRequirement, nameof(toolRequirement));
            WarningMarkers = warningMarkers;
        }

        public string CandidateId { get; }
        public MoonpalaceBoundaryCandidateKey Key { get; }
        public MoonpalaceBiomePair Pair => Key.Pair;
        public MoonpalaceBoundaryProfileId Profile => Key.Profile;
        public MoonpalaceBoundaryOrientation Orientation => Key.Orientation;
        public MoonpalaceBoundaryRouteRole RouteRole => Key.RouteRole;
        public MoonpalaceBoundaryEdgeSignature EdgeSignature => Key.EdgeSignature;
        public int Weight { get; }
        public bool MandatoryRouteAllowed { get; }
        public string ToolRequirement { get; }
        public MoonpalaceBoundaryWarningMarker WarningMarkers { get; }

        public string Signature => string.Join("|", new[]
        {
            CandidateId,
            Key.Signature,
            Weight.ToString(CultureInfo.InvariantCulture),
            MandatoryRouteAllowed ? "true" : "false",
            ToolRequirement,
            ((int)WarningMarkers).ToString(CultureInfo.InvariantCulture),
        });

        private static string RequireStableToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Candidate IDs and tokens cannot be null, empty, whitespace, or padded.", parameterName);
            }

            return value;
        }
    }
}
