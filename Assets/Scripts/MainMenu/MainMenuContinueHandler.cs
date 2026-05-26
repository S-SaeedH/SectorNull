using System.Collections;
using UnityEngine;

public class MainMenuContinueHandler : MonoBehaviour
{
    public GameObject continueButton;

    private IEnumerator Start()
    {
        yield return null; // wait for Saves UI Loader to finish

        if (PlayerPrefs.GetInt("GameCompleted", 0) == 1)
        {
            if (continueButton != null)
                continueButton.SetActive(false);
        }
    }

    public void StartNewGame()
    {
        PlayerPrefs.DeleteKey("GameCompleted");
        PlayerPrefs.Save();
    }
}