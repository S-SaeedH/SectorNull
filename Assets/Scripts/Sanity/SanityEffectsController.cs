using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class SanityEffectsController : MonoBehaviour
{
    [Header("Post Processing Volume")]
    public Volume globalVolume;

    [Header("Camera Shake")]
    public Transform cameraRoot; // Assign the camera's parent transform
    public float maxShakeIntensity = 0.05f;
    public float shakeSpeed = 10f;

    [Header("Effect Curves")]
    [Tooltip("How effects scale from full sanity (1) to zero sanity (0)")]
    public AnimationCurve effectCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Post-processing components
    private Vignette _vignette;
    private LensDistortion _lensDistortion;
    private DepthOfField _dof;
    private ChromaticAberration _chromaticAberration;
    private ColorAdjustments _colorAdjustments;

    private float _currentSanityNorm = 1f;
    private Vector3 _originalCameraPos;
    private Coroutine _pulseCoroutine;

    void Start()
    {
        if (globalVolume == null) return;

        globalVolume.profile.TryGet(out _vignette);
        globalVolume.profile.TryGet(out _lensDistortion);
        globalVolume.profile.TryGet(out _dof);
        globalVolume.profile.TryGet(out _chromaticAberration);
        globalVolume.profile.TryGet(out _colorAdjustments);

        StartCoroutine(InitializeCameraRoot());
        SanityManager.Instance.OnSanityChanged.AddListener(OnSanityChanged);
        UpdateEffects(1f);
    }

    void OnSanityChanged(float normalized)
    {
        _currentSanityNorm = normalized;
        UpdateEffects(normalized);
    }

    void Update()
    {
        float insanity = 1f - _currentSanityNorm;

        // Only shake below 60% sanity (insanity > 0.4)
        if (cameraRoot != null && insanity > 0.4f)
        {
            float intensity = (insanity - 0.4f) * maxShakeIntensity;
            float noise = Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(0f, Time.time * shakeSpeed) * 2f - 1f;
            cameraRoot.localPosition = _originalCameraPos + new Vector3(
                noise * intensity,
                noiseY * intensity,
                0f
            );
        }
        else if (cameraRoot != null)
        {
            cameraRoot.localPosition = Vector3.Lerp(
                cameraRoot.localPosition, _originalCameraPos, Time.deltaTime * 5f);
        }
    }

    void UpdateEffects(float normalized)
    {
        // Insanity is the inverse of sanity (0 = full sanity, 1 = no sanity)
        float insanity = effectCurve.Evaluate(1f - normalized);

        // --- Vignette: grows darker at edges as sanity drops ---
        if (_vignette != null)
        {
            _vignette.intensity.value = Mathf.Lerp(0.2f, 0.75f, insanity);
            _vignette.smoothness.value = Mathf.Lerp(0.3f, 1f, insanity);
        }

        // --- Lens Distortion: wobble/warp effect ---
        if (_lensDistortion != null)
        {
            _lensDistortion.intensity.value = Mathf.Lerp(0f, -0.5f, insanity);
            _lensDistortion.xMultiplier.value = Mathf.Lerp(1f, 0.6f, insanity);
            _lensDistortion.yMultiplier.value = Mathf.Lerp(1f, 0.6f, insanity);
        }

        // --- Depth of Field: vision becomes unfocused ---
if (_dof != null)
{
    // At full sanity: high focus distance = everything sharp
    // At zero sanity: very low = everything blurry
    _dof.focusDistance.value = Mathf.Lerp(0.3f, 20f, normalized);
    _dof.aperture.value = Mathf.Lerp(1f, 32f, normalized);
}

        // --- Chromatic Aberration: color fringing on edges ---
        if (_chromaticAberration != null)
        {
            _chromaticAberration.intensity.value = Mathf.Lerp(0f, 1f, insanity);
        }

        // --- Color Adjustments: desaturate & dim at low sanity ---
        if (_colorAdjustments != null)
        {
            _colorAdjustments.saturation.value = Mathf.Lerp(0f, -80f, insanity);
            _colorAdjustments.postExposure.value = Mathf.Lerp(0f, -1.5f, insanity);
        }
    }

    // Call this for a sudden "pulse" jolt (e.g. when a scary event occurs)
    public void TriggerSanityPulse(float duration = 0.5f)
    {
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(SanityPulse(duration));
    }

    IEnumerator SanityPulse(float duration)
    {
        float elapsed = 0f;
        float baseVignette = _vignette != null ? _vignette.intensity.value : 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin((elapsed / duration) * Mathf.PI);
            if (_vignette != null) _vignette.intensity.value = Mathf.Lerp(baseVignette, 0.95f, t);
            if (_chromaticAberration != null) _chromaticAberration.intensity.value = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        // Restore to current insanity level
        UpdateEffects(_currentSanityNorm);
    }

    IEnumerator InitializeCameraRoot()
    {
        yield return null;
        if (cameraRoot != null)
            _originalCameraPos = cameraRoot.localPosition;
    }
}