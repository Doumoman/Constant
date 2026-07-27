using UnityEngine;

/// <summary>
/// Constant 씬(허브/행성) 부트스트랩.
/// - RunManager 에 현재 행성 통지 + HUD 표시
/// - 시너지 버프를 플레이어에 적용 (가방 변경 시 재적용)
/// - Space(Talk) 키로 여행가방 팝업 열기
/// - 사망 감시: 부활(보험) 또는 런 종료 팝업
/// - 허브 귀환 시 여정 완료면 엔딩 팝업
/// </summary>
public class ConstantSceneController : MonoBehaviour
{
    [SerializeField] private ConstantPlanet _planet = ConstantPlanet.Hub;
    [SerializeField] private Vector3 _respawnPoint; // 부활 지점 (기본: 플레이어 시작 위치)

    private PlayerFSM _player;
    private PlayerTaste _taste;
    private PlayerVision _vision;

    // 버프 기준값 (프리팹 기본값 저장 후 배율 적용 — 재적용에도 안전)
    private float _baseMoveSpeed;
    private float _baseJumpSpeed;
    private float _baseVisionRadius;
    private bool _baseCached;

    private bool _deathHandled;
    private float _deathTimer;
    [SerializeField] private float _reviveDelay = 0.6f;

    /// <summary>런타임 생성기가 스폰 위치를 알려준다.</summary>
    public void SetRespawnPoint(Vector3 point) => _respawnPoint = point;

    private void Start()
    {
        if (_planet == ConstantPlanet.Hub)
            ConstantSceneArtDirector.DecorateHub();

        var run = SingletonManagers.Run;
        run?.NotifyPlanetLoaded(_planet);

        _player = FindFirstObjectByType<PlayerFSM>();
        if (_player != null)
        {
            _taste = _player.GetComponent<PlayerTaste>();
            if (_respawnPoint == Vector3.zero)
                _respawnPoint = _player.transform.position;
        }
        _vision = FindFirstObjectByType<PlayerVision>();

        // HUD — 씬 전환 시 이전 HUD 는 파괴되지만 UIManager._sceneUI 는 파괴된 참조를 붙들고 있다.
        // 파괴된 UnityEngine.Object 는 'is' 검사에 여전히 true 를 반환하므로, Unity 의
        // null 오버로드(== null)로 파괴 여부를 판정해 매 씬 확실히 재생성한다.
        var ui = SingletonManagers.UI;
        if (ui != null)
        {
            UI_Scene current = ui.CurrentSceneUI;
            bool liveHud = current != null && current is UI_Scene_ConstantHUD;
            if (!liveHud)
                ui.ShowSceneUI<UI_Scene_ConstantHUD>();
        }

        CacheBaseStats();
        ApplySynergy();

        if (run != null)
        {
            run.OnInventoryChanged -= ApplySynergy;
            run.OnInventoryChanged += ApplySynergy;
        }

        var input = SingletonManagers.Input;
        if (input != null)
        {
            input.OnTalkPressed -= HandleOpenSuitcase;
            input.OnTalkPressed += HandleOpenSuitcase;
        }

        // 허브 귀환 + 여정 완료 → 엔딩 (대화 바인더 등록을 기다렸다가 진입)
        if (_planet == ConstantPlanet.Hub && run != null && run.IsJourneyComplete)
            StartCoroutine(CoEnterEnding(run));
    }

    private void OnDestroy()
    {
        var run = SingletonManagers.Run;
        if (run != null)
            run.OnInventoryChanged -= ApplySynergy;

        var input = SingletonManagers.Input;
        if (input != null)
            input.OnTalkPressed -= HandleOpenSuitcase;
    }

    // ─────────────────────────────────────────────────────────────
    // 시너지 버프 적용
    // ─────────────────────────────────────────────────────────────

    private void CacheBaseStats()
    {
        if (_player == null || _baseCached) return;
        _baseMoveSpeed = _player.PlayerData.moveSpeed;
        _baseJumpSpeed = _player.PlayerData.jumpSpeed;
        _baseVisionRadius = _vision != null ? _vision.BaseRadius : 0f;
        _baseCached = true;
    }

