using UnityEngine;
using Newtonsoft.Json.Linq;
using UHFPS.Runtime;
using UHFPS.Tools;

public class UHFPSAutoSaveTrigger : MonoBehaviour, ISaveable
{
    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool disableColliderAfterUse = true;

    [Header("Debug")]
    [SerializeField] private bool logSave = false;

    private bool used;
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void Start()
    {
        ApplyUsedState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && used)
            return;

        if (!other.CompareTag(playerTag))
            return;

        used = true;
        ApplyUsedState();

        TriggerAutoSave();
    }

    public void TriggerAutoSave()
    {
        SaveGameManager.SaveGame(true);

        if (logSave)
            Debug.Log("Autosave triggered.");
    }

    private void ApplyUsedState()
    {
        if (!disableColliderAfterUse)
            return;

        if (triggerCollider != null)
            triggerCollider.enabled = !used;
    }

    public StorableCollection OnSave()
    {
        return new StorableCollection()
        {
            { "used", used }
        };
    }

    public void OnLoad(JToken data)
    {
        if (data["used"] != null)
            used = (bool)data["used"];

        ApplyUsedState();
    }
}