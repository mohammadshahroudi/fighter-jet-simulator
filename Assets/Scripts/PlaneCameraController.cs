using UnityEngine;

public class PlaneCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform plane;

    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 10f;
    [SerializeField] private float followHeight = 3f;

    [Header("Damping")]
    [SerializeField] private float positionDamping = 5f;
    [SerializeField] private float yawDamping     = 4f;
    [SerializeField] private float pitchDamping   = 3f;
    [SerializeField] private float rollDamping    = 1.5f;

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

        float posDecay = DecayFactor(positionDamping, dt);
        currentPosition += (targetPosition - currentPosition) * posDecay;

        // --- 4. Apply ---
        transform.position = currentPosition;
        transform.rotation = laggedRotation;
    }

    private float DampAngle(float current, float target, float rate, float dt)
    {
        float delta = Mathf.DeltaAngle(current, target); // handles 360° wrap correctly
        return current + delta * DecayFactor(rate, dt);
    }
}