    private void ApplySynergy()
    {
        var run = SingletonManagers.Run;
        if (run == null || _player == null || !_baseCached) return;

        var synergy = run.Synergy;
        _player.PlayerData.moveSpeed = _baseMoveSpeed * synergy.moveSpeedMul;
        _player.PlayerData.jumpSpeed = _baseJumpSpeed * synergy.jumpMul;

        if (_vision != null)
            _vision.BaseRadius = _baseVisionRadius + synergy.visionBonus;

        // 용암 면역: 열충격 결정 조합 → 작열 효과를 상시 유지
        if (_taste != null)
        {
            if (synergy.lavaImmune)
                _taste.Eat(TasteKind.Heat, float.MaxValue);
            else if (_taste.HasHeat)
                _taste.ClearTaste();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 여행가방 열기 (Space)
    // ─────────────────────────────────────────────────────────────

    private void HandleOpenSuitcase()
    {
        if (SingletonManagers.UI == null || SingletonManagers.UI.PopupCount > 0) return;
        if (_player != null && _player.IsDead) return;

        Time.timeScale = 0f;
        SingletonManagers.Input.SetInputModeUI(true);
        SingletonManagers.UI.ShowPopupUI<UI_Popup_Suitcase>();
    }

    // ─────────────────────────────────────────────────────────────
    // 사망 처리 — 부활 or 런 종료
    // ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_player == null) return;

        // 허공 낙하 가드 — 폭탄으로 외벽을 뚫고 방 밖 허공으로 떨어진 경우
        if (!_player.IsDead && _player.transform.position.y < -6f)
        {
            SingletonManagers.Run?.Toast("우주의 밑바닥은 없었다…");
            _player.Kill();
        }

        if (!_player.IsDead)
        {
            _deathHandled = false;
            _deathTimer = 0f;
            return;
        }

        if (_deathHandled) return;

        _deathTimer += Time.unscaledDeltaTime;
        if (_deathTimer < _reviveDelay) return;

        _deathHandled = true;

        var run = SingletonManagers.Run;
        if (run != null && run.TryUseRevive())
        {
            _player.Respawn(_respawnPoint);
            ApplySynergy(); // 부활 후 버프 재적용 (작열 등)
            return;
        }

        ShowRunOver(run);
    }

    private void ShowRunOver(RunManager run)
    {
        Time.timeScale = 0f;
        SingletonManagers.Input.SetInputModeUI(true);

        var popup = SingletonManagers.UI.ShowPopupUI<UI_Popup_RunResult>();
        if (popup == null) return;

        int planets = run != null ? run.PlanetsCleared : 0;
        int items = run != null ? run.ItemCount() : 0;
        popup.SetResult(
            "여정이 여기서 끝났다",
            $"통과한 행성 {planets}개 · 모은 여행용품 {items}개\n" +
            "초저가 패키지에 환불 규정은 없다.\n다음 판에 남는 것은 지식뿐이다.");
    }

    private System.Collections.IEnumerator CoEnterEnding(RunManager run)
    {
        yield return new WaitForSeconds(0.6f); // ConstantStoryBinder.RegisterRunner 대기
        ShowEnding(run);
    }

    private void ShowEnding(RunManager run)
    {
        // 관측자의 최종 통신이 준비되어 있으면 먼저 재생하고, 끝나면 정산(팝업)한다.
        var story = SingletonManagers.Story;
        if (story != null && !story.IsRunning && story.HasNode("Obs_Return"))
        {
            SingletonManagers.Input.SetInputModeUI(true);
            _ = story.StartStory("Obs_Return", onComplete: () => ShowEndingPopup(run));
            return;
        }

        ShowEndingPopup(run);
    }

    private void ShowEndingPopup(RunManager run)
    {
        Time.timeScale = 0f;
        SingletonManagers.Input.SetInputModeUI(true);

        var popup = SingletonManagers.UI.ShowPopupUI<UI_Popup_RunResult>();
        if (popup == null) return;

        int codex = SingletonManagers.Data?.CurrentData?.unlockedRecipes.Count ?? 0;
        string desc =
            $"세 행성을 지나 무사히 여행선으로 돌아왔다.\n" +
            $"모은 여행용품 {run.ItemCount()}개 · 도감 발견 {codex}/{ConstantItemDb.Recipes.Count}\n" +
            "초저가 우주여행은 전혀 싸지 않았다.";

        // 관측자의 기록 — 여행 중의 선택이 문장으로 남는다
        if (run.ObserverNotes.Count > 0)
        {
            desc += "\n\n<size=80%><color=#9aa2b5>관측 기록:";
            foreach (string note in run.ObserverNotes)
                desc += $"\n· {note}";
            desc += "</color></size>";
        }

        if (run.HasAllRelics())
            desc += "\n\n…그런데 짐 사이에서, 미등록 목적지의 티켓이\n소리 없이 인쇄되기 시작했다.";

        popup.SetResult("관광객 엔딩 — 여행의 끝", desc);
    }
}
