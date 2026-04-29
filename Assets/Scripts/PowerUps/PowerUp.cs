using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class PowerUp : MonoBehaviour
{
    public GameObject pickupEffect;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Pickup();
        }
    }

    void Pickup()
    {
        Instantiate(pickupEffect, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
