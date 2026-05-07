using System.Collections;
using UnityEngine;

public class Ring : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public Renderer ringRenderer;

    [Header("Ring Value")]
    public int pointValue = 25;

    [Header("Magnet / Collection")]
    public float snapDistance = 40f;
    public float collectDistance = 5f;

    [Header("PID Magnet")]
    public float proportionalGain = 9f;
    public float integralGain = 0.05f;
    public float derivativeGain = 3f;
    public float maxSpeed = 85f;
    public float maxForce = 160f;

    [Header("Idle Animation")]
    public float rotationSpeed = 45f;
    public float bobAmplitude = 1.5f;
    public float bobFrequency = 0.8f;
    public float pulseSpeed = 2f;
    public float pulseMin = 0.4f;
    public float pulseMax = 1.2f;

    [Header("Collect Animation")]
    public float collectFlashDuration = 0.18f;

    [Header("Audio / VFX")]
    public AudioClip collectSFX;
    public GameObject collectVFXPrefab;

    private enum RingState { Idle, Snapping, Collecting }
    private RingState state = RingState.Idle;

    private Vector3 originPosition;
    private float bobTimer;
    private Material mat;
    private bool collected;

    private Vector3 velocity;
    private Vector3 integral;
    private Vector3 previousError;

    private static readonly int s_emissionColorID = Shader.PropertyToID("_EmissionColor");
    private Color baseEmissionColor = Color.cyan;

    void Awake()
    {
        SetupReferences();
        ResetRing();
    }

    void OnEnable()
    {
        SetupReferences();
        ResetRing();
    }

    void Update()
    {
        if (collected || playerTransform == null) return;

        switch (state)
        {
            case RingState.Idle:
                UpdateIdle();
                break;

            case RingState.Snapping:
                UpdateSnappingPID();
                break;
        }
    }

    void SetupReferences()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (ringRenderer == null)
            ringRenderer = GetComponentInChildren<Renderer>();

        if (ringRenderer != null && mat == null)
        {
            mat = ringRenderer.material;

            if (mat.HasProperty(s_emissionColorID))
                baseEmissionColor = mat.GetColor(s_emissionColorID);
        }
    }

    public void SetPlayer(Transform player)
    {
        playerTransform = player;
    }

    public void ResetRing()
    {
        state = RingState.Idle;
        collected = false;

        originPosition = transform.position;
        bobTimer = Random.Range(0f, 10f);

        velocity = Vector3.zero;
        integral = Vector3.zero;
        previousError = Vector3.zero;

        transform.localScale = Vector3.one;
    }

    void UpdateIdle()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        bobTimer += Time.deltaTime;
        float bobY = Mathf.Sin(bobTimer * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = originPosition + Vector3.up * bobY;

        if (mat != null && mat.HasProperty(s_emissionColorID))
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float intensity = Mathf.Lerp(pulseMin, pulseMax, t);
            mat.SetColor(s_emissionColorID, baseEmissionColor * intensity);
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist <= snapDistance)
        {
            state = RingState.Snapping;

            velocity = Vector3.zero;
            integral = Vector3.zero;
            previousError = playerTransform.position - transform.position;
        }
    }

    void UpdateSnappingPID()
    {
        Vector3 targetPosition = playerTransform.position;
        Vector3 error = targetPosition - transform.position;

        integral += error * Time.deltaTime;
        integral = Vector3.ClampMagnitude(integral, 20f);

        Vector3 derivative = (error - previousError) / Mathf.Max(Time.deltaTime, 0.0001f);

        Vector3 force =
            proportionalGain * error +
            integralGain * integral +
            derivativeGain * derivative;

        force = Vector3.ClampMagnitude(force, maxForce);

        velocity += force * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        transform.position += velocity * Time.deltaTime;

        previousError = error;

        transform.Rotate(Vector3.up, rotationSpeed * 3f * Time.deltaTime, Space.World);

        float dist = error.magnitude;
        float t = Mathf.Clamp01(1f - dist / snapDistance);
        float scale = Mathf.Lerp(1.2f, 0.25f, t);
        transform.localScale = Vector3.one * scale;

        if (dist <= collectDistance && !collected)
            StartCoroutine(CollectRoutine());
    }

    IEnumerator CollectRoutine()
    {
        if (collected) yield break;

        collected = true;
        state = RingState.Collecting;

        if (RingScoreManager.Instance != null)
            RingScoreManager.Instance.HandleRingCollected(pointValue, transform.position);
        else
            Debug.LogError("[Ring] RingScoreManager.Instance is null!");

        if (collectVFXPrefab != null)
            Instantiate(collectVFXPrefab, transform.position, Quaternion.identity);

        if (collectSFX != null)
            AudioSource.PlayClipAtPoint(collectSFX, transform.position);

        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < collectFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectFlashDuration;

            float s = t < 0.4f
                ? Mathf.Lerp(startScale.x, 2.5f, t / 0.4f)
                : Mathf.Lerp(2.5f, 0f, (t - 0.4f) / 0.6f);

            transform.localScale = Vector3.one * s;
            yield return null;
        }

        Destroy(gameObject);
    }

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