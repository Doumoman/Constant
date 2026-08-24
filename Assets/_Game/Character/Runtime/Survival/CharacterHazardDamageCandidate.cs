using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Survival
{
    /// <summary>
    /// 환경 위험 피해 후보 값 객체(스파이크/압착/화염/일반). 셀 좌표는
    /// 알려진 경우에만 기록하며, 라이브 물리 질의나 MAP/Tilemap 변조는
    /// 하지 않는다. Void 치명 경로는 CharacterHazardPolicy가 별도로 다룬다.
    /// </summary>
    public readonly struct CharacterHazardDamageCandidate
    {
        public CharacterHazardDamageCandidate(
            CharacterHazardKind hazardKind,
            int sourceHazardId,
            int targetId,
            CharacterSurvivalTargetKind targetKind,
            int amount,
            Vector2 direction,
            bool hasCell,
            WorldTileCoord cell)
        {
            HazardKind = hazardKind;
            SourceHazardId = sourceHazardId;
            TargetId = targetId;
            TargetKind = targetKind;
            Amount = amount;
            Direction = direction;
            HasCell = hasCell;
            Cell = cell;
        }

        public CharacterHazardKind HazardKind { get; }
        public int SourceHazardId { get; }
        public int TargetId { get; }
        public CharacterSurvivalTargetKind TargetKind { get; }
        public int Amount { get; }
        public Vector2 Direction { get; }
        public bool HasCell { get; }
        public WorldTileCoord Cell { get; }
    }
}
