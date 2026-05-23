using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class HalfScreenChoice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("This Side")]
    [SerializeField] private RectTransform thisSide;
    [SerializeField] private Image overlay;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color hoverColor = new Color(0f, 1f, 0.5f, 0.35f);

    [Header("Other Side")]
    [SerializeField] private RectTransform otherSide;

    [Header("Choice Screen Fade Out")]
    [SerializeField] private CanvasGroup choiceScreenCanvasGroup;

    [Header("Click Event")]
    [SerializeField] private UnityEvent onChoiceClicked;

    [Header("Hover Settings")]
    [SerializeField] private float hoverAlpha = 0.35f;
    [SerializeField] private float hoverTextScale = 1.12f;
    [SerializeField] private float hoverSpeed = 8f;

    [Header("Click Animation Settings")]
    [SerializeField] private float expandDuration = 0.45f;
    [SerializeField] private float fadeOutDuration = 0.45f;
    [SerializeField] private bool disableChoiceScreenAfterFade = true;

    [Header("Scene Loading")]
    [SerializeField] private bool loadSceneAfterClick = false;
    [SerializeField] private string sceneToLoad;

    private bool isHovered;
    private bool isClicked;

    private void Reset()
    {
        thisSide = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (thisSide == null)
            thisSide = GetComponent<RectTransform>();

        if (overlay != null)
        {
            Color c = hoverColor;
            c.a = 0f;
            overlay.color = c;
        }

        if (label != null)
        {
            label.color = Color.white;
        }

        if (choiceScreenCanvasGroup != null)
        {
            choiceScreenCanvasGroup.alpha = 1f;
            choiceScreenCanvasGroup.interactable = true;
            choiceScreenCanvasGroup.blocksRaycasts = true;
        }
    }

    private void Update()
    {
        if (isClicked)
            return;

        AnimateHover();
    }

    private void AnimateHover()
    {
        float targetAlpha = isHovered ? hoverAlpha : 0f;
        float targetScale = isHovered ? hoverTextScale : 1f;

        if (overlay != null)
        {
            Color targetColor = hoverColor;
            targetColor.a = targetAlpha;

            overlay.color = Color.Lerp(
                overlay.color,
                targetColor,
                Time.unscaledDeltaTime * hoverSpeed
            );
        }

        if (label != null)
        {
            label.color = Color.white;

            label.transform.localScale = Vector3.Lerp(
                label.transform.localScale,
                Vector3.one * targetScale,
                Time.unscaledDeltaTime * hoverSpeed
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isClicked)
            return;

        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isClicked)
            return;

        isHovered = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isClicked)
            return;

        StartCoroutine(ClickAnimation());
    }

    private IEnumerator ClickAnimation()
    {
        isClicked = true;

        onChoiceClicked?.Invoke();

        DisableBothRaycasts();

        if (choiceScreenCanvasGroup != null)
        {
            choiceScreenCanvasGroup.interactable = false;
            choiceScreenCanvasGroup.blocksRaycasts = false;
        }

        Vector2 thisStartMin = thisSide.anchorMin;
        Vector2 thisStartMax = thisSide.anchorMax;

        Vector2 otherStartMin = otherSide.anchorMin;
        Vector2 otherStartMax = otherSide.anchorMax;

        bool isLeftSide = thisStartMin.x < 0.5f;

        Vector2 thisTargetMin = new Vector2(0f, 0f);
        Vector2 thisTargetMax = new Vector2(1f, 1f);

        Vector2 otherTargetMin;
        Vector2 otherTargetMax;

        if (isLeftSide)
        {
            otherTargetMin = new Vector2(1f, 0f);
            otherTargetMax = new Vector2(1.5f, 1f);
        }
        else
        {
            otherTargetMin = new Vector2(-0.5f, 0f);
            otherTargetMax = new Vector2(0f, 1f);
        }

        float timer = 0f;

        while (timer < expandDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / expandDuration;
            t = SmoothStep(t);

            thisSide.anchorMin = Vector2.Lerp(thisStartMin, thisTargetMin, t);
            thisSide.anchorMax = Vector2.Lerp(thisStartMax, thisTargetMax, t);

            otherSide.anchorMin = Vector2.Lerp(otherStartMin, otherTargetMin, t);
            otherSide.anchorMax = Vector2.Lerp(otherStartMax, otherTargetMax, t);

            thisSide.offsetMin = Vector2.zero;
            thisSide.offsetMax = Vector2.zero;
            otherSide.offsetMin = Vector2.zero;
            otherSide.offsetMax = Vector2.zero;

            if (overlay != null)
            {
                Color c = hoverColor;
                c.a = Mathf.Lerp(hoverAlpha, 0.6f, t);
                overlay.color = c;
            }

            if (label != null)
            {
                label.color = Color.white;

                label.transform.localScale = Vector3.Lerp(
                    label.transform.localScale,
                    Vector3.one * 1.2f,
                    Time.unscaledDeltaTime * 10f
                );
            }

            yield return null;
        }

        yield return FadeChoiceScreenOut();

        if (loadSceneAfterClick && !string.IsNullOrWhiteSpace(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private IEnumerator FadeChoiceScreenOut()
    {
        if (choiceScreenCanvasGroup == null)
            yield break;

        float timer = 0f;

        float startAlpha = choiceScreenCanvasGroup.alpha;
        float endAlpha = 0f;

        while (timer < fadeOutDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / fadeOutDuration;
            t = SmoothStep(t);

            choiceScreenCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            yield return null;
        }

        choiceScreenCanvasGroup.alpha = 0f;

        if (disableChoiceScreenAfterFade)
        {
            choiceScreenCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void DisableBothRaycasts()
    {
        if (thisSide != null)
        {
            Image thisImage = thisSide.GetComponent<Image>();
            if (thisImage != null)
                thisImage.raycastTarget = false;
        }

        if (otherSide != null)
        {
            Image otherImage = otherSide.GetComponent<Image>();
            if (otherImage != null)
                otherImage.raycastTarget = false;
        }
    }

    private float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}