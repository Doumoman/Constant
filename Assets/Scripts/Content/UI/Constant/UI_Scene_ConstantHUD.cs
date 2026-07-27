using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Constant 인게임 HUD.
/// 행성 이름 / 출항 게이지 / 시너지 요약 / 토스트 메시지를 표시한다.
/// RunManager 이벤트를 구독해 자동 갱신된다.
/// </summary>
public class UI_Scene_ConstantHUD : UI_Scene
{
    enum GameObjects
    {
        GaugeFill,
    }

    enum Texts
    {
        PlanetText,
        GaugeText,
        BuffText,
        ToastText,
        HelpText,
        ConsumText,
    }

    private Image _gaugeFill;
    private TextMeshProUGUI _toastText;
    private Coroutine _toastRoutine;

    public override void Init()
    {
        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        Bind<TextMeshProUGUI>(typeof(Texts));

        _gaugeFill = Get<GameObject>((int)GameObjects.GaugeFill).GetComponent<Image>();
        _toastText = GetText((int)Texts.ToastText);

        var run = SingletonManagers.Run;
        if (run != null)
        {
            run.OnGaugeChanged -= HandleGauge;
            run.OnGaugeChanged += HandleGauge;
            run.OnInventoryChanged -= HandleInventory;
            run.OnInventoryChanged += HandleInventory;
            run.OnToast -= ShowToast;
            run.OnToast += ShowToast;
            run.OnConsumablesChanged -= HandleConsumables;
            run.OnConsumablesChanged += HandleConsumables;

            string stageSuffix = run.CurrentPlanet != ConstantPlanet.Hub ? $"  ({StageLabel(run)})" : "";
            GetText((int)Texts.PlanetText).text = ConstantDefine.DisplayNameOf(run.CurrentPlanet) + stageSuffix;
            HandleGauge(run.Gauge, run.GaugeRequired);
            HandleInventory();
            HandleConsumables(run.Ropes, run.Bombs);
        }

        GetText((int)Texts.HelpText).text = "좌우 이동 · C 점프 · X 상호작용 · I 가방 · V 도감 · R 로프 · B 폭탄 · Space 대화";
        _toastText.text = "";
    }

    private void OnDestroy()
    {
        var run = SingletonManagers.Run;
        if (run != null)
        {
            run.OnGaugeChanged -= HandleGauge;
            run.OnInventoryChanged -= HandleInventory;
            run.OnToast -= ShowToast;
            run.OnConsumablesChanged -= HandleConsumables;
        }
    }

    private void HandleConsumables(int ropes, int bombs)
    {
        GetText((int)Texts.ConsumText).text = $"로프 {ropes} · 폭탄 {bombs}";
    }

    private static string StageLabel(RunManager run)
    {
        string baseName = run.CurrentPlanet switch
        {
            ConstantPlanet.Lavernis => "라베르니스",
            ConstantPlanet.Sylmare => "실마레",
            ConstantPlanet.Eidron => "에이드론",
            _ => "",
        };
        return $"{baseName}-{run.StageIndex}";
    }

    private void HandleGauge(float current, float required)
    {
        if (_gaugeFill != null)
            _gaugeFill.fillAmount = required > 0f ? current / required : 0f;

        var run = SingletonManagers.Run;
        bool isHub = run != null && run.CurrentPlanet == ConstantPlanet.Hub;
        GetText((int)Texts.GaugeText).text = isHub
            ? "정박 중"
            : $"출항 게이지 {current:0} / {required:0}%";
    }

    private void HandleInventory()
    {
        var run = SingletonManagers.Run;
        if (run == null) return;

        string summary = run.Synergy.Summary();
        GetText((int)Texts.BuffText).text = string.IsNullOrEmpty(summary)
            ? ""
            : $"조합 효과: {summary}";
    }

    /// <summary>화면 하단 토스트. 잠시 표시 후 사라진다.</summary>
    public void ShowToast(string message)
    {
        if (_toastText == null) return;

        if (_toastRoutine != null)
            StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(CoToast(message));
    }

    private IEnumerator CoToast(string message)
    {
        _toastText.text = message;
        _toastText.alpha = 1f;

        float hold = 2.2f;
        float fade = 0.6f;

        float t = 0f;
        while (t < hold)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            _toastText.alpha = 1f - (t / fade);
            yield return null;
        }

        _toastText.text = "";
        _toastRoutine = null;
    }
}
