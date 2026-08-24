using UnityEngine;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 접지 판정 결과 값 객체. hit이 없거나 미확정인 값은 명시적 empty로 표현한다.
    /// </summary>
    public readonly struct CharacterGroundProbeResult
    {
        public CharacterGroundProbeResult(
            bool isGrounded,
            bool hasHit,
            Vector2 normal,
            float distance,
            int supportId)
        {
            IsGrounded = isGrounded;
            HasHit = hasHit;
            Normal = normal;
            Distance = distance;
            SupportId = supportId;
        }

        public bool IsGrounded { get; }
        public bool HasHit { get; }
        public Vector2 Normal { get; }
        public float Distance { get; }

        /// <summary>지지면 stable id. 없으면 0.</summary>
        public int SupportId { get; }

        public static CharacterGroundProbeResult NotGrounded
        {
            get { return new CharacterGroundProbeResult(false, false, Vector2.zero, 0f, 0); }
        }

        public static CharacterGroundProbeResult UngroundedHit(
            Vector2 normal, float distance, int supportId)
        {
            return new CharacterGroundProbeResult(false, true, normal, distance, supportId);
        }

        public static CharacterGroundProbeResult Grounded(
            Vector2 normal, float distance, int supportId)
        {
            return new CharacterGroundProbeResult(true, true, normal, distance, supportId);
        }
    }
}
