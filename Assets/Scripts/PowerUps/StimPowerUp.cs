using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class StimPowerUp : MonoBehaviour
{
    public float multiplier = 1.4f;
    public float duration = 2f;
    public GameObject pickupEffect;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(Pickup(other));
        }
    }

    IEnumerator Pickup(Collider player)
    {
        Instantiate(pickupEffect, transform.position, transform.rotation);
        PlayerStats stats = player.GetComponent<PlayerStats>();
        // Increase max and current HP by 40
        stats.IncreaseMaxHealth(40);

        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;

        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
}
