using UnityEngine;

namespace StarNight.Character.Survival
{
    /// <summary>
    /// 불변 체력 상태 값 객체. 정책은 항상 새 상태를 반환하며 입력 상태를
    /// 변조하지 않는다.
    /// </summary>
    public readonly struct CharacterHealthState
    {
        public CharacterHealthState(
            int actorId,
            CharacterSurvivalTargetKind targetKind,
            int currentHealth,
            int maxHealth,
            float invulnerabilityRemainingSeconds)
        {
            ActorId = actorId;
            TargetKind = targetKind;
            MaxHealth = Mathf.Max(1, maxHealth);
            CurrentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
            InvulnerabilityRemainingSeconds =
                Mathf.Max(0f, invulnerabilityRemainingSeconds);
        }

        public int ActorId { get; }
        public CharacterSurvivalTargetKind TargetKind { get; }
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public float InvulnerabilityRemainingSeconds { get; }

        public bool IsInvulnerable
        {
            get { return InvulnerabilityRemainingSeconds > 0f; }
        }

        public bool IsDepleted
        {
            get { return CurrentHealth <= 0; }
        }

        /// <summary>가득 찬 체력으로 시작하는 상태.</summary>
        public static CharacterHealthState CreateFull(
            int actorId,
            CharacterSurvivalTargetKind targetKind,
            int maxHealth)
        {
            return new CharacterHealthState(
                actorId, targetKind, maxHealth, maxHealth, 0f);
        }

        /// <summary>무적 시간 경과(음수 delta는 0으로 clamp) — 새 상태 반환.</summary>
        public CharacterHealthState TickInvulnerability(float deltaSeconds)
        {
            float delta = Mathf.Max(0f, deltaSeconds);
            return new CharacterHealthState(
                ActorId,
                TargetKind,
                CurrentHealth,
                MaxHealth,
                InvulnerabilityRemainingSeconds - delta);
        }
    }
}
