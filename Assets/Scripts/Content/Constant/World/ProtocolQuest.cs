using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 에이드론 규약 회로 — 스위치 1→2→3 을 '순서대로' 눌러야 규약 금고가 열린다.
/// 순서를 어기면 절차가 처음으로 되돌아간다(파괴 대신 규약을 따르는 길).
/// 완료 시 금고 게이트가 열리고 유품 '부서진 명령 고리' + 게이지 보상.
/// — 기획서 '회로를 복구해 시설을 순서대로 작동시키거나 감시망을 과부하시킨다'
/// </summary>
public class ProtocolQuest : MonoBehaviour
{
    [SerializeField] private GameObject _gate;        // Ground 레이어 콜라이더 게이트 (열리면 비활성, 선택)
    [SerializeField] private GameObject _rewardItem;  // 유품 픽업 (초기 비활성, 선택)
    [SerializeField] private float _gaugeReward = 30f;

    private readonly List<SequenceSwitch> _switches = new List<SequenceSwitch>();
    private int _expected = 1;
    private bool _done;

    public bool IsDone => _done;

    /// <summary>런타임 생성용 초기화.</summary>
    public void SetReward(GameObject rewardItem) => _rewardItem = rewardItem;

    public void Register(SequenceSwitch sw)
    {
        if (!_switches.Contains(sw))
            _switches.Add(sw);
    }

    public void Press(SequenceSwitch sw)
    {
        if (_done) return;

        var run = SingletonManagers.Run;

        if (sw.Index == _expected)
        {
            sw.SetLit(true);
            _expected++;

            if (_expected > 3)
            {
                _done = true;
                float gained = run != null ? run.AddGauge(_gaugeReward) : 0f;
                run?.Toast($"규약 승인 3/3 — 절차 완료! 출항 게이지 +{gained:0}%");
                run?.AddObserverNote("에이드론: 불편한 규약의 순서를 끝까지 따랐다.");

                if (_gate != null) _gate.SetActive(false);
                if (_rewardItem != null)
                {
                    _rewardItem.SetActive(true);
                    run?.Toast("마지막 스위치 곁에서 무언가가 모습을 드러냈다…");
                }
            }
            else
            {
                run?.Toast($"규약 승인 ({sw.Index}/3) — 다음 절차를 찾아라");
            }
        }
        else
        {
            // 순서 위반 — 전체 리셋
            _expected = 1;
            foreach (var s in _switches)
                s.SetLit(false);
            run?.Toast($"규약 위반 (스위치 {sw.Index}) — 절차가 처음으로 되돌아갔다");
        }
    }
}
