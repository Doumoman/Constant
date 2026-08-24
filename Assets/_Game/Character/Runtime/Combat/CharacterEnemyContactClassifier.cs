using UnityEngine;

namespace StarNight.Character.Combat
{
    /// <summary>
    /// 플레이어-적 접촉의 결정적 기하 분류. AABB 겹침과 상대 중심 오프셋으로
    /// 상단/측면/하단을 판정하고, 상단 접촉은 플레이어가 하강 중일 때만 유효
    /// 밟기다. Animator 이벤트나 Unity 물리 콜백을 판정 권한으로 삼지 않는다
    /// (콜백은 이후 입력으로만 소비될 수 있다).
    /// </summary>
    public static class CharacterEnemyContactClassifier
    {
        public static CharacterContactClassification Classify(
            Vector2 playerCenter,
            Vector2 playerHalfSize,
            float playerVerticalVelocity,
            Vector2 enemyCenter,
            Vector2 enemyHalfSize)
        {
            float deltaX = playerCenter.x - enemyCenter.x;
            float deltaY = playerCenter.y - enemyCenter.y;
            float overlapX = playerHalfSize.x + enemyHalfSize.x - Mathf.Abs(deltaX);
            float overlapY = playerHalfSize.y + enemyHalfSize.y - Mathf.Abs(deltaY);

            // 분리 상태 — 전투 이벤트 없음.
            if (overlapX <= 0f || overlapY <= 0f)
            {
                return CharacterContactClassification.None;
            }

            // 세로 겹침이 더 얕으면 상/하 접촉, 아니면 측면 접촉(결정적).
            if (overlapY <= overlapX)
            {
                if (deltaY > 0f)
                {
                    bool descending = playerVerticalVelocity < 0f;
                    return new CharacterContactClassification(
                        CharacterContactSide.Top, descending);
                }

                return new CharacterContactClassification(
                    CharacterContactSide.Bottom, false);
            }

            return new CharacterContactClassification(CharacterContactSide.Side, false);
        }
    }
}
