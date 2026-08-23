using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public static class MicrochunkTileLayerRules
    {
        public const string ForbiddenPairReason = "UNLISTED_NON_DECORATION_PAIR";

        public static MicrochunkTileLayerRuleResult ValidateCell(MicrochunkTileCell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));

            var occupancy = MicrochunkTileLayerOccupancy.FromCell(cell);
            return new MicrochunkTileLayerRuleResult(1, CollectViolations(occupancy));
        }

        public static MicrochunkTileLayerRuleResult ValidateDefinition(MicrochunkDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            var violations = new List<MicrochunkTileLayerRuleViolation>();
            foreach (var cell in definition.TileCells)
            {
                violations.AddRange(CollectViolations(MicrochunkTileLayerOccupancy.FromCell(cell)));
            }

            return new MicrochunkTileLayerRuleResult(definition.TileCells.Count, violations);
        }

        private static List<MicrochunkTileLayerRuleViolation> CollectViolations(
            MicrochunkTileLayerOccupancy occupancy)
        {
            var violations = new List<MicrochunkTileLayerRuleViolation>();
            var layers = occupancy.OccupiedLayers;
            for (var firstIndex = 0; firstIndex < layers.Count; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1; secondIndex < layers.Count; secondIndex++)
                {
                    var first = layers[firstIndex];
                    var second = layers[secondIndex];
                    if (IsAllowed(first, second)) continue;

                    violations.Add(new MicrochunkTileLayerRuleViolation(
                        occupancy.Coordinate,
                        first,
                        second,
                        occupancy.GetCode(first),
                        occupancy.GetCode(second),
                        ForbiddenPairReason));
                }
            }

            return violations;
        }

        private static bool IsAllowed(MicrochunkTileLayer first, MicrochunkTileLayer second)
        {
            if (IsDecoration(first) || IsDecoration(second)) return true;

            if (first == MicrochunkTileLayer.Marker) return MarkerCanOverlay(second);
            if (second == MicrochunkTileLayer.Marker) return MarkerCanOverlay(first);
            return false;
        }

        private static bool IsDecoration(MicrochunkTileLayer layer)
        {
            return layer == MicrochunkTileLayer.DecorationBack ||
                   layer == MicrochunkTileLayer.DecorationFront;
        }

        private static bool MarkerCanOverlay(MicrochunkTileLayer layer)
        {
            return layer == MicrochunkTileLayer.GroundSolid ||
                   layer == MicrochunkTileLayer.OneWay ||
                   layer == MicrochunkTileLayer.Breakable ||
                   layer == MicrochunkTileLayer.Hazard;
        }
    }
}
