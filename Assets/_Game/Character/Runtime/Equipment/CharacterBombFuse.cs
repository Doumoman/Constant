using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 폭탄 퓨즈 모델(결정적·clamp 안전). 남은 시간이 0 이하가 되는 순간
    /// 정확히 한 번 폭발 요청을 발행한다. 시간은 주입식이며 Animator·물리
    /// 콜백에 의존하지 않는다.
    /// </summary>
    public sealed class CharacterBombFuse
    {
        private readonly CharacterBombSettings settings;

        public CharacterBombFuse(
            int bombId,
            int ownerId,
            WorldTileCoord cell,
            CharacterBombSettings settings)
        {
            BombId = bombId;
            OwnerId = ownerId;
            Cell = cell;
            this.settings = settings;
            RemainingFuseSeconds = settings.FuseSeconds;
        }

        public int BombId { get; }
        public int OwnerId { get; }
        public WorldTileCoord Cell { get; }
        public float RemainingFuseSeconds { get; private set; }
        public bool HasExploded { get; private set; }

        public float ExplosionRadiusCells
        {
            get { return settings.ExplosionRadiusCells; }
        }

        /// <summary>
        /// 퓨즈 진행. 남은 시간이 0 이하로 떨어지는 틱에서 정확히 한 번
        /// 폭발 요청을 반환한다(이후 틱은 항상 false). 음수 delta는 0으로 clamp.
        /// </summary>
        public bool Tick(float deltaSeconds, out CharacterExplosionRequest explosionRequest)
        {
            explosionRequest = default(CharacterExplosionRequest);

            if (HasExploded)
            {
                return false;
            }

            float clampedDelta = Math.Max(0f, deltaSeconds);
            RemainingFuseSeconds = Math.Max(0f, RemainingFuseSeconds - clampedDelta);

            if (RemainingFuseSeconds > 0f)
            {
                return false;
            }

            HasExploded = true;
            explosionRequest = new CharacterExplosionRequest(
                BombId,
                OwnerId,
                Cell,
                settings.ExplosionRadiusCells,
                settings.ExplosionDamageAmount);
            return true;
        }
    }
}
