using UnityEngine;
using UnityEngine.InputSystem;

public class GunLogic : MonoBehaviour
{
    [Header("Gun Settings")]
    [SerializeField] private float fireRate = 0.1f; // Time between shots (modify for upgrades)
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bullet;
    [SerializeField] private float bulletSpeed = 100f;
    

    private float nextFireTime = 0f;

    void Update()
    {
        // Left click to shoot
        if (Mouse.current != null && Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        // For testing
        Debug.Log("PEW!");
        GameObject spawnedBullet = Instantiate(bullet, firePoint.position, firePoint.rotation);
        
        Rigidbody rb = spawnedBullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = -firePoint.up * bulletSpeed;
        }
    }
}
