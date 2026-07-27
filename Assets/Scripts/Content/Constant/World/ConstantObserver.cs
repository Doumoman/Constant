using System.Collections;
using UnityEngine;

/// <summary>
/// 관측자 — 행성마다 다른 모습(안내 방송/자판기/탐사자/검표원)으로 나타나는 같은 존재.
/// X 로 말을 걸면 해당 Yarn 노드를 재생한다. 재대화 가능.
/// 허브 인트로는 새 런의 첫 방문에 1회 자동 재생된다.
/// </summary>
public class ConstantObserver : ConstantInteractable
{
    [SerializeField] private string _nodeName;
    [SerializeField] private bool _autoPlayHubIntro; // 허브 안내 방송: 런 시작 시 1회 자동
    [SerializeField] private float _autoPlayDelay = 0.8f;

    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(string nodeName, bool autoPlayHubIntro, float detectionRange)
    {
        _nodeName = nodeName;
        _autoPlayHubIntro = autoPlayHubIntro;
        _detectionRange = detectionRange;
    }

    protected override void Start()
    {
        base.Start();

        if (_autoPlayHubIntro)
            StartCoroutine(CoAutoPlay());
    }

    private IEnumerator CoAutoPlay()
    {
        // 바인더의 RegisterRunner 와 매니저 초기화가 끝나기를 기다린다
        yield return new WaitForSeconds(_autoPlayDelay);

        var run = SingletonManagers.Run;
        if (run == null || run.HubIntroPlayed || run.StarterChosen) yield break;

        run.HubIntroPlayed = true;
        Play();
    }

    protected override void OnInteract()
    {
        Play();
    }

    private void Play()
    {
        var story = SingletonManagers.Story;
        if (story == null || story.IsRunning) return;
        if (!story.HasNode(_nodeName)) return;

        // 대화 중 게임플레이 입력 잠금 — 노드의 block_player_input 커맨드와 같은 방향(멱등)
        SingletonManagers.Input.SetInputModeUI(true);
        _ = story.StartStory(_nodeName, onComplete: () =>
        {
            if (SingletonManagers.UI == null || SingletonManagers.UI.PopupCount == 0)
                SingletonManagers.Input.SetInputModeUI(false);
        });
    }
}
