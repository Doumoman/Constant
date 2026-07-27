using UnityEngine;

/// <summary>
/// 스테이지 부트스트랩 — 행성 씬(셸)이 로드될 때마다 새 랜덤 맵을 생성한다.
/// 씬에는 카메라/TileMapData/플레이어/@Systems 만 있고, 지형·콘텐츠는 여기서 만든다.
/// 시드 = 시각 기반 → 같은 씬을 다시 로드해도(스테이지 진행/재도전) 항상 다른 맵.
/// </summary>
public class ConstantStageBootstrap : MonoBehaviour
{
    [SerializeField] private ConstantPlanet _planet = ConstantPlanet.Lavernis;

    private void Awake()
    {
        var map = FindFirstObjectByType<TileMapData>();
        if (map == null)
        {
            Debug.LogError("[Constant] TileMapData 가 씬에 없습니다 — 생성 불가");
            return;
        }

        var lib = ConstantAssetLibrary.Load();
        if (lib == null)
            Debug.LogWarning("[Constant] ConstantAssetLibrary 미베이크 — 폴백 비주얼로 생성합니다 (Tools/Constant/Bake Asset Library)");

        var profile = ConstantPlanetConfigs.For(_planet);
        var run = SingletonManagers.Run;

        int seed = System.Environment.TickCount
                   ^ ((int)_planet * 7919)
                   ^ ((run != null ? run.StageIndex : 1) * 104729);

        var gen = new ConstantMapGenerator(map, lib, profile);
        gen.Generate(seed);

        // 플레이어/카메라/리스폰 배선
        Vector3 spawn = map.GridToWorld(gen.SpawnGrid);
        var player = FindFirstObjectByType<PlayerFSM>();
        if (player != null)
        {
            // 셀 중심에 그대로 두면 콜라이더 발끝이 바닥 타일에 파고들어 끼어버린다.
            // 스폰 셀 바닥면(아래 타일 상단) 위에 발끝이 살짝 뜨도록 y를 보정한다.
            var bc = player.GetComponent<BoxCollider2D>();
            if (bc != null)
            {
                float floorTop = spawn.y - 0.5f;
                float feetFromPivot = bc.offset.y - bc.size.y * 0.5f;
                spawn.y = floorTop - feetFromPivot + 0.05f;
            }
            player.transform.position = spawn;
        }

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(spawn.x + 2f, spawn.y + 2f, cam.transform.position.z);
            cam.backgroundColor = _planet switch
            {
                ConstantPlanet.Lavernis => new Color(0.045f, 0.012f, 0.010f),
                ConstantPlanet.Sylmare => new Color(0.012f, 0.020f, 0.045f),
                _ => new Color(0.015f, 0.027f, 0.035f),
            };
        }

        var controller = FindFirstObjectByType<ConstantSceneController>();
        controller?.SetRespawnPoint(spawn);

        Debug.Log($"[Constant] {_planet} 스테이지 생성 완료 (seed {seed})");
    }
}
