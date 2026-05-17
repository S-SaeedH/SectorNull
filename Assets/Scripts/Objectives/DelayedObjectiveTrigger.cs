using System.Collections;
using UnityEngine;
using UHFPS.Runtime;

public class DelayedObjectiveTrigger : MonoBehaviour
{
    public ObjectiveTrigger objectiveTrigger;
    public float delay = 2.5f;

    public void TriggerWithDelay()
    {
        StartCoroutine(TriggerAfterDelay());
    }

    private IEnumerator TriggerAfterDelay()
    {
        yield return new WaitForSeconds(delay);

        if (objectiveTrigger != null)
            objectiveTrigger.TriggerObjective();
    }
}