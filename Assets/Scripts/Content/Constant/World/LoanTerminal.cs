using UnityEngine;

/// <summary>여행사 대출 단말 — 즉시 게이지 +30, 다음 행성부터 요구치 +20. 1회. (이벤트 방)</summary>
public class LoanTerminal : ConstantInteractable
{
    [SerializeField] private float _gaugeNow = 30f;
    [SerializeField] private float _debtAfter = 20f;

    private bool _used;
    protected override bool CanInteract => !_used;

    protected override void OnInteract()
    {
        if (_used) return;
        _used = true;

        var run = SingletonManagers.Run;
        run?.TakeLoan(_gaugeNow, _debtAfter);
        run?.Toast($"여행사 대출 승인 — 게이지 +{_gaugeNow:0}%. 다음 행성부터 출항 요구치 +{_debtAfter:0}%");
        run?.AddObserverNote("여행사 대출을 받았다 — 값은 다음 좌표에서 청구된다.");
        SetDimmed(true);
    }
}
