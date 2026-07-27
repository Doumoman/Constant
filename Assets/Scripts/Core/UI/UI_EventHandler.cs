using System;
using UnityEngine;
using UnityEngine.EventSystems;

// EventSystem을 활용한 UI_Event 구현
public class UI_EventHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerClickHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public Action<PointerEventData> onClickHandler = null;
    public Action<PointerEventData> onBeginDraghandler = null;
    public Action<PointerEventData> onDraghandler = null;
    public Action<PointerEventData> onEndDraghandler = null;
    public Action<PointerEventData> onPointerDownhandler = null;
    public Action<PointerEventData> onPointerUphandler = null;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (onBeginDraghandler != null)
            onBeginDraghandler.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (onDraghandler != null)
            onDraghandler.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (onEndDraghandler != null)
            onEndDraghandler.Invoke(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (onClickHandler != null)
            onClickHandler.Invoke(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (onPointerDownhandler != null)
            onPointerDownhandler.Invoke(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (onPointerUphandler != null)
            onPointerUphandler.Invoke(eventData);
    }

    // 필요한 인터페이스 받아서 구현...
}
