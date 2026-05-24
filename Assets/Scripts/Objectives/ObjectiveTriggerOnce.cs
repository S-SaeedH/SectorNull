using UnityEngine;
using UHFPS.Runtime;

public class ObjectiveTriggerOnce : MonoBehaviour
{
    public ObjectiveTrigger objectiveTrigger;
    private bool hasTriggered = false;

    public void TriggerOnce()
    {
        if (hasTriggered) return;

        hasTriggered = true;

        if (objectiveTrigger != null)
            objectiveTrigger.TriggerObjective();
    }
}