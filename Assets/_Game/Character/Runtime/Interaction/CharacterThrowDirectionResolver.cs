namespace StarNight.Character.Interaction
{
    /// <summary>
    /// 투척 방향 결정. 결정적 우선순위: Up이 수평보다 우선한다
    /// (위+수평 동시 입력 시 Up). 방향 입력이 없으면 투척 의도가 아니다.
    /// </summary>
    public static class CharacterThrowDirectionResolver
    {
        public static bool TryResolve(
            bool upHeld,
            float horizontalInput,
            out CharacterThrowDirection direction)
        {
            if (upHeld)
            {
                direction = CharacterThrowDirection.Up;
                return true;
            }

            if (horizontalInput > 0f)
            {
                direction = CharacterThrowDirection.Right;
                return true;
            }

            if (horizontalInput < 0f)
            {
                direction = CharacterThrowDirection.Left;
                return true;
            }

            direction = default(CharacterThrowDirection);
            return false;
        }
    }
}
