using TMPro;
using UnityEngine;

/// <summary>
/// 침묵의 사당 (실마레) — 결정 앞에서 아무것도 하지 않고 가만히 서서 기다리면
/// 굳어 있던 말이 풀리며 유품 '목소리 바늘'을 건넨다.
/// 움직이면 처음부터 다시. 버튼 연타로는 절대 열리지 않는다.
/// — 기획서 '관측자의 시험: 보상이 없는 침묵을 기다려 줄 수 있는가'
/// </summary>
public class SilenceShrine : MonoBehaviour
{
    [SerializeField] private string _itemId = "voiceNeedle";
    [SerializeField] private float _range = 2.4f;
    [SerializeField] private float _holdSeconds = 4f;
    [SerializeField] private TextMeshPro _label;          // 진행 안내 라벨
    [SerializeField] private SpriteRenderer _crystal;     // 결정 비주얼 (밝아지는 연출)

    private PlayerFSM _player;
    private Vector3 _lastPlayerPos;
    private float _still;
    private bool _granted;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(TextMeshPro label, SpriteRenderer crystal)
    {
        _label = label;
        _crystal = crystal;
    }

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerFSM>();
        if (_label != null) _label.text = "…";
    }

    private void Update()
    {
        if (_granted) return;

        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerFSM>();
            if (_player == null) return;
        }

        Vector3 pos = _player.transform.position;
        bool inRange = Vector2.Distance(pos, transform.position) <= _range;
        bool still = (pos - _lastPlayerPos).sqrMagnitude < 0.0001f;
        _lastPlayerPos = pos;

        if (inRange && still && !_player.IsDead)
        {
            _still += Time.deltaTime;

            if (_label != null)
                _label.text = $"침묵… {Mathf.CeilToInt(Mathf.Max(0f, _holdSeconds - _still))}";
            if (_crystal != null)
                _crystal.color = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.6f), _still / _holdSeconds);

            if (_still >= _holdSeconds)
                Grant();
        }
        else
        {
            if (_still > 0.1f && _label != null && inRange)
                _label.text = "…처음부터.";
            else if (_label != null && !inRange)
                _label.text = "…";
            _still = 0f;
        }
    }

    private void Grant()
    {
        _granted = true;

        var run = SingletonManagers.Run;
        var def = ConstantItemDb.Get(_itemId);

        if (run != null && def != null && run.TryAddItem(_itemId))
        {
            run.Toast($"[{def.displayName}] — 굳어 있던 말이 조용히 손에 떨어졌다");
            run.AddObserverNote("실마레: 보상 없는 침묵을 끝까지 기다렸다.");
        }
        else
        {
            run?.Toast("여행가방이 가득 차 목소리를 받을 수 없었다…");
            _granted = false; // 가방 정리 후 재시도 가능
            _still = 0f;
            return;
        }

        if (_label != null) _label.text = "";
        if (_crystal != null) _crystal.color = new Color(1f, 0.92f, 0.6f);
    }
}
