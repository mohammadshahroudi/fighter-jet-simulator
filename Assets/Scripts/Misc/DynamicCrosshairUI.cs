using UnityEngine;
using UnityEngine.UI;

public class DynamicCrosshairUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunAutoLock gunAutoLock;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Camera worldCamera;

    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color targetColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float colorTransitionSpeed = 10f;

    [Header("Movement")]
    [SerializeField] private float followDecay = 18f;
    [SerializeField] private float returnDecay = 8f;
    [SerializeField] private float maxScreenOffset = 450f;

    [Header("Prediction")]
    [SerializeField] private float predictionTime = 0.15f;
    [SerializeField] private float maxPredictionOffset = 120f;
    [SerializeField] private float targetSmoothSpeed = 18f;

    private RectTransform crosshairRect;
    private Color currentColor;

    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private bool hasLastTargetPosition;

    private Vector2 smoothedUIPosition;
    private bool hasSmoothedTarget;

    void Awake()
    {
        FindRuntimeReferences();

        currentColor = defaultColor;

        if (crosshairImage != null)
            crosshairImage.color = currentColor;
    }

    void Update()
    {
        FindRuntimeReferences();

        Transform target = gunAutoLock != null ? gunAutoLock.CurrentTarget : null;

        UpdateTargetVelocity(target);
        UpdateCrosshairPosition(target);
        UpdateColor(target != null);
    }

    void FindRuntimeReferences()
    {
        if (gunAutoLock == null)
            gunAutoLock = FindFirstObjectByType<GunAutoLock>();

        if (crosshairImage == null)
            crosshairImage = GetComponent<Image>();

        if (crosshairImage != null && crosshairRect == null)
            crosshairRect = crosshairImage.GetComponent<RectTransform>();

        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    void UpdateTargetVelocity(Transform target)
    {
        if (target == null)
        {
            hasLastTargetPosition = false;
            targetVelocity = Vector3.zero;
            return;
        }

        if (!hasLastTargetPosition)
        {
            lastTargetPosition = target.position;
            hasLastTargetPosition = true;
            return;
        }

        targetVelocity = (target.position - lastTargetPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastTargetPosition = target.position;
    }

    void UpdateCrosshairPosition(Transform target)
    {
        if (crosshairRect == null || worldCamera == null)
            return;

        Vector2 desiredPosition = Vector2.zero;

        if (target != null)
        {
            Vector3 currentScreenPos = worldCamera.WorldToScreenPoint(target.position);
            Vector3 predictedWorldPos = target.position + targetVelocity * predictionTime;
            Vector3 predictedScreenPos = worldCamera.WorldToScreenPoint(predictedWorldPos);

            if (currentScreenPos.z > 0f && predictedScreenPos.z > 0f)
            {
                Vector2 screenCenter = new Vector2(
                    Screen.width * 0.5f,
                    Screen.height * 0.5f
                );

                Vector2 currentOffset = (Vector2)currentScreenPos - screenCenter;
                Vector2 predictedOffset = (Vector2)predictedScreenPos - screenCenter;

                Vector2 predictionDelta = predictedOffset - currentOffset;
                predictionDelta = Vector2.ClampMagnitude(predictionDelta, maxPredictionOffset);

                Vector2 rawPosition = currentOffset + predictionDelta;
                rawPosition = Vector2.ClampMagnitude(rawPosition, maxScreenOffset);

                if (!hasSmoothedTarget)
                {
                    smoothedUIPosition = rawPosition;
                    hasSmoothedTarget = true;
                }
                else
                {
                    smoothedUIPosition = Vector2.Lerp(
                        smoothedUIPosition,
                        rawPosition,
                        Time.deltaTime * targetSmoothSpeed
                    );
                }

                desiredPosition = smoothedUIPosition;
            }
            else
            {
                hasSmoothedTarget = false;
            }
        }
        else
        {
            hasSmoothedTarget = false;
        }

        float decay = target != null ? followDecay : returnDecay;

        crosshairRect.anchoredPosition = Decay(
            crosshairRect.anchoredPosition,
            desiredPosition,
            decay
        );

        if (target == null &&
            Vector2.Distance(crosshairRect.anchoredPosition, Vector2.zero) < 0.1f)
        {
            crosshairRect.anchoredPosition = Vector2.zero;
        }
    }

    void UpdateColor(bool hasTarget)
    {
        if (crosshairImage == null)
            return;

        Color desiredColor = hasTarget ? targetColor : defaultColor;

        currentColor = Color.Lerp(
            currentColor,
            desiredColor,
            Time.deltaTime * colorTransitionSpeed
        );

        crosshairImage.color = currentColor;
    }

    Vector2 Decay(Vector2 current, Vector2 target, float decay)
    {
        return target + (current - target) * Mathf.Exp(-decay * Time.deltaTime);
    }
}