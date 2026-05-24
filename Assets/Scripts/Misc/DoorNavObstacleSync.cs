using UnityEngine;
using UnityEngine.AI;

public class DoorNavObstacleSync : MonoBehaviour
{
    [Header("Blocker References")]
    [SerializeField] private Collider doorBlockerCollider;
    [SerializeField] private NavMeshObstacle navMeshObstacle;

    private void Awake()
    {
        if (doorBlockerCollider == null)
            doorBlockerCollider = GetComponent<Collider>();

        if (navMeshObstacle == null)
            navMeshObstacle = GetComponent<NavMeshObstacle>();
    }

    public void SetDoorOpen()
    {
        SetBlocked(false);
    }

    public void SetDoorClosed()
    {
        SetBlocked(true);
    }

    private void SetBlocked(bool blocked)
    {
        /*if (doorBlockerCollider != null)
            doorBlockerCollider.enabled = blocked;*/

        if (navMeshObstacle != null)
            navMeshObstacle.enabled = blocked;
    }
}