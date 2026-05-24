using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using Cinemachine;

public class DirectorChoiceManager : MonoBehaviour
{
    [Header("Choice UI")]
    public GameObject choiceCanvas;
    public CanvasGroup choiceCanvasGroup;
    public float fadeDuration = 2f;

    [Header("Timeline")]
    public PlayableDirector cutsceneDirector;

    [Header("Choice Effects")]
    public AudioSource choiceAudio;
    public AudioSource hallucinationAudio;
    public AudioSource whisperAudio;
    public CinemachineVirtualCamera choiceCamera;
    public GameObject choicePostFX;

    [Header("Camera Shake Settings")]
    public float shakeAmplitude = 0.12f;
    public float shakeFrequency = 0.8f;

    [Header("Fainting Camera Movement")]
    public Transform cameraSwayTarget;
    public float swayAmount = 2.0f;
    public float swaySpeed = 0.55f;
    public float rollAmount = 1.2f;

    [Header("Choice Result")]
    public bool killedDirector;
    public bool sparedDirector;

    private CinemachineBasicMultiChannelPerlin cameraNoise;
    private Quaternion originalCameraRotation;
    private bool choiceActive = false;
    private Coroutine fadeRoutine;

    private void Start()
    {
        if (choiceCamera != null)
        {
            cameraNoise = choiceCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

            if (cameraNoise != null)
            {
                cameraNoise.m_AmplitudeGain = 0f;
                cameraNoise.m_FrequencyGain = 0f;
            }
        }

        if (choicePostFX != null)
            choicePostFX.SetActive(false);

        if (choiceCanvasGroup != null)
        {
            choiceCanvasGroup.alpha = 0f;
            choiceCanvasGroup.interactable = false;
            choiceCanvasGroup.blocksRaycasts = false;
        }

        if (choiceCanvas != null)
            choiceCanvas.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!choiceActive || cameraSwayTarget == null)
            return;

        float x = Mathf.Sin(Time.unscaledTime * swaySpeed) * swayAmount;
        float y = Mathf.Sin(Time.unscaledTime * swaySpeed * 0.7f) * swayAmount;
        float z = Mathf.Sin(Time.unscaledTime * swaySpeed * 0.45f) * rollAmount;

        cameraSwayTarget.localRotation = originalCameraRotation * Quaternion.Euler(x, y, z);
    }

    public void ShowChoice()
    {
        killedDirector = false;
        sparedDirector = false;

        if (choiceCanvas != null)
            choiceCanvas.SetActive(true);

        if (choiceCanvasGroup != null)
        {
            choiceCanvasGroup.gameObject.SetActive(true);
            choiceCanvasGroup.alpha = 0f;
            choiceCanvasGroup.interactable = false;
            choiceCanvasGroup.blocksRaycasts = false;
        }

        if (choicePostFX != null)
            choicePostFX.SetActive(true);

        if (choiceAudio != null)
            choiceAudio.Play();

        if (hallucinationAudio != null)
            hallucinationAudio.Play();

        if (whisperAudio != null)
            whisperAudio.Play();

        if (cameraSwayTarget != null)
            originalCameraRotation = cameraSwayTarget.localRotation;

        choiceActive = true;

        if (cameraNoise != null)
        {
            cameraNoise.m_AmplitudeGain = shakeAmplitude;
            cameraNoise.m_FrequencyGain = shakeFrequency;
        }

        if (cutsceneDirector != null)
            cutsceneDirector.Pause();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeInChoiceUI());
    }

    public void KillDirector()
    {
        Debug.Log("Player chose to kill the director.");

        killedDirector = true;
        sparedDirector = false;

        ResolveChoiceWithoutHidingUI();
    }

    public void SpareDirector()
    {
        Debug.Log("Player chose to spare the director.");

        killedDirector = false;
        sparedDirector = true;

        ResolveChoiceWithoutHidingUI();
    }

    private void ResolveChoiceWithoutHidingUI()
    {
        choiceActive = false;

        if (choiceAudio != null)
            choiceAudio.Stop();

        if (hallucinationAudio != null)
            hallucinationAudio.Stop();

        if (whisperAudio != null)
            whisperAudio.Stop();

        if (choicePostFX != null)
            choicePostFX.SetActive(false);

        if (cameraNoise != null)
        {
            cameraNoise.m_AmplitudeGain = 0f;
            cameraNoise.m_FrequencyGain = 0f;
        }

        if (cameraSwayTarget != null)
            cameraSwayTarget.localRotation = originalCameraRotation;

        if (choiceCanvasGroup != null)
        {
            choiceCanvasGroup.interactable = false;
            choiceCanvasGroup.blocksRaycasts = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cutsceneDirector != null)
            cutsceneDirector.Resume();
    }

    public void ForceHideChoiceUI()
    {
        if (choiceCanvasGroup != null)
        {
            choiceCanvasGroup.alpha = 0f;
            choiceCanvasGroup.interactable = false;
            choiceCanvasGroup.blocksRaycasts = false;
        }

        if (choiceCanvas != null)
            choiceCanvas.SetActive(false);
    }

    private IEnumerator FadeInChoiceUI()
    {
        if (choiceCanvasGroup == null)
            yield break;

        choiceCanvasGroup.alpha = 0f;
        choiceCanvasGroup.interactable = false;
        choiceCanvasGroup.blocksRaycasts = false;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);

            choiceCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        choiceCanvasGroup.alpha = 1f;
        choiceCanvasGroup.interactable = true;
        choiceCanvasGroup.blocksRaycasts = true;
    }
}