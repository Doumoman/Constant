namespace StarNight.Character.Input
{
    /// <summary>
    /// 고정 논리 행동 ID. 기준 바인딩은 CharacterDesign 입력 규칙을 따른다
    /// (Jump=Space 기준선, Action=X, Bomb=Z, Rope=C).
    /// SafeDrop은 아래 방향 + Action 조합의 논리 행동이며 별도 장치 버튼이 아니다.
    /// 별도 일반 공격 행동은 존재하지 않는다.
    /// </summary>
    public enum CharacterActionId
    {
        Jump,
        Action,
        SafeDrop,
        Bomb,
        Rope
    }
}
