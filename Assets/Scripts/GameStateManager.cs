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

    [Header("UI Game Canvas")]
    [Tooltip("Root canvas for the in-game UI that should be disabled when game over or victory appears.")]
    public GameObject uiGameCanvas;

    [Header("Audio")]
    public AudioClip gameOverSFX;
    public AudioClip victorySFX;

    [Header("Music")]
    public AudioClip gameStartMusic;
    public AudioClip gameplayMusicLoop;
    public AudioClip bossIntroMusic;
    public AudioClip bossBattleMusicLoop;
    public AudioClip gameWonMusic;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float gameStartMusicVolume = 0.8f;
    [Range(0f, 1f)] public float gameplayMusicVolume = 0.8f;
    [Range(0f, 1f)] public float bossIntroMusicVolume = 1.0f;
    [Range(0f, 1f)] public float bossBattleMusicVolume = 0.85f;
    [Range(0f, 1f)] public float gameWonMusicVolume = 0.9f;

    [Header("Boss Intro")]
    [Tooltip("Optional explicit boss transform. If empty, first EnemyHealth that triggers victory on death is used.")]
    public Transform bossTargetOverride;
    [Tooltip("Optional reference to player follow camera script for the boss intro look-at shot.")]
    public PlaneCameraController planeCameraController;

    // -------------------------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------------------------

    public enum GameState { Playing, GameOver, Victory }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    private int totalRings   = 0;
    private int ringsCollected = 0;
    private AudioSource audioSource;
    private AudioSource musicAudioSource;
    private Coroutine startupMusicRoutine;
    private Coroutine bossIntroRoutine;
    private bool bossBattleStarted;
    private bool bossDetectionArmed;
    private Transform pendingBossTarget;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        ResetGameplayState();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;

        musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.spatialBlend = 0f;
        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = false;
        musicAudioSource.volume = musicVolume;
    }

    void Start()
    {
        // Count all rings in the scene at startup
        totalRings = FindObjectsByType<Ring>(FindObjectsSortMode.None).Length;

        // Hide UI panels at start
        SetPanelActive(hudRoot, true);
        SetPanelActive(uiGameCanvas, true);
        SetPanelActive(gameOverPanel, false);
        SetPanelActive(victoryPanel,  false);

        // Wire up buttons
        gameOverRestartButton?.onClick.AddListener(RestartScene);
        gameOverQuitButton?.onClick.AddListener(QuitGame);
        victoryRestartButton?.onClick.AddListener(RestartScene);
        victoryQuitButton?.onClick.AddListener(QuitGame);

        if (planeCameraController == null)
            planeCameraController = FindFirstObjectByType<PlaneCameraController>();

        EnemyHealth.OnEnemyEnabled += HandleEnemyEnabled;
        startupMusicRoutine = StartCoroutine(StartupMusicRoutine());

    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        EnemyHealth.OnEnemyEnabled -= HandleEnemyEnabled;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
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
        StopMusic();
        StartCoroutine(GameOverRoutine());
    }

    public void TriggerVictory()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.Victory;
        StopMusic();
        StartCoroutine(VictoryRoutine());
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
            TriggerVictory();
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
        SetPanelActive(uiGameCanvas, false);
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

        if (gameWonMusic != null)
        {
            PlayMusicClip(gameWonMusic, false, gameWonMusicVolume);
        }
        else if (victorySFX != null)
        {
            audioSource.PlayOneShot(victorySFX);
        }

        SetPanelActive(hudRoot, false);
        SetPanelActive(uiGameCanvas, false);
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
        StopMusic();
        ResetGameplayState();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void QuitGame()
    {
        StopMusic();
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

    void HandleEnemyEnabled(EnemyHealth enemy)
    {
        if (enemy == null || !enemy.TriggersVictoryOnDeath) return;
        if (CurrentState != GameState.Playing) return;
        if (bossBattleStarted) return;

        Transform bossTarget = bossTargetOverride != null ? bossTargetOverride : enemy.transform;

        if (!bossDetectionArmed)
        {
            pendingBossTarget = bossTarget;
            return;
        }

        StartBossIntro(bossTarget);
    }

    void StartBossIntro(Transform bossTarget)
    {
        bossBattleStarted = true;

        if (startupMusicRoutine != null)
        {
            StopCoroutine(startupMusicRoutine);
            startupMusicRoutine = null;
        }

        if (bossIntroRoutine != null)
            StopCoroutine(bossIntroRoutine);

        bossIntroRoutine = StartCoroutine(BossIntroRoutine(bossTarget));
    }

    IEnumerator StartupMusicRoutine()
    {
        if (gameStartMusic != null)
        {
            PlayMusicClip(gameStartMusic, false, gameStartMusicVolume);
            yield return new WaitForSecondsRealtime(gameStartMusic.length);
        }

        if (CurrentState == GameState.Playing && !bossBattleStarted && gameplayMusicLoop != null)
            PlayMusicClip(gameplayMusicLoop, true, gameplayMusicVolume);

        bossDetectionArmed = true;

        if (!bossBattleStarted && pendingBossTarget != null)
        {
            StartBossIntro(pendingBossTarget);
            pendingBossTarget = null;
        }
    }

    IEnumerator BossIntroRoutine(Transform bossTarget)
    {
        float introLength = 0f;

        UfoWarpArrivalAudio warpSfx = bossTarget != null ? bossTarget.GetComponentInParent<UfoWarpArrivalAudio>() : null;
        warpSfx?.PlayWarpIn();

        if (bossIntroMusic != null)
        {
            PlayMusicClip(bossIntroMusic, false, bossIntroMusicVolume);
            introLength = bossIntroMusic.length;
        }

        if (planeCameraController != null && bossTarget != null && introLength > 0f)
            planeCameraController.StartCinematicLookAt(bossTarget, introLength);

        if (introLength > 0f)
            yield return new WaitForSecondsRealtime(introLength);

        if (CurrentState == GameState.Playing && bossBattleMusicLoop != null)
            PlayMusicClip(bossBattleMusicLoop, true, bossBattleMusicVolume);
    }

    void PlayMusicClip(AudioClip clip, bool loop, float stageVolume = -1f)
    {
        if (musicAudioSource == null || clip == null) return;

        musicAudioSource.Stop();
        musicAudioSource.clip = clip;
        musicAudioSource.loop = loop;
        musicAudioSource.volume = stageVolume >= 0f ? Mathf.Clamp01(stageVolume) : Mathf.Clamp01(musicVolume);
        musicAudioSource.Play();
    }

    void StopMusic()
    {
        if (musicAudioSource != null)
            musicAudioSource.Stop();
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        ResetGameplayState();
    }

    void ResetGameplayState()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.Playing;
        ringsCollected = 0;
        bossBattleStarted = false;
        bossDetectionArmed = false;
        pendingBossTarget = null;
        SetPanelActive(uiGameCanvas, true);
    }
}
