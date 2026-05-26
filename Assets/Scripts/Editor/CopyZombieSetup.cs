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
        typeof(Transform),
        typeof(Animator),              // Keep target avatar
        typeof(SkinnedMeshRenderer),
        typeof(MeshRenderer),
        typeof(MeshFilter)
    };

    private static readonly HashSet<System.Type> ChildSkipTypes = new()
    {
        typeof(Transform),
        typeof(Animator),
        typeof(SkinnedMeshRenderer),
        typeof(MeshRenderer),
        typeof(MeshFilter)
    };

    [MenuItem("Tools/Zombie/Copy Full Zombie Setup FIXED")]
    private static void CopyFullZombieSetupFixed()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected.Length != 2)
        {
            Debug.LogError("Select exactly 2 objects: first SOURCE, second TARGET.");
            return;
        }

        GameObject sourceRoot = selected[0];
        GameObject targetRoot = selected[1];

        if (!EditorUtility.DisplayDialog(
                "Copy Zombie Setup",
                $"SOURCE: {sourceRoot.name}\nTARGET: {targetRoot.name}\n\nContinue?",
                "Copy",
                "Cancel"))
        {
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(targetRoot, "Copy Full Zombie Setup Fixed");

        Dictionary<string, Transform> sourceMap = BuildBoneMap(sourceRoot.transform);
        Dictionary<string, Transform> targetMap = BuildBoneMap(targetRoot.transform);

        Dictionary<Object, Object> referenceMap = new();

        // Root object mapping.
        referenceMap[sourceRoot] = targetRoot;
        referenceMap[sourceRoot.transform] = targetRoot.transform;
        referenceMap[sourceRoot.gameObject] = targetRoot.gameObject;

        // Bone object mapping.
        foreach (var sourcePair in sourceMap)
        {
            string key = sourcePair.Key;
            Transform sourceBone = sourcePair.Value;

            if (!targetMap.TryGetValue(key, out Transform targetBone))
                continue;

            referenceMap[sourceBone] = targetBone;
            referenceMap[sourceBone.gameObject] = targetBone.gameObject;
        }

        CopyTagAndLayer(sourceRoot, targetRoot);

        // Keep target Animator, but copy safe Animator settings from source.
        CopyAnimatorSettingsSafely(sourceRoot, targetRoot);

        // Remove and copy root components.
        RemoveExistingComponents(targetRoot, RootSkipTypes);
        CopyComponentsFromTo(sourceRoot, targetRoot, RootSkipTypes, referenceMap);

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
            CopyComponentsFromTo(sourceBone.gameObject, targetBone.gameObject, ChildSkipTypes, referenceMap);

            copiedBones++;
        }

        // Now remap references AFTER the new components exist.
        RemapObjectReferences(targetRoot, referenceMap);

        EditorUtility.SetDirty(targetRoot);

        Debug.Log($"Done FIXED. Parent copied. Bones copied: {copiedBones}. Missing bones: {missingBones}.");
        Debug.Log("Now select the target zombie, click NPC Health -> Find Body Parts, then Apply Prefab.");
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

    private static void CopyComponentsFromTo(
        GameObject source,
        GameObject target,
        HashSet<System.Type> skipTypes,
        Dictionary<Object, Object> referenceMap)
    {
        Component[] sourceComponents = source.GetComponents<Component>();

        foreach (Component sourceComponent in sourceComponents)
        {
            if (sourceComponent == null)
                continue;

            System.Type type = sourceComponent.GetType();

            if (skipTypes.Contains(type))
                continue;

            ComponentUtility.CopyComponent(sourceComponent);
            ComponentUtility.PasteComponentAsNew(target);

            Component copiedComponent = GetLastComponentOfType(target, type);

            if (copiedComponent != null)
            {
                referenceMap[sourceComponent] = copiedComponent;
                EditorUtility.SetDirty(copiedComponent);
            }
            else
            {
                Debug.LogWarning($"Copied component mapping failed: {source.name} -> {target.name}, Type: {type.Name}");
            }
        }
    }

    private static Component GetLastComponentOfType(GameObject obj, System.Type type)
    {
        Component[] components = obj.GetComponents<Component>();
        Component last = null;

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            if (component.GetType() == type)
                last = component;
        }

        return last;
    }

    private static void CopyAnimatorSettingsSafely(GameObject sourceRoot, GameObject targetRoot)
    {
        Animator sourceAnimator = sourceRoot.GetComponent<Animator>();
        Animator targetAnimator = targetRoot.GetComponent<Animator>();

        if (sourceAnimator == null || targetAnimator == null)
            return;

        Undo.RecordObject(targetAnimator, "Copy Animator Settings Safely");

        // Keep target avatar. Only copy controller/settings.
        targetAnimator.runtimeAnimatorController = sourceAnimator.runtimeAnimatorController;
        targetAnimator.applyRootMotion = sourceAnimator.applyRootMotion;
        targetAnimator.animatePhysics = sourceAnimator.animatePhysics;
        targetAnimator.updateMode = sourceAnimator.updateMode;
        targetAnimator.cullingMode = sourceAnimator.cullingMode;

        EditorUtility.SetDirty(targetAnimator);

        Debug.Log($"Animator kept on target '{targetRoot.name}'. Target avatar preserved: {targetAnimator.avatar?.name}");
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

            // Use Next instead of NextVisible so hidden serialized references are also checked.
            while (property.Next(enterChildren))
            {
                enterChildren = true;

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

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }
    }
}