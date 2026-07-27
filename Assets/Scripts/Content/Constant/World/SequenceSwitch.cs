using UnityEngine;

/// <summary>
/// 규약 스위치 — 에이드론 규약 회로의 단계. X로 누른다.
/// 번호 순서(1→2→3)대로 눌러야 하며, 어기면 ProtocolQuest 가 전체를 리셋한다.
/// </summary>
public class SequenceSwitch : ConstantInteractable
{
    [SerializeField] private ProtocolQuest _quest;
    [SerializeField] private int _index = 1;

    private static readonly Color LitColor = new Color(0.45f, 1f, 0.6f);
    private Color _offColor = Color.white;
    private bool _lit;

    public int Index => _index;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(ProtocolQuest quest, int index)
    {
        _quest = quest;
        _index = index;
    }

    protected override void Start()
    {
        base.Start();
        if (_highlightTarget != null)
            _offColor = _highlightTarget.color;
        if (_quest != null)
            _quest.Register(this);
    }

    protected override bool CanInteract => !_lit && _quest != null && !_quest.IsDone;

    protected override void OnInteract()
    {
        _quest?.Press(this);
    }

    public void SetLit(bool lit)
    {
        _lit = lit;
        if (_highlightTarget != null)
            _highlightTarget.color = lit ? LitColor : _offColor;
    }

    protected override void UpdateHighlight(bool near)
    {
        if (_lit) return; // 켜진 상태 색 유지
        base.UpdateHighlight(near);
    }
}
