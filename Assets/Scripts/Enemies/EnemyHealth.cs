using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Per-unit health component. Initialised by FormationSpawner
/// but can also be set manually in the Inspector.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable, IHealthProvider
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 20;
    [SerializeField] private int currentHealth;

    [Header("Events")]
    public UnityEvent<int> onHealthChanged;
    public UnityEvent onDeath;

    [Header("Death Behavior")]
    [SerializeField] private bool triggerVictoryOnDeath = false;

    [Header("Hitbox (Optional)")]
    [SerializeField] private bool forceRootHitbox = false;
    [SerializeField] private bool autoFitRootHitboxToRenderers = true;
    [SerializeField] private Vector3 rootHitboxCenter = Vector3.zero;
    [SerializeField] private float rootHitboxRadius = 250f;

    [Header("Health Color Visual")]
    [Tooltip("Renderer on the sphere visual that should change color with health. Leave empty to auto-find.")]
    [SerializeField] private Renderer healthVisualRenderer;
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color midHealthColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float colorLerpSpeed = 8f;

    [Header("Low Health Pulse")]
    [Range(0f, 1f)]
    [SerializeField] private float lowHealthPulseThreshold = 0.35f;
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float pulseStrength = 0.35f;

    [Header("Damage Flash Overlay")]
    [SerializeField] private DamageFlashOverlay damageFlashOverlay;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    float IHealthProvider.CurrentHealth => currentHealth;
    float IHealthProvider.MaxHealth => maxHealth;

    public float HealthFraction => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    private Material _healthVisualMaterial;
    private bool _canTintVisual;

    private void Awake()
    {
        if (currentHealth == 0)
            currentHealth = maxHealth;

        if (damageFlashOverlay == null)
            damageFlashOverlay = GetComponent<DamageFlashOverlay>();

        EnsureRootHitbox();
        SetupHealthVisual();
        UpdateHealthVisual(true);
    }

    private void Update()
    {
        UpdateHealthVisual(false);
    }

    public void Initialise(int hp)
    {
        maxHealth = hp;
        currentHealth = hp;

        onHealthChanged?.Invoke(currentHealth);
        UpdateHealthVisual(true);
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(amount));

        damageFlashOverlay?.TriggerFlash();

        onHealthChanged?.Invoke(currentHealth);
        UpdateHealthVisual(true);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth);
        UpdateHealthVisual(true);
    }

    public void InstantKill() => TakeDamage(currentHealth);

    private void SetupHealthVisual()
    {
        if (healthVisualRenderer == null)
            healthVisualRenderer = FindBestHealthVisualRenderer();

        if (healthVisualRenderer == null)
        {
            Debug.LogWarning($"{name}: No health visual renderer assigned or found.", this);
            return;
        }

        _healthVisualMaterial = healthVisualRenderer.material;

        if (_healthVisualMaterial == null)
        {
            Debug.LogWarning($"{name}: Health visual renderer has no material.", healthVisualRenderer);
            return;
        }

        _canTintVisual =
            _healthVisualMaterial.HasProperty("_Color") ||
            _healthVisualMaterial.HasProperty("_BaseColor");

        if (!_canTintVisual)
        {
            Debug.LogWarning(
                $"{name}: Material '{_healthVisualMaterial.name}' has no _Color or _BaseColor property.",
                healthVisualRenderer
            );
        }
    }

    private Renderer FindBestHealthVisualRenderer()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
            return null;

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;

            string lowerName = r.gameObject.name.ToLower();
            if (lowerName.Contains("sphere") || lowerName.Contains("orb") || lowerName.Contains("core"))
                return r;
        }

        return renderers[0];
    }

    private void UpdateHealthVisual(bool instant)
    {
        if (_healthVisualMaterial == null || !_canTintVisual)
            return;

        float t = Mathf.Clamp01(HealthFraction);
        Color targetColor = EvaluateHealthColor(t);

        if (t <= lowHealthPulseThreshold && IsAlive)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            targetColor = Color.Lerp(targetColor, Color.white, pulse * pulseStrength);
        }

        if (instant)
        {
            ApplyMaterialColor(targetColor);
        }
        else
        {
            Color currentColor = GetCurrentMaterialColor();
            Color nextColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorLerpSpeed);
            ApplyMaterialColor(nextColor);
        }
    }

    private Color EvaluateHealthColor(float t)
    {
        if (t > 0.5f)
        {
            float localT = (t - 0.5f) * 2f;
            return Color.Lerp(midHealthColor, fullHealthColor, localT);
        }
        else
        {
            float localT = t * 2f;
            return Color.Lerp(lowHealthColor, midHealthColor, localT);
        }
    }

    private Color GetCurrentMaterialColor()
    {
        if (_healthVisualMaterial.HasProperty("_BaseColor"))
            return _healthVisualMaterial.GetColor("_BaseColor");

        if (_healthVisualMaterial.HasProperty("_Color"))
            return _healthVisualMaterial.GetColor("_Color");

        return Color.white;
    }

    private void ApplyMaterialColor(Color color)
    {
        if (_healthVisualMaterial.HasProperty("_BaseColor"))
            _healthVisualMaterial.SetColor("_BaseColor", color);

        if (_healthVisualMaterial.HasProperty("_Color"))
            _healthVisualMaterial.SetColor("_Color", color);
    }

    private void EnsureRootHitbox()
    {
        if (!forceRootHitbox) return;

        SphereCollider rootCollider = GetComponent<SphereCollider>();
        if (rootCollider == null)
        {
            rootCollider = gameObject.AddComponent<SphereCollider>();
        }

        rootCollider.enabled = true;
        rootCollider.isTrigger = false;

        if (autoFitRootHitboxToRenderers)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }

                Vector3 localCenter = transform.InverseTransformPoint(combinedBounds.center);
                float radius = Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.y, combinedBounds.extents.z) * 1.05f;

                rootCollider.center = localCenter;
                rootCollider.radius = Mathf.Max(0.1f, radius);
                return;
            }
        }

        rootCollider.center = rootHitboxCenter;
        rootCollider.radius = Mathf.Max(0.1f, rootHitboxRadius);
    }

    private void Die()
    {
        if (triggerVictoryOnDeath)
        {
            GameStateManager.Instance?.TriggerVictory();
        }

        onDeath?.Invoke();
        Destroy(gameObject);
    }
}