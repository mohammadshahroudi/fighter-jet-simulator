using System.Collections;
using UnityEngine;

/// <summary>
/// Sky Target-inspired enemy fighter jet AI.
///
/// Mimics the arcade behavior from Sega's Sky Target (1995):
///   - Enemies fly in formation or patrol paths at constant speed
///   - On player detection, they break formation and attack
///   - Attack patterns: diving gun runs, banking evasion, loop-arounds
///   - Predictive lead-aiming for their own weapons
///   - HP-based phase changes (damaged enemies get more erratic)
///   - Scripted "hero pass" where enemy overshoots and repositions
///
/// Requires:
///   - A component on this GameObject implementing IDamageable
///   - Optionally also implementing IHealthProvider for damaged-phase behaviour
///   - Player tagged "Player" in the scene
///   - Weapon script (calls FireAtPlayer() which does its own raycast or projectile)
/// </summary>

/// <summary>
/// Optional companion to IDamageable.
/// Implement this alongside IDamageable on your health component to allow
/// the AI to read health values for phase-change behaviour.
///
/// Example:
///   public class EnemyHealth : MonoBehaviour, IDamageable, IHealthProvider { ... }
/// </summary>
public interface IHealthProvider
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
}

[RequireComponent(typeof(Rigidbody))]
public class EnemyFighterAI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector / Tuning
    // ─────────────────────────────────────────────

    [Header("Flight")]
    [Tooltip("Baseline cruising speed (units/sec)")]
    public float cruiseSpeed = 60f;
    [Tooltip("Max speed during an attack run")]
    public float attackSpeed = 90f;
    [Tooltip("Speed when repositioning after an overshoot")]
    public float repositionSpeed = 50f;
    [Tooltip("How tightly the jet turns (higher = sharper)")]
    public float turnRate = 2.5f;
    [Tooltip("Smooth rotation speed multiplier")]
    public float rollSmoothing = 4f;

    [Header("Detection")]
    public float detectionRange = 400f;
    public float losAngle = 60f;

    [Header("Attack")]
    public float attackRange = 250f;
    public float gunRange = 180f;
    [Tooltip("Seconds between bursts")]
    public float fireRate = 0.8f;
    [Tooltip("Degrees of aim tolerance before firing")]
    public float aimTolerance = 8f;
    [Tooltip("Prediction time for steering during attack runs")]
    public float steerLeadTime = 0.4f;
    [Tooltip("Prediction time for weapon fire")]
    public float fireLeadTime = 0.25f;

    [Header("Evasion")]
    [Tooltip("Chance per second to trigger a barrel-roll dodge")]
    public float evasionChance = 0.25f;
    public float evasionDuration = 1.2f;

    [Header("Formation (optional)")]
    [Tooltip("Assign a leader transform to fly in formation. Leave null for solo.")]
    public Transform formationLeader;
    public Vector3 formationOffset = new Vector3(30f, 0f, -20f);

    [Header("Phase Change (damaged behaviour)")]
    [Tooltip("Health fraction below which the jet goes erratic")]
    [Range(0f, 1f)]
    public float damagedThreshold = 0.4f;

    [Header("Patrol")]
    public Transform[] patrolWaypoints;

    [Header("Altitude")]
    public float minAltitude = 50f;
    public float altitudeCorrectionStrength = 2f;

    [Header("Player Avoidance")]
    public float playerAvoidDistance = 40f;
    public float avoidStrength = 2.5f;

    [Header("Terrain Avoidance")]
    public float terrainCheckDistance = 120f;
    public float terrainAvoidanceStrength = 2.5f;
    public float minGroundClearance = 35f;
    public LayerMask terrainMask = ~0;

    [Header("Squad Spacing")]
    public float squadAvoidDistance = 35f;
    public float squadAvoidStrength = 2f;
    public LayerMask squadMask = ~0;

    [Header("Near-Miss Flyby")]
    public float nearMissDistance = 55f;
    public float nearMissOffset = 45f;
    public float nearMissCommitTime = 0.9f;

    // ─────────────────────────────────────────────
    //  State Machine
    // ─────────────────────────────────────────────

    private enum AIState
    {
        Patrol,
        Approach,
        AttackRun,
        Overshoot,
        Evade,
        LoopAround,
        Disabled
    }

    private AIState _state = AIState.Patrol;

    // ─────────────────────────────────────────────
    //  Private References
    // ─────────────────────────────────────────────

    private Rigidbody _rb;
    private Transform _player;
    private IDamageable _damageable;
    private IHealthProvider _healthProvider;

    private float _currentSpeed;
    private float _fireTimer;
    private float _stateTimer;
    private float _evasionTimer;
    private bool _isDamaged;
    private int _waypointIndex;

    // Near-miss state
    private bool _nearMissActive;
    private float _nearMissTimer;
    private int _nearMissSide;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 5f;

        _damageable = GetComponent<IDamageable>();
        _healthProvider = GetComponent<IHealthProvider>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        _currentSpeed = cruiseSpeed;

        if (patrolWaypoints == null || patrolWaypoints.Length == 0)
            GeneratePatrolCircle();
    }

    private void Update()
    {
        if (_player == null) return;

        CheckDamagedPhase();
        UpdateNearMissState();
        RunStateMachine();
        HandleFiring();
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = transform.forward * _currentSpeed;
    }

    private void LateUpdate()
    {
        if (transform.position.y < minAltitude)
        {
            Vector3 pos = transform.position;
            pos.y = minAltitude;
            transform.position = pos;
        }
    }

    // ─────────────────────────────────────────────
    //  State Machine
    // ─────────────────────────────────────────────

    private void RunStateMachine()
    {
        _stateTimer -= Time.deltaTime;

        switch (_state)
        {
            case AIState.Patrol: StatePatrol(); break;
            case AIState.Approach: StateApproach(); break;
            case AIState.AttackRun: StateAttackRun(); break;
            case AIState.Overshoot: StateOvershoot(); break;
            case AIState.Evade: StateEvade(); break;
            case AIState.LoopAround: StateLoop(); break;
            case AIState.Disabled: StateDisabled(); break;
        }
    }

    private void StatePatrol()
    {
        _currentSpeed = cruiseSpeed;

        if (formationLeader != null)
        {
            Vector3 targetPos = formationLeader.TransformPoint(formationOffset);
            SteerToward(targetPos);
        }
        else
        {
            if (patrolWaypoints.Length > 0)
            {
                SteerToward(patrolWaypoints[_waypointIndex].position);

                if (Vector3.Distance(transform.position, patrolWaypoints[_waypointIndex].position) < 40f)
                    _waypointIndex = (_waypointIndex + 1) % patrolWaypoints.Length;
            }
        }

        if (PlayerInDetectionCone())
            TransitionTo(AIState.Approach);
    }

    private void StateApproach()
    {
        _currentSpeed = Mathf.Lerp(_currentSpeed, attackSpeed, Time.deltaTime * 1.5f);
        SteerToward(_player.position);

        float dist = DistToPlayer();

        TryRandomEvasion();

        if (dist < attackRange)
            TransitionTo(AIState.AttackRun);

        if (!PlayerInDetectionCone() && dist > detectionRange * 1.5f)
            TransitionTo(AIState.Patrol);
    }

    private void StateAttackRun()
    {
        _currentSpeed = attackSpeed;

        Vector3 aimPoint = PredictPlayerPosition(steerLeadTime);
        aimPoint = ApplyNearMissFlyby(aimPoint);
        SteerToward(aimPoint);

        float dist = DistToPlayer();

        if (dist < 30f || _stateTimer < 0f)
        {
            TransitionTo(AIState.Overshoot);
            return;
        }

        if (dist > attackRange * 1.5f)
            TransitionTo(AIState.Approach);
    }

    private void StateOvershoot()
    {
        _currentSpeed = repositionSpeed;

        if (_stateTimer < 0f)
            TransitionTo(AIState.LoopAround);
    }

    private void StateEvade()
    {
        _rb.AddTorque(transform.forward * 420f * Time.deltaTime, ForceMode.Acceleration);
        _currentSpeed = attackSpeed;

        Vector3 awayDir = (transform.position - _player.position).normalized;
        SteerToward(transform.position + awayDir * 200f);

        if (_stateTimer < 0f)
            TransitionTo(_isDamaged ? AIState.LoopAround : AIState.Approach);
    }

    private void StateLoop()
    {
        _rb.AddTorque(transform.right * 200f * Time.deltaTime, ForceMode.Acceleration);
        _currentSpeed = Mathf.Lerp(_currentSpeed, cruiseSpeed * 0.7f, Time.deltaTime * 2f);

        if (_stateTimer < 0f)
            TransitionTo(AIState.Approach);
    }

    private void StateDisabled()
    {
        _rb.AddTorque(Random.insideUnitSphere * 80f * Time.deltaTime, ForceMode.Acceleration);
        _currentSpeed = Mathf.Max(_currentSpeed - 20f * Time.deltaTime, 10f);
    }

    // ─────────────────────────────────────────────
    //  Transitions
    // ─────────────────────────────────────────────

    private void TransitionTo(AIState newState)
    {
        _state = newState;

        if (newState != AIState.AttackRun)
            _nearMissActive = false;

        switch (newState)
        {
            case AIState.Patrol: _stateTimer = 0f; break;
            case AIState.Approach: _stateTimer = 999f; break;
            case AIState.AttackRun: _stateTimer = Random.Range(4f, 7f); break;
            case AIState.Overshoot: _stateTimer = Random.Range(1.5f, 3f); break;
            case AIState.Evade: _stateTimer = evasionDuration; break;
            case AIState.LoopAround: _stateTimer = Random.Range(2.5f, 4f); break;
            case AIState.Disabled: _stateTimer = 999f; break;
        }
    }

    // ─────────────────────────────────────────────
    //  Steering
    // ─────────────────────────────────────────────

    private void SteerToward(Vector3 targetPos)
    {
        Vector3 adjustedTarget = targetPos;

        // 1) Min altitude correction
        if (transform.position.y < minAltitude)
        {
            float lift = minAltitude - transform.position.y;
            adjustedTarget += Vector3.up * lift * altitudeCorrectionStrength;
        }

        // 2) Avoid colliding with player
        if (_player != null)
        {
            Vector3 toPlayer = transform.position - _player.position;
            float dist = toPlayer.magnitude;

            if (dist < playerAvoidDistance && dist > 0.001f)
            {
                Vector3 avoidDir = toPlayer.normalized;
                float force = (1f - (dist / playerAvoidDistance)) * avoidStrength;

                if (_state == AIState.AttackRun)
                    force *= 1.5f;

                adjustedTarget += avoidDir * force * 100f;
            }
        }

        // 3) Terrain avoidance
        adjustedTarget = ApplyTerrainAvoidance(adjustedTarget);

        // 4) Squad spacing
        adjustedTarget = ApplySquadSpacing(adjustedTarget);

        Vector3 desired = adjustedTarget - transform.position;
        if (desired.sqrMagnitude < 0.001f) return;

        Vector3 desiredDir = desired.normalized;
        Quaternion desiredRot = Quaternion.LookRotation(desiredDir);

        float yawDot = Vector3.Dot(transform.right, desiredDir);
        Quaternion bankRot = Quaternion.AngleAxis(yawDot * 30f, transform.forward);
        desiredRot = desiredRot * bankRot;

        _rb.MoveRotation(Quaternion.Slerp(
            _rb.rotation,
            desiredRot,
            turnRate * Time.deltaTime
        ));
    }

    private Vector3 ApplyTerrainAvoidance(Vector3 targetPos)
    {
        Vector3 adjustedTarget = targetPos;

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 downForward = (transform.forward + Vector3.down * 0.35f).normalized;

        if (Physics.Raycast(origin, forward, out RaycastHit forwardHit, terrainCheckDistance, terrainMask, QueryTriggerInteraction.Ignore))
        {
            if (!IsPlayerOrSelf(forwardHit.transform))
            {
                adjustedTarget += Vector3.up * terrainAvoidanceStrength * 60f;
                adjustedTarget += forwardHit.normal * terrainAvoidanceStrength * 30f;
            }
        }

        if (Physics.Raycast(origin, downForward, out RaycastHit downHit, terrainCheckDistance, terrainMask, QueryTriggerInteraction.Ignore))
        {
            if (!IsPlayerOrSelf(downHit.transform))
            {
                float clearance = transform.position.y - downHit.point.y;

                if (clearance < minGroundClearance)
                {
                    float lift = minGroundClearance - clearance;
                    adjustedTarget += Vector3.up * lift * terrainAvoidanceStrength;
                }
            }
        }

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, minGroundClearance, terrainMask, QueryTriggerInteraction.Ignore))
        {
            if (!IsPlayerOrSelf(groundHit.transform))
            {
                float lift = minGroundClearance - groundHit.distance;
                adjustedTarget += Vector3.up * Mathf.Max(lift, 0f) * terrainAvoidanceStrength;
            }
        }

        return adjustedTarget;
    }

    private Vector3 ApplySquadSpacing(Vector3 targetPos)
    {
        Vector3 adjustedTarget = targetPos;

        Collider[] nearby = Physics.OverlapSphere(
            transform.position,
            squadAvoidDistance,
            squadMask,
            QueryTriggerInteraction.Ignore
        );

        Vector3 separation = Vector3.zero;
        int count = 0;

        for (int i = 0; i < nearby.Length; i++)
        {
            Collider c = nearby[i];

            if (c.attachedRigidbody == _rb)
                continue;

            EnemyFighterAI other = c.GetComponentInParent<EnemyFighterAI>();
            if (other == null || other == this)
                continue;

            Vector3 away = transform.position - other.transform.position;
            float dist = away.magnitude;

            if (dist > 0.001f && dist < squadAvoidDistance)
            {
                float weight = 1f - (dist / squadAvoidDistance);
                separation += away.normalized * weight;
                count++;
            }
        }

        if (count > 0)
        {
            separation /= count;
            adjustedTarget += separation * squadAvoidStrength * 50f;
        }

        return adjustedTarget;
    }

    // ─────────────────────────────────────────────
    //  Firing
    // ─────────────────────────────────────────────

    private void HandleFiring()
    {
        if (_state != AIState.AttackRun) return;
        if (_player == null) return;

        _fireTimer -= Time.deltaTime;
        if (_fireTimer > 0f) return;

        Vector3 toPlayer = (_player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, toPlayer);

        if (angle < aimTolerance && DistToPlayer() < gunRange)
        {
            FireAtPlayer();
            _fireTimer = _isDamaged
                ? fireRate * Random.Range(0.5f, 0.9f)
                : fireRate * Random.Range(0.9f, 1.3f);
        }
    }

    protected virtual void FireAtPlayer()
    {
        Vector3 origin = transform.position + transform.forward * 3f;
        Vector3 aimPoint = PredictPlayerPosition(fireLeadTime);
        Vector3 direction = (aimPoint - origin).normalized;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, gunRange))
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            target?.TakeDamage(10f);
        }

        // Optional: spawn muzzle flash / tracer FX here
    }

    // ─────────────────────────────────────────────
    //  Near-Miss Flyby
    // ─────────────────────────────────────────────

    private void UpdateNearMissState()
    {
        if (_player == null) return;

        if (_nearMissActive)
        {
            _nearMissTimer -= Time.deltaTime;
            if (_nearMissTimer <= 0f)
                _nearMissActive = false;

            return;
        }

        if (_state != AIState.AttackRun)
            return;

        float dist = DistToPlayer();
        if (dist > nearMissDistance)
            return;

        Vector3 toPlayer = (_player.position - transform.position).normalized;
        float forwardDot = Vector3.Dot(transform.forward, toPlayer);

        if (forwardDot > 0.7f)
        {
            _nearMissActive = true;
            _nearMissTimer = nearMissCommitTime;
            _nearMissSide = Random.value < 0.5f ? -1 : 1;
        }
    }

    private Vector3 ApplyNearMissFlyby(Vector3 targetPos)
    {
        if (!_nearMissActive || _player == null)
            return targetPos;

        Vector3 playerRight = _player.right;
        Vector3 offset = playerRight * (_nearMissSide * nearMissOffset);

        Vector3 flybyPoint = _player.position + offset;
        flybyPoint.y = Mathf.Max(flybyPoint.y, minAltitude);

        return flybyPoint;
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private bool PlayerInDetectionCone()
    {
        if (_player == null) return false;

        float dist = DistToPlayer();
        if (dist > detectionRange) return false;

        Vector3 toPlayer = (_player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > losAngle) return false;

        if (Physics.Raycast(transform.position, toPlayer, out RaycastHit hit, dist, terrainMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == _player || hit.transform.IsChildOf(_player);
        }

        return true;
    }

    private float DistToPlayer()
    {
        return _player == null ? float.MaxValue : Vector3.Distance(transform.position, _player.position);
    }

    private Vector3 PredictPlayerPosition(float seconds)
    {
        if (_player == null) return transform.position + transform.forward * 100f;

        Rigidbody playerRb = _player.GetComponent<Rigidbody>();
        Vector3 playerVel = playerRb != null ? playerRb.linearVelocity : Vector3.zero;
        return _player.position + playerVel * seconds;
    }

    private void TryRandomEvasion()
    {
        if (_state == AIState.Evade) return;

        _evasionTimer -= Time.deltaTime;
        if (_evasionTimer > 0f) return;

        _evasionTimer = 1f;
        if (Random.value < evasionChance * (_isDamaged ? 2f : 1f))
            TransitionTo(AIState.Evade);
    }

    private void CheckDamagedPhase()
    {
        if (_healthProvider == null) return;

        float max = Mathf.Max(_healthProvider.MaxHealth, 0.0001f);
        float fraction = _healthProvider.CurrentHealth / max;
        _isDamaged = fraction <= damagedThreshold;

        if (_healthProvider.CurrentHealth <= 0f && _state != AIState.Disabled)
            TransitionTo(AIState.Disabled);
    }

    private bool IsPlayerOrSelf(Transform hitTransform)
    {
        if (hitTransform == null) return false;
        if (hitTransform == transform || hitTransform.IsChildOf(transform)) return true;
        if (_player != null && (hitTransform == _player || hitTransform.IsChildOf(_player))) return true;
        return false;
    }

    // ─────────────────────────────────────────────
    //  Patrol Circle Generator
    // ─────────────────────────────────────────────

    private void GeneratePatrolCircle()
    {
        int count = 6;
        float radius = 300f;
        patrolWaypoints = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 pos = transform.position
                + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            GameObject wp = new GameObject($"PatrolWP_{i}");
            wp.transform.position = pos;
            patrolWaypoints[i] = wp.transform;
        }
    }

    // ─────────────────────────────────────────────
    //  Gizmos
    // ─────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.4f, 0f);
        Gizmos.DrawWireSphere(transform.position, gunRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, squadAvoidDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, nearMissDistance);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 10f,
            $"[{_state}]{(_isDamaged ? " ⚠DAMAGED" : "")}{(_nearMissActive ? " FLYBY" : "")}"
        );
    }
#endif
}