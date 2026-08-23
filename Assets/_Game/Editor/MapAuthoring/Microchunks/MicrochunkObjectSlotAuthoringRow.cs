using System;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkObjectSlotAuthoringRow
    {
        public const string DefaultOrientationToken = "NONE";
        public const string DefaultMarkerCode = "NONE";

        public string SlotId { get; }
        public MicrochunkLocalCoord Anchor { get; }
        public string CategoryToken { get; }
        public string PoolId { get; }
        public string OrientationToken { get; }
        public bool Required { get; }
        public bool VisibleFromRoute { get; }
        public int SafetyRadiusTiles { get; }
        public string RequiredMarkerCode { get; }

        public MicrochunkObjectSlotAuthoringRow(
            string slotId,
            int anchorX,
            int anchorY,
            string categoryToken,
            string poolId,
            string orientationToken = DefaultOrientationToken,
            int safetyRadiusTiles = 0,
            bool required = false,
            bool visibleFromRoute = true,
            string requiredMarkerCode = DefaultMarkerCode)
        {
            SlotId = MicrochunkSocketAuthoringRow.RequireCanonicalToken(slotId, nameof(slotId));
            Anchor = new MicrochunkLocalCoord(anchorX, anchorY);
            CategoryToken = RequireCategoryToken(categoryToken);
            PoolId = MicrochunkSocketAuthoringRow.RequireCanonicalToken(poolId, nameof(poolId));
            OrientationToken = RequireOrientationToken(orientationToken);
            if (safetyRadiusTiles < 0) throw new ArgumentOutOfRangeException(nameof(safetyRadiusTiles));
            SafetyRadiusTiles = safetyRadiusTiles;
            Required = required;
            VisibleFromRoute = visibleFromRoute;
            RequiredMarkerCode = MicrochunkSocketAuthoringRow.RequireCanonicalToken(
                requiredMarkerCode,
                nameof(requiredMarkerCode));
        }

        public MicrochunkObjectSlotAuthoringRow Duplicate(string slotId)
        {
            return new MicrochunkObjectSlotAuthoringRow(
                slotId,
                Anchor.X,
                Anchor.Y,
                CategoryToken,
                PoolId,
                OrientationToken,
                SafetyRadiusTiles,
                Required,
                VisibleFromRoute,
                RequiredMarkerCode);
        }

        public MicrochunkObjectSlotDefinition ToRuntimeDefinition()
        {
            return new MicrochunkObjectSlotDefinition(
                SlotId,
                Anchor,
                ParseCategory(CategoryToken),
                PoolId,
                Required,
                ParseOrientation(OrientationToken),
                VisibleFromRoute,
                SafetyRadiusTiles,
                RequiredMarkerCode,
                "In-memory object-slot authoring row.");
        }

        public static MicrochunkSlotCategory ParseCategory(string token)
        {
            switch (RequireCategoryToken(token))
            {
                case "RESOURCE": return MicrochunkSlotCategory.Resource;
                case "MAP_ELEMENT": return MicrochunkSlotCategory.MapElement;
                case "ENEMY": return MicrochunkSlotCategory.Enemy;
                case "REWARD": return MicrochunkSlotCategory.Reward;
                case "NPC": return MicrochunkSlotCategory.Npc;
                case "SHOP_ITEM": return MicrochunkSlotCategory.ShopItem;
                case "EVENT_TRIGGER": return MicrochunkSlotCategory.EventTrigger;
                case "SPECIAL_ITEM": return MicrochunkSlotCategory.SpecialItem;
                case "DECORATION": return MicrochunkSlotCategory.Decoration;
                default: throw new ArgumentOutOfRangeException(nameof(token));
            }
        }

        public static MicrochunkObjectOrientation ParseOrientation(string token)
        {
            switch (RequireOrientationToken(token))
            {
                case "NONE": return MicrochunkObjectOrientation.None;
                case "L": return MicrochunkObjectOrientation.Left;
                case "R": return MicrochunkObjectOrientation.Right;
                case "U": return MicrochunkObjectOrientation.Up;
                case "D": return MicrochunkObjectOrientation.Down;
                default: throw new ArgumentOutOfRangeException(nameof(token));
            }
        }

        private static string RequireCategoryToken(string value)
        {
            value = MicrochunkSocketAuthoringRow.RequireCanonicalToken(value, nameof(value));
            switch (value)
            {
                case "RESOURCE":
                case "MAP_ELEMENT":
                case "ENEMY":
                case "REWARD":
                case "NPC":
                case "SHOP_ITEM":
                case "EVENT_TRIGGER":
                case "SPECIAL_ITEM":
                case "DECORATION":
                    return value;
                default:
                    throw new ArgumentException("Unknown object-slot category token.", nameof(value));
            }
        }

        private static string RequireOrientationToken(string value)
        {
            value = MicrochunkSocketAuthoringRow.RequireCanonicalToken(value, nameof(value));
            if (value != "NONE" && value != "L" && value != "R" && value != "U" && value != "D")
            {
                throw new ArgumentException("Orientation must be exactly NONE, L, R, U, or D.", nameof(value));
            }
            return value;
        }
    }
}
