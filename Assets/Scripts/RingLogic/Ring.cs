using System.Collections;
using UnityEngine;

/// <summary>
/// Collectible Ring — Star Fox 64 style
///
/// The ring floats in the world, pulses gently, and rotates.
/// When the player enters the "snap zone", the ring animates toward
/// the jet and then awards points before destroying itself.
///
/// Setup:
///   1. Build a ring mesh (torus) or import one and attach this script.
///   2. Assign `playerTransform`, or tag your player jet "Player".
///   3. Optionally assign `collectSFX`, `collectVFXPrefab`, and a ring `material`
///      with an emission colour for the glow pulse.
///   4. Call RingSpawner.SpawnRing() at runtime, or place rings manually in the scene.
///   5. Subscribe to Ring.OnRingCollected to receive point notifications:
///         Ring.OnRingCollected += HandleRingCollected;
/// </summary>
public class Ring : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("The player jet transform. Auto-found via tag 'Player' if left null.")]
    public Transform playerTransform;

    [Tooltip("The ring mesh renderer (used for glow pulse). Auto-found if left null.")]
    public Renderer ringRenderer;

    [Header("Ring Value")]
    public int pointValue = 25;

    [Header("Snap / Collection")]
    [Tooltip("Distance at which the ring snaps toward the player.")]
    public float snapDistance    = 40f;

    [Tooltip("Distance at which collection is confirmed and points are awarded.")]
    public float collectDistance = 5f;

    [Tooltip("How fast the ring flies toward the player once snapping.")]
    public float snapSpeed       = 18f;

    [Header("Idle Animation")]
    public float rotationSpeed   = 45f;   // degrees/sec
    public float bobAmplitude    = 1.5f;  // world units
    public float bobFrequency    = 0.8f;  // cycles/sec
    public float pulseSpeed      = 2f;
    public float pulseMin        = 0.4f;
    public float pulseMax        = 1.2f;

    [Header("Collect Animation")]
    [Tooltip("How long the scale-up flash lasts before the ring disappears.")]
    public float collectFlashDuration = 0.18f;

    [Header("Audio / VFX")]
    public AudioClip collectSFX;

    [Tooltip("Optional particle effect spawned at collection point.")]
    public GameObject collectVFXPrefab;

    // -------------------------------------------------------------------------
    // Static event — subscribe anywhere to react to collections
    // -------------------------------------------------------------------------

    /// <summary>Fired when a ring is collected. Args: (pointValue, worldPosition)</summary>
    public static event System.Action<int, Vector3> OnRingCollected;

    // -------------------------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------------------------

    private enum RingState { Idle, Snapping, Collecting }
    private RingState state = RingState.Idle;

    private Vector3    originPosition;
    private float      bobTimer;
    private Material   mat;
    private bool       collected = false;

    // Emission property IDs cached for performance
    private static readonly int s_emissionColorID = Shader.PropertyToID("_EmissionColor");
    private Color baseEmissionColor = Color.cyan;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        originPosition = transform.position;

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (ringRenderer == null)
            ringRenderer = GetComponentInChildren<Renderer>();

        if (ringRenderer != null)
        {
            // Instance the material so each ring can pulse independently
            mat = ringRenderer.material;
            if (mat.HasProperty(s_emissionColorID))
                baseEmissionColor = mat.GetColor(s_emissionColorID);
        }
    }

    void Update()
    {
        if (collected || playerTransform == null) return;

        switch (state)
        {
            case RingState.Idle:      UpdateIdle();      break;
            case RingState.Snapping:  UpdateSnapping();  break;
        }
    }

    // -------------------------------------------------------------------------
    // State updates
    // -------------------------------------------------------------------------

    void UpdateIdle()
    {
        // --- Rotation ---
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // --- Bob ---
        bobTimer += Time.deltaTime;
        float bobY = Mathf.Sin(bobTimer * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = originPosition + Vector3.up * bobY;

        // --- Emission glow pulse ---
        if (mat != null && mat.HasProperty(s_emissionColorID))
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float intensity = Mathf.Lerp(pulseMin, pulseMax, t);
            mat.SetColor(s_emissionColorID, baseEmissionColor * intensity);
        }

        // --- Check snap distance ---
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= snapDistance)
            state = RingState.Snapping;
    }

    void UpdateSnapping()
    {
        // Fly toward the player
        transform.position = Vector3.MoveTowards(
            transform.position,
            playerTransform.position,
            snapSpeed * Time.deltaTime
        );

        // Spin faster as it closes in
        transform.Rotate(Vector3.up, rotationSpeed * 3f * Time.deltaTime, Space.World);

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // Scale up slightly as it approaches (satisfying suck-in effect)
        float scaleFactor = Mathf.Lerp(1.3f, 0.2f, 1f - (dist / snapDistance));
        transform.localScale = Vector3.one * Mathf.Max(0.1f, scaleFactor);

        if (dist <= collectDistance)
            StartCoroutine(CollectRoutine());
    }

    // -------------------------------------------------------------------------
    // Collection
    // -------------------------------------------------------------------------

    IEnumerator CollectRoutine()
    {
        if (collected) yield break;
        collected = true;
        state = RingState.Collecting;

        // Fire event first so UI/score can react immediately
        OnRingCollected?.Invoke(pointValue, transform.position);

        // Spawn VFX
        if (collectVFXPrefab != null)
            Instantiate(collectVFXPrefab, transform.position, Quaternion.identity);

        // Play SFX (on a persistent object so it survives this object's death)
        if (collectSFX != null)
            AudioSource.PlayClipAtPoint(collectSFX, transform.position);

        // Flash scale-up then shrink to nothing
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < collectFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectFlashDuration;
            // Quick scale-up then collapse
            float s = t < 0.4f
                ? Mathf.Lerp(startScale.x, 2.5f, t / 0.4f)
                : Mathf.Lerp(2.5f, 0f, (t - 0.4f) / 0.6f);
            transform.localScale = Vector3.one * s;
            yield return null;
        }

        Destroy(gameObject);
    }

    // -------------------------------------------------------------------------
    // Debug gizmos
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, snapDistance);

        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, collectDistance);
    }
#endif
}
