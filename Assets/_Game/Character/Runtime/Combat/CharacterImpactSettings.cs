using System;

namespace StarNight.Character.Combat
{
    /// <summary>
    /// 임팩트 판정 튜닝. 최소 임팩트 속도는 여기서만 중앙 관리·검증된다.
    /// 기본값은 기준선이며 수치 검증은 이후 코스 검증 소관이다.
    /// </summary>
    public readonly struct CharacterImpactSettings
    {
        public CharacterImpactSettings(
            float minimumImpactSpeed,
            int thrownEnemyDamageAmount)
        {
            if (minimumImpactSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumImpactSpeed), "최소 임팩트 속도는 0보다 커야 한다.");
            }

            if (thrownEnemyDamageAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(thrownEnemyDamageAmount), "투척 피해량은 0보다 커야 한다.");
            }

            MinimumImpactSpeed = minimumImpactSpeed;
            ThrownEnemyDamageAmount = thrownEnemyDamageAmount;
        }

        public float MinimumImpactSpeed { get; }
        public int ThrownEnemyDamageAmount { get; }

        public static CharacterImpactSettings Default
        {
            get { return new CharacterImpactSettings(1.5f, 1); }
        }
    }
}
