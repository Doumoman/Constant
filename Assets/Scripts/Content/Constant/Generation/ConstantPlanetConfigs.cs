using UnityEngine;

/// <summary>행성별 런타임 생성 설정 (에셋 참조는 ConstantAssetLibrary 가 담당).</summary>
public class ConstantPlanetProfile
{
    public ConstantPlanet planet;
    public string nodeName, richNodeName;
    public Color nodeTint = Color.white;
    public string[] itemPool;
    public PropertyTag vaultTag;
    public string vaultDoorName;
    public bool hasCore;
    public bool hasShrine;
    public bool protocolSwitches;
    public string observerNode, observerName;
    public Color observerColor;
    public string padLabel;
    public string moodLabel;
    public bool lavaHazard;
    public float visionRadius;
}

public static class ConstantPlanetConfigs
{
    public static ConstantPlanetProfile For(ConstantPlanet planet)
    {
        switch (planet)
        {
            case ConstantPlanet.Lavernis:
                return new ConstantPlanetProfile
                {
                    planet = planet,
                    nodeName = "용암 결정", richNodeName = "순수 용암 결정",
                    nodeTint = new Color(1f, 0.65f, 0.45f),
                    itemPool = new[] { "emberFlask", "meteorCan", "overFuse" },
                    vaultTag = PropertyTag.Heat, vaultDoorName = "균열 암벽",
                    hasCore = true,
                    observerNode = "Obs_Lavernis", observerName = "고장 난 자판기",
                    observerColor = new Color(0.85f, 0.45f, 0.45f),
                    padLabel = "출항 [X]",
                    moodLabel = "주민들은 약한 불을 오래 지킨다",
                    lavaHazard = true,
                };

            case ConstantPlanet.Sylmare:
                return new ConstantPlanetProfile
                {
                    planet = planet,
                    nodeName = "공명 크리스탈", richNodeName = "순수 크리스탈",
                    itemPool = new[] { "echoBell", "tideMirror", "silenceCrystal" },
                    vaultTag = PropertyTag.Echo, vaultDoorName = "얼어붙은 말의 벽",
                    hasShrine = true,
                    observerNode = "Obs_Sylmare", observerName = "형체가 흐릿한 탐사자",
                    observerColor = new Color(0.7f, 0.65f, 0.9f),
                    padLabel = "출항 [X]",
                    moodLabel = "이곳에서는 말이 천천히 얼어붙는다",
                    visionRadius = 3.5f,
                };

            default: // Eidron
                return new ConstantPlanetProfile
                {
                    planet = ConstantPlanet.Eidron,
                    nodeName = "규격 고철", richNodeName = "예비 부품",
                    itemPool = new[] { "compass", "relayThread", "meteorCan" },
                    vaultTag = PropertyTag.Machine, vaultDoorName = "동력이 끊긴 문",
                    protocolSwitches = true,
                    observerNode = "Obs_Eidron", observerName = "검표원",
                    observerColor = new Color(0.6f, 0.75f, 0.65f),
                    padLabel = "귀환 [X]",
                    moodLabel = "게시: 모든 절차에는 이유가 있다",
                };
        }
    }
}
