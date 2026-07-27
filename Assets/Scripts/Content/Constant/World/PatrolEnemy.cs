using UnityEngine;

/// <summary>
/// 순찰 몹 — 지정 구간을 좌우로 오가며, 닿으면 플레이어를 죽인다.
/// 전투 시스템이 없는 게임이라 '움직이는 해저드'다: 점프로 넘거나 타이밍으로 피한다.
/// (부활 조합 '여행자 보험'의 가치가 올라간다)
/// </summary>
public class PatrolEnemy : MonoBehaviour
{
    [SerializeField] private float _minX;
    [SerializeField] private float _maxX;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _killRange = 0.75f;
    [SerializeField] private string _killMessage = "무언가에게 붙잡혔다…";

    private int _dir = 1;
    private PlayerFSM _player;
    private Transform _visual;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(float minX, float maxX, float speed)
    {
        _minX = minX;
        _maxX = maxX;
        _speed = speed;
    }

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerFSM>();
        _visual = transform.childCount > 0 ? transform.GetChild(0) : transform;

        if (_maxX <= _minX) // 미설정 보호
        {
            _minX = transform.position.x - 3f;
            _maxX = transform.position.x + 3f;
        }
    }

    private float _stunnedUntil;

    /// <summary>기절 — 화염 메아리 폭죽/절대 영점 등.</summary>
    public void Stun(float seconds) => _stunnedUntil = Time.time + seconds;

    private void Update()
    {
        if (Time.time < _stunnedUntil) return; // 기절 중

        // 자장가 주전자/고요한 수면 패시브 — 감속
        float speedMul = SingletonManagers.Run != null && SingletonManagers.Run.Synergy.slowMobs ? 0.7f : 1f;

        // 순찰
        Vector3 pos = transform.position;
        pos.x += _dir * _speed * speedMul * Time.deltaTime;

        if (pos.x >= _maxX) { pos.x = _maxX; _dir = -1; }
        else if (pos.x <= _minX) { pos.x = _minX; _dir = 1; }

        transform.position = pos;

        // 진행 방향 바라보기 (스케일 부호)
        if (_visual != null)
        {
            Vector3 s = _visual.localScale;
            s.x = Mathf.Abs(s.x) * _dir;
            _visual.localScale = s;
        }

        // 접촉 판정
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerFSM>();
            if (_player == null) return;
        }

        if (!_player.IsDead &&
            Vector2.Distance(_player.transform.position, transform.position) <= _killRange)
        {
            SingletonManagers.Run?.Toast(_killMessage);
            _player.Kill();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(_minX, transform.position.y), new Vector3(_maxX, transform.position.y));
        Gizmos.DrawWireSphere(transform.position, _killRange);
    }
}
