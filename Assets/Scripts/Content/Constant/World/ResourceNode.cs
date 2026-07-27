using UnityEngine;

/// <summary>
/// 자원 노드 — 상호작용(X)으로 출항 게이지를 충전한다.
/// 채집 횟수가 소진되면 어두워지고 더 이상 반응하지 않는다.
/// </summary>
public class ResourceNode : ConstantInteractable
{
    [SerializeField] private string _resourceName = "자원";
    [SerializeField] private float _gaugePerHarvest = 20f;
    [SerializeField] private int _charges = 1;

    protected override bool CanInteract => _charges > 0;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(string resourceName, float gaugePerHarvest, int charges)
    {
        _resourceName = resourceName;
        _gaugePerHarvest = gaugePerHarvest;
        _charges = charges;
    }

    protected override void OnInteract()
    {
        if (_charges <= 0) return;

        var run = SingletonManagers.Run;
        if (run == null) return;

        _charges--;

        float gained = run.AddGauge(_gaugePerHarvest);
        run.Toast($"{_resourceName} 채집 — 출항 게이지 +{gained:0}%");

        // 살짝 움츠러드는 피드백
        transform.localScale *= 0.92f;

        if (_charges <= 0)
            SetDimmed(true);
    }

    protected override void UpdateHighlight(bool near)
    {
        if (_charges <= 0) { SetDimmed(true); return; }
        base.UpdateHighlight(near);
    }
}
