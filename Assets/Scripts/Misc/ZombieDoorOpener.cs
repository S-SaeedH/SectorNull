using UnityEngine;

public class ZombieDoorOpener : MonoBehaviour
{
    [Header("Zombie Detection")]
    [SerializeField] private string zombieTag = "Zombie";

    [Header("Door")]
    [SerializeField] private DoorNavObstacleSync doorBlockerSync;

    [Header("Optional")]
    [SerializeField] private float closeDelay = 3f;

    private bool opened;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(zombieTag)) return;
        if (opened) return;

        opened = true;

        // Opens path for zombie
        if (doorBlockerSync != null)
            doorBlockerSync.SetDoorOpen();

        // Optional: close later
        Invoke(nameof(CloseDoor), closeDelay);
    }

    private void CloseDoor()
    {
        opened = false;

        if (doorBlockerSync != null)
            doorBlockerSync.SetDoorClosed();
    }
}