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

    private Transform crosshairTransform;
    private Camera cam;

    private Color currentColor;
    private Color targetColor;
    private float nextCheckTime;
    private Transform firePoint;
    private Transform ownerRoot;
    private Transform lockedTarget;

    void Start()
    {
        cam = Camera.main;

        if (gunLogic == null)
            gunLogic = FindFirstObjectByType<GunLogic>();

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

        if (crosshairSprite != null)
        {
            crosshairTransform = crosshairSprite.transform;
            crosshairSprite.color = defaultColor;
        }

        currentColor = defaultColor;
        targetColor = defaultColor;
    }

    void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            CheckForTargets();
            nextCheckTime = Time.time + targetCheckInterval;
        }

        if (!IsTargetStillValid())
        {
            lockedTarget = null;
        }

        if (lockedTarget == null)
        {
            ResetCrosshairToScreenCenter();
        }

        if (autoLockEnabled)
            UpdateAutoLock();

        // Color transition
        if (crosshairSprite != null)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorTransitionSpeed);
            crosshairSprite.color = currentColor;
        }
    }

    void ResetCrosshairToScreenCenter()
    {
        if (crosshairTransform == null || cam == null)
            return;

        float distance = Mathf.Abs(crosshairTransform.position.z - cam.transform.position.z);

        Vector3 screenCenter = new Vector3(
            Screen.width * 0.5f,
            Screen.height * 0.5f,
            distance
        );

        Vector3 worldCenter = cam.ScreenToWorldPoint(screenCenter);

        crosshairTransform.position = Decay(
            crosshairTransform.position,
            worldCenter,
            returnDecay
        );

        // Prevent rotation drift from parent
        crosshairTransform.rotation = cam.transform.rotation;

        // Snap when close
        if (Vector3.Distance(crosshairTransform.position, worldCenter) < 0.01f)
        {
            crosshairTransform.position = worldCenter;
        }
    }

    void CheckForTargets()
    {
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
        if (lockedTarget == null || firePoint == null)
            return false;

        if (!lockedTarget.gameObject.activeInHierarchy)
            return false;

        Vector3 dir = lockedTarget.position - firePoint.position;

        if (dir.magnitude > targetCheckRange)
            return false;

        float dot = Vector3.Dot(-firePoint.up, dir.normalized);

        if (dot < 0.8f)
            return false;

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

        CheckRay(origin, centerDirection, layers, ref bestTarget, ref bestDistance);

        float angleRad = crosshairConeAngle * Mathf.Deg2Rad;

        Vector3 up = Vector3.up;
        if (Vector3.Dot(centerDirection, up) > 0.99f)
            up = Vector3.right;

        Vector3 right = Vector3.Cross(centerDirection, up).normalized;
        Vector3 actualUp = Vector3.Cross(right, centerDirection).normalized;

        for (int i = 0; i < coneRayCount; i++)
        {
            float angle = (i / (float)coneRayCount) * Mathf.PI * 2f;

            Vector3 offset =
                (right * Mathf.Cos(angle) + actualUp * Mathf.Sin(angle)) *
                Mathf.Tan(angleRad);

            Vector3 rayDir = (centerDirection + offset).normalized;

            CheckRay(origin, rayDir, layers, ref bestTarget, ref bestDistance);
        }

        return bestTarget;
    }

    void CheckRay(Vector3 origin, Vector3 direction, LayerMask layers,
        ref Transform bestTarget, ref float bestDistance)
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
        if (lockedTarget == null || lockRotationRoot == null)
            return;

        Vector3 dir = lockedTarget.position - lockRotationRoot.position;

        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

        lockRotationRoot.rotation = Quaternion.RotateTowards(
            lockRotationRoot.rotation,
            targetRot,
            lockTurnSpeed * Time.deltaTime
        );
    }

    bool IsValidEnemyHit(RaycastHit hit)
    {
        if (!hit.collider.CompareTag(enemyTag) &&
            !hit.collider.transform.root.CompareTag(enemyTag))
            return false;

        if (ownerRoot != null && hit.collider.transform.root == ownerRoot)
            return false;

        return true;
    }

    Vector3 Decay(Vector3 current, Vector3 target, float decay)
    {
        return target + (current - target) * Mathf.Exp(-decay * Time.deltaTime);
    }
}