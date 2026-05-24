using UnityEngine;
using UnityEngine.Events;
using UHFPS.Runtime;
using Newtonsoft.Json.Linq;

public class SanityManager : MonoBehaviour, ISaveable
{
    public static SanityManager Instance { get; private set; }

    [Header("Sanity Settings")]
    [Range(0f, 100f)] public float currentSanity = 100f;
    [Range(0f, 100f)] public float maxSanity = 100f;

    [Header("Passive Decay")]
    [Tooltip("Current sanity decay rate per second.")]
    public float decayRate = 0f;

    [Tooltip("If true, sanity will decay every frame.")]
    public bool decayEnabled = false;

    [Header("Auto Recovery")]
    public bool recoveryEnabled = true;

    [Tooltip("Seconds after last sanity drop before recovery begins.")]
    public float recoveryDelay = 10f;

    [Tooltip("Sanity units restored per second during recovery.")]
    public float recoveryRate = 0.6f;

    [Tooltip("Recovery stops at this sanity value. 0 = recover to full.")]
    public float recoveryCapSanity = 0f;

    [Header("Thresholds")]
    public float lowSanityThreshold = 40f;
    public float criticalSanityThreshold = 20f;

    [Header("Events")]
    public UnityEvent<float> OnSanityChanged;
    public UnityEvent OnLowSanity;
    public UnityEvent OnCriticalSanity;
    public UnityEvent OnSanityDepleted;

    private bool _lowTriggered;
    private bool _criticalTriggered;
    private bool _depletedTriggered;

    private float _timeSinceLastDrop;
    private bool _isRecovering;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);
    }

    private void Update()
    {
        if (decayEnabled && decayRate > 0f && currentSanity > 0f)
        {
            ModifySanity(-decayRate * Time.deltaTime);
        }

        HandleRecovery();
    }

    private void HandleRecovery()
    {
        if (!recoveryEnabled)
            return;

        if (decayEnabled)
            return;

        if (currentSanity >= maxSanity)
            return;

        float cap = recoveryCapSanity > 0f ? recoveryCapSanity : maxSanity;

        if (currentSanity >= cap)
            return;

        _timeSinceLastDrop += Time.deltaTime;

        if (_timeSinceLastDrop < recoveryDelay)
        {
            _isRecovering = false;
            return;
        }

        _isRecovering = true;

        currentSanity = Mathf.Clamp(
            currentSanity + recoveryRate * Time.deltaTime,
            0f,
            cap
        );

        InvokeSanityChanged();

        ResetThresholdsIfRecovered();
    }

    public void ModifySanity(float amount)
    {
        if (Mathf.Approximately(amount, 0f))
            return;

        bool wasDropping = amount < 0f;

        if (wasDropping)
        {
            _timeSinceLastDrop = 0f;
            _isRecovering = false;
        }

        currentSanity = Mathf.Clamp(currentSanity + amount, 0f, maxSanity);

        InvokeSanityChanged();
        CheckThresholds();
        ResetThresholdsIfRecovered();
    }

    public void SetSanity(float value)
    {
        currentSanity = Mathf.Clamp(value, 0f, maxSanity);

        InvokeSanityChanged();
        CheckThresholds();
        ResetThresholdsIfRecovered();
    }

    public void RestoreSanity(float amount)
    {
        if (amount <= 0f)
            return;

        ModifySanity(amount);
    }

    public void DamageSanity(float amount)
    {
        if (amount <= 0f)
            return;

        ModifySanity(-amount);
    }

    public void StartDecay(float rate)
    {
        decayRate = Mathf.Max(0f, rate);
        decayEnabled = decayRate > 0f;
    }

    public void StopDecay()
    {
        decayEnabled = false;
        decayRate = 0f;
    }

    public void EnterDarkArea(float rate)
    {
        StartDecay(rate);
    }

    public void ExitDarkArea()
    {
        StopDecay();
    }

    public float GetNormalized()
    {
        if (maxSanity <= 0f)
            return 0f;

        return currentSanity / maxSanity;
    }

    public bool IsRecovering()
    {
        return _isRecovering;
    }

    public bool IsDecaying()
    {
        return decayEnabled && decayRate > 0f;
    }

    private void InvokeSanityChanged()
    {
        OnSanityChanged?.Invoke(GetNormalized());
    }

    private void CheckThresholds()
    {
        if (!_lowTriggered && currentSanity <= lowSanityThreshold)
        {
            _lowTriggered = true;
            OnLowSanity?.Invoke();
        }

        if (!_criticalTriggered && currentSanity <= criticalSanityThreshold)
        {
            _criticalTriggered = true;
            OnCriticalSanity?.Invoke();
        }

        if (!_depletedTriggered && currentSanity <= 0f)
        {
            _depletedTriggered = true;
            OnSanityDepleted?.Invoke();
        }
    }

    private void ResetThresholdsIfRecovered()
    {
        if (currentSanity > lowSanityThreshold)
            _lowTriggered = false;

        if (currentSanity > criticalSanityThreshold)
            _criticalTriggered = false;

        if (currentSanity > 0f)
            _depletedTriggered = false;
    }

    // ─── UHFPS ISaveable ───────────────────────────────────────────

    public StorableCollection OnSave()
    {
        return new StorableCollection()
        {
            { "currentSanity", currentSanity },
            { "decayEnabled", decayEnabled },
            { "decayRate", decayRate },
            { "lowTriggered", _lowTriggered },
            { "criticalTriggered", _criticalTriggered },
            { "depletedTriggered", _depletedTriggered },
            { "timeSinceLastDrop", _timeSinceLastDrop }
        };
    }

    public void OnLoad(JToken data)
    {
        currentSanity = data["currentSanity"] != null
            ? (float)data["currentSanity"]
            : maxSanity;

        decayEnabled = data["decayEnabled"] != null && (bool)data["decayEnabled"];

        decayRate = data["decayRate"] != null
            ? (float)data["decayRate"]
            : 0f;

        _lowTriggered = data["lowTriggered"] != null && (bool)data["lowTriggered"];
        _criticalTriggered = data["criticalTriggered"] != null && (bool)data["criticalTriggered"];
        _depletedTriggered = data["depletedTriggered"] != null && (bool)data["depletedTriggered"];

        _timeSinceLastDrop = data["timeSinceLastDrop"] != null
            ? (float)data["timeSinceLastDrop"]
            : 0f;

        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);

        InvokeSanityChanged();
    }
}