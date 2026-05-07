using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunLogic gunLogic;
    [SerializeField] private SpriteRenderer crosshairSprite;
    [SerializeField] private Transform lockRotationRoot;

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

    [Header("Auto Lock")]
    [SerializeField] private bool autoLockEnabled = true;
    [SerializeField] private float lockTurnSpeed = 720f;
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Return To Center")]
    [SerializeField] private float returnDecay = 8f;

    private Vector3 centerLocalPosition;
    private Transform crosshairTransform;

private Vector3 currentVelocity; // optional if you expand later

    private Color currentColor;
    private Color targetColor;
    private float nextCheckTime;
    private Transform firePoint;
    private Transform ownerRoot;
    private Transform lockedTarget;

    void Start()
    {
        if (gunLogic == null)
            gunLogic = FindObjectOfType<GunLogic>();

        if (gunLogic != null)
        {
            firePoint = gunLogic.GetFirePoint();

            if (firePoint != null)
            {
                ownerRoot = firePoint.root;

                if (lockRotationRoot == null)
                    lockRotationRoot = firePoint.parent;
            }
        }

        if (targetLayers.value == 0)
            targetLayers = Physics.DefaultRaycastLayers;

        if (crosshairSprite == null)
            crosshairSprite = GetComponent<SpriteRenderer>();

        currentColor = defaultColor;
        targetColor = defaultColor;

        if (crosshairSprite != null)
        {
            
            crosshairSprite.color = currentColor;
            crosshairTransform = crosshairSprite.transform;
            centerLocalPosition = crosshairTransform.localPosition;
        }
    }

    void Update()
{
    if (Time.time >= nextCheckTime)
    {
        CheckForTargets();
        nextCheckTime = Time.time + targetCheckInterval;
    }

    // 🔥 NEW: force break lock if target invalid
    if (!IsTargetStillValid())
    {
        lockedTarget = null;
        if (crosshairTransform != null)
        crosshairTransform.localPosition = centerLocalPosition;
    }

    if (lockedTarget == null && crosshairTransform != null)
{
    crosshairTransform.localPosition = Decay(
        crosshairTransform.localPosition,
        centerLocalPosition,
        returnDecay
    );

    if (Vector3.Distance(crosshairTransform.localPosition, centerLocalPosition) < 0.001f)
    {
        crosshairTransform.localPosition = centerLocalPosition;
    }
}

    if (autoLockEnabled)
        UpdateAutoLock();

    // Return to center when no lock

    // Color
    if (crosshairSprite != null)
    {
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorTransitionSpeed);
        crosshairSprite.color = currentColor;
    }
}

    void CheckForTargets()
    {
        if (gunLogic == null)
            gunLogic = FindObjectOfType<GunLogic>();

        if (firePoint == null && gunLogic != null)
        {
            firePoint = gunLogic.GetFirePoint();

            if (firePoint != null)
            {
                ownerRoot = firePoint.root;

                if (lockRotationRoot == null)
                    lockRotationRoot = firePoint.parent;
            }
        }

        if (firePoint == null)
        {
            lockedTarget = null;
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

            if (cloudLayer >= 0)
                effectiveLayers &= ~(1 << cloudLayer);

            if (cloudsLayer >= 0)
                effectiveLayers &= ~(1 << cloudsLayer);
        }

        lockedTarget = GetBestTargetInCone(origin, direction, effectiveLayers);

        bool targetFound = lockedTarget != null;
        targetColor = targetFound ? targetAcquiredColor : defaultColor;

        gunLogic?.OnTargetInCrosshair?.Invoke(targetFound);
    }

  bool IsTargetStillValid()
{
    if (lockedTarget == null)
        return false;

    if (firePoint == null)
        return false;

    if (!lockedTarget.gameObject.activeInHierarchy)
        return false;

    Vector3 dirToTarget = lockedTarget.position - firePoint.position;

    if (dirToTarget.magnitude > targetCheckRange)
        return false;

    float dot = Vector3.Dot(-firePoint.up, dirToTarget.normalized);

    if (dot < 0.8f)
        return false;

    // Extra polish: break lock if something blocks the target
    if (Physics.Linecast(firePoint.position, lockedTarget.position, out RaycastHit hit, targetLayers))
    {
        if (hit.transform.root != lockedTarget.root)
            return false;
    }

    return true;
}

    Transform GetBestTargetInCone(Vector3 origin, Vector3 centerDirection, LayerMask layers)
    {
        Transform bestTarget = null;
        float bestDistance = Mathf.Infinity;

        CheckRayForTarget(origin, centerDirection, layers, ref bestTarget, ref bestDistance);

        float angleRad = crosshairConeAngle * Mathf.Deg2Rad;

        Vector3 up = Vector3.up;
        if (Vector3.Dot(centerDirection, up) > 0.99f)
            up = Vector3.right;

        Vector3 right = Vector3.Cross(centerDirection, up).normalized;
        Vector3 actualUp = Vector3.Cross(right, centerDirection).normalized;

        for (int i = 0; i < coneRayCount; i++)
        {
            float angle = (i / (float)coneRayCount) * 2f * Mathf.PI;

            Vector3 offset =
                (right * Mathf.Cos(angle) + actualUp * Mathf.Sin(angle))
                * Mathf.Tan(angleRad);

            Vector3 rayDirection = (centerDirection + offset).normalized;

            CheckRayForTarget(origin, rayDirection, layers, ref bestTarget, ref bestDistance);
        }

        return bestTarget;
    }

    void CheckRayForTarget(
        Vector3 origin,
        Vector3 direction,
        LayerMask layers,
        ref Transform bestTarget,
        ref float bestDistance)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, targetCheckRange, layers))
        {
            if (!IsValidEnemyHit(hit))
                return;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestTarget = hit.collider.transform.root;
            }
        }
    }

void UpdateAutoLock()
{
    if (lockedTarget == null)
        return;

    if (lockRotationRoot == null)
    {
        Debug.LogWarning("Auto Lock: Lock Rotation Root is not assigned.");
        return;
    }

    if (firePoint == null)
    {
        Debug.LogWarning("Auto Lock: FirePoint is missing.");
        return;
    }

    Vector3 targetPos = lockedTarget.position;
    Vector3 rootPos = lockRotationRoot.position;

    Vector3 targetDirection = targetPos - rootPos;

    if (targetDirection.sqrMagnitude <= 0.001f)
        return;

    Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

    lockRotationRoot.rotation = Quaternion.RotateTowards(
        lockRotationRoot.rotation,
        targetRotation,
        lockTurnSpeed * Time.deltaTime
    );
}

    private bool IsValidEnemyHit(RaycastHit hit)
    {
        if (!hit.collider.CompareTag(enemyTag) &&
            !hit.collider.transform.root.CompareTag(enemyTag))
        {
            return false;
        }

        if (ownerRoot != null && hit.collider.transform.root == ownerRoot)
            return false;

        return true;
    }

    public void SetDefaultColor(Color color)
    {
        defaultColor = color;
    }

    public void SetTargetColor(Color color)
    {
        targetAcquiredColor = color;
    }
    Vector3 Decay(Vector3 current, Vector3 target, float decay)
{
    return target + (current - target) * Mathf.Exp(-decay * Time.deltaTime);
}
}