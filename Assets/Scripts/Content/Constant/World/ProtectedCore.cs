using UnityEngine;

/// <summary>
/// 보호핵 — 주민들이 지키는 냉각의 심장. X로 뜯으면 출항 게이지가 크게 차지만(빠른 길),
/// 냉각 의뢰가 영구히 잠기고 유품 '냉각 코어 조각'도 사라진다.
/// 관측자는 이 선택을 기록한다.
/// </summary>
public class ProtectedCore : ConstantInteractable
{
    [SerializeField] private ValveQuest _quest;
    [SerializeField] private float _gaugeReward = 50f;

    private bool _ripped;

    protected override bool CanInteract => !_ripped;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(ValveQuest quest) => _quest = quest;

    protected override void OnInteract()
    {
        if (_ripped) return;
        _ripped = true;

        var run = SingletonManagers.Run;
        float gained = run != null ? run.AddGauge(_gaugeReward) : 0f;

        run?.Toast($"보호핵을 뜯어냈다 — 출항 게이지 +{gained:0}%. 배관의 소리가 잦아든다…");
        run?.AddObserverNote("라베르니스: 주민들이 지키던 보호핵을 뜯어냈다.");

        if (_quest != null)
            _quest.NotifyCoreRipped();

        SetDimmed(true);
        transform.localScale *= 0.85f;
    }

    protected override void UpdateHighlight(bool near)
    {
        if (_ripped) { SetDimmed(true); return; }
        base.UpdateHighlight(near);
    }
}
