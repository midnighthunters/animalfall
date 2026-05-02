using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using AnimalFall.Core.Animals;
using AnimalFall.Core.Levels;
using AnimalFall.Core.MegaLevel;
using AnimalFall.Effects;
using AnimalFall.UI;
using AnimalFall.UI.Screens;

namespace AnimalFall.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Runtime")]
        [SerializeField] private LevelData currentLevel;

        [Header("References")]
        [SerializeField] private Spawner spawner;
        [SerializeField] private GameUIManager ui;
        [SerializeField] private PowerUpManager powerUpManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private ComboManager comboManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private CountdownController countdown;

        [Header("New Systems")]
        [SerializeField] private HindranceManager hindranceManager;
        [SerializeField] private MegaLevelController megaLevelController;
        [SerializeField] private ResultsScreenController resultsScreen;
        [SerializeField] private VillainHUD villainHUD;

        [Header("Events")]
        public UnityEvent onLevelStart;
        public UnityEvent onLevelWin;
        public UnityEvent onLevelFail;

        public AudioManager AudioManager => audioManager;
        public LevelData CurrentLevel => currentLevel;

        private int targetCountNeeded;
        private int currentCollected;
        private float remainingTime;
        private bool isRunning;

        public bool IsRunning => isRunning;
        public int CurrentCollected => currentCollected;
        public int TargetCount => targetCountNeeded;
        public float RemainingTime => remainingTime;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (currentLevel != null)
                StartLevel(currentLevel);
        }

        public void StartLevel(LevelData level)
        {
            StopAllCoroutines();
            currentLevel = level;

            if (level == null)
            {
                Debug.LogError("[GameManager] StartLevel received null LevelData.");
                return;
            }

            targetCountNeeded = level.goal != null ? level.goal.TotalCount : 0;
            currentCollected = 0;
            remainingTime = level.timeLimit;
            isRunning = false;

            ui?.UpdateTargetText(currentCollected, targetCountNeeded);
            ui?.UpdateTimer(remainingTime);
            ui?.SetProgress(0f);

            spawner?.Setup(level);
            powerUpManager?.InitForLevel(level);
            comboManager?.ResetCombo();
            scoreManager?.ResetScore();

            if (hindranceManager != null)
                hindranceManager.InitForLevel(level);

            if (level.isMegaLevel && megaLevelController != null)
            {
                megaLevelController.InitMegaLevel(level);
                if (villainHUD != null && megaLevelController.ActiveVillain != null)
                    villainHUD.Setup(megaLevelController.ActiveVillain);
            }

            onLevelStart?.Invoke();
            StartCoroutine(countdown.PlayCountdown(OnCountdownFinished));
        }

        private void OnCountdownFinished()
        {
            isRunning = true;
            spawner?.StartSpawning();
            hindranceManager?.StartSpawning();
            StartCoroutine(LevelTimer());
        }

        private IEnumerator LevelTimer()
        {
            while (isRunning)
            {
                if (powerUpManager == null || !powerUpManager.IsPaused)
                    remainingTime -= Time.deltaTime;

                ui?.UpdateTimer(remainingTime);

                if (remainingTime <= 0f)
                {
                    remainingTime = 0;
                    isRunning = false;
                    EndLevel(false);
                    yield break;
                }

                if (LivesManager.Instance != null && !LivesManager.Instance.HasLives())
                {
                    isRunning = false;
                    EndLevel(false);
                    yield break;
                }

                float progress = targetCountNeeded > 0
                    ? Mathf.Clamp01((float)currentCollected / targetCountNeeded)
                    : 0f;
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
            ui?.ShowFloatingText("+" + (points * tapsCount), Camera.main.WorldToScreenPoint(Vector3.zero));

            EventManager.Instance?.CheckQuestProgress("animals_collected", tapsCount);

            if (currentCollected >= targetCountNeeded)
            {
                if (currentLevel.isMegaLevel && megaLevelController != null)
                {
                    megaLevelController.OnAnimalQuotaMet();
                }
                else
                {
                    isRunning = false;
                    EndLevel(true);
                }
            }
        }

        public void OnWrongTap(bool isBomb = false)
        {
            if (!isRunning) return;

            if (isBomb)
            {
                remainingTime -= currentLevel.bombTimePenalty;
                scoreManager?.AddPoints(-currentLevel.bombScorePenalty);
                ui?.ShowFloatingText("-" + currentLevel.bombScorePenalty, Camera.main.WorldToScreenPoint(Vector3.zero));
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

        public void AddTime(float seconds)
        {
            remainingTime += seconds;
            ui?.UpdateTimer(remainingTime);
        }

        public void OnMegaLevelComplete()
        {
            isRunning = false;
            EndLevel(true);
        }

        private void EndLevel(bool success)
        {
            spawner?.StopSpawning();
            powerUpManager?.CancelAll();
            hindranceManager?.StopAll();
            isRunning = false;

            ScreenEffects.Instance?.ClearAll();
            EnvironmentEffects.Instance?.ClearAll();

            int score = scoreManager != null ? scoreManager.GetScore() : 0;

            if (success)
            {
                audioManager?.PlaySFX(AudioManager.SfxType.LevelWin);
                onLevelWin?.Invoke();

                if (Services.Save.SaveService.Instance != null)
                    Services.Save.SaveService.Instance.AddCoins(currentLevel.rewardCoins);

                EventManager.Instance?.CheckQuestProgress("levels_completed", 1);
                EventManager.Instance?.CheckQuestProgress("score_earned", score);

                if (LevelManager.Instance != null)
                    LevelManager.Instance.LevelSuccess();

                if (resultsScreen != null)
                    resultsScreen.ShowWin(score, currentLevel.rewardCoins, currentLevel.isMegaLevel);
                else
                    ui?.ShowLevelComplete(score, currentLevel.rewardCoins);
            }
            else
            {
                audioManager?.PlaySFX(AudioManager.SfxType.LevelLose);
                onLevelFail?.Invoke();

                if (LivesManager.Instance != null)
                    LivesManager.Instance.UseLife();

                if (LevelManager.Instance != null)
                    LevelManager.Instance.LevelFailed();

                if (resultsScreen != null)
                    resultsScreen.ShowLose(score);
                else
                    ui?.ShowLevelFailed(score);
            }

            if (currentLevel.isMegaLevel)
            {
                megaLevelController?.Cleanup();
                villainHUD?.Hide();
            }
        }
    }
}
