using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class KillTimer : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private int startTimeSeconds = 120;
    [SerializeField] private int secondsAddedPerKill = 30;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Behavior")]
    [SerializeField] private bool triggerGameOverOnZero = true;
    
    [Header("Low Time Warning")]
    [SerializeField] private int lowTimeThreshold = 10;
    [SerializeField] private float blinkSpeed = 0.5f;

    public UnityEvent<int> onTimeChanged = new UnityEvent<int>();

    private int currentTimeSeconds;
    private Coroutine countdownRoutine;
    private Coroutine blinkRoutine;
    private readonly HashSet<EnemyHealth> subscribedEnemies = new HashSet<EnemyHealth>();

    public int CurrentTimeSeconds => currentTimeSeconds;
    public int MaxTimeSeconds => startTimeSeconds;

    private void Awake()
    {
        if (timerText == null)
            timerText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        EnemyHealth.OnEnemyEnabled += HandleEnemyEnabled;
        EnemyHealth.OnEnemyDisabled += HandleEnemyDisabled;
    }

    private void Start()
    {
        currentTimeSeconds = Mathf.Max(0, startTimeSeconds);
        UpdateTimerUI();

        SubscribeToExistingEnemies();

        if (countdownRoutine == null)
            countdownRoutine = StartCoroutine(CountdownRoutine());
    }

    private void OnDisable()
    {
        EnemyHealth.OnEnemyEnabled -= HandleEnemyEnabled;
        EnemyHealth.OnEnemyDisabled -= HandleEnemyDisabled;

        UnsubscribeFromExistingEnemies();

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
    }

    private IEnumerator CountdownRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            currentTimeSeconds = Mathf.Max(0, currentTimeSeconds - 1);
            UpdateTimerUI();

            if (currentTimeSeconds <= 0)
            {
                if (triggerGameOverOnZero && GameStateManager.Instance != null)
                    GameStateManager.Instance.TriggerGameOver();

                yield break;
            }
        }
    }

    private void HandleEnemyEnabled(EnemyHealth enemy)
    {
        if (enemy == null || subscribedEnemies.Contains(enemy)) return;
        subscribedEnemies.Add(enemy);
        enemy.onDeath.AddListener(HandleEnemyKilled);
    }

    private void HandleEnemyDisabled(EnemyHealth enemy)
    {
        if (enemy == null) return;
        subscribedEnemies.Remove(enemy);
        enemy.onDeath.RemoveListener(HandleEnemyKilled);
    }

    private void SubscribeToExistingEnemies()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy != null && !subscribedEnemies.Contains(enemy))
            {
                subscribedEnemies.Add(enemy);
                enemy.onDeath.AddListener(HandleEnemyKilled);
            }
        }
    }

    private void UnsubscribeFromExistingEnemies()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy != null && subscribedEnemies.Remove(enemy))
                enemy.onDeath.RemoveListener(HandleEnemyKilled);
        }
    }

    private void HandleEnemyKilled()
    {
        currentTimeSeconds += Mathf.Max(0, secondsAddedPerKill);
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = currentTimeSeconds / 60;
        int seconds = currentTimeSeconds % 60;
        timerText.text = $"{minutes:0}:{seconds:00}";

        // Update text color based on time remaining
        if (currentTimeSeconds <= lowTimeThreshold)
        {
            timerText.color = Color.red;
            
            // Start blinking if not already blinking
            if (blinkRoutine == null)
            {
                blinkRoutine = StartCoroutine(BlinkRoutine());
            }
        }
        else
        {
            timerText.color = Color.white;
            
            // Stop blinking if it's no longer low time
            if (blinkRoutine != null)
            {
                StopCoroutine(blinkRoutine);
                blinkRoutine = null;
                timerText.alpha = 1f;
            }
        }

        onTimeChanged?.Invoke(currentTimeSeconds);
    }

    private IEnumerator BlinkRoutine()
    {
        while (currentTimeSeconds <= lowTimeThreshold)
        {
            timerText.alpha = 0.3f;
            yield return new WaitForSeconds(blinkSpeed);
            
            timerText.alpha = 1f;
            yield return new WaitForSeconds(blinkSpeed);
        }
        blinkRoutine = null;
    }
}