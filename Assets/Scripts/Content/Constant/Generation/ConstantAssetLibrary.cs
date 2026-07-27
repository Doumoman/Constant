using System;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// 런타임 절차 생성용 에셋 라이브러리 — 2D Fantasy 번들 등 비-Resources 에셋의 참조 보관소.
/// 에디터 메뉴(Tools/Constant/Build Asset Library)가 채워서 Resources 에 저장하고,
/// 런타임 생성기가 Resources.Load 로 읽는다.
/// </summary>
public class ConstantAssetLibrary : ScriptableObject
{
    public const string ResourcePath = "ConstantAssetLibrary";

    [Serializable]
    public class PlanetAssets
    {
        public ConstantPlanet planet;
        public Sprite[] bgSprites;
        public Sprite nodeSprite;
        public GameObject padPrefab;
        public GameObject[] particles;
        public SpriteShape groundProfile;

        [Header("드레싱 — 팩 데모 씬 문법")]
        public Sprite[] bgMidSprites;       // 중거리 실루엣 (시차 레이어)
        public Sprite[] wallPanelSprites;   // 방 내부 뒷벽 패널
        public Sprite[] floorPropSprites;   // 바닥 소품
        public Sprite[] hangingPropSprites; // 천장/벽걸이 소품
        public Sprite[] glowSprites;        // 발광 액센트
        public Sprite[] setPieceSprites;    // 방 안 대형 랜드마크 (거목/크리스탈군/코어 — 배경측)
        public Sprite[] pillarSprites;      // 바닥-천장 기둥 (세로 스택)
        public Sprite[] floorFringeSprites;   // 바닥 라인을 따라 이어지는 잔장식 (이끼/파편/잔해)
        public Sprite[] ceilingFringeSprites; // 천장 라인 잔장식 (종유석/케이블/램프)
        public Sprite lightSprite;          // 소프트 발광 블롭 (빛 웅덩이/헤일로)
        public Color lightTint = new Color(1f, 0.9f, 0.7f, 1f);
        public Color wallTint = new Color(0.55f, 0.55f, 0.55f, 1f); // 패널을 뒤로 밀어주는 어두운 틴트
        public Color midTint = new Color(0.35f, 0.35f, 0.35f, 1f);
        public float veilAlpha = 0.3f;    // 배경 전체를 누르는 검은 베일 (데모 문법: 시선을 플레이필드에 고정)
        public bool hangingFlipY = true;  // 천장 소품 상하 반전 (바위/크리스탈 true, 사슬/램프 false)
    }

    public PlanetAssets[] planets;
    public GameObject spiderPrefab;
    public Sprite coreSprite;
    public Sprite crystalSprite;
    public TMP_FontAsset koreanFont;

    public PlanetAssets For(ConstantPlanet planet)
    {
        if (planets != null)
            foreach (var p in planets)
                if (p.planet == planet) return p;
        return null;
    }

    public static ConstantAssetLibrary Load() =>
        Resources.Load<ConstantAssetLibrary>(ResourcePath);
}
