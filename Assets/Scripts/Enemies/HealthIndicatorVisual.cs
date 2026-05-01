using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class HealthIndicatorVisual : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to auto-find in parent")]
    public MonoBehaviour healthSource; // must implement IHealthProvider

    private IHealthProvider _health;

    [Header("Colors")]
    public Color fullHealthColor = Color.green;
    public Color zeroHealthColor = Color.red;

    [Header("Settings")]
    [Tooltip("Smooth color transition")]
    public float colorLerpSpeed = 8f;

    private Renderer _renderer;
    private Material _materialInstance;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();

        // Important: make instance so you don't modify shared material
        _materialInstance = _renderer.material;

        if (healthSource == null)
        {
            _health = GetComponentInParent<IHealthProvider>();
        }
        else
        {
            _health = healthSource as IHealthProvider;
        }

        if (_health == null)
        {
            Debug.LogWarning($"{name}: No IHealthProvider found.", this);
        }
    }

    private void Update()
    {
        if (_health == null) return;

        float max = Mathf.Max(_health.MaxHealth, 0.0001f);
        float t = Mathf.Clamp01(_health.CurrentHealth / max);

        // Lerp from red (0) → green (1)
        Color targetColor = Color.Lerp(zeroHealthColor, fullHealthColor, t);

        // Smooth transition
        _materialInstance.color = Color.Lerp(
            _materialInstance.color,
            targetColor,
            Time.deltaTime * colorLerpSpeed
        );
    }
}