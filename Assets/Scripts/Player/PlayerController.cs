using UnityEngine;
using UnityEngine.Events;

public class PlayerController : MonoBehaviour
{
    [Header("Plane Stats")]
    [SerializeField] private float throttleIncrement     = 100f;
    [SerializeField] private float throttleReturnSpeed   = 50f;
    [SerializeField] private float maxThrottle           = 800f;
    [SerializeField] private float maxSpeed              = 120f;
    [SerializeField] private float minSpeed              = 10f;
    [SerializeField] private float normalThrottle        = 10f;
    [SerializeField] private float responsiveness        = 10f;
    [SerializeField] private float pitchMultiplier       = 1.3f;
    [SerializeField] private float yawMultiplier         = 1.3f;
    [SerializeField] private float responseModifierValue = 10f;
    [SerializeField] private float inputDecaySpeed       = 50f;

    public static PlayerController Instance { get; private set; }

    [Header("Crash")]
    [Tooltip("Tags that trigger a crash when collided with. Leave empty to crash on anything.")]
    [SerializeField] private string[] crashTags = { "Terrain", "Building", "Ground" };

    [Tooltip("Optional crash VFX spawned at the impact point.")]
    [SerializeField] private GameObject crashVFXPrefab;

    [Header("References")]
    [SerializeField] private GameInput gameInput;

    [Header("Boost")]
    [Tooltip("Multiplier applied to forward thrust while Boost is held (Space)")]
    [SerializeField] private float boostMultiplier = 2f;
    [Tooltip("How many seconds of boost the player has at full charge")]
    [SerializeField] private float maxBoostSeconds = 5f;
    [Tooltip("How many boost seconds are consumed per second while boosting")]
    [SerializeField] private float boostDrainPerSecond = 1f;
    [Tooltip("How many boost seconds are recovered per second after recharge starts")]
    [SerializeField] private float boostRechargePerSecond = 1f;
    [Tooltip("How long after boost stops before recharge begins")]
    [SerializeField] private float boostRechargeDelaySeconds = 0.5f;

    [Header("Events")]
    public UnityEvent<float> onStaminaChanged;  // passes normalized stamina (0-1)

    private Rigidbody rb;
    private float currentBoostSeconds;
    private float timeSinceBoostEnded = 0f;
    private float throttle = 10f;
    private float roll;
    private float pitch;
    private float yaw;
    private bool  hasCrashed = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("More than one PlayerController instance found!");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        currentBoostSeconds = maxBoostSeconds;
        timeSinceBoostEnded = boostRechargeDelaySeconds;
    }

    private void Update()
    {
        if (hasCrashed) return;
        HandleInputs();
    }

    private void HandleInputs()
    {
        float rawRoll  = gameInput.GetRoll()  / 3;
        float rawPitch = gameInput.GetPitch() / 3;
        float rawYaw   = gameInput.GetYaw()   / 3;

        float decay = Mathf.Exp(-inputDecaySpeed * Time.deltaTime);

        roll  = rawRoll  != 0 ? rawRoll  : roll  * decay;
        pitch = rawPitch != 0 ? rawPitch : pitch * decay;
        yaw   = rawYaw   != 0 ? rawYaw   : yaw   * decay;

        if (gameInput.GetThrottleUp())
            throttle = Mathf.Clamp(throttle + throttleIncrement, minSpeed, maxSpeed);
        else if (gameInput.GetThrottleDown())
            throttle = Mathf.Clamp(throttle - throttleIncrement, minSpeed, maxSpeed);
        else
            throttle = Mathf.MoveTowards(throttle, normalThrottle, throttleReturnSpeed * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (hasCrashed) return;

        float responseModifier = rb.mass / responseModifierValue;
        float thrustForce      = (throttle / 100f) * maxThrottle;
        bool isBoosting = gameInput != null && gameInput.GetBoost() && currentBoostSeconds > 0f;
        
        if (isBoosting)
        {
            thrustForce *= boostMultiplier;
            currentBoostSeconds = Mathf.Max(0f, currentBoostSeconds - boostDrainPerSecond * Time.deltaTime);
            timeSinceBoostEnded = 0f;
            onStaminaChanged?.Invoke(currentBoostSeconds / Mathf.Max(0.0001f, maxBoostSeconds));
        }
        else
        {
            timeSinceBoostEnded += Time.deltaTime;
            if (timeSinceBoostEnded >= boostRechargeDelaySeconds)
            {
                currentBoostSeconds = Mathf.Min(maxBoostSeconds, currentBoostSeconds + boostRechargePerSecond * Time.deltaTime);
                onStaminaChanged?.Invoke(currentBoostSeconds / Mathf.Max(0.0001f, maxBoostSeconds));
            }
        }

        rb.AddRelativeForce(Vector3.forward * thrustForce);

        rb.AddRelativeTorque(new Vector3(
            pitch * responsiveness * responseModifier * pitchMultiplier,
            yaw   * responsiveness * responseModifier * yawMultiplier,
            roll  * responsiveness * responseModifier
        ));

        float velocityCap = isBoosting ? maxThrottle * boostMultiplier : maxThrottle;
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, velocityCap);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCrashed) return;

        if (crashTags.Length == 0)
        {
            PlaneCrash(collision.contacts[0].point);
            return;
        }

        foreach (string tag in crashTags)
        {
            if (collision.gameObject.CompareTag(tag))
            {
                PlaneCrash(collision.contacts[0].point);
                return;
            }
        }
    }

    private void PlaneCrash(Vector3 impactPoint)
    {
        hasCrashed = true;

        if (crashVFXPrefab != null)
            Instantiate(crashVFXPrefab, impactPoint, Quaternion.identity);

        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        GameStateManager.Instance?.TriggerGameOver();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PowerUp"))
        {
            other.gameObject.SetActive(false);
        }
    }
    public void InitialiseSpeed(int baseSpeed)
{
    maxSpeed = baseSpeed;
}
}