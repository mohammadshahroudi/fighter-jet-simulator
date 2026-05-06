using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemies in a configurable formation.
/// Each spawned unit carries its own EnemyHealth component.
/// Follows the player position and respawns after a delay or when all enemies are gone.
/// </summary>
public class FormationSpawner : MonoBehaviour
{
    public enum FormationType
    {
        Line,
        Grid,
        VWedge,
        Circle,
        Diamond
    }

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
    [SerializeField] private Transform spawnParent;

    [Header("Player Follow")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 20f, 100f);
    [SerializeField] private bool followPlayer = true;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 15f;

    private readonly List<GameObject> spawnedUnits = new();
    private float respawnTimer;
    private bool formationActive;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                player = playerObj.transform;
        }

        respawnTimer = respawnDelay;

        if (spawnOnStart)
            SpawnFormation();
    }

    private void Update()
    {
        FollowPlayer();
        CleanupDestroyedUnits();

        if (!formationActive)
        {
            respawnTimer -= Time.deltaTime;

            if (respawnTimer <= 0f)
                SpawnFormation();
        }
        else
        {
            if (AllSpawnedUnitsDestroyed())
            {
                formationActive = false;
                respawnTimer = respawnDelay;
            }
        }
    }

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
            Vector3 worldPos = transform.position + localOffset;

            GameObject unit = Instantiate(
                enemyPrefab,
                worldPos,
                Quaternion.identity,
                spawnParent
            );

            EnemyHealth health = unit.GetComponent<EnemyHealth>() ?? unit.AddComponent<EnemyHealth>();

            int hp = randomiseHealth
                ? Random.Range(minHealth, maxHealth + 1)
                : baseHealth;

            health.Initialise(hp);
            spawnedUnits.Add(unit);
        }

        formationActive = spawnedUnits.Count > 0;
        respawnTimer = respawnDelay;

        Debug.Log($"[FormationSpawner] Spawned {spawnedUnits.Count} units in {formationType} formation.");
    }

    public void ClearFormation()
    {
        foreach (GameObject unit in spawnedUnits)
        {
            if (unit != null)
                Destroy(unit);
        }

        spawnedUnits.Clear();
        formationActive = false;
    }

private void FollowPlayer()
{
    if (!followPlayer || player == null)
        return;

    Vector3 flatForward = player.forward;
    flatForward.y = 0f;

    if (flatForward.sqrMagnitude < 0.001f)
        flatForward = Vector3.forward;
    else
        flatForward.Normalize();

    Vector3 spawnCenter =
        player.position +
        flatForward * followOffset.z +
        Vector3.up * followOffset.y +
        Vector3.right * followOffset.x;

    transform.position = spawnCenter;
}

    private void CleanupDestroyedUnits()
    {
        for (int i = spawnedUnits.Count - 1; i >= 0; i--)
        {
            if (spawnedUnits[i] == null)
                spawnedUnits.RemoveAt(i);
        }
    }

    private bool AllSpawnedUnitsDestroyed()
    {
        CleanupDestroyedUnits();
        return spawnedUnits.Count == 0;
    }

    private List<Vector3> GeneratePositions() => formationType switch
    {
        FormationType.Line    => GenerateLine(),
        FormationType.Grid    => GenerateGrid(),
        FormationType.VWedge  => GenerateVWedge(),
        FormationType.Circle  => GenerateCircle(),
        FormationType.Diamond => GenerateDiamond(),
        _                     => GenerateGrid()
    };

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

    private List<Vector3> GenerateGrid()
    {
        var positions = new List<Vector3>();
        float totalWidth = (columns - 1) * spacingX;
        float totalDepth = (rows - 1) * spacingZ;

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

    private List<Vector3> GenerateDiamond()
    {
        var positions = new List<Vector3>();
        int halfR = rows / 2;
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

    private void OnDrawGizmosSelected()
    {
        if (enemyPrefab == null) return;

        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);
        List<Vector3> preview = GeneratePositions();

        foreach (Vector3 offset in preview)
        {
            Vector3 world = transform.position + offset;
            Gizmos.DrawWireSphere(world, 0.35f);
        }
    }
}