using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

/// <summary>
/// Constant 씬 생성기 v4 — 가변 크기 룸 베이킹 (kukuta.tistory.com/196 / TinyKeep식).
///
/// 메인 룸(2유형)과 복도 룸(2유형)을 트리로 이어 붙인다 — 모든 방 도달 보장(MST 철학).
///  · 메인 가로형: 30~33 x 18~20   · 메인 세로형: 23~25 x 45~48
///  · 복도 가로형: 15~20 x 10~15   · 복도 세로형: 10~15 x 25~30 (이벤트/기믹 장소)
/// 방 바깥은 허공(우주) — 각 방은 자기 벽으로 밀폐되고, 문은 연결부에만 뚫린다.
/// 가로 연결은 바닥 정렬(걸어서 통과), 세로 연결은 낙하 통로(복도 사다리/로프로 복귀).
/// 출구는 밸브 3개를 돌려야 열린다. 로프(R)/폭탄(F)이 모든 막힘의 파훼법이다.
/// 메뉴: Tools/Constant/...
/// </summary>
public static class ConstantSceneBuilder
{
    private const string SceneFolder = "Assets/Scenes/Constant";
    private const string PlayerPrefabPath = "Assets/Resources/Prefabs/Player.prefab";
    private const string Bundle = "Assets/2D Fantasy sprite bundle";
    private const string FontPath = "Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset";

    private const int CanvasW = 220, CanvasH = 200; // 배치 캔버스 (방 밖 = 허공)
    private const int HubW = 80;                    // 허브 선체 폭
    private const int MainRoomTarget = 9;           // 행성당 메인 룸 수

    private const int GStatic = 0, GPulse = 1;
    private const int DSolid = 0, DDeadly = 1, DFake = 2;

    // ═════════════════════════════════════════════════════════════
    // 메뉴
    // ═════════════════════════════════════════════════════════════
    [MenuItem("Tools/Constant/Build All Constant Scenes")]
    public static void BuildAll() => BuildAllWithSeeds(101, 202, 303);

    [MenuItem("Tools/Constant/Build All (New Random Layouts)")]
    public static void BuildAllRandom()
    {
        int t = System.Environment.TickCount;
        BuildAllWithSeeds(t, t + 7777, t + 15555);
    }

