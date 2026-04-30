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
    [SerializeField] private GameObject tracerPrefab;
    [SerializeField] private LineRenderer tracerLine;
    [SerializeField] private bool useMovingTracer = true;
    [SerializeField] private bool animateLineTracer = true;
    [SerializeField] private float tracerSpeed = 450f;
    [SerializeField] private float tracerDuration = 0.05f;
    [SerializeField] private float tracerPrefabLifetime = 1.5f;
    [SerializeField] private bool delayImpactToTracer = true;
    [SerializeField] private bool delayDamageToTracer = false;
    [SerializeField] private float impactEffectLifetime = 1f;

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

        Vector3 startPoint = firePoint.position;

        if (Physics.Raycast(startPoint, shootDirection, out hit, raycastRange, effectiveHitLayers))
        {
            float tracerTravelTime = GetTracerTravelTime(startPoint, hit.point);

            // Apply damage if target has the IDamageable interface
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                if (delayDamageToTracer && tracerTravelTime > 0f)
                {
                    StartCoroutine(ApplyDamageAfterDelay(damageable, tracerTravelTime));
                }
                else
                {
                    damageable.TakeDamage(damage);
                }
            }

            // Impact effect
            SpawnImpactEffect(hit.point, hit.normal, tracerTravelTime);

            // Show tracer to hit point
            SpawnRaycastTracer(startPoint, hit.point);

            if (drawDebugRays)
            {
                Debug.DrawLine(firePoint.position, hit.point, debugHitColor, debugRayDuration);
            }
        }
        else
        {
            // No hit, show tracer to max range
            Vector3 endPoint = startPoint + shootDirection * raycastRange;
            SpawnRaycastTracer(startPoint, endPoint);

            if (drawDebugRays)
            {
                Debug.DrawRay(startPoint, shootDirection * raycastRange, debugMissColor, debugRayDuration);
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

    System.Collections.IEnumerator ShowTracer(Vector3 startPoint, Vector3 endPoint)
    {
        if (tracerLine == null) yield break;

        tracerLine.enabled = true;

        float travelTime = 0f;
        if (tracerSpeed > 0f)
        {
            travelTime = Vector3.Distance(startPoint, endPoint) / tracerSpeed;
        }

        tracerLine.SetPosition(0, startPoint);

        if (animateLineTracer && travelTime > 0f)
        {
            float elapsed = 0f;
            while (elapsed < travelTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / travelTime);
                tracerLine.SetPosition(1, Vector3.Lerp(startPoint, endPoint, t));
                yield return null;
            }
        }

        tracerLine.SetPosition(1, endPoint);

        yield return new WaitForSeconds(tracerDuration);

        tracerLine.enabled = false;
    }

    System.Collections.IEnumerator ShowTracerPrefab(Vector3 startPoint, Vector3 endPoint)
    {
        if (!useMovingTracer || tracerPrefab == null) yield break;

        Vector3 direction = endPoint - startPoint;
        Quaternion rotation = direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized)
            : Quaternion.identity;

        GameObject tracerObject = Instantiate(tracerPrefab, startPoint, rotation);
        Destroy(tracerObject, tracerPrefabLifetime);

        float travelTime = 0f;
        if (tracerSpeed > 0f)
        {
            travelTime = Vector3.Distance(startPoint, endPoint) / tracerSpeed;
        }

        if (travelTime <= 0f)
        {
            tracerObject.transform.position = endPoint;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < travelTime && tracerObject != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            tracerObject.transform.position = Vector3.Lerp(startPoint, endPoint, t);
            yield return null;
        }

        if (tracerObject != null)
        {
            tracerObject.transform.position = endPoint;
        }
    }

    void SpawnRaycastTracer(Vector3 startPoint, Vector3 endPoint)
    {
        if (useMovingTracer && tracerPrefab != null)
        {
            StartCoroutine(ShowTracerPrefab(startPoint, endPoint));
            return;
        }

        if (tracerLine != null)
        {
            StartCoroutine(ShowTracer(startPoint, endPoint));
        }
    }

    float GetTracerTravelTime(Vector3 startPoint, Vector3 endPoint)
    {
        if (tracerSpeed <= 0f) return 0f;
        return Vector3.Distance(startPoint, endPoint) / tracerSpeed;
    }

    void SpawnImpactEffect(Vector3 point, Vector3 normal, float tracerTravelTime)
    {
        if (impactEffect == null) return;

        if (delayImpactToTracer && tracerTravelTime > 0f)
        {
            StartCoroutine(SpawnImpactAfterDelay(point, normal, tracerTravelTime));
            return;
        }

        GameObject impact = Instantiate(impactEffect, point, Quaternion.LookRotation(normal));
        Destroy(impact, impactEffectLifetime);
    }

    System.Collections.IEnumerator SpawnImpactAfterDelay(Vector3 point, Vector3 normal, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (impactEffect == null) yield break;

        GameObject impact = Instantiate(impactEffect, point, Quaternion.LookRotation(normal));
        Destroy(impact, impactEffectLifetime);
    }

    System.Collections.IEnumerator ApplyDamageAfterDelay(IDamageable damageable, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (damageable is Object unityObject && unityObject == null)
        {
            yield break;
        }

        damageable.TakeDamage(damage);
    }
}
