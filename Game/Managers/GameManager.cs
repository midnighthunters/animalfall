// ============================================================
//  GameManager.cs  –  Animal Fall  (REFACTORED)
//  Changes vs original:
//    • All PlayerPrefs calls → SaveManager
//    • All event firing   → EventBus
//    • Direct AudioManager refs → AudioManager.Instance
//    • Eliminated mutable public references (now [SerializeField])
//    • Stars calculation added (1-star=50%, 2-star=80%, 3-star=100%)
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Runtime state ─────────────────────────────────────────
    public LevelData CurrentLevel     { get; private set; }
    public int       TargetCount      { get; private set; }
    public int       CurrentCollected { get; private set; }
    public float     RemainingTime    { get; private set; }
    public bool      IsRunning        { get; private set; }

    // ── Inspector references (no longer public API surface) ───
    [Header("Scene References")]
    [SerializeField] private Spawner           spawner;
    [SerializeField] private UIManager         ui;
    [SerializeField] private PowerUpManager    powerUpManager;
    [SerializeField] private ScoreManager      scoreManager;
    [SerializeField] private ComboManager      comboManager;
    [SerializeField] private CountdownController countdown;

    // ── Legacy events (kept for Scene-wired UnityEvents) ──────
    [Header("Legacy UnityEvents (optional)")]
    public UnityEvent onLevelStart;
    public UnityEvent onLevelWin;
    public UnityEvent onLevelFail;

    // ── Convenience shim kept for backward compat ─────────────
    // Old code: GameManager.Instance.audioManager.PlaySFX(...)
    // Now just delegates to the singleton
    public AudioManager audioManager => AudioManager.Instance;
    public ScoreManager scoreManagerRef => scoreManager;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // LevelManager drives StartLevel – nothing to do here unless standalone test
        if (LevelManager.Instance == null && CurrentLevel != null)
            StartLevel(CurrentLevel);
    }

    // ── Level flow ────────────────────────────────────────────
    public void StartLevel(LevelData level)
    {
        Debug.Log($"[GameManager] StartLevel → {level?.name ?? "NULL"}");
        StopAllCoroutines();
        CurrentLevel = level;

        if (level == null) { Debug.LogError("[GameManager] Null LevelData."); return; }

        // Count goals
        int goalSum = 0;
        if (level.goal != null)
            goalSum = level.goal.chickenCount + level.goal.dogCount  + level.goal.cowCount
                    + level.goal.catCount     + level.goal.monkeyCount + level.goal.balloonCount;

        TargetCount      = goalSum;
        CurrentCollected = 0;
        RemainingTime    = level.timeLimit;
        IsRunning        = false;

        ui?.UpdateTargetText(CurrentCollected, TargetCount);
        ui?.UpdateTimer(RemainingTime);
        ui?.SetProgress(0f);

        spawner?.Setup(level);
        powerUpManager?.InitForLevel(level);
        comboManager?.ResetCombo();
        scoreManager?.ResetScore();

        // Music
        AudioManager.Instance?.PlayMusic(AudioManager.MusicTrack.Gameplay);

        onLevelStart?.Invoke();
        EventBus.Publish(new OnLevelStarted { levelIndex = LevelManager.Instance?.CurrentLevelIndex ?? 0 });

        StartCoroutine(countdown.PlayCountdown(OnCountdownFinished));
    }

    private void OnCountdownFinished()
    {
        IsRunning = true;
        spawner?.StartSpawning();
        StartCoroutine(LevelTimer());
    }

    // ── Timer ─────────────────────────────────────────────────
    private IEnumerator LevelTimer()
    {
        while (IsRunning)
        {
            if (powerUpManager == null || !powerUpManager.isPaused)
                RemainingTime -= Time.deltaTime;

            float clamped = Mathf.Max(RemainingTime, 0f);
            ui?.UpdateTimer(clamped);
            EventBus.Publish(new OnTimerTick { remaining = clamped });

            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                IsRunning     = false;
                EndLevel(false);
                yield break;
            }

            float progress = TargetCount > 0
                ? Mathf.Clamp01((float)CurrentCollected / TargetCount)
                : 0f;
            ui?.SetProgress(progress);

            yield return null;
        }
    }

    // ── Tap handlers ──────────────────────────────────────────
    public void OnCorrectTap(int tapsCount = 1, int points = 50)
    {
        if (!IsRunning) return;

        CurrentCollected += tapsCount;
        int earned = points * tapsCount;
        scoreManager?.AddPoints(earned);
        comboManager?.OnCorrect();

        ui?.UpdateTargetText(CurrentCollected, TargetCount);
        ui?.ShowFloating("+" + earned, Camera.main.WorldToScreenPoint(Vector3.zero));

        // VFX
        VFXPoolRegistry.Instance?.Spawn(VFXPoolRegistry.Collect, Vector3.zero);

        if (CurrentCollected >= TargetCount)
        {
            IsRunning = false;
            EndLevel(true);
        }
    }

    public void OnWrongTap(bool isBomb = false)
    {
        if (!IsRunning) return;

        if (isBomb)
        {
            RemainingTime -= CurrentLevel.bombTimePenalty;
            scoreManager?.AddPoints(-CurrentLevel.bombScorePenalty);
            ui?.ShowFloating("-" + CurrentLevel.bombScorePenalty,
                Camera.main.WorldToScreenPoint(Vector3.zero));
            AudioManager.Instance?.PlaySFX(AudioManager.SfxType.Explosion);
            VFXPoolRegistry.Instance?.Spawn(VFXPoolRegistry.Explosion, Vector3.zero);
        }
        else
        {
            RemainingTime -= CurrentLevel.wrongTapTimePenalty;
            scoreManager?.AddPoints(-CurrentLevel.wrongTapScorePenalty);
            AudioManager.Instance?.PlaySFX(AudioManager.SfxType.WrongTap);
        }

        ui?.UpdateTimer(Mathf.Max(RemainingTime, 0f));
        comboManager?.ResetCombo();
    }

    public void AddTime(float seconds)
    {
        RemainingTime += seconds;
        ui?.UpdateTimer(RemainingTime);
    }

    // ── End level ─────────────────────────────────────────────
    private void EndLevel(bool success)
    {
        spawner?.StopSpawning();
        powerUpManager?.CancelAll();
        IsRunning = false;

        int finalScore  = scoreManager?.GetScore() ?? 0;
        int levelIndex  = LevelManager.Instance?.CurrentLevelIndex ?? 0;
        int stars       = CalculateStars();

        if (success)
        {
            ui?.ShowLevelComplete();
            AudioManager.Instance?.PlaySFX(AudioManager.SfxType.LevelWin);
            AudioManager.Instance?.PlayMusic(AudioManager.MusicTrack.Victory);
            VFXPoolRegistry.Instance?.Spawn(VFXPoolRegistry.LevelWin, Vector3.zero);

            SaveManager.Instance?.AddCoins(CurrentLevel.rewardCoins);
            SaveManager.Instance?.RecordLevelResult(levelIndex, finalScore, stars);

            onLevelWin?.Invoke();
            EventBus.Publish(new OnLevelCompleted
            {
                levelIndex  = levelIndex,
                score       = finalScore,
                coinsEarned = CurrentLevel.rewardCoins
            });

            // Post to leaderboard
            FirebaseManager.Instance?.PostScore(levelIndex, finalScore);

            LevelManager.Instance?.LevelSuccess();
        }
        else
        {
            ui?.ShowLevelFailed();
            AudioManager.Instance?.PlaySFX(AudioManager.SfxType.LevelLose);

            onLevelFail?.Invoke();
            EventBus.Publish(new OnLevelFailed { levelIndex = levelIndex });

            LevelManager.Instance?.LevelFailed();
        }
    }

    // ── Stars calculation ─────────────────────────────────────
    private int CalculateStars()
    {
        if (TargetCount == 0) return 3;
        float pct = (float)CurrentCollected / TargetCount;
        if (pct >= 1f)   return 3;
        if (pct >= 0.8f) return 2;
        if (pct >= 0.5f) return 1;
        return 0;
    }

    // ── Legacy shims ──────────────────────────────────────────
    // Keep these properties so existing code that reads GameManager.Instance.isRunning
    // still compiles without modifications.
    [System.Obsolete("Use IsRunning property")]
    public bool isRunning => IsRunning;
    [System.Obsolete("Use CurrentLevel property")]
    public LevelData currentLevel => CurrentLevel;
    [System.Obsolete("Use RemainingTime property")]
    public float remainingTime => RemainingTime;
    [System.Obsolete("Use CurrentCollected property")]
    public int currentCollected => CurrentCollected;
    [System.Obsolete("Use TargetCount property")]
    public int targetCountNeeded => TargetCount;
    [System.Obsolete("Use scoreManagerRef")]
    public ScoreManager scoreManager_legacy => scoreManager;
}