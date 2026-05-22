using UnityEditor;
using UnityEngine;

public class CreateZombieDoorTriggers : EditorWindow
{
    private const string TriggerName = "ZombieDoorTrigger";
    private const string BlockerName = "ZombieDoorBlocker";

    [MenuItem("Tools/Doors/Create Zombie Door Triggers")]
    private static void CreateTriggersForSelectedDoors()
    {
        GameObject[] selectedDoors = Selection.gameObjects;

        if (selectedDoors.Length == 0)
        {
            Debug.LogWarning("Select your door root objects first.");
            return;
        }

        int created = 0;
        int skipped = 0;

        foreach (GameObject doorRoot in selectedDoors)
        {
            if (doorRoot == null)
                continue;

            Transform existingTrigger = doorRoot.transform.Find(TriggerName);

            if (existingTrigger != null)
            {
                skipped++;
                continue;
            }

            Undo.RegisterFullObjectHierarchyUndo(doorRoot, "Create Zombie Door Trigger");

            GameObject triggerObj = new GameObject(TriggerName);
            Undo.RegisterCreatedObjectUndo(triggerObj, "Create Zombie Door Trigger");

            triggerObj.transform.SetParent(doorRoot.transform);
            triggerObj.transform.localRotation = Quaternion.identity;

            Bounds bounds = CalculateBounds(doorRoot);

            float width = Mathf.Max(bounds.size.x, 1.5f);
            float height = Mathf.Max(bounds.size.y, 2.5f);
            float depth = 2f;

            // Places the trigger slightly in front of the door using the door's local forward direction.
            triggerObj.transform.position = bounds.center + doorRoot.transform.forward * 1.2f;

            BoxCollider triggerCollider = triggerObj.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(width, height, depth);

            ZombieDoorOpener opener = triggerObj.AddComponent<ZombieDoorOpener>();

            // Set NPC layer automatically if it exists.
            int npcLayer = LayerMask.NameToLayer("NPC");
            if (npcLayer != -1)
                opener.zombieLayer = 1 << npcLayer;
            else
                Debug.LogWarning("NPC layer was not found. Assign the zombieLayer manually on " + triggerObj.name);

            // Try to find the UHFPS door script automatically.
            opener.uhfpsDoorScript = FindLikelyDoorScript(doorRoot);

            // Try to find the blocker sync automatically.
            DoorNavObstacleSync sync = doorRoot.GetComponentInChildren<DoorNavObstacleSync>(true);
            opener.doorBlockerSync = sync;

            EditorUtility.SetDirty(triggerObj);
            created++;
        }

        Debug.Log($"Created {created} zombie door triggers. Skipped {skipped} because they already existed.");
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;

            foreach (Renderer renderer in renderers)
                bounds.Encapsulate(renderer.bounds);

            return bounds;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;

            foreach (Collider collider in colliders)
                bounds.Encapsulate(collider.bounds);

            return bounds;
        }

        return new Bounds(root.transform.position, new Vector3(1.5f, 2.5f, 0.5f));
    }

    private static MonoBehaviour FindLikelyDoorScript(GameObject root)
    {
        MonoBehaviour[] scripts = root.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null)
                continue;

            string typeName = script.GetType().Name.ToLower();

            // Avoid selecting our own helper scripts.
            if (typeName.Contains("zombiedooropener"))
                continue;

            if (typeName.Contains("doornavobstaclesync"))
                continue;

            // Pick a likely UHFPS/dynamic door script.
            if (typeName.Contains("door"))
                return script;
        }

        return null;
    }
}