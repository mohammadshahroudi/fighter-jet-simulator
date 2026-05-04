using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunLogic gunLogic;
    [SerializeField] private SpriteRenderer crosshairSprite;

    [Header("Crosshair Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color targetAcquiredColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float colorTransitionSpeed = 10f;

    [Header("Targeting Check")]
    [SerializeField] private float targetCheckInterval = 0.05f;
    [SerializeField] private float targetCheckRange = 1000f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private bool ignoreClouds = true;

    [Header("Cone Settings")]
    [SerializeField] [Range(0f, 10f)] private float crosshairConeAngle = 2f;
    [SerializeField] private int coneRayCount = 8;

    private Color currentColor;
    private Color targetColor;
    private float nextCheckTime;
    private Transform firePoint;

    void Start()
    {
        if (gunLogic == null)
        {
            gunLogic = FindObjectOfType<GunLogic>();
        }

        if (gunLogic != null)
        {
            firePoint = gunLogic.GetFirePoint();
        }

        if (crosshairSprite == null)
        {
            crosshairSprite = GetComponent<SpriteRenderer>();
        }

        currentColor = defaultColor;
        targetColor = defaultColor;

        if (crosshairSprite != null)
        {
            crosshairSprite.color = currentColor;
        }
    }

    void Update()
    {
        // Periodic target check
        if (Time.time >= nextCheckTime)
        {
            CheckForTargets();
            nextCheckTime = Time.time + targetCheckInterval;
        }

        // Smooth color transition
        if (crosshairSprite != null)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorTransitionSpeed);
            crosshairSprite.color = currentColor;
        }
    }

    void CheckForTargets()
    {
        if (firePoint == null)
        {
            targetColor = defaultColor;
            return;
        }

        Vector3 origin = firePoint.position;
        Vector3 direction = -firePoint.up;

        LayerMask effectiveLayers = targetLayers;
        if (ignoreClouds)
        {
            int cloudLayer = LayerMask.NameToLayer("Cloud");
            if (cloudLayer >= 0)
            {
                effectiveLayers &= ~(1 << cloudLayer);
            }
        }

        bool targetFound = CheckConeForTargets(origin, direction, effectiveLayers);
        targetColor = targetFound ? targetAcquiredColor : defaultColor;

        // Notify gun logic
        gunLogic?.OnTargetInCrosshair?.Invoke(targetFound);
    }

    bool CheckConeForTargets(Vector3 origin, Vector3 centerDirection, LayerMask layers)
    {
        // Center ray
        if (Physics.Raycast(origin, centerDirection, out RaycastHit hit, targetCheckRange, layers))
        {
            if (hit.collider.GetComponentInParent<IDamageable>() != null)
            {
                return true;
            }
        }

        // Cone perimeter rays
        float angleRad = crosshairConeAngle * Mathf.Deg2Rad;
        Vector3 up = Vector3.up;
        if (Vector3.Dot(centerDirection, up) > 0.99f)
        {
            up = Vector3.right;
        }

        for (int i = 0; i < coneRayCount; i++)
        {
            float angle = (i / (float)coneRayCount) * 2f * Mathf.PI;

            Vector3 right = Vector3.Cross(centerDirection, up).normalized;
            Vector3 actualUp = Vector3.Cross(right, centerDirection).normalized;
            Vector3 offset = (right * Mathf.Cos(angle) + actualUp * Mathf.Sin(angle)) * Mathf.Tan(angleRad);
            Vector3 rayDirection = (centerDirection + offset).normalized;

            if (Physics.Raycast(origin, rayDirection, out RaycastHit coneHit, targetCheckRange, layers))
            {
                if (coneHit.collider.GetComponentInParent<IDamageable>() != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void SetDefaultColor(Color color)
    {
        defaultColor = color;
    }

    public void SetTargetColor(Color color)
    {
        targetAcquiredColor = color;
    }
}
