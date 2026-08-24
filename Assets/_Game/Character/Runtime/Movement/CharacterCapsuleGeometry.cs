using System;
using UnityEngine;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 플레이어 캡슐 기하 기준. 잠금 기준선은 0.72 × 0.90 world unit이며
    /// 1×1 지형 셀보다 작다. 셀 크기나 MAP 좌표 상수는 정의하지 않는다.
    /// </summary>
    public readonly struct CharacterCapsuleGeometry
    {
        public const float BaselineWidth = 0.72f;
        public const float BaselineHeight = 0.90f;

        public CharacterCapsuleGeometry(float width, float height)
        {
            if (width <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "캡슐 폭은 0보다 커야 한다.");
            }

            if (height <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "캡슐 높이는 0보다 커야 한다.");
            }

            Width = width;
            Height = height;
        }

        public float Width { get; }
        public float Height { get; }

        public static CharacterCapsuleGeometry Default
        {
            get { return new CharacterCapsuleGeometry(BaselineWidth, BaselineHeight); }
        }

        public Vector2 Size
        {
            get { return new Vector2(Width, Height); }
        }

        public float HalfHeight
        {
            get { return Height * 0.5f; }
        }

        /// <summary>중심점 기준 캡슐 하단 중앙. 하향 probe 계산에 사용한다.</summary>
        public Vector2 BottomCenter(Vector2 center)
        {
            return new Vector2(center.x, center.y - HalfHeight);
        }
    }
}
