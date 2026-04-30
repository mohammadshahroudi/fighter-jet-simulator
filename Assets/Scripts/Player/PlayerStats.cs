using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    public float health = 100f;

    public void TakeDamage(float amount)
    {
        health = Mathf.Max(0f, health - amount);
        Debug.Log($"Player took {amount} damage. Remaining health: {health}");

        if (health <= 0f)
        {
            GameStateManager.Instance?.TriggerGameOver();
            Destroy(gameObject);
        }
    }
}