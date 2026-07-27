using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 조합 도감 — 마크식 시각 조합법.
/// 각 레시피를 2x2 조합대 그리드(실제 배치 모양) ▸ 결과 칸으로 렌더링한다.
/// 미발견 레시피는 ? 칸으로 가려진다 (배치 모양도 비밀).
/// 좌우: 페이지 · ESC: 닫기
/// </summary>
public class UI_Popup_Codex : UI_Popup
{
    enum GameObjects { EntriesRoot }
    enum Texts { TitleText, PageText, PromptText }

    private const int PerPage = 4;
    private const float EntryH = 136f;
    private const float Cell = 46f;

    private int _page;
    private float _lastMoveTime;
    [SerializeField] private float _moveCooldown = 0.18f;

    private Transform _entriesRoot;
    private TMP_FontAsset _font;

    private static readonly Color FrameColor = new Color(0.22f, 0.24f, 0.30f);
    private static readonly Color EmptyCell = new Color(0.10f, 0.11f, 0.15f);
    private static readonly Color LockedCell = new Color(0.14f, 0.14f, 0.18f);

    public override void Init()
    {
        base.Init();
        Bind<GameObject>(typeof(GameObjects));
        Bind<TextMeshProUGUI>(typeof(Texts));

        _entriesRoot = Get<GameObject>((int)GameObjects.EntriesRoot).transform;
        _font = GetText((int)Texts.TitleText).font;

        GetText((int)Texts.TitleText).text = "조합 도감";
        GetText((int)Texts.PromptText).text = "좌우 — 페이지 · ESC — 닫기 · (가방에서 재료를 이 모양대로 놓으면 조합)";
        Refresh();
    }

    public override void OnInput(Vector2 dir)
    {
        if (Time.unscaledTime - _lastMoveTime < _moveCooldown) return;

        int pages = PageCount();
        if (dir.x > 0.5f) { _page = (_page + 1) % pages; _lastMoveTime = Time.unscaledTime; Refresh(); }
        else if (dir.x < -0.5f) { _page = (_page + pages - 1) % pages; _lastMoveTime = Time.unscaledTime; Refresh(); }
    }

    public override void OnSubmit() { }

    public override void OnCancel()
    {
        ClosePopupUI();
        if (SingletonManagers.UI.PopupCount == 0)
        {
            SingletonManagers.Input.SetInputModeUI(false);
            Time.timeScale = 1f;
        }
    }

    private int PageCount() =>
        Mathf.Max(1, Mathf.CeilToInt(ConstantItemDb.Recipes.Count / (float)PerPage));

    // ─────────────────────────────────────────────────────────────
    private void Refresh()
    {
        var run = SingletonManagers.Run;
        var recipes = ConstantItemDb.Recipes;

        foreach (Transform child in _entriesRoot)
            Destroy(child.gameObject);

        int unlockedCount = 0;
        foreach (var r in recipes)
            if (run != null && run.IsRecipeUnlocked(r.resultId)) unlockedCount++;

        GetText((int)Texts.PageText).text =
            $"{_page + 1}/{PageCount()} 페이지 · 발견 {unlockedCount}/{recipes.Count}";

        int start = _page * PerPage;
        for (int i = start; i < Mathf.Min(start + PerPage, recipes.Count); i++)
        {
            var recipe = recipes[i];
            bool unlocked = run != null && run.IsRecipeUnlocked(recipe.resultId);
            BuildEntry(recipe, unlocked, (i - start));
        }
    }

    /// <summary>레시피 1건을 시각 조합대로 렌더: [2x2 배치] >> [결과] + 효과.</summary>
    private void BuildEntry(RecipeDef recipe, bool unlocked, int row)
    {
        var entry = NewRect("Entry", _entriesRoot, new Vector2(0, -row * EntryH), new Vector2(1030, EntryH - 8));

        // 구분선
        var divider = NewRect("Divider", entry, new Vector2(0, -(EntryH - 14)), new Vector2(1010, 1.5f));
        divider.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);

        // ── 2x2 조합대 ──
        float gridX = 6f, gridY = -8f;
        var a = ConstantItemDb.Get(recipe.aId);
        var b = ConstantItemDb.Get(recipe.bId);

        // B 칸의 상대 위치 (A 는 항상 좌상단)
        Vector2Int bCell = recipe.shape switch
        {
            RecipeShape.Horizontal => new Vector2Int(1, 0),
            RecipeShape.Vertical => new Vector2Int(0, 1),
            _ => new Vector2Int(1, 1),
        };

