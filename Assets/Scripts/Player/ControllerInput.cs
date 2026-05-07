using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(200)]
public class GamepadInputBindingInstaller : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private GameInput targetGameInput;

    [Header("Sensitivity Settings")]
    [SerializeField] [Range(0f, 2f)] private float rollSensitivity = 0.7f;
    [SerializeField] [Range(0f, 2f)] private float pitchSensitivity = 1f;
    [SerializeField] [Range(0f, 2f)] private float yawSensitivity = 1f;
    [SerializeField] [Range(0f, 1f)] private float stickDeadzone = 0.15f;

    private void Start()
    {
        if (targetGameInput == null)
        {
            targetGameInput = FindFirstObjectByType<GameInput>();
        }

        if (targetGameInput == null)
        {
            Debug.LogWarning("GamepadInputBindingInstaller: No GameInput found in scene.");
            return;
        }

        if (!RuntimeInputBindingUtility.TryGetRuntimeActions(targetGameInput, out PlayerInput runtimeActions))
        {
            Debug.LogWarning("GamepadInputBindingInstaller: Could not get runtime PlayerInput actions.");
            return;
        }

        InstallBindings(runtimeActions);
    }

    private void InstallBindings(PlayerInput actions)
    {
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Roll, "<Gamepad>/rightStick/x", RuntimeInputBindingUtility.BuildAxisProcessors(true, rollSensitivity, stickDeadzone));

        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Pitch, "<Gamepad>/leftStick/y", RuntimeInputBindingUtility.BuildAxisProcessors(false, pitchSensitivity, stickDeadzone));
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Yaw, "<Gamepad>/leftStick/x", RuntimeInputBindingUtility.BuildAxisProcessors(false, yawSensitivity, stickDeadzone));

        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.ThrottleUp, "<Gamepad>/dpad/up");
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.ThrottleUp, "<Gamepad>/rightShoulder");

        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.ThrottleDown, "<Gamepad>/dpad/down");
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.ThrottleDown, "<Gamepad>/leftShoulder");

        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.Boost, "<Gamepad>/buttonSouth");
        RuntimeInputBindingUtility.AddBindingIfMissing(actions.Player.TogglePause, "<Gamepad>/start");

        actions.Player.Enable();
    }
}
