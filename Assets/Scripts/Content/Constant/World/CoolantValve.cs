using UnityEngine;

/// <summary>
/// 냉각 밸브 — 라베르니스 주민 의뢰의 단계. X로 돌린다 (1회).
/// 보호핵이 뜯긴 뒤에는 얼어붙어 작동하지 않는다.
/// </summary>
public class CoolantValve : ConstantInteractable
{
    [SerializeField] private ValveQuest _quest;

    private bool _turned;

    protected override bool CanInteract => !_turned;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(ValveQuest quest) => _quest = quest;

    protected override void OnInteract()
    {
        if (_turned || _quest == null) return;

        if (_quest.IsLocked)
        {
            SingletonManagers.Run?.Toast("밸브가 얼어붙었다 — 보호핵이 사라진 배관은 다시 돌지 않는다.");
            return;
        }

        _turned = true;
        _quest.NotifyValveTurned();

        // 가동 연출: 파랗게 물들며 약간 회전
        if (_highlightTarget != null)
            _highlightTarget.color = new Color(0.5f, 0.8f, 1f);
        transform.Rotate(0f, 0f, 45f);
    }

    protected override void UpdateHighlight(bool near)
    {
        if (_turned) return;
        base.UpdateHighlight(near);
    }
}
