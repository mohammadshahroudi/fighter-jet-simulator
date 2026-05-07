using System.Collections;
using UnityEngine;

public class SpawnManager : MonoBehaviour 
{
    [SerializeField] private GameObject[] powerUpPrefabs;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float powerUpLifeTime = 15f;

    [Header("Spawn Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float forwardDistance = 200f;
    [SerializeField] private float sideRange = 50f;
    [SerializeField] private float heightRange = 30f;

    private Rigidbody playerRb;
    void Start() 
    {
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
        }
        StartCoroutine(SpawnPowerUpRoutine());
    }

    IEnumerator SpawnPowerUpRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (player == null || powerUpPrefabs.Length == 0)
            {
                continue;
            }

            SpawnPowerUp();
        }
    }
    
    private void SpawnPowerUp()
    {
        Vector3 forwardDirection = player.forward;
        if (playerRb != null && playerRb.linearVelocity.sqrMagnitude > 1f)
        {
            forwardDirection = playerRb.linearVelocity.normalized;
        }
        Vector3 spawnPos = player.position + player.forward * forwardDistance; 
        
        spawnPos += player.right * Random.Range(-sideRange, sideRange);
        spawnPos += player.up * Random.Range(-heightRange, heightRange);
        
        int randomIndex = Random.Range(0, powerUpPrefabs.Length);

        //Instantiate(powerUpPrefabs[randomIndex], spawnPos, Quaternion.identity);

        GameObject powerUp = Instantiate(
            powerUpPrefabs[randomIndex],
            spawnPos,
            Quaternion.identity);
        Destroy(powerUp, powerUpLifeTime);
        
    } 
}