    private static void BuildAllWithSeeds(int s1, int s2, int s3)
    {
        EnsureRuntimeSprites();
        EnsureSceneFolder();
        BakeAssetLibrary();

        // 행성 씬은 셸만 — 지형/콘텐츠는 ConstantStageBootstrap 이 로드마다 새로 생성 (동적!)
        List<string> built = new List<string>
        {
            BuildHub(),
            BuildPlanetShell(LavernisConfig(s1)),
            BuildPlanetShell(SylmareConfig(s2)),
            BuildPlanetShell(EidronConfig(s3)),
        };

        AddToBuildSettings(built);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Constant] 씬 {built.Count}개 생성 완료 (행성=동적 셸):\n - " + string.Join("\n - ", built));
    }

    /// <summary>런타임 생성기가 쓸 에셋 참조를 Resources SO 로 베이크.</summary>
    [MenuItem("Tools/Constant/Bake Asset Library")]
    public static void BakeAssetLibrary()
    {
        var lib = AssetDatabase.LoadAssetAtPath<ConstantAssetLibrary>("Assets/Resources/ConstantAssetLibrary.asset");
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<ConstantAssetLibrary>();
            EnsureFolder("Assets/Resources");
            AssetDatabase.CreateAsset(lib, "Assets/Resources/ConstantAssetLibrary.asset");
        }

        Sprite S(string p) { var s = LoadSprite(p); if (s == null) Debug.LogWarning($"[Bake] 스프라이트 없음: {p}"); return s; }
        GameObject P(string p) { var g = AssetDatabase.LoadAssetAtPath<GameObject>(p); if (g == null) Debug.LogWarning($"[Bake] 프리팹 없음: {p}"); return g; }
        // 멀티 스프라이트 시트의 서브스프라이트 전부 (Lava rock_0~5 같은 변주용)
        Sprite[] SS(string p)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (all.Length == 0) Debug.LogWarning($"[Bake] 서브스프라이트 없음: {p}");
            return all;
        }
        Sprite[] Merge(params Sprite[][] arrs) =>
            arrs.SelectMany(a => a ?? System.Array.Empty<Sprite>()).Where(s => s != null).ToArray();
        // 시트에서 이름으로 특정 서브스프라이트만 (크기 실측 기반 세트피스/프린지 분류용)
        Sprite[] Pick(string path, params string[] names)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name, s => s);
            return names.Select(n =>
            {
                all.TryGetValue(n, out var s);
                if (s == null) Debug.LogWarning($"[Bake] 서브스프라이트 없음: {path} :: {n}");
                return s;
            }).Where(s => s != null).ToArray();
        }

        // 드레싱 레시피는 각 팩 데모 씬 분석에서 추출 (원거리=사전 블러본, 틴트 사다리, 검은 베일)
        lib.planets = new[]
        {
            new ConstantAssetLibrary.PlanetAssets
            {
                planet = ConstantPlanet.Lavernis,
                bgSprites = new[] { S($"{Bundle}/Lava dungeon pack/Sprites/Lava Background A blur.png") },
                nodeSprite = S($"{Bundle}/Dungeon pack/Sprites/cristals.png"),
                padPrefab = P($"{Bundle}/Lava dungeon pack/Prefabs/DOOR.prefab"),
                particles = new[]
                {
                    P($"{Bundle}/Lava dungeon pack/Prefabs/Spark particle.prefab"),
                    P($"{Bundle}/Lava dungeon pack/Prefabs/Lava Fog particle.prefab"),
                },
                groundProfile = AssetDatabase.LoadAssetAtPath<SpriteShape>($"{Bundle}/Lava dungeon pack/Sprite shapes/Lava ground.asset"),
                bgMidSprites = SS($"{Bundle}/Lava dungeon pack/Sprites/Lava Background rock blur.png"),
                wallPanelSprites = new[] { S($"{Bundle}/Lava dungeon pack/Sprites/Lava Background B blur.png") },
                floorPropSprites = Merge(
                    SS($"{Bundle}/Lava dungeon pack/Sprites/Lava rock.png"),
                    SS($"{Bundle}/Lava dungeon pack/Sprites/Lava rock symple.png"),
                    SS($"{Bundle}/Lava dungeon pack/Sprites/Lava tree.png")),
                hangingPropSprites = SS($"{Bundle}/Lava dungeon pack/Sprites/Lava rock symple.png"), // chain.png 는 통짜 시트라 허공 격자로 보임 — 제외
                glowSprites = SS($"{Bundle}/Lava dungeon pack/Sprites/Lava blocks.png"), // 희귀 발광 액센트
                setPieceSprites = Merge(
                    Pick($"{Bundle}/Lava dungeon pack/Sprites/Lava tree.png", "Lava tree_1", "Lava tree_2", "Lava tree_3"),
                    Pick($"{Bundle}/Lava dungeon pack/Sprites/Lava Background rock.png", "Lava Background rock_1", "Lava Background rock_2"),
                    new[] { S($"{Bundle}/Lava dungeon pack/Sprites/Lava foreground decor.png") }),
                pillarSprites = Merge(
                    Pick($"{Bundle}/Lava dungeon pack/Sprites/Lava environment.png", "Lava environment_6", "Lava environment_7"),
                    Pick($"{Bundle}/Lava dungeon pack/Sprites/Lava platform.png", "Lava platform_0", "Lava platform_1", "Lava platform_7")),
                floorFringeSprites = Merge(
                    Pick($"{Bundle}/Lava dungeon pack/Sprites/Lava platform.png", "Lava platform_5", "Lava platform_6", "Lava platform_8", "Lava platform_9"),
                    Pick($"{Bundle}/Lava dungeon pack/Sprites/Lava environment.png", "Lava environment_23", "Lava environment_25")),
                ceilingFringeSprites = SS($"{Bundle}/Lava dungeon pack/Sprites/Lava rock symple.png"),
                lightSprite = S($"{Bundle}/Lava dungeon pack/Sprites/Shadow.png"),
                lightTint = new Color(1f, 0.55f, 0.25f),
                wallTint = new Color(0.62f, 0.52f, 0.49f),
                midTint = new Color(0.82f, 0.63f, 0.63f), // 데모의 핑크빛 대기원근
                veilAlpha = 0.26f,
                hangingFlipY = true,
            },
            new ConstantAssetLibrary.PlanetAssets
            {
                planet = ConstantPlanet.Sylmare,
                bgSprites = new[] { S($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Background Blur/Background E blur.png") },
                nodeSprite = S($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystals.png"),
                padPrefab = P($"{Bundle}/Cristal Dungeon sprite pack/Crystal prefabs/Crystal.prefab"),
                particles = new[]
                {
                    P($"{Bundle}/Cristal Dungeon sprite pack/Crystal prefabs/Particles/Dust Particle.prefab"),
                    P($"{Bundle}/Cristal Dungeon sprite pack/Crystal prefabs/Particles/Stars Particle.prefab"),
                },
                groundProfile = AssetDatabase.LoadAssetAtPath<SpriteShape>($"{Bundle}/Cristal Dungeon sprite pack/Sprite shape/Crystal ground.asset"),
                bgMidSprites = SS($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystal back trees.png"),
                wallPanelSprites = new[]
                {
                    S($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Background Blur/Background C blur.png"),
                    S($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Background Blur/Background B blur.png"),
                },
                floorPropSprites = Merge(
                    SS($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystals.png"),
                    SS($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystal elements.png"),
                    SS($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystal trees.png")),
                hangingPropSprites = Merge(
                    SS($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystal spike.png"),
                    SS($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystal tree branches.png")),
                glowSprites = SS($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Star particle.png"),
                setPieceSprites = Merge(
                    SS($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Background A elements.png"),
                    Pick($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystal elements.png",
                        "Crystal elements_0", "Crystal elements_6", "Crystal elements_7", "Crystal elements_10"),
                    Pick($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystal trees.png",
                        "Crystal trees_0", "Crystal trees_1", "Crystal trees_2", "Crystal trees_3"),
                    Pick($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Front Crystals.png",
                        "Front Crystals_1", "Front Crystals_5", "Front Crystals_6")),
                pillarSprites = System.Array.Empty<Sprite>(), // 크리스탈 동굴은 기둥 대신 세트피스 2배
                floorFringeSprites = Merge(
                    Pick($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystals.png",
                        "Crystals_0", "Crystals_3", "Crystals_5", "Crystals_6", "Crystals_8", "Crystals_9", "Crystals_10", "Crystals_11"),
                    Pick($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystal spike.png",
                        "Crystal spike_3", "Crystal spike_4", "Crystal spike_5")),
                ceilingFringeSprites = Pick($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystal spike.png",
                    "Crystal spike_0", "Crystal spike_1", "Crystal spike_2"),
                lightSprite = S($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Star particle.png"),
                lightTint = new Color(0.45f, 0.85f, 0.95f),
                wallTint = new Color(0.68f, 0.74f, 0.86f),
                midTint = new Color(0.30f, 0.36f, 0.48f),
                veilAlpha = 0.34f, // 데모의 검은 베일 — Sylmare 어둠 연출의 핵심
                hangingFlipY = true,
            },
            new ConstantAssetLibrary.PlanetAssets
            {
                planet = ConstantPlanet.Eidron,
                bgSprites = new[]
                {
                    S($"{Bundle}/Abandoned station/Ancient base Sprites/Sky.png"),
                    S($"{Bundle}/Abandoned station/Ancient base Sprites/planet.png"), // 원경 랜드마크
                },
                nodeSprite = S($"{Bundle}/Abandoned station/Ancient base Sprites/Base boxes.png"),
                padPrefab = P($"{Bundle}/Abandoned station/Prefabs/Door.prefab"),
                particles = new[] { P($"{Bundle}/Abandoned station/Prefabs/Particle System Fog.prefab") },
                groundProfile = AssetDatabase.LoadAssetAtPath<SpriteShape>($"{Bundle}/Abandoned station/Sprite shape/Ground Sprite shape/Base ground.asset"),
                bgMidSprites = new[]
                {
                    S($"{Bundle}/Abandoned station/Ancient base Sprites/Mounts back.png"),
                    S($"{Bundle}/Abandoned station/Ancient base Sprites/Mounts tiled.png"),
                },
                wallPanelSprites = new[] { S($"{Bundle}/Abandoned station/Sprite shape/Tunel walls/Tunels.png") },
                floorPropSprites = Merge(
                    SS($"{Bundle}/Abandoned station/Ancient base Sprites/Base boxes.png"),
                    SS($"{Bundle}/Abandoned station/Ancient base Sprites/Columns.png"),
                    SS($"{Bundle}/Abandoned station/Sprite shape/Mashinery dump/Mashinery dump element.png")),
                hangingPropSprites = Merge(
                    SS($"{Bundle}/Abandoned station/Ancient base Sprites/chain.png"),
                    SS($"{Bundle}/Abandoned station/Ancient base Sprites/Cables.png"),
                    SS($"{Bundle}/Abandoned station/Ancient base Sprites/lamp.png")),
                glowSprites = SS($"{Bundle}/Abandoned station/Ancient base Sprites/Base Core.png"),
                setPieceSprites = Merge(
                    Pick($"{Bundle}/Abandoned station/Ancient base Sprites/Base Core.png",
                        "Base Core_6", "Base Core_0", "Base Core_7"),
                    Pick($"{Bundle}/Abandoned station/Ancient base Sprites/Base boxes.png",
                        "Base boxes_1", "Base boxes_2"),
                    SS($"{Bundle}/Abandoned station/Sprite shape/Mashinery dump/Mashinery dump element.png")),
                pillarSprites = Pick($"{Bundle}/Abandoned station/Ancient base Sprites/Columns.png",
                    "Columns_0", "Columns_1"),
                floorFringeSprites = Merge(
                    Pick($"{Bundle}/Abandoned station/Ancient base Sprites/Base boxes.png",
                        "Base boxes_0", "Base boxes_3"),
                    Pick($"{Bundle}/Abandoned station/Ancient base Sprites/Columns.png", "Columns_4"),
                    SS($"{Bundle}/Abandoned station/Sprite shape/Iron Railing/Iron railing.png")),
                ceilingFringeSprites = Merge(
                    SS($"{Bundle}/Abandoned station/Ancient base Sprites/Cables.png"),
                    SS($"{Bundle}/Abandoned station/Ancient base Sprites/lamp.png")),
                lightSprite = Pick($"{Bundle}/Abandoned station/Ancient base Sprites/lamp.png", "lamp_15").FirstOrDefault(),
                lightTint = new Color(1f, 0.95f, 0.75f),
                wallTint = new Color(0.70f, 0.64f, 0.67f),
                midTint = new Color(0.45f, 0.58f, 0.63f), // 청록 공기원근 (데모 Mounts back 틴트)
                veilAlpha = 0.24f,
                hangingFlipY = false, // 사슬/케이블/램프는 원래 매달린 형태
            },
        };
        lib.spiderPrefab = P($"{Bundle}/Dungeon pack/Prefabs/spider.prefab");
        lib.coreSprite = S($"{Bundle}/Abandoned station/Ancient base Sprites/Base Core.png");
        lib.crystalSprite = S($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystals.png");
        lib.koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log("[Constant] 에셋 라이브러리 베이크 완료 → Resources/ConstantAssetLibrary");
    }

    /// <summary>행성 셸 씬 — 카메라/타일맵/플레이어/@Systems 만. 맵은 런타임 생성.</summary>
    private static string BuildPlanetShell(PlanetConfig cfg)
    {
        Ctx c = NewScene(cfg.sceneName, cfg.planet, cfg.bgColor, cfg.groundProfile, camY: 100f);

        // 플레이어는 부트스트랩이 위치를 재설정한다
        c.spawnGrid = new Vector2Int(110, 150);
        c.cameraStart = new Vector3(110f, 152f, -10f);
        c.useConsumables = true; // 로프/폭탄/도감 컨트롤러

        // 셸 시스템 구성 (Finish 의 공통 배선 + 부트스트랩)
        string path = Finish(c);

        // Finish 가 저장한 씬을 다시 열어 부트스트랩 추가
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var sys = GameObject.Find("@Systems");
        if (sys != null && sys.GetComponent<ConstantStageBootstrap>() == null)
        {
            var boot = sys.AddComponent<ConstantStageBootstrap>();
            SetSerializedEnum(boot, "_planet", (int)cfg.planet);
            if (cfg.lavaHazard && sys.GetComponent<LavaHazard>() == null) sys.AddComponent<LavaHazard>();
            if (cfg.visionRadius > 0f && sys.GetComponent<PlayerVision>() == null)
            {
                var vision = sys.AddComponent<PlayerVision>();
                vision.BaseRadius = cfg.visionRadius;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }
        return path;
    }

    private static void SetSerializedEnum(Object component, string fieldName, int value)
    {
        var so = new SerializedObject(component);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.enumValueIndex = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    // ═════════════════════════════════════════════════════════════
    // 행성 설정
    // ═════════════════════════════════════════════════════════════
    private class PlanetConfig
    {
        public string sceneName;
        public ConstantPlanet planet;
        public int seed;
        public Color bgColor;
        public string groundProfile;
        public string[] bgSprites;
        public Color bgEastTint = Color.white;
        public string nodeName, richNodeName, nodeSprite;
        public Color nodeTint = Color.white;
        public string[] itemPool;
        public PropertyTag vaultTag;
        public string vaultDoorName;
        public bool hasCore;      // 라베르니스: 보호핵
        public bool hasShrine;    // 실마레: 침묵의 사당
        public bool protocolSwitches; // 에이드론
        public string observerNode, observerName;
        public Color observerColor;
        public string padPrefab, padLabel;
        public string moodLabel;
        public bool lavaHazard;
        public float visionRadius;
        public string[] decoParticles;
    }

    private static PlanetConfig LavernisConfig(int seed) => new PlanetConfig
    {
        sceneName = "Constant_Lavernis",
        planet = ConstantPlanet.Lavernis,
        seed = seed,
        bgColor = new Color(0.055f, 0.016f, 0f), // 데모 씬 void 색
        groundProfile = $"{Bundle}/Lava dungeon pack/Sprite shapes/Lava ground.asset",
        bgSprites = new[] { $"{Bundle}/Lava dungeon pack/Sprites/Lava Background A.png" },
        nodeName = "용암 결정", richNodeName = "순수 용암 결정",
        nodeSprite = $"{Bundle}/Dungeon pack/Sprites/cristals.png",
        nodeTint = new Color(1f, 0.65f, 0.45f),
        itemPool = new[] { "emberFlask", "meteorCan", "overFuse" },
        vaultTag = PropertyTag.Heat, vaultDoorName = "균열 암벽",
        hasCore = true,
        observerNode = "Obs_Lavernis", observerName = "고장 난 자판기",
        observerColor = new Color(0.85f, 0.45f, 0.45f),
        padPrefab = $"{Bundle}/Lava dungeon pack/Prefabs/DOOR.prefab", padLabel = "출항 [X]",
        moodLabel = "주민들은 약한 불을 오래 지킨다",
        lavaHazard = true,
        decoParticles = new[] { $"{Bundle}/Lava dungeon pack/Prefabs/Spark particle.prefab" },
    };

    private static PlanetConfig SylmareConfig(int seed) => new PlanetConfig
    {
        sceneName = "Constant_Sylmare",
        planet = ConstantPlanet.Sylmare,
        seed = seed,
        bgColor = new Color(0.10f, 0.15f, 0.24f), // 데모 스틸블루를 어둡게
        groundProfile = $"{Bundle}/Cristal Dungeon sprite pack/Sprite shape/Crystal ground.asset",
        bgSprites = new[]
        {
            $"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Background A.png",
            $"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Background B.png",
            $"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Background C.png",
            $"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Background D.png",
        },
        nodeName = "공명 크리스탈", richNodeName = "순수 크리스탈",
        nodeSprite = $"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystals.png",
        itemPool = new[] { "echoBell", "tideMirror", "silenceCrystal" },
        vaultTag = PropertyTag.Echo, vaultDoorName = "얼어붙은 말의 벽",
        hasShrine = true,
        observerNode = "Obs_Sylmare", observerName = "형체가 흐릿한 탐사자",
        observerColor = new Color(0.7f, 0.65f, 0.9f),
        padPrefab = $"{Bundle}/Cristal Dungeon sprite pack/Crystal prefabs/Crystal.prefab", padLabel = "출항 [X]",
        moodLabel = "이곳에서는 말이 천천히 얼어붙는다",
        visionRadius = 3.5f,
        decoParticles = new[]
        {
            $"{Bundle}/Cristal Dungeon sprite pack/Crystal prefabs/Particles/Dust Particle.prefab",
            $"{Bundle}/Cristal Dungeon sprite pack/Crystal prefabs/Particles/Stars Particle.prefab",
        },
    };

    private static PlanetConfig EidronConfig(int seed) => new PlanetConfig
    {
        sceneName = "Constant_Eidron",
        planet = ConstantPlanet.Eidron,
        seed = seed,
        bgColor = new Color(0f, 0.067f, 0.113f), // 데모 씬 하늘색
        groundProfile = $"{Bundle}/Abandoned station/Sprite shape/Ground Sprite shape/Base ground.asset",
        bgSprites = new[] { $"{Bundle}/Abandoned station/Ancient base Sprites/A Base Gradient.png" },
        nodeName = "규격 고철", richNodeName = "예비 부품",
        nodeSprite = $"{Bundle}/Abandoned station/Ancient base Sprites/Base boxes.png",
        itemPool = new[] { "compass", "relayThread", "meteorCan" },
        vaultTag = PropertyTag.Machine, vaultDoorName = "동력이 끊긴 문",
        protocolSwitches = true,
        observerNode = "Obs_Eidron", observerName = "검표원",
        observerColor = new Color(0.6f, 0.75f, 0.65f),
        padPrefab = $"{Bundle}/Abandoned station/Prefabs/Door.prefab", padLabel = "귀환 [X]",
        moodLabel = "게시: 모든 절차에는 이유가 있다",
        decoParticles = new[] { $"{Bundle}/Abandoned station/Prefabs/Particle System Fog.prefab" },
    };

    // ═════════════════════════════════════════════════════════════
    // 룸 배치 자료구조
    // ═════════════════════════════════════════════════════════════
    private class RoomBox
    {
        public int x0, y0, x1, y1;      // 포함 경계 (벽 포함)
        public bool isMain;
        public bool isVertical;         // 유형 (메인 세로형 / 복도 세로형)
        public RoomBox parent;
        public int depth;
        public int doorCount;
        public int soleDoorSide = -1;   // 0=L 1=R 2=U 3=D (첫 문 기준)
        public readonly List<Vector2Int> spots = new List<Vector2Int>();      // 콘텐츠 후보 자리
        public readonly List<Vector2Int> enemySpots = new List<Vector2Int>();

        public int W => x1 - x0 + 1;
        public int H => y1 - y0 + 1;
        public int CX => (x0 + x1) / 2;
        public Vector3 Center => new Vector3((x0 + x1 + 1) * 0.5f, (y0 + y1 + 1) * 0.5f, 0f);

        public bool Overlaps(RoomBox o, int margin) =>
            x0 - margin <= o.x1 && x1 + margin >= o.x0 &&
            y0 - margin <= o.y1 && y1 + margin >= o.y0;
    }

    // ═════════════════════════════════════════════════════════════
    // 행성 생성
    // ═════════════════════════════════════════════════════════════
    private static string BuildPlanet(PlanetConfig cfg)
    {
        var rng = new System.Random(cfg.seed);

        Ctx c = NewScene(cfg.sceneName, cfg.planet, cfg.bgColor, cfg.groundProfile, camY: 0f);
        c.useLavaHazard = cfg.lavaHazard;
        if (cfg.visionRadius > 0f) c.Vision(cfg.visionRadius);
        c.useConsumables = true;

        // ── 1) 방 배치 (트리 연결 — 모든 방 도달 보장) ──
        var rooms = new List<RoomBox>();
        var mains = new List<RoomBox>();
        var corridors = new List<RoomBox>();
        var carves = new List<Vector2Int>();

        RoomBox startRoom = new RoomBox
        {
            x0 = CanvasW / 2 - 16, y0 = CanvasH - 60,
            isMain = true, isVertical = false, depth = 0,
        };
        var sz = MainSize(rng, false);
        startRoom.x1 = startRoom.x0 + sz.x - 1;
        startRoom.y1 = startRoom.y0 + sz.y - 1;
        rooms.Add(startRoom); mains.Add(startRoom);

        int attempts = 0;
        while (mains.Count < MainRoomTarget && attempts++ < 800)
        {
            RoomBox a = mains[rng.Next(mains.Count)];
            int side = WeightedSide(rng); // 0L 1R 2U 3D

            bool horizontal = side <= 1;
            bool bVertical = rng.NextDouble() < 0.3;
            Vector2Int corrSz = CorridorSize(rng, horizontal);
            Vector2Int mainSz = MainSize(rng, bVertical);

            if (!TryPlace(rng, a, side, corrSz, mainSz, rooms,
                out RoomBox corr, out RoomBox b, carves))
                continue;

            corr.parent = a; corr.depth = a.depth; corr.isMain = false;
            b.parent = a; b.depth = a.depth + 1; b.isMain = true; b.isVertical = bVertical;
            b.doorCount = 1; b.soleDoorSide = OppositeSide(side);
            a.doorCount++;

            rooms.Add(corr); corridors.Add(corr);
            rooms.Add(b); mains.Add(b);
        }

        if (mains.Count < 4)
            Debug.LogWarning($"[Constant] {cfg.sceneName}: 메인 룸 {mains.Count}개만 배치됨 (시드 변경 권장)");

        // ── 2) 셸 페인팅 + 내부 퍼니싱 ──
        int lavaBudget = 18;
        foreach (var room in rooms)
        {
            PaintShell(c, room);
            Furnish(c, cfg, rng, room, ref lavaBudget);
        }

        // ── 3) 문 뚫기 ──
        foreach (var p in carves)
            c.RemoveAt(p.x, p.y);

        // ── 4) 카메라 룸 ──
        foreach (var room in rooms)
            c.RoomRect(room.x0, room.y0, room.x1, room.y1);

        // ── 5) 콘텐츠 배치 ──
        RoomBox exitRoom = PickExitRoom(mains, startRoom);
        PlaceContent(c, cfg, rng, rooms, mains, corridors, startRoom, exitRoom);

        // ── 6) 드레싱: 방마다 배경 패널 ──
        int bgIdx = 0;
        foreach (var room in mains)
        {
            string bg = cfg.bgSprites[bgIdx++ % cfg.bgSprites.Length];
            c.SpriteFit(bg, room.Center, "Background", 0, room.H + 4f);
        }
        for (int i = 0; i < cfg.decoParticles.Length && i < mains.Count; i++)
            c.Prefab(cfg.decoParticles[i], mains[(i * 3 + 1) % mains.Count].Center);

        // 카메라 시작 위치
        c.cameraStart = c.World(c.spawnGrid.x, c.spawnGrid.y) + new Vector3(0, 2, -10);

        return Finish(c);
    }

    private static Vector2Int MainSize(System.Random rng, bool vertical) =>
        vertical
            ? new Vector2Int(rng.Next(23, 26), rng.Next(45, 49))   // 세로형 23~25 x 45~48
            : new Vector2Int(rng.Next(30, 34), rng.Next(18, 21));  // 가로형 30~33 x 18~20

    private static Vector2Int CorridorSize(System.Random rng, bool horizontal) =>
        horizontal
            ? new Vector2Int(rng.Next(15, 21), rng.Next(10, 16))   // 가로 복도 15~20 x 10~15
            : new Vector2Int(rng.Next(10, 16), rng.Next(25, 31));  // 세로 복도 10~15 x 25~30

    private static int WeightedSide(System.Random rng)
    {
        int roll = rng.Next(100);
        if (roll < 30) return 0;      // L
        if (roll < 60) return 1;      // R
        if (roll < 78) return 3;      // D (하강 선호)
        return 2;                     // U
    }

    private static int OppositeSide(int side) => side switch { 0 => 1, 1 => 0, 2 => 3, _ => 2 };

    /// <summary>A의 side 쪽에 복도+메인을 붙인다. 가로 연결은 바닥 정렬, 세로 연결은 x 포함 관계 보장.</summary>
    private static bool TryPlace(System.Random rng, RoomBox a, int side,
        Vector2Int corrSz, Vector2Int mainSz, List<RoomBox> placed,
        out RoomBox corr, out RoomBox b, List<Vector2Int> carves)
    {
        corr = new RoomBox(); b = new RoomBox();

        if (side <= 1) // 가로 연결 — 바닥 정렬
        {
            corr.y0 = a.y0; corr.y1 = corr.y0 + corrSz.y - 1;
            b.y0 = a.y0; b.y1 = b.y0 + mainSz.y - 1;

            if (side == 1) // 오른쪽
            {
                corr.x0 = a.x1 + 1; corr.x1 = corr.x0 + corrSz.x - 1;
                b.x0 = corr.x1 + 1; b.x1 = b.x0 + mainSz.x - 1;
            }
            else // 왼쪽
            {
                corr.x1 = a.x0 - 1; corr.x0 = corr.x1 - corrSz.x + 1;
                b.x1 = corr.x0 - 1; b.x0 = b.x1 - mainSz.x + 1;
            }
        }
        else // 세로 연결 — 복도 x는 A 안에, B는 복도를 x로 포함
        {
            int cxMin = a.x0 + 3, cxMax = a.x1 - 3 - corrSz.x + 1;
            if (cxMax < cxMin) return false;
            corr.x0 = rng.Next(cxMin, cxMax + 1); corr.x1 = corr.x0 + corrSz.x - 1;

            int bxMin = Mathf.Max(2, corr.x1 + 3 - mainSz.x + 1);
            int bxMax = corr.x0 - 3;
            if (bxMax < bxMin) return false;
            b.x0 = rng.Next(bxMin, bxMax + 1); b.x1 = b.x0 + mainSz.x - 1;
            if (b.x0 > corr.x0 - 2 || b.x1 < corr.x1 + 2) return false;

            if (side == 3) // 아래로
            {
                corr.y1 = a.y0 - 1; corr.y0 = corr.y1 - corrSz.y + 1;
                b.y1 = corr.y0 - 1; b.y0 = b.y1 - mainSz.y + 1;
            }
            else // 위로
            {
                corr.y0 = a.y1 + 1; corr.y1 = corr.y0 + corrSz.y - 1;
                b.y0 = corr.y1 + 1; b.y1 = b.y0 + mainSz.y - 1;
            }
        }

        // 캔버스/충돌 검사 — margin 0: 벽끼리 딱 붙는 인접은 허용, 실제 겹침만 거부
        foreach (var r in new[] { corr, b })
        {
            if (r.x0 < 2 || r.y0 < 2 || r.x1 > CanvasW - 3 || r.y1 > CanvasH - 3) return false;
            foreach (var p in placed)
                if (r.Overlaps(p, 0)) return false;
        }
        corr.isVertical = side >= 2;

        // 문 셀 기록
        if (side <= 1)
        {
            int fy = a.y0;
            RoomBox left = side == 1 ? a : b;
            RoomBox right = side == 1 ? b : a;
            // A|복도 경계, 복도|B 경계 — 각 2겹 벽 x 3칸 높이
            foreach (int wx in new[] { left.x1, left.x1 + 1, right.x0 - 1, right.x0 })
                for (int dy = 2; dy <= 4; dy++)
                    carves.Add(new Vector2Int(wx, fy + dy));
        }
        else
        {
            RoomBox top = side == 2 ? b : a;
            RoomBox bottom = side == 2 ? a : b;
            int cx = corr.CX;
            for (int dx = -1; dx <= 1; dx++)
            {
                // 위쪽 방 바닥(2겹) + 복도 천장
                carves.Add(new Vector2Int(cx + dx, top.y0));
                carves.Add(new Vector2Int(cx + dx, top.y0 + 1));
                carves.Add(new Vector2Int(cx + dx, corr.y1));
                // 복도 바닥(2겹) + 아래 방 천장
                carves.Add(new Vector2Int(cx + dx, corr.y0));
                carves.Add(new Vector2Int(cx + dx, corr.y0 + 1));
                carves.Add(new Vector2Int(cx + dx, bottom.y1));
            }
        }
        return true;
    }

    private static RoomBox PickExitRoom(List<RoomBox> mains, RoomBox start)
    {
        // 가로 문(좌/우) 리프 우선 — 게이트가 문과 패드 사이를 확실히 가른다
        var sideLeaves = mains.Where(m => m != start && m.doorCount == 1 && m.soleDoorSide <= 1).ToList();
        if (sideLeaves.Count > 0) return sideLeaves.OrderByDescending(m => m.depth).First();

        var leaves = mains.Where(m => m != start && m.doorCount == 1).ToList();
        var pool = leaves.Count > 0 ? leaves : mains.Where(m => m != start).ToList();
        if (pool.Count == 0) return start; // 배치 실패 방어
        return pool.OrderByDescending(m => m.depth).First();
    }

    // ── 방 셸: 좌우 벽 + 천장 1겹 + 바닥 2겹 ──
    private static void PaintShell(Ctx c, RoomBox r)
    {
        c.FloorSpan(r.x0, r.x1, r.y0);
        c.FloorSpan(r.x0, r.x1, r.y0 + 1);
        c.FloorSpan(r.x0, r.x1, r.y1);
        c.Wall(r.x0, r.y0, r.y1);
        c.Wall(r.x1, r.y0, r.y1);
    }

    // ── 내부 퍼니싱 (유형별) ──
    private static void Furnish(Ctx c, PlanetConfig cfg, System.Random rng, RoomBox r, ref int lavaBudget)
    {
        if (!r.isMain)
        {
            if (!r.isVertical) // 가로 복도 — 이벤트 장소
            {
                r.spots.Add(new Vector2Int(r.CX - 3, r.y0 + 2));
                r.spots.Add(new Vector2Int(r.CX, r.y0 + 2));
                r.spots.Add(new Vector2Int(r.CX + 3, r.y0 + 2));
            }
            else // 세로 복도 — 낙하 통로 + 복귀 사다리 + 착지 발판
            {
                int cx = r.CX;
                c.LadderCol(cx, r.y0 + 2, r.y1 - 1);
                bool leftSide = rng.NextDouble() < 0.5;
                for (int y = r.y0 + 6; y <= r.y1 - 5; y += 6)
                {
                    if (leftSide) c.FloorSpan(r.x0 + 2, cx - 1, y);
                    else c.FloorSpan(cx + 1, r.x1 - 2, y);
                    leftSide = !leftSide;
                }
                r.spots.Add(new Vector2Int(cx - 2, r.y0 + 2));
            }
            return;
        }

        if (!r.isVertical) // 메인 가로형 — 2단 내부 층
        {
            int[] layers = { r.y0 + 6, r.y0 + 12 };
            foreach (int ly in layers)
            {
                if (ly > r.y1 - 4) continue;
                int gap1 = rng.Next(r.x0 + 4, r.x1 - 10);
                int gap2 = rng.Next(gap1 + 5, Mathf.Min(gap1 + 14, r.x1 - 5));
                for (int x = r.x0 + 2; x <= r.x1 - 2; x++)
                {
                    if (x >= gap1 && x < gap1 + 4) continue;
                    if (x >= gap2 && x < gap2 + 4) continue;
                    c.Ground(x, ly);
                }
                r.spots.Add(new Vector2Int(rng.Next(r.x0 + 3, r.x1 - 3), ly + 1));
            }

            // 사다리(60%): 바닥→위층
            if (rng.NextDouble() < 0.6)
                c.LadderCol(rng.Next(r.x0 + 3, r.x1 - 3), r.y0 + 2, layers[1] < r.y1 - 3 ? layers[1] : layers[0]);

            // 해저드 한 줄
            int hy = layers[rng.Next(layers.Length)];
            int hx = rng.Next(r.x0 + 4, r.x1 - 7);
            for (int i = 0; i < 3; i++)
                PlaceHazard(c, cfg, hx + i, hy + 1, ref lavaBudget);

            r.spots.Add(new Vector2Int(r.x0 + 4, r.y0 + 2));
            r.spots.Add(new Vector2Int(r.CX, r.y0 + 2));
            r.spots.Add(new Vector2Int(r.x1 - 4, r.y0 + 2));
            r.enemySpots.Add(new Vector2Int(r.CX, r.y0 + 2));
        }
        else // 메인 세로형 — 지그재그 하강 층 + 관통 사다리(70%)
        {
            bool gapLeft = rng.NextDouble() < 0.5;
            int hazardLayer1 = rng.Next(1, 4), hazardLayer2 = rng.Next(4, 7);
            int li = 0;
            for (int ly = r.y0 + 6; ly <= r.y1 - 4; ly += 6, li++)
            {
                for (int x = r.x0 + 2; x <= r.x1 - 2; x++)
                {
                    bool inGap = gapLeft ? (x <= r.x0 + 6) : (x >= r.x1 - 6);
                    if (!inGap) c.Ground(x, ly);
                }
                if (li == hazardLayer1 || li == hazardLayer2)
                {
                    int hx = gapLeft ? r.x1 - 7 : r.x0 + 3;
                    for (int i = 0; i < 3; i++)
                        PlaceHazard(c, cfg, hx + i, ly + 1, ref lavaBudget);
                }
                r.spots.Add(new Vector2Int(gapLeft ? r.x1 - 4 : r.x0 + 4, ly + 1));
                if (li == 2) r.enemySpots.Add(new Vector2Int(r.CX, ly + 1));
                gapLeft = !gapLeft;
            }
            if (rng.NextDouble() < 0.7)
                c.LadderCol(r.CX, r.y0 + 2, r.y1 - 2);
            r.spots.Add(new Vector2Int(r.CX, r.y0 + 2));
        }
    }

    private static void PlaceHazard(Ctx c, PlanetConfig cfg, int x, int y, ref int lavaBudget)
    {
        if (cfg.planet == ConstantPlanet.Lavernis && lavaBudget > 0) { c.Lava(x, y); lavaBudget--; }
        else if (cfg.planet == ConstantPlanet.Eidron) c.Gas(x, y, GPulse);
        else if (cfg.planet == ConstantPlanet.Sylmare) c.Disguised(x, y - 1, DFake); // 층 자체가 허상
        else c.Spike(x, y);
    }

    // ── 콘텐츠 배치 ──
    private static void PlaceContent(Ctx c, PlanetConfig cfg, System.Random rng,
        List<RoomBox> rooms, List<RoomBox> mains, List<RoomBox> corridors,
        RoomBox startRoom, RoomBox exitRoom)
    {
        // 시작: 스폰 + 관측자 + 무드
        c.spawnGrid = new Vector2Int(startRoom.x0 + 4, startRoom.y0 + 2);
        c.Observer(cfg.observerNode, cfg.observerName, startRoom.x0 + 10, startRoom.y0 + 2, cfg.observerColor);
        c.Label(cfg.moodLabel, c.World(startRoom.CX, startRoom.y0 + 5), 2.6f, new Color(0.95f, 0.95f, 1f, 0.8f));
        c.Label("밸브 3개 → 출구 개방 · R 로프 · F 폭탄 · I 가방", c.World(startRoom.CX, startRoom.y0 + 4), 2.0f, new Color(1f, 1f, 1f, 0.55f));
        startRoom.spots.Clear(); startRoom.enemySpots.Clear();

        // 출구: 단일 문 반대편에 패드, 그 앞을 전고 게이트로 봉쇄
        bool doorOnLeft = exitRoom.soleDoorSide == 0;
        int padX = doorOnLeft ? exitRoom.x1 - 4 : exitRoom.x0 + 4;
        int gateX = doorOnLeft ? padX - 5 : padX + 5;
        c.DeparturePad(padX, exitRoom.y0 + 2, cfg.padPrefab, cfg.padLabel);
        GameObject exitGate = c.Gate(gateX, exitRoom.y0 + 2, exitRoom.H - 3, new Color(0.5f, 0.55f, 0.6f));
        c.Label("출구 게이트 — 밸브 3개", c.World(gateX, exitRoom.y0 + 5), 2.0f, new Color(1f, 1f, 1f, 0.6f));
        exitRoom.spots.RemoveAll(s => Mathf.Abs(s.x - padX) < 8);
        exitRoom.enemySpots.Clear();

        var questGo = c.NewQuestObject("@Quest_ExitValves");
        var valveQuest = questGo.AddComponent<ValveQuest>();
        var soQ = new SerializedObject(valveQuest);
        soQ.FindProperty("_valveGoal").intValue = 3;
        soQ.FindProperty("_gate").objectReferenceValue = exitGate;
        if (cfg.hasCore)
        {
            GameObject relic = c.ItemPickup("coolantCore", padX + (doorOnLeft ? -2 : 2), exitRoom.y0 + 2);
            relic.SetActive(false);
            soQ.FindProperty("_rewardItem").objectReferenceValue = relic;
        }
        soQ.ApplyModifiedPropertiesWithoutUndo();

        // 밸브 4개: 복도 2 + 메인 2
        int valves = 0;
        foreach (var corr in corridors.OrderBy(_ => rng.Next()).Take(2))
        {
            var s = TakeSpot(corr, rng); if (s == null) continue;
            c.Valve(valveQuest, s.Value.x, s.Value.y, "밸브");
            valves++;
        }
        foreach (var m in mains.Where(m => m != startRoom && m != exitRoom).OrderBy(_ => rng.Next()))
        {
            if (valves >= 4) break;
            var s = TakeSpot(m, rng); if (s == null) continue;
            c.Valve(valveQuest, s.Value.x, s.Value.y, "밸브");
            valves++;
        }

        // 퀘스트 방 (보호핵 / 사당)
        var questCandidates = mains.Where(m => m != startRoom && m != exitRoom).OrderBy(_ => rng.Next()).ToList();
        if ((cfg.hasCore || cfg.hasShrine) && questCandidates.Count > 0)
        {
            var qr = questCandidates[0];
            var s = TakeSpot(qr, rng) ?? new Vector2Int(qr.CX, qr.y0 + 2);
            if (cfg.hasCore)
            {
                c.Core(valveQuest, s.x, s.y);
                c.Label("보호핵 — 뜯으면 출구가 강제로 열린다. 마을은 식는다.", c.World(s.x, s.y + 3), 2.0f, new Color(1f, 0.6f, 0.5f, 0.8f));
            }
            else c.Shrine(s.x, s.y);
        }

        // 태그 금고: 랜덤 메인 방의 공중 단상 (로프 + 태그 이중 잠금)
        if (questCandidates.Count > 1)
        {
            var vr = questCandidates[1];
            int vy = vr.y0 + (vr.isVertical ? 3 : 9); // 세로방은 낮게, 가로방은 높게
            if (!vr.isVertical)
            {
                int vx = vr.x1 - 10;
                c.FloorSpan(vx, vr.x1 - 2, vy);          // 단상
                GameObject door = c.Gate(vx + 2, vy + 1, 3, new Color(0.6f, 0.5f, 0.65f));
                c.ResourceNode(cfg.richNodeName, cfg.nodeSprite, vx + 4, vy + 1, cfg.nodeTint, 30f);
                c.ResourceNode(cfg.richNodeName, cfg.nodeSprite, vx + 6, vy + 1, cfg.nodeTint, 30f);
                c.TagGateAt(vx + 1, vy + 1, cfg.vaultDoorName, cfg.vaultTag, 2, door, null,
                    "봉인이 풀렸다 — 안쪽이 열린다", new Color(0.8f, 0.7f, 0.9f));
            }
            else
            {
                var s = TakeSpot(vr, rng) ?? new Vector2Int(vr.CX, vr.y0 + 2);
                GameObject door = c.Gate(s.x + 2, s.y, 3, new Color(0.6f, 0.5f, 0.65f));
                c.ResourceNode(cfg.richNodeName, cfg.nodeSprite, s.x + 4, s.y, cfg.nodeTint, 30f);
                c.TagGateAt(s.x, s.y, cfg.vaultDoorName, cfg.vaultTag, 2, door, null,
                    "봉인이 풀렸다 — 안쪽이 열린다", new Color(0.8f, 0.7f, 0.9f));
            }
        }

        // 에이드론: 규약 스위치 3 (위→아래) + 유품
        if (cfg.protocolSwitches && questCandidates.Count > 2)
        {
            var protocol = c.NewQuestObject("@Quest_Protocol").AddComponent<ProtocolQuest>();
            var swRooms = questCandidates.Skip(2).Take(3)
                .OrderByDescending(m => m.y0).ToList();
            Vector2Int last = new Vector2Int(0, 0);
            for (int i = 0; i < swRooms.Count; i++)
            {
                var s = TakeSpot(swRooms[i], rng) ?? new Vector2Int(swRooms[i].CX, swRooms[i].y0 + 2);
                c.Switch(protocol, i + 1, s.x, s.y);
                last = s;
            }
            GameObject ring = c.ItemPickup("commandRing", last.x + 2, last.y);
            ring.SetActive(false);
            var soP = new SerializedObject(protocol);
            soP.FindProperty("_rewardItem").objectReferenceValue = ring;
            soP.ApplyModifiedPropertiesWithoutUndo();
        }

        // 이벤트 스테이션: 복도에 상점/보급/복제기/대출
        var eventCorrs = corridors.Where(cr => cr.spots.Count > 0).OrderBy(_ => rng.Next()).ToList();
        for (int i = 0; i < eventCorrs.Count && i < 4; i++)
        {
            var cr = eventCorrs[i];
            switch (i)
            {
                case 0: // 상점 (가로 복도면 3상품)
                    var s0 = TakeSpot(cr, rng); if (s0 == null) break;
                    c.ShopAt(s0.Value.x, s0.Value.y, 0);
                    var s0b = TakeSpot(cr, rng); if (s0b != null) c.ShopAt(s0b.Value.x, s0b.Value.y, 1);
                    var s0c = TakeSpot(cr, rng); if (s0c != null) c.ShopAt(s0c.Value.x, s0c.Value.y, 2);
                    break;
                case 1: var s1 = TakeSpot(cr, rng); if (s1 != null) c.SupplyAt(s1.Value.x, s1.Value.y); break;
                case 2: var s2 = TakeSpot(cr, rng); if (s2 != null) c.ReplicatorAt(s2.Value.x, s2.Value.y); break;
                case 3: var s3 = TakeSpot(cr, rng); if (s3 != null) c.LoanAt(s3.Value.x, s3.Value.y); break;
            }
        }

        // 아이템 풀 → 랜덤 메인 방
        foreach (string itemId in cfg.itemPool)
        {
            var room = mains[rng.Next(mains.Count)];
            var s = TakeSpot(room, rng);
            if (s == null) { var f = c.FindFloorSpotRect(room.x0, room.y0, room.x1, room.y1, out bool ok); if (!ok) continue; s = f; }
            c.ItemPickup(itemId, s.Value.x, s.Value.y);
        }

        // 남은 자리: 노드/소모품
        foreach (var room in rooms)
        {
            foreach (var s in room.spots)
            {
                double roll = rng.NextDouble();
                if (roll < 0.45) c.ResourceNode(cfg.nodeName, cfg.nodeSprite, s.x, s.y, cfg.nodeTint);
                else if (roll < 0.55) c.ResourceNode(cfg.richNodeName, cfg.nodeSprite, s.x, s.y, cfg.nodeTint, 30f);
                else if (roll < 0.75) c.Consumable(s.x, s.y, rng.NextDouble() < 0.5);
            }
            foreach (var e in room.enemySpots)
            {
                if (rng.NextDouble() < 0.65)
                    c.Enemy(Mathf.Max(room.x0 + 2, e.x - 4), Mathf.Min(room.x1 - 2, e.x + 4), e.y,
                        1.8f + (float)rng.NextDouble() * 0.8f);
            }
        }
    }

    private static Vector2Int? TakeSpot(RoomBox room, System.Random rng)
    {
        if (room.spots.Count == 0) return null;
        int i = rng.Next(room.spots.Count);
        var s = room.spots[i];
        room.spots.RemoveAt(i);
        return s;
    }

    // ═════════════════════════════════════════════════════════════
    // 허브 — 여행선 '컨스턴트' (수제 유지)
    // ═════════════════════════════════════════════════════════════
    private static string BuildHub()
    {
        Ctx c = NewScene("Constant_Hub", ConstantPlanet.Hub,
            new Color(0.05f, 0.06f, 0.10f),
            $"{Bundle}/Abandoned station/Sprite shape/Ground Sprite shape/Base ground.asset",
            camY: 4.5f);

        c.RoomRect(0, 0, HubW - 1, 15);
        c.FloorSpan(0, HubW - 1, 0); c.FloorSpan(0, HubW - 1, 1);
        c.FloorSpan(1, HubW - 2, 15);
        c.Wall(0, 2, 15); c.Wall(HubW - 1, 2, 15);

        c.SpriteFit($"{Bundle}/Abandoned station/Ancient base Sprites/Sky.png", new Vector3(20f, 7.5f), "Background", 0, 18f);
        c.SpriteFit($"{Bundle}/Abandoned station/Ancient base Sprites/Sky.png", new Vector3(60f, 7.5f), "Background", 0, 18f);
        c.SpriteFit($"{Bundle}/Abandoned station/Ancient base Sprites/planet.png", new Vector3(52f, 10.5f), "Background", 2, 5f);
        c.SpriteFit($"{Bundle}/Abandoned station/Ancient base Sprites/Mounts back.png", new Vector3(30f, 4f), "Background", 1, 6f);

        c.Prefab($"{Bundle}/Abandoned station/Prefabs/Core.prefab", new Vector3(6.5f, 2f));
        c.Prefab($"{Bundle}/Abandoned station/Prefabs/Columns B.prefab", new Vector3(12.5f, 2f));
        c.Label("엔진룸", new Vector3(6.5f, 6.5f), 2.0f, new Color(1f, 1f, 1f, 0.4f));

        c.Prefab($"{Bundle}/Abandoned station/Prefabs/Big Box.prefab", new Vector3(24.5f, 2f));
        c.Prefab($"{Bundle}/Abandoned station/Prefabs/Small Box.prefab", new Vector3(36.5f, 2f));
        c.Prefab($"{Bundle}/Abandoned station/Prefabs/Small Box.prefab", new Vector3(40.5f, 2f));
        c.Prefab($"{Bundle}/Abandoned station/Prefabs/Small Box.prefab", new Vector3(44.5f, 2f));
        c.ItemPickup("thermos", 36, 2, isStarter: true);
        c.ItemPickup("echoBell", 40, 2, isStarter: true);
        c.ItemPickup("compass", 44, 2, isStarter: true);
        c.Label("여행용품을 하나 고르세요 (X)", new Vector3(40.5f, 5.6f), 3.4f, new Color(0.9f, 0.9f, 1f, 0.85f));

        c.SpriteFit($"{Bundle}/Abandoned station/Ancient base Sprites/Panel.png", new Vector3(64f, 3.1f), "Objects", 2, 1.6f);
        c.Prefab($"{Bundle}/Abandoned station/Prefabs/Lamp.prefab", new Vector3(18.5f, 9f));
        c.Prefab($"{Bundle}/Abandoned station/Prefabs/Lamp.prefab", new Vector3(50.5f, 9f));
        c.Prefab($"{Bundle}/Abandoned station/Prefabs/Lamp.prefab", new Vector3(66.5f, 9f));
        c.Prefab($"{Bundle}/Abandoned station/Prefabs/Particle System Fog.prefab", new Vector3(40f, 4f));

        c.Observer("Obs_Hub_Intro", "안내 단말", 58, 2, new Color(0.5f, 0.9f, 0.9f), autoPlayHubIntro: true);

        c.useConsumables = true; // 허브에서도 도감(V) 열람 가능
        c.spawnGrid = new Vector2Int(5, 3);
        c.cameraStart = new Vector3(8f, 4.5f, -10f);
        c.DeparturePad(70, 2, $"{Bundle}/Abandoned station/Prefabs/Door.prefab", "출항 [X]");

        return Finish(c);
    }

    // ═════════════════════════════════════════════════════════════
    // 빌드 컨텍스트
    // ═════════════════════════════════════════════════════════════
    private class Ctx
    {
        public string path;
        public ConstantPlanet planet;
        public TileMapData map;
        public Transform deco;
        public Transform gameplay;
        public Vector2Int spawnGrid = new Vector2Int(3, 3);
        public Vector3 cameraStart = new Vector3(8f, 4.5f, -10f);
        public bool useVision;
        public float visionRadius = 4f;
        public bool useLavaHazard;
        public bool useConsumables;

        public Vector3 World(int gx, int gy) => map.GridToWorld(new Vector2Int(gx, gy));

        public void Vision(float radius) { useVision = true; visionRadius = radius; }

        private void Set(int x, int y, TileType type, int variant = 0)
        {
            Vector2Int p = new Vector2Int(x, y);
            map.AddOrReplace(p, type, Vector2.one);
            if (variant != 0)
            {
                TileData t = map.GetTile(p);
                if (t != null) t.variant = variant;
            }
        }

        public void Ground(int x, int y) => Set(x, y, TileType.Ground);
        public void Lava(int x, int y) => Set(x, y, TileType.Lava);
        public void Spike(int x, int y) => Set(x, y, TileType.Spike);
        public void Gas(int x, int y, int variant) => Set(x, y, TileType.Gas, variant);
        public void Disguised(int x, int y, int variant) => Set(x, y, TileType.Disguised, variant);
        public void LadderCol(int x, int y0, int y1) { for (int y = y0; y <= y1; y++) Set(x, y, TileType.Ladder); }

        public void RemoveAt(int x, int y) => map.RemoveTile(new Vector2Int(x, y));

        public void FloorSpan(int x0, int x1, int y) { for (int x = x0; x <= x1; x++) Ground(x, y); }
        public void Wall(int x, int y0, int y1) { for (int y = y0; y <= y1; y++) Ground(x, y); }

        public void RoomRect(int x0, int y0, int x1, int y1)
        {
            map.AddCameraRoom(new Vector2Int(x0, y0), new Vector2Int(x1, y1));
            CameraRoomData room = map.CameraRooms[map.CameraRooms.Count - 1];
            room.roomName = $"Room {map.CameraRooms.Count}";
            room.hasSpawn = true;
            room.spawnGridPos = new Vector2Int(x0 + 4, y0 + 2);
        }

        public Vector2Int FindFloorSpotRect(int rx0, int ry0, int rx1, int ry1, out bool ok)
        {
            int cx = (rx0 + rx1) / 2;
            for (int spread = 0; spread <= (rx1 - rx0) / 2; spread++)
            {
                foreach (int x in new[] { cx - spread, cx + spread })
                {
                    if (x < rx0 + 2 || x > rx1 - 2) continue;
                    for (int y = ry0 + 2; y <= ry1 - 2; y++)
                    {
                        var below = map.GetTile(new Vector2Int(x, y - 1));
                        if (below == null || below.type != TileType.Ground) continue;
                        if (map.GetTile(new Vector2Int(x, y)) != null) continue;
                        if (map.GetTile(new Vector2Int(x, y + 1)) != null) continue;
                        ok = true;
                        return new Vector2Int(x, y);
                    }
                }
            }
            ok = false;
            return new Vector2Int(cx, ry0 + 2);
        }

        // ── 드레싱 ──
        public SpriteRenderer SpriteFit(string assetPath, Vector3 pos, string sortingLayer, int order,
            float targetHeight, Color? tint = null)
        {
            Sprite sprite = LoadSprite(assetPath);
            if (sprite == null) { Debug.LogWarning($"[Constant] 스프라이트 없음: {assetPath}"); return null; }

            GameObject go = new GameObject($"Deco_{sprite.name}");
            go.transform.SetParent(deco, false);
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            if (tint.HasValue) sr.color = tint.Value;

            float h = sprite.bounds.size.y;
            if (h > 0.001f) go.transform.localScale = Vector3.one * (targetHeight / h);
            return sr;
        }

        public GameObject Prefab(string assetPath, Vector3 pos)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) { Debug.LogWarning($"[Constant] 프리팹 없음: {assetPath}"); return null; }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(deco, true);
            go.transform.position = pos;
            return go;
        }

        public void Label(string text, Vector3 pos, float fontSize, Color color)
            => AddWorldLabel(text, pos, fontSize, color, deco);

        // ── 게임플레이 오브젝트 ──
        public GameObject NewQuestObject(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(gameplay, false);
            return go;
        }

        public GameObject ItemPickup(string itemId, int gx, int gy, bool isStarter = false)
        {
            var def = ConstantItemDb.Get(itemId);
            if (def == null) { Debug.LogWarning($"[Constant] 알 수 없는 아이템: {itemId}"); return null; }

            GameObject go = new GameObject($"Item_{itemId}");
            go.transform.SetParent(gameplay, false);
            go.transform.position = World(gx, gy);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSquareSprite();
            sr.color = ConstantDefine.ColorOf(def.property);
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = 10;
            visual.transform.localScale = Vector3.one * 0.62f;

            AddWorldLabel(def.displayName, go.transform.position + new Vector3(0f, 0.85f),
                2.4f, def.isRelic ? new Color(0.91f, 0.77f, 0.42f) : Color.white, go.transform);

            ItemPickup pickup = go.AddComponent<ItemPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("_itemId").stringValue = itemId;
            so.FindProperty("_isStarterChoice").boolValue = isStarter;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        public void ResourceNode(string name, string spritePath, int gx, int gy, Color? tint = null,
            float gauge = 20f)
        {
            GameObject go = new GameObject($"Node_{name}_{gx}_{gy}");
            go.transform.SetParent(gameplay, false);
            go.transform.position = World(gx, gy);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            Sprite sprite = LoadSprite(spritePath);
            if (sprite != null)
            {
                sr.sprite = sprite;
                float h = sprite.bounds.size.y;
                if (h > 0.001f) visual.transform.localScale = Vector3.one * (1.25f / h);
            }
            else
            {
                sr.sprite = LoadSquareSprite();
                visual.transform.localScale = Vector3.one * 0.8f;
            }
            sr.color = tint ?? Color.white;
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = 8;

            AddWorldLabel(name, go.transform.position + new Vector3(0f, 1.05f),
                1.9f, new Color(1f, 1f, 1f, 0.6f), go.transform);

            ResourceNode node = go.AddComponent<ResourceNode>();
            var so = new SerializedObject(node);
            so.FindProperty("_resourceName").stringValue = name;
            so.FindProperty("_gaugePerHarvest").floatValue = gauge;
            so.FindProperty("_charges").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void DeparturePad(int gx, int gy, string visualPrefabPath, string labelText)
        {
            GameObject go = new GameObject("DeparturePad");
            go.transform.SetParent(gameplay, false);
            go.transform.position = World(gx, gy);

            if (!string.IsNullOrEmpty(visualPrefabPath))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(visualPrefabPath);
                if (prefab != null)
                {
                    GameObject vis = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    vis.transform.SetParent(go.transform, true);
                    vis.transform.position = World(gx, gy) + new Vector3(0f, -0.5f);
                }
            }

            AddWorldLabel(labelText, go.transform.position + new Vector3(0f, 2.3f),
                2.8f, new Color(0.91f, 0.77f, 0.42f), go.transform);

            go.AddComponent<DeparturePad>();
            var so = new SerializedObject(go.GetComponent<DeparturePad>());
            var range = so.FindProperty("_detectionRange");
            if (range != null) { range.floatValue = 2.2f; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        public void Valve(ValveQuest quest, int gx, int gy, string label)
        {
            GameObject go = MakeGimmick($"Valve_{gx}_{gy}", gx, gy,
                new Color(0.45f, 0.85f, 1f), 0.7f, label, new Color(0.6f, 0.85f, 1f, 0.85f));
            CoolantValve valve = go.AddComponent<CoolantValve>();
            var so = new SerializedObject(valve);
            so.FindProperty("_quest").objectReferenceValue = quest;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Core(ValveQuest quest, int gx, int gy)
        {
            GameObject go = new GameObject($"ProtectedCore_{gx}_{gy}");
            go.transform.SetParent(gameplay, false);
            go.transform.position = World(gx, gy);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            Sprite core = LoadSprite($"{Bundle}/Abandoned station/Ancient base Sprites/Base Core.png");
            if (core != null)
            {
                sr.sprite = core;
                float h = core.bounds.size.y;
                if (h > 0.001f) visual.transform.localScale = Vector3.one * (2.0f / h);
            }
            else { sr.sprite = LoadSquareSprite(); sr.color = new Color(1f, 0.5f, 0.4f); }
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = 8;

            AddWorldLabel("보호핵", go.transform.position + new Vector3(0f, 1.5f),
                2.2f, new Color(1f, 0.6f, 0.5f), go.transform);

            ProtectedCore comp = go.AddComponent<ProtectedCore>();
            var so = new SerializedObject(comp);
            so.FindProperty("_quest").objectReferenceValue = quest;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Shrine(int gx, int gy)
        {
            GameObject go = new GameObject("SilenceShrine");
            go.transform.SetParent(gameplay, false);
            go.transform.position = World(gx, gy);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            Sprite crystal = LoadSprite($"{Bundle}/Cristal Dungeon sprite pack/Cristal Sprites/Crystals.png");
            if (crystal != null)
            {
                sr.sprite = crystal;
                float h = crystal.bounds.size.y;
                if (h > 0.001f) visual.transform.localScale = Vector3.one * (1.8f / h);
            }
            else sr.sprite = LoadSquareSprite();
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = 8;

            TextMeshPro progressLabel = MakeWorldLabelTMP("…", go.transform.position + new Vector3(0f, 1.6f),
                2.4f, new Color(0.95f, 0.9f, 0.75f), go.transform);
            AddWorldLabel("침묵의 사당 — 곁에서 가만히 기다려 보자", go.transform.position + new Vector3(0f, 2.5f),
                2.0f, new Color(0.8f, 0.7f, 1f, 0.7f), go.transform);

            SilenceShrine shrine = go.AddComponent<SilenceShrine>();
            var so = new SerializedObject(shrine);
            so.FindProperty("_label").objectReferenceValue = progressLabel;
            so.FindProperty("_crystal").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Switch(ProtocolQuest quest, int index, int gx, int gy)
        {
            GameObject go = MakeGimmick($"Switch_{index}", gx, gy,
                new Color(0.95f, 0.8f, 0.4f), 0.7f, $"규약 스위치 {index}", new Color(0.95f, 0.9f, 0.6f, 0.85f));
            SequenceSwitch sw = go.AddComponent<SequenceSwitch>();
            var so = new SerializedObject(sw);
            so.FindProperty("_quest").objectReferenceValue = quest;
            so.FindProperty("_index").intValue = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public GameObject Gate(int gx, int gy, int height, Color? color = null)
        {
            GameObject go = new GameObject($"Gate_{gx}_{gy}");
            go.transform.SetParent(gameplay, false);
            go.transform.position = World(gx, gy) + new Vector3(0f, (height - 1) * 0.5f);
            go.layer = LayerMask.NameToLayer("Ground");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSquareSprite();
            sr.color = color ?? new Color(0.55f, 0.65f, 0.6f);
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = 7;
            go.transform.localScale = new Vector3(1f, height, 1f);

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            return go;
        }

        public void TagGateAt(int gx, int gy, string gateName, PropertyTag tag, int count,
            GameObject blocker, GameObject bridge, string successToast, Color color)
        {
            GameObject go = MakeGimmick($"TagGate_{gateName}", gx, gy,
                color, 0.7f, $"{gateName} [X]", new Color(color.r, color.g, color.b, 0.9f));
            TagGate gate = go.AddComponent<TagGate>();
            var so = new SerializedObject(gate);
            so.FindProperty("_gateName").stringValue = gateName;
            so.FindProperty("_requiredTag").enumValueIndex = (int)tag;
            so.FindProperty("_requiredCount").intValue = count;
            so.FindProperty("_blocker").objectReferenceValue = blocker;
            so.FindProperty("_bridge").objectReferenceValue = bridge;
            so.FindProperty("_successToast").stringValue = successToast;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Enemy(int minGx, int maxGx, int gy, float speed = 2f)
        {
            GameObject go = new GameObject($"Enemy_{minGx}_{gy}");
            go.transform.SetParent(gameplay, false);
            go.transform.position = new Vector3((minGx + maxGx + 1) * 0.5f, gy + 0.55f, 0f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{Bundle}/Dungeon pack/Prefabs/spider.prefab");
            if (prefab != null)
            {
                GameObject vis = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                vis.name = "Visual";
                vis.transform.SetParent(go.transform, false);
                vis.transform.localPosition = Vector3.zero;
                foreach (var r in vis.GetComponentsInChildren<SpriteRenderer>())
                {
                    r.sortingLayerName = "Objects";
                    r.sortingOrder = 12;
                }
            }
            else
            {
                GameObject vis = new GameObject("Visual");
                vis.transform.SetParent(go.transform, false);
                SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
                sr.sprite = LoadSquareSprite();
                sr.color = new Color(0.9f, 0.3f, 0.3f);
                sr.sortingLayerName = "Objects";
                sr.sortingOrder = 12;
                vis.transform.localScale = Vector3.one * 0.7f;
            }

            PatrolEnemy enemy = go.AddComponent<PatrolEnemy>();
            var so = new SerializedObject(enemy);
            so.FindProperty("_minX").floatValue = minGx + 0.5f;
            so.FindProperty("_maxX").floatValue = maxGx + 0.5f;
            so.FindProperty("_speed").floatValue = speed;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Observer(string nodeName, string displayName, int gx, int gy, Color color,
            bool autoPlayHubIntro = false)
        {
            GameObject go = MakeGimmick($"Observer_{nodeName}", gx, gy,
                color, 0.85f, $"{displayName} [X]", new Color(color.r, color.g, color.b, 0.9f));
            ConstantObserver obs = go.AddComponent<ConstantObserver>();
            var so = new SerializedObject(obs);
            so.FindProperty("_nodeName").stringValue = nodeName;
            so.FindProperty("_autoPlayHubIntro").boolValue = autoPlayHubIntro;
            var range = so.FindProperty("_detectionRange");
            if (range != null) range.floatValue = 2.0f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Consumable(int gx, int gy, bool bomb)
        {
            GameObject go = MakeGimmick(bomb ? $"Bomb_{gx}_{gy}" : $"Rope_{gx}_{gy}", gx, gy,
                bomb ? new Color(0.25f, 0.25f, 0.28f) : new Color(0.75f, 0.6f, 0.4f), 0.5f,
                bomb ? "폭탄 [X]" : "로프 [X]", new Color(1f, 1f, 1f, 0.7f));
            ConsumablePickup p = go.AddComponent<ConsumablePickup>();
            var so = new SerializedObject(p);
            so.FindProperty("_isBomb").boolValue = bomb;
            so.FindProperty("_amount").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void SupplyAt(int gx, int gy)
        {
            GameObject go = MakeGimmick($"Supply_{gx}_{gy}", gx, gy,
                new Color(0.7f, 0.55f, 0.35f), 0.8f, "보급 상자 [X]", new Color(0.9f, 0.8f, 0.6f, 0.9f));
            go.AddComponent<SupplyCache>();
        }

        public void ShopAt(int gx, int gy, int goodsIndex)
        {
            string[] names = { "로프 2개 — 게이지 10%", "폭탄 2개 — 게이지 10%", "??? — 게이지 15%" };
            float[] costs = { 10f, 10f, 15f };
            int gi = Mathf.Clamp(goodsIndex, 0, 2);

            GameObject go = MakeGimmick($"Shop_{gx}_{gy}", gx, gy,
                new Color(0.9f, 0.75f, 0.4f), 0.7f, $"{names[gi]} [X]", new Color(0.95f, 0.85f, 0.6f, 0.9f));
            ShopItem item = go.AddComponent<ShopItem>();
            var so = new SerializedObject(item);
            so.FindProperty("_goods").enumValueIndex = gi;
            so.FindProperty("_cost").floatValue = costs[gi];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void ReplicatorAt(int gx, int gy)
        {
            GameObject go = MakeGimmick($"Replicator_{gx}_{gy}", gx, gy,
                new Color(0.55f, 0.85f, 0.8f), 0.8f, "복제기 — 무작위 아이템 복제 [X]", new Color(0.7f, 0.95f, 0.9f, 0.9f));
            go.AddComponent<Replicator>();
        }

        public void LoanAt(int gx, int gy)
        {
            GameObject go = MakeGimmick($"Loan_{gx}_{gy}", gx, gy,
                new Color(0.85f, 0.7f, 0.9f), 0.8f, "여행사 대출 — 지금 +30, 다음부터 +20 [X]", new Color(0.9f, 0.8f, 1f, 0.9f));
            go.AddComponent<LoanTerminal>();
        }

        private GameObject MakeGimmick(string name, int gx, int gy, Color color, float scale,
            string labelText, Color labelColor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(gameplay, false);
            go.transform.position = World(gx, gy);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSquareSprite();
            sr.color = color;
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = 9;
            visual.transform.localScale = Vector3.one * scale;

            AddWorldLabel(labelText, go.transform.position + new Vector3(0f, 0.95f),
                2.0f, labelColor, go.transform);
            return go;
        }
    }

    // ═════════════════════════════════════════════════════════════
    // 씬 생성 / 마감
    // ═════════════════════════════════════════════════════════════
    private static Ctx NewScene(string name, ConstantPlanet planet, Color bgColor, string groundProfilePath,
        float camY)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Ctx c = new Ctx { path = $"{SceneFolder}/{name}.unity", planet = planet };

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.backgroundColor = bgColor;
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(8f, camY, -10f);
        EnsureUrpCameraData(camGo);
        CameraController camCtrl = camGo.AddComponent<CameraController>();

        GameObject mapGo = new GameObject("TileMap");
        mapGo.transform.position = Vector3.zero;
        c.map = mapGo.AddComponent<TileMapData>();

        SpriteShape profile = AssetDatabase.LoadAssetAtPath<SpriteShape>(groundProfilePath);
        if (profile != null)
            SetSerializedReference(c.map, "_groundProfile", profile);
        else
            Debug.LogWarning($"[Constant] Ground SpriteShape 프로필 없음: {groundProfilePath}");

        SetSerializedReference(camCtrl, "_cameraRoomSource", c.map);

        c.deco = new GameObject("@Deco").transform;
        c.gameplay = new GameObject("@Gameplay").transform;

        return c;
    }

    private static string Finish(Ctx c)
    {
        c.map.RebuildAll();

        Vector3 spawn = c.map.GridToWorld(c.spawnGrid);

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject player = null;
        if (playerPrefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = spawn;
        }
        else
        {
            Debug.LogWarning($"[Constant] Player 프리팹을 찾을 수 없습니다: {PlayerPrefabPath}");
        }

        if (player != null && player.GetComponent<PlayerTaste>() == null)
            player.AddComponent<PlayerTaste>();

        // 카메라 시작 위치를 스폰 근처로
        Camera mainCam = Object.FindFirstObjectByType<Camera>();
        if (mainCam != null) mainCam.transform.position = c.cameraStart;

        CameraController camCtrl = Object.FindFirstObjectByType<CameraController>();
        PlayerFSM playerFsm = player != null ? player.GetComponent<PlayerFSM>() : null;
        if (camCtrl != null && playerFsm != null)
            SetSerializedReference(camCtrl, "_player", playerFsm);

        GameObject sys = new GameObject("@Systems");
        AddDialogueSystem(sys);
        ConstantSceneController controller = sys.AddComponent<ConstantSceneController>();
        var so = new SerializedObject(controller);
        so.FindProperty("_planet").enumValueIndex = (int)c.planet;
        so.FindProperty("_respawnPoint").vector3Value = spawn;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (c.useLavaHazard) sys.AddComponent<LavaHazard>();
        if (c.useConsumables) sys.AddComponent<ConstantConsumableController>();

        if (c.useVision)
        {
            PlayerVision vision = sys.AddComponent<PlayerVision>();
            vision.BaseRadius = c.visionRadius;
        }

        EditorSceneManager.MarkAllScenesDirty();
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), c.path))
            Debug.LogError($"[Constant] 씬 저장 실패: {c.path}");

        return c.path;
    }

    private static void AddDialogueSystem(GameObject sys)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Packages/dev.yarnspinner.unity/Prefabs/Dialogue System.prefab");
        if (prefab == null)
        {
            Debug.LogWarning("[Constant] Yarn 'Dialogue System' 프리팹을 찾지 못했습니다 — 관측자 대사 생략.");
            return;
        }

        GameObject ds = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        ds.name = "Dialogue System";

        var runner = ds.GetComponentInChildren<Yarn.Unity.DialogueRunner>(true);
        var project = AssetDatabase.LoadAssetAtPath<Yarn.Unity.YarnProject>(
            "Assets/Scripts/Content/Yarn/MainStory.yarnproject");
        if (runner != null && project != null)
            SetSerializedReference(runner, "yarnProject", project);
        else
            Debug.LogWarning("[Constant] DialogueRunner/YarnProject 배선 실패");

        var binder = sys.AddComponent<ConstantStoryBinder>();
        if (runner != null)
            SetSerializedReference(binder, "_runner", runner);
    }

    // ═════════════════════════════════════════════════════════════
    // 유틸
    // ═════════════════════════════════════════════════════════════
    private static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return sprite;
    }

    private static Sprite LoadSquareSprite() =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Square.png");

    private static void AddWorldLabel(string text, Vector3 pos, float fontSize, Color color, Transform parent)
        => MakeWorldLabelTMP(text, pos, fontSize, color, parent);

    private static TextMeshPro MakeWorldLabelTMP(string text, Vector3 pos, float fontSize, Color color, Transform parent)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, true);
        go.transform.position = pos;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = new Vector2(10f, 1.4f);

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "Objects";
            mr.sortingOrder = 20;
        }
        return tmp;
    }

    private static void EnsureUrpCameraData(GameObject camGo)
    {
        var type = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (type != null && camGo.GetComponent(type) == null)
            camGo.AddComponent(type);
    }

    private static void SetSerializedReference(Object component, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning($"[Constant] 직렬화 필드를 찾지 못함: {component.GetType().Name}.{fieldName}");
        }
    }

    private static void EnsureRuntimeSprites()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Sprites");

        const string squarePath = "Assets/Resources/Sprites/Square.png";
        if (!System.IO.File.Exists(squarePath))
        {
            Texture2D tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[64];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(squarePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(squarePath, ImportAssetOptions.ForceSynchronousImport);
        }
        ConfigureSpriteImporter(squarePath, 8);

        const string maskPath = "Assets/Resources/Sprites/VisionMask.png";
        if (!System.IO.File.Exists(maskPath))
        {
            int sz = 128;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[sz * sz];
            float half = sz * 0.5f;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 1f - Mathf.SmoothStep(0.80f, 1.0f, d);
                    px[y * sz + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(maskPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(maskPath, ImportAssetOptions.ForceSynchronousImport);
        }
        ConfigureSpriteImporter(maskPath, 128);

        const string matPath = "Assets/Resources/Materials/VisionDarkness.mat";
        if (!System.IO.File.Exists(matPath))
        {
            Shader shader = Shader.Find("Custom/VisionDarkness");
            if (shader != null)
            {
                EnsureFolder("Assets/Resources/Materials");
                Material mat = new Material(shader) { name = "VisionDarkness" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                Debug.LogWarning("[Constant] 'Custom/VisionDarkness' 셰이더 없음 — 어둠 머티리얼 생성 건너뜀");
            }
        }
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureSpriteImporter(string path, int pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
        if (importer.spritePixelsPerUnit != pixelsPerUnit) { importer.spritePixelsPerUnit = pixelsPerUnit; dirty = true; }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
        if (dirty) importer.SaveAndReimport();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static void EnsureSceneFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder(SceneFolder))
            AssetDatabase.CreateFolder("Assets/Scenes", "Constant");
    }

    private static void AddToBuildSettings(List<string> scenePaths)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (string path in scenePaths)
        {
            if (string.IsNullOrEmpty(path)) continue;
            if (scenes.Exists(s => s.path == path)) continue;
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
