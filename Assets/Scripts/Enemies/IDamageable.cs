/// <summary>
/// IDamageable — implement this on any object that can take damage
/// (enemy jets, the player, ground targets, etc.)
///
/// Usage:
///   public class EnemyHealth : MonoBehaviour, IDamageable
///   {
///       public void TakeDamage(float amount) { ... }
///   }
///
/// The Missile script calls TakeDamage via this interface automatically.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}