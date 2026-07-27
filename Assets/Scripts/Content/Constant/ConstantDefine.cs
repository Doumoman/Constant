using UnityEngine;

/// <summary>
/// Constant(초저가 우주여행 패키지) 전역 정의.
/// 행성 순서, 씬 이름, 아이템 태그 등 코어 루프 공용 상수/열거형 모음.
/// </summary>
public static class ConstantDefine
{
    // 씬 이름 (EditorBuildSettings 등록 필수)
    public const string SceneHub = "Constant_Hub";
    public const string SceneLavernis = "Constant_Lavernis";
    public const string SceneSylmare = "Constant_Sylmare";
    public const string SceneEidron = "Constant_Eidron";

    /// <summary>행성 진행 순서. Hub 에서 시작해 마지막 행성 출항 시 엔딩.</summary>
    public static readonly ConstantPlanet[] PlanetOrder =
    {
        ConstantPlanet.Hub,
        ConstantPlanet.Lavernis,
        ConstantPlanet.Sylmare,
        ConstantPlanet.Eidron,
    };

    public static string SceneNameOf(ConstantPlanet planet)
    {
        switch (planet)
        {
            case ConstantPlanet.Hub: return SceneHub;
            case ConstantPlanet.Lavernis: return SceneLavernis;
            case ConstantPlanet.Sylmare: return SceneSylmare;
            case ConstantPlanet.Eidron: return SceneEidron;
            default: return SceneHub;
        }
    }

    public static string DisplayNameOf(ConstantPlanet planet)
    {
        switch (planet)
        {
            case ConstantPlanet.Hub: return "여행선 '컨스턴트'";
            case ConstantPlanet.Lavernis: return "라베르니스 — 불의 행성";
            case ConstantPlanet.Sylmare: return "실마레 — 메아리의 바다";
            case ConstantPlanet.Eidron: return "에이드론 — 규약의 기계 행성";
            default: return "미지의 좌표";
        }
    }

    public static Color ColorOf(PropertyTag tag)
    {
        switch (tag)
        {
            case PropertyTag.Heat: return new Color(0.95f, 0.45f, 0.25f);
            case PropertyTag.Cold: return new Color(0.45f, 0.75f, 0.95f);
            case PropertyTag.Echo: return new Color(0.75f, 0.55f, 0.95f);
            case PropertyTag.Machine: return new Color(0.65f, 0.72f, 0.55f);
            default: return Color.gray;
        }
    }

    public static string NameOf(PropertyTag tag)
    {
        switch (tag)
        {
            case PropertyTag.Heat: return "열기";
            case PropertyTag.Cold: return "냉기";
            case PropertyTag.Echo: return "메아리";
            case PropertyTag.Machine: return "기계";
            default: return "?";
        }
    }

    public static string NameOf(ActionTag tag)
    {
        switch (tag)
        {
            case ActionTag.Launch: return "발사";
            case ActionTag.Burst: return "폭발";
            case ActionTag.Repeat: return "반복";
            case ActionTag.Guard: return "수호";
            default: return "?";
        }
    }
}

/// <summary>행성(씬) 식별자.</summary>
public enum ConstantPlanet { Hub, Lavernis, Sylmare, Eidron }

/// <summary>아이템 성질 태그 — 무엇으로 이루어졌는가.</summary>
public enum PropertyTag { Heat, Cold, Echo, Machine }

/// <summary>아이템 행동 태그 — 어떻게 작동하는가.</summary>
public enum ActionTag { Launch, Burst, Repeat, Guard }
