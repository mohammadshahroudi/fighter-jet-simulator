using UnityEngine;

public interface IHealthProvider
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
}

[RequireComponent(typeof(Rigidbody))]
public class EnemyFighterAI : MonoBehaviour
{
    [Header("Flight")]
    [Tooltip("Baseline cruising speed")]
    public float cruiseSpeed = 60f;
    [Tooltip("Max speed during an attack run")]
    public float attackSpeed = 90f;
    [Tooltip("Speed when repositioning after an overshoot")]
    public float repositionSpeed = 50f;
    [Tooltip("How tightly the jet turns")]
    public float turnRate = 2.5f;

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

    [Header("Spawn Behavior")]
    [Tooltip("If true, starts directly in AttackRun. Otherwise starts in Approach.")]
    public bool spawnInAttackRun = false;

    [Header("Evasion")]
    [Tooltip("Chance per second to trigger a dodge")]
    public float evasionChance = 0.25f;
    public float evasionDuration = 1.2f;

    [Header("Phase Change (damaged behaviour)")]
    [Tooltip("Health fraction below which the jet goes erratic")]
    [Range(0f, 1f)]
    public float damagedThreshold = 0.4f;

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

    [Header("Camera Framing")]
    public Camera gameplayCamera;
    [Tooltip("How close to screen edge enemies are allowed to get")]
    [Range(0.05f, 0.45f)]
    public float screenEdgeBuffer = 0.20f;
    [Tooltip("How strongly enemies are pulled back toward screen center")]
    public float cameraReturnStrength = 150f;
    [Tooltip("Extra forward depth used when repositioning into view")]
    public float cameraDepthOffset = 100f;

    [Header("Camera Center Pass")]
    [Tooltip("How often the enemy tries to pass through the camera center")]
    public float centerPassInterval = 6f;
    [Tooltip("How long the enemy stays committed to the center pass")]
    public float centerPassCommitTime = 1.4f;
    [Tooltip("Depth in front of the camera for the pass target")]
    public float centerPassDepth = 80f;
    [Tooltip("Random sideways offset after the center pass so they do not stall in front")]
    public float centerExitSpread = 35f;
    [Tooltip("Chance each interval to perform the pass")]
    [Range(0f, 1f)]
    public float centerPassChance = 0.8f;

    [Header("Laser FX")]
    public Transform firePoint;
    public LineRenderer laserLinePrefab;
    public float laserDuration = 0.12f;
    public AudioClip fireSfx;
    public Vector2 firePitchRange = new Vector2(0.95f, 1.05f);

    private enum AIState
    {
        Approach,
        AttackRun,
        Overshoot,
        Evade,
        Disabled
    }

    private AIState _state = AIState.Approach;

    private Rigidbody _rb;
    private Transform _player;
    private IHealthProvider _healthProvider;

    private float _currentSpeed;
    private float _fireTimer;
    private float _stateTimer;
    private float _evasionTimer;
    private bool _isDamaged;

    private bool _nearMissActive;
    private float _nearMissTimer;
    private int _nearMissSide;
    private float _evasionBias;

    // Camera center pass
    private float _centerPassTimer;
    private bool _centerPassActive;
    private float _centerPassActiveTimer;
    private Vector3 _centerPassTarget;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 5f;

        _healthProvider = GetComponent<IHealthProvider>();

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound from enemy position

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        _currentSpeed = attackSpeed;
        _centerPassTimer = Random.Range(centerPassInterval * 0.35f, centerPassInterval);
        _evasionTimer = Random.Range(0.2f, 1.5f);
        _evasionBias = Random.Range(0.7f, 1.4f);

        foreach (Collider col in GetComponentsInChildren<Collider>(includeInactive: true))
        {
            if (col.gameObject == gameObject) continue;
            if (col.GetComponent<EnemyHitProxy>() != null) continue;
            col.gameObject.AddComponent<EnemyHitProxy>();
        }

