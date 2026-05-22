using UnityEngine;
using UnityEngine.Playables;

public class DirectorChoiceManager : MonoBehaviour
{
    [Header("Choice UI")]
    public GameObject choiceCanvas;

    [Header("Timeline")]
    public PlayableDirector cutsceneDirector;

    public void ShowChoice()
    {
        if (choiceCanvas != null)
            choiceCanvas.SetActive(true);

        if (cutsceneDirector != null)
            cutsceneDirector.Pause();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void KillDirector()
    {
        Debug.Log("Player chose to kill the director.");

        if (choiceCanvas != null)
            choiceCanvas.SetActive(false);

        if (cutsceneDirector != null)
            cutsceneDirector.Resume();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SpareDirector()
    {
        Debug.Log("Player chose to spare the director.");

        if (choiceCanvas != null)
            choiceCanvas.SetActive(false);

        if (cutsceneDirector != null)
            cutsceneDirector.Resume();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}