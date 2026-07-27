using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : IManager
{
    private bool _init = false;

    // 캐싱
    private Dictionary<string, Object> _resources = new Dictionary<string, Object>();

    public void Init()
    {
        if (_init) return;
        _init = true;

        // 초기화 로직
    }

    public T Load<T>(string path) where T : Object
    {
        // 1. 이미 로드된 적이 있는지 확인 (캐싱)
        if (_resources.TryGetValue(path, out Object resource))
        {
            return resource as T;
        }

        // 2. Resources 폴더에서 로드
        T original = Resources.Load<T>(path);

        if (original == null)
        {
            Debug.LogError($"[ResourceManager] Failed to load resource: {path}");
            return null;
        }

        // 3. 딕셔너리에 저장 (다음 요청 시 메모리에서 바로 꺼냄)
        _resources.Add(path, original);

        return original;
    }

    public GameObject Instantiate(string path, Transform parent = null)
    {
        // 1. 프리팹 원본 로드
        GameObject original = Load<GameObject>($"Prefabs/{path}");

        if (original == null)
        {
            Debug.LogError($"[ResourceManager] Failed to load prefab: {path}");
            return null;
        }

        // 2. 오브젝트 생성
        GameObject go = Object.Instantiate(original, parent);

        // 3. 이름에서 (Clone) 제거
        go.name = original.name;

        return go;
    }

    // 특정 게임오브젝트를 파괴
    public void Destroy(GameObject go)
    {
        if (go == null) return;

        Object.Destroy(go);
    }

    public void Clear()
    {
        _resources.Clear();
        // 사용하지 않는 에셋 메모리 해제
        Resources.UnloadUnusedAssets();
    }

    public void OnDestroy() => Clear();
}