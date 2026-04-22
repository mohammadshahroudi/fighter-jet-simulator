using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// GameStateManager — singleton that tracks game over and victory conditions.
///
/// Game Over triggers when:
///   - The player crashes (PlayerController calls GameStateManager.Instance.TriggerGameOver())
///
/// Victory triggers when:
///   - All rings in the scene have been collected (Ring.OnRingCollected fires this automatically)
///
/// Setup:
///   1. Attach to a persistent GameObject (e.g. GameManager).
///   2. Build two UI panels (gameOverPanel, victoryPanel) in a Screen Space Overlay Canvas.
///   3. Assign the panels, labels, and buttons in the Inspector.
///   4. Call GameStateManager.Instance.TriggerGameOver() from PlayerController.PlaneCrash().
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Delay")]
    [Tooltip("Seconds between the event and the UI appearing.")]
    public float gameOverDelay = 1.8f;
    public float victoryDelay  = 1.2f;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TMP_Text   gameOverTitleLabel;   // e.g. "MISSION FAILED"
    public TMP_Text   gameOverScoreLabel;   // shows final score
    public Button     gameOverRestartButton;
    public Button     gameOverQuitButton;

    [Header("Victory UI")]
    public GameObject victoryPanel;
    public TMP_Text   victoryTitleLabel;    // e.g. "MISSION COMPLETE"
    public TMP_Text   victoryScoreLabel;
    public Button     victoryRestartButton;
    public Button     victoryQuitButton;

    [Header("HUD")]
    [Tooltip("Root of the in-game HUD — hidden when game ends.")]
    public GameObject hudRoot;

    [Header("Audio")]
    public AudioClip gameOverSFX;
    public AudioClip victorySFX;

    // -------------------------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------------------------

    public enum GameState { Playing, GameOver, Victory }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    private int totalRings   = 0;
    private int ringsCollected = 0;
    private AudioSource audioSource;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
    }

    void Start()
    {
        // Count all rings in the scene at startup
        totalRings = FindObjectsByType<Ring>(FindObjectsSortMode.None).Length;

        // Hide UI panels at start
        SetPanelActive(gameOverPanel, false);
        SetPanelActive(victoryPanel,  false);

        // Wire up buttons
        gameOverRestartButton?.onClick.AddListener(RestartScene);
        gameOverQuitButton?.onClick.AddListener(QuitGame);
        victoryRestartButton?.onClick.AddListener(RestartScene);
        victoryQuitButton?.onClick.AddListener(QuitGame);

        // Subscribe to ring collection
        Ring.OnRingCollected += HandleRingCollected;
    }

    void OnDestroy()
    {
        Ring.OnRingCollected -= HandleRingCollected;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Call from PlayerController.PlaneCrash() to trigger the game over flow.
    /// </summary>
    public void TriggerGameOver()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.GameOver;
        StartCoroutine(GameOverRoutine());
    }

    // -------------------------------------------------------------------------
    // Ring tracking
    // -------------------------------------------------------------------------

    void HandleRingCollected(int points, Vector3 _)
    {
        if (CurrentState != GameState.Playing) return;

        ringsCollected++;

        if (totalRings > 0 && ringsCollected >= totalRings)
        {
            CurrentState = GameState.Victory;
            StartCoroutine(VictoryRoutine());
        }
    }

    // -------------------------------------------------------------------------
    // Coroutines
    // -------------------------------------------------------------------------

    IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(gameOverDelay);

        if (gameOverSFX != null) audioSource.PlayOneShot(gameOverSFX);

        SetPanelActive(hudRoot, false);
        SetPanelActive(gameOverPanel, true);

        // Populate score label
        int score = GetCurrentScore();
        if (gameOverScoreLabel != null)
            gameOverScoreLabel.text = $"SCORE: {score}";

        if (gameOverTitleLabel != null)
            gameOverTitleLabel.text = "MISSION FAILED";

        // Pause physics but keep UI running
        Time.timeScale = 0f;
    }

    IEnumerator VictoryRoutine()
    {
        yield return new WaitForSeconds(victoryDelay);

        if (victorySFX != null) audioSource.PlayOneShot(victorySFX);

        SetPanelActive(hudRoot, false);
        SetPanelActive(victoryPanel, true);

        int score = GetCurrentScore();
        if (victoryScoreLabel != null)
            victoryScoreLabel.text = $"SCORE: {score}";

        if (victoryTitleLabel != null)
            victoryTitleLabel.text = "MISSION COMPLETE";

        Time.timeScale = 0f;
    }

    // -------------------------------------------------------------------------
    // Button handlers
    // -------------------------------------------------------------------------

    void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    int GetCurrentScore()
    {
        // Pull score from RingScoreManager if present
        RingScoreManager rsm = FindFirstObjectByType<RingScoreManager>();
        return rsm != null ? rsm.currentScore : 0;
    }

    void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}