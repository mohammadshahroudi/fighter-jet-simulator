using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Plane Stats")]

    [SerializeField] private float speed = 10f;
    [SerializeField] private float throttleIncrement = 0.1f;
    [SerializeField] private float maxThrottle = 200f;
    [SerializeField] private float responsiveness = 10f;
    [SerializeField] private float responseModifierValue = 10f;
    [SerializeField] private float inputDecaySpeed = 2f; // How fast input fades to 0

    [Header("References")]
    [SerializeField] private GameInput gameInput;

    private Rigidbody rb;
    private float throttle;
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
        float rawRoll  = gameInput.GetRoll();
        float rawPitch = gameInput.GetPitch();
        float rawYaw   = gameInput.GetYaw();

        // If the player is providing input, snap to it.
        // Otherwise, smoothly decay back to 0. 
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
        float responseModifier = (rb.mass / responseModifierValue) * Time.fixedDeltaTime;
        float thrustForce = (throttle / 100f) * maxThrottle;

        rb.AddRelativeForce(Vector3.forward * thrustForce);

        rb.AddRelativeTorque(new Vector3(
            pitch * responsiveness * responseModifier,
            yaw   * responsiveness * responseModifier,
            roll  * responsiveness * responseModifier
        ));
    }
}