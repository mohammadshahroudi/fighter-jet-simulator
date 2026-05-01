using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Per-unit health component. Initialised by FormationSpawner
/// but can also be set manually in the Inspector.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    // ── Inspector Fields ─────────────────────────────────────────────────────

    [Header("Health")]
    [SerializeField] private int maxHealth = 20;
    [SerializeField] private int currentHealth;

    [Header("Events")]
    public UnityEvent<int> onHealthChanged; // passes current HP
    public UnityEvent       onDeath;

    [Header("Death Behavior")]
    [SerializeField] private bool triggerVictoryOnDeath = false;

    [Header("Hitbox (Optional)")]
    [SerializeField] private bool forceRootHitbox = false;
    [SerializeField] private bool autoFitRootHitboxToRenderers = true;
    [SerializeField] private Vector3 rootHitboxCenter = Vector3.zero;
    [SerializeField] private float rootHitboxRadius = 250f;

    // ── Properties ───────────────────────────────────────────────────────────

    public int CurrentHealth => currentHealth;
    public int MaxHealth      => maxHealth;
    public bool IsAlive       => currentHealth > 0;

    /// <summary>0–1 normalised health fraction, useful for health bars.</summary>
    public float HealthFraction => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    // ── Unity Callbacks ──────────────────────────────────────────────────────

    private void Awake()
    {
        // Only set defaults when NOT initialised by the spawner
        // (Initialise overrides these values).
        if (currentHealth == 0)
            currentHealth = maxHealth;

        EnsureRootHitbox();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Called by FormationSpawner to set this unit's starting health.
    /// Overwrites any Inspector defaults.
    /// </summary>
    public void Initialise(int hp)
    {
        maxHealth     = hp;
        currentHealth = hp;
    }

    /// <summary>Apply damage. Triggers death at zero HP.</summary>
    public  void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(amount));
        onHealthChanged.Invoke(currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>Restore HP, clamped to maxHealth.</summary>
    public void Heal(int amount)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged.Invoke(currentHealth);
    }

    /// <summary>Instantly kill this unit.</summary>
    public void InstantKill() => TakeDamage((float)currentHealth);

    // ── Private ──────────────────────────────────────────────────────────────

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

        onDeath.Invoke();
        // Replace this with your death VFX / animation logic as needed.
        Destroy(gameObject);
    }
}
