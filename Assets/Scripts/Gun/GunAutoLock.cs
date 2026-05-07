using UnityEngine;

public class GunAutoLock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunLogic gunLogic;
    [SerializeField] private Transform lockRotationRoot;

    [Header("Targeting")]
    [SerializeField] private float targetCheckRange = 3000f;
    [SerializeField] private float targetCheckInterval = 0.05f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Lock Settings")]
    [SerializeField] private bool autoLockEnabled = true;
    [SerializeField] private float lockTurnSpeed = 720f;
    [SerializeField] private float minLockDot = 0.3f;
    [SerializeField] private float lockGraceTime = 0.5f;

    [Header("Screen Cone Targeting")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float screenConeRadius = 250f;
    [SerializeField] private bool requireTargetOnScreen = true;

    public Transform CurrentTarget { get; private set; }

    private Transform firePoint;
    private Transform ownerRoot;
    private float nextCheckTime;
    private float lastSeenTime;

    void Awake()
    {
        FindRuntimeReferences();

        if (targetLayers.value == 0)
            targetLayers = Physics.DefaultRaycastLayers;
    }

    void Update()
    {
        FindRuntimeReferences();

        if (Time.time >= nextCheckTime)
        {
            if (CurrentTarget == null)
                FindBestTarget();

            nextCheckTime = Time.time + targetCheckInterval;
        }

        if (!IsTargetStillValid())
            CurrentTarget = null;

        if (autoLockEnabled)
            AimGunAtTarget();

        gunLogic?.OnTargetInCrosshair?.Invoke(CurrentTarget != null);
    }

    void FindRuntimeReferences()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
        if (gunLogic == null)
            gunLogic = FindFirstObjectByType<GunLogic>();

        if (gunLogic != null && firePoint == null)
            firePoint = gunLogic.GetFirePoint();

        if (firePoint != null)
        {
            ownerRoot = firePoint.root;

            if (lockRotationRoot == null)
                lockRotationRoot = firePoint.parent;
        }
    }

   void FindBestTarget()
{
    if (firePoint == null || worldCamera == null)
        return;

    Collider[] hits = Physics.OverlapSphere(
        firePoint.position,
        targetCheckRange,
        targetLayers
    );

    Transform bestTarget = null;
    float bestScore = -Mathf.Infinity;

    Vector2 screenCenter = new Vector2(
        Screen.width * 0.5f,
        Screen.height * 0.5f
    );

    foreach (Collider hit in hits)
    {
        Transform root = hit.transform.root;

        if (!root.CompareTag(enemyTag))
            continue;

        if (ownerRoot != null && root == ownerRoot)
            continue;

        Vector3 screenPos = worldCamera.WorldToScreenPoint(root.position);

        if (screenPos.z <= 0f)
            continue;

        if (requireTargetOnScreen)
        {
            if (screenPos.x < 0f || screenPos.x > Screen.width ||
                screenPos.y < 0f || screenPos.y > Screen.height)
            {
                continue;
            }
        }

        Vector2 screenOffset = (Vector2)screenPos - screenCenter;
        float screenDistance = screenOffset.magnitude;

        if (screenDistance > screenConeRadius)
            continue;

        Vector3 toTarget = root.position - firePoint.position;
        float worldDistance = toTarget.magnitude;

        if (worldDistance > targetCheckRange || worldDistance <= 0.001f)
            continue;

        // Higher score = closer to screen center and closer in world distance
        float centerScore = 1f - (screenDistance / screenConeRadius);
        float distanceScore = 1f - Mathf.Clamp01(worldDistance / targetCheckRange);

        float score = centerScore * 3f + distanceScore;

        if (score > bestScore)
        {
            bestScore = score;
            bestTarget = root;
        }
    }

    CurrentTarget = bestTarget;

    if (CurrentTarget != null)
        lastSeenTime = Time.time;
}

    bool IsTargetStillValid()
{
    if (CurrentTarget == null || firePoint == null || worldCamera == null)
        return false;

    if (!CurrentTarget.gameObject.activeInHierarchy)
        return false;

    Vector3 toTarget = CurrentTarget.position - firePoint.position;
    float distance = toTarget.magnitude;

    if (distance > targetCheckRange || distance <= 0.001f)
        return false;

    Vector3 screenPos = worldCamera.WorldToScreenPoint(CurrentTarget.position);

    if (screenPos.z <= 0f)
        return false;

    if (requireTargetOnScreen)
    {
        if (screenPos.x < 0f || screenPos.x > Screen.width ||
            screenPos.y < 0f || screenPos.y > Screen.height)
        {
            return false;
        }
    }

    Vector2 screenCenter = new Vector2(
        Screen.width * 0.5f,
        Screen.height * 0.5f
    );

    float screenDistance = Vector2.Distance(screenPos, screenCenter);

    if (screenDistance > screenConeRadius)
        return false;

    lastSeenTime = Time.time;
    return true;
}   

    void AimGunAtTarget()
    {
        if (CurrentTarget == null || lockRotationRoot == null)
            return;

        Vector3 direction = CurrentTarget.position - lockRotationRoot.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        lockRotationRoot.rotation = Quaternion.RotateTowards(
            lockRotationRoot.rotation,
            targetRotation,
            lockTurnSpeed * Time.deltaTime
        );
    }
}