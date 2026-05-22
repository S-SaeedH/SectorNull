using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CopyZombieSetup
{
    private static readonly HashSet<System.Type> RootSkipTypes = new()
    {
        typeof(Transform)
    };

    private static readonly HashSet<System.Type> ChildSkipTypes = new()
    {
        typeof(Transform),
        typeof(Animator),
        typeof(SkinnedMeshRenderer),
        typeof(MeshRenderer),
        typeof(MeshFilter)
    };

    [MenuItem("Tools/Zombie/Copy Full Zombie Setup")]
    private static void CopyFullZombieSetup()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected.Length != 2)
        {
            Debug.LogError("Select exactly 2 objects: first SOURCE, second TARGET.");
            return;
        }

        GameObject sourceRoot = selected[0];
        GameObject targetRoot = selected[1];

        Undo.RegisterFullObjectHierarchyUndo(targetRoot, "Copy Full Zombie Setup");

        Dictionary<string, Transform> sourceMap = BuildBoneMap(sourceRoot.transform);
        Dictionary<string, Transform> targetMap = BuildBoneMap(targetRoot.transform);

        Dictionary<Object, Object> referenceMap = new();

        BuildReferenceMap(sourceRoot.transform, targetRoot.transform, sourceMap, targetMap, referenceMap);

        CopyTagAndLayer(sourceRoot, targetRoot);
        RemoveExistingComponents(targetRoot, RootSkipTypes);
        CopyComponentsFromTo(sourceRoot, targetRoot, RootSkipTypes);

        int copiedBones = 0;
        int missingBones = 0;

        foreach (var sourcePair in sourceMap)
        {
            string key = sourcePair.Key;
            Transform sourceBone = sourcePair.Value;

            if (!targetMap.TryGetValue(key, out Transform targetBone))
            {
                missingBones++;
                Debug.LogWarning($"No matching target bone found for '{sourceBone.name}' | key: {key}");
                continue;
            }

            CopyTagAndLayer(sourceBone.gameObject, targetBone.gameObject);
            RemoveExistingComponents(targetBone.gameObject, ChildSkipTypes);
            CopyComponentsFromTo(sourceBone.gameObject, targetBone.gameObject, ChildSkipTypes);

            copiedBones++;
        }

        RemapObjectReferences(targetRoot, referenceMap);

        EditorUtility.SetDirty(targetRoot);

        Debug.Log($"Done. Parent copied. Bones copied: {copiedBones}. Missing bones: {missingBones}.");
    }

    private static Dictionary<string, Transform> BuildBoneMap(Transform root)
    {
        Dictionary<string, Transform> map = new();

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string key = NormalizeBoneName(child.name);

            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                map.Add(key, child);
        }

        return map;
    }

    private static string NormalizeBoneName(string rawName)
    {
        string name = rawName;

        name = name.Replace("mixamorig:", "");
        name = name.Replace("mixamorig_", "");
        name = name.Replace("mixamorig", "");

        name = name.Trim();

        bool left =
            Regex.IsMatch(name, @"(\.L|_L|-L)$", RegexOptions.IgnoreCase) ||
            name.StartsWith("Left", System.StringComparison.OrdinalIgnoreCase);

        bool right =
            Regex.IsMatch(name, @"(\.R|_R|-R)$", RegexOptions.IgnoreCase) ||
            name.StartsWith("Right", System.StringComparison.OrdinalIgnoreCase);

        name = Regex.Replace(name, @"(\.L|_L|-L)$", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"(\.R|_R|-R)$", "", RegexOptions.IgnoreCase);

        name = Regex.Replace(name, "^Left", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, "^Right", "", RegexOptions.IgnoreCase);

        name = name.ToLowerInvariant();
        name = name.Replace(".", "");
        name = name.Replace("_", "");
        name = name.Replace("-", "");
        name = name.Replace(" ", "");

        if (left)
            name = "l" + name;

        if (right)
            name = "r" + name;

        return name;
    }

    private static void BuildReferenceMap(
        Transform sourceRoot,
        Transform targetRoot,
        Dictionary<string, Transform> sourceMap,
        Dictionary<string, Transform> targetMap,
        Dictionary<Object, Object> referenceMap)
    {
        referenceMap[sourceRoot] = targetRoot;
        referenceMap[sourceRoot.gameObject] = targetRoot.gameObject;

        foreach (var sourcePair in sourceMap)
        {
            string key = sourcePair.Key;
            Transform sourceTransform = sourcePair.Value;

            if (!targetMap.TryGetValue(key, out Transform targetTransform))
                continue;

            referenceMap[sourceTransform] = targetTransform;
            referenceMap[sourceTransform.gameObject] = targetTransform.gameObject;
        }

        foreach (var sourcePair in sourceMap)
        {
            string key = sourcePair.Key;
            Transform sourceTransform = sourcePair.Value;

            if (!targetMap.TryGetValue(key, out Transform targetTransform))
                continue;

            Component[] sourceComponents = sourceTransform.GetComponents<Component>();
            Component[] targetComponents = targetTransform.GetComponents<Component>();

            foreach (Component sourceComponent in sourceComponents)
            {
                if (sourceComponent == null)
                    continue;

                foreach (Component targetComponent in targetComponents)
                {
                    if (targetComponent == null)
                        continue;

                    if (sourceComponent.GetType() == targetComponent.GetType())
                    {
                        referenceMap[sourceComponent] = targetComponent;
                        break;
                    }
                }
            }
        }

        Component[] sourceRootComponents = sourceRoot.GetComponents<Component>();
        Component[] targetRootComponents = targetRoot.GetComponents<Component>();

        foreach (Component sourceComponent in sourceRootComponents)
        {
            if (sourceComponent == null)
                continue;

            foreach (Component targetComponent in targetRootComponents)
            {
                if (targetComponent == null)
                    continue;

                if (sourceComponent.GetType() == targetComponent.GetType())
                {
                    referenceMap[sourceComponent] = targetComponent;
                    break;
                }
            }
        }
    }

    private static void CopyTagAndLayer(GameObject source, GameObject target)
    {
        try
        {
            target.tag = source.tag;
        }
        catch
        {
            Debug.LogWarning($"Could not copy tag '{source.tag}' to '{target.name}'. Make sure the tag exists.");
        }

        target.layer = source.layer;
    }

    private static void RemoveExistingComponents(GameObject target, HashSet<System.Type> skipTypes)
    {
        Component[] components = target.GetComponents<Component>();

        for (int i = components.Length - 1; i >= 0; i--)
        {
            Component component = components[i];

            if (component == null)
                continue;

            System.Type type = component.GetType();

            if (skipTypes.Contains(type))
                continue;

            Undo.DestroyObjectImmediate(component);
        }
    }

    private static void CopyComponentsFromTo(GameObject source, GameObject target, HashSet<System.Type> skipTypes)
    {
        Component[] components = source.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            System.Type type = component.GetType();

            if (skipTypes.Contains(type))
                continue;

            ComponentUtility.CopyComponent(component);
            ComponentUtility.PasteComponentAsNew(target);
        }
    }

    private static void RemapObjectReferences(GameObject targetRoot, Dictionary<Object, Object> referenceMap)
    {
        Component[] components = targetRoot.GetComponentsInChildren<Component>(true);

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.GetIterator();

            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                Object oldReference = property.objectReferenceValue;

                if (oldReference == null)
                    continue;

                if (referenceMap.TryGetValue(oldReference, out Object newReference))
                {
                    property.objectReferenceValue = newReference;
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
        }
    }
}