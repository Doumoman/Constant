using UnityEngine;

/// <summary>
/// 출항 패드 — 출항 게이지가 가득 찼을 때 상호작용(X)하면 다음 행성으로 떠난다.
/// 허브에서는 게이지 없이 바로 출항 가능(첫 출발). 단, 시작 아이템을 골라야 한다.
/// </summary>
public class DeparturePad : ConstantInteractable
{
    /// <summary>런타임 생성용 초기화.</summary>
    public void Init(float detectionRange) => _detectionRange = detectionRange;

    protected override void OnInteract()
    {
        var run = SingletonManagers.Run;
        if (run == null) return;

        // 허브: 시작 아이템만 챙기면 출발
        if (run.CurrentPlanet == ConstantPlanet.Hub)
        {
            if (!run.StarterChosen)
            {
                run.Toast("출발 전에 여행용품을 하나 골라야 한다. (진열대에서 X)");
                return;
            }
            Confirm(run);
            return;
        }

        // 행성: 출항 게이지 100% 필요
        if (!run.IsGaugeFull)
        {
            run.Toast($"출항 게이지 부족 ({run.Gauge:0}% / {run.GaugeRequired:0}%) — 자원을 더 모으자");
            return;
        }

        Confirm(run);
    }

    private void Confirm(RunManager run)
    {
        // 일시정지 + UI 모드 (확인 팝업 규약: UI_Popup_InGamePause 패턴)
        Time.timeScale = 0f;
        SingletonManagers.Input.SetInputModeUI(true);

        var confirm = SingletonManagers.UI.ShowPopupUI<UI_Popup_Confirm>();
        if (confirm == null)
        {
            Time.timeScale = 1f;
            SingletonManagers.Input.SetInputModeUI(false);
            run.DepartToNextPlanet();
            return;
        }

        confirm.SetMessage(
            "이 좌표에서의 여정을 마치고 출항할까요?",
            onConfirm: () =>
            {
                run.DepartToNextPlanet(); // 내부에서 팝업 정리 + 타임스케일 복원
            },
            onCancel: () =>
            {
                Time.timeScale = 1f;
                if (SingletonManagers.UI.PopupCount == 0)
                    SingletonManagers.Input.SetInputModeUI(false);
            });
    }
}
