using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;
using UHFPS.Tools;

namespace UHFPS.Runtime
{
    public enum PowerType { None, Output, Input }
    public enum PartDirection { Up, Down, Left, Right }

    [RequireComponent(typeof(AudioSource))]
    public class ElectricalCircuitPuzzle : PuzzleBase, ISaveable
    {
        [Serializable]
        public sealed class PowerComponent
        {
            public PowerType PowerType;
            public PartDirection PowerDirection;
            public int PowerID;
            public int ConnectPowerID;
        }

        [Serializable]
        public sealed class ComponentFlow
        {
            public ElectricalCircuitComponent Component;
            public int Rotation;
        }

        [Serializable]
        public sealed class PowerInputEvents
        {
            public PowerComponent PowerComponent;
            public ElectricalCircuitLights InputLight;

            public UnityEvent<int> OnConnected;
            public UnityEvent<int> OnDisconnected;
        }

        [Header("Grid")]
        public ushort Rows;
        public ushort Columns;

        [Header("Power Flow")]
        public PowerComponent[] PowerFlow;
        public ElectricalCircuitComponent[] CircuitComponents;

        [Header("Components")]
        public Transform ComponentsParent;
        public float ComponentsSpacing = 0f;
        public float ComponentsSize = 1f;

        [Header("Connection")]
        public bool DisableWhenConnected = true;
        public float PowerConnectedWaitTime = 1f;

        public ComponentFlow[] ComponentsFlow;
        public List<ElectricalCircuitComponent> Components = new();
        public List<PowerInputEvents> InputEvents = new();

        [Header("Sounds")]
        public SoundClip RotateComponent;
        public SoundClip PowerConnected;
        public SoundClip PowerDisconnected;

        [Header("Events")]
        public UnityEvent OnConnected;
        public UnityEvent OnDisconnected;

        public bool isConnected;

        private AudioSource audioSource;
        private Coroutine connectedCoroutine;

        private void OnValidate()
        {
            int size = Rows * Columns;

            if (size <= 0)
                return;

            ResizePowerFlow(size);
            ResizeComponentsFlow(size);
        }

        private void ResizePowerFlow(int size)
        {
            if (PowerFlow != null && PowerFlow.Length == size)
            {
                for (int i = 0; i < PowerFlow.Length; i++)
                {
                    if (PowerFlow[i] == null)
                        PowerFlow[i] = new PowerComponent();
                }

                return;
            }

            PowerComponent[] oldPowerFlow = PowerFlow;
            PowerFlow = new PowerComponent[size];

            for (int i = 0; i < size; i++)
            {
                if (oldPowerFlow != null && i < oldPowerFlow.Length && oldPowerFlow[i] != null)
                    PowerFlow[i] = oldPowerFlow[i];
                else
                    PowerFlow[i] = new PowerComponent();
            }
        }

        private void ResizeComponentsFlow(int size)
        {
            if (ComponentsFlow != null && ComponentsFlow.Length == size)
            {
                for (int i = 0; i < ComponentsFlow.Length; i++)
                {
                    if (ComponentsFlow[i] == null)
                        ComponentsFlow[i] = new ComponentFlow();
                }

                return;
            }

            ComponentFlow[] oldComponentsFlow = ComponentsFlow;
            ComponentsFlow = new ComponentFlow[size];

            for (int i = 0; i < size; i++)
            {
                if (oldComponentsFlow != null && i < oldComponentsFlow.Length && oldComponentsFlow[i] != null)
                    ComponentsFlow[i] = oldComponentsFlow[i];
                else
                    ComponentsFlow[i] = new ComponentFlow();
            }
        }

        public override void Awake()
        {
            base.Awake();
            audioSource = GetComponent<AudioSource>();

            SyncComponentCoords();
        }

        private void Start()
        {
            if (!IsSetupValid())
                return;

            SyncComponentCoords();

            if (!SaveGameManager.GameWillLoad)
            {
                RecalculateCircuit(false);
            }
        }

        public void ReinitializeCircuit()
        {
            if (DisableWhenConnected && isConnected)
                return;

            if (!IsSetupValid())
                return;

            RecalculateCircuit(true);
        }

