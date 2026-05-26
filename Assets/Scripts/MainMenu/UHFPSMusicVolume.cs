using System;
using System.Collections;
using UnityEngine;
using UHFPS.Runtime;

[RequireComponent(typeof(AudioSource))]
public class UHFPSMusicVolume : MonoBehaviour
{
    [Header("UHFPS Option Name")]
    [SerializeField] private string musicOptionName = "volume_music";

    [Header("Music")]
    [Range(0f, 1f)]
    [SerializeField] private float baseVolume = 1f;

    private AudioSource audioSource;
    private float currentMusicVolume = 1f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ApplyVolume();
    }

    private IEnumerator Start()
    {
        while (OptionsManager.Instance == null)
            yield return null;

        OptionsManager.ObserveOption(musicOptionName, OnMusicVolumeChanged);
    }

    private void OnMusicVolumeChanged(object value)
    {
        currentMusicVolume = Convert.ToSingle(value);
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        if (audioSource == null)
            return;

        audioSource.volume = Mathf.Clamp01(currentMusicVolume) * baseVolume;
    }
}