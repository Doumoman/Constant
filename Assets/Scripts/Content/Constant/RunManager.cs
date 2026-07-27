using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Constant 단판(런) 상태 매니저.
/// 현재 행성, 출항 게이지, 여행가방(4x3) 배치, 시너지 캐시를 관리한다.
/// 씬을 넘어도 유지되며(SingletonManagers 소속), 런 종료 시 StartNewRun 으로 초기화한다.
/// </summary>
public class RunManager : IManager
{
    public const int GridWidth = 4;
    public const int GridHeight = 3;
    public const int GridSize = GridWidth * GridHeight;
    public const float BaseGaugeRequired = 100f;
    public const int StagesPerPlanet = 3; // 라베르니스-1/-2/-3 식 스테이지

    /// <summary>이번 행성의 출항 요구치 — 기본 100 + 여행사 대출 빚.</summary>
    public float GaugeRequired => BaseGaugeRequired + _debt;

    private bool _init = false;

    private ConstantPlanet _currentPlanet = ConstantPlanet.Hub;
    private float _gauge;
    private string[] _slots = new string[GridSize];  // defId, 빈 칸 = null
    private int[] _slotUses = new int[GridSize];     // 제작(사용형) 아이템의 남은 사용 횟수
    private bool _starterChosen;
    private int _revivesSpent;
    private int _planetsCleared;
    private ConstantSynergy _synergy = new ConstantSynergy();
    private readonly List<string> _observerNotes = new List<string>();

    /// <summary>허브 안내 방송(관측자 인트로) 1회 재생 여부 — 새 런마다 리셋.</summary>
    public bool HubIntroPlayed { get; set; }

    // ── 소모품 / 빚 (탈출 도구 & 여행사 대출) ──
    private int _ropes;
    private int _bombs;
    private float _debt;          // 대출 빚 — 행성 요구 게이지에 가산
    private float _pendingDebt;   // 다음 행성부터 적용될 빚

    /// <summary>소모품 수량 변경 (ropes, bombs).</summary>
    public event Action<int, int> OnConsumablesChanged;

    public int Ropes => _ropes;
    public int Bombs => _bombs;
    public float Debt => _debt;

    public void AddRope(int n) { _ropes += n; OnConsumablesChanged?.Invoke(_ropes, _bombs); }
    public void AddBomb(int n) { _bombs += n; OnConsumablesChanged?.Invoke(_ropes, _bombs); }

    public bool UseRope()
    {
        if (_ropes <= 0) return false;
        _ropes--;
        OnConsumablesChanged?.Invoke(_ropes, _bombs);
        return true;
    }

    public bool UseBomb()
    {
        if (_bombs <= 0) return false;
        _bombs--;
        OnConsumablesChanged?.Invoke(_ropes, _bombs);
        return true;
    }

    /// <summary>게이지를 화폐로 소비 (상점). 부족하면 false.</summary>
    public bool SpendGauge(float amount)
    {
        if (_gauge < amount) return false;
        _gauge -= amount;
        OnGaugeChanged?.Invoke(_gauge, GaugeRequired);
        return true;
    }

    /// <summary>여행사 대출 — 즉시 게이지를 받고, 다음 행성부터 요구치가 올라간다.</summary>
    public void TakeLoan(float gaugeNow, float debtAfter)
    {
        _gauge = Mathf.Min(_gauge + gaugeNow, GaugeRequired);
        _pendingDebt += debtAfter;
        OnGaugeChanged?.Invoke(_gauge, GaugeRequired);
    }

    /// <summary>복제기 — 가방의 무작위 아이템 하나를 복제한다 (빈 칸 필요). 성공 시 복제된 아이템 id.</summary>
    public string DuplicateRandomItem()
    {
        var candidates = new List<int>();
        for (int i = 0; i < GridSize; i++)
            if (_slots[i] != null) candidates.Add(i);
        if (candidates.Count == 0) return null;

        string id = _slots[candidates[UnityEngine.Random.Range(0, candidates.Count)]];
        return TryAddItem(id) ? id : null;
    }

    /// <summary>출항 게이지 변경 (current, required).</summary>
    public event Action<float, float> OnGaugeChanged;
    /// <summary>가방 내용/배치 변경 — 시너지 재계산 완료 후 발화.</summary>
    public event Action OnInventoryChanged;
    /// <summary>HUD 토스트 메시지 요청.</summary>
    public event Action<string> OnToast;

