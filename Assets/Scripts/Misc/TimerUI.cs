using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TimerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KillTimer killTimer;
    [SerializeField] private Image barImage;

    [Header("Color Gradient")]
    [SerializeField] private Gradient timeGradient;

    [Header("Low Time Settings")]
    [SerializeField] private int lowTimeThreshold = 10;
    [SerializeField] private float colorShiftIntensity = 1.5f;

    [Header("Animation")]
    [SerializeField] private float smoothDampSpeed = 5f;

    private float currentFillVelocity;
    private Coroutine updateRoutine;

    private void Awake()
    {
        if (killTimer == null)
        {
            killTimer = FindFirstObjectByType<KillTimer>();
        }

        // Create a default gradient if none is assigned
        if (timeGradient == null)
        {
            timeGradient = new Gradient();
            var colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.green, 0f);
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.red, 1f);
            timeGradient.colorKeys = colorKeys;
        }
    }

    private void OnEnable()
    {
        if (updateRoutine != null)
            StopCoroutine(updateRoutine);
        updateRoutine = StartCoroutine(UpdateRoutine());
    }

    private void OnDisable()
    {
        if (updateRoutine != null)
        {
            StopCoroutine(updateRoutine);
            updateRoutine = null;
        }
    }

    private IEnumerator UpdateRoutine()
    {
        while (killTimer != null && barImage != null)
        {
            float targetFill = killTimer.MaxTimeSeconds > 0
                ? (float)killTimer.CurrentTimeSeconds / killTimer.MaxTimeSeconds
                : 0f;

            targetFill = Mathf.Clamp01(targetFill);

            // Smoothly damp the fill toward the actual timer value
            barImage.fillAmount = Mathf.SmoothDamp(
                barImage.fillAmount,
                targetFill,
                ref currentFillVelocity,
                1f / smoothDampSpeed
            );

            // Update color based on current timer
            Color barColor = timeGradient.Evaluate(1f - targetFill);

            if (killTimer.CurrentTimeSeconds <= lowTimeThreshold && killTimer.CurrentTimeSeconds > 0)
            {
                barColor = Color.LerpUnclamped(barColor, Color.red, (colorShiftIntensity - 1f) * 0.5f);
            }

            barImage.color = barColor;

            yield return null;
        }
    }
}


