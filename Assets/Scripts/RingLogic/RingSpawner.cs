using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RingSpawner — follows the player and periodically spawns ring patterns
/// in front of the player.
/// </summary>
public class RingSpawner : MonoBehaviour
{
    [Header("Ring Prefab")]
    [Tooltip("Prefab with Ring.cs attached. Should contain a torus mesh.")]
    public GameObject ringPrefab;

    [Header("Player")]
    public Transform player;

    [Header("Pattern")]
    public SpawnPatternList pattern = SpawnPatternList.Line;

    public enum SpawnPatternList
    {
        Line,
        Arc,
        Circle,
        Scatter,
        Custom
    }

    [Header("Follow Player")]
    public bool followPlayer = true;
    public Vector3 followOffset = new Vector3(0f, 0f, 120f);
    public float followSmoothTime = 0.15f;

    [Header("Periodic Spawning")]
    public bool periodicSpawning = true;
    public float spawnInterval = 4f;
    public float spawnDistanceInFrontOfPlayer = 120f;
    public bool clearOldRingsBeforeNewSpawn = false;

    [Header("Line / Arc Settings")]
    public int ringCount = 8;
    public float spacingDistance = 30f;
    public float arcAngleDegrees = 90f;
    public float arcRadius = 150f;

    [Header("Circle Settings")]
    public float circleRadius = 20f;

    [Header("Scatter Settings")]
    public float scatterRadius = 100f;
    public float scatterHeightVariance = 20f;

    [Header("Custom Positions")]
    public List<Vector3> customPositions = new List<Vector3>();

    [Header("Ring Facing")]
    public bool faceApproachDirection = true;
    public Vector3 approachDirection = Vector3.zero;

    [Header("Spawn on Start")]
    public bool spawnOnStart = true;

    private Vector3 followVelocity;
    private float spawnTimer;

