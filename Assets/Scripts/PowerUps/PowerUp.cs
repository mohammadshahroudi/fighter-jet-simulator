using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class PowerUp : MonoBehaviour
{
    public enum PowerUpType
    {
        Health,
        Boost,
        HealthAndBoost
    }

    [SerializeField] private PowerUpType powerUpType;

    [Header("Health")]
    [SerializeField] private int healthAmount = 40;

    [Header("Boost")]
    [SerializeField] private float boostMultiplier = 1.4f;
    [SerializeField] private float boostDuration = 2f;

    [Header("Effects")]
    [SerializeField] private GameObject pickupEffect;

    private bool pickedUp;

    private void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;
        if (!other.CompareTag("Player")) return;

        pickedUp = true;
        Pickup(other);
    }

    private void Pickup(Collider player)
    {
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, transform.rotation);
        }

        PlayerStats stats = player.GetComponent<PlayerStats>();
        PlayerController controller = player.GetComponent<PlayerController>();

        if (stats != null &&
            (powerUpType == PowerUpType.Health || powerUpType == PowerUpType.HealthAndBoost))
        {
            stats.IncreaseMaxHealth(healthAmount);
        }

        if (controller != null &&
            (powerUpType == PowerUpType.Boost || powerUpType == PowerUpType.HealthAndBoost))
        {
            controller.ApplyBoostPowerUp(boostMultiplier, boostDuration);
        }

        Destroy(gameObject);
    }
}
