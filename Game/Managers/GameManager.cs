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
        StopAllCoroutines();
        currentLevel = level;

        if (level == null)
        {
            Debug.LogError("[GameManager] StartLevel received null LevelData. Aborting start.");
            return;
        }

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

        levelTimeSeconds = level.timeLimit;
        currentCollected = 0;
        remainingTime = levelTimeSeconds;
        isRunning = false;

        ui?.UpdateTargetText(currentCollected, targetCountNeeded);
        ui?.UpdateTimer(remainingTime);
        ui?.SetProgress(0f);

        if (spawner != null)
        {
            spawner.Setup(level);
            Debug.Log("[GameManager] Spawner.Setup called.");
        }

        powerUpManager?.InitForLevel(level);
        comboManager?.ResetCombo();
        scoreManager?.ResetScore();

        onLevelStart?.Invoke();
        StartCoroutine(countdown.PlayCountdown(OnCountdownFinished));
    }

    void OnCountdownFinished()
    {
        isRunning = true;
        spawner?.StartSpawning();
        StartCoroutine(LevelTimer());
    }

    IEnumerator LevelTimer()
    {
        while (isRunning)
        {
            if (powerUpManager == null || !powerUpManager.isPaused)
                remainingTime -= Time.deltaTime;

            ui?.UpdateTimer(remainingTime);

            if (remainingTime <= 0f)
            {
                remainingTime = 0;
                isRunning = false;
                EndLevel(false);
                yield break;
            }

            float progress = targetCountNeeded > 0 ? Mathf.Clamp01((float)currentCollected / (float)targetCountNeeded) : 0f;
            ui?.SetProgress(progress);

            yield return null;
        }
    }

    public void OnCorrectTap(int tapsCount = 1, int points = 50)
    {
        if (!isRunning) return;

        currentCollected += tapsCount;
        scoreManager?.AddPoints(points * tapsCount);
        comboManager?.OnCorrect();

        ui?.UpdateTargetText(currentCollected, targetCountNeeded);
        ui?.ShowFloating("+ " + (points * tapsCount), Camera.main.WorldToScreenPoint(Vector3.zero));

        if (currentCollected >= targetCountNeeded)
        {
            isRunning = false;
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
        }
        else
        {
            remainingTime -= currentLevel.wrongTapTimePenalty;
            scoreManager?.AddPoints(-currentLevel.wrongTapScorePenalty);
            audioManager?.PlaySFX(AudioManager.SfxType.WrongTap);
        }

        ui?.UpdateTimer(Mathf.Max(remainingTime, 0f));
        comboManager?.ResetCombo();
    }

    // --- FIX IS HERE ---
    private void EndLevel(bool success)
    {
        spawner?.StopSpawning();
        powerUpManager?.CancelAll();
        isRunning = false;

        if (success)
        {
            // 1. Show UI
            ui?.ShowLevelComplete();
            audioManager?.PlaySFX(AudioManager.SfxType.LevelWin);
            onLevelWin?.Invoke();

            if (SaveManager.Instance != null)
                SaveManager.Instance.AddCoins(currentLevel.rewardCoins);

            // 2. TELL LEVEL MANAGER TO PROCEED
            // This triggers the save progress and the 2-second timer to go back to Main Scene
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LevelSuccess();
            }
            else
            {
                Debug.LogError("LevelManager Instance is NULL. Cannot switch scenes.");
            }
        }
        else
        {
            ui?.ShowLevelFailed();
            audioManager?.PlaySFX(AudioManager.SfxType.LevelLose);
            onLevelFail?.Invoke();

            // Notify LevelManager of failure (optional, mostly for state tracking)
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LevelFailed();
            }
        }
    }

    public void AddTime(float seconds)
    {
        remainingTime += seconds;
        ui?.UpdateTimer(remainingTime);
    }
}