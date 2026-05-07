using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// KillScoreManager — tracks score from enemy kills and persists high score to PlayerPrefs.
/// 
/// Setup:
///   1. Attach to a persistent GameObject (e.g. GameManager).
///   2. The script automatically subscribes to enemy death events.
///   3. Each enemy kill adds points and may update the saved high score.
///   4. UI can read CurrentScore for in-game display and HighScore for menu display.
/// </summary>
public class KillScoreManager : MonoBehaviour
{
    private const string HighScoreKey = "HighScore";

    [Header("Scoring")]
    [SerializeField] private int pointsPerKill = 10;

    private int currentScore;
    private int highScore;
    private readonly HashSet<EnemyHealth> subscribedEnemies = new HashSet<EnemyHealth>();

    public int CurrentScore => currentScore;
    public int HighScore => highScore;

    public static KillScoreManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        EnemyHealth.OnEnemyEnabled += HandleEnemyEnabled;
        EnemyHealth.OnEnemyDisabled += HandleEnemyDisabled;
    }

    private void Start()
    {
        // Load the saved high score
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        currentScore = 0;

        // Subscribe to existing enemies
        SubscribeToExistingEnemies();
    }

    private void OnDisable()
    {
        EnemyHealth.OnEnemyEnabled -= HandleEnemyEnabled;
        EnemyHealth.OnEnemyDisabled -= HandleEnemyDisabled;

        UnsubscribeFromExistingEnemies();
    }

    private void HandleEnemyEnabled(EnemyHealth enemy)
    {
        if (enemy == null || subscribedEnemies.Contains(enemy)) return;
        subscribedEnemies.Add(enemy);
        enemy.onDeath.AddListener(OnEnemyKilled);
    }

    private void HandleEnemyDisabled(EnemyHealth enemy)
    {
        if (enemy == null) return;
        subscribedEnemies.Remove(enemy);
        enemy.onDeath.RemoveListener(OnEnemyKilled);
    }

    private void SubscribeToExistingEnemies()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy != null && !subscribedEnemies.Contains(enemy))
            {
                subscribedEnemies.Add(enemy);
                enemy.onDeath.AddListener(OnEnemyKilled);
            }
        }
    }

    private void UnsubscribeFromExistingEnemies()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy != null && subscribedEnemies.Remove(enemy))
                enemy.onDeath.RemoveListener(OnEnemyKilled);
        }
    }

    private void OnEnemyKilled()
    {
        currentScore += pointsPerKill;

        // Update high score if beaten
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Reset score to zero (call on new game / level restart).</summary>
    public void ResetScore()
    {
        currentScore = 0;
    }
}
