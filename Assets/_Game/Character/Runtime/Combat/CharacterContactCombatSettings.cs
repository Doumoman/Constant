using System;

namespace StarNight.Character.Combat
{
    /// <summary>
    /// 접촉 전투 튜닝. 밟기 반동 속도는 여기서만 중앙 관리·검증된다.
    /// 기본값은 기준선이며 수치 검증은 이후 코스 검증 소관이다.
    /// </summary>
    public readonly struct CharacterContactCombatSettings
    {
        public CharacterContactCombatSettings(
            float stompReboundVelocity,
            float stunDurationSeconds,
            int contactDamageAmount)
        {
            if (stompReboundVelocity <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stompReboundVelocity), "밟기 반동 속도는 0보다 커야 한다.");
            }

            if (stunDurationSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stunDurationSeconds), "기절 시간은 0 이상이어야 한다.");
            }

            if (contactDamageAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contactDamageAmount), "접촉 피해량은 0보다 커야 한다.");
            }

            StompReboundVelocity = stompReboundVelocity;
            StunDurationSeconds = stunDurationSeconds;
            ContactDamageAmount = contactDamageAmount;
        }

        public float StompReboundVelocity { get; }
        public float StunDurationSeconds { get; }
        public int ContactDamageAmount { get; }

        public static CharacterContactCombatSettings Default
        {
            get { return new CharacterContactCombatSettings(6f, 5f, 1); }
        }
    }
}
