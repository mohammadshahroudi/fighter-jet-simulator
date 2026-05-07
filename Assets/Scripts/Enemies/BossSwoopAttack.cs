using UnityEngine;

public class BossSwoopAttack : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 80f;
    public float turnSpeed = 8f;

    [Header("Attack")]
    public float hitDistance = 8f;
    public float damageAmount = 25f;

    private Rigidbody bossRb;
    private bool hasHit;

    void Awake()
    {
        bossRb = GetComponentInChildren<Rigidbody>();

        if (bossRb != null)
        {
            bossRb.useGravity = false;
            bossRb.isKinematic = false;
        }
    }

    void Start()
    {
        FindPlayer();
    }

    void FixedUpdate()
    {
        FindPlayer();

        if (player == null || bossRb == null)
            return;

        MoveTowardPlayer();
        CheckHit();
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
    }

    void FindPlayer()
    {
        if (player != null) return;

        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void MoveTowardPlayer()
    {
        Vector3 direction = player.position - bossRb.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

#if UNITY_6000_0_OR_NEWER
        bossRb.linearVelocity = direction * moveSpeed;
#else
        bossRb.velocity = direction * moveSpeed;
#endif

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        bossRb.MoveRotation(
            Quaternion.Slerp(
                bossRb.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime
            )
        );
    }

    void CheckHit()
    {
        if (hasHit || player == null)
            return;

        float dist = Vector3.Distance(bossRb.position, player.position);

        if (dist <= hitDistance)
        {
            hasHit = true;

            IDamageable dmg = player.GetComponent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(damageAmount);

            Debug.Log("Boss hit player!");
        }
    }
}