        for (int cy = 0; cy < 2; cy++)
        {
            for (int cx = 0; cx < 2; cx++)
            {
                Vector2 pos = new Vector2(gridX + cx * (Cell + 5), gridY - cy * (Cell + 5));

                if (!unlocked)
                {
                    MakeCell(entry, pos, LockedCell, "?", new Color(0.4f, 0.4f, 0.5f), null);
                    continue;
                }

                if (cx == 0 && cy == 0) MakeItemCell(entry, pos, a);
                else if (cx == bCell.x && cy == bCell.y) MakeItemCell(entry, pos, b);
                else MakeCell(entry, pos, EmptyCell, "", Color.clear, null);
            }
        }

        // ── 화살표 ──
        MakeText(entry, new Vector2(gridX + 118, gridY - 26), new Vector2(50, 40), ">>", 26,
            unlocked ? new Color(0.91f, 0.77f, 0.42f) : new Color(0.35f, 0.35f, 0.42f), TextAlignmentOptions.Center);

        // ── 결과 칸 + 정보 ──
        float rx = gridX + 175;
        if (unlocked)
        {
            var result = ConstantItemDb.Get(recipe.resultId);
            MakeCell(entry, new Vector2(rx, gridY - 20), new Color(0.16f, 0.17f, 0.22f),
                result.ShortLabel, ConstantDefine.ColorOf(result.property), result.RarityColor, Cell + 12);

            string badge = result.isActive ? $"사용형 x{result.uses}" : "패시브";
            MakeText(entry, new Vector2(rx + 75, gridY - 8), new Vector2(640, 30),
                $"<b><color=#{ColorUtility.ToHtmlStringRGB(result.RarityColor)}>{result.displayName}</color></b>  ({result.RarityLabel} · {badge})",
                20, Color.white, TextAlignmentOptions.TopLeft);
            MakeText(entry, new Vector2(rx + 75, gridY - 40), new Vector2(700, 34),
                result.useHint, 17, new Color(0.72f, 0.88f, 1f), TextAlignmentOptions.TopLeft);
            MakeText(entry, new Vector2(rx + 75, gridY - 74), new Vector2(700, 26),
                $"재료: {a.displayName} + {b.displayName} — {recipe.ShapeLabel}", 15,
                new Color(1f, 1f, 1f, 0.45f), TextAlignmentOptions.TopLeft);
        }
        else
        {
            MakeCell(entry, new Vector2(rx, gridY - 20), LockedCell, "?", new Color(0.4f, 0.4f, 0.5f), null, Cell + 12);
            MakeText(entry, new Vector2(rx + 75, gridY - 22), new Vector2(640, 40),
                "<color=#666677>??? — 아직 발견하지 못한 배치</color>", 19, Color.white, TextAlignmentOptions.TopLeft);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 동적 UI 조립 헬퍼
    // ─────────────────────────────────────────────────────────────
    private RectTransform NewRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private void MakeItemCell(Transform parent, Vector2 pos, ConstantItemDef def)
    {
        MakeCell(parent, pos, new Color(0.16f, 0.17f, 0.22f), def.ShortLabel,
            ConstantDefine.ColorOf(def.property), def.RarityColor);
    }

    /// <summary>조합대 칸: 프레임 + 내용물(성질색 사각) + 짧은 이름. rarityFrame 있으면 테두리색.</summary>
    private void MakeCell(Transform parent, Vector2 pos, Color bg, string label, Color fill,
        Color? rarityFrame, float size = 0f)
    {
        float s = size > 0f ? size : Cell;

        var frame = NewRect("Cell", parent, pos, new Vector2(s, s));
        frame.gameObject.AddComponent<Image>().color = rarityFrame ?? FrameColor;

        var inner = NewRect("Inner", frame, new Vector2(2, -2), new Vector2(s - 4, s - 4));
        inner.gameObject.AddComponent<Image>().color = bg;

        if (fill.a > 0.01f && label != "?")
        {
            var chip = NewRect("Chip", inner, new Vector2((s - 4) * 0.5f - 13, -5), new Vector2(26, 12));
            chip.gameObject.AddComponent<Image>().color = fill;
        }

        if (!string.IsNullOrEmpty(label))
            MakeText(frame, new Vector2(0, -(s * 0.5f - 4)), new Vector2(s, s * 0.55f), label,
                s > Cell ? 17 : 15, label == "?" ? new Color(0.5f, 0.5f, 0.6f) : Color.white,
                TextAlignmentOptions.Center);
    }

    private void MakeText(Transform parent, Vector2 pos, Vector2 size, string text, float fontSize,
        Color color, TextAlignmentOptions align)
    {
        var rt = NewRect("Text", parent, pos, size);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
    }
}