        private void RecalculateCircuit(bool playRotateSound)
        {
            SyncComponentCoords();

            RemoveAllPowerIDs();
            ResetAllFlowVisuals();

            PowerAllOutputs();
            CheckPowerStates();
            CheckAllInputs();

            if (playRotateSound && audioSource != null && RotateComponent != null)
                audioSource.PlayOneShotSoundClip(RotateComponent);
        }

        private void SyncComponentCoords()
        {
            if (Components == null)
                return;

            for (int i = 0; i < Components.Count; i++)
            {
                ElectricalCircuitComponent component = Components[i];

                if (component == null)
                    continue;

                int x = i % Columns;
                int y = i / Columns;

                component.Coords = new Vector2Int(x, y);
                component.ElectricalCircuit = this;
            }
        }

        public void PowerAllOutputs()
        {
            if (!IsSetupValid())
                return;

            SyncComponentCoords();

            for (int i = 0; i < PowerFlow.Length; i++)
            {
                PowerComponent powerComponent = PowerFlow[i];

                if (powerComponent == null)
                    continue;

                if (powerComponent.PowerType != PowerType.Output)
                    continue;

                ElectricalCircuitComponent component = Components[i];

                if (component == null)
                {
                    Debug.LogError($"Missing circuit component at index {i}.", this);
                    continue;
                }

                PartDirection fromDirection = ToOppositeDirection(powerComponent.PowerDirection);
                component.SetPowerFlow(fromDirection, powerComponent.PowerID, null);
            }
        }

        public void CheckAllInputs()
        {
            if (!IsSetupValid())
                return;

            Dictionary<PowerComponent, ElectricalCircuitComponent> inputs = new();
            Dictionary<int, List<int>> outputPairs = new();

            int inputsCount = 0;

            for (int i = 0; i < PowerFlow.Length; i++)
            {
                PowerComponent powerComponent = PowerFlow[i];

                if (powerComponent == null)
                    continue;

                if (powerComponent.PowerType == PowerType.Output)
                {
                    if (!outputPairs.ContainsKey(powerComponent.ConnectPowerID))
                        outputPairs[powerComponent.ConnectPowerID] = new List<int>();

                    outputPairs[powerComponent.ConnectPowerID].Add(powerComponent.PowerID);
                }
                else if (powerComponent.PowerType == PowerType.Input)
                {
                    ElectricalCircuitComponent component = Components[i];

                    if (component == null)
                    {
                        Debug.LogError($"Input power component at index {i} has no matching circuit component.", this);
                        continue;
                    }

                    inputs[powerComponent] = component;
                    inputsCount++;
                }
            }

            int connectedInputs = 0;

            foreach (var input in inputs)
            {
                PowerInputEvents events = InputEvents.FirstOrDefault(x =>
                {
                    if (x == null || x.PowerComponent == null)
                        return false;

                    PowerComponent powerComponent = x.PowerComponent;

                    return powerComponent.PowerType == PowerType.Input
                        && powerComponent.PowerID == input.Key.PowerID;
                });

                if (events == null)
                {
                    Debug.LogWarning($"No InputEvents entry found for input PowerID {input.Key.PowerID}.", this);
                    continue;
                }

                PartDirection oppositeDirection = ToOppositeDirection(input.Key.PowerDirection);
                ElectricalCircuitComponent.PowerFlow oppositeFlow = input.Value.GetOppositePowerFlow(oppositeDirection);

                bool hasRequiredConnections = outputPairs.TryGetValue(input.Key.PowerID, out List<int> requiredConnections);

                if (!hasRequiredConnections || requiredConnections == null || requiredConnections.Count == 0)
                {
                    if (events.InputLight != null)
                        events.InputLight.OnDisconnected(input.Key.PowerID);

                    events.OnDisconnected?.Invoke(input.Key.PowerID);
                    continue;
                }

                int connected = 0;

                foreach (int connection in requiredConnections)
                {
                    bool validConnection = oppositeFlow != null
                        && oppositeFlow.PowerFlows != null
                        && oppositeFlow.PowerFlows.Contains(connection);

                    if (validConnection)
                    {
                        if (events.InputLight != null)
                            events.InputLight.OnConnected(connection);

                        events.OnConnected?.Invoke(connection);
                        connected++;
                    }
                    else
                    {
                        if (events.InputLight != null)
                            events.InputLight.OnDisconnected(connection);

                        events.OnDisconnected?.Invoke(connection);
                    }
                }

                if (connected == requiredConnections.Count)
                    connectedInputs++;
            }

            bool allInputsConnected = inputsCount > 0 && connectedInputs == inputsCount;

            if (allInputsConnected)
            {
                if (!isConnected && !SaveGameManager.GameWillLoad)
                {
                    if (audioSource != null && PowerConnected != null)
                        audioSource.PlayOneShotSoundClip(PowerConnected);
                }

                if (DisableWhenConnected)
                {
                    canManuallySwitch = false;

                    if (connectedCoroutine != null)
                        StopCoroutine(connectedCoroutine);

                    if (isActive)
                        connectedCoroutine = StartCoroutine(OnPowerConnected());
                    else
                        DisableInteract();
                }

                if (!isConnected)
                    OnConnected?.Invoke();

                isConnected = true;
            }
            else
            {
                if (isConnected)
                {
                    if (!SaveGameManager.GameWillLoad)
                    {
                        if (audioSource != null && PowerDisconnected != null)
                            audioSource.PlayOneShotSoundClip(PowerDisconnected);
                    }

                    OnDisconnected?.Invoke();
                    isConnected = false;
                }
            }
        }

