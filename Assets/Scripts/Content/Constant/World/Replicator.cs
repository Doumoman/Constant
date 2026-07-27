using UnityEngine;

/// <summary>복제기 — 가방의 무작위 아이템 하나를 복제한다 (태그 수 증가 = 조합/도구 강화). 1회. (이벤트 방)</summary>
public class Replicator : ConstantInteractable
{
    private bool _used;
    protected override bool CanInteract => !_used;

    protected override void OnInteract()
    {
        if (_used) return;

        var run = SingletonManagers.Run;
        if (run == null) return;

        string id = run.DuplicateRandomItem();
        if (id == null)
        {
            run.Toast("복제기가 웅웅거리다 멈췄다 — 복제할 물건이 없거나 가방이 가득이다");
            return;
        }

        var def = ConstantItemDb.Get(id);
        _used = true;
        run.Toast($"복제 완료 — [{def.displayName}]이(가) 하나 더 생겼다 (무엇이 나올지는 운이었다)");
        SetDimmed(true);
    }
}
