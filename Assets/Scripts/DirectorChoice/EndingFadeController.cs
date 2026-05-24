using System.Collections;
using UnityEngine;

public class EndingFadeController : MonoBehaviour
{
    [Header("Fade")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 4f;

    private Coroutine fadeRoutine;

    public void FadeToBlack()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(1f));
    }

    public void ResetFade()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0f;
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
            yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}