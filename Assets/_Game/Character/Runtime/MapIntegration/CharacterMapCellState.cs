using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Character.MapIntegration
{
    /// <summary>
    /// 캐릭터 관점의 셀 상태 값 객체. 타일 의미의 원천은 MAP 공용
    /// <see cref="MicrochunkTileLayer"/>이며(06_CHARACTER_MAP_INTEGRATION_RULES),
    /// Decoration/Marker 계열은 비충돌 오버레이라 empty로 해석한다.
    /// Breakable은 파괴 가능한 고체다(solid + breakable).
    /// </summary>
    public readonly struct CharacterMapCellState
    {
        public CharacterMapCellState(
            bool isSolid,
            bool isOneWay,
            bool isHazard,
            bool isLiquid,
            bool isBreakable)
        {
            IsSolid = isSolid;
            IsOneWay = isOneWay;
            IsHazard = isHazard;
            IsLiquid = isLiquid;
            IsBreakable = isBreakable;
        }

        public bool IsSolid { get; }
        public bool IsOneWay { get; }
        public bool IsHazard { get; }
        public bool IsLiquid { get; }
        public bool IsBreakable { get; }

        /// <summary>모든 게임플레이 플래그가 없으면 empty/passable이다.</summary>
        public bool IsEmpty
        {
            get { return !IsSolid && !IsOneWay && !IsHazard && !IsLiquid && !IsBreakable; }
        }

        public static CharacterMapCellState Empty
        {
            get { return new CharacterMapCellState(false, false, false, false, false); }
        }

        /// <summary>MAP 타일 레이어 하나를 캐릭터 관점 플래그로 해석한다.</summary>
        public static CharacterMapCellState FromTileLayer(MicrochunkTileLayer layer)
        {
            switch (layer)
            {
                case MicrochunkTileLayer.GroundSolid:
                    return new CharacterMapCellState(true, false, false, false, false);
                case MicrochunkTileLayer.OneWay:
                    return new CharacterMapCellState(false, true, false, false, false);
                case MicrochunkTileLayer.Breakable:
                    return new CharacterMapCellState(true, false, false, false, true);
                case MicrochunkTileLayer.Hazard:
                    return new CharacterMapCellState(false, false, true, false, false);
                case MicrochunkTileLayer.Liquid:
                    return new CharacterMapCellState(false, false, false, true, false);
                case MicrochunkTileLayer.DecorationBack:
                case MicrochunkTileLayer.DecorationFront:
                case MicrochunkTileLayer.Marker:
                default:
                    return Empty;
            }
        }

        /// <summary>여러 레이어가 겹친 셀의 플래그 합성.</summary>
        public CharacterMapCellState Combine(CharacterMapCellState other)
        {
            return new CharacterMapCellState(
                IsSolid || other.IsSolid,
                IsOneWay || other.IsOneWay,
                IsHazard || other.IsHazard,
                IsLiquid || other.IsLiquid,
                IsBreakable || other.IsBreakable);
        }
    }
}
