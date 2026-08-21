using System;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteMaskRecord
    {
        public MandatoryRouteMaskRecord(
            MandatoryRouteMaskId maskId,
            MandatoryRouteMaskKind kind,
            int routeType,
            MandatoryRouteOpenMask openMask,
            bool mandatoryAllowed,
            bool active,
            string descriptionKo,
            SectorRouteMaskDefinition sourceDefinition)
        {
            if (!maskId.IsValid) throw new ArgumentException("Mask ID must be valid.", nameof(maskId));
            if (kind != MandatoryRouteMaskKind.Type1 && kind != MandatoryRouteMaskKind.Type2 && kind != MandatoryRouteMaskKind.Type3)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (routeType != (int)kind + 1) throw new ArgumentException("Route type must match kind.", nameof(routeType));
            if (!Matches(kind, openMask)) throw new ArgumentException("Open mask must match kind.", nameof(openMask));
            if (!mandatoryAllowed || !active) throw new ArgumentException("Mandatory records must be active and mandatory-allowed.");
            MaskId = maskId;
            Kind = kind;
            RouteType = routeType;
            OpenMask = openMask;
            MandatoryAllowed = mandatoryAllowed;
            Active = active;
            DescriptionKo = descriptionKo ?? string.Empty;
            SourceDefinition = sourceDefinition ?? throw new ArgumentNullException(nameof(sourceDefinition));
        }

        public MandatoryRouteMaskId MaskId { get; }
        public MandatoryRouteMaskKind Kind { get; }
        public int RouteType { get; }
        public MandatoryRouteOpenMask OpenMask { get; }
        public bool MandatoryAllowed { get; }
        public bool Active { get; }
        public string DescriptionKo { get; }
        public SectorRouteMaskDefinition SourceDefinition { get; }

        internal static bool Matches(MandatoryRouteMaskKind kind, MandatoryRouteOpenMask mask)
        {
            switch (kind)
            {
                case MandatoryRouteMaskKind.Type1: return mask == MandatoryRouteOpenMask.Type1Horizontal;
                case MandatoryRouteMaskKind.Type2: return mask == MandatoryRouteOpenMask.Type2Down;
                case MandatoryRouteMaskKind.Type3: return mask == MandatoryRouteOpenMask.Type3Up;
                default: return false;
            }
        }
    }
}
