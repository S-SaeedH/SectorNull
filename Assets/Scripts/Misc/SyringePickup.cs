using UnityEngine;
using UHFPS.Runtime;

public class SyringePickup : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public int healAmount = 50;

    public void PickupSyringe()
    {
        if (playerHealth == null)
            return;

        int newMaxHealth = (int)playerHealth.MaxHealth + healAmount;
        int newCurrentHealth = playerHealth.EntityHealth + healAmount;

        newCurrentHealth = Mathf.Clamp(newCurrentHealth, 0, newMaxHealth);

        playerHealth.MaxHealth = (uint)newMaxHealth;
        playerHealth.InitializeHealth(newCurrentHealth, newMaxHealth);

        gameObject.SetActive(false);
    }
}