using UnityEngine;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 충돌 질의 결과 값 객체. 테스트 fake world가 Unity scene 없이
    /// 자유롭게 구성할 수 있다. collider 참조 대신 stable id를 담는다.
    /// </summary>
    public readonly struct CharacterCollisionHit
    {
        public CharacterCollisionHit(
            bool hasHit,
            Vector2 point,
            Vector2 normal,
            float distance,
            int colliderId)
        {
            HasHit = hasHit;
            Point = point;
            Normal = normal;
            Distance = distance;
            ColliderId = colliderId;
        }

        public bool HasHit { get; }
        public Vector2 Point { get; }
        public Vector2 Normal { get; }
        public float Distance { get; }

        /// <summary>충돌체 stable id. 없으면 0.</summary>
        public int ColliderId { get; }

        public static CharacterCollisionHit None
        {
            get { return new CharacterCollisionHit(false, Vector2.zero, Vector2.zero, 0f, 0); }
        }
    }
}
