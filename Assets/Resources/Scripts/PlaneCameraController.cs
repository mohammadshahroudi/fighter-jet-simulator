using UnityEngine;
using Unity.Cinemachine;

public class PlaneCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform plane;

    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 10f;
    [SerializeField] private float followHeight = 3f;

    [Header("Damping")]
    [SerializeField] private float positionDamping = 5f;   // How fast camera catches up positionally
    [SerializeField] private float yawDamping     = 4f;   // Snappy — always shows where you're heading
    [SerializeField] private float pitchDamping   = 3f;   // Slightly lazy on pitch
    [SerializeField] private float rollDamping    = 1.5f; // Most lag here — roll feels heavy/cinematic

    private Vector3 currentPosition;
    private float currentYaw;
    private float currentPitch;
    private float currentRoll;

    private void Awake()
    {
        if (plane == null) return;

        // Initialise to plane's current rotation so there's no snap on start
        currentYaw   = plane.eulerAngles.y;
        currentPitch = plane.eulerAngles.x;
        currentRoll  = plane.eulerAngles.z;
        currentPosition = plane.position;
    }

    private void LateUpdate()
    {
        if (plane == null) return;

        // --- 1. Independently damp each axis at different speeds ---
        currentYaw   = Mathf.LerpAngle(currentYaw,   plane.eulerAngles.y, yawDamping   * Time.deltaTime);
        currentPitch = Mathf.LerpAngle(currentPitch, plane.eulerAngles.x, pitchDamping * Time.deltaTime);
        currentRoll  = Mathf.LerpAngle(currentRoll,  plane.eulerAngles.z, rollDamping  * Time.deltaTime);

        // --- 2. Build the lagged rotation from our independent axes ---
        Quaternion laggedRotation = Quaternion.Euler(currentPitch, currentYaw, currentRoll);

        // --- 3. Position the camera behind and above the plane using the lagged rotation ---
        Vector3 offset = new Vector3(0, followHeight, -followDistance);
        Vector3 targetPosition = plane.position + laggedRotation * offset;

        currentPosition = Vector3.Lerp(currentPosition, targetPosition, positionDamping * Time.deltaTime);

        // --- 4. Apply ---
        transform.position = currentPosition;
        transform.rotation = laggedRotation;
    }
}