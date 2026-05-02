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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (decayEnabled && currentSanity > 0f)
            ModifySanity(-decayRate * Time.deltaTime);
    }

    public void ModifySanity(float amount)
    {
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
            { "currentSanity",      currentSanity  },
            { "decayEnabled",       decayEnabled   },
            { "lowTriggered",       _lowTriggered  },
            { "criticalTriggered",  _criticalTriggered }
        };
    }

    public void OnLoad(JToken data)
    {
        currentSanity = (float)data["currentSanity"];
        decayEnabled = (bool)data["decayEnabled"];
        _lowTriggered = (bool)data["lowTriggered"];
        _criticalTriggered = (bool)data["criticalTriggered"];

        // Re-fire the changed event so all effects/UI immediately
        // reflect the loaded sanity value without waiting for next Update
        OnSanityChanged?.Invoke(GetNormalized());
    }
}