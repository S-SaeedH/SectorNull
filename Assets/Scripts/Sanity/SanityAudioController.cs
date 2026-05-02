using UnityEngine;
using UnityEngine.Audio;

public class SanityAudioController : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Ambient / Music Source")]
    public AudioSource ambientSource;

    [Header("Hallucination Sounds")]
    public AudioClip[] hallucinationSounds;
    public AudioSource hallucinationSource; // Separate AudioSource for hallucinations

    [Header("Pitch Settings")]
    public float maxPitch = 1.0f;
    public float minPitch = 0.7f;

    [Header("Hallucination Settings")]
    [Range(0f, 1f)] public float hallucinationChance = 0.3f;
    public float hallucinationInterval = 8f;

    private float _hallucinationTimer;
    private float _currentSanityNorm = 1f;
    private float _lowSanityThreshold; // Cached from SanityManager

    void Start()
    {
        SanityManager.Instance.OnSanityChanged.AddListener(OnSanityChanged);
        _hallucinationTimer = hallucinationInterval;
        _lowSanityThreshold = SanityManager.Instance.lowSanityThreshold
                              / SanityManager.Instance.maxSanity;

        // Make sure hallucinationSource doesn't loop
        if (hallucinationSource != null)
            hallucinationSource.loop = false;

        UpdateAudioEffects(1f);
    }

    void OnSanityChanged(float normalized)
    {
        _currentSanityNorm = normalized;
        UpdateAudioEffects(normalized);

        // Stop hallucination sound immediately when sanity rises above threshold
        if (normalized > _lowSanityThreshold && hallucinationSource != null
            && hallucinationSource.isPlaying)
        {
            hallucinationSource.Stop();
        }
    }

    void Update()
    {
        if (_currentSanityNorm < _lowSanityThreshold && hallucinationSounds.Length > 0)
        {
            _hallucinationTimer -= Time.deltaTime;
            if (_hallucinationTimer <= 0f)
            {
                _hallucinationTimer = hallucinationInterval;
                if (Random.value < hallucinationChance)
                    PlayHallucinationSound();
            }
        }
    }

    void UpdateAudioEffects(float normalized)
    {
        float insanity = 1f - normalized;

        if (ambientSource != null)
            audioMixer.SetFloat("SanityPitch", Mathf.Lerp(maxPitch, minPitch, insanity));

        audioMixer.SetFloat("SanityVolume", Mathf.Lerp(0f, -6f, insanity));

        hallucinationInterval = Mathf.Lerp(15f, 4f, insanity);
    }

    void PlayHallucinationSound()
    {
        if (hallucinationSource == null || hallucinationSounds.Length == 0) return;

        // Don't interrupt a currently playing hallucination
        if (hallucinationSource.isPlaying) return;

        hallucinationSource.clip = hallucinationSounds[Random.Range(0, hallucinationSounds.Length)];
        hallucinationSource.volume = Random.Range(0.3f, 0.8f);
        hallucinationSource.Play();
    }
}