using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Per-player health component. Handles damage, healing, and death.
/// </summary>
public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Events")]
    public UnityEvent<int> onHealthChanged; // passes current HP
    public UnityEvent onDeath;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public float HealthFraction => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    private void Awake()
    {
        if (currentHealth == 0)
            currentHealth = maxHealth;
    }

    /// <summary>
    /// Set this player's starting health (e.g., from a save or power-up).
    /// </summary>
    public void Initialise(int hp)
    {
        maxHealth = hp;
        currentHealth = hp;
        onHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>
    /// Apply damage. Triggers death at zero HP.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(amount));
        onHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Restore HP, clamped to maxHealth.
    /// </summary>
    public void Heal(int amount)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>
    /// Instantly kill the player.
    /// </summary>
    public void InstantKill() => TakeDamage(currentHealth);

    private void Die()
    {
        onDeath?.Invoke();
        GameStateManager.Instance?.TriggerGameOver();
        Destroy(gameObject);
    }

        /// <summary>
        /// Increase max health and current health by the given amount.
        /// </summary>
        public void IncreaseMaxHealth(int amount)
        {
            maxHealth += amount;
            currentHealth += amount;
            onHealthChanged?.Invoke(currentHealth);
        }
}