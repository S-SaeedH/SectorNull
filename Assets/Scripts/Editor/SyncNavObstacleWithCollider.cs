using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

public class SyncNavObstacleWithCollider : EditorWindow
{
    [MenuItem("Tools/NavMesh/Sync Obstacles With Box Colliders")]
    private static void SyncSelected()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("Select the parent object or the blocker objects first.");
            return;
        }

        int count = 0;

        foreach (GameObject selected in selectedObjects)
        {
            BoxCollider[] colliders = selected.GetComponentsInChildren<BoxCollider>(true);

            foreach (BoxCollider box in colliders)
            {
                NavMeshObstacle obstacle = box.GetComponent<NavMeshObstacle>();

                if (obstacle == null)
                    obstacle = box.gameObject.AddComponent<NavMeshObstacle>();

                Undo.RecordObject(obstacle, "Sync NavMeshObstacle With BoxCollider");

                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = box.center;
                obstacle.size = box.size;
                obstacle.carving = true;

                EditorUtility.SetDirty(obstacle);
                count++;
            }
        }

        Debug.Log($"Synced {count} NavMeshObstacle components with Box Colliders.");
    }
}