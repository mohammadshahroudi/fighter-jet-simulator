using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    private PlayerInput inputActions;
    public PlayerInput InputActions => inputActions;

    private void Awake()
    {
        inputActions = new PlayerInput();
        inputActions.Player.Enable();
    }

    private void OnDestroy()
    {
        inputActions.Player.Disable();
    }


    // --- New flight axes ---
    public float GetRoll()  => inputActions.Player.Roll.ReadValue<float>();
    public float GetPitch() => inputActions.Player.Pitch.ReadValue<float>();
    public float GetYaw()   => inputActions.Player.Yaw.ReadValue<float>();

    public bool GetThrottleUp()   => inputActions.Player.ThrottleUp.IsPressed();
    public bool GetThrottleDown() => inputActions.Player.ThrottleDown.IsPressed();

    public bool GetBoost() => inputActions.Player.Boost.IsPressed();



    
}