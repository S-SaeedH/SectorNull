using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class WallLampLightPresetEditor : EditorWindow
{
    private enum LampColorPreset
    {
        ColdDirtyWhite,
        WarmDirtyWhite,
        EmergencyRed,
        SicklyGreen,
        Custom
    }

    private LampColorPreset colorPreset = LampColorPreset.ColdDirtyWhite;

    private Color customColor = new Color(0.82f, 0.86f, 0.90f);

    private float intensity = 0.35f;
    private float indirectMultiplier = 0.2f;
    private float range = 3f;

    private bool useSoftShadows = true;
    private float shadowStrength = 0.7f;

    [MenuItem("Tools/Lighting/Wall Lamp Preset Tool")]
    private static void OpenWindow()
    {
        WallLampLightPresetEditor window = GetWindow<WallLampLightPresetEditor>();
        window.titleContent = new GUIContent("Wall Lamp Preset");
        window.minSize = new Vector2(360, 330);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Selected Wall Lamp Light Preset", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select one or more GameObjects with Light components, choose a color preset, then apply the wall lamp settings.",
            MessageType.Info
        );

        EditorGUILayout.Space(8);

        colorPreset = (LampColorPreset)EditorGUILayout.EnumPopup("Color Preset", colorPreset);

        if (colorPreset == LampColorPreset.Custom)
        {
            customColor = EditorGUILayout.ColorField("Custom Color", customColor);
        }
        else
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ColorField("Preview Color", GetPresetColor());
            }
        }

        EditorGUILayout.Space(8);

        intensity = EditorGUILayout.Slider("Intensity", intensity, 0f, 2f);
        indirectMultiplier = EditorGUILayout.Slider("Indirect Multiplier", indirectMultiplier, 0f, 1f);
        range = EditorGUILayout.Slider("Range", range, 0.1f, 10f);

        EditorGUILayout.Space(8);

        useSoftShadows = EditorGUILayout.Toggle("Use Soft Shadows", useSoftShadows);

        if (useSoftShadows)
        {
            shadowStrength = EditorGUILayout.Slider("Shadow Strength", shadowStrength, 0f, 1f);
        }

        EditorGUILayout.Space(12);

        if (GUILayout.Button("Apply To Selected Lights", GUILayout.Height(35)))
        {
            ApplyPresetToSelectedLights();
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Reset To Normal Wall Lamp Defaults"))
        {
            intensity = 0.35f;
            indirectMultiplier = 0.2f;
            range = 3f;
            shadowStrength = 0.7f;
            colorPreset = LampColorPreset.ColdDirtyWhite;
        }

        EditorGUILayout.Space(12);

        EditorGUILayout.LabelField("Recommended Defaults", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Intensity: 0.35");
        EditorGUILayout.LabelField("Indirect Multiplier: 0.2");
        EditorGUILayout.LabelField("Range: 3");
        EditorGUILayout.LabelField("Mode: Baked");
        EditorGUILayout.LabelField("Type: Point");
    }

    private void ApplyPresetToSelectedLights()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected. Select GameObjects that have Light components.");
            return;
        }

        int changedCount = 0;
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

            Undo.RecordObject(light, "Apply Wall Lamp Light Preset");

            light.type = LightType.Point;
            light.lightmapBakeType = LightmapBakeType.Baked;

            light.intensity = intensity;
            light.bounceIntensity = indirectMultiplier;
            light.range = range;
            light.color = GetPresetColor();

            light.shadows = useSoftShadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = useSoftShadows ? shadowStrength : 0f;
            light.shadowResolution = LightShadowResolution.Medium;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;

            EditorUtility.SetDirty(light);
            changedCount++;
        }

        Debug.Log($"Applied wall lamp settings to {changedCount} light(s). Skipped {skippedCount} object(s).");

        if (changedCount > 0)
        {
            Debug.Log("Remember to rebake lighting: Window > Rendering > Lighting > Generate Lighting.");
        }
    }

    private Color GetPresetColor()
    {
        switch (colorPreset)
        {
            case LampColorPreset.ColdDirtyWhite:
                return new Color(0.82f, 0.86f, 0.90f);

            case LampColorPreset.WarmDirtyWhite:
                return new Color(1.0f, 0.86f, 0.65f);

            case LampColorPreset.EmergencyRed:
                return new Color(0.85f, 0.08f, 0.04f);

            case LampColorPreset.SicklyGreen:
                return new Color(0.45f, 0.80f, 0.45f);

            case LampColorPreset.Custom:
                return customColor;

            default:
                return new Color(0.82f, 0.86f, 0.90f);
        }
    }
}