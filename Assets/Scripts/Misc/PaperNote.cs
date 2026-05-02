using TMPro;
using UnityEngine;

public class PaperNote : MonoBehaviour
{
    [Header("Text Object")]
    [SerializeField] private TMP_Text paperText;

    [Header("Note Content")]
    [TextArea(5, 15)]
    [SerializeField] private string noteContent = "Write your note here...";

    [Header("Text Style")]
    [SerializeField] private Color inkColor = new Color(0.08f, 0.07f, 0.06f);
    [SerializeField] private float fontSize = 0.15f;

    private void Awake()
    {
        ApplyNote();
    }

    private void OnValidate()
    {
        ApplyNote();
    }

    public void SetText(string newText)
    {
        noteContent = newText;
        ApplyNote();
    }

    private void ApplyNote()
    {
        if (paperText == null)
            return;

        paperText.text = noteContent;
        paperText.color = inkColor;
        paperText.fontSize = fontSize;

        paperText.textWrappingMode = TextWrappingModes.Normal;
        paperText.overflowMode = TextOverflowModes.Truncate;
        paperText.alignment = TextAlignmentOptions.TopLeft;
    }
}