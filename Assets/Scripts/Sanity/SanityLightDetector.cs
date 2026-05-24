using UnityEngine;

public class SanityLightDetector : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How often the player checks if they are in darkness.")]
    public float checkInterval = 0.25f;

    [Tooltip("Maximum distance to check nearby lights.")]
    public float detectionRadius = 12f;

    [Tooltip("Minimum light amount required to stop sanity decay.")]
    public float lightThreshold = 0.35f;

    [Header("Darkness Decay")]
    [Tooltip("Sanity lost per second when the player is in darkness.")]
    public float darkDecayRate = 2f;

    [Header("Light Blocking")]
    [Tooltip("Layers that block light between the player and a light source. Usually walls, ground, environment.")]
    public LayerMask lightBlockerMask;

    [Header("Flashlight Support")]
    [Tooltip("Assign the player's flashlight Light component here.")]
    public Light playerFlashlight;

    [Tooltip("If true, the flashlight can stop sanity decay.")]
    public bool flashlightProtectsSanity = true;

    [Tooltip("How much protection the flashlight gives while turned on.")]
    public float flashlightProtectionAmount = 1f;

    [Tooltip("If true, flashlight only protects sanity when it is actually pointing forward into open space.")]
    public bool requireFlashlightForwardCheck = false;

    [Tooltip("Distance checked in front of the flashlight when forward check is enabled.")]
    public float flashlightForwardCheckDistance = 3f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private float _checkTimer;
    private bool _isCurrentlyDark;

    private void Update()
    {
        _checkTimer += Time.deltaTime;

        if (_checkTimer >= checkInterval)
        {
            _checkTimer = 0f;
            CheckLightLevel();
        }
    }

    private void CheckLightLevel()
    {
        float totalLight = CalculateNearbyLightAmount();

        if (FlashlightIsProtecting())
        {
            totalLight += flashlightProtectionAmount;
        }

        bool isDark = totalLight < lightThreshold;

        if (SanityManager.Instance == null)
            return;

        if (isDark)
        {
            SanityManager.Instance.EnterDarkArea(darkDecayRate);
        }
        else
        {
            SanityManager.Instance.ExitDarkArea();
        }

        if (_isCurrentlyDark != isDark)
        {
            _isCurrentlyDark = isDark;

            if (showDebugLogs)
            {
                Debug.Log(isDark
                    ? $"SanityLightDetector: DARK. Light amount = {totalLight}"
                    : $"SanityLightDetector: LIT. Light amount = {totalLight}");
            }
        }
    }

    private float CalculateNearbyLightAmount()
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        float totalLight = 0f;
        Vector3 playerPosition = transform.position + Vector3.up * 0.5f;

        foreach (Light light in allLights)
        {
            if (light == null)
                continue;

            if (!light.enabled || !light.gameObject.activeInHierarchy)
                continue;

            if (light == playerFlashlight)
                continue;

            if (light.type == LightType.Directional)
            {
                totalLight += light.intensity;
                continue;
            }

            float distance = Vector3.Distance(playerPosition, light.transform.position);

            if (distance > detectionRadius)
                continue;

            if (distance > light.range)
                continue;

            if (IsLightBlocked(playerPosition, light.transform.position, distance))
                continue;

            float distanceFactor = 1f - Mathf.Clamp01(distance / light.range);
            float contribution = light.intensity * distanceFactor;

            totalLight += contribution;
        }

        return totalLight;
    }

    private bool IsLightBlocked(Vector3 fromPosition, Vector3 lightPosition, float distance)
    {
        Vector3 directionToLight = (lightPosition - fromPosition).normalized;

        return Physics.Raycast(
            fromPosition,
            directionToLight,
            distance,
            lightBlockerMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private bool FlashlightIsProtecting()
    {
        if (!flashlightProtectsSanity)
            return false;

        if (playerFlashlight == null)
            return false;

        if (!playerFlashlight.enabled)
            return false;

        if (!playerFlashlight.gameObject.activeInHierarchy)
            return false;

        if (requireFlashlightForwardCheck)
        {
            Vector3 origin = playerFlashlight.transform.position;
            Vector3 direction = playerFlashlight.transform.forward;

            bool blockedImmediately = Physics.Raycast(
                origin,
                direction,
                flashlightForwardCheckDistance,
                lightBlockerMask,
                QueryTriggerInteraction.Ignore
            );

            if (blockedImmediately)
                return false;
        }

        return true;
    }

    public bool IsCurrentlyDark()
    {
        return _isCurrentlyDark;
    }
}