using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(210)]
public class HotasInputBindingInstaller : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private GameInput targetGameInput;

    [Header("Sensitivity Settings")]
    [SerializeField] [Range(0f, 2f)] private float rollSensitivity = 0.7f;
    [SerializeField] [Range(0f, 2f)] private float pitchSensitivity = 1f;
    [SerializeField] [Range(0f, 2f)] private float yawSensitivity = 1f;
    [SerializeField] [Range(0f, 1f)] private float rollDeadzone = 0f;
    [SerializeField] [Range(0f, 1f)] private float pitchDeadzone = 0f;
    [SerializeField] [Range(0f, 1f)] private float yawDeadzone = 0f;
    [SerializeField] private bool enableLegacyAxisFallbacks = false;
    [SerializeField] private bool includeYawAxisFallbacks = true;

    private void Start()
    {
        if (targetGameInput == null)
        {
            targetGameInput = FindFirstObjectByType<GameInput>();
        }

        if (targetGameInput == null)
        {
            Debug.LogError("GameInput not found!");
            return;
        }

        InstallHotasBindings(targetGameInput.InputActions);
    }

    private void InstallHotasBindings(PlayerInput actions)
    {
        // T.Flight HOTAS One X primary axes.
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Roll, "<Joystick>/stick/x", RuntimeInputBindingUtility.BuildAxisProcessors(true, rollSensitivity, rollDeadzone));
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Pitch, "<Joystick>/stick/y", RuntimeInputBindingUtility.BuildAxisProcessors(false, pitchSensitivity, pitchDeadzone));
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Pitch, "<Joystick>/y", RuntimeInputBindingUtility.BuildAxisProcessors(false, pitchSensitivity, pitchDeadzone));
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Yaw, "<Joystick>/stick/x", RuntimeInputBindingUtility.BuildAxisProcessors(true, yawSensitivity, yawDeadzone));

        if (includeYawAxisFallbacks)
        {
            RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Yaw, "<Joystick>/z", RuntimeInputBindingUtility.BuildAxisProcessors(false, yawSensitivity, yawDeadzone));
            RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Yaw, "<Joystick>/rz", RuntimeInputBindingUtility.BuildAxisProcessors(false, yawSensitivity, yawDeadzone));
        }

        if (enableLegacyAxisFallbacks)
        {
            RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Roll, "<Joystick>/z", RuntimeInputBindingUtility.BuildAxisProcessors(false, rollSensitivity, rollDeadzone));
            RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Roll, "<Joystick>/rz", RuntimeInputBindingUtility.BuildAxisProcessors(false, rollSensitivity, rollDeadzone));
            RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Yaw, "<Joystick>/x", RuntimeInputBindingUtility.BuildAxisProcessors(true, yawSensitivity, yawDeadzone));
            RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Yaw, "<Joystick>/z", RuntimeInputBindingUtility.BuildAxisProcessors(false, yawSensitivity, yawDeadzone));
            RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Yaw, "<Joystick>/rz", RuntimeInputBindingUtility.BuildAxisProcessors(false, yawSensitivity, yawDeadzone));
        }

        // T.Flight HOTAS One X button mapping.
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.ThrottleDown, "<Joystick>/button3");
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.ThrottleUp, "<Joystick>/button4");
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Boost, "<Joystick>/button2");
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.TogglePause, "<Joystick>/button14");

        // Throttle is handled by HotasThrottle using the physical throttle axis.

        actions.Player.Enable();
    }
}
