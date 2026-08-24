using UnityEngine;

namespace StarNight.Character.Equipment
{
    /// <summary>폭발 피해 판정용 대상 스냅샷(적 또는 플레이어).</summary>
    public readonly struct CharacterExplosionTargetSnapshot
    {
        public CharacterExplosionTargetSnapshot(
            int targetId,
            bool isPlayer,
            Vector2 position)
        {
            TargetId = targetId;
            IsPlayer = isPlayer;
            Position = position;
        }

        public int TargetId { get; }
        public bool IsPlayer { get; }
        public Vector2 Position { get; }
    }
}
