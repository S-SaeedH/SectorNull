using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SanityUI : MonoBehaviour
{
    [Header("Sanity Bar")]
    public Slider sanitySlider;
    public Image sanityFill;           // The fill image of the slider
    public Gradient sanityGradient;    // Green → Yellow → Red
    public float barSmoothSpeed = 3f;

    [Header("Warning Icon")]
    public CanvasGroup warningIcon;
    public float pulseSpeed = 2f;

    [Header("Critical Overlay")]
    public CanvasGroup criticalOverlay; // Full screen dark/red vignette image
    public float maxOverlayAlpha = 0.6f;

    [Header("Label (Optional)")]
    public TextMeshProUGUI sanityLabel;

    private float _targetSanity = 1f;
    private bool _isPulsing;

    void Start()
    {
        SanityManager.Instance.OnSanityChanged.AddListener(OnSanityChanged);
        SanityManager.Instance.OnLowSanity.AddListener(StartPulse);

        if (warningIcon != null) warningIcon.alpha = 0f;
        if (criticalOverlay != null) criticalOverlay.alpha = 0f;
    }

    void OnSanityChanged(float normalized)
    {
        _targetSanity = normalized;

        if (sanityLabel != null)
            sanityLabel.text = Mathf.RoundToInt(normalized * 100f) + "%";

        if (sanityFill != null)
            sanityFill.color = sanityGradient.Evaluate(normalized);

        // Stop pulsing when sanity recovers above low threshold
        float lowNorm = SanityManager.Instance.lowSanityThreshold / SanityManager.Instance.maxSanity;
        if (normalized > lowNorm)
            StopPulse();

        if (criticalOverlay != null)
        {
            float critNorm = SanityManager.Instance.criticalSanityThreshold / SanityManager.Instance.maxSanity;
            float overlayAlpha = normalized < critNorm
                ? Mathf.InverseLerp(critNorm, 0f, normalized) * maxOverlayAlpha
                : 0f;
            criticalOverlay.alpha = overlayAlpha;
        }
    }

    void Update()
    {
        // Smooth bar movement
        if (sanitySlider != null)
            sanitySlider.value = Mathf.Lerp(sanitySlider.value, _targetSanity, Time.deltaTime * barSmoothSpeed);

        // Pulse the warning icon
        if (_isPulsing && warningIcon != null)
            warningIcon.alpha = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
    }

    void StartPulse()
    {
        _isPulsing = true;
    }

    public void StopPulse()
    {
        _isPulsing = false;
        if (warningIcon != null) warningIcon.alpha = 0f;
    }
}