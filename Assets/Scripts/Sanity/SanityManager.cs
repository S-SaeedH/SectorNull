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
    public float decayRate = 0f;
    public bool decayEnabled = false;

    [Header("Auto Recovery")]
    public bool recoveryEnabled = true;
    [Tooltip("Seconds after last sanity drop before recovery begins")]
    public float recoveryDelay = 10f;
    [Tooltip("Sanity units restored per second during recovery")]
    public float recoveryRate = 0.6f;
    [Tooltip("Recovery stops at this sanity value (0 = recover to full)")]
    public float recoveryCapSanity = 0f;

    [Header("Thresholds")]
    public float lowSanityThreshold = 40f;
    public float criticalSanityThreshold = 20f;

    // Events
    public UnityEvent<float> OnSanityChanged;
    public UnityEvent OnLowSanity;
    public UnityEvent OnCriticalSanity;
    public UnityEvent OnSanityDepleted;

    private bool _lowTriggered;
    private bool _criticalTriggered;
    private float _timeSinceLastDrop;
    private bool _isRecovering;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (decayEnabled && currentSanity > 0f)
            ModifySanity(-decayRate * Time.deltaTime);

        HandleRecovery();
    }

    void HandleRecovery()
    {
        if (!recoveryEnabled) return;
        if (currentSanity >= maxSanity) return;

        // If a recovery cap is set, stop recovering at that value
        float cap = recoveryCapSanity > 0f ? recoveryCapSanity : maxSanity;
        if (currentSanity >= cap) return;

        // Count up the delay timer
        _timeSinceLastDrop += Time.deltaTime;

        if (_timeSinceLastDrop >= recoveryDelay)
        {
            _isRecovering = true;
            // Recover silently (don't re-trigger drop timer)
            currentSanity = Mathf.Clamp(currentSanity + recoveryRate * Time.deltaTime, 0f, cap);
            float normalized = currentSanity / maxSanity;
            OnSanityChanged?.Invoke(normalized);

            // Re-evaluate threshold resets during recovery
            if (currentSanity > lowSanityThreshold) _lowTriggered = false;
            if (currentSanity > criticalSanityThreshold) _criticalTriggered = false;
        }
        else
        {
            _isRecovering = false;
        }
    }

    public void ModifySanity(float amount)
    {
        // Only reset the timer when sanity DROPS (negative amount)
        if (amount < 0f)
        {
            _timeSinceLastDrop = 0f;
            _isRecovering = false;
        }

        currentSanity = Mathf.Clamp(currentSanity + amount, 0f, maxSanity);
        float normalized = currentSanity / maxSanity;
        OnSanityChanged?.Invoke(normalized);

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

        if (currentSanity <= 0f)
            OnSanityDepleted?.Invoke();

        if (currentSanity > lowSanityThreshold) _lowTriggered = false;
        if (currentSanity > criticalSanityThreshold) _criticalTriggered = false;
    }

    public float GetNormalized() => currentSanity / maxSanity;

    // ─── UHFPS ISaveable ───────────────────────────────────────────

    public StorableCollection OnSave()
    {
        return new StorableCollection()
        {
            { "currentSanity",        currentSanity       },
            { "decayEnabled",         decayEnabled        },
            { "lowTriggered",         _lowTriggered       },
            { "criticalTriggered",    _criticalTriggered  },
            { "timeSinceLastDrop",    _timeSinceLastDrop  }
        };
    }

    public void OnLoad(JToken data)
    {
        currentSanity = (float)data["currentSanity"];
        decayEnabled = (bool)data["decayEnabled"];
        _lowTriggered = (bool)data["lowTriggered"];
        _criticalTriggered = (bool)data["criticalTriggered"];
        _timeSinceLastDrop = (float)data["timeSinceLastDrop"];

        OnSanityChanged?.Invoke(GetNormalized());
    }
}