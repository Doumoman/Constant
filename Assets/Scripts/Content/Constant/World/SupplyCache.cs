using UnityEngine;

/// <summary>보급 상자 — 1회, 로프/폭탄 보급. (이벤트 방)</summary>
public class SupplyCache : ConstantInteractable
{
    [SerializeField] private int _ropes = 2;
    [SerializeField] private int _bombs = 2;

    private bool _used;
    protected override bool CanInteract => !_used;

    protected override void OnInteract()
    {
        if (_used) return;
        _used = true;

        var run = SingletonManagers.Run;
        run?.AddRope(_ropes);
        run?.AddBomb(_bombs);
        run?.Toast($"보급 상자 개봉 — 로프 +{_ropes}, 폭탄 +{_bombs}");
        SetDimmed(true);
    }
}
