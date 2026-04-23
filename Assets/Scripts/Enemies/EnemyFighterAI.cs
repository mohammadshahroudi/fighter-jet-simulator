using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy Fighter Jet AI
/// 
/// States:
///   Formation  — holds a slot in a formation around a lead aircraft
///   Agro       — breaks formation and attempts to get behind the player to fire
///   Attacking  — locked behind player, firing
///   Evading    — briefly breaks off after taking damage / overshooting
///
/// Setup:
///   1. Attach this script to each enemy jet prefab.
///   2. Assign `playerTransform` (drag the player object in Inspector, or let it
///      auto-find by tag "Player").
///   3. Create a lead/anchor GameObject for the formation and assign it to
///      `formationLeader` on every jet in the group.
///   4. Each jet needs a unique `formationSlotIndex` (0, 1, 2 …).
///   5. Put a Collider (trigger or solid) and a Rigidbody on the jet.
///   6. Call EnemyFighterAI.AlertFormation() on any jet when it is destroyed
///      (e.g. from your health/damage script) so the rest go agro.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyFighterAI : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("Assign the player jet transform, or leave null to auto-find by tag 'Player'.")]
    public Transform playerTransform;

    [Tooltip("A shared Transform that defines the centre of the formation (can be an empty GameObject).")]
    public Transform formationLeader;

    [Header("Formation Settings")]
    [Tooltip("Index of this jet's slot in the formation (0 = lead, 1, 2 …).")]
    public int formationSlotIndex = 0;

    [Tooltip("How far apart formation slots are spaced (world units).")]
    public float formationSpacing = 15f;

    [Tooltip("Offset pattern per slot index. Add more entries for bigger formations.")]
    public Vector3[] formationOffsets = new Vector3[]
    {
        new Vector3(  0,   0,   0),   // slot 0 – lead
        new Vector3(-20,   0, -15),   // slot 1 – left wing
        new Vector3( 20,   0, -15),   // slot 2 – right wing
        new Vector3(-30,   5, -30),   // slot 3 – left rear high
        new Vector3( 30,   5, -30),   // slot 4 – right rear high
    };

    [Header("Flight Parameters")]
    public float maxSpeed          = 120f;
    public float formationSpeed    = 80f;
    public float agroSpeed         = 110f;
    public float turnSpeed         = 2.5f;   // how quickly the jet rotates toward target heading
    public float rollSpeed         = 3f;     // banking roll amount
    public float minAltitude       = 50f;    // prevent flying into the ground

    [Header("Combat Settings")]
    [Tooltip("Distance at which the jet starts shooting.")]
    public float fireRange         = 300f;

    [Tooltip("Half-angle cone in front of the jet within which it will fire.")]
    public float fireAngle         = 8f;

    [Tooltip("Seconds between bursts.")]
    public float fireRate          = 0.15f;

    [Tooltip("Assign your bullet / missile prefab.")]
    public GameObject projectilePrefab;

    [Tooltip("Muzzle transforms where projectiles spawn.")]
    public Transform[] muzzlePoints;

    [Header("Agro Behaviour")]
    [Tooltip("How far behind the player the jet tries to position itself.")]
    public float tailOffset        = 60f;

    [Tooltip("How close the jet needs to be to its tail position before it starts attacking.")]
    public float tailPositionTolerance = 40f;

    [Tooltip("After being hit, the jet evades for this many seconds before re-engaging.")]
    public float evasionDuration   = 3f;

    [Tooltip("Radius of random evasion manoeuvre.")]
    public float evasionRadius     = 80f;

    // -------------------------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------------------------

    public enum AIState { Formation, Agro, Attacking, Evading }
    public AIState currentState { get; private set; } = AIState.Formation;

    // Static list so any jet can alert the whole formation
    private static List<EnemyFighterAI> s_formation = new List<EnemyFighterAI>();

    private Rigidbody  rb;
    private float      nextFireTime;
    private float      evasionEndTime;
    private Vector3    evasionTarget;
    private bool       isAlerted = false;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity   = false;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 2f;

        // Register in the shared formation list
        if (!s_formation.Contains(this))
            s_formation.Add(this);
    }

    void Start()
    {
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else Debug.LogWarning($"[EnemyFighterAI] {name}: No player found. Assign 'playerTransform' or tag the player 'Player'.");
        }
    }

    void OnDestroy()
    {
        s_formation.Remove(this);
        // Alert every other jet in the formation
        AlertFormation();
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        EnforceMinAltitude();

        switch (currentState)
        {
            case AIState.Formation: UpdateFormation(); break;
            case AIState.Agro:      UpdateAgro();      break;
            case AIState.Attacking: UpdateAttacking(); break;
            case AIState.Evading:   UpdateEvading();   break;
        }
    }

    // -------------------------------------------------------------------------
    // State updates
    // -------------------------------------------------------------------------

    void UpdateFormation()
    {
        if (isAlerted)
        {
            TransitionTo(AIState.Agro);
            return;
        }

        Vector3 targetPos = GetFormationSlotPosition();
        FlyToward(targetPos, formationSpeed);
        AlignWithLeader();
    }

    void UpdateAgro()
    {
        // Fly toward a position directly behind the player
        Vector3 tailPos = GetPlayerTailPosition();
        float distToTail = Vector3.Distance(transform.position, tailPos);

        FlyToward(tailPos, agroSpeed);

        if (distToTail < tailPositionTolerance)
            TransitionTo(AIState.Attacking);
    }

    void UpdateAttacking()
    {
        // Keep chasing the player's tail and shoot when aligned
        Vector3 tailPos = GetPlayerTailPosition();
        FlyToward(tailPos, agroSpeed);

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float angleToPlayer = Vector3.Angle(transform.forward, playerTransform.position - transform.position);

        if (distToPlayer <= fireRange && angleToPlayer <= fireAngle)
            TryFire();

        // If we overshoot or player escapes, go back to agro chase
        if (distToPlayer > fireRange * 1.5f)
            TransitionTo(AIState.Agro);
    }

    void UpdateEvading()
    {
        if (Time.time >= evasionEndTime)
        {
            TransitionTo(AIState.Agro);
            return;
        }

        FlyToward(evasionTarget, maxSpeed);

        // Refresh evasion target if we reach it
        if (Vector3.Distance(transform.position, evasionTarget) < 20f)
            evasionTarget = GetRandomEvasionPoint();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Call this when a jet in the formation is destroyed to alert all others.
    /// Already called automatically from OnDestroy, but you can also call it
    /// manually (e.g. from a damage script before the object is actually destroyed).
    /// </summary>
    public static void AlertFormation()
    {
        foreach (var jet in s_formation)
        {
            if (jet != null)
                jet.OnFormationAlert();
        }
    }

    /// <summary>Call from a damage script when this jet takes a hit.</summary>
    public void OnHit()
    {
        if (currentState == AIState.Formation)
            AlertFormation();

        // Temporarily break off to evade
        StartEvasion();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    void OnFormationAlert()
    {
        isAlerted = true;
        if (currentState == AIState.Formation)
            TransitionTo(AIState.Agro);
    }

    void TransitionTo(AIState newState)
    {
        currentState = newState;

        if (newState == AIState.Evading)
        {
            evasionEndTime = Time.time + evasionDuration;
            evasionTarget  = GetRandomEvasionPoint();
        }
    }

    void StartEvasion()
    {
        TransitionTo(AIState.Evading);
    }

    /// <summary>Smoothly fly toward a world-space position at a given speed.</summary>
    void FlyToward(Vector3 targetPosition, float speed)
    {
        Vector3 desiredDir = (targetPosition - transform.position).normalized;

        // Smooth rotation toward desired direction
        Quaternion targetRot = Quaternion.LookRotation(desiredDir, transform.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);

        // Apply banking roll based on how much we are turning
        float turnDot = Vector3.Dot(transform.right, desiredDir);
        Quaternion bankRot = Quaternion.AngleAxis(-turnDot * rollSpeed * 20f, transform.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, transform.rotation * bankRot, Time.fixedDeltaTime * rollSpeed);

        // Move forward
        rb.linearVelocity = transform.forward * speed;
    }

    /// <summary>While in formation, also try to match the leader's orientation.</summary>
    void AlignWithLeader()
    {
        if (formationLeader == null) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, formationLeader.rotation, turnSpeed * 0.5f * Time.fixedDeltaTime);
    }

    /// <summary>Returns the world position of this jet's formation slot.</summary>
    Vector3 GetFormationSlotPosition()
    {
        if (formationLeader == null) return transform.position;

        int idx = Mathf.Clamp(formationSlotIndex, 0, formationOffsets.Length - 1);
        Vector3 localOffset = formationOffsets[idx] * (formationSpacing / 15f); // scale with spacing
        return formationLeader.TransformPoint(localOffset);
    }

    /// <summary>Returns a position directly behind and slightly below the player.</summary>
    Vector3 GetPlayerTailPosition()
    {
        return playerTransform.position
               - playerTransform.forward * tailOffset
               + playerTransform.up * 5f;
    }

    Vector3 GetRandomEvasionPoint()
    {
        Vector3 randomDir = Random.onUnitSphere;
        randomDir.y = Mathf.Abs(randomDir.y); // prefer not diving into the ground
        return transform.position + randomDir * evasionRadius;
    }

    void EnforceMinAltitude()
    {
        if (transform.position.y < minAltitude)
        {
            Vector3 pos = transform.position;
            pos.y = minAltitude;
            transform.position = pos;

            // Pitch up if too low
            Vector3 fwd = transform.forward;
            fwd.y = Mathf.Max(fwd.y, 0.3f);
            transform.forward = fwd.normalized;
        }
    }

    void TryFire()
    {
        if (Time.time < nextFireTime) return;
        if (projectilePrefab == null || muzzlePoints == null || muzzlePoints.Length == 0) return;

        nextFireTime = Time.time + fireRate;

        foreach (Transform muzzle in muzzlePoints)
        {
            Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
        }
    }

    // -------------------------------------------------------------------------
    // Debug gizmos (visible in Scene view)
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Formation slot
        if (formationLeader != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(GetFormationSlotPosition(), 2f);
            Gizmos.DrawLine(transform.position, GetFormationSlotPosition());
        }

        // Fire range
        Gizmos.color = new Color(1, 0, 0, 0.15f);
        Gizmos.DrawWireSphere(transform.position, fireRange);

        // Tail position
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(GetPlayerTailPosition(), 3f);
            Gizmos.DrawLine(transform.position, GetPlayerTailPosition());
        }
    }
#endif
}
