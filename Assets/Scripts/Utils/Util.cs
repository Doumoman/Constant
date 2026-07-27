using UnityEngine;

// 공통적으로 자주 쓰이는 함수들을 따로 모아서 관리하는 스크립트
// Static으로 선언하자
public static class Util
{
    public static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
            component = go.AddComponent<T>();

        return component;
    }

    /// <summary>
    ///  root 게임오브젝트 산하에 있는 <T> 컴포넌트를 가지고, name이 일치하는 child를 찾아주는 함수, recursive가 true면 child의 child도 찾아준다
    /// </summary>
    public static T FindChild<T>(GameObject root, string name = null, bool recursive = false) where T : UnityEngine.Object
    {
        if (root == null)
            return null;

        if (!recursive) // 오직 root의 Child만
        {
            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform transform = root.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || transform.name == name) // 이름이 null이거나, 일치한다면
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
        }
        else // 모든 Child
        {
            foreach (T component in root.GetComponentsInChildren<T>())
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }

        return null;
    }

    public static GameObject FindChild(GameObject root, string name = null, bool recursive = false)
    {
        Transform transform = FindChild<Transform>(root, name, recursive); // 모든 게임 오브젝트는 Transform 컴포넌트를 갖는다!

        if (transform != null)
            return transform.gameObject;
        else
            return null;
    }
}
