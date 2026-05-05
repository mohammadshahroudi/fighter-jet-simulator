using UnityEngine;

public class PlaneCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform plane;

    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 5f;
    [SerializeField] private float followHeight = 3f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistanceFromPlane = 8f;
    [SerializeField] private bool lockPositionToPlane = false;

    [Header("Damping")]
    [SerializeField] private float positionDamping = 25f;
    [SerializeField] private float yawDamping     = 4f;
    [SerializeField] private float pitchDamping   = 3f;
    [SerializeField] private float rollDamping    = 3f;

    [Header("Collision")]
    [SerializeField] private bool enableCollisionAvoidance = true;
    [SerializeField] private float collisionRadius = 0.5f;
    [SerializeField] private LayerMask collisionLayers = ~0;

    private Vector3 currentPosition;
    private float currentYaw;
    private float currentPitch;
    private float currentRoll;

    // Converts a damping rate into an exponential decay factor
    private static float DecayFactor(float rate, float dt) => 1f - Mathf.Exp(-rate * dt);

    private void Awake()
    {
        if (plane == null) return;

        currentYaw      = plane.eulerAngles.y;
        currentPitch    = plane.eulerAngles.x;
        currentRoll     = plane.eulerAngles.z;
        currentPosition = plane.position;
    }

    private void LateUpdate()
    {
        if (plane == null) return;

        float dt = Time.deltaTime;

        // --- 1. Independently damp each axis using exponential decay ---
        currentYaw   = DampAngle(currentYaw,   plane.eulerAngles.y, yawDamping,   dt);
        currentPitch = DampAngle(currentPitch, plane.eulerAngles.x, pitchDamping, dt);
        currentRoll  = DampAngle(currentRoll,  plane.eulerAngles.z, rollDamping,  dt);

        // --- 2. Build the lagged rotation from independent axes ---
        Quaternion laggedRotation = Quaternion.Euler(currentPitch, currentYaw, currentRoll);

        // --- 3. Position the camera behind and above the plane ---
        Vector3 offset = new Vector3(0, followHeight, -followDistance);
        Vector3 targetPosition = plane.position + laggedRotation * offset;

        // --- 4. Collision avoidance ---
        if (enableCollisionAvoidance)
        {
            Vector3 directionToCamera = (targetPosition - plane.position).normalized;
            float desiredDistance = Vector3.Distance(plane.position, targetPosition);

            if (Physics.SphereCast(plane.position, collisionRadius, directionToCamera,
                out RaycastHit hit, desiredDistance, collisionLayers))
            {
                // Place camera just before the collision point
                targetPosition = plane.position + directionToCamera * Mathf.Max(hit.distance - collisionRadius, minDistance);
            }
        }

        // --- 5. Position update ---
        if (lockPositionToPlane)
        {
            // Camera position is locked relative to plane - no lag
            currentPosition = targetPosition;
        }
        else
        {
            // Smooth position with damping
            float posDecay = DecayFactor(positionDamping, dt);
            currentPosition += (targetPosition - currentPosition) * posDecay;
        }

        // --- 6. Clamp max distance from plane ---
        float distanceFromPlane = Vector3.Distance(currentPosition, plane.position);
        if (distanceFromPlane > maxDistanceFromPlane)
        {
            Vector3 directionToCamera = (currentPosition - plane.position).normalized;
            currentPosition = plane.position + directionToCamera * maxDistanceFromPlane;
        }

        // --- 7. Apply ---
        transform.position = currentPosition;
        transform.rotation = laggedRotation;
    }

    private float DampAngle(float current, float target, float rate, float dt)
    {
        float delta = Mathf.DeltaAngle(current, target); // handles 360° wrap correctly
        return current + delta * DecayFactor(rate, dt);
    }
}