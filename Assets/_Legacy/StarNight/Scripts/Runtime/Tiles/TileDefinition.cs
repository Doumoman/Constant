#if LEGACY_DISABLED
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Tiles
{
    [CreateAssetMenu(
        menuName = "StarNight/Tiles/Tile Definition",
        fileName = "TileDefinition")]
    public sealed class TileDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private TileBase tile;
        [SerializeField] private TileMaterialKind materialKind;
        [SerializeField] private bool solid = true;
        [SerializeField] private TileBreakMethod breakableBy;
        [SerializeField] private bool sacred;
        [SerializeField] private bool requiredEventCore;

        public string Id => id;
        public TileBase Tile => tile;
        public TileMaterialKind MaterialKind => materialKind;
        public bool Solid => solid;
        public TileBreakMethod BreakableBy => breakableBy;
        public bool Sacred => sacred;
        public bool RequiredEventCore => requiredEventCore;
        public bool IsProtected =>
            sacred
            || requiredEventCore
            || materialKind == TileMaterialKind.ReinforcedWall
            || materialKind == TileMaterialKind.ExitFrame
            || materialKind == TileMaterialKind.RequiredEventCore;

        public void Configure(
            string definitionId,
            TileBase targetTile,
            TileMaterialKind kind,
            bool isSolid,
            TileBreakMethod allowedBreakMethods,
            bool isSacred = false,
            bool isRequiredEventCore = false)
        {
            id = definitionId;
            tile = targetTile;
            materialKind = kind;
            solid = isSolid;
            breakableBy = allowedBreakMethods;
            sacred = isSacred;
            requiredEventCore = isRequiredEventCore;
        }

        public bool CanBreak(TileBreakMethod method)
        {
            return !IsProtected
                && method != TileBreakMethod.None
                && (breakableBy & method) != 0;
        }
    }
}

#endif
