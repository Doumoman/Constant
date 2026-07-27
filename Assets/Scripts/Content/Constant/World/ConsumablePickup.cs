using UnityEngine;

/// <summary>소모품 픽업 — 로프/폭탄 (즉시 획득).</summary>
public class ConsumablePickup : ConstantInteractable
{
    [SerializeField] private bool _isBomb;
    [SerializeField] private int _amount = 1;

    private bool _taken;
    protected override bool CanInteract => !_taken;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(bool isBomb, int amount)
    {
        _isBomb = isBomb;
        _amount = amount;
    }

    protected override void OnInteract()
    {
        if (_taken) return;
        _taken = true;

        var run = SingletonManagers.Run;
        if (_isBomb) { run?.AddBomb(_amount); run?.Toast($"폭탄 +{_amount}"); }
        else { run?.AddRope(_amount); run?.Toast($"로프 +{_amount}"); }

        gameObject.SetActive(false);
    }
}
