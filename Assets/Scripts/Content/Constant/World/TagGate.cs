using UnityEngine;

/// <summary>
/// 도구 로직 게이트 — 여행가방의 태그 구성이 세계를 여는 열쇠가 된다.
/// X 상호작용 시 가방에 요구 성질 태그 아이템이 충분하면:
///  - Break 모드: 장애물(_blocker)을 파괴한다 (예: 열기 x2 → 균열 암벽 발파)
///  - Bridge 모드: 다리(_bridge)를 생성한다 (예: 냉기 x2 → 가시밭 위 얼음 다리)
/// 부족하면 현재 보유 수를 알려 준다 — "무엇을 채워야 하는가"가 곧 가방의 목적이 된다.
/// </summary>
public class TagGate : ConstantInteractable
{
    [SerializeField] private string _gateName = "봉인";
    [SerializeField] private PropertyTag _requiredTag = PropertyTag.Heat;
    [SerializeField] private int _requiredCount = 2;
    [SerializeField] private GameObject _blocker;  // Break: 성공 시 비활성
    [SerializeField] private GameObject _bridge;   // Bridge: 성공 시 활성
    [SerializeField] private string _successToast = "길이 열렸다!";

    private bool _done;

    protected override bool CanInteract => !_done;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(string gateName, PropertyTag tag, int count,
        GameObject blocker, GameObject bridge, string successToast)
    {
        _gateName = gateName;
        _requiredTag = tag;
        _requiredCount = count;
        _blocker = blocker;
        _bridge = bridge;
        _successToast = successToast;
    }

    protected override void OnInteract()
    {
        if (_done) return;

        var run = SingletonManagers.Run;
        if (run == null) return;

        int have = run.CountPropertyTag(_requiredTag);
        string tagName = ConstantDefine.NameOf(_requiredTag);

        if (have < _requiredCount)
        {
            run.Toast($"{_gateName} — [{tagName}] 태그 아이템이 더 필요하다 ({have}/{_requiredCount})");
            return;
        }

        _done = true;

        if (_blocker != null) _blocker.SetActive(false);
        if (_bridge != null) _bridge.SetActive(true);

        run.Toast($"{_successToast} ([{tagName}] {_requiredCount}개 사용 — 아이템은 소모되지 않는다)");
        SetDimmed(true);
    }
}