    private readonly List<GameObject> spawnedRings = new List<GameObject>();

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
                player = p.transform;
        }

        if (spawnOnStart)
            SpawnPatternInFrontOfPlayer();
    }

    void Update()
    {
        if (!periodicSpawning || player == null)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnPatternInFrontOfPlayer();
        }
    }

    void LateUpdate()
    {
        if (!followPlayer || player == null)
            return;

        Vector3 targetPosition =
            player.position +
            player.right * followOffset.x +
            player.up * followOffset.y +
            player.forward * followOffset.z;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            followSmoothTime
        );

        transform.rotation = player.rotation;
    }

    public void SpawnPatternInFrontOfPlayer()
    {
        if (player == null)
            return;

        Vector3 oldPosition = transform.position;
        Quaternion oldRotation = transform.rotation;

        Vector3 spawnPosition =
            player.position +
            player.right * followOffset.x +
            player.up * followOffset.y +
            player.forward * spawnDistanceInFrontOfPlayer;

        transform.position = spawnPosition;
        transform.rotation = player.rotation;

        if (clearOldRingsBeforeNewSpawn)
            ClearRings();

        SpawnPattern();

        transform.position = oldPosition;
        transform.rotation = oldRotation;
    }

    public void SpawnPattern()
    {
        switch (pattern)
        {
            case SpawnPatternList.Line:
                SpawnLine();
                break;

            case SpawnPatternList.Arc:
                SpawnArc();
                break;

            case SpawnPatternList.Circle:
                SpawnCircle();
                break;

            case SpawnPatternList.Scatter:
                SpawnScatter();
                break;

            case SpawnPatternList.Custom:
                SpawnCustom();
                break;
        }
    }

    public void ClearRings()
    {
        foreach (GameObject ring in spawnedRings)
        {
            if (ring != null)
                Destroy(ring);
        }

        spawnedRings.Clear();
    }

    public static GameObject SpawnRing(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[RingSpawner] Ring prefab is null.");
            return null;
        }

        return Instantiate(prefab, position, rotation);
    }

    void SpawnLine()
    {
        Vector3 dir = GetApproachDir();

        for (int i = 0; i < ringCount; i++)
        {
            Vector3 pos = transform.position + dir * (i * spacingDistance);
            SpawnAt(pos, dir);
        }
    }

    void SpawnArc()
    {
        float halfAngle = arcAngleDegrees * 0.5f;

        for (int i = 0; i < ringCount; i++)
        {
            float t = ringCount > 1 ? (float)i / (ringCount - 1) : 0.5f;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t) * Mathf.Deg2Rad;

            Vector3 localOffset = new Vector3(
                Mathf.Sin(angle) * arcRadius,
                0f,
                Mathf.Cos(angle) * arcRadius
            );

            Vector3 worldPos = transform.position + transform.TransformDirection(localOffset);

            Vector3 localTangent = new Vector3(
                Mathf.Cos(angle),
                0f,
                -Mathf.Sin(angle)
            );

            Vector3 worldTangent = transform.TransformDirection(localTangent);

            SpawnAt(worldPos, worldTangent);
        }
    }

    void SpawnCircle()
    {
        Vector3 forward = GetApproachDir();
        Vector3 up = transform.up;
        Vector3 right = transform.right;

        for (int i = 0; i < ringCount; i++)
        {
            float angle = (360f / ringCount) * i * Mathf.Deg2Rad;

            Vector3 offset =
                Mathf.Cos(angle) * right * circleRadius +
                Mathf.Sin(angle) * up * circleRadius;

            SpawnAt(transform.position + offset, forward);
        }
    }

    void SpawnScatter()
    {
        for (int i = 0; i < ringCount; i++)
        {
            Vector2 circle = Random.insideUnitCircle * scatterRadius;
            float height = Random.Range(-scatterHeightVariance, scatterHeightVariance);

            Vector3 pos =
                transform.position +
                transform.right * circle.x +
                transform.up * height +
                transform.forward * circle.y;

            Vector3 faceDirection = player != null
                ? (player.position - pos).normalized
                : transform.forward;

            SpawnAt(pos, faceDirection);
        }
    }

    void SpawnCustom()
    {
        Vector3 dir = GetApproachDir();

        foreach (Vector3 localPos in customPositions)
        {
            Vector3 worldPos = transform.TransformPoint(localPos);
            SpawnAt(worldPos, dir);
        }
    }

    void SpawnAt(Vector3 worldPos, Vector3 faceDir)
    {
        if (ringPrefab == null)
        {
            Debug.LogError("[RingSpawner] Ring prefab not assigned!");
            return;
        }

        Quaternion rot = faceApproachDirection && faceDir != Vector3.zero
            ? Quaternion.LookRotation(faceDir, Vector3.up)
            : Quaternion.identity;

        GameObject go = Instantiate(ringPrefab, worldPos, rot);

        Ring ring = go.GetComponent<Ring>();
        if (ring != null)
        {
            ring.SetPlayer(player);
            ring.ResetRing();
        }

        spawnedRings.Add(go);
    }

    Vector3 GetApproachDir()
    {
        if (approachDirection != Vector3.zero)
            return approachDirection.normalized;

        return transform.forward;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 3f);

        if (pattern == SpawnPatternList.Line)
        {
            Vector3 dir = GetApproachDir();

            for (int i = 0; i < ringCount; i++)
            {
                Vector3 pos = transform.position + dir * (i * spacingDistance);
                Gizmos.DrawWireSphere(pos, 1.5f);

                if (i > 0)
                {
                    Vector3 previousPos =
                        transform.position + dir * ((i - 1) * spacingDistance);

                    Gizmos.DrawLine(previousPos, pos);
                }
            }
        }

        if (pattern == SpawnPatternList.Scatter)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, scatterRadius);
        }

        if (pattern == SpawnPatternList.Circle)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, circleRadius);
        }
    }
#endif
}