using UnityEngine;

/// <summary>상점 상품 — 출항 게이지를 화폐로 지불한다. 연료를 팔아 도구를 산다. (이벤트 방)</summary>
public class ShopItem : ConstantInteractable
{
    public enum Goods { Ropes, Bombs, RandomItem }

    [SerializeField] private Goods _goods = Goods.Ropes;
    [SerializeField] private float _cost = 10f;

    private bool _sold;
    protected override bool CanInteract => !_sold;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(Goods goods, float cost)
    {
        _goods = goods;
        _cost = cost;
    }

    protected override void OnInteract()
    {
        if (_sold) return;

        var run = SingletonManagers.Run;
        if (run == null) return;

        if (!run.SpendGauge(_cost))
        {
            run.Toast($"게이지 부족 — 이 상품은 출항 게이지 {_cost:0}%가 필요하다");
            return;
        }

        switch (_goods)
        {
            case Goods.Ropes:
                run.AddRope(2);
                run.Toast($"구매: 로프 2개 (게이지 -{_cost:0}%)");
                break;
            case Goods.Bombs:
                run.AddBomb(2);
                run.Toast($"구매: 폭탄 2개 (게이지 -{_cost:0}%)");
                break;
            case Goods.RandomItem:
                var pool = ConstantItemDb.Items;
                var def = pool[Random.Range(0, pool.Count)];
                if (run.TryAddItem(def.id))
                    run.Toast($"구매: [{def.displayName}] {def.TagLabel} (게이지 -{_cost:0}%)");
                else
                {
                    run.AddGauge(_cost); // 가방 가득 — 환불
                    run.Toast("여행가방이 가득 — 거래가 취소되었다");
                    return;
                }
                break;
        }

        _sold = true;
        SetDimmed(true);
    }
}
