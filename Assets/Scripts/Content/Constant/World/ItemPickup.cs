using UnityEngine;

/// <summary>
/// 아이템 픽업 — 상호작용(X)으로 여행가방에 아이템을 담는다.
/// 시작 아이템(3택1)은 하나를 선택하면 나머지가 잠긴다.
/// </summary>
public class ItemPickup : ConstantInteractable
{
    [SerializeField] private string _itemId;
    [SerializeField] private bool _isStarterChoice; // 허브의 3택1 아이템 여부

    private bool _taken;

    public string ItemId => _itemId;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(string itemId, bool isStarter)
    {
        _itemId = itemId;
        _isStarterChoice = isStarter;
    }

    protected override bool CanInteract
    {
        get
        {
            if (_taken) return false;
            // 이미 시작 아이템을 골랐다면 나머지 진열품은 상호작용 대상에서 제외
            if (_isStarterChoice)
            {
                var run = SingletonManagers.Run;
                if (run != null && run.StarterChosen) return false;
            }
            return true;
        }
    }

    protected override void Update()
    {
        base.Update();

        // 시작 아이템: 다른 것을 이미 선택했다면 잠금 연출
        if (_isStarterChoice && !_taken)
        {
            var run = SingletonManagers.Run;
            if (run != null && run.StarterChosen)
                SetDimmed(true);
        }
    }

    protected override void OnInteract()
    {
        if (_taken) return;

        var run = SingletonManagers.Run;
        var def = ConstantItemDb.Get(_itemId);
        if (run == null || def == null) return;

        if (_isStarterChoice)
        {
            if (run.StarterChosen)
            {
                run.Toast("이미 여행용품을 골랐다. 초저가 패키지는 1인 1개.");
                return;
            }
            run.ChooseStarter(_itemId);
            run.Toast($"[{def.displayName}] 선택 — {def.TagLabel}. I 키로 가방 확인");
        }
        else
        {
            if (!run.TryAddItem(_itemId))
            {
                run.Toast("여행가방이 가득 찼다! (I: 가방 정리)");
                return;
            }
            run.Toast($"[{def.displayName}] 획득 — {def.TagLabel}");
        }

        _taken = true;
        gameObject.SetActive(false);
    }

    protected override void UpdateHighlight(bool near)
    {
        var run = SingletonManagers.Run;
        if (_isStarterChoice && run != null && run.StarterChosen)
        {
            SetDimmed(true);
            return;
        }
        base.UpdateHighlight(near);
    }
}
