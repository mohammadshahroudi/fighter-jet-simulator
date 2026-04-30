using System.Collections;
using UnityEngine;

namespace Gun
{
    public class BossFlight : MonoBehaviour, IDamageable
    //After 5 minutes the boss battle will trigger
    //UFO Boss prefab will start disabled, then when 5 minutes pass it will fly down from the sky
    //The UFO Boss will spawn small ufo enemies that will chase the player and do damage upon impact.
    //The ufo itself does not do any damage and if the player crashes into it nothing will happen but it should have collision.
    //after spawning a wave of enemies to chase the player the ufo boss will hang around for about a minute and dissapear but
    //its health will not change and it will come back after 30 seconds away in a different position equally distant in front of the player
    //this is intended to be the final boss battle so when the player wins it will say "you won" and give them an option to play again,
    //go to the shop, or the main menu, we might even just be able to use an existing menu for this
    //The UFO only moves up into the sky really quickly and back down in a different position, it doesnt actually move while in the scene
    //The UFO will make noises in the scene, especially when it warps up and down from the sky
    
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform enemySpawnPoint;

        [Header("Movement Settings")] 
        [SerializeField] private float movementSpeed = 10f;

        [Header("Health")]
        [SerializeField] private int maxHealth = 500;
        private int currentHealth;
        private bool isAlive = true;

        private bool IsFacingPlayer(float angleThreshold)
        {
            if (!player) return true;
            Vector3 toPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toPlayer);
            return angle <= angleThreshold;
        }
        
        
        private float DistanceToPlayer()
        {
            if (!player) return float.MaxValue;
            return Vector3.Distance(transform.position, player.position);
        }

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (!isAlive) return;

            currentHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(amount));

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (!isAlive) return;
            isAlive = false;

            ShowVictoryMessage();
            Destroy(gameObject, 0.5f);
        }

        private void ShowVictoryMessage()
        {
            Debug.Log("You Saved The World!");
            GameStateManager.Instance?.TriggerVictory();
        }
    }
}