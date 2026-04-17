using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemies in a configurable formation.
/// Each spawned unit carries its own EnemyHealth component.
/// </summary>
public class FormationSpawner : MonoBehaviour
{
    // ── Formation Types ──────────────────────────────────────────────────────

    public enum FormationType
    {
        Line,
        Grid,
        VWedge,
        Circle,
        Diamond
    }

    // ── Inspector Fields ─────────────────────────────────────────────────────

    [Header("Prefab")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Formation Settings")]
    [SerializeField] private FormationType formationType = FormationType.Grid;
    [SerializeField] private int columns = 4;
    [SerializeField] private int rows = 3;
    [SerializeField] private float spacingX = 2f;
    [SerializeField] private float spacingZ = 2f;

    [Header("Health Settings")]
    [SerializeField] private int baseHealth = 100;
    [SerializeField] private bool randomiseHealth = false;
    [SerializeField] private int minHealth = 50;
    [SerializeField] private int maxHealth = 150;

    [Header("Spawn Options")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Transform spawnParent; // Optional organisational parent

    // ── Runtime State ────────────────────────────────────────────────────────

    private readonly List<GameObject> spawnedUnits = new();

    // ── Unity Callbacks ──────────────────────────────────────────────────────

    private void Start()
    {
        if (spawnOnStart)
            SpawnFormation();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Clears any existing units and spawns a fresh formation.</summary>
    public void SpawnFormation()
    {
        ClearFormation();

        if (enemyPrefab == null)
        {
            Debug.LogError("[FormationSpawner] enemyPrefab is not assigned.", this);
            return;
        }

        List<Vector3> positions = GeneratePositions();

        
        foreach (Vector3 localOffset in positions)
        {
            Vector3 worldPos = transform.TransformPoint(localOffset);
            GameObject unit = Instantiate(enemyPrefab, worldPos, Quaternion.Euler(-90,0,0), spawnParent);

            // Ensure the unit has a health component, then initialise it.
            EnemyHealth health = unit.GetComponent<EnemyHealth>()
                              ?? unit.AddComponent<EnemyHealth>();

            int hp = randomiseHealth
                ? Random.Range(minHealth, maxHealth + 1)
                : baseHealth;

            health.Initialise(hp);
            spawnedUnits.Add(unit);
        }

        Debug.Log($"[FormationSpawner] Spawned {spawnedUnits.Count} units in {formationType} formation.");
    }

    /// <summary>Destroys all units that were spawned by this spawner.</summary>
    public void ClearFormation()
    {
        foreach (GameObject unit in spawnedUnits)
        {
            if (unit != null)
                Destroy(unit);
        }
        spawnedUnits.Clear();
    }

    // ── Position Generators ──────────────────────────────────────────────────

    private List<Vector3> GeneratePositions() => formationType switch
    {
        FormationType.Line    => GenerateLine(),
        FormationType.Grid    => GenerateGrid(),
        FormationType.VWedge  => GenerateVWedge(),
        FormationType.Circle  => GenerateCircle(),
        FormationType.Diamond => GenerateDiamond(),
        _                     => GenerateGrid()
    };

    // Single row of `columns` units.
    private List<Vector3> GenerateLine()
    {
        var positions = new List<Vector3>();
        float totalWidth = (columns - 1) * spacingX;

        for (int c = 0; c < columns; c++)
        {
            float x = -totalWidth / 2f + c * spacingX;
            positions.Add(new Vector3(x, 0f, 0f));
        }
        return positions;
    }

    // Rows × Columns rectangular grid.
    private List<Vector3> GenerateGrid()
    {
        var positions = new List<Vector3>();
        float totalWidth  = (columns - 1) * spacingX;
        float totalDepth  = (rows    - 1) * spacingZ;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float x = -totalWidth / 2f + c * spacingX;
                float z = -totalDepth / 2f + r * spacingZ;
                positions.Add(new Vector3(x, 0f, z));
            }
        }
        return positions;
    }

    // V-shaped wedge: each subsequent row is one unit wider and offset back.
    private List<Vector3> GenerateVWedge()
    {
        var positions = new List<Vector3>();

        for (int r = 0; r < rows; r++)
        {
            int unitsInRow = r + 1;
            float rowWidth = (unitsInRow - 1) * spacingX;

            for (int c = 0; c < unitsInRow; c++)
            {
                float x = -rowWidth / 2f + c * spacingX;
                float z = r * spacingZ;
                positions.Add(new Vector3(x, 0f, z));
            }
        }
        return positions;
    }

    // `columns` units arranged in a circle (rows is ignored for this type).
    private List<Vector3> GenerateCircle()
    {
        var positions = new List<Vector3>();
        float radius = spacingX * columns / (2f * Mathf.PI);

        for (int i = 0; i < columns; i++)
        {
            float angle = i * (360f / columns) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;
            positions.Add(new Vector3(x, 0f, z));
        }
        return positions;
    }

    // Diamond/rhombus pattern using rows × columns but skipping corners.
    private List<Vector3> GenerateDiamond()
    {
        var positions = new List<Vector3>();
        int halfR = rows    / 2;
        int halfC = columns / 2;

        for (int r = -halfR; r <= halfR; r++)
        {
            int span = halfC - Mathf.Abs(r);
            for (int c = -span; c <= span; c++)
            {
                positions.Add(new Vector3(c * spacingX, 0f, r * spacingZ));
            }
        }
        return positions;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (enemyPrefab == null) return;

        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);
        List<Vector3> preview = GeneratePositions();

        foreach (Vector3 offset in preview)
        {
            Vector3 world = transform.TransformPoint(offset);
            Gizmos.DrawWireSphere(world, 0.35f);
        }
    }
}
