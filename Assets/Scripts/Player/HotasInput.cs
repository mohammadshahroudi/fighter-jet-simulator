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
    [SerializeField] [Range(0f, 1f)] private float stickDeadzone = 0.1f;

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
        // T.Flight HOTAS One X
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Roll, "<Joystick>/stick/x", RuntimeInputBindingUtility.BuildAxisProcessors(true, rollSensitivity, stickDeadzone));
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Roll, "<Joystick>/x", RuntimeInputBindingUtility.BuildAxisProcessors(true, rollSensitivity, stickDeadzone));
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Pitch, "<Joystick>/stick/y", RuntimeInputBindingUtility.BuildAxisProcessors(false, pitchSensitivity, stickDeadzone));
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Pitch, "<Joystick>/y", RuntimeInputBindingUtility.BuildAxisProcessors(false, pitchSensitivity, stickDeadzone));
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Yaw, "<Joystick>/twist", RuntimeInputBindingUtility.BuildAxisProcessors(false, yawSensitivity, stickDeadzone));
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Yaw, "<Joystick>/z", RuntimeInputBindingUtility.BuildAxisProcessors(false, yawSensitivity, stickDeadzone));
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Yaw, "<Joystick>/rz", RuntimeInputBindingUtility.BuildAxisProcessors(false, yawSensitivity, stickDeadzone));

        // Boost button.
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Boost, "<Joystick>/button2");
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.TogglePause, "<Joystick>/button14");

        // Throttle is handled by HotasThrottle using the physical throttle axis.

        actions.Player.Enable();
    }
}
