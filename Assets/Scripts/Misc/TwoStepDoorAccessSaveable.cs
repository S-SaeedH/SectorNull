using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;
using UHFPS.Runtime;

public class TwoStepDoorAccessSaveable : MonoBehaviour, ISaveable
{
    [Header("Access State")]
    [SerializeField] private bool keycardGranted;
    [SerializeField] private bool generatorRunning;

    [Header("Components To Enable When Unlocked")]
    [SerializeField] private Behaviour[] componentsToEnable;

    [Header("Door Action")]
    public UnityEvent OnBothConditionsMet;

    [Header("Options")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool alreadyTriggered;

    private void Awake()
    {
        if (!alreadyTriggered)
            SetInteractionEnabled(false);
    }

    public void SetKeycardGranted()
    {
        keycardGranted = true;
        CheckAccess();
    }

    public void SetGeneratorRunning()
    {
        generatorRunning = true;
        CheckAccess();
    }

    private void CheckAccess()
    {
        if (triggerOnlyOnce && alreadyTriggered)
            return;

        if (keycardGranted && generatorRunning)
        {
            UnlockInteraction();
        }
    }

    private void UnlockInteraction()
    {
        alreadyTriggered = true;

        SetInteractionEnabled(true);

        OnBothConditionsMet?.Invoke();
    }

    private void SetInteractionEnabled(bool state)
    {
        foreach (Behaviour component in componentsToEnable)
        {
            if (component != null)
                component.enabled = state;
        }
    }

    public StorableCollection OnSave()
    {
        return new StorableCollection()
        {
            { nameof(keycardGranted), keycardGranted },
            { nameof(generatorRunning), generatorRunning },
            { nameof(alreadyTriggered), alreadyTriggered }
        };
    }

    public void OnLoad(JToken data)
    {
        if (data == null)
            return;

        keycardGranted = data[nameof(keycardGranted)] != null && (bool)data[nameof(keycardGranted)];
        generatorRunning = data[nameof(generatorRunning)] != null && (bool)data[nameof(generatorRunning)];
        alreadyTriggered = data[nameof(alreadyTriggered)] != null && (bool)data[nameof(alreadyTriggered)];

        SetInteractionEnabled(alreadyTriggered);
    }
}