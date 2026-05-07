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

    [Header("Presentation")]
    [SerializeField] private Vector3 fixedLocalScale = new Vector3(1.25f, 1.25f, 1f);

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
    private Transform ownerRoot;

    void Start()
    {
        if (crosshairSprite == null)
            crosshairSprite = GetComponent<SpriteRenderer>();

        if (gunLogic == null)
            gunLogic = FindObjectOfType<GunLogic>();

        if (gunLogic != null)
        {
            firePoint = gunLogic.GetFirePoint();
            if (firePoint != null)
                ownerRoot = firePoint.root;
        }

        if (targetLayers.value == 0)
            targetLayers = Physics.DefaultRaycastLayers;

        currentColor = defaultColor;
        targetColor = defaultColor;

        if (crosshairSprite != null)
            crosshairSprite.color = currentColor;
    }

    void Update()
    {
        // Keep crosshair always visible and at fixed scale
        transform.localScale = fixedLocalScale;

        if (crosshairSprite != null)
        {
            if (!crosshairSprite.enabled)
                crosshairSprite.enabled = true;

            // Periodic target check
            if (Time.time >= nextCheckTime)
            {
                CheckForTargets();
                nextCheckTime = Time.time + targetCheckInterval;
            }

            // Smooth color transition (always opaque)
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorTransitionSpeed);
            currentColor.a = 1f;
            crosshairSprite.color = currentColor;
        }
    }

    private void CheckForTargets()
    {
        if (gunLogic == null)
            gunLogic = FindObjectOfType<GunLogic>();

        if (firePoint == null && gunLogic != null)
        {
            firePoint = gunLogic.GetFirePoint();
            if (firePoint != null)
                ownerRoot = firePoint.root;
        }

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
            int cloudsLayer = LayerMask.NameToLayer("Clouds");
            if (cloudLayer >= 0)  effectiveLayers &= ~(1 << cloudLayer);
            if (cloudsLayer >= 0) effectiveLayers &= ~(1 << cloudsLayer);
        }

        bool targetFound = CheckConeForTargets(origin, direction, effectiveLayers);
        targetColor = targetFound ? targetAcquiredColor : defaultColor;

        gunLogic?.OnTargetInCrosshair?.Invoke(targetFound);
    }

    private bool CheckConeForTargets(Vector3 origin, Vector3 centerDirection, LayerMask layers)
    {
        // Center ray
        if (Physics.Raycast(origin, centerDirection, out RaycastHit hit, targetCheckRange, layers))
        {
            if (IsValidDamageableHit(hit))
                return true;
        }

        // Cone perimeter rays
        float angleRad = crosshairConeAngle * Mathf.Deg2Rad;
        Vector3 up = Vector3.Dot(centerDirection, Vector3.up) > 0.99f ? Vector3.right : Vector3.up;
        Vector3 right = Vector3.Cross(centerDirection, up).normalized;
        Vector3 actualUp = Vector3.Cross(right, centerDirection).normalized;

        for (int i = 0; i < coneRayCount; i++)
        {
            float angle = (i / (float)coneRayCount) * 2f * Mathf.PI;
            Vector3 offset = (right * Mathf.Cos(angle) + actualUp * Mathf.Sin(angle)) * Mathf.Tan(angleRad);
            Vector3 rayDirection = (centerDirection + offset).normalized;

            if (Physics.Raycast(origin, rayDirection, out RaycastHit coneHit, targetCheckRange, layers))
            {
                if (IsValidDamageableHit(coneHit))
                    return true;
            }
        }

        return false;
    }

    private bool IsValidDamageableHit(RaycastHit hit)
    {
        if (hit.collider.GetComponentInParent<IDamageable>() == null)
            return false;

        if (ownerRoot != null && hit.collider.transform.root == ownerRoot)
            return false;

        return true;
    }

    public void SetDefaultColor(Color color) => defaultColor = color;
    public void SetTargetColor(Color color) => targetAcquiredColor = color;
}
