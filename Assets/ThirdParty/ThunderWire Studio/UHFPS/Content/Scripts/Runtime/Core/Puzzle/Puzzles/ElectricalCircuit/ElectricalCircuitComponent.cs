using System.Collections.Generic;
using System;
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

        [Header("Flow")]
        public List<FlowDirection> FlowDirections = new();
        public PowerFlow[] PowerFlows;

        private void Awake()
        {
            BuildPowerFlows();

            SnapAngle();

            if (!SaveGameManager.GameWillLoad)
            {
                InitializeDirections();
            }
        }

        private void Start()
        {
            AutoFixCoordsFromPuzzleList();
        }

        private void BuildPowerFlows()
        {
            PowerFlows = new PowerFlow[FlowDirections.Count];

            for (int i = 0; i < FlowDirections.Count; i++)
            {
                FlowDirection direction = FlowDirections[i];

                PowerFlow flow = new PowerFlow
                {
                    FlowDirections = new PartDirection[direction.FlowDirections.Count],
                    FlowRenderer = direction.FlowRenderer,
                    PowerFlows = new List<int>()
                };

                for (int j = 0; j < direction.FlowDirections.Count; j++)
                {
                    flow.FlowDirections[j] = direction.FlowDirections[j];
                }

                PowerFlows[i] = flow;
            }
        }

        public void InitializeDirections()
        {
            SnapAngle();

            int angleTimes = Mathf.RoundToInt(Angle / 90f);
            angleTimes = Mathf.Abs(angleTimes) % 4;

            RotateDirections(angleTimes);
        }

        public void InteractStart()
        {
            if (ElectricalCircuit == null)
                return;

            if (ElectricalCircuit.DisableWhenConnected && ElectricalCircuit.isConnected)
                return;

            Angle = (Angle + 90f) % 360f;
            SnapAngle();

            SetComponentAngle();
            RotateDirections(1);

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

        public void RotateDirections(int times)
        {
            if (PowerFlows == null)
                return;

            times %= 4;

            if (times < 0)
                times += 4;

            foreach (PowerFlow flow in PowerFlows)
            {
                if (flow == null || flow.FlowDirections == null)
                    continue;

                for (int i = 0; i < flow.FlowDirections.Length; i++)
                {
                    flow.FlowDirections[i] = RotatePartDirection(flow.FlowDirections[i], times);
                }
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

        public PowerFlow GetOppositePowerFlow(PartDirection oppositeDir)
        {
            if (PowerFlows == null)
                return null;

            foreach (PowerFlow flow in PowerFlows)
            {
                if (flow == null || flow.FlowDirections == null)
                    continue;

                foreach (PartDirection direction in flow.FlowDirections)
                {
                    if (ElectricalCircuitPuzzle.IsOppositeDirection(direction, oppositeDir))
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

        [ContextMenu("Auto Fix Coords From Puzzle List")]
        public void AutoFixCoordsFromPuzzleList()
        {
            if (ElectricalCircuit == null)
                return;

            if (ElectricalCircuit.Components == null)
                return;

            int index = ElectricalCircuit.Components.IndexOf(this);

            if (index < 0)
            {
                Debug.LogWarning($"{name} is not inside ElectricalCircuit.Components list.", this);
                return;
            }

            if (ElectricalCircuit.Columns <= 0)
                return;

            int x = index % ElectricalCircuit.Columns;
            int y = index / ElectricalCircuit.Columns;

            Vector2Int fixedCoords = new Vector2Int(x, y);

            if (Coords != fixedCoords)
            {
                Debug.LogWarning($"{name} had wrong Coords {Coords}. Fixed to {fixedCoords}.", this);
                Coords = fixedCoords;
            }
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
                    Debug.LogWarning($"Flow {i} is null.", this);
                    continue;
                }

                string dirs = string.Join(", ", flow.FlowDirections);
                string powers = flow.PowerFlows != null ? string.Join(", ", flow.PowerFlows) : "null";

                Debug.Log($"Flow {i}: Directions [{dirs}] | PowerIDs [{powers}]", this);
            }
        }

        public StorableCollection OnCustomSave()
        {
            List<string> partDirections = new();

            if (PowerFlows != null)
            {
                foreach (PowerFlow flow in PowerFlows)
                {
                    string dirCode = "";

                    if (flow != null && flow.FlowDirections != null)
                    {
                        for (int i = 0; i < flow.FlowDirections.Length; i++)
                        {
                            int code = (int)flow.FlowDirections[i];
                            dirCode += code;
                        }
                    }

                    partDirections.Add(dirCode);
                }
            }

            return new StorableCollection()
            {
                { "angle", Angle },
                { "partDirections", partDirections },
                { "coordsX", Coords.x },
                { "coordsY", Coords.y }
            };
        }

        public void OnCustomLoad(JToken data)
        {
            if (data == null)
                return;

            Angle = data["angle"] != null ? (float)data["angle"] : Angle;
            SnapAngle();
            SetComponentAngle();

            if (data["coordsX"] != null && data["coordsY"] != null)
            {
                Coords = new Vector2Int((int)data["coordsX"], (int)data["coordsY"]);
            }
            else
            {
                AutoFixCoordsFromPuzzleList();
            }

            JToken directionsToken = data["partDirections"];

            if (directionsToken == null)
                return;

            string[] partDirections = directionsToken.ToObject<string[]>();

            if (PowerFlows == null)
                BuildPowerFlows();

            if (partDirections.Length == PowerFlows.Length)
            {
                for (int i = 0; i < partDirections.Length; i++)
                {
                    PowerFlow flow = PowerFlows[i];

                    if (flow == null || flow.FlowDirections == null)
                        continue;

                    for (int j = 0; j < flow.FlowDirections.Length && j < partDirections[i].Length; j++)
                    {
                        int code = int.Parse(partDirections[i][j].ToString());
                        flow.FlowDirections[j] = (PartDirection)code;
                    }
                }
            }
            else
            {
                Debug.LogError("Saved 'partDirections' length does not match 'PowerFlows' length!", this);
            }
        }
    }
}