using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 여행가방 팝업 — 4x3 그리드에 담긴 아이템을 재배치한다.
/// 방향키로 커서 이동, Enter 로 집기/놓기(교환), ESC 로 내려놓기/닫기.
/// 인접 태그 연결과 기묘한 조합이 우측 패널에 실시간 표시된다.
/// </summary>
public class UI_Popup_Suitcase : UI_Popup
{
    enum GameObjects
    {
        Cell_0, Cell_1, Cell_2, Cell_3,
        Cell_4, Cell_5, Cell_6, Cell_7,
        Cell_8, Cell_9, Cell_10, Cell_11,
    }

    enum Texts
    {
        TitleText,
        InfoText,
        ResonanceText,
        HandText,
    }

    private static readonly Color CellEmpty = new Color(0.13f, 0.15f, 0.20f, 0.95f);
    private static readonly Color CellCursor = new Color(0.95f, 0.85f, 0.40f, 1f);
    private static readonly Color CellHand = new Color(0.40f, 0.90f, 0.75f, 1f);

    private class CellView
    {
        public Image bg;
        public Image icon;
        public TextMeshProUGUI name;
        public TextMeshProUGUI tag;
    }

    private CellView[] _cells;
    private int _cursor;
    private int _handIndex = -1; // 집어 든 칸 (-1 = 없음)

    private float _lastMoveTime;
    [SerializeField] private float _moveCooldown = 0.14f;

    public override void Init()
    {
        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        Bind<TextMeshProUGUI>(typeof(Texts));

        _cells = new CellView[RunManager.GridSize];
        for (int i = 0; i < RunManager.GridSize; i++)
        {
            GameObject go = Get<GameObject>(i);
            var view = new CellView
            {
                bg = go.GetComponent<Image>(),
                icon = go.transform.Find("Icon")?.GetComponent<Image>(),
                name = go.transform.Find("Name")?.GetComponent<TextMeshProUGUI>(),
                tag = go.transform.Find("Tag")?.GetComponent<TextMeshProUGUI>(),
            };
            _cells[i] = view;
        }

        GetText((int)Texts.TitleText).text = "여행가방";
        Refresh();
    }

    // ─────────────────────────────────────────────────────────────
    // 입력
    // ─────────────────────────────────────────────────────────────

    public override void OnInput(Vector2 dir)
    {
        if (Time.unscaledTime - _lastMoveTime < _moveCooldown) return;

        int x = _cursor % RunManager.GridWidth;
        int y = _cursor / RunManager.GridWidth;

        if (dir.x > 0.5f) x = (x + 1) % RunManager.GridWidth;
        else if (dir.x < -0.5f) x = (x + RunManager.GridWidth - 1) % RunManager.GridWidth;
        else if (dir.y > 0.5f) y = (y + RunManager.GridHeight - 1) % RunManager.GridHeight; // 위 = 윗줄
        else if (dir.y < -0.5f) y = (y + 1) % RunManager.GridHeight;
        else return;

        _cursor = y * RunManager.GridWidth + x;
        _lastMoveTime = Time.unscaledTime;
        Refresh();
    }

    public override void OnSubmit()
    {
        var run = SingletonManagers.Run;
        if (run == null) return;

        if (_handIndex < 0)
        {
            // 집기 — 빈 칸이면 무시
            if (run.GetSlot(_cursor) == null) return;
            _handIndex = _cursor;
        }
        else
        {
            // 놓기/교환
            run.SwapSlots(_handIndex, _cursor);
            _handIndex = -1;
        }
        Refresh();
    }

    public override void OnCancel()
    {
        if (_handIndex >= 0)
        {
            // 들고 있던 것을 내려놓기
            _handIndex = -1;
            Refresh();
            return;
        }

        Close();
    }

