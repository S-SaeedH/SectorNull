using UnityEngine;
using Newtonsoft.Json.Linq;
using UHFPS.Runtime;

public class SaveableInteractionUnlocker : MonoBehaviour, ISaveable
{
    [Header("Objects To Change Layer")]
    [SerializeField] private GameObject[] layerObjects;

    [Header("Components To Enable")]
    [SerializeField] private Behaviour[] behavioursToEnable;

    [Header("Colliders To Enable")]
    [SerializeField] private Collider[] collidersToEnable;

    [Header("Layers")]
    [SerializeField] private string lockedLayerName = "Default";
    [SerializeField] private string unlockedLayerName = "Interact";
    [SerializeField] private bool includeChildren = true;

    [Header("State")]
    [SerializeField] private bool isUnlocked = false;

    [Header("Options")]
    [SerializeField] private bool startLocked = true;

    private int lockedLayer;
    private int unlockedLayer;
    private bool loaded;

    private void Awake()
    {
        CacheLayers();

        if (startLocked && !isUnlocked)
        {
            ApplyState(false);
        }
    }

    private void Start()
    {
        if (!loaded)
        {
            ApplyState(isUnlocked);
        }
    }

    public void Unlock()
    {
        isUnlocked = true;
        ApplyState(true);
    }

    public void Lock()
    {
        isUnlocked = false;
        ApplyState(false);
    }

    private void ApplyState(bool unlocked)
    {
        CacheLayers();

        ApplyLayers(unlocked);
        ApplyBehaviours(unlocked);
        ApplyColliders(unlocked);
    }

    private void ApplyLayers(bool unlocked)
    {
        int targetLayer = unlocked ? unlockedLayer : lockedLayer;

        if (targetLayer == -1)
            return;

        if (layerObjects == null)
            return;

        foreach (GameObject obj in layerObjects)
        {
            if (obj == null)
                continue;

            if (includeChildren)
                SetLayerRecursively(obj, targetLayer);
            else
                obj.layer = targetLayer;
        }
    }

    private void ApplyBehaviours(bool unlocked)
    {
        if (behavioursToEnable == null)
            return;

        foreach (Behaviour behaviour in behavioursToEnable)
        {
            if (behaviour != null)
                behaviour.enabled = unlocked;
        }
    }

    private void ApplyColliders(bool unlocked)
    {
        if (collidersToEnable == null)
            return;

        foreach (Collider col in collidersToEnable)
        {
            if (col != null)
                col.enabled = unlocked;
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void CacheLayers()
    {
        lockedLayer = LayerMask.NameToLayer(lockedLayerName);
        unlockedLayer = LayerMask.NameToLayer(unlockedLayerName);

        if (lockedLayer == -1)
            Debug.LogError($"Locked layer '{lockedLayerName}' does not exist.", this);

        if (unlockedLayer == -1)
            Debug.LogError($"Unlocked layer '{unlockedLayerName}' does not exist.", this);
    }

    public StorableCollection OnSave()
    {
        return new StorableCollection()
        {
            { nameof(isUnlocked), isUnlocked }
        };
    }

    public void OnLoad(JToken data)
    {
        loaded = true;

        if (data != null && data[nameof(isUnlocked)] != null)
            isUnlocked = (bool)data[nameof(isUnlocked)];

        ApplyState(isUnlocked);
    }

#if UNITY_EDITOR
    [ContextMenu("Force Locked Now")]
    private void ForceLockedNow()
    {
        isUnlocked = false;
        ApplyState(false);
    }

    [ContextMenu("Force Unlocked Now")]
    private void ForceUnlockedNow()
    {
        isUnlocked = true;
        ApplyState(true);
    }
#endif
}