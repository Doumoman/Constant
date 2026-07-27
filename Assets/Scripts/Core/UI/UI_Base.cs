using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class UI_Base : MonoBehaviour
{
    public abstract void Init();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Init();
    }

    // 유니티 최상위 클래스인 Object 배열로 저장
    // 예를 들어, typeof(Button)을 키로 해서 Button 타입의 오브젝트들을 배열로 저장할 수 있음
    Dictionary<Type, UnityEngine.Object[]> _objects = new Dictionary<Type, UnityEngine.Object[]>();

    // 유니티 하이라키에 있는 <T> 컴포넌트를 가진 type형 오브젝트를 이름으로 자동 매핑한 후, objects[] 딕셔너리에 넣어준다
    // Enum 값과 유니티 하이라키에 있는 게임 오브젝트의 이름을 같게 하자
    protected void Bind<T>(Type type) where T : UnityEngine.Object
    {
        string[] names = Enum.GetNames(type);
        UnityEngine.Object[] objects = new UnityEngine.Object[names.Length];
        _objects.Add(typeof(T), objects);

        for (int i = 0; i < names.Length; i++)
        {
            if (typeof(T) == typeof(GameObject))
                objects[i] = Util.FindChild(gameObject, names[i], true); // <T>가 게임오브젝트
            else
                objects[i] = Util.FindChild<T>(gameObject, names[i], true); // <T>가 컴포넌트

            if (objects[i] == null)
                Debug.Log($"Failed to Bind {names[i]}");
        }
    }

    // objects[] 딕셔너리에 있는 오브젝트를 반환하는 함수
    protected T Get<T>(int idx) where T : UnityEngine.Object
    {
        UnityEngine.Object[] objects = null;
        if (_objects.TryGetValue(typeof(T), out objects) == false)
            return null;

        return objects[idx] as T;
    }

    // 자주 사용하는 UI들의 Get<T> 함수 사용하기 쉽게 다시 선언
    protected TextMeshProUGUI GetText(int idx) { return Get<TextMeshProUGUI>(idx); }
    protected Button GetButton(int idx) { return Get<Button>(idx); }
    protected Image GetImage(int idx) { return Get<Image>(idx); }


    // 자동으로 UI에 이벤트를 연결해주는 함수
    public static void AddUIEvent(GameObject go, Action<PointerEventData> action, UIEvent type = UIEvent.Click)
    {
        UI_EventHandler evt = Util.GetOrAddComponent<UI_EventHandler>(go);

        switch (type)
        {
            case UIEvent.Click:
                evt.onClickHandler -= action;
                evt.onClickHandler += action;
                break;
            case UIEvent.DragBegin:
                evt.onBeginDraghandler -= action;
                evt.onBeginDraghandler += action;
                break;
            case UIEvent.Drag:
                evt.onDraghandler -= action;
                evt.onDraghandler += action;
                break;
            case UIEvent.DragEnd:
                evt.onEndDraghandler -= action;
                evt.onEndDraghandler += action;
                break;
            case UIEvent.PointerDown:
                evt.onPointerDownhandler -= action;
                evt.onPointerDownhandler += action;
                break;
            case UIEvent.PointerUp:
                evt.onPointerUphandler -= action;
                evt.onPointerUphandler += action;
                break;
                // 필요한 UI Event 추가...
        }
    }

    public static void ClearUIEvent(GameObject go)
    {
        UI_EventHandler evt = go.GetComponent<UI_EventHandler>();
        if (evt != null)
        {
            evt.onClickHandler = null;
            evt.onBeginDraghandler = null;
            evt.onBeginDraghandler = null;
            evt.onEndDraghandler = null;
            evt.onPointerDownhandler = null;
            evt.onPointerUphandler = null;
        }
    }
}
