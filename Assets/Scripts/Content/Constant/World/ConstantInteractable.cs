using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Constant 상호작용 오브젝트 베이스.
/// 플레이어가 감지 범위 안에 있을 때 Interact(X) 입력을 받으면 OnInteract 를 호출한다.
/// 근접 시 살짝 밝아지는 하이라이트로 상호작용 가능함을 알린다.
///
/// X 입력은 개별 구독이 아니라 정적 디스패처가 처리한다 —
/// 범위 안에 여러 오브젝트가 있어도 "가장 가까운, 상호작용 가능한" 것 하나만 발동한다.
/// (시작 아이템 3택1이 한 번에 모두 집히는 문제 방지)
/// </summary>
public abstract class ConstantInteractable : MonoBehaviour
{
    private static readonly List<ConstantInteractable> _registry = new List<ConstantInteractable>();

    [SerializeField] protected float _detectionRange = 1.6f;
    [SerializeField] protected SpriteRenderer _highlightTarget; // 비우면 자신/자식에서 탐색

    private PlayerFSM _player;
    private bool _playerNear;
    private Color _baseColor = Color.white;
    private bool _hasBaseColor;

    protected PlayerFSM Player => _player;
    protected bool PlayerNear => _playerNear;

    /// <summary>지금 이 오브젝트가 상호작용을 받을 수 있는가 (소진된 노드/집힌 아이템은 false).</summary>
    protected virtual bool CanInteract => true;

    protected virtual void OnEnable()
    {
        if (!_registry.Contains(this))
            _registry.Add(this);

        // 정적 디스패처 구독 (정적 메서드라 -=/+= 로 멱등하게 재구독)
        var input = SingletonManagers.Input;
        if (input != null)
        {
            input.OnInteractPressed -= Dispatch;
            input.OnInteractPressed += Dispatch;
        }
    }

    protected virtual void OnDisable()
    {
        _registry.Remove(this);
    }

    protected virtual void Start()
    {
        _player = FindFirstObjectByType<PlayerFSM>();

        if (_highlightTarget == null)
            _highlightTarget = GetComponentInChildren<SpriteRenderer>();
        if (_highlightTarget != null)
        {
            _baseColor = _highlightTarget.color;
            _hasBaseColor = true;
        }
    }

    protected virtual void Update()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerFSM>();
            if (_player == null) return;
        }

        bool near = Vector2.Distance(_player.transform.position, transform.position) <= _detectionRange;
        if (near != _playerNear)
        {
            _playerNear = near;
            UpdateHighlight(near);
        }
    }

    /// <summary>X 입력 시 호출 — 범위 안에서 가장 가까운, 상호작용 가능한 것 하나만 실행.</summary>
    private static void Dispatch()
    {
        PlayerFSM player = FindFirstObjectByType<PlayerFSM>();
        if (player == null || player.IsDead) return;

        // 팝업이 열려 있으면 월드 상호작용 금지 (여행가방/확인창 등)
        if (SingletonManagers.UI != null && SingletonManagers.UI.PopupCount > 0) return;

        Vector2 playerPos = player.transform.position;
        ConstantInteractable best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _registry.Count; i++)
        {
            ConstantInteractable it = _registry[i];
            if (it == null || !it.isActiveAndEnabled || !it.CanInteract) continue;

            float d = Vector2.Distance(playerPos, it.transform.position);
            if (d <= it._detectionRange && d < bestDist)
            {
                best = it;
                bestDist = d;
            }
        }

        if (best != null)
            best.OnInteract();
    }

    /// <summary>범위 안에서 X 키를 눌렀을 때 (디스패처가 가장 가까운 것만 호출).</summary>
    protected abstract void OnInteract();

    protected virtual void UpdateHighlight(bool near)
    {
        if (!_hasBaseColor || _highlightTarget == null) return;
        _highlightTarget.color = near ? _baseColor * 1.25f : _baseColor;
    }

    /// <summary>소진/비활성 연출 — 어둡게.</summary>
    protected void SetDimmed(bool dimmed)
    {
        if (!_hasBaseColor || _highlightTarget == null) return;
        _highlightTarget.color = dimmed ? _baseColor * 0.45f : _baseColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
    }
}
