using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SanityUI : MonoBehaviour
{
    [Header("Sanity Bar")]
    public Slider sanitySlider;
    public Image sanityFill;
    public Gradient sanityGradient;
    public float barSmoothSpeed = 3f;

    [Header("Warning Icon — Fill Shader")]
    public Image warningImage;              // The main warning sprite (always visible, low alpha when healthy)
    public Material sanityFillMaterial;     // SanityFillMat using UI/SanityFill shader
    [Range(0f, 1f)]
    public float baseAlpha = 0.35f;         // Resting alpha of main icon when not critical
    [Range(0f, 1f)]
    public float activeAlpha = 1f;          // Alpha when low/critical

    [Header("Pulse Echo")]
    public Image pulseEchoImage;            // Duplicate of warning sprite, child of same parent
    public float pulseNormalDuration = 2f;  // How long pulse plays after a sanity drop
    public float pulseScaleMax = 1.8f;      // How large the echo expands to
    [Range(0f, 1f)]
    public float pulseEchoStartAlpha = 0.7f;

    [Header("Pulse Speed (beats per second)")]
    public float pulseSpeedNormal = 1.2f;   // Low sanity
    public float pulseSpeedCritical = 2.5f; // Critical sanity

    [Header("Critical Overlay")]
    public CanvasGroup criticalOverlay;
    public float maxOverlayAlpha = 0.6f;

    [Header("Label (Optional)")]
    public TextMeshProUGUI sanityLabel;

    // ── Private ─────────────────────────────────────────────────────────────

    private Material _instanceMat;
    private float _targetSanity = 1f;

    // Pulse state
    private enum PulseMode { Off, Temporary, LowSanity, Critical }
    private PulseMode _pulseMode = PulseMode.Off;
    private Coroutine _tempPulseCoroutine;
    private Coroutine _echoCoroutine;
    private float _lastSanity = 1f;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    void Start()
    {
        // Material instance
        if (sanityFillMaterial != null)
        {
            _instanceMat = Instantiate(sanityFillMaterial);
            if (warningImage != null)
                warningImage.material = _instanceMat;
        }

        // Initial state
        if (warningImage != null)
        {
            Color c = warningImage.color;
            c.a = baseAlpha;
            warningImage.color = c;
        }

        if (pulseEchoImage != null)
        {
            pulseEchoImage.gameObject.SetActive(false);
        }

        if (criticalOverlay != null) criticalOverlay.alpha = 0f;

        SanityManager.Instance.OnSanityChanged.AddListener(OnSanityChanged);
        SanityManager.Instance.OnLowSanity.AddListener(OnLowSanityTriggered);
    }

    void OnDestroy()
    {
        if (_instanceMat != null)
            Destroy(_instanceMat);
    }

    // ── Sanity Events ────────────────────────────────────────────────────────

    void OnSanityChanged(float normalized)
    {
        _targetSanity = normalized;

        // Update fill shader
        if (_instanceMat != null) { }
            _instanceMat.SetFloat("_FillAmount", Mathf.Clamp01(normalized));

        // Label
        if (sanityLabel != null)
            sanityLabel.text = Mathf.RoundToInt(normalized * 100f) + "%";

        // Slider fill color
        if (sanityFill != null)
            sanityFill.color = sanityGradient.Evaluate(normalized);

        float lowNorm = SanityManager.Instance.lowSanityThreshold / SanityManager.Instance.maxSanity;
        float critNorm = SanityManager.Instance.criticalSanityThreshold / SanityManager.Instance.maxSanity;

        // ── Determine pulse mode ──────────────────────────────────────────
        bool sanityDropped = normalized < _lastSanity;

        if (normalized <= critNorm)
        {
            SetPulseMode(PulseMode.Critical);
        }
        else if (normalized <= lowNorm)
        {
            SetPulseMode(PulseMode.LowSanity);
        }
        else
        {
            // Above low threshold — if sanity dropped, do a short temporary pulse
            if (sanityDropped)
                TriggerTemporaryPulse();
            else if (_pulseMode != PulseMode.Temporary)
                SetPulseMode(PulseMode.Off);
        }

        // ── Warning icon alpha ────────────────────────────────────────────
        if (warningImage != null)
        {
            float targetAlpha = (normalized <= lowNorm) ? activeAlpha : baseAlpha;
            Color c = warningImage.color;
            c.a = targetAlpha;
            warningImage.color = c;
        }

        // ── Critical overlay ──────────────────────────────────────────────
        if (criticalOverlay != null)
        {
            float overlayAlpha = normalized < critNorm
                ? Mathf.InverseLerp(critNorm, 0f, normalized) * maxOverlayAlpha
                : 0f;
            criticalOverlay.alpha = overlayAlpha;
        }

        _lastSanity = normalized;
    }

    void OnLowSanityTriggered()
    {
        // Fired by SanityManager when crossing low threshold
        // SetPulseMode handled in OnSanityChanged already
    }

    // ── Pulse Mode Control ───────────────────────────────────────────────────

    void SetPulseMode(PulseMode mode)
    {
        if (_pulseMode == mode) return;
        _pulseMode = mode;

        if (_echoCoroutine != null)
            StopCoroutine(_echoCoroutine);

        if (mode == PulseMode.Off)
        {
            if (pulseEchoImage != null)
                pulseEchoImage.gameObject.SetActive(false);
        }
        else
        {
            float speed = (mode == PulseMode.Critical) ? pulseSpeedCritical : pulseSpeedNormal;
            _echoCoroutine = StartCoroutine(PulseLoop(speed));
        }
    }

    void TriggerTemporaryPulse()
    {
        if (_pulseMode == PulseMode.LowSanity || _pulseMode == PulseMode.Critical) return;

        if (_tempPulseCoroutine != null)
            StopCoroutine(_tempPulseCoroutine);

        _tempPulseCoroutine = StartCoroutine(TemporaryPulseRoutine());
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    IEnumerator TemporaryPulseRoutine()
    {
        SetPulseMode(PulseMode.Temporary);
        yield return new WaitForSeconds(pulseNormalDuration);

        // Only stop if still in temporary mode (not escalated)
        if (_pulseMode == PulseMode.Temporary)
            SetPulseMode(PulseMode.Off);
    }

    /// <summary>
    /// Continuously fires one echo pulse at the given beats-per-second speed.
    /// </summary>
    IEnumerator PulseLoop(float beatsPerSecond)
    {
        if (pulseEchoImage == null) yield break;

        pulseEchoImage.gameObject.SetActive(true);
        RectTransform echoRect = pulseEchoImage.rectTransform;
        RectTransform baseRect = warningImage != null ? warningImage.rectTransform : null;

        while (true)
        {
            float interval = 1f / beatsPerSecond;
            yield return StartCoroutine(SingleEcho(echoRect, baseRect, interval * 0.6f));
            yield return new WaitForSeconds(interval * 0.4f);
        }
    }

    /// <summary>
    /// One echo expansion: scale 1→pulseScaleMax, alpha pulseEchoStartAlpha→0
    /// </summary>
    IEnumerator SingleEcho(RectTransform echoRect, RectTransform baseRect, float duration)
    {
        float elapsed = 0f;
        Vector2 baseSize = baseRect != null ? baseRect.sizeDelta : echoRect.sizeDelta;

        // Make sure echo is centered on the same pivot as the base image
        echoRect.anchorMin = baseRect.anchorMin;
        echoRect.anchorMax = baseRect.anchorMax;
        echoRect.anchoredPosition = baseRect.anchoredPosition;
        echoRect.pivot = baseRect.pivot;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            // Keep scale subtle — only expand 30% beyond original
            float scale = Mathf.Lerp(1f, pulseScaleMax, eased);
            echoRect.sizeDelta = baseSize * scale;

            Color c = pulseEchoImage.color;
            c.a = Mathf.Lerp(pulseEchoStartAlpha, 0f, eased);
            pulseEchoImage.color = c;

            yield return null;
        }

        // Reset
        echoRect.sizeDelta = baseSize;
        Color reset = pulseEchoImage.color;
        reset.a = 0f;
        pulseEchoImage.color = reset;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (sanitySlider != null)
            sanitySlider.value = Mathf.Lerp(sanitySlider.value, _targetSanity, Time.deltaTime * barSmoothSpeed);
    }

    public void StopPulse()
    {
        SetPulseMode(PulseMode.Off);
    }
}