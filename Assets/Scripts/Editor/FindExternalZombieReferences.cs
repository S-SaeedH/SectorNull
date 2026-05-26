using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FindExternalZombieReferences
{
    [MenuItem("Tools/Zombie/Find External References On Selected")]
    private static void FindExternalReferences()
    {
        GameObject root = Selection.activeGameObject;

        if (root == null)
        {
            Debug.LogError("Select the zombie root first.");
            return;
        }

        Component[] components = root.GetComponentsInChildren<Component>(true);
        int found = 0;

        foreach (Component component in components)
        {
            if (component == null) continue;

            SerializedObject so = new SerializedObject(component);
            SerializedProperty prop = so.GetIterator();

            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                Object obj = prop.objectReferenceValue;
                if (obj == null) continue;

                GameObject referencedGO = null;

                if (obj is GameObject go)
                    referencedGO = go;
                else if (obj is Component referencedComponent)
                    referencedGO = referencedComponent.gameObject;

                if (referencedGO == null)
                    continue;

                if (!referencedGO.transform.IsChildOf(root.transform))
                {
                    found++;

                    Debug.LogWarning(
                        $"External reference found on '{component.gameObject.name}' component '{component.GetType().Name}' " +
                        $"property '{prop.displayName}' → '{obj.name}'",
                        component
                    );
                }
            }
        }

        if (found == 0)
            Debug.Log($"No external references found on {root.name}.");
        else
            Debug.LogWarning($"Found {found} external references on {root.name}. Click warnings to locate them.");
    }
}