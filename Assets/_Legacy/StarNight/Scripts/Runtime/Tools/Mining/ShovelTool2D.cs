#if LEGACY_DISABLED
using StarNight.Grid;
using StarNight.Tiles;
using UnityEngine;

namespace StarNight.Tools.Mining
{
    public sealed class ShovelTool2D : AdjacentMiningTool2D
    {
        public const int DefaultDurability = 12;

        [SerializeField]
        private TileDefinition[] additionalSoftTerrain =
            new TileDefinition[0];

        protected override int GddDefaultDurability => DefaultDurability;
        protected override TileBreakMethod BreakMethod =>
            TileBreakMethod.Shovel;

        public bool TryDigBelow(
            GridPos originCell,
            out MiningUseResult result)
        {
            return TryBeginUseFromCell(
                originCell,
                Vector2Int.down,
                out result);
        }

        public void ConfigureSoftTerrain(
            TileDefinition[] sandAshOrOtherSoftDefinitions)
        {
            additionalSoftTerrain =
                sandAshOrOtherSoftDefinitions ?? new TileDefinition[0];
        }

        protected override bool IsAllowedTerrain(TileDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            if (definition.MaterialKind == TileMaterialKind.Dirt
                || definition.MaterialKind == TileMaterialKind.Sand
                || definition.MaterialKind == TileMaterialKind.Ash)
            {
                return true;
            }

            for (int index = 0;
                 index < additionalSoftTerrain.Length;
                 index++)
            {
                if (additionalSoftTerrain[index] == definition)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif
