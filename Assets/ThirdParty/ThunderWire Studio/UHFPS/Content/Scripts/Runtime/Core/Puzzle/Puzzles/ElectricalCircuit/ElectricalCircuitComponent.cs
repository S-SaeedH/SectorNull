using System;
using System.Collections.Generic;
using UnityEngine;
using UHFPS.Tools;
using Newtonsoft.Json.Linq;

namespace UHFPS.Runtime
{
    public class ElectricalCircuitComponent : MonoBehaviour, IInteractStart, ISaveableCustom
    {
        [Serializable]
        public sealed class FlowDirection
        {
            public List<PartDirection> FlowDirections = new();
            public RendererMaterial[] FlowRenderer;
        }

        [Serializable]
        public sealed class PowerFlow
        {
            public PartDirection[] FlowDirections;
            public RendererMaterial[] FlowRenderer;
            public List<int> PowerFlows = new();
        }

        [Header("References")]
        public ElectricalCircuitPuzzle ElectricalCircuit;

        [Header("Visual")]
        public Texture2D ComponentIcon;
        public MeshFilter ComponentMesh;
        public Axis ComponentUp;

        [Header("Grid")]
        public Vector2Int Coords;

        [Header("Rotation")]
        public float Angle;

        [Header("Flow Setup")]
        public List<FlowDirection> FlowDirections = new();

        [Header("Runtime Flow")]
        public PowerFlow[] PowerFlows;

        private void Awake()
        {
            SnapAngle();
            SetComponentAngle();
            RebuildRuntimeFlowsFromAngle();
        }

        public void InteractStart()
        {
            if (ElectricalCircuit == null)
                return;

            if (ElectricalCircuit.DisableWhenConnected && ElectricalCircuit.isConnected)
                return;

            Angle += 90f;
            SnapAngle();

            SetComponentAngle();
            RebuildRuntimeFlowsFromAngle();

            ElectricalCircuit.ReinitializeCircuit();
        }

        public void SetComponentAngle()
        {
            SnapAngle();

            Vector3 newRotation = transform.localEulerAngles.SetComponent(ComponentUp, Angle);
            transform.localEulerAngles = newRotation;
        }

        private void SnapAngle()
        {
            Angle = Mathf.Round(Angle / 90f) * 90f;
            Angle %= 360f;

            if (Angle < 0f)
                Angle += 360f;
        }

        public void RebuildRuntimeFlowsFromAngle()
        {
            SnapAngle();

            int rotateTimes = Mathf.RoundToInt(Angle / 90f) % 4;

            if (rotateTimes < 0)
                rotateTimes += 4;

            PowerFlows = new PowerFlow[FlowDirections.Count];

            for (int i = 0; i < FlowDirections.Count; i++)
            {
                FlowDirection sourceFlow = FlowDirections[i];

                PowerFlow runtimeFlow = new PowerFlow
                {
                    FlowDirections = new PartDirection[sourceFlow.FlowDirections.Count],
                    FlowRenderer = sourceFlow.FlowRenderer,
                    PowerFlows = new List<int>()
                };

                for (int j = 0; j < sourceFlow.FlowDirections.Count; j++)
                {
                    runtimeFlow.FlowDirections[j] = RotatePartDirection(sourceFlow.FlowDirections[j], rotateTimes);
                }

                PowerFlows[i] = runtimeFlow;
            }
        }

        public void ClearPower()
        {
            if (PowerFlows == null)
                return;

            foreach (PowerFlow flow in PowerFlows)
            {
                if (flow == null)
                    continue;

                flow.PowerFlows?.Clear();
                SetFlowState(flow, false);
            }
        }

        public void SetPowerFlow(PartDirection fromDirection, int powerID, List<PowerFlow> visited)
        {
            if (ElectricalCircuit == null)
                return;

            PowerFlow inputFlow = GetOppositePowerFlow(fromDirection);

            if (inputFlow == null)
                return;

            if (visited == null)
                visited = new List<PowerFlow>();

            if (visited.Contains(inputFlow))
                return;

            visited.Add(inputFlow);

            if (!inputFlow.PowerFlows.Contains(powerID))
                inputFlow.PowerFlows.Add(powerID);

            SetFlowState(inputFlow, true);

            foreach (PartDirection direction in inputFlow.FlowDirections)
            {
                if (ElectricalCircuitPuzzle.IsOppositeDirection(fromDirection, direction))
                    continue;

                if (!GetDirectionComponent(direction, out ElectricalCircuitComponent nextComponent))
                    continue;

                if (nextComponent == null)
                    continue;

                PowerFlow nextInputFlow = nextComponent.GetOppositePowerFlow(direction);

                if (nextInputFlow == null)
                    continue;

                nextComponent.SetPowerFlow(direction, powerID, visited);
            }
        }

        public void SetFlowState(PowerFlow powerFlow, bool state)
        {
            if (powerFlow == null || powerFlow.FlowRenderer == null)
                return;

            foreach (RendererMaterial renderer in powerFlow.FlowRenderer)
            {
                if (!renderer.IsAssigned)
                    continue;

                if (state)
                    renderer.ClonedMaterial.EnableKeyword("_EMISSION");
                else
                    renderer.ClonedMaterial.DisableKeyword("_EMISSION");
            }
        }

        public bool GetDirectionComponent(PartDirection direction, out ElectricalCircuitComponent component)
        {
            component = null;

            if (ElectricalCircuit == null)
                return false;

            Vector2Int dirOutput = ElectricalCircuitPuzzle.DirectionToVector(direction);
            Vector2Int newCoords = Coords + dirOutput;

            if (!ElectricalCircuit.IsCoordsValid(newCoords))
                return false;

            int compIndex = ElectricalCircuit.CoordsToIndex(newCoords);

            if (ElectricalCircuit.Components == null)
                return false;

            if (compIndex < 0 || compIndex >= ElectricalCircuit.Components.Count)
                return false;

            component = ElectricalCircuit.Components[compIndex];

            return component != null;
        }

        public PowerFlow GetOppositePowerFlow(PartDirection incomingDirection)
        {
            if (PowerFlows == null)
                return null;

            foreach (PowerFlow flow in PowerFlows)
            {
                if (flow == null || flow.FlowDirections == null)
                    continue;

                foreach (PartDirection direction in flow.FlowDirections)
                {
                    if (ElectricalCircuitPuzzle.IsOppositeDirection(direction, incomingDirection))
                        return flow;
                }
            }

            return null;
        }

        private PartDirection RotatePartDirection(PartDirection direction, int times)
        {
            times %= 4;

            for (int i = 0; i < times; i++)
            {
                direction = direction switch
                {
                    PartDirection.Up => PartDirection.Right,
                    PartDirection.Right => PartDirection.Down,
                    PartDirection.Down => PartDirection.Left,
                    PartDirection.Left => PartDirection.Up,
                    _ => direction
                };
            }

            return direction;
        }

        [ContextMenu("Debug Flow Directions")]
        public void DebugFlowDirections()
        {
            Debug.Log($"--- Flow Debug: {name} | Coords: {Coords} | Angle: {Angle} ---", this);

            if (PowerFlows == null)
            {
                Debug.LogWarning("PowerFlows is null.", this);
                return;
            }

            for (int i = 0; i < PowerFlows.Length; i++)
            {
                PowerFlow flow = PowerFlows[i];

                if (flow == null || flow.FlowDirections == null)
                {
                    Debug.LogWarning($"Runtime Flow {i} is null.", this);
                    continue;
                }

                string dirs = string.Join(", ", flow.FlowDirections);
                string powers = flow.PowerFlows != null ? string.Join(", ", flow.PowerFlows) : "null";

                Debug.Log($"Runtime Flow {i}: Directions [{dirs}] | PowerIDs [{powers}]", this);
            }
        }

        [ContextMenu("Debug Inspector Flow Setup")]
        public void DebugInspectorFlowSetup()
        {
            Debug.Log($"--- Inspector Flow Setup: {name} | Angle: {Angle} ---", this);

            if (FlowDirections == null)
            {
                Debug.LogWarning("FlowDirections list is null.", this);
                return;
            }

            for (int i = 0; i < FlowDirections.Count; i++)
            {
                FlowDirection flow = FlowDirections[i];

                if (flow == null || flow.FlowDirections == null)
                {
                    Debug.LogWarning($"Inspector Flow {i} is null.", this);
                    continue;
                }

                string dirs = string.Join(", ", flow.FlowDirections);
                Debug.Log($"Inspector Flow {i}: Base Directions [{dirs}]", this);
            }
        }

        public StorableCollection OnCustomSave()
        {
            SnapAngle();

            return new StorableCollection()
            {
                { "angle", Angle }
            };
        }

        public void OnCustomLoad(JToken data)
        {
            if (data == null)
                return;

            Angle = data["angle"] != null ? (float)data["angle"] : Angle;

            SnapAngle();
            SetComponentAngle();
            RebuildRuntimeFlowsFromAngle();
        }
    }
}