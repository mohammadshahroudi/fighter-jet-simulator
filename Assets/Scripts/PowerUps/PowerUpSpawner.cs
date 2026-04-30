using System.Collections;
using UnityEngine;

public class SpawnManager : MonoBehaviour {
    public GameObject[] powerUpPrefabs;
    public float spawnInterval = 5.0f;

    void Start() {
        StartCoroutine(SpawnPowerUpRoutine());
    }

    IEnumerator SpawnPowerUpRoutine() {
        while (true) {
            yield return new WaitForSeconds(spawnInterval);
            
            Vector3 spawnPos = new Vector3(Random.Range(-140f, 140f), 128f, -50f);
            
            int randomIndex = Random.Range(0, powerUpPrefabs.Length);
            
            Instantiate(powerUpPrefabs[randomIndex], spawnPos, Quaternion.identity);
        }
    }
}