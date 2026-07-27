using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Popup_Input : UI_Popup
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private TextMeshProUGUI _question;

    private Action<string> _onSubmitCallBack;
    public void Setup(Action<string> submitCallback, string question)
    {
        _onSubmitCallBack = submitCallback;

        _question.text = question;
        _inputField.text = "";

        // 커서 깜빡이는 연출
        _inputField.Select();
        _inputField.ActivateInputField();
    }

    private void Awake()
    {
        _inputField.onSubmit.AddListener(OnInputFieldSubmitted); // TMP_InputField 내장 이벤트 사용
    }

    public override void OnInput(Vector2 direction)
    {
        
    }

    private void OnInputFieldSubmitted(string finalString)
    {
        // 빈칸 방지
        if (string.IsNullOrWhiteSpace(finalString))
        {
            Debug.Log("빈칸은 입력할 수 없습니다.");

            // 엔터를 쳐서 포커스가 풀렸을 테니, 다시 입력창에 커서를 잡아줍니다.
            _inputField.Select();
            _inputField.ActivateInputField();
            return;
        }

        string finalInput = finalString.Trim();
        SingletonManagers.UI.ClosePopupUI(this);
        _onSubmitCallBack?.Invoke(finalInput);
    }
}
