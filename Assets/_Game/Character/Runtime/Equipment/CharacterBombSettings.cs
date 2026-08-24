using System;

namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 폭탄 튜닝. 기본값은 기준선 — 반경 1.5셀은 중심 ±1셀(3×3 마스크,
    /// 레거시 ExplosionMask3x3 선례와 일치)을 의미한다.
    /// </summary>
    public readonly struct CharacterBombSettings
    {
        public CharacterBombSettings(
            float fuseSeconds,
            float explosionRadiusCells,
            int explosionDamageAmount)
        {
            if (fuseSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fuseSeconds), "퓨즈 시간은 0보다 커야 한다.");
            }

            if (explosionRadiusCells <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(explosionRadiusCells), "폭발 반경은 0보다 커야 한다.");
            }

            if (explosionDamageAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(explosionDamageAmount), "폭발 피해량은 0보다 커야 한다.");
            }

            FuseSeconds = fuseSeconds;
            ExplosionRadiusCells = explosionRadiusCells;
            ExplosionDamageAmount = explosionDamageAmount;
        }

        public float FuseSeconds { get; }
        public float ExplosionRadiusCells { get; }
        public int ExplosionDamageAmount { get; }

        public static CharacterBombSettings Default
        {
            get { return new CharacterBombSettings(2.5f, 1.5f, 2); }
        }
    }
}
