using UnityEngine;

namespace StarNight.Character.Survival
{
    /// <summary>
    /// 통합 생존 피해 요청 값 객체. 접촉/임팩트/폭발/위험 후보가 전부 이
    /// 하나의 요청으로 표현된다. HUD·점수·넉백·기절·제거·사망·연출을
    /// 직접 적용하지 않는다.
    /// </summary>
    public readonly struct CharacterSurvivalDamageRequest
    {
        public CharacterSurvivalDamageRequest(
            CharacterDamageSourceKind sourceKind,
            int sourceId,
            int targetId,
            CharacterSurvivalTargetKind targetKind,
            int amount,
            Vector2 direction,
            bool bypassInvulnerability)
        {
            SourceKind = sourceKind;
            SourceId = sourceId;
            TargetId = targetId;
            TargetKind = targetKind;
            Amount = amount;
            Direction = direction;
            BypassInvulnerability = bypassInvulnerability;
        }

        public CharacterDamageSourceKind SourceKind { get; }
        public int SourceId { get; }
        public int TargetId { get; }
        public CharacterSurvivalTargetKind TargetKind { get; }
        public int Amount { get; }
        public Vector2 Direction { get; }

        /// <summary>스키마 기본 false — 명시 요청만 무적을 관통한다.</summary>
        public bool BypassInvulnerability { get; }
    }
}
