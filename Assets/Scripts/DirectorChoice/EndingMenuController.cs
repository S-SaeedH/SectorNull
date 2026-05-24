using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingMenuController : MonoBehaviour
{
    public CanvasGroup endingMenuGroup;
    public GameObject creditsPanel;

    public string mainMenuSceneName = "MainMenu";
    public float fadeInDuration = 1.5f;

    public void ShowEndingMenu()
    {
        StopAllCoroutines();
        StartCoroutine(FadeInEndingMenu());
    }

    private IEnumerator FadeInEndingMenu()
    {
        if (endingMenuGroup == null)
            yield break;

        endingMenuGroup.alpha = 0f;
        endingMenuGroup.interactable = false;
        endingMenuGroup.blocksRaycasts = false;

        float timer = 0f;

        while (timer < fadeInDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeInDuration);

            endingMenuGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        endingMenuGroup.alpha = 1f;
        endingMenuGroup.interactable = true;
        endingMenuGroup.blocksRaycasts = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void HideCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        PlayerPrefs.SetInt("GameCompleted", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}