    public ConstantPlanet CurrentPlanet => _currentPlanet;

    /// <summary>행성 내 스테이지 (1..StagesPerPlanet). HUD 표기: 라베르니스-2.</summary>
    public int StageIndex { get; private set; } = 1;

    public float Gauge => _gauge;
    public bool StarterChosen => _starterChosen;
    public int PlanetsCleared => _planetsCleared;
    public ConstantSynergy Synergy => _synergy;
    public int RevivesLeft => Mathf.Max(0, _synergy.revives - _revivesSpent);

    /// <summary>관측자의 기록 — 여행 중 플레이어의 선택(공감/자율성/목적주의)이 문장으로 쌓인다. 엔딩에 출력.</summary>
    public IReadOnlyList<string> ObserverNotes => _observerNotes;

    public void AddObserverNote(string note)
    {
        if (string.IsNullOrEmpty(note) || _observerNotes.Contains(note)) return;
        _observerNotes.Add(note);
    }

    public void Init()
    {
        if (_init) return;
        _init = true;
        StartNewRun();
    }

    public void Clear()
    {
        OnGaugeChanged = null;
        OnInventoryChanged = null;
        OnToast = null;
    }

    public void OnDestroy() { }

    // ─────────────────────────────────────────────────────────────
    // 런 흐름
    // ─────────────────────────────────────────────────────────────

    /// <summary>새 여행 시작 — 가방/게이지/진행 전부 초기화.</summary>
    public void StartNewRun()
    {
        _currentPlanet = ConstantPlanet.Hub;
        StageIndex = 1;
        _gauge = 0f;
        _slots = new string[GridSize];
        _slotUses = new int[GridSize];
        _starterChosen = false;
        _revivesSpent = 0;
        _planetsCleared = 0;
        _observerNotes.Clear();
        HubIntroPlayed = false;
        _ropes = 2;
        _bombs = 2;
        _debt = 0f;
        _pendingDebt = 0f;
        OnConsumablesChanged?.Invoke(_ropes, _bombs);
        RecomputeSynergy();
    }

    /// <summary>씬 컨트롤러가 씬 로드 직후 호출. 행성별 게이지를 리셋한다.</summary>
    public void NotifyPlanetLoaded(ConstantPlanet planet)
    {
        _currentPlanet = planet;
        _gauge = 0f;
        _debt = _pendingDebt; // 대출 빚은 다음 행성부터 청구된다

        // 보급형 패시브 (온천 배낭/열선 배선 등)
        if (planet != ConstantPlanet.Hub)
        {
            if (_synergy.stageRope > 0) _ropes += _synergy.stageRope;
            if (_synergy.stageBomb > 0) _bombs += _synergy.stageBomb;
        }

        OnGaugeChanged?.Invoke(_gauge, GaugeRequired);
        OnConsumablesChanged?.Invoke(_ropes, _bombs);
    }

    /// <summary>
    /// 출항 — 행성 내 다음 스테이지(-1/-2/-3)로, 마지막 스테이지면 다음 행성으로.
    /// 같은 씬을 다시 로드해도 스테이지 부트스트랩이 새 랜덤 맵을 생성한다.
    /// </summary>
    public void DepartToNextPlanet()
    {
        // 행성 내 스테이지 진행
        if (_currentPlanet != ConstantPlanet.Hub && StageIndex < StagesPerPlanet)
        {
            StageIndex++;
            LoadPlanetScene(_currentPlanet); // 같은 행성 — 새 맵
            return;
        }

        int idx = Array.IndexOf(ConstantDefine.PlanetOrder, _currentPlanet);

        if (_currentPlanet != ConstantPlanet.Hub)
            _planetsCleared++;

        StageIndex = 1;
        bool isLast = idx < 0 || idx >= ConstantDefine.PlanetOrder.Length - 1;
        ConstantPlanet next = isLast
            ? ConstantPlanet.Hub // 마지막 행성 → 귀환
            : ConstantDefine.PlanetOrder[idx + 1];

        LoadPlanetScene(next);
    }

    /// <summary>런 종료(사망/엔딩) 후 새 여행 시작 — 상태 초기화 + 허브 로드.</summary>
    public void RestartJourney()
    {
        StartNewRun();
        LoadPlanetScene(ConstantPlanet.Hub);
    }

