using System;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkSocketAuthoringRow
    {
        public const string DefaultToolRequirementToken = "NONE";

        public string SocketId { get; }
        public string SideToken { get; }
        public string BandId { get; }
        public string TraversalKindToken { get; }
        public string EdgeSignatureId { get; }
        public bool MandatoryAllowed { get; }
        public string ToolRequirementToken { get; }

        public MicrochunkSocketAuthoringRow(
            string socketId,
            string sideToken,
            string bandId,
            string traversalKindToken,
            string edgeSignatureId,
            bool mandatoryAllowed = false,
            string toolRequirementToken = DefaultToolRequirementToken)
        {
            SocketId = RequireCanonicalToken(socketId, nameof(socketId));
            SideToken = RequireSideToken(sideToken);
            BandId = RequireCanonicalToken(bandId, nameof(bandId));
            TraversalKindToken = RequireTraversalToken(traversalKindToken);
            EdgeSignatureId = RequireCanonicalToken(edgeSignatureId, nameof(edgeSignatureId));
            MandatoryAllowed = mandatoryAllowed;
            ToolRequirementToken = RequireToolToken(toolRequirementToken);
        }

        public MicrochunkSocketAuthoringRow Duplicate(string socketId)
        {
            return new MicrochunkSocketAuthoringRow(
                socketId,
                SideToken,
                BandId,
                TraversalKindToken,
                EdgeSignatureId,
                MandatoryAllowed,
                ToolRequirementToken);
        }

        public MicrochunkSocketDefinition ToRuntimeDefinition(int minimumSafeTiles)
        {
            if (minimumSafeTiles < 0) throw new ArgumentOutOfRangeException(nameof(minimumSafeTiles));

            return new MicrochunkSocketDefinition(
                SocketId,
                ParseSide(SideToken),
                BandId,
                ParseTraversalKind(TraversalKindToken),
                "BIDIRECTIONAL",
                MandatoryAllowed,
                ParseToolRequirement(ToolRequirementToken),
                EdgeSignatureId,
                MandatoryAllowed ? MicrochunkRouteLayer.Both : MicrochunkRouteLayer.Optional,
                minimumSafeTiles,
                "In-memory socket authoring row.");
        }

        public static MicrochunkSide ParseSide(string token)
        {
            switch (RequireSideToken(token))
            {
                case "L": return MicrochunkSide.Left;
                case "R": return MicrochunkSide.Right;
                case "D": return MicrochunkSide.Down;
                case "U": return MicrochunkSide.Up;
                default: throw new ArgumentOutOfRangeException(nameof(token));
            }
        }

        public static MicrochunkTraversalKind ParseTraversalKind(string token)
        {
            switch (RequireTraversalToken(token))
            {
                case "WALK": return MicrochunkTraversalKind.Walk;
                case "DROP": return MicrochunkTraversalKind.Drop;
                case "CLIMB": return MicrochunkTraversalKind.Climb;
                case "OPTIONAL_BREAK": return MicrochunkTraversalKind.OptionalBreak;
                case "HIDDEN": return MicrochunkTraversalKind.Hidden;
                case "DECORATION": return MicrochunkTraversalKind.Decoration;
                default: throw new ArgumentOutOfRangeException(nameof(token));
            }
        }

        public static MicrochunkToolRequirement ParseToolRequirement(string token)
        {
            switch (RequireToolToken(token))
            {
                case "NONE": return MicrochunkToolRequirement.None;
                case "PICKAXE": return MicrochunkToolRequirement.Pickaxe;
                case "SHOVEL": return MicrochunkToolRequirement.Shovel;
                case "ROPE": return MicrochunkToolRequirement.Rope;
                case "EXPLOSIVE": return MicrochunkToolRequirement.Explosive;
                case "ENVIRONMENT": return MicrochunkToolRequirement.Environment;
                default: throw new ArgumentOutOfRangeException(nameof(token));
            }
        }

        private static string RequireSideToken(string value)
        {
            value = RequireCanonicalToken(value, nameof(value));
            if (value != "L" && value != "R" && value != "D" && value != "U")
            {
                throw new ArgumentException("Side must be exactly L, R, D, or U.", nameof(value));
            }
            return value;
        }

        private static string RequireTraversalToken(string value)
        {
            value = RequireCanonicalToken(value, nameof(value));
            switch (value)
            {
                case "WALK":
                case "DROP":
                case "CLIMB":
                case "OPTIONAL_BREAK":
                case "HIDDEN":
                case "DECORATION":
                    return value;
                default:
                    throw new ArgumentException("Unknown traversal-kind token.", nameof(value));
            }
        }

        private static string RequireToolToken(string value)
        {
            value = RequireCanonicalToken(value, nameof(value));
            switch (value)
            {
                case "NONE":
                case "PICKAXE":
                case "SHOVEL":
                case "ROPE":
                case "EXPLOSIVE":
                case "ENVIRONMENT":
                    return value;
                default:
                    throw new ArgumentException("Unknown tool-requirement token.", nameof(value));
            }
        }

        internal static string RequireCanonicalToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-blank canonical token is required.", parameterName);
            }
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Canonical tokens cannot contain surrounding whitespace.", parameterName);
            }
            return value;
        }
    }
}
