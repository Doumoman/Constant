namespace StarNight.Character.Combat
{
    /// <summary>
    /// 밟기의 플레이어 반동 요청 값 객체. 적 상태를 직접 변조하지 않는다
    /// (적 결과와 분리). 반동 속도는 설정에서 중앙 관리된다.
    /// </summary>
    public readonly struct CharacterStompReboundRequest
    {
        public CharacterStompReboundRequest(float reboundVerticalVelocity)
        {
            ReboundVerticalVelocity = reboundVerticalVelocity;
        }

        public float ReboundVerticalVelocity { get; }
    }
}