    private void LoadPlanetScene(ConstantPlanet planet)
    {
        // 씬 전환 전 정리 규약: 팝업 전부 닫기 + 타임스케일/입력 복원
        Time.timeScale = 1f;
        SingletonManagers.UI?.CloseAllPopupUI();
        SingletonManagers.Input?.SetInputModeUI(false);

        SceneManager.LoadScene(ConstantDefine.SceneNameOf(planet));
    }

    /// <summary>여정 전체(3행성) 완료 여부 — 귀환한 허브에서 엔딩 연출용.</summary>
    public bool IsJourneyComplete =>
        _planetsCleared >= ConstantDefine.PlanetOrder.Length - 1;

    /// <summary>사망 시 부활 시도. 성공하면 true (횟수 차감).</summary>
    public bool TryUseRevive()
    {
        if (RevivesLeft <= 0) return false;
        _revivesSpent++;
        Toast($"여행자 보험이 발동했다! (남은 부활 {RevivesLeft}회)");
        return true;
    }

    // ─────────────────────────────────────────────────────────────
    // 출항 게이지
    // ─────────────────────────────────────────────────────────────

    /// <summary>게이지 충전. 시너지 채집 배율이 적용된 실제 충전량을 반환.</summary>
    public float AddGauge(float baseAmount)
    {
        float amount = baseAmount * _synergy.gatherMul;
        _gauge = Mathf.Clamp(_gauge + amount, 0f, GaugeRequired);
        OnGaugeChanged?.Invoke(_gauge, GaugeRequired);
        return amount;
    }

    public bool IsGaugeFull => _gauge >= GaugeRequired - 0.01f;

    // ─────────────────────────────────────────────────────────────
    // 여행가방
    // ─────────────────────────────────────────────────────────────

    public string GetSlot(int index) =>
        index >= 0 && index < GridSize ? _slots[index] : null;

    public int GetSlotUses(int index) =>
        index >= 0 && index < GridSize ? _slotUses[index] : 0;

    /// <summary>도감 해금 여부.</summary>
    public bool IsRecipeUnlocked(string resultId)
    {
        var data = SingletonManagers.Data?.CurrentData;
        return data != null && data.unlockedRecipes.Contains(resultId);
    }

    /// <summary>
    /// 자동 조합 — 가방 배치가 레시피 패턴과 일치하면 재료를 소모해 결과를 만든다.
    /// 처음 만드는 레시피는 그 순간 도감에 해금(발견=지식)되어 저장된다.
    /// </summary>
    private void TryAutoCraft()
    {
        for (int pass = 0; pass < 6; pass++) // 연쇄 조합 허용
        {
            bool crafted = false;
            foreach (var recipe in ConstantItemDb.Recipes)
            {
                int found = FindPattern(recipe, out int slotA, out int slotB);
                if (found < 0) continue;

                // 발견 = 해금 (메타 저장)
                bool firstDiscovery = !IsRecipeUnlocked(recipe.resultId);
                if (firstDiscovery)
                {
                    var data = SingletonManagers.Data?.CurrentData;
                    if (data != null)
                    {
                        data.unlockedRecipes.Add(recipe.resultId);
                        SingletonManagers.Data.SaveGame();
                    }
                }

                var result = ConstantItemDb.Get(recipe.resultId);
                _slots[slotA] = recipe.resultId;
                _slotUses[slotA] = result.isActive ? result.uses : 0;
                _slots[slotB] = null;
                _slotUses[slotB] = 0;

                Toast(firstDiscovery
                    ? $"[도감 해금!] {result.displayName} — {result.useHint}"
                    : $"조합 완성: [{result.displayName}] ({result.RarityLabel})");

                crafted = true;
                break; // 한 번에 하나씩 (재스캔)
            }
            if (!crafted) break;
        }
    }

