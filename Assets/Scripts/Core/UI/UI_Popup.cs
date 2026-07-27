using UnityEngine;

public class UI_Popup : UI_Base
{
    public override void Init()
    {
        SingletonManagers.UI.SetCanvas(gameObject, true);
    }

    public virtual void ClosePopupUI()
    {
        SingletonManagers.UI.ClosePopupUI(this);
    }

    public virtual void OnInput(Vector2 direction) { } // 팝업 내 방향키 입력 처리
    public virtual void OnSubmit() { } // 팝업 내에서 결정/확인  
    public virtual void OnCancel() { }
}
