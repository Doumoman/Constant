using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkSocketDefinition
    {
        public string SocketId { get; }
        public MicrochunkSide Side { get; }
        public string BandId { get; }
        public MicrochunkTraversalKind TraversalKind { get; }
        public string Direction { get; }
        public bool MandatoryAllowed { get; }
        public MicrochunkToolRequirement ToolRequirement { get; }
        public string EdgeSignatureId { get; }
        public MicrochunkRouteLayer RouteLayer { get; }
        public int MinimumSafeTiles { get; }
        public string Notes { get; }

        public MicrochunkSocketDefinition(
            string socketId,
            MicrochunkSide side,
            string bandId,
            MicrochunkTraversalKind traversalKind,
            string direction,
            bool mandatoryAllowed,
            MicrochunkToolRequirement toolRequirement,
            string edgeSignatureId,
            MicrochunkRouteLayer routeLayer,
            int minimumSafeTiles,
            string notes)
        {
            if (!Enum.IsDefined(typeof(MicrochunkSide), side)) throw new ArgumentOutOfRangeException(nameof(side));
            if (!Enum.IsDefined(typeof(MicrochunkTraversalKind), traversalKind)) throw new ArgumentOutOfRangeException(nameof(traversalKind));
            if (!Enum.IsDefined(typeof(MicrochunkToolRequirement), toolRequirement)) throw new ArgumentOutOfRangeException(nameof(toolRequirement));
            if (!Enum.IsDefined(typeof(MicrochunkRouteLayer), routeLayer)) throw new ArgumentOutOfRangeException(nameof(routeLayer));
            if (minimumSafeTiles < 0) throw new ArgumentOutOfRangeException(nameof(minimumSafeTiles));

            SocketId = RequireToken(socketId, nameof(socketId));
            Side = side;
            BandId = RequireToken(bandId, nameof(bandId));
            TraversalKind = traversalKind;
            Direction = RequireToken(direction, nameof(direction));
            MandatoryAllowed = mandatoryAllowed;
            ToolRequirement = toolRequirement;
            EdgeSignatureId = RequireToken(edgeSignatureId, nameof(edgeSignatureId));
            RouteLayer = routeLayer;
            MinimumSafeTiles = minimumSafeTiles;
            Notes = notes ?? string.Empty;
        }

        private static string RequireToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Socket IDs and tokens cannot be null, empty, or whitespace.", parameterName);
            }

            return value;
        }
    }
}
