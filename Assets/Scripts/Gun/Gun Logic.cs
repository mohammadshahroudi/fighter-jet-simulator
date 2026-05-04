using UnityEngine;
using UnityEngine.InputSystem;

public enum WeaponType
{
    Raycast
}

public class GunLogic : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private bool acceptPlayerInput = true;

    [Header("Fire Rate Settings")]
    [SerializeField] private float fireRate = 0.1f; // Time between shots

    [Header("Fire Point")]
    [SerializeField] private Transform firePoint;

    [Header("Audio")]
    [SerializeField] private AudioSource shotAudioSource;
    [SerializeField] private AudioClip shotSfx;
    [SerializeField] private bool loopShotSound = false;
    [SerializeField] [Range(0f, 1f)] private float shotVolume = 1f;

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
    [SerializeField] private float impactEffectLifetime = 1f;

    [Header("Raycast Debug")]
    [SerializeField] private bool drawDebugRays = true;
    [SerializeField] private float debugRayDuration = 0.1f;
    [SerializeField] private Color debugHitColor = Color.red;
    [SerializeField] private Color debugMissColor = Color.yellow;

    private float nextFireTime = 0f;
    private float lastShotTime = -999f;
    private float lastImpactTime = -999f;
    private GameObject activeImpact;

    private int cloudLayer = -1;

    void Awake()
    {
        if (hitLayers.value == 0)
        {
            hitLayers = Physics.DefaultRaycastLayers;
        }

        if (shotAudioSource == null)
        {
            shotAudioSource = GetComponent<AudioSource>();
        }

        if (autoIgnoreCloudLayer)
        {
            ApplyAutoIgnoredLayers();
        }

        if (tracerLine != null)
        {
            tracerLine.positionCount = 2;
            tracerLine.useWorldSpace = true;
            tracerLine.enabled = false;
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
        if (!acceptPlayerInput)
        {
            UpdateLoopingShotSound(false);
            return;
        }

        bool isFiringThisFrame = false;

        // Left click to shoot
        if (Mouse.current != null && Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
        {
            ShootRaycast();
            nextFireTime = Time.time + fireRate;

            isFiringThisFrame = true;
        }

        UpdateLoopingShotSound(isFiringThisFrame);
    }

    void ShootRaycast()
    {
        if (firePoint == null) return;

        lastShotTime = Time.time;

        PlayShotSound();

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
            // Apply damage if target has the IDamageable interface
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && hit.collider.transform.root != transform.root)
            {
                damageable.TakeDamage(damage);
            }

            // Impact effect
            SpawnImpactEffect(hit.point, hit.normal);

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

    System.Collections.IEnumerator ShowTracer(Vector3 startPoint, Vector3 endPoint)
    {
        if (tracerLine == null) yield break;

        tracerLine.SetPosition(0, startPoint);
        tracerLine.SetPosition(1, endPoint);
        tracerLine.enabled = true;

        yield return new WaitForSeconds(tracerDuration);

        tracerLine.enabled = false;
    }

    void SpawnRaycastTracer(Vector3 startPoint, Vector3 endPoint)
    {
        if (tracerLine != null)
        {
            StartCoroutine(ShowTracer(startPoint, endPoint));
        }
    }

    void SpawnImpactEffect(Vector3 point, Vector3 normal)
    {
        if (impactEffect == null) return;
        if (Time.time - lastImpactTime < fireRate) return;

        lastImpactTime = Time.time;

        if (activeImpact != null)
        {
            Destroy(activeImpact);
        }

        activeImpact = Instantiate(impactEffect, point, Quaternion.LookRotation(normal));
        Destroy(activeImpact, impactEffectLifetime);
    }

    void PlayShotSound()
    {
        if (shotAudioSource == null || shotSfx == null) return;

        if (loopShotSound)
        {
            if (shotAudioSource.clip != shotSfx)
            {
                shotAudioSource.clip = shotSfx;
            }

            shotAudioSource.volume = shotVolume;
            shotAudioSource.loop = true;

            if (!shotAudioSource.isPlaying)
            {
                shotAudioSource.Play();
            }

            return;
        }

        shotAudioSource.PlayOneShot(shotSfx, shotVolume);
    }

    void UpdateLoopingShotSound(bool isFiringThisFrame)
    {
        if (!loopShotSound || shotAudioSource == null) return;

        if (isFiringThisFrame)
        {
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed && Time.time < nextFireTime)
        {
            return;
        }

        if (Time.time - lastShotTime <= Mathf.Max(0.02f, fireRate * 1.1f))
        {
            return;
        }

        if (shotAudioSource.isPlaying && shotAudioSource.loop)
        {
            shotAudioSource.Stop();
        }
    }

    public bool TryFireFromAI()
    {
        if (firePoint == null) return false;
        if (Time.time < nextFireTime) return false;

        ShootRaycast();
        nextFireTime = Time.time + fireRate;
        return true;
    }

    public void SetPlayerInputEnabled(bool enabled)
    {
        acceptPlayerInput = enabled;
    }

    public Transform GetFirePoint()
    {
        return firePoint;
    }
}
