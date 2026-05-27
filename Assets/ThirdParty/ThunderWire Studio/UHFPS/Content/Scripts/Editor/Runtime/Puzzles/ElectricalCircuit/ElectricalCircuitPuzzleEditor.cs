using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UHFPS.Runtime;
using UHFPS.Tools;
using ThunderWire.Editors;

namespace UHFPS.Editors
{
    [CustomEditor(typeof(ElectricalCircuitPuzzle))]
    public class ElectricalCircuitPuzzleEditor : PuzzleEditor<ElectricalCircuitPuzzle>
    {
        public const string ALPHA = "ABCDEFGHIKLMNOPQRSTVXYZ";

        private readonly bool[] foldout = new bool[2];
        private int circuitEditType = 0;

        public override void OnInspectorGUI()
        {
            EditorDrawing.DrawInspectorHeader(new GUIContent("Electrical Circuit Puzzle"), Target);
            EditorGUILayout.Space();

            serializedObject.Update();
            {
                base.OnInspectorGUI();
                EditorGUILayout.Space();

                using (new EditorDrawing.BorderBoxScope(new GUIContent("Circuit Builder")))
                {
                    float circuitPreviewSize = 270f;
                    Rect circuitPreviewRect = GUILayoutUtility.GetRect(circuitPreviewSize, circuitPreviewSize);
                    Rect maskRect = circuitPreviewRect;
                    circuitPreviewRect.y = 0f;
                    circuitPreviewRect.x = (circuitPreviewRect.width / 2) - (circuitPreviewSize / 2);
                    circuitPreviewRect.width = circuitPreviewSize;

                    GUI.BeginGroup(maskRect);
                    DrawCircuitPreview(circuitPreviewRect, Target.Rows, Target.Columns);
                    GUI.EndGroup();

                    EditorGUILayout.Space(2f);

                    Rect circuitEditButtonsRect = GUILayoutUtility.GetRect(1f, 20f);
                    circuitEditButtonsRect.x = (circuitEditButtonsRect.width / 2) - (170f / 2) + 23f;
                    circuitEditButtonsRect.width = 170f;
                    circuitEditType = GUI.Toolbar(circuitEditButtonsRect, circuitEditType, new string[] { "Change", "Rotate", "Clear" });

                    EditorGUILayout.Space(2f);

                    Rect circuitRandomRect = GUILayoutUtility.GetRect(1f, 20f);
                    circuitRandomRect.x = (circuitRandomRect.width / 2) - (100f / 2) + 23f;
                    circuitRandomRect.width = 100f;

                    if (GUI.Button(circuitRandomRect, "Randomize"))
                    {
                        Undo.RegisterFullObjectHierarchyUndo(Target, "Randomize Circuit Puzzle");

                        System.Random random = new System.Random();

                        foreach (var component in Target.ComponentsFlow)
                        {
                            if (component == null)
                                continue;

                            if (Target.CircuitComponents == null || Target.CircuitComponents.Length == 0)
                                continue;

                            component.Component = Target.CircuitComponents[random.Next(0, Target.CircuitComponents.Length)];
                            component.Rotation = random.Next(0, 4) * 90;
                        }

                        EditorUtility.SetDirty(Target);
                        serializedObject.ApplyModifiedProperties();
                    }

                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();
                    {
                        Target.Rows = (ushort)EditorGUILayout.Slider(new GUIContent("Rows"), Target.Rows, 1, 5);
                        Target.Columns = (ushort)EditorGUILayout.Slider(new GUIContent("Columns"), Target.Columns, 1, 5);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RegisterFullObjectHierarchyUndo(Target, "Rows or Columns Change");

                        int size = Target.Rows * Target.Columns;

                        Target.PowerFlow = new ElectricalCircuitPuzzle.PowerComponent[size];
                        Target.ComponentsFlow = new ElectricalCircuitPuzzle.ComponentFlow[size];

                        for (int i = 0; i < size; i++)
                        {
                            Target.PowerFlow[i] = new ElectricalCircuitPuzzle.PowerComponent();
                            Target.ComponentsFlow[i] = new ElectricalCircuitPuzzle.ComponentFlow();
                        }

                        Target.InputEvents.Clear();

                        EditorUtility.SetDirty(Target);
                        serializedObject.ApplyModifiedProperties();
                        return;
                    }

                    EditorGUILayout.HelpBox("Changing the number of rows or columns resets the entire circuit.", MessageType.Warning);

                    DrawPowerFlowSettings();

                    EditorGUILayout.Space();

                    using (new EditorDrawing.BorderBoxScope(new GUIContent("Circuit Settings")))
                    {
                        EditorGUI.indentLevel++;
                        Properties.Draw("CircuitComponents");
                        EditorGUI.indentLevel--;

                        EditorGUILayout.Space();

                        Properties.Draw("ComponentsParent");
                        Properties.Draw("ComponentsSpacing");
                        Properties.Draw("ComponentsSize");

                        if (Properties.DrawGetBool("DisableWhenConnected"))
                            Properties.Draw("PowerConnectedWaitTime");
                    }

                    EditorGUILayout.Space();

                    bool noCircuitComponents = Properties["CircuitComponents"].arraySize == 0;
                    bool noComponentsFlow = Target.ComponentsFlow == null || Target.ComponentsFlow.Length == 0;
                    bool hasEmptySlots = Target.ComponentsFlow == null || Target.ComponentsFlow.Any(x => x == null || x.Component == null);
                    bool missingParent = Target.ComponentsParent == null;

                    using (new EditorGUI.DisabledGroupScope(noCircuitComponents || noComponentsFlow || hasEmptySlots || missingParent))
                    {
                        if (GUILayout.Button("Build Circuit", GUILayout.Height(25)))
                        {
                            BuildCircuit(false);
                        }

                        if (GUILayout.Button("Build Circuit Random", GUILayout.Height(25)))
                        {
                            BuildCircuit(true);
                        }
                    }

                    if (noCircuitComponents)
                        EditorGUILayout.HelpBox("Assign Circuit Components before building.", MessageType.Info);

                    if (missingParent)
                        EditorGUILayout.HelpBox("Assign Components Parent before building.", MessageType.Warning);

                    if (hasEmptySlots)
                        EditorGUILayout.HelpBox("Every grid slot must have a component before building.", MessageType.Warning);
                }

                EditorGUILayout.Space();

                using (new EditorDrawing.BorderBoxScope(new GUIContent("Sounds")))
                {
                    Properties.Draw("RotateComponent");
                    Properties.Draw("PowerConnected");
                    Properties.Draw("PowerDisconnected");
                    Properties.Draw("PuzzleSuccess");
                }

                EditorGUILayout.Space(2f);

                using (new EditorDrawing.BorderBoxScope(new GUIContent("Events")))
                {
                    DrawInputEvents();
                    DrawGlobalEvents();
                    DrawInteractionEvents();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPowerFlowSettings()
        {
            if (Target.PowerFlow == null)
                return;

            var powerFlowArray = Target.PowerFlow
                .Where(x => x != null && x.PowerType != PowerType.None && x.PowerID > 0)
                .OrderBy(x => x.PowerType)
                .ToArray();

            if (powerFlowArray.Length <= 0)
                return;

            EditorGUILayout.Space();

            using (new EditorDrawing.BorderBoxScope(new GUIContent("Power Flow")))
            {
                foreach (var component in powerFlowArray)
                {
                    int alphaPowerID = component.PowerID - 1;
                    string alphaLabel = alphaPowerID >= 0 && alphaPowerID < ALPHA.Length
                        ? ALPHA[alphaPowerID].ToString()
                        : component.PowerID.ToString();

                    if (component.PowerType == PowerType.Output)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.PrefixLabel(
                                $"[{alphaLabel}, {component.PowerDirection}, {component.PowerID}] <b>{component.PowerType}</b>",
                                "Button",
                                EditorDrawing.Styles.RichLabel
                            );

                            IDictionary<string, ElectricalCircuitPuzzle.PowerComponent> contents = Target.PowerFlow
                                .Where(x => x != null && x.PowerType == PowerType.Input && x.PowerID > 0)
                                .ToDictionary(
                                    x =>
                                    {
                                        int id = x.PowerID - 1;
                                        string label = id >= 0 && id < ALPHA.Length ? ALPHA[id].ToString() : x.PowerID.ToString();
                                        return $"[{label}, {x.PowerDirection}] {x.PowerType}";
                                    },
                                    y => y
                                );

                            string selected = contents
                                .Where(x => x.Value.PowerID == component.ConnectPowerID)
                                .Select(x => x.Key)
                                .FirstOrDefault();

                            Rect popupRect = EditorGUILayout.GetControlRect();

                            if (contents.Count > 0)
                            {
                                EditorDrawing.DrawStringSelectPopup(
                                    popupRect,
                                    new GUIContent("Outputs To"),
                                    contents.Keys.ToArray(),
                                    selected,
                                    selection =>
                                    {
                                        component.ConnectPowerID = contents[selection].PowerID;
                                        EditorUtility.SetDirty(Target);
                                        serializedObject.ApplyModifiedProperties();
                                    }
                                );
                            }
                            else
                            {
                                EditorGUI.LabelField(popupRect, "No inputs available.");
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else if (component.PowerType == PowerType.Input)
                    {
                        EditorGUILayout.LabelField(
                            $"[{alphaLabel}, {component.PowerDirection}, {component.PowerID}] <b>{component.PowerType}</b>",
                            EditorDrawing.Styles.RichLabel
                        );
                    }
                }
            }
        }

        private void DrawInputEvents()
        {
            if (Properties["InputEvents"] == null)
                return;

            for (int i = 0; i < Properties["InputEvents"].arraySize; i++)
            {
                SerializedProperty input = Properties["InputEvents"].GetArrayElementAtIndex(i);

                if (i < 0 || i >= Target.InputEvents.Count)
                    continue;

                var inputEvent = Target.InputEvents[i];

                if (inputEvent == null || inputEvent.PowerComponent == null)
                    continue;

                var powerComp = inputEvent.PowerComponent;

                if (powerComp.PowerType == PowerType.Input)
                {
                    SerializedProperty inputLight = input.FindPropertyRelative("InputLight");
                    SerializedProperty onConnected = input.FindPropertyRelative("OnConnected");
                    SerializedProperty onDisconnected = input.FindPropertyRelative("OnDisconnected");

                    int alphaPowerID = powerComp.PowerID - 1;
                    string alphaLabel = alphaPowerID >= 0 && alphaPowerID < ALPHA.Length
                        ? ALPHA[alphaPowerID].ToString()
                        : powerComp.PowerID.ToString();

                    if (EditorDrawing.BeginFoldoutBorderLayout(
                        input,
                        new GUIContent($"[{alphaLabel}, {powerComp.PowerDirection}, {powerComp.PowerID}] Input Events")))
                    {
                        EditorGUILayout.PropertyField(inputLight);
                        EditorGUILayout.Space(1f);

                        if (EditorDrawing.BeginFoldoutBorderLayout(onConnected, new GUIContent("Events")))
                        {
                            EditorGUILayout.PropertyField(onConnected);
                            EditorGUILayout.Space(1f);
                            EditorGUILayout.PropertyField(onDisconnected);
                            EditorDrawing.EndBorderHeaderLayout();
                        }

                        EditorDrawing.EndBorderHeaderLayout();
                    }
                }

                EditorGUILayout.Space(1f);
            }
        }

        private void DrawGlobalEvents()
        {
            if (EditorDrawing.BeginFoldoutBorderLayout(new GUIContent("Global Events"), ref foldout[0]))
            {
                Properties.Draw("OnConnected");
                EditorGUILayout.Space(1f);
                Properties.Draw("OnDisconnected");

                EditorDrawing.EndBorderHeaderLayout();
            }
        }

        private void DrawInteractionEvents()
        {
            if (EditorDrawing.BeginFoldoutBorderLayout(new GUIContent("Interaction Events"), ref foldout[1]))
            {
                Properties.Draw("OnPuzzleInteractionStarted");
                EditorGUILayout.Space(1f);
                Properties.Draw("OnPuzzleInteractionEnded");

                EditorDrawing.EndBorderHeaderLayout();
            }
        }

        private void DrawCircuitPreview(Rect rect, int rows, int columns)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            float spacing = 5f;
            float slotSize = ((rect.width - spacing) / 5f) - spacing;
            float powerSlotSize = slotSize * 0.3f;
            slotSize = ((rect.width - powerSlotSize - spacing * 2) / 5f) - spacing * 2;

            float Y = (rect.height / 2) - (rows * slotSize + spacing * (rows + 1)) / 2;
            float X = (rect.width / 2) - (columns * slotSize + spacing * (columns + 1)) / 2;

            GUI.BeginGroup(rect);

            for (int y = 0; y < rows; y++)
            {
                Vector2 slotPosition = new Vector2(X + spacing, Y + y * slotSize + spacing * (y + 1));

                for (int x = 0; x < columns; x++)
                {
                    Vector2 localSlotPosition = slotPosition + (x * new Vector2(slotSize + spacing, 0));

                    if (y == 0)
                    {
                        Vector2 powerYPos = new Vector2(localSlotPosition.x, localSlotPosition.y - spacing - powerSlotSize);
                        DrawCircuitPower(new Rect(powerYPos, new Vector2(slotSize, powerSlotSize)), x, y, PartDirection.Up);
                    }

                    if (y == rows - 1)
                    {
                        Vector2 powerYPos = new Vector2(localSlotPosition.x, localSlotPosition.y + spacing + slotSize);
                        DrawCircuitPower(new Rect(powerYPos, new Vector2(slotSize, powerSlotSize)), x, y, PartDirection.Down);
                    }

                    if (x == 0)
                    {
                        Vector2 powerXPos = new Vector2(localSlotPosition.x - spacing - powerSlotSize, localSlotPosition.y);
                        DrawCircuitPower(new Rect(powerXPos, new Vector2(powerSlotSize, slotSize)), x, y, PartDirection.Left);
                    }

                    if (x == columns - 1)
                    {
                        Vector2 powerXPos = new Vector2(localSlotPosition.x + spacing + slotSize, localSlotPosition.y);
                        DrawCircuitPower(new Rect(powerXPos, new Vector2(powerSlotSize, slotSize)), x, y, PartDirection.Right);
                    }

                    DrawCircuitSlot(new Rect(localSlotPosition, new Vector2(slotSize, slotSize)), x, y);
                }
            }

            GUI.EndGroup();

            Repaint();
        }

        private void DrawCircuitPower(Rect rect, int x, int y, PartDirection direction)
        {
            if (Target.PowerFlow == null || Target.PowerFlow.Length == 0)
                return;

            int index = y * Target.Columns + x;

            if (index < 0 || index >= Target.PowerFlow.Length)
                return;

            Color rectColor = Color.black.Alpha(0.5f);

            ElectricalCircuitPuzzle.PowerComponent powerComponent = Target.PowerFlow[index];

            if (powerComponent == null)
            {
                powerComponent = new ElectricalCircuitPuzzle.PowerComponent();
                Target.PowerFlow[index] = powerComponent;
            }

            PowerType powerType = powerComponent.PowerType;
            PartDirection powerDirection = powerComponent.PowerDirection;

            if (powerDirection == direction)
            {
                if (powerType == PowerType.Output)
                    rectColor = Color.green.Alpha(0.35f);
                else if (powerType == PowerType.Input)
                    rectColor = Color.red.Alpha(0.35f);
            }

            Event e = Event.current;

            if (rect.Contains(e.mousePosition))
            {
                rectColor = Color.white.Alpha(0.35f);

                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    Undo.RegisterFullObjectHierarchyUndo(Target, "Circuit Power Change");

                    if (powerType == PowerType.Output)
                    {
                        powerComponent.ConnectPowerID = 0;
                    }
                    else if (powerType == PowerType.Input)
                    {
                        foreach (var flow in Target.PowerFlow)
                        {
                            if (flow != null && flow.PowerType == PowerType.Output && flow.ConnectPowerID == powerComponent.PowerID)
                                flow.ConnectPowerID = 0;
                        }
                    }

                    if (powerDirection != direction)
                    {
                        powerType = PowerType.None;
                        powerComponent.PowerID = 0;
                        powerComponent.ConnectPowerID = 0;
                    }

                    int powerTypeEnumCount = Enum.GetValues(typeof(PowerType)).Length;
                    powerComponent.PowerType = (PowerType)(((int)powerType + 1) % powerTypeEnumCount);
                    powerComponent.PowerDirection = direction;

                    if (powerComponent.PowerType != PowerType.None)
                    {
                        for (int i = 0; i < ALPHA.Length; i++)
                        {
                            int id = i + 1;

                            bool idExists = Target.PowerFlow.Any(x =>
                                x != null &&
                                x.PowerType == powerComponent.PowerType &&
                                x.PowerID == id);

                            if (!idExists)
                            {
                                powerComponent.PowerID = id;
                                break;
                            }
                        }
                    }
                    else
                    {
                        powerComponent.PowerID = 0;
                        powerComponent.ConnectPowerID = 0;
                    }

                    if (powerComponent.PowerType == PowerType.Input && !Target.InputEvents.Any(x => x.PowerComponent == powerComponent))
                    {
                        Target.InputEvents.Add(new ElectricalCircuitPuzzle.PowerInputEvents()
                        {
                            PowerComponent = powerComponent
                        });
                    }
                    else if (powerComponent.PowerType == PowerType.None)
                    {
                        Target.InputEvents.RemoveAll(x => x.PowerComponent == null || x.PowerComponent.PowerID == 0);
                    }

                    EditorUtility.SetDirty(Target);
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.UpdateIfRequiredOrScript();
                    e.Use();
                }
            }

            EditorGUI.DrawRect(rect, rectColor);

            if (powerType != PowerType.None && powerDirection == direction)
            {
                string label = powerComponent.PowerID > 0 && powerComponent.PowerID - 1 < ALPHA.Length
                    ? ALPHA[powerComponent.PowerID - 1].ToString()
                    : "-";

                GUI.Label(rect, label, EditorDrawing.CenterStyle(EditorStyles.miniBoldLabel));
            }
        }

        private void DrawCircuitSlot(Rect rect, int x, int y)
        {
            Color rectColor = Color.black.Alpha(0.5f);

            int index = y * Target.Columns + x;

            if (Target.ComponentsFlow == null || Target.ComponentsFlow.Length == 0 || index < 0 || index >= Target.ComponentsFlow.Length)
                return;

            ElectricalCircuitPuzzle.ComponentFlow componentFlow = Target.ComponentsFlow[index];

            if (componentFlow == null)
            {
                componentFlow = new ElectricalCircuitPuzzle.ComponentFlow();
                Target.ComponentsFlow[index] = componentFlow;
            }

            Event e = Event.current;

            if (rect.Contains(e.mousePosition))
            {
                rectColor = Color.white.Alpha(0.35f);

                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    Undo.RegisterFullObjectHierarchyUndo(Target, "Circuit Slot Change");

                    if (circuitEditType == 0)
                    {
                        int componentIndex = Array.IndexOf(Target.CircuitComponents, componentFlow.Component);

                        componentFlow.Component = componentIndex + 1 > Target.CircuitComponents.Length - 1
                            ? null
                            : Target.CircuitComponents[componentIndex + 1];
                    }
                    else if (circuitEditType == 1)
                    {
                        componentFlow.Rotation = (componentFlow.Rotation + 90) % 360;
                    }
                    else if (circuitEditType == 2)
                    {
                        componentFlow.Component = null;
                        componentFlow.Rotation = 0;
                    }

                    EditorUtility.SetDirty(Target);
                    serializedObject.ApplyModifiedProperties();
                    e.Use();
                }
            }

            EditorGUI.DrawRect(rect, rectColor);

            Matrix4x4 matrix = GUI.matrix;

            GUIUtility.RotateAroundPivot(
                componentFlow.Rotation,
                new Vector2(rect.xMin + rect.width * 0.5f, rect.yMin + rect.height * 0.5f)
            );

            if (componentFlow.Component != null && componentFlow.Component.ComponentIcon != null)
                EditorDrawing.DrawTransparentTexture(rect, componentFlow.Component.ComponentIcon);

            GUI.matrix = matrix;
        }

        private void BuildCircuit(bool random)
        {
            if (Target.ComponentsParent == null)
            {
                Debug.LogError("Cannot build circuit. ComponentsParent is missing.", Target);
                return;
            }

            if (Target.CircuitComponents == null || Target.CircuitComponents.Length == 0)
            {
                Debug.LogError("Cannot build circuit. CircuitComponents is empty.", Target);
                return;
            }

            if (Target.ComponentsFlow == null || Target.ComponentsFlow.Length != Target.Rows * Target.Columns)
            {
                Debug.LogError("Cannot build circuit. ComponentsFlow size does not match rows * columns.", Target);
                return;
            }

            if (Target.ComponentsFlow.Any(x => x == null || x.Component == null))
            {
                Debug.LogError("Cannot build circuit. Every slot must have a component assigned.", Target);
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(Target.gameObject, "Build Circuit Puzzle");

            foreach (var component in Target.Components)
            {
                if (component == null)
                    continue;

                if (component.TryGetComponent(out Collider collider))
                    Target.CollidersEnable.Remove(collider);
            }

            foreach (var component in Target.Components.ToArray())
            {
                if (component != null)
                    DestroyImmediate(component.gameObject);
            }

            Target.Components.Clear();

            float componentSize = Target.CircuitComponents[0].ComponentMesh.sharedMesh.bounds.size.x;
            componentSize *= Target.ComponentsSize;

            float panelWidth = componentSize * Target.Columns + Target.ComponentsSpacing * (Target.Columns - 1);
            float panelHeight = componentSize * Target.Rows + Target.ComponentsSpacing * (Target.Rows - 1);

            Vector2 localStart = new Vector2(panelWidth, panelHeight) / 2f;

            System.Random rand = new System.Random();

            for (int i = 0; i < Target.ComponentsFlow.Length; i++)
            {
                var component = Target.ComponentsFlow[i];

                int x = i % Target.Columns;
                int y = i / Target.Columns;

                GameObject componentGO = Instantiate(component.Component.gameObject, Target.ComponentsParent);
                componentGO.name = component.Component.gameObject.name;

                ElectricalCircuitComponent instance = componentGO.GetComponent<ElectricalCircuitComponent>();

                if (instance == null)
                {
                    Debug.LogError($"Component prefab {component.Component.name} has no ElectricalCircuitComponent.", component.Component);
                    DestroyImmediate(componentGO);
                    continue;
                }

                float angle = component.Rotation;

                if (random)
                    angle = rand.Next(0, 4) * 90;

                Vector2 localPos = componentGO.transform.localPosition;

                localPos.x = localStart.x - (x * (componentSize + Target.ComponentsSpacing)) - componentSize / 2f;
                localPos.y = localStart.y - (y * (componentSize + Target.ComponentsSpacing)) - componentSize / 2f;

                Vector3 localRot = componentGO.transform.localEulerAngles;
                localRot = localRot.SetComponent(instance.ComponentUp, angle);

                componentGO.transform.localPosition = localPos;
                componentGO.transform.localEulerAngles = localRot;
                componentGO.transform.localScale = Vector3.one * Target.ComponentsSize;

                instance.ElectricalCircuit = Target;
                instance.Coords = new Vector2Int(x, y);
                instance.Angle = angle;

                Target.Components.Add(instance);

                if (componentGO.TryGetComponent(out Collider collider))
                    Target.CollidersEnable.Add(collider);

                EditorUtility.SetDirty(componentGO);
                EditorUtility.SetDirty(instance);
            }

            EditorUtility.SetDirty(Target);
            serializedObject.ApplyModifiedProperties();

            Debug.Log("Electrical circuit built successfully.", Target);
        }
    }
}