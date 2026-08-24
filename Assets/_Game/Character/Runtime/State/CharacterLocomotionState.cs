namespace StarNight.Character.State
{
    /// <summary>
    /// 이동 상태 구분. 점프 물리·중력·충돌 질의는 이 모델의 소관이 아니며
    /// 이후 Task의 모터가 이 상태를 갱신한다.
    /// 벽 점프·대시·이중 점프 상태는 존재하지 않는다.
    /// </summary>
    public enum CharacterLocomotionState
    {
        Grounded,
        Airborne
    }
}
