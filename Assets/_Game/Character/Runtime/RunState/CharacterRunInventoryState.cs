using UnityEngine;

namespace StarNight.Character.RunState
{
    /// <summary>
    /// 불변 런 인벤토리 상태(폭탄/로프 소모품 수량 —
    /// CHARACTER_INVENTORY_SCHEMA의 bombCount/ropeCount, 휴대 슬롯과 별개).
    /// </summary>
    public readonly struct CharacterRunInventoryState
    {
        public CharacterRunInventoryState(
            int actorId,
            int bombCount,
            int ropeCount)
        {
            ActorId = actorId;
            BombCount = Mathf.Max(0, bombCount);
            RopeCount = Mathf.Max(0, ropeCount);
        }

        public int ActorId { get; }
        public int BombCount { get; }
        public int RopeCount { get; }

        /// <summary>중앙 설정의 시작 수량(폭탄 4/로프 4 기준선)으로 생성.</summary>
        public static CharacterRunInventoryState CreateStarting(
            int actorId,
            in CharacterRunStateSettings settings)
        {
            return new CharacterRunInventoryState(
                actorId,
                settings.StartingBombCount,
                settings.StartingRopeCount);
        }
    }
}
