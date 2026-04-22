using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Plane Stats")]
    [SerializeField] private float throttleIncrement   = 100f;
    [SerializeField] private float maxThrottle         = 800f;
    [SerializeField] private float responsiveness      = 10f;
    [SerializeField] private float responseModifierValue = 10f;
    [SerializeField] private float inputDecaySpeed     = 50f;

    [Header("References")]
    [SerializeField] private Gameinput gameInput;

    private Rigidbody rb;
    private float throttle = 0f;   // Fixed: was initialised to 300, clamped to 100
    private float roll;
    private float pitch;
    private float yaw;

    public static PlayerController Instance { get; private set; }

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
    }

    private void Update()
    {
        HandleInputs();
    }

    private void HandleInputs()
    {
        float rawRoll  = gameInput.GetRoll()/3;
        float rawPitch = gameInput.GetPitch();
        float rawYaw   = gameInput.GetYaw();

        roll  = rawRoll  != 0 ? rawRoll  : Mathf.Lerp(roll,  0f, inputDecaySpeed * Time.deltaTime);
        pitch = rawPitch != 0 ? rawPitch : Mathf.Lerp(pitch, 0f, inputDecaySpeed * Time.deltaTime);
        yaw   = rawYaw   != 0 ? rawYaw   : Mathf.Lerp(yaw,   0f, inputDecaySpeed * Time.deltaTime);

        if (gameInput.GetThrottleUp())
            throttle = Mathf.Clamp(throttle + throttleIncrement, 0f, 100f);
        else if (gameInput.GetThrottleDown())
            throttle = Mathf.Clamp(throttle - throttleIncrement, 0f, 100f);
    }

    private void FixedUpdate()
    {
        float responseModifier = rb.mass / responseModifierValue;
        float thrustForce      = (throttle / 100f) * maxThrottle;

        rb.AddRelativeForce(Vector3.forward * thrustForce);

        rb.AddRelativeTorque(new Vector3(
            pitch * responsiveness * responseModifier,
            yaw   * responsiveness * responseModifier,
            roll  * responsiveness * responseModifier
        ));

        // Prevent infinite acceleration — clamp to max speed
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxThrottle);
    }
}