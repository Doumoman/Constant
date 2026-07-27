using System.Collections.Generic;
using UnityEngine;

/// <summary>아이템 희귀도 — 조합 파워 밸런스의 축.</summary>
public enum ItemRarity { Normal, Rare, Epic }

/// <summary>레시피 배치 모양 (마크식 조합대: 가방 4x3 그리드 기준).</summary>
public enum RecipeShape
{
    Horizontal, // [A][B]
    Vertical,   // [A] 위, [B] 아래
    Diagonal,   // [A] 좌상, [B] 우하 (L자)
}

public class ConstantItemDef
{
    public string id;
    public string displayName;
    public string flavor;
    public PropertyTag property;
    public ActionTag action;
    public ItemRarity rarity;
    public bool isRelic;
    public bool isCrafted;
    public bool isActive;
    public int uses;
    public string effectId;
    public float effectPower;
    public string useHint;

    // 제작 아이템 패시브
    public float pJump, pMove, pGather, pVision;
    public bool pLavaImmune, pBigBomb, pSlowMobs;
    public int pRevive, pStageRope, pStageBomb;

    public ConstantItemDef(string id, string name, string flavor,
        PropertyTag property, ActionTag action, ItemRarity rarity, bool isRelic = false)
    {
        this.id = id; displayName = name; this.flavor = flavor;
        this.property = property; this.action = action;
        this.rarity = rarity; this.isRelic = isRelic;
    }

    /// <summary>조합대 칸에 들어갈 짧은 이름 (2~3자).</summary>
    public string shortName;
    public string ShortLabel => !string.IsNullOrEmpty(shortName)
        ? shortName
        : (displayName.Length <= 2 ? displayName : displayName.Substring(0, 2));

    public string TagLabel => $"{ConstantDefine.NameOf(property)}·{ConstantDefine.NameOf(action)}";
    public string RarityLabel => rarity switch { ItemRarity.Epic => "에픽", ItemRarity.Rare => "레어", _ => "노말" };

    public Color RarityColor => rarity switch
    {
        ItemRarity.Epic => new Color(0.85f, 0.6f, 1f),
        ItemRarity.Rare => new Color(0.55f, 0.8f, 1f),
        _ => Color.white,
    };
}

public class RecipeDef
{
    public string resultId;
    public string aId;
    public string bId;
    public RecipeShape shape;

    public RecipeDef(string result, string a, string b, RecipeShape shape)
    { resultId = result; aId = a; bId = b; this.shape = shape; }

    public string ShapeLabel => shape switch
    {
        RecipeShape.Horizontal => "[A][B] 가로",
        RecipeShape.Vertical => "[A]위·[B]아래",
        _ => "[A]좌상·[B]우하 대각",
    };
}

/// <summary>
/// Constant 아이템/레시피 DB v3 — 베이스 9(노말4/레어3/에픽2) + 유품 3 + 제작 43.
/// 모든 베이스 페어(동일 페어 포함)가 최소 1개 레시피를 가진다.
/// 같은 페어라도 배치 모양이 다르면 다른 결과가 나온다 (예: 종+나침반 가로=탐지기, 대각=역재생기).
/// 재료 희귀도 합이 높을수록 강력하다.
/// </summary>
public static class ConstantItemDb
{
    private static List<ConstantItemDef> _items;
    private static List<RecipeDef> _recipes;
    private static Dictionary<string, ConstantItemDef> _byId;

    public static IReadOnlyList<ConstantItemDef> Items { get { EnsureInit(); return _items; } }
    public static IReadOnlyList<RecipeDef> Recipes { get { EnsureInit(); return _recipes; } }

    public static ConstantItemDef Get(string id)
    {
        EnsureInit();
        return id != null && _byId.TryGetValue(id, out var def) ? def : null;
    }

    // 제작 아이템 축약 생성기
    private static ConstantItemDef C(string id, string name, string flavor,
        PropertyTag p, ActionTag a, ItemRarity r)
    {
        var d = new ConstantItemDef(id, name, flavor, p, a, r) { isCrafted = true };
        _items.Add(d);
        return d;
    }

    private static ConstantItemDef Active(ConstantItemDef d, string effectId, float power, int uses, string hint)
    { d.isActive = true; d.effectId = effectId; d.effectPower = power; d.uses = uses; d.useHint = $"사용(F): {hint}"; return d; }

