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
            : this(
                candidateId,
                pair,
                profile,
                orientation,
                routeRole,
                edgeSignature,
                weight,
                mandatoryRouteAllowed,
                MoonpalaceBoundaryToolRequirement.Parse(toolRequirement),
                warningMarkers)
        {
        }

        public MoonpalaceBoundaryCandidateDefinition(
            string candidateId,
            MoonpalaceBiomePair pair,
            MoonpalaceBoundaryProfileId profile,
            MoonpalaceBoundaryOrientation orientation,
            MoonpalaceBoundaryRouteRole routeRole,
            MoonpalaceBoundaryEdgeSignature edgeSignature,
            int weight,
            bool mandatoryRouteAllowed,
            MoonpalaceBoundaryToolRequirement toolRequirement,
            MoonpalaceBoundaryWarningMarker warningMarkers)
        {
            if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
            if (!toolRequirement.IsDefined)
            {
                throw new ArgumentException("Tool requirement is undefined.", nameof(toolRequirement));
            }

            if ((warningMarkers & ~AllWarningMarkers) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(warningMarkers));
            }

            CandidateId = RequireStableToken(candidateId, nameof(candidateId));
            Key = new MoonpalaceBoundaryCandidateKey(pair, profile, orientation, routeRole, edgeSignature);
            Weight = weight;
            MandatoryRouteAllowed = mandatoryRouteAllowed;
            ToolRequirement = toolRequirement;
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
        public MoonpalaceBoundaryToolRequirement ToolRequirement { get; }
        public MoonpalaceBoundaryWarningMarker WarningMarkers { get; }

        public string Signature => string.Join("|", new[]
        {
            CandidateId,
            Key.Signature,
            Weight.ToString(CultureInfo.InvariantCulture),
            MandatoryRouteAllowed ? "true" : "false",
            ToolRequirement.Token,
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
