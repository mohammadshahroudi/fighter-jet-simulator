using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Per-unit health component. Initialised by FormationSpawner
/// but can also be set manually in the Inspector.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    // ── Inspector Fields ─────────────────────────────────────────────────────

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Events")]
    public UnityEvent<int> onHealthChanged; // passes current HP
    public UnityEvent       onDeath;

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
    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
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
    public void InstantKill() => TakeDamage(currentHealth);

    // ── Private ──────────────────────────────────────────────────────────────

    private void Die()
    {
        onDeath.Invoke();
        // Replace this with your death VFX / animation logic as needed.
        Destroy(gameObject);
    }
}
