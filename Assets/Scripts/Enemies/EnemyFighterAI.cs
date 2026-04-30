using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy Fighter Jet AI
///
/// States:
///   Patrol    — flies a randomised waypoint path, unaware of the player
///   Alerted   — player entered detection range, jet turns to investigate
///   Agro      — chasing the player's tail to get behind them
///   Attacking — locked behind player, firing
///   Evading   — breaks off after taking damage or overshooting
///
/// Randomness:
///   - Patrol waypoints are randomly generated within a radius
///   - Agro approach adds a random lateral offset so jets don't all funnel identically
///   - Fire timing has a random jitter so volleys don't sync perfectly
///   - Evasion direction and duration are randomised per hit
///
/// Setup:
///   1. Attach to each enemy jet prefab (needs a Rigidbody).
///   2. Tag the player "Player" or assign playerTransform manually.
///   3. Optionally assign formationLeader + formationSlotIndex for formation flying.
///   4. Assign projectilePrefab and muzzlePoints for shooting.
///   5. Call EnemyFighterAI.AlertFormation() or OnHit() from EnemyHealth.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyFighterAI : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("Auto-found by tag 'Player' if left null.")]
    public Transform playerTransform;
    public Transform formationLeader;

    [Header("Formation")]
    public int       formationSlotIndex = 0;
    public float     formationSpacing   = 15f;
    public Vector3[] formationOffsets   = new Vector3[]
    {
        new Vector3(  0,  0,   0),
        new Vector3(-20,  0, -15),
        new Vector3( 20,  0, -15),
        new Vector3(-30,  5, -30),
        new Vector3( 30,  5, -30),
    };

    [Header("Detection")]
    [Tooltip("Distance at which the jet notices the player and becomes Alerted.")]
    public float detectionRange     = 400f;

    [Tooltip("Distance at which the jet loses the player and returns to Patrol.")]
    public float losePlayerRange    = 600f;

    [Tooltip("How long the jet investigates before giving up if it can't close in.")]
    public float alertedTimeout     = 6f;

    [Header("Flight")]
    public float patrolSpeed        = 60f;
    public float alertedSpeed       = 90f;
    public float agroSpeed          = 115f;
    public float maxSpeed           = 130f;
    public float turnSpeed          = 2.5f;
    public float rollSpeed          = 3f;
    public float minAltitude        = 50f;

    [Header("Patrol")]
    [Tooltip("Radius around spawn point within which patrol waypoints are generated.")]
    public float patrolRadius           = 200f;
    public float patrolWaypointMinTime  = 4f;
    public float patrolWaypointMaxTime  = 10f;
    public float patrolHeightVariance   = 40f;

    [Header("Combat")]
    public float      fireRange         = 300f;
    public float      fireAngle         = 8f;
    public float      fireRateBase      = 0.15f;
    public float      fireRateJitter    = 0.08f;
    [Tooltip("How many shots to fire before breaking off.")]
    public int        burstShotCount    = 4;
    [Tooltip("Seconds to break off and reposition before attacking again.")]
    public float      breakOffDuration  = 10f;
    public GameObject projectilePrefab;
    public Transform[] muzzlePoints;

    [Header("Agro")]
    public float tailOffset             = 60f;
    public float tailPositionTolerance  = 40f;
    [Tooltip("Random lateral spread so jets approach from slightly different angles.")]
    public float approachSpread         = 20f;

    [Header("Evasion")]
    public float evasionDurationMin     = 2f;
    public float evasionDurationMax     = 5f;
    public float evasionRadius          = 80f;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    public enum AIState { Formation, Patrol, Alerted, Agro, Attacking, BreakingOff, Evading }
    public AIState currentState { get; private set; } = AIState.Patrol;

    private static List<EnemyFighterAI> s_formation = new List<EnemyFighterAI>();

    private Rigidbody rb;       // kept for collision callbacks — movement is now transform-based
    private float     nextFireTime;
    private float     evasionEndTime;
    private Vector3   evasionTarget;
    private bool      isAlerted = false;

    // Burst fire tracking
    private int       shotsFiredInBurst  = 0;
    private float     breakOffEndTime;

    // Patrol
    private Vector3   patrolTarget;
    private float     patrolWaypointTimer;

    // Alerted
    private float     alertedTimer;

    // Per-jet approach offset — randomised on each agro entry
    private Vector3   approachOffset;
    private float     _agroStallTimer;
    private const float AgroStallTimeout = 4f;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        if (!s_formation.Contains(this))
            s_formation.Add(this);
    }

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else Debug.LogWarning($"[EnemyFighterAI] {name}: Player not found.");
        }

        TransitionTo(formationLeader != null ? AIState.Formation : AIState.Patrol);
    }

    void OnDestroy()
    {
        s_formation.Remove(this);
        AlertFormation();
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        EnforceMinAltitude();

        switch (currentState)
        {
            case AIState.Formation:  UpdateFormation();  break;
            case AIState.Patrol:     UpdatePatrol();     break;
            case AIState.Alerted:    UpdateAlerted();    break;
            case AIState.Agro:       UpdateAgro();       break;
            case AIState.Attacking:  UpdateAttacking();  break;
            case AIState.BreakingOff: UpdateBreakingOff(); break;
            case AIState.Evading:    UpdateEvading();    break;
        }
    }

    // -------------------------------------------------------------------------
    // State updates
    // -------------------------------------------------------------------------

    void UpdateFormation()
    {
        if (isAlerted)                        { TransitionTo(AIState.Agro);    return; }
        if (PlayerInRange(detectionRange))    { TransitionTo(AIState.Alerted); return; }

        FlyToward(GetFormationSlotPosition(), patrolSpeed);
        AlignWithLeader();
    }

    void UpdatePatrol()
    {
        if (PlayerInRange(detectionRange))    { TransitionTo(AIState.Alerted); return; }

        patrolWaypointTimer -= Time.fixedDeltaTime;

        if (patrolWaypointTimer <= 0f || Vector3.Distance(transform.position, patrolTarget) < 25f)
            patrolTarget = GetRandomPatrolPoint();

        FlyToward(patrolTarget, patrolSpeed);
    }

    void UpdateAlerted()
    {
        alertedTimer += Time.fixedDeltaTime;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist > losePlayerRange || alertedTimer >= alertedTimeout)
        {
            TransitionTo(AIState.Patrol);
            return;
        }

        // Close enough to commit — go full agro
        if (dist < detectionRange * 0.6f)
        {
            TransitionTo(AIState.Agro);
            return;
        }

        FlyToward(playerTransform.position, alertedSpeed);
    }

    void UpdateAgro()
    {
        if (!PlayerInRange(losePlayerRange)) { TransitionTo(AIState.Patrol); return; }

        Vector3 tailPos    = GetPlayerTailPosition();
        float   distToTail = Vector3.Distance(transform.position, tailPos);

        FlyToward(tailPos, agroSpeed);

        if (distToTail < tailPositionTolerance)
        {
            TransitionTo(AIState.Attacking);
            return;
        }

        // Re-roll approach offset if convergence is taking too long.
        // Prevents permanent stall when the random offset lands in an unreachable position.
        _agroStallTimer += Time.fixedDeltaTime;
        if (_agroStallTimer >= AgroStallTimeout)
        {
            approachOffset   = Random.insideUnitSphere * approachSpread;
            approachOffset.y = 0f;
            _agroStallTimer  = 0f;
        }
    }

    void UpdateAttacking()
    {
        if (!PlayerInRange(losePlayerRange)) { TransitionTo(AIState.Patrol); return; }

        float distToPlayer  = Vector3.Distance(transform.position, playerTransform.position);
        float angleToPlayer = Vector3.Angle(transform.forward, playerTransform.position - transform.position);

        FlyToward(GetPlayerTailPosition(), agroSpeed);

        // Fire when aligned and in range
        if (distToPlayer <= fireRange && angleToPlayer <= fireAngle)
        {
            if (TryFire())
            {
                shotsFiredInBurst++;
                if (shotsFiredInBurst >= burstShotCount)
                {
                    TransitionTo(AIState.BreakingOff);
                    return; // exit immediately — don't let overshoot check override this
                }
            }
        }

        // Only check overshoot if we haven't already committed to breaking off
        if (distToPlayer > fireRange * 1.5f)
            TransitionTo(AIState.Agro);
    }

    void UpdateBreakingOff()
    {
        // Fly a random evasion arc for the break off duration
        FlyToward(evasionTarget, maxSpeed);

        if (Vector3.Distance(transform.position, evasionTarget) < 20f)
            evasionTarget = GetRandomEvasionPoint();

        // After break off duration, re-engage
        if (Time.time >= breakOffEndTime)
            TransitionTo(PlayerInRange(losePlayerRange) ? AIState.Agro : AIState.Patrol);
    }

    void UpdateEvading()
    {
        if (Time.time >= evasionEndTime)
        {
            TransitionTo(PlayerInRange(detectionRange) ? AIState.Agro : AIState.Patrol);
            return;
        }

        FlyToward(evasionTarget, maxSpeed);

        if (Vector3.Distance(transform.position, evasionTarget) < 20f)
            evasionTarget = GetRandomEvasionPoint();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public static void AlertFormation()
    {
        // Snapshot the list before iterating — a jet may be destroyed (and removed) mid-alert,
        // which would throw InvalidOperationException on the live list.
        var snapshot = new List<EnemyFighterAI>(s_formation);
        foreach (var jet in snapshot)
            if (jet != null) jet.OnFormationAlert();
    }

    public void OnHit()
    {
        // Don't interrupt a break-off already in progress
        if (currentState == AIState.BreakingOff) return;

        if (currentState == AIState.Formation || currentState == AIState.Patrol)
            AlertFormation();

        TransitionTo(AIState.Evading);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    void OnFormationAlert()
    {
        isAlerted = true;
        if (currentState == AIState.Formation || currentState == AIState.Patrol)
            TransitionTo(AIState.Agro);
    }

    void TransitionTo(AIState newState)
    {
        switch (newState)
        {
            case AIState.Patrol:
                patrolTarget        = GetRandomPatrolPoint();
                patrolWaypointTimer = Random.Range(patrolWaypointMinTime, patrolWaypointMaxTime);
                break;

            case AIState.Alerted:
                alertedTimer = 0f;
                break;

            case AIState.Agro:
                // Unique lateral offset per jet so they don't all stack on the same tail position
                approachOffset      = Random.insideUnitSphere * approachSpread;
                approachOffset.y    = 0f;
                shotsFiredInBurst   = 0;  // reset burst counter each new attack run
                _agroStallTimer     = 0f; // reset stall watchdog
                break;

            case AIState.BreakingOff:
                breakOffEndTime = Time.time + breakOffDuration;
                evasionTarget   = GetRandomEvasionPoint();
                break;

            case AIState.Evading:
                evasionEndTime = Time.time + Random.Range(evasionDurationMin, evasionDurationMax);
                evasionTarget  = GetRandomEvasionPoint();
                break;
        }

        currentState = newState;
    }

    void FlyToward(Vector3 targetPosition, float speed)
    {
        Vector3    desiredDir = (targetPosition - transform.position).normalized;
        Quaternion targetRot  = Quaternion.LookRotation(desiredDir, transform.up);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);

        float      turnDot = Vector3.Dot(transform.right, desiredDir);
        Quaternion bankRot = Quaternion.AngleAxis(-turnDot * rollSpeed * 20f, transform.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, transform.rotation * bankRot, Time.fixedDeltaTime * rollSpeed);

        // Kinematic — move directly via transform instead of velocity
        transform.position += transform.forward * speed * Time.fixedDeltaTime;
    }

    void AlignWithLeader()
    {
        if (formationLeader == null) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, formationLeader.rotation, turnSpeed * 0.5f * Time.fixedDeltaTime);
    }

    bool PlayerInRange(float range) =>
        Vector3.Distance(transform.position, playerTransform.position) <= range;

    Vector3 GetFormationSlotPosition()
    {
        if (formationLeader == null) return transform.position;
        int idx = Mathf.Clamp(formationSlotIndex, 0, formationOffsets.Length - 1);
        return formationLeader.TransformPoint(formationOffsets[idx] * (formationSpacing / 15f));
    }

    Vector3 GetPlayerTailPosition()
    {
        Vector3 tail = playerTransform.position
                     - playerTransform.forward * tailOffset
                     + playerTransform.up     * 5f
                     + approachOffset;

        return tail;
    }

    Vector3 GetRandomPatrolPoint()
    {
        // NOTE: timer is NOT reset here — callers (TransitionTo and UpdatePatrol) own the timer.
        // Previously this reset the timer internally, causing a double-reset every waypoint arrival.
        Vector2 circle = Random.insideUnitCircle * patrolRadius;
        float   height = Mathf.Max(playerTransform.position.y + Random.Range(-patrolHeightVariance, patrolHeightVariance), minAltitude + 10f);
        return new Vector3(playerTransform.position.x + circle.x, height, playerTransform.position.z + circle.y);
    }

    Vector3 GetRandomEvasionPoint()
    {
        // Pick a point on the upper hemisphere so the jet doesn't dive into the ground
        Vector3 dir = Random.onUnitSphere;
        dir.y = Mathf.Abs(dir.y) + 0.2f; // bias upward, ensure never exactly horizontal
        dir.Normalize();

        // Use full evasionRadius — clamp so it's never too close to matter
        float dist = Mathf.Max(evasionRadius, 40f);
        Vector3 candidate = transform.position + dir * dist;

        // Respect min altitude
        candidate.y = Mathf.Max(candidate.y, minAltitude + 10f);
        return candidate;
    }

    void EnforceMinAltitude()
    {
        if (transform.position.y >= minAltitude) return;

        Vector3 pos = transform.position;
        pos.y = minAltitude;
        transform.position = pos;

        // Nudge the nose upward without clobbering yaw/roll accumulated by FlyToward.
        // Setting transform.forward directly would override the full rotation quaternion.
        if (transform.forward.y < 0.1f)
        {
            Quaternion nosUp = Quaternion.AngleAxis(-15f, transform.right);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                nosUp * transform.rotation,
                Time.fixedDeltaTime * 4f);
        }
    }

    bool TryFire()
    {
        if (Time.time < nextFireTime) return false;
        if (projectilePrefab == null || muzzlePoints == null || muzzlePoints.Length == 0) return false;

        nextFireTime = Time.time + fireRateBase + Random.Range(0f, fireRateJitter);

        foreach (Transform muzzle in muzzlePoints)
            Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);

        return true;
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(0f, 1f, 0f, 0.05f);
        Gizmos.DrawWireSphere(transform.position, losePlayerRange);

        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, fireRange);

        if (Application.isPlaying && currentState == AIState.Patrol)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(patrolTarget, 3f);
            Gizmos.DrawLine(transform.position, patrolTarget);
        }

        if (Application.isPlaying && playerTransform != null &&
           (currentState == AIState.Agro || currentState == AIState.Attacking))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(GetPlayerTailPosition(), 3f);
            Gizmos.DrawLine(transform.position, GetPlayerTailPosition());
        }
    }
#endif
}