    /// <summary>패턴 탐색 — A 슬롯 기준 상대 위치의 B. 매치 시 0, 실패 시 -1.</summary>
    private int FindPattern(RecipeDef recipe, out int slotA, out int slotB)
    {
        slotA = -1; slotB = -1;
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                int a = y * GridWidth + x;
                if (_slots[a] != recipe.aId) continue;

                int b = -1;
                switch (recipe.shape)
                {
                    case RecipeShape.Horizontal: if (x + 1 < GridWidth) b = a + 1; break;
                    case RecipeShape.Vertical: if (y + 1 < GridHeight) b = a + GridWidth; break;
                    case RecipeShape.Diagonal: if (x + 1 < GridWidth && y + 1 < GridHeight) b = a + GridWidth + 1; break;
                }
                if (b < 0 || _slots[b] != recipe.bId) continue;
                if (a == b) continue;

                slotA = a; slotB = b;
                return 0;
            }
        }
        return -1;
    }

    /// <summary>사용형 제작 아이템 사용 (여행가방에서 F). 성공 시 effectId 반환.</summary>
    public string UseCraftedAt(int index)
    {
        var def = ConstantItemDb.Get(GetSlot(index));
        if (def == null || !def.isCrafted || !def.isActive) return null;
        if (_slotUses[index] <= 0) return null;

        _slotUses[index]--;
        string effect = def.effectId;
        float power = def.effectPower;

        if (_slotUses[index] <= 0)
        {
            _slots[index] = null;
            _slotUses[index] = 0;
            Toast($"[{def.displayName}] 소진 — 마지막 사용");
        }
        else
        {
            Toast($"[{def.displayName}] 사용 (남은 횟수 {_slotUses[index]})");
        }

        RecomputeSynergy();

        // 효과 실행은 씬 상주 컨트롤러가 담당
        var exec = UnityEngine.Object.FindFirstObjectByType<ConstantConsumableController>();
        exec?.ExecuteCraftedEffect(effect, power);
        return effect;
    }

    /// <summary>빈 칸에 아이템 추가. 가방이 가득이면 false. (배치 결과 자동 조합 검사)</summary>
    public bool TryAddItem(string defId)
    {
        var def = ConstantItemDb.Get(defId);
        if (def == null) return false;

        for (int i = 0; i < GridSize; i++)
        {
            if (_slots[i] == null)
            {
                _slots[i] = defId;
                _slotUses[i] = def.isActive ? def.uses : 0;
                TryAutoCraft();
                RecomputeSynergy();
                return true;
            }
        }
        return false;
    }

    /// <summary>두 칸의 내용을 교환(이동 포함). (배치 결과 자동 조합 검사)</summary>
    public void SwapSlots(int a, int b)
    {
        if (a < 0 || b < 0 || a >= GridSize || b >= GridSize || a == b) return;
        (_slots[a], _slots[b]) = (_slots[b], _slots[a]);
        (_slotUses[a], _slotUses[b]) = (_slotUses[b], _slotUses[a]);
        TryAutoCraft();
        RecomputeSynergy();
    }

    /// <summary>시작 아이템 선택 (허브 3택1).</summary>
    public void ChooseStarter(string defId)
    {
        if (_starterChosen) return;
        if (TryAddItem(defId))
            _starterChosen = true;
    }

    /// <summary>유품 3종(냉각 코어/목소리 바늘/명령 고리)을 모두 갖고 있는가.</summary>
    public bool HasAllRelics()
    {
        int count = 0;
        for (int i = 0; i < GridSize; i++)
        {
            var def = ConstantItemDb.Get(_slots[i]);
            if (def != null && def.isRelic) count++;
        }
        return count >= 3;
    }

    public int ItemCount()
    {
        int count = 0;
        for (int i = 0; i < GridSize; i++)
            if (_slots[i] != null) count++;
        return count;
    }

    /// <summary>가방 안의 특정 성질 태그 아이템 수 — 도구 로직(발파/얼음 다리/동력문)의 열쇠.</summary>
    public int CountPropertyTag(PropertyTag tag)
    {
        int count = 0;
        for (int i = 0; i < GridSize; i++)
        {
            var def = ConstantItemDb.Get(_slots[i]);
            if (def != null && def.property == tag) count++;
        }
        return count;
    }

    /// <summary>가방 안의 특정 행동 태그 아이템 수.</summary>
    public int CountActionTag(ActionTag tag)
    {
        int count = 0;
        for (int i = 0; i < GridSize; i++)
        {
            var def = ConstantItemDb.Get(_slots[i]);
            if (def != null && def.action == tag) count++;
        }
        return count;
    }

    private void RecomputeSynergy()
    {
        _synergy = ConstantSynergy.Compute(_slots);
        OnInventoryChanged?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────
    // 알림
    // ─────────────────────────────────────────────────────────────

    public void Toast(string message) => OnToast?.Invoke(message);
}
