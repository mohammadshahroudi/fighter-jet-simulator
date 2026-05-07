using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public static class RuntimeInputBindingUtility
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    public static bool TryGetRuntimeActions(GameInput gameInput, out PlayerInput runtimeActions)
    {
        runtimeActions = null;
        if (gameInput == null) return false;

        FieldInfo inputActionsField = typeof(GameInput).GetField("inputActions", PrivateInstance);
        if (inputActionsField == null) return false;

        runtimeActions = inputActionsField.GetValue(gameInput) as PlayerInput;
        return runtimeActions != null;
    }

    public static string BuildAxisProcessors(bool invert, float sensitivity, float deadzone)
    {
        string invertPart = invert ? "Invert," : string.Empty;
        string deadzonePart = deadzone > 0f ? $",Deadzone(min={Mathf.Clamp01(deadzone)})" : string.Empty;
        return $"{invertPart}Scale(factor={Mathf.Max(0f, sensitivity)}){deadzonePart}";
    }

    public static void AddBindingIfMissing(InputAction action, string path, string processor = null)
    {
        if (action == null || string.IsNullOrWhiteSpace(path)) return;

        foreach (InputBinding binding in action.bindings)
        {
            if (binding.path == path)
            {
                return;
            }
        }

        InputBinding newBinding = new InputBinding { path = path };
        if (!string.IsNullOrEmpty(processor))
        {
            newBinding.processors = processor;
        }

        action.AddBinding(newBinding);
    }
}
