using UHFPS.Runtime;
using UnityEngine;

public class StartObjectiveOnEnter : MonoBehaviour
{
    public ObjectiveTrigger objectiveTrigger;
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        objectiveTrigger.TriggerObjective();
    }
}