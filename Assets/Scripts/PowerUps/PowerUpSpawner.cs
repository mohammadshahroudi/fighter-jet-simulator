using System.Collections;
using UnityEngine;

public class SpawnManager : MonoBehaviour 
{
    [SerializeField] private GameObject[] powerUpPrefabs;
    [SerializeField] private float spawnInterval = 5f;

    [Header("Spawn Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float forwardDistance = 200f;
    [SerializeField] private float sideRange = 50f;
    [SerializeField] private float heightRange = 30f;

    void Start() {
        StartCoroutine(SpawnPowerUpRoutine());
    }

    IEnumerator SpawnPowerUpRoutine() {
        while (true) {
            yield return new WaitForSeconds(spawnInterval);
            if(player == null) continue;
            
            Vector3 spawnPos = player.position + player.forward * forwardDistance;
            
            spawnPos += player.right * Random.Range(-sideRange, sideRange);
            spawnPos += player.up * Random.Range(-heightRange, heightRange);

            int randomIndex = Random.Range(0, powerUpPrefabs.Length);

            Instantiate(powerUpPrefabs[randomIndex], spawnPos, Quaternion.identity);

        }
    }
}