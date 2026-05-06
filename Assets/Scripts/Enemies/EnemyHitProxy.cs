using UnityEngine;

/// <summary>
/// EnemyHitProxy
///
/// Attach to every child GameObject of a compound enemy prefab (e.g. UFO Boss 1 → Allen,
/// Capsule, Cylinder, Sphere). When a raycast or projectile hits a child collider, this
/// script walks up the hierarchy and forwards the call to the root's IDamageable and
/// EnemyFighterAI components.
///
/// You never need to add this manually — EnemyFighterAI.Awake() auto-installs it on all
/// child objects that have a Collider but no EnemyHitProxy yet.
///
/// Why this pattern instead of GetComponentInParent in the weapon script?
///   Your weapon already calls hit.collider.GetComponentInParent<IDamageable>(), which
///   works fine. This proxy exists for the OnCollisionEnter / OnTriggerEnter path (e.g.
///   projectiles that use physics rather than raycasts) where the message lands on the
///   child MonoBehaviour, not on the root.
/// </summary>
[DisallowMultipleComponent]
public class EnemyHitProxy : MonoBehaviour
{
    // Resolved once in Start — avoids repeated GetComponentInParent calls per hit.
    private IDamageable   _damageable;
    private EnemyFighterAI _ai;

    void Start()
    {
        _damageable = GetComponentInParent<IDamageable>();
        _ai         = GetComponentInParent<EnemyFighterAI>();

        if (_damageable == null)
            Debug.LogWarning($"[EnemyHitProxy] {name}: no IDamageable found in parent hierarchy.");
    }

    // -------------------------------------------------------------------------
    // Physics projectile path (non-trigger colliders)
    // -------------------------------------------------------------------------

    void OnCollisionEnter(Collision col)
    {
        ForwardHit(col.gameObject);
    }

    // -------------------------------------------------------------------------
    // Trigger projectile path (isTrigger colliders)
    // -------------------------------------------------------------------------

    void OnTriggerEnter(Collider other)
    {
        ForwardHit(other.gameObject);
    }

    // -------------------------------------------------------------------------
    // Public entry point — weapon scripts can call this directly if preferred
    // -------------------------------------------------------------------------

    /// <summary>
    /// Forward damage from any source (raycast, projectile, explosion) to the root.
    /// Safe to call even if the root has already been destroyed.
    /// </summary>
    public void ReceiveDamage(float amount)
    {
        _damageable?.TakeDamage(amount);
        // AI reacts via CheckDamagedPhase() in Update — no separate OnHit needed
    }

    // -------------------------------------------------------------------------
    // Private
    // -------------------------------------------------------------------------

    void ForwardHit(GameObject instigator)
    {
        // Ignore self-hits (e.g. the enemy's own projectiles)
        if (instigator != null && instigator.GetComponentInParent<EnemyFighterAI>() == _ai)
            return;

        _damageable?.TakeDamage(10f);  // default collision damage — override via ReceiveDamage()
        // AI reacts via CheckDamagedPhase() in Update — no separate OnHit needed
    }
}