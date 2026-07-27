using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Constant UI 프리팹(HUD/여행가방/런 결과)을 코드로 생성하는 에디터 도구.
/// UIManager 규약: Resources/Prefabs/UI/Scene|Popup/{클래스명}.prefab,
/// 자식 GameObject 이름 == UI 클래스의 enum 멤버 이름 (Bind 규약).
/// 메뉴: Tools/Constant/Build UI Prefabs
/// </summary>
public static class ConstantUIBuilder
{
    private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";
    private const string ScenePrefabFolder = "Assets/Resources/Prefabs/UI/Scene";
    private const string PopupPrefabFolder = "Assets/Resources/Prefabs/UI/Popup";

    private static readonly Color PanelColor = new Color(0.09f, 0.10f, 0.14f, 0.96f);
    private static readonly Color AccentColor = new Color(0.91f, 0.77f, 0.42f);
    private static readonly Color SubtleColor = new Color(0.75f, 0.78f, 0.85f);

    private static TMP_FontAsset _font;
    private static Sprite _uiSprite;

    [MenuItem("Tools/Constant/Build UI Prefabs")]
    public static void BuildAll()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null)
            Debug.LogWarning($"[Constant] 한글 폰트를 찾지 못했습니다: {FontPath} — TMP 기본 폰트로 생성합니다.");
        _uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        BuildHud();
        BuildSuitcase();
        BuildRunResult();
        BuildCodex();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Constant] UI 프리팹 3종 생성 완료 (HUD / Suitcase / RunResult)");
    }

    // ─────────────────────────────────────────────────────────────
    // 1) UI_Scene_ConstantHUD
    // ─────────────────────────────────────────────────────────────
    private static void BuildHud()
    {
        GameObject root = CreateCanvasRoot("UI_Scene_ConstantHUD", typeof(UI_Scene_ConstantHUD));

        AddText(root.transform, "PlanetText", "행성 이름", 30, TextAlignmentOptions.TopLeft, Color.white,
            anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1), pivot: new Vector2(0, 1),
            pos: new Vector2(24, -20), size: new Vector2(700, 40));

        // 게이지 바 (배경 + Fill)
        Image gaugeBg = AddImage(root.transform, "GaugeBG", new Color(0f, 0f, 0f, 0.55f),
            anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1), pivot: new Vector2(0, 1),
            pos: new Vector2(24, -64), size: new Vector2(420, 26));

        Image fill = AddImage(gaugeBg.transform, "GaugeFill", AccentColor,
            anchorMin: Vector2.zero, anchorMax: Vector2.one, pivot: new Vector2(0.5f, 0.5f),
            pos: Vector2.zero, size: new Vector2(-6, -6)); // stretch - padding
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;

        AddText(root.transform, "GaugeText", "출항 게이지 0%", 22, TextAlignmentOptions.MidlineLeft, Color.white,
            anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1), pivot: new Vector2(0, 1),
            pos: new Vector2(456, -62), size: new Vector2(400, 30));

        AddText(root.transform, "BuffText", "", 19, TextAlignmentOptions.TopLeft, new Color(0.55f, 0.95f, 0.65f),
            anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1), pivot: new Vector2(0, 1),
            pos: new Vector2(24, -98), size: new Vector2(1000, 30));

        AddText(root.transform, "HelpText", "", 17, TextAlignmentOptions.BottomLeft, new Color(1f, 1f, 1f, 0.55f),
            anchorMin: new Vector2(0, 0), anchorMax: new Vector2(0, 0), pivot: new Vector2(0, 0),
            pos: new Vector2(24, 16), size: new Vector2(900, 26));

        AddText(root.transform, "ConsumText", "로프 0 · 폭탄 0", 20, TextAlignmentOptions.BottomLeft, new Color(0.85f, 0.75f, 0.55f),
            anchorMin: new Vector2(0, 0), anchorMax: new Vector2(0, 0), pivot: new Vector2(0, 0),
            pos: new Vector2(24, 48), size: new Vector2(500, 28));

        AddText(root.transform, "ToastText", "", 24, TextAlignmentOptions.Bottom, AccentColor,
            anchorMin: new Vector2(0.5f, 0), anchorMax: new Vector2(0.5f, 0), pivot: new Vector2(0.5f, 0),
            pos: new Vector2(0, 110), size: new Vector2(1300, 70));

        SavePrefab(root, $"{ScenePrefabFolder}/UI_Scene_ConstantHUD.prefab");
    }

    // ─────────────────────────────────────────────────────────────
    // 2) UI_Popup_Suitcase — 4x3 그리드 + 정보/공명 패널
    // ─────────────────────────────────────────────────────────────
    private static void BuildSuitcase()
    {
        GameObject root = CreateCanvasRoot("UI_Popup_Suitcase", typeof(UI_Popup_Suitcase));

        AddImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.66f),
            anchorMin: Vector2.zero, anchorMax: Vector2.one, pivot: new Vector2(0.5f, 0.5f),
            pos: Vector2.zero, size: Vector2.zero);

        Image panel = AddImage(root.transform, "Panel", PanelColor,
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            pos: Vector2.zero, size: new Vector2(1280, 660));

        AddText(panel.transform, "TitleText", "여행가방", 34, TextAlignmentOptions.TopLeft, AccentColor,
            anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1), pivot: new Vector2(0, 1),
            pos: new Vector2(36, -26), size: new Vector2(400, 44));

        // 4x3 그리드
        GameObject grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        grid.transform.SetParent(panel.transform, false);
        RectTransform gridRt = grid.GetComponent<RectTransform>();
        gridRt.anchorMin = gridRt.anchorMax = gridRt.pivot = new Vector2(0, 1);
        gridRt.anchoredPosition = new Vector2(36, -90);
        gridRt.sizeDelta = new Vector2(4 * 152 + 3 * 10, 3 * 138 + 2 * 10);

        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(152, 138);
        layout.spacing = new Vector2(10, 10);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;

        for (int i = 0; i < 12; i++)
        {
            Image cell = AddImage(grid.transform, $"Cell_{i}", new Color(0.13f, 0.15f, 0.20f, 0.95f),
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
                pos: Vector2.zero, size: Vector2.zero); // GridLayout 이 크기 결정

            AddImage(cell.transform, "Icon", Color.white,
                anchorMin: new Vector2(0.5f, 1), anchorMax: new Vector2(0.5f, 1), pivot: new Vector2(0.5f, 1),
                pos: new Vector2(0, -12), size: new Vector2(42, 42)).enabled = false;

            AddText(cell.transform, "Name", "", 18, TextAlignmentOptions.Center, Color.white,
                anchorMin: new Vector2(0.5f, 0), anchorMax: new Vector2(0.5f, 0), pivot: new Vector2(0.5f, 0),
                pos: new Vector2(0, 34), size: new Vector2(146, 48));

            AddText(cell.transform, "Tag", "", 14, TextAlignmentOptions.Bottom, SubtleColor,
                anchorMin: new Vector2(0.5f, 0), anchorMax: new Vector2(0.5f, 0), pivot: new Vector2(0.5f, 0),
                pos: new Vector2(0, 8), size: new Vector2(146, 22));
        }

        // 우측 정보 패널
        AddText(panel.transform, "InfoText", "빈 칸", 20, TextAlignmentOptions.TopLeft, Color.white,
            anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1), pivot: new Vector2(0, 1),
            pos: new Vector2(710, -90), size: new Vector2(530, 130));

        AddText(panel.transform, "ResonanceText", "", 18, TextAlignmentOptions.TopLeft, SubtleColor,
            anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1), pivot: new Vector2(0, 1),
            pos: new Vector2(710, -230), size: new Vector2(530, 340));

        AddText(panel.transform, "HandText", "Enter 집기 · ESC 닫기", 19, TextAlignmentOptions.BottomLeft, AccentColor,
            anchorMin: new Vector2(0, 0), anchorMax: new Vector2(0, 0), pivot: new Vector2(0, 0),
            pos: new Vector2(36, 18), size: new Vector2(1200, 30));

        SavePrefab(root, $"{PopupPrefabFolder}/UI_Popup_Suitcase.prefab");
    }

    // ─────────────────────────────────────────────────────────────
    // 3) UI_Popup_RunResult
    // ─────────────────────────────────────────────────────────────
    private static void BuildRunResult()
    {
        GameObject root = CreateCanvasRoot("UI_Popup_RunResult", typeof(UI_Popup_RunResult));

        AddImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.82f),
            anchorMin: Vector2.zero, anchorMax: Vector2.one, pivot: new Vector2(0.5f, 0.5f),
            pos: Vector2.zero, size: Vector2.zero);

        Image panel = AddImage(root.transform, "Panel", PanelColor,
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            pos: Vector2.zero, size: new Vector2(940, 480));

        AddText(panel.transform, "TitleText", "여정의 기록", 40, TextAlignmentOptions.Top, AccentColor,
            anchorMin: new Vector2(0.5f, 1), anchorMax: new Vector2(0.5f, 1), pivot: new Vector2(0.5f, 1),
            pos: new Vector2(0, -44), size: new Vector2(860, 56));

        AddText(panel.transform, "DescText", "", 23, TextAlignmentOptions.Top, Color.white,
            anchorMin: new Vector2(0.5f, 1), anchorMax: new Vector2(0.5f, 1), pivot: new Vector2(0.5f, 1),
            pos: new Vector2(0, -130), size: new Vector2(820, 240));

        AddText(panel.transform, "PromptText", "Enter — 새 여행 시작", 22, TextAlignmentOptions.Bottom, AccentColor,
            anchorMin: new Vector2(0.5f, 0), anchorMax: new Vector2(0.5f, 0), pivot: new Vector2(0.5f, 0),
            pos: new Vector2(0, 30), size: new Vector2(600, 32));

        SavePrefab(root, $"{PopupPrefabFolder}/UI_Popup_RunResult.prefab");
    }

    // ─────────────────────────────────────────────────────────────
    // 4) UI_Popup_Codex — 조합 도감 (페이지형)
    // ─────────────────────────────────────────────────────────────
    private static void BuildCodex()
    {
        GameObject root = CreateCanvasRoot("UI_Popup_Codex", typeof(UI_Popup_Codex));

        AddImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.72f),
            anchorMin: Vector2.zero, anchorMax: Vector2.one, pivot: new Vector2(0.5f, 0.5f),
            pos: Vector2.zero, size: Vector2.zero);

        Image panel = AddImage(root.transform, "Panel", PanelColor,
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            pos: Vector2.zero, size: new Vector2(1100, 720));

        AddText(panel.transform, "TitleText", "조합 도감", 34, TextAlignmentOptions.TopLeft, AccentColor,
            anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1), pivot: new Vector2(0, 1),
            pos: new Vector2(36, -26), size: new Vector2(400, 44));

        AddText(panel.transform, "PageText", "", 20, TextAlignmentOptions.TopRight, SubtleColor,
            anchorMin: new Vector2(1, 1), anchorMax: new Vector2(1, 1), pivot: new Vector2(1, 1),
            pos: new Vector2(-36, -32), size: new Vector2(400, 30));

        // 시각 조합대 엔트리 컨테이너 (코드가 동적으로 채운다)
        GameObject entriesRoot = new GameObject("EntriesRoot", typeof(RectTransform));
        entriesRoot.transform.SetParent(panel.transform, false);
        RectTransform entriesRt = (RectTransform)entriesRoot.transform;
        entriesRt.anchorMin = entriesRt.anchorMax = entriesRt.pivot = new Vector2(0, 1);
        entriesRt.anchoredPosition = new Vector2(36, -84);
        entriesRt.sizeDelta = new Vector2(1030, 560);

        AddText(panel.transform, "PromptText", "", 19, TextAlignmentOptions.BottomLeft, AccentColor,
            anchorMin: new Vector2(0, 0), anchorMax: new Vector2(0, 0), pivot: new Vector2(0, 0),
            pos: new Vector2(36, 18), size: new Vector2(800, 28));

        SavePrefab(root, $"{PopupPrefabFolder}/UI_Popup_Codex.prefab");
    }

    // ─────────────────────────────────────────────────────────────
    // 헬퍼
    // ─────────────────────────────────────────────────────────────
    private static GameObject CreateCanvasRoot(string name, System.Type uiType)
    {
        GameObject root = new GameObject(name,
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent(uiType);
        return root;
    }

    private static TextMeshProUGUI AddText(Transform parent, string name, string text, float fontSize,
        TextAlignmentOptions align, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Image AddImage(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.sprite = _uiSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
