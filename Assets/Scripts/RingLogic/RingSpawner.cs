using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RingSpawner — places rings in the world in various patterns.
///
/// Use this to build Star Fox-style corridors of rings, circles,
/// or random scatter fields. Call from the Inspector or at runtime.
///
/// Quick start:
///   1. Create an empty GameObject, attach RingSpawner.
///   2. Assign your ring prefab (a torus with Ring.cs attached).
///   3. Pick a pattern and hit Play, or call SpawnPattern() from another script.
/// </summary>
public class RingSpawner : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Ring Prefab")]
    [Tooltip("Prefab with Ring.cs attached. Should contain a torus mesh.")]
    public GameObject ringPrefab;

    [Header("Pattern")]
    public SpawnPatternList pattern = SpawnPatternList.Line;

    public enum SpawnPatternList
    {
        Line,       // rings in a straight corridor
        Arc,        // rings in a horizontal arc
        Circle,     // rings arranged in a circle the player flies through
        Scatter,    // random cloud of rings
        Custom      // use the customPositions list
    }

    [Header("Line / Arc Settings")]
    public int   ringCount        = 8;
    public float spacingDistance  = 30f;    // metres between rings along the path
    public float arcAngleDegrees  = 90f;    // total arc angle (Arc pattern only)
    public float arcRadius        = 150f;   // radius of the arc

    [Header("Circle Settings")]
    [Tooltip("Rings are arranged in a flat circle — fly through the centre.")]
    public float circleRadius     = 20f;

    [Header("Scatter Settings")]
    public float scatterRadius    = 100f;
    public float scatterHeightVariance = 20f;

    [Header("Custom Positions")]
    public List<Vector3> customPositions = new List<Vector3>();

    [Header("Ring Facing")]
    [Tooltip("If true, each ring faces the direction the player will approach from.")]
    public bool faceApproachDirection = true;

    [Tooltip("Override approach direction. Leave zero to use this object's forward.")]
    public Vector3 approachDirection = Vector3.zero;

    [Header("Spawn on Start")]
    public bool spawnOnStart = true;

    // Spawned ring references for cleanup
    private List<GameObject> spawnedRings = new List<GameObject>();

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        if (spawnOnStart)
            SpawnPattern();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Spawns rings according to the currently selected pattern.</summary>
    public void SpawnPattern()
    {
        ClearRings();

        switch (pattern)
        {
            case SpawnPatternList.Line:    SpawnLine();    break;
            case SpawnPatternList.Arc:     SpawnArc();     break;
            case SpawnPatternList.Circle:  SpawnCircle();  break;
            case SpawnPatternList.Scatter: SpawnScatter(); break;
            case SpawnPatternList.Custom:  SpawnCustom();  break;
        }
    }

    /// <summary>Destroys all rings spawned by this spawner.</summary>
    public void ClearRings()
    {
        foreach (var r in spawnedRings)
            if (r != null) Destroy(r);
        spawnedRings.Clear();
    }

    /// <summary>Convenience static method — spawn a single ring at a world position.</summary>
    public static GameObject SpawnRing(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[RingSpawner] Ring prefab is null.");
            return null;
        }
        return Instantiate(prefab, position, rotation);
    }

    // -------------------------------------------------------------------------
    // Patterns
    // -------------------------------------------------------------------------

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
        Vector3 right   = Vector3.Cross(Vector3.up, GetApproachDir()).normalized;
        float halfAngle = arcAngleDegrees * 0.5f;

        for (int i = 0; i < ringCount; i++)
        {
            float t     = ringCount > 1 ? (float)i / (ringCount - 1) : 0.5f;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t) * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Sin(angle) * arcRadius,
                0f,
                Mathf.Cos(angle) * arcRadius
            );

            // Rotate offset to align with spawner orientation
            Vector3 pos = transform.position + transform.TransformDirection(offset);

            // Each ring faces the tangent of the arc
            Vector3 tangent = new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle));
            tangent = transform.TransformDirection(tangent);
            SpawnAt(pos, tangent);
        }
    }

    void SpawnCircle()
    {
        // Rings arranged around a circle — player flies through the hole in the middle
        Vector3 forward = GetApproachDir();
        Vector3 up      = Vector3.up;
        Vector3 right   = Vector3.Cross(up, forward).normalized;

        for (int i = 0; i < ringCount; i++)
        {
            float angle = (360f / ringCount) * i * Mathf.Deg2Rad;
            Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * circleRadius;
            SpawnAt(transform.position + offset, forward);
        }
    }

    void SpawnScatter()
    {
        for (int i = 0; i < ringCount; i++)
        {
            Vector2 circle = Random.insideUnitCircle * scatterRadius;
            float   height = Random.Range(-scatterHeightVariance, scatterHeightVariance);
            Vector3 pos    = transform.position + new Vector3(circle.x, height, circle.y);
            // Random orientation for scatter
            Quaternion rot = Random.rotation;
            var go = Instantiate(ringPrefab, pos, rot);
            spawnedRings.Add(go);
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

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

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

        var go = Instantiate(ringPrefab, worldPos, rot);
        spawnedRings.Add(go);
    }

    Vector3 GetApproachDir()
    {
        if (approachDirection != Vector3.zero) return approachDirection.normalized;
        return transform.forward;
    }

    // -------------------------------------------------------------------------
    // Scene gizmos
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 3f);

        // Preview line
        if (pattern == SpawnPatternList.Line)
        {
            Vector3 dir = GetApproachDir();
            for (int i = 0; i < ringCount; i++)
            {
                Vector3 pos = transform.position + dir * (i * spacingDistance);
                Gizmos.DrawWireSphere(pos, 1.5f);
                if (i > 0) Gizmos.DrawLine(
                    transform.position + dir * ((i - 1) * spacingDistance), pos);
            }
        }

        // Preview scatter radius
        if (pattern == SpawnPatternList.Scatter)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, scatterRadius);
        }

        // Preview circle
        if (pattern == SpawnPatternList.Circle)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, circleRadius);
        }
    }
#endif
}
