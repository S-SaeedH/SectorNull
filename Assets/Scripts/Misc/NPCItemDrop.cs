using UnityEngine;

public class NPCItemDrop : MonoBehaviour
{
    [Header("Drop Settings")]
    public GameObject[] dropPrefabs;
    
    public void onNPCDeath() {
        foreach (var prefab in dropPrefabs) {
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            Instantiate(prefab, spawnPosition, Quaternion.identity);
        }
    }
}
