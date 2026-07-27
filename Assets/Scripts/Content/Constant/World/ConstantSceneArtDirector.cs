using UnityEngine;

/// <summary>
/// Constant 씬의 공통 시각 규칙을 런타임에 보강한다.
/// 허브는 수제 씬의 기존 오브젝트를 보존한 채 소팅과 레이어 구성을 바로잡는다.
/// </summary>
public static class ConstantSceneArtDirector
{
    private const string RuntimeRootName = "@ConstantArtDirection";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
        UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "Constant_Hub")
            DecorateHub();
    }

    public static void DecorateHub()
    {
        if (!Application.isPlaying || GameObject.Find(RuntimeRootName) != null)
            return;

        var library = ConstantAssetLibrary.Load();
        var assets = library != null ? library.For(ConstantPlanet.Eidron) : null;
        if (assets == null)
            return;

        Transform existingDeco = GameObject.Find("@Deco")?.transform;
        FixLegacyHubBackground(existingDeco);

        var root = new GameObject(RuntimeRootName).transform;
        if (existingDeco != null)
            root.SetParent(existingDeco, false);

        // 데모 씬처럼 가장 먼 하늘, 중거리 산맥, 실내 벽체를 명확히 분리한다.
        if (assets.bgSprites != null && assets.bgSprites.Length > 0)
        {
            // 허브는 80칸짜리 단일 수평 룸이라 고정 타일이 시차 한 장보다 안정적으로 모든 카메라 구간을 덮는다.
            for (int i = 0; i < 4; i++)
                Fit(assets.bgSprites[0], root, new Vector3(i * 29f, 7.5f, 50f), -51, 18f,
                    new Color(0.72f, 0.84f, 0.90f, 1f), $"FarSky_{i}");
        }

        if (assets.bgMidSprites != null && assets.bgMidSprites.Length > 0)
        {
            for (int i = 0; i < 3; i++)
            {
                Fit(assets.bgMidSprites[i % assets.bgMidSprites.Length], root,
                    new Vector3(12f + i * 28f, 5.4f, 40f), -44, 8.2f,
                    new Color(0.28f, 0.38f, 0.43f, 0.88f), $"MidMount_{i}");
            }
        }

        BuildHubArchitecture(assets, root);

        var cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = new Color(0.018f, 0.026f, 0.043f);
    }

    private static void FixLegacyHubBackground(Transform deco)
    {
        if (deco == null)
            return;

        foreach (var renderer in deco.GetComponentsInChildren<SpriteRenderer>(true))
        {
            string objectName = renderer.transform.root == deco.root
                ? renderer.gameObject.name
                : renderer.transform.name;

            if (!objectName.StartsWith("Deco_"))
                continue;

            if (objectName.Contains("Sky") || objectName.Contains("planet") || objectName.Contains("Mounts"))
            {
                // 새 타일형 원경이 전체 80칸을 이음새 없이 덮는다. 구 버전의 단일 배경은
                // 카메라 시작점에서 텍스처 경계가 드러나므로 런타임에만 숨긴다.
                renderer.enabled = false;
                continue;
            }

            renderer.sortingLayerName = "Default";
        }
    }

    private static void BuildHubArchitecture(ConstantAssetLibrary.PlanetAssets assets, Transform root)
    {
        Sprite[] pillars = assets.pillarSprites;
        Sprite[] sets = assets.setPieceSprites;
        Sprite[] hanging = assets.hangingPropSprites;
        Sprite[] floor = assets.floorPropSprites;

        float[] bayEdges = { 0.5f, 17f, 31f, 49f, 63f, 79.5f };
        for (int i = 0; i < bayEdges.Length; i++)
        {
            if (pillars != null && pillars.Length > 0)
                Fit(pillars[i % pillars.Length], root, new Vector3(bayEdges[i], 6.0f, 0f), -5, 7.8f,
                    new Color(0.60f, 0.66f, 0.69f, 1f), $"Frame_{i}");
        }

        // 각 카메라 구간마다 하나의 큰 초점과 작은 군집을 둔다.
        float[] zoneCenters = { 7f, 24f, 40f, 57f, 72f };
        for (int i = 0; i < zoneCenters.Length; i++)
        {
            // 첫 구간에는 기존 Core.prefab이 이미 강한 초점이므로 중복 랜드마크를 두지 않는다.
            if (i > 0 && sets != null && sets.Length > 0)
                Fit(sets[(i * 2) % sets.Length], root, new Vector3(zoneCenters[i], 5.3f, 0f), -12,
                    4.3f, new Color(0.56f, 0.64f, 0.67f, 0.92f), $"Landmark_{i}");

            if (floor != null && floor.Length > 0)
            {
                Fit(floor[(i * 3 + 1) % floor.Length], root,
                    new Vector3(zoneCenters[i] - 3.2f, 2.9f, 0f), 4, 1.75f,
                    new Color(0.76f, 0.78f, 0.75f, 1f), $"FloorCluster_{i}");
            }

            if (hanging != null && hanging.Length > 0)
            {
                Fit(hanging[(i * 2 + 1) % hanging.Length], root,
                    new Vector3(zoneCenters[i] + 1.4f, 13.5f, 0f), 3, 2.1f,
                    new Color(0.72f, 0.76f, 0.74f, 1f), $"CeilingCluster_{i}");
            }
        }
    }

    private static GameObject Fit(Sprite sprite, Transform parent, Vector3 position, int order,
        float targetHeight, Color tint, string name)
    {
        if (sprite == null || sprite.bounds.size.y < 0.001f)
            return null;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * (targetHeight / sprite.bounds.size.y);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = order;
        return go;
    }
}