        private IEnumerator OnPowerConnected()
        {
            yield return new WaitForSeconds(PowerConnectedWaitTime);

            SwitchBack();
            DisableInteract();
        }

        public void RemoveAllPowerIDs()
        {
            if (Components == null)
                return;

            foreach (ElectricalCircuitComponent component in Components)
            {
                if (component == null || component.PowerFlows == null)
                    continue;

                foreach (ElectricalCircuitComponent.PowerFlow flow in component.PowerFlows)
                {
                    if (flow == null || flow.PowerFlows == null)
                        continue;

                    flow.PowerFlows.Clear();
                }
            }
        }

        public void ResetAllFlowVisuals()
        {
            if (Components == null)
                return;

            foreach (ElectricalCircuitComponent component in Components)
            {
                if (component == null || component.PowerFlows == null)
                    continue;

                foreach (ElectricalCircuitComponent.PowerFlow flow in component.PowerFlows)
                {
                    if (flow == null)
                        continue;

                    component.SetFlowState(flow, false);
                }
            }
        }

        public void CheckPowerStates()
        {
            if (Components == null)
                return;

            foreach (ElectricalCircuitComponent component in Components)
            {
                if (component == null || component.PowerFlows == null)
                    continue;

                foreach (ElectricalCircuitComponent.PowerFlow flow in component.PowerFlows)
                {
                    if (flow == null || flow.PowerFlows == null)
                        continue;

                    if (!flow.PowerFlows.Any())
                        component.SetFlowState(flow, false);
                }
            }
        }

        public int CoordsToIndex(Vector2Int coords)
        {
            return coords.y * Columns + coords.x;
        }

        public bool IsCoordsValid(Vector2Int coords)
        {
            return coords.x >= 0 && coords.x < Columns
                && coords.y >= 0 && coords.y < Rows;
        }

        public static Vector2Int DirectionToVector(PartDirection direction)
        {
            return direction switch
            {
                PartDirection.Up => new Vector2Int(0, -1),
                PartDirection.Down => new Vector2Int(0, 1),
                PartDirection.Left => new Vector2Int(-1, 0),
                PartDirection.Right => new Vector2Int(1, 0),
                _ => Vector2Int.zero,
            };
        }

        public static bool IsOppositeDirection(PartDirection lhs, PartDirection rhs)
        {
            if (lhs == PartDirection.Up && rhs == PartDirection.Down) return true;
            if (lhs == PartDirection.Down && rhs == PartDirection.Up) return true;
            if (lhs == PartDirection.Left && rhs == PartDirection.Right) return true;
            if (lhs == PartDirection.Right && rhs == PartDirection.Left) return true;

            return false;
        }

        public static PartDirection ToOppositeDirection(PartDirection direction)
        {
            return direction switch
            {
                PartDirection.Up => PartDirection.Down,
                PartDirection.Down => PartDirection.Up,
                PartDirection.Left => PartDirection.Right,
                PartDirection.Right => PartDirection.Left,
                _ => direction
            };
        }