    private void Close()
    {
        ClosePopupUI();

        if (SingletonManagers.UI.PopupCount == 0)
        {
            SingletonManagers.Input.SetInputModeUI(false);
            Time.timeScale = 1f;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 그리기
    // ─────────────────────────────────────────────────────────────

    private void Refresh()
    {
        var run = SingletonManagers.Run;
        if (run == null || _cells == null) return;

        for (int i = 0; i < _cells.Length; i++)
        {
            var view = _cells[i];
            if (view == null || view.bg == null) continue;

            var def = ConstantItemDb.Get(run.GetSlot(i));

            // 배경: 커서 > 집은 칸 > 기본
            if (i == _cursor) view.bg.color = CellCursor;
            else if (i == _handIndex) view.bg.color = CellHand;
            else view.bg.color = CellEmpty;

            if (view.icon != null)
            {
                view.icon.enabled = def != null;
                if (def != null)
                {
                    Color c = ConstantDefine.ColorOf(def.property);
                    // 집어 든 아이템은 반투명하게
                    c.a = (i == _handIndex) ? 0.35f : 1f;
                    view.icon.color = c;
                }
            }

            if (view.name != null)
            {
                view.name.text = def != null ? def.displayName : "";
                view.name.color = def == null ? Color.white : def.rarity switch
                {
                    ItemRarity.Epic => new Color(0.85f, 0.6f, 1f),
                    ItemRarity.Rare => new Color(0.55f, 0.8f, 1f),
                    _ => Color.white,
                };
            }
            if (view.tag != null)
            {
                if (def == null) view.tag.text = "";
                else if (def.isCrafted && def.isActive)
                    view.tag.text = $"{def.TagLabel} · x{run.GetSlotUses(i)}";
                else
                    view.tag.text = def.TagLabel;
            }
        }

        RefreshInfo(run);
        RefreshResonance(run);

        GetText((int)Texts.HandText).text = _handIndex >= 0
            ? $"이동 중: {ConstantItemDb.Get(run.GetSlot(_handIndex))?.displayName}  (Enter 놓기 · ESC 취소)"
            : "Enter 집기 · F 사용(사용형) · ESC 닫기";
    }

    private void RefreshInfo(RunManager run)
    {
        var def = ConstantItemDb.Get(run.GetSlot(_cursor));
        if (def == null)
        {
            GetText((int)Texts.InfoText).text = "빈 칸";
            return;
        }

        string tags = $"[{def.TagLabel}] ({def.RarityLabel})";
        string extra = def.isRelic ? "  <color=#E8C56A>유품</color>" : def.isCrafted ? "  <color=#7FD8C8>제작</color>" : "";
        string hint = string.IsNullOrEmpty(def.useHint) ? "" : $"\n<color=#B8E0FF>{def.useHint}</color>";

        // 해금된 조합 힌트 — 이 재료로 만들 수 있는 것 (마크식 미리보기)
        string recipes = "";
        if (!def.isCrafted && !def.isRelic)
        {
            var run2 = SingletonManagers.Run;
            int shown = 0;
            foreach (var r in ConstantItemDb.Recipes)
            {
                if (shown >= 3) break;
                if (r.aId != def.id && r.bId != def.id) continue;
                if (run2 == null || !run2.IsRecipeUnlocked(r.resultId)) continue;

                var a = ConstantItemDb.Get(r.aId);
                var b = ConstantItemDb.Get(r.bId);
                var res = ConstantItemDb.Get(r.resultId);
                string aC = ColorUtility.ToHtmlStringRGB(ConstantDefine.ColorOf(a.property));
                string bC = ColorUtility.ToHtmlStringRGB(ConstantDefine.ColorOf(b.property));
                string shapeWord = r.shape switch
                {
                    RecipeShape.Horizontal => "가로",
                    RecipeShape.Vertical => "세로",
                    _ => "대각",
                };
                recipes += $"\n<size=80%>· [<color=#{aC}>{a.ShortLabel}</color>]+[<color=#{bC}>{b.ShortLabel}</color>] {shapeWord} >> {res.displayName}</size>";
                shown++;
            }
            if (shown > 0) recipes = "\n<size=80%><color=#9aa2b5>조합 가능:</color></size>" + recipes;
        }

        GetText((int)Texts.InfoText).text = $"<b>{def.displayName}</b>  {tags}{extra}\n{def.flavor}{hint}{recipes}";
    }

    private void RefreshResonance(RunManager run)
    {
        var synergy = run.Synergy;
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<b>발동 중인 연결/패시브</b>");
        if (synergy.links.Count == 0 && synergy.craftedPassives.Count == 0)
        {
            sb.AppendLine("없음 — 같은 태그를 이웃하게 두거나, 도감(V)의 배치로 조합해 보자.");
        }
        else
        {
            foreach (var (label, count) in synergy.links)
                sb.AppendLine($"· {label} x{count}");

            foreach (var passive in synergy.craftedPassives)
                sb.AppendLine($"· <color=#E8C56A>{passive}</color>");
        }

        string summary = synergy.Summary();
        if (!string.IsNullOrEmpty(summary))
        {
            sb.AppendLine();
            sb.AppendLine($"<b>합산 효과</b>  {summary}");
        }

        GetText((int)Texts.ResonanceText).text = sb.ToString();
    }

    private void Update()
    {
        // F: 커서 위 사용형 제작 아이템 사용 (legacy 폴링 — timeScale 0 에서도 동작)
        if (!Input.GetKeyDown(KeyCode.F)) return;

        var run = SingletonManagers.Run;
        var def = ConstantItemDb.Get(run?.GetSlot(_cursor));
        if (def == null || !def.isActive) return;

        run.UseCraftedAt(_cursor);
        _handIndex = -1;
        Refresh();
    }
}
