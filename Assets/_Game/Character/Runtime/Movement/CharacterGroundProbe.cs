using System;
using UnityEngine;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 캡슐 기준 하향 접지 판정. grounded 조건:
    /// (1) probe distance 안의 hit, (2) upward로 해석 가능한 surface normal,
    /// (3) vertical velocity가 상승 임계값 이하.
    /// query miss, 너무 먼 hit, 벽/수평 normal, 빠른 상승 상태는 grounded가 아니다.
    /// one-way/drop-through 정책은 이번 단계에서 구현하지 않으며
    /// ICharacterCollisionWorld 구현 교체로 확장한다.
    /// </summary>
    public sealed class CharacterGroundProbe
    {
        /// <summary>upward로 해석하는 최소 normal.y. 벽(수평) normal을 배제한다.</summary>
        public const float MinimumUpwardNormalY = 0.5f;

        private readonly ICharacterCollisionWorld world;
        private readonly CharacterCapsuleGeometry capsule;
        private readonly CharacterGroundProbeSettings settings;

        public CharacterGroundProbe(
            ICharacterCollisionWorld world,
            CharacterCapsuleGeometry capsule,
            CharacterGroundProbeSettings settings)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            this.world = world;
            this.capsule = capsule;
            this.settings = settings;
        }

        public CharacterCapsuleGeometry Capsule
        {
            get { return capsule; }
        }

        public CharacterGroundProbeSettings Settings
        {
            get { return settings; }
        }

        public CharacterGroundProbeResult Probe(Vector2 center, float verticalVelocity)
        {
            CharacterCollisionHit hit = world.CapsuleCast(
                center,
                capsule,
                Vector2.down,
                settings.ProbeDistance);

            if (!hit.HasHit)
            {
                return CharacterGroundProbeResult.NotGrounded;
            }

            bool withinDistance = hit.Distance <= settings.ProbeDistance;
            bool upwardNormal = hit.Normal.y >= MinimumUpwardNormalY;
            bool notRising = verticalVelocity <= settings.RisingVelocityThreshold;

            if (withinDistance && upwardNormal && notRising)
            {
                return CharacterGroundProbeResult.Grounded(hit.Normal, hit.Distance, hit.ColliderId);
            }

            return CharacterGroundProbeResult.UngroundedHit(hit.Normal, hit.Distance, hit.ColliderId);
        }
    }
}
