using TMPro;
using UnityEngine;

/// <summary>
/// 런 결과 팝업 — 사망(런 종료) 또는 엔딩(관광객 엔딩)을 표시한다.
/// Enter 를 누르면 새 여행이 시작된다. (단판 로그라이크: 남는 것은 지식뿐)
/// </summary>
public class UI_Popup_RunResult : UI_Popup
{
    enum Texts
    {
        TitleText,
        DescText,
        PromptText,
    }

    private string _title;
    private string _desc;
    private bool _bound;

    /// <summary>세터는 저장만 — 실제 반영은 Init 과 양쪽에서 (Init 1프레임 지연 규약).</summary>
    public void SetResult(string title, string desc)
    {
        _title = title;
        _desc = desc;
        Apply();
    }

    public override void Init()
    {
        base.Init();
        Bind<TextMeshProUGUI>(typeof(Texts));
        _bound = true;

        GetText((int)Texts.PromptText).text = "Enter — 새 여행 시작";
        Apply();
    }

    private void Apply()
    {
        if (!_bound) return;
        GetText((int)Texts.TitleText).text = string.IsNullOrEmpty(_title) ? "여정의 기록" : _title;
        GetText((int)Texts.DescText).text = _desc ?? "";
    }

    public override void OnSubmit()
    {
        SingletonManagers.Run?.RestartJourney();
    }

    public override void OnCancel()
    {
        // 결과 확인은 필수 — 닫기 대신 새 여행으로만 진행
        OnSubmit();
    }
}
