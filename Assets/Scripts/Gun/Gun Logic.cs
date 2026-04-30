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

    [Header("Raycast Layer Filtering")]
    [SerializeField] private LayerMask ignoredLayers;
    [SerializeField] private bool autoIgnoreCloudLayer = true;
    [SerializeField] private string[] autoIgnoredLayerNames = { "Cloud", "Clouds" };

    [Header("Raycast VFX")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private LineRenderer tracerLine;
    [SerializeField] private float tracerDuration = 0.05f;

    [Header("Raycast Debug")]
    [SerializeField] private bool drawDebugRays = true;
    [SerializeField] private float debugRayDuration = 0.1f;
    [SerializeField] private Color debugHitColor = Color.red;
    [SerializeField] private Color debugMissColor = Color.yellow;

    private float nextFireTime = 0f;
    private int currentBurstShot = 0;
    private float nextBurstTime = 0f;

    private int cloudLayer = -1;

    void Awake()
    {
        if (autoIgnoreCloudLayer)
        {
            ApplyAutoIgnoredLayers();
        }
    }

    void ApplyAutoIgnoredLayers()
    {
        if (autoIgnoredLayerNames == null) return;

        for (int i = 0; i < autoIgnoredLayerNames.Length; i++)
        {
            string layerName = autoIgnoredLayerNames[i];
            if (string.IsNullOrWhiteSpace(layerName)) continue;

            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0) continue;

            if (layerName == "Cloud" || layerName == "Clouds")
            {
                cloudLayer = layer;
            }

            ignoredLayers = ignoredLayers.value | (1 << layer);
        }
    }

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

            // Burst weapons set their cooldown after the burst is finished.
            if (weaponType != WeaponType.RaycastBurst)
            {
                nextFireTime = Time.time + fireRate;
            }
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

        Bullet bulletComponent = spawnedBullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.EnableTracer();
        }
    }

    void ShootRaycast()
    {
        if (firePoint == null) return;

        Vector3 shootDirection = -firePoint.up;
        LayerMask effectiveHitLayers = hitLayers;
        effectiveHitLayers &= ~ignoredLayers.value;

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
        
        RaycastHit hit;

        if (Physics.Raycast(firePoint.position, shootDirection, out hit, raycastRange, effectiveHitLayers))
        {
            // Apply damage if target has the IDamageable interface
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            // Impact effect
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

            if (drawDebugRays)
            {
                Debug.DrawLine(firePoint.position, hit.point, debugHitColor, debugRayDuration);
            }
        }
        else
        {
            // No hit, show tracer to max range
            Vector3 endPoint = firePoint.position + shootDirection * raycastRange;
            if (tracerLine != null)
            {
                StartCoroutine(ShowTracer(endPoint));
            }

            if (drawDebugRays)
            {
                Debug.DrawRay(firePoint.position, shootDirection * raycastRange, debugMissColor, debugRayDuration);
            }
        }
    }

    void StartBurst()
    {
        int totalShots = Mathf.Max(1, burstCount);

        // Fire the first shot immediately for snappy burst response.
        ShootRaycast();

        currentBurstShot = totalShots - 1;
        if (currentBurstShot > 0)
        {
            nextBurstTime = Time.time + burstDelay;
        }
        else
        {
            nextFireTime = Time.time + fireRate;
        }
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
