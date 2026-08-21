using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkObjectSlotDefinition
    {
        public string SlotId { get; }
        public MicrochunkLocalCoord Anchor { get; }
        public MicrochunkSlotCategory Category { get; }
        public string AllowedPoolId { get; }
        public bool Required { get; }
        public MicrochunkObjectOrientation Orientation { get; }
        public bool VisibleFromRoute { get; }
        public int ForbiddenRadiusTiles { get; }
        public string RequiredMarkerCode { get; }
        public string Notes { get; }

        public MicrochunkObjectSlotDefinition(
            string slotId,
            MicrochunkLocalCoord anchor,
            MicrochunkSlotCategory category,
            string allowedPoolId,
            bool required,
            MicrochunkObjectOrientation orientation,
            bool visibleFromRoute,
            int forbiddenRadiusTiles,
            string requiredMarkerCode,
            string notes)
        {
            if (!Enum.IsDefined(typeof(MicrochunkSlotCategory), category)) throw new ArgumentOutOfRangeException(nameof(category));
            if (!Enum.IsDefined(typeof(MicrochunkObjectOrientation), orientation)) throw new ArgumentOutOfRangeException(nameof(orientation));
            if (forbiddenRadiusTiles < 0) throw new ArgumentOutOfRangeException(nameof(forbiddenRadiusTiles));

            SlotId = RequireToken(slotId, nameof(slotId));
            Anchor = anchor;
            Category = category;
            AllowedPoolId = RequireToken(allowedPoolId, nameof(allowedPoolId));
            Required = required;
            Orientation = orientation;
            VisibleFromRoute = visibleFromRoute;
            ForbiddenRadiusTiles = forbiddenRadiusTiles;
            RequiredMarkerCode = RequireToken(requiredMarkerCode, nameof(requiredMarkerCode));
            Notes = notes ?? string.Empty;
        }

        private static string RequireToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Object-slot IDs and tokens cannot be null, empty, or whitespace.", parameterName);
            }

            return value;
        }
    }
}
