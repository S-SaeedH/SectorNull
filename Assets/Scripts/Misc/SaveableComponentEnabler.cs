using UnityEngine;
using Newtonsoft.Json.Linq;
using UHFPS.Runtime;

public class SaveableComponentEnabler : MonoBehaviour, ISaveable
{
    [Header("Components To Enable")]
    [SerializeField] private Behaviour[] componentsToEnable;

    [Header("State")]
    [SerializeField] private bool isEnabled;

    [Header("Options")]
    [SerializeField] private bool disableOnStart = true;

    private void Awake()
    {
        if (disableOnStart && !isEnabled)
            SetComponents(false);
    }

    public void EnableComponents()
    {
        isEnabled = true;
        SetComponents(true);
    }

    public void DisableComponents()
    {
        isEnabled = false;
        SetComponents(false);
    }

    private void SetComponents(bool state)
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
            { nameof(isEnabled), isEnabled }
        };
    }

    public void OnLoad(JToken data)
    {
        if (data == null)
            return;

        isEnabled = data[nameof(isEnabled)] != null && (bool)data[nameof(isEnabled)];

        SetComponents(isEnabled);
    }
}