        private bool IsSetupValid()
        {
            int expectedSize = Rows * Columns;

            if (expectedSize <= 0)
            {
                Debug.LogError("Rows and Columns must be greater than 0.", this);
                return false;
            }

            if (PowerFlow == null)
            {
                Debug.LogError("PowerFlow is null.", this);
                return false;
            }

            if (Components == null)
            {
                Debug.LogError("Components list is null.", this);
                return false;
            }

            if (PowerFlow.Length != expectedSize)
            {
                Debug.LogError($"PowerFlow size is wrong. Expected {expectedSize}, got {PowerFlow.Length}.", this);
                return false;
            }

            if (Components.Count != expectedSize)
            {
                Debug.LogError($"Components count is wrong. Expected {expectedSize}, got {Components.Count}.", this);
                return false;
            }

            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] == null)
                {
                    Debug.LogError($"Components[{i}] is null.", this);
                    return false;
                }
            }

            return true;
        }

        [ContextMenu("Validate Circuit Setup")]
        public void ValidateCircuitSetup()
        {
            int expectedSize = Rows * Columns;

            Debug.Log($"Circuit validation started. Rows: {Rows}, Columns: {Columns}, Expected Size: {expectedSize}", this);

            if (PowerFlow == null)
            {
                Debug.LogError("PowerFlow is null.", this);
                return;
            }

            if (Components == null)
            {
                Debug.LogError("Components list is null.", this);
                return;
            }

            if (PowerFlow.Length != expectedSize)
                Debug.LogError($"PowerFlow size is wrong. Expected {expectedSize}, got {PowerFlow.Length}.", this);
            else
                Debug.Log($"PowerFlow size is correct: {PowerFlow.Length}", this);

            if (Components.Count != expectedSize)
                Debug.LogError($"Components count is wrong. Expected {expectedSize}, got {Components.Count}.", this);
            else
                Debug.Log($"Components count is correct: {Components.Count}", this);

            SyncComponentCoords();

            for (int i = 0; i < Components.Count; i++)
            {
                ElectricalCircuitComponent component = Components[i];

                int x = i % Columns;
                int y = i / Columns;

                if (component == null)
                {
                    Debug.LogError($"Index {i} = Grid({x},{y}) = NULL", this);
                    continue;
                }

                Debug.Log($"Index {i} = Grid({x},{y}) = {component.name} | Coords: {component.Coords}", component);
            }

            var duplicateComponents = Components
                .Where(x => x != null)
                .GroupBy(x => x)
                .Where(g => g.Count() > 1);

            foreach (var duplicate in duplicateComponents)
            {
                Debug.LogError($"Duplicate component reference found: {duplicate.Key.name}", duplicate.Key);
            }

            Dictionary<Vector2Int, List<ElectricalCircuitComponent>> coordMap = new();

            foreach (ElectricalCircuitComponent component in Components)
            {
                if (component == null)
                    continue;

                if (!coordMap.ContainsKey(component.Coords))
                    coordMap[component.Coords] = new List<ElectricalCircuitComponent>();

                coordMap[component.Coords].Add(component);
            }

            foreach (var pair in coordMap)
            {
                if (pair.Value.Count > 1)
                {
                    string names = string.Join(", ", pair.Value.Select(x => x.name));
                    Debug.LogError($"Duplicate Coords found at {pair.Key}: {names}", this);
                }
            }

            Debug.Log("Circuit validation finished.", this);
        }

        public StorableCollection OnSave()
        {
            StorableCollection saveableBuffer = new();

            if (Components == null)
                return saveableBuffer;

            SyncComponentCoords();

            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] == null)
                    continue;

                saveableBuffer.Add("component_" + i, Components[i].OnCustomSave());
            }

            return saveableBuffer;
        }

        public void OnLoad(JToken data)
        {
            if (Components == null)
                return;

            SyncComponentCoords();

            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] == null)
                    continue;

                JToken componentData = data?["component_" + i];

                if (componentData != null)
                    Components[i].OnCustomLoad(componentData);
            }

            SyncComponentCoords();

            RemoveAllPowerIDs();
            ResetAllFlowVisuals();

            PowerAllOutputs();
            CheckPowerStates();
            CheckAllInputs();
        }
    }
}