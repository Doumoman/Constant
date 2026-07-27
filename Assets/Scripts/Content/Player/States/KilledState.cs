using UnityEngine;

/// <summary>
/// 사망 상태.
/// 모든 입력 비활성화, 사망 애니메이션 재생.
/// 다른 State로의 전이 없음 — 부활은 외부에서 FSM 리셋으로 처리.
/// </summary>
public class KilledState : PlayerBaseState
{
    public KilledState(PlayerFSM fsm) : base(fsm) { }

    public override void Enter()
    {

    }

    public override void Update()
    {

    }

    public override void Exit()
    {

    }
}
