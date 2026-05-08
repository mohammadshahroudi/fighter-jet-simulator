using UnityEngine;

public class BossSpawnAfterTime : MonoBehaviour
{
    [Header("Boss")]
    public GameObject bossPrefab;
    public Transform player;

    [Header("Spawn Timing")]
    public float spawnDelaySeconds = 300f; // 5 minutes

    [Header("Spawn Position")]
    public float spawnDistanceInFront = 150f;
    public float spawnHeight = 80f;

    private bool bossSpawned = false;
    private float timer = 0f;

    void Update()
    {
        if (bossSpawned) return;

        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
                player = p.transform;
            else
                return;
        }

        timer += Time.deltaTime;

        if (timer >= spawnDelaySeconds)
        {
            SpawnBoss();
        }
    }

    void SpawnBoss()
    {
        if (bossPrefab == null || player == null)
        {
            Debug.LogError("[BossSpawner] Missing references!");
            return;
        }

        Vector3 spawnPosition =
            player.position +
            player.forward * spawnDistanceInFront +
            Vector3.up * spawnHeight;

        Quaternion spawnRotation =
            Quaternion.LookRotation(-player.forward, Vector3.up);


        GameObject boss = Instantiate(bossPrefab, spawnPosition, spawnRotation);

        BossSwoopAttack swoop = boss.GetComponent<BossSwoopAttack>();
        if (swoop != null)
        {
            swoop.SetPlayer(player);
        }
        else
        {
            Debug.LogWarning("[BossSpawner] Boss has no BossSwoopAttack script!");
        }

        bossSpawned = true;

        Debug.Log("[BossSpawner] Boss spawned!");
    }
}