    private static void EnsureInit()
    {
        if (_items != null) return;
        _items = new List<ConstantItemDef>();

        // ═══════════ 베이스 9종 ═══════════
        _items.Add(new ConstantItemDef("thermos", "여행자 보온병", "초저가 패키지의 유일한 사은품.", PropertyTag.Heat, ActionTag.Guard, ItemRarity.Normal));
        _items.Add(new ConstantItemDef("emberFlask", "불씨병", "흔들면 화를 낸다. 놔두면 삐진다.", PropertyTag.Heat, ActionTag.Burst, ItemRarity.Normal));
        _items.Add(new ConstantItemDef("echoBell", "메아리 종", "울리고 나서 한참 뒤에 대답한다.", PropertyTag.Echo, ActionTag.Repeat, ItemRarity.Normal));
        _items.Add(new ConstantItemDef("compass", "태엽 나침반", "감긴 만큼만 정직하게 돈다.", PropertyTag.Machine, ActionTag.Repeat, ItemRarity.Normal));
        _items.Add(new ConstantItemDef("meteorCan", "유성 통조림", "유통기한: 약 4.6억 년.", PropertyTag.Heat, ActionTag.Launch, ItemRarity.Rare));
        _items.Add(new ConstantItemDef("tideMirror", "조수 거울", "비친 것을 반대편 물결로 되돌린다.", PropertyTag.Echo, ActionTag.Launch, ItemRarity.Rare));
        _items.Add(new ConstantItemDef("relayThread", "중계 실", "끊긴 회로 사이를 잇고 싶어 한다.", PropertyTag.Machine, ActionTag.Guard, ItemRarity.Rare));
        _items.Add(new ConstantItemDef("silenceCrystal", "침묵 결정", "말이 되지 못한 온도가 굳어 있다.", PropertyTag.Cold, ActionTag.Burst, ItemRarity.Epic));
        _items.Add(new ConstantItemDef("overFuse", "과열 퓨즈", "규정 온도를 한참 넘긴 이단아.", PropertyTag.Machine, ActionTag.Burst, ItemRarity.Epic));

        // ═══════════ 유품 3종 (조합 불가) ═══════════
        _items.Add(new ConstantItemDef("coolantCore", "냉각 코어 조각", "주민들이 몸으로 지키던 것의 일부.", PropertyTag.Cold, ActionTag.Guard, ItemRarity.Epic, isRelic: true));
        _items.Add(new ConstantItemDef("voiceNeedle", "목소리 바늘", "단 하나의 목소리만 기록되어 있다.", PropertyTag.Echo, ActionTag.Guard, ItemRarity.Epic, isRelic: true));
        _items.Add(new ConstantItemDef("commandRing", "부서진 명령 고리", "최초 명령자의 표식. 반쪽이 없다.", PropertyTag.Machine, ActionTag.Guard, ItemRarity.Epic, isRelic: true));

        ConstantItemDef d;

        // ═══════════ 제작 — 티어1: 노말+노말 ═══════════
        d = C("springShield", "온천 방호막", "미지근한 온기가 몸을 감싼다.", PropertyTag.Heat, ActionTag.Guard, ItemRarity.Normal);
        Active(d, "shield", 10f, 2, "10초간 용암/가스 면역");

        d = C("lullabyKettle", "자장가 주전자", "김 빠지는 소리가 이상하게 나른하다.", PropertyTag.Echo, ActionTag.Guard, ItemRarity.Normal);
        d.pSlowMobs = true; d.useHint = "소지 패시브: 모든 몹 이동 30% 감속";

        d = C("steamBoots", "증기 로켓 부츠", "보온병의 김이 발밑에서 폭발한다.", PropertyTag.Heat, ActionTag.Launch, ItemRarity.Normal);
        d.pJump = 0.4f; d.useHint = "소지 패시브: 점프력 +40%";

        d = C("fireworks", "화염 메아리 폭죽", "터진 소리가 몇 번이고 되울린다.", PropertyTag.Heat, ActionTag.Burst, ItemRarity.Normal);
        Active(d, "stun", 4f, 2, "씬의 모든 몹을 4초 기절");

        d = C("fuseWinder", "도화선 태엽", "감을수록 화약 냄새가 난다.", PropertyTag.Machine, ActionTag.Burst, ItemRarity.Normal);
        Active(d, "bombPack", 1f, 2, "폭탄 +1 즉시 생산");

        d = C("sonar", "음파 탐지기", "종소리가 태엽을 타고 되돌아온다.", PropertyTag.Echo, ActionTag.Repeat, ItemRarity.Normal);
        Active(d, "sonar", 0f, 3, "가장 가까운 밸브의 방향 표시");

        d = C("rewinder", "시간 역재생기", "감긴 태엽이 종소리 나던 순간으로 되감는다.", PropertyTag.Machine, ActionTag.Repeat, ItemRarity.Normal);
        Active(d, "rewind", 0f, 3, "이전 방 진입 지점으로 되감기");

        d = C("springPack", "온천 배낭", "따뜻해서 밧줄이 잘 풀린다.", PropertyTag.Heat, ActionTag.Guard, ItemRarity.Normal);
        d.pStageRope = 1; d.useHint = "소지 패시브: 스테이지 시작마다 로프 +1";

        d = C("twinPowder", "쌍둥이 화약", "혼자보다 둘이 시끄럽다.", PropertyTag.Heat, ActionTag.Burst, ItemRarity.Normal);
        Active(d, "bombPack", 2f, 1, "폭탄 +2 즉시 생산");

        d = C("resonanceDuet", "공명 이중주", "두 종이 서로를 밝혀 준다.", PropertyTag.Echo, ActionTag.Repeat, ItemRarity.Normal);
        d.pVision = 1.5f; d.useHint = "소지 패시브: 어둠 시야 +1.5";

        d = C("doubleClockwork", "이중 태엽", "두 배로 감기고 두 배로 정직하다.", PropertyTag.Machine, ActionTag.Repeat, ItemRarity.Normal);
        d.pMove = 0.15f; d.useHint = "소지 패시브: 이동속도 +15%";

        // ═══════════ 제작 — 티어2: 노말+레어 ═══════════
        d = C("cometCocoa", "혜성 코코아", "4.6억 년 묵은 코코아. 놀랍게도 따뜻하다.", PropertyTag.Heat, ActionTag.Guard, ItemRarity.Rare);
        Active(d, "gaugeCell", 15f, 2, "출항 게이지 +15% 즉시");

        d = C("mistCloak", "안개 망토", "물안개가 발소리를 지운다.", PropertyTag.Echo, ActionTag.Guard, ItemRarity.Rare);
        d.pVision = 1f; d.pMove = 0.05f; d.useHint = "소지 패시브: 시야 +1, 이속 +5%";

        d = C("heatline", "열선 배선", "따뜻한 회로에는 화약이 잘 붙는다.", PropertyTag.Machine, ActionTag.Guard, ItemRarity.Rare);
        d.pStageBomb = 1; d.useHint = "소지 패시브: 스테이지 시작마다 폭탄 +1";

        d = C("skyfallVial", "낙진 병", "쏟으면 바닥이 사라진다.", PropertyTag.Heat, ActionTag.Burst, ItemRarity.Rare);
        Active(d, "drillDown", 4f, 2, "발밑 4칸 수직 관통 파괴");

        d = C("mirrorFlare", "거울 섬광", "빛이 거울마다 다시 터진다.", PropertyTag.Heat, ActionTag.Burst, ItemRarity.Rare);
        Active(d, "stun", 6f, 2, "씬의 모든 몹을 6초 기절");

        d = C("fuseCircuit", "도화선 회로", "회로를 타고 불씨가 더 멀리 번진다.", PropertyTag.Machine, ActionTag.Burst, ItemRarity.Rare);
        d.pBigBomb = true; d.useHint = "소지 패시브: 폭탄 파괴 범위 5x5";

        d = C("starChime", "별의 풍경", "울릴 때마다 잠깐 별이 뜬다.", PropertyTag.Echo, ActionTag.Launch, ItemRarity.Rare);
        Active(d, "torch", 30f, 2, "30초간 시야 +3");

        d = C("echoMirror", "잔향 거울", "거울 속에서 종소리가 길을 밝힌다.", PropertyTag.Echo, ActionTag.Launch, ItemRarity.Rare);
        d.pVision = 2.5f; d.useHint = "소지 패시브: 어둠 시야 +2.5";

        d = C("relayBell", "중계 종", "여기서 울리면 저기서 돌아간다.", PropertyTag.Machine, ActionTag.Repeat, ItemRarity.Rare);
        Active(d, "valveRemote", 0f, 1, "가장 가까운 미가동 밸브를 원격 가동");

        d = C("orbitalHook", "궤도 갈고리", "유성이 지나간 자리에 밧줄이 남는다.", PropertyTag.Machine, ActionTag.Launch, ItemRarity.Rare);
        Active(d, "orbitalHook", 16f, 3, "머리 위 16칸 강철 로프 설치");

        d = C("tideCompass", "조수 나침반", "바늘이 다음 물결을 가리킨다.", PropertyTag.Echo, ActionTag.Launch, ItemRarity.Rare);
        Active(d, "blink", 4f, 3, "바라보는 방향 4칸 순간이동");

        d = C("gearLift", "톱니 승강기", "허공에 디딜 곳을 조립한다.", PropertyTag.Machine, ActionTag.Guard, ItemRarity.Rare);
        Active(d, "bridgeKit", 3f, 2, "발밑에 임시 발판 3칸 생성");

        // ═══════════ 제작 — 티어3: 노말+에픽 / 레어+레어 ═══════════
        d = C("cryoFlask", "극저온 보온병", "차가움을 보온한다. 모순이 유용하다.", PropertyTag.Cold, ActionTag.Guard, ItemRarity.Epic);
        Active(d, "shield", 15f, 2, "15초간 용암/가스 면역");

        d = C("boilerHeart", "보일러 심장", "몸이 조금 가벼워지고 많이 뜨거워진다.", PropertyTag.Heat, ActionTag.Launch, ItemRarity.Epic);
        d.pJump = 0.25f; d.pMove = 0.1f; d.useHint = "소지 패시브: 점프 +25%, 이속 +10%";

        d = C("thermalCore", "열충격 코어", "열기와 냉기가 번갈아 맥동한다.", PropertyTag.Cold, ActionTag.Burst, ItemRarity.Epic);
        d.pLavaImmune = true; d.useHint = "소지 패시브: 용암 위를 걸을 수 있다";

        d = C("blastEngine", "폭발 기관", "출력의 절반이 폭음이다.", PropertyTag.Machine, ActionTag.Burst, ItemRarity.Epic);
        d.pBigBomb = true; d.pStageBomb = 1; d.useHint = "소지 패시브: 폭탄 5x5 + 스테이지마다 폭탄 +1";

        d = C("silencedPeal", "봉인된 타종", "울리지 못한 종소리는 무겁다.", PropertyTag.Cold, ActionTag.Burst, ItemRarity.Epic);
        Active(d, "stun", 8f, 2, "씬의 모든 몹을 8초 기절");

        d = C("screamAmp", "절규 증폭기", "소리가 벽보다 단단해진다.", PropertyTag.Echo, ActionTag.Burst, ItemRarity.Epic);
        Active(d, "drillForward", 5f, 2, "정면 5칸 수평 관통 파괴");

        d = C("chronoAnchor", "시간 닻", "몇 번이고 그 순간에 정박한다.", PropertyTag.Cold, ActionTag.Repeat, ItemRarity.Epic);
        Active(d, "rewind", 0f, 5, "이전 방 되감기 (강화판: 5회)");

        d = C("overdrive", "과구동 태엽", "규정 회전수는 장식이다.", PropertyTag.Machine, ActionTag.Repeat, ItemRarity.Epic);
        d.pMove = 0.25f; d.useHint = "소지 패시브: 이동속도 +25%";

        d = C("meteorBrand", "유성 소환 낙인", "정면의 벽이 유성의 길이 된다.", PropertyTag.Heat, ActionTag.Launch, ItemRarity.Rare);
        Active(d, "drillForward", 5f, 2, "정면 5칸 수평 관통 파괴");

        d = C("skyCrane", "궤도 기중기", "하늘 쪽 벽은 벽이 아니다.", PropertyTag.Machine, ActionTag.Launch, ItemRarity.Rare);
        Active(d, "drillUp", 5f, 2, "머리 위 5칸 수직 관통 파괴");

        d = C("mirrorWire", "거울 배선", "거울 사이를 전류처럼 건넌다.", PropertyTag.Echo, ActionTag.Launch, ItemRarity.Rare);
        Active(d, "blink", 6f, 3, "바라보는 방향 6칸 순간이동");

        // ═══════════ 제작 — 티어4: 레어+에픽 ═══════════
        d = C("frozenComet", "얼어붙은 혜성", "떨어질 자리를 얼음이 먼저 안다.", PropertyTag.Cold, ActionTag.Launch, ItemRarity.Epic);
        Active(d, "drillDown", 7f, 2, "발밑 7칸 수직 관통 파괴");

        d = C("payloadCan", "탄두 통조림", "라벨: 흔들지 마시오. (이미 늦었다)", PropertyTag.Heat, ActionTag.Burst, ItemRarity.Epic);
        Active(d, "bombPack", 3f, 1, "폭탄 +3 즉시 생산");

        d = C("stillWater", "고요한 수면", "물결이 멎으면 모든 것이 느려 보인다.", PropertyTag.Cold, ActionTag.Guard, ItemRarity.Epic);
        d.pVision = 3f; d.pSlowMobs = true; d.useHint = "소지 패시브: 시야 +3, 몹 30% 감속";

        d = C("arcMirror", "아크 거울", "반사각이 곧 파괴각이다.", PropertyTag.Echo, ActionTag.Burst, ItemRarity.Epic);
        Active(d, "drillForward", 7f, 2, "정면 7칸 수평 관통 파괴");

        d = C("nullLattice", "무음 격자", "위험이 소리를 잃고 지나간다.", PropertyTag.Cold, ActionTag.Guard, ItemRarity.Epic);
        Active(d, "shield", 20f, 2, "20초간 용암/가스 면역");

        d = C("lifeClockwork", "구명 태엽", "멈춘 심장을 한 바퀴 더 감아 준다.", PropertyTag.Machine, ActionTag.Guard, ItemRarity.Epic);
        d.pRevive = 1; d.useHint = "소지 패시브: 사망 시 1회 부활";

        // ═══════════ 제작 — 티어5: 에픽+에픽 ═══════════
        d = C("singularityShard", "특이점 조각", "미등록 좌표의 문이 살짝 열려 있다.", PropertyTag.Cold, ActionTag.Burst, ItemRarity.Epic);
        Active(d, "singularity", 0f, 1, "출구 게이트를 즉시 개방");
        d.pGather = 1.0f; d.useHint = "패시브: 채집 2배 / " + d.useHint;

        d = C("absoluteZero", "절대 영점", "모든 것이 아주 잠깐, 완전히 멈춘다.", PropertyTag.Cold, ActionTag.Burst, ItemRarity.Epic);
        Active(d, "stun", 10f, 2, "씬의 모든 몹을 10초 기절");

        d = C("criticalCore", "임계 노심", "안전 규정: 없음. 출력: 충분함.", PropertyTag.Machine, ActionTag.Burst, ItemRarity.Epic);
        Active(d, "gaugeCell", 40f, 1, "출항 게이지 +40% 즉시");

        // ═══════════ 레시피 43종 — 모든 페어 커버 ═══════════
        _recipes = new List<RecipeDef>
        {
            // 노말+노말 (동일 페어 이형 포함)
            new RecipeDef("springShield",   "thermos",       "emberFlask",     RecipeShape.Horizontal),
            new RecipeDef("lullabyKettle",  "thermos",       "echoBell",       RecipeShape.Vertical),
            new RecipeDef("steamBoots",     "thermos",       "compass",        RecipeShape.Vertical),
            new RecipeDef("fireworks",      "emberFlask",    "echoBell",       RecipeShape.Vertical),
            new RecipeDef("fuseWinder",     "emberFlask",    "compass",        RecipeShape.Diagonal),
            new RecipeDef("sonar",          "echoBell",      "compass",        RecipeShape.Horizontal),
            new RecipeDef("rewinder",       "echoBell",      "compass",        RecipeShape.Diagonal), // 같은 페어, 다른 모양!
            new RecipeDef("springPack",     "thermos",       "thermos",        RecipeShape.Vertical),
            new RecipeDef("twinPowder",     "emberFlask",    "emberFlask",     RecipeShape.Horizontal),
            new RecipeDef("resonanceDuet",  "echoBell",      "echoBell",       RecipeShape.Vertical),
            new RecipeDef("doubleClockwork","compass",       "compass",        RecipeShape.Horizontal),
            // 노말+레어
            new RecipeDef("cometCocoa",     "thermos",       "meteorCan",      RecipeShape.Horizontal),
            new RecipeDef("mistCloak",      "thermos",       "tideMirror",     RecipeShape.Vertical),
            new RecipeDef("heatline",       "thermos",       "relayThread",    RecipeShape.Horizontal),
            new RecipeDef("skyfallVial",    "emberFlask",    "meteorCan",      RecipeShape.Horizontal),
            new RecipeDef("mirrorFlare",    "emberFlask",    "tideMirror",     RecipeShape.Vertical),
            new RecipeDef("fuseCircuit",    "relayThread",   "emberFlask",     RecipeShape.Diagonal),
            new RecipeDef("starChime",      "echoBell",      "meteorCan",      RecipeShape.Vertical),
            new RecipeDef("echoMirror",     "echoBell",      "tideMirror",     RecipeShape.Vertical),
            new RecipeDef("relayBell",      "echoBell",      "relayThread",    RecipeShape.Horizontal),
            new RecipeDef("orbitalHook",    "compass",       "meteorCan",      RecipeShape.Horizontal),
            new RecipeDef("tideCompass",    "compass",       "tideMirror",     RecipeShape.Horizontal),
            new RecipeDef("gearLift",       "compass",       "relayThread",    RecipeShape.Vertical),
            // 노말+에픽
            new RecipeDef("cryoFlask",      "thermos",       "silenceCrystal", RecipeShape.Horizontal),
            new RecipeDef("boilerHeart",    "thermos",       "overFuse",       RecipeShape.Vertical),
            new RecipeDef("thermalCore",    "emberFlask",    "silenceCrystal", RecipeShape.Horizontal),
            new RecipeDef("blastEngine",    "emberFlask",    "overFuse",       RecipeShape.Diagonal),
            new RecipeDef("silencedPeal",   "echoBell",      "silenceCrystal", RecipeShape.Vertical),
            new RecipeDef("screamAmp",      "echoBell",      "overFuse",       RecipeShape.Horizontal),
            new RecipeDef("chronoAnchor",   "compass",       "silenceCrystal", RecipeShape.Diagonal),
            new RecipeDef("overdrive",      "compass",       "overFuse",       RecipeShape.Horizontal),
            // 레어+레어
            new RecipeDef("meteorBrand",    "meteorCan",     "tideMirror",     RecipeShape.Horizontal),
            new RecipeDef("skyCrane",       "meteorCan",     "relayThread",    RecipeShape.Vertical),
            new RecipeDef("mirrorWire",     "tideMirror",    "relayThread",    RecipeShape.Diagonal),
            // 레어+에픽
            new RecipeDef("frozenComet",    "meteorCan",     "silenceCrystal", RecipeShape.Horizontal),
            new RecipeDef("payloadCan",     "meteorCan",     "overFuse",       RecipeShape.Horizontal),
            new RecipeDef("stillWater",     "tideMirror",    "silenceCrystal", RecipeShape.Vertical),
            new RecipeDef("arcMirror",      "tideMirror",    "overFuse",       RecipeShape.Horizontal),
            new RecipeDef("nullLattice",    "relayThread",   "silenceCrystal", RecipeShape.Vertical),
            new RecipeDef("lifeClockwork",  "relayThread",   "overFuse",       RecipeShape.Vertical),
            // 에픽+에픽
            new RecipeDef("singularityShard","silenceCrystal","overFuse",      RecipeShape.Horizontal),
            new RecipeDef("absoluteZero",   "silenceCrystal","silenceCrystal", RecipeShape.Horizontal),
            new RecipeDef("criticalCore",   "overFuse",      "overFuse",       RecipeShape.Horizontal),
        };

        // 조합대 칸용 짧은 이름
        void SN(string id, string s) { var it = _items.Find(x => x.id == id); if (it != null) it.shortName = s; }
        SN("thermos", "보온"); SN("emberFlask", "불씨"); SN("echoBell", "종");
        SN("compass", "태엽"); SN("meteorCan", "유성"); SN("tideMirror", "거울");
        SN("relayThread", "실"); SN("silenceCrystal", "결정"); SN("overFuse", "퓨즈");
        SN("coolantCore", "코어"); SN("voiceNeedle", "바늘"); SN("commandRing", "고리");

        _byId = new Dictionary<string, ConstantItemDef>();
        foreach (var def in _items)
            _byId[def.id] = def;
    }
}
