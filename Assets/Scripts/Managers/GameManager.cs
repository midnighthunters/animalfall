// GameManager — gameplay loop: setup → 3-2-1-GO → run → win/lose
using UnityEngine;
using DG.Tweening;
using System.Collections;
using AnimalFall.Core;
using AnimalFall.Core.Animals;
using AnimalFall.Core.MegaLevel;
using AnimalFall.Data;
using AnimalFall.Effects;
using AnimalFall.UI;
using AnimalFall.Utils;

namespace AnimalFall.Managers
{
    public enum GameState { Idle, Countdown, Running, Ended }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Core References")]
        [SerializeField] private Spawner            _spawner;
        [SerializeField] private HindranceManager   _hindranceManager;
        [SerializeField] private ScoreManager       _scoreManager;
        [SerializeField] private ComboManager       _comboManager;
        [SerializeField] private AudioManager       _audioManager;
        [SerializeField] private PowerUpManager     _powerUpManager;
        [SerializeField] private LivesManager       _livesManager;
        [SerializeField] private InputManager       _inputManager;
        [SerializeField] private MegaLevelController _megaLevelController;
        [SerializeField] private Camera             _camera;
        [SerializeField] private EnvironmentEffects _envEffects;
        [SerializeField] private GoalTracker        _goalTracker;
        [SerializeField] private GameHUD            _hud;
        [SerializeField] private CountdownController _countdown;
        [SerializeField] private VictoryOverlay     _victoryOverlay;

        [Header("Auto-Start")]
        [Tooltip("LevelData asset used when this scene loads directly.")]
        [SerializeField] private LevelData _autoStartLevel;

        [Header("Look")]
        [SerializeField] private Color _plainBackground = new Color(0.55f, 0.78f, 0.95f, 1f);

        public GameState State         { get; private set; } = GameState.Idle;
        public float     RemainingTime { get; private set; }

        private LevelData _currentLevel;
        private bool      _timerWarningFired;
        private bool      _levelStarted;
        private bool      _tutorialPaused;
        private Coroutine _tutorialPauseRoutine;
        private float     _tutorialPreviousTimeScale = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ImageLibrary.LoadAll();

            // Prefer LevelManager's current level (from map), else auto-start asset
            LevelData level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : null;
            if (level == null) level = _autoStartLevel;
            if (level == null)
            {
                Debug.LogError("[GameManager] No LevelData assigned — cannot start.");
                return;
            }

            // Pre-warm animal pool
            var pooler = ObjectPooler.Instance;
            if (pooler != null)
            {
                var animalPrefab = GetAnimalPrefab();
                if (animalPrefab != null)
                    pooler.CreatePool(animalPrefab, level.MaxOnScreen + 6);
            }

