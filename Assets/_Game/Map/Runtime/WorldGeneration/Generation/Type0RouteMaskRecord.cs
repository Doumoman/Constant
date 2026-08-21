using System;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class Type0RouteMaskRecord
    {
        internal Type0RouteMaskRecord(
            Type0RouteMaskId maskId,
            int routeType,
            Type0RouteOpenMask openMask,
            bool mandatoryAllowed,
            bool active,
            string descriptionKo,
            SectorRouteMaskDefinition sourceDefinition)
        {
            if (!maskId.IsValid)
            {
                throw new ArgumentException("Mask ID must be valid.", nameof(maskId));
            }

            MaskId = maskId;
            RouteType = routeType;
            OpenMask = openMask;
            MandatoryAllowed = mandatoryAllowed;
            Active = active;
            DescriptionKo = descriptionKo ?? throw new ArgumentNullException(nameof(descriptionKo));
            SourceDefinition = sourceDefinition ?? throw new ArgumentNullException(nameof(sourceDefinition));
        }

        public Type0RouteMaskId MaskId { get; }
        public int RouteType { get; }
        public Type0RouteOpenMask OpenMask { get; }
        public bool MandatoryAllowed { get; }
        public bool Active { get; }
        public string DescriptionKo { get; }
        public SectorRouteMaskDefinition SourceDefinition { get; }
    }
}
