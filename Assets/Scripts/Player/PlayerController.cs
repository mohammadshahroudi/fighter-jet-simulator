using UnityEngine;

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

    private Rigidbody rb;
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
        // rb.linearVelocity = new Vector3( 0,0,minSpeed);
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
        bool isBoosting = gameInput != null && gameInput.GetBoost();
        if (isBoosting)
        {
            thrustForce *= boostMultiplier;
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