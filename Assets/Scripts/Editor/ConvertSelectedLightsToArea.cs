using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static class ConvertSelectedLightsToArea
{
    [MenuItem("Tools/Lighting/Convert Selected Lights To Area Lights")]
    private static void ConvertSelectedLights()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected. Select GameObjects that have Light components.");
            return;
        }

        int convertedCount = 0;
        int skippedCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            Light light = obj.GetComponent<Light>();

            if (light == null)
            {
                Debug.LogWarning($"Skipped '{obj.name}' because it has no Light component.");
                skippedCount++;
                continue;
            }

            Undo.RecordObject(light, "Convert Selected Lights To Area Lights");

            // Area Light is the best baked type for fluorescent ceiling fixtures.
            light.type = LightType.Rectangle;
            light.lightmapBakeType = LightmapBakeType.Baked;

            // Test values based on your current scene.
            // Bright enough to affect the room, but still horror-dark.
            light.intensity = 2.0f;
            light.range = 6.0f;

            // Dirty cold white, better than pure white for horror/facility lighting.
            light.color = new Color(0.82f, 0.86f, 0.90f);

            // This is the "Indirect Multiplier" shown in the Light inspector.
            // Keep it low so baked bounce light does not brighten the whole room.
            light.bounceIntensity = 0.3f;

            // Unity 6 uses Vector2 for Area Light size.
            // X = width, Y = height.
            // Good shape for fluorescent tube lights.
            light.areaSize = new Vector2(1.2f, 0.25f);

            // Softer baked shadows.
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            light.shadowResolution = LightShadowResolution.Medium;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;

            EditorUtility.SetDirty(light);
            convertedCount++;
        }

        Debug.Log($"Converted {convertedCount} light(s) to baked Area Lights. Skipped {skippedCount} object(s).");

        if (convertedCount > 0)
        {
            Debug.Log("Remember to rebake lighting: Window > Rendering > Lighting > Generate Lighting.");
        }
    }

    [MenuItem("Tools/Lighting/Convert Selected Lights To Area Lights", true)]
    private static bool ValidateConvertSelectedLights()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }
}