            BeginLevel(level);
        }

        public void StartLevel(LevelData level)
        {
            if (_levelStarted) { Debug.LogError("[GameManager] StartLevel: already running."); return; }
            BeginLevel(level);
        }

        /// <summary>
        /// Sets the direct-GameScene fallback before Start executes. Used by the development
        /// level-jump component; normal map launches still take precedence through LevelManager.
        /// </summary>
        public void SetDirectStartLevel(LevelData level) => _autoStartLevel = level;

        public void OnCorrectTap(int basePoints)
        {
            if (State != GameState.Running) return;
            _scoreManager?.AddPoints(basePoints);
            _comboManager?.OnCorrect();
        }

        public void OnWrongTap()
        {
            if (State != GameState.Running) return;
            float penalty = _currentLevel != null ? _currentLevel.WrongTapTimePenalty : 1f;
            AddTime(-penalty);
            _comboManager?.OnWrong();
            GameEvents.OnWrongTap?.Invoke();
        }

        public void AddTime(float delta)
        {
            if (_currentLevel == null || State != GameState.Running) return;
            RemainingTime = Mathf.Clamp(RemainingTime + delta, 0f, _currentLevel.TimeLimit);
            if (RemainingTime <= 0f) EndLevel(false);
        }

        public void PauseForHindranceTutorial(float seconds)
        {
            if (State != GameState.Running) return;
            if (_tutorialPauseRoutine != null) StopCoroutine(_tutorialPauseRoutine);
            _tutorialPauseRoutine = StartCoroutine(TutorialPauseRoutine(seconds));
        }

        private IEnumerator TutorialPauseRoutine(float seconds)
        {
            _tutorialPaused = true;
            _inputManager?.BlockInput(true);
            _tutorialPreviousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(Mathf.Max(1f, seconds));
            Time.timeScale = _tutorialPreviousTimeScale;
            _inputManager?.BlockInput(false);
            _tutorialPaused = false;
            _tutorialPauseRoutine = null;
        }

        public void OnMegaLevelComplete() => EndLevel(true);

        public void EndLevel(bool won)
        {
            if (State == GameState.Ended) return;
            State = GameState.Ended;
            _spawner?.StopSpawning();
            _hindranceManager?.StopHindrances();
            _envEffects?.ClearAll();
            ScreenEffects.Instance?.ClearAll();
            CancelTutorialPause();
            _inputManager?.BlockInput(true);

            if (_goalTracker != null)
                _goalTracker.OnAllGoalsComplete -= OnGoalsComplete;

            if (won)
            {
                GameEvents.OnLevelWon?.Invoke();
                if (_currentLevel != null)
                    LevelManager.Instance?.LevelSuccess(_currentLevel.LevelNumber - 1);
            }
            else
            {
                GameEvents.OnLevelFailed?.Invoke();
                _livesManager?.UseLife();
            }
            _levelStarted = false;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void BeginLevel(LevelData level)
        {
            _levelStarted      = true;
            _currentLevel      = level;
            RemainingTime      = level.TimeLimit;
            _timerWarningFired = false;
            State              = GameState.Countdown;

            _scoreManager?.ResetScore();
            _comboManager?.ResetCombo();
            _powerUpManager?.Reset();
            _envEffects?.ClearAll();
            ScreenEffects.Instance?.ClearAll();
            CancelTutorialPause();
            _hud?.ResetWarning();

            // Plain solid background
            if (_camera != null)
            {
                DOTween.Kill(_camera);
                _camera.backgroundColor = _plainBackground;
            }

            // Disable world background sprites if present
            var worldBg = GameObject.Find("WorldBackground");
            if (worldBg != null) worldBg.SetActive(false);

            _goalTracker?.Setup(level.Goal);
            if (_goalTracker != null)
                _goalTracker.OnAllGoalsComplete += OnGoalsComplete;

            _hud?.Setup(level);

            if (level.IsMegaLevel && level.Villain != null)
                _megaLevelController?.InitMegaLevel(level);

            // Block input during countdown; no spawning yet
            _inputManager?.BlockInput(true);
            _spawner?.StopSpawning();

            GameEvents.OnLevelStarted?.Invoke(level.LevelNumber);
            Debug.Log($"[GameManager] Level {level.LevelNumber} ready — starting countdown. Timer={level.TimeLimit}s");

            if (_countdown != null)
                _countdown.PlayCountdown(StartGameplay);
            else
                StartGameplay();
        }

        private void StartGameplay()
        {
            if (_currentLevel == null) return;
            State = GameState.Running;
            RemainingTime = _currentLevel.TimeLimit;
            _timerWarningFired = false;

            _inputManager?.BlockInput(false);
            if (!_currentLevel.IsMegaLevel || _currentLevel.AllowNormalHindrancesInMegaLevel)
                _hindranceManager?.InitForLevel(_currentLevel);
            _spawner?.Setup(_currentLevel);
            _spawner?.StartSpawning();

            Debug.Log("[GameManager] Gameplay started — timer running.");
        }

        private void OnGoalsComplete()
        {
            if (State != GameState.Running) return;
            EndLevel(true);
        }

        private void CancelTutorialPause()
        {
            if (_tutorialPauseRoutine != null) StopCoroutine(_tutorialPauseRoutine);
            _tutorialPauseRoutine = null;
            if (_tutorialPaused) Time.timeScale = _tutorialPreviousTimeScale;
            _tutorialPaused = false;
        }

        private GameObject GetAnimalPrefab()
        {
            if (_spawner == null) return null;
            var f = typeof(Spawner).GetField("_animalPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f?.GetValue(_spawner) as GameObject;
        }

        private void Update()
        {
            if (State != GameState.Running) return;
            if (_tutorialPaused) return;

            RemainingTime -= Time.deltaTime;
            if (!_timerWarningFired && RemainingTime < 10f)
            {
                _timerWarningFired = true;
                GameEvents.OnTimerWarning?.Invoke();
            }
            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                EndLevel(false);
            }
        }
    }
}
