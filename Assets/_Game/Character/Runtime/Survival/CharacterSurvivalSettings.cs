using System;

namespace StarNight.Character.Survival
{
    /// <summary>
    /// 생존 중앙 설정. 최대 체력 4·피격 후 무적 0.8s는 레거시
    /// RunState(health=4)/PlayerGridContract(VoidRecoveryInvulnerabilitySeconds
    /// =0.8f) 선례를 따른 기준선이다.
    /// </summary>
    public readonly struct CharacterSurvivalSettings
    {
        public CharacterSurvivalSettings(
            int maxPlayerHealth,
            float postHitInvulnerabilitySeconds)
        {
            if (maxPlayerHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPlayerHealth));
            }

            if (postHitInvulnerabilitySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(postHitInvulnerabilitySeconds));
            }

            MaxPlayerHealth = maxPlayerHealth;
            PostHitInvulnerabilitySeconds = postHitInvulnerabilitySeconds;
        }

        public int MaxPlayerHealth { get; }
        public float PostHitInvulnerabilitySeconds { get; }

        public static CharacterSurvivalSettings Default
        {
            get { return new CharacterSurvivalSettings(4, 0.8f); }
        }
    }
}
