using UnityEngine;
using UnityEngine.InputSystem;

public enum WeaponType
{
    // Heavy Cannon, Missile?, Bombs?
    Projectile,
    // Machine Gun, LMG, Laser Gun
    Raycast,
    // Burst Gun
    RaycastBurst
}

public class GunLogic : MonoBehaviour
{
    [Header("Weapon Type")]
    [SerializeField] private WeaponType weaponType = WeaponType.Projectile;

    [Header("Fire Rate Settings")]
    [SerializeField] private float fireRate = 0.1f; // Time between shots
    [SerializeField] private int burstCount = 3; // For burst weapons
    [SerializeField] private float burstDelay = 0.05f; // Delay between burst shots

    [Header("Fire Point")]
    [SerializeField] private Transform firePoint;

    [Header("Projectile Settings")]
    [SerializeField] private GameObject bullet;
    [SerializeField] private float bulletSpeed = 100f;

    [Header("Raycast Settings")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float raycastRange = 1000f;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private LineRenderer tracerLine;
    [SerializeField] private float tracerDuration = 0.05f;

    private float nextFireTime = 0f;
    private int currentBurstShot = 0;
    private float nextBurstTime = 0f;

    void Update()
    {
        // Handle burst fire
        if (weaponType == WeaponType.RaycastBurst && currentBurstShot > 0)
        {
            if (Time.time >= nextBurstTime)
            {
                ShootRaycast();
                currentBurstShot--;
                nextBurstTime = Time.time + burstDelay;

                if (currentBurstShot == 0)
                {
                    nextFireTime = Time.time + fireRate;
                }
            }
            return;
        }

        // Left click to shoot
        if (Mouse.current != null && Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        switch (weaponType)
        {
            case WeaponType.Projectile:
                ShootProjectile();
                break;
            case WeaponType.Raycast:
                ShootRaycast();
                break;
            case WeaponType.RaycastBurst:
                StartBurst();
                break;
        }
    }

    void ShootProjectile()
    {
        if (bullet == null || firePoint == null) return;

        GameObject spawnedBullet = Instantiate(bullet, firePoint.position, firePoint.rotation);

        Rigidbody rb = spawnedBullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = -firePoint.up * bulletSpeed;
        }
    }

    void ShootRaycast()
    {
        if (firePoint == null) return;

        Vector3 shootDirection = -firePoint.up;

        // Show muzzle flash
        if (muzzleFlash != null)
        {
            GameObject flash = Instantiate(muzzleFlash, firePoint.position, Quaternion.LookRotation(shootDirection));
            ParticleSystem ps = flash.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(flash, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(flash, 2f);
            }
        }

        // Perform raycast
        RaycastHit hit;

        if (Physics.Raycast(firePoint.position, shootDirection, out hit, raycastRange, hitLayers))
        {
            // Apply damage if target has a damage interface/component
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            // Show impact effect
            if (impactEffect != null)
            {
                GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 1f);
            }

            // Show tracer to hit point
            if (tracerLine != null)
            {
                StartCoroutine(ShowTracer(hit.point));
            }

            Debug.DrawLine(firePoint.position, hit.point, Color.red, 0.1f);
        }
        else
        {
            // No hit, show tracer to max range
            Vector3 endPoint = firePoint.position + shootDirection * raycastRange;
            if (tracerLine != null)
            {
                StartCoroutine(ShowTracer(endPoint));
            }

            Debug.DrawRay(firePoint.position, shootDirection * raycastRange, Color.yellow, 0.1f);
        }
    }

    void StartBurst()
    {
        currentBurstShot = burstCount;
        nextBurstTime = Time.time;
    }

    System.Collections.IEnumerator ShowTracer(Vector3 endPoint)
    {
        if (tracerLine == null) yield break;

        tracerLine.enabled = true;
        tracerLine.SetPosition(0, firePoint.position);
        tracerLine.SetPosition(1, endPoint);

        yield return new WaitForSeconds(tracerDuration);

        tracerLine.enabled = false;
    }
}
