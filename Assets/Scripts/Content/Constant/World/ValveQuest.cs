using UnityEngine;

/// <summary>
/// 출구 개방 퀘스트 — 맵에 흩어진 밸브를 일정 수 이상 돌리면 출항 구역의 게이트가 열린다.
/// (탈출형 구조의 핵심 목표: 어느 방의 밸브를 찾으러 갈지가 곧 동선 계획이 된다)
///
/// 라베르니스 한정: '보호핵'을 뜯으면 게이트가 즉시 강제로 열리지만(빠른 길),
/// 남은 밸브가 얼어붙고 유품 '냉각 코어 조각'이 사라진다. 관측자는 이 선택을 기록한다.
/// </summary>
public class ValveQuest : MonoBehaviour
{
    [SerializeField] private int _valveGoal = 3;
    [SerializeField] private GameObject _gate;         // 출항 구역을 막는 게이트 (열리면 비활성)
    [SerializeField] private GameObject _rewardItem;   // 유품 등 보상 픽업 (초기 비활성, 선택)
    [SerializeField] private GameObject _rewardMarker; // 보상 위치 안내 라벨 (선택)

    private int _turned;
    private bool _locked;
    private bool _done;

    public bool IsLocked => _locked;
    public bool IsDone => _done;
    public int Turned => _turned;
    public int Goal => _valveGoal;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(int goal, GameObject gate, GameObject rewardItem)
    {
        _valveGoal = goal;
        _gate = gate;
        _rewardItem = rewardItem;
    }

    public void NotifyValveTurned()
    {
        if (_locked || _done) return;

        var run = SingletonManagers.Run;
        _turned++;

        if (_turned >= _valveGoal)
        {
            _done = true;
            OpenGate();
            run?.Toast($"밸브 {_turned}/{_valveGoal} — 출항 구역의 게이트가 열렸다!");

            if (_rewardItem != null)
            {
                _rewardItem.SetActive(true);
                run?.Toast("게이트 곁에서 무언가가 모습을 드러냈다…");
            }
            if (_rewardMarker != null) _rewardMarker.SetActive(true);
        }
        else
        {
            run?.Toast($"밸브 가동 ({_turned}/{_valveGoal}) — 어딘가의 배관이 웅웅거린다");
        }
    }

    /// <summary>보호핵이 뜯겼다 — 게이트 강제 개방, 남은 밸브 잠금, 유품 소실.</summary>
    public void NotifyCoreRipped()
    {
        _locked = true;
        OpenGate();

        if (!_done)
        {
            if (_rewardItem != null) _rewardItem.SetActive(false);
            if (_rewardMarker != null) _rewardMarker.SetActive(false);
        }

        SingletonManagers.Run?.Toast("보호핵의 힘이 게이트를 강제로 열었다 — 배관의 소리가 잦아든다…");
    }

    /// <summary>특이점 조각 등 외부 효과의 강제 개방.</summary>
    public void ForceOpenGate()
    {
        _done = true;
        OpenGate();
    }

    private void OpenGate()
    {
        if (_gate != null) _gate.SetActive(false);
    }
}