        TransitionTo(spawnInAttackRun ? AIState.AttackRun : AIState.Approach);
    }

    private void Update()
    {
        if (_player == null) return;

        CheckDamagedPhase();
        UpdateNearMissState();
        UpdateCenterPass();
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

    private void RunStateMachine()
    {
        _stateTimer -= Time.deltaTime;

        switch (_state)
        {
            case AIState.Approach:   StateApproach();  break;
            case AIState.AttackRun:  StateAttackRun(); break;
            case AIState.Overshoot:  StateOvershoot(); break;
            case AIState.Evade:      StateEvade();     break;
            case AIState.Disabled:   StateDisabled();  break;
        }
    }

    private void StateApproach()
    {
        _currentSpeed = Mathf.Lerp(_currentSpeed, attackSpeed, Time.deltaTime * 1.5f);

        Vector3 target = _centerPassActive ? _centerPassTarget : _player.position;
        SteerToward(target, allowCameraConstraint: !_centerPassActive);

        float dist = DistToPlayer();

        TryRandomEvasion();

        if (!_centerPassActive && dist < attackRange)
            TransitionTo(AIState.AttackRun);
    }

    private void StateAttackRun()
    {
        _currentSpeed = attackSpeed;

        Vector3 aimPoint;
        if (_centerPassActive)
        {
            aimPoint = _centerPassTarget;
            SteerToward(aimPoint, allowCameraConstraint: false);
        }
        else
        {
            aimPoint = PredictPlayerPosition(steerLeadTime);
            aimPoint = ApplyNearMissFlyby(aimPoint);
            SteerToward(aimPoint, allowCameraConstraint: true);
        }

        float dist = DistToPlayer();

        if (dist < 30f || _stateTimer < 0f)
        {
            TransitionTo(AIState.Overshoot);
            return;
        }

        if (!_centerPassActive && dist > attackRange * 1.5f)
            TransitionTo(AIState.Approach);
    }

    private void StateOvershoot()
{
    _currentSpeed = repositionSpeed;

    Vector3 forwardTarget = transform.position + transform.forward * 200f;
    SteerToward(forwardTarget, allowCameraConstraint: true);

    if (_stateTimer < 0f)
        TransitionTo(AIState.Approach);
}

    private void StateEvade()
    {
        _rb.AddTorque(transform.forward * 420f * Time.deltaTime, ForceMode.Acceleration);
        _currentSpeed = attackSpeed;

        Vector3 awayDir = (transform.position - _player.position).normalized;
        SteerToward(transform.position + awayDir * 200f, allowCameraConstraint: true);

        if (_stateTimer < 0f)
            TransitionTo(AIState.Approach);
    }

    private void StateDisabled()
    {
        _rb.AddTorque(Random.insideUnitSphere * 80f * Time.deltaTime, ForceMode.Acceleration);
        _currentSpeed = Mathf.Max(_currentSpeed - 20f * Time.deltaTime, 10f);
    }

    private void TransitionTo(AIState newState)
    {
        _state = newState;

        if (newState != AIState.AttackRun)
            _nearMissActive = false;

        switch (newState)
        {
            case AIState.Approach:   _stateTimer = 999f;                   break;
            case AIState.AttackRun:  _stateTimer = Random.Range(4f, 7f);   break;
            case AIState.Overshoot:  _stateTimer = Random.Range(1.5f, 3f); break;
            case AIState.Evade:      _stateTimer = evasionDuration;        break;
            case AIState.Disabled:   _stateTimer = 999f;                   break;
        }
    }

    private void UpdateCenterPass()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (gameplayCamera == null || _state == AIState.Disabled)
            return;

        if (_centerPassActive)
        {
            _centerPassActiveTimer -= Time.deltaTime;

            if (_centerPassActiveTimer <= 0f)
            {
                _centerPassActive = false;
                _centerPassTimer = Random.Range(centerPassInterval * 0.75f, centerPassInterval * 1.25f);
            }

            return;
        }

        _centerPassTimer -= Time.deltaTime;

        if (_centerPassTimer <= 0f)
        {
            _centerPassTimer = Random.Range(centerPassInterval * 0.75f, centerPassInterval * 1.25f);

            if (Random.value <= centerPassChance)
            {
                BeginCenterPass();
            }
        }
    }

    private void BeginCenterPass()
    {
        if (gameplayCamera == null)
            return;

        _centerPassActive = true;
        _centerPassActiveTimer = centerPassCommitTime;

        Vector3 passPoint = gameplayCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, centerPassDepth)
        );

        Vector3 exitOffset =
            gameplayCamera.transform.right * Random.Range(-centerExitSpread, centerExitSpread) +
            gameplayCamera.transform.up * Random.Range(-centerExitSpread * 0.35f, centerExitSpread * 0.35f);

        _centerPassTarget = passPoint + exitOffset;

        if (_state == AIState.Approach)
            TransitionTo(AIState.AttackRun);
    }

    private void SteerToward(Vector3 targetPos, bool allowCameraConstraint = true)
    {
        Vector3 adjustedTarget = targetPos;

        if (transform.position.y < minAltitude)
        {
            float lift = minAltitude - transform.position.y;
            adjustedTarget += Vector3.up * lift * altitudeCorrectionStrength;
        }

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

        adjustedTarget = ApplyTerrainAvoidance(adjustedTarget);
        adjustedTarget = ApplySquadSpacing(adjustedTarget);

        if (allowCameraConstraint)
            adjustedTarget = ApplyCameraViewConstraint(adjustedTarget);

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

    private Vector3 ApplyCameraViewConstraint(Vector3 targetPos)
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (gameplayCamera == null)
            return targetPos;

        Vector3 viewportPos = gameplayCamera.WorldToViewportPoint(transform.position);

        if (viewportPos.z < 0f)
        {
            Vector3 fallback =
                gameplayCamera.transform.position +
                gameplayCamera.transform.forward * cameraDepthOffset;

            fallback += gameplayCamera.transform.right * Random.Range(-20f, 20f);
            fallback += gameplayCamera.transform.up * Random.Range(-10f, 10f);

            return fallback;
        }

        float minX = screenEdgeBuffer;
        float maxX = 1f - screenEdgeBuffer;
        float minY = screenEdgeBuffer;
        float maxY = 1f - screenEdgeBuffer;

        bool outOfBounds =
            viewportPos.x < minX || viewportPos.x > maxX ||
            viewportPos.y < minY || viewportPos.y > maxY;

        if (!outOfBounds)
            return targetPos;

        float clampedX = Mathf.Clamp(viewportPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(viewportPos.y, minY, maxY);

        float depth = Mathf.Max(viewportPos.z + cameraDepthOffset, cameraDepthOffset);

        Vector3 desiredWorldPos = gameplayCamera.ViewportToWorldPoint(
            new Vector3(clampedX, clampedY, depth)
        );

        Vector3 correction = (desiredWorldPos - transform.position).normalized * cameraReturnStrength;
        return targetPos + correction;
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

            if (c.attachedRigidbody == _rb) continue;

            EnemyFighterAI other = c.GetComponentInParent<EnemyFighterAI>();
            if (other == null || other == this) continue;

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

    private void HandleFiring()
    {
        if (_state != AIState.AttackRun) return;
        if (_player == null) return;
        if (_centerPassActive) return; // do not fire during camera-center pass

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
    Vector3 origin = firePoint != null
        ? firePoint.position
        : transform.position + transform.forward * 3f;

    Vector3 aimPoint = PredictPlayerPosition(fireLeadTime);
    Vector3 direction = (aimPoint - origin).normalized;

    Vector3 endPoint = origin + direction * gunRange;

    bool hitPlayer = false;

    if (Physics.Raycast(origin, direction, out RaycastHit hit, gunRange))
    {
        endPoint = hit.point;

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

        if (target != null)
        {
            // Only track if we hit the PLAYER
            if (hit.collider.CompareTag("Player") || hit.collider.transform.IsChildOf(_player))
            {
                hitPlayer = true;
                target.TakeDamage(10f);
            }
        }
    }

    SpawnConditionalLaser(origin, endPoint, hitPlayer);
    PlayLaserSoundFromEnemy();
}

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

        if (_state != AIState.AttackRun || _centerPassActive) return;

        float dist = DistToPlayer();
        if (dist > nearMissDistance) return;

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
    if (_state == AIState.Evade || _centerPassActive) return;

    _evasionTimer -= Time.deltaTime;
    if (_evasionTimer > 0f) return;

    // Random interval so enemies do not all evaluate evasion together.
    _evasionTimer = Random.Range(0.4f, 1.6f);

    float chance = evasionChance * _evasionBias;

    if (_isDamaged)
        chance *= 1.5f;

    if (Random.value < chance)
    {
        TransitionTo(AIState.Evade);

        // Cooldown after dodging so they do not chain-roll constantly.
        _evasionTimer = Random.Range(1.2f, 2.5f);
    }
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

private void SpawnConditionalLaser(Vector3 origin, Vector3 endPoint, bool trackPlayer)
{
    if (laserLinePrefab == null)
        return;

    LineRenderer beam = Instantiate(laserLinePrefab, Vector3.zero, Quaternion.identity);

    LaserBeamTracker tracker = beam.GetComponent<LaserBeamTracker>();
    if (tracker == null)
        tracker = beam.gameObject.AddComponent<LaserBeamTracker>();

    Transform start = firePoint != null ? firePoint : transform;

    tracker.Initialise(
        beam,
        start,
        _player,
        laserDuration,
        trackPlayer,            // 🔥 KEY PART
        endPoint,
        Vector3.up * 1.5f       // slight aim offset
    );
}

private void PlayLaserSoundFromEnemy()
{
    if (fireSfx == null)
        return;

    AudioSource audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
        audioSource = gameObject.AddComponent<AudioSource>();

    audioSource.playOnAwake = false;
    audioSource.spatialBlend = 1f;
    audioSource.pitch = Random.Range(firePitchRange.x, firePitchRange.y);
    audioSource.PlayOneShot(fireSfx);
}



#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.4f, 0f);
        Gizmos.DrawWireSphere(transform.position, gunRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, squadAvoidDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, nearMissDistance);

        if (gameplayCamera != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 p1 = gameplayCamera.ViewportToWorldPoint(new Vector3(screenEdgeBuffer, screenEdgeBuffer, cameraDepthOffset));
            Vector3 p2 = gameplayCamera.ViewportToWorldPoint(new Vector3(1f - screenEdgeBuffer, screenEdgeBuffer, cameraDepthOffset));
            Vector3 p3 = gameplayCamera.ViewportToWorldPoint(new Vector3(1f - screenEdgeBuffer, 1f - screenEdgeBuffer, cameraDepthOffset));
            Vector3 p4 = gameplayCamera.ViewportToWorldPoint(new Vector3(screenEdgeBuffer, 1f - screenEdgeBuffer, cameraDepthOffset));

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);

            Gizmos.color = Color.white;
            Vector3 centerPoint = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, centerPassDepth));
            Gizmos.DrawWireSphere(centerPoint, 4f);
        }

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 10f,
            $"[{_state}]{(_isDamaged ? " ⚠DAMAGED" : "")}{(_nearMissActive ? " FLYBY" : "")}{(_centerPassActive ? " CENTER PASS" : "")}"
        );
    }
#endif
}