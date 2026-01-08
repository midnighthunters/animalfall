using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Runtime")]
    public LevelData currentLevel;
    public int targetCountNeeded;
    public float levelTimeSeconds;

    [HideInInspector] public int currentCollected;
    [HideInInspector] public float remainingTime;
    [HideInInspector] public bool isRunning;

    [Header("References")]
    public Spawner spawner;
    public UIManager ui;
    public PowerUpManager powerUpManager;
    public ScoreManager scoreManager;
    public ComboManager comboManager;
    public AudioManager audioManager;
    public CountdownController countdown;

    public UnityEvent onLevelStart;
    public UnityEvent onLevelWin;
    public UnityEvent onLevelFail;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Example: start level 1 if assigned
        if (currentLevel != null) StartLevel(currentLevel);
    }

    public void StartLevel(LevelData level)
    {
        Debug.LogFormat("[GameManager] StartLevel called for level: {0}", level != null ? level.name : "NULL");
        StopAllCoroutines(); // clear transient coroutines
        currentLevel = level;

        if (level == null)
        {
            Debug.LogError("[GameManager] StartLevel received null LevelData. Aborting start.");
            return;
        }

        // Determine target from goal: sum of all goal counts
        int goalSum = 0;
        if (level.goal != null)
        {
            goalSum =
                level.goal.chickenCount +
                level.goal.dogCount +
                level.goal.cowCount +
                level.goal.catCount +
                level.goal.monkeyCount +
                level.goal.balloonCount;
        }
        targetCountNeeded = goalSum;

        // Level time values now come from LevelManager's timer; store for UI
        levelTimeSeconds = level.timeLimit;
        currentCollected = 0;
        remainingTime = levelTimeSeconds;
        isRunning = false;

        // Defensive null checks & logs
        if (ui == null) Debug.LogWarning("[GameManager] UIManager reference is null.");
        if (spawner == null) Debug.LogWarning("[GameManager] Spawner reference is null.");
        if (powerUpManager == null) Debug.LogWarning("[GameManager] PowerUpManager reference is null.");
        if (scoreManager == null) Debug.LogWarning("[GameManager] ScoreManager reference is null.");
        if (comboManager == null) Debug.LogWarning("[GameManager] ComboManager reference is null.");
        if (audioManager == null) Debug.LogWarning("[GameManager] AudioManager reference is null.");

        ui?.UpdateTargetText(currentCollected, targetCountNeeded);
        ui?.UpdateTimer(remainingTime);
        ui?.SetProgress(0f);

        // configure spawner & start spawning
        if (spawner != null)
        {
            spawner.Setup(level);
            // spawner.StartSpawning();
            Debug.Log("[GameManager] Spawner.Setup & StartSpawning called.");
        }
        else
        {
            Debug.LogError("[GameManager] spawner is null - cannot spawn animals.");
        }

        powerUpManager?.InitForLevel(level);
        comboManager?.ResetCombo();
        scoreManager?.ResetScore();

        onLevelStart?.Invoke();
        // Start the GameManager-owned level timer coroutine so GameManager is single source of truth
        // StartCoroutine(LevelTimer());
        StartCoroutine(countdown.PlayCountdown(OnCountdownFinished));
        Debug.Log("[GameManager] LevelTimer coroutine started.");
    }

    void OnCountdownFinished()
    {
        Debug.Log("[GameManager] Countdown finished. Starting gameplay.");

        isRunning = true;
        spawner?.StartSpawning();
        StartCoroutine(LevelTimer());
    }



    IEnumerator LevelTimer()
    {
        Debug.Log("[GameManager] LevelTimer running.");
        while (isRunning)
        {
            if (powerUpManager != null && !powerUpManager.isPaused)
                remainingTime -= Time.deltaTime;
            else if (powerUpManager == null)
                remainingTime -= Time.deltaTime; // still tick if no powerUpManager

            ui?.UpdateTimer(remainingTime);

            if (remainingTime <= 0f)
            {
                remainingTime = 0;
                isRunning = false;
                Debug.Log("[GameManager] Time ran out. Ending level as failure.");
                EndLevel(false);
                yield break;
            }

            // progress bar
            float progress = targetCountNeeded > 0 ? Mathf.Clamp01((float)currentCollected / (float)targetCountNeeded) : 0f;
            ui?.SetProgress(progress);

            yield return null;
        }
        Debug.Log("[GameManager] LevelTimer ended (isRunning false).");
    }

    public void OnCorrectTap(int tapsCount = 1, int points = 50)
    {
        if (!isRunning) return;

        currentCollected += tapsCount;
        scoreManager?.AddPoints(points * tapsCount);
        comboManager?.OnCorrect();

        ui?.UpdateTargetText(currentCollected, targetCountNeeded);
        ui?.ShowFloating("+ " + (points * tapsCount), Camera.main.WorldToScreenPoint(Vector3.zero));

        Debug.LogFormat("[GameManager] Correct tap. collected={0}/{1}", currentCollected, targetCountNeeded);

        if (currentCollected >= targetCountNeeded)
        {
            isRunning = false;
            Debug.Log("[GameManager] Target reached. Ending level as success.");
            EndLevel(true);
        }
    }

    public void OnWrongTap(bool isBomb = false)
    {
        if (!isRunning) return;
        if (isBomb)
        {
            remainingTime -= currentLevel.bombTimePenalty;
            scoreManager?.AddPoints(-currentLevel.bombScorePenalty);
            ui?.ShowFloating("-" + currentLevel.bombScorePenalty, Camera.main.WorldToScreenPoint(Vector3.zero));
            audioManager?.PlaySFX(AudioManager.SfxType.Explosion);
            Debug.Log("[GameManager] Bomb tapped: applied bomb penalties.");
        }
        else
        {
            remainingTime -= currentLevel.wrongTapTimePenalty;
            scoreManager?.AddPoints(-currentLevel.wrongTapScorePenalty);
            audioManager?.PlaySFX(AudioManager.SfxType.WrongTap);
            Debug.Log("[GameManager] Wrong tap: applied generic penalties.");
        }

        ui?.UpdateTimer(Mathf.Max(remainingTime, 0f));
        comboManager?.ResetCombo();
    }

    private void EndLevel(bool success)
    {
        spawner?.StopSpawning();
        powerUpManager?.CancelAll();
        isRunning = false;

        if (success)
        {
            ui?.ShowLevelComplete();
            audioManager?.PlaySFX(AudioManager.SfxType.LevelWin);
            onLevelWin?.Invoke();
            Debug.Log("[GameManager] Level ended SUCCESS.");
            SaveManager.Instance?.AddCoins(currentLevel.rewardCoins);
        }
        else
        {
            ui?.ShowLevelFailed();
            audioManager?.PlaySFX(AudioManager.SfxType.LevelLose);
            onLevelFail?.Invoke();
            Debug.Log("[GameManager] Level ended FAIL.");
        }
    }

    // Exposed helpers
    public void AddTime(float seconds)
    {
        remainingTime += seconds;
        ui?.UpdateTimer(remainingTime);
        Debug.LogFormat("[GameManager] Added time: {0} seconds. New remaining: {1}", seconds, remainingTime);
    }
}
