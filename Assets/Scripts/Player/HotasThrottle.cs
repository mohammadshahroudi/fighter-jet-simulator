using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class HotasThrottle : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private PlayerController playerController;

    [Header("Axis Mapping")]
    [SerializeField] private string[] throttleAxisCandidates = { "throttle", "slider", "rz", "z" };
    [SerializeField] private bool invertThrottleAxis = true;
    [SerializeField] [Range(0f, 1f)] private float axisChangeThreshold = 0.01f;

    [Header("Throttle Range")]
    [SerializeField] private float minThrottle = 10f;
    [SerializeField] private float maxThrottle = 120f;
    [SerializeField] [Range(1f, 40f)] private float responseSpeed = 12f;

    private FieldInfo throttleField;
    private string resolvedAxisName;
    private float lastAxisValue;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (playerController != null)
        {
            throttleField = typeof(PlayerController).GetField("throttle", BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }

    private void Start()
    {
        ResolveAxisName();
    }

    private void Update()
    {
        if (playerController == null || throttleField == null || Joystick.current == null) return;

        float axis = ReadThrottleAxis();
        if (Mathf.Abs(axis - lastAxisValue) < axisChangeThreshold) return;

        float normalized = NormalizeThrottleAxis(axis);
        float targetThrottle = Mathf.Lerp(minThrottle, maxThrottle, normalized);

        float current = (float)throttleField.GetValue(playerController);
        float smoothed = Mathf.Lerp(current, targetThrottle, 1f - Mathf.Exp(-responseSpeed * Time.deltaTime));

        throttleField.SetValue(playerController, Mathf.Clamp(smoothed, minThrottle, maxThrottle));
        lastAxisValue = axis;
    }

    private void ResolveAxisName()
    {
        Joystick joystick = Joystick.current;
        if (joystick == null || throttleAxisCandidates == null) return;

        for (int i = 0; i < throttleAxisCandidates.Length; i++)
        {
            string candidate = throttleAxisCandidates[i];
            if (string.IsNullOrWhiteSpace(candidate)) continue;

            AxisControl axis = joystick.TryGetChildControl<AxisControl>(candidate);
            if (axis != null)
            {
                resolvedAxisName = candidate;
                return;
            }
        }
    }

    private float ReadThrottleAxis()
    {
        Joystick joystick = Joystick.current;
        if (joystick == null) return 0f;

        if (string.IsNullOrWhiteSpace(resolvedAxisName))
        {
            ResolveAxisName();
            if (string.IsNullOrWhiteSpace(resolvedAxisName)) return 0f;
        }

        AxisControl axis = joystick.TryGetChildControl<AxisControl>(resolvedAxisName);
        if (axis == null) return 0f;

        float value = axis.ReadValue();
        return invertThrottleAxis ? -value : value;
    }

    private static float NormalizeThrottleAxis(float axis)
    {
        // Some devices report 0..1 throttle while others use -1..1.
        if (axis >= 0f && axis <= 1f)
        {
            return axis;
        }

        return Mathf.InverseLerp(-1f, 1f, axis);
    }
}
