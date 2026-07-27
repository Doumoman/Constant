using System;
using System.Collections;
using UnityEngine;

public class UI_Popup_Fade : UI_Popup
{
    private CanvasGroup _canvasGroup;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

    }

    public void FadeOut(float duration, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(CoFade(0f, 1f, duration, onComplete));
    }

    public void FadeIn(float duration, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(CoFade(1f, 0f, duration, onComplete));
    }

    private IEnumerator CoFade(float startAlpha, float endAlpha, float duration, Action onComplete)
    {
        float time = 0f;
        _canvasGroup.alpha = startAlpha;

        while(time < duration)
        {
            time += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha,endAlpha,time / duration);
            yield return null;
        }

        _canvasGroup.alpha = endAlpha;
        onComplete?.Invoke(); // 페이드 연출이 끝나고 실행할 콜백 함수 호출
    }

}
