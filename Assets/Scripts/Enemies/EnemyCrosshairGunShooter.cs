using UnityEngine;

[RequireComponent(typeof(GunLogic))]
public class EnemyCrosshairGunShooter : EnemyFighterAI
{
    [Header("Enemy Crosshair Shooting")]
    [SerializeField] private GunLogic gunLogic;
    [SerializeField] private Transform crosshairOrigin;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float crosshairAngleTolerance = 3f;
    [SerializeField] private float crosshairRange = 2000f;
    [SerializeField] private LayerMask visibilityMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = false;
    [SerializeField] private float debugDuration = 0.05f;
    [SerializeField] private Color debugHitColor = Color.green;
    [SerializeField] private Color debugMissColor = Color.red;

    private Transform _player;

    private void Start()
    {
        if (gunLogic == null)
        {
            gunLogic = GetComponent<GunLogic>();
        }

        if (gunLogic != null)
        {
            gunLogic.SetPlayerInputEnabled(false);
        }

        ResolvePlayer();
    }

    protected override void FireAtPlayer()
    {
        if (gunLogic == null)
        {
            return;
        }

        if (_player == null)
        {
            ResolvePlayer();
            if (_player == null)
            {
                return;
            }
        }

        Transform originTransform = crosshairOrigin != null
            ? crosshairOrigin
            : gunLogic.GetFirePoint();

        if (originTransform == null)
        {
            originTransform = transform;
        }

        Vector3 origin = originTransform.position;
        Vector3 forward = -originTransform.up;

        Vector3 toPlayer = _player.position - origin;
        float sqrDistToPlayer = toPlayer.sqrMagnitude;
        if (sqrDistToPlayer <= 0.0001f)
        {
            return;
        }

        Vector3 toPlayerDir = toPlayer.normalized;
        float angle = Vector3.Angle(forward, toPlayerDir);
        if (angle > crosshairAngleTolerance)
        {
            if (drawDebug)
            {
                Debug.DrawRay(origin, forward * Mathf.Min(crosshairRange, Mathf.Sqrt(sqrDistToPlayer)), debugMissColor, debugDuration);
            }
            return;
        }

        float rayDistance = Mathf.Min(crosshairRange, Mathf.Sqrt(sqrDistToPlayer) + 5f);
        bool hasHit = Physics.Raycast(origin, forward, out RaycastHit hit, rayDistance, visibilityMask, QueryTriggerInteraction.Ignore);
        bool playerInCrosshair = hasHit && IsPlayerCollider(hit.collider);

        if (drawDebug)
        {
            Color c = playerInCrosshair ? debugHitColor : debugMissColor;
            float len = hasHit ? hit.distance : rayDistance;
            Debug.DrawRay(origin, forward * len, c, debugDuration);
        }

        if (!playerInCrosshair)
        {
            return;
        }

        gunLogic.TryFireFromAI();
    }

    private void ResolvePlayer()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            _player = playerObject.transform;
        }
    }

    private bool IsPlayerCollider(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        Transform current = collider.transform;
        while (current != null)
        {
            if (current.CompareTag(playerTag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
