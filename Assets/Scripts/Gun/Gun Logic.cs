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

    [Header("References")]
    [SerializeField] private GunAutoLock gunAutoLock;

    [Header("Fire Rate Settings")]
    [SerializeField] private float fireRate = 0.1f;

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

    [Header("Aim Assist Settings")]
    [SerializeField] private bool enableAimAssist = true;
    [SerializeField] [Range(0f, 10f)] private float aimAssistConeAngle = 2f;
    [SerializeField] private int aimAssistRayCount = 8;
    [SerializeField] private bool debugDrawConeRays = false;
    [SerializeField] private Color debugConeRayColor = Color.cyan;

    [Header("Raycast Layer Filtering")]
    [SerializeField] private LayerMask ignoredLayers;
    [SerializeField] private bool autoIgnoreCloudLayer = true;
    [SerializeField] private string[] autoIgnoredLayerNames = { "Cloud", "Clouds" };

    [Header("Raycast VFX")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private LineRenderer tracerLine;

    [Header("Tracer Settings")]
    [SerializeField] private float tracerDuration = 0.08f;
    [SerializeField] private float tracerWidth = 0.15f;
    [SerializeField] private Gradient tracerColorGradient;
    [SerializeField] private float tracerBrightness = 2.0f;
    [SerializeField] private Material tracerMaterial;
    [SerializeField] private int tracerPoolSize = 20;

    [SerializeField] private float impactEffectLifetime = 1f;

    [Header("Raycast Debug")]
    [SerializeField] private bool drawDebugRays = true;
    [SerializeField] private float debugRayDuration = 0.1f;
    [SerializeField] private Color debugHitColor = Color.red;
    [SerializeField] private Color debugMissColor = Color.yellow;

    private float nextFireTime = 0f;
    private float lastShotTime = -999f;

    private System.Collections.Generic.Queue<LineRenderer> tracerPool;
    private System.Collections.Generic.List<LineRenderer> activeTracers;

    public System.Action<Vector3> OnTargetHit;
    public System.Action<bool> OnTargetInCrosshair;

    void Awake()
    {
        if (hitLayers.value == 0)
            hitLayers = Physics.DefaultRaycastLayers;

        if (shotAudioSource == null)
            shotAudioSource = GetComponent<AudioSource>();

        if (gunAutoLock == null)
            gunAutoLock = FindFirstObjectByType<GunAutoLock>();

        if (autoIgnoreCloudLayer)
            ApplyAutoIgnoredLayers();

        InitializeTracerPool();
    }

    void Update()
    {
        if (!acceptPlayerInput)
        {
            UpdateLoopingShotSound(false);
            return;
        }

        bool isFiringThisFrame = false;

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
        if (firePoint == null)
            return;

        lastShotTime = Time.time;
        PlayShotSound();

        Vector3 startPoint = firePoint.position;

        Vector3 shootDirection = firePoint.forward;

        if (gunAutoLock != null && gunAutoLock.CurrentTarget != null)
        {
            shootDirection = (gunAutoLock.CurrentTarget.position - firePoint.position).normalized;
        }

        LayerMask effectiveHitLayers = hitLayers;
        effectiveHitLayers &= ~ignoredLayers.value;

        if (drawDebugRays)
        {
            Debug.DrawRay(startPoint, -firePoint.up * raycastRange, Color.yellow, debugRayDuration);
            Debug.DrawRay(startPoint, shootDirection * raycastRange, Color.green, debugRayDuration);

            if (gunAutoLock != null && gunAutoLock.CurrentTarget != null)
            {
                Debug.DrawLine(startPoint, gunAutoLock.CurrentTarget.position, Color.blue, debugRayDuration);
            }
        }

        if (muzzleFlash != null)
        {
            GameObject flash = Instantiate(
                muzzleFlash,
                firePoint.position,
                Quaternion.LookRotation(shootDirection)
            );

            ParticleSystem ps = flash.GetComponent<ParticleSystem>();

            if (ps != null)
                Destroy(flash, ps.main.duration + ps.main.startLifetime.constantMax);
            else
                Destroy(flash, 2f);
        }

        if (PerformConeRaycast(startPoint, shootDirection, effectiveHitLayers, out RaycastHit hit, out IDamageable damageable))
        {
            if (damageable != null && hit.collider.transform.root != transform.root)
            {
                damageable.TakeDamage(damage);
                OnTargetHit?.Invoke(hit.point);
            }

            SpawnImpactEffect(hit.point, hit.normal);
            SpawnRaycastTracer(startPoint, hit.point);

            if (drawDebugRays)
                Debug.DrawLine(startPoint, hit.point, debugHitColor, debugRayDuration);
        }
        else
        {
            Vector3 endPoint = startPoint + shootDirection * raycastRange;
            SpawnRaycastTracer(startPoint, endPoint);

            if (drawDebugRays)
                Debug.DrawRay(startPoint, shootDirection * raycastRange, debugMissColor, debugRayDuration);
        }
    }

    bool PerformConeRaycast(
        Vector3 origin,
        Vector3 centerDirection,
        LayerMask effectiveHitLayers,
        out RaycastHit bestHit,
        out IDamageable hitDamageable)
    {
        bestHit = default;
        hitDamageable = null;

        if (!enableAimAssist)
        {
            if (Physics.Raycast(origin, centerDirection, out bestHit, raycastRange, effectiveHitLayers))
            {
                hitDamageable = bestHit.collider.GetComponentInParent<IDamageable>();
                return true;
            }

            return false;
        }

        bool foundDamageableTarget = false;
        bool foundAnyHit = false;
        float closestDamageableDistance = float.MaxValue;

        if (Physics.Raycast(origin, centerDirection, out RaycastHit centerHit, raycastRange, effectiveHitLayers))
        {
            foundAnyHit = true;
            bestHit = centerHit;

            IDamageable centerDamageable = centerHit.collider.GetComponentInParent<IDamageable>();

            if (centerDamageable != null)
            {
                hitDamageable = centerDamageable;
                closestDamageableDistance = centerHit.distance;
                foundDamageableTarget = true;
            }

            if (debugDrawConeRays)
                Debug.DrawLine(origin, centerHit.point, centerDamageable != null ? Color.green : debugConeRayColor, debugRayDuration);
        }
        else if (debugDrawConeRays)
        {
            Debug.DrawRay(origin, centerDirection * raycastRange, debugConeRayColor, debugRayDuration);
        }

        float angleRad = aimAssistConeAngle * Mathf.Deg2Rad;

        Vector3 up = Vector3.up;

        if (Vector3.Dot(centerDirection, up) > 0.99f)
            up = Vector3.right;

        Vector3 right = Vector3.Cross(centerDirection, up).normalized;
        Vector3 actualUp = Vector3.Cross(right, centerDirection).normalized;

        for (int i = 0; i < aimAssistRayCount; i++)
        {
            float angle = (i / (float)aimAssistRayCount) * Mathf.PI * 2f;

            Vector3 offset =
                (right * Mathf.Cos(angle) + actualUp * Mathf.Sin(angle)) *
                Mathf.Tan(angleRad);

            Vector3 rayDirection = (centerDirection + offset).normalized;

            if (Physics.Raycast(origin, rayDirection, out RaycastHit coneHit, raycastRange, effectiveHitLayers))
            {
                foundAnyHit = true;

                IDamageable coneDamageable = coneHit.collider.GetComponentInParent<IDamageable>();

                if (coneDamageable != null && coneHit.distance < closestDamageableDistance)
                {
                    bestHit = coneHit;
                    hitDamageable = coneDamageable;
                    closestDamageableDistance = coneHit.distance;
                    foundDamageableTarget = true;
                }
                else if (!foundDamageableTarget)
                {
                    bestHit = coneHit;
                }

                if (debugDrawConeRays)
                    Debug.DrawLine(origin, coneHit.point, coneDamageable != null ? Color.green : debugConeRayColor, debugRayDuration);
            }
            else if (debugDrawConeRays)
            {
                Debug.DrawRay(origin, rayDirection * raycastRange, debugConeRayColor, debugRayDuration);
            }
        }

        return foundDamageableTarget || foundAnyHit;
    }

    void ApplyAutoIgnoredLayers()
    {
        if (autoIgnoredLayerNames == null)
            return;

        for (int i = 0; i < autoIgnoredLayerNames.Length; i++)
        {
            string layerName = autoIgnoredLayerNames[i];

            if (string.IsNullOrWhiteSpace(layerName))
                continue;

            int layer = LayerMask.NameToLayer(layerName);

            if (layer < 0)
                continue;

            ignoredLayers = ignoredLayers.value | (1 << layer);
        }
    }

    void InitializeTracerPool()
    {
        tracerPool = new System.Collections.Generic.Queue<LineRenderer>();
        activeTracers = new System.Collections.Generic.List<LineRenderer>();

        GameObject tracerParent = new GameObject("TracerPool");
        tracerParent.transform.SetParent(transform);

        for (int i = 0; i < tracerPoolSize; i++)
        {
            GameObject tracerObj = new GameObject($"Tracer_{i}");
            tracerObj.transform.SetParent(tracerParent.transform);

            LineRenderer lr = tracerObj.AddComponent<LineRenderer>();
            SetupTracerLineRenderer(lr);

            lr.enabled = false;
            tracerPool.Enqueue(lr);
        }

        if (tracerLine != null)
            tracerLine.enabled = false;
    }

    void SetupTracerLineRenderer(LineRenderer lr)
    {
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        lr.startWidth = tracerWidth;
        lr.endWidth = tracerWidth * 0.5f;

        if (tracerColorGradient != null && tracerColorGradient.colorKeys.Length > 0)
        {
            lr.colorGradient = tracerColorGradient;
        }
        else
        {
            Gradient defaultGradient = new Gradient();

            defaultGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.9f, 0.3f) * tracerBrightness, 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f) * tracerBrightness, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 1f)
                }
            );

            lr.colorGradient = defaultGradient;
        }

        if (tracerMaterial != null)
            lr.material = tracerMaterial;
        else if (tracerLine != null && tracerLine.material != null)
            lr.material = tracerLine.material;

        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.alignment = LineAlignment.View;
    }

    LineRenderer GetTracerFromPool()
    {
        if (tracerPool.Count > 0)
            return tracerPool.Dequeue();

        if (activeTracers.Count > 0)
        {
            LineRenderer oldest = activeTracers[0];
            activeTracers.RemoveAt(0);
            return oldest;
        }

        return null;
    }

    void ReturnTracerToPool(LineRenderer lr)
    {
        if (lr == null)
            return;

        lr.enabled = false;
        activeTracers.Remove(lr);
        tracerPool.Enqueue(lr);
    }

    System.Collections.IEnumerator ShowTracer(LineRenderer lr, Vector3 startPoint, Vector3 endPoint)
    {
        if (lr == null)
            yield break;

        lr.SetPosition(0, startPoint);
        lr.SetPosition(1, endPoint);
        lr.enabled = true;

        yield return new WaitForSeconds(tracerDuration);

        ReturnTracerToPool(lr);
    }

    void SpawnRaycastTracer(Vector3 startPoint, Vector3 endPoint)
    {
        LineRenderer lr = GetTracerFromPool();

        if (lr != null)
        {
            activeTracers.Add(lr);
            StartCoroutine(ShowTracer(lr, startPoint, endPoint));
        }
    }

    void SpawnImpactEffect(Vector3 point, Vector3 normal)
    {
        if (impactEffect == null)
            return;

        GameObject impact = Instantiate(
            impactEffect,
            point,
            Quaternion.LookRotation(normal)
        );

        Destroy(impact, impactEffectLifetime);
    }

    void PlayShotSound()
    {
        if (shotAudioSource == null || shotSfx == null)
            return;

        if (loopShotSound)
        {
            if (shotAudioSource.clip != shotSfx)
                shotAudioSource.clip = shotSfx;

            shotAudioSource.volume = shotVolume;
            shotAudioSource.loop = true;

            if (!shotAudioSource.isPlaying)
                shotAudioSource.Play();

            return;
        }

        shotAudioSource.PlayOneShot(shotSfx, shotVolume);
    }

    void UpdateLoopingShotSound(bool isFiringThisFrame)
    {
        if (!loopShotSound || shotAudioSource == null)
            return;

        if (isFiringThisFrame)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed && Time.time < nextFireTime)
            return;

        if (Time.time - lastShotTime <= Mathf.Max(0.02f, fireRate * 1.1f))
            return;

        if (shotAudioSource.isPlaying && shotAudioSource.loop)
            shotAudioSource.Stop();
    }

    public bool TryFireFromAI()
    {
        if (firePoint == null)
            return false;

        if (Time.time < nextFireTime)
            return false;

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

    public void InitialiseDamage(float baseDamage)
    {
        damage = baseDamage;
    }
}